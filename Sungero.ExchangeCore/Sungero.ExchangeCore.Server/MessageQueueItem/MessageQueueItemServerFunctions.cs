using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.MessageQueueItem;

namespace Sungero.ExchangeCore.Server
{
  partial class MessageQueueItemFunctions
  {
    
    /// <summary>
    /// Получить корневой элемент очереди.
    /// </summary>
    /// <returns>Корневой элемент очереди.</returns>
    [Public]
    public virtual IMessageQueueItem GetRootMessageQueueItem()
    {
      if (_obj.IsRootMessage == true)
        return _obj;
      
      return Sungero.ExchangeCore.MessageQueueItems.GetAll(q => Equals(q.RootBox, _obj.RootBox) &&
                                                           string.Equals(q.RootMessageId, _obj.RootMessageId) && q.IsRootMessage == true).FirstOrDefault();
    }
    
    /// <summary>
    /// Увеличить количество попыток переповтора.
    /// </summary>
    /// <param name="maxRetriesCount">Максимальное количество переповторов.</param>
    [Public]
    public virtual void IncrementRetries(int maxRetriesCount)
    {
      _obj.Retries += 1;
      
      if (_obj.Retries >= maxRetriesCount)
      {
        _obj.ProcessingStatus = Sungero.ExchangeCore.MessageQueueItem.ProcessingStatus.Suspended;
        _obj.Note = "Exceeded maximum count attempts to process message. Message queue item was automatically suspended.";
        Sungero.Exchange.PublicFunctions.Module.LogDebugFormat(_obj, string.Format("Exceeded maximum count attempts to process message. Message queue item was automatically suspended. Retries {0}.", _obj.Retries));
      }
      
      _obj.Save();
    }
    
    /// <summary>
    /// Определить, нужно ли прекращать обработку элемента очереди, если сессия загрузки исторических сообщений прекращена.
    /// </summary>
    /// <returns>True - если нужно ли прекращать обработку элемента очереди, иначе - false.</returns>
    [Public]
    public virtual bool NeedAbortHistoricalQueueItem()
    {
      if (_obj.DownloadSession == null)
        return false;
      
      var session = Sungero.ExchangeCore.HistoricalMessagesDownloadSessions.Get(_obj.DownloadSession.Id);
      if (_obj.IsManualRestart != true && session.DownloadingState == ExchangeCore.HistoricalMessagesDownloadSession.DownloadingState.Aborted)
        return true;
      
      return false;
    }
    
    /// <summary>
    /// Прекратить обработку элемента очереди при загрузке исторического сообщения.
    /// </summary>
    [Public]
    public virtual void AbortHistoricalQueueItem()
    {
      if (_obj.DownloadSession == null)
        return;
      
      var message = "Historical message download session was aborted. Message queue item was automatically suspended";
      _obj.ProcessingStatus = Sungero.ExchangeCore.MessageQueueItem.ProcessingStatus.Suspended;
      _obj.Note = message;
      _obj.Save();
      Sungero.Exchange.PublicFunctions.Module.LogDebugFormat(_obj, string.Format(message + " AsyncHandlerId: {0}.", _obj.AsyncHandlerId));
    }
    
    /// <summary>
    /// Определить, нужно ли увеличить счетчик повторов обработки элемента очереди.
    /// </summary>
    /// <returns>True - если нужно увеличить счетчик повторов, иначе - false.</returns>
    [Public]
    public virtual bool NeedIncrementRetries()
    {
      var counterpartyExternalId = _obj.CounterpartyExternalId;
      
      if (string.IsNullOrEmpty(counterpartyExternalId))
        return true;
      
      return Parties.Counterparties.GetAll(c => c.ExchangeBoxes.Any(e => Equals(e.OrganizationId, counterpartyExternalId) && 
                                                                    Equals(_obj.RootBox, e.Box) && 
                                                                    (e.CounterpartyBranchId == null || e.CounterpartyBranchId == string.Empty)))
        .Any();
    }
  }
}