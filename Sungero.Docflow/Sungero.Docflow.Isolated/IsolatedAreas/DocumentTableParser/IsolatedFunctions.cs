using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Sungero.Core;
using Sungero.IsolatedArea.Extensions;

namespace Sungero.Docflow.Isolated.DocumentTableParser
{
  public class IsolatedFunctions
  {
    /// <summary>
    /// Получить свойства поручений из тела документа.
    /// </summary>
    /// <param name="stream">Тело документа.</param>
    /// <param name="tags">Список тегов.</param>
    /// <returns>Структура содержащая свойства поручений.</returns>
    [Public]
    public virtual List<Sungero.Docflow.Structures.Module.IMinutesActionItem> GetActionItemsProperties(Stream stream, List<string> tags)
    {
      var result = new List<Sungero.Docflow.Structures.Module.IMinutesActionItem>();
      if (stream == null || tags == null || tags.Count == 0)
        return result;

      var parser = this.CreateDocumentTableParser(stream);
      var rows = parser.GetRows(tags);
      foreach (var row in rows)
      {
        var actionItem = Sungero.Docflow.Structures.Module.MinutesActionItem.Create();
        actionItem.Properties = new Dictionary<string, string>();
        foreach (var cell in row)
          actionItem.Properties.Add(cell.Key, cell.Value);
        
        result.Add(actionItem);
      }
      return result;
    }
    
    /// <summary>
    /// Создать экземпляр класса парсера таблиц.
    /// </summary>
    /// <param name="stream">Тело документа.</param>
    /// <returns>Экземпляр класса парсера таблиц.</returns>
    public virtual DocumentTableParser CreateDocumentTableParser(Stream stream)
    {
      return new DocumentTableParser(stream);
    }
  }
}