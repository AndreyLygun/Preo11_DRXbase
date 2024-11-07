using System;

namespace Sungero.Docflow.Constants
{
  public static class Module
  {

    // Цвета графиков.
    public static class Colors
    {
      public const string Purple = "#815D87";
      
      public const string Green = "#82B93A";

      public const string Red = "#FF4242";
    }
    
    // Код системы у external link для данных инициализации.
    [Sungero.Core.Public]
    public const string InitializeExternalLinkSystem = "Initialize";
    
    // Имя параметра: используется ли.
    public const string IsUsedParamName = "IsUsed";
    
    // Имя параметра: есть ли зарегистрированные документы в журнале.
    public const string HasRegisteredDocumentsParamName = "HasRegisteredDocuments";
    
    // Имя параметра: версия создана из шаблона.
    public const string CreateFromTemplate = "CreateFromTemplate";
    
    // Имя параметра: документ сохраняется из задачи на продление срока.
    public const string DeadlineExtentsionTaskCallContext = "DeadlineExtentsionTaskCallContext";
    
    // Имя параметра: документ сохраняется из задачи на контроль возврата.
    public const string CheckReturnTaskCallContext = "CheckReturnTaskCallContext";
    
    // ИД блока доработки согласования официального документа.
    public const string ApprovalReworkAssignmentBlockUid = "5";
    
    // Ключ параметра адреса веб-сервиса проверки контрагентов.
    [Sungero.Core.PublicAttribute]
    public const string CompanyDataServiceKey = "CompanyDataServiceURL";
    
    // Имя параметра адреса веб-сервиса проверки контрагентов.
    public const string CompanyDataServiceDefaultURL = "https://companydata.directum24.ru";

    // Имя параметра для управления рассылкой по умолчанию.
    [Sungero.Core.PublicAttribute]
    public const string DisableMailNotification = "DisableMailNotification";
    
    // Имя параметра для управления способом преобразования в Pdf с отметками.
    [Sungero.Core.PublicAttribute]
    public const string UseObsoletePdfConversionParamName = "UseObsoletePdfConversion";
    
    // Имя параметра для использования устаревших функций регистрации.
    [Sungero.Core.PublicAttribute]
    public const string UseObsoleteRegNumberGenerationParamName = "UseObsoleteRegNumberGeneration";
    
    // Расширение ярлыка Windows Explorer.
    public const string WindowsExplorerLinkExtension = "lnk";
    
    #region Предметное отображение
    
    // Толщина границы выделенного блока.
    public const int CurrentBlockBorderWidth = 4;
    
    // Размер отступа заголовка и основного текста в блоке.
    [Sungero.Core.PublicAttribute]
    public const int EmptyLineMargin = 2;
    
    // Текст разделительной линии.
    [Sungero.Core.PublicAttribute]
    public const string SeparatorText = "________________________________________________________________";
    
    #endregion
    
    #region Связи
    
    // Имя типа связи "Переписка".
    [Sungero.Core.PublicAttribute]
    public const string CorrespondenceRelationName = "Correspondence";
    
    // Имя типа связи "Прочие".
    [Sungero.Core.PublicAttribute]
    public const string SimpleRelationName = "Simple relation";
    
    // Имя типа связи "Отменяет".
    [Sungero.Core.PublicAttribute]
    public const string CancelRelationName = "Cancel";
    
    // Имя связи "Основание"
    [Sungero.Core.PublicAttribute]
    public const string BasisRelationName = "Basis";
    
    // Имя типа связи "Приложение".
    [Sungero.Core.PublicAttribute]
    public const string AddendumRelationName = "Addendum";
    
    // Имя типа связи "Ответное письмо".
    [Sungero.Core.PublicAttribute]
    public const string ResponseRelationName = "Response";

    // Имя типа связи "Корректировка".
    [Sungero.Core.PublicAttribute]
    public const string CorrectionRelationName = "Correction";
    
    #endregion
    
    #region Замещение
    
    // Имя параметра: имеет ли права как замещающий ответственного.
    public const string IsSubstituteResponsibleEmployeeParamName = "IsSubstituteResponsibleEmployee";
    
    // Имя параметра: имеет ли права как администратор.
    public const string IsAdministratorParamName = "IsAdministrator";
    
    #endregion
    
    #region Инициализация
    
    #region Типы документов
    
    // Guid типа документов "Проектный документ".
    public const string ProjectDocumentTypeGuid = "56df80b3-a795-4378-ace5-c20a2b1fb6d9";
    
    #endregion
    
    public const string SungeroWFAssignmentTableName = "Sungero_WF_Assignment";
    
    public const string SugeroContentEDocTableName = "Sungero_Content_EDoc";
    
    public const string SugeroOffDocTrackingTableName = "Sungero_Docflow_OffDocTracking";
    
    public const string SugeroWFTaskTableName = "Sungero_WF_Task";
    
    public const string SungeroMarkTableName = "Sungero_Docflow_Mark";
    
    public const int DefaultApprovalConvertPdfTimeout = 8;
    
    #endregion

    #region Входные параметры функции CompleteReturnControl
    
