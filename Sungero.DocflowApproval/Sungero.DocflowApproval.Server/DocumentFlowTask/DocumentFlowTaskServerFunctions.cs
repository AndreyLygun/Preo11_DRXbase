using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.OfficialDocument;
using Sungero.DocflowApproval;
using Sungero.DocflowApproval.DocumentFlowTask;
using Sungero.DocflowApproval.EntityReworkAssignment;
using Sungero.RecordManagement;
using Sungero.Workflow;

namespace Sungero.DocflowApproval.Server
{
  partial class DocumentFlowTaskFunctions
  {
    #region Кеширование параметров видимости и доступности в EntityParams
    
    /// <summary>
    /// Закешировать свойства блока и другие признаки в параметры сущности.
    /// </summary>
    [Remote]
    public virtual void FillEntityParams()
    {
      var entityBoolParams = new Dictionary<string, bool>();
      
      entityBoolParams.Add(Constants.Module.IsSendingToCounterpartyEnabledInSchemeParamName, Functions.Module.IsSendingToCounterpartyEnabledInScheme(_obj));
      entityBoolParams.Add(Constants.Module.HasAnyDocumentReviewInSchemeParamName, RecordManagement.PublicFunctions.Module.Remote.HasAnyTypeDocumentReviewBlockInScheme(_obj));
      
      foreach (var parameter in entityBoolParams)
        Sungero.Commons.PublicFunctions.Module.AddOrUpdateEntityParams(_obj, parameter.Key, parameter.Value);
    }
    
    #endregion
    
    /// <summary>
    /// Валидация наличия исполнителей блоков подписания и наличия прав на утверждение.
    /// </summary>
    /// <returns>Пустой список - если удалось вычислить всех исполнителей и у всех есть права, иначе - список ошибок.</returns>
    [Remote(IsPure = true)]
    public virtual List<string> ValidateSigningBlocksPerformers()
    {
      var errors = new List<string>();
      var document = _obj.DocumentGroup.ElectronicDocuments.First();
      var signingBlocks = Blocks.SigningBlocks.GetAll(_obj.Scheme);
      
      foreach (var block in signingBlocks)
      {
        if (block.Performers.Any())
          errors.AddRange(this.ValidateBlockPerformersApproveRights(block, document));
        else
        {
          errors.Add(DocumentFlowTasks.Resources.FailedToComputeBlockPerformerFormat(block.Title));
          Logger.DebugFormat("Failed to compute block performer in document flow task. " +
                             "BlockId: {0}, taskId: {1}.", block.Id, _obj.Id);
        }
      }
      
      return errors;
    }
    
    /// <summary>
    /// Валидация прав на утверждение документа у исполнителей блока.
    /// </summary>
    /// <param name="block">Блок схемы.</param>
    /// <param name="document">Утверждаемый документ.</param>
    /// <returns>Пустой список - если у всех исполнителей блока есть права на утверждение, иначе - список ошибок.</returns>
    public virtual List<string> ValidateBlockPerformersApproveRights(Sungero.Core.IAssignmentSchemeBlock block, IElectronicDocument document)
    {
      var errors = new List<string>();
      
      var employees = Company.PublicFunctions.Module.GetEmployeesFromRecipients(block.Performers.ToList()).Distinct();
      foreach (var employee in employees)
      {
        var employeeCanApprove = string.IsNullOrEmpty(Functions.Module.CheckEmployeeRightsToApprove(employee, document));
        if (!employeeCanApprove)
        {
          errors.Add(DocumentFlowTasks.Resources.BlockPerformerHasNoRightsToApproveDocumentFormat(employee.Name, block.Title));
          Logger.DebugFormat("Block performer in document flow task has no rights to approve document. " +
                             "PerformerId: {0}, blockId: {1}, taskId: {2}, documentId: {3}",
                             employee.Id, block.Id, _obj.Id, document.Id);
        }
      }
      
      return errors;
    }
    
    /// <summary>
    /// Получить дату последнего изменения задачи.
    /// </summary>
    /// <returns>Дата последнего изменения задачи.</returns>
    [Remote(IsPure = true)]
    public virtual DateTime? GetDocumentFlowTaskModified()
    {
      return DocumentFlowTasks.GetAll().Where(t => t.Id == _obj.Id).Select(t => t.Modified).FirstOrDefault();
    }
    
