using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccountingDocumentBase;

namespace Sungero.Docflow
{
  partial class AccountingDocumentBaseConvertingFromServerHandler
  {
    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      if (Sungero.Docflow.Addendums.Is(_source))
        e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.LeadingDocument);
      
      e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.Corrected);

      var counterparty = Exchange.PublicFunctions.ExchangeDocumentInfo.GetDocumentCounterparty(_source, _source.LastVersion);
      if (counterparty != null)
      {
        var accountingDocument = AccountingDocumentBases.As(e.Entity);
        accountingDocument.Counterparty = counterparty;
      }
      
      // Очистить статус LifeCycleState, значения которого нет в целевом документе - ограничение платформы.
      if (!PublicFunctions.OfficialDocument.IsSupportedLifeCycleState(_source))
        e.Without(_info.Properties.LifeCycleState);
      
      var sourceOfficialDocument = OfficialDocuments.As(_source);
      if (sourceOfficialDocument != null)
        e.Params.AddOrUpdate(Constants.AccountingDocumentBase.IsChangeTypeExchangeDocument, sourceOfficialDocument.ExchangeState != null);
    }
  }

  partial class AccountingDocumentBaseCorrectedPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CorrectedFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = query.Where(x => x.Id != _obj.Id && x.IsAdjustment != true);
      
      if (_obj.Counterparty != null)
        query = query.Where(x => Equals(x.Counterparty, _obj.Counterparty));
      
      return query;
    }
  }

  partial class AccountingDocumentBaseLeadingDocumentPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> LeadingDocumentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.LeadingDocumentFiltering(query, e);
      var documents = query.Where(c => !Equals(c.LifeCycleState, Sungero.Contracts.ContractBase.LifeCycleState.Obsolete) &&
                                  !Exchange.CancellationAgreements.Is(c));
      
      if (_obj.Counterparty != null)
        documents = documents.Where(d => d.Counterparty == _obj.Counterparty);
      
      return documents;
    }
  }

  partial class AccountingDocumentBaseCounterpartySignatoryPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CounterpartySignatoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Counterparty != null)
        query = query.Where(c => c.Company == _obj.Counterparty);
      
      return query;
    }
  }

  partial class AccountingDocumentBaseCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      
      if (_source.IsFormalized == true)
        e.Without(_info.Properties.Versions);
      
      if (_source.LeadingDocument == null || !_source.LeadingDocument.AccessRights.CanRead())
        e.Without(_info.Properties.LeadingDocument);
      
      if (_source.Corrected == null || !_source.Corrected.AccessRights.CanRead())
        e.Without(_info.Properties.Corrected);
      
      e.Without(_info.Properties.IsFormalized);
      e.Without(_info.Properties.SellerTitleId);
      e.Without(_info.Properties.BuyerTitleId);
      e.Without(_info.Properties.SellerSignatureId);
      e.Without(_info.Properties.BuyerSignatureId);
      e.Without(_info.Properties.BusinessUnitBox);
      e.Without(_info.Properties.FormalizedServiceType);
      e.Without(_info.Properties.IsRevision);
      e.Without(_info.Properties.FormalizedFunction);
    }
  }

  partial class AccountingDocumentBaseFilteringServerHandler<T>
  {

    public override IQueryable<T> PreFiltering(IQueryable<T> query, Sungero.Domain.PreFilteringEventArgs e)
    {
      if (_filter == null)
        return base.PreFiltering(query, e);
      
      if (Functions.Module.UsePrefilterAccountingDocuments(_filter))
      {
        query = Functions.Module.AccountingDocumentsApplyStrongFilter(query, _filter).Cast<T>();
        query = Functions.Module.AccountingDocumentsApplyOrdinaryFilter(query, _filter).Cast<T>();
      }
      
      return query;
    }

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return base.Filtering(query, e);
      
      if (!Functions.Module.UsePrefilterAccountingDocuments(_filter))
        query = Functions.Module.AccountingDocumentsApplyOrdinaryFilter(query, _filter).Cast<T>();
      
      query = Functions.Module.AccountingDocumentsApplyWeakFilter(query, _filter).Cast<T>();
      
      return query;
    }
  }

  partial class AccountingDocumentBaseContactPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ContactFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Counterparty != null)
        query = query.Where(c => c.Company == _obj.Counterparty);
      
      return query;
    }
  }

  partial class AccountingDocumentBaseServerHandlers
  {

    public override void BeforeSigning(Sungero.Domain.BeforeSigningEventArgs e)
    {
      base.BeforeSigning(e);
      
      // Если подписание выполняется в рамках агента - генерировать заглушку не надо.
      bool jobRan;
      if (e.Params.TryGetValue(ExchangeCore.PublicConstants.BoxBase.JobRunned, out jobRan) && jobRan)
        return;
      
      if (_obj.BuyerTitleId.HasValue &&
          e.Signature.SignatureType == SignatureType.Approval &&
          !Signatures.Get(_obj.Versions.Single(v => v.Id == _obj.BuyerTitleId.Value)).Any(s => s.SignatureType == SignatureType.Approval))
      {
        Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(_obj, _obj.BuyerTitleId.Value);
        Exchange.PublicFunctions.Module.EnqueueXmlToPdfBodyConverter(_obj, _obj.BuyerTitleId.Value, _obj.ExchangeState);
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      // Выдать ответственному права на изменение документа.
      var responsible = _obj.ResponsibleEmployee;
      if (responsible != null && !Equals(_obj.State.Properties.ResponsibleEmployee.OriginalValue, responsible) &&
          !Equals(responsible, Sungero.Company.Employees.Current) &&
          !_obj.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, responsible) &&
          !_obj.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, responsible) &&
          _obj.AccessRights.StrictMode != AccessRightsStrictMode.Enhanced)
        _obj.AccessRights.Grant(responsible, DefaultAccessRightsTypes.Change);

      if (_obj.LeadingDocument != null && _obj.LeadingDocument.AccessRights.CanRead() &&
          !_obj.Relations.GetRelatedFromDocuments(Contracts.PublicConstants.Module.AccountingDocumentsRelationName).Any(x => x.Id == _obj.LeadingDocument.Id))
        _obj.Relations.AddFromOrUpdate(Contracts.PublicConstants.Module.AccountingDocumentsRelationName, _obj.State.Properties.LeadingDocument.OriginalValue, _obj.LeadingDocument);
      
      if (_obj.Corrected != null && _obj.Corrected.AccessRights.CanRead() &&
          !_obj.Relations.GetRelatedFromDocuments(Constants.Module.CorrectionRelationName).Any(x => x.Id == _obj.Corrected.Id))
        _obj.Relations.AddFromOrUpdate(Constants.Module.CorrectionRelationName, _obj.State.Properties.Corrected.OriginalValue, _obj.Corrected);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (_obj.ResponsibleEmployee == null)
        _obj.ResponsibleEmployee = Company.Employees.As(_obj.Author);
      
      if (_obj.IsAdjustment == null)
        _obj.IsAdjustment = false;
      
      if (_obj.IsRevision == null)
        _obj.IsRevision = false;
      
      if (_obj.State.IsInserted && _obj.Corrected != null)
        _obj.Relations.AddFrom(Constants.Module.CorrectionRelationName, _obj.Corrected);
      
      if (_obj.State.IsInserted && _obj.LeadingDocument != null)
        _obj.Relations.AddFrom(Contracts.PublicConstants.Module.AccountingDocumentsRelationName, _obj.LeadingDocument);
    }

    public override void BeforeSaveHistory(Sungero.Content.DocumentHistoryEventArgs e)
    {
      base.BeforeSaveHistory(e);

      var isUpdateAction = e.Action == Sungero.CoreEntities.History.Action.Update;
      var isCreateAction = e.Action == Sungero.CoreEntities.History.Action.Create;
      var isVersionCreateAction = e.Action == Sungero.CoreEntities.History.Action.Update &&
        e.Operation == new Enumeration(Constants.OfficialDocument.Operation.CreateVersion);
      var properties = _obj.State.Properties;

      // Изменять историю только для изменения и создания документа.
      if (!isUpdateAction && !isCreateAction)
        return;
      
      // Добавить комментарий к записи создания в истории о том, что титул продавца получен через СО.
      if (_obj.IsFormalized == true && isCreateAction && _obj.SellerTitleId.HasValue && _obj.ExchangeState != null)
      {
        var info = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetExDocumentInfoFromVersion(_obj, _obj.SellerTitleId.Value);
        if (info != null)
        {
          e.OperationDetailed = new Enumeration(Sungero.Docflow.Constants.OfficialDocument.Operation.SellerTitleFromExchangeService);
          e.Comment = ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(info.Box).Name;
        }
      }
      
      // Добавить комментарий к записи создания в истории о том, что заполнен титул покупателя.
      if (_obj.IsFormalized == true && isVersionCreateAction && _obj.ExchangeState != null)
        e.OperationDetailed = new Enumeration(Sungero.Docflow.Constants.OfficialDocument.Operation.BuyerTitle);
      
      // Изменение суммы, НДС или валюты.
      var totalAmountWasChanged = _obj.State.Properties.TotalAmount.IsChanged;
      var sumWasChanged = totalAmountWasChanged ||
        _obj.State.Properties.VatRate.IsChanged ||
        _obj.State.Properties.VatAmount.IsChanged ||
        (_obj.State.Properties.Currency.IsChanged && _obj.TotalAmount.HasValue);
      if (sumWasChanged)
      {
        // Локализация для операции в ресурсах OfficialDocument.
        var operation = new Enumeration(Constants.OfficialDocument.Operation.TotalAmountChange);
        var operationDetailed = operation;
        if (!_obj.TotalAmount.HasValue)
          operationDetailed = totalAmountWasChanged
            ? new Enumeration(Constants.OfficialDocument.Operation.TotalAmountClear)
            : new Enumeration(Constants.OfficialDocument.Operation.TotalAmountIsEmpty);
        var historyComment = Functions.AccountingDocumentBase.GetAmountChangeHistoryComment(_obj, totalAmountWasChanged);
        e.Write(operation, operationDetailed, historyComment);
      }
    }
  }
  
}