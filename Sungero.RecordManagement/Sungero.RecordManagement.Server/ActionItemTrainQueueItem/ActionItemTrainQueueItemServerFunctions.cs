using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemTrainQueueItem;

namespace Sungero.RecordManagement.Server
{
  partial class ActionItemTrainQueueItemFunctions
  {
    /// <summary>
    /// Установить статус обработки.
    /// </summary>
    /// <param name="status">Статус.</param>
    public virtual void SetProcessedStatus(Enumeration? status)
    {
      if (_obj.ProcessingStatus == status)
        return;
      
      var lockInfo = Locks.GetLockInfo(_obj);
      if (lockInfo.IsLockedByOther)
      {
        Logger.DebugFormat("ClassifierTraining. SetProcessedStatus. Failed to set processing status. Queue item is locked by \"{0}\", queueId={1}",
                           lockInfo.OwnerName, _obj.Id);
        return;
      }
      
      _obj.ProcessingStatus = status;
      _obj.Save();
    }

  }
}