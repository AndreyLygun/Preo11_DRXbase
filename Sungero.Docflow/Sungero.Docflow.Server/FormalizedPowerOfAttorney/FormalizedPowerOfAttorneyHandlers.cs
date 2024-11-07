using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FormalizedPowerOfAttorney;
using HistoryOperation = Sungero.Docflow.Structures.OfficialDocument.HistoryOperation;

namespace Sungero.Docflow
{
  partial class FormalizedPowerOfAttorneyMainPoAPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> MainPoAFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      // 1. Ненотариальные.
      query = query.Where(x => x.IsNotarized == false);
      
      // 2. С последующим передоверием, где тип хотя бы одного представителя - ЮЛ.
      query = query.Where(x => x.DelegationType == DelegationType.WithDelegation &&
                          x.Representatives.Any(y => y.AgentType == PowerOfAttorneyBaseRepresentatives.AgentType.LegalEntity));
      
      // 3. Того же формата.
      query = query.Where(x => Equals(x.FormatVersion, _obj.FormatVersion));
      
      // 4. Состояние: Действующий.
      query = query.Where(x => x.LifeCycleState == LifeCycleState.Active);
      
      // 5. В реестре ФНС: Зарегистрирован.
      query = query.Where(x => x.FtsListState == FtsListState.Registered);
      