    public static class ReturnControl
    {
      public const int AbortTask = 0;
      public const int CompleteAssignment = 1;
      public const int SignAssignment = 2;
      public const int NotSignAssignment = 3;
      public const int DeadlineChange = 4;
    }
    
    #endregion
    
    public static class Initialize
    {
      // При создании нового вида гуид для него надо сгенерировать.
      [Sungero.Core.Public]
      public static readonly Guid SimpleDocumentKind = Guid.Parse("3981CDD1-A279-4A51-85D5-58DB391603C2");
      [Sungero.Core.Public]
      public static readonly Guid MemoKind = Guid.Parse("8CB5B6B3-755F-48F3-B5B4-2DFDE6A1FC60");
      [Sungero.Core.Public]
      public static readonly Guid AddendumKind = Guid.Parse("2734AB0B-FD71-4FD2-820E-C25042488547");
      [Sungero.Core.Public]
      public static readonly Guid ExchangeKind = Guid.Parse("4E02B0C3-D448-44E8-AE61-208A79A44205");
      [Sungero.Core.Public]
      public static readonly Guid MemoRegister = Guid.Parse("425355B4-00CC-4417-8EE2-15DE460E034D");
      [Sungero.Core.Public]
      public static readonly Guid PowerOfAttorneyKind = Guid.Parse("0B8E3CF9-77E0-43D3-85E8-7746B41EB822");
      [Sungero.Core.Public]
      public static readonly Guid FormalizedPowerOfAttorneyKind = Guid.Parse("D1FD38A9-1BE7-475E-BF1E-2B2796301BF5");
      [Sungero.Core.Public]
      public static readonly Guid CounterpartyDocumentDefaultKind = Guid.Parse("07ADBF36-0E67-4772-B6C2-06D2CB52EA34");
      [Sungero.Core.Public]
      public static readonly Guid PowerOfAttorneyRevocationKind = Guid.Parse("2D547B15-9845-47D1-A803-7A5734FE5F1B");
    }
    
    // Guid типа "Папка".
    public const string FolderTypeGuid = "271898c8-18ca-4192-9892-e27b273ce5fc";
    
    // Sid приложения-обработчика "Unknown".
    [Sungero.Core.PublicAttribute]
    public static readonly Guid UnknownAppSid = Guid.Parse("49761788-cc45-4485-adb4-55f2056ab043");
    
    #region Группы и роли
    
    public static class RoleGuid
    {
      // GUID роли "Проектные команды".
      [Sungero.Core.Public]
      public static readonly Guid ParentProjectTeam = Guid.Parse("2062682D-745C-4E02-AF2F-26AD229E8C61");

      // GUID роли "Регистраторы входящих документов".
      [Sungero.Core.Public]
      public static readonly Guid RegistrationIncomingDocument = Guid.Parse("63EBE616-8780-4CBB-9AF7-C16251B38A84");
      
      // GUID роли "Регистраторы исходящих документов".
      [Sungero.Core.Public]
      public static readonly Guid RegistrationOutgoingDocument = Guid.Parse("372D8FDB-316E-4F3C-9F6D-C2C1292BBFAE");
      
      // GUID роли "Регистраторы внутренних документов".
      [Sungero.Core.Public]
      public static readonly Guid RegistrationInternalDocument = Guid.Parse("4073E794-3543-4960-8BF7-CA58D933A900");
      
      // GUID роли "Регистраторы договоров".
      [Sungero.Core.Public]
      public static readonly Guid RegistrationContractualDocument = Guid.Parse("25C48B40-6111-4283-A94E-7D50E68DECC1");
      
      // GUID роли "Ответственные за договоры".
      [Sungero.Core.Public]
      public static readonly Guid ContractsResponsible = Guid.Parse("5D813F08-D07F-4EAC-931E-FB3D8BD67012");
      
      // GUID роли "Руководители проектов".
      [Sungero.Core.Public]
      public static readonly Guid ProjectManagersRoleGuid = Guid.Parse("61016C45-E26C-4CF8-B4BE-09F191AC1BCA");
      
      // GUID роли "Подписывающие".
      [Sungero.Core.Public]
      public static readonly Guid SigningRole = Guid.Parse("753971F5-95C6-41F5-9808-7BDC1CF3685E");
      
      // GUID роли "Делопроизводители".
      [Sungero.Core.Public]
      public static readonly Guid ClerksRole = Guid.Parse("B0A07866-7D6F-4860-8850-7016D01EA649");
      
      // GUID роли "Ответственные за настройку регистрации".
      [Sungero.Core.Public]
      public static readonly Guid RegistrationManagersRole = Guid.Parse("F295DDF0-5253-4127-AB54-4F132956FB8F");
      
      // GUID роли "Ответственные за контрагентов".
      [Sungero.Core.Public]
      public static readonly Guid CounterpartiesResponsibleRole = Guid.Parse("C719C823-C4BD-4434-A34B-D7E83E524414");
      
      // GUID роли "Пользователи с правами на удаление документов".
      [Sungero.Core.Public]
      public static readonly Guid DocumentDeleteRole = Guid.Parse("6BCA5136-821C-4D5B-9FB0-67BEE22EFDFE");
      
      // GUID роли "Руководители наших организаций".
      [Sungero.Core.Public]
      public static readonly Guid BusinessUnitHeadsRole = Guid.Parse("03C7A126-83DE-4F8F-908B-3ACB868E30C5");
      
