using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.EntityApprovalAssignment;

namespace Sungero.DocflowApproval
{
  partial class EntityApprovalAssignmentExchangeServicePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ExchangeServiceFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      var services = Functions.Module.GetExchangeServices(officialDocument).Services;
      query = query.Where(s => services.Contains(s));
      return query;
    }
  }

  partial class EntityApprovalAssignmentAddApproversApproverPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> AddApproversApproverFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = query.Where(c => c.Status == CoreEntities.DatabookEntry.Status.Active);
      
      // Отфильтровать всех пользователей.
      query = query.Where(x => x.Sid != Sungero.Domain.Shared.SystemRoleSid.AllUsers);
      
      // Отфильтровать служебные роли.
      return (IQueryable<T>)RecordManagement.PublicFunctions.Module.ObserversFiltering(query);
    }
  }

  partial class EntityApprovalAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (Functions.EntityApprovalAssignment.AreDocumentsLockedByMe(_obj))
      {
        e.AddError(Resources.SaveDocumentsBeforeComplete);
        return;
      }
      
      if (_obj.Result == Result.Approved)
        e.Result = EntityApprovalAssignments.Resources.Endorsed;
      else if (_obj.Result == Result.WithSuggestions)
        e.Result = EntityApprovalAssignments.Resources.EndorsedWithSuggestions;
      else if (_obj.Result == Result.ForRework)
        e.Result = DocflowApproval.Resources.ForRework;
      else if (_obj.Result == Result.Forward)
        e.Result = DocflowApproval.Resources.ForwardedFormat(Company.PublicFunctions.Employee.GetShortName(_obj.ForwardTo, DeclensionCase.Dative, true));
    }
  }
}