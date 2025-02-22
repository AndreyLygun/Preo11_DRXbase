using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccountingDocumentBase;
using Sungero.Domain;

namespace Sungero.Docflow.Server
{
  partial class AccountingDocumentBaseFunctions
  {

    /// <summary>
    /// Получить текстовку для записи в историю изменения суммы.
    /// </summary>
    /// <param name="isTotalAmountChanged">Признак того что изменилась общая сумма.</param>
    /// <returns>Текст комментария для истории.</returns>
    [Public]
    public virtual string GetAmountChangeHistoryComment(bool isTotalAmountChanged)
    {
      var result = string.Empty;
      if (isTotalAmountChanged && !_obj.TotalAmount.HasValue)
        return result;
      
      var currencyAlphaCode = (_obj.Currency != null) ? _obj.Currency.AlphaCode : string.Empty;
      if (_obj.TotalAmount.HasValue)
        result = string.Join("|", _obj.TotalAmount.Value, currencyAlphaCode);
      
      if (_obj.VatRate == null && !_obj.VatAmount.HasValue)
        return result;
      
      var withoutVat = _obj.VatRate != null && _obj.VatRate.Sid == Docflow.Constants.Module.VatRateWithoutVatSid;
      if (withoutVat)
        result = string.Join("|", result, Sungero.Docflow.OfficialDocuments.Resources.WithoutVatPart);
      else
      {
        var vatRate = _obj.VatRate != null
          ? string.Format(Constants.Module.VatRateHistoryTemplate, _obj.VatRate.Rate.Value.ToString())
          : string.Empty;
        var vatAmount = _obj.VatAmount.HasValue ? _obj.VatAmount.Value.ToString() : string.Empty;
        
        result = _obj.VatAmount.HasValue
          ? string.Join("|", result, string.Format(Sungero.Docflow.OfficialDocuments.Resources.VatPart, vatRate, vatAmount, currencyAlphaCode))
          : string.Join("|", result, string.Format(Sungero.Docflow.OfficialDocuments.Resources.VatPart, vatRate, string.Empty, string.Empty));
      }
      // Удаление лишней запятой в начале строки в случае когда общая сумма пуста.
      if (!_obj.TotalAmount.HasValue && result.Length > Constants.Module.VatRateHistoryTrimSymbolsCount)
        result = result.Remove(0, Constants.Module.VatRateHistoryTrimSymbolsCount);

      return _obj.NetAmount.HasValue
        ? string.Join("|", result,
                      string.Format(Sungero.Docflow.OfficialDocuments.Resources.TotalAmountWithoutVatPart, _obj.NetAmount.Value.ToString(), currencyAlphaCode))
        : result;
    }
    
    /// <summary>
    /// Получить права подписания финансовых документов.
    /// </summary>
    /// <returns>Список подходящих правил.</returns>
    public override IQueryable<ISignatureSetting> GetSignatureSettingsQuery()
    {
      var basedSettings = base.GetSignatureSettingsQuery()
        .Where(s => s.Limit == Docflow.SignatureSetting.Limit.NoLimit || (s.Limit == Docflow.SignatureSetting.Limit.Amount &&
                                                                          s.Amount >= _obj.TotalAmount && Equals(s.Currency, _obj.Currency)));
      
      if (_obj.DocumentKind != null && _obj.DocumentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts)
      {
        var category = Docflow.PublicFunctions.OfficialDocument.GetDocumentGroup(_obj);
        basedSettings = basedSettings
          .Where(s => !s.Categories.Any() || s.Categories.Any(c => Equals(c.Category, category)));
      }
      return basedSettings;
    }
    
    /// <summary>
    /// Получить дату начала квартала.
    /// </summary>
    /// <param name="currentDate">Дата.</param>
    /// <returns>Дата начала квартала.</returns>
    [Public]
    public static DateTime BeginningOfQuarter(DateTime currentDate)
    {
      if (currentDate.Month < 4)
        return currentDate.BeginningOfYear();
      if (currentDate.Month > 3 && currentDate.Month < 7)
        return new DateTime(currentDate.Year, 4, 1);
      if (currentDate.Month > 6 && currentDate.Month < 10)
        return new DateTime(currentDate.Year, 7, 1);
      return new DateTime(currentDate.Year, 10, 1);
    }
    
