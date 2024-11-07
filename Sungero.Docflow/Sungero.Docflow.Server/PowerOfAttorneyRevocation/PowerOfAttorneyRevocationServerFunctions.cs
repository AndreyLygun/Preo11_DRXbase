using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorneyRevocation;

namespace Sungero.Docflow.Server
{
  partial class PowerOfAttorneyRevocationFunctions
  {
    /// <summary>
    /// Создать тело заявления на отзыв доверенности.
    /// </summary>
    /// <returns>Тело заявления на отзыв доверенности.</returns>
    public virtual Docflow.Structures.Module.IByteArray CreatePowerOfAttorneyRevocationXml()
    {
      var poaRevocation = FormalizeDocumentsParser.PowerOfAttorney.PowerOfAttorneyRevocationBuilder.CreatePowerOfAttorneyRevocation();
      
      var revocationInfo = this.GetRevocationInfo();
      poaRevocation.RevocationInfo = revocationInfo;
      
      var legalEntityRevoker = this.GetLegalEntityRevoker();
      
      var principalDeclarant = FormalizeDocumentsParser.PowerOfAttorney.PowerOfAttorneyRevocationBuilder.CreatePrincipalDeclarant();
      principalDeclarant.Revoker = legalEntityRevoker;
      poaRevocation.Declarant = principalDeclarant;
      
      var xml = Sungero.FormalizeDocumentsParser.Extension.GetPowerOfAttorneyRevocationXml(poaRevocation);
      return Docflow.Structures.Module.ByteArray.Create(xml);
    }
    
    /// <summary>
    /// Получить сведения об отзыве доверенности.
    /// </summary>
    /// <returns>Сведения об отзыве доверенности.</returns>
    public virtual Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoARevocationV1.RevocationInfo GetRevocationInfo()
    {
      var revocationInfo = FormalizeDocumentsParser.PowerOfAttorney.PowerOfAttorneyRevocationBuilder.CreateRevocationInfo();
      
      if (_obj.FormalizedPowerOfAttorney != null && !string.IsNullOrEmpty(_obj.FormalizedPowerOfAttorney.UnifiedRegistrationNumber))
        revocationInfo.Number = _obj.FormalizedPowerOfAttorney.UnifiedRegistrationNumber.ToString();
      revocationInfo.Reason = _obj.RevocationReason;
      
      return revocationInfo;
    }
    
    /// <summary>
    /// Получить заявителя юридическое лицо.
    /// </summary>
    /// <returns>Заявитель юридическое лицо.</returns>
    public virtual Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoARevocationV1.LegalEntityRevoker GetLegalEntityRevoker()
    {
      var legalEntityRevoker = FormalizeDocumentsParser.PowerOfAttorney.PowerOfAttorneyRevocationBuilder.CreateLegalEntityRevoker();
      
      // Заполнение элемента "Сведения о доверителе - российском юридическом лице (РосОргДовер)".
      if (_obj.FormalizedPowerOfAttorney != null && _obj.FormalizedPowerOfAttorney.BusinessUnit != null)
      {
        var businessUnit = _obj.FormalizedPowerOfAttorney.BusinessUnit;
        legalEntityRevoker.Name = businessUnit.LegalName;
        legalEntityRevoker.TIN = businessUnit.TIN;
        legalEntityRevoker.TRRC = businessUnit.TRRC;
        legalEntityRevoker.PSRN = businessUnit.PSRN;
      }
      
      return legalEntityRevoker;
    }
    
