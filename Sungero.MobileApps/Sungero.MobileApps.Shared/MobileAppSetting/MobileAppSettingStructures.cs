using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.MobileApps.Structures.MobileAppSetting
{
  /// <summary>
  /// Информация для обработки запроса обновления настройки мобильного приложения.
  /// </summary>
  [Public]
  partial class MobileAppSettingChangedEventArgs
  {
    public long UserId { get; set; }
  }

  /// <summary>
  /// Информация о папке.
  /// </summary>
  partial class FolderInfo
  {
    public long Id { get; set; }

    public string FolderName { get; set; }

    public bool IsOnline { get; set; }
  }
}