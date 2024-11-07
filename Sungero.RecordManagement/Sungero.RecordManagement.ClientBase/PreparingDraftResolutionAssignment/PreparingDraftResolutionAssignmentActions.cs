using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.RecordManagement;
using Sungero.RecordManagement.PreparingDraftResolutionAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class PreparingDraftResolutionAssignmentActions
  {

    public override void ReturnUncompleted(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var allActionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.ToList();
      var actionItemsToDelete = Functions.Module.Remote.GetActionItemsAddedToAssignment(_obj, allActionItems, Company.Employees.Current,
                                                                                        PublicConstants.PreparingDraftResolutionAssignment.ResolutionGroupName);
      var hasActionItemsToDelete = actionItemsToDelete.Any();
      
      var description = hasActionItemsToDelete ? Resources.ConfirmDeleteDraftResolutionAssignment : null;
      var dropDialogId = hasActionItemsToDelete
        ? Constants.DocumentReviewTask.PreparingDraftResolutionAssignmentConfirmDialogID.ReturnUncompleted
        : Constants.DocumentReviewTask.PreparingDraftResolutionAssignmentConfirmDialogID.ReturnUncompletedWithDeletingDraftResolutions;
      var dropIsConfirmed = Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                                  description,
                                                                                  null,
                                                                                  dropDialogId);
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

    public virtual void Abort(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      var dialogId = Constants.DocumentReviewTask.PreparingDraftResolutionAssignmentConfirmDialogID.Abort;
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
      return _obj.Status == PreparingDraftResolutionAssignment.Status.InProcess &&
        Equals(_obj.Performer, _obj.Task.Author) &&
        _obj.IsRework == true &&
        Docflow.PublicFunctions.Module.IsWorkStarted(_obj) &&
        Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

    public virtual void ForRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Проверить заполненность текста комментария.
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(ReviewDraftResolutionAssignments.Resources.NeedTextToRework);
        return;
      }
      
      var documentReviewTask = DocumentReviewTasks.As(_obj.Task);
      
      // Проверить, что все поручения выданы от имени адресата.
      var wrongActionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.Where(x => documentReviewTask.Addressees.All(a => !Equals(a.Addressee, x.AssignedBy)));
      if (wrongActionItems.Any())
      {
        e.AddError(RecordManagement.Resources.ActionItemsMustBeAssignedByAddressee);
        return;
      }
      
      // Вывести предупреждение.
      var dialogID = Constants.DocumentReviewTask.PreparingDraftResolutionAssignmentConfirmDialogID.ForRework;
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(),
                                                                                            null, e.Action, dialogID))
      {
        e.Cancel();
      }
    }

    public virtual bool CanForRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return !Equals(_obj.Performer, _obj.Task.Author);
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

    public virtual void AddAssignment(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!_obj.ResolutionGroup.ActionItemExecutionTasks.Any(t => t.Status == ActionItemExecutionTask.Status.Draft))
      {
        var confirmationAccepted = Functions.Module.ShowConfirmationDialogCreationActionItem(_obj, _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault(), e);
        var dialogID = Constants.DocumentReviewTask.PreparingDraftResolutionAssignmentConfirmDialogID.AddAssignment;
        if (Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                             _obj.OtherGroup.All.ToList(),
                                                                                             confirmationAccepted ? null : e.Action,
                                                                                             dialogID) == false)
          e.Cancel();
      }
      else
      {
        // В качестве проектов резолюции нельзя отправлять поручения без заполненного срока или установленного чекбокса "Без срока".
        if (_obj.ResolutionGroup.ActionItemExecutionTasks.Any(a => !PublicFunctions.ActionItemExecutionTask.IsDeadlineAssigned(a)))
        {
          e.AddError(DocumentReviewTasks.Resources.FindResolutionWithoutDeadline);
          e.Cancel();
        }
        
        Functions.DocumentReviewTask.CheckOverdueActionItemExecutionTasks(DocumentReviewTasks.As(_obj.Task), e);
        
        var giveRights = Docflow.PublicFunctions.Module.ShowDialogGrantAccessRights(_obj,
                                                                                    _obj.OtherGroup.All.ToList(),
                                                                                    null);
        if (giveRights == false)
          e.Cancel();
        
        if (giveRights == null && Functions.PreparingDraftResolutionAssignment.ShowConfirmationDialogStartDraftResolution(_obj, e) == false)
          e.Cancel();
        
        var actionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.Where(t => t.Status == RecordManagement.ActionItemExecutionTask.Status.Draft).ToList();
        Functions.Module.Remote.PrepareDraftResolutionForStart(actionItems,
                                                               _obj,
                                                               _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault(),
                                                               _obj.AddendaGroup.OfficialDocuments.Select(x => Sungero.Content.ElectronicDocuments.As(x)).ToList(),
                                                               _obj.OtherGroup.All.ToList());
        foreach (var actionItem in actionItems)
          actionItem.Start();
      }
    }

    public virtual bool CanAddAssignment(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

    public virtual void Explored(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var hasActionItemsToDelete = _obj.ResolutionGroup.ActionItemExecutionTasks.Any();
      if (hasActionItemsToDelete)
      {
        var dropDialogId = Constants.DocumentReviewTask.PreparingDraftResolutionAssignmentConfirmDialogID.ExploredWithDeletingDraftResolutions;
        var dropIsConfirmed = Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                                    Resources.ConfirmDeleteDraftResolutionAssignment,
                                                                                    null, dropDialogId);
        if (!dropIsConfirmed)
          e.Cancel();
      }
      
      var confirmDialogId = Constants.DocumentReviewTask.PreparingDraftResolutionAssignmentConfirmDialogID.Explored;
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            hasActionItemsToDelete ? null : e.Action,
                                                                                            confirmDialogId))
      {
        e.Cancel();
      }
      
      _obj.NeedDeleteActionItems = hasActionItemsToDelete;
    }

    public virtual bool CanExplored(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

    public virtual void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.PreparingDraftResolutionAssignment.ShowForwardingDialog(_obj))
        e.Cancel();
      
      // Вывести запрос прав на группу "Дополнительно".
      var assignees = new List<IRecipient>() { _obj.Addressee };
      var assistant = Docflow.PublicFunctions.Module.GetSecretary(_obj.Addressee);
      if (assistant != null)
        assignees.Add(assistant);
      
      if (Docflow.PublicFunctions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), assignees) == false)
        e.Cancel();
      
      _obj.NeedDeleteActionItems = _obj.ResolutionGroup.ActionItemExecutionTasks
        .Where(x => !Equals(x.AssignedBy, _obj.Addressee) || x.IsDraftResolution != true)
        .Any();
    }

    public virtual bool CanForward(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

    public virtual void SendForReview(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var documentReviewTask = DocumentReviewTasks.As(_obj.Task);
      
      // В качестве проектов резолюции нельзя отправлять поручения без заполненного срока или установленного чекбокса "Без срока".
      if (_obj.ResolutionGroup.ActionItemExecutionTasks.Any(a => !PublicFunctions.ActionItemExecutionTask.IsDeadlineAssigned(a)))
      {
        e.AddError(DocumentReviewTasks.Resources.FindResolutionWithoutDeadline);
        return;
      }
      
      // Проверить, что все поручения выданы от имени адресата.
      var wrongActionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.Where(x => documentReviewTask.Addressees.All(a => !Equals(a.Addressee, x.AssignedBy)));
      if (wrongActionItems.Any())
      {
        e.AddError(RecordManagement.Resources.ActionItemsMustBeAssignedByAddressee);
        return;
      }
      
      var giveRights = Docflow.PublicFunctions.Module.ShowDialogGrantAccessRights(_obj,
                                                                                  _obj.OtherGroup.All.ToList(),
                                                                                  null);
      if (giveRights == false)
        e.Cancel();
      
      if (giveRights == null && _obj.NeedDeleteActionItems != true && !Functions.PreparingDraftResolutionAssignment.ShowConfirmationDialogSendForReview(_obj, e))
        e.Cancel();
    }

    public virtual bool CanSendForReview(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
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
      return _obj.Status.Value == PreparingDraftResolutionAssignment.Status.InProcess &&
        _obj.AccessRights.CanUpdate() &&
        Docflow.PublicFunctions.Module.IsWorkStarted(_obj) &&
        Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

  }

}