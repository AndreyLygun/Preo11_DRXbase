using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Sungero.Commons;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.RelationType;
using Sungero.Docflow;
using Sungero.Docflow.ApprovalStage;
using Sungero.Docflow.DocumentKind;
using Sungero.Docflow.OfficialDocument;
using Sungero.Domain;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using Sungero.RecordManagement.ActionItemExecutionTask;
using Sungero.Workflow;
using Init = Sungero.RecordManagement.Constants.Module.Initialize;

namespace Sungero.RecordManagement.Server
{

  public class ModuleFunctions
  {
    #region Виджеты
    
    #region Виджет "Поручения"

    /// <summary>
    /// Выбрать поручения для виджета.
    /// </summary>
    /// <param name="onlyOverdue">Только просроченные.</param>
    /// <param name="substitution">Включать замещающих.</param>
    /// <returns>Список поручений.</returns>
    public IQueryable<Sungero.RecordManagement.IActionItemExecutionTask> GetActionItemsToWidgets(bool onlyOverdue, bool substitution)
    {
      var users = substitution ? Substitutions.ActiveSubstitutedUsersWithoutSystem.ToList() : new List<IUser>();
      users.Add(Users.Current);
      var usersIds = users.Select(u => u.Id).ToList();

      return this.GetActionItemsUnderControl(usersIds, onlyOverdue);
    }
    
    /// <summary>
    /// Выбрать поручения, которые нужно проконтролировать.
    /// </summary>
    /// <param name="usersIds">Список Ид сотрудников.</param>
    /// <param name="onlyOverdue">Только просроченные.</param>
    /// <returns>Список поручений.</returns>
    [Public]
    public virtual IQueryable<IActionItemExecutionTask> GetActionItemsUnderControl(List<long> usersIds, bool onlyOverdue)
    {
      var tasks = ActionItemExecutionTasks.GetAll()
        .Where(t => t.Status == Workflow.AssignmentBase.Status.InProcess);

      if (onlyOverdue)
        tasks = tasks.Where(t => t.Deadline.HasValue &&
                            (!t.Deadline.Value.HasTime() && t.Deadline.Value < Calendar.UserToday ||
                             t.Deadline.Value.HasTime() && t.Deadline.Value < Calendar.Now));

      return tasks.Where(a => a.Supervisor != null && usersIds.Contains(a.Supervisor.Id));
    }

    #endregion

    #region "Динамика исполнения поручений в срок"

    /// <summary>
    /// Получить статистику по исполнению поручений.
    /// </summary>
    /// <param name="performer">Исполнитель, указанный в параметрах виджета.</param>
    /// <returns>Строка с результатом.</returns>
    public List<Structures.Module.ActionItemStatistic> GetActionItemCompletionStatisticForChart(Enumeration performer)
    {
      var periodBegin = Calendar.UserToday.AddMonths(-2).BeginningOfMonth();
      var periodEnd = Calendar.UserToday.EndOfMonth();
      
      var hasData = false;

      var author = Employees.Null;
      if (performer == RecordManagement.Widgets.ActionItemCompletionGraph.Performer.Author)
        author = Company.Employees.Current;

      var statistic = new List<Structures.Module.ActionItemStatistic>();

      var actionItems = Functions.Module.GetActionItemCompletionData(null, null, periodBegin, periodEnd, author, null, null, null, null, false);
      while (periodBegin <= Calendar.UserToday)
      {
        periodEnd = periodBegin.EndOfMonth();
        var currentStatistic = this.CalculateActionItemStatistic(actionItems, periodBegin, periodEnd);
        
        if (currentStatistic != null)
          hasData = true;
        
        statistic.Add(Structures.Module.ActionItemStatistic.Create(currentStatistic, periodBegin));
        
        periodBegin = periodBegin.AddMonths(1);
      }
      
      return hasData ? statistic : new List<Structures.Module.ActionItemStatistic>();
    }

    /// <summary>
    /// Получить статистику по исполнению поручений за месяц.
    /// </summary>
    /// <param name="actionItems">Список поручений.</param>
    /// <param name="beginDate">Начало периода.</param>
    /// <param name="endDate">Конец периода.</param>
    /// <returns>Статистика за период.</returns>
    private int? CalculateActionItemStatistic(List<Structures.Module.LightActionItem> actionItems, DateTime beginDate, DateTime endDate)
    {
      var serverBeginDate = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(beginDate);
      var serverEndDate = endDate.EndOfDay().FromUserTime();
      actionItems = actionItems.Where(t => t.Status == Sungero.Workflow.Task.Status.Completed &&
                                      (Calendar.Between(t.ActualDate.Value.Date, beginDate.Date, endDate.Date) ||
                                       t.Deadline.HasValue &&
                                       ((t.Deadline.Value.Date == t.Deadline.Value ? t.Deadline.Between(beginDate.Date, endDate.Date) : t.Deadline.Between(serverBeginDate, serverEndDate)) ||
                                        t.ActualDate.Value.Date >= endDate && (t.Deadline.Value.Date == t.Deadline.Value ? t.Deadline <= beginDate.Date : t.Deadline <= serverBeginDate))) ||
                                      t.Status == Sungero.Workflow.Task.Status.InProcess && t.Deadline.HasValue &&
                                      (t.Deadline.Value.Date == t.Deadline.Value ? t.Deadline <= endDate.Date : t.Deadline <= serverEndDate)).ToList();

      var totalCount = actionItems.Count;
      if (totalCount == 0)
        return null;
      
      actionItems = this.FillLightActionItemListAssignees(actionItems);
      
      var completedInTime = actionItems
        .Where(j => j.Status == Workflow.Task.Status.Completed)
        .Where(j => Docflow.PublicFunctions.Module.CalculateDelay(j.Deadline, j.ActualDate.Value, j.Assignee) == 0).Count();
      
      var inProcess = actionItems.Where(j => j.Status == Workflow.Task.Status.InProcess).Count();
      var inProcessOverdue = actionItems
        .Where(j => j.Status == Workflow.Task.Status.InProcess)
        .Where(j => Docflow.PublicFunctions.Module.CalculateDelay(j.Deadline, Calendar.Now, j.Assignee) > 0).Count();

      int currentStatistic = 0;
      int.TryParse(Math.Round(totalCount == 0 ? 0 : ((completedInTime + inProcess - inProcessOverdue) * 100.00) / (double)totalCount).ToString(),
                   out currentStatistic);

      return currentStatistic;
    }
    
    /// <summary>
    /// Получить краткую информацию по исполнению поручений в срок за период.
    /// </summary>
    /// <param name="beginDate">Начало периода.</param>
    /// <param name="endDate">Конец периода.</param>
    /// <param name="author">Автор.</param>
    /// <returns>Краткая информация по исполнению поручений в срок за период.</returns>
    [Remote]
    public virtual List<Structures.Module.LightActionItem> GetActionItemCompletionData(DateTime? beginDate,
                                                                                       DateTime? endDate,
                                                                                       IEmployee author)
    {
      return this.GetActionItemCompletionData(null, null, beginDate, endDate, author, null, null, null, null, false);
    }

    /// <summary>
    /// Признак того, что для совещания и/или документа были поручения, выполненные в срок.
    /// </summary>
    /// <param name="meeting">Совещание.</param>
    /// <param name="document">Документ.</param>
    /// <returns>True, если были поручения, выполненные в срок, False в противном случае.</returns>
    [Public, Remote]
    public bool ActionItemCompletionDataIsPresent(Meetings.IMeeting meeting, IOfficialDocument document)
    {
      return this.GetActionItemCompletionData(meeting, document, null, null, null, null, null, null, null, false).Any();
    }
    
    /// <summary>
    /// Получить краткую информацию по исполнению поручений в срок за период.
    /// </summary>
    /// <param name="meeting">Совещание.</param>
    /// <param name="document">Документ.</param>
    /// <param name="beginDate">Начало периода.</param>
    /// <param name="endDate">Конец периода.</param>
    /// <param name="author">Автор.</param>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="department">Подразделение.</param>
    /// <param name="performer">Исполнитель.</param>
    /// <param name="documentType">Тип документов во вложениях поручений.</param>
    /// <param name="isMeetingsCoverContext">Признак контекста вызова с обложки совещаний.</param>
    /// <param name="getCoAssignees">Признак необходимости получения соисполнителей.</param>
    /// <returns>Краткая информация по исполнению поручений в срок за период.</returns>
    [Obsolete("Метод не используется с 17.01.2024 и версии 4.9. Используйте метод GetActionItemCompletionData(IMeeting, IOfficialDocument, DateTime?, DateTime?, IEmployee, IBusinessUnit, IDepartment, IUser, IDocumentType, bool?).")]
    public virtual List<Structures.Module.LightActionItem> GetActionItemCompletionData(Meetings.IMeeting meeting,
                                                                                       IOfficialDocument document,
                                                                                       DateTime? beginDate,
                                                                                       DateTime? endDate,
                                                                                       IEmployee author,
                                                                                       IBusinessUnit businessUnit,
                                                                                       IDepartment department,
                                                                                       IUser performer,
                                                                                       IDocumentType documentType,
                                                                                       bool? isMeetingsCoverContext,
                                                                                       bool getCoAssignees)
    {
      var actionItemCompletionData = this.GetActionItemCompletionData(meeting, document, beginDate, endDate, author, businessUnit, department, performer, documentType, isMeetingsCoverContext);
      if (getCoAssignees)
        actionItemCompletionData = this.FillLightActionItemListCoAssigneeShortNames(actionItemCompletionData);
      return actionItemCompletionData;
    }

    /// <summary>
    /// Получить краткую информацию по исполнению поручений в срок за период.
    /// </summary>
    /// <param name="meeting">Совещание.</param>
    /// <param name="document">Документ.</param>
    /// <param name="beginDate">Начало периода.</param>
    /// <param name="endDate">Конец периода.</param>
    /// <param name="author">Автор.</param>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="department">Подразделение.</param>
    /// <param name="performer">Исполнитель.</param>
    /// <param name="documentType">Тип документов во вложениях поручений.</param>
    /// <param name="isMeetingsCoverContext">Признак контекста вызова с обложки совещаний.</param>
    /// <returns>Краткая информация по исполнению поручений в срок за период.</returns>
    public virtual List<Structures.Module.LightActionItem> GetActionItemCompletionData(Meetings.IMeeting meeting,
                                                                                       IOfficialDocument document,
                                                                                       DateTime? beginDate,
                                                                                       DateTime? endDate,
                                                                                       IEmployee author,
                                                                                       IBusinessUnit businessUnit,
                                                                                       IDepartment department,
                                                                                       IUser performer,
                                                                                       IDocumentType documentType,
                                                                                       bool? isMeetingsCoverContext)
    {
      var tasks = Enumerable.Empty<Structures.Module.LightActionItem>().ToList();
      var authorIds = new List<long>();
      var startedByIds = new List<long>();
      
      if (performer != null &&
          department != null  &&
          (!Employees.Is(performer) ||
           !Equals(Employees.As(performer).Department, department)))
      {
        /* Dmitriev_IA:
         * Если переданы одновременно Исполнитель и Подразделение, то фильтрация выборки будет происходить по обоим этим параметрам.
         * Фильтрация по Исполнителю отбирает задачи, в которых исполнителем указан переданный.
         * Фильтрация по Подразделению отбирает задачи, в которых есть Исполнитель, у Исполнителя есть подразделение и это подразделение совпадает с переданным.
         * Если это условие не выполняется, то выборка будет заведомо пустой - выполнять какие либо вычисления не имеет смысла.
         * В случае, если в качестве исполнителя передан не сотрудник, то выборка также не имеет смысла,
         *  поскольку подразделение у такого исполнителя будет пустым.
         */
        return tasks;
      }
      
      if (performer != null &&
          businessUnit != null &&
          (!Employees.Is(performer) ||
           Employees.As(performer).Department == null ||
           !Equals(Employees.As(performer).Department.BusinessUnit, businessUnit)))
      {
        /* Dmitriev_IA:
         * Если переданы одновременно Исполнитель и НОР, то фильтрация выборки будет происходить по обоим этим параметрам.
         * Фильтрация по Исполнителю отбирает задачи, в которых исполнителем указан переданный.
         * Фильтрация по НОР отбирает задачи, в которых есть Исполнитель, у Исполнителя есть подразделение, у подразделения есть НОР и эта НОР совпадает с переданной.
         * Если это условие не выполняется, то выборка будет заведомо пустой - выполнять какие либо вычисления не имеет смысла.
         * В случае, если в качестве исполнителя передан не сотрудник, то выборка также не имеет смысла,
         *  поскольку подразделение у такого исполнителя будет пустым.
         */
        return tasks;
      }
      
      var recipientsIds = Substitutions.ActiveSubstitutedUsers.Select(u => u.Id).ToList();
      recipientsIds.Add(Users.Current.Id);
      authorIds.AddRange(recipientsIds);
      startedByIds.AddRange(recipientsIds);
      
      var parameters = Structures.Module.ActionItemCompletionDataParameters.Create();
      parameters.IsAdministratorOrAdvisor = Sungero.Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor();
      parameters.Author = author;
      parameters.AuthorIds = authorIds;
      parameters.StartedByIds = startedByIds;
      parameters.BeginDate = beginDate;
      parameters.EndDate = endDate;
      parameters.Assignee = performer;
      parameters.Department = department;
      parameters.BusinessUnit = businessUnit;
      parameters.Document = document;
      parameters.Meeting = meeting;
      parameters.IsMeetingsCoverContext = isMeetingsCoverContext;
      parameters.DocumentType = documentType;
      
      AccessRights.AllowRead(
        () =>
        {
          var generalActionItems = this.GetGeneralActionItemCompletionQuery();
          generalActionItems = this.AddIsNotAdditionalTypeTaskCondition(generalActionItems);
          generalActionItems = this.AddAuthorOrStartedByCondition(generalActionItems, parameters);
          generalActionItems = this.AddActionItemCompletionConditions(generalActionItems, parameters);
          tasks = this.ExecuteActionItemCompletionQuery(generalActionItems, parameters);
          
          var componentTypeActionItems = this.GetComponentTypeActionItemCompletionQuery(parameters);
          componentTypeActionItems = this.AddMainTaskStartedByCondition(componentTypeActionItems, parameters);
          componentTypeActionItems = this.AddActionItemCompletionConditions(componentTypeActionItems, parameters);
          tasks.AddRange(this.ExecuteActionItemCompletionQuery(componentTypeActionItems, parameters));
          
          tasks = tasks.GroupBy(t => t.Id)
            .Select(g => g.First())
            .ToList();
        });
      
      return tasks;
    }

    /// <summary>
    /// Получить базовый запрос для получения данных по исполнению поручений в срок за период.
    /// </summary>
    /// <returns>Базовый запрос для получения данных по исполнению поручений в срок за период.</returns>
    public virtual IQueryable<IActionItemExecutionTask> GetGeneralActionItemCompletionQuery()
    {
      var query = ActionItemExecutionTasks.GetAll();
      return query;
    }
    
    /// <summary>
    /// Получить базовый запрос по пунктам составного поручения.
    /// </summary>
    /// <param name="parameters">Параметры.</param>
    /// <returns>Базовый запрос по пунктам составного поручения.</returns>
    public virtual IQueryable<IActionItemExecutionTask> GetComponentTypeActionItemCompletionQuery(Structures.Module.ActionItemCompletionDataParameters parameters)
    {
      return ActionItemExecutionTasks.GetAll()
        .Where(x => x.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Component);
    }

    /// <summary>
    /// Добавить условия к исходному запросу для получения данных по исполнению поручений в срок за период.
    /// </summary>
    /// <param name="query">Исходный запрос.</param>
    /// <param name="parameters">Параметры.</param>
    /// <returns>Запрос с условиями для получения данных по исполнению поручений в срок за период.</returns>
    public virtual IQueryable<IActionItemExecutionTask> AddActionItemCompletionConditions(IQueryable<IActionItemExecutionTask> query,
                                                                                          Structures.Module.ActionItemCompletionDataParameters parameters)
    {
      if (parameters == null)
        return query;

      query = this.AddCompletedOrInProcessTaskStatusCondition(query);
      query = this.AddIsNotCompoundTaskCondition(query);
      query = this.AddDeadlineCondition(query, parameters.BeginDate, parameters.EndDate);
      query = this.AddAuthorCondition(query, parameters);
      query = this.AddAssigneeCondition(query, parameters.Assignee);
      query = this.AddDepartmentCondition(query, parameters.Department);
      query = this.AddBusinessUnitCondition(query, parameters.BusinessUnit);
      query = this.AddDocumentInMainTaskGroupCondition(query, parameters.Document);
      query = this.AddCreatedFromMinutesCondition(query, parameters.Meeting, parameters.IsMeetingsCoverContext);
      query = this.AddDocumentTypeCondition(query, parameters.DocumentType);
      return query;
    }

    /// <summary>
    /// Добавить к исходному запросу условия по Автору или тому, кто поручение стартовал.
    /// </summary>
    /// <param name="query">Исходный запрос.</param>
    /// <param name="parameters">Параметры.</param>
    /// <returns>Запрос с условиями по Автору или тому, кто поручение стартовал.</returns>
    public virtual IQueryable<IActionItemExecutionTask> AddAuthorOrStartedByCondition(IQueryable<IActionItemExecutionTask> query,
                                                                                      Structures.Module.ActionItemCompletionDataParameters parameters)
    {
      // 1. Администратор должен видеть все
      // 2. Если в AuthorIds и StartedByIds пусто, то фильтровать не по чему - не добавлять лишнего.
      if (parameters == null ||
          parameters.IsAdministratorOrAdvisor ||
          ((parameters.AuthorIds == null || !parameters.AuthorIds.Any()) &&
           (parameters.StartedByIds == null || !parameters.StartedByIds.Any())))
        return query;
      
      if (parameters.AuthorIds.Any() && parameters.StartedByIds.Any() && parameters.Author == null)
      {
        return query.Where(x => parameters.AuthorIds.Contains(x.Author.Id) ||
                           parameters.StartedByIds.Contains(x.StartedBy.Id));
      }
      
      if (parameters.AuthorIds.Any() && parameters.StartedByIds.Any() && parameters.Author != null)
      {
        // Автор задан явно. Условие на автора накладывается вызывающим методом явно, тут проверять на contains излишне.
        return query.Where(x => parameters.StartedByIds.Contains(x.StartedBy.Id));
      }
      
      if (parameters.AuthorIds.Any() && !parameters.StartedByIds.Any() && parameters.Author == null)
      {
        return query.Where(x => parameters.AuthorIds.Contains(x.Author.Id));
      }
      
      if (!parameters.AuthorIds.Any() && parameters.StartedByIds.Any())
      {
        // Если задана выборка только по StartedBy, то не важно задан ли явно автор.
        return query.Where(x => parameters.StartedByIds.Contains(x.StartedBy.Id));
      }

      return query;
    }

    /// <summary>
    /// Добавить к исходному запросу условие по стартовавшим корневую задачу.
    /// </summary>
    /// <param name="query">Исходный запрос.</param>
    /// <param name="parameters">Параметры.</param>
    /// <returns>Запрос с условием по стартовавшим корневую задачу.</returns>
    public virtual IQueryable<IActionItemExecutionTask> AddMainTaskStartedByCondition(IQueryable<IActionItemExecutionTask> query,
                                                                                      Structures.Module.ActionItemCompletionDataParameters parameters)
    {
      if (parameters == null ||
          parameters.IsAdministratorOrAdvisor ||
          parameters.StartedByIds == null || !parameters.StartedByIds.Any())
        return query;

      return query.Where(x => parameters.StartedByIds.Contains(x.MainTask.StartedBy.Id));
    }

    /// <summary>
    /// Добавить к исходному запросу условие по статусу поручения: "Завершено" или "В работе".
    /// </summary>
    /// <param name="query">Исходный запрос.</param>
    /// <returns>Запрос с условием по статусу поручения: "Завершено" или "В работе".</returns>
    public virtual IQueryable<IActionItemExecutionTask> AddCompletedOrInProcessTaskStatusCondition(IQueryable<IActionItemExecutionTask> query)
    {
      return query.Where(x => x.Status == Sungero.Workflow.Task.Status.Completed ||
                         x.Status == Sungero.Workflow.Task.Status.InProcess);
    }

    /// <summary>
    /// Добавить к исходному запросу условие: "не составное".
    /// </summary>
    /// <param name="query">Исходный запрос.</param>
    /// <returns>Запрос с условием: "не составное".</returns>
    public virtual IQueryable<IActionItemExecutionTask> AddIsNotCompoundTaskCondition(IQueryable<IActionItemExecutionTask> query)
    {
      return query.Where(x => x.IsCompoundActionItem == false);
    }
    
    /// <summary>
    /// Добавить к исходному запросу условие: "не соисполнителю".
    /// </summary>
    /// <param name="query">Исходный запрос.</param>
    /// <returns>Запрос с условием: "не соисполнителю".</returns>
    public virtual IQueryable<IActionItemExecutionTask> AddIsNotAdditionalTypeTaskCondition(IQueryable<IActionItemExecutionTask> query)
    {
      return query.Where(x => x.ActionItemType != RecordManagement.ActionItemExecutionTask.ActionItemType.Additional);
    }

    /// <summary>
    /// Добавить к исходному запросу условие по типу документа основной группы вложений.
    /// </summary>
    /// <param name="query">Исходный запрос.</param>
    /// <param name="documentType">Тип документа.</param>
    /// <returns>Запрос с условием по типу документа основной группы вложений.</returns>
    /// <remarks>В коробке не используется. Добавлено для ТР "Обращения граждан". 72293.</remarks>
    public virtual IQueryable<IActionItemExecutionTask> AddDocumentTypeCondition(IQueryable<IActionItemExecutionTask> query,
                                                                                 Sungero.Docflow.IDocumentType documentType)
    {
      if (documentType == null)
        return query;

      var documentsGroupGuid = Docflow.PublicConstants.Module.TaskMainGroup.ActionItemExecutionTask;
      var documentKindIds = DocumentKinds.GetAll()
        .Where(x => Equals(x.DocumentType, documentType))
        .Select(x => x.Id)
        .ToList();
      var documentIds = OfficialDocuments.GetAll()
        .Where(x => documentKindIds.Contains(x.DocumentKind.Id))
        .Select(x => x.Id);
      return query.Where(x => x.AttachmentDetails.Any(ad => ad.GroupId == documentsGroupGuid && documentIds.Any(di => di == ad.AttachmentId)));
    }

    /// <summary>
    /// Добавить к исходному запросу условие по документу в главной группе поручения.
    /// </summary>
    /// <param name="query">Исходный запрос.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Запрос с условием по документу в главной группе поручения.</returns>
    public virtual IQueryable<IActionItemExecutionTask> AddDocumentInMainTaskGroupCondition(IQueryable<IActionItemExecutionTask> query,
                                                                                            Sungero.Docflow.IOfficialDocument document)
    {
      if (document == null)
        return query;

      var documentsGroupGuid = Docflow.PublicConstants.Module.TaskMainGroup.ActionItemExecutionTask;
      return query.Where(x => x.AttachmentDetails.Any(ad => ad.GroupId == documentsGroupGuid && ad.AttachmentId == document.Id));
    }
    
    /// <summary>
    /// Добавить к исходному запросу условие по автору поручения.
    /// </summary>
    /// <param name="query">Исходный запрос.</param>
    /// <param name="parameters">Параметры.</param>
    /// <returns>Запрос с условием по автору поручения.</returns>
    /// <remarks>Добавляет условие к запросу только если автор явно указан в параметрах.</remarks>
    public virtual IQueryable<IActionItemExecutionTask> AddAuthorCondition(IQueryable<IActionItemExecutionTask> query,
                                                                           Structures.Module.ActionItemCompletionDataParameters parameters)
    {
      if (parameters == null || parameters.Author == null)
        return query;
      
      return query.Where(x => x.Author.Id == parameters.Author.Id);
    }
    
    /// <summary>
    /// Добавить к исходному запросу условие: "Поручение создано в рамках протокола совещания".
    /// </summary>
    /// <param name="query">Исходный запрос.</param>
    /// <param name="meeting">Совещание.</param>
    /// <param name="isMeetingsCoverContext">Признак контекста вызова с обложки модуля "Совещания".</param>
    /// <returns>Запрос с условием: "Поручение создано в рамках протокола совещания".</returns>
    public virtual IQueryable<IActionItemExecutionTask> AddCreatedFromMinutesCondition(IQueryable<IActionItemExecutionTask> query,
                                                                                       Sungero.Meetings.IMeeting meeting,
                                                                                       bool? isMeetingsCoverContext)
    {
      if (meeting == null && isMeetingsCoverContext != true)
        return query;

      var documentsGroupGuid = Docflow.PublicConstants.Module.TaskMainGroup.ActionItemExecutionTask;
      var minutes = meeting == null ?
        Meetings.Minuteses.GetAll(x => x.Meeting != null) :
        Meetings.Minuteses.GetAll(x => Equals(x.Meeting, meeting));
      var minutesIds = minutes.Select(x => x.Id);
      return query.Where(x => x.AttachmentDetails.Any(ad => ad.GroupId == documentsGroupGuid && minutesIds.Any(mi => mi == ad.AttachmentId)));
    }