      // GUID роли "Руководители подразделений".
      [Sungero.Core.Public]
      public static readonly Guid DepartmentManagersRole = Guid.Parse("EA04AA41-9BD8-45D5-A479-A986137A509C");

      // GUID роли "Пользователи с расширенным доступом к исполнительской дисциплине".
      [Sungero.Core.Public]
      public static readonly Guid UsersWithAssignmentCompletionRightsRole = Guid.Parse("0E512EDB-E0F0-4818-965F-172D87AB8371");
      
      // GUID роли "Ответственные за внесение данных по документам, удостоверяющим личность".
      [Sungero.Core.Public]
      public static readonly Guid IdentificationDocumentsManagersRole = Guid.Parse("1FA8995C-895C-43C0-A54E-19FEC8782D72");
      
      // GUID роли "Ответственные за настройку системы".
      [Sungero.Core.Public]
      public static readonly Guid SystemSetupResponsibleRole = Guid.Parse("084B9F30-FFF2-4B2C-B365-070EA69C5378");
      
      // GUID роли "Пользователи интерфейса для руководителей".
      [Sungero.Core.Public]
      public static readonly Guid ManagerInterfaceUsersRole = Guid.Parse("FC4F9D00-FC90-456F-8B55-37D325B21E6D");
    }
    
    #endregion
    
    #region Права
    
    /// <summary>
    /// GUID прав.
    /// </summary>
    public static class DefaultAccessRightsTypeSid
    {
      /// <summary>
      /// Регистрация.
      /// </summary>
      public static readonly Guid Register = Guid.Parse("b46abce0-ef53-4053-9b39-0ba83f5cef6d");

      /// <summary>
      /// Удаление.
      /// </summary>
      public static readonly Guid Delete = Guid.Parse("c3a1064e-4939-4b0c-8a43-2e5a0115e13d");
      
      /// <summary>
      /// Изменение содержимого папки.
      /// </summary>
      public static readonly Guid ChangeContent = Guid.Parse("344A32D8-9814-4BB8-8D86-1F65E43FDA25");
      
      /// <summary>
      /// Отправка через сервис обмена.
      /// </summary>
      public static readonly Guid SendByExchange = Guid.Parse("56D0F76C-5442-4B0C-B363-7E353D348994");
      
      /// <summary>
      /// Установить обмен с контрагентом.
      /// </summary>
      public static readonly Guid SetExchange = Guid.Parse("6CCAD865-AA9C-4AFD-A08B-836363545AAF");
      
      /// <summary>
      /// Изменение.
      /// </summary>
      public static readonly Guid Update = Guid.Parse("179af257-a60f-44b8-97b5-1d5bbd06716b");
      
      /// <summary>
      /// Импорт электронной доверенности.
      /// </summary>
      public static readonly Guid ImportFormalizedPowerOfAttorney = Guid.Parse("f4fb494a-1a9e-44db-8ff2-22f0355f24ee");
    }
    
    #endregion
    
    public static class TaskMainGroup
    {
      [Sungero.Core.Public]
      public static readonly Guid ApprovalTask = Guid.Parse("08e1ef90-521f-41a1-a13f-c6f175007e54");

      [Sungero.Core.Public]
      public static readonly Guid DocumentReviewTask = Guid.Parse("88ec82fb-d8a8-4a36-a0d8-5c0bf42ff820");

      [Sungero.Core.Public]
      public static readonly Guid ActionItemExecutionTask = Guid.Parse("804f50fe-f3da-411b-bb2e-e5373936e029");
    }
    
    // Режимы фильтрации заданий.
    public static class FilterAssignmentsMode
    {
      public const string Default = "Default";
      
      public const string Created = "Created";

      public const string Modified = "Modified";
      
      public const string Completed = "Completed";
    }
    
    // Уведомления о завершении работ по доверенности.
    public const string ExpiringPowerOfAttorneyLastNotificationKey = "ExpiringPowerOfAttorneyLastNotification";
    public const string ExpiringPowerOfAttorneyTableName = "Sungero_Docflow_ExpiringPoA";
    
    // Максимально возможное значение дней до завершения - ограничение SQL.
    [Sungero.Core.PublicAttribute]
    public const int MaxDaysToFinish = 24855;
    
    /// <summary>
    /// Коды справки для действий по продлению срока задания.
    /// </summary>
    [Sungero.Core.Public]
    public static class HelpCodes
    {
      // Диалог продления срока задания на доработку.
      public const string DeadlineExtensionDialog = "Sungero_Docflow_DeadlineExtensionDialog";
      
      // Диалог переадресации.
      [Sungero.Core.Public]
      public const string ForwardDialog = "Sungero_Docflow_ForwardDialog";
      
      // Диалог переадресации с указанием срока.
      [Sungero.Core.Public]
      public const string ForwardDialogWithDeadline = "Sungero_Docflow_ForwardDialogWithDeadline";
    }
    
    // Количество страниц документа, на которых ищется якорь для добавления отметки об ЭП.
    public const int SearchablePagesLimit = 5;
    
    #region Интеллектуальная обработка документов
    
