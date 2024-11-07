using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.MobileApps.Structures.MobileDevice
{
  /// <summary>
  /// Информация для обработки запроса клиентских логов.
  /// </summary>
  [Public]
  partial class MobileDeviceRequestLogsEventArgs
  {
    public long UserId { get; set; }
    
    public string DeviceId { get; set; }
  }

  /// <summary>
  /// Информация для обработки запроса удаления данных с устройства.
  /// </summary>
  [Public]
  partial class MobileDeviceDeleteDataEventArgs
  {
    public long UserId { get; set; }
    
    public string DeviceId { get; set; }
  }
   
  /// <summary>
  /// Информация для обработки запроса сброса сеанса.
  /// </summary>
  [Public]
  partial class MobileDeviceResetSeanceEventArgs
  {
    public long UserId { get; set; }
    
    public string DeviceId { get; set; }
  }
}