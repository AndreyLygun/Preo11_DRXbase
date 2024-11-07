using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.DocumentFlowTask;

namespace Sungero.DocflowApproval.Client
{
  partial class DocumentFlowTaskActions
  {
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

    public override void Restart(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // В платформенном рестарте вариант процесса очищается. Сохраняем его заранее, чтобы потом восстановить.
      if (_obj.ProcessKind != null)
        e.Params.AddOrUpdate(Constants.DocumentFlowTask.LastProcessKindIdParamName, _obj.ProcessKind.Id);
      
      base.Restart(e);
    }

    public override bool CanRestart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanRestart(e);
    }

    public override void Start(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Functions.DocumentFlowTask.HasDocumentAndCanRead(_obj))
      {
        e.AddError(Docflow.Resources.NoRightsToDocument);
        return;
      }
      
      if (!Functions.DocumentFlowTask.ValidateDocumentFlowTaskStart(_obj, e))
        return;
      
      if (!Functions.DocumentFlowTask.GrantReadAccessRightToObservers(_obj))
        return;
      
      var document = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      var initiator = Sungero.Company.Employees.As(_obj.Author);
      var userConfirmedStart = false;
      var createdTasks = DocflowApproval.PublicFunctions.Module.Remote.GetDocumentFlowTasks(document);
      if (createdTasks.Any())
      {
        userConfirmedStart = Docflow.PublicFunctions.Module.RequestUserToConfirmDocumentFlowTaskCreation(createdTasks);
      }
      else
      {
        userConfirmedStart = Docflow.PublicFunctions.Module
          .ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                  null,
                                  null,
                                  Constants.DocumentFlowTask.StartConfirmDialogID);
      }
      
      if (!userConfirmedStart)
        return;
      
      if (Functions.Module.Remote.EmployeeIsApprover(_obj, initiator))
      {
        var endorsingError = Functions.DocumentFlowTask.EndorseDocuments(_obj, initiator);
        if (!string.IsNullOrEmpty(endorsingError))
        {
          e.AddError(endorsingError);
          return;
        }
      }
      
      base.Start(e);
    }

    public override bool CanStart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanStart(e);
    }

    public override void Abort(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (Functions.DocumentFlowTask.GetReasonBeforeAbort(_obj, null, e, true))
      {
        base.Abort(e);
        Functions.DocumentFlowTask.AbortAsyncProcessingNotify(_obj);
      }
    }

    public override bool CanAbort(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanAbort(e);
    }

  }

}