    /// <summary>
    /// Добавить к исходному запросу условие по сроку исполнения поручения.
    /// </summary>
    /// <param name="query">Исходный запрос.</param>
    /// <param name="beginDate">Начало периода.</param>
    /// <param name="endDate">Конец периода.</param>
    /// <returns>Запрос с условием по сроку исполнения поручения.</returns>
    /// <remarks>Условие будет добавлено для полей Deadline и ActualDate.</remarks>
    public virtual IQueryable<IActionItemExecutionTask> AddDeadlineCondition(IQueryable<IActionItemExecutionTask> query,
                                                                             DateTime? beginDate,
                                                                             DateTime? endDate)
    {
      if (!beginDate.HasValue && !endDate.HasValue)
        return query;

      var serverBeginDate = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(beginDate.Value);
      var serverEndDate = endDate.Value.EndOfDay().FromUserTime();
      return query.Where(t =>
                         t.Status == Sungero.Workflow.Task.Status.Completed &&
                         (Calendar.Between(t.ActualDate.Value.Date, beginDate.Value.Date, endDate.Value.Date) ||
                          t.Deadline.HasValue &&
                          ((t.Deadline.Value.Date == t.Deadline.Value
                            ? t.Deadline.Between(beginDate.Value.Date, endDate.Value.Date)
                            : t.Deadline.Between(serverBeginDate, serverEndDate)) ||
                           t.ActualDate.Value.Date >= endDate && (t.Deadline.Value.Date == t.Deadline.Value
                                                                  ? t.Deadline <= beginDate.Value.Date
                                                                  : t.Deadline <= serverBeginDate))) ||
                         t.Status == Sungero.Workflow.Task.Status.InProcess &&
                         t.Deadline.HasValue && (t.Deadline.Value.Date == t.Deadline.Value
                                                 ? t.Deadline <= endDate.Value.Date
                                                 : t.Deadline <= serverEndDate));
    }

    /// <summary>
    /// Добавить к исходному запросу условие по исполнителю.
    /// </summary>
    /// <param name="query">Исходный запрос.</param>
    /// <param name="assignee">Исполнитель.</param>
    /// <returns>Запрос с условием по исполнителю.</returns>
    public virtual IQueryable<IActionItemExecutionTask> AddAssigneeCondition(IQueryable<IActionItemExecutionTask> query,
                                                                             Sungero.CoreEntities.IUser assignee)
    {
      if (assignee == null)
        return query;

      return query.Where(x => Equals(x.Assignee, assignee));
    }

    /// <summary>
    /// Добавить к исходному запросу условие по подразделению.
    /// </summary>
    /// <param name="query">Исходный запрос.</param>
    /// <param name="department">Подразделение.</param>
    /// <returns>Запрос с условием по подразделению.</returns>
    public virtual IQueryable<IActionItemExecutionTask> AddDepartmentCondition(IQueryable<IActionItemExecutionTask> query,
                                                                               Sungero.Company.IDepartment department)
    {
      if (department == null)
        return query;

      return query.Where(x => x.Assignee != null && x.Assignee.Department != null && Equals(x.Assignee.Department, department));
    }

    /// <summary>
    /// Добавить к исходному запросу условие по НОР.
    /// </summary>
    /// <param name="query">Исходный запрос.</param>
    /// <param name="businessUnit">НОР.</param>
    /// <returns>Запрос с условием по НОР.</returns>
    public virtual IQueryable<IActionItemExecutionTask> AddBusinessUnitCondition(IQueryable<IActionItemExecutionTask> query,
                                                                                 Sungero.Company.IBusinessUnit businessUnit)
    {
      if (businessUnit == null)
        return query;

      return query.Where(x => x.Assignee != null &&
                         x.Assignee.Department != null &&
                         x.Assignee.Department.BusinessUnit != null &&
                         Equals(x.Assignee.Department.BusinessUnit, businessUnit));
    }

    /// <summary>
    /// Выполнить запрос получения данных по исполнению поручений в срок за период.
    /// </summary>
    /// <param name="query">Запрос.</param>
    /// <param name="parameters">Параметры.</param>
    /// <returns>Список структур Structures.Module.LightActionItem.</returns>
    public virtual List<Structures.Module.LightActionItem> ExecuteActionItemCompletionQuery(IQueryable<IActionItemExecutionTask> query,
                                                                                            Structures.Module.ActionItemCompletionDataParameters parameters)
    {
      List<Structures.Module.LightActionItem> lightActionItems = null;

      lightActionItems = query
        .Select(x => Structures.Module.LightActionItem.Create(x.Id,
                                                              x.Status,
                                                              x.ActualDate,
                                                              x.Deadline,
                                                              x.Author.Id,
                                                              x.Assignee.Id,
                                                              x.ActionItem,
                                                              x.ExecutionState,
                                                              null,
                                                              null,
                                                              null))
        .ToList();

      return lightActionItems;
    }

    /// <summary>
    /// Заполнить авторов в списке структур LightActionItem.
    /// </summary>
    /// <param name="lightActionItems">Список структур LightActionItem.</param>
    /// <returns>Список структур LightActionItem с заполненными авторами.</returns>
    public virtual List<Structures.Module.LightActionItem> FillLightActionItemListAuthors(List<Structures.Module.LightActionItem> lightActionItems)
    {
      var uniqueAuthorIds = lightActionItems.Select(x => x.AuthorId).Distinct().ToList();
      var uniqueAuthors = Sungero.CoreEntities.Users.GetAll().Where(x => uniqueAuthorIds.Contains(x.Id)).ToList();
      foreach (var lightActionItem in lightActionItems)
        lightActionItem.Author = uniqueAuthors.FirstOrDefault(x => x.Id == lightActionItem.AuthorId);

      return lightActionItems;
    }

    /// <summary>
    /// Заполнить исполнителей в списке структур LightActionItem.
    /// </summary>
    /// <param name="lightActionItems">Список структур LightActionItem.</param>
    /// <returns>Список структур LightActionItem с заполненными исполнителями.</returns>
    public virtual List<Structures.Module.LightActionItem> FillLightActionItemListAssignees(List<Structures.Module.LightActionItem> lightActionItems)
    {
      var uniqueAssigneeIds = lightActionItems.Select(x => x.AssigneeId).Distinct().ToList();
      var uniqueAssignees = Employees.GetAll().Where(x => uniqueAssigneeIds.Contains(x.Id)).ToList();
      foreach (var lightActionItem in lightActionItems)
        lightActionItem.Assignee = uniqueAssignees.FirstOrDefault(x => x.Id == lightActionItem.AssigneeId);

      return lightActionItems;
    }

    /// <summary>
    /// Заполнить имена соисполнителей в списке структур LightActionItem.
    /// </summary>
    /// <param name="lightActionItems">Список структур LightActionItem.</param>
    /// <returns>Список структур LightActionItem с заполненными именами соисполнителей.</returns>
    public virtual List<Structures.Module.LightActionItem> FillLightActionItemListCoAssigneeShortNames(List<Structures.Module.LightActionItem> lightActionItems)
    {
      var actionItemExecutionTaskIds = lightActionItems.Select(x => x.Id).ToList();
      var coAssigneeIds = Sungero.RecordManagement.ActionItemExecutionTasks
        .GetAll(x => actionItemExecutionTaskIds.Contains(x.Id))
        .Where(x => x.CoAssignees.Any())
        .ToDictionary(x => x.Id, y => y.CoAssignees.Select(ca => ca.Assignee.Id).ToList());
      var uniqueCoAssigneeIds = coAssigneeIds.SelectMany(x => x.Value).Distinct().ToList();
      var uniqueShortNames = uniqueCoAssigneeIds.ToDictionary(x => x, y => Sungero.Company.Employees.GetAll().FirstOrDefault(empl => empl.Id == y)?.Person?.ShortName);
      foreach (var lightActionItem in lightActionItems)
      {
        var actionItemExecutionTaskCoAssigneeIds = new List<long>();
        if (!coAssigneeIds.TryGetValue(lightActionItem.Id, out actionItemExecutionTaskCoAssigneeIds))
          continue;
        var shortNames = uniqueShortNames.Where(x => actionItemExecutionTaskCoAssigneeIds.Contains(x.Key)).Select(x => x.Value);
        lightActionItem.CoAssigneeShortNames = string.Join(", ", shortNames);
      }

      return lightActionItems;
    }

    #endregion

    #endregion

    #region Типы задач

    /// <summary>
    /// Создать задачу по процессу "Рассмотрение входящего".
    /// </summary>
    /// <param name="document">Документ на рассмотрение.</param>
    /// <returns>Задача по процессу "Рассмотрение входящего".</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public static ITask CreateDocumentReview(Sungero.Docflow.IOfficialDocument document)
    {
      var task = CreateDocumentReviewTask(document, Tasks.Null);
      return task;
    }
    
    /// <summary>
    /// Создать задачу на рассмотрение документа с указанием задачи-основания.
    /// </summary>
    /// <param name="documentId">ИД документа на рассмотрение.</param>
    /// <param name="addresseeId">ИД адресата.</param>
    /// <param name="activeText">Текст задачи.</param>
    /// <returns>ИД задачи на рассмотрение.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual long CreateDocumentReviewTask(long documentId, long? addresseeId, string activeText)
    {
      var document = OfficialDocuments.GetAll(d => d.Id == documentId).FirstOrDefault();
      if (document == null)
        throw AppliedCodeException.Create(string.Format("Create review task. Document with ID ({0}) not found.", documentId));
      
      var addressee = Employees.Null;
      if (addresseeId.HasValue)
      {
        addressee = Employees.GetAll(e => e.Id == addresseeId).FirstOrDefault();
        if (addressee == null)
          throw AppliedCodeException.Create(string.Format("Create review task. Employee with ID ({0}) not found.", addresseeId));
      }
      
      var task = CreateDocumentReviewTask(document, null);
      if (addresseeId.HasValue)
      {
        task.Addressees.Clear();
        task.Addressees.AddNew().Addressee = addressee;
      }
      task.ActiveText = activeText;
      task.Save();
      
      return task.Id;
    }
    
    /// <summary>
    /// Создать задачу на рассмотрение документа с указанием задачи-основания.
    /// </summary>
    /// <param name="document">Документ на рассмотрение.</param>
    /// <param name="parentTask">Задача-основание.</param>
    /// <param name="addressees">Адресаты.</param>
    /// <returns>Задача на рассмотрение.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IDocumentReviewTask CreateDocumentReviewTask(Sungero.Docflow.IOfficialDocument document, ITask parentTask, List<IEmployee> addressees)
    {
      var task = CreateDocumentReviewTask(document, parentTask);
      Functions.DocumentReviewTask.SetAddressees(task, addressees);
      return task;
    }
    
    /// <summary>
    /// Создать задачу на рассмотрение документа с указанием задачи-основания.
    /// </summary>
    /// <param name="document">Документ на рассмотрение.</param>
    /// <param name="parentTask">Задача-основание.</param>
    /// <returns>Задача на рассмотрение.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IDocumentReviewTask CreateDocumentReviewTask(Sungero.Docflow.IOfficialDocument document, ITask parentTask)
    {
      var task = parentTask == null ? DocumentReviewTasks.Create() : DocumentReviewTasks.CreateAsSubtask(parentTask);
      
      task.DocumentForReviewGroup.All.Add(document);
      
      // Выдать права группе регистрации документа.
      if (document.DocumentRegister != null)
      {
        var registrationGroup = document.DocumentRegister.RegistrationGroup;
        
        if (registrationGroup != null)
          task.AccessRights.Grant(registrationGroup, DefaultAccessRightsTypes.Change);
      }
      
      Functions.DocumentReviewTask.SynchronizeAddressees(task, document);
      
      return task;
    }
    
    /// <summary>
    /// Создать задачу на рассмотрение документа с указанием задачи-основания.
    /// </summary>
    /// <param name="documentId">ИД документа.</param>
    /// <param name="addresseeId">ИД адресата.</param>
    /// <param name="parentTaskId">ИД задачи-основания.</param>
    /// <returns>ИД задачи на рассмотрение.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual long CreateDocumentReviewTaskFromParentTask(long documentId, long addresseeId, long parentTaskId)
    {
      var document = OfficialDocuments.GetAll(d => d.Id == documentId).FirstOrDefault();
      if (document == null)
        throw AppliedCodeException.Create(string.Format("Create review task. Document with ID ({0}) not found.", documentId));
      
      var addressee = Employees.GetAll(e => e.Id == addresseeId).FirstOrDefault();
      if (addressee == null)
        throw AppliedCodeException.Create(string.Format("Create review task. Employee with ID ({0}) not found.", addresseeId));
      
      var parentTask = Tasks.GetAll(t => t.Id == parentTaskId).FirstOrDefault();
      if (parentTask == null)
        throw AppliedCodeException.Create(string.Format("Create review task. Parent task with ID ({0}) not found.", parentTaskId));
      
      var task = CreateDocumentReviewTask(document, parentTask);
      
      task.Addressees.Clear();
      task.Addressees.AddNew().Addressee = addressee;
      task.Save();
      
      return task.Id;
    }
    
    /// <summary>
    /// Создать поручение по документу.
    /// </summary>
    /// <param name="document">Документ на рассмотрение.</param>
    /// <returns>Поручение по документу.</returns>
    /// <remarks>Только для создания самостоятельного поручения.
    /// Для создания подпоручения используется CreateActionItemExecutionTask(document, parentAssignment).</remarks>
    [Remote(PackResultEntityEagerly = true), Public]
    public virtual IActionItemExecutionTask CreateActionItemExecution(IOfficialDocument document)
    {
      return this.CreateActionItemExecution(document, Assignments.Null);
    }

    /// <summary>
    /// Создать поручение по документу, с указанием задания-основания.
    /// </summary>
    /// <param name="document">Документ, на основании которого создается задача.</param>
    /// <param name="parentAssignmentId">Задание-основание.</param>
    /// <returns>Поручение по документу.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public virtual IActionItemExecutionTask CreateActionItemExecution(IOfficialDocument document, long parentAssignmentId)
    {
      return this.CreateActionItemExecution(document, Assignments.Get(parentAssignmentId));
    }

    /// <summary>
    /// Создать поручение по документу, с указанием задания-основания.
    /// </summary>
    /// <param name="document">Документ, на основании которого создается задача.</param>
    /// <param name="parentAssignmentId">Задание-основание.</param>
    /// <param name="resolution">Текст резолюции.</param>
    /// <param name="assignedBy">Пользователь - автор резолюции.</param>
    /// <returns>Поручение по документу.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public virtual IActionItemExecutionTask CreateActionItemExecutionWithResolution(IOfficialDocument document, long parentAssignmentId, string resolution, Sungero.Company.IEmployee assignedBy)
    {
      var newTask = this.CreateActionItemExecution(document, Assignments.Get(parentAssignmentId));
      newTask.ActiveText = resolution;
      newTask.AssignedBy = Docflow.PublicFunctions.Module.Remote.IsUsersCanBeResolutionAuthor(document, assignedBy) ? assignedBy : null;
      return newTask;
    }

    /// <summary>
    /// Создать задачу на исполнение поручения по документу.
    /// </summary>
    /// <param name="documentId">ИД документа на рассмотрение.</param>
    /// <param name="assigneeId">ИД адресата.</param>
    /// <param name="isUnderControl">Поручение на контроле.</param>
    /// <param name="supervisorId">ИД контролера.</param>
    /// <param name="coassigneeId">ИД соисполнителя.</param>
    /// <param name="deadline">Срок.</param>
    /// <param name="activeText">Текст задачи.</param>
    /// <returns>ИД задачи на исполнение поручения.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual long CreateActionItemExecution(long documentId, long assigneeId, bool isUnderControl, long? supervisorId, long? coassigneeId, DateTime deadline, string activeText)
    {
      var document = OfficialDocuments.GetAll(d => d.Id == documentId).FirstOrDefault();
      if (document == null)
        throw AppliedCodeException.Create(string.Format("Create action item execution task. Document with ID ({0}) not found.", documentId));
      
      var assignee = Employees.GetAll(e => e.Id == assigneeId).FirstOrDefault();
      if (assignee == null)
        throw AppliedCodeException.Create(string.Format("Create action item execution task. Employee with ID ({0}) not found.", assigneeId));
      
      var supervisor = Employees.Null;
      if (isUnderControl)
        if (supervisorId.HasValue)
      {
        supervisor = Employees.GetAll(e => e.Id == supervisorId).FirstOrDefault();
        if (supervisor == null)
          throw AppliedCodeException.Create(string.Format("Create action item execution task. Employee with ID ({0}) not found.", supervisorId));
      }
      else
        throw AppliedCodeException.Create("Create action item execution task. Supervisor is required for action item with contol.");
      
      var coassignee = Employees.Null;
      if (coassigneeId.HasValue)
      {
        coassignee = Employees.GetAll(e => e.Id == coassigneeId).FirstOrDefault();
        if (coassignee == null)
          throw AppliedCodeException.Create(string.Format("Create action item execution task. Employee with ID ({0}) not found.", coassigneeId));
      }
      
      var task = this.CreateActionItemExecution(document, null);
      task.Assignee = assignee;
      task.Deadline = deadline;
      task.IsUnderControl = isUnderControl;
      if (isUnderControl)
        task.Supervisor = supervisor;
      if (coassigneeId.HasValue)
      {
        task.CoAssignees.Clear();
        task.CoAssignees.AddNew().Assignee = coassignee;
      }
      task.ActiveText = activeText;
      task.Save();
      
      return task.Id;
    }
    
    /// <summary>
    /// Создать поручение по документу с указанием задания-основания.
    /// </summary>
    /// <param name="document">Документ, на основании которого создается задача.</param>
    /// <param name="parentAssignment">Задание-основание.</param>
    /// <returns>Поручение по документу.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public virtual IActionItemExecutionTask CreateActionItemExecution(IOfficialDocument document, IAssignment parentAssignment)
    {
      var parentAssignmentId = parentAssignment != null ? parentAssignment.Id : -1;
      Logger.DebugFormat("Start CreateActionItemExecution, CreateAsSubtask = {0}, Parent assignment (ID={1}).", parentAssignment != null, parentAssignmentId);
      
      var task = parentAssignment == null ? ActionItemExecutionTasks.Create() : ActionItemExecutionTasks.CreateAsSubtask(parentAssignment);
      var taskId = task != null ? task.Id : -1;
      
      Logger.DebugFormat("Start SynchronizeAttachmentsToActionItem from document (ID={0}).", document?.Id);
      
      if (parentAssignment != null)
      {
        Logger.DebugFormat("Start SynchronizeAttachmentsToActionItem from parent task (ID={0}).", parentAssignment.Task.Id);
        Functions.Module.SynchronizeAttachmentsToActionItem(parentAssignment.Task, task);
      }
      else
      {
        Functions.ActionItemExecutionTask.SynchronizePrimaryDocumentToActionItem(task, document);
      }
      Logger.Debug("End SynchronizeAttachmentsToActionItem.");
      
      if (document != null)
      {
        // Выдать права на изменение группе регистрации. Группа регистрации будет взята из журнала документа.
        var documentRegister = document.DocumentRegister;
        if (documentRegister != null && documentRegister.RegistrationGroup != null)
        {
          Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Grant access rights to registration group (ID={1}).", taskId, documentRegister.RegistrationGroup.Id);
          task.AccessRights.Grant(documentRegister.RegistrationGroup, DefaultAccessRightsTypes.Change);
        }
      }

      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Set task Subject.", taskId);
      task.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(task, ActionItemExecutionTasks.Resources.TaskSubject);
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). End CreateActionItemExecution.", taskId);
      
      return task;
    }

    /// <summary>
    /// Создать поручение.
    /// </summary>
    /// <returns>Поручение.</returns>
    [Remote, Public]
    public virtual IActionItemExecutionTask CreateActionItemExecution()
    {
      return ActionItemExecutionTasks.Create();
    }
    
    /// <summary>
    /// Создать поручение из открытого задания.
    /// </summary>
    /// <param name="actionItemAssignment">Задание.</param>
    /// <returns>Поручение.</returns>
    [Public]
    public virtual IActionItemExecutionTask CreateActionItemExecutionFromExecution(IActionItemExecutionAssignment actionItemAssignment)
    {
      return this.CreateActionItemExecutionFromExecution(actionItemAssignment, Employees.Current);
    }
    
    /// <summary>
    /// Создать поручение из открытого задания.
    /// </summary>
    /// <param name="actionItemAssignment">Задание.</param>
    /// <param name="assignedBy">Кто выдал поручение.</param>
    /// <returns>Поручение.</returns>
    [Public]
    public virtual IActionItemExecutionTask CreateActionItemExecutionFromExecution(IActionItemExecutionAssignment actionItemAssignment,
                                                                                   IEmployee assignedBy)
    {
      if (actionItemAssignment == null)
      {
        Logger.Debug("ActionItemExecutionAssignment is null.");
        return ActionItemExecutionTasks.Null;
      }
      
      var actionItemAssignmentId = actionItemAssignment.Id;
      Logger.DebugFormat("ActionItemExecutionAssignment (ID={0}). Get documents.", actionItemAssignmentId);
      var document = actionItemAssignment.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      
      var task = this.CreateActionItemExecution(document, actionItemAssignment);
      if (task == null)
      {
        Logger.DebugFormat("ActionItemExecutionAssignment (ID={0}). Task is null.", actionItemAssignmentId);
        return ActionItemExecutionTasks.Null;
      }
      
      var taskId = task.Id;
      Logger.DebugFormat("ActionItemExecutionAssignment (ID={0}). Task (ID={1}) created.", actionItemAssignmentId, taskId);
      
      // Для подчиненных поручений заполнить признак автовыполнения из персональных настроек.
      if (actionItemAssignment != null)
      {
        var settings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(Employees.As(task.StartedBy));
        task.IsAutoExec = settings != null && (task.IsUnderControl != true || !Equals(task.Supervisor, task.StartedBy))
          ? settings.IsAutoExecLeadingActionItem
          : false;
      }
      
      Logger.DebugFormat("ActionItemExecutionAssignment (ID={0}). Set Assignee = null. Task (ID={1}).", actionItemAssignmentId, taskId);
      task.Assignee = null;
      
      if (actionItemAssignment.Deadline.HasValue &&
          (actionItemAssignment.Deadline.Value.HasTime() && actionItemAssignment.Deadline >= Calendar.Now ||
           !actionItemAssignment.Deadline.Value.HasTime() && actionItemAssignment.Deadline >= Calendar.Today))
      {
        Logger.DebugFormat("ActionItemExecutionAssignment (ID={0}). Set Deadline = {1}. Task (ID={2}).", actionItemAssignmentId, actionItemAssignment.Deadline, taskId);
        task.Deadline = actionItemAssignment.Deadline;
      }
      
      Logger.DebugFormat("ActionItemExecutionAssignment (ID={0}). Set AssignedBy = {1} (ID={2}). Task (ID={3}).",
                         actionItemAssignmentId, Users.Current, Users.Current.Id, taskId);
      task.AssignedBy = assignedBy;
      
      Logger.DebugFormat("ActionItemExecutionAssignment (ID={0}). End CreateActionItemExecutionFromExecution.", actionItemAssignmentId);
      
      return task;
    }
    
    /// <summary>
    /// Создать задачу на ознакомление с документом.
    /// </summary>
    /// <param name="document">Документ, который отправляется на ознакомление.</param>
    /// <returns>Задача на ознакомление с документом.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IAcquaintanceTask CreateAcquaintanceTask(IOfficialDocument document)
    {
      var newAcqTask = AcquaintanceTasks.Create();
      newAcqTask.DocumentGroup.OfficialDocuments.Add(document);
      return newAcqTask;
    }
    
    /// <summary>
    /// Создать задачу на ознакомление с документом.
    /// </summary>
    /// <param name="documentId">ИД документа, который отправляется на ознакомление.</param>
    /// <param name="performerIds">Список участников.</param>
    /// <param name="activeText">Текст задачи.</param>
    /// <param name="isElectronicAcquaintance">Ознакомление в электронном виде.</param>
    /// <param name="deadline">Срок задачи.</param>
    /// <returns>ИД задачи на ознакомление с документом.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual long CreateAcquaintanceTask(long documentId, List<long> performerIds, string activeText, bool isElectronicAcquaintance, DateTime deadline)
    {
      var document = OfficialDocuments.GetAll(d => d.Id == documentId).FirstOrDefault();
      if (document == null)
        throw AppliedCodeException.Create(string.Format("Create acquaintance task. Document with ID ({0}) not found.", documentId));

      var performers = new List<IEmployee>();
      if (performerIds.Any())
      {
        performers = Employees.GetAll(e => performerIds.Contains(e.Id)).ToList();
        if (!performers.Any())
          throw AppliedCodeException.Create(string.Format("Create acquaintance task. No employee found."));
      }

      var task = CreateAcquaintanceTask(document);
      if (performerIds.Any())
      {
        task.Performers.Clear();
        foreach (var performer in performers)
          task.Performers.AddNew().Performer = performer;
      }
      task.ActiveText = activeText;
      task.IsElectronicAcquaintance = isElectronicAcquaintance;
      task.Deadline = deadline;
      task.Save();

      return task.Id;
    }
    
