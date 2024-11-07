using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.OutgoingInvoice;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{

  partial class OutgoingInvoiceFilteringServerHandler<T>
  {

    public override IQueryable<T> PreFiltering(IQueryable<T> query, Sungero.Domain.PreFilteringEventArgs e)
    {
      if (_filter == null)
        return base.PreFiltering(query, e);
      
      if (Functions.Module.UsePrefilterOutgoingInvoices(_filter))
      {
        query = Functions.Module.OutgoingInvoicesApplyStrongFilter(query, _filter).Cast<T>();
        query = Functions.Module.OutgoingInvoicesApplyOrdinaryFilter(query, _filter).Cast<T>();
      }
      
      return query;
    }
    
    /// <summary>
    /// Фильтрация списка исходящих счетов.
    /// </summary>
    /// <param name="query">Фильтруемый список счетов.</param>
    /// <param name="e">Аргументы события фильтрации.</param>
    /// <returns>Список счетов с примененными фильтрами.</returns>
    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return base.Filtering(query, e);
      
      if (!Functions.Module.UsePrefilterOutgoingInvoices(_filter))
        query = Functions.Module.OutgoingInvoicesApplyOrdinaryFilter(query, _filter).Cast<T>();
      
      query = Functions.Module.OutgoingInvoicesApplyWeakFilter(query, _filter).Cast<T>();

      return query;
    }
  }
}