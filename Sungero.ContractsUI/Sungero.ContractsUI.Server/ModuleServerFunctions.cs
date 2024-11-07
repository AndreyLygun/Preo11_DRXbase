using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Contracts;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.ContractsUI.Server
{
  public class ModuleFunctions
  {
    #region Виджеты
    
    /// <summary>
    /// Получить все договоры и доп. соглашения в стадии согласования, где Я ответственный.
    /// </summary>
    /// <param name="query">Запрос виджета.</param>
    /// <param name="substitution">Параметр "Учитывать замещения".</param>
    /// <param name="show">Параметр "Показывать".</param>
    /// <returns>Список договоров и доп. соглашений.</returns>
    public IQueryable<Sungero.Contracts.IContractualDocument> GetMyContractualDocuments(IQueryable<Sungero.Contracts.IContractualDocument> query,
                                                                                        bool substitution,
                                                                                        Enumeration show)
    {
      query = query.Where(cd => ContractBases.Is(cd) || SupAgreements.Is(cd));
      
      // Проверить статус жизненного цикла.
      query = query.Where(cd => cd.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Draft)
        .Where(cd => cd.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnApproval ||
               cd.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnRework ||
               cd.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.PendingSign ||
               cd.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed ||
               cd.ExternalApprovalState == Docflow.OfficialDocument.ExternalApprovalState.OnApproval);
      
      // Если показывать надо не все Договоры, то дофильтровываем по ответственному.
      if (show != Sungero.ContractsUI.Widgets.MyContracts.Show.All)
      {
        var currentEmployee = Company.Employees.Current;
        var responsibleEmployees = new List<IUser>();
        if (currentEmployee != null)
          responsibleEmployees.Add(currentEmployee);
        
        var employeeDepartment = currentEmployee != null ? currentEmployee.Department : null;
        
        // Учитывать замещения, если выставлен соответствующий параметр.
        if (substitution)
        {
          var substitutions = Substitutions.ActiveSubstitutedUsersWithoutSystem;
          responsibleEmployees.AddRange(substitutions);
        }
        
        // Если выбрано значение параметра "Договоры подразделения", то добавляем к списку ответственных всех сотрудников подразделения.
        if (show == Sungero.ContractsUI.Widgets.MyContracts.Show.Department)
        {
          var departmentsEmployees = Company.Employees.GetAll(e => Equals(e.Department, employeeDepartment));
          responsibleEmployees.AddRange(departmentsEmployees);
        }

        query = query.Where(cd => responsibleEmployees.Contains(cd.ResponsibleEmployee));
      }
      
      return query;
    }
    
    /// <summary>
    /// Получить все договоры и доп. соглашения на завершении, где Я ответственный.
    /// </summary>
    /// <param name="query">Запрос виджета.</param>
    /// <param name="needAutomaticRenewal">Признак "С пролонгацией".</param>
    /// <param name="substitution">Параметр "Учитывать замещения".</param>
    /// <param name="show">Параметр "Показывать".</param>
    /// <returns>Список договоров и доп. соглашений на завершении.</returns>
    public IQueryable<Sungero.Contracts.IContractualDocument> GetMyExpiringSoonContracts(IQueryable<Sungero.Contracts.IContractualDocument> query,
                                                                                         bool? needAutomaticRenewal,
                                                                                         bool substitution,
                                                                                         Enumeration show)
    {
      var today = Calendar.UserToday;
      var lastDate = today.AddDays(14);
      // Если показывать надо не все Договоры, то дофильтровываем по ответственному.
      if (show != Sungero.ContractsUI.Widgets.MyContracts.Show.All)
      {
        var currentEmployee = Employees.Current;
        var responsibleEmployees = new List<IUser>();
        if (currentEmployee != null)
          responsibleEmployees.Add(currentEmployee);
        
        var employeeDepartment = currentEmployee != null ? currentEmployee.Department : null;
        
        // Учитывать замещения, если выставлен соответствующий параметр.
        if (substitution)
        {
          var substitutions = Substitutions.ActiveSubstitutedUsersWithoutSystem;
          responsibleEmployees.AddRange(substitutions);
        }
        
        // Если выбрано значение параметра "Договоры подразделения", то добавляем к списку ответственных всех сотрудников подразделения.
        if (show == Sungero.ContractsUI.Widgets.MyContracts.Show.Department)
        {
          var departmentsEmployees = Company.Employees.GetAll(e => Equals(e.Department, employeeDepartment));
          responsibleEmployees.AddRange(departmentsEmployees);
        }
        
        query = query.Where(cd => responsibleEmployees.Contains(cd.ResponsibleEmployee));
      }
      query = query
        .Where(q => ContractBases.Is(q) || SupAgreements.Is(q))
        .Where(q => q.LifeCycleState == Sungero.Contracts.SupAgreement.LifeCycleState.Active);
      
      query = query.Where(q => q.ValidTill.HasValue)
        .Where(q => (ContractBases.Is(q) && today.AddDays(ContractBases.As(q).DaysToFinishWorks ?? 14) >= q.ValidTill) ||
               (SupAgreements.Is(q) && q.ValidTill.Value <= lastDate))
        .Where(q => SupAgreements.Is(q) || ContractBases.Is(q) && (ContractBases.As(q).DaysToFinishWorks == null ||
                                                                   ContractBases.As(q).DaysToFinishWorks <= Docflow.PublicConstants.Module.MaxDaysToFinish));

      // Признак с автопролонгацией у договоров.
      if (needAutomaticRenewal.HasValue)
        query = query.Where(q => ContractBases.Is(q) &&
                            ContractBases.As(q).IsAutomaticRenewal.HasValue &&
                            ContractBases.As(q).IsAutomaticRenewal.Value == needAutomaticRenewal.Value);
      
      return query;
    }

    #endregion
    
    #region Фильтрация списков
    
    #region Список "Документы у контрагентов"
    
    /// <summary>
    /// Отфильтровать действующие виды документов с документопотоком "Договоры".
    /// </summary>
    /// <param name="query">Фильтруемые виды документов.</param>
    /// <param name="withoutActs">True, если получить наследников договоров и доп. соглашений. Иначе - все договорные виды документов.</param>
    /// <returns>Виды документов.</returns>
    [Public, Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте серверный метод ContractsFilterContractsKind модуля Contracts.")]
    public static IQueryable<Docflow.IDocumentKind> ContractsFilterContractsKind(IQueryable<Docflow.IDocumentKind> query, bool withoutActs)
    {
      return Contracts.PublicFunctions.Module.ContractsFilterContractsKind(query, withoutActs);
    }
    
    /// <summary>
    /// Отфильтровать договорные и финансовые документы по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные документы.</returns>
    /// <remarks>Условия, которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> ContractsAtContractorsApplyStrongFilter(IQueryable<Sungero.Docflow.IOfficialDocument> query, Sungero.ContractsUI.FolderFilterState.IContractsAtContractorsFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "Категория".
      if (filter.Category != null)
        query = query.Where(c => !ContractBases.Is(c) || (ContractBases.Is(c) && Equals(c.DocumentGroup, filter.Category)));
      
      // Фильтр "Контрагент".
      if (filter.Contractor != null)
        query = query.Where(c => (Docflow.ContractualDocumentBases.Is(c) && Equals(Docflow.ContractualDocumentBases.As(c).Counterparty, filter.Contractor)) ||
                            (Docflow.AccountingDocumentBases.Is(c) && Equals(Docflow.AccountingDocumentBases.As(c).Counterparty, filter.Contractor)));
      
      // Фильтр "Ответственный за возврат".
      if (filter.Responsible != null)
        query = query.Where(c => Equals(c.ResponsibleForReturnEmployee, filter.Responsible));
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договорные и финансовые документы по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">Документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные документы.</returns>
    /// <remarks>Условия, которые используют индексы, но не максимально оптимально.</remarks>
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> ContractsAtContractorsApplyOrdinaryFilter(IQueryable<Sungero.Docflow.IOfficialDocument> query, Sungero.ContractsUI.FolderFilterState.IContractsAtContractorsFilterState filter)
    {
      // Фильтр "Вид документа".
      if (filter.DocumentKind != null)
        query = query.Where(c => Equals(c.DocumentKind, filter.DocumentKind));
      
      // Фильтр "Наша организация".
      if (filter.BusinessUnit != null)
        query = query.Where(c => Equals(c.BusinessUnit, filter.BusinessUnit));
      
      // Фильтр "Только просроченные".
      if (filter.OnlyOverdue)
      {
        var today = Calendar.UserToday;
        query = query.Where(c => c.ScheduledReturnDateFromCounterparty < today);
      }
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договорные и финансовые документы по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">Документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные документы.</returns>
    /// <remarks>Условия, которые могут выполняться долго (например, те которые не могут использовать индексы).</remarks>
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> ContractsAtContractorsApplyWeakFilter(IQueryable<Sungero.Docflow.IOfficialDocument> query, Sungero.ContractsUI.FolderFilterState.IContractsAtContractorsFilterState filter)
    {
      return query;
    }
    
    /// <summary>
    /// Определить, нужно ли использовать предварительную фильтрацию в списке "Документы у контрагентов".
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True, если нужно использовать предварительную фильтрацию.</returns>
    public virtual bool UsePrefilterContractsAtContractors(Sungero.ContractsUI.FolderFilterState.IContractsAtContractorsFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.Category != null ||
         filter.Contractor != null ||
         filter.Responsible != null);
      return hasStrongFilter;
    }
    
    #endregion
    
    #region Реестр договоров
    
    /// <summary>
    /// Отфильтровать договорные и финансовые документы по типам и признаку "Требуется возврат от контрагента".
    /// </summary>
    /// <param name="query">Документы для фильтрации.</param>
    /// <returns>Отфильтрованные документы.</returns>
    /// <remarks>Фильтрует по условиям, попадающим в индекс, но не сокращающим выборку на порядки.
    /// Рекомендуется использовать в событии фильтрации.</remarks>
    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте серверный метод ContractsAtContractorsApplyInvariantFilter модуля Contracts.")]
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> ContractsAtContractorsApplyInvariantFilter(IQueryable<Sungero.Docflow.IOfficialDocument> query)
    {
      return Contracts.PublicFunctions.Module.ContractsAtContractorsApplyInvariantFilter(query);
    }
    
    /// <summary>
    /// Отфильтровать договорные документы по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Договорные документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договорные документы.</returns>
    /// <remarks>Условия, которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractualDocumentsApplyStrongFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.ContractsUI.FolderFilterState.IContractsListFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "Категория".
      if (filter.Category != null)
        query = query.Where(c => ContractBases.Is(c) && Equals(c.DocumentGroup, filter.Category));
      
      // Фильтр "Контрагент".
      if (filter.Contractor != null)
        query = query.Where(c => Equals(c.Counterparty, filter.Contractor));
      
      // Фильтр "Подразделение".
      if (filter.Department != null)
        query = query.Where(c => Equals(c.Department, filter.Department));
      
      // Фильтрация по дате договора
      if (filter.Last30days)
        query = this.ContractualDocumentsApplyFilterByDate(query, filter);
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договорные документы по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">Договорные документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договорные документы.</returns>
    /// <remarks>Условия, которые используют индексы, но не максимально оптимально.</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractualDocumentsApplyOrdinaryFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.ContractsUI.FolderFilterState.IContractsListFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр по состоянию.
      var statuses = new List<Enumeration>();
      if (filter.Draft)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Draft);
      if (filter.Active)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Active);
      if (filter.Executed)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Closed);
      if (filter.Terminated)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Terminated);
      if (filter.Cancelled)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Obsolete);

      // Фильтр по состоянию.
      if (statuses.Any())
        query = query.Where(q => q.LifeCycleState != null && statuses.Contains(q.LifeCycleState.Value));
      
      // Фильтр "Вид документа".
      if (filter.DocumentKind != null)
        query = query.Where(c => Equals(c.DocumentKind, filter.DocumentKind));
      
      // Фильтр "Наша организация".
      if (filter.BusinessUnit != null)
        query = query.Where(c => Equals(c.BusinessUnit, filter.BusinessUnit));
      
      // Фильтрация по дате договора
      if (filter.Last365days || filter.ManualPeriod)
        query = this.ContractualDocumentsApplyFilterByDate(query, filter);
      
      // Фильтр по типу документа
      if (filter.Contracts != filter.SupAgreements)
      {
        if (filter.Contracts)
          query = query.Where(d => ContractBases.Is(d));

        if (filter.SupAgreements)
          query = query.Where(d => SupAgreements.Is(d));
      }
      else
        query = query.Where(d => ContractBases.Is(d) || SupAgreements.Is(d));
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договорные документы по неоптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Договорные документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договорные документы.</returns>
    /// <remarks>Условия, которые могут выполняться долго (например, те которые не могут использовать индексы).</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractualDocumentsApplyWeakFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.ContractsUI.FolderFilterState.IContractsListFilterState filter)
    {
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договорные документы по дате договора.
    /// </summary>
    /// <param name="query">Договорные документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договорные документы.</returns>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractualDocumentsApplyFilterByDate(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.ContractsUI.FolderFilterState.IContractsListFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      var beginDate = Calendar.UserToday.AddDays(-30);
      var endDate = Calendar.UserToday;
      
      if (filter.Last365days)
        beginDate = Calendar.UserToday.AddDays(-365);
      
      if (filter.ManualPeriod)
      {
        beginDate = filter.DateRangeFrom ?? Calendar.SqlMinValue;
        endDate = filter.DateRangeTo ?? Calendar.SqlMaxValue;
      }
      
      query = Docflow.PublicFunctions.Module.OfficialDocumentsApplyFilterByDate(query, beginDate, endDate)
        .Cast<Sungero.Contracts.IContractualDocument>();
      
      return query;
    }
    
    /// <summary>
    /// Определить, нужно ли использовать предварительную фильтрацию для договорных документов.
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True, если нужно использовать предварительную фильтрацию.</returns>
    public virtual bool UsePrefilterContractualDocuments(Sungero.ContractsUI.FolderFilterState.IContractsListFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.Category != null ||
         filter.Contractor != null ||
         filter.Department != null ||
         filter.Last30days);
      return hasStrongFilter;
    }
    
    #endregion
    
    #region История договоров
    
    /// <summary>
    /// Отфильтровать историю договоров по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">История договоров для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованная история договоров.</returns>
    /// <remarks>Условия которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractsHistoryApplyStrongFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.ContractsUI.FolderFilterState.IContractsHistoryFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "Категория".
      if (filter.Category != null)
        query = query.Where(c => Equals(c.DocumentGroup, filter.Category));
      
      // Фильтр "Ответственный".
      if (filter.Responsible != null)
        query = query.Where(c => Equals(c.ResponsibleEmployee, filter.Responsible));
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать историю договоров по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">История договоров для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованная история договоров.</returns>
    /// <remarks>Условия которые используют индексы но не максимально оптимально.</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractsHistoryApplyOrdinaryFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.ContractsUI.FolderFilterState.IContractsHistoryFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      #region Фильтр по состоянию и датам
      
      DateTime beginPeriod = Calendar.SqlMinValue;
      DateTime endPeriod = Calendar.UserToday.EndOfDay().FromUserTime();
      
      if (filter.Last30days)
        beginPeriod = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-30));
      
      if (filter.Last365days)
        beginPeriod = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-365));
      
      if (filter.ManualPeriod)
      {
        if (filter.DateRangeFrom.HasValue)
          beginPeriod = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(filter.DateRangeFrom.Value);
        
        endPeriod = filter.DateRangeTo.HasValue ? filter.DateRangeTo.Value.EndOfDay().FromUserTime() : Calendar.SqlMaxValue;
      }
      
      Enumeration? operation = null;
      var lifeCycleStates = new List<Enumeration>();
      
      if (filter.Concluded)
      {
        operation = new Enumeration(Constants.Module.SetToActiveOperationName);
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Active);
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Closed);
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Terminated);
      }
      
      if (filter.Executed)
      {
        operation = new Enumeration(Constants.Module.SetToClosedOperationName);
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Closed);
      }
      
      if (filter.Terminated)
      {
        operation = new Enumeration(Constants.Module.SetToTerminatedOperationName);
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Terminated);
      }
      
      if (filter.Cancelled)
      {
        operation = new Enumeration(Constants.Module.SetToObsoleteOperationName);
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Obsolete);
      }
      
      // Использовать можно только один WhereDocumentHistory, т.к. это отдельный подзапрос (+join).
      query = query.Where(d => d.LifeCycleState.HasValue && lifeCycleStates.Contains(d.LifeCycleState.Value))
        .WhereDocumentHistory(h => h.Operation == operation && h.HistoryDate.Between(beginPeriod, endPeriod));
      
      #endregion
      
      // Фильтр "Вид документа".
      if (filter.DocumentKind != null)
        query = query.Where(c => Equals(c.DocumentKind, filter.DocumentKind));
      
      // Фильтр "Наша организация".
      if (filter.BusinessUnit != null)
        query = query.Where(c => Equals(c.BusinessUnit, filter.BusinessUnit));
      
      // Фильтр "Тип документа".
      if (filter.SupAgreements || filter.Contracts)
        query = query.Where(d => filter.Contracts && ContractBases.Is(d) || filter.SupAgreements && SupAgreements.Is(d));
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать историю договоров по неоптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">История договоров для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованная история договоров.</returns>
    /// <remarks>Условия которые могут выполняться долго (например те которые не могут использовать индексы).</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractsHistoryApplyWeakFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.ContractsUI.FolderFilterState.IContractsHistoryFilterState filter)
    {
      return query;
    }
    
    /// <summary>
    /// Определить нужно ли использовать предварительную фильтрацию для истории договоров.
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True если нужно использовать предварительную фильтрацию.</returns>
    public virtual bool UsePrefilterContractsHistory(Sungero.ContractsUI.FolderFilterState.IContractsHistoryFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.Category != null ||
         filter.Responsible != null);
      return hasStrongFilter;
    }
    
    /// <summary>
    /// Отфильтровать договоры на завершении по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Договорные документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договорные документы.</returns>
    /// <remarks>Условия, которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ExpiringSoonContractualDocumentsApplyStrongFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.ContractsUI.FolderFilterState.IExpiringSoonContractsFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "Категория".
      if (filter.Category != null)
        query = query.Where(c => ContractBases.Is(c) && Equals(c.DocumentGroup, filter.Category));
      
      // Фильтр "Контрагент".
      if (filter.Contractor != null)
        query = query.Where(c => Equals(c.Counterparty, filter.Contractor));
      
      // Фильтр "Ответственный".
      if (filter.Responsible != null)
        query = query.Where(c => Equals(c.ResponsibleEmployee, filter.Responsible));
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договорные на завершении по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">Договорные документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договорные документы.</returns>
    /// <remarks>Условия, которые используют индексы, но не максимально оптимально.</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ExpiringSoonContractualDocumentsApplyOrdinaryFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.ContractsUI.FolderFilterState.IExpiringSoonContractsFilterState filter)
    {
      if (query == null || filter == null)
        return query;
      
      // Фильтр "Вид документа".
      if (filter.DocumentKind != null)
        query = query.Where(d => Equals(d.DocumentKind, filter.DocumentKind));
      
      // Фильтр "Наша организация".
      if (filter.BusinessUnit != null)
        query = query.Where(d => Equals(d.BusinessUnit, filter.BusinessUnit));
      
      // Фильтр по типу документа
      if (filter.Contracts && !filter.SupAgreements)
        return query.Where(d => ContractBases.Is(d));
      if (filter.SupAgreements && !filter.Contracts)
        return query.Where(d => SupAgreements.Is(d));
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договоры на завершении по неоптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Договорные документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договорные документы.</returns>
    /// <remarks>Условия, которые могут выполняться долго (например, те которые не могут использовать индексы).</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ExpiringSoonContractualDocumentsApplyWeakFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.ContractsUI.FolderFilterState.IExpiringSoonContractsFilterState filter)
    {
      return query;
    }
    
    /// <summary>
    /// Определить, нужно ли использовать предварительную фильтрацию для договоров на завершении.
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True, если нужно использовать предварительную фильтрацию.</returns>
    public virtual bool UsePrefilterExpiringSoonContractualDocuments(Sungero.ContractsUI.FolderFilterState.IExpiringSoonContractsFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.Category != null ||
         filter.Contractor != null ||
         filter.Responsible != null);
      return hasStrongFilter;
    }
    
    /// <summary>
    /// Отфильтровать действующие договоры, у которых осталось 14 (либо указанное в договоре число) дней до окончания документа.
    /// </summary>
    /// <param name="query">Договорные документы для фильтрации.</param>
    /// <returns>Отфильтрованные договорные документы.</returns>
    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте серверный метод ExpiringSoonContractualDocumentsApplyInvariantFilter модуля Contracts.")]
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ExpiringSoonContractualDocumentsApplyInvariantFilter(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      return Contracts.PublicFunctions.Module.ExpiringSoonContractualDocumentsApplyInvariantFilter(query);
    }
    
    #endregion
    
    #region Список "Документы у сотрудников"
    
    /// <summary>
    /// Отфильтровать список документов находящихся у сотрудников по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Список документов находящихся у сотрудников для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованный список документов.</returns>
    /// <remarks>Условия которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> IssuanceJournalApplyStrongFilter(IQueryable<Sungero.Docflow.IOfficialDocument> query, Sungero.ContractsUI.FolderFilterState.IIssuanceJournalFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр по делу.
      if (filter.File != null)
        query = query.Where(x => Equals(x.CaseFile, filter.File));
      
      // Фильтр по сотруднику.
      if (filter.Employee != null)
        query = query.Where(x => x.Tracking.Any(p => !p.ReturnDate.HasValue && Equals(p.DeliveredTo, filter.Employee)));
      
      // Фильтр по подразделению.
      if (filter.Department != null)
        query = query.Where(x => x.Tracking.Any(p => !p.ReturnDate.HasValue &&
                                                p.DeliveredTo != null &&
                                                Equals(p.DeliveredTo.Department, filter.Department)));
      
      // Фильтрация по сроку возврата.
      if (filter.EndDay)
        query = this.IssuanceJournalApplyFilterByDate(query, filter);
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать список документов находящихся у сотрудников по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">Список документов находящихся у сотрудников для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованный список документов.</returns>
    /// <remarks>Условия которые используют индексы но не максимально оптимально.</remarks>
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> IssuanceJournalApplyOrdinaryFilter(IQueryable<Sungero.Docflow.IOfficialDocument> query, Sungero.ContractsUI.FolderFilterState.IIssuanceJournalFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Ограничение по типу документа в списке.
      query = query.Where(x => (Sungero.Docflow.ContractualDocumentBases.Is(x) || Sungero.Docflow.AccountingDocumentBases.Is(x)));
      
      // Фильтр по виду документа.
      if (filter.DocumentKind != null)
        query = query.Where(x => Equals(x.DocumentKind, filter.DocumentKind));
      
      // Фильтрация по сроку возврата.
      if (filter.EndWeek || filter.EndMonth || filter.Manual)
        query = this.IssuanceJournalApplyFilterByDate(query, filter);
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать список документов находящихся у сотрудников по неоптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Список документов находящихся у сотрудников для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованный список документов.</returns>
    /// <remarks>Условия которые могут выполняться долго (например те которые не могут использовать индексы).</remarks>
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> IssuanceJournalApplyWeakFilter(IQueryable<Sungero.Docflow.IOfficialDocument> query, Sungero.ContractsUI.FolderFilterState.IIssuanceJournalFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Ограничение по признаку "Требуется возврат".
      query = query.Where(x => x.IsReturnRequired == true);
      
      // Фильтр по статусу.
      if (filter.Overdue)
        query = query
          .Where(x => x.Tracking.Any(d => (d.ReturnDate > d.ReturnDeadline) || (!d.ReturnDate.HasValue && d.ReturnDeadline < Calendar.UserToday)));
      
      // Фильтр по группе регистрации.
      if (filter.RegistrationGroup != null)
        query = query.Where(x => x.DocumentRegister != null && Equals(x.DocumentRegister.RegistrationGroup, filter.RegistrationGroup));
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать список документов находящихся у сотрудников по сроку возврата.
    /// </summary>
    /// <param name="query">Список документов находящихся у сотрудников для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованный список документов.</returns>
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> IssuanceJournalApplyFilterByDate(IQueryable<Sungero.Docflow.IOfficialDocument> query, Sungero.ContractsUI.FolderFilterState.IIssuanceJournalFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      var today = Calendar.UserToday;
      
      // Исключить строки из таблиц Выдачи с Результатом возврата: "Возвращен".
      var returned = Docflow.OfficialDocumentTracking.ReturnResult.Returned;
      
      // Фильтр по сроку возврата.
      if (filter.EndDay)
        query = query.Where(x => x.Tracking.Any(p => !Equals(p.ReturnResult, returned) &&
                                                p.ReturnDeadline < today.AddDays(1)));

      if (filter.EndWeek)
        query = query.Where(x => x.Tracking.Any(p => !Equals(p.ReturnResult, returned) &&
                                                p.ReturnDeadline <= today.EndOfWeek()));
      
      if (filter.EndMonth)
        query = query.Where(x => x.Tracking.Any(p => !Equals(p.ReturnResult, returned) &&
                                                p.ReturnDeadline <= today.EndOfMonth()));
      
      if (filter.Manual)
      {
        var dateFrom = filter.ReturnPeriodDataRangeFrom;
        var dateTo = filter.ReturnPeriodDataRangeTo;
        
        if (dateFrom.HasValue && !dateTo.HasValue)
          query = query.Where(x => x.Tracking.Any(p => !Equals(p.ReturnResult, returned) && p.ReturnDeadline >= dateFrom.Value));
        
        if (dateTo.HasValue && !dateFrom.HasValue)
          query = query.Where(x => x.Tracking.Any(p => !Equals(p.ReturnResult, returned) && p.ReturnDeadline <= dateTo.Value));
        
        if (dateFrom.HasValue && dateTo.HasValue)
          query = query.Where(x => x.Tracking.Any(p => !Equals(p.ReturnResult, returned) &&
                                                  p.ReturnDeadline.Between(dateFrom.Value, dateTo.Value)));
      }
      
      return query;
    }
    
    /// <summary>
    /// Определить нужно ли использовать предварительную фильтрацию для списка документов находящихся у сотрудников.
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True если нужно использовать предварительную фильтрацию.</returns>
    public virtual bool UsePrefilterIssuanceJournal(Sungero.ContractsUI.FolderFilterState.IIssuanceJournalFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.File != null ||
         filter.Employee != null ||
         filter.Department != null ||
         filter.EndDay);
      return hasStrongFilter;
    }
    
    #endregion

    #endregion
  }
}