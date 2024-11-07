using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Sungero.Core;
using Sungero.Docflow.Isolated;
using Sungero.Docflow.Structures.Module;

namespace Sungero.Docflow.Isolated.PdfConverter
{
  public class IsolatedFunctions
  {
    /// <summary>
    /// Проверить, поддерживается ли формат файла по его расширению.
    /// </summary>
    /// <param name="extension">Расширение файла.</param>
    /// <returns>True/false.</returns>
    [Public]
    public virtual bool CheckIfExtensionIsSupported(string extension)
    {
      return this.CreateFileExtensionsManager().PdfConversionAllowed(extension);
    }
    
    /// <summary>
    /// Преобразовать документ в pdf.
    /// </summary>
    /// <param name="inputStream">Поток с входным документом.</param>
    /// <param name="extension">Расширение входного документа.</param>
    /// <returns>Поток с документом.</returns>
    [Public]
    public virtual Stream GeneratePdf(Stream inputStream, string extension)
    {
      var pdfConverter = this.CreatePdfConverter();
      return pdfConverter.GeneratePdf(inputStream, extension);
    }
    
    /// <summary>
    /// Преобразовать документ в pdf/a.
    /// </summary>
    /// <param name="inputStream">Поток содержащий данные исходного документа.</param>
    /// <param name="extension">Расширение исходного документа.</param>
    /// <param name="pdfAVersion">Версия pdf/a. Поддерживаемые значения: v1A, v1B.</param>
    /// <returns>Поток с документом.</returns>
    [Public]
    public virtual Stream GeneratePdfA(Stream inputStream, string extension, string pdfAVersion)
    {
      try
      {
        var pdfConverter = this.CreatePdfAConverter();
        return pdfConverter.GeneratePdf(inputStream, extension, new string[] { pdfAVersion });
      }
      catch (Exception ex)
      {
        Logger.Error("Cannot convert to pdf/a", ex);
        throw new AppliedCodeException("Cannot convert to pdf/a");
      }
    }
    
    /// <summary>
    /// Заполнить метаданные pdf документа.
    /// </summary>
    /// <param name="inputStream">Поток, содержащий данные исходного документа.</param>
    /// <param name="author">Автор документа.</param>
    /// <param name="creationDate">Дата создания документа.</param>
    /// <param name="modificationDate">Дата изменения документа.</param>
    /// <returns>Поток с документом.</returns>
    [Public]
    public virtual Stream FillPdfDocumentMetadata(Stream inputStream, string author, DateTime creationDate, DateTime modificationDate)
    {
      var pdfConverter = this.CreatePdfConverter();
      return pdfConverter.FillPdfDocumentMetadata(inputStream, author, creationDate, modificationDate);
    }
    
    /// <summary>
    /// Склеить файлы PDF в один.
    /// </summary>
    /// <param name="firstStream">Первый PDF-файл.</param>
    /// <param name="secondStream">Второй PDF-файл, который добавляется к первому.</param>
    /// <returns>Склеенный файл, состоящий из первого и второго PDF-файлов.</returns>
    [Public]
    public virtual Stream MergePdf(Stream firstStream, Stream secondStream)
    {
      var resultStream = new MemoryStream();
      
      try
      {
        var firstDocument = new Aspose.Pdf.Document(firstStream);
        var secondDocument = new Aspose.Pdf.Document(secondStream);
        
        firstDocument.Pages.Add(secondDocument.Pages);
        firstDocument.Save(resultStream);
        return resultStream;
      }
      catch (Exception ex)
      {
        resultStream.Close();
        Logger.Error("Merging pdf error", ex);
        throw new AppliedCodeException(string.Format("Merging pdf error: {0}", ex.Message));
      }
    }
    
    /// <summary>
    /// Проверить наличие текстового слоя.
    /// </summary>
    /// <param name="body">Тело документа.</param>
    /// <returns>True, если документ содержит текстовый слой, иначе - False.</returns>
    /// <remarks>Проверяется текстовый слой только на первой странице.</remarks>
    [Public]
    public virtual bool CheckDocumentTextLayer(Stream body)
    {
      try
      {
        var pdfConverter = this.CreatePdfConverter();
        return pdfConverter.CheckDocumentTextLayer(body);
      }
      catch (Exception ex)
      {
        Logger.Error("Cannot check text layer", ex);
        throw new AppliedCodeException("Cannot check text layer");
      }
    }
    