    /// <summary>
    /// Проверить сформированную xml заявления на отзыв доверенности.
    /// </summary>
    /// <param name="xml">Тело заявления на отзыв доверенности.</param>
    /// <returns>True - если проверка xml прошла успешно.</returns>
    public virtual bool ValidateGeneratedPowerOfAttorneyRevocationXml(Docflow.Structures.Module.IByteArray xml)
    {
      if (xml == null || xml.Bytes == null || xml.Bytes.Length == 0)
        return false;
      
      var validationResult = FormalizeDocumentsParser.Extension.ValidatePoARevocationXml(xml.Bytes);
      var isValidXml = !validationResult.Any();
      if (!isValidXml)
        Logger.ErrorFormat("ValidateGeneratedPowerOfAttorneyRevocationXml. Validation error. Document id: {0}, FPoA id: {1}, Error message: {2}",
                           _obj.Id, _obj.FormalizedPowerOfAttorney?.Id, string.Join(" ", validationResult));
      
      return isValidXml;
    }
    
    /// <summary>
    /// Заполнить свойства заявления на отзыв эл. доверенности.
    /// </summary>
    /// <param name="fpoa">Заявление на отзыв эл. доверенности.</param>
    /// <param name="reason">Причина отзыва доверенности.</param>
    public virtual void FillRevocationProperties(Sungero.Docflow.IFormalizedPowerOfAttorney fpoa, string reason)
    {
      if (fpoa == null)
        return;
      
      _obj.FormalizedPowerOfAttorney = fpoa;
      _obj.RevocationReason = reason;
      _obj.BusinessUnit = fpoa.BusinessUnit;
      _obj.OurSignatory = fpoa.BusinessUnit.CEO;
      _obj.OurSigningReason = PublicFunctions.OfficialDocument.Remote.GetDefaultSignatureSetting(_obj, _obj.OurSignatory);
      _obj.PreparedBy = Company.Employees.Current;
      _obj.Department = Company.Employees.Current?.Department;
      _obj.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Draft;
    }
    
    /// <summary>
    /// Установить состояние "Действующий" в отзыве МЧД.
    /// </summary>
    /// <returns>True - если состояние успешно обновилось или уже актуально.</returns>
    [Public]
    public virtual bool TrySetLifeCycleStateActive()
    {
      if (_obj.LifeCycleState == Docflow.PowerOfAttorneyRevocation.LifeCycleState.Active)
        return true;
      
      try
      {
        _obj.LifeCycleState = Docflow.PowerOfAttorneyRevocation.LifeCycleState.Active;
        _obj.Save();
        return true;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("TrySetLifeCycleStateActive: Failed to save document. Formalized power of attorney revocation id {0}.", ex, _obj.Id);
        return false;
      }
    }
    
    /// <summary>
    /// Установить статус в реестре ФНС и текст ошибки в отзыве МЧД.
    /// </summary>
    /// <param name="ftsListState">Статус в реестре ФНС.</param>
    /// <param name="rejectReason">Текст ошибки.</param>
    /// <returns>True - если данные успешно обновились или статус уже актуален.</returns>
    [Public]
    public virtual bool TryUpdateFtsListState(Enumeration ftsListState, string rejectReason)
    {
      if (_obj.FtsListState == ftsListState)
        return true;
      try
      {
        _obj.FtsListState = ftsListState;
        _obj.FtsRejectReason = ftsListState == Docflow.PowerOfAttorneyRevocation.FtsListState.Rejected ? rejectReason : null;
        _obj.Save();
        return true;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("TryUpdateFtsListState: Update Fts list state operation completed with error. Failed to save document. Formalized power of attorney revocation id {0}.", ex, _obj.Id);
        return false;
      }
    }
    
    #region История смены состояний
    
    public override System.Collections.Generic.IEnumerable<Sungero.Docflow.Structures.OfficialDocument.HistoryOperation> StatusChangeHistoryOperations(Sungero.Content.DocumentHistoryEventArgs e)
    {
      foreach (var operation in base.StatusChangeHistoryOperations(e))
        yield return operation;
      
      if (_obj.FtsListState != _obj.State.Properties.FtsListState.OriginalValue)
      {
        if (_obj.FtsListState != null)
          yield return Sungero.Docflow.Structures.OfficialDocument.HistoryOperation.Create(
            Constants.PowerOfAttorneyRevocation.Operation.FtsStateChange,
            Sungero.Docflow.PowerOfAttorneyRevocations.Info.Properties.FtsListState.GetLocalizedValue(_obj.FtsListState));
        else
          yield return Sungero.Docflow.Structures.OfficialDocument.HistoryOperation.Create(
            Constants.PowerOfAttorneyRevocation.Operation.FtsStateClear, null);
      }
    }
    
