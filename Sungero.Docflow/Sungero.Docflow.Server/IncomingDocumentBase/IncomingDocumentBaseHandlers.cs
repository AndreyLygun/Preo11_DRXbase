using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.IncomingDocumentBase;

namespace Sungero.Docflow
{

  partial class IncomingDocumentBaseInResponseToDocumentsDocumentPropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> InResponseToDocumentsDocumentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var document = _obj.IncomingDocumentBase;
      
      var selectedDocuments = document.InResponseToDocuments
        .Where(d => d.Document != null && !Equals(d, _obj))
        .Select(d => d.Document);
      query = query.Where(x => !selectedDocuments.Contains(x));
      
      if (document.Correspondent != null)
        query = query.Where(x => x.Addressees.Any(a => Equals(a.Correspondent, document.Correspondent)));
      
      if (document.BusinessUnit != null)
        query = query.Where(x => Equals(document.BusinessUnit, x.BusinessUnit));
      
      return query;
    }
  }

  partial class IncomingDocumentBaseAddresseesDepartmentPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> AddresseesDepartmentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Addressee == null)
        return query;
      
      return query.Where(x => x.RecipientLinks.Any(r => Equals(r.Member, _obj.Addressee)));
    }
  }

  partial class IncomingDocumentBaseCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      this.CopyFromToGroup(e);
    }
    
    /// <summary>
    /// Скопировать поля групп "От кого" и "Кому".
    /// </summary>
    /// <param name="e">Аргументы события "Копирование".</param>
    public virtual void CopyFromToGroup(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.Dated);
      e.Without(_info.Properties.InNumber);
      
      if (_source.InResponseToDocuments == null || _source.InResponseToDocuments.Any(d => d.Document != null && !d.Document.AccessRights.CanRead()))
      {
        e.Without(_info.Properties.InResponseTo);
        e.Without(_info.Properties.InResponseToDocuments);
      }
    }
    
    public override void CopyRegistrationGroup(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.RegistrationNumber);
      e.Without(_info.Properties.RegistrationDate);
      e.Without(_info.Properties.DocumentRegister);
      
      if (!_source.AccessRights.CanRegister())
        e.Without(_info.Properties.DeliveryMethod);
      
      e.Without(_info.Properties.Tracking);
    }
    
    public override void CopyFileGroup(Sungero.Domain.CreatingFromEventArgs e)
    {
      if (!_source.AccessRights.CanRegister())
        e.Without(_info.Properties.CaseFile);
      
      e.Without(_info.Properties.PlacedToCaseFileDate);
      e.Without(_info.Properties.StoredIn);
      e.Without(_info.Properties.PaperCount);
      e.Without(_info.Properties.AddendaPaperCount);
    }
  }

  partial class IncomingDocumentBaseServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (_obj.IsManyAddressees == null)
        _obj.IsManyAddressees = false;
      
      if (_obj.IsManyResponses == null)
        _obj.IsManyResponses = false;
      
      _obj.State.Properties.ManyAddresseesPlaceholder.IsEnabled = false;
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      // Пропуск выполнения обработчика в случае отсутствия прав на изменение, например при выдаче прав на чтение пользователем, который сам имеет права на чтение.
      if (!_obj.AccessRights.CanUpdate())
        return;
      
      if (_obj.IsManyResponses == false)
        Functions.IncomingDocumentBase.ClearAndFillFirstResponseDocument(_obj);
      else if (_obj.IsManyResponses == true)
        _obj.InResponseTo = null;
      
      var emptyItems = _obj.InResponseToDocuments.Where(x => x.Document == null).ToList();
      foreach (var item in emptyItems)
        _obj.InResponseToDocuments.Remove(item);
      
      foreach (var responseItem in _obj.InResponseToDocuments)
      {
        if (!_obj.Relations.GetRelatedFromDocuments(Constants.Module.ResponseRelationName).Any(x => Equals(x, responseItem.Document)) && responseItem.Document.AccessRights.CanRead())
          _obj.Relations.AddFrom(Constants.Module.ResponseRelationName, responseItem.Document);
      }
      
      if (_obj.IsManyAddressees == true)
        Functions.IncomingDocumentBase.FillAddresseeFromAddressees(_obj);
      
      Functions.IncomingDocumentBase.SetManyAddresseesLabel(_obj);
    }
  }

  partial class IncomingDocumentBaseConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      // Для Входящих документов эл. обмена мапим Контрагента в Корреспондента.
      if (ExchangeDocuments.Is(_source))
      {
        e.Map(_info.Properties.Correspondent, ExchangeDocuments.Info.Properties.Counterparty);
      }
      else
      {
        var counterparty = Exchange.PublicFunctions.ExchangeDocumentInfo.GetDocumentCounterparty(_source, _source.LastVersion);
        if (counterparty != null)
        {
          var incomingDocument = IncomingDocumentBases.As(e.Entity);
          incomingDocument.Correspondent = counterparty;
        }
      }
      
      // При смене типа с вх. документа эл. обмена, а также с финансовых и договорных документов
      // дополнить примечание информацией об основании подписания со стороны контрагента.
      var sourceOfficialDocument = OfficialDocuments.As(_source);
      if (sourceOfficialDocument != null)
      {
        var note = PublicFunctions.OfficialDocument.GetNoteWithCounterpartySigningReason(sourceOfficialDocument);
        
        e.Map(_info.Properties.Note, note);
      }
      
      e.Without(Sungero.Docflow.Addendums.Info.Properties.LeadingDocument);
    }
  }

  partial class IncomingDocumentBaseFilteringServerHandler<T>
  {

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> DocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query, Sungero.Domain.FilteringEventArgs e)
    {
      return query.Where(k => k.Status == CoreEntities.DatabookEntry.Status.Active &&
                         k.DocumentType.DocumentFlow == DocumentType.DocumentFlow.Incoming &&
                         k.DocumentType.IsRegistrationAllowed == true);
    }

    public virtual IQueryable<Sungero.Docflow.IDocumentRegister> DocumentRegisterFiltering(IQueryable<Sungero.Docflow.IDocumentRegister> query, Sungero.Domain.FilteringEventArgs e)
    {
      return Functions.DocumentRegister.GetAvailableDocumentRegisters(DocumentRegister.DocumentFlow.Incoming);
    }

    public override IQueryable<T> PreFiltering(IQueryable<T> query, Sungero.Domain.PreFilteringEventArgs e)
    {
      if (_filter == null)
        return base.PreFiltering(query, e);
      
      if (Functions.Module.UsePrefilterIncomingDocuments(_filter))
      {
        query = Functions.Module.IncomingDocumentsApplyStrongFilter(query, _filter).Cast<T>();
        query = Functions.Module.IncomingDocumentsApplyOrdinaryFilter(query, _filter).Cast<T>();
      }
      
      return query;
    }
    
    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return base.Filtering(query, e);
      
      if (!Functions.Module.UsePrefilterIncomingDocuments(_filter))
        query = Functions.Module.IncomingDocumentsApplyOrdinaryFilter(query, _filter).Cast<T>();
      
      query = Functions.Module.IncomingDocumentsApplyWeakFilter(query, _filter).Cast<T>();
      
      return query;
    }
  }

  partial class IncomingDocumentBaseInResponseToPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> InResponseToFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Correspondent != null)
        query = query.Where(x => x.Addressees.Any(a => Equals(a.Correspondent, _obj.Correspondent)));
      
      if (_obj.BusinessUnit != null)
        query = query.Where(x => Equals(_obj.BusinessUnit, x.BusinessUnit));
      
      return query;
    }
  }

}