using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing;

namespace Sungero.RecordManagement.Server
{
  public class ModuleAsyncHandlers
  {

    /// <summary>
    /// Асинхронный обработчик для подготовки данных для обучения виртуального ассистента.
    /// </summary>
    /// <param name="args">Аргументы асинхронного обработчика.</param>
    public virtual void PrepareAIAssistantsTraining(Sungero.RecordManagement.Server.AsyncHandlerInvokeArgs.PrepareAIAssistantsTrainingInvokeArgs args)
    {
      args.Retry = false;
      // Обработчик выбирает данные порционно, стартуя новый экземпляр АО, сохраняя ИД первого из отобранных поручений. 
      var isFirstIteration = args.ActionItemMinId == 0;
      var logTemplate = string.Format("PrepareAIAssistantsTraining. {{0}}. AssistantId={0}, PeriodBegin={1}, PeriodEnd={2}, ActionItemMinId={3}",
                                      args.AssistantId, args.PeriodBegin, args.PeriodEnd, args.ActionItemMinId);
      
      var assistant = Intelligence.AIManagersAssistants.GetAll(x => x.Id == args.AssistantId && x.Status == Intelligence.AIManagersAssistant.Status.Active).FirstOrDefault();
      if (assistant == null)
      {
        Logger.DebugFormat(logTemplate, "Active AI assistant not found");
        return;
      }
      
      // Не перезапускать АО, если дату начала обучения сбросили во время его выполнения.
      var assigneeClassifier = assistant.Classifiers
        .Where(x => x.ClassifierType == Sungero.Intelligence.AIManagersAssistantClassifiers.ClassifierType.Assignee)
        .FirstOrDefault(x => x.TrainingStartDate.HasValue);
      if (assigneeClassifier == null)
      {
        Logger.DebugFormat(logTemplate, "Assignee classifier not found or training start date not filled");
        return;
      }
      
      var numberActionItemsToSelect = Constants.Module.MaxActionItemsInQueueIteration;
      // В случае, если дата начала обработки пустая, то взять минимально необходимое для дообучения число поручений.
      if (args.PeriodBegin == Calendar.SqlMinValue)
      {
        var minTrainingSetSize = RecordManagement.PublicFunctions.Module.Remote.GetMinTrainingSetSizeForPublishingClassifierModelValue();
        if (minTrainingSetSize >= args.ProcessedItemsCount && 
            minTrainingSetSize - args.ProcessedItemsCount < numberActionItemsToSelect)
          numberActionItemsToSelect = minTrainingSetSize - args.ProcessedItemsCount;
      }
             
      var actionItems = Functions.Module.GetActionItemsForAssigneeClassifierTraining(args.PeriodBegin, args.PeriodEnd, assistant.Manager, numberActionItemsToSelect, args.ActionItemMinId);
      if (!actionItems.Any())
      {
        try
        {
          assigneeClassifier.TrainingStartDate = null;
          assistant.Save();
        }
        catch (Domain.Shared.Exceptions.RepeatedLockException)
        {
          Logger.DebugFormat(logTemplate, "AI assistant is locked. Async handler will be restarted");
          args.Retry = true;
          return;
        }
        
        if (isFirstIteration)
          Logger.DebugFormat(logTemplate, "No suitable action items for training");
        else
          Logger.DebugFormat(logTemplate, string.Format("Training data preparation complete, {0} total action items selected", args.ProcessedItemsCount));

        return;
      }
      
      // Если была получена порция поручений, создать очереди обучения и запустить новый экземпляр АО для обработки следующей порции.
      Functions.Module.EnqueueActionItemsForAIAssistantTraining(actionItems, Intelligence.AIManagersAssistantClassifiers.ClassifierType.Assignee);
      var logMessage = string.Format("Training data preparation results: {0} action items selected", actionItems.Count);
      Logger.DebugFormat(logTemplate, logMessage);
      args.ProcessedItemsCount += actionItems.Count;      
      var actionItemMinId = actionItems.Min(x => x.Id);
      Functions.Module.CreatePrepareAIAssistantsTrainingAsyncHandler(args.AssistantId, args.PeriodBegin, args.PeriodEnd, args.ProcessedItemsCount, actionItemMinId, false);
    }

