using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyCore.PowerOfAttorneyClassifier;

namespace Sungero.PowerOfAttorneyCore.Client
{
  partial class PowerOfAttorneyClassifierFunctions
  {
    /// <summary>
    /// Установить доступность свойств.
    /// </summary>
    public virtual void SetEnabledProperties()
    {
      var loadedFromClassifier = !string.IsNullOrEmpty(_obj.Autokey);
      _obj.State.Properties.Name.IsEnabled = !loadedFromClassifier;
      _obj.State.Properties.Code.IsEnabled = !loadedFromClassifier;
      _obj.State.Properties.Mnemonic.IsEnabled = !loadedFromClassifier;
      _obj.State.Properties.Description.IsEnabled = !loadedFromClassifier;
      _obj.State.Properties.Started.IsEnabled = !loadedFromClassifier;
      _obj.State.Properties.Group.IsEnabled = !loadedFromClassifier;
      _obj.State.Properties.NsiId.IsEnabled = !loadedFromClassifier;
      _obj.State.Properties.Expiring.IsEnabled = !loadedFromClassifier;
      _obj.State.Properties.PwrVisibility.IsEnabled = !loadedFromClassifier;
      _obj.State.Properties.PwrIssuer.IsEnabled = !loadedFromClassifier;
      _obj.State.Properties.PwrRead.IsEnabled = !loadedFromClassifier;
      _obj.State.Properties.Context.IsEnabled = !loadedFromClassifier;
      _obj.State.Properties.LegalRelations.IsEnabled = !loadedFromClassifier;
      _obj.State.Properties.LawDetails.IsEnabled = !loadedFromClassifier;
    }
  }
}