using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DocumentReviewAssignment;

namespace Sungero.RecordManagement.Shared
{
  partial class DocumentReviewAssignmentFunctions
  {
    /// <summary>
    /// Проверить наличие документа на рассмотрение в задании и наличие хоть каких-то прав на него.
    /// </summary>
    /// <returns>True, если с документом можно работать.</returns>
    [Public]
    public virtual bool HasDocumentAndCanRead()
    {
      return _obj.DocumentForReviewGroup.OfficialDocuments.Any();
    }
    
    /// <summary>
    /// Получить список просроченных задач на исполнение поручения в состоянии Черновик.
    /// </summary>
    /// <returns>Список просроченных задач на исполнение поручения в состоянии Черновик.</returns>
    public virtual List<IActionItemExecutionTask> GetDraftOverdueActionItemExecutionTasks()
    {
      var tasks = _obj.ResolutionGroup.ActionItemExecutionTasks.Where(t => t.Status == RecordManagement.ActionItemExecutionTask.Status.Draft);
      var overdueTasks = new List<IActionItemExecutionTask>();
      foreach (var task in tasks)
        if (Functions.ActionItemExecutionTask.CheckOverdueActionItemExecutionTask(task))
          overdueTasks.Add(task);
      
      return overdueTasks;
    }
  }
}