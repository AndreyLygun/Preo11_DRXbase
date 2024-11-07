using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.Metadata;

namespace Sungero.SmartProcessing.Server
{
  public class ModuleAsyncHandlers
  {

    public virtual void ElasticsearchReindex(Sungero.SmartProcessing.Server.AsyncHandlerInvokeArgs.ElasticsearchReindexInvokeArgs args)
    {
      var start = Calendar.Now;
      Logger.Debug("ElasticsearchReindex. Reindex started.");
      args.Retry = false;
      
      Functions.Module.RemoveAllIndicesExistFromDocflowParams();
      Sungero.Parties.PublicFunctions.Module.ReindexCompanyBases();
      Sungero.Parties.PublicFunctions.Module.ReindexContacts();
      Sungero.Company.PublicFunctions.Module.ReindexBusinessUnits();
      Sungero.Company.PublicFunctions.Module.ReindexEmployees();
      Functions.Module.UpdateDocflowParamsIfAllIndicesExist();
      
      var processingTime = (int)Math.Round(Calendar.Now.Subtract(start).TotalSeconds);
      Logger.DebugFormat("ElasticsearchReindex. Reindex finished. Processing time: {0} sec.", processingTime);
    }

    /// <summary>
    /// Асинхронный обработчик для простановки статуса обучения классификатора в результатах распознавания.
    /// </summary>
    /// <param name="args">RecognitionInfoId - ИД результата распознавания, TrainingStatus - статус обучения.</param>
    public virtual void SetClassifierTrainingStatus(Sungero.SmartProcessing.Server.AsyncHandlerInvokeArgs.SetClassifierTrainingStatusInvokeArgs args)
    {
      var info = Commons.EntityRecognitionInfos.GetAll(x => x.Id == args.RecognitionInfoId).FirstOrDefault();
      if (info == null)
      {
        Logger.ErrorFormat("ClassifierTraining. SetClassifierTrainingStatus. Entity recognition info not found, recognitionInfoId={0}",
                           args.RecognitionInfoId);
        return;
      }
      var lockInfo = Locks.GetLockInfo(info);
      if (lockInfo.IsLockedByOther)
      {
        Logger.DebugFormat("ClassifierTraining. SetClassifierTrainingStatus. Entity recognition info is locked by \"{0}\", recognitionInfoId={1}",
                           lockInfo.OwnerName, args.RecognitionInfoId);
        args.Retry = true;
        return;
      }
      
      Logger.DebugFormat("ClassifierTraining. SetClassifierTrainingStatus. Changing status async to \"{0}\", classifierType={1}, recognitionInfoId={2}",
                         args.TrainingStatus,
                         args.ClassifierType,
                         args.RecognitionInfoId);
      
      var trainingStatus = new Enumeration(args.TrainingStatus);
      var classifierType = !string.IsNullOrEmpty(args.ClassifierType) ?
        new Enumeration(args.ClassifierType) :
        Commons.ClassifierTrainingSession.ClassifierType.DocType;
      if (args.TrainingStatus == Constants.Module.ClassifierTrainingStatusNames.Awaiting)
        Commons.PublicFunctions.EntityRecognitionInfo.ResetPropertiesToAwaitTraining(info, classifierType);
      else
        Commons.PublicFunctions.EntityRecognitionInfo.FillTrainingProperties(info, classifierType, trainingStatus);
    }
    
