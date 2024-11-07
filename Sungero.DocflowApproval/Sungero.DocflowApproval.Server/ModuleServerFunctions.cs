using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using Sungero.RecordManagement;
using Sungero.Workflow;

namespace Sungero.DocflowApproval.Server
{
  public class ModuleFunctions
  {
    #region Наличие блоков на схеме варианта процесса

    /// <summary>
    /// Проверить, есть ли хотя бы одна отправка контрагенту в схеме задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>True - если отправка включена, иначе - false.</returns>
    [Public, Remote]
    public static bool IsSendingToCounterpartyEnabledInScheme(ITask task)
    {
      var blocks = Blocks.DocumentProcessingBlocks.GetAll(task.Scheme);

      if (blocks.Any(b => b.SendToCounterparty == true))
        return true;

      return false;
    }

    #endregion

    /// <summary>
    /// Проверить права текущего сотрудника на утверждение документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Текст ошибки, если прав нет, иначе - пустая строка.</returns>
    [Remote]
    public virtual string CheckCurrentEmployeeRightsToApprove(IElectronicDocument document)
    {
      return this.CheckEmployeeRightsToApprove(Employees.Current, document);
    }

    /// <summary>
    /// Проверить права сотрудника на утверждение документа.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Текст ошибки, если прав нет, иначе - пустая строка.</returns>
    [Remote]
    public virtual string CheckEmployeeRightsToApprove(IEmployee employee, IElectronicDocument document)
    {
      if (OfficialDocuments.Is(document))
      {
        // Поиск прав подписи документа.
        var canSignByEmployee = Docflow.PublicFunctions.OfficialDocument.Remote
          .CanSignByEmployee(OfficialDocuments.As(document), employee);

        if (!canSignByEmployee)
          return Docflow.Resources.NoRightsToApproveDocument;
      }

      if (!document.AccessRights.CanApprove(employee))
        return Docflow.Resources.NoAccessRightsToApprove;

      return string.Empty;
    }

    /// <summary>
    /// Связать с основным документом документы из списка, если они не были связаны ранее.
    /// </summary>
    /// <param name="primaryDocument">Основной документ.</param>
    /// <param name="documents">Список документов.</param>
    [Public]
    public virtual void RelateDocumentsToPrimaryDocumentAsAddenda(IElectronicDocument primaryDocument, List<IElectronicDocument> documents)
    {
      var primaryDocumentAddenda = Docflow.PublicFunctions.Module.GetAddenda(primaryDocument);
      var notRelatedToPrimaryDocumentAddenda = documents.Except(primaryDocumentAddenda);

      foreach (var addendum in notRelatedToPrimaryDocumentAddenda)
      {
        if (this.IsAddendumAlreadyRelatedToPrimaryDocument(primaryDocument, addendum))
          continue;

        try
        {
          addendum.Relations.AddFromOrUpdate(Docflow.PublicConstants.Module.AddendumRelationName, null, primaryDocument);
          addendum.Save();
          Logger.DebugFormat("RelateDocumentsToPrimaryDocumentAsAddenda. Success. Primary document (ID={0}). Addendum (ID={1})",
                             primaryDocument.Id, addendum.Id);
        }
        catch (Sungero.Domain.Shared.Exceptions.SessionException ex)
        {
          Logger.ErrorFormat("RelateDocumentsToPrimaryDocumentAsAddenda. Failed. Primary document (ID={0}). Addendum (ID={1})",
                             ex, primaryDocument.Id, addendum.Id);
        }
      }
    }

    /// <summary>
    /// Проверить наличие связи между документом и приложением.
    /// </summary>
    /// <param name="primaryDocument">Основной документ.</param>
    /// <param name="addendum">Приложение.</param>
    /// <returns>True - если документы связаны, иначе - False.</returns>
    public virtual bool IsAddendumAlreadyRelatedToPrimaryDocument(IElectronicDocument primaryDocument, IElectronicDocument addendum)
    {
      var addendumIsAlreadyRelatedToThePrimary = Sungero.Content.DocumentRelations.GetAll()
        .Any(x => Equals(x.Source, primaryDocument) && Equals(x.Target, addendum) ||
             Equals(x.Source, addendum) && Equals(x.Target, primaryDocument));
      var addendumAsAddendum = Addendums.As(addendum);
      return addendumIsAlreadyRelatedToThePrimary ||
        addendumAsAddendum != null && !Equals(addendumAsAddendum.LeadingDocument, primaryDocument);
    }

