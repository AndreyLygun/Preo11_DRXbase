using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineExtensionTask;
using Sungero.Workflow;

namespace Sungero.Docflow.Server
{
  partial class DeadlineExtensionTaskFunctions
  {
    /// <summary>
    /// Обработать продление срока задания.
    /// </summary>
    public virtual void ProcessAssignmentDeadlineExtension()
    {
      // Если родительское задание прекращено, то срок не продлять.
      if (_obj.ParentAssignment.Status != Workflow.AssignmentBase.Status.InProcess)
        return;
      
      var extendDeadlineResult = this.ExtendAssignmentDeadline(Assignments.As(_obj.ParentAssignment));
      var extendDeadlineLog = this.GetExtendDeadlineLogString(extendDeadlineResult);
      Logger.WithLogger(Constants.DeadlineExtensionTask.DeadlineExtensionTaskLoggerPostfix).Debug(extendDeadlineLog);
    }
    
    /// <summary>
    /// Обработать продление срока конкурентных заданий.
    /// </summary>
    /// <param name="parentAssignment">Задание, из которого сделан запрос на продление срока.</param>
    public virtual void ProcessCompetitiveAssignmentsDeadlineExtension(IAssignment parentAssignment)
    {
      var competitiveAssignments = Functions.Module.GetParallelAssignments(parentAssignment);
      foreach (var assignment in competitiveAssignments)
      {
        var extendDeadlineResult = this.ExtendAssignmentDeadline(assignment);
        var extendDeadlineLog = this.GetExtendDeadlineLogString(extendDeadlineResult);
        if (assignment.Id != parentAssignment.Id)
          extendDeadlineLog = string.Format("{0}. Extended from competitive assignment: {1}", extendDeadlineLog, assignment.Id);
        Logger.WithLogger(Constants.DeadlineExtensionTask.DeadlineExtensionTaskLoggerPostfix).Debug(extendDeadlineLog);
      }
    }
    
    /// <summary>
    /// Продлить срок задания.
    /// </summary>
    /// <param name="assignment">Задание, для которого продлевается срок.</param>
    /// <returns>True, если срок упешно продлен, false - неуспешно, null - функции продления для задания не существует.</returns>
    public virtual bool? ExtendAssignmentDeadline(IAssignment assignment)
    {
      // Если функция ExtendAssignmentDeadline для задания реализована, возвращаем результат:
      // True - продление срока задания прошло успешно, False - неуспешно.
      var extendDeadlineResult = (bool?)Functions.Module.GetServerEntityFunctionResult(assignment,
                                                                                       "ExtendAssignmentDeadline",
                                                                                       new List<object>() { _obj.NewDeadline });
      
      // Если функции ExtendAssignmentDeadline не существует - вернется null и выполнится логика по умолчанию.
      if (extendDeadlineResult == null)
        assignment.Deadline = _obj.NewDeadline;
      
      return extendDeadlineResult;
    }
    
    /// <summary>
    /// Получить строку с информацией о продлении срока для записи в лог.
    /// </summary>
    /// <param name="extendDeadlineResult">Результат продления срока задания.</param>
    /// <returns>Строка с информацией о продлении срока.</returns>
    public virtual string GetExtendDeadlineLogString(bool? extendDeadlineResult)
    {
      var logFirstPart = string.Format("Assignment {0}.", _obj.ParentAssignment.Id);
      var isDeadlineExtended = false;
      var methodType = string.Empty;
      
      if (extendDeadlineResult != null)
      {
        isDeadlineExtended = extendDeadlineResult.Value;
        methodType = _obj.ParentAssignment.Info.Name;
      }
      else
      {
        // Если функция ExtendAssignmentDeadline не существует - вернется null и выполняется логика по умолчанию.
        isDeadlineExtended = true;
        methodType = "default";
      }
      
      return string.Format("{0} Is deadline extended: {1}. Method: {2}. Deadline extension task: {3}",
                           logFirstPart, isDeadlineExtended, methodType, _obj.Id);
    }
    
