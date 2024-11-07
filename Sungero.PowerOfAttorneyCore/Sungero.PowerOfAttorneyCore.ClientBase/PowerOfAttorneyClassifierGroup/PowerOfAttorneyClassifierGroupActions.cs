using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyCore.PowerOfAttorneyClassifierGroup;

namespace Sungero.PowerOfAttorneyCore.Client
{
  partial class PowerOfAttorneyClassifierGroupActions
  {
    public virtual void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var allDuplicates = Functions.PowerOfAttorneyClassifierGroup.Remote.GetDuplicates(_obj, true);
      if (allDuplicates.Any())
        allDuplicates.Show();
      else
        Dialogs.NotifyMessage(PowerOfAttorneyClassifierGroups.Resources.DuplicatesNotFound);
    }

    public virtual bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }
  }
}