using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorneyBase;

namespace Sungero.Docflow
{
  partial class PowerOfAttorneyBaseRepresentativesIssuedToPropertyFilteringServerHandler<T>
  {
    
    public virtual IQueryable<T> RepresentativesIssuedToFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.AgentType == PowerOfAttorneyBaseRepresentatives.AgentType.Person)
        query = query.Where(x => !Sungero.Parties.CompanyBases.Is(x));
      
      if (_obj.AgentType == PowerOfAttorneyBaseRepresentatives.AgentType.LegalEntity ||
          _obj.AgentType == PowerOfAttorneyBaseRepresentatives.AgentType.Entrepreneur)
        query = query.Where(x => Sungero.Parties.CompanyBases.Is(x));
      
      query = query.Where(x => x.Status == CoreEntities.DatabookEntry.Status.Active);
      
      return query;
    }
  }

  partial class PowerOfAttorneyBaseIssuedToPartyPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> IssuedToPartyFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.AgentType == AgentType.Person)
        return query.Where(cp => !Sungero.Parties.CompanyBases.Is(cp));
      
      if (_obj.AgentType == AgentType.LegalEntity || _obj.AgentType == AgentType.Entrepreneur)
        return query.Where(cp => Sungero.Parties.CompanyBases.Is(cp));
      
      return query;
    }
  }

  partial class PowerOfAttorneyBaseFilteringServerHandler<T>
  {

    public override IQueryable<T> PreFiltering(IQueryable<T> query, Sungero.Domain.PreFilteringEventArgs e)
    {
      if (_filter == null)
        return base.PreFiltering(query, e);
      
      if (Functions.Module.UsePrefilterPowerOfAttorneys(_filter))
      {
        query = Functions.Module.PowerOfAttorneysApplyStrongFilter(query, _filter).Cast<T>();
        query = Functions.Module.PowerOfAttorneysApplyOrdinaryFilter(query, _filter).Cast<T>();
      }
      
      return query;
    }

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return base.Filtering(query, e);
      
      if (!Functions.Module.UsePrefilterPowerOfAttorneys(_filter))
        query = Functions.Module.PowerOfAttorneysApplyOrdinaryFilter(query, _filter).Cast<T>();
      
      query = Functions.Module.PowerOfAttorneysApplyWeakFilter(query, _filter).Cast<T>();

      return query;
    }
    
  }

  partial class PowerOfAttorneyBaseCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      e.Without(_info.Properties.ValidTill);
    }
  }

  partial class PowerOfAttorneyBaseServerHandlers
  {
    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      this.InitializeManyRepresentativesProperties();
      
      // Очистить поля Подразделение и НОР, заполненные в предке.
      if (!_obj.State.IsCopied)
      {
        _obj.Department = null;
        _obj.BusinessUnit = null;
        _obj.AgentType = Docflow.PowerOfAttorneyBase.AgentType.Employee;
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      this.ValidateDatesOfValidity(e);
      this.ValidateEmployeesDeletion(e);
      this.ArrangeRepresentatives(e);
      
      if (!e.IsValid) return;
      
      this.GrantRightToRepresentativeEmployees();
    }
    
    #region Виртуальные функции
    
    /// <summary>
    /// Определить, является ли поле обязательным.
    /// </summary>
    /// <param name="representative">Представитель.</param>
    /// <returns>True - является обязательным.</returns>
    protected virtual bool IsAgentInRepresentativeRowRequired(IPowerOfAttorneyBaseRepresentatives representative)
    {
      return representative.AgentType == AgentType.Entrepreneur || representative.AgentType == AgentType.LegalEntity;
    }
    
    /// <summary>
    /// Вернуть текст ошибки, если Представитель в списке заполнен некорректно.
    /// </summary>
    /// <param name="required">True - поле является обязательным.</param>
    /// <param name="filled">True - поле заполнено.</param>
    /// <returns>Текст ошибки.</returns>
    protected virtual string GetErrorMessageForIncorrectFilledAgent(bool required, bool filled)
    {
      if (required && !filled)
        return Sungero.Docflow.PowerOfAttorneyBases.Resources.RequiredAgentNotFilled;
      
      if (!required && filled)
        return Sungero.Docflow.PowerOfAttorneyBases.Resources.NotRequiredAgentIfPersonFilled;
      
      throw AppliedCodeException.Create(string.Format("GetErrorMessageForIncorrectFilledAgent. The agent is correct. Required = {0}, filled = {1}", required, filled));
    }
    
    #endregion
    
    #region Приватные функции
    
    #region Упорядочивание представителей
    
    /// <summary>
    /// Упорядочить данные о представителях в зависимости от состояния кнопки "Несколько представителей".
    /// </summary>
    /// <param name="e">Аргументы события "До сохранения".</param>
    private void ArrangeRepresentatives(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.IsManyRepresentatives == false)
      {
        Functions.PowerOfAttorneyBase.CleanupAgentFields(_obj);
        Functions.PowerOfAttorneyBase.ClearRepresentatives(_obj);
        Functions.PowerOfAttorneyBase.CopyAgentToRepresentatives(_obj);
      }
      else
      {
        if (IsVisualModeParameterSet(e) && !this.ValidateAgentsInRepresentativeList(e))
          return;
        
        Functions.PowerOfAttorneyBase.ResetAgentFields(_obj);
        _obj.AgentType = null;
      }
    }
    
    /// <summary>
    /// Проверить, установлен ли параметр "Визуальный режим".
    /// </summary>
    /// <param name="e">Аргументы события "До сохранения".</param>
    /// <returns>True - сохранение выполняется пользователем в UI.</returns>
    private static bool IsVisualModeParameterSet(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var result = false;
      return e.Params.TryGetValue(Constants.OfficialDocument.IsVisualModeParamName, out result) && result;
    }
    
    /// <summary>
    /// Проверить все строки таблицы с представителями на предмет некорректно заполненных представителей.
    /// </summary>
    /// <param name="e">Аргументы события "До сохранения".</param>
    /// <returns>True - все строки валидные.</returns>
    private bool ValidateAgentsInRepresentativeList(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var result = true;
      
      foreach (var representative in _obj.Representatives)
      {
        var required = this.IsAgentInRepresentativeRowRequired(representative);
        var filled = representative.Agent != null;
        
        if ((required && !filled) ||
            (!required && filled))
        {
          e.AddError(representative,
                     representative.Info.Properties.Agent,
                     this.GetErrorMessageForIncorrectFilledAgent(required, filled),
                     new[] { representative.Info.Properties.Agent });
          result = false;
        }
      }
      
      return result;
    }
    
    #endregion
    
    #region Проверка действующих прав подписи
    
    /// <summary>
    /// Проверить корректность удаления сотрудников из доверенности.
    /// </summary>
    /// <param name="e">Аргументы события "До сохранения".</param>
    private void ValidateEmployeesDeletion(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.State.IsInserted) return;
      
      if (this.EmployeesDeleted() && this.ActiveSignaturesExistForDeletedEmployees())
        e.AddError(PowerOfAttorneyBases.Resources.AlreadyExistSignatureSetting, _obj.Info.Actions.FindActiveSignatureSetting);
    }
    
    /// <summary>
    /// Проверить, есть ли удаленные представители, которые являются сотрудниками.
    /// </summary>
    /// <returns>True - хотя бы один сотрудник был удален.</returns>
    private bool EmployeesDeleted()
    {
      return this.GetDeletedEmployeesQuery().Count() > 0;
    }
    
    /// <summary>
    /// Проверить, есть ли действующие права подписи для удаленных сотрудников.
    /// </summary>
    /// <returns>True - есть хотя бы один удаленный сотрудник, для которого создано и активно право подписи.</returns>
    private bool ActiveSignaturesExistForDeletedEmployees()
    {
      var deletedEmployees = this.GetDeletedEmployeesQuery();
      return Functions.PowerOfAttorneyBase.GetActiveSignatureSettingsByPoA(_obj)
        .Where(x => deletedEmployees.Contains(x.Recipient))
        .Any();
    }
    
    /// <summary>
    /// Получить список представителей ФЛ, которые были удалены,
    /// и сформировать запрос на поиск сотрудников по ним.
    /// </summary>
    /// <returns>Запрос, возвращающий сотрудников по персонам, которые были удалены.</returns>
    private IQueryable<Sungero.Company.IEmployee> GetDeletedEmployeesQuery()
    {
      var deletedPeople = _obj.State.Properties.Representatives.Deleted
        .Where(x => x.AgentType == AgentType.Person && Sungero.Parties.People.Is(x.IssuedTo))
        .Select(x => Sungero.Parties.People.As(x.IssuedTo))
        .ToList();
      
      foreach (var representative in _obj.Representatives)
      {
        var agentType = representative.State.IsInserted
          ? representative.AgentType
          : representative.State.Properties.AgentType.OriginalValue;
        
        if (agentType != AgentType.Person)
          continue;
        
        if (_obj.IsManyRepresentatives == false)
        {
          if (Sungero.Parties.People.Is(representative.State.Properties.IssuedTo.OriginalValue))
            deletedPeople.Add(Sungero.Parties.People.As(representative.State.Properties.IssuedTo.OriginalValue));
          continue;
        }
        
        if (representative.State.IsInserted)
        {
          if (Sungero.Parties.People.Is(representative.IssuedTo))
            deletedPeople.Remove(Sungero.Parties.People.As(representative.IssuedTo));
        }
        else if (representative.State.IsChanged)
        {
          if (!Equals(representative.IssuedTo, representative.State.Properties.IssuedTo.OriginalValue) &&
              Sungero.Parties.People.Is(representative.State.Properties.IssuedTo.OriginalValue))
            deletedPeople.Add(Sungero.Parties.People.As(representative.State.Properties.IssuedTo.OriginalValue));
        }
      }
      
      if (_obj.IsManyRepresentatives == false && _obj.IssuedTo != null)
        deletedPeople.Remove(_obj.IssuedTo.Person);
      
      if (deletedPeople.Count == 0)
        return new List<Sungero.Company.IEmployee>().AsQueryable();
      
      return Sungero.Company.Employees.GetAll()
        .Where(x => deletedPeople.Contains(x.Person));
    }
    
    #endregion
    
    /// <summary>
    /// Проверить, корректно ли введен срок действия доверенности.
    /// </summary>
    /// <param name="e">Аргументы события "До сохранения".</param>
    private void ValidateDatesOfValidity(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.ValidFrom > _obj.ValidTill)
      {
        e.AddError(_obj.Info.Properties.ValidFrom, PowerOfAttorneyBases.Resources.IncorrectValidDates, _obj.Info.Properties.ValidTill);
        e.AddError(_obj.Info.Properties.ValidTill, PowerOfAttorneyBases.Resources.IncorrectValidDates, _obj.Info.Properties.ValidFrom);
      }
    }
    
    /// <summary>
    /// Установить значения по умолчанию для св-в "Несколько представителей" при создании.
    /// </summary>
    private void InitializeManyRepresentativesProperties()
    {
      if (_obj.IsManyRepresentatives == null)
        _obj.IsManyRepresentatives = false;
      
      if (string.IsNullOrEmpty(_obj.ManyRepresentativesPlaceholder))
        _obj.ManyRepresentativesPlaceholder = Sungero.Docflow.PowerOfAttorneyBases.Resources.ManyRepresentatives;
    }
    
    /// <summary>
    /// Выдать права на чтение всем представителям, которые являются сотрудниками.
    /// </summary>
    private void GrantRightToRepresentativeEmployees()
    {
      if (_obj.AccessRights.StrictMode != AccessRightsStrictMode.Enhanced)
      {
        var issuedToPeople = _obj.Representatives
          .Where(r => r.AgentType == PowerOfAttorneyBaseRepresentatives.AgentType.Person && r.IssuedTo != null)
          .Select(p => p.IssuedTo)
          .ToList();
        var issuedToEmployees = Company.Employees.GetAll().Where(x => issuedToPeople.Contains(x.Person));
        foreach (var issuedToEmployee in issuedToEmployees)
          _obj.AccessRights.Grant(issuedToEmployee, DefaultAccessRightsTypes.Read);
      }
    }
    
    #endregion
  }
}