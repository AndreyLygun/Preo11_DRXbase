using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ExchangeDocumentInfoServiceDocuments;
using DcxClient = NpoComputer.DCX.ClientApi.Client;

namespace Sungero.Exchange.Server
{
  public class ModuleJobs
  {
    /// <summary>
    /// Отправка подписанных ИОП.
    /// </summary>
    public virtual void SendSignedReceiptNotifications()
    {
      var boxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().ToList();
      foreach (var box in boxes)
      {
        SendSignedReceiptNotifications(box);
      }
    }

    /// <summary>
    /// Агент создания ИОП.
    /// </summary>
    public virtual void CreateReceiptNotifications()
    {
      var boxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().Where(x => x.CertificateReceiptNotifications != null).ToList();
      foreach (var box in boxes)
      {
        CreateReceiptNotifications(box);
      }
    }

    /// <summary>
    /// Агент создания задач на отправку извещений о получении документов.
    /// </summary>
    public virtual void SendReceiptNotificationTasks()
    {
      var boxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().Select(b => b.Id).ToList();
      foreach (var box in boxes)
      {
        SendReceiptNotificationTask(box);
      }
    }
    
    /// <summary>
    /// Реализация агента для конкретного ящика, чтобы можно было выполнить в транзакции.
    /// </summary>
    /// <param name="boxId">Id ящика.</param>
    private static void SendReceiptNotificationTask(long boxId)
    {
      var box = ExchangeCore.BusinessUnitBoxes.Get(boxId);
      var hasCertificate = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.CheckAllResponsibleCertificates(box, box.Responsible);
      if (!hasCertificate)
        Logger.DebugFormat("Can't start Receipt Notification Sending Task. No certificates for responsible");
      var documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotification(box, false);
      
      // Если отправить ИОПы нельзя, то новая задача не создается.
      var client = ExchangeCore.PublicFunctions.BusinessUnitBox.GetPublicClient(box) as NpoComputer.DCX.ClientApi.Client;
      var documentsToFix = new List<IExchangeDocumentInfo>();
      foreach (var documentInfo in documentInfos)
      {
        var canSendDeliveryConfirmation = true;
        try
        {
          canSendDeliveryConfirmation = client.CanSendDeliveryConfirmation(documentInfo.ServiceDocumentId, documentInfo.ServiceMessageId);
        }
        catch (Exception ex)
        {
          Logger.DebugFormat("Error while getting document from the service to generate delivery confirmation: {0}. ServiceMessageId: {1}, ServiceDocumentId: {2}",
                             ex.Message, documentInfo.ServiceDocumentId, documentInfo.ServiceMessageId);
        }
        if (!canSendDeliveryConfirmation)
          documentsToFix.Add(documentInfo);
      }

      if (documentsToFix.Any())
      {
        foreach (var info in documentsToFix)
          Transactions.Execute(() =>
                               {
                                 var exchangeInfo = ExchangeDocumentInfos.Get(info.Id);
                                 Functions.Module.FixReceiptNotification(exchangeInfo, string.Empty, false);
                               });
      }
      
      var tasks = ReceiptNotificationSendingTasks.GetAll()
        .Where(x => Equals(x.Box, box) && Equals(x.Status, Exchange.ReceiptNotificationSendingTask.Status.InProcess));
      foreach (var task in tasks)
        try
      {
        task.Abort();
        Logger.DebugFormat("Aborted Receipt Notification Sending Task {0} for box {1}", task.Id, box.Id);
      }
      catch (Exception ex)
      {
        Logger.DebugFormat("Abort task {0} failed, box {1}, exception \r\n {2}", task.Id, box.Id, ex);
      }
      
      var responsible = box.Responsible;
      documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotification(box, false);
      var previousDay = Calendar.Today.PreviousWorkingDay().EndOfDay();
      var previousDayDocumentInfos = documentInfos.Where(x => x.MessageDate <= previousDay).ToList();
      if (previousDayDocumentInfos.Any() && hasCertificate)
      {
        Logger.DebugFormat("Document infos ids without receipt notification: {0}",  string.Join(", ", previousDayDocumentInfos.Select(x => x.Id)));
        
        // Выдать права на чтение документам. Без прав ИОП не отправить.
        foreach (var documentInfo in documentInfos)
        {
          var document = documentInfo.Document;
          if (!document.AccessRights.CanRead(responsible))
          {
            document.AccessRights.Grant(responsible, DefaultAccessRightsTypes.Read);
            document.Save();
          }
        }
        
        var receiptNotificationSendingTask = Functions.Module.CreateReceiptNotificationSendingTask(box);
        receiptNotificationSendingTask.Start();
        Logger.DebugFormat("Started Receipt Notification Sending Task {0} for box {1}", receiptNotificationSendingTask.Id, box.Id);
      }
    }
    
    /// <summary>
    /// Агент получения сообщений.
    /// </summary>
    public virtual void GetMessages()
    {
      Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Execute job GetLiteMessages. Queue items count: '{0}'.",
                                                                   ExchangeCore.MessageQueueItems.GetAll(q => q.DownloadSession == null).Count()));
      
