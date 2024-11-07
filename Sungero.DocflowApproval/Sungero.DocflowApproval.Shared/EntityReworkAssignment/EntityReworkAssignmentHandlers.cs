using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.EntityReworkAssignment;

namespace Sungero.DocflowApproval
{
  partial class EntityReworkAssignmentApproversSharedCollectionHandlers
  {

    public virtual void ApproversAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      if (_added.State.IsCopied)
      {
        _added.Approver = null;
        _added.AssignmentResult = null;
        _added.BlockId = string.Empty;
        _added.BlockName = string.Empty;
      }
      else
      {
        _added.Action = EntityReworkAssignmentApprovers.Action.SendForApproval;
      }
    }
  }

  partial class EntityReworkAssignmentSharedHandlers
  {

    public virtual void DeliveryMethodChanged(Sungero.DocflowApproval.Shared.EntityReworkAssignmentDeliveryMethodChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      if (e.NewValue == null || e.NewValue.Sid != Constants.Module.ExchangeDeliveryMethodSid)
      {
        _obj.ExchangeService = null;
        _obj.State.Properties.ExchangeService.IsEnabled = false;
      }
      else
      {
        _obj.State.Properties.ExchangeService.IsEnabled = true;
        var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
        _obj.ExchangeService = Functions.Module.Remote.GetExchangeServices(officialDocument).DefaultService;
      }
      
      Functions.EntityReworkAssignment.UpdateDeliveryMethodState(_obj);
    }

  }
}