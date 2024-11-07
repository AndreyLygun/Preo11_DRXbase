using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineExtensionAssignment;

namespace Sungero.Docflow.Client
{
  partial class DeadlineExtensionAssignmentFunctions
  {
    /// <summary>
    /// Показать диалог переадресации.
    /// </summary>
    /// <returns>True, если запрос был подтвержден.</returns>
    public virtual bool ShowForwardingDialog()
    {
      var dialogResult = PublicFunctions.Module.ShowForwardDialog(new List<IRecipient>() { _obj.Performer });
      
      if (dialogResult.ForwardButtonIsPressed)
      {
        _obj.ForwardTo = dialogResult.ForwardTo;
        return true;
      }
      
      return false;
    }
  }
}