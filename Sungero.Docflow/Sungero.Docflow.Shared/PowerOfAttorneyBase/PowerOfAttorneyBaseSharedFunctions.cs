using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorneyBase;
using FormatElement = Sungero.Docflow.DocumentRegisterNumberFormatItems.Element;

namespace Sungero.Docflow.Shared
{
  partial class PowerOfAttorneyBaseFunctions
  {
    #region Видимость и доступность полей
    
    /// <summary>
    /// Настроить у полей агента признаки видимости и обязательности.
    /// </summary>
    public virtual void SetAgentFieldsVisibleAndRequiredFlags()
    {
      var properties = _obj.State.Properties;

      properties.AgentType.IsVisible = _obj.IsManyRepresentatives == false;
      
      var isAgentTypeSet = properties.AgentType.IsVisible && _obj.AgentType != null;
      
      properties.IssuedToParty.IsVisible = isAgentTypeSet &&
        _obj.AgentType != AgentType.Employee;
      
      properties.Representative.IsVisible = isAgentTypeSet &&
        (_obj.AgentType == AgentType.LegalEntity || _obj.AgentType == AgentType.Entrepreneur);
      
      properties.IssuedTo.IsVisible = isAgentTypeSet && _obj.AgentType == AgentType.Employee;
    }
    
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);
      
      // Поле "Действует по" доступно для редактирования при изменении реквизитов и для доверенностей в разработке.
      var isDraft = _obj.LifeCycleState == Docflow.PowerOfAttorney.LifeCycleState.Draft;
      _obj.State.Properties.ValidTill.IsEnabled = isDraft || isEnabled;
      _obj.State.Properties.ValidFrom.IsEnabled = isDraft || isEnabled;

      // При перерегистрации "Кому выдана" недоступно, если в формате номера журнала есть код подразделения.
      var documentRegister = _obj.DocumentRegister;
      var departmentCodeIncludedInNumber = isRepeatRegister && documentRegister != null &&
        Functions.DocumentRegister.NumberFormatContains(documentRegister, FormatElement.DepartmentCode);
      _obj.State.Properties.IssuedTo.IsEnabled = isEnabled && !departmentCodeIncludedInNumber;
      
      _obj.State.Properties.AgentType.IsEnabled = isEnabled;
      _obj.State.Properties.IssuedToParty.IsEnabled = isEnabled;
      _obj.State.Properties.Representative.IsEnabled = isEnabled;
      
      _obj.State.Properties.Representatives.IsEnabled = isEnabled;
    }
    
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      
      if (_obj.IsManyRepresentatives == false)
      {
        _obj.State.Properties.AgentType.IsRequired = true;
        _obj.State.Properties.IssuedToParty.IsRequired = _obj.AgentType != AgentType.Employee;
        _obj.State.Properties.Representative.IsRequired = _obj.AgentType == AgentType.Entrepreneur || _obj.AgentType == AgentType.LegalEntity;
        _obj.State.Properties.IssuedTo.IsRequired = _obj.AgentType == AgentType.Employee;
        
        _obj.State.Properties.Representatives.IsRequired = false;
        _obj.State.Properties.Representatives.Properties.IssuedTo.IsRequired = false;
      }
      else
      {
        _obj.State.Properties.AgentType.IsRequired = false;
        _obj.State.Properties.IssuedTo.IsRequired = false;
        _obj.State.Properties.IssuedToParty.IsRequired = false;
        _obj.State.Properties.Representative.IsRequired = false;
        
        _obj.State.Properties.Representatives.IsRequired = true;
        _obj.State.Properties.Representatives.Properties.IssuedTo.IsRequired = true;
      }
    }
    
    public override void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
    {
      base.ChangeRegistrationPaneVisibility(needShow, repeatRegister);
      
      _obj.State.Properties.ExecutionState.IsVisible = false;
      _obj.State.Properties.ControlExecutionState.IsVisible = false;
      
    }

    /// <summary>
    /// Заполнить поля доверителя, если поле "Подготовил" было заполнено.
    /// </summary>
    /// <param name="preparedBy">Подготовил.</param>
    /// <param name="agentType">Тип представителя.</param>
    public virtual void FillPrincipalFields(Company.IEmployee preparedBy, Sungero.Core.Enumeration? agentType)
    {
      if (preparedBy != null && agentType != Docflow.PowerOfAttorneyBase.AgentType.Employee && _obj.BusinessUnit == null && _obj.Department == null)
      {
        _obj.BusinessUnit = preparedBy?.Department?.BusinessUnit;
        _obj.Department = preparedBy?.Department;
      }
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
        <Вид документа> для <Кому выдана> №<номер> от <дата> "<содержание>".
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        name += PowerOfAttorneyBases.Resources.DocumentNameFor + this.GetRepresentativeNamePart();
        
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
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
    
    /// <summary>
    /// Извлечь текст для названия доверенности обозначающий представителя.
    /// </summary>
    /// <returns>Часть названия доверенности после слова "для", обозначающее, кому выдана доверенность.</returns>
    /// <remarks>Если представитель один, то возвращаем его имя, если представителей несколько, то возвращаем фразу "нескольких представителей".</remarks>
    protected virtual string GetRepresentativeNamePart()
    {
      if (_obj.IsManyRepresentatives == true)
          return PowerOfAttorneyBases.Resources.ManyRepresentativesFor;
        
      if (_obj.IssuedTo != null && _obj.AgentType == Docflow.PowerOfAttorneyBase.AgentType.Employee)
        return _obj.IssuedTo.Name;
      else if (_obj.IssuedToParty != null)
        return _obj.IssuedToParty.Name;
      return string.Empty;
    }

    #endregion
    
    #region Представители
    
    /// <summary>
    /// Сбросить поля агента.
    /// </summary>
    public virtual void ResetAgentFields()
    {
      _obj.IssuedToParty = null;
      _obj.Representative = null;
      _obj.IssuedTo = null;
    }
    
    /// <summary>
    /// Очистка скрытых полей.
    /// </summary>
    public virtual void CleanupAgentFields()
    {
      if (_obj.AgentType == AgentType.Employee)
      {
        _obj.IssuedToParty = _obj.IssuedTo?.Person;
        _obj.Representative = null;
      }
      else if (_obj.AgentType == AgentType.Person)
      {
        _obj.IssuedTo = null;
        _obj.Representative = null;
      }
      else if (_obj.AgentType == AgentType.LegalEntity || _obj.AgentType == AgentType.Entrepreneur)
      {
        _obj.IssuedTo = null;
      }
    }
    
    /// <summary>
    /// Очистить таблицу с представителями.
    /// </summary>
    public virtual void ClearRepresentatives()
    {
      _obj.Representatives.Clear();
    }
    
    /// <summary>
    /// Копировать представителя в табличную часть.
    /// </summary>
    public virtual void CopyAgentToRepresentatives()
    {
      var newRow = _obj.Representatives.AddNew();
      
      newRow.AgentType = _obj.AgentType == AgentType.Employee
        ? AgentType.Person
        : _obj.AgentType;
      
      newRow.IssuedTo = _obj.AgentType == AgentType.Employee
        ? _obj.IssuedTo?.Person
        : _obj.IssuedToParty;
      
      newRow.Agent = _obj.AgentType == AgentType.Entrepreneur || _obj.AgentType == AgentType.LegalEntity
        ? _obj.Representative
        : null;
    }
    
    #endregion
  }
}