    /// <summary>
    /// Асинхронный обработчик для проверки статуса дообучения в Ario.
    /// </summary>
    /// <param name="args">ArioTaskId - Ид задачи Ario.</param>
    public virtual void WaitAndFinalizeAIAssistantTraining(Sungero.RecordManagement.Server.AsyncHandlerInvokeArgs.WaitAndFinalizeAIAssistantTrainingInvokeArgs args)
    {
      args.Retry = false;
      var trainQueueItems = ActionItemTrainQueueItems.GetAll(x => x.ArioTaskId == args.ArioTaskId).ToList();
      if (!trainQueueItems.Any())
      {
        Logger.ErrorFormat("ClassifierTraining. Train queue items not found, arioTaskId={0}", args.ArioTaskId);
        return;
      }
      
      var arioSettings = Docflow.PublicFunctions.SmartProcessingSetting.GetSettings();
      if (!Docflow.PublicFunctions.SmartProcessingSetting.Remote.CheckConnection(arioSettings))
      {
        Logger.ErrorFormat("ClassifierTraining. Ario services are not available, arioTaskId={0}", args.ArioTaskId);
        Functions.Module.SetActionItemTrainQueueStatuses(trainQueueItems, RecordManagement.ActionItemTrainQueueItem.ProcessingStatus.Awaiting);
        return;
      }
      
      var virtualAssistantId = trainQueueItems.First()?.AIManagersAssistantId;
      var classifierId = trainQueueItems.First()?.ClassifierId;
      Logger.DebugFormat("ClassifierTraining. Start async handler, iteration={0}, arioTaskId={1}, AIManagerAssistantId={2}, classifierId={3}",
                         args.RetryIteration, args.ArioTaskId, virtualAssistantId, classifierId);

      var trainTask = SmartProcessing.PublicFunctions.Module.GetArioTrainingTask(args.ArioTaskId);
      if (trainTask.State == SmartProcessing.PublicConstants.Module.ProcessingTaskStates.InWork && args.RetryIteration > Constants.Module.ClassifierTrainingRetryLimit)
      {
        Logger.DebugFormat("ClassifierTraining. Async handler retry limit exceeded, arioTaskId={0}", args.ArioTaskId);
        Functions.Module.SetActionItemTrainQueueStatuses(trainQueueItems, RecordManagement.ActionItemTrainQueueItem.ProcessingStatus.ErrorOccured);
        return;
      }

      args.Retry = !Functions.Module.FinalizeTraining(trainTask, trainQueueItems);
    }