    /// <summary>
    /// Обработать продление срока задачи.
    /// </summary>
    public virtual void ProcessTaskDeadlineExtension()
    {
      // Если родительское задание прекращено, то срок не продлять.
      if (_obj.ParentAssignment.Status != Workflow.AssignmentBase.Status.InProcess)
        return;
      
      // Если функция ExtendTaskDeadline для задачи реализована, возвращаем результат:
      // True - продление срока задачи прошло успешно, False - неуспешно.
      if (_obj.NewDeadline.HasValue)
      {
        var parametersList = new List<object> { _obj.ParentAssignment, _obj.NewDeadline.Value };
        var extendTaskDeadlineResult = (bool?)Functions.Module.GetServerEntityFunctionResult(_obj.ParentAssignment.Task,
                                                                                             "ExtendTaskDeadline", parametersList);
        var logFirstPart = string.Format("Task {0}", _obj.Id);
        var methodType = _obj.ParentAssignment.Task.Info.Name;
        var isDeadlineExtended = extendTaskDeadlineResult ?? false;
        this.LogExtendedDeadline(logFirstPart, isDeadlineExtended, methodType);
      }
    }
    
    /// <summary>
    /// Получить срок продления в строковом формате.
    /// </summary>
    /// <param name="desiredDeadline">Срок.</param>
    /// <returns>Строковое представление.</returns>
    public static string GetDesiredDeadlineLabel(DateTime desiredDeadline)
    {
      using (TenantInfo.Culture.SwitchTo())
      {
        if (desiredDeadline == desiredDeadline.Date)
          return desiredDeadline.ToString("d");
        
        var utcOffset = Calendar.UtcOffset.TotalHours;
        var utcOffsetLabel = utcOffset >= 0 ? "+" + utcOffset.ToString() : utcOffset.ToString();
        return string.Format("{0:g} (UTC{1})", desiredDeadline, utcOffsetLabel);
      }
    }
    
    /// <summary>
    /// Получить исполнителей продления поручения.
    /// </summary>
    /// <param name="parent">Родительское задание, от которого создается задача на продление.</param>
    /// <returns>Исполнители и признак, можно ли пользователю выбирать самому.</returns>
    [Remote(IsPure = true, PackResultEntityEagerly = true)]
    public static Structures.DeadlineExtensionTask.ActionItemAssignees GetAssigneesForActionItemExecutionTask(RecordManagement.IActionItemExecutionAssignment parent)
    {
      var users = new List<IUser>();
      var canSelect = true;
      var leadItemExecution = RecordManagement.ActionItemExecutionTasks.As(parent.Task);
      
      // Исполнителем указать контролёра, если его нет, то стартовавшего задачу, если и его нет, то автора.
      // Если контроля не было, то стартовавшего задачу.
      if (leadItemExecution.IsUnderControl == true)
      {
        canSelect = false;
        
        if (leadItemExecution.IsCompoundActionItem == true)
        {
          var part = leadItemExecution.ActionItemParts.Where(x => Equals(x.ActionItemPartExecutionTask, parent.Task)).FirstOrDefault();
          if (part.Supervisor != null)
            users.Add(part.Supervisor);
        }
        
        users.Add(leadItemExecution.Supervisor);
      }

      if (leadItemExecution.ActionItemType.Value == RecordManagement.ActionItemExecutionTask.ActionItemType.Component && leadItemExecution.ParentTask != null &&
          RecordManagement.ActionItemExecutionTasks.Is(leadItemExecution.ParentTask))
        users.Add(leadItemExecution.ParentTask.StartedBy);
      else
        users.Add(leadItemExecution.StartedBy);
      users.Add(leadItemExecution.Author);
      users = users.Where(u => u.IsSystem != true).Distinct().ToList();
      if (canSelect && users.Count == 1)
        canSelect = false;
      
      return Structures.DeadlineExtensionTask.ActionItemAssignees.Create(users, canSelect);
    }
    
