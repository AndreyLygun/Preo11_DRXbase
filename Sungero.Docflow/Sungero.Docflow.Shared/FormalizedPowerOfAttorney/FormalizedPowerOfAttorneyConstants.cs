using System;
using Sungero.Core;

namespace Sungero.Docflow.Constants
{
  public static class FormalizedPowerOfAttorney
  {
    /// <summary>
    /// Xml расширение.
    /// </summary>
    [Sungero.Core.Public]
    public const string XmlExtension = "xml";
    
    /// <summary>
    /// Html расширение.
    /// </summary>
    [Sungero.Core.Public]
    public const string HtmlExtension = "html";
    
    /// <summary>
    /// Максимальная длина для наименования органа, выдавшего ДУЛ.
    /// </summary>
    [Sungero.Core.Public]
    public const int IdentityAuthorityMaxLength = 4000;
    
    /// <summary>
    /// Названия элементов в теле эл. доверенности.
    /// </summary>
    public static class XmlElementNames
    {
      public const string PowerOfAttorney = "Доверенность";
      public const string Document = "Документ";
      public const string PowerOfAttorneyVersion2 = "Довер";
      
      public const string PowerOfAttorneyInfo = "СвДов";
      public const string AuthorizedRepresentative = "СвУпПред";
      public const string Representative = "СвПред";
      public const string Individual = "СведФизЛ";
      public const string SystemUrl003 = "СведСист";
      public const string SystemUrl002 = "СведСистОтм";
    }
    
    /// <summary>
    /// Название атрибута версии формата в теле эл. доверенности.
    /// </summary>
    public const string XmlFPoAFormatVersionAttributeName = "ВерсФорм";
    
    /// <summary>
    /// Названия атрибутов уполномоченного лица в теле эл. доверенности.
    /// </summary>
    public static class XmlIssuedToAttributeNames
    {
      public const string TIN = "ИННФЛ";
      public const string INILA = "СНИЛС";
      
      public const string IndividualName = "ФИО";
      public const string LastName = "Фамилия";
      public const string FirstName = "Имя";
      public const string MiddleName = "Отчество";
    }
    
    /// <summary>
    /// Названия атрибутов информации о доверенности в теле эл. доверенности.
    /// </summary>
    public static class XmlFPoAInfoAttributeNames
    {
      public const string UnifiedRegistrationNumber = "НомДовер";
      public const string ValidFrom = "ДатаВыдДовер";
      public const string ValidTill = "ДатаКонДовер";
      public const string RegistrationNumber = "ВнНомДовер";
      public const string RegistrationDate = "ДатаВнРегДовер";
    }

    public static class Operation
    {
      /// <summary>
      /// Импорт эл. доверенности из xml-файла.
      /// </summary>
      public const string ImportFromXml = "ImportFromXml";
      
      /// <summary>
      /// Изменение статуса "В реестре ФНС".
      /// </summary>
      public const string FtsStateChange = "FtsStateChange";
      
      /// <summary>
      /// Очистка статуса "В реестре ФНС".
      /// </summary>
      public const string FtsStateClear = "FtsStateClear";
    }
    
    /// <summary>
    /// Код диалога импорта эл. доверенности из xml-файла.
    /// </summary>
    public const string ImportFromXmlHelpCode = "Sungero_ImportFormalizedPowerOfAttorneyFromXmlDialog";
    
    /// <summary>
    /// Код диалога создания заявления на отзыв эл. доверенности.
    /// </summary>
    public const string CreatePowerOfAttorneyRevocationHelpCode = "Sungero_CreatePowerOfAttorneyRevocationDialog";
    
    /// <summary>
    /// Параметр "Последняя версия подписана и утверждена".
    /// </summary>
    [Public]
    public const string IsLastVersionApprovedParamName = "IsLastVersionApproved";
    
    /// <summary>
    /// Максимальное количество символов в контроле для указания причины отзыва эл. доверенности.
    /// </summary>
    [Public]
    public const int FPoARevocationReasonMaxLength = 500;
    
    /// <summary>
    /// Код Российской Федерации в Общероссийском классификаторе стран мира (ОКСМ).
    /// </summary>
    [Public]
    public const string RussianFederationCountryCode = "643";
    
    /// <summary>
    /// Значения атрибутов основной информации по доверенности.
    /// </summary>
    public static class XmlGeneralInfoAttributeValues
    {
      /// <summary>
      /// Сведения об информационной системе.
      /// </summary>
      public const string SourceSystemInfo = "https://m4d.nalog.gov.ru/EMCHD/check-status?guid=";
    }
    
