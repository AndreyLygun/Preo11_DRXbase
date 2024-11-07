using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractBase;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{
  partial class ContractBaseClientHandlers
  {

    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      base.Closing(e);
      
      _obj.State.Properties.DocumentGroup.IsRequired = false;      
    }

    public override void ValidTillValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      base.ValidTillValueInput(e);
      if (_obj.DaysToFinishWorks > 0 && e.NewValue != null)
      {
        TimeSpan daysRange = e.NewValue.Value - Calendar.UserToday;
        var maxDaysToFinish = daysRange.TotalDays;
        if (_obj.DaysToFinishWorks > maxDaysToFinish)
        {
          if (maxDaysToFinish > 0)
            e.AddError(Sungero.Contracts.ContractBases.Resources.DaysToFinishTooMatchFormat(maxDaysToFinish + 1));
          else            e.AddError(Sungero.Contracts.ContractBases.Resources.DocumentAlreadyFinish);
        }
      }
    }

    public virtual void DaysToFinishWorksValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue < 0)
        e.AddError(ContractBases.Resources.IncorrectReminder);
      if (e.NewValue > 0 && _obj.ValidTill != null)
      {
        TimeSpan daysRange = _obj.ValidTill.Value - Calendar.UserToday;
        var maxDaysToFinish = daysRange.TotalDays;
        if (e.NewValue > maxDaysToFinish)
        {
          if (maxDaysToFinish > 0)
            e.AddError(Sungero.Contracts.ContractBases.Resources.DaysToFinishTooMatchFormat(maxDaysToFinish + 1));
          else
            e.AddError(Sungero.Contracts.ContractBases.Resources.DocumentAlreadyFinish);
        }
      }
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.ContractBase.SetRequiredProperties(_obj);
      base.Refresh(e);
    }

  }
}