    /// <summary>
    /// Проверить, согласовал ли исполнитель документ в рамках задачи.
    /// </summary>
    /// <param name="task">Родительская задача.</param>
    /// <param name="performer">Исполнитель.</param>
    /// <param name="previousReworkAsg">Предыдущее задание на доработку.</param>
    /// <returns>True - исполнитель уже согласовал документ, False - нет.</returns>
    public virtual bool PerformerHasAlreadyApproved(ITask task, IRecipient performer, IEntityReworkAssignment previousReworkAsg)
    {
      var lastReworkCompleted = previousReworkAsg?.Completed;
      
      // При каждом рестарте время старта обновляется. Ищем согласованные задания либо со времени последнего старта задачи,
      // либо со времени последней доработки, если она была позже.
      var timeToCountFrom = task.Started;
      if (lastReworkCompleted > timeToCountFrom)
        timeToCountFrom = lastReworkCompleted;

      var assignmentsApprovedByPerformer = EntityApprovalAssignments
        .GetAll(a => Equals(a.Task, task) && Equals(a.Performer, performer) && Equals(a.CompletedBy, performer) &&
                (a.Result == DocflowApproval.EntityApprovalAssignment.Result.Approved ||
                 a.Result == DocflowApproval.EntityApprovalAssignment.Result.WithSuggestions) &&
                a.Created >= timeToCountFrom);

      if (assignmentsApprovedByPerformer.Any())
      {
        var lastReworkText = lastReworkCompleted?.ToString() ?? "none";
        foreach (var assignment in assignmentsApprovedByPerformer.ToList())
          Logger.Debug($"Task id={task.Id}. Excluded from approval: performer id={performer.Id}, previous approval assignment id={assignment.Id}, searched from time: {timeToCountFrom}, last rework time: {lastReworkText}.");
        return true;
      }
      else
        return false;
    }

    /// <summary>
    /// Проверить, подписал ли исполнитель документ в рамках задачи.
    /// </summary>
    /// <param name="task">Родительская задача.</param>
    /// <param name="performer">Исполнитель.</param>
    /// <returns>True - исполнитель уже подписал документ, False - нет.</returns>
    public virtual bool PerformerHasAlreadySigned(ITask task, IRecipient performer)
    {
      // При каждом рестарте время старта обновляется. Ищем согласованные задания либо со времени последнего старта задачи,
      // либо со времени последней доработки, если она была позже.
      var lastReworkCompleted = this.GetLastReworkCompletedTime(task);
      var timeToCountFrom = task.Started;
      if (lastReworkCompleted > timeToCountFrom)
        timeToCountFrom = lastReworkCompleted;

      var assignmentsSignedByPerformer = SigningAssignments.GetAll()
        .Where(a => Equals(a.Task, task) && Equals(a.Performer, performer) && Equals(a.CompletedBy, performer) &&
               a.Result == DocflowApproval.SigningAssignment.Result.Sign &&
               a.Created >= timeToCountFrom);

      if (assignmentsSignedByPerformer.Any())
      {
        var lastReworkText = lastReworkCompleted?.ToString() ?? "none";
        foreach (var assignment in assignmentsSignedByPerformer.ToList())
          Logger.Debug($"Task id={task.Id}. Excluded from signing: performer id={performer.Id}, previous signing assignment id={assignment.Id}, searched from time: {timeToCountFrom}, last rework time: {lastReworkText}.");
        return true;
      }
      else
        return false;
    }

    /// <summary>
    /// Получить время последнего выполненного задания на доработку в рамках задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Время последнего выполненного задания на доработку или null, если задания не было.</returns>
    public virtual DateTime? GetLastReworkCompletedTime(ITask task)
    {
      return this.GetLastCompletedReworkAssignment(task)?.Completed;
    }

    /// <summary>
    /// Получить список новых исполнителей из блока согласования, добавленных на предыдущих кругах.
    /// </summary>
    /// <param name="blockPerformers">Исполнители блока.</param>
    /// <param name="blockId">ИД блока.</param>
    /// <param name="previousReworkAsg">Предыдущее задание на доработку.</param>
    /// <returns>Исполнители, не попавшие в блок.</returns>
    public virtual List<IEmployee> GetAddedApprovers(List<IEmployee> blockPerformers, string blockId, IEntityReworkAssignment previousReworkAsg)
    {
      if (previousReworkAsg == null)
        return null;

      return previousReworkAsg.Approvers
        .Where(a => a.BlockId == blockId &&
               !blockPerformers.Contains(a.Approver) &&
               a.Action == DocflowApproval.EntityReworkAssignmentApprovers.Action.SendForApproval)
        .Select(a => a.Approver)
        .ToList();
    }

    /// <summary>
    /// Проверить, отправлять ли задание исполнителю в текущем круге согласования.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="performer">Исполнитель.</param>
    /// <param name="blockId">ИД блока согласования.</param>
    /// <param name="previousReworkAsg">Предыдущее задание на доработку.</param>
    /// <returns>True - отправить на согласование, иначе - false.</returns>
    public virtual bool NeedSendForApproval(ITask task, IRecipient performer, string blockId, IEntityReworkAssignment previousReworkAsg)
    {
      // Первый круг согласования - отправлять безусловно.
      if (previousReworkAsg == null)
        return true;

      // Повторный круг - исполнитель считается актуальным, если его нет в списке уже согласовавших,
      // либо есть и ему явно назначено отправить задание.
      var approvers = previousReworkAsg.Approvers.Where(a => a.BlockId == blockId &&
                                                        a.Approver.Equals(performer));
      if (!approvers.Any())
        return true;
      
      return this.NeedForcedSendForApproval(performer, blockId, previousReworkAsg);
    }

