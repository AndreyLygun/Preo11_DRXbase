using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using ExtractTextStatus = Sungero.SmartProcessing.ExtractTextQueueItem.ProcessingStatus;

namespace Sungero.RecordManagement.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// Запуск обучения классификатора поручений.
    /// </summary>
    public virtual void StartAIAssistantTraining()
    {
      // Необходима лицензия на модуль "Интеллектуальные функции".
      var classifierType = Sungero.Intelligence.AIManagersAssistantClassifiers.ClassifierType.Assignee;
      if (!Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Sungero.Commons.PublicConstants.Module.IntelligenceGuid))
      {
        Logger.DebugFormat("ClassifierTraining. StartAIAssistantTraining. Module license \"Intelligence\" not found, classifierType={0}", classifierType);
        return;
      }
      
      // Для элементов очереди в процессе, попытаться завершить обучение.
      Functions.Module.TryFinalizeTrainQueueItemsInProcess();
      Functions.Module.AIAssistantTrain(classifierType);
      Functions.Module.DeleteObsoleteTrainQueueItems();
    }

    /// <summary>
    /// Подготовка данных обучения классификаторов Ario, используемых для авто создания проектов поручений.
    /// </summary>
    public virtual void PrepareDataForAIAssistantTraining()
    {
      // Необходима лицензия на модуль "Интеллектуальные функции".
      var classifierType = Sungero.Intelligence.AIManagersAssistantClassifiers.ClassifierType.Assignee;
      if (!Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Sungero.Commons.PublicConstants.Module.IntelligenceGuid))
      {
        Functions.Module.ClearLastActionItemTrainQueueDate();
        Logger.DebugFormat("ClassifierTraining. PrepareDataForAIAssistantTraining. Module license \"Intelligence\" not found, classifierType={0}", classifierType);
        return;
      }
      
      // Обработать элементы очереди на извлечение текста со статусом "В процессе" - получить результаты из Ario.
      var queueItems = Functions.Module.GetExtractTextQueueItemsForAssistant(ExtractTextStatus.InProcess).OrderByDescending(x => x.Created).ToList();
      if (queueItems.Any())
        SmartProcessing.PublicFunctions.Module.ProcessTextExtractionTasks(queueItems);

      // Создать элементы очередей на обучение и извлечение текста для новых поручений.
      // Период выборки поручений определяется со времени предыдущего запуска ФП по текущий момент.
      var lastRun = Functions.Module.GetLastActionItemTrainQueueDate();
      var periodEnd = Calendar.Now;
      if (lastRun.HasValue)
      {
        var periodBegin = lastRun.Value.AddMilliseconds(1);
        Functions.Module.EnqueueActionItemsForAIAssistantTraining(periodBegin, periodEnd, classifierType);
      }
      Functions.Module.SetLastActionItemTrainQueueDate(periodEnd);
      
      // Обработать элементы очереди на извлечение текста со статусом "Ожидание обработки" - отправить запрос на извлечение текста в Ario.
      queueItems = Functions.Module.GetExtractTextQueueItemsForAssistant(ExtractTextStatus.Awaiting).OrderBy(x => x.Id).ToList();
      if (queueItems.Any())
      {
        var queueItemsInProcessCount = Functions.Module.GetExtractTextQueueItemsForAssistant(ExtractTextStatus.InProcess).Count();
        SmartProcessing.PublicFunctions.Module.SendDocumentsForTextExtraction(queueItems, queueItemsInProcessCount);
      }
    }
  }
}