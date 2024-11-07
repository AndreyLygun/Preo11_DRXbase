using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.IdentityDocumentKind;

namespace Sungero.Parties
{
  partial class IdentityDocumentKindServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(_obj.IdentitySeriesPattern) && !Functions.Module.IsRegularExpresionValid(_obj.IdentitySeriesPattern))
        e.AddError(_obj.Info.Properties.IdentitySeriesPattern, Sungero.Parties.IdentityDocumentKinds.Resources.InvalidPattern);
      
      if (!string.IsNullOrWhiteSpace(_obj.IdentityNumberPattern) && !Functions.Module.IsRegularExpresionValid(_obj.IdentityNumberPattern))
        e.AddError(_obj.Info.Properties.IdentityNumberPattern, Sungero.Parties.IdentityDocumentKinds.Resources.InvalidPattern);
      
      if (!string.IsNullOrWhiteSpace(_obj.IdentityAuthorityCodePattern) && !Functions.Module.IsRegularExpresionValid(_obj.IdentityAuthorityCodePattern))
        e.AddError(_obj.Info.Properties.IdentityAuthorityCodePattern, Sungero.Parties.IdentityDocumentKinds.Resources.InvalidPattern);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (!_obj.State.IsCopied)
      {
        _obj.SpecifyIdentitySeries = true;
        _obj.SpecifyIdentityExpirationDate = false;
        _obj.SpecifyIdentityAuthorityCode = true;
        _obj.SpecifyBirthPlace = false;
      }
    }
  }

  partial class IdentityDocumentKindCreatingFromServerHandler
  {
    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.SID);
    }
  }
}