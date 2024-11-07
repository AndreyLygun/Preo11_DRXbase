using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.CounterpartyConflictProcessingAssignment;

namespace Sungero.ExchangeCore.Client
{
  partial class CounterpartyConflictProcessingAssignmentActions
  {
    public virtual void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Functions.CounterpartyConflictProcessingAssignment.ShowForwardingDialog(_obj))
        e.Cancel();
    }

    public virtual bool CanForward(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return !Docflow.PublicFunctions.Module.IsTaskUsingOldScheme(_obj.Task) && !Docflow.PublicFunctions.Module.IsCompetitive(_obj);
    }

    public virtual void Complete(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      
    }

    public virtual bool CanComplete(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }

}