    /// <summary>
    /// Добавить отметку о регистрации на заданную страницу документа.
    /// </summary>
    /// <param name="inputStream">Поток с входным документом.</param>
    /// <param name="htmlStamp">Строка, содержащая html для отметки о регистрации.</param>
    /// <param name="pageNumber">Страница, на которой необходимо проставить отметку.</param>
    /// <param name="rightIndentInCm">Отступ с правого края, в см.</param>
    /// <param name="bottomIndentInCm">Отступ с нижнего края, в см.</param>
    /// <returns>Поток с документом.</returns>
    [Public]
    public virtual Stream AddRegistrationStamp(Stream inputStream, string htmlStamp, int pageNumber, double rightIndentInCm, double bottomIndentInCm)
    {
      var pdfStamper = this.CreatePdfStamper();
      
      try
      {
        var document = new Aspose.Pdf.Document(inputStream);
        var stamp = pdfStamper.CreateMarkFromHtml(htmlStamp);

        // Установить координаты отметки.
        var page = document.Pages[pageNumber];
        var rectConsiderRotation = page.GetPageRect(true);
        stamp.XIndent = rectConsiderRotation.Width - (rightIndentInCm * PdfConverter.PdfStamper.DotsPerCm) - stamp.Width;
        stamp.YIndent = bottomIndentInCm * PdfConverter.PdfStamper.DotsPerCm;

        return pdfStamper.AddStampToDocumentPage(inputStream, pageNumber, stamp);
      }
      catch (Exception ex)
      {
        Logger.Error("Cannot add stamp", ex);
        throw new AppliedCodeException("Cannot add stamp");
      }
    }
    
    /// <summary>
    /// Добавить отметку о подписи к документу согласно символу-якорю.
    /// </summary>
    /// <param name="inputStream">Поток с входным документом.</param>
    /// <param name="extension">Расширение файла.</param>
    /// <param name="htmlMark">Строка, содержащая html для отметки об ЭП.</param>
    /// <param name="anchorSymbol">Символ-якорь.</param>
    /// <param name="searchablePagesNumber">Количество страниц для поиска символа.</param>
    /// <returns>Поток с документом.</returns>
    /// <remarks>Если символов-якорей в документе нет, то отметка проставляется на последней странице.</remarks>
    [Public]
    public virtual Stream AddSignatureStamp(Stream inputStream, string extension, string htmlMark, string anchorSymbol,
                                            int searchablePagesNumber)
    {
      var pdfStamper = this.CreatePdfStamper();
      return pdfStamper.AddSignatureMark(inputStream, extension, htmlMark, anchorSymbol, searchablePagesNumber);
    }
    
    /// <summary>
    /// Преобразовать документ в PDF и проставить на него отметки.
    /// </summary>
    /// <param name="documentWithMarks">Модель документа с отметками для простановки.</param>
    /// <returns>Модель документа с проставленными отметками.</returns>
    [Public]
    public virtual Structures.Module.IDocumentMarksDto ConvertToPdfWithMarks(Structures.Module.IDocumentMarksDto documentWithMarks)
    {
      PdfConversionLogger.Debug(documentWithMarks, "Start conversion with marks");
      this.UpdateWordDocumentTagsFromMarks(documentWithMarks);
      this.ConvertBodyToPdf(documentWithMarks);
      if (!documentWithMarks.IsConverted)
        return documentWithMarks;
      this.AddMarksToPdfBody(documentWithMarks);
      
      PdfConversionLogger.Debug(documentWithMarks, "Conversion with marks done");
      return documentWithMarks;
    }
    
    /// <summary>
    /// Обновить тэги в документе Word из отметок.
    /// </summary>
    /// <param name="documentWithMarks">Модель документа с отметками.</param>
    public virtual void UpdateWordDocumentTagsFromMarks(IDocumentMarksDto documentWithMarks)
    {
      if (!this.CreateFileExtensionsManager().UpdateDocumentTagsAllowed(documentWithMarks.BodyExtension))
        return;
      
      PdfConversionLogger.Debug(documentWithMarks, "Start update word document tags from marks");
      var taggedMarkDtos = documentWithMarks.Marks.Where(m => m.Tags.Any());
      PdfConversionLogger.Debug(documentWithMarks, $"Found {taggedMarkDtos.Count()} tagged marks: [{string.Join(",", taggedMarkDtos.Select(x => x.Id))}]");
      if (!taggedMarkDtos.Any())
        return;
      
      Aspose.Words.Document document;
      if (!ConverterBase.TryCreateWordDocument(documentWithMarks.Body, out document))
      {
        foreach (var markDto in taggedMarkDtos)
          markDto.IsSuccessful = false;
        PdfConversionLogger.Debug(documentWithMarks, "Cannot create Aspose.Words.Document from body");
        return;
      }
      
      foreach (var markDto in taggedMarkDtos)
      {
        var tagUpdater = new DocxTagUpdater(document, markDto) { LogMessagePrefix = PdfConversionLogger.GetLogMessagePrefix(documentWithMarks) };
        tagUpdater.UpdateTagFromMarkDto();
        markDto.IsSuccessful = tagUpdater.MarkDto.IsSuccessful;
        markDto.Page = tagUpdater.MarkDto.Page;
        markDto.X = tagUpdater.MarkDto.X;
        markDto.Y = tagUpdater.MarkDto.Y;
      }
      
      this.WriteWordDocumentToDocumentWithMarksDto(documentWithMarks, document);
    }
    