    /// <summary>
    /// Получение последнего задания на доработку.
    /// </summary>
    /// <returns>Последнее задание на доработку.</returns>
    public IEntityReworkAssignment GetLastReworkAssignment()
    {
      return EntityReworkAssignments
        .GetAll(a => Equals(a.Task, _obj) && a.Created > _obj.Started)
        .OrderByDescending(asg => asg.Created)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Установить способ отправки документа по умолчанию.
    /// </summary>
    [Public]
    public virtual void SetDefaultDeliveryMethod()
    {
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.First());
      if (OutgoingLetters.As(officialDocument)?.IsManyAddressees == true)
        return;
      
      _obj.ExchangeService = Functions.Module.GetExchangeServices(officialDocument).DefaultService;
      if (_obj.ExchangeService != null)
        _obj.DeliveryMethod = Docflow.PublicFunctions.MailDeliveryMethod.Remote.GetExchangeDeliveryMethod();
      else
        _obj.DeliveryMethod = officialDocument?.DeliveryMethod;
    }
    
    /// <summary>
    /// Обновить свойства документа при старте задачи на согласование по процессу.
    /// </summary>
    /// <param name="document">Документ.</param>
    public virtual void UpdateOfficialDocumentStateOnStart(IOfficialDocument document)
    {
      Docflow.PublicFunctions.OfficialDocument.SetLifeCycleStateDraft(document);
      Docflow.PublicFunctions.OfficialDocument.SetDeliveryMethod(document, _obj.DeliveryMethod);
    }
    
    /// <summary>
    /// Получить основной документ.
    /// </summary>
    /// <returns>Документ.</returns>
    public virtual IElectronicDocument GetApprovalDocument()
    {
      return _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
    }
    
    #region Предметное отображение

    /// <summary>
    /// Построить модель состояния согласования по процессу.
    /// </summary>
    /// <returns>Схема модели состояния.</returns>
    [Public, Remote(IsPure = true)]
    public string GetStateViewXml()
    {
      return this.GetStateView().ToString();
    }
    
    /// <summary>
    /// Построить предметное отображение согласования по процессу.
    /// </summary>
    /// <returns>Предметное отображение.</returns>
    [Remote(IsPure = true)]
    [LocalizeFunction("GetStateViewFunctionName", "GetStateViewFunctionDescription")]
    public Sungero.Core.StateView GetStateView()
    {
      var stateView = StateView.Create();
      
      var startedByUser = Sungero.CoreEntities.Users.As(Sungero.Workflow.WorkflowHistories.GetAll()
                                                        .Where(h => h.EntityId == _obj.Id)
                                                        .Where(h => h.Operation == Sungero.Workflow.WorkflowHistory.Operation.Start)
                                                        .OrderBy(h => h.HistoryDate)
                                                        .Select(h => h.User)
                                                        .FirstOrDefault());
      
      if (_obj.Started.HasValue)
        Docflow.PublicFunctions.OfficialDocument
          .AddUserActionBlock(stateView, _obj.Author, DocumentFlowTasks.Resources.DocumentIsSentForApprovalByProcess, _obj.Started.Value, _obj, null, startedByUser);
      else
        Docflow.PublicFunctions.OfficialDocument
          .AddUserActionBlock(stateView, _obj.Author, DocumentFlowTasks.Resources.DraftTaskCreated, _obj.Created.Value, _obj, null, _obj.Author);
      
      // Добавить основной блок для задачи.
      var taskBlock = this.AddTaskBlock(stateView);

      return stateView;
    }
    
    /// <summary>
    /// Построить модель состояния согласования по процессу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Схема модели состояния.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView(Sungero.Docflow.IOfficialDocument document)
    {
      if (Equals(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault(), document) ||
          _obj.AddendaGroup.ElectronicDocuments.Any(x => Equals(x, document)))
      {
        return this.GetStateView();
      }
      
      return null;
    }
    
    /// <summary>
    /// Добавить в контрол состояния блок задачи на согласование.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <returns>Добавленный блок.</returns>
    private StateBlock AddTaskBlock(StateView stateView)
    {
      var taskBlock = stateView.AddBlock();
      
      var isDraft = _obj.Status == Workflow.Task.Status.Draft;
      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle(isDraft);
      var labelStyle = Docflow.PublicFunctions.Module.CreateStyle(false, isDraft, false);
      
      taskBlock.Entity = _obj;
      taskBlock.AssignIcon(OfficialDocuments.Info.Actions.SendForDocumentFlow, StateBlockIconSize.Large);
      taskBlock.IsExpanded = _obj.Status == Workflow.Task.Status.InProcess;
      taskBlock.AddLabel(DocumentFlowTasks.Resources.ProcessBasedApproval, headerStyle);
      taskBlock.AddLineBreak();
      taskBlock.AddLabel(DocumentFlowTasks.Resources.BaseProcess, labelStyle);
      taskBlock.AddLabel(_obj.ProcessKind.DisplayValue, Docflow.PublicFunctions.Module.CreateStyle(Sungero.Core.Colors.Common.DarkBlue));
      this.AddStatus(taskBlock, labelStyle);
      return taskBlock;
    }
    
