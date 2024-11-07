using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentComparer.Comparers;
using Newtonsoft.Json;
using Sungero.Core;
using Sungero.Docflow.Structures.Module;

namespace Sungero.Docflow.Isolated.DocumentBodyComparer
{
  public class IsolatedFunctions
  {

    /// <summary>
    /// Сравнить сконвертированные в PDF тела.
    /// </summary>
    /// <param name="firstPdfStream">Тело исходного документа.</param>
    /// <param name="secondPdfStream">Тело документа с изменениями.</param>
    /// <returns>Результат сравнения.</returns>
    [Public]
    public virtual IDocumentComparisonResult ComparePdfVersions(Stream firstPdfStream, Stream secondPdfStream)
    {
      try
      {
        var comparer = this.CreateDocumentComparer();
        var result = DocumentComparisonResult.Create();
        
        using (var resultStream = new MemoryStream())
        {
          var diffs = comparer.Compare(firstPdfStream, secondPdfStream, resultStream);
          result.DifferencesCount = diffs.Count;
          if (diffs.Any())
            result.ResultPdf = resultStream.ToArray();
        }
        
        return result;
      }
      catch (DocumentComparer.Exceptions.NothingToCompareException ex)
      {
        // Обработать ситуацию, когда тела слишком разные.
        if (ex.Message.Contains("The documents are different. Comparison is not allowed."))
        {
          var result = DocumentComparisonResult.Create();
          result.DocumentsAreDifferent = true;
          return result;
        }
        else
        {
          Logger.Error("Cannot compare documents", ex);
          throw new AppliedCodeException(ex.Message);
        }
      }
      catch (Exception ex)
      {
        Logger.Error("Cannot compare documents", ex);
        throw new AppliedCodeException(ex.Message);
      }
    }
    
    /// <summary>
    /// Создать экземпляр класса сравнения документов.
    /// </summary>
    /// <returns>Экземпляр класса сравнения документов.</returns>
    public virtual DocumentComparer.Comparers.PdfComparer CreateDocumentComparer()
    {
      return new DocumentComparer.Comparers.PdfComparer();
    }
    
  }
}