using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.ArioExtensions.Models;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.ExtractTextQueueItem;

namespace Sungero.SmartProcessing.Server
{
  partial class ExtractTextQueueItemFunctions
  {
    /// <summary>
    /// Проверить состояние задачи на извлечение текста, сохранить полученный результат.
    /// </summary>
    /// <param name="arioConnector">Коннектор к Ario.</param>
    public virtual void ProcessTextExtractionTask(ArioExtensions.ArioConnector arioConnector = null)
    {
      if (!_obj.ArioTaskId.HasValue || _obj.ProcessingStatus != ProcessingStatus.InProcess)
        return;
      
      var arioTaskId = _obj.ArioTaskId.Value;
      try
      {
        if (arioConnector == null)
          arioConnector = Functions.Module.GetArioConnector();

        var extractTextTaskInfo = arioConnector.GetExtractTextTaskInfo(arioTaskId);
        if (extractTextTaskInfo == null || extractTextTaskInfo.Task == null)
        {
          Logger.DebugFormat("ProcessTextExtractionTask. Failed to get text extraction task status, queueId={0}, arioTaskId={1}",
                             _obj.Id, arioTaskId);
          return;
        }
        var arioResult = extractTextTaskInfo.Result;
        if (extractTextTaskInfo.Task.State == TaskState.HasError)
        {
          var errorMessage = string.Empty;
          if (arioResult != null)
          {
            if (!string.IsNullOrEmpty(arioResult.Message))
              errorMessage = string.Format(", message={0}", arioResult.Message);
            if (!string.IsNullOrEmpty(arioResult.Error))
              errorMessage += string.Format(", error={0}", arioResult.Error);
          }
          else
          {
            errorMessage = ". Task.Result is null";
          }
          Logger.DebugFormat("ProcessTextExtractionTask. Text extraction task failed{0}, queueId={1}, arioTaskId={2}",
                             errorMessage, _obj.Id, arioTaskId);
          this.SetProcessedStatus(ProcessingStatus.ErrorOccured);
          return;
        }
        if (extractTextTaskInfo.Task.State == TaskState.Aborted)
        {
          Logger.DebugFormat("ProcessTextExtractionTask. Ario services were restarted. Document will be resent for text extraction, queueId={0}, arioTaskId={1}",
                             _obj.Id, arioTaskId);
          _obj.ArioTaskId = null;
          this.SetProcessedStatus(ProcessingStatus.Awaiting);
          return;
        }
        if (extractTextTaskInfo.Task.State == TaskState.Completed)
        {
          if (arioResult != null && arioResult.Results.Any())
          {
            var extractedText = arioResult.Results.First().Text;
            if (!string.IsNullOrEmpty(extractedText))
            {
              using (var memory = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(extractedText)))
                _obj.ExtractedText.Write(memory);
            }
          }
          this.SetProcessedStatus(ProcessingStatus.Success);
          return;
        }
      }
      catch (Exception ex)
      {
        // Если ошибка вызвана недоступностью Ario, завершить обработку. Иначе изменить статус очереди на "Ошибка".
        if (!Functions.Module.IsArioEnabled(arioConnector))
        {
          Logger.DebugFormat("ProcessTextExtractionTask. Ario services are not available, queueId={0}", _obj.Id);
          return;
        }
        Logger.ErrorFormat("ProcessTextExtractionTask. Text extraction error, queueId={0}, arioTaskId={1}",
                           ex, _obj.Id, arioTaskId);
        
        this.SetProcessedStatus(ProcessingStatus.ErrorOccured);
      }
    }
    
    /// <summary>
    /// Отправить документ на извлечение текста в Ario.
    /// </summary>
    /// <param name="arioConnector">Коннектор к Ario.</param>
    public virtual void SendDocumentForTextExtraction(ArioExtensions.ArioConnector arioConnector = null)
    {
      _obj.ArioTaskId = null;
      try
      {
        var body = this.GetDocumentBody();
        if (body.Length > 0)
        {
          if (arioConnector == null)
            arioConnector = Functions.Module.GetArioConnector();
          
          var arioTaskId = arioConnector.ExtractTextAsync(body)?.Id ?? -1;
          if (arioTaskId > 0)
            _obj.ArioTaskId = arioTaskId;
          else
            Logger.DebugFormat("SendDocumentForTextExtraction. Ario task is empty, queueId={0}", _obj.Id);
        }
        else
          Logger.DebugFormat("SendDocumentForTextExtraction. Failed to get document body, queueId={0}", _obj.Id);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("SendDocumentForTextExtraction. Error sending document for text extraction, queueId={0}", ex, _obj.Id);
      }

      this.SetProcessedStatus(_obj.ArioTaskId.HasValue ? ProcessingStatus.InProcess : ProcessingStatus.ErrorOccured);
    }

    /// <summary>
    /// Получить тело документа для извлечения текста.
    /// </summary>
    /// <returns>Тело документа в виде массива байт.</returns>
    public virtual byte[] GetDocumentBody()
    {
      var result = new byte[0];
      if (!_obj.DocumentId.HasValue || !_obj.DocumentVersionNumber.HasValue)
      {
        Logger.DebugFormat("GetDocumentBody. Document version not specified, queueId={0}", _obj.Id);
        return result;
      }
      var documentId = _obj.DocumentId.Value;
      var document = Sungero.Content.ElectronicDocuments.GetAll(x => x.Id == documentId).FirstOrDefault();
      if (document == null)
      {
        Logger.DebugFormat("GetDocumentBody. Document not found, queueId={0}, documentId={1}", _obj.Id, documentId);
        return result;
      }
      
      if (document.IsEncrypted)
      {
        Logger.DebugFormat("GetDocumentBody. Document has been encrypted, queueId={0}, documentId={1}", _obj.Id, documentId);
        return result;
      }
                  
      if (!document.HasVersions)
      {
        Logger.DebugFormat("GetDocumentBody. Document has no versions, queueId={0}, documentId={1}", _obj.Id, documentId);
        return result;
      }
      var version = document.Versions.Where(x => x.Number == _obj.DocumentVersionNumber.Value).FirstOrDefault();
      if (version == null)
      {
        Logger.DebugFormat("GetDocumentBody. Version not found, queueId={0}, documentId={1}, versionNumber={2}",
                           _obj.Id, documentId, _obj.DocumentVersionNumber);
        return result;
      }
      if (version.Body != null)
      {
        using (var memory = new System.IO.MemoryStream())
        {
          AccessRights.SuppressSecurityEvents(() => version.Body.Read().CopyTo(memory));
          result = memory.ToArray();
        }
      }
      if (result.Length == 0)
        Logger.DebugFormat("GetDocumentBody. Document is empty, queueId={0}, documentId={1}, versionNumber={2}",
                           _obj.Id, documentId, _obj.DocumentVersionNumber);
      return result;
    }

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
        Logger.DebugFormat("ExtractTextQueueItem. Failed to set processing status. Queue item is locked by \"{0}\", queueId={1}",
                           lockInfo.OwnerName, _obj.Id);
        return;
      }
      
      _obj.ProcessingStatus = status;
      _obj.Save();
    }
    
  }
}