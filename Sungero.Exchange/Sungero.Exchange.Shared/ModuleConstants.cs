using System;

namespace Sungero.Exchange.Constants
{
  public static class Module
  {
    [Sungero.Core.Public]
    public const string SendSignedReceiptNotificationsId = "a050e9dc-ac0a-40c2-a322-7f1832e53f36";
    
    [Sungero.Core.Public]
    public const string CreateReceiptNotifications = "b54f0e86-0cac-49bf-b99b-30ffd8030d9b";
    
    public const string LastBoxIncomingMessageId = "LastBoxIncomingMessageId_{0}";
    public const string LastBoxOutgoingMessageId = "LastBoxOutgoingMessageId_{0}";
    public const string XmlExtension = "xml";
    
    public const string ExchangeDocument = "ExchangeDocument";
    
    public const string RoubleCurrencyCode = "643";
    
    // Имя типа связи "Приложение".
    [Sungero.Core.Public]
    public const string AddendumRelationName = "Addendum";
    
    // Имя типа связи "Прочие".
    [Sungero.Core.Public]
    public const string SimpleRelationRelationName = "Simple relation";
    
    #region Системные имена действий
    
    public const string ReviewAction = "SendForReview";
    
    public const string ApprovalAction = "SendForApproval";
    
    public const string FreeApprovalAction = "SendForFreeApproval";
    
    public const string ExecutionAction = "SendForExecution";
    
    #endregion
    
    // Операции над документом в сервисах обмена.
    public static class Exchange
    {
      /// <summary>
      /// Отправка документов.
      /// </summary>
      [Sungero.Core.Public]
      public const string SendDocument = "ExchSendDoc";
      
      /// <summary>
      /// Отправка ответа контрагенту.
      /// </summary>
      [Sungero.Core.Public]
      public const string SendAnswer = "ExchSendAnswer";
      
      /// <summary>
      /// Получение ответа от контрагента.
      /// </summary>
      [Sungero.Core.Public]
      public const string GetAnswer = "ExchGetAnswer";
      
      /// <summary>
      /// Документ подписан (нами или КА).
      /// </summary>
      [Sungero.Core.Public]
      public const string DetailedSign = "ExchSign";
      
      /// <summary>
      /// В подписании отказано (нами или КА).
      /// </summary>
      [Sungero.Core.Public]
      public const string DetailedReject = "ExchReject";
      
      /// <summary>
      /// Отправлено уведомление об уточнении (нами или КА).
      /// </summary>
      [Sungero.Core.Public]
      public const string DetailedInvoiceReject = "ExchInvReject";
      
      /// <summary>
      /// Отправка извещения о получении.
      /// </summary>
      [Sungero.Core.Public]
      public const string SendReadMark = "ExchReadTo";
      
      /// <summary>
      /// Получение извещения о получении.
      /// </summary>
      [Sungero.Core.Public]
      public const string GetReadMark = "ExchReadFrom";
      
      /// <summary>
      /// Отправка уведомления о приеме.
      /// </summary>
      [Sungero.Core.Public]
      public const string SendNoteReceiptReadMark = "ExchReadNRecTo";
      
      /// <summary>
      /// Получение уведомления о приеме.
      /// </summary>
      [Sungero.Core.Public]
      public const string GetNoteReceiptReadMark = "ExchReadNRFrom";
      
      /// <summary>
      /// Отправка извещение о получении уведомления о приеме.
      /// </summary>
      [Sungero.Core.Public]
      public const string SendRNoteReceiptReadMark = "ExchReadRNRecTo";
      
      /// <summary>
      /// Получение извещение о получении уведомления о приеме.
      /// </summary>
      [Sungero.Core.Public]
      public const string GetRNoteReceiptReadMark = "ExchReadRNRFrom";
      
      /// <summary>
      /// Документ аннулирован нашей организацией.
      /// </summary>
      [Sungero.Core.Public]
      public const string ObsoleteOur = "ExchObsoletOur";

      /// <summary>
      /// Документ аннулирован контрагентом.
      /// </summary>
      [Sungero.Core.Public]
      public const string ObsoletedByCounterparty = "ExchObsoleteCP";
      
