using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ReceiptNotificationSendingAssignment;

namespace Sungero.Exchange.Client
{
  partial class ReceiptNotificationSendingAssignmentActions
  {
    public virtual void Forwarded(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.ReceiptNotificationSendingAssignment.ShowForwardingDialog(_obj))
        e.Cancel();
    }

    public virtual bool CanForwarded(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return !Docflow.PublicFunctions.Module.IsCompetitive(_obj);
    }

    public virtual void ShowDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var documents = Functions.Module.Remote.GetDocumentsWithoutReceiptNotification(_obj.Box);
      documents.Show();
    }

    public virtual bool CanShowDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void Complete(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.Module.HasCurrentUserExchangeServiceCertificate(_obj.Box))
      {
        e.AddError(Resources.CertificateNotFound);
        return;
      }
      
      // Замена стандартного диалога подтверждения выполнения действия.
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                 Constants.ReceiptNotificationSendingTask.ReceiptNotificationSendingAssignmentConfirmDialogID.Complete))
        e.Cancel();
      
      Functions.ReceiptNotificationSendingAssignment.SendReceiptNotification(_obj, e);
    }

    public virtual bool CanComplete(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }

}