    /// <summary>
    /// Получить задачу на продление срока по заданию.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Задача, на основе которой создано задание.</returns>
    /// <remarks>Для реализации своей логики продления используются функции:
    /// GetAssigneesForDeadlineExtension - получения списка возможных сотрудников, которые могут продлить срок,
    /// ExtendAssignmentDeadline - продление сроков в задании,
    /// GetPerformersForDeadlineExtensionNotification - получение списка сотрудников, которых уведомить о продлении срока,
    /// GetNewDeadlineForDeadlineExtensionNotification - вычисления нового срока для конкретного получателя уведомления,
    /// ExtendTaskDeadline - продление сроков в задаче.
    /// Функции необязательные. Если они не реализованы, то будет использоваться логика по умолчанию.
    /// </remarks>
    [Remote(PackResultEntityEagerly = true)]
    [Public]
    public static IDeadlineExtensionTask GetDeadlineExtension(Sungero.Workflow.IAssignment assignment)
    {
      // Проверить наличие старой задачи на продление срока.
      var oldTask = Docflow.DeadlineExtensionTasks.GetAll()
        .Where(j => Equals(j.ParentAssignment, assignment))
        .Where(j => j.Status == Workflow.Task.Status.InProcess || j.Status == Workflow.Task.Status.Draft)
        .FirstOrDefault();
      
      if (oldTask != null)
        return oldTask;
      
      var task = Docflow.DeadlineExtensionTasks.CreateAsSubtask(assignment);
      
      task.NeedsReview = false;
      task.Subject = Functions.DeadlineExtensionTask.GetDeadlineExtensionSubject(task, DeadlineExtensionTasks.Resources.ExtendDeadlineTaskSubject);
      
      task.Assignee = Functions.DeadlineExtensionTask.GetAssigneesForDeadlineExtensionFromAssignment(task).FirstOrDefault();
      task.CurrentDeadline = assignment.Deadline;
      task.Author = assignment.Performer;

      return task;
    }
    
    /// <summary>
    /// Получить список сотрудников, у которых можно запросить продление срока.
    /// </summary>
    /// <returns>Список сотрудников.</returns>
    [Remote(IsPure = true)]
    public virtual List<IUser> GetAssigneesForDeadlineExtensionFromAssignment()
    {
      var deadlineAssignees = (List<IUser>)Functions.Module.GetServerEntityFunctionResult(_obj.ParentAssignment,
                                                                                          "GetAssigneesForDeadlineExtension",
                                                                                          null);
      
      var haveDeadlineAssignees = deadlineAssignees?.Any() == true;
      var assignees = haveDeadlineAssignees ? deadlineAssignees : this.GetDefaultAssigneesForDeadlineExtension();
      assignees = assignees.Where(a => a != null && a.IsSystem != true).ToList();
      
      this.LogAssigneesInfo(assignees, haveDeadlineAssignees ? _obj.ParentAssignment.GetType().Name : "default");
      return assignees;
    }
    
    /// <summary>
    /// Зписать в лог информацию о получателях.
    /// </summary>
    /// <param name="assignees">Список получателей.</param>
    /// <param name="methodEntityType">Название типа сущности, откуда был вызван метод.</param>
    public virtual void LogAssigneesInfo(List<IUser> assignees, string methodEntityType)
    {
      var assigneesId = assignees.Where(a => a != null).Select(a => a.Id);
      Logger.WithLogger(Constants.DeadlineExtensionTask.DeadlineExtensionTaskLoggerPostfix)
        .Debug(string.Format("Assignees [{0}] from {1}. Parent assignment {2}, deadline extension task {3}.",
                             string.Join(",", assigneesId),
                             methodEntityType,
                             _obj.ParentAssignment.Id,
                             _obj.Id));
    }
    
    /// <summary>
    /// Записать в лог результат продления срока.
    /// </summary>
    /// <param name="firstPart">Первая часть лога.</param>
    /// <param name="isDeadlineExtended">Признак продления срока.</param>
    /// <param name="methodEntityType">Название типа сущности, откуда был вызван метод.</param>
    public virtual void LogExtendedDeadline(string firstPart, bool isDeadlineExtended, string methodEntityType)
    {
      Logger.WithLogger(Constants.DeadlineExtensionTask.DeadlineExtensionTaskLoggerPostfix)
        .Debug(string.Format("{0} is deadline extended {1} from {2}. Deadline extension task {3}",
                             firstPart,
                             isDeadlineExtended,
                             methodEntityType,
                             _obj.Id));
    }
    
