using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewManagerAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class ReviewManagerAssignmentFunctions
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
      
      var dialogResult = Functions.Module.ShowForwardDialog(excludedPerformers);
      if (dialogResult.ForwardButtonIsPressed)
      {
        _obj.Addressee = dialogResult.ForwardTo;
        return true;
      }
      
      return false;
    }
  }
}