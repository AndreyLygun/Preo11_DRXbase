using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OutgoingDocumentBase;

namespace Sungero.Docflow
{
  partial class OutgoingDocumentBaseInResponseToDocumentsSharedHandlers
  {

    public virtual void InResponseToDocumentsDocumentChanged(Sungero.Docflow.Shared.OutgoingDocumentBaseInResponseToDocumentsDocumentChangedEventArgs e)
    {
      if (Equals(e.NewValue, null))
        return;
      
      if (_obj.OutgoingDocumentBase.IsManyAddressees == false &&
          _obj.OutgoingDocumentBase.Correspondent == null &&
          !Equals(_obj.OutgoingDocumentBase.Correspondent, e.NewValue.Correspondent))
        _obj.OutgoingDocumentBase.Correspondent = e.NewValue.Correspondent;
      
      if (_obj.OutgoingDocumentBase.IsManyAddressees == true && !_obj.OutgoingDocumentBase.Addressees.Any())
      {
        var newAddressee = _obj.OutgoingDocumentBase.Addressees.AddNew();
        newAddressee.Correspondent = e.NewValue.Correspondent;
      }
      
      if (Equals(_obj.OutgoingDocumentBase.Project, null))
        Functions.OfficialDocument.CopyProjects(e.NewValue, _obj.OutgoingDocumentBase);
    }
  }

  partial class OutgoingDocumentBaseAddresseesSharedCollectionHandlers
  {

    public virtual void AddresseesAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.Number = (_obj.Addressees.Max(a => a.Number) ?? 0) + 1;
    }

    public virtual void AddresseesDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      if (_obj.IsManyAddressees == true && _obj.InResponseTo != null && !_obj.Addressees.Any(x => Equals(x.Correspondent, _obj.InResponseTo.Correspondent)))
        _obj.InResponseTo = null;
      
