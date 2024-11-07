using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Sungero.Core;
using Sungero.SmartProcessing.Structures.Module;

namespace Sungero.SmartProcessing.Isolated.Repacking
{
  public class IsolatedFunctions
  {
    /// <summary>
    /// Создать сборщик Pdf документов.
    /// </summary>
    /// <returns>Гуид сборщика Pdf документов.</returns>
    [Public]
    public virtual Guid CreateNewPdfBuilder()
    {
      var builder = new PdfBuilder();
      PdfBuildManager.Instance.AddPdfBuilder(builder);
      return builder.Guid;
    }

    /// <summary>
    /// Добавить исходный документ.
    /// </summary>
    /// <param name="builderGuid">Гуид сборщика Pdf документов.</param>
    /// <param name="documentId">Ид документа.</param>
    /// <param name="stream">Тело документа.</param>
    [Public]
    public virtual void AddSourceDocument(Guid builderGuid, long documentId, Stream stream)
    {
      var builder = PdfBuildManager.Instance.GetPdfBuilder(builderGuid);
      builder.AddSourceDocument(documentId, stream);
    }

    /// <summary>
    /// Собрать документ.
    /// </summary>
    /// <param name="builderGuid">Гуид сборщика Pdf документов.</param>
    /// <param name="pages">Cтраницы.</param>
    /// <returns>Тело собранного документа.</returns>
    [Public]
    public virtual Stream BuildDocument(Guid builderGuid, List<Structures.Module.IRepackingPage> pages)
    {
      var pageChanges = pages.Select(x => new Page(x.DocumentId, x.Number, x.Rotation));
      var builder = PdfBuildManager.Instance.GetPdfBuilder(builderGuid);
      return builder.Build(pageChanges.ToList());
    }

    /// <summary>
    /// Удалить сборщик Pdf документов.
    /// </summary>
    /// <param name="builderGuid">Гуид сборщика Pdf документов.</param>
    [Public]
    public virtual void DeletePdfBuilder(Guid builderGuid)
    {
      PdfBuildManager.Instance.DeletePdfBuilder(builderGuid);
    }
    
    /// <summary>
    /// Сформировать строку JSON для статики перекомплектования.
    /// </summary>
    /// <param name="repackingDocuments">Документы для перекомплектования.</param>
    /// <param name="documentTypes">Типы документов.</param>
    /// <returns>Строка JSON для статики перекомплектования.</returns>
    [Public]
    public string BuildDocumentAndTypesResponseContent(List<Structures.Module.IRepackingDocumentDTO> repackingDocuments, List<Structures.Module.IRepackingDocumentType> documentTypes)
    {
      return new JObject(
        new JProperty("types", new JArray(documentTypes.Select(x => new JObject(
          new JProperty("name", x.Name),
          new JProperty("id", x.Id))))),
        new JProperty("documents", new JArray(repackingDocuments.Select(x => new JObject(
          new JProperty("name", x.Name),
          new JProperty("id", x.DocumentId.ToString()),
          new JProperty("version", x.VersionId),
          new JProperty("typeId", x.Type)))))).ToString();
    }
  }
}