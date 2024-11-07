using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.EntityApprovalAssignment;

namespace Sungero.DocflowApproval
{
  partial class EntityApprovalAssignmentClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      Functions.EntityApprovalAssignment.Remote.FillEntityParams(_obj);
      
      if (_obj.Status == Status.InProcess && Functions.EntityApprovalAssignment.IsUserWithoutRightsOnMainDocument(_obj))
      {
        e.HideAction(_obj.Info.Actions.Approved);
        e.HideAction(_obj.Info.Actions.WithSuggestions);
        e.HideAction(_obj.Info.Actions.Forward);
        e.HideAction(_obj.Info.Actions.AddApprover);
        e.HideAction(_obj.Info.Actions.ApprovalForm);
        e.AddError(Docflow.Resources.NoRightsToDocument);
      }
      
      if (!Functions.EntityApprovalAssignment.CanApproveWithSuggestions(_obj))
        e.HideAction(_obj.Info.Actions.WithSuggestions);
      
      var addingApproversIsAllowed = Functions.EntityApprovalAssignment.CanAddApprovers(_obj);
      if (!addingApproversIsAllowed)
        e.HideAction(_obj.Info.Actions.AddApprover);
    }
    
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.EntityApprovalAssignment.FillEntityParamsIfEmpty(_obj);
      
      if (_obj.Status == Status.InProcess && Functions.EntityApprovalAssignment.IsUserWithoutRightsOnMainDocument(_obj))
        e.AddError(Docflow.Resources.NoRightsToDocument);
      
      Functions.EntityApprovalAssignment.UpdateAddresseesFieldState(_obj);
      
      var allowedToChangeProperties = Functions.EntityApprovalAssignment.CanChangeProperties(_obj);
      var allowedToSendToCounterparty = Functions.EntityApprovalAssignment.IsSendingToCounterpartyEnabledInScheme(_obj);
      _obj.State.Properties.AddApprovers.IsVisible = allowedToChangeProperties;
      _obj.State.Properties.DeliveryMethod.IsVisible = allowedToChangeProperties && allowedToSendToCounterparty;
      _obj.State.Properties.ExchangeService.IsVisible = allowedToChangeProperties && allowedToSendToCounterparty;
      if (allowedToChangeProperties && allowedToSendToCounterparty)
        Functions.EntityApprovalAssignment.UpdateDeliveryMethodState(_obj);
      
      _obj.State.Properties.ReworkPerformer.IsVisible = Functions.EntityApprovalAssignment.CanChangeReworkPerformer(_obj);

      // Скрывать контрол состояния со сводкой, если сводка пустая.
      var needShowDocumentSummary = Functions.EntityApprovalAssignment.NeedViewDocumentSummary(_obj) &&
        (!Functions.EntityApprovalAssignment.NeedHideDocumentSummary(_obj));
      _obj.State.Controls.DocumentSummary.IsVisible = needShowDocumentSummary;
    }
    
    public virtual void DeliveryMethodValueInput(Sungero.DocflowApproval.Client.EntityApprovalAssignmentDeliveryMethodValueInputEventArgs e)
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