using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.CancellationAgreement;

namespace Sungero.Exchange
{
  partial class CancellationAgreementServerHandlers
  {
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);

      if (_obj.LeadingDocument != null && _obj.LeadingDocument.AccessRights.CanRead() &&
          !_obj.Relations.GetRelatedFromDocuments(Constants.Module.SimpleRelationRelationName).Contains(_obj.LeadingDocument))
        _obj.Relations.AddFromOrUpdate(Constants.Module.SimpleRelationRelationName, _obj.State.Properties.LeadingDocument.OriginalValue, _obj.LeadingDocument);

      var leadingDocumentChanged = !Equals(_obj.LeadingDocument, _obj.State.Properties.LeadingDocument.OriginalValue);
      if (_obj.LeadingDocument != null && leadingDocumentChanged && _obj.AccessRights.StrictMode != AccessRightsStrictMode.Enhanced)
      {
        var accessRightsLimit = Docflow.PublicFunctions.OfficialDocument.GetAvailableAccessRights(_obj);
        if (accessRightsLimit != Guid.Empty)
          Docflow.PublicFunctions.OfficialDocument.CopyAccessRightsToDocument(_obj.LeadingDocument, _obj, accessRightsLimit);
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (_obj.State.IsInserted && _obj.LeadingDocument != null)
        _obj.Relations.AddFrom(Constants.Module.SimpleRelationRelationName, _obj.LeadingDocument);
    }
  }

}