using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemPredictionInfo;

namespace Sungero.RecordManagement.Server
{
  partial class ActionItemPredictionInfoFunctions
  {
    /// <summary>
    /// Сохранить результат предсказания поручения.
    /// </summary>
    /// <returns>True - если удалось сохранить результат, иначе false.</returns>
    [Public]
    public virtual bool TrySave()
    {
      try
      {
        _obj.Save();
        return true;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Cannot save prediction info (ID = {0})", ex, _obj.Id);
        return false;
      }
    }
    
    /// <summary>
    /// Очистить результат обработки документа и черновики поручений во вложениях родительского задания или задачи.
    /// </summary>
    [Public]
    public virtual void RemoveArioTaskInfoAndActionItemDraft()
    {
      _obj.ArioTaskId = null;
      _obj.ArioTaskStatus = null;
      _obj.ArioResultJson = null;
      
      if (!_obj.ActionItemId.HasValue)
        return;
      var draftActionItem = ActionItemExecutionTasks.GetAll(x => x.Id == _obj.ActionItemId.Value).FirstOrDefault();
      var parentAssignment = draftActionItem?.ParentAssignment;
      if (draftActionItem == null || parentAssignment == null)
        return;
      
      if (ActionItemExecutionAssignments.Is(parentAssignment))
        RecordManagement.PublicFunctions.ActionItemExecutionAssignment.RemoveDraftFromAttachments(ActionItemExecutionAssignments.As(parentAssignment), draftActionItem);
      
      if (DocumentReviewTasks.Is(parentAssignment.Task))
        RecordManagement.PublicFunctions.DocumentReviewTask.RemoveDraftFromAttachments(DocumentReviewTasks.As(parentAssignment.Task), draftActionItem);
    }
  }
}