    /// <summary>
    /// Проверить, есть ли в списке согласующих явное указание отправить исполнителю задание.
    /// </summary>
    /// <param name="performer">Исполнитель.</param>
    /// <param name="blockId">ИД блока согласования.</param>
    /// <param name="previousReworkAsg">Предыдущее задание на доработку.</param>
    /// <returns>True - исполнителю назначено отправить задание, False - не назначено.</returns>
    public virtual bool NeedForcedSendForApproval(IRecipient performer, string blockId, IEntityReworkAssignment previousReworkAsg)
    {
      if (previousReworkAsg == null)
        return false;
      
      var approvers = previousReworkAsg.Approvers.Where(a => a.BlockId == blockId &&
                                                        a.Approver.Equals(performer));
      return approvers.Any(a => a.Action == DocflowApproval.EntityReworkAssignmentApprovers.Action.SendForApproval);
    }

    /// <summary>
    /// Создать задачу на согласование документа по процессу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Задача на согласование документа по процессу.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IDocumentFlowTask CreateDocumentFlowTask(IOfficialDocument document)
    {
      var task = DocumentFlowTasks.Create();
      task.DocumentGroup.ElectronicDocuments.Add(document);

      return task;
    }

    /// <summary>
    /// Получить созданные задачи на согласование документа по процессу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Список созданных задач на согласование документа по процессу.</returns>
    [Public, Remote]
    public IQueryable<IDocumentFlowTask> GetDocumentFlowTasks(IElectronicDocument document)
    {
      var docGuid = document.GetEntityMetadata().GetOriginal().NameGuid;
      var documentFlowTaskDocumentGroupGuid = Constants.Module.DocumentFlowTaskOfficialDocumentGroupGuid;
      return DocumentFlowTasks.GetAll()
        .Where(t => t.Status == Workflow.Task.Status.InProcess ||
               t.Status == Workflow.Task.Status.Suspended)
        .Where(t => t.AttachmentDetails
               .Any(att => att.AttachmentId == document.Id && att.EntityTypeGuid == docGuid &&
                    att.GroupId == documentFlowTaskDocumentGroupGuid));
    }

    /// <summary>
    /// Проверить, созданы ли по документу задания на обработку за рамками текущей задачи.
    /// </summary>
    /// <param name="task">Текущая задача.</param>
    /// <param name="document">Документ.</param>
    /// <returns>True, если по документу уже созданы задания на обработку за рамками текущей задачи.</returns>
    [Remote(IsPure = true)]
    public virtual bool HasOtherDocumentProcessingAssignments(ITask task, IOfficialDocument document)
    {
      var hasDocumentProcessingAssignments = false;

      Sungero.Core.AccessRights.AllowRead(
        () =>
        {
          hasDocumentProcessingAssignments = this.GetOtherDocumentProcessingAssignments(task, document).Any();
        });

      return hasDocumentProcessingAssignments;
    }

    /// <summary>
    /// Получение активных заданий на обработку документа за пределами текущей задачи.
    /// </summary>
    /// <param name="task">Текущая задача.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Активные задания на обработку документа за пределами текущей задачи.</returns>
    public virtual IQueryable<DocflowApproval.IDocumentProcessingAssignment> GetOtherDocumentProcessingAssignments(ITask task, IOfficialDocument document)
    {
      var typeGuid = document.GetEntityMetadata().GetOriginal().NameGuid;
      var groupId = Constants.Module.DocumentFlowTaskOfficialDocumentGroupGuid;
      return DocflowApproval.DocumentProcessingAssignments.GetAll(a => a.Status == Workflow.Assignment.Status.InProcess &&
                                                                  a.CreateActionItems == true &&
                                                                  !Equals(a.MainTask, task) &&
                                                                  a.MainTask.AttachmentDetails
                                                                  .Any(d => d.EntityTypeGuid == typeGuid &&
                                                                       d.GroupId == groupId &&
                                                                       d.AttachmentId == document.Id));
    }

    /// <summary>
    /// Проверить, есть ли по документу задания на рассмотрение или создание поручения в рамках согласования по регламенту.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если по документу есть задания на рассмотрение или создание поручения в рамках согласования по регламенту.</returns>
    [Remote(IsPure = true)]
    public virtual bool HasApprovalReviewOrExecutionAssignments(IOfficialDocument document)
    {
      var hasApprovalExecutionAssignments = false;

      Sungero.Core.AccessRights.AllowRead(
        () =>
        {
          hasApprovalExecutionAssignments = this.GetApprovalReviewOrExecutionAssignments(document).Any();
        });

      return hasApprovalExecutionAssignments;
    }

