using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorneyRevocation;

namespace Sungero.Docflow.Client
{
  partial class PowerOfAttorneyRevocationVersionsActions
  {
    public override void CreateDocumentFromVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.CreateDocumentFromVersion(e);
    }

    public override bool CanCreateDocumentFromVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override bool CanCreateVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override void CreateVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.CreateVersion(e);
    }

    public override void ImportVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.ImportVersion(e);
    }

    public override bool CanImportVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override void EditVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.EditVersion(e);
    }

    public override bool CanEditVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

  }

  partial class PowerOfAttorneyRevocationCollectionActions
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

  partial class PowerOfAttorneyRevocationActions
  {
    public virtual void ReCreateRevocation(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Sungero.Docflow.PublicFunctions.Module.IsPoAKonturLicenseEnable())
      {
        Dialogs.NotifyMessage(FormalizedPowerOfAttorneys.Resources.NoLicenseToPowerOfAttorneyKontur);
        return;
      }
      
      Functions.PowerOfAttorneyRevocation.ShowReCreateRevocationDialog(_obj);
    }

    public virtual bool CanReCreateRevocation(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.LifeCycleState == PowerOfAttorney.LifeCycleState.Draft &&
        _obj.AccessRights.CanUpdate() &&
        (Functions.Module.IsLockedByMe(_obj) || _obj.State.IsInserted) &&
        (!_obj.FtsListState.HasValue || _obj.FtsListState == FtsListState.Rejected);
    }

    public override bool CanCreateFromTemplate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CreateFromTemplate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromTemplate(e);
    }

    public override bool CanCreateFromScanner(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CreateFromScanner(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromScanner(e);
    }

    public override bool CanCreateFromFile(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CreateFromFile(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromFile(e);
    }

    public override void ScanInNewVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ScanInNewVersion(e);
    }

    public override bool CanScanInNewVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override bool CanImportInLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void ImportInLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ImportInLastVersion(e);
    }

    public override void ImportInNewVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ImportInNewVersion(e);
    }

    public override bool CanImportInNewVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CreateVersionFromLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateVersionFromLastVersion(e);
    }

    public override bool CanCreateVersionFromLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void DeliverDocument(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.DeliverDocument(e);
    }

    public override bool CanDeliverDocument(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CopyEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CopyEntity(e);
    }

    public override bool CanCopyEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public virtual void RegisterRevocationWithService(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Sungero.Docflow.PublicFunctions.Module.IsPoAKonturLicenseEnable())
      {
        Dialogs.NotifyMessage(FormalizedPowerOfAttorneys.Resources.NoLicenseToPowerOfAttorneyKontur);
        return;
      }

      if (_obj.FormalizedPowerOfAttorney.FtsListState == Docflow.FormalizedPowerOfAttorney.FtsListState.Revoked)
      {
        e.AddError(Sungero.Docflow.PowerOfAttorneyRevocations.Resources.SendRevocationToRevokedFPoAError);
        return;
      }
      
      // Валидация xml и подписи отзыва перед отправкой в ФНС.
      var validationError = PublicFunctions.PowerOfAttorneyRevocation.Remote.ValidateRevocationBeforeSending(_obj);
      if (!string.IsNullOrEmpty(validationError))
      {
        e.AddError(validationError);
        return;
      }
      
      var sendingResult = PublicFunctions.PowerOfAttorneyRevocation.Remote.RegisterRevocationWithService(_obj);
      if (!string.IsNullOrEmpty(sendingResult.ErrorType))
      {
        // Ошибка подключения.
        if (sendingResult.ErrorType == PowerOfAttorneyCore.PublicConstants.Module.PowerOfAttorneyServiceErrors.ConnectionError)
          e.AddError(PowerOfAttorneyCore.Resources.PowerOfAttorneyNoConnection);
        else
          e.AddError(Sungero.Docflow.PowerOfAttorneyRevocations.Resources.RegisterPowerOfAttorneyRevocationError);
        
        return;
      }
      
      e.CloseFormAfterAction = true;
    }

    public virtual bool CanRegisterRevocationWithService(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      if (_obj.FormalizedPowerOfAttorney.ValidTill.HasValue &&
          _obj.FormalizedPowerOfAttorney.ValidTill < Calendar.UserToday)
        return false;
      
      return _obj.LifeCycleState == PowerOfAttorney.LifeCycleState.Draft &&
        _obj.HasVersions &&
        _obj.AccessRights.CanUpdate() &&
        (Functions.Module.IsLockedByMe(_obj) || _obj.State.IsInserted) &&
        (!_obj.FtsListState.HasValue || _obj.FtsListState == FtsListState.Rejected);
    }
  }

}