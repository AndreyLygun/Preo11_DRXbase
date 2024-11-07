using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.CounterpartyConflictProcessingAssignment;

namespace Sungero.ExchangeCore
{
  partial class CounterpartyConflictProcessingAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (_obj.Result.Value == Result.Forward && !string.IsNullOrEmpty(_obj.ActiveText))
      {
        e.Result = CounterpartyConflictProcessingAssignments.Resources.ForwardedFormat(Company.PublicFunctions.Employee.GetShortName(_obj.ForwardTo, DeclensionCase.Dative, true));
      }
    }
  }
}