    /// <summary>
    /// Записать в лог новый срок выполнения из уведомления.
    /// </summary>
    /// <param name="noticeId">ИД уведомления.</param>
    /// <param name="newDeadline">Новый срок выполнения.</param>
    public virtual void LogDeadlineExtensionNotice(long noticeId, DateTime? newDeadline)
    {
      Logger.WithLogger(Constants.DeadlineExtensionTask.DeadlineExtensionTaskLoggerPostfix)
        .Debug(string.Format("Notification {0} new deadline {1}. Deadline extension task {2}",
                             noticeId,
                             newDeadline,
                             _obj.Id));
    }
    
    /// <summary>
    /// Получить список сотрудников, у которых можно запросить продление срока.
    /// </summary>
    /// <returns>Список сотрудников.</returns>
    public virtual List<IUser> GetDefaultAssigneesForDeadlineExtension()
    {
      var assignees = new List<IUser>();
      
      var author = Company.Employees.As(_obj.ParentAssignment.Author);
      if (author != null && author.Department.Manager != null)
      {
        if (Equals(_obj.ParentAssignment.Author, _obj.ParentAssignment.Performer))
        {
          assignees.Add(author.Department.Manager);
          assignees.Add(_obj.ParentAssignment.Author);
        }
        else
        {
          assignees.Add(_obj.ParentAssignment.Author);
          assignees.Add(author.Department.Manager);
        }
      }
      else
      {
        assignees.Add(_obj.ParentAssignment.Author);
      }
      
      return assignees.Distinct().ToList();
    }
    
    /// <summary>
    /// Получить тему задачи на продление срока.
    /// </summary>
    /// <param name="beginningSubject">Начальная тема задачи.</param>
    /// <returns>Сформированная тема задачи.</returns>
    [Public]
    public virtual string GetDeadlineExtensionSubject(CommonLibrary.LocalizedString beginningSubject)
    {
      using (TenantInfo.Culture.SwitchTo())
      {
        
        var subject = Functions.Module.IsTaskUsingOldScheme(_obj)
          ? Functions.DeadlineExtensionTask.GetOldSchemeDeadlineExtensionSubject(_obj, string.Format(">> {0} ", beginningSubject))
          : string.Format(">> {0} {1}", beginningSubject, _obj.ParentAssignment.Subject);
        
        subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
        
        if (subject == null)
          return null;
        
        if (subject.Length > DeadlineExtensionTasks.Info.Properties.Subject.Length)
          subject = subject.Substring(0, DeadlineExtensionTasks.Info.Properties.Subject.Length);
        
        return subject;
      }
    }
    
    /// <summary>
    /// Сформировать тему задачи на продление срока для старой схемы.
    /// </summary>
    /// <param name="subject">Начальная тема задачи.</param>
    /// <returns>Сформированная тема задачи.</returns>
    public virtual string GetOldSchemeDeadlineExtensionSubject(string subject)
    {
      if (Sungero.RecordManagement.ActionItemExecutionAssignments.Is(_obj.ParentAssignment))
      {
        var executionAssignment = Sungero.RecordManagement.ActionItemExecutionAssignments.As(_obj.ParentAssignment);
        if (!string.IsNullOrWhiteSpace(executionAssignment.ActionItem))
        {
          var hasDocument = executionAssignment.DocumentsGroup.OfficialDocuments.Any();
          var resolution = Sungero.RecordManagement.PublicFunctions.ActionItemExecutionTask
            .FormatActionItemForSubject(executionAssignment.ActionItem, hasDocument);
          
          // Кавычки даже для поручений без документа.
          if (!hasDocument)
            resolution = string.Format("\"{0}\"", resolution);
          
          subject += DeadlineExtensionTasks.Resources.SubjectFromActionItemFormat(resolution);
        }
        
        // Добавить имя документа, если поручение с документом.
        var document = executionAssignment.DocumentsGroup.OfficialDocuments.FirstOrDefault();
        if (document != null)
          subject += Sungero.RecordManagement.ActionItemExecutionTasks.Resources.SubjectWithDocumentFormat(document.Name);
      }
      else
      {
        subject += _obj.ParentAssignment.Subject;
      }
      return subject;
    }

