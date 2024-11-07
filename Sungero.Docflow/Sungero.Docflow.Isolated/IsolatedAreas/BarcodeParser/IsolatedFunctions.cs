using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Sungero.Core;
using Sungero.Docflow.Structures.Module;

namespace Sungero.Docflow.Isolated.BarcodeParser
{
  public class IsolatedFunctions
  {
    /// <summary>
    /// Поиск ИД документа по штрихкодам.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="formattedTenantId">Имя тенанта в формате, нужном при подстановке в штрих-код.</param>
    /// <returns>Список распознанных ИД документа.</returns>
    /// <remarks>
    /// Поиск штрихкодов осуществляется только на первой странице документа.
    /// Формат штрихкода: Code128.
    /// </remarks>
    [Public]
    public virtual List<long> SearchDocumentBarcodeIds(System.IO.Stream document, string formattedTenantId)
    {
      var barcodeParser = this.CreateBarcodeParser();
      var documentBarcodes = barcodeParser.GetDocumentBarcodes(document);
      return barcodeParser.ParseDocumentIds(documentBarcodes, formattedTenantId);
    }
    
    /// <summary>
    /// Создать средство поиска штрих-кодов в документе.
    /// </summary>
    /// <returns>Средство поиска штрих-кодов в документе.</returns>
    public virtual Sungero.Docflow.Isolated.BarcodeParser.Parser CreateBarcodeParser()
    {
      return new BarcodeParser.Parser();
    }
  }
}