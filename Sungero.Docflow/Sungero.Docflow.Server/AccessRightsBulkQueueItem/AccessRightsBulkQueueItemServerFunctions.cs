using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccessRightsBulkQueueItem;

namespace Sungero.Docflow.Server
{
  partial class AccessRightsBulkQueueItemFunctions
  {
    /// <summary>
    /// Изменить статус элемента очереди на "Обработано".
    /// </summary>
    /// <returns>True - если статус элемента успешно изменен, иначе false.</returns>
    public virtual bool SetProcessingStatusProcessed()
    {
      return this.ChangeItemStatus(Docflow.AccessRightsBulkQueueItem.ProcessingStatus.Processed);
    }
    
    /// <summary>
    /// Изменить статус элемента очереди на "В процессе".
    /// </summary>
    /// <returns>True - если статус элемента успешно изменен, иначе false.</returns>
    public virtual bool SetProcessingStatusInProcess()
    {
      return this.ChangeItemStatus(Docflow.AccessRightsBulkQueueItem.ProcessingStatus.InProcess);
    }
    
    /// <summary>
    /// Изменить статус элемента очереди на "Обработка прекращена".
    /// </summary>
    /// <returns>True - если статус элемента успешно изменен, иначе false.</returns>
    public virtual bool SetProcessingStatusSuspended()
    {
      return this.ChangeItemStatus(Docflow.AccessRightsBulkQueueItem.ProcessingStatus.Suspended);
    }

    /// <summary>
    /// Изменить статус элемента очереди.
    /// </summary>
    /// <param name="status">Статус элемента.</param>
    /// <returns>True - если статус элемента успешно изменен, иначе false.</returns>
    public virtual bool ChangeItemStatus(Enumeration status)
    {
      try
      {
        _obj.ProcessingStatus = status;
        _obj.Save();
      }
      catch (Exception ex)
      {
        Logger.DebugFormat("ChangeItemStatus. Queue item (ID={0}). Cannot change queue item processing status. {1}", _obj.Id, ex.ToString());
        return false;
      }
      return true;
    }
    
    /// <summary>
    /// Удалить элемент очереди.
    /// </summary>
    /// <param name="queueItem">Элемент очереди.</param>
    public static void DeleteAccessRightsBulkQueueItem(IAccessRightsBulkQueueItem queueItem)
    {
      if (queueItem != null)
        AccessRightsBulkQueueItems.Delete(queueItem);
    }
  }
}