    /// <summary>
    /// Получение заданий на рассмотрение или создание поручения в рамках согласования по регламенту.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Задания на рассмотрение или создание поручения в рамках согласования по регламенту.</returns>
    public virtual IQueryable<IAssignment> GetApprovalReviewOrExecutionAssignments(IOfficialDocument document)
    {
      var groupId = Docflow.PublicConstants.Module.TaskMainGroup.ApprovalTask;
      var typeGuid = document.GetEntityMetadata().GetOriginal().NameGuid;
      return Assignments.GetAll().Where(a => a.Status == Workflow.Assignment.Status.InProcess &&
                                        (ApprovalReviewAssignments.Is(a) ||
                                         ApprovalExecutionAssignments.Is(a)) &&
                                        a.MainTask.AttachmentDetails
                                        .Any(d => d.AttachmentTypeGuid == typeGuid &&
                                             d.AttachmentId == document.Id &&
                                             d.GroupId == groupId));
    }

    /// <summary>
    /// Проверить, созданы ли по документу задачи на рассмотрение.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если по документу созданы задачи на рассмотрение.</returns>
    [Remote(IsPure = true)]
    public virtual bool HasDocumentReviewTasks(IOfficialDocument document)
    {
      var hasDocumentReviewTasks = false;

      Sungero.Core.AccessRights.AllowRead(
        () =>
        {
          hasDocumentReviewTasks = this.GetDocumentReviewTasks(document).Any();
        });

      return hasDocumentReviewTasks;
    }

    /// <summary>
    /// Получение активных задач на рассмотрение.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Активные задачи на рассмотрение.</returns>
    public virtual IQueryable<IDocumentReviewTask> GetDocumentReviewTasks(IOfficialDocument document)
    {
      var typeGuid = document.GetEntityMetadata().GetOriginal().NameGuid;
      var groupId = Docflow.PublicConstants.Module.TaskMainGroup.DocumentReviewTask;
      return DocumentReviewTasks.GetAll(t => t.Status == Workflow.Task.Status.InProcess &&
                                        t.AttachmentDetails.Any(d => d.EntityTypeGuid == typeGuid &&
                                                                d.GroupId == groupId &&
                                                                d.AttachmentId == document.Id));
    }

    /// <summary>
    /// Добавить исполнителя в задание согласования.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="newApprover">Новый согласующий.</param>
    /// <param name="deadline">Новый срок для задания.</param>
    [Public, Remote]
    public void AddApprover(IAssignment assignment, IEmployee newApprover, DateTime? deadline)
    {
      var operation = new Enumeration(Constants.EntityApprovalAssignment.AddApprover);
      assignment.Forward(newApprover, ForwardingLocation.Next, deadline);
      assignment.History.Write(operation, operation, Company.PublicFunctions.Employee.GetShortName(newApprover, false));
    }

    /// <summary>
    /// Получить доступные сервисы обмена.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Сервисы обмена.</returns>
    [Remote(IsPure = true)]
    public static Structures.Module.ExchangeServices GetExchangeServices(Docflow.IOfficialDocument document)
    {
      if (document == null)
        return Structures.Module.ExchangeServices.Create(new List<ExchangeCore.IExchangeService>(), null);

      var services = Docflow.PublicFunctions.OfficialDocument.GetExchangeServices(document);
      return Structures.Module.ExchangeServices.Create(services, services.FirstOrDefault());
    }

    /// <summary>
    /// Получить список сотрудников, у которых можно запросить продление срока.
    /// </summary>
    /// <param name="assignment">Задание, из которого запрашивают продление.</param>
    /// <returns>Список сотрудников.</returns>
    /// <remarks>Первый в списке сотрудник подставится по умолчанию в поле Кому в задаче на продление.</remarks>
    public virtual List<IUser> GetDeadlineAssignees(IAssignment assignment)
    {
      var assignees = new List<IUser>();
      var author = Company.Employees.As(assignment.Author);
      var performer = Company.Employees.As(assignment.Performer);

      // Порядок добавления сотрудников в список важен, т.к. первый в списке подставится по умолчанию.
      if (author != null && !Equals(author, performer))
        assignees.Add(author);
      
      var authorManager = Docflow.PublicFunctions.Module.Remote.GetManager(author);
      if (author != null && authorManager != null)
        assignees.Add(authorManager);
      
      var performerManager = Docflow.PublicFunctions.Module.Remote.GetManager(performer);
      if (performer != null && performerManager != null)
        assignees.Add(performerManager);
      
      if (author != null && !assignees.Contains(author))
        assignees.Add(author);
      
      return assignees.Distinct().ToList();
    }
    
