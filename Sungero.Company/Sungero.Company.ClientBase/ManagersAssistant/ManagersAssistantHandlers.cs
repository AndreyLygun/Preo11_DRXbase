using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.ManagersAssistant;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class ManagersAssistantClientHandlers
  {

    public override void PreparesResolutionValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      base.PreparesResolutionValueInput(e);
      _obj.State.Properties.IsAssistant.IsEnabled = e.NewValue != true;
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      _obj.State.Properties.IsAssistant.IsEnabled = _obj.PreparesResolution != true;
      _obj.State.Properties.SendActionItems.IsEnabled = _obj.IsAssistant != true;
    }

  }
}