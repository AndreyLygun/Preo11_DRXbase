using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Sungero.Core;
using Sungero.SmartProcessing.Structures.Module;

namespace Sungero.SmartProcessing.Isolated.HtmlDocumentParser
{
  public class HtmlDocumentParser
  {
    /// <summary>
    /// Получить текст из html-тела письма.
    /// </summary>
    /// <param name="htmlBody">Тело письма.</param>
    /// <returns>Текст письма.</returns>
    public virtual string GetText(Stream htmlBody)
    {
      using (var streamReader = new StreamReader(htmlBody))
      {
        var document = new Aspose.Words.Document();
        var builder = new Aspose.Words.DocumentBuilder(document);
        var html = streamReader.ReadToEnd();
        builder.InsertHtml(html);
        var text = document.ToString(Aspose.Words.SaveFormat.Text);
        return text;
      }
    }
  }
}