    /// <summary>
    /// Получить список сотрудников без исполнителя, у которых можно запросить продление срока.
    /// </summary>
    /// <param name="assignment">Задание, из которого запрашивают продление.</param>
    /// <returns>Список сотрудников.</returns>
    /// <remarks>Первый в списке сотрудник подставится по умолчанию в поле Кому в задаче на продление.</remarks>
    public virtual List<IUser> GetDeadlineAssigneesWithoutPerformer(IAssignment assignment)
    {
      var assignees = new List<IUser>();
      var author = Company.Employees.As(assignment.Author);
      var performer = Company.Employees.As(assignment.Performer);
      
      // Порядок добавления сотрудников в список важен, т.к. первый в списке подставится по умолчанию.
      if (author != null && !Equals(author, performer))
        assignees.Add(author);
      
      var authorManager = Docflow.PublicFunctions.Module.Remote.GetManager(author);
      if (author != null && authorManager != null && !Equals(authorManager, performer))
        assignees.Add(authorManager);
      
      var performerManager = Docflow.PublicFunctions.Module.Remote.GetManager(performer);
      if (performer != null && performerManager != null && !Equals(performerManager, performer))
        assignees.Add(performerManager);
      
      if (!assignees.Any() && performer != null)
        assignees.Add(performer);
      
      return assignees.Distinct().ToList();
    }

    /// <summary>
    /// Получить ответственного за возврат.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача возврата.</param>
    /// <returns>Ответственный за возврат.</returns>
    /// <remarks>Ответственным считается:
    /// 1. Кому выдан из строки в Выдаче с действием Endorsement с пустой датой возврата;
    /// 2. Кому выдан из последней по дате выдачи строки в Выдаче с действием Sending;
    /// 3. Ответственный за документ, если это несистемный пользователь;
    /// 4. Инициатор задачи, если это несистемный пользователь;
    /// 5. Кому выдан из последней по дате выдачи строки в Выдаче с действием Endorsement;
    /// Null, если не удалось определить ответственного за возврат.</remarks>
    [Public]
    public virtual IEmployee GetResponsibleToReturn(IOfficialDocument document, ITask task)
    {
      var unreturnedEndorsementTracking = Docflow.PublicFunctions.OfficialDocument.GetUnreturnedFromCounterpartyTracking(document, task);
      if (unreturnedEndorsementTracking.Any())
        return unreturnedEndorsementTracking.OrderByDescending(x => Equals(x.ReturnTask, task)).FirstOrDefault()?.DeliveredTo;

      var sendingTracking = document.Tracking.Where(x => x.Action == Docflow.OfficialDocumentTracking.Action.Sending);
      if (sendingTracking.Any())
        return sendingTracking.OrderByDescending(x => x.DeliveryDate).FirstOrDefault()?.DeliveredTo;

      var responsible = Docflow.PublicFunctions.OfficialDocument.GetDocumentResponsibleEmployee(document);
      if (responsible != null && responsible.IsSystem != true)
        return responsible;

      if (task.Author != null && task.Author.IsSystem != true)
        return Company.Employees.As(task.Author);

      var endorsementTracking = document.Tracking
        .Where(x => x.Action == Docflow.OfficialDocumentTracking.Action.Endorsement);
      if (endorsementTracking.Any())
        return endorsementTracking.OrderByDescending(x => x.DeliveryDate).FirstOrDefault()?.DeliveredTo;

      return null;
    }

    /// <summary>
    /// Получить документы, которые возможно преобразовать в PDF.
    /// </summary>
    /// <param name="documents">Список документов.</param>
    /// <returns>Документы, которые возможно преобразовать в PDF.</returns>
    public virtual List<IOfficialDocument> FilterDocumentsToConvertToPdf(List<IOfficialDocument> documents)
    {
      var documentsToConvert = new List<IOfficialDocument>();
      foreach (var document in documents)
      {
        if (!document.HasVersions)
        {
          Logger.DebugFormat("FilterDocumentToConvertToPdf. Document with Id {0} has no version.", document.Id);
          continue;
        }

        var version = document.LastVersion;
        var validationResult = Docflow.PublicFunctions.OfficialDocument.Remote.ValidateDocumentBeforeConversion(document, version.Id);
        if (validationResult.HasErrors)
        {
          if (validationResult.HasLockError)
          {
            Logger.DebugFormat("FilterDocumentToConvertToPdf. {0}", validationResult.ErrorMessage);
            var lockInfo = Locks.GetLockInfo(version.Body);            
            throw new Sungero.Domain.Shared.Exceptions.RepeatedLockException(true, lockInfo);
          }
          else
          {
            Logger.Debug("FilterDocumentToConvertToPdf. {0}", validationResult.ErrorMessage);
            continue;
          }
        }
        else if (validationResult.IsExchangeDocument)
        {
          Logger.DebugFormat("FilterDocumentToConvertToPdf. Document with Id {0} is exchange document. Skipped converting to PDF.", document.Id);
          continue;
        }

        documentsToConvert.Add(document);
      }

      return documentsToConvert;
    }

