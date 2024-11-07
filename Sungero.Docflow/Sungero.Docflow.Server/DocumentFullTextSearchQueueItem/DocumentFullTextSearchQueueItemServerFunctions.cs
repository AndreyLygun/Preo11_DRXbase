using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentFullTextSearchQueueItem;

namespace Sungero.Docflow.Server
{
  partial class DocumentFullTextSearchQueueItemFunctions
  {
    /// <summary>
    /// Подготовить элемент очереди для повторной отправки на извлечение текста в Ario.
    /// </summary>
    [Public]
    public void PrepareToResendForTextExtraction()
    {
      _obj.Retries = _obj.Retries.HasValue ? _obj.Retries + 1 : 1;
      _obj.ProcessingStatus = Docflow.DocumentFullTextSearchQueueItem.ProcessingStatus.Scheduled;
      _obj.ExtractTextTaskId = null;
      
      // Задать пониженный приоритет для повторных обработок.
      _obj.Priority = Constants.DocumentFullTextSearchQueueItem.Priorities.Low;
      try
      {
        _obj.Save();
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("IndexDocumentsForFullTextSearch. Document (ID = {0}).", ex, _obj.DocumentId);
      }
    }
  }
}