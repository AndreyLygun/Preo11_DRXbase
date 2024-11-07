using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewDraftResolutionAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class ReviewDraftResolutionAssignmentActions
  {
    public virtual void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.ReviewDraftResolutionAssignment.ShowForwardingDialog(_obj))
        e.Cancel();
      
      // Запрос прав на группу "Дополнительно".
      var assignees = new List<IRecipient>() { _obj.Addressee };
      var assistant = Docflow.PublicFunctions.Module.GetSecretary(_obj.Addressee);
      if (assistant != null)
        assignees.Add(assistant);
      var grandRightDialogResult = Docflow.PublicFunctions.Module
        .ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), assignees);
      if (grandRightDialogResult == false)
        e.Cancel();
      
      _obj.NeedDeleteActionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.Any();
    }

    public virtual bool CanForward(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }
    
    public virtual void AddResolution(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      
      // Проверить, что все поручения выданы от имени адресата.
      var documentReviewTask = DocumentReviewTasks.As(_obj.Task);
      var wrongActionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.Where(x => documentReviewTask.Addressees.All(a => !Equals(a.Addressee, x.AssignedBy)));
      if (wrongActionItems.Any())
      {
        e.AddError(RecordManagement.Resources.ActionItemsMustBeAssignedByAddressee);
        return;
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

    public virtual bool CanAddResolution(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void Informed(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Подтверждение удаления проекта резолюции.
      var hasActionItemsToDelete = _obj.ResolutionGroup.ActionItemExecutionTasks.Any();
      var dropDialogId = Constants.DocumentReviewTask.ReviewDraftResolutionAssignmentConfirmDialogID.InformedWithDrop;
      if (hasActionItemsToDelete)
      {
        var dropIsConfirmed = Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                                    Resources.ConfirmDeleteDraftResolutionAssignment,
                                                                                    null, dropDialogId);
        if (!dropIsConfirmed)
          e.Cancel();
      }
      
      // Запрос прав на группу "Дополнительно".
      var grandRightDialogResult = Docflow.PublicFunctions.Module
        .ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), null);
      if (grandRightDialogResult == false)
        e.Cancel();
      
      // Подтверждение выполнения действия.
      var dialogId = Constants.DocumentReviewTask.ReviewDraftResolutionAssignmentConfirmDialogID.Informed;
      if (!hasActionItemsToDelete && grandRightDialogResult == null &&
          !Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null, dialogId))
        e.Cancel();
      
      _obj.NeedDeleteActionItems = hasActionItemsToDelete;
    }

    public virtual bool CanInformed(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

    public virtual void ForExecution(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      Functions.DocumentReviewTask.CheckOverdueActionItemExecutionTasks(DocumentReviewTasks.As(_obj.Task), e);
      
      // Замена стандартного диалога подтверждения выполнения действия.
      var dialogID = Constants.DocumentReviewTask.ReviewDraftResolutionAssignmentConfirmDialogID.ForExecution;
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            e.Action,
                                                                                            dialogID))
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

    public virtual bool CanForExecution(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

  }

}