    /// <summary>
    /// Создать задачу на ознакомление с документом.
    /// </summary>
    /// <param name="document">Документ, который отправляется на ознакомление.</param>
    /// <param name="parentAssignment">Задание, из которого создается подзадача.</param>
    /// <returns>Задача на ознакомление по документу.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IAcquaintanceTask CreateAcquaintanceTaskAsSubTask(IOfficialDocument document, IAssignment parentAssignment)
    {
      var newAcqTask = AcquaintanceTasks.CreateAsSubtask(parentAssignment);
      RecordManagement.PublicFunctions.Module.SynchronizeAttachmentsToAcquaintance(parentAssignment.Task, newAcqTask);
      return newAcqTask;
    }

    #region AbortSubtasksAndSendNotices

    /// <summary>
    /// Рекурсивно завершить все подзадачи, выслать уведомления.
    /// </summary>
    /// <param name="actionItem">Поручение, подзадачи которого следует завершить.</param>
    [Public, Remote]
    public static void AbortSubtasksAndSendNotices(IActionItemExecutionTask actionItem)
    {
      AbortSubtasksAndSendNotices(actionItem, null, string.Empty);
    }

    /// <summary>
    /// Рекурсивно завершить все подзадачи, выслать уведомления.
    /// </summary>
    /// <param name="actionItem">Поручение, подзадачи которого следует завершить.</param>
    /// <param name="performer">Исполнитель, которого не нужно уведомлять.</param>
    /// <param name="abortingReason">Причина прекращения.</param>
    public static void AbortSubtasksAndSendNotices(IActionItemExecutionTask actionItem, IUser performer = null, string abortingReason = "")
    {
      // Собрать всех пользователей, которым нужно выслать уведомления.
      var recipients = new List<Sungero.CoreEntities.IUser>();
      
      // Уведомить актуальных контролера и исполнителя текущего поручения.
      recipients.AddRange(Functions.ActionItemExecutionTask.GetActualSupervisorAndAssignee(actionItem));
      
      // Получить дерево всех подзадач текущего поручения.
      var subTasks = Functions.Module.GetSubtasksForTaskRecursive(actionItem);
      foreach (var subTask in subTasks.Where(t => ActionItemExecutionTasks.Is(t) || DeadlineExtensionTasks.Is(t) ||
                                             Docflow.DeadlineExtensionTasks.Is(t) || StatusReportRequestTasks.Is(t)))
      {
        var actionItemExecutionSubTask = ActionItemExecutionTasks.As(subTask);
        if (actionItemExecutionSubTask != null)
          actionItemExecutionSubTask.AbortingReason = string.IsNullOrEmpty(abortingReason) ? actionItemExecutionSubTask.AbortingReason : abortingReason;

        subTask.Abort();
        
        // Для подзадач-поручений: уведомить актуальных исполнителя и контролера.
        if (actionItemExecutionSubTask != null)
          recipients.AddRange(Functions.ActionItemExecutionTask.GetActualSupervisorAndAssignee(actionItemExecutionSubTask));
        // Для остальных подзадач: уведомить исполнителей всех заданий/уведомлений в подзадаче.
        else
          recipients.AddRange(AssignmentBases.GetAll(a => Equals(a.Task, subTask)).Select(u => u.Performer).ToList());
      }

      // Исключить дубли, текущего пользователя и пользователя из параметра.
      recipients = recipients.Distinct().ToList();
      if (performer != null)
        recipients.Remove(performer);
      else
        recipients.Remove(Users.Current);

      // Выслать уведомление.
      if (recipients.Any())
      {
        var threadSubject = ActionItemExecutionTasks.Resources.NoticeSubjectWithoutDoc;
        var noticesSubject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(actionItem, Sungero.RecordManagement.Resources.TwoSpotTemplateFormat(threadSubject));
        Docflow.PublicFunctions.Module.Remote.SendNoticesAsSubtask(noticesSubject, recipients, actionItem, actionItem.AbortingReason, performer, threadSubject);
      }
    }
    
    #endregion

    /// <summary>
    /// Рекурсивно получить все незавершенные подзадачи.
    /// </summary>
    /// <param name="task">Задача, для которой необходимо получить незавершенные подзадачи.</param>
    /// <returns>Список незавершенных подзадач.</returns>
    public static List<ITask> GetSubtasksForTaskRecursive(ITask task)
    {
      var subTasksByParentTask = Functions.Module.GetSubtasksForTaskByParentTask(task, null).ToList();
      var subTasksByParentAssignment = Functions.Module.GetSubtasksForTaskByParentAssignment(task, null).ToList();
      var result = new List<ITask>();
      result.AddRange(subTasksByParentTask);
      result.AddRange(subTasksByParentAssignment);
      foreach (var subTask in subTasksByParentTask)
        result.AddRange(GetSubtasksForTaskRecursive(subTask));
      foreach (var subTask in subTasksByParentAssignment)
        result.AddRange(GetSubtasksForTaskRecursive(subTask));

      return result;
    }
    
    /// <summary>
    /// Получить все подзадачи, привязанные через задачу.
    /// </summary>
    /// <param name="task">Задача, для которой необходимо получить подзадачи.</param>
    /// <param name="status">Статус подзадач, которые необходимо получить.</param>
    /// <returns>Список подзадач.</returns>
    public static IQueryable<ITask> GetSubtasksForTaskByParentTask(ITask task, Enumeration? status)
    {
      if (status == null)
        status = Workflow.Task.Status.InProcess;
      return Tasks.GetAll()
        .Where(x => x.Status == status)
        .Where(x => x.ParentTask != null && Equals(task, x.ParentTask));
    }
    
    /// <summary>
    /// Получить все подзадачи, привязанные через задания.
    /// </summary>
    /// <param name="task">Задача, для которой необходимо получить подзадачи.</param>
    /// <param name="status">Статус подзадач, которые необходимо получить.</param>
    /// <returns>Список подзадач.</returns>
    public static IQueryable<ITask> GetSubtasksForTaskByParentAssignment(ITask task, Enumeration? status)
    {
      if (status == null)
        status = Workflow.Task.Status.InProcess;
      return Tasks.GetAll()
        .Where(x => x.Status == status)
        .Where(x => x.ParentAssignment != null && Equals(task, x.ParentAssignment.Task));
    }
    
    /// <summary>
    /// Получить ведущую задачу.
    /// </summary>
    /// <param name="task">Задача, для которой нужно получить ведущую.</param>
    /// <returns>Ведущая задача.</returns>
    public static ITask GetParentTask(ITask task)
    {
      if (task == null)
        return null;
      return task.ParentAssignment != null ? task.ParentAssignment.Task : task.ParentTask;
    }
    
    /// <summary>
    /// Создать и выполнить асинхронное событие выполнения ведущего задания на исполнение поручения.
    /// </summary>
    /// <param name="actionItemId">ИД поручения.</param>
    /// <param name="parentAssignmentId">ИД ведущего задания на исполнение поручения.</param>
    /// <param name="parentTaskStartId">Количество стартов задачи, в рамках которой создано ведущее задание.</param>
    /// <param name="needAbortChildActionItems">Признак необходимости прекращения подчиненных поручений.</param>
    [Remote]
    public virtual void CompleteParentActionItemExecutionAssignmentAsync(long actionItemId, long parentAssignmentId, long? parentTaskStartId, bool needAbortChildActionItems)
    {
      Logger.DebugFormat("CompleteParentActionItemExecutionAssignmentAsync({0}, {1}, {2}): TaskId {0}, ParentAssignmentId {1}, ParentTaskStartId {2}.",
                         actionItemId, parentAssignmentId, parentTaskStartId);
      var completeParentActionItemHandler = RecordManagement.AsyncHandlers.CompleteParentActionItemExecutionAssignment.Create();
      completeParentActionItemHandler.actionItemId = actionItemId;
      completeParentActionItemHandler.parentAssignmentId = parentAssignmentId;
      completeParentActionItemHandler.parentTaskStartId = parentTaskStartId ?? 0;
      completeParentActionItemHandler.needAbortChildActionItems = needAbortChildActionItems;
      completeParentActionItemHandler.ExecuteAsync();
    }

    /// <summary>
    /// Создать и выполнить асинхронное событие выполнения ведущего задания на исполнение поручения.
    /// </summary>
    /// <param name="actionItemId">ИД поручения.</param>
    /// <param name="parentAssignmentId">ИД ведущего задания на исполнение поручения.</param>
    /// <param name="parentTaskStartId">Количество стартов задачи, в рамках которой создано ведущее задание.</param>
    [Remote, Obsolete("Метод не используется с 31.05.2024 и версии 4.11. Используйте метод CompleteParentActionItemExecutionAssignmentAsync(long actionItemId, long parentAssignmentId, long? parentTaskStartId, bool needAbortChildActionItems).")]
    public virtual void CompleteParentActionItemExecutionAssignmentAsync(long actionItemId, long parentAssignmentId, long? parentTaskStartId)
    {
      Logger.DebugFormat("CompleteParentActionItemExecutionAssignmentAsync({0}, {1}, {2}): TaskId {0}, ParentAssignmentId {1}, ParentTaskStartId {2}.",
                         actionItemId, parentAssignmentId, parentTaskStartId);
      var completeParentActionItemHandler = RecordManagement.AsyncHandlers.CompleteParentActionItemExecutionAssignment.Create();
      completeParentActionItemHandler.actionItemId = actionItemId;
      completeParentActionItemHandler.parentAssignmentId = parentAssignmentId;
      completeParentActionItemHandler.parentTaskStartId = parentTaskStartId ?? 0;
      completeParentActionItemHandler.ExecuteAsync();
    }
    
    /// <summary>
    /// Создать и выполнить асинхронное событие изменения составного поручения.
    /// </summary>
    /// <param name="changes">Изменения.</param>
    /// <param name="actionItemTaskId">Ид задачи.</param>
    /// <param name="onEditGuid">Guid поручения.</param>
    [Public, Remote]
    public virtual void ChangeCompoundActionItemAsync(RecordManagement.Structures.ActionItemExecutionTask.IActionItemChanges changes, long actionItemTaskId, string onEditGuid)
    {
      Logger.DebugFormat("ChangeCompoundActionItemAsync({0}): actionItemTaskId {0}", actionItemTaskId);
      var changeCompoundActionItemHandler = RecordManagement.AsyncHandlers.ChangeCompoundActionItem.Create();
      changeCompoundActionItemHandler.ActionItemTaskId = actionItemTaskId;
      changeCompoundActionItemHandler.OldSupervisor = changes.OldSupervisor?.Id ?? -1;
      changeCompoundActionItemHandler.NewSupervisor = changes.NewSupervisor?.Id ?? -1;
      changeCompoundActionItemHandler.OldAssignee = changes.OldAssignee?.Id ?? -1;
      changeCompoundActionItemHandler.NewAssignee = changes.NewAssignee?.Id ?? -1;
      changeCompoundActionItemHandler.OldDeadline = changes.OldDeadline ?? DateTime.MinValue;
      changeCompoundActionItemHandler.NewDeadline = changes.NewDeadline ?? DateTime.MinValue;
      changeCompoundActionItemHandler.OldCoAssignees = string.Join(",", changes.OldCoAssignees.Select(x => x.Id).ToList());
      changeCompoundActionItemHandler.NewCoAssignees = string.Join(",", changes.NewCoAssignees.Select(x => x.Id).ToList());
      changeCompoundActionItemHandler.CoAssigneesOldDeadline = changes.CoAssigneesOldDeadline ?? DateTime.MinValue;
      changeCompoundActionItemHandler.CoAssigneesNewDeadline = changes.CoAssigneesNewDeadline ?? DateTime.MinValue;
      changeCompoundActionItemHandler.EditingReason = changes.EditingReason;
      changeCompoundActionItemHandler.AdditionalInfo = changes.AdditionalInfo;
      changeCompoundActionItemHandler.TaskIds = string.Join(",", changes.TaskIds);
      changeCompoundActionItemHandler.ActionItemPartsText = changes.ActionItemPartsText;
      changeCompoundActionItemHandler.OnEditGuid = onEditGuid;
      changeCompoundActionItemHandler.InitiatorOfChange = changes.InitiatorOfChange.Id;
      changeCompoundActionItemHandler.ChangeContext = changes.ChangeContext;
      
      changeCompoundActionItemHandler.ExecuteAsync();
    }
    
    #endregion

    #region Работа с документами

    /// <summary>
    /// Получить виды документов по документопотоку.
    /// </summary>
    /// <param name="direction">Документопоток вида документа.</param>
    /// <returns>Виды документов.</returns>
    [Remote(IsPure = true)]
    public static List<IDocumentKind> GetFilteredDocumentKinds(Enumeration direction)
    {
      if (direction == Docflow.DocumentKind.DocumentFlow.Incoming)
        return DocumentKinds.GetAll(d => d.DocumentFlow.Value == Docflow.DocumentKind.DocumentFlow.Incoming).ToList();
      else if (direction == Docflow.DocumentKind.DocumentFlow.Outgoing)
        return DocumentKinds.GetAll(d => d.DocumentFlow.Value == Docflow.DocumentKind.DocumentFlow.Outgoing).ToList();
      else if (direction == Docflow.DocumentKind.DocumentFlow.Inner)
        return DocumentKinds.GetAll(d => d.DocumentFlow.Value == Docflow.DocumentKind.DocumentFlow.Inner).ToList();
      else if (direction == Docflow.DocumentKind.DocumentFlow.Contracts)
        return DocumentKinds.GetAll(d => d.DocumentFlow.Value == Docflow.DocumentKind.DocumentFlow.Contracts).ToList();
      else
        return null;
    }

    /// <summary>
    /// Получить входящее письмо по ИД.
    /// </summary>
    /// <param name="letterId">ИД письма.</param>
    /// <returns>Если письмо не существует возвращает null.</returns>
    [Remote(IsPure = true)]
    public static IOutgoingDocumentBase GetIncomingLetterById(long letterId)
    {
      return Sungero.Docflow.OutgoingDocumentBases.GetAll().FirstOrDefault(l => l.Id == letterId);
    }
    
    /// <summary>
    /// Провалидировать подписи документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="onlyLastSignature">Проверить только последнюю подпись.</param>
    /// <returns>Если подписи валидны, возвращает пустой список, иначе список ошибок.</returns>
    [Public]
    public static List<string> GetDocumentSignatureValidationErrors(IEntity document, bool onlyLastSignature)
    {
      var validationMessages = new List<string>();
      if (document == null)
        return validationMessages;
      
      var signatures = new List<ISignature>();
      if (onlyLastSignature)
      {
        signatures = Signatures
          .Get(document, q => q.Where(s => s.SignatureType == SignatureType.Approval && s.IsExternal != true).OrderByDescending(s => s.Id).Take(1))
          .ToList();
      }
      else
      {
        signatures = Signatures
          .Get(document, q => q.Where(s => s.SignatureType == SignatureType.Approval && s.IsExternal != true))
          .ToList();
      }
      
      foreach (var signature in signatures)
      {
        var error = Functions.Module.GetSignatureValidationErrors(signature);
        if (!string.IsNullOrWhiteSpace(error))
          validationMessages.Add(error);
      }
      
      return validationMessages;
    }
    
    /// <summary>
    /// Провалидировать подпись.
    /// </summary>
    /// <param name="signature">Подпись.</param>
    /// <returns>Если подпись валидна, возвращает пустую строку, иначе строку с ошибкой.</returns>
    [Public]
    public static string GetSignatureValidationErrors(Sungero.Domain.Shared.ISignature signature)
    {
      if (signature == null)
        return string.Empty;

      var separator = ". ";
      var signatureErrors = Docflow.PublicFunctions.Module.GetSignatureValidationErrorsAsString(signature, separator);
      if (string.IsNullOrWhiteSpace(signatureErrors))
        return string.Empty;
      
      var signatory = string.IsNullOrWhiteSpace(signature.SubstitutedUserFullName)
        ? signature.SignatoryFullName
        : RecordManagement.Resources.SignatorySubstituteFormat(signature.SignatoryFullName, signature.SubstitutedUserFullName);
      
      return RecordManagement.Resources.SignatureValidationMessageFormat(signatory,
                                                                         signature.SigningDate,
                                                                         signatureErrors);
    }
    
