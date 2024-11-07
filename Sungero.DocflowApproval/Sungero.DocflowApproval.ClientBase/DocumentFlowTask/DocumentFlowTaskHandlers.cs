using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.DocumentFlowTask;

namespace Sungero.DocflowApproval
{
  partial class DocumentFlowTaskClientHandlers
  {
    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      Functions.DocumentFlowTask.Remote.FillEntityParams(_obj);
      
      if (_obj.Status == Status.InProcess && !Functions.DocumentFlowTask.HasDocumentAndCanRead(_obj))
      {
        e.AddError(Docflow.Resources.NoRightsToDocument);
        e.Params.AddOrUpdate(Constants.Module.NeedShowNoRightsHintParamName, true);
      }
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.DocumentFlowTask.FillEntityParamsIfEmpty(_obj);
      Functions.DocumentFlowTask.UpdateFieldsAvailability(_obj);
      
      _obj.State.Properties.ProcessKind.IsRequired = _obj.Status == Status.Draft;
      
      var needShowHint = false;
      if (e.Params.TryGetValue(Constants.Module.NeedShowNoRightsHintParamName, out needShowHint) && needShowHint)
        e.AddError(Docflow.Resources.NoRightsToDocument);
      
      Functions.DocumentFlowTask.UpdateAddresseesFieldState(_obj);
    }

    public virtual void DeliveryMethodValueInput(Sungero.DocflowApproval.Client.DocumentFlowTaskDeliveryMethodValueInputEventArgs e)
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