    /// <summary>
    /// Преобразовать тело документа в PDF.
    /// </summary>
    /// <param name="documentWithMarks">Модель документа с отметками.</param>
    public virtual void ConvertBodyToPdf(IDocumentMarksDto documentWithMarks)
    {
      var pdfConverter = this.CreatePdfConverter();
      Aspose.Pdf.Document pdfDocument;
      try
      {
        pdfDocument = pdfConverter.GeneratePdfDocument(documentWithMarks.Body, documentWithMarks.BodyExtension);
        this.WritePdfDocumentToDocumentWithMarksDto(documentWithMarks, pdfDocument);
        documentWithMarks.IsConverted = true;
        PdfConversionLogger.Debug(documentWithMarks, "Converted to pdf");
      }
      catch (Exception ex)
      {
        PdfConversionLogger.Error(ex, documentWithMarks, "An error occured while converting document to pdf");
        this.SetDocumentWithMarksDtoConversionErrorState(documentWithMarks);
        foreach (var markDto in documentWithMarks.Marks)
          markDto.IsSuccessful = false;
        return;
      }
    }
    
    /// <summary>
    /// Добавить отметки на преобразованное тело документа.
    /// </summary>
    /// <param name="documentWithMarks">Модель документа с отметками.</param>
    public virtual void AddMarksToPdfBody(IDocumentMarksDto documentWithMarks)
    {
      if (!documentWithMarks.IsConverted || documentWithMarks.BodyExtension != FileExtensionsManager.Pdf)
        return;
      
      PdfConversionLogger.Debug(documentWithMarks, "Start add marks to pdf body");
      var markDtos = documentWithMarks.Marks.Where(m => !m.Tags.Any());
      var pdfStamper = this.CreatePdfStamper();
      Aspose.Pdf.Document pdfDocument;
      if (!ConverterBase.TryCreatePdfDocument(documentWithMarks.Body, out pdfDocument))
      {
        PdfConversionLogger.Debug(documentWithMarks, "AddMarksToPdfBody. Cannot read document body");
        this.SetDocumentWithMarksDtoConversionErrorState(documentWithMarks);
        return;
      }
      
      if (this.BlankPageRequired(documentWithMarks))
        pdfDocument.Pages.Add();
      
      foreach (var markDto in markDtos)
        markDto.IsSuccessful = pdfStamper.TryAddMarkToPdfBody(pdfDocument, markDto, documentWithMarks);
      
      this.WritePdfDocumentToDocumentWithMarksDto(documentWithMarks, pdfDocument);
      PdfConversionLogger.Debug(documentWithMarks, "Add marks to pdf body done");
    }
    
    /// <summary>
    /// Определить необходимость создания новой страницы в документе.
    /// </summary>
    /// <param name="documentWithMarks">Модель документа с отметками.</param>
    /// <returns>True - необходимо создать новую страницу. False - иначе.</returns>
    public virtual bool BlankPageRequired(IDocumentMarksDto documentWithMarks)
    {
      return documentWithMarks.Marks.Any(x => x.OnBlankPage.GetValueOrDefault() && !x.Tags.Any() && string.IsNullOrWhiteSpace(x.Anchor));
    }
    
    /// <summary>
    /// Установить состояние ошибки преобразования для модели документа с отметками.
    /// </summary>
    /// <param name="documentWithMarks">Модель документа с отметками.</param>
    public virtual void SetDocumentWithMarksDtoConversionErrorState(IDocumentMarksDto documentWithMarks)
    {
      documentWithMarks.Body = Stream.Null;
      documentWithMarks.IsConverted = false;
    }
    
