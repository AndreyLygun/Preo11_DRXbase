using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.AccountingDocumentBase;
using FormatElement = Sungero.Docflow.DocumentRegisterNumberFormatItems.Element;

namespace Sungero.Docflow.Shared
{
  partial class AccountingDocumentBaseFunctions
  {
    /// <summary>
    /// Получение категории договора/доп. соглашения, указанного в акте.
    /// </summary>
    /// <returns>Категория договора.</returns>
    public override IDocumentGroupBase GetDocumentGroup()
    {
      if (_obj.LeadingDocument != null)
        return Functions.OfficialDocument.GetDocumentGroup(_obj.LeadingDocument);
      return null;
    }
    
    /// <summary>
    /// Получить контрагентов по документу.
    /// </summary>
    /// <returns>Контрагенты.</returns>
    public override List<Sungero.Parties.ICounterparty> GetCounterparties()
    {
      if (_obj.Counterparty == null)
        return null;
      
      return new List<Sungero.Parties.ICounterparty>() { _obj.Counterparty };
    }
    
    /// <summary>
    /// Получить основание подписания со стороны контрагента.
    /// </summary>
    /// <returns>Основание подписания со стороны контрагента.</returns>
    [Public]
    public override string GetCounterpartySigningReason()
    {
      return _obj.CounterpartySigningReason;
    }
    
    /// <summary>
    /// Поведение панели по умолчанию.
    /// </summary>
    /// <returns>True, если панель должна быть отображена при создании документа.</returns>
    public override bool DefaultRegistrationPaneVisibility()
    {
      return true;
    }
    
    /// <summary>
    /// Показ панели регистрации.
    /// </summary>
    /// <param name="conditions">Условие.</param>
    /// <returns>Результат проверки необходимости отображения.</returns>
    public override bool NeedShowRegistrationPane(bool conditions)
    {
      return base.NeedShowRegistrationPane(true);
    }
    
    /// <summary>
    /// Изменить доступность свойства "Вид носителя документа".
    /// </summary>
    public override void ChangeMediumTypePropertyAccess()
    {
      _obj.State.Properties.Medium.IsEnabled = _obj.ExchangeState == null && _obj.IsFormalized != true;
    }
    
    /// <summary>
    /// Сменить доступность реквизитов документа.
    /// </summary>
    /// <param name="isEnabled">True, если свойства должны быть доступны.</param>
    /// <param name="isRepeatRegister">Перерегистрация.</param>
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      if (_obj.VerificationState == VerificationState.InProcess && this.IsNumerationSucceed())
      {
        this.EnableRequisitesForVerification();
      }
      else
      {
        base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);
        var properties = _obj.State.Properties;
        
        var documentParams = ((Sungero.Domain.Shared.IExtendedEntity)_obj).Params;
        var isChangeTypeExchangeDocument = documentParams.ContainsKey(Constants.AccountingDocumentBase.IsChangeTypeExchangeDocument) 
          ? (bool)documentParams[Constants.AccountingDocumentBase.IsChangeTypeExchangeDocument] : false;
        
        var enabledState = !(_obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnApproval ||
                             _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.PendingSign ||
                             _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed);
        
        properties.TotalAmount.IsEnabled = enabledState || isChangeTypeExchangeDocument;
        properties.Currency.IsEnabled = enabledState || isChangeTypeExchangeDocument;
        properties.VatRate.IsEnabled = enabledState || isChangeTypeExchangeDocument;
        properties.VatAmount.IsEnabled = enabledState || isChangeTypeExchangeDocument;

        // Документ-основание.
        isEnabled = isEnabled && enabledState;

        var register = _obj.DocumentRegister;
        var leadingNumberIncludedInNumber = register != null &&
          (Functions.DocumentRegister.NumberFormatContains(register, FormatElement.LeadingNumber) ||
           register.NumberingSection == Docflow.DocumentRegister.NumberingSection.LeadingDocument);
        
