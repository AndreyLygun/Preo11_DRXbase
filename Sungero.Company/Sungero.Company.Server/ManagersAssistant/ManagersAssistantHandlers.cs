using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.ManagersAssistant;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class ManagersAssistantServerHandlers
  {

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      base.BeforeDelete(e);
      Functions.ManagersAssistant.RemoveAssistantsFromRoleUsersWithAssignmentCompletionRights(_obj, null);
    }
    
    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      if (!_obj.State.IsCopied)
      {
        _obj.PreparesAssignmentCompletion = false;
        _obj.IsAssistant = true;
        _obj.SendActionItems = true;
      }
    }
    
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      Functions.ManagersAssistant.UpdateRoleUsersWithAssignmentCompletionRights(_obj);
      
      if (_obj.Assistant == null)
        return;
      
      Functions.ManagersAssistant.ValidateManagersAssistants(_obj, e);
    }
  }
}