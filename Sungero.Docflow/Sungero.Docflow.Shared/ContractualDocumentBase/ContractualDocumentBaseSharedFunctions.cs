using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ContractualDocumentBase;

namespace Sungero.Docflow.Shared
{
  partial class ContractualDocumentBaseFunctions
  {

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
    /// Установить обязательность свойств в зависимости от заполненных данных.
    /// </summary>
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      
      // Изменить обязательность полей в зависимости от того, программная или визуальная работа.
      var isVisualMode = ((Domain.Shared.IExtendedEntity)_obj).Params.ContainsKey(Sungero.Docflow.PublicConstants.OfficialDocument.IsVisualModeParamName);

      // При визуальной работе обязательность содержания и контрагента как в Contract.
      // Обязательность категории вычисляется по стандартной логике.
      // При программной работе содержание, контрагента и категорию делаем необязательными.
      // Чтобы сбросить обязательность, если она изменилась в вызове текущего метода в базовой сущности.
      _obj.State.Properties.Subject.IsRequired = isVisualMode;
      _obj.State.Properties.Counterparty.IsRequired = isVisualMode;
    }
    
    /// <summary>
    /// Сменить доступность реквизитов документа.
    /// </summary>
    /// <param name="isEnabled">True, если свойства должны быть доступны.</param>
    /// <param name="isRepeatRegister">Перерегистрация.</param>
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);
      
      var documentParams = ((Sungero.Domain.Shared.IExtendedEntity)_obj).Params;
      var isChangeTypeExchangeDocument = documentParams.ContainsKey(Constants.ContractualDocumentBase.IsChangeTypeExchangeDocument) 
        ? (bool)documentParams[Constants.ContractualDocumentBase.IsChangeTypeExchangeDocument] : false;
      
      var enabledState = !(_obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnApproval ||
                           _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.PendingSign ||
                           _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed);
      
      _obj.State.Properties.Currency.IsEnabled = enabledState || isChangeTypeExchangeDocument;
      _obj.State.Properties.TotalAmount.IsEnabled = enabledState || isChangeTypeExchangeDocument;
      _obj.State.Properties.VatRate.IsEnabled = enabledState || isChangeTypeExchangeDocument;
      _obj.State.Properties.VatAmount.IsEnabled = enabledState || isChangeTypeExchangeDocument;
    }
    
    /// <summary>
    /// Сменить доступность поля Контрагент. Доступность зависит от статуса.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    /// <param name="counterpartyCodeInNumber">Признак вхождения кода контрагента в формат номера. TRUE - входит.</param>
    /// <param name="enabledState">Признак доступности поля в зависимости от статуса.</param>
    public override void ChangeCounterpartyPropertyAccess(bool isEnabled, bool counterpartyCodeInNumber, bool enabledState)
    {
      _obj.State.Properties.Counterparty.IsEnabled = isEnabled && !counterpartyCodeInNumber && (enabledState || _obj.Counterparty == null);
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
    
    #region Интеллектуальная обработка
    
    /// <summary>
    /// Сменить доступность поля Контрагент.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    public override void ChangeCounterpartyPropertyAccess(bool isEnabled)
    {
      _obj.State.Properties.Counterparty.IsEnabled = isEnabled;
    }
    
    [Public]
    public override bool HasEmptyRequiredProperties()
    {
      return string.IsNullOrEmpty(_obj.Subject) || _obj.Counterparty == null;
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
    
    #endregion
  }
}