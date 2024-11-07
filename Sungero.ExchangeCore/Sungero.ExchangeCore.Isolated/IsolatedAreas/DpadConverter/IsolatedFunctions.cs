using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Sungero.Core;
using Sungero.ExchangeCore.Structures.Module;

namespace Sungero.ExchangeCore.Isolated.DpadConverter
{
  public class IsolatedFunctions
  {
    /// <summary>
    /// Проставить штамп с подписями на документ.
    /// </summary>
    /// <param name="stream">Документ, на который ставится штамп.</param>
    /// <param name="signatureStamp">Штамп.</param>
    /// <returns>Документ со штампом.</returns>
    [Public]
    public virtual Stream AddSignatureStamp(Stream stream, Structures.Module.IDpadSignatureStamp signatureStamp)
    {
      Logger.DebugFormat("Execute. AddSignatureStamp DocumentId: {0}.", signatureStamp.PageStampInfo.DocumentId);
      var dpadConverter = this.CreateDpadConverter();
      var dpadSignatureStamp = dpadConverter.GetSignatureStamp(signatureStamp);
      NpoComputer.DpadCP.Core.Stamps.Stamper.AddDirectumStamps(stream, dpadSignatureStamp);
      Logger.DebugFormat("Done. AddSignatureStamp DocumentId: {0}.", signatureStamp.PageStampInfo.DocumentId);
      return stream;
    }
    
    /// <summary>
    /// Поставить временный штамп на документ.
    /// </summary>
    /// <param name="stream">Документ, на который ставится штамп.</param>
    /// <returns>Документ со штампом.</returns>
    [Public]
    public virtual Stream AddTempStamp(Stream stream)
    {
      NpoComputer.DpadCP.Core.Stamps.Stamper.AddTempStamp(stream);
      return stream;
    }
    
    /// <summary>
    /// Поставить постраничный штамп на документ.
    /// </summary>
    /// <param name="stream">Документ, на который ставится штамп.</param>
    /// <param name="pageStamp">Информация о постраничном штампе.</param>
    /// <returns>Документ со штампом.</returns>
    [Public]
    public virtual Stream AddPaginationStamp(Stream stream, Structures.Module.IDpadPageStampInfo pageStamp)
    {
      Logger.DebugFormat("Execute. AddPaginationStamp DocumentId: {0}.", pageStamp.DocumentId);
      var documentInfo = new NpoComputer.DpadCP.Core.Stamps.DocumentInfo()
      {
        DocumentId = pageStamp.DocumentId,
        Title = pageStamp.Title
      };
      NpoComputer.DpadCP.Core.Stamps.Stamper.AddPaginationStamp(stream, documentInfo);
      Logger.DebugFormat("Done. AddPaginationStamp DocumentId: {0}.", pageStamp.DocumentId);
      return stream;
    }
    
    /// <summary>
    /// Сгенерировать PDF на основании переданных титулов документа.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="buyerTitle">Титул покупателя.</param>
    /// <param name="signatureStamp">Штамп с информацией о подписях.</param>
    /// <returns>Поток с содержимым PDF.</returns>
    [Public]
    public virtual Stream GeneratePdfForDocumentTitles(Stream sellerTitle, Stream buyerTitle, Structures.Module.IDpadSignatureStamp signatureStamp)
    {
      Logger.DebugFormat("Execute. GeneratePdfForDocumentTitles DocumentId: {0}.", signatureStamp?.PageStampInfo.DocumentId);
      
      var document = buyerTitle == null ? NpoComputer.DpadCP.Standard.FormalizedDocumentFactory.CreateDocument(sellerTitle) :
        NpoComputer.DpadCP.Standard.FormalizedDocumentFactory.CreateDocument(sellerTitle, buyerTitle);
      
      System.IO.Stream result = null;
      
      if (signatureStamp == null)
        result = document.ConvertToPdf();
      else
      {
        var dpadConverter = this.CreateDpadConverter();
        var dpadSignaturesStamp = dpadConverter.GetSignatureStamp(signatureStamp);
        result = document.ConvertToPdfWithStamp(dpadSignaturesStamp);
      }
      
      Logger.DebugFormat("Done. GeneratePdfForDocumentTitles DocumentId: {0}.", signatureStamp?.PageStampInfo.DocumentId);
      return result;
    }
    
    /// <summary>
    /// Сгенерировать PDF на основании переданных титулов УПД 970 приказа.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="buyerTitle">Титул покупателя.</param>
    /// <param name="signatureStamp">Штамп с информацией о подписях.</param>
    /// <returns>Поток с содержимым PDF.</returns>
    [Public]
    public virtual Stream GeneratePdfForUniversalTransferDocument970(Stream sellerTitle, Stream buyerTitle, Structures.Module.IDpadSignatureStamp signatureStamp)
    {
      Logger.DebugFormat("Execute. GeneratePdfForUniversalTransferDocument970 DocumentId: {0}.", signatureStamp?.PageStampInfo.DocumentId);
      
      var xmlContents = new List<byte[]>();
      xmlContents.Add(this.StreamToByteArray(sellerTitle));
      if (buyerTitle != null)
        xmlContents.Add(this.StreamToByteArray(buyerTitle));

      var documentProcessing = new NpoComputer.DpadCP.GeneralTransfer970.DocumentProcessing();
      var visualModel = documentProcessing.CreateVisualModel(xmlContents) as NpoComputer.DpadCP.GeneralTransfer970.DocumentProcessing.VisualModel;

      Logger.DebugFormat("GeneratePdfForUniversalTransferDocument970. IsInvoiceOnly: {0}, IsProductTraceabilityInfo: {1}.",
                         visualModel.IsInvoiceOnly, visualModel.IsProductTraceabilityInfo);

      var templateStream = this.GetUtd970Template(sellerTitle, buyerTitle, visualModel, documentProcessing);
      
      List<Stream> titles = new List<Stream> { sellerTitle };
      if (buyerTitle != null)
        titles.Add(buyerTitle);
      
      var result = documentProcessing.ConvertToPdf(titles, templateStream, visualModel);
      
      if (signatureStamp != null)
        result = this.AddSignatureStamp(result, signatureStamp);
      
      Logger.DebugFormat("Done. GeneratePdfForUniversalTransferDocument970 DocumentId: {0}.", signatureStamp?.PageStampInfo.DocumentId);
      return result;
    }
 