    // Ключ параметра адреса сервиса Ario.
    [Sungero.Core.Public]
    public const string ArioUrlKey = "ArioUrl";
    
    // Ключ параметра минимально допустимой вероятности для поля факта, извлеченного Ario.
    // Факт с полем, вероятность которого ниже минимально допустимой, отбрасывается как недостоверный.
    [Sungero.Core.Public]
    public const string MinFactProbabilityKey = "MinFactProbability";
    
    // Ключ параметра вероятности для поля факта, извлеченного Ario, выше которой факт считается достоверным.
    [Sungero.Core.Public]
    public const string TrustedFactProbabilityKey = "TrustedFactProbability";
    
    // Названия параметров отображения фокусировки подсветки в предпросмотре.
    public static class HighlightActivationStyleParamNames
    {
      // Признак фокусировки поля с помощью рамки.
      public const string UseBorder = "HighlightActivationStyleUseBorder";
      
      // Цвет рамки.
      public const string BorderColor = "HighlightActivationStyleBorderColor";
      
      // Толщина рамки.
      public const string BorderWidth = "HighlightActivationStyleBorderWidth";
      
      // Признак фокусировки поля с помощью заливки.
      public const string UseFilling = "HighlightActivationStyleUseFilling";
      
      // Цвет заливки.
      public const string FillingColor = "HighlightActivationStyleFillingColor";
    }
    
    [Sungero.Core.Public]
    public const char PositionElementDelimiter = '|';

    public const int HighlightActivationBorderDefaultWidth = 10;
    
    [Sungero.Core.Public]
    public const char PropertyAndPositionDelimiter = '-';

    [Sungero.Core.Public]
    public const char PositionsDelimiter = '#';
    
    /// <summary>
    /// Имя функции обработки.
    /// </summary>
    [Sungero.Core.Public]
    public static class ProcessingFunctionName
    {
      [Sungero.Core.Public]
      public const string CreateIncomingLetter = "CreateIncomingLetter";
      [Sungero.Core.Public]
      public const string CreateContractStatement = "CreateContractStatement";
      [Sungero.Core.Public]
      public const string CreateWaybill = "CreateWaybill";
      [Sungero.Core.Public]
      public const string CreateTaxInvoice = "CreateTaxInvoice";
      [Sungero.Core.Public]
      public const string CreateTaxInvoiceCorrection = "CreateTaxInvoiceCorrection";
      [Sungero.Core.Public]
      public const string CreateUniversalTransferDocument = "CreateUniversalTransferDocument";
      [Sungero.Core.Public]
      public const string CreateUniversalTransferCorrectionDocument = "CreateUniversalTransferCorrectionDocument";
      [Sungero.Core.Public]
      public const string CreateIncomingInvoice = "CreateIncomingInvoice";
      [Sungero.Core.Public]
      public const string CreateContract = "CreateContract";
      [Sungero.Core.Public]
      public const string CreateSupAgreement = "CreateSupAgreement";
      [Sungero.Core.Public]
      public const string CreateSimpleDocument = "CreateSimpleDocument";
    }
    #endregion
    
    #region Ставки НДС
    
    /// <summary>
    /// Значение по умолчанию для ставки "Без НДС".
    /// </summary>
    public const int DefaultVatRateWithoutVat = 0;
    
    /// <summary>
    /// Sid ставки НДС "Без НДС".
    /// </summary>
    public const string VatRateWithoutVatSid = "930EC682-0CA7-4B9E-9F0F-2F5CE8B6A90B";
    
    /// <summary>
    /// Значение по умолчанию для ставки "0%".
    /// </summary>
    public const int DefaultVatRateZeroPercent = 0;
    
    /// <summary>
    /// Sid ставки НДС "0%".
    /// </summary>
    public const string VatRateZeroPercentSid = "D54D8812-2BBF-4BBA-8CA5-BECB3FBD6DDB";
    
    /// <summary>
    /// Значение по умолчанию для ставки "10%".
    /// </summary>
    public const int DefaultVatRateTenPercent = 10;
    
    /// <summary>
    /// Sid ставки НДС "10%".
    /// </summary>
    public const string VatRateTenPercentSid = "C35498D8-511F-4218-8137-39EC11A1596B";
    
    /// <summary>
    /// Значение по умолчанию для ставки "20%".
    /// </summary>
    public const int DefaultVatRateTwentyPercent = 20;
    
    /// <summary>
    /// Sid ставки НДС "20%".
    /// </summary>
    public const string VatRateTwentyPercentSid = "D99F2F70-1069-4190-95EE-948F49C065C5";
    
    #endregion
    
    #region Oid сертификата
    
    /// <summary>
    /// Список идентификаторов объектов.
    /// </summary>
    public static class CertificateOid
    {
      [Sungero.Core.Public]
      public const string CommonName = "2.5.4.3";
      [Sungero.Core.Public]
      public const string Country = "2.5.4.6";
      [Sungero.Core.Public]
      public const string State = "2.5.4.8";
      [Sungero.Core.Public]
      public const string Locality = "2.5.4.7";
      [Sungero.Core.Public]
      public const string Street = "2.5.4.9";
      [Sungero.Core.Public]
      public const string Department = "2.5.4.11";
      [Sungero.Core.Public]
      public const string Surname = "2.5.4.4";
      [Sungero.Core.Public]
      public const string GivenName = "2.5.4.42";
      [Sungero.Core.Public]
      public const string JobTitle = "2.5.4.12";
      [Sungero.Core.Public]
      public const string OrganizationName = "2.5.4.10";
      [Sungero.Core.Public]
      public const string Email = "1.2.840.113549.1.9.1";
      [Sungero.Core.Public]
      public const string TIN = "1.2.643.3.131.1.1";
    }
    