    /// <summary>
    /// Проверить, является ли сотрудник согласующим (указан исполнителем хотя бы в одном с блоке "Согласование" задачи).
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>True, если сотрудник является согласующим, иначе - false.</returns>
    [Remote(IsPure = true)]
    public virtual bool EmployeeIsApprover(ITask task, IEmployee employee)
    {
      var approvalBlocks = Blocks.ApprovalBlocks.GetAll(task.Scheme);
      foreach (var block in approvalBlocks)
      {
        var approvers = Company.PublicFunctions.Module.GetEmployeesFromRecipients(block.Performers.ToList());
        if (approvers.Contains(employee))
          return true;
      }

      return false;
    }

    /// <summary>
    /// Необходимо ли требовать усиленную подпись.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>True - необходимо требовать, False - нет.</returns>
    [Remote(IsPure = true)]
    public virtual bool NeedStrongSignature(ITask task)
    {
      var approvalBlocks = Blocks.ApprovalBlocks.GetAll(task.Scheme);
      foreach (var block in approvalBlocks)
      {
        if (block.NeedStrongSignature == true)
          return true;
      }

      return false;
    }

    /// <summary>
    /// Обновить выдачу документов эл. обмена после отправки в сервис обмена.
    /// </summary>
    /// <param name="infos">Сведения о документах эл. обмена.</param>
    /// <param name="returnDeadline">Срок возврата.</param>
    /// <param name="returnTask">Задача на контроль возврата.</param>
    /// <param name="deliveredTo">Кому передан.</param>
    public virtual void UpdateExchangeDocumentsTrackingAfterSending(List<Exchange.IExchangeDocumentInfo> infos,
                                                                    DateTime returnDeadline, ITask returnTask, long? deliveredTo)
    {
      foreach (var info in infos)
      {
        var document = info.Document;
        if (info.Document == null)
          continue;

        var unreturnedTracking = Docflow.PublicFunctions.OfficialDocument.GetUnreturnedFromCounterpartyTracking(document, returnTask)
          .FirstOrDefault(x => x.ExternalLinkId == info.Id);
        if (unreturnedTracking == null)
          continue;

        if (deliveredTo.HasValue)
          unreturnedTracking.DeliveredTo = Sungero.Company.Employees.Get(deliveredTo.Value);
        unreturnedTracking.ReturnDeadline = returnDeadline;
        unreturnedTracking.ReturnTask = returnTask;
        Docflow.PublicFunctions.OfficialDocument.WriteTrackingLog("CheckReturnFromCounterparty. Update exchange documents tracking after sending.", unreturnedTracking);
      }
    }

    /// <summary>
    /// Обновить выдачу бумажного документа после отправки.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="responsible">Ответственный.</param>
    /// <param name="returnDeadline">Срок возврата.</param>
    /// <param name="returnTask">Задача на контроль возврата.</param>
    public virtual void UpdatePaperDocumentTrackingAfterSending(IOfficialDocument document, IEmployee responsible, DateTime returnDeadline, ITask returnTask)
    {
      var latestTracking = Docflow.PublicFunctions.OfficialDocument.GetLatestDocumentTracking(document);
      if (latestTracking != null && latestTracking.DeliveryDate.Value > returnDeadline)
        returnDeadline = latestTracking.DeliveryDate.Value;

      Docflow.PublicFunctions.OfficialDocument.AddOrUpdateEndorsementInfoInTracking(document, responsible.Id, returnDeadline, returnTask);
    }

    /// <summary>
    /// Проверить, отправлялся ли документ в сервисы обмена.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True - если документ отправлялся в сервисы обмена, иначе - false.</returns>
    public virtual bool IsExchangeDocument(IOfficialDocument document)
    {
      var exchangeDocumentInfo = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(document);
      return exchangeDocumentInfo != null;
    }

    /// <summary>
    /// Получить адресатов для выходного свойства блока.
    /// </summary>
    /// <param name="blockAddressees">Адресаты блока.</param>
    /// <param name="assignmentAddressees">Адресаты задания.</param>
    /// <returns>Адресаты для выходного свойства блока.</returns>
    public virtual System.Collections.Generic.IEnumerable<IEmployee> GetBlockOutAddressees(System.Collections.Generic.IEnumerable<IEmployee> blockAddressees,
                                                                                           System.Collections.Generic.IEnumerable<IEmployee> assignmentAddressees)
    {
      blockAddressees = blockAddressees?.Distinct();
      var blockAddresseesCount = blockAddressees?.Count() ?? 0;
      assignmentAddressees = assignmentAddressees?.Distinct();
      var assignmentAddresseesCount = assignmentAddressees?.Count() ?? 0;
      if (blockAddresseesCount == assignmentAddresseesCount &&
          blockAddressees != null && assignmentAddressees != null &&
          !blockAddressees.Except(assignmentAddressees).Any())
        return blockAddressees;

      return assignmentAddressees;
    }

