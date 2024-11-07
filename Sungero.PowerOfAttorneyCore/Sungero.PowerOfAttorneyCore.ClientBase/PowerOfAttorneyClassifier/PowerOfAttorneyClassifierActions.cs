using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyCore.PowerOfAttorneyClassifier;

namespace Sungero.PowerOfAttorneyCore.Client
{
  partial class PowerOfAttorneyClassifierActions
  {
    public virtual void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var allDuplicates = Functions.PowerOfAttorneyClassifier.Remote.GetDuplicates(_obj, true);
      if (allDuplicates.Any())
        allDuplicates.Show();
      else
        Dialogs.NotifyMessage(PowerOfAttorneyClassifiers.Resources.DuplicatesNotFound);
    }

    public virtual bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

}