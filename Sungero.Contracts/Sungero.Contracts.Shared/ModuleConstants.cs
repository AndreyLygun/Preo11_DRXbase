using System;

namespace Sungero.Contracts.Constants
{
  public static class Module
  {
    // Отправка уведомлений о завершении договора.
    public const string NotificationDatabaseKey = "LastNotificationOfExpiringContracts";
    public const string ExpiringContractTableName = "Sungero_Contrac_ExpiringContracts";
    
    public const string HaveRelationKey = "HaveRelation";
    
    public static class Initialize
    {
      #region Виды документов
      
      [Sungero.Core.Public]
      public static readonly Guid ContractKind = Guid.Parse("2A42D335-4A84-4019-AB54-D0AB8344D232");
      [Sungero.Core.Public]
      public static readonly Guid SupAgreementKind = Guid.Parse("A5B2C424-0F31-4809-B160-ACC0C4583574");
      [Sungero.Core.Public]
      public static readonly Guid IncomingInvoiceKind = Guid.Parse("558BAAB7-784D-42F8-BCB7-BA8C0E8821A3");
      [Sungero.Core.Public]
      public static readonly Guid OutgoingInvoiceKind = Guid.Parse("0BE84C5B-7747-4A1F-940B-2F7C9CCE6C55");
      [Sungero.Core.Public]
      public static readonly Guid SupAgreementRegister = Guid.Parse("8A583ACF-FAE5-4D92-A54B-4DA73A81E46C");
      [Sungero.Core.Public]
      public static readonly Guid OutgoingInvoiceRegister = Guid.Parse("C89D7196-FCBB-4D11-BEC8-5696C824DC5A");
      
      #endregion
    }

    #region Связи
    
    /// <summary>
    /// Имя типа связи "Доп. соглашение".
    /// </summary>
    [Sungero.Core.PublicAttribute]
    public const string SupAgreementRelationName = "SupAgreement";
    
    /// <summary>
    /// Имя типа связи "Финансовые документы".
    /// </summary>
    [Sungero.Core.PublicAttribute]
    public const string AccountingDocumentsRelationName = "FinancialDocuments";
    
    /// <summary>
    /// Имя типа связи "Переписка".
    /// </summary>
    [Sungero.Core.PublicAttribute]
    public const string CorrespondenceRelationName = "Correspondence";
    
    #endregion
    
    public static readonly Guid ContractsUIGuid = Guid.Parse("3c8b7d3a-187d-4445-8a8c-1d00ece44556");
    
    [Sungero.Core.Public]
    public const string ContractGuid = "f37c7e63-b134-4446-9b5b-f8811f6c9666";
    
    [Sungero.Core.Public]
    public const string SupAgreementGuid = "265f2c57-6a8a-4a15-833b-ca00e8047fa5";
    
    // GUID роли "Пользователи с доступом к договорам".
    [Sungero.Core.Public]
    public static readonly Guid UsersWithAccessToContractsRole = Guid.Parse("5EC6980D-E9D8-4895-96DF-1603B7451F8F");
    
    /// <summary>
    /// Константы инициализации модуля.
    /// </summary>
    public static class Init
    {
      public static class Contracts
      {
        /// <summary>
        /// Название параметра для хранения проинициализированной версии кода модуля.
        /// </summary>
        public const string Name = "InitContractsUpdate";
        
        public const string FirstInitVersion = "4.8.0.0";
        public const string Version49 = "4.9.0.0";
        public const string Version410 = "4.10.0.0";
        public const string Version411 = "4.11.0.0";
      }
    }
  }
}