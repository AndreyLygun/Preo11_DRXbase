using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.EntityApprovalAssignment;
using Sungero.Workflow;

namespace Sungero.DocflowApproval.Client
{
  partial class EntityApprovalAssignmentActions
  {
    public virtual void AddApprover(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var dialog = Dialogs.CreateInputDialog(EntityApprovalAssignments.Resources.AddApprover);
      var activeAssignments = Functions.EntityApprovalAssignment.Remote.GetActiveAssignments(_obj);
      var employee = dialog.AddSelect<Sungero.Company.IEmployee>(EntityApprovalAssignments.Resources.Approver, true, null)
        .Where(x => x.Status != Company.Employee.Status.Closed && !Equals(x, _obj.Performer) &&
               !activeAssignments.Any(p => Equals(p.Performer, x)));
      var defaultDeadline = Docflow.PublicFunctions.Module.CheckDeadline(_obj.Deadline, Calendar.Now) ? _obj.Deadline : null;
      var deadline = dialog.AddDate(EntityApprovalAssignments.Resources.AddApproverDeadline, _obj.Deadline.HasValue, defaultDeadline).AsDateTime();
      var addButton = dialog.Buttons.AddCustom(EntityApprovalAssignments.Resources.Add);
      dialog.Buttons.AddCancel();
      dialog.SetOnButtonClick(a =>
                              {
                                if (a.IsValid && a.Button == addButton)
                                {
                                  if (Functions.EntityApprovalAssignment.Remote.CanForwardTo(_obj, employee.Value))
                                  {
                                    Functions.Module.Remote.GrantReadAccessRightsToDocuments(_obj.AddendaGroup.ElectronicDocuments.ToList(), _obj.Task.Author);
                                    DocflowApproval.Functions.Module.Remote.AddApprover(_obj, employee.Value, deadline.Value);
                                    var employeeShortName = Company.PublicFunctions.Employee.GetShortName(employee.Value, DeclensionCase.Nominative, false);
                                    Dialogs.NotifyMessage(EntityApprovalAssignments.Resources.NewApproverAddedFormat(employeeShortName));
                                  }
                                  else
                                    a.AddError(EntityApprovalAssignments.Resources.ApproverAlreadyExistsFormat(employee.Value.Person.ShortName));
                                }
                              });
      
      dialog.SetOnRefresh((r) =>
                          {
                            if (!Docflow.PublicFunctions.Module.CheckDeadline(employee.Value ?? Users.Current, deadline.Value, Calendar.Now))
                              r.AddError(EntityApprovalAssignments.Resources.ImpossibleSpecifyDeadlineLessThanToday, deadline);
                            else
                            {
                              var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(employee.Value ?? Users.Current, deadline.Value);
                              if (!string.IsNullOrEmpty(warnMessage))
                                r.AddWarning(warnMessage);
                            }
                          });
      
      var result = dialog.Show();
    }

    public virtual bool CanAddApprover(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Status.InProcess && _obj.AccessRights.CanUpdate() && !Docflow.PublicFunctions.Module.IsCompetitiveWorkStarted(_obj);
    }

    public virtual void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Functions.EntityApprovalAssignment.ValidateBeforeForward(_obj, e))
        e.Cancel();
      
      if (!DocflowApproval.Functions.EntityApprovalAssignment.ShowForwardingDialog(_obj))
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

    public virtual void WithSuggestions(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Functions.EntityApprovalAssignment.ValidateBeforeWithSuggestions(_obj, e))
        e.Cancel();
      
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                 null,
                                                                 null,
                                                                 Constants.EntityApprovalAssignment.WithSuggestionsConfirmDialogID))
      {
        e.Cancel();
      }
      
      if (!_obj.DocumentGroup.ElectronicDocuments.Any())
        return;
      
      var endorsingError = Functions.EntityApprovalAssignment.EndorseDocuments(_obj, true, true);
      if (!string.IsNullOrEmpty(endorsingError))
      {
        e.AddError(endorsingError);
        return;
      }
    }

    public virtual bool CanWithSuggestions(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void ForRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.EntityApprovalAssignment.ValidateBeforeRework(_obj, e))
        e.Cancel();
      
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                 null,
                                                                 null,
                                                                 Constants.EntityApprovalAssignment.ForReworkConfirmDialogID))
        e.Cancel();
      
      if (Functions.EntityApprovalAssignment.IsUserWithoutRightsOnMainDocument(_obj))
        return;
      
      if (!_obj.DocumentGroup.ElectronicDocuments.Any())
        return;
      
      var endorsingError = Functions.EntityApprovalAssignment.EndorseDocuments(_obj, false, false);
      if (!string.IsNullOrEmpty(endorsingError))
      {
        e.AddError(endorsingError);
        return;
      }
    }

    public virtual bool CanForRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void Approved(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Functions.EntityApprovalAssignment.ValidateBeforeApproval(_obj, e))
        e.Cancel();
      
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                 null,
                                                                 null,
                                                                 Constants.EntityApprovalAssignment.ApprovedConfirmDialogID))
      {
        e.Cancel();
      }
      
      if (!_obj.DocumentGroup.ElectronicDocuments.Any())
        return;
      
      var endorsingError = Functions.EntityApprovalAssignment.EndorseDocuments(_obj, true, false);
      if (!string.IsNullOrEmpty(endorsingError))
      {
        e.AddError(endorsingError);
        return;
      }
    }

    public virtual bool CanApproved(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }
}