    /// <summary>
    /// Старт поручений по протоколу совещания.
    /// </summary>
    /// <param name="args">Аргументы асинхронного обработчика.</param>
    public virtual void StartActionItemExecutionTasks(Sungero.RecordManagement.Server.AsyncHandlerInvokeArgs.StartActionItemExecutionTasksInvokeArgs args)
    {
      Logger.DebugFormat("Execute async handler StartActionItemExecutionTasks. RetryIteration: {0}, TaskIds: {1}", args.RetryIteration, args.TaskIds);
      
      var taskIds = args.TaskIds.Split(',')
        .Select(long.Parse)
        .ToList();
      
      var tasks = ActionItemExecutionTasks.GetAll()
        .Where(t => taskIds.Contains(t.Id) && t.Status == RecordManagement.ActionItemExecutionTask.Status.Draft)
        .ToList();
      
      if (!tasks.Any())
      {
        Logger.Debug("Task list is empty, async handler will be stopped.");
        args.Retry = false;
        return;
      }
      
      foreach (var task in tasks)
        Functions.ActionItemExecutionTask.RelateAddedAddendaToPrimaryDocument(task);
      
      var primaryDocuments = tasks.SelectMany(t => t.DocumentsGroup.OfficialDocuments)
        .Cast<Sungero.Docflow.IOfficialDocument>()
        .Distinct()
        .ToList();
      
      var addenda = tasks.SelectMany(t => t.AddendaGroup.All)
        .Cast<Sungero.Domain.Shared.IEntity>()
        .Distinct()
        .ToList();
      
      var documents = new List<Sungero.Domain.Shared.IEntity>();
      documents.AddRange(primaryDocuments);
      documents.AddRange(addenda);
      documents = documents.Distinct().ToList();
      
      foreach (var primaryDocument in primaryDocuments)
      {
        var task = tasks.First(t => t.DocumentsGroup.All.Contains(primaryDocument));
        Sungero.Docflow.PublicFunctions.OfficialDocument.SetDocumentAssignee(primaryDocument, task.Assignee);
        Functions.Module.SetDocumentStatesWhenStartingActionItems(task, primaryDocument);
      }
      
      Functions.Module.GrantAccessRightsToDocumentsWhenStartingActionItems(tasks, documents);
      
      foreach (var document in documents)
        document.Save();
      
      var errorOccurred = false;
      foreach (var task in tasks)
      {
        // Есть небольшая вероятность что когда воркер начнет выполнять асинхронный обработчик, поручение к этому времени будет просрочено.
        if (RecordManagement.PublicFunctions.ActionItemExecutionTask.CheckOverdueActionItemExecutionTask(task))
        {
          Logger.ErrorFormat("Action item execution task is overdue. Task id: {0}", task.Id);
          continue;
        }
        
        try
        {
          // Заменить значение свойства Стартовал на значение параметра StartedByUserId перед стартом задачи.
          // Сделано для корректной работы отчёта "Контроль исполнения поручений по совещаниям" (270972).
          var taskParams = ((Sungero.Domain.Shared.IExtendedEntity)task).Params;
          taskParams.Add(RecordManagement.Constants.Module.StartedByUserId, args.StartedByUserId);
          task.Start();
        }
        catch (Sungero.Domain.Shared.Exceptions.RepeatedLockException)
        {
          Logger.DebugFormat("StartActionItemExecutionTasks: task is locked. Task id: {0}", task.Id);
          args.Retry = true;
          return;
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("Start action item execution task failed. Task id: {0}", ex, task.Id);
          errorOccurred = true;
        }
        
        if (errorOccurred)
        {
          args.Retry = false;
          var exceptionMessage = string.Format("StartActionItemExecutionTasks: {0}", Sungero.RecordManagement.Resources.StartActionItemExecutionTasksErrorTitle);
          throw AppliedCodeException.Create(exceptionMessage);
        }
      }
      
      Logger.DebugFormat("Done async handler StartActionItemExecutionTasks. RetryIteration: {0}", args.RetryIteration);
    }
    
    /// <summary>
    /// Выполнить действия по корректировке поручений, которые связаны с ожиданием разблокировки заданий текущего поручения.
    /// </summary>
    /// <param name="args">Аргументы асинхронного обработчика.</param>
    public virtual void ApplyActionItemLockDependentChanges(Sungero.RecordManagement.Server.AsyncHandlerInvokeArgs.ApplyActionItemLockDependentChangesInvokeArgs args)
    {
      Logger.DebugFormat("ApplyActionItemLockDependentChanges: start async for action item execution task with id {0}, retry iteration {1}.", args.ActionItemTaskId, args.RetryIteration);
      var actionItemTask = RecordManagement.ActionItemExecutionTasks.Get(args.ActionItemTaskId);
      
      var taskInProcess = actionItemTask.Status == Workflow.Task.Status.InProcess;
      if (!taskInProcess || actionItemTask.OnEditGuid != args.OnEditGuid)
      {
        if (!taskInProcess)
        {
          actionItemTask.OnEditGuid = string.Empty;
          actionItemTask.Save();
        }
        
        args.Retry = false;
        Logger.DebugFormat("ApplyActionItemLockDependentChanges. Task with id {0} not in process or already changing.", args.ActionItemTaskId);
        return;
      }
      
      var changes = Functions.ActionItemExecutionTask.DeserializeActionItemChanges(actionItemTask, args.OldSupervisor, args.NewSupervisor, args.OldAssignee, args.NewAssignee,
                                                                                   args.OldDeadline, args.NewDeadline, args.OldCoAssignees, args.NewCoAssignees,
                                                                                   args.CoAssigneesOldDeadline, args.CoAssigneesNewDeadline, args.EditingReason, args.AdditionalInfo,
                                                                                   string.Empty, string.Empty, args.InitiatorOfChange, args.ChangeContext);
      Functions.ActionItemExecutionTask.ApplyActionItemLockDependentChanges(actionItemTask, changes);
      Logger.DebugFormat("ApplyActionItemLockDependentChanges: done async for action item execution task with id {0}, retry iteration {1}.", args.ActionItemTaskId, args.RetryIteration);
    }
    
