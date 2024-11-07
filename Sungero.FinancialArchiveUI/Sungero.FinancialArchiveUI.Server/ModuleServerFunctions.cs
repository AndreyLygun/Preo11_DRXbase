using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.FinancialArchiveUI.Server
{
  public class ModuleFunctions
  {
    #region Фильтрация договоров и доп. соглашений
    
    /// <summary>
    /// Отфильтровать договоры и доп. соглашения по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Договоры и доп. соглашения для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договоры и доп. соглашения.</returns>
    /// <remarks>Условия, которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> FinContractListApplyStrongFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.FinancialArchiveUI.FolderFilterState.IFinContractListFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "Категория".
      if (filter.Category != null)
        query = query.Where(c => Equals(c.DocumentGroup, filter.Category));
      
      // Фильтр "Контрагент".
      if (filter.Contractor != null)
        query = query.Where(c => Equals(c.Counterparty, filter.Contractor));
      
      // Фильтр "Подразделение".
      if (filter.Department != null)
        query = query.Where(c => Equals(c.Department, filter.Department));
      
      // Фильтр "Период".
      if (filter.CurrentMonth || filter.PreviousMonth)
        query = this.FinContractListApplyFilterByDate(query, filter);
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договоры и доп. соглашения по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query"> Договоры и доп. соглашения для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Договоры и доп. соглашения.</returns>
    /// <remarks>Условия, которые используют индексы, но не максимально оптимально.</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> FinContractListApplyOrdinaryFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.FinancialArchiveUI.FolderFilterState.IFinContractListFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "Статус" (безусловный фильтр).
      query = query.Where(x => x.LifeCycleState == Contracts.ContractBase.LifeCycleState.Terminated ||
                          x.LifeCycleState == Contracts.ContractBase.LifeCycleState.Active ||
                          x.LifeCycleState == Contracts.ContractBase.LifeCycleState.Closed);
      
      // Фильтр "Тип документа".
      if (filter.Contracts || filter.SupAgreements)
        query = query.Where(d => (Sungero.Contracts.ContractBases.Is(d) && filter.Contracts) ||
                            Sungero.Contracts.SupAgreements.Is(d) && filter.SupAgreements);
      
      // Фильтр "Вид документа".
      if (filter.DocumentKind != null)
        query = query.Where(c => Equals(c.DocumentKind, filter.DocumentKind));
      
      // Фильтр "Наша организация".
      if (filter.BusinessUnit != null)
        query = query.Where(c => Equals(c.BusinessUnit, filter.BusinessUnit));
      
      // Фильтр "Период".
      if (filter.CurrentQuarter || filter.PreviousQuarter || filter.ManualPeriod)
        query = this.FinContractListApplyFilterByDate(query, filter);
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договоры и доп. соглашения по неоптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Договоры и доп. соглашения для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договоры и доп. соглашения.</returns>
    /// <remarks>Условия, которые могут выполняться долго (например те, которые не могут использовать индексы).</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> FinContractListApplyWeakFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.FinancialArchiveUI.FolderFilterState.IFinContractListFilterState filter)
    {
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договоры и доп. соглашения по установленной дате.
    /// </summary>
    /// <param name="query">Договоры и доп. соглашения для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договоры и доп. соглашения.</returns>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> FinContractListApplyFilterByDate(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.FinancialArchiveUI.FolderFilterState.IFinContractListFilterState filter)
    {
      var today = Calendar.UserToday;
      var beginDate = today.BeginningOfMonth();
      var endDate = today.EndOfMonth();

      if (filter.PreviousMonth)
      {
        beginDate = today.AddMonths(-1).BeginningOfMonth();
        endDate = today.AddMonths(-1).EndOfMonth();
      }
      
      if (filter.CurrentQuarter)
      {
        beginDate = Docflow.PublicFunctions.AccountingDocumentBase.BeginningOfQuarter(today);
        endDate = Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(today);
      }
      
      if (filter.PreviousQuarter)
      {
        beginDate = Docflow.PublicFunctions.AccountingDocumentBase.BeginningOfQuarter(today.AddMonths(-3));
        endDate = Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(today.AddMonths(-3));
      }

      if (filter.ManualPeriod)
      {
        beginDate = filter.DateRangeFrom ?? Calendar.SqlMinValue;
        endDate = filter.DateRangeTo ?? Calendar.SqlMaxValue;
      }
      
      query = Sungero.Docflow.PublicFunctions.Module.OfficialDocumentsApplyFilterByDate(query, beginDate, endDate)
        .Cast<Sungero.Contracts.IContractualDocument>();
      
      return query;
    }
    
    /// <summary>
    /// Определить, нужно ли использовать предварительную фильтрацию для договоров и доп. соглашений.
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True, если нужно использовать предварительную фильтрацию.</returns>
    public virtual bool UsePrefilterFinContractList(Sungero.FinancialArchiveUI.FolderFilterState.IFinContractListFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.Category != null ||
         filter.Contractor != null ||
         filter.Department != null ||
         filter.CurrentMonth ||
         filter.PreviousMonth);

      return hasStrongFilter;
    }
    #endregion
  }
}