    /// <summary>
    /// Получить дату окончания квартала.
    /// </summary>
    /// <param name="currentDate">Дата.</param>
    /// <returns>Дата окончания квартала.</returns>
    [Public]
    public static DateTime EndOfQuarter(DateTime currentDate)
    {
      if (currentDate.Month < 4)
        return new DateTime(currentDate.Year, 3, 31);
      if (currentDate.Month > 3 && currentDate.Month < 7)
        return new DateTime(currentDate.Year, 6, 30);
      if (currentDate.Month > 6 && currentDate.Month < 10)
        return new DateTime(currentDate.Year, 9, 30);
      return currentDate.EndOfYear();
    }
    
    /// <summary>
    /// Сгенерировать титул продавца.
    /// </summary>
    /// <param name="sellerTitle">Параметры титула для генерации.</param>
    [Remote, Public]
    public virtual void GenerateSellerTitle(Structures.AccountingDocumentBase.ISellerTitle sellerTitle)
    {
      FinancialArchive.PublicFunctions.Module.Remote.GenerateSellerTitle(_obj, sellerTitle);
      if (!_obj.SellerTitleId.HasValue)
        return;
      
      // Поставить документ в очередь на генерацию pdf.
      var sellerVersion = _obj.Versions.Single(v => v.Id == _obj.SellerTitleId);

      if (sellerVersion.PublicBody.Size == 0)
      {
        var previousVersion = _obj.Versions.Where(v => v.Id != _obj.SellerTitleId).OrderBy(v => v.Number).LastOrDefault();
        if (previousVersion != null)
        {
          sellerVersion.PublicBody.Write(previousVersion.PublicBody.Read());
          sellerVersion.AssociatedApplication = previousVersion.AssociatedApplication;
        }
      }
      
      Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(_obj, _obj.SellerTitleId.Value);
      Exchange.PublicFunctions.Module.EnqueueXmlToPdfBodyConverter(_obj, _obj.SellerTitleId.Value, _obj.ExchangeState);
    }
    
    /// <summary>
    /// Сгенерировать титул покупателя.
    /// </summary>
    /// <param name="buyerTitle">Параметры титула для генерации.</param>
    /// <param name="isAgent">Признак вызова из фонового процесса.</param>
    [Remote, Public]
    public virtual void GenerateAnswer(Structures.AccountingDocumentBase.IBuyerTitle buyerTitle, bool isAgent)
    {
      Exchange.PublicFunctions.Module.GenerateBuyerTitle(_obj, buyerTitle);

      // Поставить документ в очередь на генерацию pdf.
      if (_obj.BuyerTitleId.HasValue)
      {
        var version = _obj.Versions.Single(v => v.Id == _obj.BuyerTitleId);
        var sellerVersion = _obj.Versions.Single(v => v.Id == _obj.SellerTitleId);
        version.PublicBody.Write(sellerVersion.PublicBody.Read());
        version.AssociatedApplication = sellerVersion.AssociatedApplication;
        if (isAgent)
        {
          Docflow.PublicFunctions.Module.GeneratePublicBodyForExchangeDocument(_obj, _obj.BuyerTitleId.Value, _obj.ExchangeState);
        }
        else
        {
          Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(_obj, _obj.BuyerTitleId.Value);
          Exchange.PublicFunctions.Module.EnqueueXmlToPdfBodyConverter(_obj, _obj.BuyerTitleId.Value, _obj.ExchangeState);
        }
      }
    }
    
    /// <summary>
    /// Сгенерировать титул покупателя в автоматическом режиме.
    /// </summary>
    /// <param name="signatory">Подписывающий.</param>
    /// <param name="isAgent">Признак вызова из фонового процесса.</param>
    [Remote, Public]
    public virtual void GenerateDefaultAnswer(Company.IEmployee signatory, bool isAgent)
    {
      var signatureSetting = Functions.OfficialDocument.GetDefaultSignatureSetting(_obj, signatory);
      var errorlist = Functions.AccountingDocumentBase.TitleDialogValidationErrors(_obj, signatory, null, null, null, signatureSetting);
      var validationText = string.Join(Environment.NewLine, errorlist.Select(l => l.Text));
      if (errorlist.Any())
        throw AppliedCodeException.Create(validationText);
      
      var buyerTitle = this.CreateDefaultBuyerTitleStructure(signatory, signatureSetting);
      this.GenerateAnswer(buyerTitle, isAgent);
    }
    
