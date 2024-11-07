using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Exchange.CancellationAgreement;
using Sungero.ExchangeCore;

namespace Sungero.Exchange.Server
{
  partial class CancellationAgreementFunctions
  {
    /// <summary>
    /// Сформировать XML-тело соглашения об аннулировании.
    /// </summary>
    /// <param name="reason">Причина аннулирования.</param>
    /// <returns>Сообщение об ошибке: если заполнено - при формировании были ошибки.</returns>
    [Public]
    public virtual string GenerateCancellationAgreementBody(string reason)
    {
      var info = Functions.ExchangeDocumentInfo.GetLastDocumentInfo(_obj.LeadingDocument);
      var box = BusinessUnitBoxes.As(info.RootBox);
      byte[] documentContent;
      
      var certificate = Functions.Module.GetExchangeCertificatesForEmployee(box, _obj.OurSignatory)
        .OrderByDescending(c => c.NotAfter)
        .FirstOrDefault();
      if (certificate == null)
        return Exchange.Resources.OurSignatoryHasNoCertificate;
      
      var thumbprint = certificate.Thumbprint;
      var messageId = info.ServiceMessageId;
      var documentId = info.ServiceDocumentId;
      
      try
      {
        var client = ExchangeCore.PublicFunctions.BusinessUnitBox.GetPublicClient(box) as NpoComputer.DCX.ClientApi.Client;
        var document = client.GenerateRevocationOffer(documentId, reason, messageId, thumbprint);
        _obj.ServiceDocumentId = document.ServiceEntityId;
        documentContent = document.Content;

        this.CreateCancellationAgreementVersion(documentContent);
      }
      catch (Exception ex)
      {
        Functions.Module.LogErrorFormat("Generating cancellation agreement body failed.", ex);
        return Sungero.Docflow.OfficialDocuments.Resources.CancellationAgreementCreationFailed;
      }

      return string.Empty;
    }

    /// <summary>
    /// Создать версию соглашения об аннулировании.
    /// </summary>
    /// <param name="body">Тело версии.</param>
    [Public]
    public virtual void CreateCancellationAgreementVersion(byte[] body)
    {
      _obj.Versions.AddNew();
      var byteArray = Docflow.Structures.Module.ByteArray.Create(body);
      Docflow.PublicFunctions.OfficialDocument.WriteBytesToDocumentLastVersionBody(_obj, byteArray, Constants.Module.XmlExtension);
      _obj.Save();
    }
    
    /// <summary>
    /// Отправить документ в сервис обмена.
    /// </summary>
    /// <param name="addenda">Приложения.</param>
    /// <param name="receiver">Получатель (головная организация или филиал контрагента).</param>
    /// <param name="receiverServiceDepartmentId">Внешний ИД подразделения контрагента.</param>
    /// <param name="senderBox">Абонентский ящик отправителя.</param>
    /// <param name="senderServiceDepartmentId">Внешний ИД подразделения абонентского ящика отправителя.</param>
    /// <param name="certificate">Сертификат, которым подписаны документы.</param>
    /// <param name="needSign">Требовать подписание от контрагента.</param>
    /// <param name="comment">Комментарий к сообщению в сервисе.</param>
    [Public]
    public override void SendDocuments(List<Sungero.Docflow.IOfficialDocument> addenda,
                                       Parties.ICounterparty receiver, string receiverServiceDepartmentId,
                                       ExchangeCore.IBusinessUnitBox senderBox, string senderServiceDepartmentId,
                                       ICertificate certificate, bool needSign, string comment)
    {
      if (Functions.Module.HasNotApprovedDocuments(_obj, addenda))
        throw AppliedCodeException.Create(Resources.SendCounterpartyNotApproved);
      
      var parentInfo = Functions.ExchangeDocumentInfo.GetExchangeDocumentInfo(_obj.LeadingDocument, senderBox);
      if (parentInfo == null)
      {
        Functions.Module.LogErrorFormat(string.Format("Parent info not found for cancellation agreement with id = {0}", _obj.Id));
        throw AppliedCodeException.Create(Resources.ErrorWhileSendingDocToCounterpartyDetailed);
      }
      
      // Создать из СоА документ обмена. СБИС требует вложения всех СоА пакета.
      var cancellationAgreements = new List<Sungero.Exchange.ICancellationAgreement>() { _obj };
      cancellationAgreements.AddRange(addenda.Cast<Sungero.Exchange.ICancellationAgreement>());
      var primaryDocuments = new List<NpoComputer.DCX.Common.IDocument>();
      var signatures = new List<NpoComputer.DCX.Common.Signature>();
      foreach (var cancellationAgreement in cancellationAgreements)
      {
        // "ИД документа в СО" может быть пуст после импорта. СБИС корректно обработает СоА с новым GUID.
        if (string.IsNullOrWhiteSpace(cancellationAgreement.ServiceDocumentId))
          cancellationAgreement.ServiceDocumentId = Guid.NewGuid().ToString();
        var serviceEntityId = cancellationAgreement.ServiceDocumentId;
        var parentDocumentInfo = Functions.ExchangeDocumentInfo.GetLastDocumentInfo(cancellationAgreement.LeadingDocument);
        var primaryDocument = Functions.Module.CreatePrimaryDocumentForCancellationAgreement(cancellationAgreement,
                                                                                             parentDocumentInfo.ServiceMessageId,
                                                                                             parentDocumentInfo.ServiceDocumentId,
                                                                                             serviceEntityId,
                                                                                             needSign,
                                                                                             cancellationAgreement.Reason);
        primaryDocuments.Add(primaryDocument);
        
        // Создать из подписи СоА подпись для документа обмена.
        var signature = Functions.Module.GetDocumentSignature(cancellationAgreement, certificate);
        var dcxSign = Functions.Module.CreateExchangeDocumentSignature(senderBox,
                                                                       serviceEntityId,
                                                                       signature.Body,
                                                                       signature.FormalizedPoAUnifiedRegNumber);
        signatures.Add(dcxSign);
      }
      
      // Отправить СоА в сервис.
      var client = Functions.Module.GetClient(senderBox);
      var message = Functions.Module.CreateMessage(primaryDocuments, new List<NpoComputer.DCX.Common.IReglamentDocument>(), signatures, client, receiver,
                                                   string.Empty, null, senderBox, null, parentInfo.ServiceMessageId);
      var sentMessage = client.SendRevocationOffer(message);
      
      // Постобработка.
      foreach (var documentServiceIds in sentMessage.DocumentIds)
      {
        var cancellationAgreement = cancellationAgreements.Where(x => x.ServiceDocumentId.ToString() == documentServiceIds.LocalId).Single();
        var parentDocumentInfo = Functions.ExchangeDocumentInfo.GetLastDocumentInfo(cancellationAgreement.LeadingDocument);
        this.ProcessCancellationAgreementAfterSendingToCounterparty(cancellationAgreement, parentDocumentInfo, receiver, senderBox, certificate, sentMessage, documentServiceIds);
      }
    }
    
