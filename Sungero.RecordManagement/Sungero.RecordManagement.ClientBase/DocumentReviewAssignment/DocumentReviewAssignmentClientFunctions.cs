using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DocumentReviewAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class DocumentReviewAssignmentFunctions
  {
    /// <summary>
    /// Проверить просроченные поручения, вывести ошибку в случае просрочки.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void CheckOverdueActionItemExecutionTasks(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var overdueTasks = Functions.DocumentReviewAssignment.GetDraftOverdueActionItemExecutionTasks(_obj);
      if (overdueTasks.Any())
      {
        e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanTodayCorrectIt);
        e.Cancel();
      }
    }

    /// <summary>
    /// Показать диалог переадресации.
    /// </summary>
    /// <returns>True, если запрос был подтвержден.</returns>
    public virtual bool ShowForwardingDialog()
    {
      var excludedPerformers = new List<IRecipient>();
      excludedPerformers.Add(_obj.Performer);
      if (DocumentReviewTasks.Is(_obj.Task))
        excludedPerformers.Add(DocumentReviewTasks.As(_obj.Task).Addressee);
      
      var dialogText = _obj.ResolutionGroup.ActionItemExecutionTasks.Any()
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