using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.DocumentProcessingAssignment;

namespace Sungero.DocflowApproval
{
  partial class DocumentProcessingAssignmentSharedHandlers
  {

    public virtual void ExchangeServiceChanged(Sungero.DocflowApproval.Shared.DocumentProcessingAssignmentExchangeServiceChangedEventArgs e)
    {
      var document = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      var isManyAddressees = OutgoingDocumentBases.Is(document) ? OutgoingDocumentBases.As(document).IsManyAddressees.Value : false;
      _obj.DeliveryMethodDescription = Docflow.PublicFunctions.ApprovalTask.GetDeliveryMethodDescription(_obj.DeliveryMethod, e.NewValue, isManyAddressees);
    }

    public virtual void DeliveryMethodChanged(Sungero.DocflowApproval.Shared.DocumentProcessingAssignmentDeliveryMethodChangedEventArgs e)
    {
      var document = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      var isManyAddressees = OutgoingDocumentBases.Is(document) ? OutgoingDocumentBases.As(document).IsManyAddressees.Value : false;
      _obj.DeliveryMethodDescription = Docflow.PublicFunctions.ApprovalTask.GetDeliveryMethodDescription(e.NewValue, _obj.ExchangeService, isManyAddressees);
    }

  }
}