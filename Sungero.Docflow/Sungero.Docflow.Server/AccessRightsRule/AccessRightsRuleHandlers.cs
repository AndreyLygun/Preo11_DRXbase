using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccessRightsRule;

namespace Sungero.Docflow
{
  partial class AccessRightsRuleCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.BulkProcessingState);
      e.Without(_info.Properties.LaunchId);
    }
  }

  partial class AccessRightsRuleMembersRecipientPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> MembersRecipientFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var systemRecipientsSid = Company.PublicFunctions.Module.GetSystemRecipientsSidWithoutAllUsers(true);
      return query.Where(x => !x.Sid.HasValue || x.Sid.HasValue && !systemRecipientsSid.Contains(x.Sid.Value));
    }
  }

  partial class AccessRightsRuleFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return query;
      
      if (_filter.Active || _filter.Closed)
        query = query.Where(r => _filter.Active && r.Status == Status.Active || _filter.Closed && r.Status == Status.Closed);
      
      if (_filter.BusinessUnit != null)
        query = query.Where(r => !r.BusinessUnits.Any() || r.BusinessUnits.Any(u => Equals(u.BusinessUnit, _filter.BusinessUnit)));
      
      if (_filter.Department != null)
        query = query.Where(r => !r.Departments.Any() || r.Departments.Any(u => Equals(u.Department, _filter.Department)));
      
      if (_filter.DocumentKind != null)
        query = query.Where(r => !r.DocumentKinds.Any() || r.DocumentKinds.Any(u => Equals(u.DocumentKind, _filter.DocumentKind)));
      
      return query;
    }
  }

  partial class AccessRightsRuleDocumentGroupsDocumentGroupPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentGroupsDocumentGroupFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var groups = Functions.AccessRightsRule.GetDocumentGroups(_root);
      return query.Where(g => groups.Contains(g));
    }
  }

  partial class AccessRightsRuleServerHandlers
  {

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      bool planBulkProcessing = false;
      if (e.Params.TryGetValue(Constants.AccessRightsRule.PlanBulkProcessing, out planBulkProcessing) && planBulkProcessing)
        PublicFunctions.Module.CreateGrantAccessRightsToDocumentsByRuleAsyncHandler(_obj.Id, _obj.LaunchId);
      
      e.Params.Remove(Constants.AccessRightsRule.PlanBulkProcessing);
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (Equals(_obj.Status, Status.Closed))
      {
        Functions.AccessRightsRule.CancelBulkProcessing(_obj);
        return;
      }

      var bulkProcessingCanceled = false;
      if (e.Params.TryGetValue(Constants.AccessRightsRule.BulkProcessingCanceled, out bulkProcessingCanceled) && bulkProcessingCanceled)
      {
        e.Params.Remove(Constants.AccessRightsRule.BulkProcessingCanceled);
        return;
      }
      
      if (!_obj.DocumentKinds.Any() &&
          !_obj.BusinessUnits.Any() &&
          !_obj.Departments.Any() &&
          !_obj.DocumentGroups.Any())
      {
        e.AddError(AccessRightsRules.Resources.RuleMustHaveCriteria);
      }

      var planBulkProcessing = _obj.GrantRightsOnExistingDocuments == true &&
        Functions.AccessRightsRule.CriteriaOrMembersChanged(_obj) && _obj.Members.Any();
      
      e.Params.AddOrUpdate(Constants.AccessRightsRule.PlanBulkProcessing, planBulkProcessing);
      if (planBulkProcessing)
        Functions.AccessRightsRule.PlanBulkProcessing(_obj);
      else if (_obj.GrantRightsOnExistingDocuments == false)
        Functions.AccessRightsRule.CancelBulkProcessing(_obj);
    }

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      _obj.Modified = Calendar.Now;
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (_obj.GrantRightsOnLeadingDocument == null)
        _obj.GrantRightsOnLeadingDocument = false;
      
      if (_obj.GrantRightsOnExistingDocuments == null)
        _obj.GrantRightsOnExistingDocuments = false;
      
      _obj.Modified = Calendar.Now;
    }
  }

}