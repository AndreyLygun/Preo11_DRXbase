using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalConvertPdfStage;

namespace Sungero.Docflow.Server
{
  partial class ApprovalConvertPdfStageFunctions
  {

    public override Docflow.Structures.ApprovalFunctionStageBase.ExecutionResult Execute(IApprovalTask approvalTask)
    {
      Logger.DebugFormat("ApprovalConvertToPdfStage. Start execute convert to pdf for task id: {0}, start id: {1}.", approvalTask.Id, approvalTask.StartId);
      
      var result = base.Execute(approvalTask);
      
      var documents = new List<IOfficialDocument>();
      
      var documentFromTask = approvalTask.DocumentGroup.OfficialDocuments.SingleOrDefault();
      if (documentFromTask == null)
      {
        Logger.ErrorFormat("ApprovalConvertToPdfStage. Primary document not found. task id: {0}, start id: {1}", approvalTask.Id, approvalTask.StartId);
        return this.GetErrorResult(Sungero.Docflow.Resources.PrimaryDocumentNotFoundError);
      }
      
      documents.Add(documentFromTask);
      if (_obj.ConvertWithAddenda == true)
      {
        var addenda = approvalTask.AddendaGroup.OfficialDocuments.ToList();
        documents.AddRange(addenda);
      }
      
      var documentsToConvert = new List<IOfficialDocument>();
      foreach (var document in documents)
      {
        if (!document.HasVersions)
        {
          Logger.DebugFormat("ApprovalConvertToPdfStage. Document with Id {0} has no version.", document.Id);
          continue;
        }
        
        var validationResult = PublicFunctions.OfficialDocument.Remote.ValidateDocumentBeforeConversion(document, document.LastVersion.Id);
        if (validationResult.HasErrors)
        {
          if (validationResult.HasLockError)
          {
            Logger.DebugFormat("ApprovalConvertToPdfStage. {0}", validationResult.ErrorMessage);
            return this.GetRetryResult(validationResult.ErrorMessage);
          }
          else
          {
            Logger.Debug(validationResult.ErrorMessage);
            continue;
          }
        }
        else if (validationResult.IsExchangeDocument)
        {
          Logger.DebugFormat("ApprovalConvertToPdfStage. Document with Id {0} is exchange document. Skipped converting to PDF.", document.Id);
          continue;
        }
        
        documentsToConvert.Add(document);
      }
      
      var useObsoletePdfConversion = Sungero.Docflow.PublicFunctions.Module.Remote.UseObsoletePdfConversion();
      if (!useObsoletePdfConversion)
      {
        foreach (var document in documentsToConvert)
          this.UpdateDocumentMarks(document);
      }
      
      var results = this.ConvertToPdfWithResult(documentsToConvert);
      if (results.Any(x => x.HasConvertionError))
      {
        var errorMessage = string.Join(";" + Environment.NewLine, results.Where(x => x.HasConvertionError).Select(x => x.ErrorMessage));
        result = this.GetErrorResult(errorMessage);
      }
      else
      {
        result = this.GetSuccessResult();
      }
      
      return result;
    }
    
    /// <summary>
    /// Сконвертировать документы в PDF.
    /// </summary>
    /// <param name="documents">Список документов.</param>
    /// <returns>Информация о результатах конвертации документов в pdf.</returns>
    public virtual List<Sungero.Docflow.Structures.OfficialDocument.IConversionToPdfResult> ConvertToPdfWithResult(List<IOfficialDocument> documents)
    {
      var results = new List<Sungero.Docflow.Structures.OfficialDocument.IConversionToPdfResult>();
      
      foreach (var document in documents)
      {
        try
        {
          Logger.DebugFormat("ApprovalConvertToPdfStage. Start convert to pdf for document id {0}.", document.Id);
          var conversionResult = this.ConvertToPdf(document);
          results.Add(conversionResult);
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("ApprovalConvertToPdfStage. Convert to pdf error. Document Id {0}, Version Id {1}", ex, document.Id, document.LastVersion.Id);
          var conversionResult = Sungero.Docflow.Structures.OfficialDocument.ConversionToPdfResult.Create();
          conversionResult.ErrorTitle = OfficialDocuments.Resources.ConvertionErrorTitleBase;
          conversionResult.ErrorMessage = ex.Message;
          conversionResult.HasConvertionError = true;
          conversionResult.HasErrors = true;
          results.Add(conversionResult);
        }
      }
      
      return results;
    }
    
    /// <summary>
    /// Преобразовать тело документа в формат pdf.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Информация о результате генерации PublicBody для версии документа.</returns>
    public virtual Structures.OfficialDocument.IConversionToPdfResult ConvertToPdf(IOfficialDocument document)
    {
      return Docflow.PublicFunctions.Module.ConvertToPdf(document);
    }
    
    /// <summary>
    /// Актуализировать отметки документа для проставления.
    /// </summary>
    /// <param name="document">Документ.</param>
    public virtual void UpdateDocumentMarks(IOfficialDocument document)
    {
      if (document.LastVersionApproved.GetValueOrDefault())
      {
        Logger.DebugFormat("ApprovalConvertToPdfStage. Create or update signature mark for document id {0}.", document.Id);
        var signatureMark = Functions.OfficialDocument.GetOrCreateSignatureMark(document);
        signatureMark.Save();
      }
      else
      {
        Logger.DebugFormat("ApprovalConvertToPdfStage. Delete signature mark for document id {0}.", document.Id);
        Functions.OfficialDocument.DeleteSignatureMark(document);
      }
      
      if (!Functions.OfficialDocument.IsNotRegistered(document) &&
          _obj.AddRegistrationDetails.GetValueOrDefault())
      {
        Logger.DebugFormat("ApprovalConvertToPdfStage. Create or update registration marks for document id {0}.", document.Id);
        var regNumberMark = Functions.OfficialDocument.GetOrCreateRegistrationNumberMark(document);
        regNumberMark.Save();
        var regDateMark = Functions.OfficialDocument.GetOrCreateRegistrationDateMark(document);
        regDateMark.Save();
      }
      else
      {
        Logger.DebugFormat("ApprovalConvertToPdfStage. Delete registration marks for document id {0}.", document.Id);
        Functions.OfficialDocument.DeleteRegistrationNumberMark(document);
        Functions.OfficialDocument.DeleteRegistrationDateMark(document);
      }
    }
  }
}