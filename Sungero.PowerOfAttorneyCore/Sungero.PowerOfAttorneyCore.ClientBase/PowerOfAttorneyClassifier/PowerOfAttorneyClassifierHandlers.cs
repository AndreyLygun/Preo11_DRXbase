using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyCore.PowerOfAttorneyClassifier;

namespace Sungero.PowerOfAttorneyCore
{
  partial class PowerOfAttorneyClassifierClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.PowerOfAttorneyClassifier.SetEnabledProperties(_obj);
    }

  }
}