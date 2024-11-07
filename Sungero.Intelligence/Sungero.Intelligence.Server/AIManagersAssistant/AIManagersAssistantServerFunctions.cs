using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Intelligence.AIManagersAssistant;

namespace Sungero.Intelligence.Server
{
  partial class AIManagersAssistantFunctions
  {
    /// <summary>
    /// Создать классификатор по ответственному исполнителю.
    /// </summary>
    /// <returns>True - если классификатор успешно создан, False - в противном случае.</returns>
    [Public, Remote]
    public virtual bool CreateAssigneeClassifier()
    {
      return this.CreateClassifier(Intelligence.AIManagersAssistantClassifiers.ClassifierType.Assignee).HasValue;
    }
    
    /// <summary>
    /// Создать классификатор.
    /// </summary>
    /// <param name="classifierType">Тип классификатора.</param>
    /// <returns>ИД созданного классификатора.</returns>
    [Public]
    public virtual int? CreateClassifier(Enumeration classifierType)
    {
      try
      {
        var classifierTypeName = _obj.Info.Properties.Classifiers.Properties.ClassifierType.GetLocalizedValue(classifierType);
        var classifierName = string.Format("{0} {1}. {2}", _obj.Info.LocalizedName, _obj.Manager.Id, classifierTypeName);
        var minProbability = Constants.AIManagersAssistant.LowerClassificationLimit / 100d;
        var classifierId = SmartProcessing.PublicFunctions.Module.CreateClassifier(classifierName, minProbability, false);
        var classifier = _obj.Classifiers.AddNew();
        classifier.ClassifierName = classifierName;
        classifier.ClassifierType = classifierType;
        classifier.ClassifierId = classifierId;
        _obj.Save();
        Logger.DebugFormat("CreateClassifier. New classifier added for AI manager assistant, ClassifierId={0}, AIManagersAssistantId={1}", classifierId, _obj.Id);
        return classifierId;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("CreateClassifier. Error creating classifier, AIManagersAssistantId={0}", ex, _obj.Id);
        return null;
      }
    }

    /// <summary>
    /// Сбросить результаты обучения классификатора.
    /// </summary>
    /// <param name="classifierType">Тип классификатора.</param>
    /// <returns>Сообщение об ошибке, либо пустая строка, если модель классификатора успешно распубликована.</returns>
    [Public, Remote]
    public virtual string ResetClassifierTraining(Enumeration classifierType)
    {
      var classifier = _obj.Classifiers.FirstOrDefault(x => x.ClassifierType == classifierType && x.ClassifierId.HasValue && x.ModelId.HasValue);
      if (classifier == null)
        return AIManagersAssistants.Resources.ClassifierModelNotFound;
      
      var logTemplate = "ResetClassifierTraining. {0}. ClassifierName={1}, ClassifierId={2}, ModelId={3}.";
      if (classifier.IsModelActive == Intelligence.AIManagersAssistantClassifiers.IsModelActive.Yes)
      {
        var successfullyUnpublished = SmartProcessing.PublicFunctions.Module.Remote.UnpublishClassifierModel(classifier.ClassifierId.Value);
        
        if (!successfullyUnpublished)
        {
          Logger.ErrorFormat(logTemplate, "Unpublish model error", classifier.ClassifierName, classifier.ClassifierId.Value, classifier.ModelId.Value);
          return string.Format(SmartProcessing.Resources.UnpublishModelError, classifier.ClassifierId.Value, classifier.ClassifierName);
        }
        
        Logger.DebugFormat(logTemplate, "Model unpublished successfully", classifier.ClassifierName, classifier.ClassifierId.Value, classifier.ModelId.Value);
      }
      else
      {
        Logger.DebugFormat(logTemplate, "Model is inactive, no need to unpublish in Ario", classifier.ClassifierName, classifier.ClassifierId.Value, classifier.ModelId.Value);
      }
      classifier.ModelId = null;
      classifier.IsModelActive = Intelligence.AIManagersAssistantClassifiers.IsModelActive.No;
      _obj.Save();
      return string.Empty;
    }
  }
}