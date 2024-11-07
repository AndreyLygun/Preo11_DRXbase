using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccessRightsBulkQueueItem;

namespace Sungero.Docflow.Shared
{
  partial class AccessRightsBulkQueueItemFunctions
  {
    /// <summary>
    /// Установить приоритет обработки элементов очереди по умолчанию.
    /// </summary>
    public virtual void SetDefaultPriority()
    {
      _obj.Priority = Constants.AccessRightsBulkQueueItem.Priorities.Default;
    }
    
    /// <summary>
    /// Установить низкий приоритет обработки элементов очереди.
    /// </summary>
    public virtual void SetLowPriority()
    {
      _obj.Priority = Constants.AccessRightsBulkQueueItem.Priorities.Low;
    }
  }
}