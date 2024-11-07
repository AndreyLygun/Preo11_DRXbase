using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Contracts.ContractBase;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.RelationType;
using Sungero.Docflow;
using Sungero.Docflow.ApprovalStage;
using Sungero.Docflow.DocumentKind;

namespace Sungero.Contracts.Server
{
  partial class IssuanceJournalFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> IssuanceJournalDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      query = query.Where(d => d.Status == CoreEntities.DatabookEntry.Status.Active)
        .Where(d => d.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts ||
               d.DocumentType.DocumentTypeGuid == Docflow.PublicConstants.AccountingDocumentBase.IncomingInvoiceGuid ||
               d.DocumentType.DocumentTypeGuid == Docflow.PublicConstants.AccountingDocumentBase.IncomingTaxInvoiceGuid ||
               d.DocumentType.DocumentTypeGuid == Docflow.PublicConstants.AccountingDocumentBase.OutcomingTaxInvoiceGuid);
      
      return query;
    }

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
  }

  partial class ContractsHistoryFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> ContractsHistoryDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      return Functions.Module.ContractsFilterContractsKind(query, true);
    }

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractsHistoryDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      if (_filter == null)
        return query.Where(d => ContractBases.Is(d) || SupAgreements.Is(d));
      
      if (!Functions.Module.UsePrefilterContractsHistory(_filter))
        query = Functions.Module.ContractsHistoryApplyOrdinaryFilter(query, _filter);
      
      query = Functions.Module.ContractsHistoryApplyWeakFilter(query, _filter);
      
      return query;
    }

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
  }

  partial class ContractsAtContractorsFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> ContractsAtContractorsDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      return Functions.Module.ContractsFilterContractsKind(query, false);
    }

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
  }

  partial class ExpiringSoonContractsFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> ExpiringSoonContractsDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      return Functions.Module.ContractsFilterContractsKind(query, true);
    }

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
  }

  partial class ContractsListFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> ContractsListDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      return Functions.Module.ContractsFilterContractsKind(query, true);
    }

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractsListDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      if (_filter == null)
        return query.Where(d => ContractBases.Is(d) || SupAgreements.Is(d));
      
      if (!Functions.Module.UsePrefilterContractualDocuments(_filter))
        query = Functions.Module.ContractualDocumentsApplyOrdinaryFilter(query, _filter);
      
      query = Functions.Module.ContractualDocumentsApplyWeakFilter(query, _filter);
      
      return query;
    }

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
  }

  partial class ContractsHandlers
  {

  }
}