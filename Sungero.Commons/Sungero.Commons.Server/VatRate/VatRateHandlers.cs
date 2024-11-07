using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.VatRate;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons
{
  partial class VatRateCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.Sid);
    }
  }

}