    #endregion
    
    /// <summary>
    /// Заполнить Состояние, статус В реестре ФНС и сообщение об ошибке по коду ошибки.
    /// </summary>
    /// <param name="errorCode">Код ошибки.</param>
    [Public]
    public virtual void HandleRevocationRegistrationError(string errorCode)
    {
      _obj.LifeCycleState = Sungero.Docflow.PowerOfAttorneyRevocation.LifeCycleState.Draft;
      _obj.FtsListState = Sungero.Docflow.PowerOfAttorneyRevocation.FtsListState.Rejected;
      _obj.RegisteredSignatureId = null;

      _obj.FormalizedPowerOfAttorney.FtsListState = Sungero.Docflow.FormalizedPowerOfAttorney.FtsListState.Registered;
      _obj.FormalizedPowerOfAttorney.Save();

      var reasonsDictionary = this.GetErrorCodeAndReasonMapping();
      _obj.FtsRejectReason = reasonsDictionary
        .Where(x => string.Equals(x.Key, errorCode, StringComparison.InvariantCultureIgnoreCase))
        .Select(x => x.Value)
        .FirstOrDefault();
      if (string.IsNullOrEmpty(_obj.FtsRejectReason))
        _obj.FtsRejectReason = Sungero.Docflow.PowerOfAttorneyRevocations.Resources.DefaultErrorMessage;
      
      _obj.Save();
      
      Logger.Error($"HandleRegistrationError: FPoARevocation registration error: {errorCode}. FPoARevocationId: {_obj.Id}.");
    }
    
    /// <summary>
    /// Получить словарь соответствия кодов ошибок и текста ошибок.
    /// </summary>
    /// <returns>Словарь соответствия кодов ошибок и текста ошибок.</returns>
    public virtual System.Collections.Generic.Dictionary<string, string> GetErrorCodeAndReasonMapping()
    {
      var result = new Dictionary<string, string>();
      
      result.Add(Constants.PowerOfAttorneyRevocation.FPoARevocationRegistrationErrors.ExternalSystemIsUnavailableError,
                 PowerOfAttorneyRevocations.Resources.ExternalSystemIsUnavailableErrorMessage);
      result.Add(Constants.PowerOfAttorneyRevocation.FPoARevocationRegistrationErrors.RepeatedRegistrationError,
                 PowerOfAttorneyRevocations.Resources.RepeatedRegistrationErrorMessage);
      result.Add(Constants.PowerOfAttorneyRevocation.FPoARevocationRegistrationErrors.DifferentSignatureError,
                 PowerOfAttorneyRevocations.Resources.DifferentSignatureErrorMessage);
      result.Add(Constants.PowerOfAttorneyRevocation.FPoARevocationRegistrationErrors.ExternalSystemBadResponseError,
                 PowerOfAttorneyRevocations.Resources.ExternalSystemBadResponseErrorMessage);
      result.Add(Constants.PowerOfAttorneyRevocation.FPoARevocationRegistrationErrors.PoaNotFoundError,
                 PowerOfAttorneyRevocations.Resources.PoaNotFoundErrorMessage);
      
      return result;
    }
    
