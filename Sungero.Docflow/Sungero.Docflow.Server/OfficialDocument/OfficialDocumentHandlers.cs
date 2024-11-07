using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OfficialDocument;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using HistoryOperation = Sungero.Docflow.Structures.OfficialDocument.HistoryOperation;

namespace Sungero.Docflow
{
  partial class OfficialDocumentTopicPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> TopicFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = query.Where(x => x.Parent == null);
      return query;
    }
  }

  partial class OfficialDocumentSubtopicPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> SubtopicFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = query.Where(x => Equals(x.Parent, _obj.Topic));
      return query;
    }
  }

  partial class OfficialDocumentOurSigningReasonPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> OurSigningReasonFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var now = Calendar.Now;
      var availableSettings = Functions.OfficialDocument.GetSignatureSettingsWithCertificateByEmployee(_obj, _obj.OurSignatory);
      query = query.Where(x => availableSettings.Contains(x));
      return query;
    }
  }

  partial class OfficialDocumentCaseFilePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CaseFileFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return PublicFunctions.Module.CaseFileFiltering(_obj, query).Cast<T>();
    }
  }

  partial class OfficialDocumentDocumentKindSearchPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentKindSearchDialogFiltering(IQueryable<T> query, Sungero.Domain.PropertySearchDialogFilteringEventArgs e)
    {
      if (e.EntityType != null)
        query = query.Where(k => k.DocumentType.DocumentTypeGuid == e.EntityType);

      return query;
    }
  }

  partial class OfficialDocumentConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      var sourceOfficialDocument = OfficialDocuments.As(_source);
      
      // Не копировать порядковый номер.
      e.Without(_info.Properties.Index);
      
      // Не копируем состояние ЖЦ для Вх. документа эл. обмена.
      if (ExchangeDocuments.Is(_source) &&
          sourceOfficialDocument.LifeCycleState != OfficialDocument.LifeCycleState.Obsolete)
        e.Without(_info.Properties.LifeCycleState);
      
      // Добавить параметр того, что документ меняет тип.
      var paramName = string.Format("doc{0}_ConvertingFrom", _source.Id);
      e.Params.AddOrUpdate(paramName, true);
      
      var sourceEntityGUID = _source.GetEntityMetadata().GetOriginal().NameGuid.ToString();
      var recognitionInfo = Commons.EntityRecognitionInfos.GetAll()
        .Where(r => r.EntityId == _source.Id && r.EntityType == sourceEntityGUID)
        .OrderByDescending(r => r.Id)
        .FirstOrDefault();
      
      if (recognitionInfo != null && sourceOfficialDocument.VerificationState == OfficialDocument.VerificationState.InProcess)
        Sungero.Commons.PublicFunctions.EntityRecognitionInfo.Remote.Clone(recognitionInfo, e.Entity);
      
      var businessUnit = Exchange.PublicFunctions.ExchangeDocumentInfo.GetDocumentBusinessUnit(_source, _source.LastVersion);
      if (businessUnit != null)
        e.Map(_info.Properties.BusinessUnit, businessUnit);
    }
  }

  partial class OfficialDocumentProjectPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ProjectFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(x => x.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed);
    }
  }

  partial class OfficialDocumentOurSignatoryPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> OurSignatoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      return (IQueryable<T>)Functions.OfficialDocument.FilterSignatories(_obj, query);
    }
  }

  partial class OfficialDocumentDocumentGroupPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentGroupFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var documentKind = _obj.DocumentKind;
      if (documentKind == null)
        return query;
      
      var availableGroups = Functions.DocumentGroupBase.GetAvailableDocumentGroup(documentKind).ToList();
      return query.Where(a => availableGroups.Contains(a));

    }
  }

  partial class OfficialDocumentDocumentRegisterPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentRegisterFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.DocumentKind != null)
      {
        var availableDocumentRegistersIds = Functions.OfficialDocument.GetDocumentRegistersByDocument(_obj);
        query = query.Where(l => availableDocumentRegistersIds.Contains(l.Id));
      }
      return query;
    }
  }

  partial class OfficialDocumentDocumentKindPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.RegistrationState == RegistrationState.Registered)
        query = query.Where(k => k.NumberingType != Docflow.DocumentKind.NumberingType.NotNumerable);
      
      var availableDocumentKinds = Functions.DocumentKind.GetAvailableDocumentKinds(_obj);
      query = query.Where(k => availableDocumentKinds.Contains(k));
      
      query = PublicFunctions.Module.FilterDocumentKindsByAccessRights(query).Cast<T>();
      return query;
    }
  }

  partial class OfficialDocumentCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      this.CopyRegistrationGroup(e);
      this.CopyFileGroup(e);
      this.CopyLifeCycleGroup(e);
      this.CopySigningGroup(e);
      this.CopyMainGroup(e);
      
      e.Params.AddOrUpdate(Constants.OfficialDocument.GrantAccessRightsToDocumentAsync, true);
    }
    
    /// <summary>
    /// Скопировать поля группы "Регистрация".
    /// </summary>
    /// <param name="e">Аргументы события "Копирование".</param>
    public virtual void CopyRegistrationGroup(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.RegistrationNumber);
      e.Without(_info.Properties.RegistrationDate);
      e.Without(_info.Properties.DocumentRegister);
      e.Without(_info.Properties.DeliveryMethod);
      e.Without(_info.Properties.Tracking);
    }
    
    /// <summary>
    /// Скопировать поля группы "Хранение".
    /// </summary>
    /// <param name="e">Аргументы события "Копирование".</param>
    public virtual void CopyFileGroup(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.CaseFile);
      e.Without(_info.Properties.PlacedToCaseFileDate);
      e.Without(_info.Properties.StoredIn);
      e.Without(_info.Properties.PaperCount);
      e.Without(_info.Properties.AddendaPaperCount);
    }
    
    /// <summary>
    /// Скопировать поля группы "Жизненный цикл".
    /// </summary>
    /// <param name="e">Аргументы события "Копирование".</param>
    public virtual void CopyLifeCycleGroup(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.LifeCycleState);
      e.Without(_info.Properties.RegistrationState);
      e.Without(_info.Properties.VerificationState);
      e.Without(_info.Properties.InternalApprovalState);
      e.Without(_info.Properties.ExternalApprovalState);
      e.Without(_info.Properties.ExchangeState);
      e.Without(_info.Properties.ExecutionState);
      e.Without(_info.Properties.ControlExecutionState);
      e.Without(_info.Properties.LocationState);
    }
    
    /// <summary>
    /// Скопировать поля групп "Контрагент" и "Наша организация".
    /// </summary>
    /// <param name="e">Аргументы события "Копирование".</param>
    public virtual void CopySigningGroup(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.OurSignatory);
      e.Without(_info.Properties.OurSigningReason);
    }
    
    /// <summary>
    /// Скопировать поля группы "Основное".
    /// </summary>
    /// <param name="e">Аргументы события "Копирование".</param>
    public virtual void CopyMainGroup(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.Assignee);
    }
  }

  partial class OfficialDocumentServerHandlers
  {

    public override void AfterDelete(Sungero.Domain.AfterDeleteEventArgs e)
    {
      // Удалить результаты распознавания документа после его удаления.
      var asyncDeleteRecognitionInfoHandler = SmartProcessing.AsyncHandlers.DeleteEntityRecognitionInfo.Create();
      asyncDeleteRecognitionInfoHandler.EntityId = _obj.Id;
      asyncDeleteRecognitionInfoHandler.ExecuteAsync();
    }

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      // Сбрасывать значение параметра, так как после сохранения рег. данные должны быть валидны.
      bool paramValue;
      if (e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.NeedValidateRegisterFormat, out paramValue) && paramValue)
        e.Params.Remove(Sungero.Docflow.Constants.OfficialDocument.NeedValidateRegisterFormat);

      Commons.PublicFunctions.Module.DeleteEntityParams(_obj, Docflow.Constants.OfficialDocument.DeletedMarkIdsParamName);

      var isTransferring = _obj.State.IsBinaryDataTransferring;
      if (e.Params.Contains(Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToDocumentAsync) && !isTransferring)
      {
        if (e.Params.Contains(Constants.OfficialDocument.GrantAccessRightsToProjectDocument))
        {
          Sungero.Projects.Jobs.GrantAccessRightsToProjectDocuments.Enqueue();
          e.Params.Remove(Constants.OfficialDocument.GrantAccessRightsToProjectDocument);
        }
        PublicFunctions.Module.CreateGrantAccessRightsToDocumentAsyncHandler(_obj.Id, new List<long>(), true);
        e.Params.Remove(Constants.OfficialDocument.GrantAccessRightsToDocumentAsync);
      }
      
      if (e.Params.Contains(Constants.OfficialDocument.ExecuteAllMonitoringBlocksForReturnTasks))
      {
        var returnTasksInProcess = CheckReturnTasks.GetAll(t => t.DocumentToReturn.Equals(_obj) && t.Status == Workflow.Task.Status.InProcess);
        foreach (var returnTask in returnTasksInProcess)
          returnTask.Blocks.ExecuteAllMonitoringBlocks();
        e.Params.Remove(Constants.OfficialDocument.ExecuteAllMonitoringBlocksForReturnTasks);
      }
      
      // Если документ уже возвращен, выполнить все активные блоки мониторинга в задачах возврата, связанных с документом.
      if (e.Params.Contains(Constants.OfficialDocument.ExecuteAllMonitoringBlocksForCounterpartyReturnTasks))
      {
        var returnTasksInProcess = _obj.Tracking
          .Where(x => x.Action == OfficialDocumentTracking.Action.Endorsement &&
                 x.ReturnTask != null &&
                 x.ReturnTask.Status == Sungero.Workflow.Task.Status.InProcess)
          .Select(x => x.ReturnTask);
        
        foreach (var returnTask in returnTasksInProcess)
          returnTask.Blocks.ExecuteAllMonitoringBlocks();
        
        e.Params.Remove(Constants.OfficialDocument.ExecuteAllMonitoringBlocksForCounterpartyReturnTasks);
      }
    }

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      var ids = Functions.OfficialDocument.GetTaskIdsWhereDocumentInRequredGroup(_obj);
      if (ids.Any())
        throw AppliedCodeException.Create(OfficialDocuments.Resources.DocumentUseInTasksFormat(string.Join(", ", ids.ToArray())));
      
      var canDelete = Functions.OfficialDocument.CheckDeleteEntityAccessRights(_obj);
      if (!_obj.AccessRights.CanDelete() || !canDelete)
        throw AppliedCodeException.Create(OfficialDocuments.Resources.NoRightsToUpdateOrDelete);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      // Заполнить статус регистрации по умолчанию.
      _obj.RegistrationState = RegistrationState.NotRegistered;
      _obj.State.Properties.RegistrationState.IsEnabled = false;
      
      Functions.OfficialDocument.RefreshDocumentForm(_obj);
      Functions.OfficialDocument.FillOrganizationStructure(_obj);
      
      // Переопределить вид документа при смене типа.
      var paramName = string.Format("doc{0}_ConvertingFrom", _obj.Id);
      if (e.Params.Contains(paramName))
      {
        // При смене вида происходит удаление параметра.
        _obj.DocumentKind = Functions.OfficialDocument.GetDefaultDocumentKind(_obj);
      }
      
      // Заполнить вид документа.
      if (_obj.DocumentKind == null)
      {
        // Заполнить вид документа, если подобрался вид по умолчанию.
        var defaultDocumentKind = Functions.OfficialDocument.GetDefaultDocumentKind(_obj);
        if (defaultDocumentKind != null)
          _obj.DocumentKind = defaultDocumentKind;
      }
      else if (_obj.State.IsCopied &&
               _obj.DocumentKind.Status == Sungero.Docflow.DocumentKind.Status.Closed)
      {
        // Если скопировали документ с закрытым видом - очищаем поле.
        _obj.DocumentKind = null;
      }

      // Заполнить статус жизненного цикла в зависимости от вида документа.
      Functions.OfficialDocument.SetLifeCycleState(_obj);
      if (_obj.LifeCycleState == null)
        _obj.LifeCycleState = OfficialDocument.LifeCycleState.Draft;
    }

    public override void BeforeSigning(Sungero.Domain.BeforeSigningEventArgs e)
    {
      var canSignLockedDocument = Functions.OfficialDocument.CanSignLockedDocument(_obj);
      var lockInfo = Locks.GetLockInfo(_obj);
      if (lockInfo == null || !lockInfo.IsLockedByOther || !canSignLockedDocument)
      {
        if (e.Signature.SignatureType == SignatureType.Approval && e.Signature.Signatory != null)
        {
          // Заполнить статус согласования "Подписан".
          Functions.OfficialDocument.SetInternalApprovalStateToSigned(_obj);
          
          var changedSignatory = !Equals(_obj.OurSignatory, Company.Employees.Current);
          
          // Заполнить подписывающего в карточке документа.
          Functions.OfficialDocument.SetDocumentSignatory(_obj, Company.Employees.Current);

          // Заполнить основание в карточке документа.
          Functions.OfficialDocument.SetOurSigningReason(_obj, Company.Employees.Current, e, changedSignatory);
          
          // Заполнить Единый рег. № из эл. доверенности в подпись.
          Functions.OfficialDocument.SetUnifiedRegistrationNumber(_obj, Company.Employees.Current, e.Signature, e.Certificate);
        }
      }
      
      // Если подписание выполняется в рамках агента - генерировать заглушку не надо.
      bool jobRan;
      if (e.Params.TryGetValue(ExchangeCore.PublicConstants.BoxBase.JobRunned, out jobRan) && jobRan)
        return;
      
      var versionId = (e.Signature as IInternalSignature).SignedEntityProperties
        .Select(p => p.ChildEntityId).Single();
      var info = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetExDocumentInfoFromVersion(_obj, versionId.Value);
      
      var version = _obj.Versions.Single(v => v.Id == versionId);
      if (e.Signature.SignatureType == SignatureType.Approval &&
          info != null &&
          !Signatures.Get(version, q => q.Where(s => s.SignatureType == SignatureType.Approval && s.Id != info.SenderSignId).Take(1)).Any())
      {
        Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(_obj, version.Id);
        Exchange.PublicFunctions.Module.EnqueueXmlToPdfBodyConverter(_obj, version.Id, _obj.ExchangeState);
      }
    }

    public override void BeforeSaveHistory(Sungero.Content.DocumentHistoryEventArgs e)
    {
      var isUpdateAction = e.Action == Sungero.CoreEntities.History.Action.Update;
      var isVersionCreateAction = e.Action == Sungero.CoreEntities.History.Action.Update &&
        e.Operation == new Enumeration(Constants.OfficialDocument.Operation.CreateVersion);
      var isCreateAction = e.Action == Sungero.CoreEntities.History.Action.Create;
      var isChangeTypeAction = e.Action == Sungero.CoreEntities.History.Action.ChangeType;
      var properties = _obj.State.Properties;
      
      var documentParams = ((Domain.Shared.IExtendedEntity)_obj).Params;
      if (isVersionCreateAction && documentParams.ContainsKey(Docflow.PublicConstants.OfficialDocument.FindByBarcodeParamName))
        e.Comment = Sungero.Docflow.OfficialDocuments.Resources.VersionCreatedByCaptureService;
      
      var isAddRegistrationStampAction = isVersionCreateAction && documentParams.ContainsKey(Docflow.PublicConstants.OfficialDocument.AddHistoryCommentAboutRegistrationStamp);
      if (isAddRegistrationStampAction)
        e.Comment = Sungero.Docflow.OfficialDocuments.Resources.VersionWithRegistrationStamp;
      
      if (isCreateAction && documentParams.ContainsKey(Docflow.PublicConstants.OfficialDocument.AddHistoryCommentRepackingAddNewDocument))
        e.Comment = Sungero.Docflow.OfficialDocuments.Resources.DocumentCreateFromRepacking;
      
      // Изменять историю для изменения, создания и смены типа документа. Историю для создания версии не изменять,
      // кроме случая, когда версия создана с отметкой о регистрации.
      if ((!isUpdateAction || isVersionCreateAction && !isAddRegistrationStampAction) && !isCreateAction && !isChangeTypeAction)
        return;
      
      var historyRecordOverwritten = false;
      
      #region Очистка рег. данных при смене типа
      
      // Определить, что произошла смена типа документа.
      var documentKindOriginalValue = _obj.State.Properties.DocumentKind.OriginalValue;
      var documentTypeChange = documentKindOriginalValue != null &&
        !documentKindOriginalValue.DocumentType.Equals(_obj.DocumentKind.DocumentType);
      
      // При смене типа для нумеруемых документов автоматически очищаются рег.данные.
      // Записать в историю информацию об очистке полей регистрации.
      var changeTypeUnregistration = isChangeTypeAction && !string.IsNullOrWhiteSpace(properties.RegistrationNumber.OriginalValue);
      if (changeTypeUnregistration)
      {
        using (TenantInfo.Culture.SwitchTo())
        {
          /*
           * Только для нумеруемых. У ненумеруемого нечего очищать.
           * Для регистрируемого нельзя сменить тип, пока он зарегистрирован.
           */
          var numberingTypeOriginalValue = documentKindOriginalValue.NumberingType;
          var isSubstitute = !_obj.AccessRights.GetSubstitutedWhoCanRegister().Any(u => u.Id == Users.Current.Id);
          if (isSubstitute && _obj.RegistrationState != RegistrationState.Registered)
            isSubstitute = Docflow.PublicFunctions.RegistrationSetting.GetSettingByDocument(_obj, Docflow.RegistrationSetting.SettingType.Reservation) == null;
          
          if (numberingTypeOriginalValue.Equals(DocumentKind.NumberingType.Numerable))
            e.Write(new Enumeration(Constants.OfficialDocument.Operation.Unnumeration), null, OfficialDocuments.Resources.ChangeTypeUnnumerationComment, isSubstitute);
        }
      }
      
      #endregion
      
      #region История регистрации
      
      var registrationState = _obj.RegistrationState;
      var registrationStateIsChanged = registrationState != properties.RegistrationState.OriginalValue;
      var registrationDataChanged =
        (registrationStateIsChanged && (!isCreateAction || registrationState != RegistrationState.NotRegistered)) ||
        _obj.RegistrationNumber != properties.RegistrationNumber.OriginalValue ||
        _obj.RegistrationDate != properties.RegistrationDate.OriginalValue ||
        _obj.DocumentRegister != properties.DocumentRegister.OriginalValue;
      
      if (registrationDataChanged)
      {
        using (TenantInfo.Culture.SwitchTo())
        {
          var isDocumentNotifiable = _obj.DocumentKind != null && _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable;
          var isDocumentReserved = registrationState == RegistrationState.Reserved;
          var wasDocumentReserved = properties.RegistrationState.OriginalValue == RegistrationState.Reserved;
          
          var unregistrationEventName = wasDocumentReserved ? Constants.OfficialDocument.Operation.Unreservation :
            (isDocumentNotifiable ? Constants.OfficialDocument.Operation.Unregistration : Constants.OfficialDocument.Operation.Unnumeration);
          
          var operation = e.Operation;
          var operationDetailed = e.OperationDetailed;
          var separator = "|";
          var registrationDate = _obj.RegistrationDate.HasValue ? _obj.RegistrationDate.Value.ToString("d") : string.Empty;
          var comment = !string.IsNullOrWhiteSpace(_obj.RegistrationNumber) ?
            string.Join(separator, _obj.RegistrationNumber, registrationDate, _obj.DocumentRegister) :
            e.Comment;
          
          // Изменение статуса регистрации. При смене типа у пронумерованного документа также идет изменение статуса.
          if (registrationStateIsChanged || changeTypeUnregistration)
          {
            // Регистрация.
            if (registrationState == RegistrationState.Registered)
            {
              if (isDocumentNotifiable)
              {
                operation = new Enumeration(Functions.OfficialDocument.GetRegistrationOperation());
                operationDetailed = operation;
              }
              else
              {
                operation = new Enumeration(Constants.OfficialDocument.Operation.Numeration);
                operationDetailed = operation;
              }
            }
            else if (isDocumentReserved)
            {
              // Резервирование.
              operation = new Enumeration(Constants.OfficialDocument.Operation.Reservation);
              operationDetailed = operation;
            }
            else if (registrationState == RegistrationState.NotRegistered && isUpdateAction && !changeTypeUnregistration)
            {
              // Отмена регистрации.
              operation = new Enumeration(unregistrationEventName);
            }
          }
          else
          {
            // Изменение только регистрационных данных.
            operation = new Enumeration(isDocumentNotifiable ? Constants.OfficialDocument.Operation.ChangeRegistration : Constants.OfficialDocument.Operation.ChangeNumeration);
            operationDetailed = operation;
          }
          
          var isSubstitute = !_obj.AccessRights.GetSubstitutedWhoCanRegister().Any(u => u.Id == Users.Current.Id);
          if (isSubstitute && registrationState != RegistrationState.Registered)
            isSubstitute = Docflow.PublicFunctions.RegistrationSetting.GetSettingByDocument(_obj, Docflow.RegistrationSetting.SettingType.Reservation) == null;
          
          // Добавить отдельную запись истории, если регистрация/нумерация происходят при создании или смене типа.
          if (isCreateAction || isChangeTypeAction && registrationState != RegistrationState.NotRegistered)
            e.Write(operation, operationDetailed, comment, isSubstitute);
          else
          {
            e.Operation = operation;
            e.OperationDetailed = operationDetailed;
            e.Comment = comment;
            e.IsSubstitute = isSubstitute;
            historyRecordOverwritten = true;
          }
        }
      }
      
      #endregion
      
      #region История смены состояний
      
      /*
       * Для любой смены состояния:
       * - всегда писать отдельной строкой, если это смена типа документа;
       * - дописывать в историю, если это не смена типа.
       */
      
      var operations = Functions.OfficialDocument.StatusChangeHistoryOperations(_obj, e);
      Functions.OfficialDocument.WriteStatusChangeHistory(_obj, e, operations, historyRecordOverwritten);
      
      #endregion
      
      var isConvertToPdfAction = e.Operation == Content.DocumentHistory.Operation.UpdateVerBody &&
        documentParams.ContainsKey(Docflow.PublicConstants.OfficialDocument.AddHistoryCommentAboutPDFConvert);
      if (isConvertToPdfAction)
      {
        var comment = Sungero.Docflow.OfficialDocuments.Resources.ConvertToPdfHistoryComment;
        if (string.IsNullOrEmpty(e.Comment))
        {
          e.Comment = comment;
        }
        else
        {
          var operation = new Enumeration(Constants.OfficialDocument.Operation.ContentChange);
          var version = e.VersionNumber;
          e.Write(operation, null, comment, version);
        }
      }
    }
    
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Пропуск выполнения обработчика в случае отсутствия прав на изменение, например при выдаче прав на чтение пользователем, который сам имеет права на чтение.
      if (!_obj.AccessRights.CanUpdate())
        return;
      Functions.OfficialDocument.GrantRegistrationGroupRights(_obj, e);
      Functions.OfficialDocument.ValidateOffDocBeforeSave(_obj, e);
      Functions.OfficialDocument.SyncStorage(_obj);
      
      // Не вызывать сохранение, если изменилось только тело документа.
      if (Functions.OfficialDocument.IsOnlyVersionChanged(_obj))
        return;

      Functions.OfficialDocument.RegisterAndNumerate(_obj, e);
      Functions.OfficialDocument.ReturningTasksControl(_obj, e);
      Functions.OfficialDocument.UpdateOffDocFieldsBeforeSave(_obj, e);
    }

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      // Пропуск выполнения обработчика в случае отсутствия прав на изменение, например при выдаче прав на чтение пользователем, который сам имеет права на чтение.
      if (!_obj.AccessRights.CanUpdate())
        return;
      
      // Заполнить регистрационный номер.
      if (_obj.RegistrationDate != null && _obj.DocumentRegister != null)
      {
        var useObsoleteRegNumberGeneration = Functions.Module.GetDocflowParamsBoolValue(PublicConstants.Module.UseObsoleteRegNumberGenerationParamName);
        var formatItems = Functions.DocumentRegister.GetNumberFormatItemsValues(_obj);
        var leadDocumentId = Functions.OfficialDocument.GetLeadDocumentId(_obj);
        var departmentId = _obj.Department != null ? _obj.Department.Id : 0;
        var businessUnitId = _obj.BusinessUnit != null ? _obj.BusinessUnit.Id : 0;
        
        if (string.IsNullOrEmpty(_obj.RegistrationNumber))
        {
          var registrationIndex = 0;
          do
          {
            // Для доп.соглашений и актов номер устанавливать в разрезе ведущего документа.
            registrationIndex = Functions.DocumentRegister.GetNextRegistrationNumber(_obj.DocumentRegister, _obj.RegistrationDate.Value, leadDocumentId, departmentId, businessUnitId);
            var registrationIndexWithLeadZero = registrationIndex.ToString();
            if (registrationIndexWithLeadZero.Length < _obj.DocumentRegister.NumberOfDigitsInNumber)
              registrationIndexWithLeadZero = string.Concat(Enumerable.Repeat("0", (_obj.DocumentRegister.NumberOfDigitsInNumber - registrationIndexWithLeadZero.Length) ?? 0)) +
                registrationIndexWithLeadZero;

            string registrationNumberPrefixValue;
            e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.RegistrationNumberPrefix, out registrationNumberPrefixValue);
            string registrationNumberPostfixValue;
            e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.RegistrationNumberPostfix, out registrationNumberPostfixValue);
            _obj.RegistrationNumber = registrationNumberPrefixValue + registrationIndexWithLeadZero +
              registrationNumberPostfixValue;
          } while (!(useObsoleteRegNumberGeneration
                     ? Functions.DocumentRegister.IsRegistrationNumberUnique(_obj.DocumentRegister, _obj, _obj.RegistrationNumber, registrationIndex, _obj.RegistrationDate.Value,
                                                                             formatItems.DepartmentCode, formatItems.BusinessUnitCode, formatItems.CaseFileIndex,
                                                                             formatItems.DocumentKindCode, formatItems.CounterpartyCode, formatItems.LeadingDocumentId)
                     : Functions.DocumentRegister.IsRegistrationNumberUnique(_obj.DocumentRegister, _obj, _obj.RegistrationNumber, registrationIndex,
                                                                             _obj.RegistrationDate.Value)));

          _obj.Index = registrationIndex;
        }
        else if (!string.IsNullOrEmpty(_obj.RegistrationNumber) && _obj.Index.HasValue && _obj.Index.Value > 0 &&
                 _obj.RegistrationNumber != _obj.State.Properties.RegistrationNumber.OriginalValue)
        {
          var currentCode = Functions.DocumentRegister.GetCurrentNumber(_obj.DocumentRegister, _obj.RegistrationDate.Value, leadDocumentId, departmentId, businessUnitId);
          if (_obj.Index == (currentCode + 1))
          {
            Functions.DocumentRegister.SetCurrentNumber(_obj.DocumentRegister, _obj.Index.Value, leadDocumentId, departmentId, businessUnitId, _obj.RegistrationDate.Value);
          }
        }
        
      }
      
      if (e.Params.Contains(Sungero.Docflow.Constants.OfficialDocument.RepeatRegister))
        e.Params.Remove(Sungero.Docflow.Constants.OfficialDocument.RepeatRegister);
    }
  }

}