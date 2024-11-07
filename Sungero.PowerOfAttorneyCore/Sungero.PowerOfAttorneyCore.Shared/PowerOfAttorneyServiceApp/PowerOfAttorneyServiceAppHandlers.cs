using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyCore.PowerOfAttorneyServiceApp;

namespace Sungero.PowerOfAttorneyCore
{
  partial class PowerOfAttorneyServiceAppSharedHandlers
  {

    public virtual void UriChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (e.NewValue == e.OldValue || e.NewValue == null)
        return;
      
      var trimmedUri = e.NewValue.Trim();
      if (e.NewValue == trimmedUri)
        return;
      
      _obj.Uri = trimmedUri;
    }

  }
}