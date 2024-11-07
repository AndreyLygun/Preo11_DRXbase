using System;
using Sungero.Core;

namespace Sungero.DocflowApproval.Constants
{
  public static class Module
  {
    /// <summary>
    /// Имя типа связи "Переписка".
    /// </summary>
    public const string CorrespondenceRelationName = "Correspondence";
    
    /// <summary>
    /// ИД способа отправки через сервис эл. обмена.
    /// </summary>
    public const string ExchangeDeliveryMethodSid = "267c030c-a93a-44d8-ba60-17d8b56ad9c8";
    
    /// <summary>
    /// Имя параметра для кэширования значения.
    /// </summary>
    public const string NeedToShowExchangeServiceHint = "NeedToShowExchangeServiceHint";
    
    /// <summary>
    /// Количество дней для возврата документа контрагентом по умолчанию.
    /// </summary>
    public const int DefaultDaysToReturn = 10;
    
    /// <summary>
    /// Имя параметра "Показывать хинт об отсутствии прав на документ".
    /// </summary>
    public const string NeedShowNoRightsHintParamName = "NeedShowNoRightsHint";
    
    /// <summary>
    /// Имя параметра "Имеется хотя бы одна отправка контрагенту".
    /// </summary>
    public const string IsSendingToCounterpartyEnabledInSchemeParamName = "IsSendingToCounterpartyEnabledInScheme";
    
    /// <summary>
    /// Имя параметра "Имеется хотя бы одно рассмотрение документа в схеме задачи".
    /// </summary>
    public const string HasAnyDocumentReviewInSchemeParamName = "HasAnyDocumentReviewInScheme";
    
    /// <summary>
    /// Guid группы вложений "Документ" задачи на согласование документа по процессу.
    /// </summary>
    public static readonly Guid DocumentFlowTaskOfficialDocumentGroupGuid = Guid.Parse("4195347f-2ca3-4fdc-9460-c22609cc3abf");
    
    /// <summary>
    /// Коды справки для действий по продлению срока задания.
    /// </summary>
    public static class HelpCodes
    {
      // Диалог продления срока задания на доработку.
      public const string DeadlineExtensionDialog = "Sungero_DocflowApproval_DeadlineExtensionDialog";
    }
    
    public static class CompleteWithoutSend
    {
      public const string Complete = "CompleteWithoutSend.Complete";
      public const string Cancel = "CompleteWithoutSend.Cancel";
      public const string Send = "CompleteWithoutSend.Send";
    }

    /// <summary>
    /// Константы инициализации модуля.
    /// </summary>
    public static class Init
    {
      public static class DocflowApproval
      {
        /// <summary>
        /// Название параметра для хранения проинициализированной версии кода модуля.
        /// </summary>
        public const string Name = "InitDocflowApprovalUpdate";
        
        public const string FirstInitVersion = "4.8.0.0";
        public const string Version49 = "4.9.0.0";
        public const string Version410 = "4.10.0.0";
      }
    }
    
    public static class Initialize
    {
      public static readonly Guid CaoComputedRoleExternalEntityId = Guid.Parse("DCCC3734-861D-4108-831A-6AA49E0A2739");
      public static readonly Guid CaoComputedRoleUuid = Guid.Parse("4BC4C3E6-6E4A-483A-8E1F-DD5052070325");
      
      /// <summary>
      /// Guid типа Docflow.MailDeliveryMethod.
      /// </summary>
      public static readonly Guid MailDeliveryMethodTypeGuid = Guid.Parse("276D7E4A-EA11-4740-AF17-893ABC6BC6E9");
      
      /// <summary>
      /// Уникальный идентификатор для ExternalLink способа доставки (Docflow.MailDeliveryMethod) Exchange.
      /// </summary>
      public static readonly Guid ExchangeDeliveryMethodSid = Guid.Parse("267C030C-A93A-44D8-BA60-17D8B56AD9C8");
      
      /// <summary>
      /// Уникальный идентификатор для ExternalLink способа доставки (Docflow.MailDeliveryMethod) Email.
      /// </summary>
      public static readonly Guid EmailDeliveryMethodSid = Guid.Parse("CEAD7532-08BA-4A87-81D1-FF622C9E65B1");
      
      /// <summary>
      /// Guid типа Projects.ProjectDocument.
      /// </summary>
      public static readonly Guid ProjectDocumentTypeGuid = Guid.Parse("56DF80B3-A795-4378-ACE5-C20A2B1FB6D9");
      
      /// <summary>
      /// Уникальный идентификатор для ExternalLink типа документа (Docflow.DocumentType) ProjectDocument.
      /// </summary>
      public static readonly Guid ProjectDocumentExternalEntityId = Guid.Parse("50C5885D-00D3-4D7F-AFD0-6C0EE956E347");
      