    /// <summary>
    /// Получить последнее выполненное задание на доработку.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Задание на доработку.</returns>
    public virtual IEntityReworkAssignment GetLastCompletedReworkAssignment(Workflow.ITask task)
    {
      return EntityReworkAssignments
        .GetAll(a => Equals(a.Task, task) &&
                a.Result == DocflowApproval.EntityReworkAssignment.Result.ForReapproval &&
                a.TaskStartId == task.StartId)
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
    }

    #region Функции вычисляемых выражений

    /// <summary>
    /// Увеличить целочисленный параметр процесса на заданное число.
    /// </summary>
    /// <param name="parameter">Целочисленный параметр процесса.</param>
    /// <param name="number">Прибавляемое число.</param>
    /// <returns>Увеличенный целочисленный параметр процесса.</returns>
    [Public, ExpressionElement("IncreaseParameter", "IncreaseParameterDescription", "", "NumberToAdd")]
    public static int IncreaseIntegerParameter(int parameter, int number)
    {
      return parameter + number;
    }
    
    /// <summary>
    /// Получить всех участников задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Список участников.</returns>
    /// <remarks>Участниками являются исполнители всех выполненных или текущих заданий в задаче,
    /// в т.ч. исполнители подзадач и задач, для которых текущая задача является подзадачей.</remarks>
    [Public, ExpressionElement("TaskPerformers", "TaskPerformersDescription")]
    public static List<IEmployee> GetPerformers(Sungero.Workflow.ITask task)
    {
      var performers = new List<IEmployee>();
      var assignments = Assignments.GetAll()
        .Where(x => Equals(x.MainTask, task.MainTask) &&
               (x.Task.Status == Workflow.Task.Status.InProcess ||
                x.Task.Status == Workflow.Task.Status.Completed));
      foreach (var assignment in assignments)
        performers.Add(Employees.As(assignment.Performer));

      performers.Add(Employees.As(task.Author));

      return performers.Distinct().ToList();
    }

    /// <summary>
    /// ПОлучить количество сущностей.
    /// </summary>
    /// <param name="entities">Сущности.</param>
    /// <returns>Количество сущностей.</returns>
    [Public, ExpressionElement("GetCountExpressionElementName", "GetCountExpressionElementDescription")]
    public static int GetCount(System.Collections.Generic.IEnumerable<IEntity> entities)
    {
      return entities?.Count() ?? 0;
    }

    #endregion Функции вычисляемых выражений

    /// <summary>
    /// Отправить запрос на подготовку предпросмотра для документов из вложений задания.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <remarks>Предпросмотр для электронных документов.</remarks>
    public virtual void PrepareAllAttachmentsPreviews(IAssignment assignment)
    {
      var documents = assignment.AllAttachments
        .Where(x => Content.ElectronicDocuments.Is(x))
        .Select(x => Content.ElectronicDocuments.As(x))
        .Where(x => x.HasVersions)
        .ToList();

      foreach (var document in documents)
        Sungero.Core.PreviewService.PreparePreview(document.LastVersion);
    }

    /// <summary>
    /// Добавить новых согласующих.
    /// </summary>
    /// <param name="block">Блок согласования.</param>
    /// <param name="previousReworkAsg">Последнее выполненное задание на доработку.</param>
    public virtual void AddNewPerformers(DocflowApproval.Server.DocflowApprovalBlocks.ApprovalBlock block, IEntityReworkAssignment previousReworkAsg)
    {
      var performers = Company.PublicFunctions.Module.GetEmployeesFromRecipients(block.Performers.ToList()).Distinct().ToList();
      var addedPerformers = Functions.Module.GetAddedApprovers(performers, block.Id, previousReworkAsg);
      if (addedPerformers != null)
      {
        foreach (var performer in addedPerformers)
          block.Performers.Add(performer);
        Logger.WithLogger(Constants.EntityApprovalAssignment.EntityApprovalAssignmentLoggerPostfix)
          .Debug(string.Format("Task id: {0}, block id: {1}. Added performers: [{2}]",
                               previousReworkAsg.Task.Id, block.Id, string.Join(", ", addedPerformers.Select(p => p.Id))));
      }
    }
    
