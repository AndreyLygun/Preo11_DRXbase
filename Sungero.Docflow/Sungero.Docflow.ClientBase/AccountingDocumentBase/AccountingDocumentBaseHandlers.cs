using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccountingDocumentBase;

namespace Sungero.Docflow
{
  partial class AccountingDocumentBaseClientHandlers
  {

    public virtual void NetAmountValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue <= 0)
        e.AddError(Sungero.Docflow.Resources.NetAmountMustBePositive);
    }

    public virtual void VatAmountValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue < 0)
        e.AddError(Sungero.Docflow.Resources.VatAmountMustBePositive);
      
      if (e.NewValue > _obj.TotalAmount)
      {
        e.AddError(_obj.Info.Properties.VatAmount, Sungero.Docflow.Resources.VatAmountMustBeMoreTotalAmount, _obj.Info.Properties.TotalAmount);
      }
      
      Functions.AccountingDocumentBase.FillNetAmount(_obj, _obj.TotalAmount, e.NewValue);
    }

    public virtual void VatRateValueInput(Sungero.Docflow.Client.AccountingDocumentBaseVatRateValueInputEventArgs e)
    {
      Functions.AccountingDocumentBase.FillVatAmount(_obj, _obj.TotalAmount, e.NewValue);
      Functions.AccountingDocumentBase.FillNetAmount(_obj, _obj.TotalAmount, _obj.VatAmount);
    }

    public virtual void DateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (e.NewValue != null && e.NewValue < Calendar.SqlMinValue)
        e.AddError(_obj.Info.Properties.Date, Sungero.Docflow.OfficialDocuments.Resources.SetCorrectDate);
    }

    public virtual void CounterpartyValueInput(Sungero.Docflow.Client.AccountingDocumentBaseCounterpartyValueInputEventArgs e)
    {
      this._obj.State.Properties.Counterparty.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void BusinessUnitValueInput(Sungero.Docflow.Client.OfficialDocumentBusinessUnitValueInputEventArgs e)
    {
      base.BusinessUnitValueInput(e);
      this._obj.State.Properties.BusinessUnit.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public virtual void CurrencyValueInput(Sungero.Docflow.Client.AccountingDocumentBaseCurrencyValueInputEventArgs e)
    {
      this._obj.State.Properties.Currency.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      base.Closing(e);
      
      _obj.State.Properties.Counterparty.IsRequired = false;
    }

    public override void DocumentKindValueInput(Sungero.Docflow.Client.OfficialDocumentDocumentKindValueInputEventArgs e)
    {
      base.DocumentKindValueInput(e);
      if (e.NewValue != e.OldValue && e.NewValue != null)
      {
        if (_obj.BusinessUnit != null && _obj.Department != null && _obj.DocumentRegister != null)
        {
          Enumeration settingType;
          if (e.NewValue.NumberingType == Docflow.DocumentKind.NumberingType.Numerable)
            settingType = Docflow.RegistrationSetting.SettingType.Numeration;
          else
            settingType = _obj.RegistrationState == RegistrationState.Registered ?
              Docflow.RegistrationSetting.SettingType.Registration :
              Docflow.RegistrationSetting.SettingType.Reservation;

          var hasRegistrationSetting = Functions.RegistrationSetting.GetAvailableSettingsByParams(_obj, settingType)
            .Any(s => Equals(s.DocumentRegister, _obj.DocumentRegister));
          if (!hasRegistrationSetting)
            e.AddError(AccountingDocumentBases.Resources.NeedCancelRegistrationToChangeKind);
        }
      }
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      if (_obj.IsFormalized == true)
      {
        e.HideAction(_obj.Info.Actions.OpenOriginal);
        e.HideAction(_obj.Info.Actions.CreateDocumentFromVersion);
      }
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);

      if (_obj.ExchangeState == OfficialDocument.ExchangeState.SignRequired && _obj.BuyerTitleId == null &&
          !FinancialArchive.IncomingTaxInvoices.Is(_obj))
        e.AddInformation(Sungero.Docflow.AccountingDocumentBases.Resources.FillBuyerInfoAwaited, AccountingDocumentBases.Info.Actions.FillBuyerInfo);
      
      if (!_obj.State.IsInserted && _obj.IsFormalizedSignatoryEmpty == true)
        e.AddInformation(Sungero.Docflow.AccountingDocumentBases.Resources.FillSellerInfoAwaited, AccountingDocumentBases.Info.Actions.FillSellerInfo);
      
      var isCompany = Sungero.Parties.CompanyBases.Is(_obj.Counterparty) || _obj.Counterparty == null;
      _obj.State.Properties.Contact.IsEnabled = isCompany;
      _obj.State.Properties.CounterpartySignatory.IsEnabled = isCompany;
      
      var canChangeVatAmount = !(_obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnApproval ||
                                 _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.PendingSign ||
                                 _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed);

      if (canChangeVatAmount && !Docflow.PublicFunctions.AccountingDocumentBase.CheckVatAmount(_obj, _obj.VatAmount))
      {
        e.AddWarning(Sungero.Docflow.Resources.VerifyVatAmount);
        _obj.State.Properties.VatAmount.HighlightColor = Colors.Common.LightYellow;
      }
    }

    public override void ShowingSignDialog(Sungero.Domain.Client.ShowingSignDialogEventArgs e)
    {
      base.ShowingSignDialog(e);
      
      if (e.CanApprove)
      {

        if (Sungero.Exchange.PublicFunctions.Module.Remote.IsSbisUniversalTransferDocument970(_obj))
        {
          e.CanApprove = false;
          e.Hint.Add(Docflow.Resources.ActionNotAvailableForSbis);
          return;
        }
        
        try
        {
          Functions.AccountingDocumentBase.GenerateDefaultSellerTitle(_obj);
        }
        catch (AppliedCodeException ex)
        {
          e.CanApprove = false;
          e.Hint.Add(ex.Message);
        }
        catch (Exception ex)
        {
          e.CanApprove = false;
          Logger.ErrorFormat("Error generation default seller title: ", ex);
          e.Hint.Add(Sungero.Docflow.AccountingDocumentBases.Resources.ErrorSellerTitlePropertiesFilling);
        }
        
        try
        {
          Functions.AccountingDocumentBase.GenerateDefaultBuyerTitle(_obj);
        }
        catch (AppliedCodeException ex)
        {
          e.CanApprove = false;
          e.Hint.Add(ex.Message);
        }
        catch (Exception ex)
        {
          e.CanApprove = false;
          Logger.ErrorFormat("Error generation default buyer title: ", ex);
          e.Hint.Add(Sungero.Docflow.AccountingDocumentBases.Resources.ErrorBuyerTitlePropertiesFilling);
        }
      }
    }

    public virtual void IsAdjustmentValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      _obj.State.Properties.Corrected.IsEnabled = e.NewValue == true;
    }

    public virtual void TotalAmountValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue <= 0)
        e.AddError(Docflow.Resources.TotalAmountMustBePositive);
      
      Functions.AccountingDocumentBase.FillVatAmount(_obj, e.NewValue, _obj.VatRate);
      Functions.AccountingDocumentBase.FillNetAmount(_obj, e.NewValue, _obj.VatAmount);
      this._obj.State.Properties.TotalAmount.HighlightColor = Sungero.Core.Colors.Empty;
    }
  }

}