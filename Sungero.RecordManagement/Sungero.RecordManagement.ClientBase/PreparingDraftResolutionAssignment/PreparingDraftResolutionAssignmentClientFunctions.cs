using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.PreparingDraftResolutionAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class PreparingDraftResolutionAssignmentFunctions
  {
    /// <summary>
    /// Диалог подтверждения старта поручений из проекта резолюции.
    /// </summary>
    /// <param name="e">Аргументы.</param>
    /// <returns>True, если диалог был, иначе false.</returns>
    public bool ShowConfirmationDialogStartDraftResolution(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var dialogText = PreparingDraftResolutionAssignments.Resources.ExecuteAndStartDraftResolution;
      var dialogTextDescription = PreparingDraftResolutionAssignments.Resources.ExecuteAndStartDraftResolutionDescription;
      var task = DocumentReviewTasks.As(_obj.Task);
      var dialogID = Constants.DocumentReviewTask.PreparingDraftResolutionAssignmentConfirmDialogID.AddAssignment;
      return Docflow.PublicFunctions.Module.ShowConfirmationDialog(dialogText, dialogTextDescription, null, dialogID);
    }
    
    /// <summary>
    /// Диалог подтверждения отправки документа на рассмотрение.
    /// </summary>
    /// <param name="e">Аргументы.</param>
    /// <returns>True, если диалог был, иначе false.</returns>
    public bool ShowConfirmationDialogSendForReview(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var actionItemsExist = _obj.ResolutionGroup.All.Count != 0;
      var dialogText = actionItemsExist ? PreparingDraftResolutionAssignments.Resources.SendForReviewWithResolution :
        PreparingDraftResolutionAssignments.Resources.SendForReviewWithoutResolution;
      var dialogID = Constants.DocumentReviewTask.PreparingDraftResolutionAssignmentConfirmDialogID.SendForReview;
      return Docflow.PublicFunctions.Module.ShowConfirmationDialog(dialogText, null, null, dialogID);
    }
    
    /// <summary>
    /// Показать диалог переадресации.
    /// </summary>
    /// <returns>True, если запрос был подтвержден.</returns>
    public virtual bool ShowForwardingDialog()
    {
      var documentReviewTask = DocumentReviewTasks.As(_obj.Task);
      var excludedPerformers = new List<IRecipient>();
      excludedPerformers.Add(documentReviewTask.Addressee);
      excludedPerformers.Add(_obj.Performer);
      
      var hasActionItemsToDelete = _obj.ResolutionGroup.ActionItemExecutionTasks
        .Where(x => !Equals(x.AssignedBy, _obj.Addressee) || x.IsDraftResolution != true)
        .Any();
      
      var dialogText = hasActionItemsToDelete
        ? Resources.ConfirmDeleteDraftResolutionAssignment
        : null;
      var dialogResult = Functions.Module.ShowForwardDialog(excludedPerformers, dialogText);
      if (dialogResult.ForwardButtonIsPressed)
      {
        _obj.Addressee = dialogResult.ForwardTo;
        return true;
      }
      
      return false;
    }
  }
}