      if (_obj.IsManyAddressees == true)
        Functions.OutgoingDocumentBase.RemoveDocumentsOfDeletedCorrespondents(_obj);
    }
  }

  partial class OutgoingDocumentBaseAddresseesSharedHandlers
  {

    public virtual void AddresseesAddresseeChanged(Sungero.Docflow.Shared.OutgoingDocumentBaseAddresseesAddresseeChangedEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue) && !Equals(e.NewValue.Company, _obj.Correspondent))
        _obj.Correspondent = e.NewValue.Company;
    }
    
    public virtual void AddresseesCorrespondentChanged(Sungero.Docflow.Shared.OutgoingDocumentBaseAddresseesCorrespondentChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;

      if (_obj.Addressee != null && !Equals(_obj.Addressee.Company, e.NewValue))
        _obj.Addressee = null;
      
      if (_obj.OutgoingDocumentBase.InResponseTo != null && !_obj.OutgoingDocumentBase.Addressees.Any(x => Equals(x.Correspondent, _obj.OutgoingDocumentBase.InResponseTo.Correspondent)))
        _obj.OutgoingDocumentBase.InResponseTo = null;
      
      if (!_obj.OutgoingDocumentBase.Addressees.Any(a => Equals(a.Correspondent, e.OldValue)))
        Functions.OutgoingDocumentBase.RemoveDocumentsOfDeletedCorrespondents(_obj.OutgoingDocumentBase);
    }
  }

  partial class OutgoingDocumentBaseSharedHandlers
  {

    public virtual void IsManyResponsesChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue == true)
        Functions.OutgoingDocumentBase.FillFirstResponseDocument(_obj);
      else if (e.NewValue == false)
        Functions.OutgoingDocumentBase.FillInResponseToFromInResponseToDocuments(_obj);
    }

    public virtual void TrackNumberChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (!Equals(e.NewValue, e.OldValue))
        this.SyncAddressees();
    }

    public virtual void SentDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      if (!Equals(e.NewValue, e.OldValue))
        this.SyncAddressees();
    }

    public override void DeliveryMethodChanged(Sungero.Docflow.Shared.OfficialDocumentDeliveryMethodChangedEventArgs e)
    {
      base.DeliveryMethodChanged(e);
      
      if (!Equals(e.NewValue, e.OldValue))
        this.SyncAddressees();
    }

    public virtual void IsManyAddresseesChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue == true)
      {
        var inResponseTo = _obj.InResponseTo;
        var storedInResponseToDocuments = _obj.InResponseToDocuments.Where(d => d.Document != null).Select(d => d.Document).ToList();
        Functions.OutgoingDocumentBase.ClearAndFillFirstAddressee(_obj);
        
        // Восстановление ссылок на ответные документы после очистки и заполнения адресатов (bugs 216660, 346134).
        if (inResponseTo != null && _obj.Addressees.Any(x => Equals(x.Correspondent, inResponseTo.Correspondent)))
          _obj.InResponseTo = inResponseTo;
        foreach (var document in storedInResponseToDocuments)
          _obj.InResponseToDocuments.AddNew().Document = document;
        
        _obj.Correspondent = Parties.PublicFunctions.Counterparty.Remote.GetDistributionListCounterparty();
        _obj.DistributionCorrespondent = _obj.Correspondent.Name;
        _obj.DeliveryMethod = null;
        _obj.SentDate = null;
        _obj.TrackNumber = null;
        _obj.Addressee = null;
      }
      else if (e.NewValue == false)
      {
        var addressee = _obj.Addressees.OrderBy(a => a.Number).FirstOrDefault(a => a.Correspondent != null);
        _obj.Correspondent = addressee?.Correspondent;
        _obj.DeliveryMethod = addressee?.DeliveryMethod;
        _obj.SentDate = addressee?.SentDate;
        _obj.TrackNumber = addressee?.TrackNumber;
        _obj.Addressee = addressee?.Addressee;
        
        Functions.OutgoingDocumentBase.ClearAndFillFirstAddressee(_obj);
        Functions.OutgoingDocumentBase.RemoveDocumentsOfDeletedCorrespondents(_obj);
      }
    }

    public virtual void InResponseToChanged(Sungero.Docflow.Shared.OutgoingDocumentBaseInResponseToChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue) || e.NewValue == null)
        return;

      if (_obj.IsManyAddressees == false && !Equals(_obj.Correspondent, e.NewValue.Correspondent))
        _obj.Correspondent = e.NewValue.Correspondent;
      
      if (_obj.IsManyAddressees == true && !_obj.Addressees.Any())
      {
        var newAddressee = _obj.Addressees.AddNew();
        newAddressee.Correspondent = e.NewValue.Correspondent;
      }
      
      Functions.OfficialDocument.CopyProjects(e.NewValue, _obj);
    }

    public virtual void AddresseeChanged(Sungero.Docflow.Shared.OutgoingDocumentBaseAddresseeChangedEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue) && !Equals(e.NewValue.Company, _obj.Correspondent))
        _obj.Correspondent = e.NewValue.Company;
      
      if (!Equals(e.NewValue, e.OldValue))
        this.SyncAddressees();
    }

    public virtual void CorrespondentChanged(Sungero.Docflow.Shared.OutgoingDocumentBaseCorrespondentChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      _obj.State.Properties.Addressee.IsEnabled = Sungero.Parties.CompanyBases.Is(e.NewValue) || e.NewValue == null;
      if (!_obj.State.Properties.Addressee.IsEnabled ||
          (_obj.Addressee != null && !Equals(_obj.Addressee.Company, e.NewValue)))
        _obj.Addressee = null;
      
      // Очистку поля и коллекции "В ответ на", при изменении контрагента, выполнять только для одноадресного письма.
      if (_obj.IsManyAddressees == false)
      {
        var isManyResponses = _obj.IsManyResponses.GetValueOrDefault();
        if (!isManyResponses && _obj.InResponseTo != null && !Equals(_obj.InResponseTo.Correspondent, e.NewValue))
          _obj.InResponseTo = null;
        
        if (isManyResponses)
        {
          var otherCounterpartyDocuments = _obj.InResponseToDocuments
            .Where(d => d.Document == null || !Equals(d.Document.Correspondent, e.NewValue))
            .ToList();
          
          foreach (var document in otherCounterpartyDocuments)
            _obj.InResponseToDocuments.Remove(document);
        }
      }
      
      this.SyncAddressees();
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      base.SubjectChanged(e);
    }
    
    private void SyncAddressees()
    {
      if (_obj.IsManyAddressees == true)
      {
        _obj.Correspondent = Parties.PublicFunctions.Counterparty.Remote.GetDistributionListCounterparty();
        _obj.DeliveryMethod = null;
        _obj.SentDate = null;
        _obj.TrackNumber = null;
        _obj.Addressee = null;
      }
      else if (_obj.IsManyAddressees == false)
        Functions.OutgoingDocumentBase.ClearAndFillFirstAddressee(_obj);
    }
  }
}
