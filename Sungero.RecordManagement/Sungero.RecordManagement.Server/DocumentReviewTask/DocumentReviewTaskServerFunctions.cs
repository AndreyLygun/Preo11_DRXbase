using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.RecordManagement.DocumentReviewTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server
{
  partial class DocumentReviewTaskFunctions
  {
    /// <summary>
    /// Проверить, является ли текущий пользователь исполнителем задания или его замещающим.
    /// </summary>
    /// <param name="performer">Исполнитель задания.</param>
    /// <returns>True, если совпадает с исполнителем или замещающим, иначе false.</returns>
    [Remote]
    public static bool CurrentUserIsPerformerOrSubstitute(IUser performer)
    {
      var activeUsersWhoSubstitute = Substitutions.ActiveUsersWhoSubstitute(performer);
      return activeUsersWhoSubstitute.Contains(Users.Current) || Equals(performer, Users.Current);
    }
    
    #region Контрол "Состояние"

    /// <summary>
    /// Построить модель состояния рассмотрения.
    /// </summary>
    /// <returns>Схема модели состояния.</returns>
    [Public, Remote(IsPure = true)]
    public string GetStateViewXml()
    {
      return this.GetStateView().ToString();
    }
    
    /// <summary>
    /// Построить модель состояния задачи на рассмотрение документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Контрол состояния.</returns>
    public Sungero.Core.StateView GetStateView(Sungero.Docflow.IOfficialDocument document)
    {
      if (_obj.DocumentForReviewGroup.OfficialDocuments.Any(d => Equals(document, d)))
        return this.GetStateView();
      else
        return StateView.Create();
    }
    
    /// <summary>
    /// Построить модель состояния рассмотрения.
    /// </summary>
    /// <returns>Схема модели состояния.</returns>
    [Remote(IsPure = true)]
    [LocalizeFunction("GetStateViewFunctionName", "GetStateViewFunctionDescription")]
    public Sungero.Core.StateView GetStateView()
    {
      return this.GetDocumentReviewStateView(true);
    }
    
    /// <summary>
    /// Построить модель состояния рассмотрения.
    /// </summary>
    /// <param name="addActionItemExecutionBlocks">Добавлять блок информации о поручениях и проектах резолюции.</param>
    /// <returns>Схема модели состояния.</returns>
    [Public]
    public Sungero.Core.StateView GetDocumentReviewStateView(bool? addActionItemExecutionBlocks)
    {
      var stateView = StateView.Create();
      
      // Пропустить задачу-контейнер.
      if (_obj.Addressees.Count > 1 && _obj.Started.HasValue)
        return stateView;
      
      var comment = Docflow.PublicFunctions.Module.GetTaskUserComment(_obj, Resources.ConsiderationText);
      
      if (_obj.Started.HasValue)
      {
        var startedBy = _obj.StartedBy;
        if (_obj.StartedBy.IsSystem == true)
          startedBy = _obj.MainTask != null ? _obj.MainTask.StartedBy : _obj.Author;
        
        Docflow.PublicFunctions.OfficialDocument.AddUserActionBlock(stateView, _obj.Author,
                                                                    DocumentReviewTasks.Resources.StateViewDocumentSent,
                                                                    _obj.Started.Value, _obj,
                                                                    comment, startedBy);
      }
      else
      {
        Docflow.PublicFunctions.OfficialDocument.AddUserActionBlock(stateView, _obj.Author,
                                                                    Docflow.ApprovalTasks.Resources.StateViewTaskDrawCreated,
                                                                    _obj.Created.Value, _obj,
                                                                    comment, _obj.Author);
      }
      
      var startDate = this.GetIterationStartDate();
      var managerAssignment = this.GetManagerAssignment(startDate);
      var resolutionAssignment = this.GetPreparingDraftResolutionAssignment(startDate);
      var reworkAssignment = this.GetReviewReworkAssignment(startDate);
      var reviewResolutionAssignment = this.GetReviewResolutionAssignment(startDate);
      
      List<long> actionItemIds = null;
      
      AccessRights.AllowRead(
        () =>
        {
          // Разделено на два запроса для оптимизации.
          var actionItemsByReviewTaskIds = ActionItemExecutionTasks
            .GetAll(t => t.ParentAssignment != null && Equals(t.ParentAssignment.Task, _obj))
            .Select(t => t.Id)
            .ToList();
          
          if (managerAssignment != null)
          {
            var actionItemsByManagerAssignmentIds = ActionItemExecutionTasks.GetAll()
              .Where(t => Equals(t.ParentAssignment, managerAssignment))
              .Select(t => t.Id)
              .ToList();
            
            actionItemIds = actionItemsByReviewTaskIds.Union(actionItemsByManagerAssignmentIds)
              .ToList();
          }
          else
            actionItemIds = actionItemsByReviewTaskIds;
          
        });
      
      var actionItems = ActionItemExecutionTasks.GetAll().Where(t => actionItemIds.Contains(t.Id)).ToList();
      var tasksCount = actionItems.Count;
      if (Sungero.Docflow.PublicFunctions.Module.IsBlocksNumberOverLimit(tasksCount))
        throw AppliedCodeException.Create(Sungero.Docflow.Resources.ExceededNumberOfBlocks);

      var managerBlock = this.AddReviewManagerBlock(stateView, actionItems, managerAssignment, resolutionAssignment, reworkAssignment);
      if (managerBlock != null)
      {
        this.AddPreraringDraftResolutionBlock(managerBlock, resolutionAssignment, managerAssignment);
        this.AddReviewResolutionBlock(managerBlock, reviewResolutionAssignment);
        this.AddReviewReworkBlock(managerBlock, reworkAssignment);
      }
      
      // Добавить блок информации о поручениях и вложенных проектах резолюций, созданных в рамках рассмотрения.
      if (addActionItemExecutionBlocks == true)
        this.AddActionItemExecutionBlocks(stateView, actionItems);
      
      return stateView;
    }
    
    /// <summary>
    /// Добавить статус выполнения задания.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="style">Стиль.</param>
    /// <param name="assignment">Задание.</param>
    private void AddAssignmentStatusInfoToRight(StateBlock block, Sungero.Core.StateBlockLabelStyle style, IAssignment assignment)
    {
      // Добавить колонку справа, если всего одна колонка (main).
      var rightContent = block.Contents.LastOrDefault();
      if (block.Contents.Count() <= 1)
        rightContent = block.AddContent();
      else
        rightContent.AddLineBreak();

      rightContent.AddLabel(Assignments.Info.Properties.Status.GetLocalizedValue(assignment.Status), style);
    }
    
    /// <summary>
    /// Добавить блок информации о рассмотрении документа руководителем.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <param name="actionItemExecutionTasks">Задачи на исполнения поручений.</param>
    /// <param name="managerAssignment">Задание руководителя.</param>
    /// <param name="resolutionAssignment">Задание на подготовку проекта резолюции.</param>
    /// <param name="reworkAssignment">Задание на доработку инициатором.</param>
    /// <returns>Полученный блок.</returns>
    private StateBlock AddReviewManagerBlock(StateView stateView, List<IActionItemExecutionTask> actionItemExecutionTasks,
                                             IAssignment managerAssignment, IAssignment resolutionAssignment, IAssignment reworkAssignment)
    {
      var author = string.Empty;
      if (managerAssignment != null)
        author = Docflow.PublicFunctions.OfficialDocument.GetAuthor(managerAssignment.Performer, managerAssignment.CompletedBy);
      else if (_obj.Addressee != null)
        author = Docflow.PublicFunctions.OfficialDocument.GetAuthor(_obj.Addressee, _obj.Addressee);
      
      var filteredActionItems = actionItemExecutionTasks.Where(t => t.Status != Workflow.Task.Status.Draft && Equals(t.AssignedBy, _obj.Addressee))
        .OrderBy(t => t.Started);
      
      var isCompleted = (managerAssignment != null && managerAssignment.Status == Workflow.AssignmentBase.Status.Completed) ||
        (resolutionAssignment != null && resolutionAssignment.Result == RecordManagement.PreparingDraftResolutionAssignment.Result.AddAssignment);
      var isReworkResolution = managerAssignment != null && ReviewDraftResolutionAssignments.Is(managerAssignment) &&
        managerAssignment.Result == RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution &&
        !(resolutionAssignment != null && resolutionAssignment.Result == RecordManagement.PreparingDraftResolutionAssignment.Result.AddAssignment);
      var isRework = reworkAssignment != null && reworkAssignment.Status == Sungero.Workflow.Assignment.Status.InProcess;
      var isDraft = _obj.Status == Workflow.Task.Status.Draft;
      
      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle(isDraft);
      var performerStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle(isDraft);
      var labelStyle = Docflow.PublicFunctions.Module.CreateStyle(false, isDraft, false);
      var separatorStyle = Docflow.PublicFunctions.Module.CreateSeparatorStyle();
      
      // Добавить блок. Установить иконку и сущность.
      var block = stateView.AddBlock();
      block.Entity = _obj;
      if (isCompleted && !isReworkResolution && !isRework)
        block.AssignIcon(ReviewManagerAssignments.Info.Actions.AddResolution, StateBlockIconSize.Large);
      else
        block.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Large);

      // Рассмотрение руководителем ещё в работе.
      if (!isCompleted || isReworkResolution || isRework)
      {
        // Добавить заголовок.
        block.AddLabel(Docflow.Resources.StateViewDocumentReview, headerStyle);
        block.AddLineBreak();
        if (managerAssignment != null && !isReworkResolution)
        {
          if (managerAssignment.Status == Workflow.AssignmentBase.Status.Aborted)
            Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Resources.StateViewAborted);
          else if (managerAssignment.IsRead == false)
            Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Resources.StateViewUnRead);
          else
            this.AddAssignmentStatusInfoToRight(block, labelStyle, managerAssignment);
        }
        else if (_obj.Status == Workflow.Task.Status.Completed)
        {
          Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Resources.StateViewCompleted);
        }
        else if (_obj.Status == Workflow.Task.Status.Aborted)
        {
          Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Resources.StateViewAborted);
        }
        
        // Адресат.
        if (_obj.Addressee != null)
          block.AddLabel(string.Format("{0}: {1}",
                                       Docflow.Resources.StateViewAddressee,
                                       Company.PublicFunctions.Employee.GetShortName(_obj.Addressee, false)), performerStyle);

        var deadline = managerAssignment != null && !isReworkResolution ?
          managerAssignment.Deadline : _obj.MaxDeadline;
        var deadlineString = deadline.HasValue ?
          Docflow.PublicFunctions.Module.ToShortDateShortTime(deadline.Value.ToUserTime()) :
          Docflow.OfficialDocuments.Resources.StateViewWithoutTerm;

        block.AddLabel(string.Format("{0}: {1}", Docflow.OfficialDocuments.Resources.StateViewDeadline, deadlineString),
                       performerStyle);
        
        // Информация о задержке выполнения.
        if (_obj.Status == Workflow.Task.Status.InProcess || _obj.Status == Workflow.Task.Status.UnderReview)
        {
          if (!isReworkResolution && managerAssignment != null && managerAssignment.Deadline.HasValue)
            Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(block, managerAssignment.Deadline.Value, managerAssignment.Performer);
          else if (resolutionAssignment != null && resolutionAssignment.Deadline.HasValue)
            Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(block, resolutionAssignment.Deadline.Value, resolutionAssignment.Performer);
        }
      }
      else if (managerAssignment != null || resolutionAssignment != null)
      {
        // Рассмотрение завершено.
        // Добавить заголовок.
        var completionDate = managerAssignment == null ? resolutionAssignment.Completed.Value.ToUserTime() : managerAssignment.Completed.Value.ToUserTime();
        var resolutionDate = Docflow.PublicFunctions.Module.ToShortDateShortTime(completionDate);
        block.AddLabel(Docflow.Resources.StateViewResolution, headerStyle);
        block.AddLineBreak();
        block.AddLabel(string.Format("{0}: {1} {2}: {3}",
                                     DocumentReviewTasks.Resources.StateViewAuthor,
                                     author,
                                     Docflow.OfficialDocuments.Resources.StateViewDate,
                                     resolutionDate), performerStyle);
        block.AddLineBreak();
        block.AddLabel(Docflow.Constants.Module.SeparatorText, separatorStyle);
        block.AddLineBreak();
        block.AddEmptyLine(Docflow.Constants.Module.EmptyLineMargin);
        
        // Если поручения не созданы, или рассмотрение выполнено с результатом "Вынести резолюцию" или "Принято к сведению" и помощник сам не отправлял поручения в работу.
        // В старых задачах поручение и рассмотрение не связаны, поэтому обрабатываем такие случаи как резолюцию.
        if (!filteredActionItems.Any() || (managerAssignment != null && managerAssignment.Result != RecordManagement.ReviewManagerAssignment.Result.AddAssignment &&
                                           managerAssignment.Result != RecordManagement.ReviewDraftResolutionAssignment.Result.ForExecution &&
                                           managerAssignment.Result != RecordManagement.DocumentReviewAssignment.Result.ActionItemsSent &&
                                           managerAssignment.Result != RecordManagement.DocumentReviewAssignment.Result.DraftResApprove &&
                                           !(resolutionAssignment != null && resolutionAssignment.Result == RecordManagement.PreparingDraftResolutionAssignment.Result.AddAssignment)))
        {
          var comment = resolutionAssignment != null && resolutionAssignment.Result == RecordManagement.PreparingDraftResolutionAssignment.Result.AddAssignment ?
            Docflow.PublicFunctions.Module.GetFormatedUserText(resolutionAssignment.Texts.Last().Body) :
            Docflow.PublicFunctions.Module.GetFormatedUserText(managerAssignment.Texts.Last().Body);
          block.AddLabel(comment);
          block.AddLineBreak();
        }
        else
        {
          // Добавить информацию по каждому поручению.
          foreach (var actionItem in filteredActionItems)
          {
            if (actionItem.IsCompoundActionItem == true)
            {
              foreach (var item in actionItem.ActionItemParts)
              {
                if (item.ActionItemPartExecutionTask != null)
                  Functions.ActionItemExecutionTask.AddActionItemInfo(block, item.ActionItemPartExecutionTask, author);
              }
            }
            else
            {
              Functions.ActionItemExecutionTask.AddActionItemInfo(block, actionItem, author);
            }
          }
        }
      }
      return block;
    }
    
    /// <summary>
    /// Добавить блок информации о создании поручения по резолюции.
    /// </summary>
    /// <param name="parentBlock">Основной блок.</param>
    /// <param name="resolutionAssignment">Задание на обработку резолюции.</param>
    private void AddReviewResolutionBlock(StateBlock parentBlock, IAssignment resolutionAssignment)
    {
      this.AddAssignmentBlock(parentBlock, resolutionAssignment, DocumentReviewTasks.Resources.StateViewSendActionItemOnResolution, string.Empty);
    }
    
    /// <summary>
    /// Добавить блок информации о подготовке проекта резолюции.
    /// </summary>
    /// <param name="parentBlock">Основной блок.</param>
    /// <param name="resolutionAssignment">Задание на подготовку проекта резолюции.</param>
    /// <param name="managerAssignment">Задание руководителю.</param>
    private void AddPreraringDraftResolutionBlock(StateBlock parentBlock, IAssignment resolutionAssignment, IAssignment managerAssignment)
    {
      var result = string.Empty;
      if (managerAssignment == null && resolutionAssignment != null &&
          resolutionAssignment.Status == Workflow.AssignmentBase.Status.Completed &&
          resolutionAssignment.Result != RecordManagement.PreparingDraftResolutionAssignment.Result.AddAssignment)
        result = Docflow.PublicFunctions.Module.GetFormatedUserText(resolutionAssignment.Texts.Last().Body);
      
      this.AddAssignmentBlock(parentBlock, resolutionAssignment, DocumentReviewTasks.Resources.PreparingDraftResolution, result);
    }
    
    /// <summary>
    /// Добавить блок информации о доработке инициатором.
    /// </summary>
    /// <param name="parentBlock">Основной блок.</param>
    /// <param name="reworkAssignment">Задание на доработку инициатором.</param>
    public void AddReviewReworkBlock(Sungero.Core.StateBlock parentBlock, IAssignment reworkAssignment)
    {
      this.AddAssignmentBlock(parentBlock, reworkAssignment, DocumentReviewTasks.Resources.ReviewReworkAssignment, string.Empty);
    }
    
    /// <summary>
    /// Добавить блок информации о поручениях и вложенных проектах резолюций, созданных в рамках рассмотрения.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <param name="actionItemExecutionTasks">Задачи на исполнения поручений.</param>
    private void AddActionItemExecutionBlocks(StateView stateView, List<IActionItemExecutionTask> actionItemExecutionTasks)
    {
      // Проекты резолюции.
      var draftResolutions = _obj.ResolutionGroup.ActionItemExecutionTasks
        .Where(x => Equals(x.AssignedBy, _obj.Addressee) && x.IsDraftResolution == true)
        .ToList();
      
      var actionItems = new List<IActionItemExecutionTask>();
      actionItems.AddRange(actionItemExecutionTasks.Where(t => t.ParentAssignment != null && Equals(t.ParentAssignment.Task, _obj)).ToList());
      actionItems.AddRange(draftResolutions);
      
      foreach (var actionItem in actionItems)
      {
        if (stateView.Blocks.Any(b => b.HasEntity(actionItem)))
          continue;
        
        // Сформировать схему представления поручения без добавления дополнительного блока резолюции.
        var actionItemStateView = PublicFunctions.ActionItemExecutionTask.GetActionItemExecutionTaskStateView(actionItem,
                                                                                                              null, null, null, true, false);
        foreach (var block in actionItemStateView.Blocks)
          stateView.AddBlock(block);
      }
    }
    
    /// <summary>
    /// Получить дату начала текущей итерации рассмотрения.
    /// </summary>
    /// <returns>Дата начала текущей итерации рассмотрения.</returns>
    private DateTime GetIterationStartDate()
    {
      if (!_obj.Started.HasValue)
        return _obj.Created.Value;
      
      var startDate = _obj.Started.Value;
      var lastForwardedAsg = Assignments.GetAll()
        .Where(a => Equals(a.Task, _obj))
        .Where(a => a.Created >= startDate)
        .Where(a => a.Status == Workflow.AssignmentBase.Status.Completed)
        .Where(a => a.Result == RecordManagement.ReviewManagerAssignment.Result.Forward)
        .OrderByDescending(a => a.Completed.Value)
        .FirstOrDefault();
      
      if (lastForwardedAsg != null)
        startDate = lastForwardedAsg.Completed.Value;
      
      return startDate;
    }
    
    /// <summary>
    /// Получить задание руководителю.
    /// </summary>
    /// <param name="startDate">Дата начала текущей итерации рассмотрения.</param>
    /// <returns>Задание руководителю.</returns>
    private IAssignment GetManagerAssignment(DateTime startDate)
    {
      return Assignments.GetAll()
        .Where(a => Equals(a.Task, _obj))
        .Where(a => a.Created >= startDate)
        .Where(a => ReviewManagerAssignments.Is(a) || ReviewDraftResolutionAssignments.Is(a) || DocumentReviewAssignments.Is(a))
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить задание на подготовку проекта резолюции.
    /// </summary>
    /// <param name="startDate">Дата начала текущей итерации рассмотрения.</param>
    /// <returns>Задание на подготовку проекта резолюции.</returns>
    private IAssignment GetPreparingDraftResolutionAssignment(DateTime startDate)
    {
      return Assignments.GetAll()
        .Where(a => Equals(a.Task, _obj))
        .Where(a => a.Created >= startDate)
        .Where(a => PreparingDraftResolutionAssignments.Is(a))
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить задание на доработку инициатором.
    /// </summary>
    /// <param name="startDate">Дата начала текущей итерации рассмотрения.</param>
    /// <returns>Задание на доработку инициатором.</returns>
    private IAssignment GetReviewReworkAssignment(DateTime startDate)
    {
      return ReviewReworkAssignments.GetAll()
        .Where(a => Equals(a.Task, _obj))
        .Where(a => a.Created >= startDate)
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить задание на обработку резолюции.
    /// </summary>
    /// <param name="startDate">Дата начала текущей итерации рассмотрения.</param>
    /// <returns>Задание на обработку резолюции.</returns>
    private IAssignment GetReviewResolutionAssignment(DateTime startDate)
    {
      return ReviewResolutionAssignments.GetAll()
        .Where(a => Equals(a.Task, _obj) && a.Created >= startDate)
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Добавить блок с заданием.
    /// </summary>
    /// <param name="parentBlock">Основной блок.</param>
    /// <param name="assignment">Задание.</param>
    /// <param name="header">Заголовок блока.</param>
    /// <param name="result">Результат выполнения задания.</param>
    private void AddAssignmentBlock(StateBlock parentBlock, IAssignment assignment, string header, string result)
    {
      if (assignment != null && (assignment.Status != Workflow.AssignmentBase.Status.Completed || !string.IsNullOrEmpty(result)))
      {
        var isDraft = _obj.Status == Workflow.Task.Status.Draft;
        var labelStyle = Docflow.PublicFunctions.Module.CreateStyle(false, isDraft, false);
        
        parentBlock.IsExpanded = true;
        var block = parentBlock.AddChildBlock();
        
        block.Entity = assignment;
        block.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Large);
        
        if (assignment.Status == Workflow.AssignmentBase.Status.InProcess && assignment.IsRead == false)
          Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Resources.StateViewUnRead);
        else
          this.AddAssignmentStatusInfoToRight(block, labelStyle, assignment);

        var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle(isDraft);
        var performerStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle(isDraft);
        
        block.AddLabel(header, headerStyle);
        block.AddLineBreak();

        var resolutionPerformerName = Employees.Is(assignment.Performer) ?
          Company.PublicFunctions.Employee.GetShortName(Employees.As(assignment.Performer), false) :
          assignment.Performer.Name;
        
        var deadline = assignment.Deadline.HasValue ?
          Docflow.PublicFunctions.Module.ToShortDateShortTime(assignment.Deadline.Value.ToUserTime()) :
          Docflow.OfficialDocuments.Resources.StateViewWithoutTerm;
        block.AddLabel(string.Format("{0}: {1} {2}: {3}",
                                     Docflow.OfficialDocuments.Resources.StateViewTo,
                                     resolutionPerformerName,
                                     Docflow.OfficialDocuments.Resources.StateViewDeadline,
                                     deadline), performerStyle);
        
        if (!string.IsNullOrEmpty(result))
        {
          var separatorStyle = Docflow.PublicFunctions.Module.CreateSeparatorStyle();
          
          block.AddLineBreak();
          block.AddLabel(Docflow.Constants.Module.SeparatorText, separatorStyle);
          block.AddLineBreak();
          block.AddEmptyLine(Docflow.Constants.Module.EmptyLineMargin);
          block.AddLabel(result);
        }
        else
        {
          // Информация о задержке выполнения.
          if (assignment.Deadline.HasValue && (_obj.Status == Workflow.Task.Status.InProcess || _obj.Status == Workflow.Task.Status.UnderReview))
            Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(block, assignment.Deadline.Value, assignment.Performer);
        }
      }
    }
    
    #endregion
    
    /// <summary>
    /// Получить результат выполнения задания руководителю с последней итерации.
    /// </summary>
    /// <param name="task">Задача "рассмотрение входящего".</param>
    /// <returns>Результат задания руководителю.</returns>
    public static Enumeration? GetLastAssignmentResult(IDocumentReviewTask task)
    {
      var lastAssignments = Assignments.GetAll(c => Equals(c.Task, task) && c.Status == Sungero.Workflow.Assignment.Status.Completed)
        .OrderByDescending(c => c.Completed);
      if (!lastAssignments.Any())
        return null;
      else
        return lastAssignments.First().Result.Value;
    }
    
    /// <summary>
    /// Получить последнее задание, отправленное на доработку инициатору руководителем или помощником.
    /// </summary>
    /// <param name="task">Задача на рассмотрение.</param>
    /// <returns>Последнее задание, отправленное на доработку.</returns>
    public static IAssignment GetLastAssignmentSentForRework(IDocumentReviewTask task)
    {
      return Assignments.GetAll()
        .Where(a => Equals(a.Task, task) &&
               (ReviewManagerAssignments.Is(a) && a.Result == RecordManagement.ReviewManagerAssignment.Result.ForRework ||
                PreparingDraftResolutionAssignments.Is(a) && a.Result == RecordManagement.PreparingDraftResolutionAssignment.Result.ForRework ||
                DocumentReviewAssignments.Is(a) && a.Result == RecordManagement.DocumentReviewAssignment.Result.DocsRework))
        .OrderByDescending(a => a.Created).FirstOrDefault();
    }
    
    /// <summary>
    /// Выдать права на вложения, не выше прав инициатора задачи.
    /// </summary>
    /// <param name="assignees">Исполнители.</param>
    public virtual void GrantRightForAttachmentsToAssignees(List<IRecipient> assignees)
    {
      Logger.DebugFormat("DocumentReviewTask({0}). GrantRightForAttachmentsToAssignees.", _obj.Id);
      this.GrantRightsForMainDocumentAndAddendaToAssignees(assignees, false, new List<Sungero.Docflow.IOfficialDocument>());
      
      // Дополнительно обновляем права наблюдателей.
      Logger.DebugFormat("DocumentReviewTask({0}). GrantReadAccessRightsForAttachments.", _obj.Id);
      Docflow.PublicFunctions.Module.GrantReadAccessRightsForAttachmentsConsideringCurrentRights(_obj.AddendaGroup.All.ToList(), _obj.ResolutionObservers.Select(o => o.Observer));
    }

    /// <summary>
    /// Выдать права исполнителям на основной документ и приложения.
    /// </summary>
    /// <param name="assignees">Исполнители.</param>
    /// <param name="getAddendaFromAssignment">Получать приложения из задания: true - да, false - из задачи.</param>
    /// <param name="addenda">Список приложений.</param>
    public virtual void GrantRightsForMainDocumentAndAddendaToAssignees(List<IRecipient> assignees, bool getAddendaFromAssignment, List<Sungero.Docflow.IOfficialDocument> addenda)
    {
      Logger.DebugFormat("DocumentReviewTask({0}). GrantRightsForMainDocumentAndAddendaToAssignees.", _obj.Id);
      foreach (var assignee in assignees)
      {
        this.GrantRightsOnDocumentForReview(assignee);
        this.GrantRightsOnAddendaGroup(assignee, getAddendaFromAssignment, addenda);
      }
    }
    
    /// <summary>
    /// Выдать права "по умолчанию" на группы вложений.
    /// </summary>
    /// <param name="performers">Исполнители блока.</param>
    /// <remarks>Метод выдает права на проекты резолюций и документы группы "Приложения".</remarks>
    public virtual void GrantDefaultRightsOnAttachments(List<IRecipient> performers)
    {
      performers = performers.Where(x => Company.Employees.Is(x)).ToList();
      Functions.DocumentReviewTask.GrantRightsForMainDocumentAndAddendaToAssignees(_obj, performers, true, _obj.AddendaGroup.OfficialDocuments.ToList());
      
      // Выдавать права на проекты резолюции только сотрудникам, которые могут готовить проект резолюции для руководителя. Bug 328465.
      performers = performers.Where(x => Company.PublicFunctions.Employee.Remote.CanPrepareDraftResolution(Company.Employees.As(x))).ToList();
      if (performers.Any())
        Functions.DocumentReviewTask.GrantRightsForDraftResolutionToAssignees(_obj, performers, _obj.ResolutionGroup.ActionItemExecutionTasks.ToList());
    }
    
    /// <summary>
    /// Выдать права исполнителям на проекты резолюций.
    /// </summary>
    /// <param name="assignees">Исполнители.</param>
    /// <param name="draftResolutions">Список проектов резолюций.</param>
    public virtual void GrantRightsForDraftResolutionToAssignees(List<IRecipient> assignees, List<IActionItemExecutionTask> draftResolutions)
    {
      Logger.DebugFormat("DocumentReviewTask({0}). GrantRightsForResolutionToAssignees.", _obj.Id);
      var rights = DefaultAccessRightsTypes.Change;
      foreach (var assignee in assignees)
      {
        // Выдаем права на чтение, если прав нет.
        foreach (var draftResolution in draftResolutions)
        {
          Logger.DebugFormat("DocumentReviewTask({0}). Grant rights({1}) on action item({2}) for assignee({3})",
                             _obj.Id, rights, draftResolution.Id, assignee.Id);
          Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(draftResolution, assignee, rights);
        }
      }
    }
    
    /// <summary>
    /// Выдать права на документ для рассмотрения.
    /// </summary>
    /// <param name="assignee">Исполнитель.</param>
    public virtual void GrantRightsOnDocumentForReview(IRecipient assignee)
    {
      Logger.DebugFormat("DocumentReviewTask({0}). GrantRightsOnDocumentForReview.", _obj.Id);
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      var rights = this.GetCreationContextAttachmentRights();
      Logger.DebugFormat("DocumentReviewTask({0}) Grant rights({1}) on document({2}) for assignee({3})",
                         _obj.Id, rights, document.Id, assignee.Id);
      Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(document, assignee, rights);
    }
    
    /// <summary>
    /// Выдать права на документы группы "Приложения".
    /// </summary>
    /// <param name="assignee">Исполнитель.</param>
    public virtual void GrantRightsOnAddendaGroup(IRecipient assignee)
    {
      this.GrantRightsOnAddendaGroup(assignee, false, new List<Sungero.Docflow.IOfficialDocument>());
    }

    /// <summary>
    /// Выдать права на документы группы "Приложения".
    /// </summary>
    /// <param name="assignee">Исполнитель.</param>
    /// <param name="getAddendaFromAssignment">Получать приложения из задания: true - да, false - из задачи.</param>
    /// <param name="addenda">Список приложений.</param>
    public virtual void GrantRightsOnAddendaGroup(IRecipient assignee, bool getAddendaFromAssignment, List<Sungero.Docflow.IOfficialDocument> addenda)
    {
      Logger.DebugFormat("DocumentReviewTask({0}). GrantRightsOnAddendaGroup.", _obj.Id);
      // На приложения - на изменение, но не выше, чем у инициатора.
      var attachments = getAddendaFromAssignment ? addenda : _obj.AddendaGroup.OfficialDocuments;
      var rights = this.GetCreationContextAttachmentRights();
      foreach (var attachment in attachments.Where(a => a.Info.AccessRightsMode != Metadata.AccessRightsMode.Type))
      {
        Logger.DebugFormat("DocumentReviewTask({0}). Grant rights({1}) on document({2}) for assignee({3})",
                           _obj.Id, rights, attachment.Id, assignee.Id);
        this.GrantRightsOnAddendum(attachment, assignee, _obj.Author, rights);
      }
    }
    
    /// <summary>
    /// Выдать права на документ группы "Приложения".
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <param name="recipient">Субъект прав.</param>
    /// <param name="taskAuthor">Инициатор задачи.</param>
    /// <param name="accessRightsType">Тип прав.</param>
    public virtual void GrantRightsOnAddendum(IEntity entity, IRecipient recipient, IRecipient taskAuthor, Guid accessRightsType)
    {
      Logger.DebugFormat("DocumentReviewTask({0}). GrantRightsOnAddendum.", _obj.Id);
      var rightType = accessRightsType == DefaultAccessRightsTypes.Change && entity.AccessRights.CanUpdate(taskAuthor)
        ? DefaultAccessRightsTypes.Change
        : DefaultAccessRightsTypes.Read;
      Logger.DebugFormat("DocumentReviewTask({0}) Grant rights({1}) on addendum({2}) for recipient({3})",
                         _obj.Id, rightType, entity.Id, recipient.Id);
      Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(entity, recipient, rightType);
    }
    
    /// <summary>
    /// Выдать права на задачу помощнику руководителя для корректной работы с приложениями.
    /// </summary>
    public virtual void GrantRightsOnTaskForSecretary()
    {
      var assistant = Docflow.PublicFunctions.Module.GetSecretary(_obj.Addressee);
      if (!Equals(assistant, _obj.Author) &&
          !_obj.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, assistant) &&
          !_obj.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, assistant))
      {
        _obj.AccessRights.Grant(assistant, DefaultAccessRightsTypes.Change);
      }
    }
    
    /// <summary>
    /// Получить нестандартных исполнителей задачи.
    /// </summary>
    /// <returns>Исполнители.</returns>
    public virtual List<IRecipient> GetTaskAdditionalAssignees()
    {
      var assignees = new List<IRecipient>();

      var documentReview = DocumentReviewTasks.As(_obj);
      if (documentReview == null)
        return assignees;
      
      var document = documentReview.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      var clerk = Docflow.PublicFunctions.Module.Remote.GetRegistrar(document);
      
      var recipients = documentReview.Addressees
        .Where(x => x.Addressee != null)
        .Select(x => x.Addressee)
        .ToList();
      foreach (var recipient in recipients)
      {
        assignees.Add(recipient);
        var secretary = Docflow.PublicFunctions.Module.GetSecretary(recipient) ?? clerk;
        assignees.Add(secretary ?? documentReview.Author);
      }
      
      assignees.AddRange(documentReview.ResolutionObservers.Where(o => o.Observer != null).Select(o => o.Observer));
      
      return assignees.Distinct().ToList();
    }
    
    /// <summary>
    /// Обновить адресата после переадресации.
    /// </summary>
    /// <param name="newAddressee">Новый адресат.</param>
    public void UpdateReviewTaskAfterForward(IEmployee newAddressee)
    {
      _obj.Addressee = newAddressee;
      _obj.Addressees.Clear();
      _obj.Addressees.AddNew().Addressee = newAddressee;
    }
    
    /// <summary>
    /// Получить делопроизводителя для отправки поручений.
    /// </summary>
    /// <returns>Исполнитель задания по отправке поручения.</returns>
    public IUser GetClerkToSendActionItem()
    {
      var author = _obj.Author;
      var addressee = Employees.As(_obj.Addressee);
      var secretary = Employees.Null;
      
      // Личный секретарь адресата (руководителя).
      if (addressee != null)
        secretary = Docflow.PublicFunctions.Module.GetSecretary(addressee);
      
      // Ответственный за группу регистрации, либо инициатор.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      if (secretary == null && document.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable)
        secretary = Docflow.PublicFunctions.Module.Remote.GetRegistrar(document);
      
      return secretary ?? author;
    }

    /// <summary>
    /// Отправить проект резолюции на исполнение.
    /// </summary>
    /// <param name="parentAssignment">Задание на рассмотрение.</param>
    [Remote, Public, Obsolete("Метод не используется с 29.12.2023 и версии 4.9. Используйте метод GetChildActionItemsForDraftResolution.")]
    public void StartActionItemsForDraftResolution(IAssignment parentAssignment)
    {
      var actionItems = this.GetChildActionItemsForDraftResolution(parentAssignment);
      foreach (var actionItem in actionItems)
        actionItem.Start();
    }
    
    /// <summary>
    /// Получить список проектов резолюции, дочерних для текущего задания на рассмотрение.
    /// </summary>
    /// <param name="parentAssignment">Задание на рассмотрение.</param>
    /// <returns>Список проектов резолюции, дочерних для текущего задания на рассмотрение.</returns>
    /// <remarks>Проекты резолюции не стартуются сразу, так как Remote-функция выполняется в отдельной сессии и это может привести к ошибкам при длительном выполнении (см. Bug 282735).</remarks>
    [Remote, Public]
    [Obsolete("Метод не используется с 26.04.2024 и версии 4.10. Используйте функцию PrepareDraftResolutionForStart модуля RecordManagement.")]
    public virtual List<IActionItemExecutionTask> GetChildActionItemsForDraftResolution(IAssignment parentAssignment)
    {
      var actionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.Where(t => t.Status == RecordManagement.ActionItemExecutionTask.Status.Draft).ToList();
      Functions.Module.PrepareDraftResolutionForStart(actionItems,
                                                      parentAssignment,
                                                      _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault(),
                                                      _obj.AddendaGroup.OfficialDocuments.Select(x => Sungero.Content.ElectronicDocuments.As(x)).ToList(),
                                                      _obj.OtherGroup.All.ToList());
      
      return actionItems;
    }
    
    /// <summary>
    /// Рекурсивно завершить все подзадачи на рассмотрение.
    /// </summary>
    public virtual void AbortDocumentReviewSubTasks()
    {
      var subReviewTasks = DocumentReviewTasks.GetAll()
        .Where(x => x.ParentTask != null && Equals(x.ParentTask, _obj))
        .Where(x => x.Status == Sungero.Workflow.Task.Status.InProcess);
      foreach (var reviewTask in subReviewTasks)
        reviewTask.Abort();
    }
    
    /// <summary>
    /// Получить выполненные подзадачи на рассмотрение документа.
    /// </summary>
    /// <returns>Выполненные подзадачи на рассмотрение документа для текущей задачи.</returns>
    public virtual IQueryable<IDocumentReviewTask> GetCompletedDocumentReviewSubTasks()
    {
      var subTasksByParentTask = Functions.Module.GetSubtasksForTaskByParentTask(_obj, Sungero.Workflow.Task.Status.Completed)
        .Where(t => DocumentReviewTasks.Is(t))
        .Select(t => DocumentReviewTasks.As(t));
      
      var subTasksByParentAssignment = Functions.Module.GetSubtasksForTaskByParentAssignment(_obj, Sungero.Workflow.Task.Status.Completed)
        .Where(t => DocumentReviewTasks.Is(t))
        .Select(t => DocumentReviewTasks.As(t));
      
      var subTasks = new List<IDocumentReviewTask>();
      subTasks.AddRange(subTasksByParentTask);
      subTasks.AddRange(subTasksByParentAssignment);
      return subTasks.AsQueryable();
    }
    
    /// <summary>
    /// Получить состояние исполнения документа исключительно по этой задаче.
    /// </summary>
    /// <returns>Состояние исполнения документа исключительно по этой задаче.</returns>
    public virtual Enumeration? GetDocumentExecutionState()
    {
      if (_obj.Status == Sungero.Workflow.Task.Status.Aborted)
        return null;
      
      var preparingDraftResolutionAssignments = PreparingDraftResolutionAssignments.GetAll()
        .Where(x => x.Task.Id == _obj.Id &&
               x.Task.StartId == _obj.StartId);
      var reviewDraftResolutionAssignments = ReviewDraftResolutionAssignments.GetAll()
        .Where(x => x.Task.Id == _obj.Id &&
               x.Task.StartId == _obj.StartId);
      var reviewManagerAssignments = ReviewManagerAssignments.GetAll()
        .Where(x => x.Task.Id == _obj.Id &&
               x.Task.StartId == _obj.StartId);
      var documentReviewAssignments = DocumentReviewAssignments.GetAll()
        .Where(x => x.Task.Id == _obj.Id &&
               x.Task.StartId == _obj.StartId);
      var reviewResolutionAssignments = ReviewResolutionAssignments.GetAll()
        .Where(x => x.Task.Id == _obj.Id &&
               x.Task.StartId == _obj.StartId);
      var actionItemsInProcess = ActionItemExecutionTasks.GetAll()
        .Where(t => Equals(t.MainTask, _obj.MainTask))
        .Where(t => t.Status == Workflow.Task.Status.InProcess);
      var asgIdsOnExecution = reviewDraftResolutionAssignments.Where(x => x.Result == Sungero.RecordManagement.ReviewDraftResolutionAssignment.Result.ForExecution).Select(x => x.Id).ToList();
      asgIdsOnExecution.AddRange(reviewManagerAssignments.Where(x => x.Result == Sungero.RecordManagement.ReviewManagerAssignment.Result.AddAssignment).Select(x => x.Id));
      asgIdsOnExecution.AddRange(documentReviewAssignments.Where(x => x.Result == Sungero.RecordManagement.DocumentReviewAssignment.Result.ActionItemsSent ||
                                                                 x.Result == Sungero.RecordManagement.DocumentReviewAssignment.Result.DraftResApprove).Select(x => x.Id));
      asgIdsOnExecution.AddRange(reviewResolutionAssignments.Where(x => x.Status == Workflow.AssignmentBase.Status.Completed).Select(x => x.Id));
      
      // Статус "На исполнении".
      if (asgIdsOnExecution.Any() && actionItemsInProcess.Any(y => asgIdsOnExecution.Contains(y.ParentAssignment.Id)))
        return Sungero.Docflow.OfficialDocument.ExecutionState.OnExecution;
      
      // Статус "Отправка на исполнение".
      if (reviewResolutionAssignments.Any(x => x.Status == Workflow.AssignmentBase.Status.InProcess))
        return Sungero.Docflow.OfficialDocument.ExecutionState.Sending;
      
      // Статус "На рассмотрении".
      if (reviewManagerAssignments.Any(x => x.Status == Workflow.AssignmentBase.Status.InProcess) ||
          reviewDraftResolutionAssignments.Any(x => x.Status == Workflow.AssignmentBase.Status.InProcess) ||
          documentReviewAssignments.Any(x => x.Status == Workflow.AssignmentBase.Status.InProcess) ||
          preparingDraftResolutionAssignments.Any(x => x.Status == Workflow.AssignmentBase.Status.InProcess))
        return Sungero.Docflow.OfficialDocument.ExecutionState.OnReview;
      
      // Статус "Не требует исполнения".
      if (reviewDraftResolutionAssignments.Any(x => x.Result == Sungero.RecordManagement.ReviewDraftResolutionAssignment.Result.Informed) ||
          reviewManagerAssignments.Any(x => x.Result == Sungero.RecordManagement.ReviewManagerAssignment.Result.Explored) ||
          documentReviewAssignments.Any(x => x.Result == Sungero.RecordManagement.DocumentReviewAssignment.Result.Informed) ||
          preparingDraftResolutionAssignments.Any(x => x.Result == Sungero.RecordManagement.PreparingDraftResolutionAssignment.Result.Explored) ||
          reviewResolutionAssignments.Any(x => x.Status == Workflow.AssignmentBase.Status.Completed &&
                                          !actionItemsInProcess.Any(y => y.ParentAssignment.Id == x.Id)))
        return Sungero.Docflow.OfficialDocument.ExecutionState.WithoutExecut;
      
      return null;
    }
    
    /// <summary>
    /// Определить, может ли автор задачи готовить проекты резолюций.
    /// </summary>
    /// <returns>True - может, False - не может.</returns>
    /// <remarks>Автор задачи может готовить проект резолюции, если:
    /// <para>- является помощником с таким правом как минимум для одного из адресатов;</para>
    /// <para>- замещает помощника с таким правом как минимум для одного из адресатов.</para>
    /// </remarks>
    [Remote(IsPure = true)]
    public virtual bool CanAuthorPrepareResolution()
    {
      var addressees = _obj.Addressees
        .Select(x => x.Addressee)
        .ToList();
      return this.CanAuthorPrepareResolution(addressees);
    }
    
    /// <summary>
    /// Определить, может ли автор задачи готовить проекты резолюций.
    /// </summary>
    /// <param name="addressees">Адресаты.</param>
    /// <returns>True - может, False - не может.</returns>
    /// <remarks>Автор задачи может готовить проект резолюции, если:
    /// <para>- является помощником с таким правом как минимум для одного из адресатов;</para>
    /// <para>- замещает помощника с таким правом как минимум для одного из адресатов, и замещение не является системным.</para>
    /// </remarks>
    public virtual bool CanAuthorPrepareResolution(List<IEmployee> addressees)
    {
      var assistants = addressees
        .SelectMany(x => Company.PublicFunctions.Employee.GetManagerAssistantsWhoPrepareDraftResolution(x));
      var authorSubstitute = Sungero.Company.PublicFunctions.Module.GetUsersSubstitutedBy(_obj.Author, false);
      return assistants.Any(x => Equals(x.Assistant, _obj.Author)) || assistants.Any(x => authorSubstitute.Contains(x.Assistant));
    }
    
    /// <summary>
    /// Определить, может ли автор задачи готовить проекты резолюций для заданных адресатов.
    /// </summary>
    /// <param name="addressees">Адресаты.</param>
    /// <returns>True - автор может готовить проекты для всех адресатов, иначе False.</returns>
    [Remote(IsPure = true)]
    public virtual bool CanAuthorPrepareResolutionForAddressees(List<IEmployee> addressees)
    {
      var authorAndSubstitutes = Company.PublicFunctions.Module.GetUsersSubstitutedBy(_obj.Author);
      authorAndSubstitutes.Add(_obj.Author);
      
      return addressees.All(x => Company.PublicFunctions.Employee.GetManagerAssistantsWhoPrepareDraftResolution(x).Any(p => authorAndSubstitutes.Contains(p.Assistant)));
    }
    
    #region Синхронизация группы приложений
    
    /// <summary>
    /// Связать с основным документом документы из группы Приложения, если они не были связаны ранее.
    /// </summary>
    public virtual void RelateAddedAddendaToPrimaryDocument()
    {
      var primaryDocument = _obj.DocumentForReviewGroup.OfficialDocuments.SingleOrDefault();
      if (primaryDocument == null)
        return;
      
      Logger.DebugFormat("DocumentReviewTask (ID={0}). Add relation with type Addendum to primary document (ID={1})",
                         _obj.Id, primaryDocument.Id);
      var taskAddenda = Sungero.DocflowApproval.PublicFunctions.Module.GetNonObsoleteDocumentsFromAttachments(primaryDocument, _obj.AddendaGroup.All);
      Sungero.DocflowApproval.PublicFunctions.Module.RelateDocumentsToPrimaryDocumentAsAddenda(primaryDocument, taskAddenda);
    }
    
    #endregion
    
    /// <summary>
    /// Выполнить блоки мониторинга родительской задачи на согласование по регламенту.
    /// </summary>
    public virtual void ExecuteParentApprovalTaskMonitorings()
    {
      var task = _obj.ParentTask;
      if (task != null && Docflow.ApprovalTasks.Is(task))
      {
        var approvalTask = Docflow.ApprovalTasks.As(task);
        var link = Docflow.PublicFunctions.ApprovalReviewTaskStage.GetApprovalTaskExternalLink(approvalTask);
        if (link != null)
        {
          long reviewTaskId = 0;
          long.TryParse(link.ExternalEntityId, out reviewTaskId);

          if (reviewTaskId == _obj.Id)
            approvalTask.Blocks.ExecuteAllMonitoringBlocks();
        }
      }
    }
    
    /// <summary>
    /// Выполнить блоки мониторинга родительской задачи на рассмотрение документа.
    /// </summary>
    public virtual void ExecuteParentDocumentReviewTaskMonitorings()
    {
      var task = _obj.ParentTask;
      if (task != null && DocumentReviewTasks.Is(task))
      {
        var reviewTask = DocumentReviewTasks.As(task);
        if (Functions.DocumentReviewTask.AllDocumentReviewSubTasksAreCompleted(reviewTask))
        {
          Logger.DebugFormat("DocumentReviewTask(ID={0}) Call ExecuteAllMonitoringBlocks of ParentTask(ID={1})", _obj.Id, reviewTask.Id);
          reviewTask.Blocks.ExecuteAllMonitoringBlocks();
        }
      }
    }
    
    /// <summary>
    /// Проверить, выполнены ли все подчиненные задачи на рассмотрение документа.
    /// </summary>
    /// <returns>True, если все подчиненные поручения выполнены, иначе - False.</returns>
    public virtual bool AllDocumentReviewSubTasksAreCompleted()
    {
      var subTasks = DocumentReviewTasks.GetAll()
        .Where(x => Equals(x.ParentTask, _obj))
        .ToList();
      var result = subTasks.All(t => Functions.DocumentReviewTask.IsDocumentReviewTaskCompleted(t));
      Logger.DebugFormat("DocumentReviewTask(ID={0}) AllDocumentReviewSubTasksAreCompleted = {1}", _obj.Id, result);
      return result;
    }
    
    /// <summary>
    /// Получить права доступа на документ в зависимости от контекста.
    /// </summary>
    /// <returns>Права доступа на основной документ в зависимости от контекста.</returns>
    /// <remarks>Если задача на рассмотрение была запущена из согласования по регламенту или из согласования по процессу, то Чтение, иначе - Изменение.</remarks>
    public virtual Guid GetCreationContextAttachmentRights()
    {
      return Docflow.ApprovalTasks.Is(_obj.MainTask) || DocflowApproval.DocumentFlowTasks.Is(_obj.MainTask) ?
        DefaultAccessRightsTypes.Read :
        DefaultAccessRightsTypes.Change;
    }
    
    /// <summary>
    /// Удалить все поручения, где поле "Выдал" не соответствует ни одному из адресатов текущей задачи.
    /// </summary>
    public virtual void DeleteDraftActionItems()
    {
      Functions.Module.DeleteActionItemExecutionTasks(_obj.ResolutionGroup.ActionItemExecutionTasks
                                                      .Where(x => _obj.Addressees.All(a => !Equals(a.Addressee, x.AssignedBy)))
                                                      .ToList());
    }
    
    /// <summary>
    /// Удалить черновик поручения из области вложения черновиков.
    /// </summary>
    /// <param name="draftActionItem">Черновик поручения.</param>
    [Public]
    public virtual void RemoveDraftFromAttachments(IActionItemExecutionTask draftActionItem)
    {
      if (_obj.ResolutionGroup.ActionItemExecutionTasks.Contains(draftActionItem))
        _obj.ResolutionGroup.ActionItemExecutionTasks.Remove(draftActionItem);
    }
    
    #region Функции вычисляемых выражений
    
    [ExpressionElement("GetAddresseesCountExpressionName", "GetAddresseesCountExpressionDescription")]
    public static int GetAddresseesCount(IDocumentReviewTask task)
    {
      return task.Addressees.Count;
    }
    
    [ExpressionElement("NeedReviewWithDraftResolutionName", "NeedReviewWithDraftResolutionDescription")]
    public static bool NeedReviewWithDraftResolution(IDocumentReviewTask task)
    {
      var addressees = task.Addressees.Select(a => a.Addressee).ToList();
      var assistants = addressees
        .SelectMany(x => Company.PublicFunctions.Employee.GetManagerAssistantsWhoPrepareDraftResolution(x));
      
      // Нет помощника - рассмотрение без проекта резолюции.
      if (!assistants.Any())
        return false;
      
      // У помощника не было возможности подготовить проект резолюции - рассмотрение с проектом резолюции.
      return !Functions.DocumentReviewTask.CouldAuthorPrepareDraftResolutionByNow(task);
    }
    
    [ExpressionElement("ReviewWithDraftResolutionExpressionName", "ReviewWithDraftResolutionExpressionDescription")]
    public static bool ReviewWithDraftResolution(IDocumentReviewTask task)
    {
      var addressees = task.Addressees.Select(a => a.Addressee).ToList();
      var assistants = addressees
        .SelectMany(x => Company.PublicFunctions.Employee.GetManagerAssistantsWhoPrepareDraftResolution(x));
      
      // Нет помощника - рассмотрение без проекта резолюции.
      if (!assistants.Any())
        return false;
      
      // У помощника не было возможности подготовить проект резолюции - рассмотрение с проектом резолюции.
      if (!Functions.DocumentReviewTask.CouldAuthorPrepareDraftResolutionByNow(task))
        return true;
      
      // Помощник создал проект резолюции - рассмотрение с проектом, иначе - без него.
      return task.ResolutionGroup.All.Any();
    }
    
    [ExpressionElement("HasDraftResolutionBeenPreparedExpressionName", "HasDraftResolutionBeenPreparedExpressionDescription")]
    public static bool DraftResolutionExists(IDocumentReviewTask task)
    {
      if (!Functions.DocumentReviewTask.CouldAuthorPrepareDraftResolutionByNow(task))
        return false;
      
      return task.ResolutionGroup.All.Any();
    }
    
    /// <summary>
    /// Была ли у инициатора возможность подготовить проект резолюции к текущему моменту.
    /// </summary>
    /// <returns>True, если возможность была, иначе False.</returns>
    public virtual bool CouldAuthorPrepareDraftResolutionByNow()
    {
      var lastAssignment = Assignments.GetAll(x => Equals(x.Task, _obj)).OrderByDescending(x => x.Created).FirstOrDefault();
      var isForwarded = lastAssignment != null &&
        (lastAssignment.Result == Sungero.RecordManagement.DocumentReviewAssignment.Result.Forward ||
         lastAssignment.Result == Sungero.RecordManagement.ReviewReworkAssignment.Result.Forward);
      var isRework = Sungero.RecordManagement.ReviewReworkAssignments.Is(lastAssignment);
      var canAuthorPrepareResolution = Functions.DocumentReviewTask.CanAuthorPrepareResolution(_obj);
      
      // Исключаем ситуации, когда инициатор не может готовить проекты резолюции (проекты подготовить нельзя)
      // либо руководитель переадресовал задание другому адресату (все проекты будут удалены).
      // В остальных случаях возможность подготовить проект была.
      var result = (!isForwarded || isForwarded && isRework) && canAuthorPrepareResolution;
      return result;
    }
    
    [ExpressionElement("GetDefaultDeadlineExpressionName", "GetDefaultDeadlineExpressionDescription", "", "GetDefaultDeadlineAddressee")]
    public static DateTime? GetDefaultDeadline(IDocumentReviewTask task, IEmployee addressee)
    {
      // Если указан срок - вычислить количество рабочих часов на рассмотрение.
      if (!task.Deadline.HasValue || !task.Started.HasValue)
        return null;

      var deadline = Docflow.PublicFunctions.Module.GetDateWithTime(task.Deadline.Value, task.Author);
      var deadlineInWorkingHours = WorkingTime.GetDurationInWorkingHours(task.Started.Value, deadline, task.Author);
      if (deadlineInWorkingHours <= 0)
        deadlineInWorkingHours = 1;
      
      // Добавить руководителю количество рабочих часов в его часовом поясе.
      return Calendar.Now.AddWorkingHours(addressee, deadlineInWorkingHours);
    }
    
    [ExpressionElement("GetActionItemClerkExpressionName", "GetActionItemClerkExpressionDescription")]
    public static IUser GetActionItemClerk(IDocumentReviewTask task)
    {
      return Functions.DocumentReviewTask.GetClerkToSendActionItem(task);
    }
    
    [ExpressionElement("AreTasksCreatedForAllAddresseesExpressionName", "AreTasksCreatedForAllAddresseesExpressionDescription")]
    public static bool AreAllTasksCreated(IDocumentReviewTask task)
    {
      return task.Addressees.All(x => x.TaskCreated == true);
    }
    
    #endregion
    
    /// <summary>
    /// Добавить задачу на исполнение поручения в группу вложений проектов резолюции задачи.
    /// </summary>
    /// <param name="actionItem">Задача на исполнение поручения.</param>
    public virtual void AddActionItemToResolutionGroup(IActionItemExecutionTask actionItem)
    {
      if (!_obj.ResolutionGroup.ActionItemExecutionTasks.Any(x => Equals(x, actionItem)))
      {
        _obj.ResolutionGroup.ActionItemExecutionTasks.Add(actionItem);
        Functions.Module.SynchronizeAttachmentsToActionItem(_obj, actionItem);
        actionItem.Save();
      }
    }
    
    /// <summary>
    /// Проверить, что необходима подготовка проекта поручения.
    /// </summary>
    /// <param name="task">Задача на рассмотрение документа.</param>
    /// <returns>True - если в системе настроена подготовка проектов поручений виртуальным ассистентом.</returns>
    [Public, ExpressionElement("AIAssistantDraftActionItem", "")]
    public static bool WithDraftActionItem(IDocumentReviewTask task)
    {
      if (!Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Sungero.Commons.PublicConstants.Module.IntelligenceGuid))
        return false;

      var document = task.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      return RecordManagement.Functions.Module.CanAiAssistantPrepareDrafts(task.Id, task.Addressee, RecordManagement.ActionItemPredictionInfo.TaskType.DocumentReview, document);
    }
    
    /// <summary>
    /// Проверить, что автор задачи является помощником, который готовит проекты резолюций.
    /// </summary>
    /// <param name="task">Задача на рассмотрение документа.</param>
    /// <returns>True - автор является помощником.</returns>
    [Public, ExpressionElement("IsAuthorAssistant", "")]
    public static bool IsAuthorAssistant(IDocumentReviewTask task)
    {
      var author = Company.Employees.As(task.Author);
      if (author == null)
        return false;
      return Company.PublicFunctions.Employee.CanPrepareDraftResolutionForManager(author, task.Addressee);
    }
  }
}