using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.OfficialDocument;
using Sungero.Workflow;

namespace Sungero.DocflowApproval.Server.DocflowApprovalBlocks
{
  partial class RegisterFPoADocsWithFtsBlockHandlers
  {
    public virtual void RegisterFPoADocsWithFtsBlockStart()
    {
      if (!Sungero.Docflow.PublicFunctions.Module.IsPoAKonturLicenseEnable())
      {
        this.SetBlockErrorResult(Sungero.DocflowApproval.Resources.NoLicenseForRegisterToFts);
        return;
      }
      
      Logger.DebugFormat("RegisterFPoADocsWithFtsBlock. Start for task with id {0}.", _obj.Id);
      
      Logger.DebugFormat("RegisterFPoADocsWithFtsBlock. Try validate fpoa document before registration in FTS. Document Id {0}, task Id {1}",
                         _block.Document?.Id, _obj.Id);
      var validationError = Sungero.Docflow.PublicFunctions.Module.ValidatePoaDocumentBeforeRegistration(_block.Document);
      if (!string.IsNullOrEmpty(validationError))
      {
        Logger.DebugFormat("RegisterFPoADocsWithFtsBlock. Fpoa document is invalid. Document Id {0}, task Id {1}, validation error message: {2}",
                           _block.Document?.Id, _obj.Id, validationError);
        this.SetBlockErrorResult(validationError);
        return;
      }
      
      try
      {
        Logger.DebugFormat("RegisterFPoADocsWithFtsBlock. Try to send fpoa document for registration to FTS. Document Id {0}", _block.Document.Id);
        var sendingError = Sungero.Docflow.PublicFunctions.Module.SendPoaDocumentForRegistrationToFts(_block.Document, _obj.Id);
        if (!string.IsNullOrEmpty(sendingError))
        {
          Logger.DebugFormat("RegisterFPoADocsWithFtsBlock. Failed to send fpoa document for registration to FTS. Document Id {0}, error message: {1}",
                             _block.Document.Id, sendingError);
          this.SetBlockErrorResult(sendingError);
          return;
        }
        
        Logger.DebugFormat("RegisterFPoADocsWithFtsBlock. Document registration with FTS successfully started. Task Id {0}, document Id: {1}", _obj.Id, _block.Document.Id);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("RegisterFPoADocsWithFtsBlock. Registration in FTS error: {0}. Document Id {1}", ex, ex.Message, _block.Document.Id);
        this.SetBlockErrorResult(string.Empty);
        return;
      }
    }
    
    public virtual bool RegisterFPoADocsWithFtsBlockResult()
    {
      if (_block.OutProperties.ExecutionResult == ExecutionResult.Error)
        return true;
      
      if (_block.Document == null)
      {
        Logger.DebugFormat("RegisterFPoADocsWithFtsBlock. Error: fpoa document not found. Approval task Id {0}.", _obj.Id);
        this.SetBlockErrorResult(Sungero.Docflow.Resources.PrimaryDocumentNotFoundError);
        return true;
      }
      
      if (FormalizedPowerOfAttorneys.Is(_block.Document) &&
          FormalizedPowerOfAttorneys.As(_block.Document).FtsListState == Sungero.Docflow.FormalizedPowerOfAttorney.FtsListState.Revoked)
      {
        Logger.DebugFormat("RegisterFPoADocsWithFtsBlock. Registration error: fpoa was already revoked, Approval task Id {0}, Document Id {1}.", _obj.Id, _block.Document.Id);
        this.SetBlockErrorResult(FormalizedPowerOfAttorneys.Resources.FormalizedPowerOfAttorneySendForRegistrationError);
        return true;
      }
      
      var isFtsRegistrationComplete = Sungero.Docflow.PublicFunctions.Module.CheckPoaDocumentRegistrationIsComplete(_block.Document);
      if (!isFtsRegistrationComplete)
      {
        Logger.DebugFormat("RegisterFPoADocsWithFtsBlock. Retry check registration in FTS. Approval task Id {0}, Document Id {1}.", _obj.Id, _block.Document.Id);
        return false;
      }
      
      var ftsRegistrationError = Sungero.Docflow.PublicFunctions.Module.GetPoaDocumentFtsRegistrationError(_block.Document);
      if (!string.IsNullOrEmpty(ftsRegistrationError))
      {
        Logger.DebugFormat("RegisterFPoADocsWithFtsBlock. Registration error: {0}, Approval task Id {1}, Document Id {2}.", ftsRegistrationError, _obj.Id, _block.Document.Id);
        this.SetBlockErrorResult(ftsRegistrationError);
        return true;
      }
      
      Logger.DebugFormat("RegisterFPoADocsWithFtsBlock. Registration in FTS completed successfully. Approval task Id {0}, Document Id {1}.", _obj.Id, _block.Document.Id);
      _block.OutProperties.ExecutionResult = ExecutionResult.Success;
      return true;
    }
    