    /// <summary>
    /// Асинхронный обработчик для проверки статуса дообучения в Ario.
    /// </summary>
    /// <param name="args">ClassifierTrainingSessionId - ИД сессии обучения классификатора.</param>
    public virtual void TrainClassifier(Sungero.SmartProcessing.Server.AsyncHandlerInvokeArgs.TrainClassifierInvokeArgs args)
    {
      args.Retry = false;
      var trainingSessionId = args.ClassifierTrainingSessionId;
      Logger.DebugFormat("ClassifierTraining. TrainClassifier. Start async handler, iteration={0}, sessionId={1}",
                         args.RetryIteration, trainingSessionId);
      
      var trainingSession = Sungero.Commons.ClassifierTrainingSessions.GetAll(x => x.Id == trainingSessionId).FirstOrDefault();
      if (trainingSession == null)
      {
        Logger.ErrorFormat("ClassifierTraining. TrainClassifier. Training session not found, sessionId={0}", trainingSessionId);
        return;
      }
      
      if (args.RetryIteration > Sungero.SmartProcessing.Constants.Module.ClassifierTrainingRetryLimit)
      {
        Logger.DebugFormat("ClassifierTraining. TrainClassifier. Async handler retry limit exceeded, sessionId={0}", trainingSessionId);
        Sungero.SmartProcessing.Functions.Module.SetClassifierTrainingStatus(trainingSession, new Enumeration(Constants.Module.ClassifierTrainingStatusNames.Awaiting));
        Sungero.Commons.PublicFunctions.ClassifierTrainingSession.SetSessionStatus(trainingSession, new Enumeration("Rollback"));
        return;
      }
      
      var arioTaskInfo = SmartProcessing.Functions.Module.GetTrainingTaskInfo(trainingSession.ArioTaskId.Value);
      // Обработка ошибок подключения к сервисам Ario.
      if (arioTaskInfo == null)
      {
        Logger.ErrorFormat("ClassifierTraining. TrainClassifier. Error while getting result from Ario, arioTaskId={0}, sessionId={1}",
                           trainingSession.ArioTaskId.Value, trainingSessionId);
        args.Retry = true;
        return;
      }
      
      // Обработка ошибок.
      var arioTask = arioTaskInfo.Task;
      var arioTaskStatus = arioTask.State;
      
      if (arioTaskStatus == Constants.Module.ProcessingTaskStates.Terminated ||
          (arioTaskStatus == Constants.Module.ProcessingTaskStates.ErrorOccurred && arioTaskInfo.Result == "Aborted after restart."))
      {
        Logger.ErrorFormat("ClassifierTraining. TrainClassifier. Ario task terminated, arioTaskId={0}, sessionId={1}",
                           arioTask.Id, trainingSessionId);
        Sungero.SmartProcessing.Functions.Module.SetClassifierTrainingStatus(trainingSession, new Enumeration(Constants.Module.ClassifierTrainingStatusNames.Awaiting));
        Sungero.Commons.PublicFunctions.ClassifierTrainingSession.SetSessionStatus(trainingSession, new Enumeration("Rollback"));
        return;
      }
      
      if (arioTaskStatus == Constants.Module.ProcessingTaskStates.ErrorOccurred)
      {
        Logger.ErrorFormat("ClassifierTraining. TrainClassifier. Ario service error, errorMessage={0}, sessionId={1}",
                           arioTaskInfo.Result, trainingSessionId);
        Sungero.SmartProcessing.Functions.Module.SetClassifierTrainingStatus(trainingSession, new Enumeration(Constants.Module.ClassifierTrainingStatusNames.Error));
        Sungero.Commons.PublicFunctions.ClassifierTrainingSession.SetSessionStatus(trainingSession, new Enumeration("Error"));
        return;
      }

      // Дообучение завершено - заполнить статусы обучения в сессии и результатах распознавания.
      if (arioTaskStatus == Constants.Module.ProcessingTaskStates.Completed)
      {
        args.Retry = !SmartProcessing.Functions.Module.FinalizeClassifierTraining(trainingSession, arioTask);
        return;
      }

      // Дообучение не завершено - повторный запуск АО через указанный в настройке интервал.
      args.Retry = true;
    }
    
