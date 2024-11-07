using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewReworkAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class ReviewReworkAssignmentActions
  {
    public override void ReturnUncompleted(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var allActionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.ToList();
      var actionItemsToDelete = Functions.Module.Remote.GetActionItemsAddedToAssignment(_obj, allActionItems, Company.Employees.Current,
                                                                                        PublicConstants.PreparingDraftResolutionAssignment.ResolutionGroupName);
      var hasActionItemsToDelete = actionItemsToDelete.Any();
      
      var description = hasActionItemsToDelete ? Resources.ConfirmDeleteDraftResolutionAssignment : null;
      var dropDialogId = hasActionItemsToDelete
        ? Constants.DocumentReviewTask.ReviewReworkAssignmentConfirmDialogID.ReturnUncompleted
        : Constants.DocumentReviewTask.ReviewReworkAssignmentConfirmDialogID.ReturnUncompletedWithDeletingDraftResolutions;
      var dropIsConfirmed = Docflow.PublicFunctions.Module
        .ShowConfirmationDialog(e.Action.ConfirmationMessage, description, null, dropDialogId);
      
      if (!dropIsConfirmed)
      {
        e.CloseFormAfterAction = false;
        return;
      }
      
      base.ReturnUncompleted(e);
    }

    public override bool CanReturnUncompleted(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanReturnUncompleted(e);
    }

    public virtual void Informed(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      
    }

    public virtual bool CanInformed(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void PrintResolution(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.Save();
      var actionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.ToList();
      Functions.DocumentReviewTask.OpenDraftResolutionReport(DocumentReviewTasks.As(_obj.Task), _obj.ActiveText, actionItems);
    }

    public virtual bool CanPrintResolution(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.Assignment.Status.InProcess &&
        _obj.ResolutionGroup.ActionItemExecutionTasks.Any() &&
        Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

    public virtual void AddResolution(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.Save();
      
      var actionItem = Functions.DocumentReviewTask.CreateDraftResolution(DocumentReviewTasks.As(_obj.Task));
      // Синхронизируем вложения от задания, а не задачи,
      // иначе на MS SQL в проект резолюции не пробросятся приложения,
      // которые добавил в группу исполнитель текущего задания.
      Functions.DocumentReviewTask.FillDraftResolutionProperties(DocumentReviewTasks.As(_obj.Task),
                                                                 _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault(),
                                                                 _obj.AddendaGroup.OfficialDocuments.Select(x => Sungero.Content.ElectronicDocuments.As(x)).ToList(),
                                                                 _obj.OtherGroup.All.ToList(),
                                                                 actionItem);
      actionItem.ShowModal();
      if (!actionItem.State.IsInserted)
      {
        var draftActionItem = Functions.Module.Remote.GetActionitemById(actionItem.Id);
        _obj.ResolutionGroup.ActionItemExecutionTasks.Add(draftActionItem);
      }
    }

    public virtual bool CanAddResolution(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.Assignment.Status.InProcess &&
        Docflow.PublicFunctions.Module.IsWorkStarted(_obj) &&
        Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

    public virtual void Abort(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      var dialogId = Constants.DocumentReviewTask.ReviewReworkAssignmentConfirmDialogID.Abort;
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            e.Action,
                                                                                            dialogId))
      {
        return;
      }
      
      _obj.Task.Abort();
      e.CloseFormAfterAction = true;
    }

    public virtual bool CanAbort(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status.Value == ReviewReworkAssignment.Status.InProcess &&
        Docflow.PublicFunctions.Module.IsWorkStarted(_obj);
    }

    public virtual void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.ReviewReworkAssignment.ShowForwardingDialog(_obj))
        e.Cancel();
      
      // Вывести запрос прав на группу "Дополнительно".
      var assignees = new List<IRecipient>() { _obj.Addressee };
      var assistant = Docflow.PublicFunctions.Module.GetSecretary(_obj.Addressee);
      if (assistant != null)
        assignees.Add(assistant);
      
      if (Docflow.PublicFunctions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), assignees) == false)
        e.Cancel();
    }

    public virtual bool CanForward(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

    public virtual void SendForReview(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var documentReviewTask = DocumentReviewTasks.As(_obj.Task);
      
      // Проверить, что исполнитель может готовить проект резолюции
      // и все поручения выданы адресатами рассмотрения.
      var actionItemAssigners = _obj.ResolutionGroup.ActionItemExecutionTasks.Select(a => a.AssignedBy).ToList();
      if (Functions.ReviewReworkAssignment.CanPrepareDraftResolution(_obj) &&
          actionItemAssigners.Any(x => documentReviewTask.Addressees.All(a => !Equals(a.Addressee, x))))
      {
        e.AddError(RecordManagement.Resources.ActionItemsMustBeAssignedByAddressee);
        return;
      }
      
      var giveRights = Docflow.PublicFunctions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), null);
      if (giveRights == false)
        e.Cancel();
      
      var dialogId = Constants.DocumentReviewTask.ReviewReworkAssignmentConfirmDialogID.SendForReview;
      if (giveRights == null && !Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null, dialogId))
        e.Cancel();
    }

    public virtual bool CanSendForReview(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

  }

}