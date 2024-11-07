using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement;

namespace Sungero.Intelligence.Server
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Сбросить дату начала обучения в справочнике виртуальных ассистентов.
    /// </summary>
    /// <param name="assistantId">ИД виртаульного ассистента.</param>
    /// <param name="classifierType">Тип классификатора.</param>
    /// <returns>Сообщение об ошибке или пустая строка.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual string ResetAIAssistantTrainingStartDate(long assistantId, string classifierType)
    {
      try
      {
        var assistant = AIManagersAssistants.GetAll(x => x.Id == assistantId).FirstOrDefault();
        if (assistant == null)
          return string.Format("AI managers assistant with ID {0} not found.", assistantId);
        var classifierInfo = assistant.Classifiers.FirstOrDefault(x => x.ClassifierType.ToString() == classifierType);
        if (classifierInfo == null)
          return string.Format("Classifier type {0} not found.", classifierType);
        if (!classifierInfo.TrainingStartDate.HasValue)
          return "Training start date is not filled in.";
        var lockInfo = Locks.GetLockInfo(assistant);
        if (lockInfo.IsLockedByOther)
          return string.Format("AI managers assistant is locked by \"{0}\".", lockInfo.OwnerName);
        
        classifierInfo.TrainingStartDate = null;
        assistant.Save();
        return string.Empty;
      }
      catch (Exception ex)
      {
        var error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        return string.Format("Failed to reset training start date: {0}", error);
      }
    }
    
    /// <summary>
    /// Создать классификатор для виртуального ассистента.
    /// </summary>
    /// <param name="virtualAssistantId">Ид виртуального ассистента.</param>
    /// <param name="classifierType">Тип классификатора.</param>
    /// <returns>Ид классификатора при успешном создании, иначе null.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual int? CreateVirtualAssistantClassifier(long virtualAssistantId, string classifierType)
    {
      var logTemplate = string.Format("ClassifierTraining. CreateVirtualAssistantClassifier. {{0}}, assistantId={0}, classifierType={1}", virtualAssistantId, classifierType);
      
      if (string.IsNullOrWhiteSpace(classifierType))
      {
        Logger.ErrorFormat(logTemplate, "Failed to create classifier. Empty classifierType passed");
        return null;
      }
      
      var assistant = Intelligence.AIManagersAssistants.GetAll(x => x.Id == virtualAssistantId).FirstOrDefault();
      if (assistant == null)
      {
        Logger.ErrorFormat(logTemplate, "Failed to create classifier. AI assistant not found");
        return null;
      }
      if (Locks.GetLockInfo(assistant).IsLocked)
      {
        Logger.ErrorFormat(logTemplate, "Failed to create classifier. AI assistant is locked");
        return null;
      }
      
      return Intelligence.PublicFunctions.AIManagersAssistant.CreateClassifier(assistant, new Enumeration(classifierType));
    }

  }
}