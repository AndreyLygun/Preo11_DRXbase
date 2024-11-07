using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ReceiptNotificationSendingTask;
using Sungero.Workflow;

namespace Sungero.Exchange.Server.ReceiptNotificationSendingTaskBlocks
{
  partial class ReceiptNotificationSendingAssignmentBlockHandlers
  {
    public virtual void ReceiptNotificationSendingAssignmentBlockCompleteAssignment(Sungero.Exchange.IReceiptNotificationSendingAssignment assignment)
    {
      if (assignment.Result == Sungero.Exchange.ReceiptNotificationSendingAssignment.Result.Forwarded)
      {
        _obj.Addressee = assignment.Addressee;
        _obj.MaxDeadline = assignment.NewDeadline;
        assignment.Forward(assignment.Addressee, ForwardingLocation.Next);
      }
    }

    public virtual void ReceiptNotificationSendingAssignmentBlockStartAssignment(Sungero.Exchange.IReceiptNotificationSendingAssignment assignment)
    {
      // Переадресованное задание должно приходить от последнего исполнителя.
      var lastProcessingAssignment = ReceiptNotificationSendingAssignments.GetAll()
        .Where(a => Equals(a.Task, assignment.Task) && a.Id != assignment.Id)
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
      if (lastProcessingAssignment != null)
        assignment.Author = lastProcessingAssignment.Performer;
      
      assignment.Box = _obj.Box;
      assignment.Deadline = _obj.MaxDeadline;
    }
  }
}