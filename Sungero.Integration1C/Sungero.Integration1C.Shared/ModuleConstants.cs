using System;
using Sungero.Core;

namespace Sungero.Integration1C.Constants 
{
  public static class Module
  {
    // Код системы у external link для внутренних ссылок.
    public const string InternalLinkSystemId = "InternalLink";
    
    /// <summary>
    /// Имя параметра последнего уведомления администратора о результатах синхронизации с 1С.
    /// </summary>
    public const string LastNotifyOfSyncDateParamName = "LastNotificationOfSynchronization1C";
    
    /// <summary>
    /// GUID для роли "Ответственные за синхронизацию с учетными системами".
    /// </summary>
    public static readonly Guid SynchronizationResponsibleRoleGuid = Guid.Parse("6F98BA36-3B7F-4767-8369-88A65578DC5A");
    
    /// <summary>
    /// Guid модуля Integration1C.
    /// </summary>
    [Sungero.Core.Public]
    public static readonly Guid Integration1CGuid = Guid.Parse("f7b1d5b7-5af1-4a9f-b4d7-4e18840d7195");
    
    /// <summary>
    /// Константы инициализации модуля.
    /// </summary>
    public static class Init
    {
      public static class Integration1C
      {
        /// <summary>
        /// Название параметра для хранения проинициализированной версии кода модуля.
        /// </summary>
        public const string Name = "InitIntegration1CUpdate";
        
        public const string FirstInitVersion = "4.8.0.0";
        public const string Version49 = "4.9.0.0";
      }
    }
  }
}