      /// <summary>
      /// Документ отозван нашей организацией.
      /// </summary>
      [Sungero.Core.Public]
      public const string TerminateOur = "ExchTerminOur";
      
      /// <summary>
      /// Документ отозван контрагентом.
      /// </summary>
      [Sungero.Core.Public]
      public const string TerminatedByCounterparty = "ExchTerminCP";
    }
    
    /// <summary>
    /// Коды справки для действий по обмену.
    /// </summary>
    public static class HelpCodes
    {
      // Диалог отправки документа.
      public const string SendDocument = "Sungero_Exchange_SendDocumentDialog";
      
      // Диалог отправки ответа на документ.
      public const string SendAnswerOnDocument = "Sungero_Exchange_SendReplyToDocumentDialog";
    }
    
    /// <summary>
    /// Максимальный размер документа, который может быть отправлен через сервис обмена.
    /// </summary>
    public const int ExchangeDocumentMaxSize = 31457280;
    
    public const string FunctionUTDDop = "ДОП";
    
    public const string FunctionUTDDopCorrection = "ДИС";
    
    /// <summary>
    /// Коды документов по КНД.
    /// </summary>
    public static class TaxDocumentClassifier
    {
      /// <summary>
      /// Торг-12.
      /// </summary>
      [Sungero.Core.Public]
      public const string Waybill = "1175004";
      
      /// <summary>
      /// Акт.
      /// </summary>
      [Sungero.Core.Public]
      public const string Act = "1175006";
      
      /// <summary>
      /// ДПТ.
      /// </summary>
      [Sungero.Core.Public]
      public const string GoodsTransferSeller = "1175010";
      
      /// <summary>
      /// ДПРР.
      /// </summary>
      [Sungero.Core.Public]
      public const string WorksTransferSeller = "1175012";
      
      /// <summary>
      /// УПД по приказу ММВ-7-15/820 и ЕД-7-26/970.
      /// </summary>
      [Sungero.Core.Public]
      public const string UniversalTransferDocumentSeller = "1115131";
      
      /// <summary>
      /// УПД по приказу ММВ-7-15/820 и ЕД-7-26/970.
      /// </summary>
      [Sungero.Core.Public]
      public const string UniversalTransferDocumentBuyer = "1115132";
      
      /// <summary>
      /// УПД по приказу ММВ-7-15/155.
      /// </summary>
      [Sungero.Core.Public]
      public const string UniversalTransferDocumentSeller155 = "1115125";
      
      /// <summary>
      /// УКД.
      /// </summary>
      [Sungero.Core.Public]
      public const string UniversalCorrectionDocumentSeller = "1115127";
    }
    
    /// <summary>
    /// Версии форматов УПД.
    /// </summary>
    public static class UniversalTransferDocumentFormatVersions
    {
      /// <summary>
      /// Версия формата 5.01.
      /// </summary>
      [Sungero.Core.Public]
      public const string Version501 = "5.01";
      
      /// <summary>
      /// Версия формата 5.02.
      /// </summary>
      [Sungero.Core.Public]
      public const string Version502 = "5.02";
    }
    
    /// <summary>
    /// Уникальные идентификаторы типа документа.
    /// </summary>
    /// <remarks>Используется в Диадок.</remarks>
    public static class DocumentTypeNamedId
    {
      /// <summary>
      /// УКД.
      /// </summary>
      [Sungero.Core.Public]
      public const string UniversalCorrectionDocument = "UniversalCorrectionDocument";
      
      /// <summary>
      /// Исправление УКД.
      /// </summary>
      [Sungero.Core.Public]
      public const string UniversalCorrectionDocumentRevision = "UniversalCorrectionDocumentRevision";
      
      /// <summary>
      /// УПД.
      /// </summary>
      public const string UniversalTransferDocument = "UniversalTransferDocument";
      
      /// <summary>
      /// Исправление УПД.
      /// </summary>
      public const string UniversalTransferDocumentRevision = "UniversalTransferDocumentRevision";
    }
    
    public static class Initialize
    {
      #region Виды документов
      