    /// <summary>
    /// Выполнить действия по корректировке поручений, которые не связаны с ожиданием разблокировки заданий текущего поручения.
    /// </summary>
    /// <param name="args">Аргументы асинхронного обработчика.</param>
    public virtual void ApplyActionItemLockIndependentChanges(Sungero.RecordManagement.Server.AsyncHandlerInvokeArgs.ApplyActionItemLockIndependentChangesInvokeArgs args)
    {
      Logger.DebugFormat("ApplyActionItemLockIndependentChanges: start async for action item execution task with id {0}, retry iteration {1}.", args.ActionItemTaskId, args.RetryIteration);
      var actionItemTask = RecordManagement.ActionItemExecutionTasks.Get(args.ActionItemTaskId);
      
      if (actionItemTask.Status != Workflow.Task.Status.InProcess)
      {
        args.Retry = false;
        Logger.DebugFormat("ApplyActionItemLockIndependentChanges. Task with id {0} not in process.", args.ActionItemTaskId);
        return;
      }
      
      if (actionItemTask.OnEditGuid != args.OnEditGuid)
      {
        args.Retry = false;
        Logger.DebugFormat("ApplyActionItemLockIndependentChanges. Task with id {0} already changing.", args.ActionItemTaskId);
        return;
      }
      
      if (!Functions.ActionItemExecutionTask.AssignmentsCreated(actionItemTask))
      {
        args.Retry = true;
        Logger.DebugFormat("ApplyActionItemLockIndependentChanges: assignments not created for task with id {0}, retry iteration {1}.", args.ActionItemTaskId, args.RetryIteration);
        
        if (args.OldAssignee != args.NewAssignee)
          Functions.ActionItemExecutionTask.AbortActionItemExecutionAssignment(actionItemTask, args.OldAssignee);
        
        if (args.OldSupervisor != args.NewSupervisor)
          Functions.ActionItemExecutionTask.AbortActionItemSupervisorAssignments(actionItemTask, args.OldSupervisor);
        
        return;
      }
      
      while (!Functions.ActionItemExecutionTask.AreAssignmentsCreated(actionItemTask))
        Functions.ActionItemExecutionTask.CreateActionItemExecutionTask(actionItemTask);
      
      var changes = Functions.ActionItemExecutionTask.DeserializeActionItemChanges(actionItemTask, args.OldSupervisor, args.NewSupervisor, args.OldAssignee, args.NewAssignee,
                                                                                   args.OldDeadline, args.NewDeadline, args.OldCoAssignees, args.NewCoAssignees,
                                                                                   args.CoAssigneesOldDeadline, args.CoAssigneesNewDeadline, args.EditingReason, args.AdditionalInfo,
                                                                                   string.Empty, string.Empty, args.InitiatorOfChange, args.ChangeContext);
      
      Functions.ActionItemExecutionTask.ApplyActionItemLockIndependentChanges(actionItemTask, changes);
      Functions.Module.ExecuteApplyActionItemLockDependentChanges(changes, actionItemTask.Id, actionItemTask.OnEditGuid);
      Logger.DebugFormat("ApplyActionItemLockIndependentChanges: done async for action item execution task with id {0}, retry iteration {1}.", args.ActionItemTaskId, args.RetryIteration);
    }

