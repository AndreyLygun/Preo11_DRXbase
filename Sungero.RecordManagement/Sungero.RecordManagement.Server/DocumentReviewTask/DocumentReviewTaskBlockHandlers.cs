using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OfficialDocument;
using Sungero.RecordManagement.DocumentReviewTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server.DocumentReviewTaskBlocks
{
  partial class WaitForPrepareDraftResolutionHandlers
  {

    public virtual bool WaitForPrepareDraftResolutionResult()
    {
      return Sungero.RecordManagement.Functions.Module.CheckArioTasksStatus(_obj.Id, RecordManagement.ActionItemPredictionInfo.TaskType.DocumentReview);
    }

    public virtual void WaitForPrepareDraftResolutionStart()
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      Sungero.RecordManagement.PublicFunctions.Module.SendDocumentForAssigneeClassification(_obj.Id, RecordManagement.ActionItemPredictionInfo.TaskType.DocumentReview, document, _obj.Addressee);
      
      _block.Period = TimeSpan.FromSeconds(Constants.DocumentReviewTask.WaitArioProcessingBlockDefaultParams.PeriodInSeconds);
      if (_block.RelativeDeadline.Equals(TimeSpan.Zero))
        _block.RelativeDeadline = TimeSpan.FromMinutes(Constants.DocumentReviewTask.WaitArioProcessingBlockDefaultParams.RelativeDeadlineInMinutes);
    }

    public virtual void WaitForPrepareDraftResolutionEnd()
    {
      var draftActionItem = Sungero.RecordManagement.PublicFunctions.Module.PrepareDraftActionItem(_obj, RecordManagement.ActionItemPredictionInfo.TaskType.DocumentReview,
                                                                                                   _block.DraftResolutionDeadline, _block.DraftResolutionActiveText);
      if (draftActionItem != null)
        Functions.DocumentReviewTask.AddActionItemToResolutionGroup(_obj, draftActionItem);
    }
  }

  #region Назначение прав на документы

  partial class GrantAccessRightsToDocumentsBlockHandlers
  {
    
    public virtual void GrantAccessRightsToDocumentsBlockExecute()
    {
      Logger.DebugFormat("DocumentReviewTask({0}) GrantAccessRightsToDocumentsBlockExecute", _obj.Id);
      var addressees = _obj.Addressees.Select(a => a.Addressee).ToList();
      var assistants = new List<IRecipient>() { };
      foreach (var addressee in addressees)
      {
        assistants.AddRange(Sungero.Company.PublicFunctions.Employee.GetAssistantWhoPreparesDraftResolution(addressee));
      }
      
      // Выдать адресатам и помощникам права на вложения.
      var addresseesAndAssistants = assistants.Concat(addressees.Select(a => Recipients.As(a))).ToList();
      Functions.DocumentReviewTask.GrantRightsForMainDocumentAndAddendaToAssignees(_obj, addresseesAndAssistants, false, new List<Sungero.Docflow.IOfficialDocument>());
    }
  }

  #endregion

  #region Заполнение актуального адресата задачи

  partial class SetNewAddresseeBlockHandlers
  {

    public virtual void SetNewAddresseeBlockExecute()
    {
      // Заполнить нового адресата в задаче.
      Logger.DebugFormat("DocumentReviewTask({0}) SetNewAddresseeBlockExecute", _obj.Id);
      Functions.DocumentReviewTask.UpdateReviewTaskAfterForward(_obj, _block.Addressee);
      
      // Удаление неактуальных проектов резолюции.
      Functions.Module.DeleteActionItemExecutionTasks(_obj.ResolutionGroup.ActionItemExecutionTasks
                                                      .Where(x => !Equals(_block.ActualFromAddressees, x.AssignedBy))
                                                      .ToList());
    }
  }

  #endregion

  #region Мониторинг завершения всех подзадач

  partial class WaitForAddresseesReviewBlockHandlers
  {
    [Obsolete("Метод не используется с 25.08.2023 и версии 4.8. Ожидание завершения настроено в блоке SendReviewTasksToAddresseesBlock.")]
    public virtual void WaitForAddresseesReviewBlockStart()
    {
      Logger.DebugFormat("DocumentReviewTask({0}) WaitForAddresseesReviewBlockStart", _obj.Id);
      _block.Period = TimeSpan.FromHours(Constants.DocumentReviewTask.CheckCompletionMonitoringPeriodInHours);
    }

    [Obsolete("Метод не используется с 25.08.2023 и версии 4.8. Ожидание завершения настроено в блоке SendReviewTasksToAddresseesBlock.")]
    public virtual bool WaitForAddresseesReviewBlockResult()
    {
      var result = Functions.DocumentReviewTask.AllDocumentReviewSubTasksAreCompleted(_obj);
      Logger.DebugFormat("DocumentReviewTask({0}) WaitForAddresseesReviewBlockResult result ({1})", _obj.Id, result);
      return result;
    }
  }

  #endregion

  #region Удаление проектов резолюции

  partial class DeleteObsoleteDraftResolutionsBlockHandlers
  {

    public virtual void DeleteObsoleteDraftResolutionsBlockExecute()
    {
      Logger.DebugFormat("DocumentReviewTask({0}) DeleteObsoleteDraftResolutionsBlockExecute", _obj.Id);
      
      Functions.Module.DeleteActionItemExecutionTasks(_obj.ResolutionGroup.ActionItemExecutionTasks
                                                      .Where(x => !_block.ActualFromAddressees.Contains(x.AssignedBy))
                                                      .Where(x => x.Status == RecordManagement.ActionItemExecutionTask.Status.Draft)
                                                      .ToList());
    }
  }

  #endregion

  #region Подзадачи на рассмотрение

  partial class SendReviewTasksToAddresseesBlockHandlers
  {

    public virtual void SendReviewTasksToAddresseesBlockStart()
    {
      Functions.DocumentReviewTask.RelateAddedAddendaToPrimaryDocument(_obj);
    }
    
    public virtual void SendReviewTasksToAddresseesBlockStartTask(Sungero.RecordManagement.IDocumentReviewTask task, Sungero.RecordManagement.IDocumentReviewTaskAddressees item)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) SendReviewTasksToAddresseesBlockStartTask", _obj.Id);
      
      task.Importance = _obj.Importance;
      Functions.Module.SynchronizeAttachmentsToDocumentReview(_obj, task);
      
      // Задать текст задачи.
      task.Subject = string.Format(">> {0}", _obj.Subject);
      task.ActiveText = _obj.ActiveText;
      
      // Задать адресата.
      // Очистить Адресатов, которые могли заполниться из документа.
      task.Addressees.Clear();
      var newAddressee = task.Addressees.AddNew();
      newAddressee.Addressee = item.Addressee;
      
      // Синхронизировать вложенные проекты резолюции.
      var canAuthorPrepareResolution = Functions.DocumentReviewTask.CanAuthorPrepareResolution(task);
      foreach (var resolution in _obj.ResolutionGroup.ActionItemExecutionTasks)
      {
        if (canAuthorPrepareResolution && task.Addressees.Any(x => Equals(resolution.AssignedBy, x.Addressee)))
          task.ResolutionGroup.ActionItemExecutionTasks.Add(resolution);
      }
      
      // Задать срок.
      task.Deadline = _obj.Deadline;
      task.MaxDeadline = _obj.MaxDeadline;
      
      Logger.DebugFormat("DocumentReviewTask({0}) SendReviewTasksToAddresseesBlockStartTask Subtask {1} created", _obj.Id, task.Id);
      
      item.TaskCreated = true;
    }
  }

  partial class SendReviewToAddresseeBlockHandlers
  {
    [Obsolete("Метод не используется с 25.08.2023 и версии 4.8. Используйте блок SendReviewTasksToAddresseesBlock.")]
    public virtual void SendReviewToAddresseeBlockExecute()
    {
      Logger.DebugFormat("DocumentReviewTask({0}) SendReviewToAddresseeBlockExecute", _obj.Id);
      
      var documentReviewTask = DocumentReviewTasks.CreateAsSubtask(_obj);
      Logger.DebugFormat("DocumentReviewTask({0}) SendReviewToAddresseeBlockExecute Create as subtask {1}", _obj.Id, documentReviewTask.Id);
      
      documentReviewTask.Importance = _obj.Importance;
      Functions.Module.SynchronizeAttachmentsToDocumentReview(_obj, documentReviewTask);
      
      // Задать тему, текст задачи.
      documentReviewTask.Subject = string.Format(">> {0}", _obj.Subject);
      documentReviewTask.ThreadSubject = _obj.ThreadSubject;
      documentReviewTask.ActiveText = _obj.ActiveText;
      
      // Задать адресата и инициатора.
      // Очистить Адресатов, которые могли заполниться из документа.
      documentReviewTask.Addressees.Clear();
      var addressee = _obj.Addressees.FirstOrDefault(t => t.TaskCreated != true);
      var newAddressee = documentReviewTask.Addressees.AddNew();
      newAddressee.Addressee = addressee.Addressee;
      documentReviewTask.Author = _obj.Author;
      
      // Синхронизировать вложенные проекты резолюции.
      var canAuthorPrepareResolution = Functions.DocumentReviewTask.CanAuthorPrepareResolution(documentReviewTask);
      foreach (var resolution in _obj.ResolutionGroup.ActionItemExecutionTasks)
      {
        if (canAuthorPrepareResolution && documentReviewTask.Addressees.Any(x => Equals(resolution.AssignedBy, x.Addressee)))
          documentReviewTask.ResolutionGroup.ActionItemExecutionTasks.Add(resolution);
      }
      
      // Задать срок.
      documentReviewTask.Deadline = _obj.Deadline;
      documentReviewTask.MaxDeadline = _obj.MaxDeadline;
      
      documentReviewTask.Start();
      Logger.DebugFormat("DocumentReviewTask({0}) SendReviewToAddresseeBlockExecute Subtask {1} started", _obj.Id, documentReviewTask.Id);
      
      addressee.TaskCreated = true;
    }
  }

  #endregion

  #region Задание на доработку рассмотрения

  partial class ReviewReworkBlockHandlers
  {

    public virtual void ReviewReworkBlockStart()
    {
      Logger.DebugFormat("DocumentReviewTask({0}) ReviewReworkBlockStart", _obj.Id);
      
      Functions.DocumentReviewTask.RelateAddedAddendaToPrimaryDocument(_obj);
    }
    
    public virtual void ReviewReworkBlockStartAssignment(Sungero.RecordManagement.IReviewReworkAssignment assignment)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) ReviewReworkBlockStartAssignment", _obj.Id);

      var lastAssignmentSentForRework = Functions.DocumentReviewTask.GetLastAssignmentSentForRework(_obj);
      if (lastAssignmentSentForRework != null)
        assignment.Author = lastAssignmentSentForRework.Performer;
      
      // Выдать исполнителю права на вложения.
      if (_block.GrantRightsByDefault == true)
      {
        var performers = _block.Performers.Where(x => Company.Employees.Is(x)).ToList();
        Functions.DocumentReviewTask.GrantRightsForMainDocumentAndAddendaToAssignees(_obj, performers, true, assignment.AddendaGroup.OfficialDocuments.ToList());
        
        // Выдавать права на проекты резолюции только сотрудникам, которые могут готовить проект резолюции для руководителя. Bug 328465.
        performers = performers.Where(x => Company.PublicFunctions.Employee.Remote.CanPrepareDraftResolution(Company.Employees.As(x))).ToList();
        if (performers.Any())
          Functions.DocumentReviewTask.GrantRightsForDraftResolutionToAssignees(_obj, performers, _obj.ResolutionGroup.ActionItemExecutionTasks.ToList());
      }
      
      // Синхронизация вложений добавленных событием заполнения.
      Functions.DocumentReviewTask.SynchronizeDocumentReviewAttachmentsToDraftResolutions(_obj);
    }
    
    public virtual void ReviewReworkBlockCompleteAssignment(Sungero.RecordManagement.IReviewReworkAssignment assignment)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) ReviewReworkBlockCompleteAssignment", _obj.Id);
      
      // Заполнить нового адресата в задаче.
      if (assignment.Result == RecordManagement.ReviewReworkAssignment.Result.Forward)
        Functions.DocumentReviewTask.UpdateReviewTaskAfterForward(_obj, assignment.Addressee);
      
      Functions.DocumentReviewTask.RelateAddedAddendaToPrimaryDocument(_obj);
      Functions.DocumentReviewTask.SynchronizeDocumentReviewAttachmentsToDraftResolutions(_obj);
    }
    
    public virtual void ReviewReworkBlockEnd(System.Collections.Generic.IEnumerable<Sungero.RecordManagement.IReviewReworkAssignment> createdAssignments)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) ReviewReworkBlockEnd", _obj.Id);
    }
  }

  #endregion

  #region Создание поручения делопроизводителем

  partial class ProcessResolutionBlockHandlers
  {

    public virtual void ProcessResolutionBlockStart()
    {
      Logger.DebugFormat("DocumentReviewTask({0}) ProcessResolutionBlockStart", _obj.Id);
      
      Functions.DocumentReviewTask.RelateAddedAddendaToPrimaryDocument(_obj);
    }
    
    public virtual void ProcessResolutionBlockStartAssignment(Sungero.RecordManagement.IReviewResolutionAssignment assignment)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) ProcessResolutionBlockStartAssignment", _obj.Id);
      assignment.ResolutionText = _obj.ResolutionText;
      
      // Установить "От" как исполнителя рассмотрения.
      assignment.Author = _obj.Addressee;
      
      // Обновить статус исполнения - отправка на исполнение.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.Sending);
      Functions.Module.SetDocumentControlExecutionState(document);

      // Исключаем автора, так как требуется не повышать ему права (147926).
      if (_block.GrantRightsByDefault == true)
      {
        var performers = _block.Performers.Where(x => !Equals(x, _obj.Author)).ToList();
        Functions.DocumentReviewTask.GrantRightsForMainDocumentAndAddendaToAssignees(_obj, performers, true, assignment.AddendaGroup.OfficialDocuments.ToList());
      }
    }
    
    public virtual void ProcessResolutionBlockCompleteAssignment(Sungero.RecordManagement.IReviewResolutionAssignment assignment)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) ProcessResolutionBlockCompleteAssignment", _obj.Id);
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      
      // Если поручения не созданы, то изменить статус исполнения - не требует исполнения.
      if (!ActionItemExecutionTasks.GetAll(t => t.Status == Workflow.Task.Status.InProcess && Equals(t.ParentAssignment, assignment)).Any())
      {
        Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.WithoutExecut);
        Functions.Module.SetDocumentControlExecutionState(document);
      }
      else
      {
        Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.OnExecution);
        Functions.Module.SetDocumentControlExecutionState(document);
      }
      
      Functions.DocumentReviewTask.RelateAddedAddendaToPrimaryDocument(_obj);
    }
    
    public virtual void ProcessResolutionBlockEnd(System.Collections.Generic.IEnumerable<Sungero.RecordManagement.IReviewResolutionAssignment> createdAssignments)
    {
      Docflow.PublicFunctions.Module.ExecuteWaitAssignmentMonitoring(createdAssignments.Select(a => a.Id).ToList());
      Logger.DebugFormat("DocumentReviewTask({0}) ProcessResolutionBlockEnd", _obj.Id);
    }
  }

  #endregion

  #region Создание и доработка проектов резолюций

  partial class PrepareDraftResolutionBlockHandlers
  {
    
    public virtual void PrepareDraftResolutionBlockStart()
    {
      Logger.DebugFormat("DocumentReviewTask({0}) PrepareDraftResolutionBlockStart", _obj.Id);
      
      Functions.DocumentReviewTask.RelateAddedAddendaToPrimaryDocument(_obj);
      
      // Выдать права помощнику руководителя, чтобы он мог удалять приложения в задании на подготовку/доработку проекта резолюции.
      Logger.DebugFormat("DocumentReviewTask({0}). GrantRightsOnTaskForSecretary", _obj.Id);
      Functions.DocumentReviewTask.GrantRightsOnTaskForSecretary(_obj);
    }
    
    public virtual void PrepareDraftResolutionBlockStartAssignment(Sungero.RecordManagement.IPreparingDraftResolutionAssignment assignment)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) PrepareDraftResolutionBlockStartAssignment", _obj.Id);
      
      var lastReview = Assignments
        .GetAll(a => Equals(a.Task, _obj) && Equals(a.TaskStartId, _obj.StartId))
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();

      if (lastReview != null && DocumentReviewAssignments.Is(lastReview) &&
          lastReview.Result == RecordManagement.DocumentReviewAssignment.Result.DraftResRework)
        assignment.IsRework = true;
      
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.OnReview);
      Functions.Module.SetDocumentControlExecutionState(document);
      
      var result = Functions.DocumentReviewTask.GetLastAssignmentResult(_obj);
      if (result == RecordManagement.DocumentReviewAssignment.Result.DraftResRework)
        assignment.Author = _obj.Addressee;
      
      // Выдать исполнителю права на вложения.
      if (_block.GrantRightsByDefault == true)
      {
        Functions.DocumentReviewTask.GrantRightsForMainDocumentAndAddendaToAssignees(_obj, _block.Performers.ToList(), true, assignment.AddendaGroup.OfficialDocuments.ToList());
        Functions.DocumentReviewTask.GrantRightsForDraftResolutionToAssignees(_obj, _block.Performers.ToList(), _obj.ResolutionGroup.ActionItemExecutionTasks.ToList());
      }
      
      // Синхронизация вложений добавленных событием заполнения.
      Functions.DocumentReviewTask.SynchronizeDocumentReviewAttachmentsToDraftResolutions(_obj);
    }
    
    public virtual void PrepareDraftResolutionBlockCompleteAssignment(Sungero.RecordManagement.IPreparingDraftResolutionAssignment assignment)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) PrepareDraftResolutionBlockCompleteAssignment", _obj.Id);
      
      // Обновить статус исполнения - не требует исполнения.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      if (assignment.Result == Sungero.RecordManagement.PreparingDraftResolutionAssignment.Result.Explored)
      {
        Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.WithoutExecut);
        Functions.Module.SetDocumentControlExecutionState(document);
      }
      
      Functions.DocumentReviewTask.RelateAddedAddendaToPrimaryDocument(_obj);
      Functions.DocumentReviewTask.SynchronizeDocumentReviewAttachmentsToDraftResolutions(_obj);
    }
    
    public virtual void PrepareDraftResolutionBlockEnd(System.Collections.Generic.IEnumerable<Sungero.RecordManagement.IPreparingDraftResolutionAssignment> createdAssignments)
    {
      Docflow.PublicFunctions.Module.ExecuteWaitAssignmentMonitoring(createdAssignments.Select(a => a.Id).ToList());
      Logger.DebugFormat("DocumentReviewTask({0}) PrepareDraftResolutionBlockEnd", _obj.Id);
    }
  }

  #endregion

}