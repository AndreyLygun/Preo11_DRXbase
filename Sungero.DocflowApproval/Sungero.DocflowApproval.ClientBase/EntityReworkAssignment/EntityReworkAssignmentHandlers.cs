using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.EntityReworkAssignment;

namespace Sungero.DocflowApproval
{
  
  partial class EntityReworkAssignmentApproversClientHandlers
  {

    public virtual IEnumerable<Enumeration> ApproversActionFiltering(IEnumerable<Enumeration> query)
    {
      if (_obj.AssignmentResult == EntityApprovalAssignments.Info.Properties.Result.GetLocalizedValue(DocflowApproval.EntityApprovalAssignment.Result.ForRework))
        return query.Where(q => q.Equals(EntityReworkAssignmentApprovers.Action.SendForApproval));
      return query;
    }

    public virtual void ApproversApproverValueInput(Sungero.DocflowApproval.Client.EntityReworkAssignmentApproversApproverValueInputEventArgs e)
    {
      _obj.Action = EntityReworkAssignmentApprovers.Action.SendForApproval;
      _obj.BlockId = string.Empty;
      _obj.AssignmentResult = null;
      _obj.BlockName = null;
    }
  }

  partial class EntityReworkAssignmentClientHandlers
  {

    public virtual void NewDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      if (!Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, Calendar.Now))
        e.AddError(EntityReworkAssignments.Resources.ImpossibleSpecifyDeadlineLessThanToday);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      Functions.EntityReworkAssignment.Remote.FillEntityParams(_obj);
      
      if (_obj.Status == Status.InProcess && Functions.EntityReworkAssignment.IsUserWithoutRightsOnMainDocument(_obj))
      {
        e.HideAction(_obj.Info.Actions.ForReapproval);
        e.HideAction(_obj.Info.Actions.Forward);
        e.AddError(Docflow.Resources.NoRightsToDocument);
      }
      
      Functions.EntityReworkAssignment.UpdateDeliveryMethodState(_obj);
    }
    
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.EntityReworkAssignment.FillEntityParamsIfEmpty(_obj);
      
      if (_obj.Status == Status.InProcess && Functions.EntityReworkAssignment.IsUserWithoutRightsOnMainDocument(_obj))
        e.AddError(Docflow.Resources.NoRightsToDocument);
      
      Functions.EntityReworkAssignment.UpdateAddresseesFieldState(_obj);
      
      var deliveryMethodIsVisible = Functions.EntityReworkAssignment.CanSpecifyDeliveryMethod(_obj)
        && Functions.EntityReworkAssignment.IsSendingToCounterpartyEnabledInScheme(_obj);
      _obj.State.Properties.DeliveryMethod.IsVisible = deliveryMethodIsVisible;
      _obj.State.Properties.ExchangeService.IsVisible = deliveryMethodIsVisible;
      _obj.State.Properties.ExchangeService.IsRequired = deliveryMethodIsVisible && _obj.State.Properties.ExchangeService.IsEnabled;
      
      var approversIsEnabled = Functions.EntityReworkAssignment.IsChangeApproversEnabled(_obj);
      _obj.State.Properties.Approvers.IsEnabled = approversIsEnabled;
      _obj.State.Properties.ApproversDisabledReasonDescription.IsVisible = !approversIsEnabled && !string.IsNullOrWhiteSpace(_obj.ApproversDisabledReasonDescription);
      
      _obj.State.Properties.NewDeadline.IsVisible = Functions.EntityReworkAssignment.IsChangeApprovalDeadlineEnabled(_obj);
    }
    
    public virtual void DeliveryMethodValueInput(Sungero.DocflowApproval.Client.EntityReworkAssignmentDeliveryMethodValueInputEventArgs e)
    {
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (e.NewValue != null && e.NewValue.Sid == Constants.Module.ExchangeDeliveryMethodSid)
      {
        var services = Functions.Module.Remote.GetExchangeServices(officialDocument).Services;
        if (!services.Any())
          e.AddError(ApprovalTasks.Resources.DeliveryByExchangeNotAllowed, e.Property);
        
        return;
      }
      
      Functions.Module.ShowExchangeHint(_obj.State.Properties.DeliveryMethod, e.Property, e.NewValue, e, officialDocument);
    }
  }
}