      /// <summary>
      /// Guid типа RecordManagement.OutgoingLetter.
      /// </summary>
      public static readonly Guid OutgoingLetterTypeGuid = Guid.Parse("D1D2A452-7732-4BA8-B199-0A4DC78898AC");
      
      /// <summary>
      /// Уникальный идентификатор для ExternalLink типа документа (Docflow.DocumentType) OutgoingLetter.
      /// </summary>
      public static readonly Guid OutgoingLetterExternalEntityId = Guid.Parse("4DC32A72-1617-469C-A969-B669EB54C7E3");
      
      /// <summary>
      /// Guid типа FinancialArchive.UniversalTransferDocument.
      /// </summary>
      public static readonly Guid UniversalTransferDocumentTypeGuid = Guid.Parse("58986E23-2B0A-4082-AF37-BD1991BC6F7E");
      
      /// <summary>
      /// Уникальный идентификатор для ExternalLink типа документа (Docflow.DocumentType) UniversalTransferDocument.
      /// </summary>
      public static readonly Guid UniversalTransferDocumentExternalEntityId = Guid.Parse("2A926B12-136C-4D39-8E00-927CC1924D32");
      
      /// <summary>
      /// Guid типа FinancialArchive.ContractStatement.
      /// </summary>
      public static readonly Guid ContractStatementTypeGuid = Guid.Parse("F2F5774D-5CA3-4725-B31D-AC618F6B8850");
      
      /// <summary>
      /// Уникальный идентификатор для ExternalLink типа документа (Docflow.DocumentType) ContractStatement.
      /// </summary>
      public static readonly Guid ContractStatementExternalEntityId = Guid.Parse("4F2EFBA4-D49C-480F-8793-1235127F164F");
      
      /// <summary>
      /// Guid типа FinancialArchive.Waybill.
      /// </summary>
      public static readonly Guid WaybillTypeGuid = Guid.Parse("4E81F9CA-B95A-4FD4-BF76-EA7176C215A7");
      
      /// <summary>
      /// Уникальный идентификатор для ExternalLink типа документа (Docflow.DocumentType) Waybill.
      /// </summary>
      public static readonly Guid WaybillExternalEntityId = Guid.Parse("BF9FA017-F863-4B7C-A8AD-A5762AE29113");
      
      /// <summary>
      /// Guid типа Exchange.CancellationAgreement.
      /// </summary>
      public static readonly Guid CancellationAgreementTypeGuid = Guid.Parse("4C57F798-1547-4DE0-B240-D9D97901DF5F");
      
      /// <summary>
      /// Уникальный идентификатор для ExternalLink типа документа (Docflow.DocumentType) CancellationAgreement.
      /// </summary>
      public static readonly Guid CancellationAgreementExternalEntityId = Guid.Parse("F2749BAB-AFEC-4384-8E31-CFFE86DAD3D5");
      
      /// <summary>
      /// Guid типа Docflow.Memo.
      /// </summary>
      public static readonly Guid MemoTypeGuid = Guid.Parse("95AF409B-83FE-4697-A805-5A86CEEC33F5");
      
      /// <summary>
      /// Уникальный идентификатор для ExternalLink типа документа (Docflow.DocumentType) Memo.
      /// </summary>
      public static readonly Guid MemoExternalEntityId = Guid.Parse("72BF621F-B87A-4617-BA8F-A26985631D7E");
      
      /// <summary>
      /// Guid типа Contracts.IncomingInvoice.
      /// </summary>
      public static readonly Guid IncomingInvoiceTypeGuid = Guid.Parse("A523A263-BC00-40F9-810D-F582BAE2205D");
      
      /// <summary>
      /// Уникальный идентификатор для ExternalLink типа документа (Docflow.DocumentType) IncomingInvoice.
      /// </summary>
      public static readonly Guid IncomingInvoiceExternalEntityId = Guid.Parse("EE92EDB7-EED1-4F06-AEE9-15A51AD09B82");
      
      /// <summary>
      /// Guid типа Contracts.Contract.
      /// </summary>
      public static readonly Guid ContractTypeGuid = Guid.Parse("F37C7E63-B134-4446-9B5B-F8811F6C9666");
      
      /// <summary>
      /// Уникальный идентификатор для ExternalLink типа документа (Docflow.DocumentType) Contract.
      /// </summary>
      public static readonly Guid ContractExternalEntityId = Guid.Parse("71FFAF45-26A9-4062-B74C-41E5E20BDEED");
      
      /// <summary>
      /// Guid типа Contracts.SupAgreement.
      /// </summary>
      public static readonly Guid SupAgreementTypeGuid = Guid.Parse("265F2C57-6A8A-4A15-833B-CA00E8047FA5");
      
      /// <summary>
      /// Уникальный идентификатор для ExternalLink типа документа (Docflow.DocumentType) SupAgreement.
      /// </summary>
      public static readonly Guid SupAgreementExternalEntityId = Guid.Parse("EFE420E7-7E47-430A-9671-BDE180C06484");
    }
    
  }
}