      var boxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().ToList();
      foreach (var box in boxes)
      {
        Functions.Module.SyncLiteMessages(box);
      }
      
      Exchange.PublicFunctions.Module.LogDebugFormat("Job GetLiteMessages. Run exchange checkup.");
      foreach (var box in boxes)
      {
        Functions.Module.RunExchangeCheckup(box);
      }
      
      Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Done job GetLiteMessages. Queue items count: '{0}'.",
                                                                   ExchangeCore.MessageQueueItems.GetAll(q => q.DownloadSession == null).Count()));
    }
    
    /// <summary>
    /// Агент получения исторических сообщений.
    /// </summary>
    public virtual void GetHistoricalMessages()
    {
      Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Execute job GetHistoricalMessages. Queue items count: '{0}'.",
                                                                   ExchangeCore.MessageQueueItems.GetAll(q => q.DownloadSession != null).Count()));
      
      var boxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().ToList();
      foreach (var box in boxes)
      {
        Exchange.Functions.Module.SyncLiteHistoricalMessages(box);
      }
      
      Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Done job GetHistoricalMessages. Queue items count: '{0}'.",
                                                                   ExchangeCore.MessageQueueItems.GetAll(q => q.DownloadSession != null).Count()));
    }

    /// <summary>
    /// Агент конвертации тел документов.
    /// </summary>
    public virtual void BodyConverterJob()
    {
      Exchange.PublicFunctions.Module.LogDebugFormat("BodyConverterJob. Start.");
      var queueItems = this.GetNotProcessingBodyConverterQueueItems();
      Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("BodyConverterJob. Queue items count: {0}.", queueItems.Count));
      
      var queueItemsForDelete = new List<long>();
      
      foreach (var queueItem in queueItems)
      {
        if (ExchangeCore.PublicFunctions.BodyConverterQueueItem.IsObsoleteQueueItem(queueItem))
        {
          Exchange.PublicFunctions.Module.LogDebugFormat(queueItem, string.Format("BodyConverterJob. Queue item is obsolete."));
          queueItemsForDelete.Add(queueItem.Id);
        }
        else if (Sungero.ExchangeCore.PublicFunctions.BodyConverterQueueItem.HasSimilarQueueItemInProcessing(queueItem))
          Exchange.PublicFunctions.Module.LogDebugFormat(queueItem, string.Format("BodyConverterJob. Found similiar queue item in processing: DocumentId: {0} VersionId: {1}.", queueItem.Document.Id, queueItem.VersionId));
        else
          Functions.Module.ExecuteConvertDocumentToPdfAsyncHandler(queueItem);
      }
      
      this.ClearBodyConverterQueueItems(queueItemsForDelete);
      
      Exchange.PublicFunctions.Module.LogDebugFormat("BodyConverterJob. Done.");
    }
    
    /// <summary>
    /// Получить элементы очереди конвертации, по которым не запущены асинхронные обработчики.
    /// </summary>
    /// <returns>Список элементов очереди ковертации.</returns>
    public virtual List<ExchangeCore.IBodyConverterQueueItem> GetNotProcessingBodyConverterQueueItems()
    {
      return ExchangeCore.BodyConverterQueueItems.GetAll()
        .Where(x => x.ProcessingStatus != ExchangeCore.BodyConverterQueueItem.ProcessingStatus.Processed && (x.AsyncHandlerId == null || x.AsyncHandlerId == string.Empty))
        .ToList();
    }
    
    /// <summary>
    /// Удалить элементы очереди конвертации.
    /// </summary>
    /// <param name="queueItemIds">Ид элементов очереди.</param>
    public virtual void ClearBodyConverterQueueItems(List<long> queueItemIds)
    {
      var queueItemsForDelete = ExchangeCore.BodyConverterQueueItems.GetAll().Where(x => queueItemIds.Contains(x.Id)).ToList();
      
      foreach (var queueItem in queueItemsForDelete)
      {
        Transactions.Execute(
          () =>
          {
            ExchangeCore.BodyConverterQueueItems.Delete(queueItem);
          });
      }
    }

    /// <summary>
    /// Реализация агента создания ИОП для конкретного ящика.
    /// </summary>
    /// <param name="box">Абонентский ящик нашей организации.</param>
    private static void CreateReceiptNotifications(Sungero.ExchangeCore.IBusinessUnitBox box)
    {
      Exchange.PublicFunctions.Module.LogDebugFormat(box, "Execute CreateReceiptNotifications.");
      var partSize = 25;
      var skip = 0;
      var certificate = box.CertificateReceiptNotifications;
      var documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotificationPart(box, skip, partSize, true);
      if (!documentInfos.Any())
        return;
      
      while (documentInfos.Any())
      {
        try
        {
          var serviceDocs = Functions.Module.GetGeneratedDeliveryConfirmationDocuments(documentInfos, box, box.CertificateReceiptNotifications, true);
          
          foreach (var doc in serviceDocs)
          {
            var info = doc.Info;
            if (info.ServiceDocuments.Any(d => d.DocumentType == doc.ReglamentDocumentType && d.ParentDocumentId == doc.ParentDocumentId))
              continue;
            
            var serviceDocument = info.ServiceDocuments.AddNew();
            serviceDocument.DocumentType = doc.ReglamentDocumentType;
            serviceDocument.Body = doc.Content;
            serviceDocument.GeneratedName = doc.Name;
            serviceDocument.DocumentId = doc.ServiceDocumentId;
            serviceDocument.StageId = doc.ServiceDocumentStageId;
            serviceDocument.Certificate = doc.Certificate;
            serviceDocument.ParentDocumentId = doc.ParentDocumentId;
            
            // Выдать права на документ подписывающему ИОП.
            var document = info.Document;
            if (!document.AccessRights.CanRead(certificate.Owner))
            {
              document.AccessRights.Grant(certificate.Owner, DefaultAccessRightsTypes.Read);
              document.Save();
            }

            var isReceiptOnSellerTitle = doc.ParentDocumentId == info.ServiceDocumentId;
            var isReceiptOnBuyerTitle = doc.ParentDocumentId == info.ExternalBuyerTitleId;
            if (isReceiptOnSellerTitle)
              info.DeliveryConfirmationStatus = Exchange.ExchangeDocumentInfo.DeliveryConfirmationStatus.Generated;
            if (isReceiptOnBuyerTitle)
              info.BuyerDeliveryConfirmationStatus = Exchange.ExchangeDocumentInfo.BuyerDeliveryConfirmationStatus.Generated;
            
            info.Save();
          }
          
          var documentsToFix = documentInfos.Where(x => !serviceDocs.Any(s => Equals(s.LinkedDocument, x.Document))).ToList();
          if (documentsToFix.Any())
            Functions.Module.FixReceiptNotification(documentsToFix, string.Empty);
        }
        catch (Exception ex)
        {
          var error = Resources.DeliveryConfirmationError;
          Exchange.PublicFunctions.Module.LogErrorFormat(error, ex);
        }
        finally
        {
          skip += partSize;
          documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotificationPart(box, skip, partSize, true);
        }
      }

    }
    
    /// <summary>
    /// Реализация отправки подписанных ИОП для конкретного ящика.
    /// </summary>
    /// <param name="box">Абонентский ящик нашей организации.</param>
    private static void SendSignedReceiptNotifications(Sungero.ExchangeCore.IBusinessUnitBox box)
    {
      Exchange.PublicFunctions.Module.LogDebugFormat(box, "Execute SendSignedReceiptNotifications.");
      var partSize = 25;
      var skip = 0;
      var documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotificationPart(box, skip, partSize, false);
      if (!documentInfos.Any())
        return;
      
      while (documentInfos.Any())
      {
        try
        {
          var documentsToSend = new List<Structures.Module.ReglamentDocumentWithCertificate>();
          
          foreach (var info in documentInfos)
          {
            Func<Enumeration?, bool> isRootDocumentReceipt = x => x == DocumentType.Receipt || x == DocumentType.IReceipt;
            var isInvoiceFlow = Functions.Module.IsInvoiceFlowDocument(info.Document);
            var reglamentDocument = info.ServiceDocuments
              .Where(d => d.Sign != null && d.Date == null)
              .Select(d =>
                      {
                        var parentId = d.ParentDocumentId == info.ExternalBuyerTitleId ? info.ExternalBuyerTitleId : info.ServiceDocumentId;
                        var counterpartyId = info.ServiceCounterpartyId;

                        return Structures.Module.ReglamentDocumentWithCertificate.Create(d.GeneratedName, d.Body, d.Certificate,
                                                                                         d.Sign, parentId, box, info.Document,
                                                                                         info.ServiceMessageId, d.DocumentId, d.StageId,
                                                                                         counterpartyId,
                                                                                         isRootDocumentReceipt(d.DocumentType), info,
                                                                                         isInvoiceFlow, d.DocumentType,
                                                                                         d.FormalizedPoAUnifiedRegNo);
                      })
              .ToList();
            documentsToSend.AddRange(reglamentDocument);
          }
          if (documentsToSend.Any())
          {
            var processingDocumentInfos = string.Join(", ", documentsToSend.Select(d => d.Info.Id.ToString()).ToList());
            var sendLog = string.Format("Execute SendSignedReceiptNotifications. Processing document infos: {0}", processingDocumentInfos);
            Exchange.PublicFunctions.Module.LogDebugFormat(box, sendLog);
            Sungero.Exchange.Functions.Module.SendDeliveryConfirmation(documentsToSend, box);
          }
        }
        catch (Exception ex)
        {
          var error = Resources.DeliveryConfirmationError;
          Exchange.PublicFunctions.Module.LogErrorFormat(error, ex);
        }
        finally
        {
          skip += partSize;
          documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotificationPart(box, skip, partSize, false);
        }
      }
    }
  }
}