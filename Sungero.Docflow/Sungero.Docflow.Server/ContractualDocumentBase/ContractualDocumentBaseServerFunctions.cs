using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ContractualDocumentBase;

namespace Sungero.Docflow.Server
{
  partial class ContractualDocumentBaseFunctions
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
    /// Получить права подписания договорных документов.
    /// </summary>
    /// <returns>Список подходящих правил.</returns>
    public override IQueryable<ISignatureSetting> GetSignatureSettingsQuery()
    {
      var totalAmount = _obj.TotalAmount.HasValue ? _obj.TotalAmount.Value : 0d;
      var basedSettings = base.GetSignatureSettingsQuery()
        .Where(s => s.Limit == Docflow.SignatureSetting.Limit.NoLimit ||
               (s.Limit == Docflow.SignatureSetting.Limit.Amount && s.Amount >= totalAmount && 
                Equals(s.Currency, _obj.Currency)));
      
      if (_obj.DocumentKind != null && _obj.DocumentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts)
      {
        var category = Docflow.PublicFunctions.OfficialDocument.GetDocumentGroup(_obj);
        basedSettings = basedSettings
          .Where(s => !s.Categories.Any() || s.Categories.Any(c => Equals(c.Category, category)));
      }
      return basedSettings;
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
          hasSpecifiedTypeRelations = AccountingDocumentBases.GetAll().Any(x => Equals(x.LeadingDocument, _obj));
        });
      return base.HasSpecifiedTypeRelations() || hasSpecifiedTypeRelations;
    }
  }
}