    /// <summary>
    /// Асинхронный обработчик распознавания в Ario документов из пакета.
    /// </summary>
    /// <param name="args">BlobPackageId - ИД пакета бинарных образов документов.</param>
    public virtual void ProcessBlobPackage(Sungero.SmartProcessing.Server.AsyncHandlerInvokeArgs.ProcessBlobPackageInvokeArgs args)
    {
      // Проверка статусов задач в Ario.
      var arioError = false;
      var blobPackage = BlobPackages.Null;
      try
      {
        blobPackage = SmartProcessing.BlobPackages.GetAll().Where(r => r.Id == args.BlobPackageId).FirstOrDefault();
        var blobsSuccessfullyProcessed = this.CheckArioTasksStatus(blobPackage);
        if (!blobsSuccessfullyProcessed)
        {
          args.NextRetryTime = this.GetProcessBlobPackageNextRetryTime(args.RetryIteration);
          args.Retry = true;
          
          // Вызываем string.Format здесь, т.к. нельзя использовать params в public-функциях.
          var iterationLogMessage = string.Format("Smart processing. ProcessBlobPackage, iteration: {0}, next retry time: {1}",
                                                  args.RetryIteration,
                                                  args.NextRetryTime);
          Functions.Module.LogMessage(iterationLogMessage, blobPackage);
          return;
        }
      }
      catch (Exception ex)
      {
        Functions.Module.LogError("Smart processing. ProcessBlobPackage. Error while trying to check tasks statuses in Ario.", ex, blobPackage);
        arioError = true;
      }
      
      // Запуск обработки пакета документов.
      try
      {
        // Отправка уведомления, если были ошибки на Ario. Для почты отправить оригинальные тела задачей на верификацию.
        if ((!this.HasBlobsSuccessfullyProcessedByArio(blobPackage) || arioError) &&
            blobPackage.SourceType != SmartProcessing.BlobPackage.SourceType.Mail)
        {
          this.SendNotificationAboutArioErrors(blobPackage);
          Functions.Module.LogMessage("Smart processing. ProcessBlobPackage. Fatal error while processing captured package.", blobPackage);
          return;
        }
        Functions.Module.ProcessCapturedPackage(blobPackage);
      }
      catch (Exception ex)
      {
        Functions.Module.LogError("Smart processing. ProcessBlobPackage. Fatal error while processing captured package.", ex, blobPackage);
      }
    }
    
    /// <summary>
    /// Получить время следующей попытки интеллектуальной обработки документов.
    /// </summary>
    /// <param name="retryIteration">Номер итерации (начинается с нуля).</param>
    /// <returns>Время следующей попытки.</returns>
    public virtual DateTime GetProcessBlobPackageNextRetryTime(int retryIteration)
    {
      var retryTimeout = 60;
      switch (retryIteration)
      {
        case 0:
          retryTimeout = Constants.Module.ArioDocumentProcessingMinDuration;
          break;
        case 1:
          retryTimeout = Constants.Module.ArioAccessMinPeriod;
          break;
        case 2:
          retryTimeout = Constants.Module.ArioAccessMinPeriod;
          break;
        case 3:
          retryTimeout = Constants.Module.ArioAccessMinPeriod;
          break;
        case 4:
          retryTimeout = Constants.Module.ArioAccessMinPeriod;
          break;
      }
      
      return Calendar.Now.AddSeconds(retryTimeout).ToUtcTime().Value;
    }
    