    /// <summary>
    /// Настроить выходные параметры блока при возникновении ошибки процесса регистрации документа в ФНС.
    /// </summary>
    /// <param name="errorMessage">Текст ошибки.</param>
    public virtual void SetBlockErrorResult(string errorMessage)
    {
      _block.OutProperties.ErrorMessage = errorMessage;
      _block.OutProperties.ExecutionResult = ExecutionResult.Error;
    }
  }

  partial class ConvertPdfBlockHandlers
  {
    public virtual void ConvertPdfBlockExecute()
    {
      this.ExecutionStarted();
      
      try
      {
        if (!this.BlockHasDocuments())
        {
          this.ExecutionDone();
          return;
        }

        var documents = Functions.Module.FilterDocumentsToConvertToPdf(_block.Documents.ToList());
        var useObsoletePdfConversion = Sungero.Docflow.PublicFunctions.Module.Remote.UseObsoletePdfConversion();
        if (!useObsoletePdfConversion)
        {
          foreach (var document in documents)
            this.UpdateDocumentMarks(document);
        }
        
        var results = this.ConvertToPdfWithResult(documents);
        if (results.Any(x => x.HasConvertionError || x.HasMarksError))
        {
          this.ExecutionConvertError();
          return;
        }
        else if (results.Any(x => x.HasErrors))
        {
          this.LogAction(Docflow.OfficialDocuments.Resources.ConvertionErrorTitleBase);
          if (this.ExecutionExpired())
            return;
          this.ExecutionRetried();
          return;
        }
      }
      catch (Sungero.Domain.Shared.Exceptions.RepeatedLockException ex)
      {
        this.LogAction("Exception occurred", string.Format("Message: {0}", ex.Message));
        if (this.ExecutionExpired())
          return;
        
        this.ExecutionRetried();
        return;
      }
      catch (Exception ex)
      {
        this.LogAction("Exception occurred", string.Format("Message: {0}", ex.Message));
        this.ExecutionConvertError();
        return;
      }
      
      this.ExecutionDone();
    }
    
    /// <summary>
    /// Проверить, переданы ли в блок документы.
    /// </summary>
    /// <returns>True - переданы, False - не переданы.</returns>
    public virtual bool BlockHasDocuments()
    {
      var hasDocuments = _block.Documents.Any();
      if (!hasDocuments)
        this.LogAction("Documents are not provided");
      
      return hasDocuments;
    }

    /// <summary>
    /// Проверить блокировки последних версий документов.
    /// </summary>
    /// <param name="documents">Список документов.</param>
    /// <exception cref="Sungero.Domain.Shared.Exceptions.RepeatedLockException">Последняя версия одного из документов заблокирована.</exception>
    [Obsolete("Метод не используется с 22.08.2024 и версии 4.11. Блокировки версий документов теперь проверяются в методе FilterDocumentsToConvertToPdf.")]
    public virtual void CheckDocumentLastVersionBodyLocks(List<IOfficialDocument> documents)
    {
      var firstLockedBodyDocument = documents.Where(x => Locks.GetLockInfo(x.LastVersion.Body).IsLocked).FirstOrDefault();
      if (firstLockedBodyDocument == null)
        return;
      
      var lockInfo = Locks.GetLockInfo(firstLockedBodyDocument.LastVersion.Body);
      var exceptionMessage = string.Format("Document (ID={0}) body locked by {1}", firstLockedBodyDocument.Id, lockInfo.OwnerName);
      this.LogAction("Check body locks", exceptionMessage);
      throw new Sungero.Domain.Shared.Exceptions.RepeatedLockException(true, lockInfo);
    }
    