    /// <summary>
    /// Обработать соглашение об аннулировании после отправки контрагенту.
    /// </summary>
    /// <param name="document">Соглашение об аннулировании.</param>
    /// <param name="parentInfo">Информация из сервиса обмена о ведущем документе.</param>
    /// <param name="receiver">Получатель (головная организация или филиал контрагента).</param>
    /// <param name="senderBox">Абонентский ящик отправителя.</param>
    /// <param name="certificate">Сертификат, которым подписаны документы.</param>
    /// <param name="sentMessage">Отправленное сообщение.</param>
    /// <param name="documentServiceIds">Сопоставление ИД RX и СО.</param>
    public virtual void ProcessCancellationAgreementAfterSendingToCounterparty(IOfficialDocument document,
                                                                               IExchangeDocumentInfo parentInfo,
                                                                               Parties.ICounterparty receiver,
                                                                               ExchangeCore.IBusinessUnitBox senderBox,
                                                                               ICertificate certificate,
                                                                               NpoComputer.DCX.Common.SentMessage sentMessage,
                                                                               NpoComputer.DCX.Common.DocumentIds documentServiceIds)
    {
      var client = Functions.Module.GetClient(senderBox);
      var isTwoSidedCancellationAgreement = client.IsTwoSidedRevocation(parentInfo.ServiceDocumentId, parentInfo.ServiceMessageId);
      
      // Создать для СоА сведения о документе обмена.
      var receiptStatus = Functions.Module.ResolveReceiptStatus(documentServiceIds.ReceiptStatus);
      var сancellationAgreementInfo = Functions.Module.SaveExternalDocumentInfo(document, documentServiceIds.ServiceId, sentMessage.ServiceMessageId,
                                                                                isTwoSidedCancellationAgreement, receiver, parentInfo.CounterpartyDepartmentBox, senderBox,
                                                                                receiptStatus);
      
      сancellationAgreementInfo.ParentDocumentInfo = parentInfo;
      var signature = Functions.Module.GetDocumentSignature(document, certificate);
      сancellationAgreementInfo.SenderSignId = signature.Id;
      var signStatus = isTwoSidedCancellationAgreement ? NpoComputer.DCX.Common.SignStatus.Waiting : NpoComputer.DCX.Common.SignStatus.None;
      сancellationAgreementInfo.ExchangeState = Functions.Module.GetExchangeStateBySignStatusForOutgoingDocumentInfo(signStatus);
      сancellationAgreementInfo.Save();
      
      Functions.Module.AddFPoaUnifiedRegNumberToSignatureData(document, signature, senderBox, sentMessage, documentServiceIds.LocalId);
      document.ExchangeState = Functions.Module.GetExchangeStateBySignStatusForOutgoingDocument(signStatus);
      Functions.Module.AddTrackingRecordInfo(сancellationAgreementInfo, document, signStatus != NpoComputer.DCX.Common.SignStatus.None);
      Functions.Module.WriteHistoryForOutgoingExchangeDocument(document, сancellationAgreementInfo, receiver, senderBox);
      var cancellationAgreement = CancellationAgreements.As(document);
      Functions.CancellationAgreement.UpdateLifeCycle(cancellationAgreement, document.RegistrationState, document.InternalApprovalState, document.ExternalApprovalState);
      cancellationAgreement.Save();
      
      var versionId = сancellationAgreementInfo.VersionId.Value;
      Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(document, versionId);
      Functions.Module.EnqueueXmlToPdfBodyConverter(document, versionId, document.ExchangeState);
    }
    
