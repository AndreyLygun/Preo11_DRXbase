using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ContractualDocumentBase;

namespace Sungero.Docflow
{

  partial class ContractualDocumentBaseConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);

      if (Sungero.Docflow.Addendums.Is(_source))
        e.Without(Sungero.Docflow.ContractualDocumentBases.Info.Properties.LeadingDocument);
      
      var counterparty = Exchange.PublicFunctions.ExchangeDocumentInfo.GetDocumentCounterparty(_source, _source.LastVersion);
      if (counterparty != null)
      {
        var contractualDocument = ContractualDocumentBases.As(e.Entity);
        contractualDocument.Counterparty = counterparty;
      }
      
      var sourceOfficialDocument = OfficialDocuments.As(_source);
      if (sourceOfficialDocument != null)
        e.Params.AddOrUpdate(Constants.ContractualDocumentBase.IsChangeTypeExchangeDocument, sourceOfficialDocument.ExchangeState != null);
    }
  }

  partial class ContractualDocumentBaseServerHandlers
  {

    public override void BeforeSaveHistory(Sungero.Content.DocumentHistoryEventArgs e)
    {
      base.BeforeSaveHistory(e);
      
      var isUpdateAction = e.Action == Sungero.CoreEntities.History.Action.Update;
      var isCreateAction = e.Action == Sungero.CoreEntities.History.Action.Create;
      var properties = _obj.State.Properties;

      // Изменять историю только для изменения и создания документа.
      if (!isUpdateAction && !isCreateAction)
        return;
      
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
        var historyComment = Functions.ContractualDocumentBase.GetAmountChangeHistoryComment(_obj, totalAmountWasChanged);
        e.Write(operation, operationDetailed, historyComment);
      }
    }
  }
}