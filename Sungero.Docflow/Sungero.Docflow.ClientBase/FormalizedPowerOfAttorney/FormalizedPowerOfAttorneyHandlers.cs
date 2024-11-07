using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FormalizedPowerOfAttorney;

namespace Sungero.Docflow
{
  partial class FormalizedPowerOfAttorneyClientHandlers
  {
    #region form handlers

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      // Занесение доверенности не завершено. Не дизейблить обязательные поля, чтобы можно было завершить занесение.
      if (_obj.RegistrationState == Docflow.OfficialDocument.RegistrationState.Registered &&
          _obj.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Draft &&
          ((_obj.AgentType == null && _obj.IsManyRepresentatives == false) || _obj.BusinessUnit == null || _obj.Department == null))
      {
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.RepeatRegister, true);
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.NeedValidateRegisterFormat, true);
      }
      
      this.SetIsPoAImportedParameterIfEmpty(e);
      
      var registrationErrorIsVisible = _obj.FtsListState == Docflow.FormalizedPowerOfAttorney.FtsListState.Rejected;
      _obj.State.Properties.FtsRejectReason.IsVisible = registrationErrorIsVisible;
      _obj.State.Properties.FormatVersion.IsEnabled = _obj.MainPoA == null && !_obj.HasVersions;
      
      _obj.State.Properties.IsNotarized.IsVisible = _obj.IsNotarized == true;
      
      base.Refresh(e);
      
      if (_obj.AgentType == AgentType.LegalEntity && _obj.FormatVersion == FormatVersion.Version003)
      {
        _obj.State.Properties.Representative.IsEnabled = false;
        _obj.State.Properties.Representative.IsRequired = false;
      }
      
      Functions.FormalizedPowerOfAttorney.ChangePowersFieldsVisibility(_obj);
      
      _obj.State.Pages.MainPoAInfoPage.IsVisible = _obj.IsDelegated == true;
      _obj.State.Properties.MainPoA.IsVisible = _obj.IsDelegated == true;
      _obj.State.Properties.IsNotarized.IsVisible = _obj.IsNotarized == true;
    }
    
    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      base.Closing(e);
      RemoveIsPoAImportedParameter(e);
    }
    
    /// <summary>
    /// Вычислить и установить параметр "Доверенность импортирована",
    /// если параметр отсутствует.
    /// </summary>
    /// <param name="e">Аргументы события "Обновление формы".</param>
    private void SetIsPoAImportedParameterIfEmpty(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!e.Params.Contains(Constants.FormalizedPowerOfAttorney.IsLastVersionApprovedParamName) && _obj.HasVersions)
      {
        var signatures = Signatures.Get(_obj.LastVersion);
        if (_obj.LastVersionApproved == true || signatures.Any(x => x.SignatureType == SignatureType.Approval))
          e.Params.AddOrUpdate(Constants.FormalizedPowerOfAttorney.IsLastVersionApprovedParamName, signatures.Any(x => x.IsExternal == true));
      }
    }
    
    /// <summary>
    /// Удалить параметр "Доверенность импортирована".
    /// </summary>
    /// <param name="e">Аргументы события "Закрытие формы".</param>
    private static void RemoveIsPoAImportedParameter(Sungero.Presentation.FormClosingEventArgs e)
    {
      e.Params.Remove(Constants.FormalizedPowerOfAttorney.IsLastVersionApprovedParamName);
    }
    
    #endregion
    
    #region property handlers
    
    public virtual void IsDelegatedValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      var isPoaRetrust = e.NewValue == true;
      _obj.State.Pages.MainPoAInfoPage.IsVisible = isPoaRetrust;
      _obj.State.Properties.MainPoA.IsVisible = isPoaRetrust;
      Functions.FormalizedPowerOfAttorney.SetMainPoAPropertiesAccess(_obj, _obj.MainPoA == null);
    }
    
    public virtual void PowersTypeValueInput(Sungero.Presentation.EnumerationValueInputEventArgs e)
    {
      // Добавляем обработчик клиентского события, чтобы отработал Refresh.
      Functions.FormalizedPowerOfAttorney.ChangePowersFieldsVisibility(_obj);
    }
    
    public virtual void UnifiedRegistrationNumberValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (!string.IsNullOrEmpty(e.NewValue))
        e.NewValue = e.NewValue.Trim();
      
      Guid guid;
      if (!Guid.TryParse(e.NewValue, out guid))
        e.AddError(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.ErrorValidateUnifiedRegistrationNumber);
    }

    #endregion
  }
}