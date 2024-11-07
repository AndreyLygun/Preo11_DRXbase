using System;
using Sungero.Core;

namespace Sungero.MobileApps.Constants
{
  public static class MobileDevice
  {
    /// <summary>
    /// Таймаут запросов в секундах.
    /// </summary>
    public const int RequestTimeout = 60;
    
    /// <summary>
    /// Запрос клиентских логов.
    /// </summary>
    public const string RequestLogs = "RequestLogs";
    
    /// <summary>
    /// Запрос удаления данных с устройства.
    /// </summary>
    public const string DeleteData = "DeleteData";
    
    /// <summary>
    /// Запрос сброса сеанса.
    /// </summary>
    public const string ResetSeance = "ResetSeance";
  }
}