using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ReceiptNotificationSendingAssignment;

namespace Sungero.Exchange.Client
{
  partial class ReceiptNotificationSendingAssignmentFunctions
  {
    /// <summary>
    /// Отправить извещения о получении.
    /// </summary>
    /// <param name="e">Аргументы.</param>
    public void SendReceiptNotification(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var task = ReceiptNotificationSendingTasks.As(_obj.Task);
      var certificate = Functions.Module.GetCurrentUserExchangeCertificate(_obj.Box, Company.Employees.Current);
      if (certificate == null)
      {
        e.AddError(Resources.CertificateNotFound);
        e.Cancel();
      }
      
      var result = Exchange.Functions.Module.SendDeliveryConfirmation(_obj.Box, certificate, true);
      if (!string.IsNullOrEmpty(result))
      {
        e.AddError(result);
        e.Cancel();
      }
    }
    
    /// <summary>
    /// Показать диалог переадресации.
    /// </summary>
    /// <returns>True, если запрос был подтвержден.</returns>
    public virtual bool ShowForwardingDialog()
    {
      var excludedPerformers = new List<IRecipient>();
      excludedPerformers.Add(_obj.Performer);
     
      var dialogResult = Docflow.PublicFunctions.Module.ShowForwardDialog(excludedPerformers, _obj.Deadline, TimeSpan.FromDays(2));
      if (dialogResult.ForwardButtonIsPressed)
      {
        _obj.Addressee = dialogResult.ForwardTo;
        _obj.NewDeadline = dialogResult.Deadline;
        
        return true;
      }
      
      return false;
    }
  }
}