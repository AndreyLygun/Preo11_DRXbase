using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalReworkAssignment;

namespace Sungero.Docflow.Client
{
  partial class ApprovalReworkAssignmentFunctions
  {
    /// <summary>
    /// Показать диалог переадресации.
    /// </summary>
    /// <returns>True, если запрос был подтвержден.</returns>
    public virtual bool ShowForwardingDialog()
    {
      var dialogResult = Docflow.PublicFunctions.Module.ShowForwardDialog(new List<IRecipient>() { _obj.Performer });
      if (dialogResult.ForwardButtonIsPressed)
      {
        _obj.ForwardPerformer = dialogResult.ForwardTo;
        return true;
      }
      return false;
    }
    
  }
}