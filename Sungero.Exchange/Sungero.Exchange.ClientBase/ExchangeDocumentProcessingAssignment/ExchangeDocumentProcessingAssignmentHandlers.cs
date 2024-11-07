using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ExchangeDocumentProcessingAssignment;

namespace Sungero.Exchange
{
  partial class ExchangeDocumentProcessingAssignmentClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      var hasNoCurrentUserExchangeServiceCertificate = !Functions.Module.HasCurrentUserExchangeServiceCertificate(_obj.BusinessUnitBox);
      e.Params.AddOrUpdate(Constants.ExchangeDocumentProcessingAssignment.HasNoCurrentUserExchangeServiceCertificate, hasNoCurrentUserExchangeServiceCertificate);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var hasNoCurrentUserExchangeServiceCertificate = false;
      if (e.Params.Contains(Constants.ExchangeDocumentProcessingAssignment.HasNoCurrentUserExchangeServiceCertificate))
        e.Params.TryGetValue(Constants.ExchangeDocumentProcessingAssignment.HasNoCurrentUserExchangeServiceCertificate, out hasNoCurrentUserExchangeServiceCertificate);
      
      // Проверить, что у пользователя есть сертификат сервиса обмена, если задачу еще не переадресовывали.
      if (e.IsValid && ExchangeDocumentProcessingTasks.As(_obj.Task).Addressee == null && hasNoCurrentUserExchangeServiceCertificate)
        e.AddWarning(ExchangeDocumentProcessingAssignments.Resources.CertificateNotFound);
    }

  }
}