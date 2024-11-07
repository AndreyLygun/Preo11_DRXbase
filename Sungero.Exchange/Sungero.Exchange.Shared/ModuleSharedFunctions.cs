using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore;

namespace Sungero.Exchange.Shared
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Отправить уведомление об уточнении документа.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="receiver">Получатель.</param>
    /// <param name="note">Комментарий.</param>
    /// <param name="throwError">Не гасить ошибку.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="certificate">Сертификат для подписания УОУ.</param>
    /// <param name="isInvoiceAmendmentRequest">True для УОУ, False для отказа.</param>
    /// <returns>Строка с ошибкой отправки уведомления. Пусто - если отправка успешная.</returns>
    [Public]
    public static string GenerateAndSendAmendmentRequest(List<Docflow.IOfficialDocument> documents, Parties.ICounterparty receiver, string note, bool throwError,
                                                         ExchangeCore.IBoxBase box, ICertificate certificate, bool isInvoiceAmendmentRequest)
    {
      if (!documents.Any())
        return string.Empty;

      var error = Resources.AmendmentRequestError;
      var serviceDocs = new List<Structures.Module.ReglamentDocumentWithCertificate>();
      
      try
      {
        serviceDocs.AddRange(Functions.Module.Remote.GenerateAmendmentRequestDocuments(documents.ToList(), box, note, throwError, certificate, isInvoiceAmendmentRequest));
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat(error, ex);
        return ex.Message;
      }
      
      if (!serviceDocs.Any())
        return Resources.AllAnswersIsAlreadySent;
      
      try
      {
        var signableDocuments = PrepareReglamentDocumentsForSigning(serviceDocs);
        var signs = ExternalSignatures.Sign(certificate, signableDocuments);
        
        // Проверяем что в словарь подписей записалось тело сертификата, потому что если у пользователя на компьютере не установлен переданный методу ExternalSignatures.Sign() сертификат,
        // то он запишет тело как null.
        if (signs.Any(sign => sign.Value == null || sign.Value.Length == 0))
          return Resources.DocumentEndorseError;
        
        foreach (var doc in serviceDocs)
          doc.Signature = signs[doc.ParentDocumentId];
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat(error, ex);
        return Resources.DocumentEndorseError;
      }

      try
      {
        var serviceCounterpartyId = string.Empty;
        var externalDocumentInfo = Functions.ExchangeDocumentInfo.Remote.GetIncomingExDocumentInfo(documents.FirstOrDefault());
        if (externalDocumentInfo != null)
          serviceCounterpartyId = externalDocumentInfo.ServiceCounterpartyId;
        
        Functions.Module.Remote.SendAmendmentRequest(serviceDocs, receiver, box, note);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat(error, ex);
        return string.Format("{0}: {1}", error, ex.Message.ToString().ToLower());
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Проверить, относится ли документ к счетам-фактурам или УПД.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Признак того, является ли документ счетом-фактурой или УПД.</returns>
    /// <remarks>По возможности надо пользоваться сервисными признаками, для накладных из Диадока - врёт.</remarks>
    public static bool IsInvoiceFlowDocument(Docflow.IOfficialDocument document)
    {
      return FinancialArchive.UniversalTransferDocuments.Is(document) ||
        FinancialArchive.IncomingTaxInvoices.Is(document) ||
        FinancialArchive.OutgoingTaxInvoices.Is(document);
    }
    
    /// <summary>
    /// Проверка, есть ли у текущего пользователя сертификат сервиса обмена.
    /// </summary>
    /// <param name="businessUnitBox">Абонентский ящик нашей организации.</param>
    /// <returns>True, если есть, иначе False.</returns>
    public virtual bool HasCurrentUserExchangeServiceCertificate(IBusinessUnitBox businessUnitBox)
    {
      // Получить доступные сертификаты.
      var availableCertificates = Functions.Module.Remote.GetCertificates(Users.Current).AsEnumerable();
      // Проверить наличие сертификатов ответственного, если сервис предоставляет такую возможность.
      if (businessUnitBox.HasExchangeServiceCertificates == true)
        availableCertificates = availableCertificates.Where(x => businessUnitBox.ExchangeServiceCertificates.Any(z => z.Certificate.Equals(x)));
      
      return availableCertificates.Any();
    }

    /// <summary>
    /// Проверить, является ли сервис абонентского ящика сервисом СБИС.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <returns>True - если является, иначе - false.</returns>
    [Public]
    public static bool IsSbisExchangeService(Sungero.ExchangeCore.IBusinessUnitBox box)
    {
      if (box == null)
        return false;
      
      return ExchangeCore.ExchangeService.ExchangeProvider.Sbis == box.ExchangeService.ExchangeProvider;
    }
    
    /// <summary>
    /// Сгенерировать и отправить извещения о получении документа.
    /// </summary>
    /// <param name="rootBox">Абонентский ящик.</param>
    /// <param name="certificate">Сертификат для подписания ИОПов.</param>
    /// <param name="documentInfos">Список информации о документах, для которых требуется отправить ИОП.</param>
    /// <param name="bulkMode">Режим для большой нагрузки.
    /// Если true - будут выполняться генерации ИОП в сервисе обмена и подписываться все доступные ИОП-ы.
    /// Если false - только одна пачка ИОП будет подписана, если совсем нечего подписывать - будет сгенерирована.</param>
    /// <param name="partSize">Размер порций сообщений, для которых требуется отправить ИОП.</param>
    /// <returns>Строка с ошибкой отправки извещения. Пусто - если отправка успешная.</returns>
    [Public]
    public virtual string GenerateAndSendDeliveryConfirmation(IBusinessUnitBox rootBox,
                                                              ICertificate certificate, List<IExchangeDocumentInfo> documentInfos,
                                                              bool bulkMode, int partSize)
    {
      if (!documentInfos.Any())
        return string.Empty;
      
      if (certificate == null)
      {
        var error = Resources.CertificateNotFound;
        Logger.ErrorFormat(error);
        return error;
      }
      
      var isJobSendReceiptEnabled = PublicFunctions.Module.Remote.IsJobEnabled(PublicConstants.Module.SendSignedReceiptNotificationsId);
      var isJobCreateReceiptEnabled = PublicFunctions.Module.Remote.IsJobEnabled(Constants.Module.CreateReceiptNotifications);
      
      // Если в системе настроена автоматическая схема работы с иопами, и выбранный сертификат не попадает под неё - не делаем ничего.
      if (!bulkMode &&
          rootBox.CertificateReceiptNotifications != null &&
          !Equals(rootBox.CertificateReceiptNotifications, certificate) &&
          isJobCreateReceiptEnabled)
        return string.Empty;
      
      var skip = 0;
      
      while (documentInfos.Any())
      {
        // Если bulkMode выключен - разрешаем только один прогон.
        if (!bulkMode && skip >= partSize)
          break;
        
        var serviceDocs = new List<Structures.Module.ReglamentDocumentWithCertificate>();
        var error = Resources.DeliveryConfirmationError;
        try
        {
          serviceDocs = Functions.Module.Remote.GetGeneratedDeliveryConfirmationDocuments(documentInfos, rootBox, certificate, bulkMode);
          
          // Если снаружи пришел параметр, что генерировать не надо, но сгенерированных совсем нет - генерируем хотя бы одну пачку ИОП.
          // Так на небольших объемах спасаемся от лишней задачки на отправку ИОП.
          if (!bulkMode && (!serviceDocs.Any() || !isJobCreateReceiptEnabled))
            serviceDocs = Functions.Module.Remote.GetGeneratedDeliveryConfirmationDocuments(documentInfos, rootBox, certificate, true);

          // Проставляем NotRequired в двух случаях:
          // 1. Это массовая обработка и ИОПы не сгенерировались.
          // 2. Это единичная обработка, ФП отключен, а ИОПы пытались генерироваться, но не сгенерировались.
          var documentsToFix = documentInfos.Where(x => !serviceDocs.Any(s => Equals(s.LinkedDocument, x.Document))).ToList();
          if (documentsToFix.Any() && (bulkMode == true || !bulkMode && !isJobCreateReceiptEnabled))
            Functions.Module.Remote.FixReceiptNotification(documentsToFix, string.Empty);

          if (!serviceDocs.Any())
          {
            skip += partSize;
            documentInfos = Functions.Module.Remote.GetDocumentInfosWithoutReceiptNotificationPart(rootBox, skip, partSize, false);
            continue;
          }
          var documentsToSign = serviceDocs.Where(d => d.Signature == null).ToList();

          try
          {
            Logger.DebugFormat("Try sign {0} documents", documentsToSign.Count());

            var signableDocuments = PrepareReglamentDocumentsForSigning(documentsToSign);
            var signs = ExternalSignatures.Sign(certificate, signableDocuments);
            // Проверяем что в словарь подписей записалось тело сертификата, потому что если у пользователя на компьютере не установлен переданный методу ExternalSignatures.Sign() сертификат,
            // то он запишет тело как null.
            if (signs.Any(sign => sign.Value == null || sign.Value.Length == 0))
              return Resources.DocumentEndorseError;
            
            Logger.DebugFormat("Sign {0} documents", signs.Count());
            foreach (var document in documentsToSign)
            {
              Logger.DebugFormat("Get signatory for parent document id {0}", document.ParentDocumentId);
              document.Signature = signs[document.ParentDocumentId];
              var formalizedPoAUnifiedRegNo = Docflow.PublicFunctions.OfficialDocument.Remote.GetFormalizedPoAUnifiedRegNo(document.LinkedDocument, Company.Employees.Current, certificate);
              document.FormalizedPoAUnifiedRegNumber = formalizedPoAUnifiedRegNo;
              Logger.Debug(string.Format("Sign receipt notification with ExchangeDocumentInfoId = {0}, DocumentType = {1}, ServiceCounterpartyId = {2}," +
                                         " ParentDocumentId = {3} LinkedDocumentId = {4}, ServiceMessageId = {5}, FormalizedPoAUnifiedRegNo = {6}",
                                         document.Info.Id, document.ReglamentDocumentType, document.ServiceCounterpartyId, document.ParentDocumentId,
                                         document.LinkedDocument.Id, document.ServiceMessageId, formalizedPoAUnifiedRegNo));
            }

            if (isJobSendReceiptEnabled)
              Functions.Module.Remote.SaveDeliveryConfirmationSigns(documentsToSign);
          }
          catch (Sungero.Domain.Shared.Exceptions.EntitySigningException ex)
          {
            Logger.ErrorFormat(error, ex);
            return ex.Message;
          }
          catch (Exception ex)
          {
            Logger.ErrorFormat(error, ex);
            return Resources.DocumentEndorseError;
          }
          
          if (!isJobSendReceiptEnabled)
            Functions.Module.Remote.SendDeliveryConfirmation(serviceDocs, rootBox);
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat(error, ex);
          return string.Format("{0}: {1}", error, ex.Message.ToString().ToLower());
        }

        if (Functions.Module.Remote.IsReglamentDocumentsNotSent(documentInfos))
          skip += partSize;
        documentInfos = Functions.Module.Remote.GetDocumentInfosWithoutReceiptNotificationPart(rootBox, skip, partSize, false);
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Преобразовать сервисные документы к виду, пригодному для подписания.
    /// </summary>
    /// <param name="documents">Сервисные документы.</param>
    /// <returns>Словарь тел сервисных документов, где ключом является ИД родительского документа.</returns>
    public static System.Collections.Generic.Dictionary<string, byte[]> PrepareReglamentDocumentsForSigning(List<Structures.Module.ReglamentDocumentWithCertificate> documents)
    {
      return RemoveReglamentDocumentDuplicates(documents).ToDictionary(d => d.ParentDocumentId, d => d.Content);
    }
    
    /// <summary>
    /// Убрать дубли сервисных документов.
    /// </summary>
    /// <param name="documents">Сервисные документы.</param>
    /// <returns>Сервисные документы с уникальными ИД родительского документа.</returns>
    private static List<Structures.Module.ReglamentDocumentWithCertificate> RemoveReglamentDocumentDuplicates(List<Structures.Module.ReglamentDocumentWithCertificate> documents)
    {
      var groupedDocuments = documents.GroupBy(d => d.ParentDocumentId);
      LogReglamentDocumentDuplicates(groupedDocuments);
      
      return groupedDocuments.Select(g => g.LastOrDefault(d => d.Content != null && d.Content.Length > 0)).Where(d => d != null).ToList();
    }

    /// <summary>
    /// Залогировать дубли сервисных документов.
    /// </summary>
    /// <param name="documentGroups">Сгруппированные по ключу сервисные документы.</param>
    private static void LogReglamentDocumentDuplicates(IEnumerable<IGrouping<string, Structures.Module.ReglamentDocumentWithCertificate>> documentGroups)
    {
      foreach (var reglamentDocuments in documentGroups.Where(g => g.Count() > 1))
      {
        var reglamentDocument = reglamentDocuments.First();
        Logger.DebugFormat("PrepareReglamentDocumentsForSigning. {0} duplicates detected. ParentDocumentId = {1}, ServiceMessageId = {2}, ReglamentDocumentType = {3}.",
                           reglamentDocuments.Count(), reglamentDocument.ParentDocumentId, reglamentDocument.ServiceMessageId, reglamentDocument.ReglamentDocumentType);
      }
    }
  }
}