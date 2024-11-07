using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.Server;
using Sungero.RecordManagement.ActionItemExecutionTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement
{
  partial class ActionItemExecutionTaskAuthorPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> AuthorFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      return Docflow.PublicFunctions.Module.UsersCanBeResolutionAuthorFilter(query, _obj.DocumentsGroup.OfficialDocuments.SingleOrDefault()).Cast<T>();
    }
  }

  partial class ActionItemExecutionTaskActionItemObserversObserverPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ActionItemObserversObserverFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return (IQueryable<T>)PublicFunctions.Module.ObserversFiltering(query);
    }
  }

  partial class ActionItemExecutionTaskCreatingFromServerHandler
  {
    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.ExecutionState);
      e.Without(_info.Properties.Report);
      e.Without(_info.Properties.ActualDate);
      e.Without(_info.Properties.ReportNote);
      e.Without(_info.Properties.AbortingReason);
      e.Without(_info.Properties.ActionItemType);
      e.Without(_info.Properties.OnEdit);
      e.Without(_info.Properties.OnEditGuid);
      e.Without(_info.Properties.Started);
      var hasIndefiniteDeadline = _source.HasIndefiniteDeadline == true && Functions.Module.AllowActionItemsWithIndefiniteDeadline();
      e.Map(_info.Properties.HasIndefiniteDeadline, hasIndefiniteDeadline);
      
      if (hasIndefiniteDeadline)
        e.Without(_info.Properties.CoAssigneesDeadline);
      
      if (_source.Assignee != null && _source.Assignee.Status == CoreEntities.DatabookEntry.Status.Closed)
        e.Without(_info.Properties.Assignee);
      
      if (_source.Supervisor != null && _source.Supervisor.Status == CoreEntities.DatabookEntry.Status.Closed)
        e.Without(_info.Properties.Supervisor);
    }
  }

  partial class ActionItemExecutionTaskAssignedByPropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> AssignedByFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      return Docflow.PublicFunctions.Module.UsersCanBeResolutionAuthorFilter(query, _obj.DocumentsGroup.OfficialDocuments.SingleOrDefault()).Cast<T>();
    }
  }
  
  partial class ActionItemExecutionTaskFilteringServerHandler<T>
  {

    public override IQueryable<T> PreFiltering(IQueryable<T> query, Sungero.Domain.PreFilteringEventArgs e)
    {
      if (_filter == null)
        return query;
      
      if (Functions.Module.UsePrefilterActionItemExecutionTasks(_filter))
      {
        query = Functions.Module.ActionItemExecutionTasksApplyStrongFilter(query, _filter).Cast<T>();
        query = Functions.Module.ActionItemExecutionTasksApplyOrdinaryFilter(query, _filter).Cast<T>();
      }
      
      return query;
    }
    
    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      // Вернуть нефильтрованный список, если нет фильтра. Он будет использоваться во всех Get() и GetAll().
      if (_filter == null)
        return query;
      
      if (!Functions.Module.UsePrefilterActionItemExecutionTasks(_filter))
        query = Functions.Module.ActionItemExecutionTasksApplyOrdinaryFilter(query, _filter).Cast<T>();
      
      query = Functions.Module.ActionItemExecutionTasksApplyWeakFilter(query, _filter).Cast<T>();
      
      return query;
    }
  }

  partial class ActionItemExecutionTaskServerHandlers
  {

    public override void BeforeSaveHistory(Sungero.Domain.HistoryEventArgs e)
    {
      // Проверить, что было изменение сроков, которое необходимо отразить в истории.
      var taskParams = ((Domain.Shared.IExtendedEntity)_obj).Params;
      if (!taskParams.ContainsKey(PublicConstants.ActionItemExecutionTask.ChangeDeadlinesWriteInHistoryParamName))
        return;
      
      // Сформировать комментарий для записи в историю поручения.
      var changeDeadlineComment = Functions.ActionItemExecutionTask.GetActionItemChangeDeadlineHistoryText(_obj, taskParams);
      
      // Если изменились только сроки, сформировать одну строку записи в историю (подменить строку записи по умолчанию),
      // иначе информацию об изменении сроков записать отдельной строкой.
      var changeDeadlineOperationText = Constants.ActionItemExecutionTask.Operation.ChangeDeadline;
      if (taskParams.ContainsKey(PublicConstants.ActionItemExecutionTask.ChangeOnlyDeadlinesWriteInHistoryParamName))
      {
        e.Operation = new Enumeration(changeDeadlineOperationText);
        e.Comment = changeDeadlineComment;
      }
      else
      {
        e.Write(new Enumeration(changeDeadlineOperationText), null, changeDeadlineComment);
      }
      
      // Очистить параметры, сигнализирующие об изменении сроков,
      // чтобы не было ложных срабатываний при последующих сохранениях записей в историю.
      taskParams.Remove(PublicConstants.ActionItemExecutionTask.ChangeDeadlinesWriteInHistoryParamName);
      taskParams.Remove(PublicConstants.ActionItemExecutionTask.ChangeOnlyDeadlinesWriteInHistoryParamName);
    }

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      var taskIsCompleted = Functions.ActionItemExecutionTask.IsActionItemExecutionTaskCompleted(_obj);
      if (taskIsCompleted && _obj.ActionItemType == ActionItemType.Component)
        Functions.ActionItemExecutionTask.ExecuteParentActionItemExecutionTaskMonitorings(_obj);
      
      // Удалить стартованное подчиненное поручение из вложений родительского задания.
      if (_obj.Status != ActionItemExecutionTask.Status.Draft && _obj.ParentAssignment != null)
      {
        var parentAssignment = ActionItemExecutionAssignments.As(_obj.ParentAssignment);
        if (parentAssignment != null && parentAssignment.ActionItemDraftGroup.All.Contains(_obj))
        {
          parentAssignment.ActionItemDraftGroup.All.Remove(_obj);
          parentAssignment.Save();
        }
      }
    }
    
    public override void BeforeAbort(Sungero.Workflow.Server.BeforeAbortEventArgs e)
    {
      _obj.ExecutionState = ExecutionState.Aborted;
      
      // Если прекращён черновик, прикладную логику по прекращению выполнять не надо.
      if (_obj.State.Properties.Status.OriginalValue == Workflow.Task.Status.Draft)
        return;
      
      // Обновить статус исполнения документа - исполнен, статус контроля исполнения - снято с контроля.
      if (_obj.DocumentsGroup.OfficialDocuments.Any())
      {
        var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
        Functions.Module.SetDocumentExecutionState(_obj, document, null);
        Functions.Module.SetDocumentControlExecutionState(document);
      }
      
      // Прекратить задачи на запрос отчета, созданные из текущей задачи.
      Functions.ActionItemExecutionTask.AbortReportRequestTasksCreatedFromTask(_obj);
      
      // Прекратить задачи на запрос отчета, созданные из родительского задания.
      Functions.ActionItemExecutionTask.AbortReportRequestTasksCreatedFromAssignmentToAssignee(_obj,
                                                                                               ActionItemExecutionAssignments.As(_obj.ParentAssignment),
                                                                                               _obj.Assignee);
      
      // Прекратить задачи на запрос отчета, созданные из составного поручения исполнителю пункта.
      if (ActionItemExecutionTasks.As(_obj.ParentTask)?.IsCompoundActionItem == true)
      {
        Functions.ActionItemExecutionTask.AbortReportRequestTasksCreatedFromTaskToAssignee(_obj,
                                                                                           ActionItemExecutionTasks.As(_obj.ParentTask),
                                                                                           _obj.Assignee);
      }
      
      // При программном вызове не выполнять рекурсивную остановку подзадач.
      if (!e.Params.Contains(RecordManagement.Constants.ActionItemExecutionTask.WorkingWithGUI))
        return;
      
      // Рекурсивно прекратить подзадачи.
      Functions.Module.AbortSubtasksAndSendNotices(_obj);
    }

    public override void BeforeRestart(Sungero.Workflow.Server.BeforeRestartEventArgs e)
    {
      Logger.Debug("ActionItemExecutionTask (ID={0}). Start BeforeRestart.", _obj.Id);
      // Очистить причину прекращения и статус.
      _obj.AbortingReason = string.Empty;
      _obj.ExecutionState = null;
      _obj.OnEditGuid = string.Empty;
      
      // Очистить свойство созданных заданий у свойств-коллекций.
      if (_obj.CoAssignees != null && _obj.CoAssignees.Count > 0)
      {
        foreach (var assignee in _obj.CoAssignees)
        {
          assignee.AssignmentCreated = false;
          Logger.Debug("ActionItemExecutionTask (ID={0}). Set AssignmentCreated flag for assignee {1} to false.", _obj.Id, assignee.Assignee.Id);
        }
      }
      if (_obj.ActionItemParts != null && _obj.ActionItemParts.Count > 0)
      {
        foreach (var part in _obj.ActionItemParts)
        {
          part.AssignmentCreated = false;
          Logger.Debug("ActionItemExecutionTask (ID={0}). Set AssignmentCreated flag for action item part {1} to false.", _obj.Id, part.Id);
        }
      }
    }

    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Start BeforeStart.", _obj.Id);
      
      // Если задача была стартована через UI, то проверяем корректность срока.
      bool startedFromUI;
      if (e.Params.TryGetValue(PublicConstants.ActionItemExecutionTask.CheckDeadline, out startedFromUI) && startedFromUI)
        e.Params.Remove(PublicConstants.ActionItemExecutionTask.CheckDeadline);
      
      if (!Functions.ActionItemExecutionTask.ValidateActionItemExecutionTaskStart(_obj, e, startedFromUI))
        return;

      // Задать текст в переписке.
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Set ActiveText.", _obj.Id);
      if (_obj.IsCompoundActionItem == true)
      {
        _obj.ActiveText = string.IsNullOrWhiteSpace(_obj.ActiveText) ? Sungero.RecordManagement.ActionItemExecutionTasks.Resources.DefaultActionItem : _obj.ActiveText;
        _obj.ThreadSubject = Sungero.RecordManagement.ActionItemExecutionTasks.Resources.CompoundActionItemThreadSubject;
      }
      else if (_obj.ActionItemType != ActionItemType.Component)
        _obj.ThreadSubject = Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ActionItemThreadSubject;

      if (_obj.ActionItemType == ActionItemType.Component)
      {
        // Синхронизировать текст пункта составного поручения в прикладное поле.
        Functions.ActionItemExecutionTask.SynchronizeActiveText(_obj);
        
        // При рестарте поручения обновляется текст, срок и исполнитель в табличной части составного поручения.
        Functions.ActionItemExecutionTask.SynchronizeActionItemPart(_obj, false);
      }
      
      if (_obj.ActionItemType == ActionItemType.Additional)
        _obj.ActiveText = ActionItemExecutionTasks.Resources.SentToCoAssignee;
      
      // Выдать права на изменение для возможности прекращения задачи.
      if (!PublicFunctions.Module.Remote.IsTaskTypeUsingProcessKind(_obj))
      {
        Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Grant access right to task.", _obj.Id);
        Functions.ActionItemExecutionTask.GrantAccessRightToTask(_obj, _obj);
      }
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Synchronize primary document if task is draft resolution.", _obj.Id);
      if (_obj.IsDraftResolution == true && !_obj.DocumentsGroup.OfficialDocuments.Any())
      {
        if (ReviewDraftResolutionAssignments.Is(_obj.ParentAssignment))
          _obj.DocumentsGroup.OfficialDocuments.Add(ReviewDraftResolutionAssignments.As(_obj.ParentAssignment).DocumentForReviewGroup.OfficialDocuments.FirstOrDefault());
        else if (DocumentReviewAssignments.Is(_obj.ParentAssignment))
          _obj.DocumentsGroup.OfficialDocuments.Add(DocumentReviewAssignments.As(_obj.ParentAssignment).DocumentForReviewGroup.OfficialDocuments.FirstOrDefault());
        else
          _obj.DocumentsGroup.OfficialDocuments.Add(PreparingDraftResolutionAssignments.As(_obj.ParentAssignment).DocumentForReviewGroup.OfficialDocuments.FirstOrDefault());
      }
      
      // Заменить значение свойства Стартовал на значение параметра.
      // Также подменить стартовавшего в переписке.
      // Сделано для корректной работы отчёта "Контроль исполнения поручений по совещаниям" (270972).
      if (e.Params.Contains(RecordManagement.Constants.Module.StartedByUserId))
      {
        long? startedByUserId = null;
        e.Params.TryGetValue(RecordManagement.Constants.Module.StartedByUserId, out startedByUserId);
        if (startedByUserId.HasValue)
        {
          var startedBy = Users.GetAll(x => x.Id == startedByUserId.Value).FirstOrDefault();
          if (startedBy != null)
          {
            _obj.StartedBy = startedBy;
            Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Change StartedBy to parameter value (ID={1}).", _obj.Id, startedByUserId);
            
            // Подменить стартовавшего в единственном на текущий момент сообщении переписки (о старте задачи).
            var startMessage = _obj.Texts.FirstOrDefault();
            if (startMessage != null)
              startMessage.WrittenBy = startedBy;
          }
        }
        
        e.Params.Remove(RecordManagement.Constants.Module.StartedByUserId);
      }
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). End BeforeStart", _obj.Id);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Start Created.", _obj.Id);
      
      _obj.ActionItemType = ActionItemType.Main;
      
      _obj.OnEdit = false;
      _obj.OnEditGuid = string.Empty;
      
      if (!_obj.State.IsCopied)
      {
        Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Not Copied.", _obj.Id);
        
        _obj.NeedsReview = false;
        _obj.IsUnderControl = false;
        _obj.IsCompoundActionItem = false;
        _obj.HasIndefiniteDeadline = false;
        
        // Заполнение из персональных настроек происходит в методе создания подчиненного поручения.
        _obj.IsAutoExec = false;
        _obj.Subject = Docflow.Resources.AutoformatTaskSubject;
        var employee = Employees.As(_obj.Author);
        if (employee != null)
        {
          Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Author is employee (ID={1}).", _obj.Id, employee.Id);
          var settings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(employee);
          if (settings != null)
          {
            Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Has PersonalSettings (ID={1}).", _obj.Id, settings.Id);
            _obj.IsUnderControl = settings.FollowUpActionItem;
            Logger.DebugFormat("ActionItemExecutionTask (ID={0}). GetResolutionAuthor.", _obj.Id);
            var resolutionAuthor = Docflow.PublicFunctions.PersonalSetting.GetResolutionAuthor(settings);
            Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Set AssignedBy.", _obj.Id);
            _obj.AssignedBy = Docflow.PublicFunctions.Module.Remote.IsUsersCanBeResolutionAuthor(_obj.DocumentsGroup.OfficialDocuments.SingleOrDefault(), resolutionAuthor)
              ? resolutionAuthor
              : null;
          }
        }
      }
      else
      {
        Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Copied.", _obj.Id);
        
        if (_obj.Author != null && _obj.AssignedBy != null && !_obj.Author.Equals(_obj.AssignedBy))
          _obj.Author = Users.As(_obj.AssignedBy);
        
        // Сброс отметок о создании заданий соисполнителям.
        if (_obj.CoAssignees.Count > 0)
          foreach (var assignee in _obj.CoAssignees)
          {
            assignee.AssignmentCreated = false;
            Logger.Debug("ActionItemExecutionTask (ID={0}). Set AssignmentCreated flag for assignee {1} to false.", _obj.Id, assignee.Assignee.Id);
          }
        
        // Сброс отметок о создании заданий по частям составного поручения.
        if (_obj.IsCompoundActionItem == true)
          foreach (var part in _obj.ActionItemParts)
          {
            part.AssignmentCreated = false;
            Logger.Debug("ActionItemExecutionTask (ID={0}). Set AssignmentCreated flag for action item part {1} to false.", _obj.Id, part.Id);
            part.ActionItemPartExecutionTask = null;
            Logger.Debug("ActionItemExecutionTask (ID={0}). Clear ActionItemExecutionTask for action item part {1}.", _obj.Id, part.Id);
          }
        
        // Сброс индивидуального контролера в пунктах составного поручения.
        if (!_obj.IsUnderControl.Value)
          foreach (var part in _obj.ActionItemParts)
            part.Supervisor = null;
        
        // Сброс сроков исполнителя и соисполнителей в пунктах поручения.
        if (_obj.HasIndefiniteDeadline.Value)
          foreach (var part in _obj.ActionItemParts)
          {
            part.Deadline = null;
            part.CoAssigneesDeadline = null;
          }
        
        // Сброс результатов исполнения.
        if (_obj.ResultGroup.All.Any())
          _obj.ResultGroup.All.Clear();
      }
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Set Subject.", _obj.Id);
      var subjectTemplate = _obj.IsCompoundActionItem == true ?
        ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
        ActionItemExecutionTasks.Resources.TaskSubject;
      _obj.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, subjectTemplate);
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). End Created.", _obj.Id);
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Start BeforeSave.", _obj.Id);
      
      if (!Functions.ActionItemExecutionTask.ValidateActionItemExecutionTaskSave(_obj, e))
        return;

      var isCompoundActionItem = _obj.IsCompoundActionItem == true;
      if (isCompoundActionItem)
      {
        if (string.IsNullOrWhiteSpace(_obj.ActiveText) && !_obj.ActionItemParts.Any(i => string.IsNullOrEmpty(i.ActionItemPart)))
          _obj.ActiveText = ActionItemExecutionTasks.Resources.DefaultActionItem;
      }
      
      // Синхронизировать текст поручения в прикладное поле.
      if (_obj.ActionItemType != ActionItemType.Additional)
        Functions.ActionItemExecutionTask.SynchronizeActiveText(_obj);

      // Выдать права на документы для всех, кому выданы права на задачу.
      if (_obj.State.IsChanged)
      {
        // Выдать права по каждой группе в отдельности, так как AllAttachments включает в себя удаленные до сохранения документы. Bug 181206.
        Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Start GrantManualReadRightForAttachments.", _obj.Id);
        var allAttachments = _obj.DocumentsGroup.All.ToList();
        allAttachments.AddRange(_obj.AddendaGroup.All);
        allAttachments.AddRange(_obj.OtherGroup.All);
        allAttachments.AddRange(_obj.ResultGroup.All);
        
        Docflow.PublicFunctions.Module.GrantManualReadRightForAttachments(_obj, allAttachments);
        Logger.DebugFormat("ActionItemExecutionTask (ID={0}). End GrantManualReadRightForAttachments.", _obj.Id);
      }
      
      if (_obj.State.Properties.IsCompoundActionItem.IsChanged)
      {
        if (isCompoundActionItem)
        {
          // Очистить ненужные свойства в составном поручении.
          _obj.Assignee = null;
          _obj.CoAssignees.Clear();
          _obj.Deadline = null;
          
          // Заменить первый символ на прописной.
          foreach (var job in _obj.ActionItemParts)
            job.ActionItemPart = Docflow.PublicFunctions.Module.ReplaceFirstSymbolToUpperCase(job.ActionItemPart);
        }
        else
        {
          // Очистить ненужные свойства в несоставном поручении.
          _obj.ActionItemParts.Clear();
        }
      }
      
      // Заполнить тему.
      var isDraftActionItem = _obj.Status == ActionItemExecutionTask.Status.Draft && _obj.IsDraftResolution != true;
      var defaultSubject = ActionItemExecutionTasks.Resources.TaskSubject;
      if (isCompoundActionItem)
        defaultSubject = ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject;
      else if (isDraftActionItem)
      {
        var authorName = Company.PublicFunctions.Employee.GetShortName(_obj.AssignedBy, false);
        defaultSubject = ActionItemExecutionTasks.Resources.DraftActionItemSubjectFormat(authorName);
      }
      
      var subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, defaultSubject);
      if (subject == Docflow.Resources.AutoformatTaskSubject)
        subject = defaultSubject;
      
      // Не перезаписывать тему, если не изменилась.
      if (subject != _obj.Subject)
        _obj.Subject = subject;
      
      // Задать текст в переписке.
      var threadSubject = string.Empty;
      if (isCompoundActionItem)
        threadSubject = ActionItemExecutionTasks.Resources.CompoundActionItemThreadSubject;
      else if (_obj.ActionItemType != ActionItemType.Component)
        threadSubject = isDraftActionItem ?
          ActionItemExecutionTasks.Resources.DraftActionItemThreadSubject :
          ActionItemExecutionTasks.Resources.ActionItemThreadSubject;
      
      // Не перезаписывать текст в переписке без необходимости, чтобы избежать блокировок.
      if (!string.IsNullOrEmpty(threadSubject) && threadSubject != _obj.ThreadSubject)
        _obj.ThreadSubject = threadSubject;
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). End BeforeSave.", _obj.Id);
    }
  }
}