    /// <summary>
    /// Сформировать тело заявления на отзыв эл. доверенности.
    /// </summary>
    /// <returns>True - если генерация завершилась успешно.</returns>
    [Public, Remote]
    public virtual bool GenerateRevocationBody()
    {
      var xml = Functions.PowerOfAttorneyRevocation.CreatePowerOfAttorneyRevocationXml(_obj);
      var isValidXml = Functions.PowerOfAttorneyRevocation.ValidateGeneratedPowerOfAttorneyRevocationXml(_obj, xml);
      
      if (isValidXml)
      {
        if (_obj.HasVersions && _obj.LastVersionApproved == true)
        {
          Logger.DebugFormat("GenerateRevocationBody. Last version has been approved, new version will be added. Document id: {0}", _obj.Id);
          _obj.Versions.AddNew();
        }
        
        Functions.OfficialDocument.WriteBytesToDocumentLastVersionBody(_obj, xml, Constants.FormalizedPowerOfAttorney.XmlExtension);
        return true;
      }
      else
        Logger.ErrorFormat("GenerateRevocationBody. Generate power of attorney revocation body validation error. Document id: {0}", _obj.Id);
      
      return false;
    }
    
    /// <summary>
    /// Переформировать отзыв эл. доверенности.
    /// </summary>
    /// <param name="reason">Причина отзыва.</param>
    /// <returns>True - если отзыв был успешно переформирован.</returns>
    [Public, Remote]
    public virtual bool ReCreateRevocation(string reason)
    {
      _obj.RevocationReason = reason;
      _obj.FtsListState = null;
      _obj.FtsRejectReason = string.Empty;
      
      if (this.GenerateRevocationBody())
        this.GenerateRevocationPdf();
      else
        return false;
      
      return true;
    }
    
    /// <summary>
    /// Отправить отзыв эл. доверенности в ФНС.
    /// </summary>
    /// <returns>Результат отправки отзыва эл. доверенности.</returns>
    [Public, Remote]
    public virtual PowerOfAttorneyCore.Structures.Module.IResponseResult RegisterRevocationWithService()
    {
      return this.RegisterRevocationWithService(null);
    }
    
    /// <summary>
    /// Отправить отзыв эл. доверенности в ФНС.
    /// </summary>
    /// <param name="taskId">Ид задачи, если регистрация происходит в контексте задачи.</param>
    /// <returns>Результат отправки отзыва эл. доверенности.</returns>
    [Public, Remote]
    public virtual PowerOfAttorneyCore.Structures.Module.IResponseResult RegisterRevocationWithService(long? taskId)
    {
      var bodyBytes = Functions.Module.GetBinaryData(_obj.LastVersion.Body);
      var signature = Functions.OfficialDocument.GetSignatureFromOurSignatory(_obj, _obj.LastVersion.Id);
      var signatureBytes = signature?.GetDataSignature();
      var sendingResult = PowerOfAttorneyCore.PublicFunctions.Module.SendPowerOfAttorneyRevocation(_obj.BusinessUnit,
                                                                                                   bodyBytes,
                                                                                                   signatureBytes);
      if (!string.IsNullOrWhiteSpace(sendingResult.ErrorCode) || !string.IsNullOrWhiteSpace(sendingResult.ErrorType))
      {
        this.HandleRevocationRegistrationError(sendingResult.ErrorCode);
        return sendingResult;
      }
      
      if (_obj.FormalizedPowerOfAttorney.AccessRights.CanUpdate() && !Locks.GetLockInfo(_obj.FormalizedPowerOfAttorney).IsLocked)
      {
        _obj.FormalizedPowerOfAttorney.FtsListState = Docflow.FormalizedPowerOfAttorney.FtsListState.OnRevoke;
        _obj.FormalizedPowerOfAttorney.Save();
      }
      else
      {
        Logger.DebugFormat("RegisterRevocationWithService: Failed to change formalized power of attorney life cycle state. Insufficient access rights or document is locked. Formalized power of attorney id {0}. Power of attorney revocation id {1}",
                           _obj.FormalizedPowerOfAttorney.Id,
                           _obj.Id);
      }
      
      // Успешная отправка на регистрацию.
      _obj.FtsListState = Docflow.PowerOfAttorneyRevocation.FtsListState.OnRegistration;
      _obj.RegisteredSignatureId = signature.Id;
      _obj.Save();
      
      var queueItem = PowerOfAttorneyQueueItems.Create();
      queueItem.OperationType = Docflow.PowerOfAttorneyQueueItem.OperationType.Revocation;
      queueItem.DocumentId = _obj.Id;
      queueItem.OperationId = sendingResult.OperationId;
      queueItem.RevocationDate = signature.SigningDate;
      queueItem.TaskId = taskId;
      queueItem.Save();
      
      sendingResult.QueueItem = queueItem;
      
      var documentHyperlink = Hyperlinks.Get(_obj);
      var startMessage = PowerOfAttorneyRevocations.Resources.RevocationSentSuccessfully;
      var completeMessage = PowerOfAttorneyRevocations.Resources.SetFPoARevocationStateCompletionMessageFormat(documentHyperlink, Environment.NewLine);
      var errorMessage = PowerOfAttorneyRevocations.Resources.SetFPoARevocationStateErrorMessageFormat(documentHyperlink, Environment.NewLine);
      
      // Старт асинхронного обработчика, который получит статус регистрации отзыва доверенности и установит актуальные состояния доверенности и отзыва.
      var asyncHandler = AsyncHandlers.SetFPoARevocationState.Create();
      asyncHandler.QueueItemId = queueItem.Id;
      asyncHandler.ExecuteAsync(startMessage, completeMessage, errorMessage, Users.Current);
      return sendingResult;
    }
    