      // 6. "Действует по" не меньше текущей даты.
      return query.Where(x => x.ValidTill >= Calendar.Today);
    }
  }

  partial class FormalizedPowerOfAttorneyStructuredPowersPowerPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> StructuredPowersPowerFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var currentDay = Calendar.Today;
      return query.Where(f => f.Status == PowerOfAttorneyCore.PowerOfAttorneyClassifier.Status.Active &&
                         (!f.Revoked.HasValue || f.Revoked >= currentDay) &&
                         f.Started <= currentDay);
    }
  }

  partial class FormalizedPowerOfAttorneyFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      query = base.Filtering(query, e);
      return query;
    }
    
  }

  partial class FormalizedPowerOfAttorneyServerHandlers
  {
    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      _obj.LifeCycleState = FormalizedPowerOfAttorney.LifeCycleState.Draft;
      _obj.IsNotarized = false;

      if (!_obj.State.IsCopied)
      {
        _obj.FormatVersion = FormatVersion.Version003;
        _obj.PowersType = PowersType.FreeForm;
        _obj.DelegationType = FormalizedPowerOfAttorney.DelegationType.NoDelegation;
        _obj.IsDelegated = false;
      }
      
      if (_obj.State.IsCopied && _obj.PowersType == PowersType.Classifier)
      {
        var revokedPowers = _obj.StructuredPowers.Where(f => f.Power.Status == PowerOfAttorneyCore.PowerOfAttorneyClassifier.Status.Closed ||
                                                        f.Power.Started > Calendar.Today ||
                                                        f.Power.Revoked < Calendar.Today).ToList();
        foreach (var revokedPower in revokedPowers)
          _obj.StructuredPowers.Remove(revokedPower);
      }
    }
    
    #region BeforeSave
    
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      // Пропуск выполнения обработчика в случае отсутствия прав на изменение, например при выдаче прав на чтение пользователем, который сам имеет права на чтение.
      if (!_obj.AccessRights.CanUpdate())
        return;
      
      if (IsJustImportedParameterSet(e))
      {
        var duplicatesError = Functions.FormalizedPowerOfAttorney.GetDuplicatesErrorText(_obj);
        if (!string.IsNullOrEmpty(duplicatesError))
          e.AddError(duplicatesError, _obj.Info.Actions.ShowDuplicates);
        Functions.FormalizedPowerOfAttorney.SetLifeCycleActiveAfterImport(_obj);
      }
      
      if (_obj.FormatVersion == FormatVersion.Version002)
      {
        _obj.StructuredPowers.Clear();
      }
      else if (_obj.FormatVersion == FormatVersion.Version003)
      {
        if (_obj.PowersType == PowersType.Classifier)
          _obj.Powers = null;
        else
          _obj.StructuredPowers.Clear();
        
        if (_obj.AgentType == AgentType.LegalEntity && _obj.Representative != null)
          _obj.Representative = null;
      }
      
      if (_obj.IsDelegated == true)
      {
        this.ValidateMainPoAValidDates(e);
        this.ValidateIfTrustWithinMainPoADatesRange(e);
        this.ValidateStructuredPowers(e);
      }
    }
    
    #region override base    
    
    protected override bool IsAgentInRepresentativeRowRequired(Sungero.Docflow.IPowerOfAttorneyBaseRepresentatives representative)
    {
      if (representative.AgentType == AgentType.LegalEntity && _obj.FormatVersion == FormatVersion.Version003)
        return false;
      
      return base.IsAgentInRepresentativeRowRequired(representative);
    }
    
    protected override string GetErrorMessageForIncorrectFilledAgent(bool required, bool filled)
    {
      if (!required && filled)
        return Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.NotRequiredAgentIfPersonOrLegalEntityFilled;
      
      return base.GetErrorMessageForIncorrectFilledAgent(required, filled);
    }
    
    #endregion
    
    private static bool IsJustImportedParameterSet(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var result = false;
      return e.Params.TryGetValue(Constants.FormalizedPowerOfAttorney.FPoAWasJustImportedParamName, out result) && result;
    }
    
    /// <summary>
    /// Проверка корректности срока действия корневой доверенности.
    /// </summary>
    /// <param name="e">Доп. параметры обработчика BeforeSave.</param>
    private void ValidateMainPoAValidDates(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (!_obj.MainPoAValidFrom.HasValue && !_obj.MainPoAValidTill.HasValue)
        return;
      
      if (!_obj.MainPoAValidFrom.HasValue)
      {
        e.AddError(_obj.Info.Properties.MainPoAValidFrom, FormalizedPowerOfAttorneys.Resources.MainPoaValidFromNotSpecifiedWhileTillSpecified);
        return;
      }
      if (!_obj.MainPoAValidTill.HasValue)
      {
        e.AddError(_obj.Info.Properties.MainPoAValidTill, FormalizedPowerOfAttorneys.Resources.MainPoaValidTillNotSpecifiedWhileFromSpecified);
        return;
      }
      
      if (_obj.MainPoAValidFrom > _obj.MainPoAValidTill)
      {
        e.AddError(_obj.Info.Properties.MainPoAValidFrom, FormalizedPowerOfAttorneys.Resources.IncorrectMainPoAValidDates, _obj.Info.Properties.MainPoAValidTill);
        e.AddError(_obj.Info.Properties.MainPoAValidTill, FormalizedPowerOfAttorneys.Resources.IncorrectMainPoAValidDates, _obj.Info.Properties.MainPoAValidFrom);
      }
    }
    
    /// <summary>
    /// Проверка корректности срока действия передоверия.
    /// Срок действия передоверия должено быть в рамках срока корневой доверенности.
    /// </summary>
    /// <param name="e">Доп. параметры обработчика BeforeSave.</param>
    private void ValidateIfTrustWithinMainPoADatesRange(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (!_obj.MainPoAValidFrom.HasValue || !_obj.MainPoAValidTill.HasValue)
        return;
      
      if (_obj.ValidFrom >= _obj.MainPoAValidFrom &&
          _obj.ValidTill <= _obj.MainPoAValidTill)
        return;
      
      e.AddError(_obj.Info.Properties.ValidFrom, FormalizedPowerOfAttorneys.Resources.TrustNotWithinMainPoADatesRange, _obj.Info.Properties.MainPoAValidFrom);
      e.AddError(_obj.Info.Properties.ValidTill, FormalizedPowerOfAttorneys.Resources.TrustNotWithinMainPoADatesRange, _obj.Info.Properties.MainPoAValidTill);
    }
    
    /// <summary>
    /// Проверка корректности списка полномочий.
    /// </summary>
    /// <param name="e">Доп. параметры обработчика BeforeSave.</param>
    private void ValidateStructuredPowers(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.MainPoA == null || _obj.PowersType != PowersType.Classifier)
        return;
      
      var mainStructuredPowers = _obj.MainPoA.StructuredPowers.Select(p => p.Power).ToList();
      if (_obj.StructuredPowers.Select(p => p.Power).Except(mainStructuredPowers).Any())
        e.AddError(_obj.Info.Properties.StructuredPowers, Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.ExcessiveStructuredPowers, _obj.Info.Properties.StructuredPowers);
    }    
    
    #endregion
  }

  partial class FormalizedPowerOfAttorneyCreatingFromServerHandler
  {
    
    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      e.Without(_info.Properties.UnifiedRegistrationNumber);
      e.Without(_info.Properties.RegisteredSignatureId);
      e.Without(_info.Properties.Index);
      e.Without(_info.Properties.ValidFrom);
      e.Map(_info.Properties.LifeCycleState, Sungero.Docflow.FormalizedPowerOfAttorney.LifeCycleState.Draft);
      e.Without(_info.Properties.FtsListState);
      e.Without(_info.Properties.FtsRejectReason);
      e.Without(_info.Properties.Versions);
      e.Without(_info.Properties.HasVersions);
      e.Without(_info.Properties.Note);
    }
  }

}