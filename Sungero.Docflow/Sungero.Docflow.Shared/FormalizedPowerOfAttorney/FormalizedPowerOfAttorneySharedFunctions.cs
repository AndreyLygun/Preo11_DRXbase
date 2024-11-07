using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FormalizedPowerOfAttorney;
using Sungero.Parties;

namespace Sungero.Docflow.Shared
{
  partial class FormalizedPowerOfAttorneyFunctions
  {
    
    /// <summary>
    /// Очистка информации о корневой доверенности.
    /// </summary>
    public virtual void ClearMainPoAProperties()
    {
      _obj.MainPoAPrincipal = null;
      _obj.MainPoAUnifiedNumber = null;
      _obj.MainPoARegistrationNumber = null;
      _obj.MainPoAValidFrom = null;
      _obj.MainPoAValidTill = null;
    }
    
    /// <summary>
    /// Заполнить информацию о корневой доверенности.
    /// </summary>
    /// <param name="mainPowerOfAttorney">Доверенность, на основании которой сформировано передоверие.</param>
    public virtual void FillMainPoAProperties(IFormalizedPowerOfAttorney mainPowerOfAttorney)
    {
      if (mainPowerOfAttorney == null)
        return;
      
      _obj.MainPoAPrincipal = mainPowerOfAttorney.BusinessUnit?.Company;
      _obj.MainPoARegistrationNumber = mainPowerOfAttorney.RegistrationNumber;
      _obj.MainPoAUnifiedNumber = mainPowerOfAttorney.UnifiedRegistrationNumber;
      _obj.MainPoAValidFrom = mainPowerOfAttorney.ValidFrom;
      _obj.MainPoAValidTill = mainPowerOfAttorney.ValidTill;

      if (_obj.FormatVersion != mainPowerOfAttorney.FormatVersion)
        _obj.FormatVersion = mainPowerOfAttorney.FormatVersion;
      if (_obj.PowersType != mainPowerOfAttorney.PowersType)
        _obj.PowersType = mainPowerOfAttorney.PowersType;
      if (_obj.PowersType == PowersType.FreeForm && _obj.Powers != mainPowerOfAttorney.Powers)
        _obj.Powers = mainPowerOfAttorney.Powers;
      
      var mainStructuredPowers = mainPowerOfAttorney.StructuredPowers.Select(p => p.Power).ToList();
      var retrustStructuredPowers = _obj.StructuredPowers.Select(p => p.Power).ToList();
      if (_obj.PowersType == PowersType.Classifier && (retrustStructuredPowers.Except(mainStructuredPowers).Any() || mainStructuredPowers.Except(retrustStructuredPowers).Any()))
      {
        _obj.StructuredPowers.Clear();
        foreach (var mainStructuredPower in mainStructuredPowers)
          _obj.StructuredPowers.AddNew().Power = mainStructuredPower;
      }
      
      if (_obj.ValidFrom != mainPowerOfAttorney.ValidFrom)
        _obj.ValidFrom = mainPowerOfAttorney.ValidFrom;
      if (_obj.ValidTill != mainPowerOfAttorney.ValidTill)
        _obj.ValidTill = mainPowerOfAttorney.ValidTill;

      var mainIssuedToParty = Sungero.Parties.Companies.As(mainPowerOfAttorney.IssuedToParty);
      if (mainIssuedToParty != null)
      {
        var businessUnit = Sungero.Company.PublicFunctions.BusinessUnit
          .GetBusinessUnits(mainIssuedToParty.TIN, mainIssuedToParty.TRRC)
          .FirstOrDefault();
        
        var signatureSetting = SignatureSettings.GetAll()
          .Where(s => s.Document == mainPowerOfAttorney)
          .FirstOrDefault();
        
        if (_obj.BusinessUnit != businessUnit)
          _obj.BusinessUnit = businessUnit;
        
        _obj.OurSignatory = signatureSetting != null ? Sungero.Company.Employees.As(signatureSetting.Recipient) : null;
        
        _obj.OurSigningReason = signatureSetting != null ? signatureSetting : null;
      }
      
    }
    
    /// <summary>
    /// Изменить отображение поля "Полномочия".
    /// </summary>
    public virtual void ChangePowersFieldsVisibility()
    {
      _obj.State.Properties.Powers.IsVisible = _obj.PowersType == PowersType.FreeForm;
      _obj.State.Properties.StructuredPowers.IsVisible = _obj.PowersType == PowersType.Classifier;
    }
    
