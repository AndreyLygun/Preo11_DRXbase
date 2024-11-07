using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Aspose.Cells;
using Aspose.Imaging;
using Aspose.Pdf;
using Aspose.Pdf.Text;
using Aspose.Slides;
using Aspose.Words;
using Aspose.Words.Markup;
using Aspose.Words.Shaping;
using BitMiracle.LibTiff.Classic;
using Newtonsoft.Json;
using SkiaSharp;
using Sungero.Core;
using Sungero.Docflow.Structures.Module;

namespace Sungero.Docflow.Isolated.PdfConverter
{
  /// <summary>
  /// Класс для записи логов в процессе преобразования в PDF с отметками.
  /// </summary>
  public static class PdfConversionLogger
  {
    /// <summary>
    /// Записать в лог сообщение уровня Debug.
    /// </summary>
    /// <param name="documentWithMarks">Модель документа с отметками.</param>
    /// <param name="message">Сообщение.</param>
    public static void Debug(IDocumentMarksDto documentWithMarks, string message)
    {
      Logger.Debug($"{GetLogMessagePrefix(documentWithMarks)} {message}");
    }
    
    /// <summary>
    /// Записать в лог сообщение уровня Error.
    /// </summary>
    /// <param name="ex">Исключение.</param>
    /// <param name="documentWithMarks">Модель документа с отметками.</param>
    /// <param name="message">Сообщение.</param>
    public static void Error(Exception ex, IDocumentMarksDto documentWithMarks, string message)
    {
      Logger.Error($"{GetLogMessagePrefix(documentWithMarks)}. Body extension={documentWithMarks.BodyExtension}. {message}.", ex);
    }
    
    /// <summary>
    /// Получить префикс сообщения записи в лог.
    /// </summary>
    /// <param name="documentWithMarks">Модель документа с отметками.</param>
    /// <returns>Префикс сообщения записи в лог.</returns>
    public static string GetLogMessagePrefix(IDocumentMarksDto documentWithMarks)
    {
      return $"ConvertToPdf. Document={documentWithMarks.DocumentId}, Version={documentWithMarks.VersionId}.";
    }
  }
  
  /// <summary>
  /// Класс управления расширениями файлов.
  /// </summary>
  public class FileExtensionsManager
  {
    public const string Pdf = "pdf";
    public const string Docx = "docx";
    public const string Doc = "doc";
    public const string Rtf = "rtf";
    public const string Xls = "xls";
    public const string Xlsx = "xlsx";
    public const string Xlsm = "xlsm";
    public const string Odt = "odt";
    public const string Ods = "ods";
    public const string Txt = "txt";
    public const string Jpg = "jpg";
    public const string Jpeg = "jpeg";
    public const string Png = "png";
    public const string Bmp = "bmp";
    public const string Tif = "tif";
    public const string Tiff = "tiff";
    public const string Gif = "gif";
    public const string Html = "html";
    public const string Ppt = "ppt";
    public const string Pptx = "pptx";
    
    /// <summary>
    /// Расширения, для которых доступно преобразование в pdf.
    /// </summary>
    public List<string> PdfConversionAllowedExtensions { get; set; }
    
    /// <summary>
    /// Расширения, для которых доступен поиск якоря.
    /// </summary>
    public List<string> AnchorSearchAllowedExtensions { get; set; }
    
    /// <summary>
    /// Расширения, для которых доступен поиск якоря, ограниченный количеством страниц.
    /// </summary>
    public List<string> PageLimitedAnchorSearchAllowedExtensions { get; set; }
    
    /// <summary>
    /// Расширения, для которых доступно обновление тэгов документа.
    /// </summary>
    public List<string> UpdateDocumentTagsAllowedExtensions { get; set; }

    public FileExtensionsManager()
    {
      this.PdfConversionAllowedExtensions = new List<string>() {
        Pdf, Docx, Doc, Rtf, Xls, Xlsx, Xlsm, Odt, Ods, Txt, Jpg, Jpeg, Png, Bmp, Tif, Tiff, Gif, Html
      };
      this.AnchorSearchAllowedExtensions = new List<string>() {
        Pdf, Docx, Doc, Rtf, Xls, Xlsx, Odt, Ods, Txt
      };
      this.PageLimitedAnchorSearchAllowedExtensions = new List<string>() {
        Xlsx, Xls, Ods
      };
      this.UpdateDocumentTagsAllowedExtensions = new List<string>() {
        Docx, Doc
      };
    }
    
    /// <summary>
    /// Для расширения доступно преобразование в pdf.
    /// </summary>
    /// <param name="extension">Расширение.</param>
    /// <returns>True - доступно, False - иначе.</returns>
    public bool PdfConversionAllowed(string extension)
    {
      return this.PdfConversionAllowedExtensions.Contains(extension.ToLower());
    }
    
    /// <summary>
    /// Для расширения доступен поиск якоря.
    /// </summary>
    /// <param name="extension">Расширение.</param>
    /// <returns>True - доступно, False - иначе.</returns>
    public bool AnchorSearchAllowed(string extension)
    {
      return this.AnchorSearchAllowedExtensions.Contains(extension.ToLower());
    }
    
    /// <summary>
    /// Для расширения доступен поиск якоря ограниченный количеством страниц.
    /// </summary>
    /// <param name="extension">Расширение.</param>
    /// <returns>True - доступно, False - иначе.</returns>
    public bool PageLimitedAnchorSearchAllowed(string extension)
    {
      return this.PageLimitedAnchorSearchAllowedExtensions.Contains(extension.ToLower());
    }
    
    /// <summary>
    /// Для расширения доступно обновление тэгов документа.
    /// </summary>
    /// <param name="extension">Расширение.</param>
    /// <returns>True - доступно, False - иначе.</returns>
    public bool UpdateDocumentTagsAllowed(string extension)
    {
      return this.UpdateDocumentTagsAllowedExtensions.Contains(extension.ToLower());
    }
  }
  
  /// <summary>
  /// Базовый конвертер в pdf. Реализует общую логику конвейера преобразования в pdf для разных форматов.
  /// Специфическая логика предобработки и постобработки реализуется в классах-наследниках.
  /// </summary>
  public abstract class ConverterBase
  {
    #region Константы
    
    /// <summary>
    /// Минимальная совместимая версия PDF для корректного отображения отметки.
    /// </summary>
    public const string MinCompatibleVersion = "1.4";
    
    /// <summary>
    /// Ширина страницы, чтобы она была формата А4 (число взято из Aspose.Pdf).
    /// </summary>
    public const int PageWidth = 595;
    
    /// <summary>
    /// Высота страницы, чтобы она была формата А4 (число взято из Aspose.Pdf).
    /// </summary>
    public const int PageHeight = 842;
    
    /// <summary>
    /// Размеры левого, правого и верхнего поля страницы, чтобы она была формата А4 (число взято из Aspose.Pdf).
    /// </summary>
    public const int LeftRighTopMargin = 35;
    
    /// <summary>
    /// Размер нижнего поля страницы, чтобы она была формата А4 (число взято из Aspose.Pdf).
    /// </summary>
    public const int BottomMargin = 55;
    
    /// <summary>
    /// Имя нового экземпляра класса библиотеки LibTiff.
    /// </summary>
    public const string LibTiffInstanceName = "in-memory";
    
    /// <summary>
    /// Режим открытия файла библиотекой LibTiff.
    /// </summary>
    public const string LibTiffOpenMode = "r";
    
    #endregion
    
    #region Методы для заказной разработки
    
    /// <summary>
    /// Преобразовать изображение в pdf без подгона под а4.
    /// </summary>
    /// <param name="inputStream">Поток с картинкой.</param>
    /// <param name="resultStream">Поток для записи результата.</param>
    public virtual void ConvertImageToPdfWithoutScale(Stream inputStream, Stream resultStream)
    {

      var pdfDocument = new Aspose.Pdf.Document();
      var page = pdfDocument.Pages.Add();

      using (var img = System.Drawing.Image.FromStream(inputStream))
      {
        var imageHeight = img.Height * img.GetFrameCount(FrameDimension.Page);
        page.PageInfo.Height = imageHeight;
        page.PageInfo.Width = img.Width;
        page.PageInfo.Margin = new Aspose.Pdf.MarginInfo(0, 0, 0, 0);
        var image = new Aspose.Pdf.Image
        {
          ImageStream = inputStream,
          IsInNewPage = true,
          IsKeptWithNext = true,
          HorizontalAlignment = Aspose.Pdf.HorizontalAlignment.Center,
        };
        page.Paragraphs.Add(image);
        pdfDocument.Save(resultStream);
      }
    }
    
    /// <summary>
    /// Заполнить метаданные pdf документа.
    /// </summary>
    /// <param name="inputStream">Поток с pdf документом.</param>
    /// <param name="author">Автор документа.</param>
    /// <param name="creationDate">Дата создания документа.</param>
    /// <param name="modificationDate">Дата изменения документа.</param>
    /// <returns>Поток с документом.</returns>
    [Public]
    public virtual Stream FillPdfDocumentMetadata(Stream inputStream, string author, DateTime creationDate, DateTime modificationDate)
    {
      var resultStream = new MemoryStream();
      
      try
      {
        var document = new Aspose.Pdf.Document(inputStream);
        this.PreprocessPdfDocument(document);
        this.FillPdfDocumentMetadata(document, author, creationDate, modificationDate);
        this.SaveDocument(document, resultStream, document.PdfFormat);
      }
      catch (Exception ex)
      {
        resultStream.Close();
        Logger.Error("Cannot fill pdf document metadata", ex);
        throw new AppliedCodeException("Cannot fill pdf document metadata");
      }
      
      return resultStream;
    }
    
    #endregion
    
    #region Обработчики предупреждений о замене шрифтов
    
    /// <summary>
    /// Обработчик предупреждений о замене шрифтов при конвертации документов Word.
    /// </summary>
    public class WordDocumentSubstitutionWarnings : Aspose.Words.IWarningCallback
    {
      public WarningInfoCollection FontWarnings
      {
        get { return fontWarnings; }
        set { fontWarnings = value; }
      }
      
      private WarningInfoCollection fontWarnings;
      
      public WordDocumentSubstitutionWarnings()
      {
        this.FontWarnings = new Aspose.Words.WarningInfoCollection();
      }
      
      public void Warning(Aspose.Words.WarningInfo info)
      {
        if (info.WarningType == Aspose.Words.WarningType.FontSubstitution)
          this.FontWarnings.Warning(info);
      }
    }

    /// <summary>
    /// Обработчик предупреждений о замене шрифтов при конвертации документов Excel.
    /// </summary>
    public class ExcelDocumentSubstitutionWarnings : Aspose.Cells.IWarningCallback
    {
      public List<Aspose.Cells.WarningInfo> FontWarnings
      {
        get { return fontWarnings; }
        set { fontWarnings = value; }
      }
      
      private List<Aspose.Cells.WarningInfo> fontWarnings;
      
      public ExcelDocumentSubstitutionWarnings()
      {
        this.FontWarnings = new List<Aspose.Cells.WarningInfo>();
      }
      
      public void Warning(Aspose.Cells.WarningInfo info)
      {
        if (info.WarningType == Aspose.Cells.WarningType.FontSubstitution)
          this.FontWarnings.Add(info);
      }
    }

    #endregion
    
    #region Преобразование в pdf
    
    /// <summary>
    /// Проверить, поддерживается ли формат файла по его расширению.
    /// </summary>
    /// <param name="extension">Расширение файла.</param>
    /// <returns>True/false.</returns>
    [Obsolete("Метод не используется с 20.08.2024 и версии 4.11. Используйте метод PdfConversionAllowed(string) класса FileExtensionsManager.")]
    public static bool CheckIfExtensionIsSupported(string extension)
    {
      return new FileExtensionsManager().PdfConversionAllowed(extension);
    }
    
    /// <summary>
    /// Преобразовать документ в pdf.
    /// </summary>
    /// <param name="inputStream">Поток с телом документа.</param>
    /// <param name="extension">Расширение оригинала документа.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Документ в формате pdf.</returns>
    public virtual Aspose.Pdf.Document GeneratePdfDocument(Stream inputStream, string extension, params string[] saveParameters)
    {
      try
      {
        var pdfStream = this.GeneratePdf(inputStream, extension, saveParameters);
        return new Aspose.Pdf.Document(pdfStream);
      }
      catch (Exception ex)
      {
        Logger.Error("Cannot convert file to pdf", ex);
        throw new AppliedCodeException("Cannot convert file to pdf");
      }
      finally
      {
        inputStream.Close();
      }
    }
    