    #endregion
    
    // Перенос документов между хранилищами.
    public const string StoragePolicySettingsTableName = "Sungero_Docflow_StoragePolicySettings";
    
    // Системные пользователи.
    [Sungero.Core.Public]
    public static readonly Guid CollaborationService = Guid.Parse("F28BCB67-223E-466E-B251-8748D9F12C14");
    
    [Sungero.Core.Public]
    public static readonly Guid DefaultGroup = Guid.Parse("73490DC5-1209-4D72-AFD3-BDCF60456E46");
    
    [Sungero.Core.Public]
    public static readonly Guid DefaultUser = Guid.Parse("63490DC5-1209-4D72-AFD3-BDCF60482E46");
    
    // Ограничение длины названия файла при выгрузке.
    public const int ExportNameLength = 50;
    
    // Guid справочника видов документов.
    public static readonly Guid DocumentKindTypeGuid = Guid.Parse("14a59623-89a2-4ea8-b6e9-2ad4365f358c");
    
    // Guid типа документа "Служебная записка".
    public const string MemoTypeGuid = "95af409b-83fe-4697-a805-5a86ceec33f5";
    
    // Максимальное количество адресатов, которые будут выведены в шаблон документа.
    public const int AddresseesShortListLimit = 4;
    
    /// <summary>
    /// Позиция ИД документа в комментарии записи в истории.
    /// </summary>
    public const int DocumentIdCommentPosition = 1;
    
    /// <summary>
    /// Позиция ИД группы вложений в комментарии записи в истории.
    /// </summary>
    public const int AttachmentGroupIdCommentPosition = 2;
    
    /// <summary>
    /// Разделитель элементов в комментарии записи в истории.
    /// </summary>
    public const char HistoryCommentDelimiter = '|';
    
    /// <summary>
    /// Пробел нулевой ширины.
    /// </summary>
    public const char ZeroWidthSpace = '\u200b';
    
    public const string ApprovalSignatureType = "Approval";
    
    public const string EndorsingSignatureType = "Endorsing";
    
    /// <summary>
    /// Отступ справа для простановки отметки о поступлении, в сантиметрах.
    /// </summary>
    [Sungero.Core.PublicAttribute]
    public const double RegistrationStampDefaultRightIndent = 1.0;
    
    /// <summary>
    /// Отступ снизу для простановки отметки о поступлении, в сантиметрах.
    /// </summary>
    [Sungero.Core.PublicAttribute]
    public const double RegistrationStampDefaultBottomIndent = 0.3;
    
    /// <summary>
    /// Отступ справа для простановки отметки о поступлении в центре страницы, в сантиметрах.
    /// </summary>
    [Sungero.Core.PublicAttribute]
    public const double RegistrationStampDefaultPageCenterIndent = 8.5;
    
    /// <summary>
    /// Имя параметра с датой последнего запуска ФП "Документооборот. Рассылка электронных писем о заданиях".
    /// </summary>
    public const string LastNotificationOfAssignment = "LastNotificationOfAssignment";
    
    /// <summary>
    /// Имя параметра "Количество писем в пакете".
    /// </summary>
    public const string SummaryMailNotificationsBunchCountParamName = "SummaryMailNotificationsBunchCount";

    /// <summary>
    /// Количество рабочих дней, которые считаются как ближайшее время для выполнения заданий.
    /// </summary>
    public const int SummaryMailNotificationClosestDaysCount = 3;
    
    /// <summary>
    /// Количество писем в пакете.
    /// </summary>
    public const int SummaryMailNotificationsBunchCount = 50;
    
    /// <summary>
    /// Количество пользователей для отправки писем со сводкой.
    /// </summary>
    public const int SummaryMailEmployeesBunchSize = 100;
    
    /// <summary>
    /// Имя параметра "Количество пользователей для отправки писем со сводкой".
    /// </summary>
    public const string SummaryMailEmployeesBunchSizeParamName = "SummaryMailEmployeesBunchSize";
    
    /// <summary>
    /// Количество заданий для отправки писем о новых/просроченных заданиях.
    /// </summary>
    public const int MailAssignmentsBunchSize = 100;
    
    /// <summary>
    /// Имя параметра "Количество заданий для отправки писем о новых/просроченных заданиях".
    /// </summary>
    public const string MailAssignmentsBunchSizeParamName = "MailAssignmentsBunchSize";
    
    /// <summary>
    /// Максимальное количество переповторов отправки писем по заданиям и задачам.
    /// </summary>
    public const int SendMailNotificationRetriesMaxCount = 3;
    