    /// <summary>
    /// Проверить отзыв эл. доверенности перед отправкой запроса к сервису.
    /// </summary>
    /// <returns>Сообщение об ошибке или пустая строка, если ошибок нет.</returns>
    [Public, Remote]
    public virtual string ValidateRevocationBeforeSending()
    {
      var validationError = this.ValidateBodyAndSignature();

      if (!string.IsNullOrEmpty(validationError))
        return validationError;
      
      if (!Sungero.PowerOfAttorneyCore.PublicFunctions.Module.HasPowerOfAttorneyServiceConnection(_obj.BusinessUnit))
        return Sungero.PowerOfAttorneyCore.Resources.ServiceConnectionNotConfigured;
      
      // Валидация xml по схеме.
      try
      {
        var body = Docflow.PublicFunctions.Module.GetBinaryData(_obj.LastVersion.Body);
        var xml = Docflow.Structures.Module.ByteArray.Create(body);
        this.ValidateGeneratedPowerOfAttorneyRevocationXml(xml);
      }
      catch
      {
        return FormalizedPowerOfAttorneys.Resources.XmlLoadFailed;
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Сгенерировать PDF из тела отзыва.
    /// </summary>
    [Public, Remote]
    public virtual void GenerateRevocationPdf()
    {
      var useObsoletePdfConversion = PublicFunctions.Module.Remote.UseObsoletePdfConversion();
      if (useObsoletePdfConversion)
        this.ConvertToPdfAndAddSignatureMark(_obj.LastVersion.Id);
      else
        this.ConvertToPdfWithMarks(_obj.LastVersion.Id);
      Functions.OfficialDocument.LogPdfConversion(_obj, _obj.LastVersion.Id, "Signature mark. Added interactively");
    }
    
    public override bool CanConvertToPdfInteractively()
    {
      return true;
    }
    
    /// <summary>
    /// Преобразовать документ в PDF и поставить отметку об ЭП, если есть утверждающая подпись.
    /// </summary>
    /// <param name="versionId">ИД версии документа.</param>
    /// <returns>Результат преобразования в PDF.</returns>
    [Remote]
    public override Sungero.Docflow.Structures.OfficialDocument.IConversionToPdfResult ConvertToPdfAndAddSignatureMark(long versionId)
    {
      var signatureMark = string.Empty;
      
      var signature = Functions.OfficialDocument.GetSignatureForMark(_obj, versionId);
      if (signature != null)
        signatureMark = Functions.Module.GetSignatureMarkAsHtml(_obj, signature);
      
      return this.GeneratePublicBodyWithSignatureMark(versionId, signatureMark);
    }
    
    /// <summary>
    /// Получить тело и расширение версии для преобразования в PDF с отметкой об ЭП.
    /// </summary>
    /// <param name="version">Версия для генерации.</param>
    /// <param name="isSignatureMark">Признак отметки об ЭП. True - отметка об ЭП, False - отметка о поступлении.</param>
    /// <returns>Тело версии документа и расширение.</returns>
    /// <remarks>Для преобразования в PDF отзыва эл. доверенности необходимо сначала получить ее в виде html.</remarks>
    [Public]
    public override Structures.OfficialDocument.IVersionBody GetBodyToConvertToPdf(Sungero.Content.IElectronicDocumentVersions version, bool isSignatureMark)
    {
      return this.GetBodyToConvertToPdfWithMarks(version);
    }
    
    /// <summary>
    /// Получить тело и расширение версии для преобразования в PDF с отметками.
    /// </summary>
    /// <param name="version">Версия для преобразования.</param>
    /// <returns>Тело версии документа и расширение.</returns>
    /// <remarks>Для преобразования в PDF отзыва эл. доверенности необходимо сначала получить ее в виде html.</remarks>
    public override Sungero.Docflow.Structures.OfficialDocument.IVersionBody GetBodyToConvertToPdfWithMarks(Sungero.Content.IElectronicDocumentVersions version)
    {
      var result = Structures.OfficialDocument.VersionBody.Create();
      if (version == null)
        return result;
      
      var html = this.GetPoARevocationAsHtml(version);
      if (string.IsNullOrWhiteSpace(html))
        return result;

      result.Body = System.Text.Encoding.UTF8.GetBytes(html);
      result.Extension = Constants.FormalizedPowerOfAttorney.HtmlExtension;
      return result;
    }
    
    /// <summary>
    /// Получить отзыв эл. доверенности в виде html.
    /// </summary>
    /// <param name="version">Версия, на основании которой формируется html.</param>
    /// <returns>Отзыв эл. доверенности в виде html.</returns>
    [Public]
    public virtual string GetPoARevocationAsHtml(Sungero.Content.IElectronicDocumentVersions version)
    {
      if (version == null)
        return string.Empty;
      
      // Получить модель отзыва эл. доверенности из xml.
      using (var body = new System.IO.MemoryStream())
      {
        // Выключить error-логирование при доступе к зашифрованным бинарным данным.
        AccessRights.SuppressSecurityEvents(() => version.Body.Read().CopyTo(body));
        var revocation = FormalizeDocumentsParser.Extension.GetPowerOfAttorneyRevocation(body.ToArray());
        return revocation != null
          ? FormalizeDocumentsParser.PowerOfAttorney.RevocationHtmlProducer.ProduceRevocationHtml(revocation)
          : string.Empty;
      }
    }
    
    /// <summary>
    /// Определить, поддерживается ли преобразование в PDF для переданного расширения.
    /// </summary>
    /// <param name="extension">Расширение.</param>
    /// <returns>True, если поддерживается, иначе False.</returns>
    /// <remarks>Заявления на отзыв МЧД имеют расширение XML, которое всегда поддерживается.</remarks>
    [Public]
    public override bool CheckPdfConvertibilityByExtension(string extension)
    {
      return true;
    }
    
    /// <summary>
    /// Заполнить подписывающего в карточке документа.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    [Remote]
    public override void SetDocumentSignatory(Company.IEmployee employee)
    {
      if (_obj.OurSignatory != null)
        return;
      base.SetDocumentSignatory(employee);
    }
    
    /// <summary>
    /// Заполнить основание в карточке документа.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <param name="e">Аргументы события подписания.</param>
    /// <param name="changedSignatory">Признак смены подписывающего.</param>
    public override void SetOurSigningReason(Company.IEmployee employee, Sungero.Domain.BeforeSigningEventArgs e, bool changedSignatory)
    {
      if (!Equals(_obj.OurSignatory, employee))
        return;
      base.SetOurSigningReason(employee, e, changedSignatory);
    }
  }
}