    /// <summary>
    /// Создать структуру для генерации титула покупателя со значениями по умолчанию.
    /// </summary>
    /// <param name="signatory">Подписывающий.</param>
    /// <param name="signatureSetting">Право подписи сотрудника по умолчанию.</param>
    /// <returns>Структура для генерации титула покупателя.</returns>
    public virtual Sungero.Docflow.Structures.AccountingDocumentBase.IBuyerTitle CreateDefaultBuyerTitleStructure(Company.IEmployee signatory, ISignatureSetting signatureSetting)
    {
      // У УКД доступен только вариант "ответственный за оформление".
      var authority = Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_DealAndRegister;
      if (_obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer && _obj.IsAdjustment == true)
        authority = Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Register;
      
      var basis = Functions.Module.GetSigningReason(signatureSetting);
      var businessUnit = Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(signatory);
      var buyerTitle = Docflow.Structures.AccountingDocumentBase.BuyerTitle.Create();
      buyerTitle.ActOfDisagreement = null;
      buyerTitle.Signatory = signatory;
      buyerTitle.SignatoryPowersBase = !string.IsNullOrWhiteSpace(basis) ? basis : null;
      buyerTitle.Consignee = null;
      buyerTitle.ConsigneePowersBase = null;
      buyerTitle.BuyerAcceptanceStatus = Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Accepted;
      buyerTitle.SignatoryPowers = authority;
      buyerTitle.AcceptanceDate = Calendar.Now;
      buyerTitle.ConsigneePowerOfAttorney = null;
      buyerTitle.ConsigneeOtherReason = null;
      buyerTitle.SignatureSetting = signatureSetting;
      buyerTitle.SignerAdditionalInfo = string.Format("{0}, {1}, {2}", businessUnit.LegalName, businessUnit.TIN, basis);
      var taxDocumentClassifier = Exchange.PublicFunctions.Module.Remote.GetTaxDocumentClassifier(_obj);
      buyerTitle.TaxDocumentClassifierCode = taxDocumentClassifier?.TaxDocumentClassifierCode;
      buyerTitle.TaxDocumentClassifierFormatVersion = taxDocumentClassifier?.TaxDocumentClassifierFormatVersion;
      
      return buyerTitle;
    }

    /// <summary>
    /// Валидация диалога заполнения титула.
    /// </summary>
    /// <param name="signatory">Подписал.</param>
    /// <param name="consignee">Груз получил.</param>
    /// <param name="consigneePowerOfAttorney">Доверенность груз принявшего.</param>
    /// <param name="consigneeOtherReason">Документ груз принявшего.</param>
    /// <param name="signatorySetting">Право подписи подписавшего.</param>
    /// <returns>Список ошибок.</returns>
    [Remote]
    public virtual List<Structures.AccountingDocumentBase.GenerateTitleError> TitleDialogValidationErrors(Company.IEmployee signatory,
                                                                                                          Company.IEmployee consignee,
                                                                                                          IPowerOfAttorneyBase consigneePowerOfAttorney,
                                                                                                          string consigneeOtherReason,
                                                                                                          ISignatureSetting signatorySetting)
    {
      var errorList = new List<Structures.AccountingDocumentBase.GenerateTitleError>();
      var signatoryType = Constants.AccountingDocumentBase.GenerateTitleTypes.Signatory;
      var consigneeType = Constants.AccountingDocumentBase.GenerateTitleTypes.Consignee;
      var consigneePoAType = Constants.AccountingDocumentBase.GenerateTitleTypes.ConsigneePowerOfAttorney;
      
      if (Sungero.Exchange.PublicFunctions.Module.Remote.IsSbisUniversalTransferDocument970(_obj))
      {
        errorList.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(null, Docflow.Resources.ActionNotAvailableForSbis));
        return errorList;
      }
      