    /// <summary>
    /// Изменить отображение панели регистрации.
    /// </summary>
    /// <param name="needShow">Признак отображения.</param>
    /// <param name="repeatRegister">Признак повторной регистрации\изменения реквизитов.</param>
    public override void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
    {
      base.ChangeRegistrationPaneVisibility(needShow, repeatRegister);
      
      var properties = _obj.State.Properties;
      
      properties.UnifiedRegistrationNumber.IsVisible = needShow;
      properties.FtsListState.IsVisible = needShow;
    }
    
    /// <summary>
    /// Изменить доступность свойства "Вид носителя документа".
    /// </summary>
    public override void ChangeMediumTypePropertyAccess()
    {
      _obj.State.Properties.Medium.IsEnabled = false;
    }
    
    /// <summary>
    /// Установить состояние жизненного цикла эл. доверенности в Действующее.
    /// </summary>
    [Public, Obsolete("Метод не используется с 01.09.2023 и версии 4.8. Используйте метод SetLifeCycleActiveAfterImport.")]
    public virtual void SetActiveLifeCycleState()
    {
      var issuedToDefined =
        _obj.AgentType == AgentType.Employee && _obj.IssuedTo != null ||
        _obj.AgentType == AgentType.LegalEntity && _obj.IssuedToParty != null && _obj.Representative != null ||
        _obj.AgentType == AgentType.Entrepreneur && _obj.IssuedToParty != null && _obj.Representative != null ||
        _obj.AgentType == AgentType.Person && _obj.IssuedToParty != null;
      
      if (_obj.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Draft &&
          issuedToDefined && _obj.BusinessUnit != null && _obj.Department != null &&
          _obj.ValidFrom != null && _obj.ValidTill != null && _obj.HasVersions)
      {
        _obj.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Active;
        _obj.FtsListState = Docflow.FormalizedPowerOfAttorney.FtsListState.Registered;
      }
    }
    
    /// <summary>
    /// Установить состояние ЖЦ эл. доверенности - Действующий, в реестре ФНС - Зарегистрирован.
    /// </summary>
    [Public, Obsolete("Метод не используется с 01.09.2023 и версии 4.8. Используйте метод SetLifeCycleActiveAfterImport.")]
    public virtual void SetLifeCycleAndFtsListStates()
    {
      var issuedToDefined =
        _obj.AgentType == AgentType.Employee && _obj.IssuedTo != null ||
        _obj.AgentType == AgentType.LegalEntity && _obj.IssuedToParty != null && _obj.Representative != null ||
        _obj.AgentType == AgentType.Entrepreneur && _obj.IssuedToParty != null && _obj.Representative != null ||
        _obj.AgentType == AgentType.Person && _obj.IssuedToParty != null;
      
      if (_obj.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Draft &&
          issuedToDefined && _obj.BusinessUnit != null && _obj.Department != null &&
          _obj.ValidFrom != null && _obj.ValidTill != null && !string.IsNullOrWhiteSpace(_obj.Powers) &&
          _obj.OurSignatory != null && _obj.OurSigningReason != null && _obj.HasVersions)
      {
        this.SetLifeCycleAndFtsListStates(LifeCycleState.Active, FtsListState.Registered);
      }
    }
    
    /// <summary>
    /// Установить состояние ЖЦ импортируемой эл. доверенности - Действующий.
    /// </summary>
    /// <remarks>Для установки активного состояния эл. доверенность должна соответствовать определенным критериям.</remarks>
    [Public]
    public virtual void SetLifeCycleActiveAfterImport()
    {
      var issuedToDefined =
        _obj.AgentType == AgentType.Employee && _obj.IssuedTo != null ||
        _obj.AgentType == AgentType.LegalEntity && _obj.IssuedToParty != null && _obj.Representative != null ||
        _obj.AgentType == AgentType.Entrepreneur && _obj.IssuedToParty != null && _obj.Representative != null ||
        _obj.AgentType == AgentType.Person && _obj.IssuedToParty != null;
      var isPowerStructured = _obj.PowersType == PowersType.Classifier;
      var isPowersDefined = (!string.IsNullOrWhiteSpace(_obj.Powers) && !isPowerStructured) ||
        (_obj.StructuredPowers.Any() && isPowerStructured);
      
      if (_obj.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Draft &&
          issuedToDefined &&
          _obj.BusinessUnit != null &&
          _obj.Department != null &&
          _obj.ValidFrom != null &&
          _obj.ValidTill != null &&
          isPowersDefined &&
          _obj.OurSignatory != null &&
          _obj.OurSigningReason != null &&
          _obj.HasVersions)
      {
        this.SetLifeCycleAndFtsListStates(LifeCycleState.Active, _obj.FtsListState);
      }
    }
    
