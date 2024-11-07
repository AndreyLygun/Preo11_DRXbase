using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Intelligence.AIManagersAssistant;

namespace Sungero.Intelligence.Client
{
  partial class AIManagersAssistantFunctions
  {

    /// <summary>
    /// Проверить, что свойство-коллекция - это классификаторы виртуального ассистента.
    /// </summary>
    /// <param name="collection">Коллекция.</param>
    /// <param name="rootEntity">Родительская сущность.</param>
    /// <returns>Результат проверки.</returns>
    public static bool CollectionIsAIAssistantClassifiers(Sungero.Domain.Shared.IChildEntityCollection<Sungero.Domain.Shared.IChildEntity> collection,
                                                          Sungero.Domain.Shared.IEntity rootEntity)
    {
      var virtualAssistant = Intelligence.AIManagersAssistants.As(rootEntity);
      return virtualAssistant != null && collection == virtualAssistant.Classifiers;
    }

    /// <summary>
    /// Показать диалог обучения классификатора.
    /// </summary>
    /// <returns>True - если начата подготовка данных для обучения, иначе - False.</returns>
    public virtual bool ShowClassifierTrainingDialog()
    {
      var assigneeClassifier = _obj.Classifiers.Where(x => x.ClassifierType == AIManagersAssistantClassifiers.ClassifierType.Assignee);
      if (assigneeClassifier.Any(x => x.TrainingStartDate.HasValue))
      {
        var previousTrainingDate = assigneeClassifier.First().TrainingStartDate.Value;
        Dialogs.NotifyMessage(AIManagersAssistants.Resources.ClassifierTrainingAlreadyStartedFormat(previousTrainingDate));
        return false;
      }
      
      var dialog = Dialogs.CreateInputDialog(AIManagersAssistants.Resources.ClassifierTrainingDialogTitle);
      // Принудительно увеличить ширину диалога для корректного отображения кнопок, хинта и заголовка.
      var fakeControl = dialog.AddDate("123456789012345678901", false);
      fakeControl.IsVisible = false;
      var startDate = dialog.AddDate(AIManagersAssistants.Resources.DateFrom, false);
      var today = Calendar.Today.EndOfDay();
      var trainButton = dialog.Buttons.AddCustom(AIManagersAssistants.Resources.TrainButton);
      var minTrainingSetSize = RecordManagement.PublicFunctions.Module.Remote.GetMinTrainingSetSizeForPublishingClassifierModelValue();
      dialog.Buttons.AddCancel();
      dialog.SetOnRefresh(
        (e) =>
        {
          if (assigneeClassifier.Any(x => x.ModelId.HasValue))
            e.AddWarning(AIManagersAssistants.Resources.TraininResultsWillReset);
          if (startDate.Value == null)
            e.AddInformation(AIManagersAssistants.Resources.EmptyDateFromMinTrainCountFormat(minTrainingSetSize));
        });
      dialog.SetOnButtonClick(
        (e) =>
        {
          if (e.Button != trainButton || !e.IsValid)
            return;
          
          if (startDate.Value != null && startDate.Value > today)
            e.AddError(AIManagersAssistants.Resources.WrongDatePeriod);
        });

      if (dialog.Show() != trainButton)
        return false;
      
      // Создать классификатор, он еще не создан.
      if (!assigneeClassifier.Any(x => x.ClassifierId.HasValue) && !PublicFunctions.AIManagersAssistant.Remote.CreateAssigneeClassifier(_obj))
      {
        Dialogs.ShowMessage(AIManagersAssistants.Resources.FailedToCreateClassifier, MessageType.Error);
        return false;
      }
      
      // Распубликовать модель, если она существует.
      if (assigneeClassifier.Any(x => x.ModelId.HasValue))
      {
        var errorMessage = PublicFunctions.AIManagersAssistant.Remote.ResetClassifierTraining(_obj, AIManagersAssistantClassifiers.ClassifierType.Assignee);
        if (!string.IsNullOrEmpty(errorMessage))
        {
          Dialogs.ShowMessage(AIManagersAssistants.Resources.FailedToUnpublishClassifierModel, MessageType.Error);
          return false;
        }
      }

      // Сохранить в таблице с классификаторами дату и время начала обучения для контроля последующих запусков АО.
      assigneeClassifier.First().TrainingStartDate = Calendar.Now;
      _obj.Save();
      
      RecordManagement.PublicFunctions.Module.Remote.CreatePrepareAIAssistantsTrainingAsyncHandler(_obj.Id, startDate.Value, today, 0, null, true);
      
      return true;
    }

    /// <summary>
    /// Сбросить результаты обучения классификатора.
    /// </summary>
    /// <param name="classifierType">Тип классификатора.</param>
    public virtual void ResetClassifierTraining(Enumeration classifierType)
    {
      var classifier = _obj.Classifiers.FirstOrDefault(x => x.ClassifierId.HasValue && x.ClassifierType == classifierType);
      if (classifier == null)
        return;
      
      var successfullyUnpublished = SmartProcessing.PublicFunctions.Module.Remote.UnpublishClassifierModel(classifier.ClassifierId.Value);
      if (successfullyUnpublished)
      {
        classifier.ModelId = null;
        classifier.IsModelActive = Intelligence.AIManagersAssistantClassifiers.IsModelActive.No;
        _obj.Save();
        Dialogs.NotifyMessage(string.Format(SmartProcessing.Resources.UnpublishModelSuccess, classifier.ClassifierId.Value, classifier.ClassifierName));
      }
      else
      {
        var errorMessage = string.Format(SmartProcessing.Resources.UnpublishModelError, classifier.ClassifierId.Value, classifier.ClassifierName);
        Dialogs.NotifyMessage(errorMessage);
        Logger.Debug(errorMessage);
      }
    }

  }
}