    public virtual void ChangeCompoundActionItem(Sungero.RecordManagement.Server.AsyncHandlerInvokeArgs.ChangeCompoundActionItemInvokeArgs args)
    {
      Logger.DebugFormat("ChangeCompoundActionItem: start async for action item execution task with id {0}, retry iteration {1}.", args.ActionItemTaskId, args.RetryIteration);
      
      var actionItemTask = RecordManagement.ActionItemExecutionTasks.Get(args.ActionItemTaskId);
      if (actionItemTask.Status != Workflow.Task.Status.InProcess || actionItemTask.OnEditGuid != args.OnEditGuid)
      {
        args.Retry = false;
        Logger.DebugFormat("ChangeCompoundActionItem. Task with id {0} not in process or already changing.", args.ActionItemTaskId);
        return;
      }
      
      var changes = Functions.ActionItemExecutionTask.DeserializeActionItemChanges(actionItemTask, args.OldSupervisor, args.NewSupervisor, args.OldAssignee, args.NewAssignee,
                                                                                   args.OldDeadline, args.NewDeadline, args.OldCoAssignees, args.NewCoAssignees,
                                                                                   args.CoAssigneesOldDeadline, args.CoAssigneesNewDeadline, args.EditingReason, args.AdditionalInfo,
                                                                                   args.TaskIds, args.ActionItemPartsText, args.InitiatorOfChange, args.ChangeContext);
      
      // Инициализировать адресатов.
      var addressees = new List<IUser>();
      
      try
      {
        // Получить список заинтересованных в изменении поручения для отправки уведомления.
        // Находится здесь, чтобы учитывать состояние поручения до изменений.
        addressees = Functions.ActionItemExecutionTask.GetCompoundActionItemChangeNotificationAddressees(actionItemTask, changes);
        var oldSupervisors = new List<Company.IEmployee>();
        
        // Закешировать список пунктов для сокращения числа обращений к SQL.
        var actionItemPartTasks = ActionItemExecutionTasks.GetAll()
          .Where(t => t.Status == Sungero.Workflow.Task.Status.InProcess)
          .Where(t => changes.TaskIds.Contains(t.Id))
          .ToList();
        
        // Протащить изменения в выбранные пункты поручения, которые еще в работе/на приемке.
        foreach (var actionItemPartTask in actionItemPartTasks)
        {
          if (actionItemPartTask == null)
            continue;
          
          var oldSupervisorAssignment = Functions.ActionItemExecutionTask.GetActualActionItemSupervisorAssignment(actionItemPartTask);
          
          // Сохранить предыдущих контролера и срок перед их обновлением в карточке
          // для корректной отработки прекращения запросов отчета.
          var oldSupervisor = actionItemPartTask.Supervisor;
          var oldDeadline = actionItemPartTask.Deadline;
          
          // Если для данного пункта значения новых контролера и срока совпадают со старыми, то корректировку делать не нужно.
          var deadlineChanged = changes.NewDeadline != null && !Equals(oldDeadline, changes.NewDeadline);
          var supervisorChanged = changes.NewSupervisor != null && !Equals(oldSupervisor, changes.NewSupervisor);
          if (!deadlineChanged && !supervisorChanged)
            continue;
          
          // Создать структуру для текущего пункта копированием из общей структуры, с одной лишь разницей в данных -
          // значения старого срока и старого контролера заполнить данными из пункта, а не оставлять пустыми, как в changes.
          var partChanges = Functions.Module.CopyActionItemChangesStructure(changes);
          partChanges.OldDeadline = oldDeadline;
          partChanges.OldSupervisor = oldSupervisor;
          
          if (oldSupervisor != null && supervisorChanged)
            oldSupervisors.Add(oldSupervisor);
          
          Functions.ActionItemExecutionTask.UpdateActionItemPartTask(actionItemTask, actionItemPartTask, partChanges);
          actionItemPartTask.OnEditGuid = Guid.NewGuid().ToString();
          actionItemPartTask.Save();
          
          try
          {
            // Переадресовать измененное задание контролеру.
            Functions.ActionItemExecutionTask.ForwardChangedAssignments(actionItemTask, partChanges, null, oldSupervisorAssignment);
            
            // Обработать смену срока в задании на исполнение для текущего пункта поручения.
            var executionAssignment = Functions.ActionItemExecutionTask.GetActualActionItemExecutionAssignment(actionItemPartTask);
            if (partChanges.NewDeadline != null && deadlineChanged && executionAssignment != null)
            {
              // Прокинуть срок в задание исполнителя, если задача не заблокирована.
              Functions.ActionItemExecutionTask.ChangeExecutionAssignmentDeadline(actionItemTask, partChanges.NewDeadline, executionAssignment);
              
              // Прекратить запросы на продление срока от исполнителя, у которого сменился срок.
              Functions.ActionItemExecutionTask.AbortDeadlineExtensionTasks(actionItemTask, actionItemPartTask);
            }
          }
          catch (Exception ex)
          {
            Logger.ErrorFormat("ChangeCompoundActionItem. Error while processing task with id {0}:{1}.", args.ActionItemTaskId, ex.Message);
            throw AppliedCodeException.Create(ActionItemExecutionTasks.Resources.ActionItemChangeError);
          }
          
          Functions.Module.ExecuteApplyActionItemLockIndependentChanges(partChanges, actionItemPartTask.Id, actionItemPartTask.OnEditGuid);
          
          // Прекратить неактуальные запросы отчета от предыдущего контролера из пункта поручения.
          if (oldSupervisor != null)
            Functions.ActionItemExecutionTask.AbortReportRequestTasksCreatedFromTaskByAuthor(actionItemTask, actionItemPartTask, oldSupervisor);
        }
        
        // Прекратить неактуальные запросы отчетов от предыдущих контролеров из основной задачи.
        Functions.ActionItemExecutionTask.AbortReportRequestTasksFromOldCompoundActionItemSupervisors(actionItemTask, oldSupervisors);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("ChangeCompoundActionItem. Error while processing task with id {0}:{1}.", args.ActionItemTaskId, ex.Message);
        throw AppliedCodeException.Create(ActionItemExecutionTasks.Resources.ActionItemChangeError);
      }
      
      // Разослать уведомления об изменении поручения.
      Functions.ActionItemExecutionTask.SendActionItemChangeNotifications(actionItemTask, changes, addressees);
      Functions.Module.ExecuteApplyActionItemLockIndependentChanges(changes, actionItemTask.Id, actionItemTask.OnEditGuid);
      
      Logger.DebugFormat("ChangeCompoundActionItem: done async for action item execution task with id {0}, retry iteration {1}.", args.ActionItemTaskId, args.RetryIteration);
    }

