using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentComparisonInfo;

namespace Sungero.Docflow.Server
{
  partial class DocumentComparisonInfoFunctions
  {
    /// <summary>
    /// Получить последний результат сравнения документов по хэшу сравниваемых версий.
    /// </summary>
    /// <param name="firstVersionHash">Хэш версии до изменения.</param>
    /// <param name="secondVersionHash">Хэш версии после изменения.</param>
    /// <param name="author">Инициатор сравнения.</param>
    /// <returns>Если дубликат найден, возвращает запись результата сравнения, иначе - null.</returns>
    /// <remarks>Последним считается результат сравнения с максимальным ИД.</remarks>
    [Public, Remote]
    public static IDocumentComparisonInfo GetLastDocumentComparisonInfo(string firstVersionHash, string secondVersionHash, IUser author)
    {
      return DocumentComparisonInfos
        .GetAll()
        .Where(x => Equals(x.Author, author))
        .Where(x => x.FirstVersionHash == firstVersionHash && x.SecondVersionHash == secondVersionHash)
        .OrderByDescending(x => x.Id)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить версии, сконвертированные в PDF через Ario.
    /// </summary>
    /// <returns>Статус обработки.</returns>
    [Public]
    public virtual Sungero.Core.Enumeration? FillPdfVersionsFromArio()
    {
      var errorMessage = string.Empty;
      var isFirstVersionConverted = _obj.FirstPdfVersion != null && _obj.FirstPdfVersion.Size > 0;
      var isSecondVersionConverted = _obj.SecondPdfVersion != null && _obj.SecondPdfVersion.Size > 0;
      
      try
      {
        if (!isFirstVersionConverted && _obj.FirstArioTaskId.HasValue)
        {
          // TODO Kotegov: Выделить в отдельную функцию проверки статуса конвертции в Ario.
          Logger.DebugFormat("FillPdfVersionsFromArio: start getting first pdf version from Ario. Document ID = {0}, version = {1} (cmpid={2}).",  _obj.FirstDocumentId, _obj.FirstVersionNumber, _obj.Id);
          var firstArioResult = PublicFunctions.Module.GetConversionResultFromArio(_obj.FirstArioTaskId.Value);
          if (firstArioResult.HasErrors)
          {
            Logger.ErrorFormat("FillPdfVersionsFromArio: cannot convert first document to pdf. Document ID = {0}, version = {1} (cmpid={2}).", _obj.FirstDocumentId, _obj.FirstVersionNumber, _obj.Id);
            errorMessage = Resources.ErrorWhileComparingDocuments;
          }
          else if (firstArioResult.IsConversionCompleted)
          {
            using (var firstPdfStream = new MemoryStream(firstArioResult.Body))
              _obj.FirstPdfVersion.Write(firstPdfStream);
            _obj.Save();
            isFirstVersionConverted = true;
          }
        }
        
        if (string.IsNullOrEmpty(errorMessage) && !isSecondVersionConverted && _obj.SecondArioTaskId.HasValue)
        {
          Logger.DebugFormat("FillPdfVersionsFromArio: start getting second pdf version from Ario. Document ID = {0}, version = {1} (cmpid={2}).",  _obj.SecondDocumentId, _obj.SecondVersionNumber, _obj.Id);
          var secondArioResult = PublicFunctions.Module.GetConversionResultFromArio(_obj.SecondArioTaskId.Value);
          if (secondArioResult.HasErrors)
          {
            Logger.ErrorFormat("FillPdfVersionsFromArio: cannot convert second document to pdf. Document ID = {0}, version = {1} (cmpid={2}).", _obj.SecondDocumentId, _obj.SecondVersionNumber, _obj.Id);
            errorMessage = Resources.ErrorWhileComparingDocuments;
          }
          else if (secondArioResult.IsConversionCompleted)
          {
            using (var secondPdfStream = new MemoryStream(secondArioResult.Body))
              _obj.SecondPdfVersion.Write(secondPdfStream);
            _obj.Save();
            isSecondVersionConverted = true;
          }
        }
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("FillPdfVersionsFromArio: error while getting pdf versions from Ario (cmpid={0}).", ex, _obj.Id);
        errorMessage = Resources.ErrorWhileComparingDocuments;
      }
      
      if (!string.IsNullOrEmpty(errorMessage))
      {
        _obj.ErrorMessage = errorMessage;
        _obj.ProcessingStatus = ProcessingStatus.Error;
      }
      else if (isFirstVersionConverted && isSecondVersionConverted)
      {
        _obj.ProcessingStatus = ProcessingStatus.PdfConverted;
      }
      
      _obj.Save();
      Logger.DebugFormat("FillPdfVersionsFromArio: end getting pdf versions from Ario (cmpid={0}).", _obj.Id);
      return _obj.ProcessingStatus;
    }
    
    /// <summary>
    /// Конвертировать тела в PDF.
    /// </summary>
    /// <returns>Статус обработки.</returns>
    [Public]
    public virtual Sungero.Core.Enumeration? ConvertVersionsToPdf()
    {
      try
      {
        _obj.ProcessingStatus = ProcessingStatus.PdfConverting;

        Logger.DebugFormat("ConvertVersionsToPdf: start converting first document ID = {0}, version = {1} (cmpid={2}).", _obj.FirstDocumentId, _obj.FirstVersionNumber, _obj.Id);
        var firstVersionBody = Functions.Module.GetVersionBody(_obj.FirstDocumentId.Value, _obj.FirstVersionNumber.Value);
        if (_obj.FirstVersionHash != firstVersionBody.Hash)
          _obj.FirstVersionHash = firstVersionBody.Hash;
        var firstPdfConversionResult = this.GetConvertedPdfVersion(Functions.Module.GetBinaryData(firstVersionBody), _obj.FirstVersionHash, _obj.FirstVersionExtension);
        
        // Вернуть ошибку конвертации, если не получилось сконвертировать версию в pdf.
        if (firstPdfConversionResult.HasErrors)
        {
          Logger.ErrorFormat("ConvertVersionsToPdf: cannot convert first document ID = {0}, version = {1} (cmpid={2}).", _obj.FirstDocumentId, _obj.FirstVersionNumber, _obj.Id);
          _obj.ErrorMessage = Resources.ErrorWhileComparingDocuments;
          _obj.ProcessingStatus = ProcessingStatus.Error;
          _obj.Save();
          return ProcessingStatus.Error;
        }
        
        // Заполнить сконвертированную pdf версию первого документа, если удалось сконвертировать сразу.
        if (firstPdfConversionResult.IsConversionCompleted)
        {
          using (var pdfStream = new MemoryStream(firstPdfConversionResult.Body))
            _obj.FirstPdfVersion.Write(pdfStream);
        }
        
        // Заполнить ИД задачи в Ario, если отправили на конвертацию.
        if (firstPdfConversionResult.IsArioConversion)
          _obj.FirstArioTaskId = firstPdfConversionResult.ArioTaskId;

        Logger.DebugFormat("ConvertVersionsToPdf: start converting second document ID = {0}, version = {1} (cmpid={2}).", _obj.SecondDocumentId, _obj.SecondVersionNumber, _obj.Id);
        var secondVersionBody = Functions.Module.GetVersionBody(_obj.SecondDocumentId.Value, _obj.SecondVersionNumber.Value);
        if (_obj.SecondVersionHash != secondVersionBody.Hash)
          _obj.SecondVersionHash = secondVersionBody.Hash;
        
        var secondPdfConversionResult = this.GetConvertedPdfVersion(Functions.Module.GetBinaryData(secondVersionBody), _obj.SecondVersionHash, _obj.SecondVersionExtension);
        
        // Вернуть ошибку конвертации, если не получилось сконвертировать версию в pdf.
        if (secondPdfConversionResult.HasErrors)
        {
          Logger.ErrorFormat("ConvertVersionsToPdf: cannot convert second document ID = {0}, version = {1} (cmpid={2}).", _obj.SecondDocumentId, _obj.SecondVersionNumber, _obj.Id);
          _obj.ErrorMessage = Resources.ErrorWhileComparingDocuments;
          _obj.ProcessingStatus = ProcessingStatus.Error;
          _obj.Save();
          return ProcessingStatus.Error;
        }
        
        // Заполнить сконвертированную pdf версию первого документа, если удалось сконвертировать сразу.
        if (secondPdfConversionResult.IsConversionCompleted)
        {
          using (var pdfStream = new MemoryStream(secondPdfConversionResult.Body))
            _obj.SecondPdfVersion.Write(pdfStream);
        }
        
        // Заполнить ИД задачи в Ario, если отправили на конвертацию.
        if (secondPdfConversionResult.IsArioConversion)
          _obj.SecondArioTaskId = secondPdfConversionResult.ArioTaskId;
        
        // Если удалось сконвертировать сразу обе версии, то статус конвертации - Завершено.
        if (_obj.FirstPdfVersion != null && _obj.FirstPdfVersion.Size > 0 &&
            _obj.SecondPdfVersion != null && _obj.SecondPdfVersion.Size > 0)
        {
          _obj.ProcessingStatus = ProcessingStatus.PdfConverted;
          Logger.DebugFormat("ConvertVersionsToPdf: end converting versions to pdf (cmpid={0}).", _obj.Id);
        }
        
        _obj.Save();
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("ConvertVersionsToPdf: error while converting document versions (cmpid={0}).", ex, _obj.Id);
        _obj.ErrorMessage = Resources.ErrorWhileComparingDocuments;
        _obj.ProcessingStatus = ProcessingStatus.Error;
        _obj.Save();
      }
      
      return _obj.ProcessingStatus;
    }
    
    /// <summary>
    /// Получить версию, сконвертированную в PDF.
    /// </summary>
    /// <param name="body">Тело исходной версии.</param>
    /// <param name="versionHash">Хэш версии.</param>
    /// <param name="extension">Расширение версии.</param>
    /// <returns>Статус обработки.</returns>
    [Public]
    public virtual Structures.Module.IPdfConversionResult GetConvertedPdfVersion(byte[] body, string versionHash, string extension)
    {
      // Проверить наличие тела.
      if (body == null || body.Length == 0)
      {
        Logger.Error("GetConvertedPdfVersion. Document does not have content.");
        return Functions.Module.CreatePdfConversionResult(null, false, 0, false, true);
      }
      
      // Проверить текстовый слой. Если документ PDF с текстовым слоем, вернуть исходное тело.
      try
      {
        if (extension == Constants.OfficialDocument.PdfExtension)
        {
          bool hasTextLayer = false;
          using (var bodyStream = new MemoryStream(body))
            hasTextLayer = IsolatedFunctions.PdfConverter.CheckDocumentTextLayer(bodyStream);
          if (hasTextLayer)
          {
            Logger.Debug("GetConvertedPdfVersion. Pdf document contains text layer. Conversion not needed.");
            return Functions.Module.CreatePdfConversionResult(body, true, 0, false, false);
          }
        }
      }
      catch
      {
        Logger.Error("GetConvertedPdfVersion. Cannot check text layer.");
      }
      
      // Получить pdf из предыдущих результатов сравнения.
      var convertedVersion = this.GetCachedPdfVersion(versionHash);
      if (convertedVersion != null && convertedVersion.Length > 0)
        return Functions.Module.CreatePdfConversionResult(convertedVersion, true, 0, false, false);
      
      // Получить pdf с помощью Aspose.
      if (this.GetSupportedExtensionsForDocumentComparison().Contains(extension))
        return Functions.Module.ConvertDocumentToPdfByAspose(body, extension);
      
      // Отправить документ на асинхронную конвертацию в Ario.
      return Functions.Module.ConvertDocumentByArioAsync(body, string.Format("rxsource.{0}", extension));
    }
    
    /// <summary>
    /// Получить список поддерживаемых расширений для конвертации в PDF с текстовым слоем с помощью Aspose.
    /// </summary>
    /// <returns>Список поддерживаемых расширений.</returns>
    public virtual List<string> GetSupportedExtensionsForDocumentComparison()
    {
      return new List<string>() { "doc", "docx", "docm", "rtf", "odt", "txt" };
    }
    
    /// <summary>
    /// Сравнить сконвертированные в PDF тела.
    /// </summary>
    /// <returns>Статус обработки.</returns>
    [Public]
    public virtual Sungero.Core.Enumeration? ComparePdfVersions()
    {
      Logger.DebugFormat("ComparePdfVersions: start comparing (cmpid={0}).", _obj.Id);
      try
      {
        // Библиотека сравнения ожидает первым эталонный документ, а сравниваемый (измененный, доработанный) - вторым.
        Logger.DebugFormat("ComparePdfVersions: processing with DocumentComparer (cmpid={0}).", _obj.Id);
        var comparisonResult = IsolatedFunctions.DocumentBodyComparer.ComparePdfVersions(_obj.FirstPdfVersion.Read(), _obj.SecondPdfVersion.Read());
        
        // Обработать ответ от библиотеки для слишком разных тел.
        if (comparisonResult.DocumentsAreDifferent)
        {
          _obj.ErrorMessage = Resources.DocumentsAreDifferent;
          _obj.ProcessingStatus = ProcessingStatus.Error;
          _obj.Save();
          
          Logger.DebugFormat("ComparePdfVersions: The documents are different (cmpid={0}).", _obj.Id);
          return _obj.ProcessingStatus;
        }
        
        // Заполнить количество различий.
        _obj.DifferencesCount = comparisonResult.DifferencesCount;
        
        // Заполнить результат сравнения в формате PDF.
        if (comparisonResult.ResultPdf != null && comparisonResult.ResultPdf.Length > 0)
        {
          using (var pdfStream = new MemoryStream(comparisonResult.ResultPdf))
            _obj.ResultPdf.Write(pdfStream);
        }

        _obj.ProcessingStatus = ProcessingStatus.Compared;
        _obj.Save();
        Logger.DebugFormat("ComparePdfVersions: end comparing (cmpid={0}).", _obj.Id);
      }
      catch (Exception ex)
      {
        var documentsInfo = string.Format("First document(ID = {0}, version = {1}), second document (ID = {2}, version = {3})",
                                          _obj.FirstDocumentId, _obj.FirstVersionNumber, _obj.SecondDocumentId, _obj.SecondVersionNumber);
        Logger.ErrorFormat("ComparePdfVersions: error while comparing pdf versions (cmpid={0}). {1}", ex, _obj.Id, documentsInfo);
        
        _obj.ErrorMessage = Resources.ErrorWhileComparingDocuments;
        _obj.ProcessingStatus = ProcessingStatus.Error;
        _obj.Save();
      }
      
      return _obj.ProcessingStatus;
    }
    
    /// <summary>
    /// Получить сконвертированную в PDF версию из других результатов сравнения.
    /// </summary>
    /// <param name="versionHash">Хэш исходной версии.</param>
    /// <returns>Сконвертированное в PDF тело версии или null, если результат не найден.</returns>
    /// <remarks>Искать только среди результатов сравнения, доступных текущему инициатору сравнения (автору).</remarks>
    [Public]
    public virtual byte[] GetCachedPdfVersion(string versionHash)
    {
      var comparisonInfosBySameAuthor = DocumentComparisonInfos.GetAll().Where(d => Equals(d.Author, _obj.Author) &&
                                                                               d.Id != _obj.Id);
      
      var comparisonInfoWithFirstVersion = comparisonInfosBySameAuthor.FirstOrDefault(d => Equals(d.FirstVersionHash, versionHash) && d.FirstPdfVersion.Size > 0);
      if (comparisonInfoWithFirstVersion != null)
      {
        Logger.DebugFormat("GetCachedPdfVersion: found duplicate comparison result by first version hash, id - {0}, status - {1} (cmpid={2}).",
                           comparisonInfoWithFirstVersion.Id, comparisonInfoWithFirstVersion.ProcessingStatus, _obj.Id);
        return Functions.Module.GetBinaryData(comparisonInfoWithFirstVersion.FirstPdfVersion);
      }
      
      var comparisonInfoWithSecondVersion = comparisonInfosBySameAuthor.FirstOrDefault(d => Equals(d.SecondVersionHash, versionHash) && d.SecondPdfVersion.Size > 0);
      if (comparisonInfoWithSecondVersion != null)
      {
        Logger.DebugFormat("GetCachedPdfVersion: found duplicate comparison result by second version hash, id - {0}, status - {1} (cmpid={2}).",
                           comparisonInfoWithSecondVersion.Id, comparisonInfoWithSecondVersion.ProcessingStatus, _obj.Id);
        return Functions.Module.GetBinaryData(comparisonInfoWithSecondVersion.SecondPdfVersion);
      }
      
      return null;
    }
  }
}