using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccessRightsBulkQueueItem;

namespace Sungero.Docflow
{
  partial class AccessRightsBulkQueueItemServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      Functions.AccessRightsBulkQueueItem.SetDefaultPriority(_obj);
    }
  }
}