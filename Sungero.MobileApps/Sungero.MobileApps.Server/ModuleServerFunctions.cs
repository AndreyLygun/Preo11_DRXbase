using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.MobileApps.Server
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Создать настройку мобильных приложени.
    /// </summary>
    /// <returns>Новая настройка мобильных приложений.</returns>
    [Remote]
    public static IMobileAppSetting CreateMobileAppSetting()
    {
      return MobileAppSettings.Create();
    }
  }
}