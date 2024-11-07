using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.MobileApps.MobileAppSetting;

namespace Sungero.MobileApps
{
  partial class MobileAppSettingClientHandlers
  {
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      _obj.State.Properties.Employee.IsEnabled = _obj.Employee == null || _obj.State.IsInserted;
      Functions.MobileAppSetting.SetRequiredProperties(_obj);
    }
  }
}