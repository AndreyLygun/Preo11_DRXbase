using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccountingDocumentBase;
using Sungero.Docflow.Structures.AccountingDocumentBase;
using Sungero.Domain.Shared.Validation;

namespace Sungero.Docflow.Client
{
  partial class AccountingDocumentBaseFunctions
  {
    /// <summary>
    /// Диалог заполнения информации о продавце.
    /// </summary>
    public virtual void SellerTitlePropertiesFillingDialog()
    {
      var isDpt = _obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GoodsTransfer;
      var isDprr = _obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.WorksTransfer;
      var isUtdAny = _obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer;
      var isUtdCorrection = isUtdAny && _obj.IsAdjustment == true;
      var isUtdNotCorrection = isUtdAny && _obj.IsAdjustment != true;
      var taxDocumentClassifier = Exchange.PublicFunctions.Module.Remote.GetTaxDocumentClassifier(_obj);
      var isUtd970 = Sungero.Exchange.PublicFunctions.Module.IsUniversalTransferDocumentSeller970(taxDocumentClassifier);
      
      if (!isDpt && !isDprr && !isUtdAny)
        return;
      
      var dialog = Dialogs.CreateInputDialog(AccountingDocumentBases.Resources.PropertiesFillingDialog_SellerTitle);

      if (isDpt)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.SellerGoodsTransfer;
      else if (isDprr)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.SellerWorksTransfer;
      else if (isUtdNotCorrection)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.SellerUniversalTransfer;
      else if (isUtdCorrection)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.SellerUniversalCorrectionTransfer;

      Action<CommonLibrary.InputDialogRefreshEventArgs> refresh = null;
      
      dialog.Text = AccountingDocumentBases.Resources.PropertiesFillingDialog_Text_SellerTitle;
      
      // Поле Подписал.
      var showSaveAndSignButton = false;
      var defaultSignatory = Company.Employees.Null;
      var signedBy = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_SignedBy, true, Company.Employees.Null);
      
      if (Functions.OfficialDocument.Remote.SignatorySettingWithAllUsersExist(_obj))
      {
        if (_obj.OurSignatory != null)
          defaultSignatory = _obj.OurSignatory;
        else if (Company.Employees.Current != null)
          defaultSignatory = Company.Employees.Current;
        
        showSaveAndSignButton = Users.Current != null;
      }
      else
      {
        var signatoriesIds = Functions.OfficialDocument.Remote.GetSignatoriesIds(_obj);
        
        if (signatoriesIds.Any(s => _obj.OurSignatory != null && Equals(s, _obj.OurSignatory.Id)))
          defaultSignatory = _obj.OurSignatory;
        else if (signatoriesIds.Any(s => Company.Employees.Current != null && Equals(s, Company.Employees.Current.Id)))
          defaultSignatory = Company.Employees.Current;
        else if (signatoriesIds.Count() == 1)
          defaultSignatory = Company.PublicFunctions.Module.Remote.GetEmployeeById(signatoriesIds.First());
        
        var defaultEmployees = Functions.AccountingDocumentBase.Remote.GetEmployeesByIds(signatoriesIds);
        
        signedBy.From(defaultEmployees);
        
        showSaveAndSignButton = signatoriesIds.Any(s => Users.Current != null && Equals(s, Users.Current.Id));
      }
      