    /// <summary>
    /// Преобразовать документ в pdf.
    /// </summary>
    /// <param name="inputStream">Поток с входным документом.</param>
    /// <param name="extension">Расширение входного документа.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Поток с документом.</returns>
    public virtual Stream GeneratePdf(Stream inputStream, string extension, params string[] saveParameters)
    {
      try
      {
        switch (extension.ToLower())
        {
          case FileExtensionsManager.Pdf:
            return this.ConvertPdfToPdf(inputStream, saveParameters);
          case FileExtensionsManager.Doc:
          case FileExtensionsManager.Docx:
          case FileExtensionsManager.Odt:
          case FileExtensionsManager.Rtf:
            return this.ConvertWordToPdf(inputStream, saveParameters);
          case FileExtensionsManager.Xls:
          case FileExtensionsManager.Xlsx:
          case FileExtensionsManager.Xlsm:
          case FileExtensionsManager.Ods:
            return this.ConvertExcelToPdf(inputStream, saveParameters);
          case FileExtensionsManager.Ppt:
          case FileExtensionsManager.Pptx:
            return this.ConvertPresentationToPdf(inputStream, saveParameters);
          case FileExtensionsManager.Jpg:
          case FileExtensionsManager.Jpeg:
          case FileExtensionsManager.Png:
          case FileExtensionsManager.Bmp:
            return this.ConvertImageToPdf(inputStream, saveParameters);
          case FileExtensionsManager.Tiff:
          case FileExtensionsManager.Tif:
            return this.ConvertScanToPdf(inputStream, saveParameters);
          case FileExtensionsManager.Gif:
            return this.ConvertGifToPdf(inputStream, saveParameters);
          case FileExtensionsManager.Txt:
            return this.ConvertTxtToPdf(inputStream, saveParameters);
          case FileExtensionsManager.Html:
            return this.ConvertHtmlToPdf(inputStream);
          default:
            return null;
        }
      }
      catch (Exception ex)
      {
        Logger.Error("Cannot convert file to pdf", ex);
        throw new AppliedCodeException("Cannot convert file to pdf");
      }
      finally
      {
        inputStream.Close();
      }
    }
    
    #region Преобразование pdf
    
    /// <summary>
    /// Преобразовать pdf документ в pdf.
    /// </summary>
    /// <param name="inputStream">Поток с исходным документом.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Поток для записи результата.</returns>
    public virtual Stream ConvertPdfToPdf(Stream inputStream, params string[] saveParameters)
    {
      var resultStream = new MemoryStream();
      try
      {
        var document = new Aspose.Pdf.Document(inputStream);
        this.PreprocessPdfDocument(document);
        this.Validate(document);
        var pdfFormat = this.GetPdfFormat(document, saveParameters);
        this.SaveDocument(document, resultStream, pdfFormat);
      }
      catch (Exception ex)
      {
        resultStream.Close();
        Logger.Error("Cannot convert pdf", ex);
        throw new AppliedCodeException("Cannot convert pdf");
      }
      return resultStream;
    }
    
    /// <summary>
    /// Получить формат pdf.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Формат pdf.</returns>
    public virtual Aspose.Pdf.PdfFormat GetPdfFormat(Aspose.Pdf.Document document, params string[] saveParameters)
    {
      return document.PdfFormat;
    }
    
    /// <summary>
    /// Обработка pdf-документа перед преобразованием в pdf.
    /// </summary>
    /// <param name="document">Документ.</param>
    public virtual void PreprocessPdfDocument(Aspose.Pdf.Document document)
    {
      // HACK fix бага Aspose PDFNET-44378.
      this.FillModifyDate(document);
      this.UpgradePdfVersion(document);
    }
    
    /// <summary>
    /// Заполнить дату изменения pdf документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    public virtual void FillModifyDate(Aspose.Pdf.Document document)
    {
      try
      {
        // Если свойство отсутствует или его значение пустое, то при чтении Aspose сгенерирует исключение.
        var modDate = document.Info.ModDate;
      }
      catch (Exception ex)
      {
        Logger.Debug("Document modify date is empty", ex);
        document.Info.ModDate = DateTime.Now;
        document.Save();
      }
    }
    
    /// <summary>
    /// Заполнить метаданные pdf-документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="author">Автор документа.</param>
    /// <param name="creationDate">Дата создания документа.</param>
    /// <param name="modificationDate">Дата изменения документа.</param>
    [Public]
    public virtual void FillPdfDocumentMetadata(Aspose.Pdf.Document document,
                                                string author,
                                                DateTime creationDate,
                                                DateTime modificationDate)
    {
      document.Info.Author = author;
      document.Info.CreationDate = creationDate;
      document.Info.ModDate = modificationDate;
    }
    
    /// <summary>
    /// Проверить документ на валидность перед конвертацией в pdf.
    /// </summary>
    /// <param name="document">Документ.</param>
    public virtual void Validate(Aspose.Pdf.Document document)
    {
      
    }
    
    /// <summary>
    /// Сохранить поток в pdf-документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="resultStream">Поток.</param>
    /// <param name="pdfFormat">Формат pdf.</param>
    public virtual void SaveDocument(Aspose.Pdf.Document document,
                                     MemoryStream resultStream,
                                     Aspose.Pdf.PdfFormat pdfFormat)
    {
      document.Save(resultStream);
    }
    
    #endregion
    
    #region Преобразование текстовых документов с форматированием
    
    /// <summary>
    /// Преобразовать текстовый документ в pdf.
    /// </summary>
    /// <param name="inputStream">Поток с текстовым документом.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Поток для записи результата.</returns>
    public virtual Stream ConvertWordToPdf(Stream inputStream, params string[] saveParameters)
    {
      var resultStream = new MemoryStream();
      try
      {
        var word = new Aspose.Words.Document(inputStream);
        this.PreprocessWordDocument(word);
        this.Validate(word);
        var saveOptions = this.GetWordSaveOptions(word, saveParameters);
        // Включить поддержку кернинга с помощью библиотеки HarfBuzz.
        word.LayoutOptions.TextShaperFactory = Aspose.Words.Shaping.HarfBuzz.HarfBuzzTextShaperFactory.Instance;
        // Подключить обработчик предупреждений Aspose о замене или отсутствии шрифтов.
        var substitutionWarningHandler = new WordDocumentSubstitutionWarnings();
        word.WarningCallback = substitutionWarningHandler;
        word.Save(resultStream, saveOptions);
        if (substitutionWarningHandler.FontWarnings.Any())
        {
          var message = string.Join(" ", substitutionWarningHandler.FontWarnings.Select(x => x.Description));
          Logger.DebugFormat("ConvertWordToPdf. {0}", message);
        }
      }
      catch (Exception ex)
      {
        resultStream.Close();
        Logger.Error("Cannot convert word to pdf", ex);
        throw new AppliedCodeException("Cannot convert word to pdf");
      }
      return resultStream;
    }
    
    /// <summary>
    /// Обработка текстового документа перед конвертацией в pdf.
    /// </summary>
    /// <param name="word">Документ.</param>
    public virtual void PreprocessWordDocument(Aspose.Words.Document word)
    {
      
    }
    
    /// <summary>
    /// Получить опции сохранения для текстового документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Опции сохранения для текстового документа.</returns>
    public virtual Aspose.Words.Saving.SaveOptions GetWordSaveOptions(Aspose.Words.Document document, params string[] saveParameters)
    {
      var saveOptions = Aspose.Words.Saving.SaveOptions.CreateSaveOptions(Aspose.Words.SaveFormat.Pdf);
      // При обновлении вычисляемых полей они начинают отображаться на английском. Поэтому автообновление нужно отключить (142678).
      saveOptions.UpdateFields = false;
      return saveOptions;
    }
    
    /// <summary>
    /// Проверить документ на валидность перед конвертацией в pdf.
    /// </summary>
    /// <param name="word">Документ.</param>
    public virtual void Validate(Aspose.Words.Document word)
    {
      
    }
    
    #endregion
    
    #region Преобразование таблиц
    
    /// <summary>
    /// Преобразовать таблицы excel в pdf.
    /// </summary>
    /// <param name="inputStream">Поток с документом-таблицей.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Поток для записи результата.</returns>
    public virtual Stream ConvertExcelToPdf(Stream inputStream, params string[] saveParameters)
    {
      var resultStream = new MemoryStream();
      try
      {
        var workbook = new Aspose.Cells.Workbook(inputStream);
        var saveOptions = this.GetCellsSaveOptions(workbook, saveParameters);
        this.Validate(workbook);
        // Подключить обработчик предупреждений Aspose о замене шрифтов.
        var substitutionWarningHandler = new ExcelDocumentSubstitutionWarnings();
        saveOptions.WarningCallback = substitutionWarningHandler;
        workbook.Save(resultStream, saveOptions);
        if (substitutionWarningHandler.FontWarnings.Any())
        {
          var message = string.Join(" ", substitutionWarningHandler.FontWarnings.Select(x => x.Description));
          Logger.DebugFormat("ConvertExcelToPdf. {0}", message);
        }
      }
      catch (Exception ex)
      {
        resultStream.Close();
        Logger.Error("Cannot convert excel to pdf", ex);
        throw new AppliedCodeException("Cannot convert excel to pdf");
      }
      return resultStream;
    }
    
    /// <summary>
    /// Получить опции сохранения для таблицы.
    /// </summary>
    /// <param name="workbook">Документ.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Опции сохранения для таблицы.</returns>
    public virtual Aspose.Cells.PdfSaveOptions GetCellsSaveOptions(Aspose.Cells.Workbook workbook, params string[] saveParameters)
    {
      var saveOptions = new Aspose.Cells.PdfSaveOptions();
      if (workbook.FileFormat == FileFormatType.Ods)
        saveOptions.AllColumnsInOnePagePerSheet = true;
      return saveOptions;
    }
    
    /// <summary>
    /// Проверить документ на валидность перед конвертацией в pdf.
    /// </summary>
    /// <param name="workbook">Документ.</param>
    public virtual void Validate(Aspose.Cells.Workbook workbook)
    {
      
    }
    
    #endregion
    
    #region Преобразование презентаций
    
    /// <summary>
    /// Преобразовать презентацию в pdf.
    /// </summary>
    /// <param name="inputStream">Поток с презентацией.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Поток для записи результата.</returns>
    public virtual Stream ConvertPresentationToPdf(Stream inputStream, params string[] saveParameters)
    {
      var resultStream = new MemoryStream();
      try
      {
        var presentation = new Aspose.Slides.Presentation(inputStream);
        var saveOptions = this.GetSlidesSaveOptions(presentation, saveParameters);
        this.Validate(presentation);
        presentation.Save(resultStream, Aspose.Slides.Export.SaveFormat.Pdf, saveOptions);
      }
      catch (Exception ex)
      {
        resultStream.Close();
        Logger.Error("Cannot convert presentation to pdf", ex);
        throw new AppliedCodeException("Cannot convert presentation to pdf");
      }
      return resultStream;
    }
    
    /// <summary>
    /// Получить опции сохранения для презентации.
    /// </summary>
    /// <param name="presentation">Презентация.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Опции сохранения для презентации.</returns>
    public virtual Aspose.Slides.Export.PdfOptions GetSlidesSaveOptions(Aspose.Slides.Presentation presentation, params string[] saveParameters)
    {
      var saveOptions = new Aspose.Slides.Export.PdfOptions();
      return saveOptions;
    }
    
    /// <summary>
    /// Проверить документ на валидность перед конвертацией в pdf.
    /// </summary>
    /// <param name="presentation">Документ.</param>
    public virtual void Validate(Aspose.Slides.Presentation presentation)
    {
      
    }
    
    #endregion
    
    #region Преобразование изображений
    
    /// <summary>
    /// Преобразовать изображение в pdf.
    /// </summary>
    /// <param name="inputStream">Поток с картинкой.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Поток для записи результата.</returns>
    public virtual Stream ConvertImageToPdf(Stream inputStream, params string[] saveParameters)
    {
      var resultStream = new MemoryStream();
      try
      {
        var document = new Aspose.Words.Document();
        var builder = this.GetDocumentBuilder(document);
        this.AddImage(inputStream, builder);
        var options = this.GetImageSaveOptions(document, saveParameters);
        document.Save(resultStream, options);
      }
      catch (Exception ex)
      {
        resultStream.Close();
        Logger.Error("Cannot convert image to pdf", ex);
        throw new AppliedCodeException("Cannot convert image to pdf");
      }
      return resultStream;
    }
    
    /// <summary>
    /// Получить обработчик документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Обработчик документа.</returns>
    public virtual Aspose.Words.DocumentBuilder GetDocumentBuilder(Aspose.Words.Document document)
    {
      var builder = new DocumentBuilder(document);
      builder.PageSetup.PageWidth = PageWidth;
      builder.PageSetup.PageHeight = PageHeight;
      builder.PageSetup.LeftMargin = builder.PageSetup.RightMargin = builder.PageSetup.TopMargin = LeftRighTopMargin;
      builder.PageSetup.BottomMargin = BottomMargin;
      return builder;
    }
    
    /// <summary>
    /// Добавить картинку в документ.
    /// </summary>
    /// <param name="inputStream">Поток с документом.</param>
    /// <param name="builder">Обработчик документа.</param>
    public virtual void AddImage(Stream inputStream, Aspose.Words.DocumentBuilder builder)
    {
      inputStream.Flush();
      inputStream.Position = 0;
      using (var image = Aspose.Imaging.Image.Load(inputStream))
      {
        var pageSize = this.CalculatePageSize(image.Width, image.Height, builder);
        builder.InsertImage(inputStream, pageSize.Width, pageSize.Height);
      }
    }
    