    public virtual void CompleteParentActionItemExecutionAssignment(Sungero.RecordManagement.Server.AsyncHandlerInvokeArgs.CompleteParentActionItemExecutionAssignmentInvokeArgs args)
    {
      var formattedArgs = string.Format("{0}, {1}, {2}, {3}", args.actionItemId, args.parentAssignmentId, args.parentTaskStartId, args.needAbortChildActionItems);
      Logger.DebugFormat("CompleteParentActionItemExecutionAssignment({1}): Start for Action item execution task (ID = {0}).", args.actionItemId, formattedArgs);
      
      var task = ActionItemExecutionTasks.GetAll(t => t.Id == args.actionItemId).FirstOrDefault();
      if (task == null)
      {
        Logger.DebugFormat("CompleteParentActionItemExecutionAssignment({1}): asynchronous handler was terminated. Task {0} not found", args.actionItemId, formattedArgs);
        return;
      }
      
      var parentAssignment = ActionItemExecutionAssignments.GetAll(t => t.Id == args.parentAssignmentId).FirstOrDefault();
      if (parentAssignment == null)
      {
        Logger.DebugFormat("CompleteParentActionItemExecutionAssignment({1}): asynchronous handler was terminated. Parent assignment {0} not found", args.parentAssignmentId, formattedArgs);
        return;
      }
      
      if (parentAssignment.Status != Workflow.Assignment.Status.InProcess)
      {
        Logger.DebugFormat("CompleteParentActionItemExecutionAssignment({1}): asynchronous handler was terminated. Parent assignment {0} not in process", args.parentAssignmentId, formattedArgs);
        return;
      }
      
      if (parentAssignment.TaskStartId != args.parentTaskStartId)
      {
        Logger.DebugFormat("CompleteParentActionItemExecutionAssignment({2}): asynchronous handler was terminated. StartId was changed. Value in args {0}, value in task {1} ",
                           args.parentTaskStartId, parentAssignment.TaskStartId, formattedArgs);
        return;
      }
      
      // Если задача в работе или на приёмке, не выполнять родительское задание до её завершения.
      if (task.Status == RecordManagement.ActionItemExecutionTask.Status.InProcess ||
          task.Status == RecordManagement.ActionItemExecutionTask.Status.UnderReview)
      {
        args.Retry = true;
        Logger.DebugFormat("CompleteParentActionItemExecutionAssignment({3}): Action item (ID = {0}) has status {1}. Parent assignment (ID = {2}) not completed.",
                           task.Id, task.Status, parentAssignment.Id, formattedArgs);
        return;
      }
      
      try
      {
        // Установить признак необходимости прекращения подчиненных поручений.
        if (parentAssignment.NeedAbortChildActionItems != args.needAbortChildActionItems)
          Functions.ActionItemExecutionTask.SetNeedAbortChildActionItemsInParentAssignment(task, args.needAbortChildActionItems);
        // Добавить документы из группы "Результаты исполнения" в ведущее задание на исполнение.
        Functions.ActionItemExecutionTask.SynchronizeResultGroup(task);
        // Выполнить ведущее задание на исполнение.
        Functions.ActionItemExecutionTask.CompleteParentAssignment(task);
        // Заполнить в ведущем задании на исполнение свойство "Выполнил" исполнителем задания.
        Functions.ActionItemExecutionTask.SetCompletedByInParentAssignment(task);
      }
      catch (Sungero.Domain.Shared.Exceptions.RepeatedLockException)
      {
        Logger.DebugFormat("CompleteParentActionItemExecutionAssignment({1}): parent assignment (ID = {0}) is locked.", parentAssignment.Id, formattedArgs);
        args.Retry = true;
        return;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("CompleteParentActionItemExecutionAssignment({0}): unhandled exception", ex, formattedArgs);
        return;
      }
    }
    
    public virtual void ExcludeFromAcquaintance(Sungero.RecordManagement.Server.AsyncHandlerInvokeArgs.ExcludeFromAcquaintanceInvokeArgs args)
    {
      var assignments = Functions.Module.GetActiveAcquaintanceAssignments(args.AssignmentIds);
      foreach (var assignment in assignments)
      {
        // Не обрабатывать завершённые и прекращённые задания.
        if (assignment.Status == Sungero.Workflow.Assignment.Status.Completed &&
            assignment.Status == Sungero.Workflow.Assignment.Status.Aborted)
          continue;
        
        // Если задание заблокировано, то нужно повторное выполнение обработчика.
        var locksInfo = Locks.GetLockInfo(assignment);
        if (locksInfo.IsLockedByOther)
        {
          args.Retry = true;
          continue;
        }
        
        Logger.DebugFormat("ExcludeFromAcquaintance: acquaintance assignment with id {0} has been excluded async.", assignment.Id);
        assignment.Abort();
      }
    }
  }
}