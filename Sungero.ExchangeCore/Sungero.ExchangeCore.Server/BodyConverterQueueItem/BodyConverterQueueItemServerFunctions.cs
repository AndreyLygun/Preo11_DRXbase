using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.BodyConverterQueueItem;

namespace Sungero.ExchangeCore.Server
{
  partial class BodyConverterQueueItemFunctions
  {
    /// <summary>
    /// Найден элемент очереди в обработке у которого версия и документ совпадает с текущим элементом очереди.
    /// </summary>
    /// <returns>True - есть похожий в обработке, иначе - false.</returns>
    [Public]
    public virtual bool HasSimilarQueueItemInProcessing()
    {
      return Sungero.ExchangeCore.BodyConverterQueueItems.GetAll().Any(x => x.Document != null && x.VersionId != null && !Equals(x.Id, _obj.Id) && Equals(x.Document.Id, _obj.Document.Id) &&
                                                                       Equals(x.VersionId, _obj.VersionId) && x.AsyncHandlerId != null && x.AsyncHandlerId != string.Empty);
    }
    
    /// <summary>
    /// Признак устаревшего элемента очереди конвертации.
    /// </summary>
    /// <returns>True, если элемент очереди устарел.</returns>
    [Public]
    public virtual bool IsObsoleteQueueItem()
    {
      if (_obj.Document == null)
      {
        Exchange.PublicFunctions.Module.LogDebugFormat(_obj, string.Format("IsObsoleteQueueItem. Queue item property Document is null."));
        return true;
      }
      
      if (_obj.VersionId == null)
      {
        Exchange.PublicFunctions.Module.LogDebugFormat(_obj, string.Format("IsObsoleteQueueItem. Queue item property VersionId is null."));
        return true;
      }
      
      var equalsQueueItems = ExchangeCore.BodyConverterQueueItems.GetAll().Where(x => x.Document != null && x.VersionId != null &&
                                                                                 Equals(x.Document.Id, _obj.Document.Id) &&
                                                                                 Equals(x.VersionId, _obj.VersionId) &&
                                                                                 x.Id != _obj.Id);

      if (equalsQueueItems.Any() && _obj.Id < equalsQueueItems.Max(x => x.Id))
        return true;
      
      return false;
    }
  }
}