    /// <summary>
    /// Значения атрибутов элемента "Сведения о физическом лице".
    /// </summary>
    public static class XmlIndividualInfoAttributeValues
    {
      /// <summary>
      /// Гражданин Российской Федерации.
      /// </summary>
      public const string RussianFederationCitizen = "1";
    }
    
    /// <summary>
    /// Статус асинхронной операции.
    /// </summary>
    public static class FPoARegistrationStatus
    {
      /// <summary>
      /// В очереди на регистрацию.
      /// </summary>
      public const string Queued = "queued";
      
      /// <summary>
      /// Обработка регистрации.
      /// </summary>
      public const string Processing = "processing";

      /// <summary>
      /// Регистрация успешно завершена.
      /// </summary>
      public const string Done = "done";
      
      /// <summary>
      /// При регистрации возникли ошибки.
      /// </summary>
      public const string Error = "error";
    }
    
    /// <summary>
    /// Состояния эл. доверенности.
    /// </summary>
    [Sungero.Core.Public]
    public static class FPoAState
    {
      public const string Created = "created";
      public const string Valid = "valid";
      public const string Invalid = "invalid";
      public const string Expired = "poaIsExpired";
      public const string Revoked = "poaWasRevoked";
      public const string NotValidYet = "poaIsNotValidYet";
      
      /// <summary>
      /// Код ошибки "Доверенность не найдена".
      /// </summary>
      [Sungero.Core.Public]
      public const string PoANotFoundError = "poaNotFound";
    }
    
    /// <summary>
    /// Ошибки при регистрации с эл. доверенностями.
    /// </summary>
    [Sungero.Core.Public]
    public static class FPoARegistrationErrors
    {
      /// <summary>
      /// Код ошибки "недоступность ФНС".
      /// </summary>
      [Sungero.Core.Public]
      public const string ExternalSystemIsUnavailableError = "externalsystemisunavailable";
      
      /// <summary>
      /// Код ошибки "невалидный сертификат".
      /// </summary>
      [Sungero.Core.Public]
      public const string InvalidCertificateError = "invalidcertificate";
      
      /// <summary>
      /// Код ошибки "ошибка при отправке на регистрацию".
      /// </summary>
      [Sungero.Core.Public]
      public const string SendForRegistrationError = "sendforregistrationerror";
      
      /// <summary>
      /// Код ошибки "доверенность уже зарегистрирована".
      /// </summary>
      [Sungero.Core.Public]
      public const string RepeatedRegistrationError = "conflictsamecontentandsamesignature";
      
      /// <summary>
      /// Код ошибки "подписана не тем сертификатом".
      /// </summary>
      [Sungero.Core.Public]
      public const string DifferentSignatureError = "conflictdifferentsignature";
      
      /// <summary>
      /// Код ошибки "данные подписанта в xml не совпадают с ЕГРЮЛ (СНИЛС, ИНН, ФИО, ОГРН)".
      /// </summary>
      [Sungero.Core.Public]
      public const string UnsupportedPoA = "unsupportedpoa";
    }
    
    /// <summary>
    /// Имя параметра "Эл. доверенность только что была импортирована".
    /// </summary>
    [Sungero.Core.Public]
    public const string FPoAWasJustImportedParamName = "FPoAWasJustImported";
    
    /// <summary>
    /// Идентификатор системы, в которой осуществляется хранение МЧД.
    /// </summary>
    [Sungero.Core.Public]
    public const string FPoAStorageSystemId = "Directum RX";
    
    /// <summary>
    /// Позиция символа - признака нотариальной доверенности в xml в атрибуте Доверенность[ПрЭлФорм].
    /// </summary>
    public const int IsNotarizedSignPosition = 2;
    
    /// <summary>
    /// Значение символа в атрибуте Доверенность[ПрЭлФорм] - указывающее, что доверенность является нотариальной.
    /// </summary>
    public const string IsNotarizedSignValue = "1";
    
    /// <summary>
    /// Префикс идентификатора файла для эл. доверенности версии 002.
    /// </summary>
    public const string FPoAV2FilePrefix = "ON_DOVBB_";
    
    /// <summary>
    /// Префикс идентификатора файла для эл. доверенности версии 003.
    /// </summary>
    public const string FPoAV3FilePrefix = "ON_EMCHD_";
  }
}