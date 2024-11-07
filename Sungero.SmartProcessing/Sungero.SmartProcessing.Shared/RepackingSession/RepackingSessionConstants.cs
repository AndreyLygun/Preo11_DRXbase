using System;
using Sungero.Core;

namespace Sungero.SmartProcessing.Constants
{
  public static class RepackingSession
  {
    /// <summary>
    /// Формат URL сессии перекомплектования.
    /// </summary>
    public const string UrlTemplate = "{0}/?id={1}";
    
    /// <summary>
    /// Формат URL клиента перекомплектования.
    /// </summary>
    public const string ClientUrlTemplate = "{0}://{1}:{2}/{3}/repacking";
    
    /// <summary>
    /// Имя параметра относительного адреса.
    /// </summary>
    public const string WebHostPathBaseParamName = "WEB_HOST_PATH_BASE";
  }
}