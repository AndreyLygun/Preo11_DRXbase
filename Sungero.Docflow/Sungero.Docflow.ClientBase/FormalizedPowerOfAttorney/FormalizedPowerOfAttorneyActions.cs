using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FormalizedPowerOfAttorney;

namespace Sungero.Docflow.Client
{
  partial class FormalizedPowerOfAttorneyCollectionActions
  {
    public override void OpenDocumentEdit(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.OpenDocumentEdit(e);
    }

    public override bool CanOpenDocumentEdit(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

  }

  partial class FormalizedPowerOfAttorneyVersionsActions
  {
    public override void EditVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.EditVersion(e);
    }

    public override bool CanEditVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override bool CanImportVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override void ImportVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.ImportVersion(e);
    }

    public override void CreateDocumentFromVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.CreateDocumentFromVersion(e);
    }

    public override bool CanCreateDocumentFromVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override void CreateVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.CreateVersion(e);
    }

    public override bool CanCreateVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

  }

  partial class FormalizedPowerOfAttorneyActions
  {
    public override void ChangeManyRepresentatives(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ChangeManyRepresentatives(e);
    }

    public override bool CanChangeManyRepresentatives(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      if (_obj.FtsListState != null &&
          _obj.FtsListState != Docflow.FormalizedPowerOfAttorney.FtsListState.Rejected)
        return false;
      
      if (base.CanChangeManyRepresentatives(e))
        return true;
      
      var isRequisitiesChanging = false;
      e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.RepeatRegister, out isRequisitiesChanging);
      
      var isNotRegistered = _obj.RegistrationState == null || _obj.RegistrationState == RegistrationState.NotRegistered;
      
      var canEditCard = _obj.AccessRights.CanUpdate() &&
        Functions.Module.IsLockedByMe(_obj);
      
      return canEditCard &&
        Functions.FormalizedPowerOfAttorney.IsImported(_obj) &&
        (isRequisitiesChanging || isNotRegistered);
    }

    public virtual void OpenInFtsRegistry(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Sungero.Docflow.PublicFunctions.Module.IsPoAKonturLicenseEnable())
      {
        Dialogs.NotifyMessage(FormalizedPowerOfAttorneys.Resources.NoLicenseToPowerOfAttorneyKontur);
        return;
      }
      
      if (!Functions.FormalizedPowerOfAttorney.CheckSearchData(_obj))
      {
        e.AddError(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.OpenInFtsRegistryErrorMessage);
        return;
      }
      
      Functions.FormalizedPowerOfAttorney.OpenInFtsRegistry(_obj);
    }

    public virtual bool CanOpenInFtsRegistry(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !string.IsNullOrWhiteSpace(_obj.UnifiedRegistrationNumber);
    }

    public virtual void CreateRevocation(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Sungero.Docflow.PublicFunctions.Module.IsPoAKonturLicenseEnable())
      {
        Dialogs.NotifyMessage(FormalizedPowerOfAttorneys.Resources.NoLicenseToPowerOfAttorneyKontur);
        return;
      }
      
      var revocation = Functions.FormalizedPowerOfAttorney.Remote.GetRevocation(_obj);
      
      if (revocation == null)
        Functions.FormalizedPowerOfAttorney.ShowCreateRevocationDialog(_obj);
      else
      {
        if (revocation.AccessRights.CanRead())
          revocation.Show();
        else
          Dialogs.ShowMessage(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.NoRightsToDocument, MessageType.Error);
      }
    }

    public virtual bool CanCreateRevocation(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Docflow.PublicFunctions.FormalizedPowerOfAttorney.CanCreateRevocation(_obj);
    }

    public override void ConvertToPdf(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var useObsoletePdfConversion = PublicFunctions.Module.Remote.UseObsoletePdfConversion();
      Structures.OfficialDocument.IConversionToPdfResult result = null;
      if (useObsoletePdfConversion)
      {
        result = Functions.FormalizedPowerOfAttorney.Remote.ConvertToPdfWithSignatureMark(_obj);
      }
      else
      {
        result = Functions.OfficialDocument.Remote.ValidateDocumentBeforeConversion(_obj, _obj.LastVersion.Id);
        if (!result.HasErrors)
        {
          var signatureMark = Functions.OfficialDocument.Remote.GetOrCreateSignatureMark(_obj);
          signatureMark.Save();
          result = Functions.OfficialDocument.Remote.ConvertToPdfWithMarks(_obj, _obj.LastVersion.Id);
        }
      }
      
      if (!result.HasErrors)
        Dialogs.NotifyMessage(OfficialDocuments.Resources.ConvertionDone);
      else
        Dialogs.ShowMessage(result.ErrorTitle, result.ErrorMessage, MessageType.Information);
    }

    public override bool CanConvertToPdf(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanConvertToPdf(e);
    }

    public virtual void CheckStateWithService(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Sungero.Docflow.PublicFunctions.Module.IsPoAKonturLicenseEnable())
      {
        Dialogs.NotifyMessage(FormalizedPowerOfAttorneys.Resources.NoLicenseToPowerOfAttorneyKontur);
        return;
      }
      
      if (!e.Validate())
        return;
      
      var resultMessage = Functions.FormalizedPowerOfAttorney.Remote.SyncFormalizedPowerOfAttorneyState(_obj);
      Dialogs.NotifyMessage(resultMessage);
    }

    public virtual bool CanCheckStateWithService(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Docflow.PublicFunctions.FormalizedPowerOfAttorney.CanCheckStateWithService(_obj);
    }

    public virtual void RegisterWithService(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Sungero.Docflow.PublicFunctions.Module.IsPoAKonturLicenseEnable())
      {
        Dialogs.NotifyMessage(FormalizedPowerOfAttorneys.Resources.NoLicenseToPowerOfAttorneyKontur);
        return;
      }
      
      if (!e.Validate())
        return;
      
      // Проверка версии, подписи и xml.
      var validationError = Functions.FormalizedPowerOfAttorney.Remote.ValidateFormalizedPoABeforeSending(_obj);
      if (!string.IsNullOrEmpty(validationError))
      {
        e.AddError(validationError);
        return;
      }
      
      // Отправка запроса на регистрацию.
      var sendingResult = Functions.FormalizedPowerOfAttorney.Remote.RegisterFormalizedPowerOfAttorneyWithService(_obj);
      
      // Обработка ошибок.
      if (!string.IsNullOrEmpty(sendingResult.ErrorType))
      {
        // Ошибка подключения.
        if (sendingResult.ErrorType == PowerOfAttorneyCore.PublicConstants.Module.PowerOfAttorneyServiceErrors.ConnectionError)
          e.AddError(PowerOfAttorneyCore.Resources.PowerOfAttorneyNoConnection);
        else
          e.AddError(FormalizedPowerOfAttorneys.Resources.FormalizedPowerOfAttorneySendForRegistrationError);
        
        return;
      }
      
      // Успешная отправка на регистрацию.
      e.CloseFormAfterAction = true;
    }
    
    public virtual bool CanRegisterWithService(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Docflow.PublicFunctions.FormalizedPowerOfAttorney.CanRegisterWithService(_obj);
    }

    public virtual void GenerateBodyWithPdf(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Sungero.Docflow.PublicFunctions.Module.IsPoAKonturLicenseEnable())
      {
        Dialogs.NotifyMessage(FormalizedPowerOfAttorneys.Resources.NoLicenseToPowerOfAttorneyKontur);
        return;
      }
      
      if (Functions.FormalizedPowerOfAttorney.IsImported(_obj))
      {
        e.AddError(FormalizedPowerOfAttorneys.Resources.CannotGeneratePdfForImportedFPoA);
        return;
      }
      
      if (!e.Validate())
        return;
      
      // Если формат не выбран - используем последний.
      if (!_obj.FormatVersion.HasValue)
        _obj.FormatVersion = FormatVersion.Version003;
      
      // Чтобы при добавлении версии пустое Содержание не заполнялось, добавить признак того, что версия создается из шаблона.
      e.Params.Add(Constants.Module.CreateFromTemplate, true);
      
      // Проверить, что у представителя и подписывающего указаны даты рождения.
      // Проверка делается до генерации xml, потому что пустые даты попадают в xml как 01-01-0001 и не валидируются по xsd.
      if (Functions.FormalizedPowerOfAttorney.CheckRequiredPropertiesValues(_obj) &&
          Functions.FormalizedPowerOfAttorney.Remote.GenerateFormalizedPowerOfAttorneyBody(_obj))
      {
        var useObsoletePdfConversion = PublicFunctions.Module.Remote.UseObsoletePdfConversion();
        if (useObsoletePdfConversion)
          Functions.FormalizedPowerOfAttorney.GeneratePdfWithSignatureMark(_obj);
        else
          Functions.OfficialDocument.Remote.ConvertToPdfWithMarks(_obj, _obj.LastVersion.Id);
        
        Dialogs.NotifyMessage(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.GenerateBodyWithPdfSuccess);
      }
      else
      {
        e.AddError(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.GenerateBodyWithPdfError);
      }
      
      e.Params.Remove(Constants.Module.CreateFromTemplate);
    }

    public virtual bool CanGenerateBodyWithPdf(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Docflow.PublicFunctions.FormalizedPowerOfAttorney.CanGenerateBodyWithPdf(_obj);
    }

    public virtual void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicates = Functions.FormalizedPowerOfAttorney.GetDuplicates(_obj);
      if (duplicates.Any())
        duplicates.Show();
      else
        Dialogs.NotifyMessage(FormalizedPowerOfAttorneys.Resources.DuplicatesNotFound);
    }

    public virtual bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public override void DeliverDocument(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.DeliverDocument(e);
    }

    public override bool CanDeliverDocument(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void ImportInLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ImportInLastVersion(e);
    }

    public override bool CanImportInLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override bool CanImportInNewVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void ImportInNewVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ImportInNewVersion(e);
    }

    public override void CreateVersionFromLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateVersionFromLastVersion(e);
    }

    public override bool CanCreateVersionFromLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CopyEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CopyEntity(e);
    }

    public override bool CanCopyEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public override void ScanInNewVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ScanInNewVersion(e);
    }

    public override bool CanScanInNewVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CreateFromScanner(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromScanner(e);
    }

    public override bool CanCreateFromScanner(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override bool CanCreateFromFile(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CreateFromFile(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromFile(e);
    }

    public override void CreateFromTemplate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromTemplate(e);
    }

    public override bool CanCreateFromTemplate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public virtual bool CanImportVersionWithSignature(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Docflow.PublicFunctions.FormalizedPowerOfAttorney.CanImportVersionWithSignature(_obj);
    }

    public virtual void ImportVersionWithSignature(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Чтобы при добавлении версии пустое Содержание не заполнялось, добавить признак того, что версия создается из шаблона.
      e.Params.Add(Constants.Module.CreateFromTemplate, true);
      
      if (Functions.FormalizedPowerOfAttorney.ShowImportVersionWithSignatureDialog(_obj) == true)
      {
        var useObsoletePdfConversion = PublicFunctions.Module.Remote.UseObsoletePdfConversion();
        if (useObsoletePdfConversion)
        {
          Functions.FormalizedPowerOfAttorney.Remote.GenerateFormalizedPowerOfAttorneyPdf(_obj);
        }
        else
        {
          var signatureMark = Functions.OfficialDocument.Remote.GetOrCreateSignatureMark(_obj);
          signatureMark.Save();
          Functions.OfficialDocument.Remote.ConvertToPdfWithMarks(_obj, _obj.LastVersion.Id);
        }
        
        if (Sungero.Docflow.PublicFunctions.Module.IsPoAKonturLicenseEnable() &&
            Functions.FormalizedPowerOfAttorney.CheckRequiredPropertiesValues(_obj) &&
            _obj.LifeCycleState == LifeCycleState.Active)
        {
          var syncResultMessage = Functions.FormalizedPowerOfAttorney.Remote.SyncFormalizedPowerOfAttorneyState(_obj);
          Dialogs.NotifyMessage(syncResultMessage);
        }
        
        if (_obj.RegistrationState == Docflow.OfficialDocument.RegistrationState.Registered)
        {
          e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.RepeatRegister, true);
          e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.NeedValidateRegisterFormat, true);
          SetIsPoAImportedParameterToTrue(e);
        }
      }
      
      e.Params.Remove(Constants.Module.CreateFromTemplate);
    }
    
    /// <summary>
    /// Установить параметр "Доверенность импортирована" в True.
    /// </summary>
    /// <param name="e">Аргумент действия "Импорт эл. доверенности".</param>
    private static void SetIsPoAImportedParameterToTrue(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      e.Params.AddOrUpdate(Constants.FormalizedPowerOfAttorney.IsLastVersionApprovedParamName, true);
    }
  }
}