      [Sungero.Core.Public]
      public static readonly Guid CancellationAgreementKind = Guid.Parse("f2c9c53a-069a-48c8-9855-2323984757f1");
      
      #endregion
    }
    
    /// <summary>
    /// Идентификатор версии, уникальный в рамках функции типа документа.
    /// </summary>
    /// <remarks>Используется в Диадок.</remarks>
    [Sungero.Core.Public]
    public const string UCDVersion = "ucd736_05_01_02";
    
    /// <summary>
    /// Максимальная длина пути документа, который может быть отправлен через сервис обмена.
    /// </summary>
    public const int ExchangeDocumentMaxLength = 250;
    
    /// <summary>
    /// Результат по умолчанию при вызове из схлопнутого задания на подписание и отправку.
    /// </summary>
    [Sungero.Core.Public]
    public const string DefaultSignResult = "SignAndSend";
    
    /// <summary>
    /// Количество дней для уведомления о том, что сообщение висит в очереди.
    /// </summary>
    public const int PoisonedMessagePeriod = 7;
    
    /// <summary>
    /// Ссылка на эл. доверенность в сервисе.
    /// </summary>
    [Sungero.Core.Public]
    public const string DefaultFormalizedPoALink = "https://m4d.nalog.gov.ru/";
    
    /// <summary>
    /// Максимальное количество загружаемых сообщений за одно выполнение фонового процесса "Получение сообщений".
    /// </summary>
    public const int MaxMessagesToLoading = 1000;
    
    /// <summary>
    /// Максимальное количество попыток получения сообщений.
    /// </summary>
    public const int MaxAttemptsToReceiveMessages = 100;
    
    [Sungero.Core.Public]
    public const string CancellationAgreementGuid = "4c57f798-1547-4de0-b240-d9d97901df5f";
    
    /// <summary>
    /// Константы инициализации модуля.
    /// </summary>
    public static class Init
    {
      public static class Exchange
      {
        /// <summary>
        /// Название параметра для хранения проинициализированной версии кода модуля.
        /// </summary>
        public const string Name = "InitExchangeUpdate";
        
        public const string FirstInitVersion = "4.8.0.0";
        public const string Version49 = "4.9.0.0";
        public const string Version410 = "4.10.0.0";
        public const string Version411 = "4.11.0.0";
      }
    }
    
    public static class BuyerTitleOperationContent
    {
      [Sungero.Core.Public]
      public const string ResultsAcceptedWithoutDisagreement = "Результаты работ (оказанных услуг) приняты без претензий";
      
      [Sungero.Core.Public]
      public const string ServicesProvidedInFull = "Услуги оказаны в полном объеме";
      
      [Sungero.Core.Public]
      public const string AgreedWithPriceChange = "С изменением стоимости согласен";
      
      [Sungero.Core.Public]
      public const string GoodsAcceptedWithoutDisagreement = "Товары (работы, услуги, права) приняты без расхождений (претензий)";
      
      [Sungero.Core.Public]
      public const string GoodsAcceptedWithClaims = "Товары (работы, услуги, права) приняты с расхождениями (претензией)";
      
      [Sungero.Core.Public]
      public const string GoodsNotAccepted = "Товары (работы, услуги, права) не приняты";
      
      [Sungero.Core.Public]
      public const string GoodsTransferred = "Товары переданы";
      
      [Sungero.Core.Public]
      public const string ValuesAcceptedWithoutDisagreement = "Перечисленные в документе ценности приняты без претензий";
    }
    
    /// <summary>
    /// Используется для определения типа счетов из сервиса обмена.
    /// </summary>
    public static class InvoiceTypes
    {
      [Sungero.Core.Public]
      public const string DiadocFormalized = "Diadoc 1.01";
      
      [Sungero.Core.Public]
      public const string DiadocSemiFormalized = "Diadoc semi-formalized";
      
      [Sungero.Core.Public]
      public const string UnsupportedType = "Unsupported type";
      
      [Sungero.Core.Public]
      public const string SbisFormalizedVersion501 = "Sbis 5.01";
    }
  }
}