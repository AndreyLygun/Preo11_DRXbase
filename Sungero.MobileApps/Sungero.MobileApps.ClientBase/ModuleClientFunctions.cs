using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.MobileApps.Client
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Создать и показать карточку настройки мобильных приложений.
    /// </summary>
    [LocalizeFunction("CreateMobileAppSettingName", "")]
    public virtual void CreateMobileAppSetting()
    {
      Functions.Module.Remote.CreateMobileAppSetting().Show();
    }
  }
}