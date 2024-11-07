using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Content.Shared.ElectronicDocument;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.DocumentFlowTask;
using Sungero.Domain.Shared;
using CommonsPublicFuncs = Sungero.Commons.PublicFunctions;

namespace Sungero.DocflowApproval.Shared
{
  partial class DocumentFlowTaskFunctions
  {
    #region Получение закешированных параметров видимости и доступности с EntityParams
    
    /// <summary>
    /// Закешировать свойства блока и других признаков в параметры сущности, если их нет в кеше.
    /// </summary>
    public virtual void FillEntityParamsIfEmpty()
    {
      var anyParameterIsMissing =
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.Module.IsSendingToCounterpartyEnabledInSchemeParamName) ||
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.Module.HasAnyDocumentReviewInSchemeParamName);
      
      if (anyParameterIsMissing)
        Functions.DocumentFlowTask.Remote.FillEntityParams(_obj);
    }
    
    /// <summary>
    /// Имеется ли хотя бы одна отправка контрагенту.
    /// </summary>
    /// <returns>True - имеется, False - нет.</returns>
    public virtual bool IsSendingToCounterpartyEnabledInScheme()
    {
      return CommonsPublicFuncs.Module
        .GetBooleanEntityParamsValue(_obj, Constants.Module.IsSendingToCounterpartyEnabledInSchemeParamName);
    }
    
    /// <summary>
    /// Имеется ли хотя бы одно рассмотрение документа.
    /// </summary>
    /// <returns>True - имеется, False - нет.</returns>
    public virtual bool HasAnyDocumentReviewInScheme()
    {
      return CommonsPublicFuncs.Module
        .GetBooleanEntityParamsValue(_obj, Constants.Module.HasAnyDocumentReviewInSchemeParamName);
    }
    
    #endregion
    
    #region Доступность и заполнение свойств
    
    /// <summary>
    /// Заполнить вариант процесса.
    /// </summary>
    /// <remarks>Только если вариант процесса не заполнен или выбран неподходящий.</remarks>
    public virtual void SetDefaultProcessKind()
    {
      var processKinds = Sungero.Workflow.ProcessKinds.GetAllMatches(_obj);
      
      // Не найдено подходящих вариантов процесса - очищаем.
      if (!processKinds.Any())
      {
        _obj.ProcessKind = null;
        return;
      }
      
      // Найден один вариант процесса - заполняем им.
      if (processKinds.Count() == 1)
      {
        _obj.ProcessKind = processKinds.First();
        return;
      }
      
      // Найдено несколько вариантов процесса.
      // Если уже заполнен подходящий вариант процесса - не перебиваем.
      if (_obj.ProcessKind != null && processKinds.Contains(_obj.ProcessKind))
        return;
      
      // Не заполнен подходящий вариант - заполняем самым приоритетным (если такой есть) или чистим поле.
      var topPriority = processKinds.Max(p => p.Priority);
      var processKindsTop = processKinds.Where(p => p.Priority == topPriority);
      if (processKindsTop.Count() == 1)
        _obj.ProcessKind = processKindsTop.First();
      else
        _obj.ProcessKind = null;
    }
    
    /// <summary>
    /// Заполнить адресатов по умолчанию.
    /// </summary>
    public virtual void SetDefaultAddressees()
    {
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (officialDocument == null)
        return;
      
      var addressees = Sungero.Docflow.PublicFunctions.OfficialDocument.GetAddressees(officialDocument);
      this.SetAddressees(addressees);
    }
    
    /// <summary>
    /// Задать адресатов в задаче.
    /// </summary>
    /// <param name="addressees">Адресаты.</param>
    public virtual void SetAddressees(List<Company.IEmployee> addressees)
    {
      _obj.Addressees.Clear();
      if (addressees == null)
        return;
      addressees = addressees.Where(x => x != null).Distinct().ToList();
      foreach (var addressee in addressees)
        _obj.Addressees.AddNew().Addressee = addressee;
    }
    
    /// <summary>
    /// Обновить доступность, видимость и обязательность свойств.
    /// </summary>
    public virtual void UpdateFieldsAvailability()
    {
      this.UpdateDeliveryMethodAvailability();
      this.UpdateAddresseesFieldAvailability();
    }
    
    /// <summary>
    /// Обновить доступность способа отправки.
    /// </summary>
    public virtual void UpdateDeliveryMethodAvailability()
    {
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());

      var deliveryFieldsAreNotAvailable = _obj.ProcessKind == null ||
        officialDocument == null ||
        RecordManagement.OutgoingLetters.As(officialDocument)?.IsManyAddressees == true ||
        !Functions.DocumentFlowTask.IsSendingToCounterpartyEnabledInScheme(_obj);
      
      if (deliveryFieldsAreNotAvailable)
      {
        this.HideAndClearDeliveryFields();
        return;
      }
      
      this.SetDefaultDeliveryFieldsState();
      
      if (_obj.DeliveryMethod?.Sid == Constants.Module.ExchangeDeliveryMethodSid)
      {
        this.UpdateStateForExchangeDeliveryMethod(officialDocument);
      }
    }
    
    /// <summary>
    /// Отключить способ отправки на форме, когда он неактуален.
    /// </summary>
    public virtual void HideAndClearDeliveryFields()
    {
      _obj.State.Properties.DeliveryMethod.IsVisible = false;
      if (_obj.DeliveryMethod != null)
        _obj.DeliveryMethod = null;
      
      _obj.State.Properties.ExchangeService.IsVisible = false;
      _obj.State.Properties.ExchangeService.IsRequired = false;
      if (_obj.ExchangeService != null)
        _obj.ExchangeService = null;
    }
    
    /// <summary>
    /// Установить состояние полей доставки по умолчанию.
    /// </summary>
    public virtual void SetDefaultDeliveryFieldsState()
    {
      _obj.State.Properties.DeliveryMethod.IsVisible = true;
      _obj.State.Properties.DeliveryMethod.IsEnabled = true;
      _obj.State.Properties.ExchangeService.IsVisible = true;
      _obj.State.Properties.ExchangeService.IsEnabled = false;
      _obj.State.Properties.ExchangeService.IsRequired = false;
    }
    
    /// <summary>
    /// Обновить состояние полей доставки для способа отправки "Сервис обмена".
    /// </summary>
    /// <param name="officialDocument">Документ.</param>
    public virtual void UpdateStateForExchangeDeliveryMethod(IOfficialDocument officialDocument)
    {
      var formParams = ((Domain.Shared.IExtendedEntity)_obj).Params;
      var isIncomingDocument = false;
      
      if (formParams.ContainsKey(Constants.EntityApprovalAssignment.IsIncomingDocument))
        isIncomingDocument = (bool)formParams[Constants.EntityApprovalAssignment.IsIncomingDocument];
      else
      {
        isIncomingDocument = Docflow.PublicFunctions.OfficialDocument.Remote.CanSendAnswer(officialDocument);
        formParams[Constants.EntityApprovalAssignment.IsIncomingDocument] = isIncomingDocument;
      }
      
      var isFormalizedDocument = AccountingDocumentBases.As(officialDocument)?.IsFormalized == true;
      _obj.State.Properties.DeliveryMethod.IsEnabled = !isIncomingDocument;
      _obj.State.Properties.ExchangeService.IsEnabled = !(isIncomingDocument || isFormalizedDocument);
      _obj.State.Properties.ExchangeService.IsRequired = _obj.State.Properties.ExchangeService.IsEnabled;
    }
    
    /// <summary>
    /// Обновить состояние поля "Адресаты".
    /// </summary>
    public virtual void UpdateAddresseesFieldState()
    {
      _obj.State.Properties.Addressees.IsVisible = this.AddresseesFieldIsVisible();
      _obj.State.Properties.Addressees.IsEnabled = this.AddresseesFieldIsEnabled();
    }
    
    /// <summary>
    /// Определить нужно ли показывать поле "Адресаты".
    /// </summary>
    /// <returns>True - нужно, False - иначе.</returns>
    public virtual bool AddresseesFieldIsVisible()
    {
      var document = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      var reviewBlocksExist = _obj.ProcessKind != null && this.HasAnyDocumentReviewInScheme();
      
      return document != null && reviewBlocksExist;
    }
    
    /// <summary>
    /// Определить доступность для редактирования поля "Адресаты".
    /// </summary>
    /// <returns>True - нужно, False - иначе.</returns>
    public virtual bool AddresseesFieldIsEnabled()
    {
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      
      return officialDocument == null || Sungero.Docflow.PublicFunctions.OfficialDocument.TaskAdresseesFieldIsEnabled(officialDocument);
    }
    
    /// <summary>
    /// Обновить состояние поля "Адресаты".
    /// </summary>
    public virtual void UpdateAddresseesFieldAvailability()
    {
      var document = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      var reviewBlocksExist = _obj.ProcessKind != null && this.HasAnyDocumentReviewInScheme();
      
      _obj.State.Properties.Addressees.IsVisible = document != null && reviewBlocksExist;
      
      var officialDocument = OfficialDocuments.As(document);
      if (officialDocument == null)
        return;
      
      _obj.State.Properties.Addressees.IsEnabled = Sungero.Docflow.PublicFunctions.OfficialDocument.TaskAdresseesFieldIsEnabled(officialDocument);
    }
    
    #endregion Доступность и заполнение свойств.

    /// <summary>
    /// Проверить наличие согласуемого документа в задаче и наличие хоть каких-то прав на него.
    /// </summary>
    /// <returns>True, если с документом можно работать.</returns>
    public virtual bool HasDocumentAndCanRead()
    {
      return _obj.DocumentGroup.ElectronicDocuments.Any();
    }
    
    /// <summary>
    /// Сохранить права автора задачи.
    /// </summary>
    public virtual void PreserveAuthorOriginalAttachmentsRights()
    {
      _obj.RevokedDocumentsRights.Clear();
      
      var author = _obj.Author;
      var documents = _obj.AllAttachments
        .Where(x => ElectronicDocuments.Is(x))
        .Where(x => x.AccessRights.StrictMode == AccessRightsStrictMode.None)
        .Select(x => ElectronicDocuments.As(x));
      
      foreach (var document in documents)
      {
        var rightsType = this.GetRecipientDocumentFlowTaskRevokedDocumentsRightsType(author, document);
        if (rightsType == null)
          continue;
        
        Logger.DebugFormat("PreserveAuthorOriginalAttachmentsRights. Task: {0}. Document: {1}. User: {2}. Rights: {3}", _obj.Id, document.Id, author.Id, rightsType.ToString());

        if (_obj.RevokedDocumentsRights.Any(r => r.EntityId == document.Id))
        {
          Logger.DebugFormat("PreserveAuthorOriginalAttachmentsRights. Already preserved. Task: {0}. Document: {1}. User: {2}. Rights: {3}", _obj.Id, document.Id, author.Id, rightsType.ToString());
          continue;
        }
        
        var preservedRights = _obj.RevokedDocumentsRights.AddNew();
        preservedRights.EntityId = document.Id;
        preservedRights.RightType = rightsType;
        Logger.DebugFormat("PreserveAuthorOriginalAttachmentsRights. Preserved. Task: {0}. Document: {1}. User: {2}. Rights: {3}", _obj.Id, document.Id, author.Id, rightsType.ToString());
      }
    }
    
    /// <summary>
    /// Получить тип прав коллекции RevokedDocumentsRights для субъекта прав по документу.
    /// </summary>
    /// <param name="recipient">Субъект прав.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Тип прав коллекции RevokedDocumentsRights для субъекта прав по документу.</returns>
    public virtual Enumeration? GetRecipientDocumentFlowTaskRevokedDocumentsRightsType(IRecipient recipient, IElectronicDocument document)
    {
      if (document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, recipient))
        return DocflowApproval.DocumentFlowTaskRevokedDocumentsRights.RightType.FullAccess;
      
      if (document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, recipient))
        return DocflowApproval.DocumentFlowTaskRevokedDocumentsRights.RightType.Edit;
      
      if (document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Read, recipient))
        return DocflowApproval.DocumentFlowTaskRevokedDocumentsRights.RightType.Read;
      
      return null;
    }

  }
}