    /// <summary>
    /// Рассчитать размеры страницы.
    /// </summary>
    /// <param name="imageWidth">Ширина картинки.</param>
    /// <param name="imageHeight">Высота картинки.</param>
    /// <param name="builder">Обработчик документа.</param>
    /// <returns>Масштабированные размеры страницы.</returns>
    public virtual ScaledPageSize CalculatePageSize(double imageWidth, double imageHeight, Aspose.Words.DocumentBuilder builder)
    {
      var resultedPageSize = new ScaledPageSize();
      builder.PageSetup.Orientation = imageWidth > imageHeight ? Aspose.Words.Orientation.Landscape : Aspose.Words.Orientation.Portrait;
      var maxHeight = builder.PageSetup.PageHeight - builder.PageSetup.TopMargin - builder.PageSetup.BottomMargin;
      var maxWidth = builder.PageSetup.PageWidth - builder.PageSetup.LeftMargin - builder.PageSetup.RightMargin;
      var ratio = Math.Min(maxWidth / imageWidth, maxHeight / imageHeight);
      resultedPageSize.Width = imageWidth * ratio;
      resultedPageSize.Height = imageHeight * ratio;
      return resultedPageSize;
    }
    
    /// <summary>
    /// Получить опции сохранения для картинки.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Опции сохранения для картинки.</returns>
    public virtual Aspose.Words.Saving.PdfSaveOptions GetImageSaveOptions(Aspose.Words.Document document, params string[] saveParameters)
    {
      var saveOptions = (Aspose.Words.Saving.PdfSaveOptions)Aspose.Words.Saving.PdfSaveOptions.CreateSaveOptions(Aspose.Words.SaveFormat.Pdf);
      // Отключить понижение качества вставляемых картинок.
      saveOptions.DownsampleOptions.DownsampleImages = false;
      return saveOptions;
    }
    
    #endregion
    
    #region Преобразование сканов

    /// <summary>
    /// Преобразовать изображение tiff/tif в pdf.
    /// </summary>
    /// <param name="inputStream">Поток с документом формата tiff/tif.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Поток для записи результата.</returns>
    public virtual Stream ConvertScanToPdf(Stream inputStream, params string[] saveParameters)
    {
      var resultStream = new MemoryStream();
      try
      {
        var document = new Aspose.Words.Document();
        var builder = this.GetDocumentBuilder(document);
        var convertedInPngScans = this.ConvertScanToPngs(inputStream);
        this.AddImages(builder, convertedInPngScans);
        var saveOptions = this.GetImageSaveOptions(document, saveParameters);
        document.Save(resultStream, saveOptions);
      }
      catch (Exception ex)
      {
        resultStream.Close();
        Logger.Error("Cannot convert tiff to pdf", ex);
        throw new AppliedCodeException("Cannot convert tiff to pdf");
      }
      
      return resultStream;
    }
    
    /// <summary>
    /// Добавить картинки в документ.
    /// </summary>
    /// <param name="builder">Обработчик документа.</param>
    /// <param name="inputStream">Поток с документом.</param>
    /// <remarks>Во входящем потоке с документом - несколько картинок формата tiff/tif.</remarks>
    public virtual void AddImages(DocumentBuilder builder, Stream inputStream)
    {
      using (var image = System.Drawing.Image.FromStream(inputStream))
      {
        var dimension = new FrameDimension(image.FrameDimensionsList[0]);
        var framesCount = image.GetFrameCount(dimension);
        for (int frameIdx = 0; frameIdx < framesCount; frameIdx++)
        {
          if (frameIdx != 0)
            builder.InsertBreak(BreakType.SectionBreakNewPage);
          image.SelectActiveFrame(dimension, frameIdx);
          var pageSize = this.CalculatePageSize(image.Width, image.Height, builder);
          
          using (var memoryStream = new MemoryStream())
          {
            image.Save(memoryStream, ImageFormat.Png);
            var bytes = memoryStream.ToArray();
            builder.InsertImage(bytes, pageSize.Width, pageSize.Height);
          }
        }
      }
    }
    
    /// <summary>
    /// Добавить картинки в документ.
    /// </summary>
    /// <param name="builder">Обработчик документа.</param>
    /// <param name="inputStreams">Список потоков со страницами документа.</param>
    /// <remarks>Во входящих потоках - страницы документа в формате png (по одной странице в каждом потоке).
    /// Перегрузка добавлена для корректного преобразования tiff/tif в pdf на linux с использованием библиотеки LibTiff.</remarks>
    public virtual void AddImages(DocumentBuilder builder, List<MemoryStream> inputStreams)
    {
      for (int frameIdx = 0; frameIdx < inputStreams.Count; frameIdx++)
      {
        var inputStream = inputStreams[frameIdx];
        using (var image = System.Drawing.Image.FromStream(inputStream))
        {
          if (frameIdx != 0)
            builder.InsertBreak(BreakType.SectionBreakNewPage);
          
          var pageSize = this.CalculatePageSize(image.Width, image.Height, builder);
          builder.InsertImage(inputStream, pageSize.Width, pageSize.Height);
        }
      }
    }
    
    /// <summary>
    /// Конвертация tiff/tif в список потоков изображений png.
    /// </summary>
    /// <param name="inputStream">Поток с документом формата tiff/tif.</param>
    /// <returns>Список потоков, содержащих изображения png.</returns>
    public virtual List<MemoryStream> ConvertScanToPngs(Stream inputStream)
    {
      var resultStreams = new List<MemoryStream>();
      var tiff = Tiff.ClientOpen(LibTiffInstanceName, LibTiffOpenMode, inputStream, new TiffStream());
      short directoriesCount = tiff.NumberOfDirectories();
      for (short dirIdx = 0; dirIdx < directoriesCount; dirIdx++)
      {
        tiff.SetDirectory(dirIdx);
        FieldValue[] value = tiff.GetField(TiffTag.IMAGEWIDTH);
        int width = value[0].ToInt();
        value = tiff.GetField(TiffTag.IMAGELENGTH);
        int height = value[0].ToInt();
        int[] raster = new int[height * width];
        
        if (!tiff.ReadRGBAImage(width, height, raster))
          throw new AppliedCodeException("Cannot read the image and decode it into RGBA format raster.");
        
        resultStreams.Add(this.ConvertDecodedRasterToPng(raster, width, height));
      }
      
      return resultStreams;
    }
    
