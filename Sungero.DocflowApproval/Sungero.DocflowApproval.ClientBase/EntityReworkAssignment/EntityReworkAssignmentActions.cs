using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.DocflowApproval.EntityReworkAssignment;

namespace Sungero.DocflowApproval.Client
{
  partial class EntityReworkAssignmentActions
  {
    public virtual void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Functions.EntityReworkAssignment.ValidateBeforeForward(_obj, e))
        e.Cancel();
      
      if (!Functions.EntityReworkAssignment.ShowForwardingDialog(_obj))
        e.Cancel();
    }

    public virtual bool CanForward(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Status == Status.InProcess && !Docflow.PublicFunctions.Module.IsCompetitive(_obj);
    }

    public virtual void ExtendDeadline(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var newDeadline = Functions.EntityReworkAssignment.GetNewDeadline(_obj.Deadline);
      
      if (newDeadline != null)
      {
        _obj.Deadline = newDeadline.Value;
        _obj.Save();
        Dialogs.NotifyMessage(EntityReworkAssignments.Resources.NewDeadlineSet);
      }
    }

    public virtual bool CanExtendDeadline(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Status.InProcess &&
        _obj.AccessRights.CanUpdate() &&
        _obj.Deadline.HasValue;
    }

    public virtual void Abort(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;

      if (DocumentFlowTasks.Is(_obj.Task))
      {
        if (Functions.DocumentFlowTask.GetReasonBeforeAbort(DocumentFlowTasks.As(_obj.Task), _obj.ActiveText, e, false))
          Functions.DocumentFlowTask.AbortAsyncProcessingNotify(DocumentFlowTasks.As(_obj.Task));
        else
          return;
      }
      else if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                      null,
                                                                      null,
                                                                      Constants.EntityReworkAssignment.AbortConfirmDialogID))
      {
        return;
      }
      
      _obj.Task.Abort();
      e.CloseFormAfterAction = true;
    }

    public virtual bool CanAbort(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Status.InProcess &&
        _obj.AccessRights.CanUpdate() &&
        Docflow.PublicFunctions.Module.IsWorkStarted(_obj);
    }

    public virtual void ForReapproval(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!_obj.DocumentGroup.ElectronicDocuments.Any())
        return;
      
      if (!Functions.EntityReworkAssignment.ValidateBeforeForReapproval(_obj, e))
        e.Cancel();
      
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                 null,
                                                                 null,
                                                                 Constants.EntityReworkAssignment.ForReapprovalConfirmDialogID))
      {
        e.Cancel();
      }
      
      var performer = Sungero.Company.Employees.As(_obj.Performer);
      if (Functions.Module.Remote.EmployeeIsApprover(_obj.Task, performer))
      {
        var endorsingError = Functions.EntityReworkAssignment.EndorseDocuments(_obj);
        if (!string.IsNullOrEmpty(endorsingError))
        {
          e.AddError(endorsingError);
          return;
        }
      }
    }

    public virtual bool CanForReapproval(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }

}