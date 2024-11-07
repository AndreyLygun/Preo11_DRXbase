using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionAssignment;
using Sungero.RecordManagement.ActionItemExecutionTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server
{
  partial class ActionItemExecutionAssignmentFunctions
  {
    /// <summary>
    /// Построить модель представления.
    /// </summary>
    /// <returns>Xml представление контрола состояние.</returns>
    [Remote(IsPure = true)]
    [LocalizeFunction("GetDraftActionItemStateViewFunctionName", "GetDraftActionItemStateViewFunctionDescription")]
    public StateView GetDraftActionItemStateView()
    {
      return Functions.Module.GetStateViewForDraftResolution(_obj.ActionItemDraftGroup.ActionItemExecutionTasks.ToList());
    }
    
    /// <summary>
    /// Построить модель состояния пояснения.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Модель состояния.</returns>
    [Remote(IsPure = true)]
    [LocalizeFunction("GetActionItemExecutionAssignmentStateViewFunctionName", "GetActionItemExecutionAssignmentStateViewFunctionDescription")]
    public static Sungero.Core.StateView GetActionItemExecutionAssignmentStateView(IActionItemExecutionAssignment assignment)
    {
      var stateView = Sungero.Core.StateView.Create();
      var block = stateView.AddBlock();
      var content = block.AddContent();
      
      content.AddLabel(GetDescription(assignment));
      
      block.ShowBorder = false;
      
      return stateView;
    }
    
    /// <summary>
    /// Получить пояснение к заданию.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Пояснение.</returns>
    private static string GetDescription(IActionItemExecutionAssignment assignment)
    {
      var description = string.Empty;
      
      var mainTask = ActionItemExecutionTasks.As(assignment.Task);
      
      if (mainTask == null)
        return description;
      
      var supervisor = mainTask.Supervisor;
      
      if (supervisor != null)
        description += (mainTask.ActionItemType == ActionItemType.Additional)
          ? RecordManagement.ActionItemExecutionTasks.Resources.OnControlWithResponsibleFormat(Sungero.Company.PublicFunctions.Employee.GetShortName(supervisor, false).TrimEnd('.'))
          : RecordManagement.ActionItemExecutionTasks.Resources.OnControlFormat(Sungero.Company.PublicFunctions.Employee.GetShortName(supervisor, false).TrimEnd('.'));
      
      if (mainTask.ActionItemType == ActionItemType.Additional)
      {
        description += RecordManagement.ActionItemExecutionTasks.Resources.YouAreAdditionalAssignee;
      }
      else
      {
        if (mainTask.ActionItemType == ActionItemType.Main && mainTask.CoAssignees.Any() && !mainTask.CoAssignees.Any(ca => Equals(ca.Assignee, assignment.Performer)))
          description += RecordManagement.ActionItemExecutionTasks.Resources.YouAreResponsibleAssignee;
        else
          description += RecordManagement.ActionItemExecutionTasks.Resources.YouAreAssignee;
      }
      
      return description;
    }
    
    /// <summary>
    /// Построить модель состояния.
    /// </summary>
    /// <returns>Модель состояния.</returns>
    [Remote(IsPure = true)]
    [LocalizeFunction("GetStateViewFunctionName", "GetStateViewFunctionDescription")]
    public Sungero.Core.StateView GetStateView()
    {
      var task = ActionItemExecutionTasks.As(_obj.Task);
      var additional = task.ActionItemType == ActionItemType.Additional;

      // Не выделять текущее, если задание прекращено.
      if (_obj.Status == Workflow.AssignmentBase.Status.Aborted && !additional)
      {
        var mainActionItemExecutionTask = Functions.ActionItemExecutionTask.GetMainActionItemExecutionTask(task);
        var stateViewModel = Structures.ActionItemExecutionTask.StateViewModel.Create();
        return Functions.ActionItemExecutionTask.GetActionItemExecutionTaskStateView(mainActionItemExecutionTask, null, stateViewModel, null, false, true);
      }
      else
        return Functions.ActionItemExecutionTask.GetStateView(task);
    }
    
    /// <summary>
    /// Проверка, все ли задания соисполнителям созданы.
    /// </summary>
    /// <returns>True, если все задания созданы, иначе False.</returns>
    [Remote(IsPure = true)]
    public bool AllCoAssigneeAssignmentsCreated()
    {
      var task = ActionItemExecutionTasks.As(_obj.Task);
      var allCoAssigneeAssignmentsCreated = task.CoAssignees.All(a => a.AssignmentCreated == true);
      if (!allCoAssigneeAssignmentsCreated)
      {
        var assigneesWithoutAssignments = task.CoAssignees.Where(a => a.AssignmentCreated != true).Select(x => x.Assignee);
        var coAssigneesTasks = Functions.ActionItemExecutionTask.GetCoAssigneeActionItemExecutionTasks(task, _obj);
        foreach (var assignee in assigneesWithoutAssignments)
        {
          var subTask = coAssigneesTasks.Where(t => Equals(assignee, t.Assignee)).FirstOrDefault();
          if (subTask != null)
            Logger.ErrorFormat("ActionItemExecutionTask (ID={0}). Wrong AssignmentCreated flag value for coassignee (ID={1}), subtask (ID={2}) exists.",
                               task.Id,
                               assignee.Id,
                               subTask.Id);
        }
        
      }
      return allCoAssigneeAssignmentsCreated;
    }
    
    /// <summary>
    /// Получить задания соисполнителей, не завершивших работу по поручению.
    /// </summary>
    /// <param name="assignment">Поручение.</param>
    /// <returns>Задания соисполнителей, не завершивших работу.</returns>
    [Public, Remote(IsPure = true), Obsolete("Метод не используется с 10.04.2024 и версии 4.10. Используйте функцию GetUnfinishedActionItems.")]
    public static IQueryable<IActionItemExecutionAssignment> GetActionItems(IActionItemExecutionAssignment assignment)
    {
      return GetUnfinishedActionItems(assignment);
    }
    
    /// <summary>
    /// Получить задания соисполнителей, не завершивших работу по поручению.
    /// </summary>
    /// <param name="assignment">Поручение.</param>
    /// <returns>Задания соисполнителей, не завершивших работу.</returns>
    [Public, Remote(IsPure = true)]
    public static IQueryable<IActionItemExecutionAssignment> GetUnfinishedActionItems(IActionItemExecutionAssignment assignment)
    {
      var actionItemSubtasks = assignment.Subtasks.Where(s => ActionItemExecutionTasks.Is(s));
      var actionItemSubtasksAssignments = actionItemSubtasks.SelectMany(a => Functions.ActionItemExecutionTask.GetUnfinishedActionItems(ActionItemExecutionTasks.As(a)));
      
      return ActionItemExecutionAssignments.GetAll(a => actionItemSubtasksAssignments.Contains(a));
    }
    
    /// <summary>
    /// Получить соисполнителей, не завершивших работу по поручению.
    /// </summary>
    /// <param name="entity">Поручение.</param>
    /// <returns>Соисполнителей, не завершивших работу.</returns>
    [Remote(IsPure = true), Obsolete("Метод не используется с 10.04.2024 и версии 4.10. Используйте метод GetUnfinishedSubActionItemsAssignees модуля RecordManagement.")]
    public static IQueryable<IUser> GetActionItemsAssignees(IActionItemExecutionAssignment entity)
    {
      return Functions.Module.GetUnfinishedSubActionItemsAssignees(entity);
    }
    
    /// <summary>
    /// Получить вложенные поручения соисполнителям.
    /// </summary>
    /// <param name="entity">Задание ответственного исполнителя.</param>
    /// <returns>Поручения.</returns>
    [Remote(IsPure = true)]
    public static List<IActionItemExecutionTask> GetSubActionItemExecution(IActionItemExecutionAssignment entity)
    {
      return ActionItemExecutionTasks
        .GetAll()
        .Where(j => j.Status.Value == Workflow.Task.Status.InProcess)
        .Where(j => j.ActionItemType == ActionItemType.Additional)
        .Where(j => j.ParentAssignment == entity)
        .ToList();
    }
    
    /// <summary>
    /// Получить все невыполненные подчиненные поручения.
    /// </summary>
    /// <returns>Список невыполненных подчиненных поручений.</returns>
    [Remote(IsPure = true)]
    public virtual List<IActionItemExecutionTask> GetNotCompletedSubActionItems()
    {
      var subActionItems = Functions.ActionItemExecutionTask.GetSubActionItemExecutions(_obj);
      var result = subActionItems.Where(x => x.IsCompoundActionItem != true ||
                                        x.IsCompoundActionItem == true &&
                                        !Functions.ActionItemExecutionTask.AllActionItemPartsAreCompleted(x));
      return result.ToList();
    }
    
    /// <summary>
    /// Получить все невыполненные подчиненные задачи на продление срока.
    /// </summary>
    /// <returns>Список невыполненных подчиненных задач на продление срока.</returns>
    [Remote(IsPure = true)]
    public virtual List<ITask> GetNotCompletedSubDeadlineExtensionTasks()
    {
      return Tasks.GetAll()
        .Where(t => DeadlineExtensionTasks.Is(t) || Docflow.DeadlineExtensionTasks.Is(t))
        .Where(t => t.ParentAssignment == _obj)
        .Where(t => t.Status.Value == Workflow.Task.Status.InProcess)
        .ToList();
    }
    
    /// <summary>
    /// Получить все невыполненные подчиненные задачи на запрос отчёта.
    /// </summary>
    /// <returns>Список невыполненных подчиненных задач на запрос отчёта.</returns>
    [Remote(IsPure = true)]
    public virtual List<IStatusReportRequestTask> GetNotCompletedSubReportRequestTasks()
    {
      return StatusReportRequestTasks.GetAll()
        .Where(t => t.ParentAssignment == _obj)
        .Where(t => t.Status.Value == Workflow.Task.Status.InProcess)
        .ToList();
    }
    
    /// <summary>
    /// Получить список сотрудников, у которых можно запросить продление срока.
    /// </summary>
    /// <returns>Список сотрудников.</returns>
    public virtual List<IUser> GetAssigneesForDeadlineExtension()
    {
      var assignees = new List<IUser>();
      var mainTask = ActionItemExecutionTasks.As(_obj.Task);
      if (mainTask.IsUnderControl == true)
      {
        var supervisor = mainTask.Supervisor;
        assignees.Add(supervisor);
      }
      else
      {
        if (mainTask.ActionItemType == ActionItemType.Component)
          assignees.Add(mainTask.ParentTask.StartedBy);
        else
          assignees.Add(mainTask.StartedBy);
        assignees.Add(mainTask.Author);
      }

      return assignees.Distinct().Where(a => a.IsSystem != true).ToList();
    }
    
    /// <summary>
    /// Продлить срок задания.
    /// </summary>
    /// <param name="newDeadline">Новый срок.</param>
    /// <returns>True - продление срока задания прошло успешно, False - неуспешно.</returns>
    public virtual bool ExtendAssignmentDeadline(DateTime newDeadline)
    {
      // Обновить срок у задания.
      _obj.Deadline = newDeadline;
      _obj.ScheduledDate = newDeadline;
      
      return true;
    }
    
    /// <summary>
    /// Получить получателей уведомления.
    /// </summary>
    /// <returns>Список получателей уведомления.</returns>
    public virtual List<IEmployee> GetPerformersForDeadlineExtensionNotification()
    {
      var assignees = new List<IEmployee>();
      assignees.Add(Sungero.Company.Employees.As(_obj.Performer));
      
      var task = ActionItemExecutionTasks.As(_obj.Task);
      if (task.CoAssignees != null)
        foreach (var assignee in task.CoAssignees)
          assignees.Add(Sungero.Company.Employees.As(assignee.Assignee));
      
      return assignees;
    }
    
    /// <summary>
    /// Получить срок исполнителя.
    /// </summary>
    /// <param name="performer">Исполнитель.</param>
    /// <returns>Срок исполнителя.</returns>
    public virtual DateTime GetNewDeadlineForDeadlineExtensionNotification(IUser performer)
    {
      var task = ActionItemExecutionTasks.As(_obj.Task);
      if (task.CoAssignees.Select(a => a.Assignee).Contains(performer))
        return task.CoAssigneesDeadline.Value;
      return task.Deadline.Value;
    }
    
    /// <summary>
    /// Удалить черновик поручения из области вложения черновиков.
    /// </summary>
    /// <param name="draftActionItem">Черновик поручения.</param>
    [Public]
    public virtual void RemoveDraftFromAttachments(IActionItemExecutionTask draftActionItem)
    {
      if (_obj.ActionItemDraftGroup.ActionItemExecutionTasks.Contains(draftActionItem))
        _obj.ActionItemDraftGroup.ActionItemExecutionTasks.Remove(draftActionItem);
    }
  }
}