using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.ManagersAssistantBase;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class ManagersAssistantBaseServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.Status == CoreEntities.DatabookEntry.Status.Closed)
        return;
      
      if (_obj.Manager == null)
        return;
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (!_obj.State.IsCopied)
      {
        _obj.PreparesActionItemDrafts = false;
        _obj.PreparesResolution = false;
      }
      
    }
  }

}