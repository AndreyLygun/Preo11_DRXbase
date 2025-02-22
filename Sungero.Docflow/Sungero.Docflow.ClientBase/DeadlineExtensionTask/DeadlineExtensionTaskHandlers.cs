using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineExtensionTask;

namespace Sungero.Docflow
{
  partial class DeadlineExtensionTaskClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      var assignees = Functions.DeadlineExtensionTask.Remote.GetAssigneesForDeadlineExtensionFromAssignment(_obj);
      e.Params.AddOrUpdate(Constants.DeadlineExtensionTask.CanSelectAssignee, assignees.Count() > 1);
    }

    public virtual void NewDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      if (_obj.CurrentDeadline < Calendar.Now)
      {
        // Проверить корректность срока.
        if (!Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, Calendar.Now))
          e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday);
      }
      else
      {
        // Новый срок должен быть больше старого.
        if (!Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, _obj.CurrentDeadline))
          e.AddError(DeadlineExtensionTasks.Resources.DesiredDeadlineIsNotCorrect);
      }
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      _obj.State.Properties.Texts.IsVisible = false;

      bool canSelect = true;
      if (e.Params.TryGetValue(Constants.DeadlineExtensionTask.CanSelectAssignee, out canSelect) && !canSelect)
        _obj.State.Properties.Assignee.IsEnabled = false;
    }

  }
}