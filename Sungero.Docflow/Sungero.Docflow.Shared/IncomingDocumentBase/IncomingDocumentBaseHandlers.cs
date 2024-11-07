using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.IncomingDocumentBase;

namespace Sungero.Docflow
{
  partial class IncomingDocumentBaseInResponseToDocumentsSharedHandlers
  {

    public virtual void InResponseToDocumentsDocumentChanged(Sungero.Docflow.Shared.IncomingDocumentBaseInResponseToDocumentsDocumentChangedEventArgs e)
    {
      if (e.NewValue == null)
        return;
      
      var document = _obj.IncomingDocumentBase;
      Functions.IncomingDocumentBase.FillCorrespondent(document, e.NewValue);
      
      if (document.Project == null)
        Functions.OfficialDocument.CopyProjects(e.NewValue, document);
    }
  }

  partial class IncomingDocumentBaseAddresseesSharedHandlers
  {

    public virtual void AddresseesAddresseeChanged(Sungero.Docflow.Shared.IncomingDocumentBaseAddresseesAddresseeChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue.Department != null)
        _obj.Department = e.NewValue.Department;
    }
  }

  partial class IncomingDocumentBaseAddresseesSharedCollectionHandlers
  {

    public virtual void AddresseesAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.Number = (_obj.Addressees.Max(a => a.Number) ?? 0) + 1;
    }
  }

  partial class IncomingDocumentBaseSharedHandlers
  {

    public virtual void IsManyResponsesChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue == true)
        Functions.IncomingDocumentBase.FillFirstResponseDocument(_obj);
      else if (e.NewValue == false)
        Functions.IncomingDocumentBase.FillInResponseToFromInResponseToDocuments(_obj);
    }

    public virtual void IsManyAddresseesChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue == true)
      {
        Functions.IncomingDocumentBase.ClearAndFillFirstAddressee(_obj);
        Functions.IncomingDocumentBase.SetManyAddresseesPlaceholder(_obj);
      }
      else if (e.NewValue == false)
      {
        Functions.IncomingDocumentBase.FillAddresseeFromAddressees(_obj);
        Functions.IncomingDocumentBase.ClearAndFillFirstAddressee(_obj);
      }
    }
    
    public virtual void CorrespondentChanged(Sungero.Docflow.Shared.IncomingDocumentBaseCorrespondentChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      var isManyResponses = _obj.IsManyResponses.GetValueOrDefault();
      if (!isManyResponses && _obj.InResponseTo != null &&
          !_obj.InResponseTo.Addressees.Any(a => Equals(a.Correspondent, _obj.Correspondent)))
        _obj.InResponseTo = null;
      
      if (isManyResponses && _obj.InResponseToDocuments.Any())
      {
        var otherCounterpartyDocuments = _obj.InResponseToDocuments
          .Where(d => d.Document == null || !d.Document.Addressees.Any(a => Equals(a.Correspondent, _obj.Correspondent)))
          .ToList();
        foreach (var documentRow in otherCounterpartyDocuments)
          _obj.InResponseToDocuments.Remove(documentRow);
      }
    }

    public virtual void InResponseToChanged(Sungero.Docflow.Shared.IncomingDocumentBaseInResponseToChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue) || e.NewValue == null)
        return;
      
      Functions.IncomingDocumentBase.FillCorrespondent(_obj, e.NewValue);
      
      if (_obj.Project == null)
        Functions.OfficialDocument.CopyProjects(e.NewValue, _obj);
    }

    public virtual void AddresseeChanged(Sungero.Docflow.Shared.IncomingDocumentBaseAddresseeChangedEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue) && _obj.BusinessUnit == null)
      {
        // Не чистить, если указан адресат с пустой НОР.
        if (e.NewValue.Department.BusinessUnit != null)
          _obj.BusinessUnit = e.NewValue.Department.BusinessUnit;
      }
      
      if (_obj.IsManyAddressees == false)
        Functions.IncomingDocumentBase.ClearAndFillFirstAddressee(_obj);
    }
  }
}