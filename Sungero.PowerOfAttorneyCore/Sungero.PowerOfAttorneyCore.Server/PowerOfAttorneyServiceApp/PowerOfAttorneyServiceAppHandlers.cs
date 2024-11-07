using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyCore.PowerOfAttorneyServiceApp;

namespace Sungero.PowerOfAttorneyCore
{
  partial class PowerOfAttorneyServiceAppServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Проверка, что адрес не "кривой".
      if (!System.Uri.IsWellFormedUriString(_obj.Uri, UriKind.Absolute))
        e.AddError(Sungero.PowerOfAttorneyCore.PowerOfAttorneyServiceApps.Resources.InvalidUrl);
    }
  }

  partial class PowerOfAttorneyServiceAppCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.APIKey);
    }
  }

}