    /// <summary>
    /// Удалить исполнителей, которые уже согласовали документ в рамках задачи.
    /// </summary>
    /// <param name="block">Блок согласования.</param>
    /// <param name="task">Задача.</param>
    /// <param name="previousReworkAsg">Последнее выполненное задание на доработку.</param>
    public virtual void RemovePerformersWhoAlreadyApproved(DocflowApproval.Server.DocflowApprovalBlocks.ApprovalBlock block, ITask task,
                                                           IEntityReworkAssignment previousReworkAsg)
    {
      var performers = Company.PublicFunctions.Module.GetEmployeesFromRecipients(block.Performers.ToList()).Distinct().ToList();
      
      // Удалить исполнителей прошлого круга согласования, которые более не актуальны.
      var performersToRemove = performers.Where(p => !Functions.Module.NeedSendForApproval(task, p, block.Id, previousReworkAsg)).ToList();
      
      // Если не разрешена повторная отправка - удалить тех, кто фактически согласовал документ.
      if (block.AllowReapproval != true)
      {
        // Удалить тех, кто согласовал в текущем круге согласования, но в другом блоке.
        performersToRemove.AddRange(performers.Where(p => Functions.Module.PerformerHasAlreadyApproved(task, p, previousReworkAsg)));
        
        // Удалить тех, чья согласующая подпись уже стоит на документе.
        var document = (Sungero.Content.IElectronicDocument)Docflow.PublicFunctions.Module.GetServerEntityFunctionResult(task, "GetApprovalDocument", null);
        foreach (var employee in performers)
        {
          if (Docflow.PublicFunctions.Module.LastVersionHasSignature(document, employee))
            performersToRemove.Add(employee);
        }
        
        // Если исполнителю явно назначено отправить задание, то не исключать его, даже если он уже согласовал.
        var forcedPerformers = performersToRemove.Where(x => Functions.Module.NeedForcedSendForApproval(x, block.Id, previousReworkAsg)).ToList();
        foreach (var performer in forcedPerformers)
          performersToRemove.Remove(performer);
      }

      // Удалить исключенных исполнителей.
      if (performersToRemove.Any())
      {
        var performersToKeep = performers.Except(performersToRemove);
        block.Performers.Clear();
        foreach (var performer in performersToKeep)
          block.Performers.Add(performer);
        Logger.WithLogger(Constants.EntityApprovalAssignment.EntityApprovalAssignmentLoggerPostfix)
          .Debug(string.Format("Task id: {0}, block id: {1}. Removed performers: [{2}]",
                               task.Id, block.Id,
                               string.Join(", ", performersToRemove.Distinct().Select(p => p.Id))));
      }
    }
    
    /// <summary>
    /// Удалить исполнителей, которые уже подписали документ в рамках задачи.
    /// </summary>
    /// <param name="block">Блок подписания.</param>
    /// <param name="task">Задача.</param>
    public virtual void RemovePerformersWhoAlreadySigned(DocflowApproval.Server.DocflowApprovalBlocks.SigningBlock block, ITask task)
    {
      if (block.AllowReSign == true)
        return;
      
      var employeePerformers = Company.PublicFunctions.Module.GetEmployeesFromRecipients(block.Performers.ToList()).Distinct();
      var performersToRemove = employeePerformers.Where(p => Functions.Module.PerformerHasAlreadySigned(task, p)).ToList();
      
      if (performersToRemove.Any())
      {
        var performersToKeep = employeePerformers.Except(performersToRemove);
        block.Performers.Clear();
        foreach (var performer in performersToKeep)
          block.Performers.Add(performer);
      }
    }
    
    /// <summary>
    /// Выдать права на документы на чтение.
    /// </summary>
    /// <param name="documents">Список документов.</param>
    /// <param name="recipient">Субъект прав.</param>
    [Remote]
    public virtual void GrantReadAccessRightsToDocuments(List<IElectronicDocument> documents, IRecipient recipient)
    {
      foreach (var document in documents)
        Docflow.PublicFunctions.Module.GrantAccessRightsOnDocument(document, recipient, DefaultAccessRightsTypes.Read);
    }

    #region Функции для сервиса интеграции

    /// <summary>
    /// Создать задачу на согласование по регламенту.
    /// </summary>
    /// <param name="documentId">ИД согласуемого документа.</param>
    /// <param name="text">Текст задачи.</param>
    /// <param name="addApproverIds">Список ИД дополнительных согласующих.</param>
    /// <returns>ИД созданной задачи.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual long CreateDocumentFlowTask(long documentId, string text, List<long> addApproverIds)
    {
      var document = OfficialDocuments.GetAll(d => d.Id == documentId).FirstOrDefault();
      if (document == null)
        throw AppliedCodeException.Create(string.Format("Create document flow task. Document with ID ({0}) not found.", documentId));
      
      var addApprovers = new List<IEmployee>();
      foreach (var addApproverId in addApproverIds)
      {
        var addApprover = Employees.GetAll(e => e.Id == addApproverId).FirstOrDefault();
        if (addApprover != null)
        {
          if (!addApprovers.Contains(addApprover))
            addApprovers.Add(addApprover);
        }
        else
          throw AppliedCodeException.Create(string.Format("Create document flow task. Employee with ID ({0}) not found.", addApproverId));
      }
      
      var task = DocumentFlowTasks.Create();
      task.DocumentGroup.ElectronicDocuments.Add(ElectronicDocuments.As(document));

      foreach (var addApprover in addApprovers)
      {
        var newApproverRow = task.AddApprovers.AddNew();
        newApproverRow.Approver = addApprover;
      }
      
      task.ActiveText = text;
      task.Save();
      
      return task.Id;
    }
    
    #endregion
  }
}