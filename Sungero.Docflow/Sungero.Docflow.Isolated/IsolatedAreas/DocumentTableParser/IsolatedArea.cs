using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aspose.Words;
using Aspose.Words.Markup;
using Aspose.Words.Tables;
using Newtonsoft.Json;
using Sungero.Docflow.Structures.Module;
using Sungero.IsolatedArea.Extensions;

namespace Sungero.Docflow.Isolated.DocumentTableParser
{
  public class DocumentTableParser
  {
    protected readonly Document Document;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DocumentTableParser" /> и считывает документ Word.
    /// </summary>
    /// <param name="documentStream">Поток документа.</param>
    public DocumentTableParser(Stream documentStream)
    {
      this.Document = new Document(documentStream);
    }

    /// <summary>
    /// Получить значения из строк таблицы с определенными тегами.
    /// </summary>
    /// <param name="tags">Список тегов.</param>
    /// <returns>Словарь значений колонок.</returns>
    /// <remarks>Только первая таблица, в которой есть все указанные теги.</remarks>
    public virtual List<Dictionary<string, string>> GetRows(List<string> tags)
    {
      var result = new List<Dictionary<string, string>>();
      if (tags == null || tags.Count == 0)
        return result;
      
      var tables = this.Document.GetChildNodes(NodeType.Table, true);
      foreach (Table table in tables)
      {
        var taggedColumnNumbers = new Dictionary<string, int>();
        foreach (var tag in tags)
        {
          var columnNumber = this.GetTaggedColumnNumber(table, tag);
          if (columnNumber.HasValue)
            taggedColumnNumbers.Add(tag, columnNumber.Value);
        }
        if (taggedColumnNumbers.Count != tags.Count)
          continue;
        
        result = this.GetTableRows(table, taggedColumnNumbers);
        break;
      }
      
      return result;
    }
    
    /// <summary>
    /// Получить список строк из таблицы по тегам.
    /// </summary>
    /// <param name="table">Таблица.</param>
    /// <param name="taggedColumnNumbers">Словарь соответствия тегов и номеров колонок таблицы.</param>
    /// <returns>Список строк таблицы.</returns>
    public virtual List<Dictionary<string, string>> GetTableRows(Table table, Dictionary<string, int> taggedColumnNumbers)
    {
      var result = new List<Dictionary<string, string>>();
      if (table == null || taggedColumnNumbers == null || taggedColumnNumbers.Count == 0)
        return result;
      
      for (int i = 1; i < table.Rows.Count; i++)
      {
        var tableRow = new Dictionary<string, string>();
        foreach (var taggedColumnNumber in taggedColumnNumbers)
        {
          if (table.Rows[i].Cells.Count <= taggedColumnNumber.Value)
            continue;
          var cellValue = string.Join(Environment.NewLine,
                                      table.Rows[i].Cells[taggedColumnNumber.Value].Select(x => x.ToString(SaveFormat.Text).Trim()));
          tableRow.Add(taggedColumnNumber.Key, cellValue);
        }
        result.Add(tableRow);
      }
      return result;
    }
    
    /// <summary>
    /// Получить номер колонки таблицы по тегу.
    /// </summary>
    /// <param name="table">Таблица.</param>
    /// <param name="tag">Тег.</param>
    /// <returns>Номер колонки или null если колонка с таким тегом не найдена.</returns>
    public virtual int? GetTaggedColumnNumber(Table table, string tag)
    {
      if (table == null || string.IsNullOrWhiteSpace(tag))
        return null;
      
      var cells = table.FirstRow.Cells;
      for (int i = 0; i < cells.Count; i++)
      {
        if (this.IsCellNodesContainsTag(cells[i], tag) || this.IsCellContentEqualsTag(cells[i], tag))
          return i;
      }
      return null;
    }
    
    /// <summary>
    /// Проверить что ноды ячейки содержат тег.
    /// </summary>
    /// <param name="cell">Ячейка.</param>
    /// <param name="tag">Тег.</param>
    /// <returns>True если ноды ячейки содержат тег, иначе False.</returns>
    public virtual bool IsCellNodesContainsTag(Cell cell, string tag)
    {
      if (cell == null || string.IsNullOrWhiteSpace(tag))
        return false;
      
      foreach (StructuredDocumentTag cellTag in cell.GetChildNodes(NodeType.StructuredDocumentTag, true))
      {
        if (string.Equals(cellTag.Tag, tag, StringComparison.InvariantCultureIgnoreCase))
          return true;
      }
      return false;
    }
    
    /// <summary>
    /// Проверить что содержимое ячейки равно тегу.
    /// </summary>
    /// <param name="cell">Ячейка.</param>
    /// <param name="tag">Тег.</param>
    /// <returns>True если содержимое ячейки равно тегу, иначе False.</returns>
    public virtual bool IsCellContentEqualsTag(Cell cell, string tag)
    {
      if (cell == null || string.IsNullOrWhiteSpace(tag))
        return false;
      
      return string.Equals(cell.ToString(SaveFormat.Text).Trim(),
                           tag, StringComparison.InvariantCultureIgnoreCase);
    }
  }
}