    private void AddStatus(Sungero.Core.StateBlock taskBlock, Sungero.Core.StateBlockLabelStyle labelStyle)
    {
      var status = string.Empty;
      if (_obj.Status == Workflow.Task.Status.InProcess)
        status = ApprovalTasks.Resources.StateViewInProcess;
      else if (_obj.Status == Workflow.Task.Status.Completed)
        status = ApprovalTasks.Resources.StateViewCompleted;
      else if (_obj.Status == Workflow.Task.Status.Aborted)
        status = ApprovalTasks.Resources.StateViewAborted;
      else if (_obj.Status == Workflow.Task.Status.Suspended)
        status = ApprovalTasks.Resources.StateViewSuspended;
      else if (_obj.Status == Workflow.Task.Status.Draft)
        status = ApprovalTasks.Resources.StateViewDraft;
      
      Docflow.PublicFunctions.Module.AddInfoToRightContent(taskBlock, status, labelStyle);
    }
    
    #endregion
    
    #region Прекращение задачи
        
    /// <summary>
    /// Отправить уведомления о прекращении задачи на согласование по процессу.
    /// </summary>
    public virtual void SendApprovalAbortNotice()
    {
      var document = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      
      var subject = string.Empty;
      var threadSubject = string.Empty;
      
      // Отправить уведомления о прекращении.
      using (Sungero.Core.CultureInfoExtensions.SwitchTo(TenantInfo.Culture))
      {
        threadSubject = DocumentFlowTasks.Resources.AbortNoticeSubject;
        if (document != null)
          subject = string.Format(Sungero.Exchange.Resources.TaskSubjectTemplate, threadSubject, Docflow.PublicFunctions.Module.TrimSpecialSymbols(document.Name));
        else
        {
          var approvalTaskSubject = string.Format("{0}{1}", _obj.Subject.Substring(0, 1).ToLower(), _obj.Subject.Remove(0, 1));
          subject = string.Format("{0} {1}", DocumentFlowTasks.Resources.AbortApprovalTask, Docflow.PublicFunctions.Module.TrimSpecialSymbols(approvalTaskSubject));
        }
      }
      
      var addressees = this.GetAbortNotificationAddressees();
      if (addressees.Any())
        Docflow.PublicFunctions.Module.Remote.SendNoticesAsSubtask(subject, addressees, _obj, _obj.AbortingReason, Users.Current, threadSubject);
    }
    
    /// <summary>
    /// Получить всех пользователей, кого нужно уведомить о прекращении задачи.
    /// </summary>
    /// <returns>Список пользователей, кого нужно уведомить.</returns>
    public virtual List<IUser> GetAbortNotificationAddressees()
    {
      var allPerformers = Assignments.GetAll(asg => Equals(asg.Task, _obj) && asg.IsRead.Value).Select(app => app.Performer).ToList();
      allPerformers.Add(_obj.Author);
      allPerformers = allPerformers.Distinct().ToList();
      
      allPerformers.Remove(Users.Current);
      return allPerformers;
    }
    
    /// <summary>
    /// Выполнить действия, необходимые при прекращении задачи.
    /// </summary>
    /// <param name="setObsolete">Пометить документ как устаревший.</param>
    public virtual void ProcessTaskAbortAsync(bool setObsolete)
    {
      var asyncHandler = DocflowApproval.AsyncHandlers.ProcessDocumentFlowTaskAbort.Create();
      asyncHandler.TaskId = _obj.Id;
      asyncHandler.SetObsolete = setObsolete;
      asyncHandler.Aborted = Calendar.Now;
      
      asyncHandler.ExecuteAsync();
    }
    
    /// <summary>
    /// Выполнить действия, необходимые при прекращении задачи.
    /// </summary>
    /// <param name="setObsolete">Пометить документ как устаревший.</param>
    public virtual void ProcessTaskAbort(bool setObsolete)
    {
      var document = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (document == null)
        return;
      
      this.SetPrimaryDocumentStatesOnAbort(document, setObsolete);
      this.GrantAccessRightsForAttachmentsToInitiatorOnAbort();
      this.AbortAllSubDocumentReviewTasks();
    }
    
