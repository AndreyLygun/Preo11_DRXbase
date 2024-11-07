using System;
using Sungero.Core;

namespace Sungero.MobileApps.Constants
{
  public static class Module
  {
    /// <summary>
    /// Очередь сообщений для сервера мобильных приложений. 
    /// </summary>
    [Public]
    public const string MobileAppQueueName = "mobile_app_events";

    #region Инициализация

    public const string SungeroMobAppsMobileDeviceTableName = "Sungero_MobApps_MobileDevice";

    public const string SungeroMobileDeviceIndex0 = "idx_Devices_Employee_DeviceId";

    #endregion
    
    /// <summary>
    /// Константы инициализации модуля.
    /// </summary>
    public static class Init
    {
      public static class MobileApps
      {
        /// <summary>
        /// Название параметра для хранения проинициализированной версии кода модуля.
        /// </summary>
        public const string Name = "InitMobileAppsUpdate";
        
        public const string FirstInitVersion = "4.8.0.0";
        public const string Version49 = "4.9.0.0";
      }
    }
  }
}