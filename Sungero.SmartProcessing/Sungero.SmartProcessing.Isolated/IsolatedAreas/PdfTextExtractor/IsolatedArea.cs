using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using PdfSharpCore.Pdf;
using Sungero.Core;

namespace Sungero.SmartProcessing.Isolated.PdfTextExtractor
{
  public class PdfTextExtractor
  {
    const string MetadataNotFoundException = "Ario metadata not found.";
    
    /// <summary>
    /// Получить текст pdf-документа.
    /// </summary>
    /// <param name="documentBody">Тело документа.</param>
    /// <returns>Текст документа.</returns>
    public string GetTextFromMetadata(Stream documentBody)
    {
      return string.Join(Environment.NewLine, this.GetAllPageTextsFromMetadata(documentBody));
    }
    
    /// <summary>
    /// Получить текст всех страниц pdf-документа.
    /// </summary>
    /// <param name="documentBody">Тело документа.</param>
    /// <returns>Тексты страниц.</returns>
    public List<string> GetAllPageTextsFromMetadata(Stream documentBody)
    {
      var result = new List<string>();
      using (var document = PdfSharpCore.Pdf.IO.PdfReader.Open(documentBody))
      {
        foreach (var page in document.Pages)
          result.Add(this.GetPageTextFromMetadata(page));
      }
      return result;
    }
    
    /// <summary>
    /// Получить текст из метаданных страницы.
    /// </summary>
    /// <param name="page">Страница.</param>
    /// <returns>Текст страницы.</returns>
    public string GetPageTextFromMetadata(PdfPage page)
    {
      // Метаданные должны быть на каждой странице.
      var pieceInfo = page.Elements.GetDictionary("/PieceInfo");
      if (pieceInfo == null)
        throw new AppliedCodeException(MetadataNotFoundException);
      
      var ario = pieceInfo?.Elements.GetDictionary("/Ario");
      var privateData = ario?.Elements.GetDictionary("/Private");
      var value = (privateData?.Elements.GetReference("/PageData").Value as PdfSharpCore.Pdf.PdfDictionary)?.Stream?.ToString();
      if (string.IsNullOrEmpty(value))
        throw new AppliedCodeException(MetadataNotFoundException);

      var bytes = new PdfSharpCore.Pdf.Internal.RawEncoding().GetBytes(value);
      var json = JObject.Parse(Encoding.UTF8.GetString(bytes));
      var text = json["Text"]?.ToString();
      return text;
    }
  }
}