    /// <summary>
    /// Установить состояние эл. доверенности.
    /// </summary>
    /// <param name="lifeCycleState">Состояние жизненного цикла.</param>
    /// <param name="ftsListState">Состояние в реестре ФНС.</param>
    [Public]
    public virtual void SetLifeCycleAndFtsListStates(Enumeration? lifeCycleState, Enumeration? ftsListState)
    {
      if (_obj.LifeCycleState != Docflow.FormalizedPowerOfAttorney.LifeCycleState.Obsolete &&
          _obj.LifeCycleState != lifeCycleState)
      {
        _obj.LifeCycleState = lifeCycleState;
      }
      
      if (_obj.FtsListState != ftsListState)
        _obj.FtsListState = ftsListState;
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
    /// Обновить жизненный цикл документа.
    /// </summary>
    /// <param name="registrationState">Статус регистрации.</param>
    /// <param name="approvalState">Статус согласования.</param>
    /// <param name="counterpartyApprovalState">Статус согласования с контрагентом.</param>
    public override void UpdateLifeCycle(Enumeration? registrationState,
                                         Enumeration? approvalState,
                                         Enumeration? counterpartyApprovalState)
    {
      // Не обновлять жизненный цикл в зависимости от других статусов.
    }
    
    public override void FillName()
    {
      var documentKind = _obj.DocumentKind;
      
      if (documentKind != null && !documentKind.GenerateDocumentName.Value && _obj.Name == Docflow.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (documentKind == null || !documentKind.GenerateDocumentName.Value)
        return;

      var name = string.Empty;
      
      /* Имя в формате:
        <Вид документа> для <Кому выдана> №<Единый рег. номер> (рег. №<номер>) от <дата> "<содержание>".
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        name += PowerOfAttorneyBases.Resources.DocumentNameFor + this.GetRepresentativeNamePart();
        
        if (!string.IsNullOrWhiteSpace(_obj.UnifiedRegistrationNumber))
          name += FormalizedPowerOfAttorneys.Resources.UnifiedRegistrationNumberFormat(_obj.UnifiedRegistrationNumber);
        
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += FormalizedPowerOfAttorneys.Resources.RegistrationNumberInBracketsFormat(_obj.RegistrationNumber);
        
        if (_obj.RegistrationDate != null)
          name += OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
          name += " \"" + _obj.Subject + "\"";
      }
      
      if (string.IsNullOrWhiteSpace(name))
        name = Docflow.Resources.DocumentNameAutotext;
      else if (documentKind != null)
        name = documentKind.ShortName + name;
      
      name = Functions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Functions.OfficialDocument.AddClosingQuote(name, _obj);
      
    }
    
    #region Доступность свойств
    
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);
      
      var mainPoAIsEmpty = _obj.MainPoA == null;
      _obj.State.Properties.FormatVersion.IsEnabled = mainPoAIsEmpty;
      _obj.State.Properties.DelegationType.IsEnabled = _obj.IsDelegated == false;
      
      _obj.State.Properties.PowersType.IsEnabled = mainPoAIsEmpty && _obj.FormatVersion == FormatVersion.Version003;
      _obj.State.Properties.Powers.IsEnabled = _obj.PowersType == PowersType.FreeForm && mainPoAIsEmpty;

      var isMainPoAPropertiesEnabled = _obj.IsDelegated == true && mainPoAIsEmpty;
      Functions.FormalizedPowerOfAttorney.SetMainPoAPropertiesAccess(_obj, isMainPoAPropertiesEnabled);
      
      if (_obj.State.IsInserted)
        return;
      
      var powersInFreeForm = _obj.PowersType == PowersType.FreeForm;
      var powersFromClassifier = _obj.PowersType == PowersType.Classifier;

      if (!_obj.HasVersions)
      {
        _obj.State.Properties.IsDelegated.IsEnabled = true;
        
        this.SetAgentsAPropertiesAccess(true);
        
        _obj.State.Properties.Powers.IsEnabled = powersInFreeForm && mainPoAIsEmpty;
        _obj.State.Properties.StructuredPowers.IsEnabled = powersFromClassifier;
        
        _obj.State.Properties.BusinessUnit.IsEnabled = true;
        _obj.State.Properties.Department.IsEnabled = true;
        _obj.State.Properties.OurSignatory.IsEnabled = true;
        _obj.State.Properties.PreparedBy.IsEnabled = true;
        _obj.State.Properties.OurSigningReason.IsEnabled = _obj.OurSignatory != null;

        return;
      }
      
      var needEnableProperties = true;
      var isRegistered = _obj.DocumentRegister != null;
      var isProcessedInFts = _obj.FtsListState == Docflow.FormalizedPowerOfAttorney.FtsListState.OnRegistration ||
        _obj.FtsListState == Docflow.FormalizedPowerOfAttorney.FtsListState.Registered ||
        _obj.FtsListState == Docflow.FormalizedPowerOfAttorney.FtsListState.OnRevoke ||
        _obj.FtsListState == Docflow.FormalizedPowerOfAttorney.FtsListState.Revoked;
      var isRegistrationError = _obj.FtsListState == Docflow.FormalizedPowerOfAttorney.FtsListState.Rejected;
      
      if (this.IsImported())
        needEnableProperties = (isRegistrationError || _obj.FtsListState == null) && (isRepeatRegister || !isRegistered);
      else
        needEnableProperties = isEnabled && (isRegistrationError || _obj.LastVersionApproved == false && _obj.FtsListState == null) && (isRepeatRegister || !isRegistered);
      
      _obj.State.Properties.DelegationType.IsEnabled = needEnableProperties && _obj.IsDelegated == false;
      _obj.State.Properties.IsDelegated.IsEnabled = needEnableProperties;
      _obj.State.Properties.MainPoA.IsEnabled = needEnableProperties;
      
      this.SetAgentsAPropertiesAccess(needEnableProperties);
      
      _obj.State.Properties.PowersType.IsEnabled = mainPoAIsEmpty && _obj.FormatVersion == FormatVersion.Version003 && needEnableProperties;
      _obj.State.Properties.Powers.IsEnabled = mainPoAIsEmpty && powersInFreeForm && needEnableProperties;
      _obj.State.Properties.StructuredPowers.IsEnabled = powersFromClassifier && needEnableProperties;
      
      _obj.State.Properties.BusinessUnit.IsEnabled = needEnableProperties;
      _obj.State.Properties.Department.IsEnabled = needEnableProperties;
      _obj.State.Properties.OurSignatory.IsEnabled = needEnableProperties;
      _obj.State.Properties.PreparedBy.IsEnabled = needEnableProperties;
      _obj.State.Properties.OurSigningReason.IsEnabled = _obj.OurSignatory != null && needEnableProperties;
      
      _obj.State.Properties.ValidFrom.IsEnabled = needEnableProperties;
      _obj.State.Properties.ValidTill.IsEnabled = needEnableProperties;
      
      _obj.State.Properties.RegistrationNumber.IsEnabled = !isProcessedInFts && isRepeatRegister;
      _obj.State.Properties.RegistrationDate.IsEnabled = !isProcessedInFts && isRepeatRegister;
      
      _obj.State.Properties.FormatVersion.IsEnabled = needEnableProperties && mainPoAIsEmpty;

      isMainPoAPropertiesEnabled = needEnableProperties && isMainPoAPropertiesEnabled;      
      Functions.FormalizedPowerOfAttorney.SetMainPoAPropertiesAccess(_obj, isMainPoAPropertiesEnabled);
    }
    
    /// <summary>
    /// Установить доступность полей на вкладке На основании.
    /// </summary>
    /// <param name="isEnabled">True - если поля доступны, иначе - false.</param>
    public virtual void SetMainPoAPropertiesAccess(bool isEnabled)
    {
      _obj.State.Properties.MainPoAPrincipal.IsEnabled = isEnabled;
      _obj.State.Properties.MainPoAUnifiedNumber.IsEnabled = isEnabled;
      _obj.State.Properties.MainPoARegistrationNumber.IsEnabled = isEnabled;
      _obj.State.Properties.MainPoAValidFrom.IsEnabled = isEnabled;
      _obj.State.Properties.MainPoAValidTill.IsEnabled = isEnabled;
    }
    
    /// <summary>
    /// Установить доступность свойств представителя(ей).
    /// </summary>
    /// <param name="isEnabled">True - если поля доступны, иначе - false.</param>
    private void SetAgentsAPropertiesAccess(bool isEnabled)
    {
      _obj.State.Properties.AgentType.IsEnabled = isEnabled;
      _obj.State.Properties.IssuedTo.IsEnabled = isEnabled;
      _obj.State.Properties.IssuedToParty.IsEnabled = isEnabled;
      _obj.State.Properties.Representative.IsEnabled = isEnabled;
      _obj.State.Properties.Representatives.IsEnabled = isEnabled;
    }
    
    #endregion
    
    #region Обязательность свойств
    
    /// <summary>
    /// Установить обязательность свойств в зависимости от заполненных данных.
    /// </summary>
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      
      _obj.State.Properties.Subject.IsRequired = _obj.Info.Properties.Subject.IsRequired;
      
      // Изменить обязательность полей в зависимости от того, программная или визуальная работа.
      var fpoaParams = ((Domain.Shared.IExtendedEntity)_obj).Params;
      var isVisualMode = fpoaParams.ContainsKey(Docflow.PublicConstants.OfficialDocument.IsVisualModeParamName);
      
      // Изменить обязательность полей в зависимости от внешней подписи.
      var needSetRequired = this.NeedSetRequiredProperties(this.IsImported());
      var isPowerStructured = _obj.PowersType == PowersType.Classifier;
      
      _obj.State.Properties.BusinessUnit.IsRequired = isVisualMode;
      _obj.State.Properties.Department.IsRequired = isVisualMode;
      _obj.State.Properties.ValidFrom.IsRequired = isVisualMode;
      _obj.State.Properties.Powers.IsRequired = isVisualMode && needSetRequired && !isPowerStructured;
      _obj.State.Properties.StructuredPowers.IsRequired = isVisualMode && needSetRequired && isPowerStructured;
      _obj.State.Properties.OurSignatory.IsRequired = isVisualMode && needSetRequired;
      _obj.State.Properties.OurSigningReason.IsRequired = isVisualMode && needSetRequired;
      this.SetRequiredForMainPoAProperties();
      
      if (!isVisualMode)
      {
        _obj.State.Properties.Powers.IsRequired = false;
        _obj.State.Properties.StructuredPowers.IsRequired = false;
        _obj.State.Properties.OurSignatory.IsRequired = false;
        _obj.State.Properties.OurSigningReason.IsRequired = false;
        _obj.State.Properties.MainPoAPrincipal.IsRequired = false;
        _obj.State.Properties.MainPoARegistrationNumber.IsRequired = false;
        _obj.State.Properties.MainPoAUnifiedNumber.IsRequired = false;
        _obj.State.Properties.MainPoAValidFrom.IsRequired = false;
        _obj.State.Properties.MainPoAValidTill.IsRequired = false;
        
        this.UpdateRequiredForRepresentativesIfNotVisualMode();
      }
      else
      {
        this.RemoveRepresentativeMandatoryIfNeeded();
      }
    }
    
    /// <summary>
    /// Установка обязательности для свойств корневой доверенности.
    /// </summary>
    private void SetRequiredForMainPoAProperties()
    {
      var isPoaRetrust = _obj.IsDelegated == true;
      var isVersion003 = _obj.FormatVersion == FormatVersion.Version003;
      
      _obj.State.Properties.MainPoAPrincipal.IsRequired = isPoaRetrust;
      _obj.State.Properties.MainPoAUnifiedNumber.IsRequired = isPoaRetrust;
      _obj.State.Properties.MainPoARegistrationNumber.IsRequired = isPoaRetrust && isVersion003;
      _obj.State.Properties.MainPoAValidFrom.IsRequired = isPoaRetrust && isVersion003;
      _obj.State.Properties.MainPoAValidTill.IsRequired = isPoaRetrust && isVersion003;
    }
    
    /// <summary>
    /// Снять обязательность свойств представителей при импорте.
    /// </summary>
    private void UpdateRequiredForRepresentativesIfNotVisualMode()
    {
      _obj.State.Properties.AgentType.IsRequired = false;
      _obj.State.Properties.IssuedTo.IsRequired = false;
      _obj.State.Properties.IssuedToParty.IsRequired = false;
      _obj.State.Properties.Representative.IsRequired = false;
      
      _obj.State.Properties.Representatives.IsRequired = false;
      _obj.State.Properties.Representatives.Properties.IssuedTo.IsRequired = false;
      _obj.State.Properties.Representatives.Properties.Agent.IsRequired = false;      
    }
    
    /// <summary>
    /// Снять обязательность свойства представителя,
    /// если формат версии 003 и представитель ЮЛ.
    /// </summary>
    private void RemoveRepresentativeMandatoryIfNeeded()
    {
      if (_obj.IsManyRepresentatives == false &&
         _obj.FormatVersion == FormatVersion.Version003 &&
         _obj.AgentType == AgentType.LegalEntity)
        _obj.State.Properties.Representative.IsRequired = false;
    }    
    
    #endregion
    
    #region Доступность действий
    
    /// <summary>
    /// Проверить возможность импорта доверенности.
    /// </summary>
    /// <returns>True - да, иначе - false.</returns>
    [Public]
    public virtual bool CanImportVersionWithSignature()
    {
      if (_obj.FtsListState != null)
        return false;
      
      if (_obj.HasVersions || _obj.LastVersionApproved == true)
        return false;
      
      return FormalizedPowerOfAttorneys.AccessRights.CanCreate() &&
        FormalizedPowerOfAttorneys.AccessRights.CanApprove() &&
        (Functions.Module.IsLockedByMe(_obj) || _obj.State.IsInserted);
    }
    
    /// <summary>
    /// Проверить возможность сформировать доверенность (передоверие).
    /// </summary>
    /// <returns>True - да, иначе - false.</returns>
    [Public]
    public virtual bool CanGenerateBodyWithPdf()
    {
      if (_obj.State.IsInserted)
        return false;
      
      if (_obj.FtsListState != null &&
          _obj.FtsListState != Docflow.FormalizedPowerOfAttorney.FtsListState.Rejected)
        return false;
      
      if (this.IsImported())
        return false;
      
      if (_obj.LastVersionApproved == true &&
          _obj.FtsListState != null &&
          _obj.FtsListState != Docflow.FormalizedPowerOfAttorney.FtsListState.Rejected)
        return false;
      
      return _obj.AccessRights.CanUpdate() &&
        (Functions.Module.IsLockedByMe(_obj) || _obj.State.IsInserted);
    }
    
    /// <summary>
    /// Проверить возможность зарегистрировать доверенность.
    /// </summary>
    /// <returns>True - да, иначе - false.</returns>
    [Public]
    public virtual bool CanRegisterWithService()
    {
      if (_obj.FtsListState != null &&
          _obj.FtsListState != Docflow.FormalizedPowerOfAttorney.FtsListState.Rejected)
        return false;
      
      return _obj.HasVersions &&
        _obj.AccessRights.CanUpdate() &&
        Functions.Module.IsLockedByMe(_obj);
    }
    
    /// <summary>
    /// Проверить возможность актуализировать состояние доверенности.
    /// </summary>
    /// <returns>True - да, иначе - false.</returns>
    [Public]
    public virtual bool CanCheckStateWithService()
    {
      return _obj.AccessRights.CanUpdate() && _obj.HasVersions &&
        Functions.Module.IsLockedByMe(_obj);
    }
    
    /// <summary>
    /// Проверить возможность отозвать доверенность.
    /// </summary>
    /// <returns>True - да, иначе - false.</returns>
    [Public]
    public virtual bool CanCreateRevocation()
    {
      if (_obj.FtsListState != Docflow.FormalizedPowerOfAttorney.FtsListState.Registered &&
          _obj.FtsListState != Docflow.FormalizedPowerOfAttorney.FtsListState.OnRevoke)
        return false;
      
      if (_obj.FtsListState == Docflow.FormalizedPowerOfAttorney.FtsListState.Registered &&
          _obj.ValidTill.HasValue && _obj.ValidTill < Calendar.UserToday)
        return false;
      
      if (_obj.IsNotarized == true) return false;
      
      return _obj.AccessRights.CanRead() &&
        (_obj.LastVersionApproved == true || this.IsImported());
    }
    
    #endregion
    
    #region Поиск дублей
    
    /// <summary>
    /// Получить текст ошибки о наличии дублей.
    /// </summary>
    /// <returns>Текст ошибки или пустая строка, если ошибок нет.</returns>
    public virtual string GetDuplicatesErrorText()
    {
      var duplicates = this.GetDuplicates();
      
      if (!duplicates.Any())
        return string.Empty;
      
      // Сформировать текст ошибки.
      return FormalizedPowerOfAttorneys.Resources.DuplicatesDetected;
    }
    
    /// <summary>
    /// Получить дубли эл. доверенности.
    /// </summary>
    /// <returns>Дубли эл. доверенности.</returns>
    public virtual List<IFormalizedPowerOfAttorney> GetDuplicates()
    {
      return Functions.FormalizedPowerOfAttorney.Remote.GetFormalizedPowerOfAttorneyDuplicates(_obj);
    }
    
    #endregion
    
    public override void SetLifeCycleState()
    {
      // Эл. доверенность становится действующей только если она зарегистрирована в ФНС,
      // независимо от того, является ли она автонумеруемой.
      return;
    }
    
    /// <summary>
    /// Проверить корректность значений для обязательных свойств.
    /// </summary>
    /// <returns>True, если свойства заполнены корректно, иначе - false.</returns>
    public virtual bool CheckRequiredPropertiesValues()
    {
      if (_obj.OurSignatory == null || _obj.OurSignatory?.Person?.DateOfBirth == null)
      {
        Logger.ErrorFormat("Execute CheckRequiredPropertiesValues: formalized power of attorney id {0}, date of birth of our signatory not specified.", _obj.Id);
        return false;
      }
      
      foreach (var representative in _obj.Representatives)
      {
        if (representative.Agent != null && !representative.Agent.DateOfBirth.HasValue)
        {
          Logger.ErrorFormat("Execute CheckRequiredPropertiesValues: formalized power of attorney id {0}, date of birth of representative not specified.", _obj.Id);
          return false;
        }
        
        var person = representative.AgentType == PowerOfAttorneyBaseRepresentatives.AgentType.Person ? People.As(representative.IssuedTo) : null;        
        if (person != null && !person.DateOfBirth.HasValue)
        {
          Logger.ErrorFormat("Execute CheckRequiredPropertiesValues: formalized power of attorney id {0}, date of birth of issued to not specified.", _obj.Id);
          return false;
        }
        
        if (person != null && person.Citizenship == null)
        {
          Logger.ErrorFormat("Execute CheckRequiredPropertiesValues: formalized power of attorney id {0}, citizenship of issued to not specified.", _obj.Id);
          return false;
        }
      }
      
      if (!this.ValidateAgentIdentityKind())
        return false;
      
      if (_obj.BusinessUnit == null || _obj.Department == null)
      {
        Logger.ErrorFormat("Execute CheckRequiredPropertiesValues: formalized power of attorney id {0}, business unit or department not specified.", _obj.Id);
        return false;
      }
      
      if (_obj.OurSignatory == null || _obj.OurSigningReason == null)
      {
        Logger.ErrorFormat("Execute CheckRequiredPropertiesValues: formalized power of attorney id {0}, signatory or signing reason not specified.", _obj.Id);
        return false;
      }
      
      if (_obj.ValidFrom == null || _obj.ValidTill == null)
      {
        Logger.ErrorFormat("Execute CheckRequiredPropertiesValues: formalized power of attorney id {0}, valid date not specified.", _obj.Id);
        return false;
      }
      
      if (string.IsNullOrWhiteSpace(_obj.BusinessUnit.PSRN))
      {
        Logger.ErrorFormat("Execute CheckRequiredPropertiesValues: formalized power of attorney id {0}, business unit PSRN is not specified.", _obj.Id);
        return false;
      }
      
      return true;
    }

    /// <summary>
    /// Проверка типа удостоверения личности представителя.
    /// </summary>
    /// <returns>True - если все реквизиты заполнены верно.</returns>
    public virtual bool ValidateAgentIdentityKind()
    {
      if (_obj.FormatVersion != FormatVersion.Version003)
        return true;
      
      var allowedIdentityKinds = Functions.Module.Remote.GetDocflowParamsStringValue(Constants.Module.FPoAIdentityDocumentCodesParamName)?.Split(',') ??
        Constants.Module.FPoAIdentityDocumentCodes.Split(',');

      foreach (var representative in _obj.Representatives)
      {
        var person = representative.AgentType == PowerOfAttorneyBaseRepresentatives.AgentType.Person ? People.As(representative.IssuedTo) : null;
        var agent = representative.AgentType == PowerOfAttorneyBaseRepresentatives.AgentType.Entrepreneur ? representative.Agent : null;
        
        if (person != null && person.IdentityKind == null)
        {
          Logger.ErrorFormat("Execute ValidateAgentIdentityKind: formalized power of attorney id {0}, identity document of issued to not specified.", _obj.Id);
          return false;
        }
        
        if (agent != null && agent.IdentityKind == null)
        {
          Logger.ErrorFormat("Execute ValidateAgentIdentityKind: formalized power of attorney id {0}, identity document of representative not specified.", _obj.Id);
          return false;
        }
        
        if (person != null && !allowedIdentityKinds.Contains(person?.IdentityKind?.Code) ||
            agent != null && !allowedIdentityKinds.Contains(agent?.IdentityKind?.Code))
        {
          Logger.ErrorFormat("Execute ValidateAgentIdentityKind: formalized power of attorney id {0}, identity document of issued to or representative is not supported.", _obj.Id);
          return false;
        }
      }
      
      return true;
    }
    
    /// <summary>
    /// Определить необходимость установить обязательные поля.
    /// </summary>
    /// <param name="hasExternalSignature">Есть ли внешняя подпись.</param>
    /// <returns>Нужно ли устанавливать обязательные поля.</returns>
    public virtual bool NeedSetRequiredProperties(bool hasExternalSignature)
    {
      return hasExternalSignature && (_obj.LifeCycleState == LifeCycleState.Draft || _obj.FtsListState == FtsListState.Rejected) ||
        !hasExternalSignature && (_obj.FtsListState == null || _obj.FtsListState == FtsListState.OnRegistration || _obj.FtsListState == FtsListState.Rejected);
    }
    
    /// <summary>
    /// Установить признак "Эл. доверенность только что была импортирована".
    /// </summary>
    [Public]
    public virtual void SetJustImportedParam()
    {
      var fpoaParams = ((Sungero.Domain.Shared.IExtendedEntity)_obj).Params;
      if (fpoaParams.ContainsKey(Constants.FormalizedPowerOfAttorney.FPoAWasJustImportedParamName))
        fpoaParams[Constants.FormalizedPowerOfAttorney.FPoAWasJustImportedParamName] = true;
      else
        fpoaParams.Add(Constants.FormalizedPowerOfAttorney.FPoAWasJustImportedParamName, true);
    }

    /// <summary>
    /// Проверить данные для поиска эл. доверенности на сайте ФНС.
    /// </summary>
    /// <returns>True, если данные корректные, иначе - false.</returns>
    public virtual bool CheckSearchData()
    {
      if ((_obj.AgentType == AgentType.Entrepreneur || _obj.AgentType == AgentType.LegalEntity) &&
          _obj.Representative == null && _obj.IssuedToParty == null)
      {
        return false;
      }
      
      if ((_obj.AgentType == AgentType.Employee || _obj.AgentType == AgentType.Person) && _obj.IssuedToParty == null)
      {
        return false;
      }

      if (_obj.BusinessUnit == null)
      {
        return false;
      }

      var issuerTin = _obj.BusinessUnit.TIN;
      if (string.IsNullOrEmpty(issuerTin))
      {
        return false;
      }

      return _obj.Representatives.Any(x => !string.IsNullOrEmpty(x.Agent?.TIN) || !string.IsNullOrEmpty(x.IssuedTo?.TIN));
    }
    
    /// <summary>
    /// Проверить, что процесс регистрации эл. доверенности завершен.
    /// </summary>
    /// <returns>True, если процесс регистрации завершен.</returns>
    [Public]
    public virtual bool CheckRegistrationProcessIsComplete()
    {
      return _obj.FtsListState == Docflow.FormalizedPowerOfAttorney.FtsListState.Registered ||
        _obj.FtsListState == Docflow.FormalizedPowerOfAttorney.FtsListState.Rejected;
    }
    
    /// <summary>
    /// Проверить, импортирована ли эл. доверенность.
    /// </summary>
    /// <returns>True - эл. доверенность импортирована.</returns>
    public bool IsImported()
    {
      var documentParams = ((Sungero.Domain.Shared.IExtendedEntity)_obj).Params;
      return documentParams.ContainsKey(Constants.FormalizedPowerOfAttorney.IsLastVersionApprovedParamName) &&
        (bool)documentParams[Constants.FormalizedPowerOfAttorney.IsLastVersionApprovedParamName];
    }
  }
}