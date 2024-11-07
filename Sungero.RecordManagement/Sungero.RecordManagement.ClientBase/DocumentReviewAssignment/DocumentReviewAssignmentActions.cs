using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DocumentReviewAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class DocumentReviewAssignmentActions
  {
    public override void ReturnUncompleted(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var allActionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.ToList();
      var actionItemsToDelete = Functions.Module.Remote.GetActionItemsAddedToAssignment(_obj, allActionItems, Company.Employees.Current,
                                                                                        PublicConstants.PreparingDraftResolutionAssignment.ResolutionGroupName);
      var hasActionItemsToDelete = actionItemsToDelete.Any();
      
      var description = hasActionItemsToDelete ? Resources.ConfirmDeleteDraftResolutionAssignment : null;
      var dropDialogId = hasActionItemsToDelete
        ? Constants.DocumentReviewTask.DocumentReviewAssignmentConfirmDialogID.ReturnUncompleted
        : Constants.DocumentReviewTask.DocumentReviewAssignmentConfirmDialogID.ReturnUncompletedWithDeletingDraftResolutions;
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

    public virtual void ResPassed(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Проверить заполненность текста резолюции при выполнении задания с результатом "Вынести резолюцию".
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(ReviewManagerAssignments.Resources.ResolutionTextNeeded);
        return;
      }
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(), e.Action,
                                                                                            Constants.DocumentReviewTask.ReviewManagerAssignmentConfirmDialogID.AddResolution))
        e.Cancel();
    }

    public virtual bool CanResPassed(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.DocumentReviewAssignment.HasDocumentAndCanRead(_obj) &&
        !_obj.ResolutionGroup.ActionItemExecutionTasks.Any();
    }

    public virtual void DraftResApprove(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // В качестве проектов резолюции нельзя отправлять поручения без заполненного срока или установленного чекбокса "Без срока".
      if (_obj.ResolutionGroup.ActionItemExecutionTasks.Any(a => !PublicFunctions.ActionItemExecutionTask.IsDeadlineAssigned(a)))
      {
        e.AddError(DocumentReviewTasks.Resources.FindResolutionWithoutDeadline);
        e.Cancel();
      }
      
      Functions.DocumentReviewAssignment.CheckOverdueActionItemExecutionTasks(_obj, e);
      
      // Замена стандартного диалога подтверждения выполнения действия.
      var dialogID = Constants.DocumentReviewTask.ReviewDraftResolutionAssignmentConfirmDialogID.ForExecution;
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            e.Action,
                                                                                            dialogID))
        e.Cancel();
      
      var draftResolution = Functions.DocumentReviewAssignment.Remote.GetDraftResolutionPreparedForStart(_obj);
      foreach (var actionItem in draftResolution)
        actionItem.Start();
    }

    public virtual bool CanDraftResApprove(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.DocumentReviewAssignment.HasDocumentAndCanRead(_obj) &&
        _obj.ResolutionGroup.ActionItemExecutionTasks.Any();
    }

    public virtual void DraftResRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Проверить, что все поручения выданы от имени адресата.
      var documentReviewTask = DocumentReviewTasks.As(_obj.Task);
      if (documentReviewTask != null)
      {
        var wrongActionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.Where(x => documentReviewTask.Addressees.All(a => !Equals(a.Addressee, x.AssignedBy)));
        if (wrongActionItems.Any())
        {
          e.AddError(RecordManagement.Resources.ActionItemsMustBeAssignedByAddressee);
          return;
        }
      }
      
      // Проверить заполненность текста резолюции.
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(ReviewDraftResolutionAssignments.Resources.NeedTextToRework);
        return;
      }
      
      var dialogID = Constants.DocumentReviewTask.ReviewDraftResolutionAssignmentConfirmDialogID.AddResolution;
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            e.Action,
                                                                                            dialogID))
        e.Cancel();
    }

    public virtual bool CanDraftResRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      // Если задание было сформировано на старой схеме и со старым кодом блока, AssistantPrepareResolution будет null, но это не должно влиять на доступность (326410).
      return _obj.ResolutionGroup.ActionItemExecutionTasks.Any() && (!_obj.AssistantPrepareResolution.HasValue || _obj.AssistantPrepareResolution.Value);
    }

    public virtual void DocsRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Проверить заполненность текста комментария.
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(ReviewDraftResolutionAssignments.Resources.NeedTextToRework);
        return;
      }
      
      // Вывести предупреждение.
      var dialogID = Constants.DocumentReviewTask.ReviewManagerAssignmentConfirmDialogID.ForRework;
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(),
                                                                                            null, e.Action, dialogID))
      {
        e.Cancel();
      }
    }

    public virtual bool CanDocsRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return !_obj.ResolutionGroup.ActionItemExecutionTasks.Any() || _obj.AssistantPrepareResolution == false;
    }

    public virtual void ActionItemsSent(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var confirmationAccepted = Functions.Module.ShowConfirmationDialogCreationActionItem(_obj, _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault(), e);
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(),
                                                                                            confirmationAccepted ? null : e.Action,
                                                                                            Constants.DocumentReviewTask.ReviewManagerAssignmentConfirmDialogID.AddAssignment))
        e.Cancel();
    }

    public virtual bool CanActionItemsSent(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.DocumentReviewAssignment.HasDocumentAndCanRead(_obj) &&
        !_obj.ResolutionGroup.ActionItemExecutionTasks.Any();
    }

    public virtual void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.DocumentReviewAssignment.ShowForwardingDialog(_obj))
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
      return Functions.DocumentReviewAssignment.HasDocumentAndCanRead(_obj);
    }

    public virtual void Informed(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Подтверждение удаления проекта резолюции.
      var hasActionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.Any();
      if (hasActionItems)
      {
        var dropDialogId = Constants.DocumentReviewTask.ReviewDraftResolutionAssignmentConfirmDialogID.InformedWithDrop;
        var dropIsConfirmed = Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                                    Resources.ConfirmDeleteDraftResolutionAssignment,
                                                                                    null, dropDialogId);
        if (!dropIsConfirmed)
          e.Cancel();
      }
      
      // Вывести предупреждение, если заполнена резолюция.
      var resolutionExists = !string.IsNullOrWhiteSpace(_obj.ActiveText);
      if (resolutionExists && !hasActionItems)
      {
        var dialogText = e.Action.ConfirmationMessage;
        var dialogDescription = ReviewManagerAssignments.Resources.ConfirmResultIsExploredDescription;
        var dialogID = Constants.DocumentReviewTask.ReviewManagerAssignmentConfirmDialogID.ExploredWithResolution;
        if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(dialogText, dialogDescription, null, dialogID))
          e.Cancel();
      }

      // Запрос прав на группу "Дополнительно".
      var grandRightDialogResult = Docflow.PublicFunctions.Module
        .ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), null);
      if (grandRightDialogResult == false)
        e.Cancel();
      
      // Подтверждение выполнения действия.
      var dialogId = Constants.DocumentReviewTask.ReviewDraftResolutionAssignmentConfirmDialogID.Informed;
      if (!hasActionItems && !resolutionExists && grandRightDialogResult == null &&
          !Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null, dialogId))
        e.Cancel();
    }

    public virtual bool CanInformed(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.DocumentReviewAssignment.HasDocumentAndCanRead(_obj);
    }

    public virtual void CreateActionItem(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      var performer = Company.Employees.As(_obj.Performer);
      var assignedBy = performer.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed &&
        Docflow.PublicFunctions.Module.Remote.IsUsersCanBeResolutionAuthor(document, performer) ?
        performer :
        null;
      var actionItem = Functions.Module.Remote.CreateActionItemExecutionWithResolution(document, _obj.Id, _obj.ActiveText, assignedBy);
      
      Functions.ActionItemExecutionTask.SynchronizeDocumentGroupsToActionItem(actionItem,
                                                                              _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault(),
                                                                              _obj.AddendaGroup.OfficialDocuments.Select(x => Sungero.Content.ElectronicDocuments.As(x)).ToList(),
                                                                              _obj.OtherGroup.All.ToList());
      
      actionItem.ShowModal();
    }

    public virtual bool CanCreateActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status.Value == Workflow.Task.Status.InProcess &&
        Docflow.PublicFunctions.Module.IsWorkStarted(_obj) &&
        Functions.DocumentReviewAssignment.HasDocumentAndCanRead(_obj) &&
        !_obj.ResolutionGroup.ActionItemExecutionTasks.Any();
    }
    
  }

}