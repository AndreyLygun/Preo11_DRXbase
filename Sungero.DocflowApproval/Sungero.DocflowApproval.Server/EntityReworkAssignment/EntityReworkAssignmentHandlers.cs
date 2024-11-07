using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.EntityReworkAssignment;

namespace Sungero.DocflowApproval
{
  partial class EntityReworkAssignmentApproversApproverPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ApproversApproverFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var approvers = _root.Approvers.Where(a => a.Approver != null).Select(a => a.Approver);
      return query.Where(q => !approvers.Contains(q));
    }
  }
  
  partial class EntityReworkAssignmentExchangeServicePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ExchangeServiceFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      var services = Functions.Module.GetExchangeServices(officialDocument).Services;
      query = query.Where(s => services.Contains(s));
      return query;
    }
  }

  partial class EntityReworkAssignmentServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.ApproversDisabledReasonDescription = EntityReworkAssignments.Resources.ApproversDisabledReasonDescription;
    }
    
    public override void BeforeSaveHistory(Sungero.Domain.HistoryEventArgs e)
    {
      base.BeforeSaveHistory(e);
      
      if (_obj.State.Properties.Deadline.IsChanged && !_obj.State.IsInserted)
      {
        e.Operation = new Enumeration(Constants.EntityReworkAssignment.Operation.DeadlineExtend);
      }
    }

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (Functions.EntityReworkAssignment.AreDocumentsLockedByMe(_obj))
      {
        e.AddError(Resources.SaveDocumentsBeforeComplete);
        return;
      }
      
      if (_obj.Result == Result.ForReapproval)
        e.Result = EntityReworkAssignments.Resources.ForReapproval;
      else if (_obj.Result == Result.Forward)
        e.Result = DocflowApproval.Resources.ForwardedFormat(Company.PublicFunctions.Employee.GetShortName(_obj.ForwardTo, DeclensionCase.Dative, true));
    }
  }

}