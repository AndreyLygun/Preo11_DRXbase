using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.DocflowApproval.CheckReturnFromCounterpartyAssignment;

namespace Sungero.DocflowApproval.Client
{
  partial class CheckReturnFromCounterpartyAssignmentActions
  {
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

    public virtual void NotSigned(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Functions.CheckReturnFromCounterpartyAssignment.ValidateBeforeNotSigned(_obj, e))
        e.Cancel();
      
      var confirmationAccepted = Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                                       null,
                                                                                       null,
                                                                                       Constants.CheckReturnFromCounterpartyAssignment.NotSignedConfirmDialogID);
      if (!confirmationAccepted)
        e.Cancel();
    }

    public virtual bool CanNotSigned(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void Signed(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Functions.CheckReturnFromCounterpartyAssignment.ValidateBeforeSigned(_obj, e))
        e.Cancel();
      
      var confirmationAccepted = Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                                       null,
                                                                                       null,
                                                                                       Constants.CheckReturnFromCounterpartyAssignment.SignedConfirmDialogID);
      if (!confirmationAccepted)
        e.Cancel();
    }

    public virtual bool CanSigned(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }
  }
}