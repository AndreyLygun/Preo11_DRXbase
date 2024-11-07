using System;
using System.Collections.Generic;
using System.Linq;
using Aspose.BarCode;
using Newtonsoft.Json;
using Sungero.Core;
using Sungero.Docflow;
using Sungero.Docflow.Structures.Module;

namespace Sungero.Docflow.Isolated.BarcodeParser
{
  public class Parser
  {
    /// <summary>
    /// Поиск штрихкодов в документе.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Список распознанных штрихкодов.</returns>
    /// <remarks>
    /// Поиск штрихкодов осуществляется только на первой странице документа.
    /// Формат штрихкода: Code128.
    /// </remarks>
    public virtual List<string> GetDocumentBarcodes(System.IO.Stream document)
    {
      var barcodes = new List<string>();
      try
      {
        barcodes = this.ExtractBarcodes(document, Aspose.BarCode.BarCodeRecognition.DecodeType.Code128);
      }
      catch (Aspose.BarCode.BarCodeException ex)
      {
        Logger.Error(ex.Message);
      }
      
      return barcodes;
    }
    
    /// <summary>
    /// Извлечь штрихкод из PDF документа.
    /// </summary>
    /// <param name="inputStream">PDF документ.</param>
    /// <param name="barcodeType">Тип штрихкода.</param>
    /// <returns>Список извлеченных штрихкодов.</returns>
    /// <remarks>Извлекает штрихкоды в формате Code128 только с первой страницы документа.</remarks>
    public List<string> ExtractBarcodes(System.IO.Stream inputStream, Aspose.BarCode.BarCodeRecognition.SingleDecodeType barcodeType)
    {
      var docBarcodes = new List<string>();
      try
      {
        var pdfDoc = new Aspose.Pdf.Document(inputStream);
        var converter = new Aspose.Pdf.Facades.PdfConverter();
        converter.BindPdf(pdfDoc);
        converter.StartPage = 1;
        converter.EndPage = 1;
        converter.RenderingOptions.BarcodeOptimization = true;
        converter.Resolution = new Aspose.Pdf.Devices.Resolution(300);
        converter.DoConvert();
        using (var stream = new System.IO.MemoryStream())
        {
          converter.GetNextImage(stream, System.Drawing.Imaging.ImageFormat.Png);
          
          if (barcodeType == null)
            barcodeType = Aspose.BarCode.BarCodeRecognition.DecodeType.Code128;
          
          using (var barcodeReader = new Aspose.BarCode.BarCodeRecognition.BarCodeReader(stream, barcodeType))
          {
            foreach (var result in barcodeReader.ReadBarCodes())
              docBarcodes.Add(result.CodeText);
          }
        }
        return docBarcodes;
      }
      catch (Exception ex)
      {
        throw new Aspose.BarCode.BarCodeException("Cant read barcode from document", ex);
      }
    }
    
    /// <summary>
    /// Получение ИД документов из списка штрихкодов.
    /// </summary>
    /// <param name="barcodes">Список штрихкодов.</param>
    /// <param name="formattedTenantId">Имя тенанта в формате, нужном при подстановке в штрих-код.</param>
    /// <returns>Список распознанных ИД документа.</returns>
    public virtual List<long> ParseDocumentIds(List<string> barcodes, string formattedTenantId)
    {
      var documentIds = new List<long>();
      
      var ourBarcodes = barcodes.Where(b => b.Contains(formattedTenantId));
      foreach (var barcode in ourBarcodes)
      {
        var documentId = this.ParseDocumentId(barcode, formattedTenantId);
        if (documentId.HasValue)
          documentIds.Add(documentId.Value);
      }
      
      return documentIds;
    }
    
    /// <summary>
    /// Получение ИД документа из штрихкода.
    /// </summary>
    /// <param name="barcode">Штрихкод.</param>
    /// <param name="formattedTenantId">Имя тенанта в формате, нужном при подстановке в штрих-код.</param>
    /// <returns>Распознанный ИД документа.</returns>
    /// <remarks>
    /// Ожидается, что штрихкод имеет формат "id тенанта - id документа".
    /// </remarks>
    public virtual long? ParseDocumentId(string barcode, string formattedTenantId)
    {
      var lastBarcodeElement = barcode.Split(new string[] { " - ", "-" }, StringSplitOptions.None).Last();
      long documentId;
      if (!long.TryParse(lastBarcodeElement, out documentId))
        return null;
      
      return documentId;
    }
  }
}