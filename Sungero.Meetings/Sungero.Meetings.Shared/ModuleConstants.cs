using System;
using Sungero.Core;

namespace Sungero.Meetings.Constants
{
  public static class Module
  {
    // GUID роли "Ответственные за совещания".
    [Sungero.Core.Public]
    public static readonly Guid MeetingResponsibleRole = Guid.Parse("83D3331C-82E9-44CC-8AF1-46A234DE467D");
    
    // GUID роли "Пользователи с доступом к совещаниям".
    [Sungero.Core.Public]
    public static readonly Guid UsersWithAccessToMeetingRole = Guid.Parse("C3C04556-624F-4D88-A924-759355105889");
    
    [Sungero.Core.Public]
    public static readonly Guid AgendaKind = Guid.Parse("68B6FB25-7F78-4AB9-AE0C-D8947B99FA24");
    [Sungero.Core.Public]
    public static readonly Guid MinutesKind = Guid.Parse("75D45529-60AE-4D95-9C8F-B1016B766253");
    public static readonly Guid MinutesRegister = Guid.Parse("88DD573B-522C-415B-8965-215845305ACF");
    
    [Public]
    public static readonly Guid MeetingsUIGuid = Guid.Parse("6ea9a047-b597-42eb-8f90-da8c559dd057");
    
    /// <summary>
    /// Константы инициализации модуля.
    /// </summary>
    public static class Init
    {
      public static class Meetings
      {
        /// <summary>
        /// Название параметра для хранения проинициализированной версии кода модуля.
        /// </summary>
        public const string Name = "InitMeetingsUpdate";
        
        public const string FirstInitVersion = "4.8.0.0";
        public const string Version49 = "4.9.0.0";
      }
    }
  }
}