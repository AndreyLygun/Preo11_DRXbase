using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.ExtractTextQueueItem;

namespace Sungero.SmartProcessing
{
  partial class ExtractTextQueueItemServerHandlers
  {

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      if (_obj.Created == null)
        _obj.Created = Calendar.Now;
    }
  }

}