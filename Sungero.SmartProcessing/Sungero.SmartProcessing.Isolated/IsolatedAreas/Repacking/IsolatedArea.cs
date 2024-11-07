using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Sungero.Core;
using Sungero.SmartProcessing.Structures.Module;

namespace Sungero.SmartProcessing.Isolated.Repacking
{
  /// <summary>
  /// Менеджер сборки Pdf документов.
  /// </summary>
  public sealed class PdfBuildManager
  {
    #region Thread-safety Singletone

    // About Singletones: https://csharpindepth.com/Articles/Singleton
    private static readonly PdfBuildManager BuildInstance = new PdfBuildManager();
    
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1409:RemoveUnnecessaryCode", Justification = "Part of a thread-safety Singletone.")]
    static PdfBuildManager()
    {
    }
    
    private PdfBuildManager()
    {
    }
    
    public static PdfBuildManager Instance
    {
      get
      {
        return BuildInstance;
      }
    }
    
    #endregion
    
    /// <summary>
    /// Активные сборщики.
    /// </summary>
    public List<IPdfBuilder> ActivePdfBuilders { get; private set; }
    
    /// <summary>
    /// Добавить PDF сборщик.
    /// </summary>
    /// <param name="builder">Сборщик.</param>
    public void AddPdfBuilder(IPdfBuilder builder)
    {
      if (this.ActivePdfBuilders == null)
        this.ActivePdfBuilders = new List<IPdfBuilder>();
      this.ActivePdfBuilders.Add(builder);
    }
    
    /// <summary>
    /// Получить сборщик тел документов.
    /// </summary>
    /// <param name="builderGuid">Гуид сборщика.</param>
    /// <returns>Сборщик.</returns>
    public IPdfBuilder GetPdfBuilder(Guid builderGuid)
    {
      return this.ActivePdfBuilders.Where(x => x.Guid == builderGuid).FirstOrDefault();
    }
    
    /// <summary>
    /// Убрать сборщик из списка активных.
    /// </summary>
    /// <param name="builderGuid">Гуид упаковщика.</param>
    public void DeletePdfBuilder(Guid builderGuid)
    {
      var builder = this.ActivePdfBuilders.Where(x => x.Guid == builderGuid).FirstOrDefault();
      if (builder != null)
        this.ActivePdfBuilders.Remove(builder);
    }
  }

  /// <summary>
  /// Сборщик Pdf документов.
  /// </summary>
  public interface IPdfBuilder
  {
    /// <summary>
    /// Уникальный идентификатор.
    /// </summary>
    Guid Guid { get; }
    
    /// <summary>
    /// Исходные документы для сборки.
    /// </summary>
    Dictionary<long, Aspose.Pdf.Document> SourceDocuments { get; set; }
    
    /// <summary>
    /// Добавить исходный документ.
    /// </summary>
    /// <param name="id">Ид документа.</param>
    /// <param name="body">Тело документа.</param>
    void AddSourceDocument(long id, Stream body);
    
    /// <summary>
    /// Собрать новый документ.
    /// </summary>
    /// <param name="pages">Информация о страницах нового документа.</param>
    /// <returns>Тело нового документа.</returns>
    Stream Build(List<Page> pages);
  }

  /// <summary>
  /// Сборщик Pdf документов.
  /// </summary>
  public class PdfBuilder : IPdfBuilder
  {
    /// <summary>
    /// Кол-во возможных углов поворота.
    /// </summary>
    public const int RotationAngleCount = 4;
    
    /// <summary>
    /// Уникальный идентификатор.
    /// </summary>
    public Guid Guid { get; protected set; }
    
    /// <summary>
    /// Исходные документы для сборки.
    /// </summary>
    public Dictionary<long, Aspose.Pdf.Document> SourceDocuments { get; set; }
    
    /// <summary>
    /// Добавить исходный документ.
    /// </summary>
    /// <param name="id">Ид документа.</param>
    /// <param name="body">Тело документа.</param>
    public virtual void AddSourceDocument(long id, Stream body)
    {
      var document = new Aspose.Pdf.Document(body);
      this.SourceDocuments.Add(id, document);
    }
    
    /// <summary>
    /// Собрать документ.
    /// </summary>
    /// <param name="pages">Информация о страницах нового документа.</param>
    /// <returns>Тело нового документа.</returns>
    public virtual Stream Build(List<Page> pages)
    {
      var outputStream = new MemoryStream();
      var document = new Aspose.Pdf.Document();
      foreach (var page in pages)
        document.Pages.Add(this.GetAsposePage(page));
      this.SetAsposeDocumentPageRotations(document, pages);
      document.Save(outputStream);
      return outputStream;
    }
    
    /// <summary>
    /// Получить Aspose страницу.
    /// </summary>
    /// <param name="page">Страница.</param>
    /// <returns>Aspose страница.</returns>
    public virtual Aspose.Pdf.Page GetAsposePage(Page page)
    {
      if (!this.SourceDocuments.ContainsKey(page.DocumentId))
        throw new ArgumentException(string.Format("Builder {0}. Source document with id {1} not found.", this.Guid, page.DocumentId));
      var document = this.SourceDocuments[page.DocumentId];
      if (document.Pages.Count < page.Number + 1)
        throw new IndexOutOfRangeException(string.Format("Builder {0}. Source document with id {1}. Page number {2} is out of range.", this.Guid, page.DocumentId, page.Number + 1));
      return document.Pages[page.Number + 1];
    }
    
    /// <summary>
    /// Задать повороты страниц в Pdf документе.
    /// </summary>
    /// <param name="document">Pdf документ.</param>
    /// <param name="pages">Страницы.</param>
    public virtual void SetAsposeDocumentPageRotations(Aspose.Pdf.Document document, List<Page> pages)
    {
      for (var index = 1; index <= pages.Count; index++)
      {
        var currentRotation = (int)document.Pages[index].Rotate;
        var newRotation = (currentRotation + pages[index - 1].Rotation) % RotationAngleCount;
        if (newRotation < 0)
          newRotation = RotationAngleCount + newRotation;
        document.Pages[index].Rotate = (Aspose.Pdf.Rotation)newRotation;
      }
    }
    
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PdfBuilder"/>
    /// </summary>
    public PdfBuilder()
    {
      this.Guid = System.Guid.NewGuid();
      this.SourceDocuments = new Dictionary<long, Aspose.Pdf.Document>();
    }
  }
  
  /// <summary>
  /// Страница.
  /// </summary>
  public class Page
  {
    /// <summary>
    /// ИД документа.
    /// </summary>
    public long DocumentId { get; set; }
    
    /// <summary>
    /// Номер.
    /// </summary>
    public int Number { get; set; }
    
    /// <summary>
    /// Поворот.
    /// </summary>
    /// <remarks>Количество поворотов на 90 градусов. Положительные значения - по часовой стрелке, отрицательные - против.</remarks>
    public int Rotation { get; set; }
    
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Page" />
    /// </summary>
    /// <param name="documentId">ИД документа.</param>
    /// <param name="number">Номер.</param>
    /// <param name="pageRotation">Поворот.</param>
    public Page(long documentId, int number, int pageRotation)
    {
      this.DocumentId = documentId;
      this.Number = number;
      this.Rotation = pageRotation;
    }
  }
}