    /// <summary>
    /// Параметры процесса назначения прав на пачку документов.
    /// </summary>
    public static class AccessRightsBulkProcessing
    {
      /// <summary>
      /// Максимальное количество элементов очереди выдачи прав на пачку документов, обрабатываемых фоновым процессом.
      /// </summary>
      public const int JobQueueItemsLimit = 70;
      
      /// <summary>
      /// Имя параметра "Максимальное количество элементов очереди выдачи прав на пачку документов, обрабатываемых фоновым процессом".
      /// </summary>
      public const string JobQueueItemsLimitParamName = "AccessRightsBulkProcessingJobQueueItemsLimit";
      
      /// <summary>
      /// Количество документов в пакете для массовой выдачи прав.
      /// </summary>
      public const int BatchSize = 100;
      
      /// <summary>
      /// Имя параметра "Количество документов в пакете для массовой выдачи прав".
      /// </summary>
      public const string BatchSizeParamName = "AccessRightsBulkProcessingBatchSize";
      
      /// <summary>
      /// Максимальное количество переповторов элемента очереди выдачи прав на пачку документов.
      /// </summary>
      public const int RetriesLimit = 50;
      
      /// <summary>
      /// Имя параметра "Максимальное количество переповторов элемента очереди выдачи прав на пачку документов".
      /// </summary>
      public const string RetriesLimitParamName = "AccessRightsBulkProcessingRetriesLimit";
    }
    
    /// <summary>
    /// Размер отступа для шаблона письма.
    /// </summary>
    public const int SummaryMailLeftMarginSize = 12;
    
    // Разделители UnsignedAdditionalInfo в подписи документа.
    [Sungero.Core.Public]
    public static class UnsignedAdditionalInfoSeparator
    {
      [Sungero.Core.Public]
      public const char Attribute = '|';
      
      [Sungero.Core.Public]
      public const char KeyValue = '=';
    }
    
    // Ключ для поля Единый рег. № эл. доверенности в UnsignedAdditionalInfo подписи документа.
    [Sungero.Core.Public]
    public const string UnsignedAdditionalInfoKeyFPoA = "FPoA";
    
    /// <summary>
    /// Максимальное количество переповторов асинхронного обработчика сообщений сервиса обмена.
    /// </summary>
    [Sungero.Core.Public]
    public const int ProcessMessagesRetriesMaxCount = 100;
    
    /// <summary>
    /// Имя параметра "Максимальное количество переповторов асинхронного обработчика сообщений сервиса обмена".
    /// </summary>
    [Sungero.Core.Public]
    public const string ProcessMessagesRetriesMaxCountParamName = "ProcessMessagesRetriesMaxCount";
    
    /// <summary>
    /// Максимальное количество переповторов асинхронного обработчика преобразования документов электронного обмена в pdf.
    /// </summary>
    [Sungero.Core.Public]
    public const int ConvertExchangeDocumentToPdfRetriesMaxCount = 50;
    
    /// <summary>
    /// Имя параметра "Масимальной количество переповоторов асинхронного обработчика сообщенинй сервиса обмена при исторической загрузке".
    /// </summary>
    [Sungero.Core.Public]
    public const string ProcessHistoricalMessageRetriesMaxCountParamName = "ProcessHistoricalMessageRetriesMaxCount";
    
    /// <summary>
    /// Масимальной количество переповоторов асинхронного обработчика сообщенинй сервиса обмена при исторической загрузке.
    /// </summary>
    [Sungero.Core.Public]
    public const int ProcessHistoricalMessageRetriesMaxCount = 10;
    
    /// <summary>
    /// Имя параметра "Максимальное количество переповторов асинхронного обработчика преобразования документов электронного обмена в pdf".
    /// </summary>
    [Sungero.Core.Public]
    public const string ConvertExchangeDocumentToPdfRetriesMaxCountParamName = "ConvertExchangeDocumentToPdfRetriesMaxCount";
    
    // Параметр "Имя исходного файла при загрузке приложения".
    [Sungero.Core.Public]
    public const string AddendumSourceFileNameParamName = "AddendumSourceFileName";
    
    /// <summary>
    /// Разделитель-запятая.
    /// </summary>
    public const char CommaDelimiter = ',';
    
    /// <summary>
    /// Максимальная длина имени файла при сравнении двух документов.
    /// </summary>
    public const int MaxDocumentNameLengthForComparingDocuments = 30;
    
    /// <summary>
    /// Максимальная длина имени файла при сравнении версий.
    /// </summary>
    public const int MaxDocumentNameLengthForComparingVersions = 60;
    
    /// <summary>
    /// Лимит элементов очереди исторических сообщений.
    /// </summary>
    [Sungero.Core.Public]
    public const int HistoricalMessageQueueItemLimit = 10000;
    
    /// <summary>
    /// Имя параметра "Лимит элементов очереди исторических сообщений".
    /// </summary>
    [Sungero.Core.Public]
    public const string HistoricalMessageQueueItemLimitParamName = "HistoricalMessageQueueItemLimit";
    
    /// <summary>
    /// Завершающий символ для краткого наименования.
    /// </summary>
    public const string ShortNameEnd = ".";
    
    /// <summary>
    /// Шаблон строки денежной суммы с указанием валюты прописью.
    /// </summary>
    public const string MoneyAmountWithCurrencyTemplate = "{0} {1} ({2})";
    
