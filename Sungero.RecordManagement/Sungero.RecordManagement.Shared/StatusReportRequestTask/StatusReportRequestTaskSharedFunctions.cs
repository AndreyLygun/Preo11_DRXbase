using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.StatusReportRequestTask;

namespace Sungero.RecordManagement.Shared
{
  partial class StatusReportRequestTaskFunctions
  {
    /// <summary>
    /// Получить тему запроса отчета.
    /// </summary>
    /// <param name="beginningSubject">Начальная тема.</param>
    /// <param name="performer">Исполнитель запроса отчета.</param>
    /// <returns>Сформированная тема.</returns>
    public virtual string GetStatusReportRequestSubject(CommonLibrary.LocalizedString beginningSubject, Sungero.Company.IEmployee performer)
    {
      var parentTask = ActionItemExecutionTasks.As(_obj.ParentTask);
      var parentAssignment = ActionItemExecutionAssignments.As(_obj.ParentAssignment);
      
      if (performer != null)
      {
        if (parentTask != null && parentTask.IsCompoundActionItem.Value)
        {
          var partAssignment = Functions.ActionItemExecutionTask.Remote.GetUnfinishedActionItems(parentTask)
            .Where(j => Equals(j.Performer, performer))
            .Where(a => ActionItemExecutionTasks.Is(a.Task))
            .FirstOrDefault();

          parentTask = partAssignment != null ? ActionItemExecutionTasks.As(partAssignment.Task) : parentTask;
        }
        
        if (parentTask == null && parentAssignment != null)
        {
          var assignment = Functions.ActionItemExecutionAssignment.Remote.GetUnfinishedActionItems(parentAssignment)
            .FirstOrDefault(j => Equals(j.Performer, performer));

          parentTask = assignment != null ? ActionItemExecutionTasks.As(assignment.Task) : ActionItemExecutionTasks.As(parentAssignment.Task);
        }
      }
      else
        parentTask = parentTask ?? ActionItemExecutionTasks.As(parentAssignment.Task);
      
      var subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(parentTask, beginningSubject);
      
      return Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
    }
    
    /// <summary>
    /// Получить тему запроса отчета.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="beginningSubject">Начальная тема.</param>
    /// <returns>Сформированная тема.</returns>
    [Obsolete("Метод не используется с 05.04.2024 и версии 4.10. Используйте метод GetStatusReportRequestSubject(IStatusReportRequestTask task, LocalizedString beginningSubject, IEmployee performer).")]
    public static string GetStatusReportRequestSubject(Sungero.RecordManagement.IStatusReportRequestTask task, CommonLibrary.LocalizedString beginningSubject)
    {
      return Functions.StatusReportRequestTask.GetStatusReportRequestSubject(task, beginningSubject, task.Assignee);
    }
  }
}