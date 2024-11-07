using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyCore.PowerOfAttorneyServiceConnection;

namespace Sungero.PowerOfAttorneyCore
{
  partial class PowerOfAttorneyServiceConnectionSharedHandlers
  {

    public virtual void BusinessUnitChanged(Sungero.PowerOfAttorneyCore.Shared.PowerOfAttorneyServiceConnectionBusinessUnitChangedEventArgs e)
    {
      Functions.PowerOfAttorneyServiceConnection.FillName(_obj);
    }

    public virtual void ServiceAppChanged(Sungero.PowerOfAttorneyCore.Shared.PowerOfAttorneyServiceConnectionServiceAppChangedEventArgs e)
    {
      _obj.OrganizationId = null;
      Functions.PowerOfAttorneyServiceConnection.FillName(_obj);
    }

  }
}