    /// <summary>
    /// Шаблон строки денежной суммы с указанием валюты прописью без десятичного значения.
    /// </summary>
    public const string MoneyAmountWithCurrencyWithoutDecimalValueTemplate = "{0} {1} {2} {3}";
    
    /// <summary>
    /// Шаблон строки десятичного числа.
    /// </summary>
    public const string DecimalValueStringTemplate = "{0},{1}";
    
    /// <summary>
    /// Формат целой части десятичного числа при конвертации в строку.
    /// </summary>
    public const string DecimalValueIntegerPartFormat = "N0";
    
    /// <summary>
    /// Формат дробной части десятичного числа при конвертации в строку.
    /// </summary>
    public const string DecimalValueFractionalPartFormat = "D2";
    
    /// <summary>
    /// Отображаемое значение для ставки НДС "Без НДС".
    /// </summary>
    public const string VatRateWithoutVatDisplayValue = "-";
    
    /// <summary>
    /// Шаблон строки ставки НДС для записи в историю.
    /// </summary>
    public const string VatRateHistoryTemplate = " ({0}%)";
    
    /// <summary>
    /// Количество символов удаляемых из строки записи в историю ставки НДС.
    /// </summary>
    public const int VatRateHistoryTrimSymbolsCount = 2;
    
    /// <summary>
    /// Параметры процесса индексации документов для полнотекстового поиска.
    /// </summary>
    public static class DocumentIndexing
    {
      /// <summary>
      /// Максимальный размер версии документа для отправки на индексацию.
      /// </summary>
      public const int VersionMaxSizeInBytes = 78643200;
      
      /// <summary>
      /// Допустимые расширения для отправки на индексацию.
      /// </summary>
      public const string VersionAllowedExtensions = "bmp,gif,jpeg,jpg,png,tiff,tif,pdf";
      
      /// <summary>
      /// Максимальное количество итераций асинхронных обработчиков индексации документа по умолчанию.
      /// </summary>
      public const int RetryLimit = 50;

      /// <summary>
      /// Имя параметра: максимальное количество итераций асинхронных обработчиков индексации документа.
      /// </summary>
      public const string RetryLimitParamName = "IndexDocumentsRetriesLimit";
      
      /// <summary>
      /// Имя параметра: дата и время последнего запуска фоновой индексации документов.
      /// </summary>
      public const string LastIndexingRunParamName = "IndexDocumentsJobLastRunDate";
      
      /// <summary>
      /// Количество одновременно индексируемых документов.
      /// </summary>
      public const int DefaultQueueItemsInProcessLimit = 120;
      
      /// <summary>
      /// Имя параметра: количество одновременно индексируемых документов.
      /// </summary>
      public const string QueueItemsInProcessLimitParamName = "IndexDocumentsQueueItemsLimit";
      
      /// <summary>
      /// Формат хранения дат для полнотекстового поиска в таблице Docflow_Params.
      /// </summary>
      public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.ffff+0";
      
      /// <summary>
      /// Количество документов, обрабатываемых за итерацию, при массовой переиндексации исторических документов.
      /// </summary>
      public const int DefaultBulkIndexingDocumentsLimit = 500;
      
      /// <summary>
      /// Имя параметра: количество документов, обрабатываемых за итерацию, при массовой переиндексации исторических документов.
      /// </summary>
      public const string BulkIndexingDocumentsLimitParamName = "IndexDocumentsBulkBatchSize";
      
      /// <summary>
      /// Режимы массовой индексации документов для полнотекстового поиска.
      /// </summary>
      public static class BulkIndexingMode
      {
        /// <summary>
        /// Режим массовой индексации документов по дате создания.
        /// </summary>
        public const string Created = "Created";
        
        /// <summary>
        /// Режим массовой индексации документов по дате изменения.
        /// </summary>
        public const string Modified = "Modified";
      }
    }
    
    /// <summary>
    /// ИД фонового процесса "Индексация документов для полнотекстового поиска".
    /// </summary>
    public const string IndexDocumentsForFullTextSearchId = "9d346c59-72e0-4957-b13a-9becf38218b1";
    
    /// <summary>
    /// Максимальное количество элементов очереди эл. доверенностей, обрабатываемых одновременно.
    /// </summary>
    [Sungero.Core.Public]
    public const int FPoAQueueItemBatchSize = 30;
    
    /// <summary>
    /// Имя параметра "Максимальное количество элементов очереди эл. доверенностей, обрабатываемых одновременно".
    /// </summary>
    [Sungero.Core.Public]
    public const string FPoAQueueItemBatchSizeParamName = "FPoAQueueItemBatchSize";
    
    /// <summary>
    /// Максимальное количество переповторов асинхронного обработчика синхронизации статуса эл. доверенности.
    /// </summary>
    public const int SyncFormalizedPoAWithServiceRetriesMaxCount = 20;
    
    /// <summary>
    /// Имя параметра "Максимальное количество переповторов асинхронного обработчика для получения результата операции с доверенностью".
    /// </summary>
    [Sungero.Core.Public]
    public const string FPoAGetOperationRetriesMaxCountParamName = "FPoAGetOperationRetriesMaxCount";
    
