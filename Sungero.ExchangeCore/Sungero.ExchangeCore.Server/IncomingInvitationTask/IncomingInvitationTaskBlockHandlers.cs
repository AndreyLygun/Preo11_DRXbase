using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.IncomingInvitationTask;
using Sungero.Workflow;

namespace Sungero.ExchangeCore.Server.IncomingInvitationTaskBlocks
{
  partial class IncomingInvitationAssignmentBlockHandlers
  {

    public virtual void IncomingInvitationAssignmentBlockStartAssignment(Sungero.ExchangeCore.IIncomingInvitationAssignment assignment)
    {
      assignment.Box = _obj.Box;
      assignment.Counterparty = _obj.Counterparty;
    }
  }

}