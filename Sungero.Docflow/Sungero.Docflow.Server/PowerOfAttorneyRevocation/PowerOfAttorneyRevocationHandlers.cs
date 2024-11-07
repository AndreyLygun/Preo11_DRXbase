using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorneyRevocation;

namespace Sungero.Docflow
{
  partial class PowerOfAttorneyRevocationServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      var formalizedPoAChanged = !Equals(_obj.FormalizedPowerOfAttorney, _obj.State.Properties.FormalizedPowerOfAttorney.OriginalValue);
      if (_obj.FormalizedPowerOfAttorney != null && formalizedPoAChanged && _obj.AccessRights.StrictMode != AccessRightsStrictMode.Enhanced)
      {
        var accessRightsLimit = Functions.OfficialDocument.GetAvailableAccessRights(_obj);
        if (accessRightsLimit != Guid.Empty)
          Functions.OfficialDocument.CopyAccessRightsToDocument(_obj.FormalizedPowerOfAttorney, _obj, accessRightsLimit);
      }
    }
  }

}