using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.OfficialDocument;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server.RecordManagementBlocks
{
  partial class DocumentMultipleAddresseeReviewBlockHandlers
  {

    public virtual void DocumentMultipleAddresseeReviewBlockEnd(System.Collections.Generic.IEnumerable<Sungero.RecordManagement.IDocumentReviewTask> createdTasks)
    {
      var reviewTask = createdTasks.FirstOrDefault();
      if (reviewTask is null)
        return;
      
      if (reviewTask.Status == Workflow.Task.Status.Completed)
      {
        var document = reviewTask.DocumentForReviewGroup.OfficialDocuments.SingleOrDefault();
        if (Memos.Is(document))
          Sungero.Docflow.PublicFunctions.Memo.SetInternalApprovalState(Memos.As(document), Docflow.Memo.InternalApprovalState.Reviewed);
      }
    }

    public virtual void DocumentMultipleAddresseeReviewBlockStartTask(Sungero.RecordManagement.IDocumentReviewTask task)
    {
      if (_block.MaxDeadline.HasValue)
        task.Deadline = _block.MaxDeadline;
      
      Sungero.RecordManagement.Functions.DocumentReviewTask.SetAddressees(task, _block.Addressees.ToList());
      
      var document = task.DocumentForReviewGroup.OfficialDocuments.SingleOrDefault();
      if (Memos.Is(document))
        Sungero.Docflow.PublicFunctions.Memo.SetInternalApprovalState(Memos.As(document), Docflow.Memo.InternalApprovalState.PendingReview);
    }
  }

  partial class DocumentReviewBlockHandlers
  {

    public virtual void DocumentReviewBlockStart()
    {
      Logger.DebugFormat("DocumentReviewTask({0}) DocumentReviewBlockStart", _obj.Id);
      var reviewTask = DocumentReviewTasks.As(_obj);
      if (reviewTask != null)
        Functions.DocumentReviewTask.RelateAddedAddendaToPrimaryDocument(reviewTask);
      
      // Отправить запрос на подготовку предпросмотра для документов.
      Docflow.PublicFunctions.Module.PrepareAllAttachmentsPreviews(_obj);
    }
    
    public virtual void DocumentReviewBlockStartAssignment(Sungero.RecordManagement.IDocumentReviewAssignment assignment)
    {
      assignment.AssistantPrepareResolution = _block.AssistantPrepareResolution;

      var reviewTask = DocumentReviewTasks.As(_obj);
      if (reviewTask != null)
      {
        Logger.DebugFormat("DocumentReviewTask({0}) DocumentReviewBlockStartAssignment", reviewTask.Id);
        
        // Вложить проект резолюции от виртуального ассистента.
        if (assignment.AssistantPrepareResolution == false && Company.Employees.Is(_obj.Author))
        {
          var authorEmployee = Company.Employees.As(_obj.Author);
          assignment.AssistantPrepareResolution = Company.PublicFunctions.Employee.CanPrepareDraftResolutionForManager(authorEmployee, reviewTask.Addressee);
        }
        
        // Обновить статус исполнения - на рассмотрении.
        var document = reviewTask.DocumentForReviewGroup.OfficialDocuments.First();
        Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.OnReview);
        Functions.Module.SetDocumentControlExecutionState(document);
        
        // Выдать исполнителю права на вложения.
        if (_block.GrantRightsByDefault == true)
          Functions.DocumentReviewTask.GrantRightsForMainDocumentAndAddendaToAssignees(reviewTask, _block.Performers.ToList(), true, assignment.AddendaGroup.OfficialDocuments.ToList());
        
        // Синхронизация вложений добавленных событием заполнения.
        Functions.DocumentReviewTask.SynchronizeDocumentReviewAttachmentsToDraftResolutions(reviewTask);
      }
    }

    public virtual void DocumentReviewBlockCompleteAssignment(Sungero.RecordManagement.IDocumentReviewAssignment assignment)
    {
      var reviewTask = DocumentReviewTasks.As(_obj);
      if (reviewTask == null)
      {
        if (assignment.Result == Sungero.RecordManagement.DocumentReviewAssignment.Result.Forward)
          assignment.Forward(assignment.Addressee, ForwardingLocation.Next);
        return;
      }
      
      Logger.DebugFormat("DocumentReviewTask({0}) DocumentReviewBlockCompleteAssignment", reviewTask.Id);
      var document = reviewTask.DocumentForReviewGroup.OfficialDocuments.First();
      
      // Заполнить текст резолюции из задания руководителя в задачу.
      if (assignment.Result == Sungero.RecordManagement.DocumentReviewAssignment.Result.ResPassed)
        reviewTask.ResolutionText = assignment.ActiveText;

      // Обновить статус исполнения - на исполнении.
      if (assignment.Result == Sungero.RecordManagement.DocumentReviewAssignment.Result.DraftResApprove)
      {
        Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.OnExecution);
        Functions.Module.SetDocumentControlExecutionState(document);
      }
      
      // Обновить статус исполнения - не требует исполнения.
      if (assignment.Result == Sungero.RecordManagement.DocumentReviewAssignment.Result.Informed)
      {
        Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.WithoutExecut);
        Functions.Module.SetDocumentControlExecutionState(document);
      }
      
      Functions.DocumentReviewTask.RelateAddedAddendaToPrimaryDocument(reviewTask);
      Functions.DocumentReviewTask.SynchronizeDocumentReviewAttachmentsToDraftResolutions(reviewTask);
    }

    public virtual void DocumentReviewBlockEnd(System.Collections.Generic.IEnumerable<Sungero.RecordManagement.IDocumentReviewAssignment> createdAssignments)
    {
      Docflow.PublicFunctions.Module.ExecuteWaitAssignmentMonitoring(createdAssignments.Select(a => a.Id).ToList());
      Logger.DebugFormat("DocumentReviewTask({0}) DocumentReviewBlockEnd", _obj.Id);
    }

  }

}