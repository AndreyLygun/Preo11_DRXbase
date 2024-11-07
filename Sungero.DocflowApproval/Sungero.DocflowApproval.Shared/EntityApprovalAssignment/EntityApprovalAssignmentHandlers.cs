using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.EntityApprovalAssignment;

namespace Sungero.DocflowApproval
{
  partial class EntityApprovalAssignmentSharedHandlers
  {
    
    public virtual void DeliveryMethodChanged(Sungero.DocflowApproval.Shared.EntityApprovalAssignmentDeliveryMethodChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      if (e.NewValue == null || e.NewValue.Sid != Constants.Module.ExchangeDeliveryMethodSid)
      {
        _obj.ExchangeService = null;
        _obj.State.Properties.ExchangeService.IsEnabled = false;
        _obj.State.Properties.ExchangeService.IsRequired = false;
      }
      else
      {
        _obj.State.Properties.ExchangeService.IsEnabled = true;
        _obj.State.Properties.ExchangeService.IsRequired = true;
        var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
        _obj.ExchangeService = Functions.Module.Remote.GetExchangeServices(officialDocument).DefaultService;
      }
      
      Functions.EntityApprovalAssignment.UpdateDeliveryMethodState(_obj);
    }

  }
}