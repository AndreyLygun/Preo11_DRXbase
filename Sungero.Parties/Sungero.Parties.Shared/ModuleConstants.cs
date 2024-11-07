using System;

namespace Sungero.Parties.Constants
{
  public static class Module
  {
    public static class Init
    {
      public static class Banks
      {
        public const string Version49 = "4.9.0.0";
        public const string Version410 = "4.10.0.0";
        public const string Name = "InitBanksUpdate";
      }
      
      public static class Parties
      {
        /// <summary>
        /// Название параметра для хранения проинициализированной версии кода модуля.
        /// </summary>
        public const string Name = "InitPartiesUpdate";
        
        public const string FirstInitVersion = "4.8.0.0";
        public const string Version49 = "4.9.0.0";
        public const string Version410 = "4.10.0.0";
      }
    }
    
    public static class HelpCodes
    {
      public const string CounterpartyInvitation = "Sungero_CounterpartyInvitation";
    }
    
    public static class RolesGuid
    {
      /// <summary>
      /// GUID для роли "Пользователи с правами на работу через сервис обмена".
      /// </summary>
      public static readonly Guid ExchangeServiceUsers = new Guid("5AFA06FB-3B66-4216-8681-56ACDEAC7FC1");
      
      /// <summary>
      /// GUID для роли "Пользователи с доступом к контрагентам".
      /// </summary>
      [Sungero.Core.Public]
      public static readonly Guid UsersWithAccessToCounterpartyRole = Guid.Parse("B8A078B9-4514-43D6-88B3-BB7EF112E31D");
    }
    
    /// <summary>
    /// Sid'ы для предустановленных видов документов, удостоверяющих личность.
    /// </summary>
    public static class IdentityDocumentKindsGuid
    {
      // SID Свидетельство о рождении.
      [Sungero.Core.Public]
      public const string BirthCertificate = "9D163AF8-962D-487D-B733-DA62CBD3B003";
      
      // SID Военный билет.
      [Sungero.Core.Public]
      public const string MilitaryID = "9D163AF8-962D-487D-B733-DA62CBD3B007";
      
      // SID Дипломатический паспорт.
      [Sungero.Core.Public]
      public const string DiplomaticPassport = "9D163AF8-962D-487D-B733-DA62CBD3B009";
      
      // SID Паспорт иностранного гражданина.
      [Sungero.Core.Public]
      public const string ForeignCitizenPassport = "9D163AF8-962D-487D-B733-DA62CBD3B010";
      
      // SID Свидетельство о рассмотрении ходатайства о признании лица беженцем на территории Российской Федерации по существу.
      [Sungero.Core.Public]
      public const string CertificateOfConsiderationAsRefugee = "9D163AF8-962D-487D-B733-DA62CBD3B011";
      
      // SID Вид на жительство в Российской Федерации.
      [Sungero.Core.Public]
      public const string ResidentCard = "9D163AF8-962D-487D-B733-DA62CBD3B012";
      
      // SID Удостоверение беженца.
      [Sungero.Core.Public]
      public const string RefugeeCertificate = "9D163AF8-962D-487D-B733-DA62CBD3B013";
      
      // SID Временное удостоверение личности гражданина Российской Федерации.
      [Sungero.Core.Public]
      public const string TemporaryID = "9D163AF8-962D-487D-B733-DA62CBD3B014";
      
      // SID Разрешение на временное проживание в Российской Федерации.
      [Sungero.Core.Public]
      public const string TemporaryResidencePermit = "9D163AF8-962D-487D-B733-DA62CBD3B015";
      
      // SID Свидетельство о предоставлении временного убежища на территории Российской Федерации.
      [Sungero.Core.Public]
      public const string CertificateOfProvisionTemporaryAsylum = "9D163AF8-962D-487D-B733-DA62CBD3B019";
      
      // SID Паспорт гражданина Российской Федерации.
      [Sungero.Core.Public]
      public const string CitizenPassport = "9D163AF8-962D-487D-B733-DA62CBD3B021";
      
      // SID Загранпаспорт гражданина Российской Федерации.
      [Sungero.Core.Public]
      public const string InternationalPassport = "9D163AF8-962D-487D-B733-DA62CBD3B022";
      
      // SID Удостоверение личности военнослужащего Российской Федерации.
      [Sungero.Core.Public]
      public const string IdentityCardOfMilitaryPersonnel = "9D163AF8-962D-487D-B733-DA62CBD3B024";
      
      // SID Паспорт моряка.
      [Sungero.Core.Public]
      public const string SailorPassport = "9D163AF8-962D-487D-B733-DA62CBD3B026";
      
      // SID Иные документы.
      [Sungero.Core.Public]
      public const string OtherDocuments = "9D163AF8-962D-487D-B733-DA62CBD3B091";
    }
    
    /// <summary>
    /// Регулярные выражения для реквизитов документов, удостоверяющих личность.
    /// </summary>
    public static class IdentityDocumentKindPropsPattern
    {
      // 1-25 любых букв или цифр, включая пробел, точку, двоеточие, дробную черту и тире, например, "III/34.67 BL:87-56".
      [Sungero.Core.Public]
      public const string OneToTwentyFiveAlphanumericChars = @"^[a-zA-Zа-яА-ЯёЁ0-9\.\-/: ]{1,25}$";
      
      // 6-7 цифр, например, "123456" или "1234567".
      [Sungero.Core.Public]
      public const string SixToSevenDigits = @"^\d{6,7}$";
      
      // 7 цифр, например, "1234567".
      [Sungero.Core.Public]
      public const string SevenDigits = @"^\d{7}$";
      
      // 6 цифр, например, "123456".
      [Sungero.Core.Public]
      public const string SixDigits = @"^\d{6}$";
      
      // 2 цифры, например "33".
      [Sungero.Core.Public]
      public const string TwoDigits = @"^\d{2}$";
      
      // 2 русские буквы, например, "Ак" 
      [Sungero.Core.Public]
      public const string TwoAlphaChars = @"^[а-яА-ЯёЁ]{2}$";

      // 2 цифры, необязательный пробел, 2 цифры, например, "25 47" или "2574".
      [Sungero.Core.Public]
      public const string TwoDigitsOptionalSpaceTwoDigits = @"^[\d]{2}\ ?[\d]{2}$";
      
      // 3 цифры, тире, 3 цифры, например, "401-014".
      [Sungero.Core.Public]
      public const string ThreeDigitsDashThreeDigits = @"^[\d]{3}\-[\d]{3}$";
    }
    
    // Код системы у external link для данных инициализации.
    [Sungero.Core.Public]
    public const string InitializeExternalLinkSystem = "Initialize";

    public const string SugeroCounterpartyTableName = "Sungero_Parties_Counterparty";
    
    /// <summary>
    /// Шаблон поиска инициалов в строке, в различных вариантах написания.
    /// </summary>
    [Sungero.Core.Public]
    public const string InitialsRegex = @"(?:^|\s)+([А-Я])(?:\.|\s){0,}([А-Я])?(?:\.|\s|$)+";
  }
}