      // Поле Полномочия.
      CommonLibrary.IDropDownDialogValue hasAuthority = null;
      if (isDpt || isDprr)
        hasAuthority = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority, true, 0)
          .From(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_DealAndRegister,
                AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Register);
      else if (isUtdAny && _obj.IsAdjustment != true)
        hasAuthority = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority, true, 0)
          .From(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_DealAndRegisterAndInvoiceSignatory,
                AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_RegisterAndInvoiceSignatory);
      else if (isUtdAny && _obj.IsAdjustment == true)
      {
        hasAuthority = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority, true, 0)
          .From(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_RegisterAndInvoiceSignatory);
        hasAuthority.IsEnabled = false;
      }

      // Поле Основание.
      INavigationDialogValue<ISignatureSetting> basis = null;
      basis = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_Basis, true, SignatureSettings.Null);

      // Поле Доп. сведения.
      CommonLibrary.IMultilineStringDialogValue signerAdditionalInfo = null;
      if (isUtd970)
      {
        signerAdditionalInfo = dialog.AddMultilineString(AccountingDocumentBases.Resources.PropertiesFillingDialog_SignerAdditionalInfo, false);
        hasAuthority.IsVisible = false;
      }
      
      CommonLibrary.CustomDialogButton saveAndSignButton = null;
      if (showSaveAndSignButton)
        saveAndSignButton = dialog.Buttons.AddCustom(AccountingDocumentBases.Resources.PropertiesFillingDialog_SaveAndSign);
      
      var saveButton = dialog.Buttons.AddCustom(AccountingDocumentBases.Resources.PropertiesFillingDialog_Save);
      dialog.Buttons.Default = saveAndSignButton ?? saveButton;
      var cancelButton = dialog.Buttons.AddCancel();
      
      IQueryable<ISignatureSetting> basisValues = null;
      List<ISignatureSetting> settings = null;
      
      signedBy.SetOnValueChanged(
        (sc) =>
        {
          settings = Functions.OfficialDocument.Remote.GetSignatureSettings(_obj, sc.NewValue);
          if (basis != null)
          {
            basisValues = Functions.OfficialDocument.Remote.GetSignatureSettingsWithCertificateByEmployee(_obj, sc.NewValue);
            basis.From(basisValues);
            basis.IsEnabled = sc.NewValue != null;
            basis.IsRequired = sc.NewValue != null;
            basis.Value = _obj.OurSigningReason != null && basisValues.Contains(_obj.OurSigningReason)
              ? _obj.OurSigningReason
              : Functions.OfficialDocument.Remote.GetDefaultSignatureSetting(_obj, sc.NewValue);
          }
        });
      signedBy.Value = defaultSignatory;
      
      dialog.SetOnRefresh(refresh);
      dialog.SetOnButtonClick(
        (b) =>
        {
          if (b.Button != saveAndSignButton && b.Button != saveButton)
            return;
          
          if (!b.IsValid)
            return;
          
          var dialogProperties = Structures.AccountingDocumentBase.TitleGenerationDialogProperties.Create();
          dialogProperties.Signatory = signedBy.Value;
          dialogProperties.SignatorySetting = basis?.Value;
          dialogProperties.SignerAdditionalInfo = signerAdditionalInfo?.Value;
          
          var errorList = this.TitleDialogValidationErrors(dialogProperties);
          foreach (var errors in errorList.GroupBy(e => e.Text))
          {
            var controls = new List<CommonLibrary.IDialogControl>();
            foreach (var error in errors)
            {
              if (error.Type == Constants.AccountingDocumentBase.GenerateTitleTypes.Signatory)
                controls.Add(basis);
            }

            b.AddError(errors.Key, controls.ToArray());
          }

          if (!b.IsValid)
            return;
          
          var basisValue = basis != null
            ? SignatureSettings.Info.Properties.Reason.GetLocalizedValue(basis.Value.Reason)
            : string.Empty;
          var hasAuthorityValue = hasAuthority?.Value;
          var signatureSetting = basis?.Value;
          var additionalInfo = signerAdditionalInfo?.Value;
          var title = SellerTitle.Create(signedBy.Value, basisValue, hasAuthorityValue, signatureSetting, additionalInfo,
                                         taxDocumentClassifier?.TaxDocumentClassifierCode,
                                         taxDocumentClassifier?.TaxDocumentClassifierFormatVersion);
          
          this.GenerateSellerTitle(title, b, b.Button == saveAndSignButton);
        });
      
      dialog.Show();
    }

    /// <summary>
    /// Сгенерировать титул продавца.
    /// </summary>
    /// <param name="title">Параметры титула для генерации.</param>
    /// <param name="buttonClickEventArgs">Аргументы события нажатия на кнопку диалога.</param>
    /// <param name="needSign">Признак необходимости утвердить документ с приложениями.</param>
    private void GenerateSellerTitle(ISellerTitle title, InputDialogButtonClickEventArgs buttonClickEventArgs, bool needSign)
    {
      try
      {
        Functions.AccountingDocumentBase.Remote.GenerateSellerTitle(_obj, title);
      }
      catch (AppliedCodeException ex)
      {
        buttonClickEventArgs.AddError(ex.Message);
        return;
      }
      catch (ValidationException ex)
      {
        buttonClickEventArgs.AddError(ex.Message);
        return;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Error generation title: ", ex);
        buttonClickEventArgs.AddError(Sungero.Docflow.AccountingDocumentBases.Resources.ErrorSellerTitlePropertiesFilling);
        return;
      }

      if (!needSign)
        return;
      
      try
      {
        Functions.Module.ApproveWithAddenda(_obj, null, null, null, false, true, string.Empty);
      }
      catch (Exception ex)
      {
        buttonClickEventArgs.AddError(ex.Message);
      }
    }

    /// <summary>
    /// Диалог заполнения информации о покупателе.
    /// </summary>
    public virtual void BuyerTitlePropertiesFillingDialog()
    {
      var taxDocumentClassifier = Exchange.PublicFunctions.Module.Remote.GetTaxDocumentClassifier(_obj);
      var taxDocumentClassifierCode = taxDocumentClassifier?.TaxDocumentClassifierCode;
      var isAct = _obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.Act;
      var isTorg12 = _obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.Waybill;
      var isDpt = _obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GoodsTransfer &&
        taxDocumentClassifierCode == Exchange.PublicConstants.Module.TaxDocumentClassifier.GoodsTransferSeller;
      var isDprr = _obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.WorksTransfer &&
        taxDocumentClassifierCode == Exchange.PublicConstants.Module.TaxDocumentClassifier.WorksTransferSeller;
      var isUtdAny = _obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer;
      var isUcd = isUtdAny && _obj.IsAdjustment == true;
      var isUtd = isUtdAny && _obj.IsAdjustment != true;
      var isOldUcd = isUcd && (taxDocumentClassifierCode == Exchange.PublicConstants.Module.TaxDocumentClassifier.UniversalCorrectionDocumentSeller);
      var isUtd970 = Sungero.Exchange.PublicFunctions.Module.IsUniversalTransferDocumentSeller970(taxDocumentClassifier);
      var isWaybill = isTorg12 || isDpt;
      var isContractStatement = isAct || isDprr;
      
      if (!isUtdAny && !isWaybill && !isContractStatement)
        return;
      
      var dialog = Dialogs.CreateInputDialog(AccountingDocumentBases.Resources.PropertiesFillingDialog_Title);

      if (isTorg12)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.Waybill;
      else if (isAct)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.ContractStatement;
      else if (isDpt)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.GoodsTransfer;
      else if (isDprr)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.WorksTransfer;
      else if (isUtd)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.UniversalTransfer;
      else if (isUcd)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.UniversalCorrectionTransfer;

      Action<CommonLibrary.InputDialogRefreshEventArgs> refresh = null;

      var dialogText = string.Empty;
      
      if (isUtd)
        dialogText = AccountingDocumentBases.Resources.PropertiesFillingDialog_Text_Universal;

      if (isUcd)
        dialogText = AccountingDocumentBases.Resources.PropertiesFillingDialog_Text_UniversalCorrection;

      if (isWaybill)
        dialogText = AccountingDocumentBases.Resources.PropertiesFillingDialog_Text_Waybill;

      if (isContractStatement)
        dialogText = AccountingDocumentBases.Resources.PropertiesFillingDialog_Text_Act;
      
      dialog.Text = dialogText;
      
      // Поле Подписал.
      var showSaveAndSignButton = false;
      var defaultSignatory = Company.Employees.Null;
      var signatory = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_SignedBy, true, defaultSignatory);

      if (Functions.OfficialDocument.Remote.SignatorySettingWithAllUsersExist(_obj))
      {
        if (_obj.OurSignatory != null)
          defaultSignatory = _obj.OurSignatory;
        else if (Company.Employees.Current != null)
          defaultSignatory = Company.Employees.Current;
        
        showSaveAndSignButton = Users.Current != null;
      }
      else
      {
        var signatoriesIds = Functions.OfficialDocument.Remote.GetSignatoriesIds(_obj);
        
        if (signatoriesIds.Any(s => _obj.OurSignatory != null && Equals(s, _obj.OurSignatory.Id)))
          defaultSignatory = _obj.OurSignatory;
        else if (signatoriesIds.Any(s => Company.Employees.Current != null && Equals(s, Company.Employees.Current.Id)))
          defaultSignatory = Company.Employees.Current;
        else if (signatoriesIds.Count() == 1)
          defaultSignatory = Company.PublicFunctions.Module.Remote.GetEmployeeById(signatoriesIds.First());
        
        var defaultEmployees = Functions.AccountingDocumentBase.Remote.GetEmployeesByIds(signatoriesIds);
        
        signatory.From(defaultEmployees);
        
        showSaveAndSignButton = signatoriesIds.Any(s => Users.Current != null && Equals(s, Users.Current.Id));
      }
      
      // Поле Полномочия.
      CommonLibrary.IDropDownDialogValue hasAuthority = null;
      if (!isAct && !isTorg12)
      {
        hasAuthority = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority, true, 0);
        if (isOldUcd)
        {
          hasAuthority.From(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Register);
          hasAuthority.IsEnabled = false;
        }
        else if (isUcd)
          hasAuthority.From(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Register,
                            AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_SignSchf,
                            AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_SignSchfAndRegister,
                            AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Other);
        else
          hasAuthority.From(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Register,
                            AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Deal,
                            AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_DealAndRegister);
      }

      // Поле Основание.
      INavigationDialogValue<ISignatureSetting> basis = null;
      if (!isTorg12)
        basis = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_Basis, true, SignatureSettings.Null);
      
      // Поле Доп. сведения.
      CommonLibrary.IMultilineStringDialogValue signerAdditionalInfo = null;
      if (isUtd970)
      {
        signerAdditionalInfo = dialog.AddMultilineString(AccountingDocumentBases.Resources.PropertiesFillingDialog_SignerAdditionalInfo, false);
        hasAuthority.IsVisible = false;
      }

      // Дата подписания (Дата согласования, если УКД).
      var signingLabel = isUcd ?
        AccountingDocumentBases.Resources.PropertiesFillingDialog_DateApproving :
        AccountingDocumentBases.Resources.PropertiesFillingDialog_AcceptanceDate;
      var signingDate = dialog.AddDate(signingLabel, true, Calendar.UserToday);
      
      // Результат и Разногласия.
      CommonLibrary.IDropDownDialogValue result = null;
      CommonLibrary.IMultilineStringDialogValue disagreement = null;
      if (!isUcd && !isContractStatement)
      {
        var values = new List<string>()
        {
          AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_Accepted,
          AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_AcceptedWithDisagreement
        };
        // Для исправлений УПД по приказу 970 нельзя указать код итога 3.
        var isUtd970Revision = _obj.IsRevision == true && isUtd970;
        if (isUtd && !isUtd970Revision)
          values.Add(AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_NotAccepted);
        
        result = dialog
          .AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_Result, true, 0)
          .From(values.ToArray());
        disagreement = dialog.AddMultilineString(AccountingDocumentBases.Resources.PropertiesFillingDialog_Disagreement, false);
      }
      
      // Поле Результат для УКД.
      CommonLibrary.IDropDownDialogValue adjustmentResult = null;
      if (isUcd)
      {
        adjustmentResult = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_Result, true, 0)
          .From(AccountingDocumentBases.Resources.PropertiesFillingDialog_AdjustmentResult_AgreedChanges);
        adjustmentResult.IsEnabled = false;
      }
      
      // Груз принял получатель груза.
      CommonLibrary.IBooleanDialogValue isSameConsignee = null;
      INavigationDialogValue<Company.IEmployee> consignee = null;
      CommonLibrary.IDropDownDialogValue consigneeBasis = null;
      INavigationDialogValue<IPowerOfAttorney> consigneeAttorney = null;
      CommonLibrary.IStringDialogValue consigneeDocument = null;
      if (isWaybill || isUtd)
      {
        isSameConsignee = dialog.AddBoolean(AccountingDocumentBases.Resources.PropertiesFillingDialog_SameConsignee, true);
        consignee = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_Consignee, false, Company.Employees.Null)
          .Where(x => Equals(x.Status, CoreEntities.DatabookEntry.Status.Active));
        consigneeBasis = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_ConsigneeBasis, false, 0);
        consigneeAttorney = dialog.AddSelect(PowerOfAttorneys.Info.LocalizedName, false, PowerOfAttorneys.Null);
        consigneeDocument = dialog.AddString(AccountingDocumentBases.Resources.PropertiesFillingDialog_Document, false);
        consigneeDocument.MaxLength(Constants.AccountingDocumentBase.PowersBaseConsigneeMaxLength);
      }

      CommonLibrary.CustomDialogButton saveAndSignButton = null;
      
      if (showSaveAndSignButton)
        saveAndSignButton = dialog.Buttons.AddCustom(AccountingDocumentBases.Resources.PropertiesFillingDialog_SaveAndSign);
      
      var saveButton = dialog.Buttons.AddCustom(AccountingDocumentBases.Resources.PropertiesFillingDialog_Save);
      dialog.Buttons.Default = saveAndSignButton ?? saveButton;
      var cancelButton = dialog.Buttons.AddCancel();
      
      IQueryable<ISignatureSetting> settings = null;
      IPowerOfAttorney[] consigneePowerOfAttorneyValues = null;
      
      signatory.SetOnValueChanged(
        (sc) =>
        {
          if (basis != null)
          {
            settings = Functions.OfficialDocument.Remote.GetSignatureSettingsWithCertificateByEmployee(_obj, sc.NewValue);
            basis.From(settings);
            basis.IsEnabled = sc.NewValue != null;
            basis.IsRequired = sc.NewValue != null;
            basis.Value = _obj.OurSigningReason != null && settings.Contains(_obj.OurSigningReason)
              ? _obj.OurSigningReason
              : Functions.OfficialDocument.Remote.GetDefaultSignatureSetting(_obj, sc.NewValue);
          }
        });
      signatory.Value = defaultSignatory;
      
      Action<CommonLibrary.InputDialogValueChangedEventArgs<string>> consigneeBasisChanged =
        cb =>
      {
        var basisIsDuties = cb.NewValue == SignatureSettings.Info.Properties.Reason.GetLocalizedValue(SignatureSetting.Reason.Duties);
        var basisIsAttorney = cb.NewValue == SignatureSettings.Info.Properties.Reason.GetLocalizedValue(SignatureSetting.Reason.PowerOfAttorney);
        var basisIsOther = cb.NewValue == SignatureSettings.Info.Properties.Reason.GetLocalizedValue(SignatureSetting.Reason.Other);
        
        if (consigneeAttorney != null)
        {
          consigneeAttorney.IsVisible = !basisIsOther;
          consigneeAttorney.IsRequired = basisIsAttorney;
          consigneeAttorney.IsEnabled = basisIsAttorney;
          if (!consigneeAttorney.IsEnabled)
            consigneeAttorney.Value = null;
          else
            consigneeAttorney.Value = consigneePowerOfAttorneyValues.Length == 1 ? consigneePowerOfAttorneyValues.SingleOrDefault() : null;
        }
        
        if (consigneeDocument != null)
        {
          consigneeDocument.IsVisible = basisIsOther;
          consigneeDocument.IsRequired = basisIsOther;
          if (!consigneeDocument.IsVisible)
            consigneeDocument.Value = null;
        }
      };
      
      if (consignee != null)
        consignee.SetOnValueChanged(
          ce =>
          {
            var cbValues = new List<string>();
            if (ce.NewValue != null)
            {
              cbValues.Add(SignatureSettings.Info.Properties.Reason.GetLocalizedValue(SignatureSetting.Reason.Duties));
              cbValues.Add(SignatureSettings.Info.Properties.Reason.GetLocalizedValue(SignatureSetting.Reason.PowerOfAttorney));
              if (!isTorg12)
                cbValues.Add(SignatureSettings.Info.Properties.Reason.GetLocalizedValue(SignatureSetting.Reason.Other));

              consigneePowerOfAttorneyValues = Functions.PowerOfAttorney.Remote.GetActivePowerOfAttorneys(ce.NewValue, signingDate.Value).ToArray();
            }
            else
              consigneePowerOfAttorneyValues = new IPowerOfAttorney[0];
            
            consigneeBasis.From(cbValues.ToArray());
            consigneeAttorney.From(consigneePowerOfAttorneyValues);

            consigneeBasis.Value = cbValues.OrderBy(v => v != consigneeBasis.Value).FirstOrDefault();
            consigneeBasisChanged.Invoke(new CommonLibrary.InputDialogValueChangedEventArgs<string>(null, consigneeBasis.Value));
          });
      
      if (consigneeBasis != null)
        consigneeBasis.SetOnValueChanged(consigneeBasisChanged);
      
      signingDate.SetOnValueChanged(
        sd =>
        {
          if (consigneeAttorney != null)
          {
            if (sd.NewValue.HasValue && consignee.Value != null)
              consigneePowerOfAttorneyValues = Functions.PowerOfAttorney.Remote.GetActivePowerOfAttorneys(consignee.Value, signingDate.Value).ToArray();
            else
              consigneePowerOfAttorneyValues = new IPowerOfAttorney[0];

            consigneeAttorney.From(consigneePowerOfAttorneyValues);
            consigneeBasisChanged.Invoke(new CommonLibrary.InputDialogValueChangedEventArgs<string>(null, consigneeBasis.Value));
          }
        });
      
      if (result != null)
        result.SetOnValueChanged(
          r =>
          {
            if (string.Equals(r.NewValue, AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_Accepted) && disagreement != null)
              disagreement.Value = string.Empty;
          });

      refresh = (r) =>
      {
        if (disagreement != null)
          disagreement.IsEnabled = string.Equals(result.Value, AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_AcceptedWithDisagreement) ||
            string.Equals(result.Value, AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_NotAccepted);
        
        if (isSameConsignee != null)
        {
          var needConsignee = !isSameConsignee.Value.Value;
          consignee.IsEnabled = needConsignee;
          consignee.IsRequired = needConsignee;
          consigneeBasis.IsEnabled = needConsignee && consignee.Value != null;
          consigneeBasis.IsRequired = needConsignee && consignee.Value != null;

          if (!needConsignee)
          {
            consignee.Value = Company.Employees.Null;
            consigneeBasis.Value = string.Empty;
          }
        }
      };
      
      dialog.SetOnRefresh(refresh);
      dialog.SetOnButtonClick(
        (b) =>
        {
          if (b.Button != saveAndSignButton && b.Button != saveButton)
            return;
          
          if (!b.IsValid)
            return;
          
          var notSameConsignee = isSameConsignee != null && isSameConsignee.Value != true;
          var consigneePowerOfAttorneyValue = notSameConsignee ? consigneeAttorney?.Value : null;
          var consigneeOtherReasonValue = notSameConsignee ? consigneeDocument?.Value : string.Empty;

          var dialogProperties = Structures.AccountingDocumentBase.TitleGenerationDialogProperties.Create();
          dialogProperties.Signatory = signatory.Value;
          dialogProperties.Consignee = consignee?.Value;
          dialogProperties.ConsigneePowerOfAttorney = consigneePowerOfAttorneyValue;
          dialogProperties.ConsigneeOtherReason = consigneeOtherReasonValue;
          dialogProperties.SignatorySetting = basis?.Value;
          dialogProperties.SignerAdditionalInfo = signerAdditionalInfo?.Value;
          dialogProperties.NeedValidatePowersBaseConsignee = notSameConsignee;
          
          var errorList = this.TitleDialogValidationErrors(dialogProperties);
          
          var controlErrorTypes = new Dictionary<string, IDialogControl>
          {
            { Constants.AccountingDocumentBase.GenerateTitleTypes.Signatory, signatory },
            { Constants.AccountingDocumentBase.GenerateTitleTypes.Consignee, consignee },
            { Constants.AccountingDocumentBase.GenerateTitleTypes.ConsigneePowerOfAttorney, consigneeAttorney },
            { Constants.AccountingDocumentBase.GenerateTitleTypes.SignatoryPowersBase, basis },
            { Constants.AccountingDocumentBase.GenerateTitleTypes.SignerAdditionalInfo, signerAdditionalInfo }
          };
          foreach (var errors in errorList.GroupBy(e => e.Text))
          {
            var controls = errors.Where(x => x.Type != null).Select(error => controlErrorTypes.GetValueOrDefault(error.Type, null));
            b.AddError(errors.Key, controls.Where(x => x != null).ToArray());
          }
          
          if (!b.IsValid)
            return;
          
          var basisValue = Functions.Module.GetSigningReason(basis?.Value);
          var consigneeBasisValue = string.Empty;
          Company.IEmployee consigneeValue = null;
          if (isSameConsignee != null)
          {
            consigneeBasisValue = isSameConsignee.Value == true ? basisValue : consigneeBasis.Value;
            consigneeValue = isSameConsignee.Value == true ? signatory?.Value : consignee?.Value;
          }
          var title = BuyerTitle.Create();
          title.ActOfDisagreement = disagreement?.Value;
          title.Signatory = signatory.Value;
          title.SignatoryPowersBase = basisValue;
          title.SignatureSetting = basis?.Value;
          title.Consignee = consigneeValue;
          title.ConsigneePowersBase = consigneeBasisValue;
          title.SignerAdditionalInfo = signerAdditionalInfo?.Value;
          FillBuyerTitleAcceptanceStatus(result, title);
          title.SignatoryPowers = hasAuthority?.Value;
          title.AcceptanceDate = signingDate.Value;
          title.ConsigneePowerOfAttorney = consigneePowerOfAttorneyValue;
          title.ConsigneeOtherReason = consigneeOtherReasonValue;
          title.TaxDocumentClassifierCode = taxDocumentClassifierCode;
          var formatVersion = taxDocumentClassifier?.TaxDocumentClassifierFormatVersion;
          title.TaxDocumentClassifierFormatVersion = formatVersion;
          
          this.GenerateBuyerTitle(title, b, b.Button == saveAndSignButton);
        });
      dialog.Show();
    }

    /// <summary>
    /// Сгенерировать титул покупателя.
    /// </summary>
    /// <param name="title">Параметры титула для генерации.</param>
    /// <param name="buttonClickEventArgs">Аргументы события нажатия на кнопку диалога.</param>
    /// <param name="needSign">Признак необходимости утвердить документ с приложениями.</param>
    private void GenerateBuyerTitle(IBuyerTitle title, InputDialogButtonClickEventArgs buttonClickEventArgs, bool needSign)
    {
      try
      {
        Functions.AccountingDocumentBase.Remote.GenerateAnswer(_obj, title, false);
      }
      catch (AppliedCodeException ex)
      {
        buttonClickEventArgs.AddError(ex.Message);
        return;
      }
      catch (ValidationException ex)
      {
        buttonClickEventArgs.AddError(ex.Message);
        return;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Error generation title: ", ex);
        buttonClickEventArgs.AddError(Sungero.Docflow.AccountingDocumentBases.Resources.ErrorBuyerTitlePropertiesFilling);
        return;
      }

      if (!needSign)
        return;

      try
      {
        Functions.Module.ApproveWithAddenda(_obj, null, null, null, false, true, string.Empty);
      }
      catch (Exception ex)
      {
        buttonClickEventArgs.AddError(ex.Message);
      }
    }

    /// <summary>
    /// Заполнить результат в параметрах для формирования титула покупателя.
    /// </summary>
    /// <param name="result">Контрол с результатом.</param>
    /// <param name="title">Параметры титула.</param>
    private static void FillBuyerTitleAcceptanceStatus(IDropDownDialogValue result, IBuyerTitle title)
    {
      if (result == null || !result.IsVisible || string.Equals(result.Value, AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_Accepted))
        title.BuyerAcceptanceStatus = Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Accepted;
      else if (string.Equals(result.Value, AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_AcceptedWithDisagreement))
        title.BuyerAcceptanceStatus = Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.PartiallyAccepted;
      else if (string.Equals(result.Value, AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_NotAccepted))
        title.BuyerAcceptanceStatus = Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Rejected;
      else
        title.BuyerAcceptanceStatus = Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Accepted;
    }
    
    /// <summary>
    /// Валидация диалога заполнения титула.
    /// </summary>
    /// <param name="dialogProperties">Свойства диалога.</param>
    /// <returns>Список ошибок.</returns>
    public virtual List<Structures.AccountingDocumentBase.GenerateTitleError> TitleDialogValidationErrors(Structures.AccountingDocumentBase.ITitleGenerationDialogProperties dialogProperties)
    {
      var errorList = Functions.AccountingDocumentBase.Remote.TitleDialogValidationErrors(_obj, dialogProperties.Signatory,
                                                                                          dialogProperties.Consignee,
                                                                                          dialogProperties.ConsigneePowerOfAttorney,
                                                                                          dialogProperties.ConsigneeOtherReason,
                                                                                          dialogProperties.SignatorySetting);
      this.ValidateSignerAdditionalInfo(dialogProperties.SignerAdditionalInfo, errorList);
      
      if (dialogProperties.NeedValidatePowersBaseConsignee && dialogProperties.SignatorySetting != null)
        this.ValidatePowersBaseConsignee(dialogProperties.SignatorySetting, errorList);
      
      return errorList;
    }
    
    /// <summary>
    /// Валидация дополнительных сведений подписанта.
    /// </summary>
    /// <param name="additionalInfo">Дополнительные сведения.</param>
    /// <param name="errorList">Список ошибок.</param>
    private void ValidateSignerAdditionalInfo(string additionalInfo, List<Structures.AccountingDocumentBase.GenerateTitleError> errorList)
    {
      if (string.IsNullOrWhiteSpace(additionalInfo) || additionalInfo.Length <= Constants.AccountingDocumentBase.SignerAdditionalInfoMaxLength)
        return;
      
      var error = string.Format(AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_SignerAdditionalInfoGreaterMaxLength,
                                Constants.AccountingDocumentBase.SignerAdditionalInfoMaxLength);
      errorList.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(Constants.AccountingDocumentBase.GenerateTitleTypes.SignerAdditionalInfo, error));
    }

    /// <summary>
    /// Валидация основания грузополучателя.
    /// </summary>
    /// <param name="basis">Основание.</param>
    /// <param name="errorList">Список ошибок.</param>
    private void ValidatePowersBaseConsignee(ISignatureSetting basis, List<GenerateTitleError> errorList)
    {
      var powersBase = Functions.Module.GetSigningReason(basis);
      if (string.IsNullOrEmpty(powersBase) || powersBase.Length <= Constants.AccountingDocumentBase.PowersBaseConsigneeMaxLength)
        return;
      
      var error = string.Format(AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_ConsigneePowersBaseGreaterMaxLength,
                                Constants.AccountingDocumentBase.PowersBaseConsigneeMaxLength);
      errorList.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(Constants.AccountingDocumentBase.GenerateTitleTypes.SignatoryPowersBase, error));
    }

    /// <summary>
    /// Заполнение значений доверенности и документа с основанием "Другой документ".
    /// </summary>
    /// <param name="newValue">Основание.</param>
    /// <param name="powerOfAttorney">Доверенность.</param>
    /// <param name="basisDocument">Документ основания.</param>
    /// <param name="powerOfAttorneyValues">Список доверенностей.</param>
    /// <param name="basisDocumentValues">Список документов основания.</param>
    private static void FillBasisDocuments(string newValue,
                                           INavigationDialogValue<IPowerOfAttorney> powerOfAttorney,
                                           CommonLibrary.IDropDownDialogValue basisDocument,
                                           IPowerOfAttorney[] powerOfAttorneyValues,
                                           string[] basisDocumentValues)
    {
      var basisIsAttorney = newValue == SignatureSettings.Info.Properties.Reason.GetLocalizedValue(SignatureSetting.Reason.PowerOfAttorney);
      var basisIsOther = newValue == SignatureSettings.Info.Properties.Reason.GetLocalizedValue(SignatureSetting.Reason.Other);
      
      if (powerOfAttorney != null)
      {
        powerOfAttorney.IsVisible = !basisIsOther;
        powerOfAttorney.IsRequired = basisIsAttorney;
        powerOfAttorney.IsEnabled = basisIsAttorney;
        if (!powerOfAttorney.IsEnabled)
          powerOfAttorney.Value = null;
        else
          powerOfAttorney.Value = powerOfAttorneyValues.Length == 1 ? powerOfAttorneyValues.Single() : null;
      }
      if (basisDocument != null)
      {
        basisDocument.IsVisible = basisIsOther;
        basisDocument.IsRequired = basisIsOther;
        if (!basisDocument.IsVisible)
          basisDocument.Value = null;
        else
          basisDocument.Value = basisDocumentValues.Length == 1 ? basisDocumentValues.Single() : null;
      }
    }
    
    /// <summary>
    /// Генерировать титул покупателя в автоматическом режиме.
    /// </summary>
    [Public]
    public virtual void GenerateDefaultBuyerTitle()
    {
      if (_obj.ExchangeState == OfficialDocument.ExchangeState.SignRequired && _obj.BuyerTitleId == null)
        Docflow.PublicFunctions.AccountingDocumentBase.Remote.GenerateDefaultAnswer(_obj, Company.Employees.Current, false);
    }
    
    /// <summary>
    /// Генерировать титул продавца в автоматическом режиме.
    /// </summary>
    [Public]
    public virtual void GenerateDefaultSellerTitle()
    {
      if (_obj.IsFormalized == true && _obj.SellerTitleId != null && !FinancialArchive.PublicFunctions.Module.Remote.HasSellerSignatoryInfo(_obj))
      {
        Docflow.PublicFunctions.AccountingDocumentBase.Remote.GenerateDefaultSellerTitle(_obj, Sungero.Company.Employees.Current);
      }
    }
    
    /// <summary>
    /// Дополнительное условие доступности действия "Сменить тип".
    /// </summary>
    /// <returns>True - если действие "Сменить тип" доступно, иначе - false.</returns>
    public override bool CanChangeDocumentType()
    {
      return _obj.IsFormalized != true && base.CanChangeDocumentType();
    }
  }
}