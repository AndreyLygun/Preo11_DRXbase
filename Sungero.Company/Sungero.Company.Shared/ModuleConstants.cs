using System;

namespace Sungero.Company.Constants
{
  public static class Module
  {

    /// <summary>
    /// Единый постфикс логгера для передачи прав.
    /// </summary>
    public const string SubstitutionsAccessRightsTransferLoggerPostfix = "SubstitutionsAccessRightsTransfer";
    
    // Имена хранимых процедур для ограничения видимости оргструктуры.
    public const string GetHeadRecipientsByEmployeeProcedureName = "Sungero_Company_GetHeadRecipientsByEmployee";
    public const string GetAllVisibleRecipientsProcedureName = "Sungero_Company_GetAllVisibleRecipients";
    
    // Типы реципиентов.
    public const string BusinessUnitTypeGuid = "eff95720-181f-4f7d-892d-dec034c7b2ab";
    public const string DepartmentTypeGuid = "61b1c19f-26e2-49a5-b3d3-0d3618151e12";
    public const string EmployeeTypeGuid = "b7905516-2be5-4931-961c-cb38d5677565";
    
    // Индексы для ограничения видимости.
    public const string RecipientTableName = "Sungero_Core_Recipient";
    
    // Количество обрабатываемых элементов очереди фоновым процессом "Компания. Удаление устаревших системных замещений".
    public const int DeleteSystemSubstitutionBatchSize = 100;
    
    // Передача прав.
    [Sungero.Core.Public]
    public const string TransferAccessRights = "TransferAR";
    
    // GUID роли "Руководители наших организаций".
    [Sungero.Core.Public]
    public static readonly Guid BusinessUnitHeadsRole = Guid.Parse("03C7A126-83DE-4F8F-908B-3ACB868E30C5");
    
    // GUID роли "Руководители подразделений".
    [Sungero.Core.Public]
    public static readonly Guid DepartmentManagersRole = Guid.Parse("EA04AA41-9BD8-45D5-A479-A986137A509C");
    
    /// <summary>
    /// GUID для роли "Пользователи с доступом к орг. структуре".
    /// </summary>
    [Sungero.Core.Public]
    public static readonly Guid UsersWithAccessToCompanyRole = Guid.Parse("FFCE34EF-0CE3-4269-936B-DA7906FD0E72");
    
    // GUID модуля.
    [Sungero.Core.Public]
    public static readonly Guid ModuleGuid = Guid.Parse("d534e107-a54d-48ec-85ff-bc44d731a82f");
    
    /// <summary>
    /// Константы инициализации модуля.
    /// </summary>
    public static class Init
    {
      public static class Company
      {
        /// <summary>
        /// Название параметра для хранения проинициализированной версии кода модуля.
        /// </summary>
        public const string Name = "InitCompanyUpdate";
        
        public const string FirstInitVersion = "4.8.0.0";
        public const string Version49 = "4.9.0.0";
      }
    }
  }
}