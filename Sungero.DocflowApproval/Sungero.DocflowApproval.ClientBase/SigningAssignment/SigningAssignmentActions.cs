using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.SigningAssignment;

namespace Sungero.DocflowApproval.Client
{
  partial class SigningAssignmentActions
  {
    public virtual void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.SigningAssignment.ValidateBeforeForward(_obj, e))
        e.Cancel();
      
      if (!Functions.SigningAssignment.ShowForwardingDialog(_obj))
        e.Cancel();
    }
    
    public virtual bool CanForward(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return !Docflow.PublicFunctions.Module.IsCompetitive(_obj);
    }

    public virtual void ApprovalForm(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!_obj.DocumentGroup.ElectronicDocuments.Any())
      {
        e.AddError(Docflow.Resources.NoRightsToDocument);
        return;
      }
      
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.Single());
      Docflow.PublicFunctions.Module.RunApprovalSheetReport(officialDocument);
    }

    public virtual bool CanApprovalForm(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.DocumentGroup.ElectronicDocuments.Any(d => OfficialDocuments.Is(d) && d.HasVersions);
    }

    public virtual void Reject(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!_obj.DocumentGroup.ElectronicDocuments.Any())
        return;
      
      if (!Functions.SigningAssignment.ValidateBeforeReject(_obj, e))
        e.Cancel();

      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                 null,
                                                                 null,
                                                                 Constants.SigningAssignment.RejectConfirmDialogID))
      {
        e.Cancel();
      }
      
      var signingError = Functions.SigningAssignment.NotEndorseDocuments(_obj);
      if (!string.IsNullOrEmpty(signingError))
      {
        e.AddError(signingError);
        return;
      }
    }

    public virtual bool CanReject(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void ForRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.SigningAssignment.ValidateBeforeRework(_obj, e))
        e.Cancel();
      
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                 null,
                                                                 null,
                                                                 Constants.SigningAssignment.ForReworkConfirmDialogID))
      {
        e.Cancel();
      }
      
      if (Functions.SigningAssignment.IsUserWithoutRightsOnMainDocument(_obj))
        return;
      
      if (!_obj.DocumentGroup.ElectronicDocuments.Any())
        return;
      
      var signingError = Functions.SigningAssignment.NotEndorseDocuments(_obj);
      if (!string.IsNullOrEmpty(signingError))
      {
        e.AddError(signingError);
        return;
      }
    }
    
    public virtual bool CanForRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
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
    
    public virtual void Sign(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!_obj.DocumentGroup.ElectronicDocuments.Any())
        return;
      
      if (!Functions.SigningAssignment.ValidateBeforeSign(_obj, e))
        e.Cancel();
      
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                 null,
                                                                 null,
                                                                 Constants.SigningAssignment.SignConfirmDialogID))
      {
        e.Cancel();
      }
      
      var signingError = Functions.SigningAssignment.SignDocuments(_obj);
      if (!string.IsNullOrEmpty(signingError))
      {
        e.AddError(signingError);
        return;
      }
    }
    
    public virtual bool CanSign(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }
}