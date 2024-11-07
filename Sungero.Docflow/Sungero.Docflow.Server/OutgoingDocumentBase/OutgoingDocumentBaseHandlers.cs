using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OutgoingDocumentBase;

namespace Sungero.Docflow
{
  partial class OutgoingDocumentBaseInResponseToDocumentsDocumentPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> InResponseToDocumentsDocumentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var selectedDocuments = _obj.OutgoingDocumentBase.InResponseToDocuments
        .Where(d => d.Document != null && !Equals(d, _obj))
        .Select(d => d.Document);
      query = query.Where(x => !selectedDocuments.Contains(x));
      
      if (_obj.OutgoingDocumentBase.Addressees.Any(a => a.Correspondent != null))
      {
        var correspondents = _obj.OutgoingDocumentBase.Addressees.Where(a => a.Correspondent != null).Select(a => a.Correspondent).ToList();
        query = query.Where(l => correspondents.Contains(l.Correspondent));
      }
      
      if (_obj.OutgoingDocumentBase.BusinessUnit != null)
        query = query.Where(l => Equals(_obj.OutgoingDocumentBase.BusinessUnit, l.BusinessUnit));
      
      return query;
    }
  }

  partial class OutgoingDocumentBaseConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      e.Without(Sungero.Docflow.Addendums.Info.Properties.LeadingDocument);
      
      var counterparty = Exchange.PublicFunctions.ExchangeDocumentInfo.GetDocumentCounterparty(_source, _source.LastVersion);
      if (counterparty != null)
      {
        var outgoingDocument = OutgoingDocumentBases.As(e.Entity);
        outgoingDocument.IsManyAddressees = false;
        outgoingDocument.Correspondent = counterparty;
      }
    }
  }

  partial class OutgoingDocumentBaseCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      
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
      e.Without(_info.Properties.TrackNumber);
      e.Without(_info.Properties.SentDate);
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
    
    public override void CopySigningGroup(Sungero.Domain.CreatingFromEventArgs e)
    {
      if (_source.OurSigningReason != null && _source.OurSigningReason.Status == SignatureSetting.Status.Closed)
        e.Without(_info.Properties.OurSigningReason);
    }
  }

  partial class OutgoingDocumentBaseAddresseesAddresseePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> AddresseesAddresseeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Correspondent != null)
      {
        if (Sungero.Parties.People.Is(_obj.Correspondent))
          return query.Where(c => c.Company == null);

        query = query.Where(c => Equals(c.Company, _obj.Correspondent));
      }
      return query;
    }
  }

  partial class OutgoingDocumentBaseFilteringServerHandler<T>
  {

    public override IQueryable<T> PreFiltering(IQueryable<T> query, Sungero.Domain.PreFilteringEventArgs e)
    {
      if (_filter == null)
        return base.PreFiltering(query, e);
      
      if (Functions.Module.UsePrefilterOutgoingDocuments(_filter))
      {
        query = Functions.Module.OutgoingDocumentsApplyStrongFilter(query, _filter).Cast<T>();
        query = Functions.Module.OutgoingDocumentsApplyOrdinaryFilter(query, _filter).Cast<T>();
      }
      
      return query;
    }
    
    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return base.Filtering(query, e);
      
      if (!Functions.Module.UsePrefilterOutgoingDocuments(_filter))
        query = Functions.Module.OutgoingDocumentsApplyOrdinaryFilter(query, _filter).Cast<T>();
      
      query = Functions.Module.OutgoingDocumentsApplyWeakFilter(query, _filter).Cast<T>();

      return query;
    }

    public virtual IQueryable<Sungero.Company.IDepartment> DepartmentFiltering(IQueryable<Sungero.Company.IDepartment> query, Sungero.Domain.FilteringEventArgs e)
    {
      return query;
    }

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> DocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query, Sungero.Domain.FilteringEventArgs e)
    {
      return query.Where(k => k.Status == CoreEntities.DatabookEntry.Status.Active &&
                         k.DocumentType.DocumentFlow == DocumentType.DocumentFlow.Outgoing &&
                         k.DocumentType.IsRegistrationAllowed == true);
    }

    public virtual IQueryable<Sungero.Docflow.IDocumentRegister> DocumentRegisterFiltering(IQueryable<Sungero.Docflow.IDocumentRegister> query, Sungero.Domain.FilteringEventArgs e)
    {
      return Functions.DocumentRegister.GetAvailableDocumentRegisters(DocumentRegister.DocumentFlow.Outgoing);
    }
  }

  partial class OutgoingDocumentBaseServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      if (_obj.IsManyAddressees == true && !_obj.Addressees.Any())
        e.AddError(_obj.Info.Properties.Addressees, OutgoingDocumentBases.Resources.NeedFillAddressee);
      
      if (_obj.IsManyResponses == false)
        Functions.OutgoingDocumentBase.ClearAndFillFirstResponseDocument(_obj);
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
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      // Заполнить исполнителя.
      if (_obj.Assignee == null)
        _obj.Assignee = Company.Employees.As(_obj.Author);
      
      if (_obj.IsManyAddressees == null)
        _obj.IsManyAddressees = false;
      
      if (!_obj.State.IsCopied)
        _obj.IsManyResponses = false;
    }
  }

  partial class OutgoingDocumentBaseInResponseToPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> InResponseToFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Addressees.Any(a => a.Correspondent != null))
      {
        var correspondents = _obj.Addressees.Where(a => a.Correspondent != null).Select(a => a.Correspondent).ToList();
        query = query.Where(l => correspondents.Contains(l.Correspondent));
      }
      
      if (_obj.BusinessUnit != null)
        query = query.Where(l => Equals(_obj.BusinessUnit, l.BusinessUnit));
      
      return query;
    }
  }

  partial class OutgoingDocumentBaseAddresseePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> AddresseeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Correspondent != null)
        query = query.Where(c => Equals(c.Company, _obj.Correspondent));
      return query;
    }
  }

}