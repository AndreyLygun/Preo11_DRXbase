using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange;
using Sungero.ExchangeCore.HistoricalMessagesDownloadSession;

namespace Sungero.ExchangeCore.Server
{
  partial class HistoricalMessagesDownloadSessionFunctions
  {  
    /// <summary>
    /// Получить не обработанные элементы очереди синхронизации сообщений по сессии загрузки исторических сообщений.
    /// </summary>
    /// <returns>Не обработанные элементы очереди синхронизации сообщений по сессии загрузки исторических сообщений.</returns>
    [Public]
    public virtual List<IMessageQueueItem> GetNotProcessedMessageQueueItems()
    {
      return ExchangeCore.MessageQueueItems.GetAll(item => item.DownloadSession != null && Equals(item.DownloadSession, _obj) &&
                                                   Equals(item.RootBox, _obj.BusinessUnitBox) &&
                                                   item.ProcessingStatus == ExchangeCore.MessageQueueItem.ProcessingStatus.NotProcessed)
                                           .ToList();
    }
    
    /// <summary>
    /// Получить прекращенные элементы очереди синхронизации сообщений по сессии загрузки исторических сообщений.
    /// </summary>
    /// <returns>Прекращенные элементы очереди синхронизации сообщений по сессии загрузки исторических сообщений.</returns>
    [Public]
    public virtual List<IMessageQueueItem> GetSuspendedMessageQueueItems()
    {
      return ExchangeCore.MessageQueueItems.GetAll(item => item.DownloadSession != null && Equals(item.DownloadSession, _obj) &&
                                                   Equals(item.RootBox, _obj.BusinessUnitBox) &&
                                                   item.ProcessingStatus == ExchangeCore.MessageQueueItem.ProcessingStatus.Suspended)
                                           .ToList();
    }
    
    /// <summary>
    /// Получить сведения о документе обмена по сессии загрузки исторических сообщений.
    /// </summary>
    /// <returns>Cведения о документе обмена по сессии загрузки исторических сообщений.</returns>
    [Public]
    public virtual List<IExchangeDocumentInfo> GetDocumentInfos()
    {
      return Exchange.ExchangeDocumentInfos.GetAll(info => info.DownloadSession != null && Equals(info.DownloadSession, _obj))
                                           .ToList();
    }
    
    /// <summary>
    /// Получить основную информацию о сессии загрузки исторических сообщений.
    /// </summary>
    /// <returns>Основная информация о сессии загрузки исторических сообщений.</returns>
    [Public]
    public virtual string GetMainInfo()
    {
      return string.Format("Session ({0}), box ({1}), from {2} to {3}, {4}.",
                           _obj.Id,
                           _obj.BusinessUnitBox.Id,
                           _obj.PeriodBegin.Value.ToString("dd.MM.yy"),
                           _obj.PeriodEnd.Value.ToString("dd.MM.yy"),
                           _obj.DownloadingState);
    }

    /// <summary>
    /// Получить дополнительную информацию о сессии загрузки исторических сообщений.
    /// </summary>
    /// <returns>Основная информация о сессии загрузки исторических сообщений.</returns>
    [Public]
    public virtual string GetAdditionalInfo()
    {
      return string.Format("Messages: not processed - {0}, suspended - {1}. Downloaded - {2}. Last message {3}.",                           
                           this.GetNotProcessedMessageQueueItems().Count(),
                           this.GetSuspendedMessageQueueItems().Count(),
                           this.GetDocumentInfos().Count(),
                           _obj.LastMessageDate != null ? _obj.LastMessageDate.Value.ToString("dd.MM.yy H:mm") : "empty");
    }
  }
}