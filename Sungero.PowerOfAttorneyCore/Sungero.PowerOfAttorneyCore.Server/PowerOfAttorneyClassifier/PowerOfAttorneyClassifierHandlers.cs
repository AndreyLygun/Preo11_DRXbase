using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyCore.PowerOfAttorneyClassifier;

namespace Sungero.PowerOfAttorneyCore
{
  partial class PowerOfAttorneyClassifierCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.Mnemonic);
      e.Without(_info.Properties.Code);
      e.Without(_info.Properties.Autokey);
    }
  }

  partial class PowerOfAttorneyClassifierServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed)
        return;
          
      if (_obj.Started > _obj.Revoked)
        e.AddError(PowerOfAttorneyClassifiers.Resources.RevokedDateMustBeAfterStartedDate);
      
      var allDuplicates = Functions.PowerOfAttorneyClassifier.GetDuplicates(_obj, true);
      if (allDuplicates.Any())
      {
        var firstDuplicate = allDuplicates.OrderByDescending(x => x.Id).First();
        var firstDuplicateShortName = firstDuplicate.DisplayValue.Length > 50 ?
          firstDuplicate.DisplayValue.Substring(0, 50) :
          firstDuplicate.DisplayValue;
        e.AddError(PowerOfAttorneyClassifiers.Resources.DuplicateFoundFormat(firstDuplicateShortName), _obj.Info.Actions.ShowDuplicates);
      }
    }
  }

}