    /// <summary>
    /// Конвертировать декодированные данные в поток данных изображения.
    /// </summary>
    /// <param name="raster">Декодированные данные.</param>
    /// <param name="width">Ширина изображения.</param>
    /// <param name="height">Высота изображения.</param>
    /// <returns>Поток сконвертированного изображения.</returns>
    public virtual MemoryStream ConvertDecodedRasterToPng(int[] raster, int width, int height)
    {
      using (Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
      {
        var rectangle = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
        BitmapData bmpdata = bmp.LockBits(rectangle, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
        byte[] bits = new byte[bmpdata.Stride * bmpdata.Height];
        
        for (int y = 0; y < bmp.Height; y++)
        {
          int rasterOffset = y * bmp.Width;
          int bitsOffset = (bmp.Height - y - 1) * bmpdata.Stride;
          for (int x = 0; x < bmp.Width; x++)
          {
            int rgba = raster[rasterOffset++];
            bits[bitsOffset++] = (byte)((rgba >> 16) & 0xff);
            bits[bitsOffset++] = (byte)((rgba >> 8) & 0xff);
            bits[bitsOffset++] = (byte)(rgba & 0xff);
            bits[bitsOffset++] = (byte)((rgba >> 24) & 0xff);
          }
        }
        
        Marshal.Copy(bits, 0, bmpdata.Scan0, bits.Length);
        bmp.UnlockBits(bmpdata);
        var memoryStream = new MemoryStream();
        var encoder = this.GetEncoder("image/png");
        var encoderParams = this.GetEncoderParameters(EncoderValue.CompressionLZW);
        bmp.Save(memoryStream, encoder, encoderParams);
        encoderParams.Dispose();
        
        return memoryStream;
      }
    }
    
    #endregion
    
    #region Преобразование картинок формата gif
    
    /// <summary>
    /// Преобразовать gif в pdf.
    /// </summary>
    /// <param name="inputStream">Поток с документом формата gif.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Поток для записи результата.</returns>
    public virtual Stream ConvertGifToPdf(Stream inputStream, params string[] saveParameters)
    {
      var resultStream = new MemoryStream();
      try
      {
        var doc = new Aspose.Words.Document();
        var builder = this.GetDocumentBuilder(doc);
        this.AddGif(inputStream, builder);
        var saveOptions = this.GetImageSaveOptions(doc, saveParameters);
        doc.Save(resultStream, saveOptions);
      }
      catch (Exception ex)
      {
        resultStream.Close();
        Logger.Error("Cannot convert gif to pdf", ex);
        throw new AppliedCodeException("Cannot convert gif to pdf");
      }
      return resultStream;
    }
    
    /// <summary>
    /// Вставить в документ картинку формата gif.
    /// </summary>
    /// <param name="inputStream">Поток памяти с картинкой.</param>
    /// <param name="builder">Обработчик документа.</param>
    public virtual void AddGif(Stream inputStream, Aspose.Words.DocumentBuilder builder)
    {
      inputStream.Flush();
      inputStream.Position = 0;
      using (var skiaSharpStream = new SKManagedStream(inputStream))
        using (var codec = SKCodec.Create(skiaSharpStream))
      {
        int frameCount = codec.FrameCount;
        for (int frame = 0; frame < frameCount; frame++)
        {
          SKImageInfo imageInfo = new SKImageInfo(codec.Info.Width, codec.Info.Height);
          var bitmap = new SKBitmap(imageInfo);
          IntPtr pointer = bitmap.GetPixels();
          SKCodecOptions codecOptions = new SKCodecOptions(frame);
          codec.GetPixels(imageInfo, pointer, codecOptions);

          using (SKImage image = SKImage.FromBitmap(bitmap))
          {
            using (SKData data = image.Encode())
            {
              byte[] imageByteArray = data.ToArray();
              if (frame != 0)
                builder.InsertBreak(BreakType.SectionBreakNewPage);
              builder.InsertImage(imageByteArray);
            }
          }
        }
      }
    }
    
    #endregion
    
    #region Преобразование текстовых документов
    
    /// <summary>
    /// Преобразовать txt в pdf.
    /// </summary>
    /// <param name="inputStream">Поток с документом формата txt.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Поток для записи результата.</returns>
    /// <remarks>Преобразование происходит в две попытки. Если не смогли сохранить документ в pdf, используя word,
    /// то создаем новый pdf-документ и копируем в него текст из исходного документа.</remarks>
    public virtual Stream ConvertTxtToPdf(Stream inputStream, params string[] saveParameters)
    {
      var resultStream = new MemoryStream();
      try
      {
        var wordDocument = new Aspose.Words.Document(inputStream);
        var saveOptions = this.GetWordSaveOptions(wordDocument, saveParameters);
        wordDocument.Save(resultStream, saveOptions);
      }
      catch (Aspose.Words.UnsupportedFileFormatException ex)
      {
        Logger.Debug("Cannot txt to pdf via Aspose.Words. Document converted via Aspose.Pdf", ex);
        
        // Преобразовать, используя Aspose.Pdf.
        // Aspose.Words мог не распознать формат txt содержащим только пробелы.
        // Aspose.Pdf преобразует, не соблюдая разбивку по страницам и форматирование пробелами.
        var pdfDocument = new Aspose.Pdf.Document();
        this.AddTextToPdfDocument(inputStream, pdfDocument);
        pdfDocument.Save(resultStream);
      }
      catch (Exception ex)
      {
        resultStream.Close();
        Logger.Error("Cannot convert txt to pdf", ex);
        throw new AppliedCodeException("Cannot convert txt to pdf");
      }
      return resultStream;
    }
    
    /// <summary>
    /// Добавить текст в pdf-документ.
    /// </summary>
    /// <param name="inputStream">Поток памяти с текстом.</param>
    /// <param name="pdfDocument">Pdf-документ.</param>
    public void AddTextToPdfDocument(Stream inputStream, Aspose.Pdf.Document pdfDocument)
    {
      var page = pdfDocument.Pages.Add();
      using (TextReader textReader = new StreamReader(inputStream))
      {
        var text = new TextFragment(textReader.ReadToEnd());
        page.Paragraphs.Add(text);
      }
    }
    
    #endregion
    
    /// <summary>
    /// Попытаться создать Aspose.Words.Document.
    /// </summary>
    /// <param name="stream">Поток.</param>
    /// <param name="document">Документ Aspose.Words.Document.</param>
    /// <returns>True - создание прошло успешно. False - иначе.</returns>
    public static bool TryCreateWordDocument(Stream stream, out Aspose.Words.Document document)
    {
      try
      {
        var bodyStream = new MemoryStream();
        stream.CopyTo(bodyStream);
        document = new Aspose.Words.Document(bodyStream);
        return true;
      }
      catch (Exception ex)
      {
        Logger.Error(ex, "TryCreateWordDocument. An error occured");
        document = null;
        return false;
      }
    }
    
    /// <summary>
    /// Попытаться создать Aspose.Pdf.Document.
    /// </summary>
    /// <param name="stream">Поток.</param>
    /// <param name="document">Документ Aspose.Pdf.Document.</param>
    /// <returns>True - создание прошло успешно. False - иначе.</returns>
    public static bool TryCreatePdfDocument(Stream stream, out Aspose.Pdf.Document document)
    {
      try
      {
        var bodyStream = new MemoryStream();
        stream.CopyTo(bodyStream);
        document = new Aspose.Pdf.Document(bodyStream);
        return true;
      }
      catch (Exception ex)
      {
        Logger.Error(ex, "TryCreatePdfDocument. An error occured");
        document = null;
        return false;
      }
    }
    
    /// <summary>
    /// Преобразовать html в pdf.
    /// </summary>
    /// <param name="inputStream">Поток с документом формата html.</param>
    /// <returns>Поток для записи результата.</returns>
    public virtual Stream ConvertHtmlToPdf(Stream inputStream)
    {
      var resultStream = new MemoryStream();
      try
      {
        var htmlDocument = new Aspose.Words.Document(inputStream, new Aspose.Words.Loading.HtmlLoadOptions());
        htmlDocument.Save(resultStream, Aspose.Words.SaveFormat.Pdf);
      }
      catch (Exception ex)
      {
        resultStream.Close();
        Logger.Error("Cannot convert html to pdf", ex);
        throw new AppliedCodeException("Cannot convert html to pdf");
      }
      return resultStream;
    }
    
    /// <summary>
    /// Получить информацию о кодировке и декодировке изображения.
    /// </summary>
    /// <param name="mimeType">Строка, содержащая тип MIME.</param>
    /// <returns>Информация о кодировке и декодировке изображения.</returns>
    public virtual ImageCodecInfo GetEncoder(string mimeType)
    {
      return ImageCodecInfo.GetImageEncoders()
        .FirstOrDefault(t => string.Equals(t.MimeType, mimeType, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Получить параметры кодировщика.
    /// </summary>
    /// <param name="compression">Метод сжатия изображения.</param>
    /// <returns>Параметры кодировщика.</returns>
    public virtual EncoderParameters GetEncoderParameters(EncoderValue compression)
    {
      var countParameters = 1;
      var encoderParams = new EncoderParameters(countParameters);
      encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Compression, (long)compression);
      return encoderParams;
    }
    
    public void UpgradePdfVersion(Aspose.Pdf.Document document)
    {
      if (!document.IsPdfaCompliant)
      {
        // Получить версию стандарта PDF из свойств документа. Достаточно первых двух чисел, разделённых точкой.
        var versionRegex = new Regex(@"^\d{1,2}\.\d{1,2}");
        var pdfVersionAsString = versionRegex.Match(document.Version).Value;
        var minCompatibleVersion = Version.Parse(MinCompatibleVersion);

        if (Version.TryParse(pdfVersionAsString, out Version version) && version < minCompatibleVersion)
        {
          Logger.DebugFormat("GetUpgradedPdf. Convert Pdf version to 1.4. Current version is {0}", version);
          using (var convertLog = new MemoryStream())
          {
            var options = new Aspose.Pdf.PdfFormatConversionOptions(Aspose.Pdf.PdfFormat.v_1_4);
            options.LogStream = convertLog;
            document.Convert(options);
          }
        }
      }
      document.Save();
    }
    
    #endregion
  }
  
  /// <summary>
  /// Конвертер в pdf. Реализует специфичную логику предобработки и постобработки тела документа при преобразовании в pdf.
  /// </summary>
  public class Converter : ConverterBase
  {
    #region Преобразование в pdf
    
    public override void PreprocessPdfDocument(Aspose.Pdf.Document document)
    {
      base.PreprocessPdfDocument(document);
      if (document.IsPdfaCompliant)
        document.RemovePdfaCompliance();
    }
    
    /// <summary>
    /// Обработка текстового документа перед конвертацией в pdf.
    /// </summary>
    /// <param name="word">Документ.</param>
    public override void PreprocessWordDocument(Aspose.Words.Document word)
    {
      word.AcceptAllRevisions();
      var comments = word.GetChildNodes(NodeType.Comment, true);
      comments.Clear();
    }
    
    #endregion
    
    #region Проверка наличия текстового слоя
    
    /// <summary>
    /// Проверить, есть ли в документе текстовый слой.
    /// </summary>
    /// <param name="inputStream">Поток с входным документом формата PDF.</param>
    /// <returns>True, если есть, иначе - false.</returns>
    /// <remarks>Наличие текста проверяется только на первой странице.</remarks>
    public virtual bool CheckDocumentTextLayer(Stream inputStream)
    {
      try
      {
        var document = new Aspose.Pdf.Document(inputStream);
        var textSelector = new Aspose.Pdf.OperatorSelector(new Aspose.Pdf.Operators.TextShowOperator());
        document.Pages[1].Contents.Accept(textSelector);
        return textSelector.Selected.Count != 0;
      }
      catch (Exception ex)
      {
        Logger.Error("Cannot check if document has text layer", ex);
        throw new AppliedCodeException("Cannot check if document has text layer");
      }
      finally
      {
        inputStream.Close();
      }
    }

    #endregion
  }
  
  /// <summary>
  /// Конвертер в pdf/a. Реализует логику предобработки тела документа и сохранение в pdf/a.
  /// </summary>
  public class ConverterPdfA : ConverterBase
  {
    #region Константы
    
    public const double MarginForImages = 0.0;
    
    #endregion
    
    #region Поля и свойства
    
    /// <summary>
    /// Список ошибок при встраивании шрифтов в pdf.
    /// </summary>
    /// <value>По умолчанию пустой список. На событии замены шрифта добавляется текст ошибки.</value>
    public List<string> FontSubstitutionErrors { get; private set; }

    #endregion
    
    #region Преобразование в pdf/a
    
    #region Преобразование pdf
    
    public override void PreprocessPdfDocument(Aspose.Pdf.Document document)
    {
      base.PreprocessPdfDocument(document);
      this.EmbedFonts(document);
    }
    
    /// <summary>
    /// Встроить шрифты.
    /// </summary>
    /// <param name="document">Доумент.</param>
    public virtual void EmbedFonts(Aspose.Pdf.Document document)
    {
      document.EmbedStandardFonts = true;
      var fonts = document.FontUtilities.GetAllFonts();
      foreach (var font in fonts)
      {
        if (font.IsAccessible && !font.IsEmbedded)
          font.IsEmbedded = true;
        else if (!font.IsAccessible && !font.IsEmbedded && !font.IsSubset)
          throw new AppliedCodeException(string.Format("Font {0} not found", font.FontName));
      }
      
      this.SetSubstitutionHandler(document);
      document.Save();
    }
    
    /// <summary>
    /// Установить обработчик события замены шрифтов при конвертации документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    public virtual void SetSubstitutionHandler(Aspose.Pdf.Document document)
    {
      this.FontSubstitutionErrors = new List<string>();
      Aspose.Pdf.Document.FontSubstitutionHandler substitutionHandler = new Aspose.Pdf.Document.FontSubstitutionHandler(this.OnFontSubstitution);
      document.FontSubstitution += substitutionHandler;
    }
    
    /// <summary>
    /// Обработчик события замены шрифта.
    /// </summary>
    /// <param name="oldFont">Старый шрифт.</param>
    /// <param name="newFont">Новый шрифт.</param>
    public virtual void OnFontSubstitution(Aspose.Pdf.Text.Font oldFont, Aspose.Pdf.Text.Font newFont)
    {
      this.FontSubstitutionErrors.Add(string.Format("Замена шрифта не допускается. Попытка заменить шрифт '{0}' на '{1}'", (object)oldFont.FontName, (object)newFont.FontName));
    }
    
    /// <summary>
    /// Сохранить поток в pdf-документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="resultStream">Поток.</param>
    /// <param name="pdfFormat">Формат pdf.</param>
    public override void SaveDocument(Aspose.Pdf.Document document,
                                      MemoryStream resultStream,
                                      Aspose.Pdf.PdfFormat pdfFormat)
    {
      using (var convertLog = new MemoryStream())
      {
        document.Convert(convertLog, pdfFormat, ConvertErrorAction.Delete);
        document.Save(resultStream);
      }
    }
    
    #endregion
    
    #region Преобразование текстовых документов с форматированием
    
    /// <summary>
    /// Получить опции сохранения для текстового документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Опции сохранения для текстового документа.</returns>
    public override Aspose.Words.Saving.SaveOptions GetWordSaveOptions(Aspose.Words.Document document, params string[] saveParameters)
    {
      var saveOptions = (Aspose.Words.Saving.PdfSaveOptions)base.GetWordSaveOptions(document);
      switch (saveParameters[0])
      {
        case "v1A":
          saveOptions.Compliance = Aspose.Words.Saving.PdfCompliance.PdfA1a;
          break;
        case "v1B":
          saveOptions.Compliance = Aspose.Words.Saving.PdfCompliance.PdfA1b;
          break;
        default:
          throw new AppliedCodeException(string.Format("Converting to {0} not supported for Word", saveParameters));
      }
      return saveOptions;
    }
    
    /// <summary>
    /// Проверить документ на валидность перед конвертацией в pdf.
    /// </summary>
    /// <param name="word">Документ.</param>
    public override void Validate(Aspose.Words.Document word)
    {
      var fonts = word.FontInfos;
      var fontNames = fonts.Select(x => x.Name).ToList();
      this.CheckFonts(fontNames);
    }
    
    #endregion
    
    #region Преобразование таблиц
    
    /// <summary>
    /// Получить опции сохранения для таблицы.
    /// </summary>
    /// <param name="workbook">Документ.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Опции сохранения для таблицы.</returns>
    public override Aspose.Cells.PdfSaveOptions GetCellsSaveOptions(Aspose.Cells.Workbook workbook, params string[] saveParameters)
    {
      var saveOptions = (Aspose.Cells.PdfSaveOptions)base.GetCellsSaveOptions(workbook);
      switch (saveParameters[0])
      {
        case "v1A":
          saveOptions.Compliance = Aspose.Cells.Rendering.PdfCompliance.PdfA1a;
          break;
        case "v1B":
          saveOptions.Compliance = Aspose.Cells.Rendering.PdfCompliance.PdfA1b;
          break;
        default:
          throw new AppliedCodeException(string.Format("Convert to {0} not supported for Excel", saveParameters));
      }
      return saveOptions;
    }
    
    /// <summary>
    /// Проверить документ на валидность перед конвертацией в pdf.
    /// </summary>
    /// <param name="workbook">Документ.</param>
    public override void Validate(Aspose.Cells.Workbook workbook)
    {
      var fonts = workbook.GetFonts();
      var fontNames = fonts.Select(x => x.Name).ToList();
      this.CheckFonts(fontNames);
    }
    
    #endregion
    
    #region Преобразование презентаций
    
    /// <summary>
    /// Получить опции сохранения для презентации.
    /// </summary>
    /// <param name="presentation">Презентация.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Опции сохранения для презентации.</returns>
    public override Aspose.Slides.Export.PdfOptions GetSlidesSaveOptions(Aspose.Slides.Presentation presentation, params string[] saveParameters)
    {
      var saveOptions = (Aspose.Slides.Export.PdfOptions)base.GetSlidesSaveOptions(presentation);
      switch (saveParameters[0])
      {
        case "v1A":
          saveOptions.Compliance = Aspose.Slides.Export.PdfCompliance.PdfA1a;
          break;
        case "v1B":
          saveOptions.Compliance = Aspose.Slides.Export.PdfCompliance.PdfA1b;
          break;
        default:
          throw new AppliedCodeException(string.Format("Convert to {0} not supported for presentation", saveParameters));
      }
      return saveOptions;
    }
    
    /// <summary>
    /// Проверить документ на валидность перед конвертацией в pdf.
    /// </summary>
    /// <param name="presentation">Документ.</param>
    public override void Validate(Aspose.Slides.Presentation presentation)
    {
      var fonts = presentation.FontsManager.GetFonts();
      var fontNames = fonts.Select(x => x.FontName).ToList();
      this.CheckFonts(fontNames);
    }
    
    #endregion
    
    #region Преобразование изображений
    
    /// <summary>
    /// Получить опции сохранения для картинки.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Опции сохранения для картинки.</returns>
    public override Aspose.Words.Saving.PdfSaveOptions GetImageSaveOptions(Aspose.Words.Document document, params string[] saveParameters)
    {
      var saveOptions = (Aspose.Words.Saving.PdfSaveOptions)base.GetImageSaveOptions(document);
      switch (saveParameters[0])
      {
        case "v1A":
          saveOptions.Compliance = Aspose.Words.Saving.PdfCompliance.PdfA1a;
          break;
        case "v1B":
          saveOptions.Compliance = Aspose.Words.Saving.PdfCompliance.PdfA1b;
          break;
        default:
          throw new AppliedCodeException(string.Format("Convert to {0} not supported for images", saveParameters));
      }
      return saveOptions;
    }
    
    /// <summary>
    /// Рассчитать размеры страницы.
    /// </summary>
    /// <param name="imageWidth">Ширина картинки.</param>
    /// <param name="imageHeight">Высота картинки.</param>
    /// <param name="builder">Обработчик документа.</param>
    /// <returns>Масштабированные размеры страницы.</returns>
    public override ScaledPageSize CalculatePageSize(double imageWidth, double imageHeight, DocumentBuilder builder)
    {
      var resultedPageSize = new ScaledPageSize();
      builder.PageSetup.Orientation = imageWidth > imageHeight ? Aspose.Words.Orientation.Landscape : Aspose.Words.Orientation.Portrait;
      builder.PageSetup.PageWidth = imageWidth;
      builder.PageSetup.PageHeight = imageHeight;
      resultedPageSize.Width = imageWidth;
      resultedPageSize.Height = imageHeight;
      return resultedPageSize;
    }
    
    /// <summary>
    /// Получить обработчик документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Обработчик документа.</returns>
    public override DocumentBuilder GetDocumentBuilder(Aspose.Words.Document document)
    {
      var builder = new DocumentBuilder(document);
      builder.PageSetup.LeftMargin = MarginForImages;
      builder.PageSetup.RightMargin = MarginForImages;
      builder.PageSetup.TopMargin = MarginForImages;
      builder.PageSetup.BottomMargin = MarginForImages;
      return builder;
    }
    
    #endregion
    
    #region Преобразование сканов
    
    /// <summary>
    /// Преобразовать изображение tiff/tif в pdf/a.
    /// </summary>
    /// <param name="inputStream">Поток с документом формата tiff/tif.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Поток для записи результата.</returns>
    public override Stream ConvertScanToPdf(Stream inputStream, params string[] saveParameters)
    {
      var resultStream = new MemoryStream();
      var tempFrameStreamStorage = new List<MemoryStream>();
      try
      {
        var document = new Aspose.Pdf.Document();
        this.AddPages(document, inputStream, tempFrameStreamStorage);
        var pdfFormat = this.GetPdfFormat(document, saveParameters);
        
        using (var convertLog = new MemoryStream())
        {
          document.Convert(convertLog, pdfFormat, ConvertErrorAction.Delete);
          document.Save(resultStream);
        }
      }
      catch (Exception ex)
      {
        resultStream.Close();
        Logger.Error("Cannot convert tiff to pdf/a", ex);
        throw new AppliedCodeException("Cannot convert tiff to pdf/a");
      }
      finally
      {
        tempFrameStreamStorage.ForEach(x => x.Dispose());
      }
      return resultStream;
    }
    
    /// <summary>
    /// Добавить страницы из документа формата tif/tiff при конвертации.
    /// </summary>
    /// <param name="pdfDocument">Документ.</param>
    /// <param name="imageStream">Поток с документом.</param>
    /// <param name="tempFrameStreamStorage">Временное хранилище для потоков с изображениями.</param>
    public virtual void AddPages(Aspose.Pdf.Document pdfDocument, Stream imageStream, List<MemoryStream> tempFrameStreamStorage)
    {
      using (var bitmap = new System.Drawing.Bitmap(imageStream))
      {
        var dimension = new FrameDimension(bitmap.FrameDimensionsList.First());
        int frameCount = bitmap.GetFrameCount(dimension);

        var encoder = this.GetEncoder("image/png");
        var encoderParams = this.GetEncoderParameters(EncoderValue.CompressionLZW);

        for (int frameIdx = 0; frameIdx <= frameCount - 1; frameIdx++)
        {
          var currentFrameStream = new MemoryStream();
          tempFrameStreamStorage.Add(currentFrameStream);

          bitmap.SelectActiveFrame(dimension, frameIdx);
          bitmap.Save(currentFrameStream, encoder, encoderParams);

          var page = pdfDocument.Pages.Add();
          this.AddImageToPage(page, currentFrameStream);
        }
      }
    }
    
    /// <summary>
    /// Добавить изображение на страницу при конвертации в pdf/a.
    /// </summary>
    /// <param name="documentPage">Страница документа.</param>
    /// <param name="imageStream">Поток с изображением.</param>
    public virtual void AddImageToPage(Page documentPage, Stream imageStream)
    {
      using (System.Drawing.Image imageFromStream = System.Drawing.Image.FromStream(imageStream))
      {
        Aspose.Pdf.Image resultImage = new Aspose.Pdf.Image();
        resultImage.ImageStream = imageStream;
        resultImage.IsInNewPage = true;
        resultImage.IsKeptWithNext = true;
        resultImage.HorizontalAlignment = HorizontalAlignment.Center;
        Aspose.Pdf.BaseParagraph paragraph = resultImage;
        documentPage.PageInfo.Height = (double)imageFromStream.Height;
        documentPage.PageInfo.Width = (double)imageFromStream.Width;
        documentPage.PageInfo.Margin = new MarginInfo(0.0, 0.0, 0.0, 0.0);
        documentPage.Paragraphs.Add(paragraph);
      }
    }
    
    /// <summary>
    /// Получить формат pdf.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="saveParameters">Опции сохранения.</param>
    /// <returns>Формат pdf.</returns>
    public override Aspose.Pdf.PdfFormat GetPdfFormat(Aspose.Pdf.Document document, params string[] saveParameters)
    {
      var pdfFormat = new Aspose.Pdf.PdfFormat();
      switch (saveParameters[0])
      {
        case "v1A":
          pdfFormat = Aspose.Pdf.PdfFormat.PDF_A_1A;
          break;
        case "v1B":
          pdfFormat = Aspose.Pdf.PdfFormat.PDF_A_1B;
          break;
        default:
          throw new AppliedCodeException(string.Format("Convert to {0} not supported for images", saveParameters));
      }
      return pdfFormat;
    }
    
    #endregion
    
    /// <summary>
    /// Проверить доступность шрифтов.
    /// </summary>
    /// <param name="fontNames">Список шрифтов.</param>
    public virtual void CheckFonts(List<string> fontNames)
    {
      foreach (var name in fontNames)
      {
        if (!this.IsFontAvailable(name))
        {
          Logger.DebugFormat("Font {0} not found", name);
          throw new AppliedCodeException(string.Format("Font {0} not found", name));
        }
      }
    }
    
    /// <summary>
    /// Проверить доступность шрифта.
    /// </summary>
    /// <param name="fontName">Шрифт.</param>
    /// <returns>True - если шрифт доступен, иначе false.</returns>
    private bool IsFontAvailable(string fontName)
    {
      try
      {
        var font = Aspose.Pdf.Text.FontRepository.FindFont(fontName);
        if (font == null)
          return false;
        
        if (!font.IsAccessible && !font.IsEmbedded && !font.IsSubset)
          return false;
      }
      catch (Aspose.Pdf.FontNotFoundException)
      {
        return false;
      }
      return true;
    }

    #endregion
  }
  
  /// <summary>
  /// Класс подстановки строковых значений в документ Word по тегам. Реализует общую логику поиска тегов.
  /// </summary>
  public class DocxTagUpdater
  {
    /// <summary>
    /// Имя параметра, содержащего формат даты и времени. Используется для парсинга даты из строки при подстановке в теги типа "Дата".
    /// </summary>
    protected const string DateTimeFormatParamName = "DateTimeFormat";
    
    /// <summary>
    /// Максимальное количество перемещений при поиске начала тэга.
    /// </summary>
    public const int TagStartMovementsLimit = 1000;
    
    /// <summary>
    /// Имя вида узла SDTTEXTINPUT.
    /// </summary>
    public const string SdtTextInputNodeKindName = "SDTTEXTINPUT";
    
    /// <summary>
    /// Имя вида узла BOOKMARKSTART.
    /// </summary>
    public const string BookmarkStartNodeKindName = "BOOKMARKSTART";
    
    /// <summary>
    /// Имя вида узла BOOKMARKEND.
    /// </summary>
    public const string BookmarkEndNodeKindName = "BOOKMARKEND";
    
    /// <summary>
    /// Документ Aspose.Words.Document.
    /// </summary>
    public Aspose.Words.Document Document { get; protected set; }
    
    /// <summary>
    /// Модель отметки.
    /// </summary>
    public IMarkDto MarkDto { get; protected set; }
    
    /// <summary>
    /// Подходящие тэги документа.
    /// </summary>
    public List<StructuredDocumentTag> MatchedTags { get; protected set; }
    
    /// <summary>
    /// Префикс сообщения логирования.
    /// </summary>
    public string LogMessagePrefix { get; set; }
    
    public DocxTagUpdater(Aspose.Words.Document document, IMarkDto markDto)
    {
      this.Document = document;
      this.MarkDto = markDto;
      this.MatchedTags = new List<StructuredDocumentTag>();
    }
    
    /// <summary>
    /// Попытаться обновить значение тэга по модели отметки.
    /// </summary>
    public virtual void UpdateTagFromMarkDto()
    {
      if (string.IsNullOrEmpty(this.MarkDto.Content))
      {
        Logger.Debug($"{this.LogMessagePrefix} Skip adding Mark={this.MarkDto.Id} due to empty content");
        this.MarkDto.IsSuccessful = false;
        return;
      }
      
      if (!this.TryMatchTags())
      {
        Logger.Debug($"{this.LogMessagePrefix} Skip adding Mark={this.MarkDto.Id}. Document has no tags: [{string.Join(",", this.MarkDto.Tags)}]");
        this.MarkDto.IsSuccessful = false;
        return;
      }
      
      if (!this.TrySetMatchedTagsValue())
      {
        Logger.Debug($"{this.LogMessagePrefix} Cannot add Mark={this.MarkDto.Id}");
        this.MarkDto.IsSuccessful = false;
        return;
      }
      
      Logger.Debug($"{this.LogMessagePrefix} Mark={this.MarkDto.Id} added successfully");
      this.MarkDto.IsSuccessful = true;
      
      this.TrySetTagCoordinatesToMarkDto();
    }
    
    /// <summary>
    /// Подобрать тэги документа по модели отметки.
    /// </summary>
    /// <returns>True - тэги подобраны. False - отсутствуют подходящие модели отметки тэги.</returns>
    public virtual bool TryMatchTags()
    {
      this.MatchedTags = this.Document.GetChildNodes(NodeType.StructuredDocumentTag, true)
        .Cast<StructuredDocumentTag>()
        .Where(x => this.MarkDto.Tags.Contains(x.Tag))
        .ToList();
      return this.MatchedTags.Any();
    }
    
    /// <summary>
    /// Обновить значения в подходящих тэгах документа.
    /// </summary>
    /// <returns>True - значения всех тэгов обновлены. False - значение как минимум одного тэга не обновлено.</returns>
    public virtual bool TrySetMatchedTagsValue()
    {
      foreach (var tag in this.MatchedTags)
      {
        var tagValueUpdated = false;
        switch (tag.SdtType)
        {
          case SdtType.RichText:
          case SdtType.PlainText:
            tagValueUpdated = this.TryUpdateTextTagValue(tag, this.MarkDto.Content);
            break;
          case SdtType.Date:
            tagValueUpdated = this.TryUpdateDateTagValue(tag, this.MarkDto.Content);
            break;
          default:
            tagValueUpdated = false;
            break;
        }
        if (!tagValueUpdated)
          return false;
      }
      return true;
    }
    
    /// <summary>
    /// Попытаться установить координаты тэга Word документа в модель отметки.
    /// </summary>
    /// <returns>True - координаты установлены успешно. False - не удалось установить координаты тэга.</returns>
    /// <remarks>Координаты тэга вычисляются по layout документа. Начало координат - левый верхний угол.</remarks>
    public virtual bool TrySetTagCoordinatesToMarkDto()
    {
      var tag = this.MatchedTags.FirstOrDefault();
      if (tag == null)
      {
        Logger.Debug($"{this.LogMessagePrefix} TryGetTagCoordinates. Has no tags: {string.Join(",", this.MarkDto.Tags)}");
        return false;
      }
      
      var layoutCollector = new Aspose.Words.Layout.LayoutCollector(this.Document);
      var layoutEnumerator = new Aspose.Words.Layout.LayoutEnumerator(this.Document);
      
      var paragraphNode = this.GetNearestParagraphNode(tag);
      if (paragraphNode == null)
      {
        Logger.Debug($"{this.LogMessagePrefix} Try get '{tag.Tag}' tag coordinates. Can't find paragraph node");
        return false;
      }
      
      var collectorTag = layoutCollector.GetEntity(paragraphNode);
      if (collectorTag == null)
      {
        Logger.Debug($"{this.LogMessagePrefix} Try get '{tag.Tag}' tag coordinates. Can't get tag layout collector");
        return false;
      }

      layoutEnumerator.Current = collectorTag;
      // layoutEnumerator будет находиться в позиции после элемента тэга.
      // Чтобы поставить его на позицию элемента конца тэга, надо сделать шаг назад.
      layoutEnumerator.MovePrevious();
      this.MoveToTagStart(layoutEnumerator);
      this.MarkDto.Page = layoutEnumerator.PageIndex;
      this.MarkDto.X = System.Math.Round(layoutEnumerator.Rectangle.X / PdfStamper.DotsPerCm);
      this.MarkDto.Y = System.Math.Round(layoutEnumerator.Rectangle.Y / PdfStamper.DotsPerCm);
      return true;
    }
    
    /// <summary>
    /// Обновить значение в текстовом тэге.
    /// </summary>
    /// <param name="tag">Тэг.</param>
    /// <param name="value">Значение.</param>
    /// <returns>True - значение успешно обновлено в текстовом тэге. False - иначе.</returns>
    protected bool TryUpdateTextTagValue(Aspose.Words.Markup.StructuredDocumentTag tag, string value)
    {
      var node = tag.FirstChild;
      if (node == null)
        return false;
      
      try
      {
        switch (node.NodeType)
        {
        case NodeType.Run:
          ((Aspose.Words.Run)node).Text = value;
          tag.RemoveAllChildren();
          tag.AppendChild(node);
          break;
        case NodeType.Paragraph:
          tag.RemoveAllChildren();
          var paragraph = new Aspose.Words.Paragraph(tag.Document);
          var run = new Run(tag.Document, value);
          if (paragraph != null)
          {
            paragraph.AppendChild(run);
            tag.AppendChild(paragraph);
          }
          break;
        case NodeType.Cell:
          var cell = (Aspose.Words.Tables.Cell)node;
          cell.RemoveAllChildren();
          var cellParagraph = new Aspose.Words.Paragraph(tag.Document);
          var textRun = new Run(tag.Document, value);
          cellParagraph.AppendChild(textRun);
          cell.AppendChild(cellParagraph);
          break;
        default:
          Logger.Debug($"{this.LogMessagePrefix} TryUpdateTextTagValue. Unknown node type for tag {tag.Tag}");
          return false;
        }
      }
      catch (Exception ex)
      {
        Logger.Error($"{this.LogMessagePrefix} TryUpdateTextTagValue. An error occured", ex);
        return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Обновить значение в тэге даты.
    /// </summary>
    /// <param name="tag">Тэг.</param>
    /// <param name="value">Значение.</param>
    /// <returns>True - значение успешно обновлено в тэге даты. False - иначе.</returns>
    protected bool TryUpdateDateTagValue(Aspose.Words.Markup.StructuredDocumentTag tag, string value)
    {
      /* Тип экземпляра StructuredDocumentTag определяется через enum SdtType.
       * В зависимости от его значения конкретный экземпляр имеет некоторые свойства, например "FullDate" появлвется при SdtType=Date.
       * https://reference.aspose.com/words/net/aspose.words.markup/sdttype/
       */
      DateTime date;
      var dateTimeFormat = string.Empty;
      
      /* Формат даты может быть передан в AdditionalParams.
       * Если этот параметр не передан, то парсим дату и время с региональными настройками по умолчанию.
       */
      this.MarkDto.AdditionalParams?.TryGetValue(DateTimeFormatParamName, out dateTimeFormat);
      if (!string.IsNullOrEmpty(dateTimeFormat))
      {
        Logger.Debug($"{this.LogMessagePrefix} DateTimeFormat='{dateTimeFormat}', try parse '{value}'");
        DateTime.TryParseExact(value, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
      }
      else
      {
        Logger.Debug($"{this.LogMessagePrefix} DateTimeFormat not set, try parse '{value}' with default regional settings");
        DateTime.TryParse(value, out date);
      }
      
      if (date != null)
      {
        tag.GetType().GetProperty(@"FullDate").SetValue(tag, date);
      }
      else
      {
        Logger.Debug($"{this.LogMessagePrefix} Failed to parse '{value}' as a valid DateTime value");
        return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Получить ближайший элемент типа <c>Aspose.Words.Paragraph</c>.
    /// </summary>
    /// <param name="tagNode">Тэг.</param>
    /// <returns>Ближайший элемент типа <c>Aspose.Words.Paragraph</c>.</returns>
    protected Aspose.Words.Paragraph GetNearestParagraphNode(StructuredDocumentTag tagNode)
    {
      if (tagNode == null)
        return null;

      var childParagraph = tagNode.FirstChild as Aspose.Words.Paragraph;
      if (childParagraph != null)
        return childParagraph;

      var childRun = tagNode.FirstChild as Aspose.Words.Run;
      var parentParagraph = childRun?.ParentParagraph;
      if (parentParagraph != null)
        return parentParagraph;

      return null;
    }
    
    /// <summary>
    /// Сдвинуть layoutEnumerator на начало тэга.
    /// </summary>
    /// <param name="layoutEnumerator">LayoutEnumerator.</param>
    protected void MoveToTagStart(Aspose.Words.Layout.LayoutEnumerator layoutEnumerator)
    {
      this.MoveFromTagEndNodeToTagStartNode(layoutEnumerator, SdtTextInputNodeKindName, SdtTextInputNodeKindName);
      this.MoveFromTagEndNodeToTagStartNode(layoutEnumerator, BookmarkStartNodeKindName, BookmarkEndNodeKindName);
    }
    
    /// <summary>
    /// Сдвинуть layoutEnumerator с последнего элемента тэга до первого.
    /// </summary>
    /// <param name="layoutEnumerator">LayoutEnumerator.</param>
    /// <param name="startNodeKind">Вид первого элемента тэга.</param>
    /// <param name="endNodeKind">Вид последнего элемента тэга.</param>
    protected void MoveFromTagEndNodeToTagStartNode(Aspose.Words.Layout.LayoutEnumerator layoutEnumerator, string startNodeKind, string endNodeKind)
    {
      if (layoutEnumerator.Kind != endNodeKind)
        return;
      
      var movements = 0;
      do
      {
        layoutEnumerator.MovePrevious();
        movements++;
      }
      while (layoutEnumerator.Kind != startNodeKind && movements <= TagStartMovementsLimit);
    }
  }
  
  /// <summary>
  /// Класс простановки штампов в pdf. Реализует логику генерации и простановки штампов, а также поиска мест для вставки штампов.
  /// </summary>
  public class PdfStamper
  {
    #region Константы

    /// <summary>
    /// Разрешение штампа.
    /// </summary>
    public const double DotsPerCm = 72 / 2.54;
    
    /// <summary>
    /// Отступ снизу страницы для простановки штампа по умолчанию.
    /// </summary>
    public const int BottomIndent = 20;
    
    #endregion
    
    #region Свойства
    
    /// <summary>
    /// Менеджер управления расширениями файлов.
    /// </summary>
    public FileExtensionsManager FileExtensionsManager { get; set; }
    
    #endregion
    
    #region Конструкторы
    
    public PdfStamper()
    {
      this.FillFileExtensionsManager();
    }
    
    #endregion
    
    #region Отметки для вставки в pdf
    
    /// <summary>
    /// Добавить отметку на тело pdf-документа.
    /// </summary>
    /// <param name="pdfDocument">Документ.</param>
    /// <param name="markDto">Модель отметки.</param>
    /// <param name="documentWithMarks">Модель документа с отметками.</param>
    /// <returns>Если метка добавилась - true, иначе - false.</returns>
    public bool TryAddMarkToPdfBody(Aspose.Pdf.Document pdfDocument, IMarkDto markDto, IDocumentMarksDto documentWithMarks)
    {
      if (pdfDocument == null)
        return false;

      if (string.IsNullOrEmpty(markDto.Content))
      {
        PdfConversionLogger.Debug(documentWithMarks, $"Skip adding Mark={markDto.Id} due to empty content");
        return false;
      }
      
      if (!string.IsNullOrEmpty(markDto.Anchor))
      {
        PdfConversionLogger.Debug(documentWithMarks, $"Try set Mark={markDto.Id} by anchor");
        return this.TrySetMarkByAnchor(pdfDocument, markDto, documentWithMarks);
      }
      else if (markDto.Page == 0)
      {
        PdfConversionLogger.Debug(documentWithMarks, $"Try set Mark={markDto.Id} on all pages");
        return this.TrySetMarkOnAllPages(pdfDocument, markDto, documentWithMarks);
      }
      else
      {
        PdfConversionLogger.Debug(documentWithMarks, $"Try set Mark={markDto.Id} by coordinates");
        return this.TrySetMarkByCoordinates(pdfDocument, markDto, documentWithMarks);
      }
    }

    /// <summary>
    /// Попробовать поставить отметку по якорю.
    /// </summary>
    /// <param name="pdfDocument">Документ.</param>
    /// <param name="markDto">Модель отметки.</param>
    /// <param name="documentWithMarks">Модель документ с отметками.</param>
    /// <returns>Если отметка поставилась - true, иначе - false.</returns>
    public bool TrySetMarkByAnchor(Aspose.Pdf.Document pdfDocument, IMarkDto markDto, IDocumentMarksDto documentWithMarks)
    {
      var mark = this.CreateMarkFromHtml(markDto.Content);
      this.SetMarkRotationFromDto(mark, markDto);
      var documentPagesCount = pdfDocument.Pages.Count;
      var lastAnchorSearchablePage = this.FileExtensionsManager.AnchorSearchAllowed(documentWithMarks.BodyExtension) ?
        this.GetLastSearchablePage(documentPagesCount, documentWithMarks.MaxAnchorSearchPageCount.Value, documentWithMarks.BodyExtension) :
        documentPagesCount;
      this.SetDtoCoordinatesByAnchor(markDto, mark, pdfDocument, lastAnchorSearchablePage);
      var page = pdfDocument.Pages[markDto.Page];
      this.SetMarkCoordinatesFromDto(mark, markDto, page);
      if (!this.IsMarkInsidePageRect(mark, page))
      {
        PdfConversionLogger.Debug(documentWithMarks, $"Mark={markDto.Id} is over the page ({markDto.Page}) boundaries");
        return false;
      }
      
      this.UpdateDtoCoordinatesFromMark(mark, markDto, page);
      this.SetMarkOnPage(mark, page);
      return true;
    }
    
    /// <summary>
    /// Попробовать поставить отметку по координатам.
    /// </summary>
    /// <param name="pdfDocument">Документ.</param>
    /// <param name="markDto">Модель отметки.</param>
    /// <param name="documentWithMarks">Модель документа с отметками.</param>
    /// <returns>Если отметка поставилась - true, иначе - false.</returns>
    public bool TrySetMarkByCoordinates(Aspose.Pdf.Document pdfDocument, IMarkDto markDto, IDocumentMarksDto documentWithMarks)
    {
      if (!this.PageExists(pdfDocument, markDto.Page))
      {
        PdfConversionLogger.Debug(documentWithMarks, $"Mark={markDto.Id} page ({markDto.Page}) is over the document pages count ({pdfDocument.Pages.Count})");
        return false;
      }
      
      var mark = this.CreateMarkFromHtml(markDto.Content);
      
      if (markDto.OnBlankPage.GetValueOrDefault())
        markDto.Page = -1;
      
      this.UpdateDtoPageNumber(markDto, pdfDocument);
      this.SetMarkRotationFromDto(mark, markDto);
      this.SetMarkCoordinatesFromDto(mark, markDto, pdfDocument.Pages[markDto.Page]);

      if (!this.IsMarkInsidePageRect(mark, pdfDocument.Pages[markDto.Page]))
      {
        PdfConversionLogger.Debug(documentWithMarks, $"Mark={markDto.Id} is over the page ({markDto.Page}) boundaries.");
        return false;
      }
      
      this.UpdateDtoCoordinatesFromMark(mark, markDto, pdfDocument.Pages[markDto.Page]);
      this.SetMarkOnPage(mark, pdfDocument.Pages[markDto.Page]);
      return true;
    }
    
    /// <summary>
    /// Попробовать поставить отметки на все страницы.
    /// </summary>
    /// <param name="pdfDocument">Документ.</param>
    /// <param name="markDto">Модель отметки.</param>
    /// <param name="documentWithMarks">Модель документ с отметками.</param>
    /// <returns>Если все отметки поставились - true, иначе - false.</returns>
    public bool TrySetMarkOnAllPages(Aspose.Pdf.Document pdfDocument, IMarkDto markDto, IDocumentMarksDto documentWithMarks)
    {
      var markedPages = new Dictionary<Aspose.Pdf.Page, Aspose.Pdf.PdfPageStamp>();
      foreach (var page in pdfDocument.Pages)
      {
        var mark = this.CreateMarkFromHtml(markDto.Content);
        this.SetMarkRotationFromDto(mark, markDto);
        this.SetMarkCoordinatesFromDto(mark, markDto, page);
        if (!this.IsMarkInsidePageRect(mark, page))
        {
          PdfConversionLogger.Debug(documentWithMarks, $"Mark={markDto.Id} is over the page ({markDto.Page}) boundaries.");
          return false;
        }
        markedPages.Add(page, mark);
      }
      
      foreach (var markedPage in markedPages)
        this.SetMarkOnPage(markedPage.Value, markedPage.Key);
      return true;
    }
    
    /// <summary>
    /// Установить координаты в модели отметки по якорю.
    /// </summary>
    /// <param name="markDto">Модель отметки.</param>
    /// <param name="mark">Отметка.</param>
    /// <param name="document">Документ.</param>
    /// <param name="lastAnchorSearchablePage">Последняя страница для поиска якоря.</param>
    /// <remarks>Поиск якоря происходит с последней страницы документа до указанной.</remarks>
    public virtual void SetDtoCoordinatesByAnchor(IMarkDto markDto, Aspose.Pdf.PdfPageStamp mark, Aspose.Pdf.Document document, int lastAnchorSearchablePage)
    {
      var lastPage = document.Pages[document.Pages.Count];
      var lastPageRectangle = lastPage.GetPageRect(true);
      var markWidth = mark.Width;
      var markHeight = mark.Height;
      this.CalculateProjectionLengthAndHeight(ref markWidth, ref markHeight, mark.RotateAngle);
      var lastPageHorizontalCenter = lastPageRectangle.Width / 2;
      var markLeftTopCornerX = lastPageHorizontalCenter - (markWidth / 2);
      var markLeftTopCornerY = BottomIndent + markHeight;
      
      markDto.X = System.Math.Round(markLeftTopCornerX / PdfStamper.DotsPerCm, 2);
      markDto.Y = System.Math.Round((lastPageRectangle.Height - markLeftTopCornerY) / PdfStamper.DotsPerCm, 2);
      markDto.Page = document.Pages.Count;
      
      var anchorSymbol = markDto.Anchor;
      if (!string.IsNullOrEmpty(anchorSymbol))
      {
        // Поиск символа производится постранично с конца документа.
        for (var pageNumber = document.Pages.Count; pageNumber > lastAnchorSearchablePage; pageNumber--)
        {
          var page = document.Pages[pageNumber];
          var lastAnchorEntry = this.GetLastAnchorEntry(page, anchorSymbol);
          if (lastAnchorEntry == null)
          {
            continue;
          }
          else
          {
            // Установить центры символа-якоря и отметки на одной линии по горизонтали.
            var anchorCenterY = lastAnchorEntry.Position.YIndent + (lastAnchorEntry.Rectangle.Height / 2);
            markLeftTopCornerX = lastAnchorEntry.Position.XIndent;
            markLeftTopCornerY = anchorCenterY + (markHeight / 2);
            markDto.X = System.Math.Round(markLeftTopCornerX / PdfStamper.DotsPerCm, 2);
            markDto.Y = System.Math.Round((page.GetPageRect(true).Height - markLeftTopCornerY) / PdfStamper.DotsPerCm, 2);
            markDto.Page = pageNumber;
            return;
          }
        }
      }
    }

    /// <summary>
    /// Установить координаты отметки из модели отметки.
    /// </summary>
    /// <param name="mark">Отметка.</param>
    /// <param name="markDto">Модель отметки.</param>
    /// <param name="page">Страница.</param>
    public virtual void SetMarkCoordinatesFromDto(Aspose.Pdf.PdfPageStamp mark, IMarkDto markDto, Aspose.Pdf.Page page)
    {
      var pageRectangle = page.GetPageRect(true);
      var markWidth = mark.Width;
      var markHeight = mark.Height;
      this.CalculateProjectionLengthAndHeight(ref markWidth, ref markHeight, mark.RotateAngle);
      
      mark.XIndent = markDto.X >= 0 ?
        markDto.X * PdfStamper.DotsPerCm :
        pageRectangle.Width + (markDto.X * PdfStamper.DotsPerCm) - markWidth;
      mark.YIndent = markDto.Y >= 0 ?
        pageRectangle.Height - (markDto.Y * PdfStamper.DotsPerCm) - markHeight :
        -1 * markDto.Y * PdfStamper.DotsPerCm;
    }

    /// <summary>
    /// Посчитать длину проекции повернутого прямоугольника по горизонтали и вертикали.
    /// </summary>
    /// <param name="width">Ширина.</param>
    /// <param name="height">Высота.</param>
    /// <param name="rotateAngle">Угол поворота.</param>
    public void CalculateProjectionLengthAndHeight(ref double width, ref double height, double rotateAngle)
    {
      var widthProjection = this.CalculateProjectionLength(width, height, rotateAngle);
      var heightProjection = this.CalculateProjectionLength(width, height, 90 - rotateAngle);
      width = widthProjection;
      height = heightProjection;
    }
    
    /// <summary>
    /// Посчитать длину проекции повернутого прямоугольника.
    /// </summary>
    /// <param name="width">Ширина.</param>
    /// <param name="height">Высота.</param>
    /// <param name="rotateAngle">Угол поворота.</param>
    /// <returns>Длина проекции прямоугольника.</returns>
    public double CalculateProjectionLength(double width, double height, double rotateAngle)
    {
      // Преобразование угла в радианы
      var thetaRad = Math.PI * rotateAngle / 180.0;
      
      // Проекция длины и высоты на плоскость
      var widthProjection = width * Math.Cos(thetaRad);
      var heightProjection = height * Math.Sin(thetaRad);

      // Вычисление длины проекции
      var projectionLength = Math.Abs(widthProjection) + Math.Abs(heightProjection);

      return projectionLength;
    }
    
    /// <summary>
    /// Установить номер страницы.
    /// </summary>
    /// <param name="markDto">Модель отметки.</param>
    /// <param name="pdfDocument">Документ PDF.</param>
    /// <remarks>Страницы Aspose.Pdf.Document принадлежат отрезку [1..n], где n - количество страниц в документе.
    /// pageNumber &gt; 0 - поиск страницы производится от 1 до n.
    /// pageNumber = 0 - поиск страниц не производится, отметка будет проставлена на каждую страницу.
    /// pageNumber &lt; 0 - поиск страницы производится от n до 1 таким образом, что pageNumber = -1 будет соответствовать последней странице.</remarks>
    public virtual void UpdateDtoPageNumber(IMarkDto markDto, Aspose.Pdf.Document pdfDocument)
    {
      if (markDto.Page == 0)
        return;
      
      markDto.Page = markDto.Page > 0 ?
        markDto.Page :
        pdfDocument.Pages.Count + markDto.Page + 1;
    }
    
    /// <summary>
    /// Установить угол поворота штампа.
    /// </summary>
    /// <param name="mark">Отметка.</param>
    /// <param name="markDto">Модель отметки.</param>
    public virtual void SetMarkRotationFromDto(Aspose.Pdf.PdfPageStamp mark, IMarkDto markDto)
    {
      mark.RotateAngle = markDto.RotateAngle ?? 0;
    }

    /// <summary>
    /// Установить координаты модели отметки из отметки.
    /// </summary>
    /// <param name="mark">Отметка.</param>
    /// <param name="markDto">Модель отметки.</param>
    /// <param name="page">Страница.</param>
    public virtual void UpdateDtoCoordinatesFromMark(Aspose.Pdf.PdfPageStamp mark, IMarkDto markDto, Aspose.Pdf.Page page)
    {
      /* В Aspose система координат документа имеет начало в левом нижнем углу (I четверть координатной плоскости).
       * В Directum RX система координат имеет начало в левом верхнем углу (IV четверть координатной плоскости).
       * Фактические координаты отметки должны быть приведены из системы координат Aspose в систему координат Directum RX.
       */
      var pageRectangle = page.GetPageRect(true);
      var markHeight = this.CalculateProjectionLength(mark.Width, mark.Height, 90 - mark.RotateAngle);
      var markLeftTopCornerX = mark.XIndent;
      var markLeftTopCornerY = mark.YIndent + markHeight;
      markDto.X = System.Math.Round(markLeftTopCornerX / PdfStamper.DotsPerCm, 2);
      markDto.Y = System.Math.Round((pageRectangle.Height - markLeftTopCornerY) / PdfStamper.DotsPerCm, 2);
    }
    
    /// <summary>
    /// Проверить, что страница с указанным номером существует в документе.
    /// </summary>
    /// <param name="pdfDocument">Pdf документ.</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <returns>True - страница существует в докуенте. False - страница не существует в документе.</returns>
    /// <remarks>Страницы Aspose.Pdf.Document принадлежат отрезку [1..n], где n - количество страниц в документе.
    /// pageNumber &gt; 0 - поиск страницы производится от 1 до n.
    /// pageNumber = 0 - поиск страниц не производится, отметка будет проставлена на каждую страницу.
    /// pageNumber &lt; 0 - поиск страницы производится от n до 1 таким образом, что pageNumber = -1 будет соответствовать последней странице.</remarks>
    public bool PageExists(Aspose.Pdf.Document pdfDocument, int pageNumber)
    {
      var pagesCount = pdfDocument.Pages.Count;
      if (pageNumber > 0)
        return pageNumber <= pagesCount;
      if (pageNumber < 0)
        return pagesCount + pageNumber >= 0;
      return true;
    }
    
    /// <summary>
    /// Убедиться в том, что отметка полностью помещается на страницу.
    /// </summary>
    /// <param name="mark">Отметка.</param>
    /// <param name="page">Страница.</param>
    /// <returns>True - отметка полностью помещается на страницу. False - отметка выходит за рамки страницы.</returns>
    public virtual bool IsMarkInsidePageRect(Aspose.Pdf.PdfPageStamp mark, Aspose.Pdf.Page page)
    {
      var pageRectangle = page.GetPageRect(true);
      var markWidth = mark.Width;
      var markHeight = mark.Height;
      this.CalculateProjectionLengthAndHeight(ref markWidth, ref markHeight, mark.RotateAngle);
      var isMarkInsidePage = mark.XIndent >= 0 && mark.XIndent + markWidth <= pageRectangle.Width &&
        mark.YIndent >= 0 && mark.YIndent + markHeight <= pageRectangle.Height;
      return isMarkInsidePage;
    }
    
    /// <summary>
    /// Установить отметку на страницу документа.
    /// </summary>
    /// <param name="mark">Отметка.</param>
    /// <param name="documentPage">Страница документа.</param>
    protected void SetMarkOnPage(Aspose.Pdf.PdfPageStamp mark, Aspose.Pdf.Page documentPage)
    {
      var rectConsiderRotation = documentPage.GetPageRect(true);
      if (mark.Width <= rectConsiderRotation.Width || mark.Width <= (rectConsiderRotation.Height - BottomIndent))
        documentPage.AddStamp(mark);
    }
    
    /// <summary>
    /// Создать штамп из шаблона html для вставки в pdf.
    /// </summary>
    /// <param name="html">Шаблон штампа в html.</param>
    /// <returns>Документ pdf со штампом.</returns>
    [Obsolete("Метод не используется с 02.07.2024 и версии 4.11. Используйте метод CreateMarkFromHtml.")]
    public virtual Aspose.Pdf.PdfPageStamp CreateStampFromHtml(string html)
    {
      return this.CreateMarkFromHtml(html);
    }
    
    /// <summary>
    /// Добавить отметку о подписи на последнюю страницу документа без поиска символов-якорей.
    /// </summary>
    /// <param name="inputStream">Поток с входным документом.</param>
    /// <param name="stamp">Отметка о подписи.</param>
    /// <returns>Документ с отметкой о подписи.</returns>
    public virtual Stream AddSignatureMarkToDocumentWithoutAnchorSearch(Stream inputStream, Aspose.Pdf.PdfPageStamp stamp)
    {
      var document = new Aspose.Pdf.Document(inputStream);
      var lastPage = document.Pages[document.Pages.Count];
      var rectConsiderRotation = lastPage.GetPageRect(true);

      stamp.XIndent = (rectConsiderRotation.Width / 2) - (stamp.Width / 2);
      stamp.YIndent = BottomIndent;
      stamp.Background = false;

      return this.AddStampToDocumentPage(inputStream, lastPage.Number, stamp);
    }
    
    /// <summary>
    /// Добавить отметку на страницу документа.
    /// </summary>
    /// <param name="inputStream">Поток с входным документом.</param>
    /// <param name="pageNumber">Номер страницы документа, на которую нужно проставить отметку.</param>
    /// <param name="stamp">Отметка.</param>
    /// <returns>Страница документа с отметкой.</returns>
    public virtual Stream AddStampToDocumentPage(Stream inputStream, int pageNumber, Aspose.Pdf.PdfPageStamp stamp)
    {
      try
      {
        // Создание нового потока, в который будет записан документ с отметкой (во входной поток записывать нельзя).
        var outputStream = new MemoryStream();
        var document = new Aspose.Pdf.Document(inputStream);
        var documentPage = document.Pages[pageNumber];
        var rectConsiderRotation = documentPage.GetPageRect(true);
        if (stamp.Width > rectConsiderRotation.Width || stamp.Width > (rectConsiderRotation.Height - BottomIndent))
        {
          inputStream.CopyTo(outputStream);
        }
        else
        {
          documentPage.AddStamp(stamp);
          document.Save(outputStream);
        }
        
        return outputStream;
      }
      catch (Exception ex)
      {
        Logger.Error("Cannot add stamp to document page", ex);
        throw new AppliedCodeException("Cannot add stamp to document page");
      }
      finally
      {
        inputStream.Close();
      }
    }
    
    /// <summary>
    /// Для документов версии ниже 1.4 поднять версию до 1.4 перед вставкой отметки.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>PDF документ, сконвертированный до версии 1.4, или исходный, если версию поднимать не требовалось.</returns>
    /// <remarks>При вставке отметки в pdf версии ниже, чем 1.4, портятся шрифты в документе.
    /// В Adobe Reader такие документы либо не открываются совсем, либо отображаются некорректно.
    /// Для корректного отображения отметки pdf-документ будет сконвертирован до версии pdf 1.4.
    /// Документы в формате pdf/a не конвертируем, т.к. формат основан на версии pdf 1.4 и не требует конвертации.</remarks>
    [Obsolete("Метод не используется с 02.07.2024 и версии 4.11. Повышение версии PDF происходит в процессе преобразования (метод PreprocessPdfDocument класса PdfConverter).")]
    public Stream GetUpgradedPdf(Aspose.Pdf.Document document)
    {
      if (!document.IsPdfaCompliant)
      {
        // Получить версию стандарта PDF из свойств документа. Достаточно первых двух чисел, разделённых точкой.
        var versionRegex = new Regex(@"^\d{1,2}\.\d{1,2}");
        var pdfVersionAsString = versionRegex.Match(document.Version).Value;
        var minCompatibleVersion = Version.Parse("1.4");

        if (Version.TryParse(pdfVersionAsString, out Version version) && version < minCompatibleVersion)
        {
          Logger.DebugFormat("GetUpgradedPdf. Convert Pdf version to 1.4. Current version is {0}", version);
          using (var convertLog = new MemoryStream())
          {
            var options = new Aspose.Pdf.PdfFormatConversionOptions(Aspose.Pdf.PdfFormat.v_1_4);
            options.LogStream = convertLog;
            document.Convert(options);
          }
        }
      }
      // Необходимо пересохранить документ в поток, чтобы изменение версии применилось до простановки отметки, а не после.
      var docStream = new MemoryStream();
      document.Save(docStream);
      return docStream;
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
    /// <remarks>Поиск якорей доступен в документах с текстовым слоем. Если символов-якорей в документе нет, то отметка проставляется на последней странице.</remarks>
    public virtual Stream AddSignatureMark(Stream inputStream, string extension, string htmlMark, string anchorSymbol,
                                           int searchablePagesNumber)
    {
      try
      {
        var document = new Aspose.Pdf.Document(inputStream);
        var mark = this.CreateMarkFromHtml(htmlMark);

        // Если в документе не предусмотрено наличие якорей (например, в картинке), то подпись - на последней странице.
        var anchorSymbolSearchNeeded = this.FileExtensionsManager.AnchorSearchAllowed(extension);
        if (!anchorSymbolSearchNeeded)
          return this.AddSignatureMarkToDocumentWithoutAnchorSearch(inputStream, mark);

        // Ограничение количества страниц, на которых будет искаться символ-якорь, применимо только к excel-файлам.
        var lastSearchablePage = this.GetLastSearchablePage(document.Pages.Count, searchablePagesNumber, extension);

        // Поиск символа производится постранично с конца документа.
        for (var pageNumber = document.Pages.Count; pageNumber > lastSearchablePage; pageNumber--)
        {
          var page = document.Pages[pageNumber];
          var lastAnchorEntry = this.GetLastAnchorEntry(page, anchorSymbol);
          if (lastAnchorEntry == null)
            continue;

          // Установить центры символа-якоря и отметки об ЭП на одной линии по горизонтали.
          mark.XIndent = lastAnchorEntry.Position.XIndent;
          mark.YIndent = lastAnchorEntry.Position.YIndent - (mark.Height / 2) + (lastAnchorEntry.Rectangle.Height / 2);

          return this.AddStampToDocumentPage(inputStream, page.Number, mark);
        }

        // Проставить отметку об ЭП на последней странице, если символ-якорь в документе не найден.
        return this.AddSignatureMarkToDocumentWithoutAnchorSearch(inputStream, mark);
      }
      catch (Exception ex)
      {
        Logger.Error("Cannot add stamp", ex);
        throw new AppliedCodeException("Cannot add stamp");
      }
    }
    
    #endregion
    
    #region Поиск якоря для вставки штампа
    
    /// <summary>
    /// Получить номер страницы с которой начинаем искать символ якоря.
    /// </summary>
    /// <param name="docPagesCount">Количество страниц документа.</param>
    /// <param name="searchablePagesNumber">Количество страниц для поиска символа.</param>
    /// <param name="extension">Расширение файла.</param>
    /// <returns>Номер страницы с которой начинаем искать символ якоря.</returns>
    /// <remarks>Для excel файлов ищем символ на последних searchablePagesNumber страницах, для всех остальных на всех страницах.</remarks>
    public virtual int GetLastSearchablePage(int docPagesCount, int searchablePagesNumber, string extension)
    {
      var pageLimitedAnchorSearch = this.FileExtensionsManager.PageLimitedAnchorSearchAllowed(extension);
      return docPagesCount > searchablePagesNumber && pageLimitedAnchorSearch ?
        docPagesCount - searchablePagesNumber :
        0;
    }
    
    /// <summary>
    /// Преобразовать html для штампа в pdf.
    /// </summary>
    /// <param name="html">Строка html.</param>
    /// <returns>Штамп для вставки в pdf.</returns>
    public virtual Aspose.Pdf.PdfPageStamp CreateMarkFromHtml(string html)
    {
      try
      {
        var htmlLoadOptions = this.GetDefaultHtmlLoadOptions();
        
        Aspose.Pdf.Document stampPdfDoc;
        using (var htmlStream = new MemoryStream(Encoding.UTF8.GetBytes(html)))
          stampPdfDoc = new Aspose.Pdf.Document(htmlStream, htmlLoadOptions);
        
        var finalHtmlLoadOptions = this.GetDefaultHtmlLoadOptions();
        finalHtmlLoadOptions.PageInfo.Height = stampPdfDoc.Pages[1].CalculateContentBBox().Height;
        
        Aspose.Pdf.Document finalStampPdfDoc;
        using (var htmlStream = new MemoryStream(Encoding.UTF8.GetBytes(html)))
          finalStampPdfDoc = new Aspose.Pdf.Document(htmlStream, finalHtmlLoadOptions);
        
        if (finalStampPdfDoc.Pages.Count > 0)
        {
          var mark = new Aspose.Pdf.PdfPageStamp(finalStampPdfDoc.Pages[1]);
          mark.Background = false;
          return mark;
        }
        return null;
      }
      catch (Exception ex)
      {
        Logger.Error("CreateMarkFromHtml.Cannot transform html to pdf", ex);
        throw new AppliedCodeException("CreateMarkFromHtml.Cannot transform html to pdf");
      }
    }
    
    /// <summary>
    /// Получить экземпляр Aspose.Pdf.HtmlLoadOptions по умолчанию.
    /// </summary>
    /// <returns>Экземпляр Aspose.Pdf.HtmlLoadOptions по умолчанию.</returns>
    public virtual Aspose.Pdf.HtmlLoadOptions GetDefaultHtmlLoadOptions()
    {
      var htmlLoadOptions = new Aspose.Pdf.HtmlLoadOptions();
      htmlLoadOptions.IsRenderToSinglePage = true;
      htmlLoadOptions.PageInfo.Margin = new Aspose.Pdf.MarginInfo(0, 0, 0, 0);
      htmlLoadOptions.PageLayoutOption = HtmlPageLayoutOption.FitToWidestContentWidth;
      return htmlLoadOptions;
    }
    
    /// <summary>
    /// Определить по расширению файла, нужно ли искать в нём символы-якоря.
    /// </summary>
    /// <param name="extension">Расширение файла.</param>
    /// <returns>True/false.</returns>
    [Obsolete("Метод не используется с 20.08.2024 и версии 4.11. Используйте метод AnchorSearchAllowed(string) класса FileExtensionsManager.")]
    public static bool CheckIfExtensionIsSupportedForAnchorSearch(string extension)
    {
      return new FileExtensionsManager().AnchorSearchAllowed(extension);
    }
    
    /// <summary>
    /// Получить последнее вхождение символа-якоря на странице.
    /// </summary>
    /// <param name="page">Страница.</param>
    /// <param name="anchor">Символ-якорь.</param>
    /// <returns>Фрагмент текста, являющийся последним вхождением. Null, если символ-якорь не найден.</returns>
    /// <remarks>Последним считается вхождение, находящееся ниже по странице.
    /// Если два вхождения располагаются на одном уровне - считается то, которое правее.</remarks>
    public virtual TextFragment GetLastAnchorEntry(Aspose.Pdf.Page page, string anchor)
    {
      TextSearchOptions textSearchOptions = new TextSearchOptions(true);
      // IgnoreResourceFontErrors используется для игнорирования ошибок отсутствия шрифта.
      textSearchOptions.IgnoreResourceFontErrors = true;
      var absorber = new TextFragmentAbsorber(anchor, textSearchOptions);
      page.Accept(absorber);
      if (absorber.TextFragments.Count == 0)
        return null;

      // Найти последнее вхождение символа-якоря на странице.
      // Условное самое первое вхождение будет иметь координаты левого верхнего угла.
      // https://forum.aspose.com/t/textfragment-at-top-of-page/64774.
      // Ось X - горизонтальная.
      // Ось Y - вертикальная.
      // Начало координат - левый нижний угол.
      var lastEntry = new TextFragment();
      var rectConsiderRotation = page.GetPageRect(true);
      lastEntry.Position.XIndent = 0;
      lastEntry.Position.YIndent = rectConsiderRotation.Height;
      foreach (TextFragment textFragment in absorber.TextFragments)
      {
        if (textFragment.Position.YIndent < lastEntry.Position.YIndent ||
            textFragment.Position.YIndent == lastEntry.Position.YIndent &&
            textFragment.Position.XIndent > lastEntry.Position.XIndent)
          lastEntry = textFragment;
      }

      return lastEntry;
    }
    
    #endregion
    
    #region Методы для заказной разработки
    
    /// <summary>
    /// Получить документ с отметкой на всех страницах.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="stamp">Отметка.</param>
    /// <param name="needUpgradePdfVersion">Признак того, что нужно повышать версию PDF перед простановкой отметки.</param>
    /// <returns>Документ с проставленной отметкой.</returns>
    /// <remarks>Координаты места простановки берутся из свойств отметки.</remarks>
    public virtual Stream GetPdfDocumentWithStamp(Aspose.Pdf.Document document, Aspose.Pdf.PdfPageStamp stamp, bool needUpgradePdfVersion)
    {
      foreach (Aspose.Pdf.Page documentPage in document.Pages)
        documentPage.AddStamp(stamp);

      var resultStream = new MemoryStream();
      document.Save(resultStream);

      return resultStream;
    }

    /// <summary>
    /// Получить документ с отметкой на заданных страницах.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="stamp">Отметка.</param>
    /// <param name="pageNumbers">Массив номеров страниц, на которые нужно поставить отметку.</param>
    /// <param name="needUpgradePdfVersion">Признак того, что нужно повышать версию PDF перед простановкой отметки.</param>
    /// <returns>Документ с проставленной отметкой.</returns>
    /// <remarks>Координаты места простановки берутся из свойств отметки.</remarks>
    public virtual Stream GetPdfDocumentWithStamp(Aspose.Pdf.Document document, Aspose.Pdf.PdfPageStamp stamp, int[] pageNumbers, bool needUpgradePdfVersion)
    {
      foreach (var pageNumber in pageNumbers)
      {
        if (document.Pages.Count >= pageNumber)
        {
          var documentPage = document.Pages[pageNumber];
          documentPage.AddStamp(stamp);
        }
      }

      var resultStream = new MemoryStream();
      document.Save(resultStream);

      return resultStream;
    }
    
    /// <summary>
    /// Добавить штамп к документу по координатам.
    /// </summary>
    /// <param name="inputStream">Поток с входным документом.</param>
    /// <param name="htmlMark">Строка, содержащая html для штампа.</param>
    /// <param name="rightIndentInCm">Отступ с правого края, в см.</param>
    /// <param name="bottomIndentInCm">Отступ с нижнего края, в см.</param>
    /// <returns>Поток с документом.</returns>
    /// <remarks>Штамп будет добавлен на последнюю страницу документа.</remarks>
    public virtual Stream AddStampToLastPage(Stream inputStream, string htmlMark, double rightIndentInCm, double bottomIndentInCm)
    {
      try
      {
        var document = new Aspose.Pdf.Document(inputStream);
        var mark = this.CreateMarkFromHtml(htmlMark);
        mark.Background = false;

        // Установить координаты отметки.
        var lastPage = document.Pages[document.Pages.Count];
        var rectConsiderRotation = lastPage.GetPageRect(true);
        mark.XIndent = rectConsiderRotation.Width - (rightIndentInCm * DotsPerCm) - mark.Width;
        mark.YIndent = bottomIndentInCm * DotsPerCm;

        return this.AddStampToDocumentPage(inputStream, lastPage.Number, mark);
      }
      catch (Exception ex)
      {
        Logger.Error("Cannot add stamp", ex);
        throw new AppliedCodeException("Cannot add stamp");
      }
    }
    
    #endregion
    
    /// <summary>
    /// Заполнить менеджер управления расширениями файлов.
    /// </summary>
    public virtual void FillFileExtensionsManager()
    {
      this.FileExtensionsManager = new FileExtensionsManager();
    }
  }
  
  /// <summary>
  /// Масштабированный размер страницы.
  /// </summary>
  public class ScaledPageSize
  {
    /// <summary>
    /// Ширина.
    /// </summary>
    /// <value>По умолчанию пустое значение. Заполняется при расчете размеров страницы.</value>
    public double Width { get; set; }
    
    /// <summary>
    /// Высота.
    /// </summary>
    /// <value>По умолчанию пустое значение. Заполняется при расчете размеров страницы.</value>
    public double Height { get; set; }
  }
  
  /// <summary>
  /// Координаты отметки.
  /// </summary>
  public class MarkCoordinates
  {
    /// <summary>
    /// Страница.
    /// </summary>
    public int Page { get; set; }
    
    /// <summary>
    /// Отступ по оси X.
    /// </summary>
    public double XIndent { get; set; }
    
    /// <summary>
    /// Отступ по оси Y.
    /// </summary>
    public double YIndent { get; set; }
  }
}