    /// <summary>
    /// Записать pdf документ в модель документа с отметками.
    /// </summary>
    /// <param name="documentWithMarks">Модель документа с отметками.</param>
    /// <param name="pdfDocument">Pdf документ.</param>
    public virtual void WritePdfDocumentToDocumentWithMarksDto(IDocumentMarksDto documentWithMarks, Aspose.Pdf.Document pdfDocument)
    {
      var documentStream = new MemoryStream();
      pdfDocument.Save(documentStream);
      documentWithMarks.Body = documentStream;
      documentWithMarks.BodyExtension = FileExtensionsManager.Pdf;
    }
    
    /// <summary>
    /// Записать Word документ в модель документа с отметками.
    /// </summary>
    /// <param name="documentWithMarks">Модель документа с отметками.</param>
    /// <param name="wordDocument">Word документ.</param>
    public virtual void WriteWordDocumentToDocumentWithMarksDto(IDocumentMarksDto documentWithMarks, Aspose.Words.Document wordDocument)
    {
      var saveOptions = Aspose.Words.Saving.SaveOptions.CreateSaveOptions(Aspose.Words.SaveFormat.Docx);
      saveOptions.UpdateFields = false;
      var resultStream = new MemoryStream();
      wordDocument.Save(resultStream, saveOptions);
      documentWithMarks.Body = resultStream;
    }
    
    /// <summary>
    /// Получить координаты последнего вхождения строки в документ.
    /// </summary>
    /// <param name="pdfDocumentStream">Поток с входным документом в формате PDF.</param>
    /// <param name="searchablePagesNumber">Количество страниц для поиска строки.</param>
    /// <param name="searchString">Строка для поиска.</param>
    /// <returns>Структура с результатами поиска.</returns>
    /// <remarks>Ось X - горизонтальная, ось Y - вертикальная. Начало координат - левый нижний угол.</remarks>
    [Public]
    public virtual IPdfStringSearchResult GetLastStringEntryPosition(System.IO.Stream pdfDocumentStream,
                                                                     int searchablePagesNumber,
                                                                     string searchString)
    {
      var document = new Aspose.Pdf.Document(pdfDocumentStream);

      // Ограничение количества страниц, на которых будет искаться символ-якорь.
      var lastSearchablePage = document.Pages.Count > searchablePagesNumber ?
        document.Pages.Count - searchablePagesNumber : 0;

      for (var pageNumber = document.Pages.Count; pageNumber > lastSearchablePage; pageNumber--)
      {
        var page = document.Pages[pageNumber];
        var lastStringEntry = this.CreatePdfStamper().GetLastAnchorEntry(page, searchString);
        if (lastStringEntry == null)
          continue;
        var pageRectConsiderRotation = page.GetPageRect(true);

        var result = PdfStringSearchResult.Create();
        result.XIndent = lastStringEntry.Position.XIndent / PdfConverter.PdfStamper.DotsPerCm;
        result.YIndent = lastStringEntry.Position.YIndent / PdfConverter.PdfStamper.DotsPerCm;
        result.PageNumber = page.Number;
        result.PageWidth = pageRectConsiderRotation.Width / PdfConverter.PdfStamper.DotsPerCm;
        result.PageHeight = pageRectConsiderRotation.Height / PdfConverter.PdfStamper.DotsPerCm;
        result.PageCount = document.Pages.Count;

        return result;
      }

      return null;
    }

    /// <summary>
    /// Создать конвертер в PDF.
    /// </summary>
    /// <returns>Конвертер в PDF.</returns>
    public virtual Sungero.Docflow.Isolated.PdfConverter.Converter CreatePdfConverter()
    {
      return new PdfConverter.Converter();
    }
    
    /// <summary>
    /// Создать конвертер в PDF/A.
    /// </summary>
    /// <returns>Конвертер в PDF/A.</returns>
    public virtual Sungero.Docflow.Isolated.PdfConverter.ConverterPdfA CreatePdfAConverter()
    {
      return new PdfConverter.ConverterPdfA();
    }
    
    /// <summary>
    /// Создать экземпляр класса для простановки штампов.
    /// </summary>
    /// <returns>Экземпляр PdfStamper.</returns>
    public virtual Sungero.Docflow.Isolated.PdfConverter.PdfStamper CreatePdfStamper()
    {
      return new PdfConverter.PdfStamper();
    }
    
    /// <summary>
    /// Создать экземпляр класса для управления расширениями файлов.
    /// </summary>
    /// <returns>Экземпляр FileExtensionsManager.</returns>
    public virtual Sungero.Docflow.Isolated.PdfConverter.FileExtensionsManager CreateFileExtensionsManager()
    {
      return new PdfConverter.FileExtensionsManager();
    }
  }
}