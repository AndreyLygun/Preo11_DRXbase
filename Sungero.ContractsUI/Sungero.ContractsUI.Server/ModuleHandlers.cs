using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.ContractsUI.Server
{

  partial class ContractsListFolderHandlers
  {
    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте вычисляемую папку ContractsList модуля Contracts.")]
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractsListPreFiltering(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      if (_filter == null)
        return query;
      
      if (Functions.Module.UsePrefilterContractualDocuments(_filter))
      {
        query = Functions.Module.ContractualDocumentsApplyStrongFilter(query, _filter);
        query = Functions.Module.ContractualDocumentsApplyOrdinaryFilter(query, _filter);
      }
      
      return query;
    }
    
    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте вычисляемую папку ContractsList модуля Contracts.")]
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractsListDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      if (_filter == null)
        return query.Where(d => ContractBases.Is(d) || SupAgreements.Is(d));
      
      if (!Functions.Module.UsePrefilterContractualDocuments(_filter))
        query = Functions.Module.ContractualDocumentsApplyOrdinaryFilter(query, _filter);
      
      query = Functions.Module.ContractualDocumentsApplyWeakFilter(query, _filter);
      
      return query;
    }
    
    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте вычисляемую папку ContractsList модуля Contracts.")]
    public virtual IQueryable<Sungero.Docflow.IDocumentKind> ContractsListDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      return Functions.Module.ContractsFilterContractsKind(query, true);
    }
  }

  partial class ContractsHistoryFolderHandlers
  {
    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте вычисляемую папку ContractsHistory модуля Contracts.")]
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractsHistoryPreFiltering(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      if (_filter == null)
        return query;
      
      if (Functions.Module.UsePrefilterContractsHistory(_filter))
      {
        query = Functions.Module.ContractsHistoryApplyStrongFilter(query, _filter);
        query = Functions.Module.ContractsHistoryApplyOrdinaryFilter(query, _filter);
      }
      
      return query;
    }

    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте вычисляемую папку ContractsHistory модуля Contracts.")]
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractsHistoryDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      if (_filter == null)
        return query.Where(d => ContractBases.Is(d) || SupAgreements.Is(d));
      
      if (!Functions.Module.UsePrefilterContractsHistory(_filter))
        query = Functions.Module.ContractsHistoryApplyOrdinaryFilter(query, _filter);
      
      query = Functions.Module.ContractsHistoryApplyWeakFilter(query, _filter);
      
      return query;
    }
    
    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте вычисляемую папку ContractsHistory модуля Contracts.")]
    public virtual IQueryable<Sungero.Docflow.IDocumentKind> ContractsHistoryDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      return Functions.Module.ContractsFilterContractsKind(query, true);
    }
  }

  partial class ExpiringSoonContractsFolderHandlers
  {
    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте вычисляемую папку ExpiringSoonContracts модуля Contracts.")]
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ExpiringSoonContractsPreFiltering(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      if (_filter == null)
        return query;
      
      if (Functions.Module.UsePrefilterExpiringSoonContractualDocuments(_filter))
      {
        query = Functions.Module.ExpiringSoonContractualDocumentsApplyStrongFilter(query, _filter);
        query = Functions.Module.ExpiringSoonContractualDocumentsApplyOrdinaryFilter(query, _filter);
      }
      
      return query;
    }

    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте вычисляемую папку ExpiringSoonContracts модуля Contracts.")]
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ExpiringSoonContractsDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      query = Functions.Module.ExpiringSoonContractualDocumentsApplyInvariantFilter(query);
      
      if (_filter == null)
        return query;
      
      if (!Functions.Module.UsePrefilterExpiringSoonContractualDocuments(_filter))
        query = Functions.Module.ExpiringSoonContractualDocumentsApplyOrdinaryFilter(query, _filter);
      
      query = Functions.Module.ExpiringSoonContractualDocumentsApplyWeakFilter(query, _filter);
      
      return query;
    }
    
    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте вычисляемую папку ExpiringSoonContracts модуля Contracts.")]
    public virtual IQueryable<Sungero.Docflow.IDocumentKind> ExpiringSoonContractsDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      return Functions.Module.ContractsFilterContractsKind(query, true);
    }
  }

  partial class ContractsAtContractorsFolderHandlers
  {
    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте вычисляемую папку ContractsAtContractors модуля Contracts.")]
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> ContractsAtContractorsPreFiltering(IQueryable<Sungero.Docflow.IOfficialDocument> query)
    {
      if (_filter == null)
        return query;
      
      if (Functions.Module.UsePrefilterContractsAtContractors(_filter))
      {
        query = Functions.Module.ContractsAtContractorsApplyStrongFilter(query, _filter);
        query = Functions.Module.ContractsAtContractorsApplyOrdinaryFilter(query, _filter);
      }
      
      return query;
    }

    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте вычисляемую папку ContractsAtContractors модуля Contracts.")]
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> ContractsAtContractorsDataQuery(IQueryable<Sungero.Docflow.IOfficialDocument> query)
    {
      query = Functions.Module.ContractsAtContractorsApplyInvariantFilter(query);
      
      if (_filter == null)
        return query;

      if (!Functions.Module.UsePrefilterContractsAtContractors(_filter))
        query = Functions.Module.ContractsAtContractorsApplyOrdinaryFilter(query, _filter);
      
      query = Functions.Module.ContractsAtContractorsApplyWeakFilter(query, _filter);
      
      return query;
    }
    
    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте вычисляемую папку ContractsAtContractors модуля Contracts.")]
    public virtual IQueryable<Sungero.Docflow.IDocumentKind> ContractsAtContractorsDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      return Functions.Module.ContractsFilterContractsKind(query, false);
    }
  }

  partial class IssuanceJournalFolderHandlers
  {
    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте вычисляемую папку IssuanceJournal модуля Contracts.")]
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> IssuanceJournalPreFiltering(IQueryable<Sungero.Docflow.IOfficialDocument> query)
    {
      if (_filter == null)
        return query;
      
      if (Functions.Module.UsePrefilterIssuanceJournal(_filter))
      {
        query = Functions.Module.IssuanceJournalApplyStrongFilter(query, _filter);
        query = Functions.Module.IssuanceJournalApplyOrdinaryFilter(query, _filter);
      }
      
      return query;
    }

    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте вычисляемую папку IssuanceJournal модуля Contracts.")]
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> IssuanceJournalDataQuery(IQueryable<Sungero.Docflow.IOfficialDocument> query)
    {
      if (_filter == null)
        return query.Where(x => x.IsReturnRequired == true &&
                           (Sungero.Docflow.ContractualDocumentBases.Is(x) || Sungero.Docflow.AccountingDocumentBases.Is(x)));
      
      if (!Functions.Module.UsePrefilterIssuanceJournal(_filter))
        query = Functions.Module.IssuanceJournalApplyOrdinaryFilter(query, _filter);
      
      query = Functions.Module.IssuanceJournalApplyWeakFilter(query, _filter);
      
      return query;
    }
    
    [Obsolete("Метод не используется с 11.01.2024 и версии 4.9. Используйте вычисляемую папку IssuanceJournal модуля Contracts.")]
    public virtual IQueryable<Sungero.Docflow.IDocumentKind> IssuanceJournalDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      query = query.Where(d => d.Status == CoreEntities.DatabookEntry.Status.Active)
        .Where(d => d.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts ||
               d.DocumentType.DocumentTypeGuid == Docflow.PublicConstants.AccountingDocumentBase.IncomingInvoiceGuid ||
               d.DocumentType.DocumentTypeGuid == Docflow.PublicConstants.AccountingDocumentBase.IncomingTaxInvoiceGuid ||
               d.DocumentType.DocumentTypeGuid == Docflow.PublicConstants.AccountingDocumentBase.OutcomingTaxInvoiceGuid);
      
      return query;
    }
  }

  partial class ContractsUIHandlers
  {

  }
}