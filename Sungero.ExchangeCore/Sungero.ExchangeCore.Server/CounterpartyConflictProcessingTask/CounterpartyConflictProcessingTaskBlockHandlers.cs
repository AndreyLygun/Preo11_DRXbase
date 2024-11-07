using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.CounterpartyConflictProcessingTask;
using Sungero.Workflow;

namespace Sungero.ExchangeCore.Server.CounterpartyConflictProcessingTaskBlocks
{
  partial class CounterpartyConflictProcessingAssignmentBlockHandlers
  {

    public virtual void CounterpartyConflictProcessingAssignmentBlockCompleteAssignment(Sungero.ExchangeCore.ICounterpartyConflictProcessingAssignment assignment)
    {
      if (assignment.ForwardTo != null && assignment.Result == Sungero.ExchangeCore.CounterpartyConflictProcessingAssignment.Result.Forward)
      {
        _obj.MaxDeadline = assignment.NewDeadline;
        assignment.Forward(assignment.ForwardTo, ForwardingLocation.Next);
      }
    }
  }
  
}