        properties.LeadingDocument.IsEnabled = !leadingNumberIncludedInNumber;
        
        if (_obj.IsFormalized == true)
        {
          properties.BusinessUnit.IsEnabled = false;
          properties.IsAdjustment.IsEnabled = false;
          properties.DocumentKind.IsEnabled = isEnabled || isRepeatRegister;
        }
      }
      
      // Корректируемый документ доступен, если проставлен признак Корректировка.
      _obj.State.Properties.Corrected.IsEnabled = _obj.IsAdjustment == true;
      
      this.EnableRegistrationNumberAndDate();
    }
    
    /// <summary>
    /// Заполнить подписывающего.
    /// </summary>
    /// <param name="signatory">Подписывающий со стороны контрагента.</param>
    public override void FillCounterpartySignatory(Parties.IContact signatory)
    {
      _obj.CounterpartySignatory = signatory;
    }
    
    /// <summary>
    /// Заполнить основание со стороны контрагента.
    /// </summary>
    /// <param name="signingReason">Основание контрагента.</param>
    public override void FillCounterpartySigningReason(string signingReason)
    {
      if (!string.IsNullOrEmpty(signingReason) && signingReason.Length > _obj.Info.Properties.CounterpartySigningReason.Length)
        signingReason = signingReason.Substring(0, _obj.Info.Properties.CounterpartySigningReason.Length);
      _obj.CounterpartySigningReason = signingReason;
    }
    
    /// <summary>
    /// Заполнить имя.
    /// </summary>
    public override void FillName()
    {
      // Не автоформируемое имя.
      if (_obj != null && _obj.DocumentKind != null && !_obj.DocumentKind.GenerateDocumentName.Value)
      {
        if (_obj.Name == OfficialDocuments.Resources.DocumentNameAutotext)
          _obj.Name = string.Empty;
        
        if (_obj.VerificationState != null && string.IsNullOrWhiteSpace(_obj.Name))
          _obj.Name = _obj.DocumentKind.ShortName;
      }
      
      if (_obj.DocumentKind == null || (!_obj.DocumentKind.GenerateDocumentName.Value && _obj.IsFormalized != true))
        return;
      
      // Автоформируемое имя.
      var name = string.Empty;
      
      /* Имя в формате:
        <Вид документа> №<номер> от <дата> <контрагент> "<содержание>".
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.RegistrationDate != null)
          name += OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        if (_obj.Counterparty != null)
          name += " " + _obj.Counterparty.DisplayValue;
        
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
          name += " \"" + _obj.Subject + "\"";
      }

      if (string.IsNullOrWhiteSpace(name))
      {
        name = _obj.VerificationState == null ? OfficialDocuments.Resources.DocumentNameAutotext : _obj.DocumentKind.ShortName;
      }
      else if (_obj.DocumentKind != null && _obj.IsAdjustment != true)
      {
        name = _obj.DocumentKind.ShortName + name;
      }
      else if (_obj.DocumentKind != null && _obj.IsAdjustment == true)
      {
        using (TenantInfo.Culture.SwitchTo())
        {
          name = AccountingDocumentBases.Resources.Adjustment + _obj.DocumentKind.ShortName.ToLower() + name;
        }
      }

      name = Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
    }
    
    #region Доступность свойств
    
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      
      // Подразделение обязательно, только если это указано в метаданных.
      _obj.State.Properties.Department.IsRequired = _obj.Info.Properties.Department.IsRequired;
      
      // Содержание обязательно, только если это указано в метаданных.
      _obj.State.Properties.Subject.IsRequired = _obj.Info.Properties.Subject.IsRequired;
      
      // Изменить обязательность полей в зависимости от того, программная или визуальная работа.
      var isVisualMode = ((Domain.Shared.IExtendedEntity)_obj).Params.ContainsKey(Docflow.PublicConstants.OfficialDocument.IsVisualModeParamName);
      _obj.State.Properties.Counterparty.IsRequired = isVisualMode;
    }
    
    [Public]
    public override bool HasEmptyRequiredProperties()
    {
      return _obj.Counterparty == null;
    }
    
    /// <summary>
    /// Сменить доступность поля Контрагент.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    public override void ChangeCounterpartyPropertyAccess(bool isEnabled)
    {
      _obj.State.Properties.Counterparty.IsEnabled = isEnabled;
    }
    
    #endregion
    
    /// <summary>
    /// Изменить отображение панели регистрации.
    /// </summary>
    /// <param name="needShow">Признак отображения.</param>
    /// <param name="repeatRegister">Признак повторной регистрации\изменения реквизитов.</param>
    public override void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
    {
      base.ChangeRegistrationPaneVisibility(needShow, repeatRegister);
      _obj.State.Properties.ExecutionState.IsVisible = false;
      _obj.State.Properties.ControlExecutionState.IsVisible = false;
    }
    
    /// <summary>
    /// Добавить связанные с финансовыми документами документы в группу вложений.
    /// </summary>
    /// <param name="group">Группа вложений.</param>
    public override void AddRelatedDocumentsToAttachmentGroup(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup group)
    {
      var documentInGroupIds = group.All.Select(x => x.Id);
      
      // Получить ведущий документ.
      var financialDocuments = _obj.Relations.GetRelatedFromDocuments(Contracts.PublicConstants.Module.AccountingDocumentsRelationName);
      var documentsToAdd = financialDocuments.Where(d => !documentInGroupIds.Contains(d.Id)).ToList();
      
      // Добавить корректируемые документы.
      var correctDocuments = _obj.Relations.GetRelatedFromDocuments(Constants.Module.CorrectionRelationName);
      documentsToAdd.AddRange(correctDocuments.Where(d => !documentInGroupIds.Contains(d.Id)).ToList());
      
      foreach (var document in documentsToAdd)
        group.All.Add(document);
    }
    
    /// <summary>
    /// Сменить доступность поля Контрагент. Доступность зависит от статуса.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    /// <param name="counterpartyCodeInNumber">Признак вхождения кода контрагента в формат номера. TRUE - входит.</param>
    /// <param name="enabledState">Признак доступности поля в зависимости от статуса.</param>
    public override void ChangeCounterpartyPropertyAccess(bool isEnabled, bool counterpartyCodeInNumber, bool enabledState)
    {
      var properties = _obj.State.Properties;
      var register = _obj.DocumentRegister;
      
      var leadingNumberIncludedInNumber = register != null &&
        (Functions.DocumentRegister.NumberFormatContains(register, FormatElement.LeadingNumber) ||
         register.NumberingSection == Docflow.DocumentRegister.NumberingSection.LeadingDocument);
      
      properties.Counterparty.IsEnabled = isEnabled && !counterpartyCodeInNumber && (!leadingNumberIncludedInNumber && _obj.IsFormalized != true && enabledState);
      
      if (_obj.IsFormalized == true)
        properties.Counterparty.IsEnabled = false;
    }

    /// <summary>
    /// Проверять рег. номер на уникальность.
    /// </summary>
    /// <returns>True - проверять, False - не проверять.</returns>
    public override bool CheckRegistrationNumberUnique()
    {
      return false;
    }
    
    /// <summary>
    /// Сделать доступными рег. номер и рег. дату незарегистрированного документа регистрируемого вида в процессе верификации.
    /// </summary>
    public override void EnableRegistrationNumberAndDate()
    {
      var verificationInProcess = _obj.VerificationState == Docflow.OfficialDocument.VerificationState.InProcess;
      if (!verificationInProcess)
        return;
      
      base.EnableRegistrationNumberAndDate();
    }
    
    /// <summary>
    /// Заполнить свойство "Ведущий документ" в зависимости от типа документа.
    /// </summary>
    /// <param name="leadingDocument">Ведущий документ.</param>
    /// <remarks>Используется при смене типа.</remarks>
    [Public]
    public override void FillLeadingDocument(IOfficialDocument leadingDocument)
    {
      var contractualDocument = ContractualDocumentBases.As(leadingDocument);
      if (contractualDocument != null && (_obj.Counterparty == null || Equals(_obj.Counterparty, contractualDocument.Counterparty)))
        _obj.LeadingDocument = contractualDocument;
      else
        _obj.LeadingDocument = null;
    }
    
    /// <summary>
    /// Проверить требование подписи контрагента.
    /// </summary>
    /// <param name="senderBox">Абонентский ящик отправителя.</param>
    /// <param name="isPrimaryDocument">Признак, что документ основной.</param>
    /// <param name="needSign">Признак Требуется подписание.</param>
    /// <returns> True - требуется подписание, иначе - false.</returns>
    /// <remarks>Для основного документа возвращается переданное в метод значение. Для приложения дополнительно проверяется:
    /// не требуется подпись для счет-фактур и исх. счетов. Исключение СБИС - подпись на счет-фактуру требуется.</remarks>
    [Public]
    public override bool NeedCounterpartySign(ExchangeCore.IBusinessUnitBox senderBox,
                                                bool isPrimaryDocument, bool needSign)
    {
      if (isPrimaryDocument || senderBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis)
        return needSign;
      
      var isTaxInvoice = _obj.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Schf;
      return needSign && !isTaxInvoice;
    }
    
    /// <summary>
    /// Проверить, что сумма НДС совпадает с авторасчетом.
    /// </summary>
    /// <param name="vatAmount">Сумма НДС.</param>
    /// <returns>True - если значение суммы НДС совпадает с авторасчетом, иначе false.</returns>
    [Public]
    public virtual bool CheckVatAmount(double? vatAmount)
    {
      if (!_obj.TotalAmount.HasValue || _obj.VatRate == null)
        return true;
      
      if (_obj.VatRate != null && vatAmount == null)
        return false;
      
      var expectedVatAmount = Sungero.Commons.PublicFunctions.Module.GetVatAmountFromTotal(_obj.TotalAmount.Value, _obj.VatRate);
      return vatAmount.Value == expectedVatAmount;
    }
    
    /// <summary>
    /// Заполнить сумму НДС.
    /// </summary>
    /// <param name="totalAmount">Сумма.</param>
    /// <param name="vatRate">Ставка НДС.</param>
    [Public]
    public virtual void FillVatAmount(double? totalAmount, Sungero.Commons.IVatRate vatRate)
    {
      if (totalAmount == null)
        _obj.VatAmount = null;
      
      if (totalAmount.HasValue && vatRate != null)
        _obj.VatAmount = Sungero.Commons.PublicFunctions.Module.GetVatAmountFromTotal(totalAmount.Value, vatRate);
    }
    
    /// <summary>
    /// Заполнить сумму без НДС.
    /// </summary>
    /// <param name="totalAmount">Сумма.</param>
    /// <param name="vatAmount">Сумма НДС.</param>
    [Public]
    public virtual void FillNetAmount(double? totalAmount, double? vatAmount)
    {
      if (totalAmount == null || vatAmount == null)
        _obj.NetAmount = null;
      
      if (totalAmount.HasValue && vatAmount.HasValue)
        _obj.NetAmount = Math.Round(totalAmount.Value - vatAmount.Value, 2);
    }
    
    /// <summary>
    /// Определить ответственного за документ.
    /// </summary>
    /// <returns>Ответственный за документ.</returns>
    public override Sungero.Company.IEmployee GetDocumentResponsibleEmployee()
    {
      if (_obj.ResponsibleEmployee != null)
        return _obj.ResponsibleEmployee;
      
      return base.GetDocumentResponsibleEmployee();
    }
  }
}