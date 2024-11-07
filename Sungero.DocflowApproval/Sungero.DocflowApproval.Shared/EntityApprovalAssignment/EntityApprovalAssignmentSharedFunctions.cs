using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.EntityApprovalAssignment;
using CommonsPublicFuncs = Sungero.Commons.PublicFunctions;

namespace Sungero.DocflowApproval.Shared
{
  partial class EntityApprovalAssignmentFunctions
  {
    #region Получение закешированных параметров видимости и доступности с EntityParams
    
    /// <summary>
    /// Закешировать свойства блока и других признаков в параметры сущности, если их нет в кеше.
    /// </summary>
    public virtual void FillEntityParamsIfEmpty()
    {
      var anyParameterIsMissing =
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.Module.NeedShowNoRightsHintParamName) ||
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.EntityApprovalAssignment.AllowApproveWithSuggestionsParamName) ||
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.EntityApprovalAssignment.AllowChangePropertiesParamName) ||
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.EntityApprovalAssignment.AllowChangeReworkPerformerParamName) ||
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.EntityApprovalAssignment.HideDocumentSummaryParamName) ||
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.EntityApprovalAssignment.NeedStrongSignatureParamName) ||
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.EntityApprovalAssignment.AllowAddApproversParamName) ||
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.Module.IsSendingToCounterpartyEnabledInSchemeParamName) ||
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.Module.HasAnyDocumentReviewInSchemeParamName);
      
      if (anyParameterIsMissing)
        Functions.EntityApprovalAssignment.Remote.FillEntityParams(_obj);
    }
    
    /// <summary>
    /// Пользователь без прав на основной документ.
    /// </summary>
    /// <returns>True - на документ не хватает прав, False - права есть, или нет документа.</returns>
    public virtual bool IsUserWithoutRightsOnMainDocument()
    {
      return CommonsPublicFuncs.Module
        .GetBooleanEntityParamsValue(_obj, Constants.Module.NeedShowNoRightsHintParamName);
    }
    
    /// <summary>
    /// Разрешено ли согласование с замечаниями.
    /// </summary>
    /// <returns>True - разрешено, False - нет.</returns>
    public virtual bool CanApproveWithSuggestions()
    {
      return CommonsPublicFuncs.Module
        .GetBooleanEntityParamsValue(_obj, Constants.EntityApprovalAssignment.AllowApproveWithSuggestionsParamName);
    }
    
    /// <summary>
    /// Разрешено ли изменение параметров.
    /// </summary>
    /// <returns>True - разрешено, False - нет.</returns>
    public virtual bool CanChangeProperties()
    {
      return CommonsPublicFuncs.Module
        .GetBooleanEntityParamsValue(_obj, Constants.EntityApprovalAssignment.AllowChangePropertiesParamName);
    }

    /// <summary>
    /// Разрешено ли выбирать ответственного за доработку.
    /// </summary>
    /// <returns>True - разрешено, False - нет.</returns>
    public virtual bool CanChangeReworkPerformer()
    {
      return CommonsPublicFuncs.Module
        .GetBooleanEntityParamsValue(_obj, Constants.EntityApprovalAssignment.AllowChangeReworkPerformerParamName);
    }
    
    /// <summary>
    /// Необходимо ли скрыть реквизиты документа.
    /// </summary>
    /// <returns>True - необходимо скрыть, False - нет.</returns>
    public virtual bool NeedHideDocumentSummary()
    {
      return CommonsPublicFuncs.Module
        .GetBooleanEntityParamsValue(_obj, Constants.EntityApprovalAssignment.HideDocumentSummaryParamName);
    }

    /// <summary>
    /// Необходимо ли требовать усиленную подпись.
    /// </summary>
    /// <returns>True - необходимо требовать, False - нет.</returns>
    public virtual bool NeedStrongSignature()
    {
      return CommonsPublicFuncs.Module
        .GetBooleanEntityParamsValue(_obj, Constants.EntityApprovalAssignment.NeedStrongSignatureParamName);
    }
    
    /// <summary>
    /// Разрешено ли добавление согласующих.
    /// </summary>
    /// <returns>True - разрешено, False - нет.</returns>
    public virtual bool CanAddApprovers()
    {
      return CommonsPublicFuncs.Module
        .GetBooleanEntityParamsValue(_obj, Constants.EntityApprovalAssignment.AllowAddApproversParamName);
    }
    
    /// <summary>
    /// Имеется ли хотя бы одна отправка контрагенту.
    /// </summary>
    /// <returns>True - разрешено, False - нет.</returns>
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
    
    /// <summary>
    /// Обновить отображение доставки.
    /// </summary>
    public virtual void UpdateDeliveryMethodState()
    {
      var document = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      if (document == null)
        return;

      // Не давать изменять способ доставки для наследников не оффдока и исходящих писем на несколько адресатов.
      if (!OfficialDocuments.Is(document) || OutgoingDocumentBases.As(document)?.IsManyAddressees == true)
      {
        _obj.State.Properties.DeliveryMethod.IsEnabled = false;
        _obj.State.Properties.ExchangeService.IsEnabled = false;
        return;
      }
      
      this.SetDefaultDeliveryFieldsState();
      
      if (_obj.DeliveryMethod?.Sid == Constants.Module.ExchangeDeliveryMethodSid)
      {
        this.UpdateStateForExchangeDeliveryMethod(OfficialDocuments.As(document));
      }
    }
    
    public virtual void SetDefaultDeliveryFieldsState()
    {
      _obj.State.Properties.DeliveryMethod.IsEnabled = true;
      _obj.State.Properties.ExchangeService.IsEnabled = false;
      _obj.State.Properties.ExchangeService.IsRequired = false;
    }
    
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
    /// Проверить, не заблокированы ли документы текущим пользователем.
    /// </summary>
    /// <returns>True - хотя бы один заблокирован, False - все свободны.</returns>
    public virtual bool AreDocumentsLockedByMe()
    {
      var documents = new List<IElectronicDocument>();
      documents.Add(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      documents.AddRange(_obj.AddendaGroup.ElectronicDocuments);
      
      return Functions.Module.IsAnyDocumentLockedByCurrentEmployee(documents);
    }
    
    /// <summary>
    /// Обновить состояние поля "Адресаты".
    /// </summary>
    public virtual void UpdateAddresseesFieldState()
    {
      var changingPropertiesIsAllowed = this.CanChangeProperties();
      if (!changingPropertiesIsAllowed)
      {
        this.SetDefaultAddresseesFieldState();
        return;
      }
      
      var task = _obj.Task;
      if (DocumentFlowTasks.Is(task))
      {
        _obj.State.Properties.Addressees.IsVisible = this.AddresseesFieldIsVisible();
        _obj.State.Properties.Addressees.IsEnabled = this.AddresseesFieldIsEnabled();
        return;
      }
      
      this.SetDefaultAddresseesFieldState();
    }
    
    /// <summary>
    /// Определить нужно ли показывать поле "Адресаты".
    /// </summary>
    /// <returns>True - нужно, False - иначе.</returns>
    public virtual bool AddresseesFieldIsVisible()
    {
      return this.HasAnyDocumentReviewInScheme();
    }
    
    /// <summary>
    /// Определить доступность для редактирования поля "Адресаты".
    /// </summary>
    /// <returns>True - нужно, False - иначе.</returns>
    public virtual bool AddresseesFieldIsEnabled()
    {
      var document = _obj.DocumentGroup.All.OfType<IOfficialDocument>().FirstOrDefault();
      return Functions.Module.AssignmentAddresseesIsEnabled(document);
    }
    
    /// <summary>
    /// Установить состояние поля "Адресаты" по умолчанию.
    /// </summary>
    public virtual void SetDefaultAddresseesFieldState()
    {
      _obj.State.Properties.Addressees.IsVisible = false;
      _obj.State.Properties.Addressees.IsEnabled = false;
    }
  }
}