    /// <summary>
    /// Сконвертировать документы в PDF.
    /// </summary>
    /// <param name="documents">Список документов.</param>
    [Obsolete("Метод не используется с 23.07.2024 и версии 4.11. Используйте метод ConvertToPdfWithResult.")]
    public virtual void ConvertToPdf(List<IOfficialDocument> documents)
    {
      this.ConvertToPdfWithResult(documents);
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
        var documentLogInfo = string.Format("Document (ID={0}, Version={1})", document.Id, document.LastVersion.Id);
        try
        {
          this.LogAction("Start converting to pdf", documentLogInfo);
          var conversionResult = Sungero.Docflow.PublicFunctions.Module.ConvertToPdf(document);
          if (conversionResult.HasErrors)
            this.LogAction("Converting to pdf failed", string.Format("{0}. {1}", documentLogInfo, conversionResult.ErrorMessage));
          results.Add(conversionResult);
        }
        catch (Exception ex)
        {
          this.LogAction("Converting to pdf failed", string.Format("{0}. {1}", documentLogInfo, ex.Message));
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
    /// Обработать начало выполнения блока.
    /// </summary>
    public virtual void ExecutionStarted()
    {
      this.LogAction("Start execution");
    }
    
    /// <summary>
    /// Проверить, истек ли срок исполнения блока.
    /// </summary>
    /// <returns>True - истек, False - иначе.</returns>
    public virtual bool ExecutionExpired()
    {
      var isExpired = !_block.Timeout.HasValue || _block.Timeout.Value < Calendar.Now;
      
      if (isExpired)
      {
        _block.OutProperties.ExecutionResult = ExecutionResult.Expired;
        this.LogAction("Execution expired");
      }
      
      return isExpired;
    }
    
    /// <summary>
    /// Обработать отправку исполнения на переповтор.
    /// </summary>
    public virtual void ExecutionRetried()
    {
      _block.RetrySettings.Retry = true;
      this.LogAction("Sent for retry");
    }
    
    /// <summary>
    /// Обработать успешное выполнение блока.
    /// </summary>
    public virtual void ExecutionDone()
    {
      _block.OutProperties.ExecutionResult = ExecutionResult.Success;
      this.LogAction("Execution done");
    }
    
    /// <summary>
    /// Обработать ошибку преобразования.
    /// </summary>
    public virtual void ExecutionConvertError()
    {
      _block.OutProperties.ExecutionResult = ExecutionResult.ConvertError;
      this.LogAction("Execution convert error. Sent to manual conversion");
    }
    
    /// <summary>
    /// Залогировать действие.
    /// </summary>
    /// <param name="action">Действие.</param>
    /// <param name="additional">Дополнительная информация.</param>
    public virtual void LogAction(string action, object additional = null)
    {
      var parts = new List<string>();
      parts.Add("ApprovalConvertToPdfBlock");
      if (!string.IsNullOrWhiteSpace(action))
        parts.Add(action);
      parts.Add($"Task (ID={_obj.MainTaskId})");
      parts.Add($"StartId: {_obj.MainTask.StartId}");
      parts.Add($"RetryIteration: {_block.RetrySettings.RetryIteration}");
      if (additional != null)
        parts.Add(additional.ToString());
      
      Logger.Debug(string.Join(". ", parts));
    }
    
    /// <summary>
    /// Актуализировать отметки документа для проставления.
    /// </summary>
    /// <param name="document">Документ.</param>
    public virtual void UpdateDocumentMarks(IOfficialDocument document)
    {
      if (document.LastVersionApproved.GetValueOrDefault())
      {
        var signatureMark = Sungero.Docflow.PublicFunctions.OfficialDocument.Remote.GetOrCreateSignatureMark(document);
        signatureMark.Save();
      }
      else
      {
        Sungero.Docflow.PublicFunctions.OfficialDocument.Remote.DeleteSignatureMark(document);
      }
      
      if (_block.AddRegistrationDetails.HasValue && _block.AddRegistrationDetails.Value &&
          !Sungero.Docflow.PublicFunctions.OfficialDocument.IsNotRegistered(document))
      {
        var regNumberMark = Sungero.Docflow.PublicFunctions.OfficialDocument.Remote.GetOrCreateRegistrationNumberMark(document);
        regNumberMark.Save();
        var regDateMark = Sungero.Docflow.PublicFunctions.OfficialDocument.Remote.GetOrCreateRegistrationDateMark(document);
        regDateMark.Save();
      }
      else
      {
        Sungero.Docflow.PublicFunctions.OfficialDocument.Remote.DeleteRegistrationNumberMark(document);
        Sungero.Docflow.PublicFunctions.OfficialDocument.Remote.DeleteRegistrationDateMark(document);
      }
    }
  }

  partial class WaitForCounterpartySignBlockHandlers
  {

    public virtual void WaitForCounterpartySignBlockStart()
    {
      Logger.DebugFormat("WaitForCounterpartySignBlock. Start for task with id {0}.", _obj.Id);
      
      var officialDocument = Docflow.OfficialDocuments.As(_block.Document);
      if (officialDocument == null)
        return;
      
      if (officialDocument.ExternalApprovalState == ExternalApprovalState.Signed)
        return;
      
      var exchangeDocumentInfo = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(officialDocument);
      var isExchangeDocument = exchangeDocumentInfo != null;
      if (isExchangeDocument)
      {
        Logger.DebugFormat("WaitForCounterpartySignBlock. Update exchange documents tracking (TaskId = {0}, ServiceMessageId = {1}).",
                           _obj.Id, exchangeDocumentInfo.ServiceMessageId);
        var packageInfos = Exchange.PublicFunctions.Module.GetOutgoingPackageDocumentsExchangeInfos(exchangeDocumentInfo.ServiceMessageId);
        var returnDeadline = this.GetReturnDeadlineForExchangeDocumentsPackage(packageInfos);
        Functions.Module.UpdateExchangeDocumentsTrackingAfterSending(packageInfos, returnDeadline, _obj, null);
      }
      else
      {
        Logger.DebugFormat("WaitForCounterpartySignBlock. Update paper document tracking (TaskId = {1}, DocumentId = {0}).",
                           officialDocument.Id, _obj.Id);
        // Если не смогли определить ответственного за документ, приостанавливаем задачу,
        // т.к. не знаем, кого указать в поле "Кому передан" в выдаче документа.
        var responsible = Functions.Module.GetResponsibleToReturn(officialDocument, _obj);
        if (responsible == null || responsible.IsSystem == true)
          throw AppliedCodeException.Create(Sungero.DocflowApproval.Resources.NoResponsibleToReturnFormat(officialDocument.Id));

        if (_block.RelativeDeadline == TimeSpan.Zero)
          _block.RelativeDeadline = TimeSpan.FromDays(DocflowApproval.Constants.Module.DefaultDaysToReturn);
        
        var returnDeadline = Calendar.GetUserNow(responsible)
          .AddWorkingDays(responsible, _block.RelativeDeadline.Days)
          .AddWorkingHours(responsible, _block.RelativeDeadline.Hours);
        Functions.Module.UpdatePaperDocumentTrackingAfterSending(officialDocument, responsible, returnDeadline, _obj);
        officialDocument.ExternalApprovalState = ExternalApprovalState.OnApproval;
      }
    }

    public virtual bool WaitForCounterpartySignBlockResult()
    {
      if (_block.Document == null)
        return false;
      
      var officialDocument = Docflow.OfficialDocuments.As(_block.Document);
      if (officialDocument == null)
        return false;
      
      var exchangeDocumentInfo = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(officialDocument);
      var isExchangeDocument = exchangeDocumentInfo != null;
      if (isExchangeDocument)
      {
        var executionResult = this.GetExecutionResultForExchangeDocument(officialDocument);
        if (executionResult != null)
          _block.OutProperties.ExecutionResult = executionResult;
        Logger.DebugFormat("WaitForCounterpartySignBlock. Done for task with id {0} for exchange document.", _obj.Id);
        return executionResult != null;
      }
      
      var isSigned = officialDocument.ExternalApprovalState == ExternalApprovalState.Signed;
      var isUnsigned = officialDocument.ExternalApprovalState == ExternalApprovalState.Unsigned;
      if (isSigned || isUnsigned)
      {
        var unreturnedTracking = Docflow.PublicFunctions.OfficialDocument.GetUnreturnedFromCounterpartyTracking(officialDocument, _obj);
        if (unreturnedTracking.Any())
        {
          var responsible = unreturnedTracking.First().DeliveredTo;
          Docflow.PublicFunctions.OfficialDocument.UpdateTrackingAfterReturnFromCounterparty(officialDocument, responsible, _obj, isSigned);
        }
        _block.OutProperties.ExecutionResult = isSigned ? ExecutionResult.Success : ExecutionResult.NotSigned;
        
        Logger.DebugFormat("WaitForCounterpartySignBlock. Done for task with id {0} for paper document.", _obj.Id);
        return true;
      }
      
      return false;
    }
    
    /// <summary>
    /// Вычислить результат выполнения блока "Ожидание подписания контрагентом" для документа эл. обмена.
    /// </summary>
    /// <param name="document">Документ эл. обмена.</param>
    /// <returns>Результат выполнения блока.</returns>
    protected virtual Enumeration? GetExecutionResultForExchangeDocument(IOfficialDocument document)
    {
      if (document.ExchangeState == Docflow.OfficialDocument.ExchangeState.Sent ||
          document.ExchangeState == Docflow.OfficialDocument.ExchangeState.Received ||
          document.ExchangeState == Docflow.OfficialDocument.ExchangeState.SignRequired)
      {
        return ExecutionResult.Deadline;
      }
      
      if (document.ExchangeState == Docflow.OfficialDocument.ExchangeState.Signed)
        return ExecutionResult.Success;
      
      if (document.ExchangeState == Docflow.OfficialDocument.ExchangeState.Rejected ||
          document.ExchangeState == Docflow.OfficialDocument.ExchangeState.Terminated ||
          document.ExchangeState == Docflow.OfficialDocument.ExchangeState.Obsolete)
      {
        return ExecutionResult.NotSigned;
      }
      
      return null;
    }
    
    /// <summary>
    /// Получить срок возврата для комплекта документов с учетом настроек блока и рабочего времени ответственного за возврат.
    /// </summary>
    /// <param name="infos">Сведения о документах обмена.</param>
    /// <returns>Срок возврата.</returns>
    /// <remarks>Если ни у одного из документов нет выдачи с пустой датой возврата,
    /// то срок возврата будет - сумма текущего времени сервера и срока прекращения в блоке.</remarks>
    protected virtual DateTime GetReturnDeadlineForExchangeDocumentsPackage(List<Exchange.IExchangeDocumentInfo> infos)
    {
      var unreturnedTracking = infos.Where(x => x.Document != null)
        .Select(x => Docflow.PublicFunctions.OfficialDocument.GetUnreturnedFromCounterpartyTracking(x.Document, _obj)
                .FirstOrDefault(t => t.ExternalLinkId == x.Id))
        .Where(x => x != null)
        .OrderByDescending(x => x.DeliveryDate)
        .FirstOrDefault();
      
      var returnDeadline = Calendar.Now
        .AddWorkingDays(_block.RelativeDeadline.Days)
        .AddWorkingHours(_block.RelativeDeadline.Hours);
      
      if (unreturnedTracking != null && unreturnedTracking.DeliveryDate.HasValue)
      {
        var responsible = unreturnedTracking.DeliveredTo;
        returnDeadline = Calendar.GetUserNow(responsible)
          .AddWorkingDays(responsible, _block.RelativeDeadline.Days)
          .AddWorkingHours(responsible, _block.RelativeDeadline.Hours);
        
        if (unreturnedTracking.DeliveryDate.Value > returnDeadline)
          returnDeadline = unreturnedTracking.DeliveryDate.Value;
      }
      
      return returnDeadline;
    }
  }

  partial class AdvancedAssignmentBlockHandlers
  {
    public virtual void AdvancedAssignmentBlockStartAssignment(Sungero.DocflowApproval.IAdvancedAssignment assignment)
    {
      if (_block.AllowSendForRework.GetValueOrDefault())
        assignment.ReworkPerformer = _block.ReworkPerformer;
    }
    
    public virtual void AdvancedAssignmentBlockCompleteAssignment(Sungero.DocflowApproval.IAdvancedAssignment assignment)
    {
      Functions.AdvancedAssignment.RelateAddedAddendaToPrimaryDocument(assignment);
      Functions.Module.GrantReadAccessRightsToDocuments(assignment.AddendaGroup.ElectronicDocuments.ToList(), assignment.Task.Author);
      
      if (assignment.Result == Sungero.DocflowApproval.EntityApprovalAssignment.Result.Forward)
        assignment.Forward(assignment.ForwardTo, ForwardingLocation.Next);
    }
  }

  partial class SigningBlockHandlers
  {

    public virtual void SigningBlockStart()
    {
      // Удалить исполнителей, которые уже подписали.
      Functions.Module.RemovePerformersWhoAlreadySigned(_block, _obj);
    }

    public virtual void SigningBlockStartAssignment(Sungero.DocflowApproval.ISigningAssignment assignment)
    {
      assignment.ReworkPerformer = _block.ReworkPerformer;
      
      // Отправить запрос на подготовку предпросмотра для документов.
      Functions.Module.PrepareAllAttachmentsPreviews(assignment);
      
      // Установить статус согласования документа - На подписании.
      var officialDocument = OfficialDocuments.As(assignment.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      var pendingSignState = InternalApprovalState.PendingSign;
      if (officialDocument != null)
        Docflow.PublicFunctions.OfficialDocument.UpdateDocumentApprovalState(officialDocument, pendingSignState, _obj.Id);
    }
    
    public virtual void SigningBlockCompleteAssignment(Sungero.DocflowApproval.ISigningAssignment assignment)
    {
      if (assignment.Result == DocflowApproval.SigningAssignment.Result.Forward)
        assignment.Forward(assignment.ForwardTo, ForwardingLocation.Next);
      
      Functions.SigningAssignment.RelateAddedAddendaToPrimaryDocument(assignment);
      Functions.Module.GrantReadAccessRightsToDocuments(assignment.AddendaGroup.ElectronicDocuments.ToList(), assignment.Task.Author);
      
      if (assignment.Result == DocflowApproval.SigningAssignment.Result.Sign)
        Functions.SigningAssignment.UpdateDocumentAndAddendaStateAfterSign(assignment);
    }
    
  }

  partial class CheckReturnFromCounterpartyBlockHandlers
  {
    public virtual void CheckReturnFromCounterpartyBlockStartAssignment(Sungero.DocflowApproval.ICheckReturnFromCounterpartyAssignment assignment)
    {
      Logger.DebugFormat("CheckReturnFromCounterpartyBlock. Start assignment with id {0} for task with id {1}", assignment.Id, _obj.Id);
      
      // Если отправлено одностороннее соглашение об аннулировании, выполнить контроль возврата сразу.
      if (Functions.CheckReturnFromCounterpartyAssignment.NeedAutoCompleteAssignment(assignment))
      {
        assignment.ActiveText = Exchange.Resources.ReturnTaskCompleteResultSigned;
        assignment.Complete(DocflowApproval.CheckReturnFromCounterpartyAssignment.Result.Signed);
      }
      
      var document = assignment.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      var officialDocument = OfficialDocuments.As(document);
      if (officialDocument == null)
        return;
      
      var isExchangeDocument = Functions.Module.IsExchangeDocument(officialDocument);
      if (officialDocument.ExternalApprovalState == null && !isExchangeDocument)
        officialDocument.ExternalApprovalState = ExternalApprovalState.OnApproval;
      
      this.UpdateDocumentTracking(officialDocument, isExchangeDocument, assignment);
    }

    public virtual void CheckReturnFromCounterpartyBlockCompleteAssignment(Sungero.DocflowApproval.ICheckReturnFromCounterpartyAssignment assignment)
    {
      Functions.CheckReturnFromCounterpartyAssignment.RelateAddedAddendaToPrimaryDocument(assignment);
      Functions.Module.GrantReadAccessRightsToDocuments(assignment.AddendaGroup.ElectronicDocuments.ToList(), assignment.Task.Author);
      
      var document = assignment.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      var officialDocument = OfficialDocuments.As(document);
      if (officialDocument == null)
        return;
      
      var isExchangeDocument = Functions.Module.IsExchangeDocument(officialDocument);
      var isSigned = assignment.Result == DocflowApproval.CheckReturnFromCounterpartyAssignment.Result.Signed;
      var isNotSigned = assignment.Result == DocflowApproval.CheckReturnFromCounterpartyAssignment.Result.NotSigned;
      if ((isSigned || isNotSigned) && !isExchangeDocument)
      {
        officialDocument.ExternalApprovalState = isSigned ? ExternalApprovalState.Signed : ExternalApprovalState.Unsigned;
        Docflow.PublicFunctions.OfficialDocument.UpdateTrackingAfterReturnFromCounterparty(officialDocument, assignment.Performer, _obj, isSigned);
      }
      
      Logger.DebugFormat("CheckReturnFromCounterpartyBlock. Complete assignment with id {0} for task with id {1}", assignment.Id, _obj.Id);
    }
    
    /// <summary>
    /// Обновить выдачу документа с учетом настроек блока.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="isExchangeDocument">Признак, является ли документ МКДО.</param>
    /// <param name="assignment">Задание на контроль возврата от контрагента.</param>
    public virtual void UpdateDocumentTracking(IOfficialDocument document, bool isExchangeDocument,
                                               Sungero.DocflowApproval.ICheckReturnFromCounterpartyAssignment assignment)
    {
      var performer = _block.IsParallel
        ? Company.PublicFunctions.Module.GetEmployeesFromRecipients(_block.Performers.ToList()).FirstOrDefault()
        : assignment.Performer;
      var returnDeadline = this.GetReturnDeadlineForDocument(document, performer);
      
      // При параллельном создании заданий обновляем выдачу только для одного из исполнителей.
      // Обновляем выдачу здесь, а не на старте блока, т.к. на старте еще нет возможности получить только основной документ (нет групп вложений).
      if (!Equals(performer, assignment.Performer))
        return;
      
      var exchangeDocumentInfo = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(document);
      if (isExchangeDocument)
      {
        var documentsPackage = Exchange.PublicFunctions.Module.GetOutgoingPackageDocumentsExchangeInfos(exchangeDocumentInfo.ServiceMessageId)
          .Where(d => d.ExchangeState == Sungero.Docflow.OfficialDocument.ExchangeState.SignAwaited)
          .ToList();
        Functions.Module.UpdateExchangeDocumentsTrackingAfterSending(documentsPackage, returnDeadline, _obj, performer.Id);
      }
      else
      {
        if (document.ExternalApprovalState == null)
          document.ExternalApprovalState = ExternalApprovalState.OnApproval;
        
        Docflow.PublicFunctions.OfficialDocument.AddOrUpdateEndorsementInfoInTracking(document, performer.Id, returnDeadline, _obj);
      }
    }
    
    /// <summary>
    /// Получить срок возврата документа с учетом выдачи, настроек блока и рабочего времени ответственного.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="performer">Ответственный за контроль возврата.</param>
    /// <returns>Срок возврата.</returns>
    /// <remarks>Если в настройках блока не указан срок возврата, берется значение по умолчанию - 10 рабочих дней.
    /// Если дата выдачи больше конечного срока возврата, то возвращаем дату выдачи.</remarks>
    public virtual DateTime GetReturnDeadlineForDocument(IOfficialDocument document, IUser performer)
    {
      var returnDeadline = Calendar.GetUserNow(performer);
      if (!_block.RelativeDeadlineDays.HasValue && !_block.RelativeDeadlineHours.HasValue)
        returnDeadline = returnDeadline.AddWorkingDays(performer, DocflowApproval.Constants.Module.DefaultDaysToReturn);
      if (_block.RelativeDeadlineDays.HasValue)
        returnDeadline = returnDeadline.AddWorkingDays(performer, _block.RelativeDeadlineDays.Value);
      if (_block.RelativeDeadlineHours.HasValue)
        returnDeadline = returnDeadline.AddWorkingHours(performer, _block.RelativeDeadlineHours.Value);
      
      var latestTracking = Docflow.PublicFunctions.OfficialDocument.GetLatestDocumentTracking(document);
      if (latestTracking != null && latestTracking.DeliveryDate.Value > returnDeadline)
        returnDeadline = latestTracking.DeliveryDate.Value;
      
      return returnDeadline;
    }
  }

  partial class DocumentProcessingBlockHandlers
  {

    public virtual void DocumentProcessingBlockStartAssignment(Sungero.DocflowApproval.IDocumentProcessingAssignment assignment)
    {
      assignment.PrintDocument = _block.PrintDocument == true;
      assignment.RegisterDocument = _block.RegisterDocument == true;
      assignment.SendToCounterparty = _block.SendToCounterparty == true;
      assignment.CreateActionItems = _block.CreateActionItems == true;
      assignment.ReworkPerformer = _block.ReworkPerformer;
      assignment.DeliveryMethod = _block.DeliveryMethod;
      assignment.ExchangeService = _block.ExchangeService;

      // Статус "На исполнении".
      var officialDocument = OfficialDocuments.As(assignment.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (officialDocument != null && _block.CreateActionItems == true && officialDocument.ExecutionState != ExecutionState.Sending)
        officialDocument.ExecutionState = ExecutionState.Sending;
    }
    
    public virtual void DocumentProcessingBlockCompleteAssignment(Sungero.DocflowApproval.IDocumentProcessingAssignment assignment)
    {
      Functions.DocumentProcessingAssignment.RelateAddedAddendaToPrimaryDocument(assignment);
      Functions.Module.GrantReadAccessRightsToDocuments(assignment.AddendaGroup.ElectronicDocuments.ToList(), assignment.Task.Author);
      
      // Статус "Не требует исполнения".
      var officialDocument = OfficialDocuments.As(assignment.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (officialDocument == null)
        return;
      
      if (_block.CreateActionItems == true &&
          !Docflow.PublicFunctions.OfficialDocument.Remote.HasActiveActionItemExecutionTasks(officialDocument) &&
          officialDocument.ExecutionState != ExecutionState.WithoutExecut)
      {
        officialDocument.ExecutionState = ExecutionState.WithoutExecut;
      }

      if (assignment.SendToCounterparty == true &&
          assignment.Result == DocflowApproval.DocumentProcessingAssignment.Result.Complete)
      {
        var exchangeDocumentInfo = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(officialDocument);
        var isExchangeDocument = exchangeDocumentInfo != null &&
          officialDocument.Tracking.Any(t => !t.ReturnDate.HasValue && t.ExternalLinkId == exchangeDocumentInfo.Id);
        if (!isExchangeDocument)
        {
          Docflow.PublicFunctions.OfficialDocument.IssueDocumentToCounterparty(officialDocument, assignment.Performer.Id,
                                                                               Docflow.OfficialDocumentTracking.Action.Sending,
                                                                               null, null);
        }
      }
    }

  }

  partial class ApprovalBlockHandlers
  {

    public virtual void ApprovalBlockStart()
    {
      var previousReworkAsg = Functions.Module.GetLastCompletedReworkAssignment(_obj);
      Logger.WithLogger(Constants.EntityApprovalAssignment.EntityApprovalAssignmentLoggerPostfix)
        .Debug(string.Format("Start ApprovalBlock. Task id: {0}, block id: {1}, previous rework assignment id: {2}", _obj.Id, _block.Id, previousReworkAsg?.Id.ToString() ?? "null"));
      
      // Добавить исполнителей.
      Functions.Module.AddNewPerformers(_block, previousReworkAsg);
      
      // Удалить исполнителей, которые уже согласовали.
      Functions.Module.RemovePerformersWhoAlreadyApproved(_block, _obj, previousReworkAsg);
      
      // Отправить запрос на подготовку предпросмотра для документов.
      Docflow.PublicFunctions.Module.PrepareAllAttachmentsPreviews(_obj);
    }
    
    public virtual void ApprovalBlockStartAssignment(Sungero.DocflowApproval.IEntityApprovalAssignment assignment)
    {
      foreach (var approver in _block.AddApprovers.Distinct())
      {
        var newApprover = assignment.AddApprovers.AddNew();
        newApprover.Approver = approver;
      }
      
      foreach (var addressee in _block.Addressees.Distinct())
      {
        var newAddressee = assignment.Addressees.AddNew();
        newAddressee.Addressee = addressee;
      }
      
      assignment.DeliveryMethod = _block.DeliveryMethod;
      assignment.ExchangeService = _block.ExchangeService;
      assignment.ReworkPerformer = _block.ReworkPerformer;
      
      // Установить статус согласования документа - На согласовании.
      var officialDocument = OfficialDocuments.As(assignment.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      var approvalState = InternalApprovalState.OnApproval;
      if (officialDocument != null)
        Docflow.PublicFunctions.OfficialDocument.UpdateDocumentApprovalState(officialDocument, approvalState, _obj.Id);
    }
    
    public virtual void ApprovalBlockCompleteAssignment(Sungero.DocflowApproval.IEntityApprovalAssignment assignment)
    {
      if (assignment.Result == DocflowApproval.EntityApprovalAssignment.Result.Forward)
        assignment.Forward(assignment.ForwardTo, ForwardingLocation.Next, assignment.ForwardDeadline);

      Functions.EntityApprovalAssignment.RelateAddedAddendaToPrimaryDocument(assignment);
      Functions.Module.GrantReadAccessRightsToDocuments(assignment.AddendaGroup.ElectronicDocuments.ToList(), assignment.Task.Author);
      
      _block.OutProperties.OutAddressees = Functions.Module.GetBlockOutAddressees(_block.Addressees,
                                                                                  assignment.Addressees?.Select(x => x.Addressee));
      Logger.WithLogger(Constants.EntityApprovalAssignment.EntityApprovalAssignmentLoggerPostfix)
        .Debug(string.Format("Task id: {0}. Complete Assignment (ID: {1}, CompletedBy: {2}). Set Block OutAddressees: [{3}]",
                             _obj.Id,
                             assignment.Id,
                             assignment.CompletedBy.Id,
                             string.Join(", ", _block.OutProperties.OutAddressees.Select(x => x.Id))));
      
      if (assignment.Result == DocflowApproval.EntityApprovalAssignment.Result.ForRework)
        _obj.Blocks.ExecuteAllMonitoringBlocks();
      
      var officialDocument = OfficialDocuments.As(assignment.DocumentGroup.ElectronicDocuments.SingleOrDefault());
      if (officialDocument == null)
        return;
      
      Docflow.PublicFunctions.OfficialDocument.SetDeliveryMethod(officialDocument, assignment.DeliveryMethod);
    }
  }

  partial class ReworkBlockHandlers
  {

    public virtual void ReworkBlockStartAssignment(Sungero.DocflowApproval.IEntityReworkAssignment assignment)
    {
      var task = assignment.Task;
      
      // Выдать права на задачу исполнителю доработки.
      if (!Equals(task.Author, assignment.Performer) &&
          !task.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, assignment.Performer) &&
          !task.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, assignment.Performer))
        task.AccessRights.Grant(assignment.Performer, DefaultAccessRightsTypes.Change);
      
      foreach (var addressee in _block.Addressees.Distinct())
      {
        var newAddressee = assignment.Addressees.AddNew();
        newAddressee.Addressee = addressee;
      }
      
      // Установить статус согласования документа - На доработке.
      var officialDocument = OfficialDocuments.As(assignment.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      var reworkState = InternalApprovalState.OnRework;
      if (officialDocument != null)
        Docflow.PublicFunctions.OfficialDocument.UpdateDocumentApprovalState(officialDocument, reworkState, task.Id);
      
      assignment.DeliveryMethod = _block.DeliveryMethod;
      // Делаем свойство ExchangeService необязательным, т.к. при изменении DeliveryMethod на "Сервис эл. обмена",
      // ExchangeService в задании становится обязательным. Если в блоке схемы указан способ доставки "Сервис эл. обмена",
      // а сам сервис обмена не задан, то создание задания падает из-за незаполненного обязательного свойства (Bug 332205).
      assignment.State.Properties.ExchangeService.IsRequired = false;
      assignment.ExchangeService = _block.ExchangeService;
      
      // Заполнить список согласующих.
      Functions.EntityReworkAssignment.SetApprovers(assignment);
      
      // Установить новый срок согласования.
      if (_block.AllowChangeApprovalDeadline == true)
        assignment.NewDeadline = _block.CurrentApprovalDeadline;
    }
    
    public virtual void ReworkBlockCompleteAssignment(Sungero.DocflowApproval.IEntityReworkAssignment assignment)
    {
      if (assignment.Result == DocflowApproval.EntityReworkAssignment.Result.Forward)
        assignment.Forward(assignment.ForwardTo, ForwardingLocation.Next);
      
      if (_block.AllowChangeApprovers == true)
      {
        _block.OutProperties.NoticeRecipients = assignment.Approvers.Where(x => x.Action == Sungero.DocflowApproval.EntityReworkAssignmentApprovers.Action.SendNotice).Select(x => x.Approver);
        _block.OutProperties.NewApprovers = assignment.Approvers.Where(x => x.BlockId == null &&
                                                                       x.Action == Sungero.DocflowApproval.EntityReworkAssignmentApprovers.Action.SendForApproval).Select(x => x.Approver);
      }
      
      Functions.EntityReworkAssignment.RelateAddedAddendaToPrimaryDocument(assignment);
      Functions.Module.GrantReadAccessRightsToDocuments(assignment.AddendaGroup.ElectronicDocuments.ToList(), assignment.Task.Author);
      
      _block.OutProperties.OutAddressees = Functions.Module.GetBlockOutAddressees(_block.Addressees,
                                                                                  assignment.Addressees?.Select(x => x.Addressee));
      
      Logger.WithLogger(Constants.EntityReworkAssignment.EntityReworkAssignmentLoggerPostfix)
        .Debug(string.Format("Task id {0}. Complete Assignment (ID: {1}, CompletedBy: {2}). Set Block OutAddressees: [{3}], NoticeRecipients: [{4}], NewApprovers [{5}].",
                             _obj.Id,
                             assignment.Id,
                             assignment.CompletedBy.Id,
                             string.Join(", ", _block.OutProperties.OutAddressees.Select(x => x.Id)),
                             string.Join(", ", _block.OutProperties.NoticeRecipients.Select(x => x.Id)),
                             string.Join(", ", _block.OutProperties.NewApprovers.Select(x => x.Id))));
      
      var officialDocument = OfficialDocuments.As(assignment.DocumentGroup.ElectronicDocuments.SingleOrDefault());
      if (officialDocument == null)
        return;
      
      Docflow.PublicFunctions.OfficialDocument.SetDeliveryMethod(officialDocument, assignment.DeliveryMethod);
    }
  }
}