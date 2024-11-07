using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.CounterpartyConflictProcessingAssignment;

namespace Sungero.ExchangeCore.Client
{
  partial class CounterpartyConflictProcessingAssignmentFunctions
  {
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
        _obj.ForwardTo = dialogResult.ForwardTo;
        _obj.NewDeadline = dialogResult.Deadline;
        
        return true;
      }
      
      return false;
    }
  }
}