    /// <summary>
    /// Максимальное количество переповторов асинхронного обработчика для получения результата операции с доверенностью.
    /// </summary>
    [Sungero.Core.Public]
    public const int FPoAGetOperationRetriesMaxCount = 50;
    
    /// <summary>
    /// Имя параметра "Шаблон адреса поиска электронной доверенности в реестре ФНС".
    /// </summary>
    [Sungero.Core.Public]
    public const string SearchFPoAInFtsRegistryTemplateParamName = "SearchFPoAInFtsRegistryTemplate";
    
    /// <summary>
    /// Шаблон адреса поиска электронной доверенности в реестре ФНС.
    /// </summary>
    [Sungero.Core.Public]
    public const string SearchFPoAInFtsRegistryTemplate = "https://m4d.nalog.gov.ru/EMCHD/get-info?guid={0}&innP={1}&innR={2}";
    
    /// <summary>
    /// Guid модуля PowerOfAttorneyKontur.
    /// </summary>
    [Sungero.Core.Public]
    public static readonly Guid PowerOfAttorneyKonturGuid = Guid.Parse("a37bcb31-c5ce-4052-af97-ab7cbd19bf27");
    
    /// <summary>
    /// Имя параметра "Коды документов, удостоверяющих личность, поддерживаемые в эл. доверенности".
    /// </summary>
    public const string FPoAIdentityDocumentCodesParamName = "FPoAIdentityDocumentCodes";
    
    /// <summary>
    /// Коды документов, удостоверяющих личность, поддерживаемые в эл. доверенности.
    /// </summary>
    public const string FPoAIdentityDocumentCodes = "21,07,10,11,12,13,15,19,24";
    
    /// <summary>
    /// Имя параметра "Количество дней на передачу прав в замещении после закрытия карточки сотрудника".
    /// </summary>
    [Sungero.Core.Public]
    public const string SubstitutionAccessRightsGrantBufferDaysCountParamName = "SubstitutionAccessRightsGrantBufferDaysCount";
    
    /// <summary>
    /// Количество дней на передачу прав в замещении после закрытия карточки сотрудника.
    /// </summary>
    [Sungero.Core.Public]
    public const int SubstitutionAccessRightsGrantBufferDaysCount = 1800;
    
    /// <summary>
    /// Имя параметра "Количество дней ожидания перед передачей прав в замещении после закрытия карточки сотрудника".
    /// </summary>
    [Sungero.Core.Public]
    public const string SubstitutionAccessRightsWaitingDaysCountParamName = "SubstitutionAccessRightsWaitingDaysCount";
    
    /// <summary>
    /// Количество дней ожидания перед передачей прав в замещении после закрытия карточки сотрудника.
    /// </summary>
    [Sungero.Core.Public]
    public const int SubstitutionAccessRightsWaitingDaysCount = 180;
    
    /// <summary>
    /// Количество удаляемых элементов очереди выдачи прав на пакет документов.
    /// </summary>
    public const int DeleteAccessRightsBulkQueueItemsBatchSize = 100;
    
    /// <summary>
    /// Количество элементов очереди передачи прав для команды RXCmd.
    /// </summary>
    [Sungero.Core.Public]
    public const int BulkTransferAccesRightsSessionBatchSize = 500;
    
    /// <summary>
    /// Имя параметра "Количество элементов очереди передачи прав для команды RXCmd".
    /// </summary>
    [Sungero.Core.Public]
    public const string BulkTransferAccesRightsSessionBatchSizeParamName = "BulkTransferAccesRightsBatchSize";
    
    /// <summary>
    /// Имя параметра: признак ошибки при получении контента для отметки.
    /// </summary>
    public const string GetContentErrorParamName = "GetContentError";
    
    /// <summary>
    /// Константы инициализации модуля.
    /// </summary>
    public static class Init
    {
      public static class Docflow
      {
        /// <summary>
        /// Название параметра для хранения проинициализированной версии кода модуля.
        /// </summary>
        public const string Name = "InitDocflowUpdate";
        
        public const string FirstInitVersion = "4.8.0.0";
        public const string Version49 = "4.9.0.0";
        public const string Version410 = "4.10.0.0";
        public const string Version411 = "4.11.0.0";
      }
    }
    
    /// <summary>
    /// Постфикс логгера возврата документа.
    /// </summary>
    [Sungero.Core.Public]
    public const string TrackingLoggerPostfix = "Tracking";
    
    /// <summary>
    /// Максимальная длина расширения файла.
    /// </summary>
    public const int MaxFileExtensionLength = 5;
    
    /// <summary>
    /// ИД ведущего документа.
    /// </summary>
    public const string LeadingDocumentIdParamName = "LeadingDocumentId";
    
    /// <summary>
    /// Паттерн открывающего 'html' тэга.
    /// </summary>
    public const string HtmlOpeningTagPattern = @"\<html.*\>";
    
    /// <summary>
    /// Паттерн закрывающего 'html' тэга.
    /// </summary>
    public const string HtmlClosingTagPattern = @"\<\/html\>\s*$";
    
    /// <summary>
    /// Формат хранения даты и времени только для дат.
    /// </summary>
    public const string ShortDateFormat = "dd.MM.yyyy";
  }
}