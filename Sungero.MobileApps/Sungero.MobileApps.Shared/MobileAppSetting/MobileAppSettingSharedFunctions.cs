using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.MobileApps.MobileAppSetting;

namespace Sungero.MobileApps.Shared
{
  partial class MobileAppSettingFunctions
  {
    /// <summary>
    /// Установить обязательность свойств в зависимости от заполненных данных.
    /// </summary>
    public virtual void SetRequiredProperties()
    {
      _obj.State.Properties.VisibleFolders.IsRequired = _obj.IsVisibleFoldersLimited ?? false;
    }

    /// <summary>
    /// Устанавливает всем настроенным папкам значение признака онлайн-папка.
    /// </summary>
    public virtual void SetIsFolderOnline()
    {
      foreach (var folder in _obj.VisibleFolders)
        folder.IsOnline = _obj.IsAllFoldersOnline;
    }
  }
}