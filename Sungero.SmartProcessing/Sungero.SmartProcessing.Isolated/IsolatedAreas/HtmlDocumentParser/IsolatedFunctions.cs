using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Sungero.Core;
using Sungero.SmartProcessing.Structures.Module;

namespace Sungero.SmartProcessing.Isolated.HtmlDocumentParser
{
  public class IsolatedFunctions
  {
    /// <summary>
    /// Получить текст из тела письма.
    /// </summary>
    /// <param name="htmlBody">Тело письма.</param>
    /// <returns>Текст письма.</returns>
    [Public]
    public virtual string GetText(Stream htmlBody)
    {
      var htmlDocumentParser = this.CreateHtmlDocumentParser();
      return htmlDocumentParser.GetText(htmlBody);
    }
    
    /// <summary>
    /// Создать парсер html-документов.
    /// </summary>
    /// <returns>Парсер html-документов.</returns>
    public virtual HtmlDocumentParser CreateHtmlDocumentParser()
    {
      return new HtmlDocumentParser();
    }
  }
}