    /// <summary>
    /// Отправить ответ на соглашение об аннулировании в сервис обмена.
    /// </summary>
    /// <param name="senderBox">Абонентский ящик отправителя.</param>
    /// <param name="receiver">Получатель (головная организация или филиал контрагента).</param>
    /// <param name="certificate">Сертификат, которым подписаны документы.</param>
    /// <param name="isAgent">Признак вызова из фонового процесса. Иначе - пользователем в RX.</param>
    public override void SendAnswer(Sungero.ExchangeCore.IBusinessUnitBox senderBox, Sungero.Parties.ICounterparty receiver, ICertificate certificate, bool isAgent)
    {
      var client = Functions.Module.GetClient(senderBox);
      var documentInfo = Functions.ExchangeDocumentInfo.GetIncomingExDocumentInfo(_obj);
      var signature = Functions.Module.GetDocumentSignature(_obj, certificate);
      
      if (!Functions.Module.ValidateBeforeSendAnswer(_obj, documentInfo, signature, client))
        return;
      
      var serviceDocumentId = documentInfo.ServiceDocumentId;
      var dcxSign = Functions.Module.CreateExchangeDocumentSignature(senderBox, serviceDocumentId, signature.Body, signature.FormalizedPoAUnifiedRegNumber);

      try
      {
        
        var parentInfo = documentInfo.ParentDocumentInfo;
        var primaryDocuments = new List<NpoComputer.DCX.Common.IDocument>();
        
        if (senderBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis)
        {
          var primaryDocument = Functions.Module.CreatePrimaryDocumentForCancellationAgreement(_obj, parentInfo.ServiceMessageId,
                                                                                               parentInfo.ServiceDocumentId, documentInfo.ServiceDocumentId,
                                                                                               true, _obj.Reason);
          primaryDocuments.Add(primaryDocument);
        }
        
        var sentMessage = Functions.Module.SendMessage(primaryDocuments,
                                                       new List<NpoComputer.DCX.Common.IReglamentDocument>(),
                                                       new List<NpoComputer.DCX.Common.Signature>() { dcxSign },
                                                       client, receiver, documentInfo.ServiceCounterpartyId,
                                                       null, senderBox, null, parentInfo.ServiceMessageId);
        
        documentInfo.ReceiverSignId = signature.Id;
        documentInfo.Save();
        
        Functions.Module.AddFPoaUnifiedRegNumberToSignatureData(_obj, signature,
                                                                senderBox, sentMessage,
                                                                serviceDocumentId);
        
        if (isAgent)
        {
          Docflow.PublicFunctions.Module.GeneratePublicBodyForExchangeDocument(_obj, documentInfo.VersionId.Value, _obj.ExchangeState);
        }
        else
        {
          Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(_obj, documentInfo.VersionId.Value);
          Functions.Module.EnqueueXmlToPdfBodyConverter(_obj, documentInfo.VersionId.Value, Docflow.OfficialDocument.ExchangeState.Signed);
        }
        
        Functions.Module.LogDebugFormat(documentInfo, "Send answer to cancellation agreement.");
      }
      catch (Exception ex)
      {
        throw AppliedCodeException.Create(ex.Message, ex);
      }
    }
    
    /// <summary>
    /// Получить права подписи соглашения об аннулировании.
    /// </summary>
    /// <returns>Права подписи на соглашение об аннулировании и основной документ.</returns>
    public override IQueryable<Sungero.Docflow.ISignatureSetting> GetSignatureSettingsQuery()
    {
      var settingIds = Docflow.PublicFunctions.OfficialDocument.Remote.GetSignatureSettingsQuery(_obj.LeadingDocument).Select(s => s.Id).ToList();
      var cancellationAgreementSettingIds = base.GetSignatureSettingsQuery().Select(s => s.Id).ToList();
      
      settingIds.AddRange(cancellationAgreementSettingIds);
      return SignatureSettings.GetAll(s => settingIds.Contains(s.Id));
    }
  }
}