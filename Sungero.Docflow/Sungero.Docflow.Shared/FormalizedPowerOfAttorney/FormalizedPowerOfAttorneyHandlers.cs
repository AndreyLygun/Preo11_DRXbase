using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FormalizedPowerOfAttorney;

namespace Sungero.Docflow
{
  partial class FormalizedPowerOfAttorneySharedHandlers
  {

    public virtual void MainPoAChanged(Sungero.Docflow.Shared.FormalizedPowerOfAttorneyMainPoAChangedEventArgs e)
    {
      if (e.NewValue == null)
        Functions.FormalizedPowerOfAttorney.ClearMainPoAProperties(_obj);
      else
        Functions.FormalizedPowerOfAttorney.FillMainPoAProperties(_obj, e.NewValue);
    }

    public virtual void IsDelegatedChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue == true)
        _obj.DelegationType = FormalizedPowerOfAttorney.DelegationType.NoDelegation;
      else
      {
        Functions.FormalizedPowerOfAttorney.ClearMainPoAProperties(_obj);
        _obj.MainPoA = null;
      }
    }

    public virtual void PowersTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      Functions.FormalizedPowerOfAttorney.SetRequiredProperties(_obj);
    }

    public virtual void FormatVersionChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue != FormatVersion.Version003)
        _obj.PowersType = PowersType.FreeForm;
    }
    
    public virtual void FtsListStateChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue != FtsListState.Rejected && !string.IsNullOrWhiteSpace(_obj.FtsRejectReason))
        _obj.FtsRejectReason = string.Empty;
    }
    
    public virtual void UnifiedRegistrationNumberChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      this.FillName();
    }
  }
}