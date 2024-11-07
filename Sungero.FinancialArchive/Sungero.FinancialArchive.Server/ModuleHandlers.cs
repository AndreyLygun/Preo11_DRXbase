using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.FinancialArchive.Server
{
  partial class SignAwaitedDocumentsFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IAccountingDocumentBase> SignAwaitedDocumentsDataQuery(IQueryable<Sungero.Docflow.IAccountingDocumentBase> query)
    {
      query = query.Where(x => x.IsFormalized == true && (x.ExchangeState == Docflow.OfficialDocument.ExchangeState.SignAwaited ||
                                                          x.ExchangeState == Docflow.OfficialDocument.ExchangeState.SignRequired));
      #region Фильтры
      
      if (_filter == null)
        return query;
      
      // Фильтр "Контрагент".
      if (_filter.Counterparty != null)
        query = query.Where(c => Equals(c.Counterparty, _filter.Counterparty));
      
      // Фильтр "Наша организация".
      if (_filter.BusinessUnit != null)
        query = query.Where(c => Equals(c.BusinessUnit, _filter.BusinessUnit));
      
      // Фильтр "Подразделение".
      if (_filter.Department != null)
        query = query.Where(c => Equals(c.Department, _filter.Department));
      
      // Фильтр "Ожидают подписания".
      if (_filter.ByBusinessUnit && !_filter.ByCounterparty)
        query = query.Where(c => Equals(c.ExchangeState, Sungero.Docflow.AccountingDocumentBase.ExchangeState.SignRequired));
      
      if (_filter.ByCounterparty && !_filter.ByBusinessUnit)
        query = query.Where(c => Equals(c.ExchangeState, Sungero.Docflow.AccountingDocumentBase.ExchangeState.SignAwaited));
      
      #region Фильтрация по дате

      var beginDate = Calendar.UserToday.BeginningOfMonth();
      var endDate = Calendar.UserToday.EndOfMonth();

      if (_filter.PreviousMonth)
      {
        beginDate = Calendar.UserToday.AddMonths(-1).BeginningOfMonth();
        endDate = Calendar.UserToday.AddMonths(-1).EndOfMonth();
      }
      if (_filter.CurrentQuarter)
      {
        beginDate = Docflow.PublicFunctions.AccountingDocumentBase.BeginningOfQuarter(Calendar.UserToday);
        endDate = Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(Calendar.UserToday);
      }
      if (_filter.PreviousQuarter)
      {
        beginDate = Docflow.PublicFunctions.AccountingDocumentBase.BeginningOfQuarter(Calendar.UserToday.AddMonths(-3));
        endDate = Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(Calendar.UserToday.AddMonths(-3));
      }

      if (_filter.ManualPeriod)
      {
        beginDate = _filter.DateRangeFrom ?? Calendar.SqlMinValue;
        endDate = _filter.DateRangeTo ?? Calendar.SqlMaxValue;
      }
      
      query = Sungero.Docflow.PublicFunctions.Module.OfficialDocumentsApplyFilterByDate(query, beginDate, endDate)
        .Cast<Sungero.Docflow.IAccountingDocumentBase>();
      
      #endregion
      
      #endregion
      
      return query;
    }
  }

  partial class DocumentsWithoutScanFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IAccountingDocumentBase> DocumentsWithoutScanDataQuery(IQueryable<Sungero.Docflow.IAccountingDocumentBase> query)
    {
      // Получить все финансовые документы без скан-копий.
      var scanExtensions = new List<string>() { "pdf", "jpg", "tiff", "png", "tif", "bmp", "jpeg" };

      query = query.Where(x => !Contracts.IncomingInvoices.Is(x) && !Contracts.OutgoingInvoices.Is(x))
        .Where(x => x.IsFormalized != true)
        .Where(x => x.LifeCycleState != Sungero.Docflow.AccountingDocumentBase.LifeCycleState.Obsolete);
      
      var infos = Exchange.ExchangeDocumentInfos.GetAll().Where(x => query.Contains(x.Document)).Select(d => d.Document).ToList();
      query = query.Where(x => !infos.Contains(x));
      
      var associatedApps = Sungero.Content.AssociatedApplications.GetAll().Where(x => scanExtensions.Contains(x.Extension.ToLower())).ToList();
      query = query.Where(x => !x.Versions.Any() || !associatedApps.Contains(x.AssociatedApplication));
      
      #region Фильтры

      if (_filter == null)
        return query;
      
      // Фильтр "Контрагент".
      if (_filter.Counterparty != null)
        query = query.Where(c => Equals(c.Counterparty, _filter.Counterparty));
      
      // Фильтр "Наша организация".
      if (_filter.BusinessUnit != null)
        query = query.Where(c => Equals(c.BusinessUnit, _filter.BusinessUnit));
      
      // Фильтр "Подразделение".
      if (_filter.Department != null)
        query = query.Where(c => Equals(c.Department, _filter.Department));
      
      #region Фильтрация по дате

      var beginDate = Calendar.UserToday.BeginningOfMonth();
      var endDate = Calendar.UserToday.EndOfMonth();
      if (_filter.PreviousMonth)
      {
        beginDate = Calendar.UserToday.AddMonths(-1).BeginningOfMonth();
        endDate = Calendar.UserToday.AddMonths(-1).EndOfMonth();
      }
      if (_filter.CurrentQuarter)
      {
        beginDate = Docflow.PublicFunctions.AccountingDocumentBase.BeginningOfQuarter(Calendar.UserToday);
        endDate = Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(Calendar.UserToday);
      }
      if (_filter.PreviousQuarter)
      {
        beginDate = Docflow.PublicFunctions.AccountingDocumentBase.BeginningOfQuarter(Calendar.UserToday.AddMonths(-3));
        endDate = Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(Calendar.UserToday.AddMonths(-3));
      }

      if (_filter.ManualPeriod)
      {
        beginDate = _filter.DateRangeFrom ?? Calendar.SqlMinValue;
        endDate = _filter.DateRangeTo ?? Calendar.SqlMaxValue;
      }
      
      query = Sungero.Docflow.PublicFunctions.Module.OfficialDocumentsApplyFilterByDate(query, beginDate, endDate)
        .Cast<Sungero.Docflow.IAccountingDocumentBase>();
      
      #endregion
      
      #endregion

      return query;
    }
  }

  partial class FinContractListFolderHandlers
  {

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> FinContractListDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      if (_filter == null)
        return query;
      
      if (!Functions.Module.UsePrefilterFinContractList(_filter))
        query = Functions.Module.FinContractListApplyOrdinaryFilter(query, _filter);
      
      query = Functions.Module.FinContractListApplyWeakFilter(query, _filter);
      
      return query;
    }

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> FinContractListPreFiltering(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      if (_filter == null)
        return query;
      
      if (Functions.Module.UsePrefilterFinContractList(_filter))
      {
        query = Functions.Module.FinContractListApplyStrongFilter(query, _filter);
        query = Functions.Module.FinContractListApplyOrdinaryFilter(query, _filter);
      }
      
      return query;
    }

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> FinContractListDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      return ContractsUI.PublicFunctions.Module.ContractsFilterContractsKind(query, true);
    }
  }

  partial class FinancialArchiveHandlers
  {
  }
}