    /// <summary>
    /// Проверить статус задач по обработке в Ario.
    /// </summary>
    /// <param name="blobPackage">Пакет бинарных образов документов.</param>
    /// <returns>Признак того, что все задачи по обработке успешно завершены.</returns>
    public virtual bool CheckArioTasksStatus(IBlobPackage blobPackage)
    {
      var unprocessedBlobs = blobPackage.Blobs.Select(x => x.Blob)
        .Where(b => b.ArioTaskStatus == SmartProcessing.Blob.ArioTaskStatus.InProcess && b.ArioTaskId != null);
      var blobPackageProcessed = true;
      
      foreach (var blob in unprocessedBlobs)
      {
        var processTaskInfo = Functions.Module.GetProcessTaskInfo(blob.ArioTaskId.Value);
        var processTask = processTaskInfo.Task;
        var arioTaskStatus = processTask.State;
        if (arioTaskStatus == Constants.Module.ProcessingTaskStates.Completed)
        {
          blob.ArioResultJson = processTaskInfo.GetArioResultJson();
          blob.ArioTaskStatus = SmartProcessing.Blob.ArioTaskStatus.Success;
          blob.Save();
        }
        
        if (arioTaskStatus == Constants.Module.ProcessingTaskStates.New ||
            arioTaskStatus == Constants.Module.ProcessingTaskStates.InWork)
        {
          blobPackageProcessed = false;
          continue;
        }
        
        if (arioTaskStatus == Constants.Module.ProcessingTaskStates.ErrorOccurred ||
            arioTaskStatus == Constants.Module.ProcessingTaskStates.TrainingCompleted ||
            arioTaskStatus == Constants.Module.ProcessingTaskStates.Terminated)
        {
          blob.ArioTaskStatus = SmartProcessing.Blob.ArioTaskStatus.ErrorOccurred;
          blob.Save();
        }
      }
      
      return blobPackageProcessed;
    }
    
    /// <summary>
    /// Проверить, есть ли успешно обработанные в Ario бинарные образы документов.
    /// </summary>
    /// <param name="blobPackage">Пакет бинарных образов документов.</param>
    /// <returns>Признак того, что хотя бы один бинарный образ документов успешно обработан в Ario.</returns>
    [Public]
    public virtual bool HasBlobsSuccessfullyProcessedByArio(IBlobPackage blobPackage)
    {
      return blobPackage.Blobs.Select(x => x.Blob).Where(b => b.ArioTaskStatus == SmartProcessing.Blob.ArioTaskStatus.Success).Count() > 0;
    }
    
    /// <summary>
    /// Отправить уведомление ответственному за верификацию об ошибках в Ario.
    /// </summary>
    /// <param name="blobPackage">Пакет бинарных образов документов.</param>
    [Public]
    public virtual void SendNotificationAboutArioErrors(IBlobPackage blobPackage)
    {
      var utcOffset = Calendar.UtcOffset.TotalHours;
      var utcOffsetLabel = utcOffset >= 0 ? "+" + utcOffset.ToString() : utcOffset.ToString();
      var timeWithUtc = string.Format("{0:g} (UTC{1})", blobPackage.DcsProcessingBeginDate.GetValueOrDefault(), utcOffsetLabel);
      var title = Sungero.SmartProcessing.Resources.ArioErrorsNotificationTitleFormat(timeWithUtc);
      var text = Sungero.SmartProcessing.Resources.ArioErrorsNotificationTextFormat(blobPackage.SourceName,
                                                                                    blobPackage.PackageId,
                                                                                    timeWithUtc,
                                                                                    Resources.ArioErrorsNotificationPrefix);
      
      var responsible = Functions.Module.GetResponsible(blobPackage);
      if (responsible == null)
        return;
      
      var task = Workflow.SimpleTasks.CreateWithNotices(title, responsible);
      task.ActiveText = text;
      task.Save();
      task.Start();
    }

    /// <summary>
    /// Асинхронный обработчик удаления результатов распознавания сущности.
    /// </summary>
    /// <param name="args">Параметр - ИД сущности, результаты распознавания которой нужно удалить.</param>
    public virtual void DeleteEntityRecognitionInfo(Sungero.SmartProcessing.Server.AsyncHandlerInvokeArgs.DeleteEntityRecognitionInfoInvokeArgs args)
    {
      var electronicDocumentMetadata = typeof(IElectronicDocument).GetEntityMetadata();
      var documentRecognitionInfos = Commons.EntityRecognitionInfos.GetAll().Where(r => r.EntityId == args.EntityId);
      
      foreach (var recognitionInfo in documentRecognitionInfos)
      {
        if (electronicDocumentMetadata.IsAncestorFor(Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(Guid.Parse(recognitionInfo.EntityType))))
          Commons.EntityRecognitionInfos.Delete(recognitionInfo);
      }
    }
    
  }
}