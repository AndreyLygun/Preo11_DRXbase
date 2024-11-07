using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.DocflowApproval.AdvancedAssignment;

namespace Sungero.DocflowApproval.Client
{
  partial class AdvancedAssignmentActions
  {
    public virtual void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Functions.AdvancedAssignment.ValidateBeforeForward(_obj, e))
        e.Cancel();
      
      if (!Functions.AdvancedAssignment.ShowForwardingDialog(_obj))
        e.Cancel();
    }

    public virtual bool CanForward(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.AdvancedAssignment.CanForward(_obj) && !Docflow.PublicFunctions.Module.IsCompetitive(_obj);
    }

    public virtual void ExtendDeadline(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var task = Docflow.PublicFunctions.DeadlineExtensionTask.Remote.GetDeadlineExtension(_obj);
      task.Show();
    }

    public virtual bool CanExtendDeadline(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.AssignmentBase.Status.InProcess &&
        _obj.AccessRights.CanUpdate() &&
        _obj.Deadline.HasValue;
    }

    public virtual void ForRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.AdvancedAssignment.ValidateBeforeRework(_obj, e))
        e.Cancel();
      
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                 null,
                                                                 null,
                                                                 Constants.AdvancedAssignment.ReworkConfirmDialogID))
      {
        e.Cancel();
      }
    }

    public virtual bool CanForRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.AdvancedAssignment.CanSendForRework(_obj);
    }

    public virtual void Complete(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Functions.AdvancedAssignment.ValidateBeforeComplete(_obj, e))
        e.Cancel();
      
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                 null,
                                                                 null,
                                                                 Constants.AdvancedAssignment.CompleteConfirmDialogID))
      {
        e.Cancel();
      }
    }

    public virtual bool CanComplete(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }

}