    /// <summary>
    /// Установить состояние исполнения документа по задаче.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="document">Документ.</param>
    /// <param name="state">Состояние исполнения.</param>
    /// <remarks>Применяется к задачам на рассмотрение документа и исполнения поручений по документу.
    /// При установке статуса принимаются в расчет другие задачи на рассмотрение или исполнение поручения по документу.
    /// </remarks>
    public virtual void SetDocumentExecutionState(ITask task, IOfficialDocument document, Enumeration? state)
    {
      if (task == null || document == null || !document.AccessRights.CanUpdate())
        return;
      
      Enumeration? executionState = state;

      Logger.DebugFormat("RM SetExecutionState(task:{0}, document:{1}, state:{2})", task.Id, document.Id, state);
      var states = Functions.Module.GetExecutionStateVariants(task, document);
      states = states.Where(t => t != null).Distinct().ToList();
      if (states.Any())
      {
        if (state != null && !states.Contains(state))
          states.Add(state);
        Logger.DebugFormat("RM SetExecutionState(task:{0}, document:{1}, state:{2}). ExecutionState variants: {3}",
                           task.Id, document.Id, state, string.Join(", ", states));
        var priorities = PublicFunctions.Module.GetExecutionStatePriorities();
        priorities = priorities.Where(x => states.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
        executionState = priorities.OrderByDescending(p => p.Value).FirstOrDefault().Key;
      }
      
      Sungero.Docflow.PublicFunctions.OfficialDocument.SetExecutionState(document, executionState);
      Logger.DebugFormat("RM SetExecutionState(task:{0}, document:{1}, state:{2}). ExecutionState: {2}",
                         task.Id, document.Id, executionState);
    }
    
    /// <summary>
    /// Установить состояние контроля исполнения документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    public virtual void SetDocumentControlExecutionState(IOfficialDocument document)
    {
      if (document == null || !document.AccessRights.CanUpdate())
        return;
      
      var controlExecutionState = Sungero.Docflow.PublicFunctions.OfficialDocument.GetControlExecutionState(document);
      Sungero.Docflow.PublicFunctions.OfficialDocument.SetControlExecutionState(document, controlExecutionState);
      Logger.DebugFormat("RM SetControlExecutionState(document:{0}). ControlExecutionState: {1}",
                         document.Id, controlExecutionState);
    }
    
    /// <summary>
    /// Получить возможные варианты статуса исполнения документа.
    /// </summary>
    /// <param name="task">Задача, в рамках которой меняется статус исполнения документа.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Список возможных статусов исполнения документа.</returns>
    public virtual List<Enumeration?> GetExecutionStateVariants(ITask task, IOfficialDocument document)
    {
      /* Статус исполнения документа зависит от:
       * - других рассмотрений, которые выполняются по документу
       * - других рассмотрений, которые были выполнены в рамках многоадресного рассмотрения
       * - поручений по документу, которые выполняются или уже были выполнены
       */
      var states = new List<Enumeration?>();

      var otherReviewTasks = Sungero.Docflow.PublicFunctions.OfficialDocument.GetDocumentReviewTasks(document)
        .Where(t => t.Id != task.Id)
        .ToList();
      states.AddRange(otherReviewTasks.Select(x => Functions.DocumentReviewTask.GetDocumentExecutionState(x)));
      
      var parentTask = Functions.Module.GetParentTask(task);
      /* Поручение может быть создано в рамках рассмотрения из задачи-контейнера,
       * в этом случае необходимо учесть параллельные ветки задачи-контейнера.
       * Если поручение создано в рамках простого рассмотрения, то это рассмотрение будет учтено в otherReviewTasks.
       */
      if (ActionItemExecutionTasks.Is(task) && parentTask != null && DocumentReviewTasks.Is(parentTask))
        parentTask = Functions.Module.GetParentTask(parentTask);
      if (parentTask != null && DocumentReviewTasks.Is(parentTask))
      {
        var reviewSubTasks = Functions.DocumentReviewTask.GetCompletedDocumentReviewSubTasks(DocumentReviewTasks.As(parentTask))
          .Where(t => t.Id != task.Id)
          .Where(t => t.StartId != null && t.StartId == parentTask.StartId)
          .ToList();
        // Исключить рассмотрение, в рамках которого создано поручение.
        if (ActionItemExecutionTasks.Is(task))
        {
          parentTask = Functions.Module.GetParentTask(task);
          reviewSubTasks = reviewSubTasks.Where(x => x.Id != parentTask.Id).ToList();
        }
        states.AddRange(reviewSubTasks.Select(x => Functions.DocumentReviewTask.GetDocumentExecutionState(x)));
      }
      
      // Добавить статус для документа по поручениям первого уровня.
      var actionItems = Sungero.Docflow.PublicFunctions.OfficialDocument.GetFirstLevelActionItems(document).ToList();
      states.AddRange(actionItems.Select(x => Functions.ActionItemExecutionTask.GetDocumentExecutionState(x)));

      return states;
    }
    
    #endregion

    #region Работа с SQL

    /// <summary>
    /// Выполнить SQL-запрос.
    /// </summary>
    /// <param name="format">Формат запроса.</param>
    /// <param name="args">Аргументы запроса, подставляемые в формат.</param>
    public static void ExecuteSQLCommandFormat(string format, params object[] args)
    {
      // Функция дублируется из Docflow, т.к. нельзя исп. params в public-функциях.
      var command = string.Format(format, args);
      Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
    }

    #endregion

    #region Подпапки входящих
    
    /// <summary>
    /// Применить к списку заданий стандартные фильтры: по длинному периоду (180 дней) и по статусу "Завершено".
    /// </summary>
    /// <param name="query">Список заданий.</param>
    /// <returns>Отфильтрованный список заданий.</returns>
    [Public]
    public IQueryable<Sungero.Workflow.IAssignmentBase> ApplyCommonSubfolderFilters(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      return this.ApplyCommonSubfolderFilters(query, false, false, false, false, true);
    }
    
    /// <summary>
    /// Применить к списку заданий фильтры по статусу и периоду.
    /// </summary>
    /// <param name="query">Список заданий.</param>
    /// <param name="inProcess">Признак показа заданий "В работе".</param>
    /// <param name="shortPeriod">Фильтр по короткому периоду (30 дней).</param>
    /// <param name="middlePeriod">Фильтр по среднему периоду (90 дней).</param>
    /// <param name="longPeriod">Фильтр по длинному периоду (180 дней).</param>
    /// <param name="longPeriodToCompleted">Фильтр по длинному периоду (180 дней) для завершённых заданий.</param>
    /// <returns>Отфильтрованный список заданий.</returns>
    [Public]
    public IQueryable<Sungero.Workflow.IAssignmentBase> ApplyCommonSubfolderFilters(IQueryable<Sungero.Workflow.IAssignmentBase> query,
                                                                                    bool inProcess,
                                                                                    bool shortPeriod,
                                                                                    bool middlePeriod,
                                                                                    bool longPeriod,
                                                                                    bool longPeriodToCompleted)
    {
      // Фильтр по статусу.
      if (inProcess)
        return query.Where(a => a.Status == Workflow.AssignmentBase.Status.InProcess);
      
      // Фильтр по периоду.
      DateTime? periodBegin = null;
      if (shortPeriod)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-30));
      else if (middlePeriod)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-90));
      else if (longPeriod || longPeriodToCompleted)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-180));
      
      if (shortPeriod || middlePeriod || longPeriod)
        query = query.Where(a => a.Created >= periodBegin);
      else if (longPeriodToCompleted)
        query = query.Where(a => a.Created >= periodBegin || a.Status == Workflow.AssignmentBase.Status.InProcess);

      return query;
    }
    
    /// <summary>
    /// Применить к списку задач стандартные фильтры: по длинному периоду (180 дней) и по статусу "Завершено".
    /// </summary>
    /// <param name="query">Список задач.</param>
    /// <returns>Отфильтрованный список задач.</returns>
    [Public]
    public IQueryable<Sungero.Workflow.ITask> ApplyCommonSubfolderFilters(IQueryable<Sungero.Workflow.ITask> query)
    {
      return this.ApplyCommonSubfolderFilters(query, false, false, false, false, true);
    }
    
    /// <summary>
    /// Применить к списку задач фильтры по статусу и периоду.
    /// </summary>
    /// <param name="query">Список задач.</param>
    /// <param name="inProcess">Признак показа задач "В работе".</param>
    /// <param name="shortPeriod">Фильтр по короткому периоду (30 дней).</param>
    /// <param name="middlePeriod">Фильтр по среднему периоду (90 дней).</param>
    /// <param name="longPeriod">Фильтр по длинному периоду (180 дней).</param>
    /// <param name="longPeriodToCompleted">Фильтр по длинному периоду (180 дней) для завершённых задач.</param>
    /// <returns>Отфильтрованный список задач.</returns>
    [Public]
    public IQueryable<Sungero.Workflow.ITask> ApplyCommonSubfolderFilters(IQueryable<Sungero.Workflow.ITask> query,
                                                                          bool inProcess,
                                                                          bool shortPeriod,
                                                                          bool middlePeriod,
                                                                          bool longPeriod,
                                                                          bool longPeriodToCompleted)
    {
      // Фильтр по статусу.
      if (inProcess)
        return query.Where(t => t.Status == Workflow.Task.Status.InProcess || t.Status == Workflow.Task.Status.Draft);

      // Фильтр по периоду.
      DateTime? periodBegin = null;
      if (shortPeriod)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-30));
      else if (middlePeriod)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.Today.AddDays(-90));
      else if (longPeriod || longPeriodToCompleted)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.Today.AddDays(-180));
      
      if (shortPeriod || middlePeriod || longPeriod)
        query = query.Where(t => t.Created >= periodBegin);
      else if (longPeriodToCompleted)
        query = query.Where(t => t.Created >= periodBegin || t.Status == Workflow.AssignmentBase.Status.InProcess);
      
      return query;
    }

    #endregion
    
    #region Старт поручений по протоколу совещания
    
    /// <summary>
    /// Выдать права доступа на документы при старте поручений по протоколу совещания.
    /// </summary>
    /// <param name="tasks">Поручения по протоколу совещания.</param>
    /// <param name="documents">Документы.</param>
    public virtual void GrantAccessRightsToDocumentsWhenStartingActionItems(List<IActionItemExecutionTask> tasks, List<Sungero.Domain.Shared.IEntity> documents)
    {
      var firstTask = tasks.First();
      
      foreach (var task in tasks)
      {
        // Если в поручении есть соисполнители, исполнитель будет контролером в поручении для соисполнителя.
        if (task.CoAssignees.Any())
        {
          Functions.ActionItemExecutionTask.GrantAccessRightsToSupervisor(task, documents, task.Assignee);
          Functions.ActionItemExecutionTask.GrantAccessRightsToCoAssignee(task, documents);
        }
        else
          Functions.ActionItemExecutionTask.GrantAccessRightsToAssignee(task, documents);
      }
      
      // "Контролер", "Выдал", "Автор" задачи будут для всех поручений одинаковы.
      Functions.ActionItemExecutionTask.GrantAccessRightsToAttachments(firstTask, documents, false);
    }
    
    /// <summary>
    /// Установить статусы документа при старте поручений по протоколу совещания.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="document">Документ.</param>
    public virtual void SetDocumentStatesWhenStartingActionItems(IActionItemExecutionTask task, IOfficialDocument document)
    {
      Functions.Module.SetDocumentExecutionState(task, document, Sungero.Docflow.OfficialDocument.ExecutionState.OnExecution);
      Sungero.Docflow.PublicFunctions.OfficialDocument.SetControlExecutionState(document, Sungero.Docflow.OfficialDocument.ControlExecutionState.OnControl);
    }
    
    #endregion

    #region Фильтрация списков

    #region Поручения
    
    /// <summary>
    /// Отфильтровать поручения по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Поручения для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные поручения.</returns>
    /// <remarks>Условия которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<IActionItemExecutionTask> ActionItemExecutionTasksApplyStrongFilter(IQueryable<IActionItemExecutionTask> query, IActionItemExecutionTaskFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр по автору.
      if (filter.Author != null)
        query = query.Where(x => Equals(x.AssignedBy, filter.Author));
      
      // Фильтр по исполнителю.
      if (filter.Assignee != null)
        query = query.Where(x => Equals(x.Assignee, filter.Assignee) || x.CoAssignees.Any(y => Equals(y.Assignee, filter.Assignee)));
      
      // Фильтр по контролеру.
      if (filter.Supervisor != null)
        query = query.Where(x => Equals(x.Supervisor, filter.Supervisor));
      
      // Фильтр по плановому сроку.
      if (filter.LastMonth)
      {
        var now = Calendar.Now;
        var today = Calendar.UserToday;
        var tomorrow = today.AddDays(1);
        var lastMonthBeginDate = today.AddDays(-30);
        var lastMonthBeginDateNextDay = lastMonthBeginDate.AddDays(1);
        var lastMonthBeginDateWithTime = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(lastMonthBeginDate);

        query = query.Where(x => ((x.Deadline >= lastMonthBeginDateWithTime && x.Deadline < now) ||
                                  x.Deadline == lastMonthBeginDate || x.Deadline == lastMonthBeginDateNextDay || x.Deadline == today) &&
                            x.Deadline != tomorrow);
      }
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать поручения по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">Поручения для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные поручения.</returns>
    /// <remarks>Условия которые используют индексы но не максимально оптимально.</remarks>
    public virtual IQueryable<IActionItemExecutionTask> ActionItemExecutionTasksApplyOrdinaryFilter(IQueryable<IActionItemExecutionTask> query, IActionItemExecutionTaskFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Не показывать не стартованные поручения.
      query = query.Where(x => x.Status != Sungero.Workflow.Task.Status.Draft);
      
      // Фильтр по статусу.
      var statuses = new List<Enumeration>();
      if (filter.OnExecution)
      {
        statuses.Add(RecordManagement.ActionItemExecutionTask.ExecutionState.OnExecution);
        statuses.Add(RecordManagement.ActionItemExecutionTask.ExecutionState.OnControl);
        statuses.Add(RecordManagement.ActionItemExecutionTask.ExecutionState.OnRework);
      }
      
      if (filter.Executed)
      {
        statuses.Add(RecordManagement.ActionItemExecutionTask.ExecutionState.Executed);
        statuses.Add(RecordManagement.ActionItemExecutionTask.ExecutionState.Aborted);
      }
      
      if (statuses.Any())
        query = query.Where(x => x.ExecutionState != null && statuses.Contains(x.ExecutionState.Value));
      
      // Фильтр по соблюдению сроков.
      var now = Calendar.Now;
      var today = Calendar.UserToday;
      var tomorrow = today.AddDays(1);
      if (filter.Overdue)
        query = query.Where(x => x.Status != Workflow.Task.Status.Aborted && x.Deadline != null &&
                            ((x.ActualDate == null && x.Deadline < now && x.Deadline != today && x.Deadline != tomorrow) ||
                             (x.ActualDate != null && x.ActualDate > x.Deadline)));
      
      // Фильтр по плановому сроку.
      if (filter.ManualPeriod)
      {
        if (filter.DateRangeFrom != null)
        {
          var dateRangeFromNextDay = filter.DateRangeFrom.Value.AddDays(1);
          var dateFromWithTime = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(filter.DateRangeFrom.Value);
          query = query.Where(x => x.Deadline == null ||
                              x.Deadline >= dateFromWithTime ||
                              x.Deadline == filter.DateRangeFrom.Value ||
                              x.Deadline == dateRangeFromNextDay);
        }
        
        if (filter.DateRangeTo != null)
        {
          var dateRangeNextDay = filter.DateRangeTo.Value.AddDays(1);
          var dateTo = filter.DateRangeTo.Value.EndOfDay().FromUserTime();
          query = query.Where(x => x.Deadline != null &&
                              ((x.Deadline < dateTo || x.Deadline == filter.DateRangeTo.Value) &&
                               x.Deadline != dateRangeNextDay));
        }
      }
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать поручения по неоптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Поручения для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные поручения.</returns>
    /// <remarks>Условия которые могут выполняться долго (например те которые не могут использовать индексы).</remarks>
    public virtual IQueryable<IActionItemExecutionTask> ActionItemExecutionTasksApplyWeakFilter(IQueryable<IActionItemExecutionTask> query, IActionItemExecutionTaskFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Не показывать составные поручения (только подзадачи).
      query = query.Where(x => x.IsCompoundActionItem == false);
      
      return query;
    }
    
    /// <summary>
    /// Определить нужно ли использовать предварительную фильтрацию для поручений.
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True если нужно использовать предварительную  фильтрацию.</returns>
    public virtual bool UsePrefilterActionItemExecutionTasks(IActionItemExecutionTaskFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.Author != null ||
         filter.Assignee != null ||
         filter.Supervisor != null ||
         filter.LastMonth);
      return hasStrongFilter;
    }
    
    #endregion
    
    #region Приказы и распоряжения
    
    /// <summary>
    /// Определить, нужно ли использовать предварительную фильтрацию для приказов и распоряжений.
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True, если нужно использовать предварительную фильтрацию.</returns>
    public virtual bool UsePrefilterOrdersCompanyDirectives(FolderFilterState.IOrdersCompanyDirectivesFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.DocumentRegister != null ||
         filter.Department != null ||
         filter.LastWeek);
      return hasStrongFilter;
    }
    
    /// <summary>
    /// Отфильтровать приказы и распоряжения по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Приказы и распоряжения для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные приказы и распоряжения.</returns>
    /// <remarks>Условия, которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<Sungero.RecordManagement.IOrderBase> OrdersCompanyDirectivesApplyStrongFilter(IQueryable<Sungero.RecordManagement.IOrderBase> query, FolderFilterState.IOrdersCompanyDirectivesFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "Журнал регистрации".
      if (filter.DocumentRegister != null)
        query = query.Where(p => Equals(p.DocumentRegister, filter.DocumentRegister));
      
      // Фильтр "Подразделение".
      if (filter.Department != null)
        query = query.Where(p => Equals(p.Department, filter.Department));
      
      // Фильтр "Интервал времени".
      if (filter.LastWeek)
        query = this.OrdersCompanyDirectivesApplyFilterByDate(query, filter);
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать приказы и распоряжения по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">Приказы и распоряжения для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные приказы и распоряжения.</returns>
    /// <remarks>Условия, которые используют индексы, но не максимально оптимально.</remarks>
    public virtual IQueryable<Sungero.RecordManagement.IOrderBase> OrdersCompanyDirectivesApplyOrdinaryFilter(IQueryable<Sungero.RecordManagement.IOrderBase> query, FolderFilterState.IOrdersCompanyDirectivesFilterState filter)
    {
      if (filter == null || query == null)
        return query;

      // Фильтр "Вид документа".
      if (filter.DocumentKind != null)
        query = query.Where(p => Equals(p.DocumentKind, filter.DocumentKind));
      
      // Статус.
      if ((filter.Registered || filter.NotRegistered || filter.Reserved) && !(filter.Registered && filter.NotRegistered && filter.Reserved))
      {
        query = query.Where(p => filter.Registered && p.RegistrationState == Sungero.Docflow.OfficialDocument.RegistrationState.Registered ||
                            filter.NotRegistered && p.RegistrationState == Sungero.Docflow.OfficialDocument.RegistrationState.NotRegistered ||
                            filter.Reserved && p.RegistrationState == Sungero.Docflow.OfficialDocument.RegistrationState.Reserved);
      }
      
      // Фильтр "Наша организация".
      if (filter.BusinessUnit != null)
        query = query.Where(p => Equals(p.BusinessUnit, filter.BusinessUnit));
      
      // Фильтр "Интервал времени".
      if (filter.LastMonth || filter.Last90Days || filter.ManualPeriod)
        query = this.OrdersCompanyDirectivesApplyFilterByDate(query, filter);
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать приказы и распоряжения по неоптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Приказы и распоряжения для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные приказы и распоряжения.</returns>
    /// <remarks>Условия, которые могут выполняться долго (например, те которые не могут использовать индексы).</remarks>
    public virtual IQueryable<Sungero.RecordManagement.IOrderBase> OrdersCompanyDirectivesApplyWeakFilter(IQueryable<Sungero.RecordManagement.IOrderBase> query, FolderFilterState.IOrdersCompanyDirectivesFilterState filter)
    {
      return query;
    }
    
    /// <summary>
    /// Отфильтровать приказы и распоряжения по дате документа.
    /// </summary>
    /// <param name="query">Приказы и распоряжения для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные приказы и распоряжения.</returns>
    public virtual IQueryable<Sungero.RecordManagement.IOrderBase> OrdersCompanyDirectivesApplyFilterByDate(IQueryable<Sungero.RecordManagement.IOrderBase> query, FolderFilterState.IOrdersCompanyDirectivesFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      var periodBegin = Calendar.UserToday.AddDays(-7);
      var periodEnd = Calendar.UserToday.EndOfDay();
      
      if (filter.LastWeek)
        periodBegin = Calendar.UserToday.AddDays(-7);
      
      if (filter.LastMonth)
        periodBegin = Calendar.UserToday.AddDays(-30);
      
      if (filter.Last90Days)
        periodBegin = Calendar.UserToday.AddDays(-90);
      
      if (filter.ManualPeriod)
      {
        periodBegin = filter.DateRangeFrom ?? Calendar.SqlMinValue;
        periodEnd = filter.DateRangeTo ?? Calendar.SqlMaxValue;
      }
      
      query = Docflow.PublicFunctions.Module.OfficialDocumentsApplyFilterByDate(query, periodBegin, periodEnd)
        .Cast<IOrderBase>();
      
      return query;
    }
    
    #endregion
    
    #endregion
    
    #region Работа с вложениями
    
    /// <summary>
    /// Получить список текущих приложений по документу для задачи на исполнение поручений.
    /// </summary>
    /// <param name="documentGroup">Группа Основной документ.</param>
    /// <param name="removedAddendaIds">ИД удаленных из группы приложений (используется только в старых задачах, которые были стартованы до нового механизма синхронизации вложений).</param>
    /// <returns>Список приложений.</returns>
    [Public]
    public virtual List<IElectronicDocument> GetActualAddendaForActionItemExecutionTask(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup documentGroup, List<long> removedAddendaIds)
    {
      return Docflow.PublicFunctions.Module.GetActualAddenda(documentGroup, removedAddendaIds);
    }
    
    /// <summary>
    /// Получить список текущих приложений по документу для задачи на рассмотрение документа.
    /// </summary>
    /// <param name="documentGroup">Группа Основной документ.</param>
    /// <param name="removedAddendaIds">ИД удаленных из группы приложений (используется только в старых задачах, которые были стартованы до нового механизма синхронизации вложений).</param>
    /// <returns>Список приложений.</returns>
    [Public]
    public virtual List<IElectronicDocument> GetActualAddendaForDocumentReviewTask(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup documentGroup, List<long> removedAddendaIds)
    {
      return Docflow.PublicFunctions.Module.GetActualAddenda(documentGroup, removedAddendaIds);
    }
    
    /// <summary>
    /// Получить список текущих приложений по документу.
    /// </summary>
    /// <param name="documentGroup">Группа Основной документ.</param>
    /// <param name="removedAddendaIds">ИД удаленных из группы приложений (используется только в старых задачах, которые были стартованы до нового механизма синхронизации вложений).</param>
    /// <returns>Список приложений.</returns>
    [Obsolete("Метод не используется с 13.06.2024 и версии 4.11. Используйте метод с таким же именем из модуля Docflow.")]
    public virtual List<IElectronicDocument> GetActualAddenda(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup documentGroup, List<long> removedAddendaIds)
    {
      var document = documentGroup.All.OfType<Sungero.Docflow.IOfficialDocument>().FirstOrDefault();
      if (document == null)
        return Enumerable.Empty<IElectronicDocument>().ToList();
      
      // Документы, связанные связью Приложение с основным документом.
      var documentAddenda = Docflow.PublicFunctions.Module.GetAddenda(document);

      // Исключаем удаленные вручную приложения для совместимости со старыми версиями задач.
      return documentAddenda.Where(a => !removedAddendaIds.Contains(a.Id)).ToList();
    }
    
    /// <summary>
    /// Получить все проекты резолюции, добавленные пользователем в рамках задания.
    /// </summary>
    /// <param name="assignment">Задание, в котором добавлялись проекты резолюции.</param>
    /// <param name="allActionItems">Все проекты рехолюции из задания.</param>
    /// <param name="performer">Исполнитель, который вкладывал проекты резолюции.</param>
    /// <param name="attachmentGroupName">Имя группы вложений.</param>
    /// <returns>Все проекты резолюции, добавленные пользователем в рамках задания.</returns>
    [Public, Remote(IsPure = true)]
    public virtual List<IActionItemExecutionTask> GetActionItemsAddedToAssignment(IAssignment assignment,
                                                                                  List<IActionItemExecutionTask> allActionItems,
                                                                                  IUser performer, string attachmentGroupName)
    {
      var actionItemsToDelete = new List<IActionItemExecutionTask>();
      foreach (var actionItem in allActionItems)
      {
        var attachmentInfo = assignment.AttachmentsInfo
          .Where(i => i.IsLinkedTo(actionItem) &&
                 i.GroupName == attachmentGroupName &&
                 Equals(i.AttachedBy, performer))
          .FirstOrDefault();
        
        if (attachmentInfo != null)
          actionItemsToDelete.Add(actionItem);
      }
      return actionItemsToDelete;
    }
    
    #endregion
    
    /// <summary>
    /// Получить исполнителей, не завершивших работу по поручению.
    /// </summary>
    /// <param name="task">Поручение, для которого требуется получить исполнителей.</param>
    /// <returns>Список исполнителей, не завершивших работу по поручению.</returns>
    [Public, Remote(IsPure = true)]
    public virtual IQueryable<IUser> GetUnfinishedActionItemsAssignees(IActionItemExecutionTask task)
    {
      return Functions.ActionItemExecutionTask.GetUnfinishedActionItems(task).Select(p => p.Performer);
    }
    
    /// <summary>
    /// Получить соисполнителей, не завершивших работу по поручению.
    /// </summary>
    /// <param name="assignment">Поручение.</param>
    /// <returns>Соисполнители, не завершившие работу по поручению.</returns>
    [Public, Remote(IsPure = true)]
    public virtual IQueryable<IUser> GetUnfinishedSubActionItemsAssignees(IActionItemExecutionAssignment assignment)
    {
      return Functions.ActionItemExecutionAssignment.GetUnfinishedActionItems(assignment).Select(p => p.Performer);
    }
    
    /// <summary>
    /// Получить информацию о контроле поручения.
    /// </summary>
    /// <param name="actionItemTask">Поручение.</param>
    /// <returns>Информация о контролере.</returns>
    [Public]
    public virtual string GetSupervisorInfoForActionItem(IActionItemExecutionTask actionItemTask)
    {
      var supervisor = actionItemTask.Supervisor;
      var isOnControl = actionItemTask.IsUnderControl == true;
      var supervisorLabel = string.Empty;
      if (isOnControl && supervisor != null)
        supervisorLabel = Company.PublicFunctions.Employee.GetShortName(supervisor, false);
      return supervisorLabel;
    }
    
    /// <summary>
    /// Данные для печати проекта резолюции.
    /// </summary>
    /// <param name="resolution">Список поручений.</param>
    /// <param name="reportSessionId">ИД сессии.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public static List<Structures.DraftResolutionReport.DraftResolutionReportParameters> GetDraftResolutionReportData(List<IActionItemExecutionTask> resolution,
                                                                                                                      string reportSessionId)
    {
      var result = new List<Structures.DraftResolutionReport.DraftResolutionReportParameters>();
      foreach (var actionItemTask in resolution)
      {
        // Контролер.
        var supervisor = actionItemTask.Supervisor;
        var isOnControl = actionItemTask.IsUnderControl == true;
        var supervisorLabel = string.Empty;
        if (isOnControl && supervisor != null)
          supervisorLabel = Company.PublicFunctions.Employee.GetShortName(supervisor, false);
        
        // Равноправное поручение.
        if (actionItemTask.IsCompoundActionItem == true)
        {
          foreach (var part in actionItemTask.ActionItemParts)
          {
            var partSupervisorLabel = supervisorLabel;
            if (isOnControl && part.Supervisor != null)
              partSupervisorLabel = Company.PublicFunctions.Employee.GetShortName(part.Supervisor, false);
            var deadline = part.Deadline ?? actionItemTask.FinalDeadline ?? null;
            var coAssigneeDeadline = part.CoAssigneesDeadline ?? actionItemTask.CoAssigneesDeadline ?? null;
            var resolutionLabel = string.Join("\r\n", actionItemTask.ActiveText, part.ActionItemPart);
            var subAssignees = Functions.ActionItemExecutionTask.GetPartCoAssignees(actionItemTask, part.PartGuid);
            var data = GetActionItemDraftResolutionReportData(part.Assignee,
                                                              subAssignees,
                                                              deadline,
                                                              coAssigneeDeadline,
                                                              resolutionLabel,
                                                              partSupervisorLabel,
                                                              reportSessionId);
            result.Add(data);
          }
        }
        else
        {
          // Поручение с соисполнителями.
          var deadline = actionItemTask.Deadline ?? null;
          var coAssigneeDeadline = actionItemTask.CoAssigneesDeadline ?? null;
          var subAssignees = actionItemTask.CoAssignees.Select(a => a.Assignee).ToList();
          var data = GetActionItemDraftResolutionReportData(actionItemTask.Assignee,
                                                            subAssignees,
                                                            deadline,
                                                            coAssigneeDeadline,
                                                            actionItemTask.ActiveText,
                                                            supervisorLabel,
                                                            reportSessionId);
          result.Add(data);
        }
      }
      return result;
    }
    
    /// <summary>
    /// Получение данных поручения для отчета Проект резолюции.
    /// </summary>
    /// <param name="assignee">Исполнитель.</param>
    /// <param name="subAssignees">Соисполнители.</param>
    /// <param name="deadline">Срок исполнения.</param>
    /// <param name="coAssigneeDeadline">Срок соисполнителей.</param>
    /// <param name="actionItem">Текст поручения.</param>
    /// <param name="supervisorLabel">Контролёр.</param>
    /// <param name="reportSessionId">Ид сессии.</param>
    /// <returns>Данные поручения.</returns>
    public static Structures.DraftResolutionReport.DraftResolutionReportParameters GetActionItemDraftResolutionReportData(IEmployee assignee,
                                                                                                                          List<IEmployee> subAssignees,
                                                                                                                          DateTime? deadline,
                                                                                                                          DateTime? coAssigneeDeadline,
                                                                                                                          string actionItem,
                                                                                                                          string supervisorLabel,
                                                                                                                          string reportSessionId)
    {
      var data = new Structures.DraftResolutionReport.DraftResolutionReportParameters();
      data.ReportSessionId = reportSessionId;
      
      // Исполнители и срок.
      var assigneeShortName = Company.PublicFunctions.Employee.GetShortName(assignee, false);
      if (subAssignees != null && subAssignees.Any())
        assigneeShortName = string.Format("<u>{0}</u>{1}{2}", assigneeShortName, Environment.NewLine,
                                          string.Join(", ", subAssignees.Select(p => Company.PublicFunctions.Employee.GetShortName(p, false))));
      
      data.PerformersLabel = assigneeShortName;
      if (!Equals(deadline, null))
        data.Deadline = deadline.Value.HasTime() ? deadline.Value.ToUserTime().ToString("g") : deadline.Value.ToString("d");
      else
        data.Deadline = Resources.ActionItemIndefiniteDeadline;
      
      // Срок соисполнителей.
      var formattedCoAssigneeDeadline = string.Empty;
      if (!Equals(coAssigneeDeadline, null))
      {
        formattedCoAssigneeDeadline = coAssigneeDeadline.Value.HasTime() ? coAssigneeDeadline.Value.ToUserTime().ToString("g") : coAssigneeDeadline.Value.ToString("d");
        data.Deadline = string.Join("\n", data.Deadline, formattedCoAssigneeDeadline + Sungero.RecordManagement.Resources.CoAssignees);
      }
      
      // Поручение.
      data.ResolutionLabel = actionItem;
      
      // Контролёр.
      data.SupervisorLabel = supervisorLabel;
      
      return data;
    }
    
    /// <summary>
    /// Исключить из наблюдателей системных пользователей.
    /// </summary>
    /// <param name="query">Запрос.</param>
    /// <returns>Отфильтрованный результат запроса.</returns>
    [Public]
    public IQueryable<Sungero.CoreEntities.IRecipient> ObserversFiltering(IQueryable<Sungero.CoreEntities.IRecipient> query)
    {
      var systemRecipientsSid = Company.PublicFunctions.Module.GetSystemRecipientsSidWithoutAllUsers(true);
      return query.Where(x => !systemRecipientsSid.Contains(x.Sid.Value));
    }

    /// <summary>
    /// Получить константу срока рассмотрения документа по умолчанию в днях.
    /// </summary>
    /// <returns>Константу срока рассмотрения документа по умолчанию в днях.</returns>
    [RemoteAttribute]
    public virtual int GetDocumentReviewDefaultDays()
    {
      return Constants.Module.DocumentReviewDefaultDays;
    }
    
    /// <summary>
    /// Получить отфильтрованные журналы регистрации для отчета.
    /// </summary>
    /// <param name="direction">Документопоток.</param>
    /// <returns>Журналы регистрации.</returns>
    [Remote(IsPure = true)]
    public static List<IDocumentRegister> GetFilteredDocumentRegistersForReport(Enumeration direction)
    {
      var needFilterDocumentRegisters = !Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor();
      return Docflow.PublicFunctions.DocumentRegister.Remote.GetFilteredDocumentRegisters(direction, null, needFilterDocumentRegisters).ToList();
    }
    
    /// <summary>
    /// Удалить поручения.
    /// </summary>
    /// <param name="actionItems">Список поручений.</param>
    [Remote]
    public static void DeleteActionItemExecutionTasks(List<IActionItemExecutionTask> actionItems)
    {
      foreach (var draftResolution in actionItems)
        ActionItemExecutionTasks.Delete(draftResolution);
    }
    
    /// <summary>
    /// Получить списки ознакомления.
    /// </summary>
    /// <returns>Списки ознакомления.</returns>
    [Public, Remote(IsPure = true)]
    public IQueryable<IAcquaintanceList> GetAcquaintanceLists()
    {
      return AcquaintanceLists.GetAll()
        .Where(a => a.Status == Sungero.RecordManagement.AcquaintanceList.Status.Active);
    }
    
    /// <summary>
    /// Создать список ознакомления.
    /// </summary>
    /// <returns>Список ознакомления.</returns>
    [Public, Remote]
    public IAcquaintanceList CreateAcquaintanceList()
    {
      return AcquaintanceLists.Create();
    }
    
    /// <summary>
    /// Получить поручение по ИД.
    /// </summary>
    /// <param name="id">ИД задачи.</param>
    /// <returns>Поручение.</returns>
    [Remote]
    public IActionItemExecutionTask GetActionitemById(long id)
    {
      return ActionItemExecutionTasks.GetAll(t => Equals(t.Id, id)).FirstOrDefault();
    }
    
    /// <summary>
    /// Получить статус выполнения задания на ознакомление.
    /// </summary>
    /// <param name="assignment">Задание на ознакомление.</param>
    /// <param name="isElectronicAcquaintance">Признак "Электронное ознакомление".</param>
    /// <param name="isCompleted">Признак завершённости задачи.</param>
    /// <returns>Статус выполнения задания на ознакомление.</returns>
    public virtual string GetAcquaintanceAssignmentState(IAcquaintanceAssignment assignment,
                                                         bool isElectronicAcquaintance,
                                                         bool isCompleted)
    {
      if (!isCompleted)
        return string.Empty;
      
      if (Equals(assignment.CompletedBy, assignment.Performer) || !isElectronicAcquaintance)
        return Reports.Resources.AcquaintanceReport.AcquaintedState;

      return Reports.Resources.AcquaintanceReport.CompletedState;
    }
    
    /// <summary>
    /// Получить все приложения по задачам ознакомления с документом.
    /// </summary>
    /// <param name="tasks">Задачи.</param>
    /// <returns>Список приложений.</returns>
    [Remote(IsPure = true)]
    public List<IElectronicDocument> GetAcquintanceTaskAddendas(List<IAcquaintanceTask> tasks)
    {
      var addenda = new List<IElectronicDocument>();
      var addendaIds = tasks.SelectMany(x => x.AcquaintanceVersions)
        .Where(x => x.IsMainDocument != true)
        .Select(x => x.DocumentId);
      
      var documentAddenda = tasks.SelectMany(x => x.AddendaGroup.OfficialDocuments)
        .Where(x => addendaIds.Contains(x.Id))
        .Distinct()
        .ToList();
      addenda.AddRange(documentAddenda);

      return addenda;
    }
    
    /// <summary>
    /// Получить все приложения по задаче ознакомления с документом.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Список приложений.</returns>
    [Remote(IsPure = true)]
    public List<IElectronicDocument> GetAcquintanceTaskAddendas(IAcquaintanceTask task)
    {
      return this.GetAcquintanceTaskAddendas(new List<IAcquaintanceTask> { task });
    }
    
    /// <summary>
    /// Получить значение поля Адресат в отчете Журнал исходящих документов.
    /// </summary>
    /// <param name="letterId">ИД исходящего письма.</param>
    /// <returns>Значение поля Адресат.</returns>
    [Public]
    public string GetOutgoingDocumentReportAddressee(long letterId)
    {
      var outgoingLetter = Sungero.Docflow.OutgoingDocumentBases.Get(letterId);
      if (outgoingLetter == null)
        return string.Empty;
      if (outgoingLetter.Addressees.Count < 5)
      {
        var addresseeList = new List<string>();
        foreach (var addressee in outgoingLetter.Addressees.OrderBy(a => a.Number))
        {
          var addresseeString = addressee.Addressee == null
            ? addressee.Correspondent.Name
            : string.Concat(addressee.Correspondent.Name, "\n", addressee.Addressee.Name);

          addresseeList.Add(addresseeString);
        }
        return string.Join("\n\n", addresseeList);
      }
      else
        return Docflow.PublicFunctions.Module.ReplaceFirstSymbolToUpperCase(
          OutgoingLetters.Resources.CorrespondentToManyAddressees.ToString().Trim());
    }
    
    /// <summary>
    /// Получить данные для отчета DraftResolutionReport.
    /// </summary>
    /// <param name="actionItems">Поручения.</param>
    /// <param name="reportSessionId">Ид отчета.</param>
    /// <param name="textResolution">Текстовая резолюция.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public virtual List<Structures.DraftResolutionReport.DraftResolutionReportParameters> GetDraftResolutionReportData(List<IActionItemExecutionTask> actionItems, string reportSessionId, string textResolution)
    {
      // Получить данные для отчета.
      var reportData = new List<Structures.DraftResolutionReport.DraftResolutionReportParameters>();
      if (actionItems.Any())
      {
        reportData = PublicFunctions.Module.GetDraftResolutionReportData(actionItems, reportSessionId);
      }
      else
      {
        // Если нет поручений, то берём текстовую резолюцию.
        reportData = new List<Structures.DraftResolutionReport.DraftResolutionReportParameters>();
        var data = new Structures.DraftResolutionReport.DraftResolutionReportParameters();
        data.ReportSessionId = reportSessionId;
        data.PerformersLabel = textResolution;
        reportData.Add(data);
      }
      return reportData;
    }
    
    /// <summary>
    /// Получить представление документа для отчета DraftResolutionReport.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns> Представление.</returns>
    [Public]
    public virtual string GetDraftResolutionReportDocumentShortName(Docflow.IOfficialDocument document)
    {
      // Номер и дата документа.
      var documentShortName = string.Empty;
      if (document != null)
      {
        if (!string.IsNullOrWhiteSpace(document.RegistrationNumber))
          documentShortName += string.Format("{0} {1}", Docflow.OfficialDocuments.Resources.Number, document.RegistrationNumber);
        
        if (document.RegistrationDate.HasValue)
          documentShortName += Docflow.OfficialDocuments.Resources.DateFrom + document.RegistrationDate.Value.ToString("d");
        
        if (!string.IsNullOrWhiteSpace(document.RegistrationNumber))
          documentShortName += string.Format(" ({0} {1})", Reports.Resources.DraftResolutionReport.IDPrefix, document.Id.ToString());
        else
          documentShortName += string.Format(" {0} {1}", Reports.Resources.DraftResolutionReport.IDPrefix, document.Id.ToString());
      }
      return documentShortName;
    }
    
    /// <summary>
    /// Получить представление документа для отчета ActionItemPrintReport.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="actionItem">Поручение.</param>
    /// <returns>Представление.</returns>
    [Public]
    public virtual string GetActionItemPrintReportDocumentShortName(Docflow.IOfficialDocument document, Sungero.Workflow.IAssignment actionItem)
    {
      // Номер и дата документа.
      var documentShortName = string.Empty;
      if (document != null)
      {
        // "К документу".
        documentShortName += Reports.Resources.ActionItemPrintReport.ToDocument;
        
        // Номер.
        if (!string.IsNullOrWhiteSpace(document.RegistrationNumber))
          documentShortName += string.Format("{0} {1}", Docflow.OfficialDocuments.Resources.Number, document.RegistrationNumber);
        
        // Дата.
        if (document.RegistrationDate.HasValue)
          documentShortName += string.Format("{0}{1}", Docflow.OfficialDocuments.Resources.DateFrom, document.RegistrationDate.Value.ToString("d"));
        
        // ИД и разделитель /.
        documentShortName += string.Format(" ({0} {1}) / ", Reports.Resources.ActionItemPrintReport.DocumentID, document.Id.ToString());
      }
      
      // ИД поручения.
      documentShortName += string.Format("{0} {1}", Reports.Resources.ActionItemPrintReport.ActionItemID, actionItem.Id.ToString());
      
      return documentShortName;
    }
    
    /// <summary>
    /// Получить данные для отчета ActionItemPrintReport.
    /// </summary>
    /// <param name="actionItemTask">Поручение.</param>
    /// <param name="reportId">Ид отчета.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public virtual List<Structures.ActionItemPrintReport.ActionItemPrintReportParameters> GetActionItemPrintReportData(IActionItemExecutionTask actionItemTask, string reportId)
    {
      // Получить данные для отчета.
      var reportData = new List<Structures.ActionItemPrintReport.ActionItemPrintReportParameters>();
      
      // Контролёр.
      var supervisor = this.GetSupervisorInfoForActionItem(actionItemTask);
      // От кого.
      var fromAuthor = this.GetAuthorLineInfoForActionItem(actionItemTask);
      var actionItemText = string.Empty;
      IEmployee assignee = null;
      DateTime? deadline;
      DateTime? coAssigneesDeadline = null;
      var subAssignees = new List<IEmployee>() { };

      if (actionItemTask.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Component)
      {
        var task = ActionItemExecutionTasks.As(actionItemTask.ParentTask);
        var part = task.ActionItemParts.Where(n => Equals(n.ActionItemPartExecutionTask, actionItemTask)).FirstOrDefault();
        actionItemText = string.Join("\r\n", task.ActiveText, part.ActionItemPart);
        assignee = actionItemTask.Assignee;
        deadline = part.Deadline ?? task.FinalDeadline ?? Calendar.Now;
        subAssignees = Functions.ActionItemExecutionTask.GetPartCoAssignees(task, part.PartGuid);
        coAssigneesDeadline = part.CoAssigneesDeadline;
      }
      else
      {
        var task = actionItemTask.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Additional ? ActionItemExecutionTasks.As(actionItemTask.ParentAssignment.Task) : actionItemTask;
        // Поручение с соисполнителями.
        actionItemText = task.ActiveText;
        if (task.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Component &&
            task.ParentTask.ActiveText != task.ActiveText)
          actionItemText = string.Join("\r\n", task.ParentTask.ActiveText, task.ActiveText);
        
        subAssignees = task.CoAssignees.Select(a => a.Assignee).ToList();
        assignee = task.Assignee;
        deadline = actionItemTask.Deadline ?? Calendar.Now;
        if (!(actionItemTask.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Additional))
          coAssigneesDeadline = task.CoAssigneesDeadline;
      }
      
      var assigneeShortName = Company.PublicFunctions.Employee.GetShortName(assignee, false);
      
      var formattedDeadline = string.Empty;
      if (deadline != null)
        formattedDeadline = deadline.Value.HasTime() ? deadline.Value.ToUserTime().ToString("g") : deadline.Value.ToString("d");
      if (actionItemTask.HasIndefiniteDeadline.Value)
        formattedDeadline = Resources.ActionItemIndefiniteDeadline;
      var formattedCoAssigneesDeadline = string.Empty;
      if (subAssignees != null && subAssignees.Any())
      {
        assigneeShortName = string.Format("<u>{0}</u>{1}{2}",
                                          assigneeShortName,
                                          Environment.NewLine,
                                          string.Join(", ", subAssignees.Select(p => Company.PublicFunctions.Employee.GetShortName(p, false))));
        coAssigneesDeadline = coAssigneesDeadline ?? null;
        if (coAssigneesDeadline != null)
          formattedCoAssigneesDeadline = coAssigneesDeadline.Value.HasTime() ? coAssigneesDeadline.Value.ToUserTime().ToString("g") : coAssigneesDeadline.Value.ToString("d");
      }

      var data = this.GetActionItemPrintReportData(assigneeShortName, formattedDeadline, formattedCoAssigneesDeadline, fromAuthor, supervisor, actionItemText, reportId);
      reportData.Add(data);
      return reportData;
    }
    
    /// <summary>
    /// Получить данные для отчета ActionItemPrintReport.
    /// </summary>
    /// <param name="assigneeShortName">Кому.</param>
    /// <param name="deadline">Срок.</param>
    /// <param name="coAssigneesDeadline">Срок соисполнителей.</param>
    /// <param name="fromAuthor">От кого.</param>
    /// <param name="supervisor">Контролер.</param>
    /// <param name="actionItemText">Текст поручения.</param>
    /// <param name="reportId">Ид отчета.</param>
    /// <returns>Структура для отчета.</returns>
    [Public]
    public virtual Structures.ActionItemPrintReport.ActionItemPrintReportParameters GetActionItemPrintReportData(string assigneeShortName, string deadline, string coAssigneesDeadline, string fromAuthor, string supervisor,
                                                                                                                 string actionItemText, string reportId)
    {
      var data = new Structures.ActionItemPrintReport.ActionItemPrintReportParameters();
      data.ReportSessionId = reportId;
      data.Performer = assigneeShortName;
      data.Deadline = deadline;
      data.CoAssigneesDeadline = coAssigneesDeadline;
      data.ActionItemText = actionItemText;
      data.Supervisor = supervisor;
      data.FromAuthor = fromAuthor;
      
      return data;
    }
    
    /// <summary>
    /// Получить цепочку сотрудников, выдавших поручение.
    /// </summary>
    /// <param name="actionItemTask">Поручение.</param>
    /// <returns>Информация о выдавших поручение.</returns>
    [Public]
    public virtual string GetAuthorLineInfoForActionItem(IActionItemExecutionTask actionItemTask)
    {
      var authorInfo = Company.PublicFunctions.Employee.GetShortName(Employees.As(actionItemTask.AssignedBy), false);
      var currentTask = Workflow.Tasks.As(actionItemTask);
      var parentTask = currentTask.ParentTask != null ? currentTask.ParentTask : currentTask.ParentAssignment != null ? currentTask.ParentAssignment.Task : currentTask.MainTask;
      while (ActionItemExecutionTasks.As(parentTask) != null && currentTask != parentTask)
      {
        if (ActionItemExecutionTasks.As(currentTask).ActionItemType != RecordManagement.ActionItemExecutionTask.ActionItemType.Component)
          authorInfo = string.Format("{0} -> {1}", Company.PublicFunctions.Employee.GetShortName(Employees.As(ActionItemExecutionTasks.As(parentTask).AssignedBy), false), authorInfo);
        
        currentTask = parentTask;
        parentTask = parentTask.ParentTask != null ? parentTask.ParentTask : currentTask.ParentAssignment != null ? currentTask.ParentAssignment.Task : currentTask.MainTask;
      }
      return authorInfo;
    }
    
    /// <summary>
    /// Создать параметры модуля.
    /// </summary>
    public virtual void CreateSettings()
    {
      var recordManagementSettings = RecordManagementSettings.Create();
      recordManagementSettings.Name = RecordManagementSettings.Info.LocalizedName;
      recordManagementSettings.AllowActionItemsWithIndefiniteDeadline = false;
      recordManagementSettings.AllowAcquaintanceBySubstitute = false;
      recordManagementSettings.ControlRelativeDeadlineInDays = 1;
      recordManagementSettings.Save();
    }
    
    /// <summary>
    /// Создать и выполнить асинхронное событие изменения поручения.
    /// </summary>
    /// <param name="changes">Изменения.</param>
    /// <param name="actionItemTaskId">Ид задачи.</param>
    /// <param name="onEditGuid">Guid поручения.</param>
    [Public, Remote]
    public virtual void ExecuteApplyActionItemLockIndependentChanges(RecordManagement.Structures.ActionItemExecutionTask.IActionItemChanges changes, long actionItemTaskId, string onEditGuid)
    {
      Logger.DebugFormat("ApplyActionItemLockIndependentChanges({0}): actionItemTaskId {0}", actionItemTaskId);
      var asyncChangeActionItem = RecordManagement.AsyncHandlers.ApplyActionItemLockIndependentChanges.Create();
      asyncChangeActionItem.ActionItemTaskId = actionItemTaskId;
      asyncChangeActionItem.OldSupervisor = changes.OldSupervisor?.Id ?? -1;
      asyncChangeActionItem.NewSupervisor = changes.NewSupervisor?.Id ?? -1;
      asyncChangeActionItem.OldAssignee = changes.OldAssignee?.Id ?? -1;
      asyncChangeActionItem.NewAssignee = changes.NewAssignee?.Id ?? -1;
      asyncChangeActionItem.OldDeadline = changes.OldDeadline ?? DateTime.MinValue;
      asyncChangeActionItem.NewDeadline = changes.NewDeadline ?? DateTime.MinValue;
      asyncChangeActionItem.OldCoAssignees = string.Join(",", changes.OldCoAssignees.Select(x => x.Id).ToList());
      asyncChangeActionItem.NewCoAssignees = string.Join(",", changes.NewCoAssignees.Select(x => x.Id).ToList());
      asyncChangeActionItem.CoAssigneesOldDeadline = changes.CoAssigneesOldDeadline ?? DateTime.MinValue;
      asyncChangeActionItem.CoAssigneesNewDeadline = changes.CoAssigneesNewDeadline ?? DateTime.MinValue;
      asyncChangeActionItem.EditingReason = changes.EditingReason;
      asyncChangeActionItem.AdditionalInfo = changes.AdditionalInfo;
      asyncChangeActionItem.OnEditGuid = onEditGuid;
      asyncChangeActionItem.InitiatorOfChange = changes.InitiatorOfChange.Id;
      asyncChangeActionItem.ChangeContext = changes.ChangeContext;
      asyncChangeActionItem.ExecuteAsync();
    }
    
    /// <summary>
    /// Создать и выполнить асинхронное событие изменения поручения.
    /// </summary>
    /// <param name="changes">Изменения.</param>
    /// <param name="actionItemTaskId">Ид задачи.</param>
    /// <param name="onEditGuid">Guid поручения.</param>
    public virtual void ExecuteApplyActionItemLockDependentChanges(RecordManagement.Structures.ActionItemExecutionTask.IActionItemChanges changes, long actionItemTaskId, string onEditGuid)
    {
      Logger.DebugFormat("ApplyActionItemLockDependentChanges({0}): actionItemTaskId {0}", actionItemTaskId);
      var asyncChangeActionItem = RecordManagement.AsyncHandlers.ApplyActionItemLockDependentChanges.Create();
      asyncChangeActionItem.ActionItemTaskId = actionItemTaskId;
      asyncChangeActionItem.OldSupervisor = changes.OldSupervisor?.Id ?? -1;
      asyncChangeActionItem.NewSupervisor = changes.NewSupervisor?.Id ?? -1;
      asyncChangeActionItem.OldAssignee = changes.OldAssignee?.Id ?? -1;
      asyncChangeActionItem.NewAssignee = changes.NewAssignee?.Id ?? -1;
      asyncChangeActionItem.OldDeadline = changes.OldDeadline ?? DateTime.MinValue;
      asyncChangeActionItem.NewDeadline = changes.NewDeadline ?? DateTime.MinValue;
      asyncChangeActionItem.OldCoAssignees = string.Join(",", changes.OldCoAssignees.Select(x => x.Id).ToList());
      asyncChangeActionItem.NewCoAssignees = string.Join(",", changes.NewCoAssignees.Select(x => x.Id).ToList());
      asyncChangeActionItem.CoAssigneesOldDeadline = changes.CoAssigneesOldDeadline ?? DateTime.MinValue;
      asyncChangeActionItem.CoAssigneesNewDeadline = changes.CoAssigneesNewDeadline ?? DateTime.MinValue;
      asyncChangeActionItem.EditingReason = changes.EditingReason;
      asyncChangeActionItem.AdditionalInfo = changes.AdditionalInfo;
      asyncChangeActionItem.OnEditGuid = onEditGuid;
      asyncChangeActionItem.InitiatorOfChange = changes.InitiatorOfChange.Id;
      asyncChangeActionItem.ChangeContext = changes.ChangeContext;
      asyncChangeActionItem.ExecuteAsync();
    }

    /// <summary>
    /// Проверить, что по поручению уже созданы все актуальные задания, и его можно корректировать.
    /// </summary>
    /// <param name="tasks">Список задач.</param>
    /// <returns>Текст ошибки, если задания не созданы. Иначе пустую строку.</returns>
    public virtual string CheckActionItemAssignmentsCreated(List<IActionItemExecutionTask> tasks)
    {
      var error = ActionItemExecutionTasks.Resources.ActionItemIsAlreadyInChangingProcess;

      // По простому поручению не созданы подзадачи соисполнителям.
      if (tasks.Any(t => t.CoAssignees.Any(ca => ca.AssignmentCreated != true)))
        return error;

      // По составному поручению не созданы подзадачи по пунктам.
      if (tasks.Any(t => t.ActionItemParts.Any(aip => aip.AssignmentCreated != true)))
        return error;
      
      // По составному поручению не созданы подзадачи соисполнителям.
      if (tasks.Any(t => t.ActionItemParts.Any(aip => aip.ActionItemPartExecutionTask == null ||
                                               (aip.ActionItemPartExecutionTask.Status == Sungero.Workflow.Task.Status.InProcess &&
                                                aip.ActionItemPartExecutionTask.CoAssignees.Any(ca => ca.AssignmentCreated != true)))))
        return error;

      var onExecutionTasksIds = tasks
        .Where(j => j.ExecutionState == RecordManagement.ActionItemExecutionTask.ExecutionState.OnExecution ||
               j.ExecutionState == RecordManagement.ActionItemExecutionTask.ExecutionState.OnRework)
        .Select(t => t.Id)
        .ToList();
      
      // Проверить для каждой задачи, что поручение на исполнении и есть задания на исполнение в работе.
      var executionAssignmentsCount = ActionItemExecutionAssignments.GetAll()
        .Where(j => j.Status == Workflow.AssignmentBase.Status.InProcess)
        .Where(j => onExecutionTasksIds.Contains(j.Task.Id))
        .Where(j => Equals(j.Performer, ActionItemExecutionTasks.As(j.Task).Assignee))
        .Where(j => j.TaskStartId == j.Task.StartId)
        .Count();

      if (executionAssignmentsCount != onExecutionTasksIds.Count)
        return error;

      var tasksIds = tasks.Select(t => t.Id).ToList();
      var actionItemExecutionTasksInProcess = ActionItemExecutionTasks.GetAll()
        .Where(t => t.Status == Workflow.Task.Status.InProcess);
      
      // В задачах соисполнителю должно быть хотя бы одно задание.
      var coAssigneeTasks = actionItemExecutionTasksInProcess
        .Where(t => t.ParentAssignment != null && tasksIds.Contains(t.ParentAssignment.Task.Id))
        .Where(t => t.ActionItemType == Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Additional);
      var allAssignmentsStarted = this.CheckAllAssignmentsOnTasksStarted(coAssigneeTasks);
      
      if (!allAssignmentsStarted)
        return error;
      
      // В пунктах составного поручения должно быть хотя бы одно задание.
      var compoundActionItemTasks = actionItemExecutionTasksInProcess
        .Where(t => tasksIds.Contains(t.ParentTask.Id) && t.ActionItemType == Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Component);
      var allCompoundAssignmentsStarted = this.CheckAllAssignmentsOnTasksStarted(compoundActionItemTasks);
      
      if (!allCompoundAssignmentsStarted)
        return error;
      
      // В задачах соисполнителям пунктов составного поручения должно быть хотя бы одно задание.
      var compoundActionItemCoAssigneeTasks = actionItemExecutionTasksInProcess
        .Where(t => t.ActionItemType == Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Additional &&
               t.ParentAssignment != null && tasksIds.Contains(t.ParentAssignment.Task.ParentTask.Id));
      var allCompoundCoAssigneeAssignmentsStarted = this.CheckAllAssignmentsOnTasksStarted(compoundActionItemCoAssigneeTasks);
      
      if (!allCompoundCoAssigneeAssignmentsStarted)
        return error;

      return null;
    }
    
    /// <summary>
    /// Проверить, что поручение (в том числе подпоручения соисполнителям, пункты составного и подпоручения соисполнителям пунктов)
    /// не корректируется в текущий момент.
    /// </summary>
    /// <param name="tasks">Список задач.</param>
    /// <returns>Текст ошибки, если корректируется. Иначе пустую строку.</returns>
    public virtual string CheckActionItemNotInChangingProcess(List<IActionItemExecutionTask> tasks)
    {
      // Проверить пункты составного поручения, подпоручения соисполнителям пунктов и подпоручения соисполнителям текущих поручений.
      var tasksToCheck = tasks
        .SelectMany(t => t.ActionItemParts)
        .Where(x => x.ActionItemPartExecutionTask != null)
        .Where(x => x.ActionItemPartExecutionTask.Status == Sungero.Workflow.Task.Status.InProcess)
        .Select(x => x.ActionItemPartExecutionTask)
        .ToList();
      
      tasksToCheck.AddRange(tasks);
      
      // Проверить пункты составного поручения и текущие поручения.
      if (tasksToCheck.Any(x => (x.OnEditGuid ?? string.Empty) != string.Empty))
        return ActionItemExecutionTasks.Resources.ActionItemIsAlreadyInChangingProcess;
      
      // Проверить подпоручения соисполнителям пунктов и подпоручения соисполнителям текущих поручений.
      var tasksToCheckIds = tasksToCheck.Select(t => t.Id).ToList();
      if (!this.CheckCoAssigneeActionItemsNotInChangingProcess(tasksToCheckIds))
        return ActionItemExecutionTasks.Resources.ActionItemIsAlreadyInChangingProcess;
      
      // Проверить главные составные поручения, если корректируется пункт.
      if (tasks.Where(t => t.ActionItemType == ActionItemType.Component).Any())
      {
        var mainActionItemsIds = tasks.Where(t => t.ParentTask != null).Select(t => t.ParentTask.Id).ToList();
        var mainActionItemNotInChangingProcessErrorText = this.CheckCurrentActionItemNotInChangingProcess(mainActionItemsIds);
        if (!string.IsNullOrEmpty(mainActionItemNotInChangingProcessErrorText))
          return mainActionItemNotInChangingProcessErrorText;
      }
      
      return null;
    }
    
    /// <summary>
    /// Проверить, что подпоручения соисполнителям не корректируются в текущий момент.
    /// </summary>
    /// <param name="tasksIds">Список Id задач.</param>
    /// <returns>True - ни одно из подпоручений не корректируется.
    /// False - часть подпоручений корректируются.</returns>
    public virtual bool CheckCoAssigneeActionItemsNotInChangingProcess(List<long> tasksIds)
    {
      var executionAssignmentIds = ActionItemExecutionAssignments.GetAll()
        .Where(j => tasksIds.Contains(j.Task.Id))
        .Where(j => Equals(j.Performer, ActionItemExecutionTasks.As(j.Task).Assignee))
        .Where(j => j.TaskStartId == j.Task.StartId)
        .OrderByDescending(j => j.Created)
        .Select(j => j.Id)
        .ToList();
      
      var anyCoAssigneesTasksOnEdit = ActionItemExecutionTasks.GetAll()
        .Where(t => t.ActionItemType == ActionItemType.Additional)
        .Where(t => t.ParentAssignment != null)
        .Where(t => executionAssignmentIds.Contains(t.ParentAssignment.Id))
        .Where(t => (t.OnEditGuid ?? string.Empty) != string.Empty)
        .Any();
      
      return !anyCoAssigneesTasksOnEdit;
    }
    
    /// <summary>
    /// Проверить, что поручение не корректируется в текущий момент.
    /// </summary>
    /// <param name="tasksIds">Список Id задач.</param>
    /// <returns>Текст ошибки, если корректируется. Иначе пустую строку.</returns>
    public virtual string CheckCurrentActionItemNotInChangingProcess(List<long> tasksIds)
    {
      var onEdit = ActionItemExecutionTasks.GetAll().Where(a => tasksIds.Contains(a.Id))
        .Any(a => (a.OnEditGuid ?? string.Empty) != string.Empty);
      if (onEdit)
        return ActionItemExecutionTasks.Resources.ActionItemIsAlreadyInChangingProcess;
      
      return null;
    }
    
    /// <summary>
    /// Проверить, что у всех поручений есть как минимум одно стартованное задание.
    /// </summary>
    /// <param name="tasks">Поручения.</param>
    /// <returns>True, если у всех поручений есть задания. Иначе False.</returns>
    public virtual bool CheckAllAssignmentsOnTasksStarted(IQueryable<IActionItemExecutionTask> tasks)
    {
      var tasksIds = tasks.Select(t => t.Id).ToList();
      if (tasksIds.Count == 0)
        return true;
      
      var tasksCount = Assignments.GetAll().Where(a => tasksIds.Contains(a.Task.Id)).Select(a => a.Task.Id).Distinct().Count();
      if (tasksIds.Count != tasksCount)
        return false;
      
      return true;
    }
    
    /// <summary>
    /// Скопировать изменения в поручении в новый экземпляр структуры.
    /// </summary>
    /// <param name="changes">Изменения в поручении.</param>
    /// <returns>Скопированные изменения.</returns>
    public virtual Structures.ActionItemExecutionTask.IActionItemChanges CopyActionItemChangesStructure(Structures.ActionItemExecutionTask.IActionItemChanges changes)
    {
      var copiedChanges = Structures.ActionItemExecutionTask.ActionItemChanges.Create();
      copiedChanges.OldSupervisor = changes.OldSupervisor;
      copiedChanges.NewSupervisor = changes.NewSupervisor;
      copiedChanges.OldAssignee = changes.OldAssignee;
      copiedChanges.NewAssignee = changes.NewAssignee;
      copiedChanges.OldDeadline = changes.OldDeadline;
      copiedChanges.NewDeadline = changes.NewDeadline;
      copiedChanges.OldCoAssignees = changes.OldCoAssignees;
      copiedChanges.NewCoAssignees = changes.NewCoAssignees;
      copiedChanges.CoAssigneesOldDeadline = changes.CoAssigneesOldDeadline;
      copiedChanges.CoAssigneesNewDeadline = changes.CoAssigneesNewDeadline;
      copiedChanges.EditingReason = changes.EditingReason;
      copiedChanges.AdditionalInfo = changes.AdditionalInfo;
      copiedChanges.TaskIds = changes.TaskIds;
      copiedChanges.ActionItemPartsText = changes.ActionItemPartsText;
      copiedChanges.InitiatorOfChange = changes.InitiatorOfChange;
      copiedChanges.ChangeContext = changes.ChangeContext;
      
      return copiedChanges;
    }
    
    /// <summary>
    /// Проверить, что ни одно поручение не было изменено с момента указанной даты.
    /// </summary>
    /// <param name="tasksIds">Список Id поручений.</param>
    /// <param name="lastActionItemChangeDate">Дата последнего изменения поручений.</param>
    /// <returns>Текст ошибки, если хотя бы одно поручение было изменено. Иначе null.</returns>
    public virtual string CheckActionItemNotChanged(List<long> tasksIds, DateTime? lastActionItemChangeDate)
    {
      if (!lastActionItemChangeDate.HasValue)
        return null;
      
      var actualLastActionItemChangeDate = Functions.Module.GetLastActionItemChangeDate(tasksIds);
      Logger.DebugFormat("lastActionItemChangeDate: {0}, actualLastActionItemChangeDate {1}", lastActionItemChangeDate, actualLastActionItemChangeDate);
      if (lastActionItemChangeDate < actualLastActionItemChangeDate)
        return RecordManagement.ActionItemExecutionTasks.Resources.ActionItemWasChanged;
      
      return null;
    }
    
    /// <summary>
    /// Получить максимальную дату последнего изменения поручений из списка.
    /// </summary>
    /// <param name="tasksIds">Список Id задач.</param>
    /// <returns>Максимальная дата последнего изменения поручений из списка.</returns>
    public virtual DateTime? GetLastActionItemChangeDate(List<long> tasksIds)
    {
      return ActionItemExecutionTasks.GetAll()
        .Where(t => tasksIds.Contains(t.Id))
        .Select(t => t.Modified)
        .OrderByDescending(t => t)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить активные задания на ознакомление.
    /// </summary>
    /// <param name="assignmentsIds">ИД заданий на ознакомление, записанные в виде строки через запятую.</param>
    /// <returns>Задания на ознакомление.</returns>
    public virtual List<IAcquaintanceAssignment> GetActiveAcquaintanceAssignments(string assignmentsIds)
    {
      var splittedAssignmentsIds = assignmentsIds.Split(',');
      var assignments = AcquaintanceAssignments.GetAll()
        .Where(x => splittedAssignmentsIds.Contains(x.Id.ToString()))
        .Where(x => x.Status != Workflow.Assignment.Status.Completed &&
               x.Status != Workflow.Assignment.Status.Aborted)
        .ToList();
      return assignments;
    }
    
    /// <summary>
    /// Построить модель представления проекта резолюции.
    /// </summary>
    /// <param name="resolutionTasks">Задача на исполнение поручения.</param>
    /// <returns>Xml представление контрола состояния.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateViewForDraftResolution(List<IActionItemExecutionTask> resolutionTasks)
    {
      var stateView = StateView.Create();
      if (resolutionTasks == null || resolutionTasks.Count == 0)
        return stateView;
      
      var skipResolutionBlock = false;
      var statusesCache = new Dictionary<Enumeration?, string>();
      foreach (var task in resolutionTasks)
      {
        var stateViewModel = Structures.ActionItemExecutionTask.StateViewModel.Create();
        stateViewModel.StatusesCache = statusesCache;
        var blocks = PublicFunctions.ActionItemExecutionTask.GetActionItemExecutionTaskStateView(task, task, stateViewModel, null, skipResolutionBlock, false).Blocks;
        statusesCache = stateViewModel.StatusesCache;
        
        // Убираем первый блок с текстовой информацией по поручению.
        foreach (var block in blocks.Skip(1))
          stateView.AddBlock(block);
        
        // Строим блок резолюции только для первого поручения.
        skipResolutionBlock = true;
      }
      return stateView;
    }
    
    /// <summary>
    /// Подготовить поручения из проекта резолюции к старту: синхронизировать вложения,
    /// установить ведущее задание, выдать участникам права на вложения из группы "Дополнительно".
    /// </summary>
    /// <param name="draftResolution">Проект резолюции.</param>
    /// <param name="parentAssignment">Ведущее задание.</param>
    /// <param name="primaryDocument">Основной документ.</param>
    /// <param name="addendaDocuments">Приложения.</param>
    /// <param name="otherAttachments">Дополнительно.</param>
    [Public, Remote]
    public virtual void PrepareDraftResolutionForStart(List<IActionItemExecutionTask> draftResolution,
                                                       IAssignment parentAssignment,
                                                       IOfficialDocument primaryDocument,
                                                       List<IElectronicDocument> addendaDocuments,
                                                       List<IEntity> otherAttachments)
    {
      // TODO Shklyaev: переделать метод, когда в платформе сделают возможность стартовать задачу как подзадачу от задания (65004).
      foreach (var actionItem in draftResolution)
      {
        // Для каждого поручения из проекта резолюции перебиваем ведущее задание на текущее.
        // После указания ведущего задания (ParentAssignment) и MainTask в поручении будут очищены все его вложения,
        // поэтому синхронизацию вложений делаем после установки ведущего задания. Также отдельно выдаём права на вложения группы "Дополнительно".
        Functions.ActionItemExecutionTask.ClearAttachments(actionItem);
        Functions.ActionItemExecutionTask.SetParentAssignment(actionItem, parentAssignment);
        Functions.ActionItemExecutionTask.SynchronizeDocumentGroupsToActionItem(actionItem, primaryDocument, addendaDocuments, otherAttachments);
        Functions.ActionItemExecutionTask.GrantAccessRightsForOtherAttachmentsToParticipants(actionItem);
        
        actionItem.WaitForParentAssignment = true;
        ((Domain.Shared.IExtendedEntity)actionItem).Params[PublicConstants.ActionItemExecutionTask.CheckDeadline] = true;
        
        actionItem.Save();
      }
    }
    
    /// <summary>
    /// Синхронизировать вложения задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="needAppendAddenda">Признак, что надо дополнить коллекции добавленных и удаленных вручную документов.</param>
    [Obsolete("Метод не используется с 26.04.2024 и версии 4.10. Синхронизация группы 'Приложения' осуществляется в событии 'Заполнение вложений' задачи.")]
    public virtual void SynchronizeAttachments(ITask task, bool needAppendAddenda)
    {
      var reviewTask = DocumentReviewTasks.As(task);
      if (reviewTask != null)
      {
        if (needAppendAddenda)
        {
          Functions.DocumentReviewTask.AddedAddendaAppend(reviewTask);
          Functions.DocumentReviewTask.RemovedAddendaAppend(reviewTask);
        }
        
        Functions.DocumentReviewTask.SynchronizeAddendaAndAttachmentsGroup(reviewTask);
        Functions.DocumentReviewTask.SynchronizeAddendaToDraftResolution(reviewTask);
        Functions.DocumentReviewTask.RelateAddedAddendaToPrimaryDocument(reviewTask);
      }
    }
    
    /// <summary>
    /// Проверить, что задача использует схему из no-code.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>True - если схема задачи задается через варианты процессов, иначе false.</returns>
    [Public, Remote]
    [Obsolete("Метод не используется с 16.02.2024 и версии 4.10, так как название не соответствует содержанию. Используйте метод IsTaskTypeUsingProcessKind(Sungero.Workflow.ITask task).")]
    public virtual bool IsTaskUsingProcessKind(Sungero.Workflow.ITask task)
    {
      return this.IsTaskTypeUsingProcessKind(task);
    }
    
    /// <summary>
    /// Проверить, что тип задачи использует схему из no-code.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>True - если схема задачи задается через варианты процессов, иначе false.</returns>
    [Public, Remote]
    public virtual bool IsTaskTypeUsingProcessKind(Sungero.Workflow.ITask task)
    {
      var taskGuid = task.GetEntityMetadata().GetOriginal().NameGuid;
      return new Sungero.Workflow.Server.UseSchemeFromSettingsGetter().Get(taskGuid);
    }
    
    #region Автоматическое создание проектов подчиненных поручений

    /// <summary>
    /// Создать АО для подготовки очереди обучения виртуального ассистента.
    /// </summary>
    /// <param name="assistantId">ИД ассистента.</param>
    /// <param name="periodBegin">Начало периода.</param>
    /// <param name="periodEnd">Конец периода.</param>
    /// <param name="processedItemsCount">Число поручений, обработанных в предыдущих итерациях.</param>
    /// <param name="actionItemMinId">Минимальный ИД поручения.</param>
    /// <param name="withStartNotification">Отобразить уведомление о старте обучения.</param>
    [Public, Remote]
    public virtual void CreatePrepareAIAssistantsTrainingAsyncHandler(long assistantId, DateTime? periodBegin, DateTime? periodEnd,
                                                                      int? processedItemsCount, long? actionItemMinId,
                                                                      bool withStartNotification)
    {
      var asyncHandler = RecordManagement.AsyncHandlers.PrepareAIAssistantsTraining.Create();
      asyncHandler.AssistantId = assistantId;
      asyncHandler.PeriodBegin = periodBegin.HasValue ? periodBegin.Value : Calendar.SqlMinValue;
      asyncHandler.PeriodEnd = periodEnd.HasValue ? periodEnd.Value : Calendar.Today.EndOfDay();
      asyncHandler.ActionItemMinId = actionItemMinId.HasValue ? actionItemMinId.Value : 0;
      asyncHandler.ProcessedItemsCount = processedItemsCount.HasValue && processedItemsCount.Value >= 0 ? processedItemsCount.Value : 0;

      if (withStartNotification)
        asyncHandler.ExecuteAsync(Resources.ClassifierTrainingPreparationStarted);
      else
        asyncHandler.ExecuteAsync();
    }
    
    /// <summary>
    /// Обучение классификатора для виртуального ассистента.
    /// </summary>
    /// <param name="classifierType">Тип классификатора.</param>
    public virtual void AIAssistantTrain(Enumeration classifierType)
    {
      var validTrainQueueItems = Functions.Module.GetVerifiedTrainQueueItems(
        ActionItemTrainQueueItems.GetAll(x => x.ProcessingStatus == RecordManagement.ActionItemTrainQueueItem.ProcessingStatus.Awaiting), classifierType);
      
      var logTemplate = string.Format("ClassifierTraining. AIAssistantTrain. {{0}}, classifierType={0}", classifierType);
      
      if (!validTrainQueueItems.Any())
      {
        Logger.DebugFormat(logTemplate, "Train queue items not found");
        return;
      }
      
      var groupedTrainQueueItems = validTrainQueueItems.OrderBy(x => x.ClassifierId.Value).GroupBy(x => x.ClassifierId.Value);
      foreach (var classGroup in groupedTrainQueueItems)
      {
        var classifierId = (int)classGroup.Key;
        var trainQueueItems = classGroup.OrderBy(x => x.Id).ToList();
        
        var assistantId = trainQueueItems
          .Where(x => x.AIManagersAssistantId.HasValue)
          .FirstOrDefault()?.AIManagersAssistantId;
        logTemplate += string.Format(", classifierId={0}, assistantId={1}", classifierId, assistantId);
        var assistant = Intelligence.AIManagersAssistants
          .GetAll(x => x.Id == assistantId && x.Classifiers.Any(c => c.ClassifierId == classifierId) && x.Status == Intelligence.AIManagersAssistant.Status.Active)
          .FirstOrDefault();
        if (assistant == null)
        {
          Logger.ErrorFormat(logTemplate, "AI assistant not found");
          continue;
        }
        
        var classifierInfo = assistant.Classifiers.Where(k => k.ClassifierId == classifierId).FirstOrDefault();
        if (classifierInfo == null)
        {
          Logger.ErrorFormat(logTemplate, "Classifier not found");
          continue;
        }
        
        var modelId = classifierInfo.ModelId;
        var classifierTrainingData = this.PrepareAIAssistantTrainingData(trainQueueItems, classifierId, !modelId.HasValue, classifierType);
        var logMessage = string.Format("Action items selected for train, totalCount={0}", classifierTrainingData.Count);
        Logger.DebugFormat(logTemplate, logMessage);
        if (!classifierTrainingData.Any())
          continue;
        
        // Отправка запроса в Ario.
        var binaryData = this.GetAIAssistantTrainingCsv(classifierTrainingData);
        var arioTaskInfo = SmartProcessing.PublicFunctions.Module.TrainClassifierAsync(classifierId, binaryData, modelId);
        if (arioTaskInfo == null || arioTaskInfo.State == SmartProcessing.PublicConstants.Module.ProcessingTaskStates.Terminated)
        {
          Logger.ErrorFormat(logTemplate, "Failed to send data to Ario services");
          continue;
        }
        
        var processingStatus = RecordManagement.ActionItemTrainQueueItem.ProcessingStatus.InProcess;
        if (arioTaskInfo.State == SmartProcessing.PublicConstants.Module.ProcessingTaskStates.ErrorOccurred)
        {
          Logger.ErrorFormat(logTemplate, "Ario task error");
          processingStatus = RecordManagement.ActionItemTrainQueueItem.ProcessingStatus.ErrorOccured;
        }
        
        var takenTrainQueueItems = classifierTrainingData.Select(x => x.ActionItemTrainQueueItem).ToList();
        foreach (var trainQueueItem in takenTrainQueueItems)
        {
          trainQueueItem.ArioTaskId = arioTaskInfo.Id;
          Functions.ActionItemTrainQueueItem.SetProcessedStatus(trainQueueItem, processingStatus);
        }
        
        if (arioTaskInfo.Id.HasValue)
        {
          var trainHandler = AsyncHandlers.WaitAndFinalizeAIAssistantTraining.Create();
          trainHandler.ArioTaskId = arioTaskInfo.Id.Value;
          trainHandler.ExecuteAsync();
        }
      }
    }
    
    /// <summary>
    /// Получить готовые к обучению элементы очереди на обучение классификатора исполнителей.
    /// </summary>
    /// <param name="trainQueueItems">Элементы очереди на обучение классификатора исполнителей.</param>
    /// <param name="classifierType">Тип классификатора.</param>
    /// <returns>Готовые к обучению элементы очереди.</returns>
    [Public]
    public List<IActionItemTrainQueueItem> GetVerifiedTrainQueueItems(System.Collections.Generic.IEnumerable<IActionItemTrainQueueItem> trainQueueItems, Enumeration classifierType)
    {
      var errorItems = new List<IActionItemTrainQueueItem>();
      
      var trainQueueAssistantIds = trainQueueItems
        .Where(x => x.AIManagersAssistantId.HasValue)
        .Select(x => x.AIManagersAssistantId.Value)
        .Distinct()
        .ToList();

      var virtualAssistants = Intelligence.AIManagersAssistants
        .GetAll(x => x.Status == CoreEntities.DatabookEntry.Status.Active)
        .Where(x => trainQueueAssistantIds.Contains(x.Id));
      
      var withoutAssistant = trainQueueItems
        .Where(x => !x.AIManagersAssistantId.HasValue || !virtualAssistants.Any(a => a.Id == x.AIManagersAssistantId.Value))
        .ToList();
      if (withoutAssistant.Any())
      {
        errorItems.AddRange(withoutAssistant);
        Logger.ErrorFormat("ClassifierTraining. VerifyTrainQueuItems. Virtual assistants not found, classifierType={0}, assistantIds: {1}",
                           classifierType, string.Join(", ", withoutAssistant.Where(x => x.AIManagersAssistantId != null).Select(x => x.AIManagersAssistantId).Distinct()));
      }

      var withoutClassifier = trainQueueItems
        .Where(x => !withoutAssistant.Contains(x))
        .Where(x => !x.ClassifierId.HasValue || !virtualAssistants.Any(a => a.Id == x.AIManagersAssistantId.Value && a.Classifiers
                                                                       .Any(c => c.ClassifierId == x.ClassifierId.Value && c.ClassifierType == classifierType)))
        .ToList();
      if (withoutClassifier.Any())
      {
        errorItems.AddRange(withoutClassifier);
        Logger.ErrorFormat("ClassifierTraining. VerifyTrainQueuItems. Classifiers not set for virtual assistants, classifierType={0}, assistantId(classifierId): {1}",
                           classifierType, string.Join(", ", withoutClassifier.Select(x => string.Format("({0}({1}))", x.AIManagersAssistantId, x.ClassifierId))));
      }
      
      // Элементы очереди на обучение, по которым текст не извлечен.
      var existExtractQueueItems = SmartProcessing.ExtractTextQueueItems.GetAll(x => trainQueueItems.Select(t => t.ExtractTextQueueItemId).Contains(x.Id));
      errorItems.AddRange(trainQueueItems.Where(x => !x.ExtractTextQueueItemId.HasValue || !existExtractQueueItems.Select(e => e.Id).Contains(x.ExtractTextQueueItemId.Value) ||
                                                existExtractQueueItems.Any(e => e.Id == x.ExtractTextQueueItemId &&
                                                                           (e.ProcessingStatus == SmartProcessing.ExtractTextQueueItem.ProcessingStatus.ErrorOccured ||
                                                                            (e.ProcessingStatus == SmartProcessing.ExtractTextQueueItem.ProcessingStatus.Success && e.ExtractedText.Size == 0)))));
      var awaitingForExtractText = trainQueueItems.Where(x => existExtractQueueItems.Any(e => e.Id == x.ExtractTextQueueItemId &&
                                                                                         (e.ProcessingStatus == SmartProcessing.ExtractTextQueueItem.ProcessingStatus.Awaiting ||
                                                                                          (e.ProcessingStatus == SmartProcessing.ExtractTextQueueItem.ProcessingStatus.InProcess)))).ToList();
      
      // Элементы очереди на обучение, по которым нет действующего исполнителя.
      var validActionItemTasks = ActionItemExecutionTasks.GetAll(x => trainQueueItems.Select(t => t.ActionItemId).Contains(x.Id) &&
                                                                 x.Status != Workflow.Task.Status.Aborted &&
                                                                 x.Assignee != null &&
                                                                 x.Assignee.Status != CoreEntities.DatabookEntry.Status.Closed);
      errorItems.AddRange(trainQueueItems.Where(x => !x.ActionItemId.HasValue || !validActionItemTasks.Select(t => t.Id).Contains(x.ActionItemId.Value)));
      
      errorItems = errorItems.Distinct().ToList();
      if (errorItems.Any())
        this.SetActionItemTrainQueueStatuses(errorItems, RecordManagement.ActionItemTrainQueueItem.ProcessingStatus.ErrorOccured);
      
      return trainQueueItems.Except(errorItems).Except(awaitingForExtractText).ToList();
    }
    
    /// <summary>
    /// Получить минимальное количество документов в обучающей выборке для публикации модели.
    /// </summary>
    /// <returns>Минимальное количество документов в обучающей выборке для публикации модели.</returns>
    [Public, Remote(IsPure = true)]
    public virtual int GetMinTrainingSetSizeForPublishingClassifierModelValue()
    {
      var limit = (int)Docflow.PublicFunctions.Module.Remote.GetDocflowParamsNumbericValue(Constants.Module.MinTrainingSetSizeToPublishClassifierModelParamName);
      if (limit <= 0)
        limit = Constants.Module.MinTrainingSetSizeToPublishClassifierModel;
      return limit;
    }
    
    /// <summary>
    /// Получить минимальное количество документов в обучающей выборке для публикации модели.
    /// </summary>
    /// <returns>Минимальное количество документов в обучающей выборке для публикации модели.</returns>
    [Public]
    [Obsolete("Метод не используется c версии 4.11. Используйте метод: GetMinTrainingSetSizeForPublishingClassifierModelValue().")]
    public virtual int GetMinTrainingSetSizeToPublishClassifierModel()
    {
      return this.GetMinTrainingSetSizeForPublishingClassifierModelValue();
    }
    
    /// <summary>
    /// Удалить элементы очереди на обучение, по которым завершена обработка.
    /// </summary>
    /// <remarks>Связанные элементы очереди на извлечение текста тоже удаляются.</remarks>
    public virtual void DeleteObsoleteTrainQueueItems()
    {
      var trainQueueItems = ActionItemTrainQueueItems.GetAll()
        .Where(x => x.ProcessingStatus == RecordManagement.ActionItemTrainQueueItem.ProcessingStatus.Success ||
               x.ProcessingStatus == RecordManagement.ActionItemTrainQueueItem.ProcessingStatus.ErrorOccured)
        .ToList();
      
      if (!trainQueueItems.Any())
        return;
      
      var extractTextQueueIds = trainQueueItems
        .Where(x => x.ExtractTextQueueItemId.HasValue)
        .Select(x => x.ExtractTextQueueItemId.Value)
        .Distinct();
      
      var logPrefix = "ClassifierTraining. DeleteObsoleteTrainQueueItems.";
      
      foreach (var item in trainQueueItems)
        if (!this.TryDeleteActionItemTrainQueueItem(item))
          Logger.DebugFormat("{0} Error deleting action item train queue item (ID={1})", logPrefix, item.Id);

      foreach (var queueItemId in extractTextQueueIds)
      {
        // Перед удалением элемента очереди на извлечение текста проверить, что он не используется в очереди на обучение.
        if (ActionItemTrainQueueItems.GetAll().Any(x => x.ExtractTextQueueItemId.HasValue && x.ExtractTextQueueItemId.Value == queueItemId))
          continue;
        
        if (!SmartProcessing.PublicFunctions.Module.TryDeleteExtractTextQueueItem(queueItemId))
          Logger.DebugFormat("{0} Error deleting extract text queue item (ID={1})", logPrefix, queueItemId);
      }
      
    }
    
    /// <summary>
    /// Удалить элемент очереди на обучение классификатора для поручений.
    /// </summary>
    /// <param name="item">Элемент очереди на обучение.</param>
    /// <returns>True - если удалось удалить, false - если при удалении возникла ошибка.</returns>
    public virtual bool TryDeleteActionItemTrainQueueItem(IActionItemTrainQueueItem item)
    {
      try
      {
        ActionItemTrainQueueItems.Delete(item);
        return true;
      }
      catch
      {
        return false;
      }
    }
    
    /// <summary>
    /// Поставить в очередь данные для обучения виртуальных ассистентов.
    /// </summary>
    /// <param name="periodBegin">Начало периода.</param>
    /// <param name="periodEnd">Конец периода.</param>
    /// <param name="classifierType">Тип классификатора.</param>
    [Public]
    public virtual void EnqueueActionItemsForAIAssistantTraining(DateTime periodBegin, DateTime periodEnd, Enumeration classifierType)
    {
      var actionItems = this.GetActionItemsForAIAssistantTraining(periodBegin, periodEnd, classifierType);
      if (!actionItems.Any())
        return;
      this.EnqueueActionItemsForAIAssistantTraining(actionItems, classifierType);
    }
    
    /// <summary>
    /// Поставить в очередь данные для обучения виртуальных ассистентов.
    /// </summary>
    /// <param name="actionItems">Список поручений.</param>
    /// <param name="classifierType">Тип классификатора.</param>
    /// <returns>Кол-во успешно поставленных в очередь данных.</returns>
    [Public]
    public virtual int EnqueueActionItemsForAIAssistantTraining(List<IActionItemExecutionTask> actionItems, Enumeration classifierType)
    {
      var queueItemsCount = 0;
      var managers = actionItems.Select(x => x.AssignedBy).Distinct().ToList();
      var managerAssistants = Intelligence.AIManagersAssistants.GetAll()
        .Where(x => managers.Contains(x.Manager) &&
               x.Status == Intelligence.AIManagersAssistant.Status.Active)
        .ToList();
      foreach (var assistant in managerAssistants)
      {
        var classifierId = assistant.Classifiers.FirstOrDefault(x => x.ClassifierType == classifierType)?.ClassifierId;
        if (!classifierId.HasValue)
          classifierId = Intelligence.PublicFunctions.AIManagersAssistant.CreateClassifier(assistant, classifierType);
        if (!classifierId.HasValue)
          continue;
        queueItemsCount += this.EnqueueActionItemsForAIAssistantTraining(assistant, classifierId.Value, actionItems).Count;
      }
      return queueItemsCount;
    }
    
    /// <summary>
    /// Поставить в очередь данные для обучения виртуального ассистента.
    /// </summary>
    /// <param name="assistant">Виртуальный ассистент.</param>
    /// <param name="classifierId">ИД классификатора для обучения.</param>
    /// <param name="actionItems">Список поручений.</param>
    /// <returns>Данные для обучения.</returns>
    [Public]
    public virtual List<IActionItemTrainQueueItem> EnqueueActionItemsForAIAssistantTraining(Intelligence.IAIManagersAssistant assistant,
                                                                                            int classifierId,
                                                                                            List<IActionItemExecutionTask> actionItems)
    {
      var result = new List<IActionItemTrainQueueItem>();

      // Для каждого поручения создать очереди на извлечение текста и на обучение классификатора.
      // Элементы очереди создавать в обратном порядке относительно ИД поручений, так как уходить
      // на извлечение текста и обучение первыми должны самые новые поручения.
      foreach (var task in actionItems.Where(x => Equals(x.AssignedBy, assistant.Manager)).OrderByDescending(x => x.Id))
      {
        var trainQueueItem = this.EnqueueActionItemsForAIAssistantTraining(task, assistant.Id, classifierId);
        if (trainQueueItem != null)
          result.Add(trainQueueItem);
      }
      return result;
    }
    
    /// <summary>
    /// Поставить в очередь данные для обучения виртуального ассистента по поручению.
    /// </summary>
    /// <param name="actionItemId">Ид поручения.</param>
    /// <param name="virtualAssistantId">Ид виртуального ассистента.</param>
    /// <param name="classifierId">ИД классификатора для обучения.</param>
    /// <returns>True при успешной обработке, иначе false.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual bool EnqueueActionItemsForAIAssistantTraining(long actionItemId, long virtualAssistantId, int classifierId)
    {
      var actionItem = ActionItemExecutionTasks.GetAll(x => x.Id == actionItemId).FirstOrDefault();
      if (actionItem == null)
        return false;
      
      return this.EnqueueActionItemsForAIAssistantTraining(actionItem, virtualAssistantId, classifierId) != null;
    }
    
    /// <summary>
    /// Поставить в очередь данные для обучения виртуального ассистента по поручению.
    /// </summary>
    /// <param name="actionItem">Поручение.</param>
    /// <param name="virtualAssistantId">Ид виртуального ассистента.</param>
    /// <param name="classifierId">ИД классификатора для обучения.</param>
    /// <returns>Данные для обучения.</returns>
    public virtual IActionItemTrainQueueItem EnqueueActionItemsForAIAssistantTraining(IActionItemExecutionTask actionItem, long virtualAssistantId, int classifierId)
    {
      var document = actionItem.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document == null || !document.HasVersions)
        return ActionItemTrainQueueItems.Null;

      try
      {
        var documentVersionNumber = document.LastVersion.Number.Value;
        var extractTextQueueItem = SmartProcessing.PublicFunctions.Module.GetOrCreateExtractTextQueueItem(document.Id, documentVersionNumber);
        return this.GetOrCreateActionItemTrainQueueItem(actionItem.Id, virtualAssistantId, classifierId, extractTextQueueItem.Id);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("ClassifierTraining. EnqueueActionItemsForAIAssistantTraining. Error while creating training query, taskId={0}, assistantId={1}, documentId={2}",
                           ex, actionItem.Id, virtualAssistantId, document.Id);
        return ActionItemTrainQueueItems.Null;
      }
    }
    
    /// <summary>
    /// Создать элемент очереди обучения классификатора для поручений.
    /// </summary>
    /// <param name="actionItemId">ИД поручения.</param>
    /// <param name="virtualAssistantId">ИД виртуального ассистента.</param>
    /// <param name="classifierId">ИД классификатора.</param>
    /// <param name="extractTextQueueItemId">ИД элемента очереди на извлечение текста.</param>
    /// <returns>Элемент очереди обучения классификатора для поручений.</returns>
    public virtual IActionItemTrainQueueItem GetOrCreateActionItemTrainQueueItem(long actionItemId, long virtualAssistantId,
                                                                                 int classifierId, long extractTextQueueItemId)
    {
      var result = ActionItemTrainQueueItems.GetAll()
        .Where(x => x.ActionItemId == actionItemId && x.AIManagersAssistantId == virtualAssistantId && x.ClassifierId == classifierId)
        .FirstOrDefault();
      if (result == null)
      {
        result = RecordManagement.ActionItemTrainQueueItems.Create();
        result.ActionItemId = actionItemId;
        result.AIManagersAssistantId = virtualAssistantId;
        result.ClassifierId = classifierId;
      }
      result.ProcessingStatus = RecordManagement.ActionItemTrainQueueItem.ProcessingStatus.Awaiting;
      result.ExtractTextQueueItemId = extractTextQueueItemId;
      result.Save();
      return result;
    }
    
    /// <summary>
    /// Получить список поручений для обучения виртуального ассистента.
    /// </summary>
    /// <param name="periodBegin">Начало периода.</param>
    /// <param name="periodEnd">Конец периода.</param>
    /// <param name="classifierType">Тип классификатора.</param>
    /// <returns>Список задач на исполнение поручений.</returns>
    public virtual List<IActionItemExecutionTask> GetActionItemsForAIAssistantTraining(DateTime periodBegin, DateTime periodEnd, Enumeration classifierType)
    {
      if (classifierType == Intelligence.AIManagersAssistantClassifiers.ClassifierType.Assignee)
        return this.GetActionItemsForAssigneeClassifierTraining(periodBegin, periodEnd);
      
      return new List<IActionItemExecutionTask>();
    }
    
    /// <summary>
    /// Получить список ИД поручений для обучения виртуального ассистента.
    /// </summary>
    /// <param name="virtualAssistantId">ИД виртуального ассистента.</param>
    /// <param name="periodBegin">Начало периода.</param>
    /// <param name="periodEnd">Конец периода.</param>
    /// <param name="maxItemsCount">Максимальное количество отбираемых поручений.</param>
    /// <returns>Список ИД задач на исполнение поручений.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual List<long> GetActionItemsIdsForAIAssistantTraining(long virtualAssistantId, DateTime? periodBegin, DateTime? periodEnd, int? maxItemsCount)
    {
      // Если начало периода и количество поручений не указаны - отбирать последние поручения в количестве, необходимом для публикации модели классификатора.
      if (!maxItemsCount.HasValue && !periodBegin.HasValue)
        maxItemsCount = this.GetMinTrainingSetSizeForPublishingClassifierModelValue();
      
      if (!periodBegin.HasValue)
        periodBegin = Calendar.SqlMinValue;
      
      if (!periodEnd.HasValue)
        periodEnd = Calendar.Now;
      
      var virtualAssistant = Intelligence.AIManagersAssistants.GetAll(x => x.Id == virtualAssistantId).FirstOrDefault();
      if (virtualAssistant == null)
      {
        Logger.DebugFormat("ClassifierTraining. GetActionItemsIdsForAIAssistantTraining. AI assistant not found. virtualAssistantId={0}", virtualAssistantId);
        return new List<long>();
      }
      var actionItems = this.GetActionItemsForAssigneeClassifierTraining(periodBegin.Value, periodEnd.Value, virtualAssistant.Manager, maxItemsCount, null);
      return actionItems.Any() ? actionItems.Select(x => x.Id).ToList() : new List<long>();
    }

    /// <summary>
    /// Получить список поручений для обучения классификатора по ответственным исполнителям.
    /// </summary>
    /// <param name="periodBegin">Начало периода.</param>
    /// <param name="periodEnd">Конец периода.</param>
    /// <param name="assignedBy">Руководитель, выдавший поручения.</param>
    /// <returns>Список задач на исполнение поручений.</returns>
    public virtual List<IActionItemExecutionTask> GetActionItemsForAssigneeClassifierTraining(DateTime periodBegin, DateTime periodEnd, IEmployee assignedBy = null)
    {
      return this.GetActionItemsForAssigneeClassifierTraining(periodBegin, periodEnd, assignedBy, null, null);
    }

    /// <summary>
    /// Получить список поручений для обучения классификатора по ответственным исполнителям.
    /// </summary>
    /// <param name="periodBegin">Начало периода.</param>
    /// <param name="periodEnd">Конец периода.</param>
    /// <param name="assignedBy">Руководитель, выдавший поручения.</param>
    /// <param name="maxItemsCount">Максимальное количество отбираемых поручений.</param>
    /// <param name="actionItemMinId">Минимальный ИД поручения.</param>
    /// <returns>Список задач на исполнение поручений.</returns>
    public virtual List<IActionItemExecutionTask> GetActionItemsForAssigneeClassifierTraining(DateTime periodBegin, DateTime periodEnd,
                                                                                              IEmployee assignedBy, int? maxItemsCount,
                                                                                              long? actionItemMinId)
    {
      var result = new List<IActionItemExecutionTask>();
      var isLimited = maxItemsCount.HasValue;

      if (isLimited && maxItemsCount.Value <= 0)
        return result;
      
      var minId = actionItemMinId.HasValue ? actionItemMinId.Value : 0L;
      
      // Получить выполненные задания по поручениям, выданным руководителями с вирт. помощниками.
      var managers = Intelligence.AIManagersAssistants.GetAll()
        .Where(x => x.Status == Intelligence.AIManagersAssistant.Status.Active)
        .Where(x => assignedBy == null || Equals(x.Manager, assignedBy))
        .Select(x => x.Manager)
        .ToList();

      var assignments = ActionItemExecutionAssignments.GetAll()
        .Where(x => managers.Contains(x.AssignedBy) &&
               (x.Status == Workflow.AssignmentBase.Status.InProcess || x.Status == Workflow.AssignmentBase.Status.Completed) &&
               x.Created.Between(periodBegin, periodEnd) &&
               ActionItemExecutionTasks.Is(x.Task) &&
               ActionItemExecutionTasks.As(x.Task).ActionItemType == ActionItemType.Main);

      // Поручения для подготовки получать в обратном порядке по ИД, так как уходить
      // на извлечение текста и обучение первыми должны самые новые поручения.
      assignments = assignments.OrderByDescending(x => x.Task.Id);
      
      if (minId > 0)
        assignments = assignments.Where(x => x.Task.Id < minId);

      // Проверить для каждого поручения условия включения в обучающий набор.
      foreach (var assignment in assignments.ToList())
      {
        var actionItem = ActionItemExecutionTasks.As(assignment.Task);
        
        // Не брать для обучения поручение, если у документа отсутствуют версии или он зашифрован.
        var document = actionItem.DocumentsGroup.OfficialDocuments.FirstOrDefault();
        if (document == null || !document.HasVersions || document.IsEncrypted == true)
          continue;
        
        // Не брать для обучения поручение, отправленное на доработку, если ранее уже выполнялись задания по текущей задаче.
        if (ActionItemExecutionAssignments.GetAll()
            .Where(x => Equals(x.Task, actionItem) && x.Status == Workflow.AssignmentBase.Status.Completed && x.Completed < periodBegin)
            .Any())
          continue;
        
        // Не брать в обучение, если по документу было создано несколько подченных поручений из одного задания.
        var documentsGroupGuid = Docflow.PublicConstants.Module.TaskMainGroup.ActionItemExecutionTask;
        if (actionItem.ParentAssignment != null &&
            ActionItemExecutionTasks.GetAll()
            .Any(x => Equals(x.ParentAssignment, actionItem.ParentAssignment) &&
                 !Equals(x, actionItem) &&
                 x.ActionItemType == ActionItemType.Main &&
                 x.AttachmentDetails.Any(a => a.GroupId == documentsGroupGuid && a.AttachmentId == document.Id) &&
                 (x.Status == RecordManagement.ActionItemExecutionTask.Status.Completed ||
                  x.Status == RecordManagement.ActionItemExecutionTask.Status.InProcess)))
          continue;
        
        if (!result.Contains(actionItem))
          result.Add(actionItem);
        
        if (isLimited && result.Count >= maxItemsCount.Value)
          break;
      }
      return result;
    }
    
    /// <summary>
    /// Получить время последней обработки очереди обучения классификатора для поручений.
    /// </summary>
    /// <returns>Значение.</returns>
    public virtual DateTime? GetLastActionItemTrainQueueDate()
    {
      var lastRunString = Docflow.PublicFunctions.Module.Remote.GetDocflowParamsStringValue(Constants.Module.LastActionItemTrainQueueDateParamName);
      if (!string.IsNullOrEmpty(lastRunString))
        return DateTime.Parse(lastRunString, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
      return null;
    }
    
    /// <summary>
    /// Установить время последней обработки очереди обучения классификатора для поручений.
    /// </summary>
    /// <param name="lastRun">Дата и время последней обработки.</param>
    public virtual void SetLastActionItemTrainQueueDate(DateTime? lastRun)
    {
      var lastRunString = lastRun.HasValue ? lastRun.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffff+0") : string.Empty;
      Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.LastActionItemTrainQueueDateParamName, lastRunString);
    }
    
    /// <summary>
    /// Очистить время последней обработки очереди обучения классификатора для поручений.
    /// </summary>
    public static void ClearLastActionItemTrainQueueDate()
    {
      var lastTrainDate = Functions.Module.GetLastActionItemTrainQueueDate();
      if (lastTrainDate.HasValue)
        Functions.Module.SetLastActionItemTrainQueueDate(null);
    }
    
    /// <summary>
    /// Подготовить и получить данные для обучения классификатора виртуальных помощников.
    /// </summary>
    /// <param name="trainQueueItems">Элементы очереди обучения классификатора для поручений.</param>
    /// <param name="classifierId">Ид классификатора.</param>
    /// <param name="isFirstTraining">Первичное обучение.</param>
    /// <param name="classifierType">Тип классификатора.</param>
    /// <returns>Данные для обучения классификатора виртуальных помощников.</returns>
    public virtual List<Structures.Module.IAIAssistantTrainingData> PrepareAIAssistantTrainingData(
      List<IActionItemTrainQueueItem> trainQueueItems, int classifierId, bool isFirstTraining, Enumeration classifierType)
    {
      var logPrefix = "ClassifierTraining. PrepareAIAssistantTrainingData.";
      var logPostfix = string.Format(", classifierType={0}", classifierType);
      
      var actionItemsTrainingData = this.GetAIAssistantTrainingData(trainQueueItems, classifierId);
      
      // Выставить статус ошибки непригодным для обучения элементам очереди обучения.
      var errorItems = actionItemsTrainingData.Where(x => x.HasError).Select(x => x.ActionItemTrainQueueItem).ToList();
      if (errorItems.Any())
        this.SetActionItemTrainQueueStatuses(errorItems, RecordManagement.ActionItemTrainQueueItem.ProcessingStatus.ErrorOccured);
      
      actionItemsTrainingData = actionItemsTrainingData.Where(x => x.SentForTraining).ToList();
      if (actionItemsTrainingData.Count < Constants.Module.MinItemsInClassFirstTrainingCount)
      {
        Logger.DebugFormat("{0} Minimum count of training data not reached, classifierId={1}, minTrainingItemsCount={2}{3}",
                           logPrefix, classifierId, Constants.Module.MinItemsInClassFirstTrainingCount, logPostfix);
        actionItemsTrainingData.Clear();
      }

      if (isFirstTraining && actionItemsTrainingData.Any())
      {
        // Проверить кол-во классов для первичного обучения.
        var groupedClasses = actionItemsTrainingData.GroupBy(x => x.AssigneeId);
        var excludedClasses = groupedClasses.Where(x => x.Count() < Constants.Module.MinItemsInClassFirstTrainingCount);
        if (excludedClasses.Any())
        {
          var classesIds = excludedClasses.Select(x => x.Key);
          Logger.DebugFormat("{0} Classes with Id {1} excluded by document count, classifierId={2}, minClassSize={3}{4}",
                             logPrefix, string.Join(",", classesIds), classifierId, Constants.Module.MinItemsInClassFirstTrainingCount, logPostfix);
          groupedClasses = groupedClasses.Where(x => !classesIds.Contains(x.Key)).ToList();
        }

        if (groupedClasses.Count() < Constants.Module.MinClassesFirstTrainingCount)
        {
          Logger.DebugFormat("{0} Minimum count of training classes not reached, classifierId={1}, minClassesForTrainingCount={2}{3}",
                             logPrefix, classifierId, Constants.Module.MinClassesFirstTrainingCount, logPostfix);
          return new List<Structures.Module.IAIAssistantTrainingData>();
        }
        actionItemsTrainingData = groupedClasses.SelectMany(x => x).ToList();
      }
      return actionItemsTrainingData;
    }
    
    /// <summary>
    /// Сформировать структуру данных для обучения классификатора поручений.
    /// </summary>
    /// <param name="awaitingActionItemTrainQueueItems">Элементы очереди обучения классификатора.</param>
    /// <param name="classifierID">ИД текущего классификатора.</param>
    /// <returns>Данные для обучения классификатора.</returns>
    [Public]
    public virtual List<Structures.Module.IAIAssistantTrainingData> GetAIAssistantTrainingData(List<IActionItemTrainQueueItem> awaitingActionItemTrainQueueItems, int classifierID)
    {
      long sizeLimit = Sungero.SmartProcessing.PublicFunctions.Module.GetCsvTrainingDatasetLimitBytes();
      long datasetSize = 0;
      var serialNumber = 0;
      var actionItemsTrainingData = new List<Structures.Module.IAIAssistantTrainingData>();
      foreach (var actionItemTrainQueueItem in awaitingActionItemTrainQueueItems)
      {
        var trainingDataItem = Structures.Module.AIAssistantTrainingData.Create();
        trainingDataItem.SerialNumber = serialNumber;
        trainingDataItem.ActionItemTrainQueueItem = actionItemTrainQueueItem;
        trainingDataItem.SentForTraining = false;
        trainingDataItem.HasError = false;
        
        if (!actionItemsTrainingData.Any(x => x.ActionItemTrainQueueItem.AIManagersAssistantId == actionItemTrainQueueItem.AIManagersAssistantId &&
                                         x.ActionItemTrainQueueItem.ClassifierId == actionItemTrainQueueItem.ClassifierId &&
                                         x.ActionItemTrainQueueItem.ActionItemId == actionItemTrainQueueItem.ActionItemId))
        {
          try
          {
            var extractTextQueueItem = Sungero.SmartProcessing.ExtractTextQueueItems.GetAll(x => x.Id == actionItemTrainQueueItem.ExtractTextQueueItemId.Value).First();
            
            using (var reader = new StreamReader(extractTextQueueItem.ExtractedText.Read(), System.Text.Encoding.UTF8))
              trainingDataItem.Text = SmartProcessing.PublicFunctions.Module.FilterCharactersForTraining(reader.ReadToEnd());
            
            trainingDataItem.AssigneeId = ActionItemExecutionTasks.GetAll(x => x.Id == actionItemTrainQueueItem.ActionItemId.Value).First().Assignee.Id;
            // Сформировать строку по формату CSV для Ario.
            var csvLine = Sungero.SmartProcessing.PublicFunctions.Module.GetFormattedTextForTrainingDataset(serialNumber,
                                                                                                            trainingDataItem.AssigneeId.ToString(),
                                                                                                            trainingDataItem.Text);
            var size = datasetSize + System.Text.Encoding.Default.GetByteCount(csvLine);
            if (size <= sizeLimit)
            {
              trainingDataItem.SentForTraining = true;
              datasetSize = size;
            }
          }
          catch (Exception ex)
          {
            Logger.Error("ClassifierTraining. GetAIAssistantTrainingData. Error while get training data, actionItemTrainQueueItemId={0}", ex, actionItemTrainQueueItem.Id);
            trainingDataItem.HasError = true;
          }
        }
        else
        {
          // Исключать дубли очередей (гипотетически могут возникнуть при одновременном запуске исторического обучения ВА через UI и утилиту rxcmd).
          Logger.DebugFormat("ClassifierTraining. GetAIAssistantTrainingData. Double training queue item found, AIManagersAssistantId={0}, ClassifierId={1}, ActionItemId={2}",
                             actionItemTrainQueueItem.AIManagersAssistantId, actionItemTrainQueueItem.ClassifierId, actionItemTrainQueueItem.ActionItemId);
          trainingDataItem.HasError = true;
        }
        
        serialNumber++;
        actionItemsTrainingData.Add(trainingDataItem);
      }
      
      return actionItemsTrainingData;
    }

    /// <summary>
    /// Сформировать CSV-файл для обучения.
    /// </summary>
    /// <param name="actionItemsTrainingData">Данные для обучения классификатора.</param>
    /// <returns>CSV-файл для обучения.</returns>
    [Public]
    public virtual byte[] GetAIAssistantTrainingCsv(List<Structures.Module.IAIAssistantTrainingData> actionItemsTrainingData)
    {
      if (!actionItemsTrainingData.Any())
        return null;
      const string DatasetHeader = "Id,Category,Text";
      var trainingDataset = new System.Text.StringBuilder();
      var result = new byte[0];
      
      trainingDataset.AppendLine(DatasetHeader);
      foreach (var actionItemsTrainingDataItem in actionItemsTrainingData)
      {
        var csvLine = Sungero.SmartProcessing.PublicFunctions.Module.GetFormattedTextForTrainingDataset(actionItemsTrainingDataItem.SerialNumber,
                                                                                                        actionItemsTrainingDataItem.AssigneeId.ToString(),
                                                                                                        actionItemsTrainingDataItem.Text);
        trainingDataset.AppendLine(csvLine);
      }
      
      result = System.Text.Encoding.UTF8.GetBytes(trainingDataset.ToString());
      return result;
    }
    
    /// <summary>
    /// Попытка завершить элементы очереди обучения классификатора для поручений со статусом "В процессе".
    /// </summary>
    public virtual void TryFinalizeTrainQueueItemsInProcess()
    {
      var trainQueueItemsInProcess = ActionItemTrainQueueItems.GetAll(x => x.ProcessingStatus == RecordManagement.ActionItemTrainQueueItem.ProcessingStatus.InProcess &&
                                                                      x.ArioTaskId.HasValue &&
                                                                      x.AIManagersAssistantId.HasValue).ToList();
      if (!trainQueueItemsInProcess.Any())
        return;

      var arioSettings = Docflow.PublicFunctions.SmartProcessingSetting.GetSettings();
      if (!Docflow.PublicFunctions.SmartProcessingSetting.Remote.CheckConnection(arioSettings))
      {
        Logger.ErrorFormat("ClassifierTraining. AssistantClassifierTraining. TryFinalizeTrainQueueItemsInProcess. Ario services are not available.");
        this.SetActionItemTrainQueueStatuses(trainQueueItemsInProcess, RecordManagement.ActionItemTrainQueueItem.ProcessingStatus.Awaiting);
        return;
      }
      
      var queueItemsTasks = trainQueueItemsInProcess.GroupBy(x => x.ArioTaskId.Value);
      foreach (var arioTaskByQueueItems in queueItemsTasks)
      {
        var trainTask = SmartProcessing.PublicFunctions.Module.GetArioTrainingTask(arioTaskByQueueItems.Key);
        this.FinalizeTraining(trainTask, arioTaskByQueueItems.ToList());
      }
    }
    
    /// <summary>
    /// Завершить элементы очереди обучения классификатора для поручений.
    /// </summary>
    /// <param name="trainTask">Задача на обучение.</param>
    /// <param name="trainQueueItems">Элементы очереди обучения классификатора для поручений.</param>
    /// <returns>Обработка завершена.</returns>
    public virtual bool FinalizeTraining(Sungero.SmartProcessing.Structures.Module.IArioTaskInfo trainTask, List<IActionItemTrainQueueItem> trainQueueItems)
    {
      var logPrefix = "ClassifierTraining. FinalizeTraining.";
      if (trainTask.State == SmartProcessing.PublicConstants.Module.ProcessingTaskStates.Terminated ||
          (trainTask.State == SmartProcessing.PublicConstants.Module.ProcessingTaskStates.ErrorOccurred && trainTask.ErrorMessage == "Aborted after restart."))
      {
        Logger.ErrorFormat("{0} Ario task terminated, arioTaskId={1}", logPrefix, trainTask.Id);
        this.SetActionItemTrainQueueStatuses(trainQueueItems, RecordManagement.ActionItemTrainQueueItem.ProcessingStatus.Awaiting);
        return true;
      }

      if (trainTask.State == SmartProcessing.PublicConstants.Module.ProcessingTaskStates.ErrorOccurred)
      {
        Logger.ErrorFormat("{0} Ario service error, errorMessage={1}, arioTaskId={2}", logPrefix, trainTask.ErrorMessage, trainTask.Id);
        this.SetActionItemTrainQueueStatuses(trainQueueItems, RecordManagement.ActionItemTrainQueueItem.ProcessingStatus.ErrorOccured);
        return true;
      }

      if (trainTask.State == SmartProcessing.PublicConstants.Module.ProcessingTaskStates.Completed)
      {
        var modelPublished = false;
        var classifierId = trainTask.ClassifierTrainingModel.ClassifierId;
        var modelId = trainTask.ClassifierTrainingModel.TrainedModelId;
        if (modelId == trainTask.ClassifierTrainingModel.PublishedModelId)
          modelPublished = true;
        else
        {
          var trainDocumentsCount = trainTask.ClassifierTrainingModel.TrainSetCount;
          if (trainDocumentsCount >= Functions.Module.GetMinTrainingSetSizeForPublishingClassifierModelValue())
          {
            try
            {
              Logger.DebugFormat("{0} Start to publish classifier model, classifierId={1}, modelId={2}", logPrefix, classifierId, modelId);
              SmartProcessing.PublicFunctions.Module.PublishClassifierModel(classifierId, modelId);
              modelPublished = true;
              Logger.DebugFormat("{0} Model published successfully, classifierId={1}, modelId={2}", logPrefix, classifierId, modelId);
            }
            catch (Exception ex)
            {
              Logger.ErrorFormat("{0} Failed to publish classifier model, classifierId={1}, trainedModelId={2}, arioTaskId={3}", ex.Message, logPrefix, classifierId, modelId, trainTask.Id);
              return false;
            }
          }
          else
            Logger.DebugFormat("{0} Not enough documents to publish model, trainDocumentsCount={1}, classifierId={2}, trainedModelId={3}", logPrefix, trainDocumentsCount, classifierId, modelId);
        }
        
        var virtualAssistantId = trainQueueItems.First()?.AIManagersAssistantId;
        var virtualAssistant = Intelligence.AIManagersAssistants.GetAll(x => x.Id == virtualAssistantId).FirstOrDefault();
        try
        {
          var classifier = virtualAssistant.Classifiers.FirstOrDefault(x => x.ClassifierId == classifierId);
          classifier.ModelId = modelId;
          if (modelPublished)
            classifier.IsModelActive = Intelligence.AIManagersAssistantClassifiers.IsModelActive.Yes;
          virtualAssistant.Save();
        }
        catch (Domain.Shared.Exceptions.RepeatedLockException)
        {
          Logger.DebugFormat("{0} AIManagerAssistant is locked, AIManagerAssistantId={1}, arioTaskId={2}", logPrefix, virtualAssistantId, trainTask.Id);
          return false;
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("{0} Failed to set ModelId for AIManagerAssistant, classifierId={1}, AIManagerAssistantId={2}, trainedModelId={3}, arioTaskId={4}",
                             ex, logPrefix, classifierId, virtualAssistantId, modelId, trainTask.Id);
          this.SetActionItemTrainQueueStatuses(trainQueueItems, RecordManagement.ActionItemTrainQueueItem.ProcessingStatus.ErrorOccured);
          return true;
        }
        
        this.SetActionItemTrainQueueStatuses(trainQueueItems, RecordManagement.ActionItemTrainQueueItem.ProcessingStatus.Success);
        Logger.DebugFormat("{0} Training is successfully completed, classifierId={1}, AIManagerAssistantId={2}, trainedModelId={3}, arioTaskId={4}",
                           logPrefix, classifierId, virtualAssistantId, modelId, trainTask.Id);
        return true;
      }
      
      return false;
    }
    
    /// <summary>
    /// Установить статусы элементам очереди обучения классификатора для поручений.
    /// </summary>
    /// <param name="queueItems">Элементы очереди обучения классификатора для поручений.</param>
    /// <param name="status">Статус.</param>
    public virtual void SetActionItemTrainQueueStatuses(List<IActionItemTrainQueueItem> queueItems, Enumeration? status)
    {
      foreach (var queuItem in queueItems)
        Functions.ActionItemTrainQueueItem.SetProcessedStatus(queuItem, status);
    }
    
    /// <summary>
    /// Получить элементы очереди на извлечение текста для обучения классификатора исполнителей.
    /// </summary>
    /// <param name="processingStatus">Статус обработки.</param>
    /// <returns>Элементы очереди на извлечение текста.</returns>
    [Public]
    public virtual IQueryable<Sungero.SmartProcessing.IExtractTextQueueItem> GetExtractTextQueueItemsForAssistant(Enumeration processingStatus)
    {
      return SmartProcessing.PublicFunctions.Module.GetExtractTextQueueItems(processingStatus)
        .Where(x => ActionItemTrainQueueItems.GetAll()
               .Any(ai => ai.ExtractTextQueueItemId.HasValue && ai.ExtractTextQueueItemId.Value == x.Id));
    }
    
    /// <summary>
    /// Проверить статус задачи классификации документа в Ario.
    /// </summary>
    /// <param name="taskId">Ид задачи.</param>
    /// <param name="taskType">Тип задачи.</param>
    /// <returns>Признак того, что задача по обработке завершена.</returns>
    public virtual bool CheckArioTasksStatus(long taskId, Sungero.Core.Enumeration taskType)
    {
      var predictionInfo = ActionItemPredictionInfos.GetAll()
        .Where(x => x.TaskId == taskId && x.TaskType == taskType)
        .FirstOrDefault();

      if (predictionInfo == null)
      {
        Logger.DebugFormat("{0}Task (ID={1}). WaitArioProcessingBlockResult. Action item prediction item not found",
                           taskType, taskId);
        return true;
      }
      
      // Если ИД задачи Ario не заполнен, значит при отправке возникла ошибка. Логгирование в событии "Старт блока".
      if (!predictionInfo.ArioTaskId.HasValue)
        return true;
      
      if (!predictionInfo.ArioTaskStatus.HasValue)
      {
        // При первом запуске заполнить статус задачи Ario.
        predictionInfo.ArioTaskStatus = RecordManagement.ActionItemPredictionInfo.ArioTaskStatus.InProcess;
        return !Functions.ActionItemPredictionInfo.TrySave(predictionInfo);
      }

      if (predictionInfo.ArioTaskStatus.Value != RecordManagement.ActionItemPredictionInfo.ArioTaskStatus.InProcess)
        return true;
      
      var arioTaskInfo = SmartProcessing.PublicFunctions.Module.GetArioTaskInfo(predictionInfo.ArioTaskId.Value);

      if (arioTaskInfo.State == SmartProcessing.PublicConstants.Module.ProcessingTaskStates.InWork)
        return false;
      
      if (arioTaskInfo.State == SmartProcessing.PublicConstants.Module.ProcessingTaskStates.ErrorOccurred)
      {
        if (!string.IsNullOrEmpty(arioTaskInfo.ErrorMessage) || string.IsNullOrEmpty(arioTaskInfo.ResultJson))
          Logger.DebugFormat("{0}Task (ID={1}). WaitArioProcessingBlockResult. Error while getting result from Ario, arioTaskId={2}, errorMessage={3}",
                             taskType, taskId,  arioTaskInfo.Id, arioTaskInfo.ErrorMessage);
        predictionInfo.ArioTaskStatus = RecordManagement.ActionItemPredictionInfo.ArioTaskStatus.ErrorOccured;
        Functions.ActionItemPredictionInfo.TrySave(predictionInfo);
        return true;
      }
      
      predictionInfo.ArioResultJson = arioTaskInfo.ResultJson;
      predictionInfo.ArioTaskStatus = RecordManagement.ActionItemPredictionInfo.ArioTaskStatus.Success;
      Functions.ActionItemPredictionInfo.TrySave(predictionInfo);
      
      return true;
    }
    
    /// <summary>
    /// Получить проект подчиненного поручения, созданный для задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="perfomer">Исполнитель.</param>
    /// <param name="taskType">Тип задачи.</param>
    /// <returns>Проект подчиненного поручения.</returns>
    [Public]
    public virtual IActionItemExecutionTask GetDraftActionItemForTask(ITask task, IUser perfomer, Sungero.Core.Enumeration taskType)
    {
      // Получить проект подчиненного поручения из результатов предсказания поручений.
      var predictionInfo = ActionItemPredictionInfos.GetAll()
        .FirstOrDefault(x => x.TaskId == task.Id
                        && x.TaskType == taskType
                        && x.ActionItemId.HasValue);
      if (predictionInfo == null)
        return null;
      var draftActionItem = ActionItemExecutionTasks.GetAll()
        .Where(x => x.Id == predictionInfo.ActionItemId.Value &&
               x.Status == RecordManagement.ActionItemExecutionTask.Status.Draft)
        .FirstOrDefault();
      if (draftActionItem == null)
        return null;
      
      var isManagerAssistantPerformer = false;
      if (taskType == RecordManagement.ActionItemPredictionInfo.TaskType.DocumentReview)
      {
        var reviewTask = RecordManagement.DocumentReviewTasks.As(task);
        var performerEmployee = Company.Employees.As(perfomer);
        if (reviewTask != null && performerEmployee != null)
        {
          var assistants = Company.PublicFunctions.Employee.GetManagerAssistantsWhoPrepareDraftResolution(reviewTask.Addressee)
            .Select(x => x.Assistant);
          isManagerAssistantPerformer = assistants.Contains(performerEmployee);
        }
      }
      
      // Проверить, что проект подчиненного поручения создан для текущего исполнителя, или его помощника (для рассмотрения).
      if (!Equals(draftActionItem.AssignedBy, perfomer) && !isManagerAssistantPerformer)
        return null;
      
      return draftActionItem;
    }

    /// <summary>
    /// Отправить документ на классификацию по исполнителю.
    /// </summary>
    /// <param name="taskId">Ид родительской задачи.</param>
    /// <param name="taskType">Тип задачи.</param>
    /// <param name="document">Документ.</param>
    /// <param name="assignee">Адресат.</param>
    /// <returns>Текст ошибки, или пустая строка в случае успеха.</returns>
    [Public]
    public virtual string SendDocumentForAssigneeClassification(long taskId, Sungero.Core.Enumeration taskType, IOfficialDocument document, IEmployee assignee)
    {
      var predictionInfo = ActionItemPredictionInfos.GetAll().FirstOrDefault(x => x.TaskId == taskId && x.TaskType == taskType);
      if (predictionInfo == null)
      {
        predictionInfo = ActionItemPredictionInfos.Create();
        predictionInfo.TaskId = taskId;
        predictionInfo.TaskType = taskType;
      }
      else
        RecordManagement.PublicFunctions.ActionItemPredictionInfo.RemoveArioTaskInfoAndActionItemDraft(predictionInfo);
      
      var errorMessage = string.Empty;
      
      var assistant = this.GetAiAssistantToPrepareActionItems(assignee, taskType);
      if (document != null && assistant != null && assistant.Classifiers.Any())
      {
        predictionInfo.AIManagerAssistant = assistant;
        try
        {
          var classifierIds = assistant.Classifiers
            .Where(x => x.ClassifierId.HasValue)
            .Select(x => x.ClassifierId.Value)
            .ToList();
          var arioTask = SmartProcessing.PublicFunctions.Module.ClassifyDocumentAsync(document, classifierIds);

          if (arioTask.Id.HasValue && arioTask.Id.Value > 0)
            predictionInfo.ArioTaskId = arioTask.Id.Value;
          else
          {
            errorMessage = string.Format("{0} classifierIds={1}, documentId={2}",
                                         arioTask.ErrorMessage, string.Join(",", classifierIds), document.Id);
          }
        }
        catch (Exception ex)
        {
          errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        }
      }
      else
      {
        errorMessage = "No data found for classification in Ario";
      }
      
      if (!string.IsNullOrEmpty(errorMessage))
      {
        predictionInfo.ArioTaskStatus = RecordManagement.ActionItemPredictionInfo.ArioTaskStatus.ErrorOccured;
        Logger.DebugFormat("{0}Task (ID={1}). WaitArioProcessingBlockStart. Error while sending document to Ario: {2}",
                           taskType, taskId, errorMessage);
      }
      
      Functions.ActionItemPredictionInfo.TrySave(predictionInfo);
      return errorMessage;
    }

    /// <summary>
    /// Создать черновик поручения.
    /// </summary>
    /// <param name="performer">Исполнитель.</param>
    /// <param name="assignee">Исполнитель задания.</param>
    /// <param name="deadline">Конечный срок.</param>
    /// <param name="maxDeadline">Максимальный конечный срок.</param>
    /// <param name="importance">Важность.</param>
    /// <param name="hasIndefiniteDeadline">Без срока.</param>
    /// <param name="activeText">Текст поручения.</param>
    /// <returns>Задача на исполнение поручения.</returns>
    [Public]
    public virtual IActionItemExecutionTask CreateDraftActionItemExecutionTask(IEmployee performer, IEmployee assignee, DateTime? deadline, DateTime? maxDeadline,
                                                                               Sungero.Core.Enumeration? importance, bool hasIndefiniteDeadline, string activeText)
    {
      var draftActionItem = ActionItemExecutionTasks.Create();
      draftActionItem.Status = RecordManagement.ActionItemExecutionTask.Status.Draft;
      draftActionItem.Assignee = performer;
      draftActionItem.AssignedBy = assignee;
      draftActionItem.StartedBy = assignee;
      draftActionItem.Importance = importance;
      draftActionItem.HasIndefiniteDeadline = hasIndefiniteDeadline == true;
      draftActionItem.Deadline = deadline;
      draftActionItem.MaxDeadline = maxDeadline;
      draftActionItem.ActiveText = string.IsNullOrEmpty(activeText) ? ActionItemExecutionTasks.Resources.DefaultDraftActionItemActiveText : activeText;
      var assigneePersonalSettings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(assignee);
      if (assigneePersonalSettings?.FollowUpActionItem == true)
      {
        var supervisor = Docflow.PublicFunctions.PersonalSetting.GetSupervisor(assigneePersonalSettings);
        if (supervisor != null)
        {
          draftActionItem.IsUnderControl = true;
          draftActionItem.Supervisor = supervisor;
        }
      }
      
      var isControlledByAssignee = draftActionItem.IsUnderControl == true && Equals(draftActionItem.Supervisor, draftActionItem.StartedBy);
      draftActionItem.IsAutoExec = !isControlledByAssignee && assigneePersonalSettings?.IsAutoExecLeadingActionItem == true;
      
      return draftActionItem;
    }
    
    /// <summary>
    /// Определить исполнителя и создать черновик поручения.
    /// </summary>
    /// <param name="task">Родительская задача.</param>
    /// <param name="taskType">Тип задачи.</param>
    /// <param name="deadline">Срок.</param>
    /// <param name="activeText">Текст поручения.</param>
    /// <returns>Черновик поручения.</returns>
    [Public]
    public virtual IActionItemExecutionTask PrepareDraftActionItem(ITask task, Sungero.Core.Enumeration taskType, DateTime? deadline, string activeText)
    {
      var predictionInfo = ActionItemPredictionInfos.GetAll()
        .FirstOrDefault(x => x.TaskId == task.Id && x.TaskType == taskType);
      
      if (predictionInfo == null)
        return null;
      
      if (predictionInfo.ArioTaskStatus == RecordManagement.ActionItemPredictionInfo.ArioTaskStatus.ErrorOccured)
        return null;
      
      if (predictionInfo.ArioTaskStatus == RecordManagement.ActionItemPredictionInfo.ArioTaskStatus.InProcess)
      {
        Logger.DebugFormat("{0}Task (ID={1}). PrepareDraftActionItemBlockExecute. Ario processing time expired, no response received",
                           taskType, task.Id);
        predictionInfo.ArioTaskStatus = RecordManagement.ActionItemPredictionInfo.ArioTaskStatus.ErrorOccured;
        if (!Functions.ActionItemPredictionInfo.TrySave(predictionInfo))
          return null;
      }
      
      // Определить исполнителя по результатам Ario.
      var classifier = predictionInfo.AIManagerAssistant.Classifiers
        .Where(x => x.ClassifierId.HasValue && x.ClassifierType == Intelligence.AIManagersAssistantClassifiers.ClassifierType.Assignee)
        .FirstOrDefault();
      if (classifier == null)
        return null;
      var limit = (double)classifier.LowerClassificationLimit / 100;
      var performer = SmartProcessing.PublicFunctions.Module.GetPerformerByPredictionResult(predictionInfo.ArioResultJson, classifier.ClassifierId.Value, limit);
      if (performer == null)
        return null;
      var hasIndefiniteDeadline = false;
      var assignee = Employees.Null;
      
      if (taskType == RecordManagement.ActionItemPredictionInfo.TaskType.ActionItem)
      {
        var actionItemTask = Sungero.RecordManagement.ActionItemExecutionTasks.As(task);
        assignee = actionItemTask.Assignee;
        if (deadline == null)
        {
          hasIndefiniteDeadline = actionItemTask.HasIndefiniteDeadline.Value;
          deadline = actionItemTask.Deadline;
        }
      }
      else
      {
        var documentReviewTask = Sungero.RecordManagement.DocumentReviewTasks.As(task);
        assignee = documentReviewTask.Addressee;
      }

      var draft = predictionInfo.ActionItemId.HasValue ?
        ActionItemExecutionTasks.GetAll(x => x.IsDraftResolution == true && x.Id == predictionInfo.ActionItemId.Value).FirstOrDefault() :
        ActionItemExecutionTasks.Null;
      
      if (draft == null)
      {
        draft = this.CreateDraftActionItemExecutionTask(performer, assignee, deadline, deadline, task.Importance, hasIndefiniteDeadline, activeText);
        if (taskType == RecordManagement.ActionItemPredictionInfo.TaskType.DocumentReview)
        {
          draft.IsDraftResolution = true;
          draft.State.Properties.Deadline.IsRequired = false;
        }
        predictionInfo.ActionItemId = draft.Id;
        draft.Save();
      }
      else
      {
        if (draft.AssignedBy != assignee)
        {
          // У проекта резолюции может быть пустой срок.
          draft.State.Properties.Deadline.IsRequired = false;
          draft.Deadline = deadline;
          draft.MaxDeadline = deadline;
          draft.AssignedBy = assignee;
          draft.ActiveText = string.IsNullOrEmpty(activeText) ? ActionItemExecutionTasks.Resources.DefaultDraftActionItemActiveText : activeText;
        }
        draft.Assignee = performer;
        draft.Save();
      }

      predictionInfo.Assignee = performer;
      Functions.ActionItemPredictionInfo.TrySave(predictionInfo);
      return draft;
    }
    
    /// <summary>
    /// Проверить настроена ли в системе настроена подготовка проектов поручений виртуальным ассистентом.
    /// </summary>
    /// <param name="taskId">Ид задачи.</param>
    /// <param name="assignee">Исполнитель задания.</param>
    /// <param name="taskType">Тип задачи.</param>
    /// <param name="document">Документ.</param>
    /// <returns>True - если в системе настроена подготовка проектов поручений виртуальным ассистентом.</returns>
    public static bool CanAiAssistantPrepareDrafts(long taskId, IEmployee assignee, Sungero.Core.Enumeration taskType, IOfficialDocument document)
    {
      var logTemplate = string.Format("{0}Task (ID={1}). CanAiAssistantPrepareDrafts. {{0}}", taskType, taskId);
      
      if (document == null)
      {
        Logger.DebugFormat(logTemplate, "No document found");
        return false;
      }

      if (document.IsEncrypted)
      {
        Logger.DebugFormat(logTemplate, "Document is encrypted");
        return false;
      }

      if (!document.HasVersions)
      {
        Logger.DebugFormat(logTemplate, "Document has no versions");
        return false;
      }

      var assistant = Functions.Module.GetAiAssistantToPrepareActionItems(assignee, taskType);
      if (assistant == null)
      {
        Logger.DebugFormat(logTemplate, "AI assistant not found");
        return false;
      }
      
      var assigneeClassifier = assistant.Classifiers.Where(x => x.ClassifierType == Intelligence.AIManagersAssistantClassifiers.ClassifierType.Assignee).FirstOrDefault();
      if (assigneeClassifier == null)
      {
        Logger.DebugFormat(logTemplate, string.Format("Assignee classifier ID not found, assistantId={0}", assistant.Id));
        return false;
      }
      
      if (assigneeClassifier.IsModelActive != Intelligence.AIManagersAssistantClassifiers.IsModelActive.Yes)
      {
        Logger.DebugFormat(logTemplate, string.Format("Assignee classifier has no active models, assistantId={0}", assistant.Id));
        return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Получить виртуального ассистента с включенной опцией "Готовит проекты подчиненных поручений".
    /// </summary>
    /// <param name="assignee">Руководитель, для которого получать ассистента.</param>
    /// <returns>Виртуальный ассистент.</returns>
    [Obsolete("Метод не используется с 11.03.2024 и версии 4.10. Используйте универсальный метод: GetAiAssistantToPrepareActionItems(IEmployee assignee, Enumeration taskType).")]
    public virtual Intelligence.IAIManagersAssistant GetAIAssistantPreparingActionItemDrafts(IEmployee assignee)
    {
      return this.GetAiAssistantToPrepareActionItems(assignee, RecordManagement.ActionItemPredictionInfo.TaskType.ActionItem);
    }
    
    /// <summary>
    /// Получить виртуального ассистента с включенной опцией подготовки задачи на исполнение поручения.
    /// </summary>
    /// <param name="assignee">Руководитель, для которого получать ассистента.</param>
    /// <param name="taskType">Тип задачи.</param>
    /// <returns>Виртуальный ассистент.</returns>
    public virtual Intelligence.IAIManagersAssistant GetAiAssistantToPrepareActionItems(IEmployee assignee, Enumeration taskType)
    {
      var assistants = Intelligence.AIManagersAssistants.GetAll()
        .Where(x => Equals(x.Manager, assignee) && x.Status == Intelligence.AIManagersAssistant.Status.Active);
      
      if (taskType == RecordManagement.ActionItemPredictionInfo.TaskType.ActionItem)
        assistants = assistants.Where(x => x.PreparesActionItemDrafts == true);
      else if (taskType == RecordManagement.ActionItemPredictionInfo.TaskType.DocumentReview)
        assistants = assistants.Where(x => x.PreparesResolution == true);
      
      return assistants.FirstOrDefault();
    }
    
    #endregion

    #region Наличие блоков на схеме варианта процесса
    
    /// <summary>
    /// Проверить, есть ли хотя бы одно одно- или многоадресное рассмотрение документа в схеме задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>True - есть, иначе - false.</returns>
    [Public, Remote]
    public virtual bool HasAnyTypeDocumentReviewBlockInScheme(ITask task)
    {
      return this.HasAnyDocumentReviewBlockInScheme(task)
        || this.HasAnyMultipleAddresseeReviewBlockInScheme(task);
    }
    
    /// <summary>
    /// Проверить, есть ли хотя бы одно одноадресное рассмотрение документа в схеме задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>True - есть, иначе - false.</returns>
    [Public, Remote]
    public virtual bool HasAnyDocumentReviewBlockInScheme(ITask task)
    {
      return Blocks.DocumentReviewBlocks.GetAll(task.Scheme).Any();
    }
    
    /// <summary>
    /// Проверить, есть ли хотя бы одно многоадресное рассмотрение документа в схеме задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>True - есть, иначе - false.</returns>
    [Public, Remote]
    public virtual bool HasAnyMultipleAddresseeReviewBlockInScheme(ITask task)
    {
      return Blocks.DocumentMultipleAddresseeReviewBlocks.GetAll(task.Scheme).Any();
    }
    
    #endregion
  }
}