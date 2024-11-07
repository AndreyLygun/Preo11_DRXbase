using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.IncomingInvoice;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{
  partial class IncomingInvoiceConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);

      if (Sungero.Docflow.AccountingDocumentBases.Is(_source))
      {
        e.Map(_info.Properties.Number, Sungero.Docflow.AccountingDocumentBases.Info.Properties.RegistrationNumber);
        e.Map(_info.Properties.Date, Sungero.Docflow.AccountingDocumentBases.Info.Properties.RegistrationDate);
        e.Map(_info.Properties.Contract, Sungero.Docflow.AccountingDocumentBases.Info.Properties.LeadingDocument);
        
        // Котегов: Отключен проброс Number и Date, иначе они перетирали одноименные свойства (баг 115832).
        e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.Number);
        e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.Date);
        
        // Отключить проброс полей, которых нет во входящих счетах.
        e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.IsAdjustment);
        e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.CounterpartySignatory);
        e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.CounterpartySigningReason);
        e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.Contact);
        e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.ResponsibleEmployee);
      }
      
      // Исключаем проброс LeadingDocument, так как в счете должно заполняться поле Contract.
      e.Without(Sungero.Docflow.OfficialDocuments.Info.Properties.LeadingDocument);
      
      // При смене типа с вх. документа эл. обмена, а также с финансовых и договорных документов
      // дополнить примечание информацией об основании подписания со стороны контрагента.
      var sourceOfficialDocument = Sungero.Docflow.OfficialDocuments.As(_source);
      if (sourceOfficialDocument != null)
      {
        var note = Sungero.Docflow.PublicFunctions.OfficialDocument.GetNoteWithCounterpartySigningReason(sourceOfficialDocument);
        
        e.Map(_info.Properties.Note, note);
      }
    }
  }

  partial class IncomingInvoiceCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      
      if (_source.Contract == null || !_source.Contract.AccessRights.CanRead())
        e.Without(_info.Properties.Contract);
    }
  }

  partial class IncomingInvoiceServerHandlers
  {

    public override void BeforeSaveHistory(Sungero.Content.DocumentHistoryEventArgs e)
    {
      var isCreateAction = e.Action == Sungero.CoreEntities.History.Action.Create;
      var isChangeTypeAction = e.Action == Sungero.CoreEntities.History.Action.ChangeType;
      
      if (!isCreateAction && !isChangeTypeAction)
        base.BeforeSaveHistory(e);
      else
      {
        // Изменение суммы, НДС или валюты.
        var totalAmountWasChanged = _obj.State.Properties.TotalAmount.IsChanged;
        var sumWasChanged = totalAmountWasChanged ||
          _obj.State.Properties.VatRate.IsChanged ||
          _obj.State.Properties.VatAmount.IsChanged ||
          (_obj.State.Properties.Currency.IsChanged && _obj.TotalAmount.HasValue);
        if (sumWasChanged)
        {
          // Локализация для операции в ресурсах OfficialDocument.
          var operation = new Enumeration(Sungero.Docflow.Constants.OfficialDocument.Operation.TotalAmountChange);
          var operationDetailed = operation;
          if (!_obj.TotalAmount.HasValue)
            operationDetailed = totalAmountWasChanged
              ? new Enumeration(Sungero.Docflow.Constants.OfficialDocument.Operation.TotalAmountClear)
              : new Enumeration(Sungero.Docflow.Constants.OfficialDocument.Operation.TotalAmountIsEmpty);
          var historyComment = Sungero.Docflow.PublicFunctions.AccountingDocumentBase.GetAmountChangeHistoryComment(_obj, totalAmountWasChanged);
          e.Write(operation, operationDetailed, historyComment);
        }
        var documentParams = ((Domain.Shared.IExtendedEntity)_obj).Params;
        if (isCreateAction && documentParams.ContainsKey(Docflow.PublicConstants.OfficialDocument.AddHistoryCommentRepackingAddNewDocument))
          e.Comment = Sungero.Docflow.OfficialDocuments.Resources.DocumentCreateFromRepacking;
      }
    }
    
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      // Пропуск выполнения обработчика в случае отсутствия прав на изменение, например при выдаче прав на чтение пользователем, который сам имеет права на чтение.
      if (!_obj.AccessRights.CanUpdate())
        return;
      
      if (_obj.Date.HasValue && _obj.PaymentDueDate.HasValue &&
          _obj.Date.Value > _obj.PaymentDueDate)
        e.AddError(_obj.Info.Properties.PaymentDueDate, IncomingInvoices.Resources.DatePaymentDeadlineValidationMessage, _obj.Info.Properties.Date);
      
      if (Functions.IncomingInvoice.HaveDuplicates(_obj,
                                                   _obj.DocumentKind,
                                                   _obj.Number,
                                                   _obj.Date,
                                                   _obj.TotalAmount,
                                                   _obj.Currency,
                                                   _obj.Counterparty))
        e.AddWarning(IncomingInvoices.Resources.DuplicateDetected, _obj.Info.Actions.ShowDuplicates);
      
      if (_obj.Contract != null && _obj.Contract.AccessRights.CanRead() && !_obj.Relations.GetRelatedFromDocuments(Constants.Module.AccountingDocumentsRelationName).Any(x => x.Id == _obj.Contract.Id))
        _obj.Relations.AddFromOrUpdate(Constants.Module.AccountingDocumentsRelationName, _obj.State.Properties.Contract.OriginalValue, _obj.Contract);
      
      if (_obj.State.Properties.Date.IsChanged)
        _obj.DocumentDate = _obj.Date.HasValue ? _obj.Date : _obj.Created;
    }
    
    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (_obj.State.IsInserted && _obj.Contract != null)
        _obj.Relations.AddFrom(Constants.Module.AccountingDocumentsRelationName, _obj.Contract);
      
      _obj.ResponsibleEmployee = null;
    }
  }

  partial class IncomingInvoiceFilteringServerHandler<T>
  {

    public override IQueryable<T> PreFiltering(IQueryable<T> query, Sungero.Domain.PreFilteringEventArgs e)
    {
      if (_filter == null)
        return base.PreFiltering(query, e);
      
      if (Functions.Module.UsePrefilterIncomingInvoices(_filter))
      {
        query = Functions.Module.IncomingInvoicesApplyStrongFilter(query, _filter).Cast<T>();
        query = Functions.Module.IncomingInvoicesApplyOrdinaryFilter(query, _filter).Cast<T>();
      }
      
      return query;
    }
    
    /// <summary>
    /// Фильтрация списка входящих счетов.
    /// </summary>
    /// <param name="query">Фильтруемый список счетов.</param>
    /// <param name="e">Аргументы события фильтрации.</param>
    /// <returns>Список счетов с примененными фильтрами.</returns>
    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return base.Filtering(query, e);
      
      if (!Functions.Module.UsePrefilterIncomingInvoices(_filter))
        query = Functions.Module.IncomingInvoicesApplyOrdinaryFilter(query, _filter).Cast<T>();
      
      query = Functions.Module.IncomingInvoicesApplyWeakFilter(query, _filter).Cast<T>();

      return query;
    }
  }
  
  partial class IncomingInvoiceContractPropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> ContractFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Counterparty != null)
        query = query.Where(c => Equals(c.Counterparty, _obj.Counterparty));
      
      query = query.Where(c => !Equals(c.LifeCycleState, Sungero.Contracts.ContractBase.LifeCycleState.Obsolete));
      
      return query;
    }
  }
}
