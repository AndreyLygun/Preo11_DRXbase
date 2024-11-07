using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentComparisonInfo;

namespace Sungero.Docflow
{
  partial class DocumentComparisonInfoServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      var daysToStore = Docflow.PublicFunctions.Module.Remote.GetDocflowParamsNumbericValue(Constants.DocumentComparisonInfo.DaysToStoreParamName);
      if (daysToStore == 0)
        daysToStore = Constants.DocumentComparisonInfo.DefaultDaysToStore;
      _obj.DeletionDate = Calendar.AddWorkingDays(Calendar.Now, (int)daysToStore);
    }
  }

}