    /// <summary>
    /// Получить тему уведомления о продление срока.
    /// </summary>
    /// <param name="newDeadline">Новый срок задания.</param>
    /// <returns>Сформированная тема уведомления.</returns>
    public virtual string GetDeadlineNoticeSubject(DateTime newDeadline)
    {
      var desiredDeadlineLabel = Functions.DeadlineExtensionTask.GetDesiredDeadlineLabel(newDeadline);
      var subjectFormat = DeadlineExtensionTasks.Resources.ExtensionDeadlineFormat(desiredDeadlineLabel);
      var subject = Functions.DeadlineExtensionTask.GetDeadlineExtensionSubject(_obj, subjectFormat);
      return Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
    }
    
    /// <summary>
    /// Обработать уведомление о продлении срока.
    /// </summary>
    /// <param name="notice">Уведомление о продлении срока.</param>
    public virtual void ProcessExtendDeadlineNotice(IDeadlineExtensionNotification notice)
    {
      if (notice.Performer.Equals(_obj.Author))
      {
        notice.NewDeadline = _obj.NewDeadline;
        notice.PreviousDeadline = _obj.CurrentDeadline;
        return;
      }
      
      this.ProcessNewDeadline(notice);
    }
    
    /// <summary>
    /// Обработать новый срок для уведомления о продлении срока.
    /// </summary>
    /// <param name="notice">Уведомление о продлении срока.</param>
    public virtual void ProcessNewDeadline(IDeadlineExtensionNotification notice)
    {
      var newDeadline = (DateTime?)Functions.Module.GetServerEntityFunctionResult(_obj.ParentAssignment, "GetNewDeadlineForDeadlineExtensionNotification", new List<object> { notice.Performer });
      if (newDeadline.HasValue)
      {
        notice.Subject = Functions.DeadlineExtensionTask.GetDeadlineNoticeSubject(_obj, newDeadline.Value);
        notice.NewDeadline = newDeadline;
        notice.PreviousDeadline = null;
        this.LogDeadlineExtensionNotice(notice.Id, notice.NewDeadline);
      }
      else
      {
        notice.NewDeadline = _obj.NewDeadline;
        notice.PreviousDeadline = _obj.CurrentDeadline;
        this.LogDeadlineExtensionNotice(notice.Id, notice.NewDeadline);
      }
    }

    #region Expression-функции для No-Code
    
    /// <summary>
    /// Получить тему уведомления о продление срока.
    /// </summary>
    /// <param name="task">Задача "Продление срока".</param>
    /// <returns>Сформированная тема уведомления.</returns>
    [ExpressionElement("DeadlineExtensionNotifySubject", "")]
    public static string GetDeadlineExtensionSubject(Sungero.Docflow.IDeadlineExtensionTask task)
    {
      var desiredDeadline = task.NewDeadline.Value;
      return Functions.DeadlineExtensionTask.GetDeadlineNoticeSubject(task, desiredDeadline);
    }

    /// <summary>
    /// Получить получателей уведомления.
    /// </summary>
    /// <param name="task">Задача "Запрос на продление срока".</param>
    /// <returns>Список получателей уведомления.</returns>
    [ExpressionElement("DeadlineExtensionNotifyPerformers", "")]
    public static List<IEmployee> GetPerformersForNotification(Sungero.Docflow.IDeadlineExtensionTask task)
    {
      var parentAssignment = Assignments.As(task.ParentAssignment);
      if (parentAssignment != null && Functions.Module.IsCompetitive(parentAssignment))
      {
        var competitiveAssignments = Functions.Module.GetActiveParallelAssignments(parentAssignment);
        return competitiveAssignments.Select(a => Employees.As(a.Performer)).ToList();
      }
      
      var assigneesObject = Functions.Module.GetServerEntityFunctionResult(task.ParentAssignment, "GetPerformersForDeadlineExtensionNotification", null);
      var assignees = (List<IEmployee>)assigneesObject;
      if (assignees?.Any() != true)
      {
        var author = Employees.As(task.Author);
        if (author != null)
          assignees = new List<IEmployee>() { author };
      }
      return assignees;
    }
    
    #endregion
  }
}