    /// <summary>
    /// Установить статусы основного документа при прекращении задачи.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="setObsolete">Пометить документ устаревшим.</param>
    public virtual void SetPrimaryDocumentStatesOnAbort(IOfficialDocument document, bool setObsolete)
    {
      Docflow.PublicFunctions.OfficialDocument.SetDocumentStateAborted(document, setObsolete);
      
      if (document.ExecutionState != null &&
          !Docflow.PublicFunctions.OfficialDocument.Remote.HasActiveOrCompletedActionItems(document) &&
          !Functions.Module.HasOtherDocumentProcessingAssignments(_obj, document) &&
          !Functions.Module.HasApprovalReviewOrExecutionAssignments(document) &&
          !Functions.Module.HasDocumentReviewTasks(document))
      {
        document.ExecutionState = null;
      }
    }
    
    /// <summary>
    /// Вернуть права инициатору после прекращения задачи.
    /// </summary>
    public virtual void GrantAccessRightsForAttachmentsToInitiatorOnAbort()
    {
      Logger.Debug("Start GrantAccessRightsForAttachmentsToInitiatorOnAbort");
      
      var allTaskDocuments = this.GetRevokedElectronicDocuments();
      foreach (var document in allTaskDocuments)
      {
        if (document.AccessRights.CanUpdate(_obj.Author) ||
            _obj.RevokedDocumentsRights
            .Any(r => r.EntityId == document.Id && r.RightType != DocflowApproval.DocumentFlowTaskRevokedDocumentsRights.RightType.Read))
        {
          Docflow.PublicFunctions.Module.GrantAccessRightsOnDocument(document, _obj.Author, DefaultAccessRightsTypes.Change);
        }
        else
        {
          Docflow.PublicFunctions.Module.GrantAccessRightsOnDocument(document, _obj.Author, DefaultAccessRightsTypes.Read);
        }
      }
      
      Logger.Debug("Done GrantAccessRightsForAttachmentsToInitiatorOnAbort");
    }
    
    /// <summary>
    /// Прекратить все подчиненные задачи на рассмотрение документа.
    /// </summary>
    public virtual void AbortAllSubDocumentReviewTasks()
    {
      var tasks = _obj.Subtasks.OfType<IDocumentReviewTask>()
        .Where(x => x.Status == Workflow.Task.Status.InProcess);
      foreach (var task in tasks)
      {
        Logger.DebugFormat("DocumentFlowTask. Abort sub DocumentReviewTask. DocumentFlowTask (ID={0}) (StartId={1}). DocumentReviewTask (ID={2})", _obj.Id, _obj.StartId, task.Id);
        task.Abort();
      }
    }
    
    /// <summary>
    /// Возможно ли выполнить действия, необходимые при прекращении задачи, синхронно.
    /// </summary>
    /// <returns>True - возможно, False - иначе.</returns>
    public virtual bool CanAbortSynchronously()
    {
      return this.CanAbortSynchronouslyDocumentCondition() &&
        this.CanAbortSynchronouslySubDocumentReviewTaskCondition();
    }
    
    /// <summary>
    /// Возможно ли выполнить действия, необходимые при прекращении задачи, синхронно в зависимости от основного документа.
    /// </summary>
    /// <returns>True - возможно, False - иначе.</returns>
    public virtual bool CanAbortSynchronouslyDocumentCondition()
    {
      var documents = this.GetRevokedElectronicDocuments();
      if (!documents.Any())
        return true;
      
      foreach (var document in documents)
      {
        if (!document.AccessRights.CanUpdate() || Locks.GetLockInfo(document).IsLocked)
          return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Получить список документов задачи, на которые были понижены права у пользователя.
    /// </summary>
    /// <returns>Список документов задачи.</returns>
    public virtual List<IElectronicDocument> GetRevokedElectronicDocuments()
    {
      var revokedDocumentIds = _obj.RevokedDocumentsRights.Select(d => d.EntityId).ToList();
      return _obj.AllAttachments
        .Where(d => ElectronicDocuments.Is(d))
        .Select(d => ElectronicDocuments.As(d))
        .Where(d => revokedDocumentIds.Contains(d.Id))
        .ToList();
    }
    
    /// <summary>
    /// Возможно ли выполнить действия, необходимые при прекращении задачи, синхронно в зависимости от подчиненных задач на рассмотрение документа.
    /// </summary>
    /// <returns>True - возможно, False - иначе.</returns>
    public virtual bool CanAbortSynchronouslySubDocumentReviewTaskCondition()
    {
      var subDocumentReviewTasks = _obj.Subtasks.OfType<IDocumentReviewTask>();
      if (!subDocumentReviewTasks.Any())
        return true;
      
      return subDocumentReviewTasks.All(x => x.AccessRights.CanUpdate()) &&
        subDocumentReviewTasks.All(x => !Locks.GetLockInfo(x).IsLocked);
    }
    
    #endregion
  }
}