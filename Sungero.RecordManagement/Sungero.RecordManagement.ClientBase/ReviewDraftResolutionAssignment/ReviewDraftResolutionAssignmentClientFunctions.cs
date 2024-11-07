using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewDraftResolutionAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class ReviewDraftResolutionAssignmentFunctions
  {
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
      
      var hasActionItemsToDelete = _obj.ResolutionGroup.ActionItemExecutionTasks.Any();
      var dialogText = hasActionItemsToDelete ? Resources.ConfirmDeleteDraftResolutionAssignment : null;
      
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