    /// <summary>
    /// Получить шаблон для УПД 970 приказа.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="buyerTitle">Титул покупателя.</param>
    /// <param name="visualModel">Модель представления УПД по 970 приказу.</param>
    /// <param name="documentProcessing">Класс работы с УПД по 970 приказу.</param>
    /// <returns>Поток с содержимым шаблона.</returns>
    /// <remarks>Титулы покупателя и продавца нужны как точка расширения для заказной разработки.</remarks>
    public virtual Stream GetUtd970Template(Stream sellerTitle, Stream buyerTitle,
                                            NpoComputer.DpadCP.GeneralTransfer970.DocumentProcessing.VisualModel visualModel,
                                            NpoComputer.DpadCP.GeneralTransfer970.DocumentProcessing documentProcessing)
    {
      var templateName = string.Empty;
      if (visualModel.IsInvoiceOnly)
      {
        templateName = visualModel.IsProductTraceabilityInfo
          ? NpoComputer.DpadCP.GeneralTransfer970.DocumentProcessing.Invoice970
          : NpoComputer.DpadCP.GeneralTransfer970.DocumentProcessing.Invoice970WithoutTraceability;
      }
      else
      {
        templateName = visualModel.IsProductTraceabilityInfo
          ? NpoComputer.DpadCP.GeneralTransfer970.DocumentProcessing.GeneralTransfer970
          : NpoComputer.DpadCP.GeneralTransfer970.DocumentProcessing.GeneralTransfer970WithoutTraceability;
      }
      
      return documentProcessing.GetTemplate(templateName);
    }

    /// <summary>
    /// Сгенерировать поток PDF на основе переданного потока XML-документа.
    /// </summary>
    /// <param name="xmlStream">Поток XML-документа.</param>
    /// <param name="documentName">Имя документа.</param>
    /// <param name="documentProcessing">Класс обработки XML-документа.</param>
    /// <returns>Поток с содержимым PDF.</returns>
    private Stream GeneratePdfInvoice(Stream xmlStream, string documentName,
                                      NpoComputer.DpadCP.InvoiceForPayments.DocumentProcessingInvoiceBase documentProcessing)
    {
      var loggerPrefix = string.Format("GeneratePdfInvoice DocumentName: {0}.", documentName);
      Logger.Debug(loggerPrefix + "Execute.");

      var xmlContents = new List<byte[]>();
      xmlContents.Add(this.StreamToByteArray(xmlStream));

      var template = documentProcessing.GetTemplate(NpoComputer.DpadCP.InvoiceForPayments.DocumentProcessingInvoiceBase.InvoiceForPayments);
      var visualModel = documentProcessing.CreateVisualModel(xmlContents) as NpoComputer.DpadCP.InvoiceForPayments.VisualModel;
      var docStream = new List<Stream> { xmlStream };

      var result = documentProcessing.ConvertToPdf(docStream, template, visualModel);
      Logger.Debug(loggerPrefix + "Done.");
      return result;
    }

    /// <summary>
    /// Сгенерировать поток PDF для счета Диадок 1.01.
    /// </summary>
    /// <param name="xmlStream">Поток XML-документа.</param>
    /// <param name="documentName">Имя документа.</param>
    /// <returns>Поток с содержимым PDF.</returns>
    [Public]
    public virtual Stream GeneratePdfDiadocInvoice101(Stream xmlStream, string documentName)
    {
      var documentProcessing = new NpoComputer.DpadCP.InvoiceForPayments.DocumentProcessingInvoiceDiadoc101();
      return this.GeneratePdfInvoice(xmlStream, documentName, documentProcessing);
    }
    
    /// <summary>
    /// Сгенерировать поток PDF для счета СБИС 5.01.
    /// </summary>
    /// <param name="xmlStream">Поток XML-документа.</param>
    /// <param name="documentName">Имя документа.</param>
    /// <returns>Поток с содержимым PDF.</returns>
    [Public]
    public virtual Stream GeneratePdfSbisInvoice501(Stream xmlStream, string documentName)
    {
      var documentProcessing = new NpoComputer.DpadCP.InvoiceForPayments.DocumentProcessingInvoiceSbis501();
      return this.GeneratePdfInvoice(xmlStream, documentName, documentProcessing);
    }
    
    /// <summary>
    /// Преобразовать поток в массив байтов.
    /// </summary>
    /// <param name="stream">Поток.</param>
    /// <returns>Массив байтов.</returns>
    public virtual byte[] StreamToByteArray(Stream stream)
    {
      using (var memoryStream = new System.IO.MemoryStream())
      {
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
      }
    }
    
    /// <summary>
    /// Создать экземпляр класса Dpad конвертера.
    /// </summary>
    /// <returns>Экземпляр класса Dpad конвертера.</returns>
    public virtual DpadConverter CreateDpadConverter()
    {
      return new DpadConverter();
    }
  }
}