      if (string.IsNullOrEmpty(_obj.BusinessUnit.TIN))
        errorList.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(null, AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_TIN));
      
      if (signatorySetting != null && signatorySetting.JobTitle == null && signatory != null && signatory.JobTitle == null)
        errorList.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(signatoryType, AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_SignatoryJobTitle));
      
      if (consignee != null && consignee != signatory && consignee.JobTitle == null)
        errorList.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(consigneeType, AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_ConsigneeJobTitle));
      
      if (consigneePowerOfAttorney != null)
      {
        var number = string.Empty;
        if (Docflow.FormalizedPowerOfAttorneys.Is(consigneePowerOfAttorney))
          number = Docflow.FormalizedPowerOfAttorneys.As(consigneePowerOfAttorney).UnifiedRegistrationNumber;
        else
          number = consigneePowerOfAttorney.RegistrationNumber;
        
        if (consigneePowerOfAttorney.RegistrationDate == null || string.IsNullOrWhiteSpace(number))
          errorList.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(consigneePoAType, AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_AttorneyRegistration));
        
        if (consigneePowerOfAttorney.OurSignatory != null && consigneePowerOfAttorney.OurSignatory.JobTitle == null)
          errorList.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(consigneePoAType, AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_AttorneyJobTitle));
      }
      
      if (_obj.FormalizedServiceType == FormalizedServiceType.Waybill && !string.IsNullOrWhiteSpace(consigneeOtherReason))
        errorList.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(null, AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_OtherDocument));
      
      if (_obj.FormalizedServiceType != Docflow.AccountingDocumentBase.FormalizedServiceType.Waybill && signatorySetting == null)
        errorList.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(null, AccountingDocumentBases.Resources.SignatureSettingRequiredForBuyerTitle));
      
      if (_obj.FormalizedServiceType == FormalizedServiceType.GeneralTransfer && signatorySetting != null)
      {
        if (signatorySetting.Reason == Docflow.SignatureSetting.Reason.PowerOfAttorney)
        {
          var powerOfAttorney = PowerOfAttorneyBases.As(signatorySetting.Document);
          this.TitleDialogValidatePowerOfAttorney(powerOfAttorney, errorList);
        }
        else if (signatorySetting.Reason == Docflow.SignatureSetting.Reason.FormalizedPoA)
        {
          var formalizedPoA = FormalizedPowerOfAttorneys.As(signatorySetting.Document);
          this.TitleDialogValidateFormalizedPoA(formalizedPoA, errorList);
        }
      }
      
      return errorList;
    }
    
    /// <summary>
    /// Валидация доверенности в диалоге заполнения титула.
    /// </summary>
    /// <param name="powerOfAttorney">Доверенность.</param>
    /// <param name="errorlist">Список ошибок.</param>
    public virtual void TitleDialogValidatePowerOfAttorney(IPowerOfAttorneyBase powerOfAttorney, List<Structures.AccountingDocumentBase.GenerateTitleError> errorlist)
    {
      var validFrom = powerOfAttorney?.ValidFrom ?? powerOfAttorney?.RegistrationDate;
      var registrationNumber = powerOfAttorney?.RegistrationNumber;
      
      if (validFrom == null || string.IsNullOrEmpty(registrationNumber))
        errorlist.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(null, AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_AttorneyRegistrationForSigner));
    }
    
    /// <summary>
    /// Валидация эл. доверенности в диалоге заполнения титула.
    /// </summary>
    /// <param name="formalizedPoA">Эл. доверенность.</param>
    /// <param name="errorlist">Список ошибок.</param>
    public virtual void TitleDialogValidateFormalizedPoA(IFormalizedPowerOfAttorney formalizedPoA, List<Structures.AccountingDocumentBase.GenerateTitleError> errorlist)
    {
      if (formalizedPoA?.ValidFrom == null || string.IsNullOrEmpty(formalizedPoA?.UnifiedRegistrationNumber))
        errorlist.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(null, AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_FormalizedPoARegistrationForSigner));
    }
    
    /// <summary>
    /// Сгенерировать титул продавца в автоматическом режиме.
    /// </summary>
    /// <param name="signatory">Подписывающий.</param>
    [Remote, Public]
    public virtual void GenerateDefaultSellerTitle(Sungero.Company.IEmployee signatory)
    {
      var signatureSetting = Functions.OfficialDocument.GetDefaultSignatureSetting(_obj, signatory);
      var errorList = Functions.AccountingDocumentBase.TitleDialogValidationErrors(_obj, signatory, null, null, null, signatureSetting);
      var validationText = string.Join(Environment.NewLine, errorList.Select(l => l.Text));
      if (errorList.Any())
        throw AppliedCodeException.Create(validationText);
      
      // Полномочия: Лицо, совершившее сделку и отв. за оформление.
      var power = Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_DealAndRegister;
      // Для УКД: Лицо, ответственное за оформление свершившегося события.
      if (_obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer && _obj.IsAdjustment == true)
        power = Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Register;
      
      var sellerTitle = Docflow.Structures.AccountingDocumentBase.SellerTitle.Create();
      sellerTitle.Signatory = signatory;
      sellerTitle.SignatoryPowersBase = SignatureSettings.Info.Properties.Reason.GetLocalizedValue(signatureSetting.Reason);
      sellerTitle.SignatoryPowers = power;
      sellerTitle.SignatureSetting = signatureSetting;
      var taxDocumentClassifier = Exchange.PublicFunctions.Module.Remote.GetTaxDocumentClassifier(_obj);
      sellerTitle.TaxDocumentClassifierCode = taxDocumentClassifier?.TaxDocumentClassifierCode;
      sellerTitle.TaxDocumentClassifierFormatVersion = taxDocumentClassifier?.TaxDocumentClassifierFormatVersion;

      Functions.AccountingDocumentBase.GenerateSellerTitle(_obj, sellerTitle);
    }
    
    /// <summary>
    /// Получить список сотрудников по id.
    /// </summary>
    /// <param name="ids">Список Id.</param>
    /// <returns>Список сотрудников.</returns>
    [Remote]
    public static List<Company.IEmployee> GetEmployeesByIds(List<long> ids)
    {
      return Company.Employees.GetAll(x => ids.Contains(x.Id)).ToList();
    }
    
    /// <summary>
    /// Получить КНД.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>КНД.</returns>
    [Remote, Obsolete("Метод не используется с 05.08.2024 и версии 4.6, используйте метод Exchange.PublicFunctions.Module.Remote.GetTaxDocumentClassifier()")]
    public static string GetTaxDocumentClassifier(IAccountingDocumentBase document)
    {
      if (document.SellerTitleId.HasValue)
      {
        var sellerVersion = document.Versions.Single(v => v.Id == document.SellerTitleId);
        System.IO.Stream body = null;
        // Выключить error-логирование при доступе к зашифрованной версии.
        AccessRights.SuppressSecurityEvents(() => body = sellerVersion.Body.Read());
        return Exchange.PublicFunctions.Module.GetTaxDocumentClassifierByContent(body).TaxDocumentClassifierCode;
      }
      return string.Empty;
    }

    /// <summary>
    /// Проверить, связан ли документ специализированной связью.
    /// </summary>
    /// <returns>True - если связан, иначе - false.</returns>
    [Remote(IsPure = true)]
    public override bool HasSpecifiedTypeRelations()
    {
      var hasSpecifiedTypeRelations = false;
      AccessRights.AllowRead(
        () =>
        {
          hasSpecifiedTypeRelations = AccountingDocumentBases.GetAll().Any(x => Equals(x.Corrected, _obj));
        });
      return base.HasSpecifiedTypeRelations() || hasSpecifiedTypeRelations;
    }
    
    /// <summary>
    /// Получить значение общей суммы для шаблона документа.
    /// </summary>
    /// <param name="document">Финансовый документ.</param>
    /// <returns>Значение общей суммы.</returns>
    [Sungero.Core.Converter("GetTotalAmount")]
    [Public]
    public static string GetTotalAmount(IAccountingDocumentBase document)
    {
      return document.TotalAmount.HasValue ? Docflow.PublicFunctions.Module.GetDecimalValueStringWithTwoDigits(document.TotalAmount.Value) : null;
    }
    
    /// <summary>
    /// Получить значение суммы НДС для шаблона документа.
    /// </summary>
    /// <param name="document">Финансовый документ.</param>
    /// <returns>Значение суммы НДС.</returns>
    [Sungero.Core.Converter("GetVatAmount")]
    [Public]
    public static string GetVatAmount(IAccountingDocumentBase document)
    {
      if (document.VatRate != null && document.VatRate.Sid == Docflow.Constants.Module.VatRateWithoutVatSid)
        return Constants.Module.VatRateWithoutVatDisplayValue;
      
      return document.VatAmount.HasValue ? Docflow.PublicFunctions.Module.GetDecimalValueStringWithTwoDigits(document.VatAmount.Value) : null;
    }
    
    /// <summary>
    /// Получить значение ставки НДС для шаблона документа.
    /// </summary>
    /// <param name="document">Финансовый документ.</param>
    /// <returns>Значение ставки НДС.</returns>
    [Sungero.Core.Converter("GetVatRate")]
    [Public]
    public static string GetVatRate(IAccountingDocumentBase document)
    {
      if (document.VatRate == null)
        return null;
      
      return document.VatRate.Sid == Docflow.Constants.Module.VatRateWithoutVatSid
        ? Sungero.Docflow.AccountingDocumentBases.Resources.VatRateWithoutVatNameForTemplate
        : Sungero.Docflow.AccountingDocumentBases.Resources.VatRateFormatForTemplateFormat(document.VatRate.Rate);
    }
    
    /// <summary>
    /// Получить значение суммы без НДС для шаблона документа.
    /// </summary>
    /// <param name="document">Финансовый документ.</param>
    /// <returns>Значение суммы без НДС.</returns>
    [Sungero.Core.Converter("GetNetAmount")]
    [Public]
    public static string GetNetAmount(IAccountingDocumentBase document)
    {
      return document.NetAmount.HasValue ? Docflow.PublicFunctions.Module.GetDecimalValueStringWithTwoDigits(document.NetAmount.Value) : null;
    }
    
    /// <summary>
    /// Получить для финансового документа сумму прописью с валютой без указания десятичного значения.
    /// </summary>
    /// <param name="accountingDocument">Финансовый документ.</param>
    /// <returns>Сумма прописью с валютой.</returns>
    [Converter("TotalAmountInCurrencyToWordsWithoutDecimalValue")]
    [Public]
    public static string TotalAmountInCurrencyToWordsWithoutDecimalValue(IAccountingDocumentBase accountingDocument)
    {
      if (accountingDocument.TotalAmount == null || accountingDocument.Currency == null)
        return null;
      
      return Docflow.PublicFunctions.Module.GetAmountWithCurrencyInWordsWithoutDecimalValue(accountingDocument.TotalAmount.Value, accountingDocument.Currency);
    }
    
    /// <summary>
    /// Получить для финансового документа ставку и сумму НДС прописью с валютой.
    /// </summary>
    /// <param name="accountingDocument">Финансовый документ.</param>
    /// <returns>Ставка и сумма НДС прописью с валютой.</returns>
    [Converter("VatRateWithVatAmountInCurrencyToWords")]
    [Public]
    public static string VatRateWithVatAmountInCurrencyToWords(IAccountingDocumentBase accountingDocument)
    {
      if (accountingDocument.VatRate == null || accountingDocument.VatAmount == null || accountingDocument.Currency == null)
        return null;
      
      if (accountingDocument.VatRate.Sid == Docflow.Constants.Module.VatRateWithoutVatSid)
        return Sungero.Docflow.AccountingDocumentBases.Resources.VatRateWithoutVatNameForTemplate;
      
      var vatAmountWithCurrency = Docflow.PublicFunctions.Module.GetAmountWithCurrencyInWords(accountingDocument.VatAmount.Value, accountingDocument.Currency);
      return Sungero.Docflow.AccountingDocumentBases.Resources.VatRateWithAmountForTemplateFormat(accountingDocument.VatRate.Rate, vatAmountWithCurrency);
    }
    
  }
}