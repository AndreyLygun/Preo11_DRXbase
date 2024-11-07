using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.EntityReworkAssignment;

namespace Sungero.DocflowApproval.Server
{
  partial class EntityReworkAssignmentFunctions
  {
    #region Кеширование параметров видимости и доступности в EntityParams
    
    /// <summary>
    /// Закешировать свойства блока и другие признаки в параметры сущности.
    /// </summary>
    [Remote]
    public virtual void FillEntityParams()
    {
      var entityBoolParams = new Dictionary<string, bool>();
      
      // При первом обращении к вложениям они кэшируются с учетом прав на сущности,
      // последующие обращения, в том числе через AllowRead, работают с закешированными сущностями и правами.
      // Если первое обращение было через AllowRead, то последующий код будет работать так, будто есть права, и наоборот,
      // если кэширование было без прав на сущности, то в AllowRead вложений не получить.
      // Корректность доступных действий важнее функциональности ниже, поэтому обеспечиваем работу NeedRightsToMainDocument
      // с серверными вложениями, а не из кэша.
      // BUGS 319348, 320495.
      entityBoolParams.Add(Constants.Module.NeedShowNoRightsHintParamName, this.NeedRightsToMainDocument());
      
      var block = this.GetReworkBlock();
      entityBoolParams.Add(Constants.EntityReworkAssignment.SpecifyDeliveryMethodParamName, this.GetSpecifyDeliveryMethodPropertyValue(block));
      
      entityBoolParams.Add(Constants.Module.IsSendingToCounterpartyEnabledInSchemeParamName, Functions.Module.IsSendingToCounterpartyEnabledInScheme(_obj.Task));
      entityBoolParams.Add(Constants.Module.HasAnyDocumentReviewInSchemeParamName, RecordManagement.PublicFunctions.Module.Remote.HasAnyTypeDocumentReviewBlockInScheme(_obj.Task));
      
      foreach (var parameter in entityBoolParams)
        Sungero.Commons.PublicFunctions.Module.AddOrUpdateEntityParams(_obj, parameter.Key, parameter.Value);
      
      Sungero.Commons.PublicFunctions.Module.AddOrUpdateEntityParams(_obj,
                                                                     Constants.EntityReworkAssignment.CanChangeApprovers,
                                                                     this.CanChangeApprovers());
      
      Sungero.Commons.PublicFunctions.Module.AddOrUpdateEntityParams(_obj,
                                                                     Constants.EntityReworkAssignment.CanChangeApprovalDeadline,
                                                                     this.CanChangeApprovalDeadline());
    }
    
    /// <summary>
    /// Проверить основной документ на нехватку прав.
    /// </summary>
    /// <returns>True - на документ не хватает прав. False - права есть, или их выдавать не нужно.</returns>
    [Remote(IsPure = true)]
    public virtual bool NeedRightsToMainDocument()
    {
      var document = Sungero.Content.ElectronicDocuments.Null;
      AccessRights.AllowRead(
        () =>
        {
          document = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
        });
      
      return document != null && !document.AccessRights.CanRead();
    }
    
    /// <summary>
    /// Получить исполнителей активных или будущих заданий на доработку. 
    /// </summary>
    /// <returns>Исполнители заданий.</returns>
    [Remote(IsPure = true)]
    public virtual IQueryable<IRecipient> GetActiveAndFutureAssignmentsPerformers()
    {
      var reworkBlock = this.GetReworkBlock();
      var blockPerformers = Company.PublicFunctions.Module.GetEmployeesFromRecipients(reworkBlock.Performers.ToList());
      return Docflow.PublicFunctions.Module.Remote.GetActiveAndFutureAssignmentsPerformers(_obj, blockPerformers);
    }

    /// <summary>
    /// Получить блок доработки.
    /// </summary>
    /// <returns>Блок доработки.</returns>
    public virtual IReworkBlockSchemeBlock GetReworkBlock()
    {
      return Blocks.ReworkBlocks.Get(_obj.Task.Scheme, _obj.BlockUid);
    }
    
    /// <summary>
    /// Указать способ доставки.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <returns>True - разрешено указывать, False - нет или block = null.</returns>
    public virtual bool GetSpecifyDeliveryMethodPropertyValue(IReworkBlockSchemeBlock block)
    {
      if (block == null)
        return false;
      
      return block.SpecifyDeliveryMethod.GetValueOrDefault();
    }

    /// <summary>
    /// Получить возможность изменения состава согласующих.
    /// </summary>
    /// <returns>True - можно изменять, иначе - false.</returns>
    [Remote(IsPure = true)]
    public virtual bool CanChangeApprovers()
    {
      return this.GetReworkBlock().AllowChangeApprovers == true;
    }

    /// <summary>
    /// Получить возможность изменения нового срока согласования.
    /// </summary>
    /// <returns>True - можно изменять, иначе - false.</returns>
    [Remote(IsPure = true)]
    public virtual bool CanChangeApprovalDeadline()
    {
      return this.GetReworkBlock().AllowChangeApprovalDeadline == true;
    }

    #endregion

    /// <summary>
    /// Возможность переадресации сотруднику задания на доработку.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>True - если можно переадресовать, иначе - false.</returns>
    [Remote(IsPure = true), Obsolete("Метод не используется с 19.06.2024 и версии 4.11. Теперь сотрудники, которым можно переадресовать задание, фильтруются в диалоге переадресации.")]
    public virtual bool CanForwardTo(Company.IEmployee employee)
    {
      if (Equals(_obj.Performer, employee))
        return false;
      var reworkAssignments = EntityReworkAssignments.GetAll(era => Equals(era.Task, _obj.Task) &&
                                                             Equals(era.TaskStartId, _obj.TaskStartId) &&
                                                             Equals(era.IterationId, _obj.IterationId));
      
      if (reworkAssignments.Any(ra => Equals(ra.Performer, employee) && ra.Status == Status.InProcess))
        return false;
      
      // Проверка переадресованных заданий.
      var assignments = reworkAssignments.ToList();
      foreach (var assignment in assignments)
      {
        var forwardedAssignmentCount = assignment.ForwardedTo.Count(u => Equals(u, employee));
        var activeAssignmentCount = assignments.Count(a => Equals(a.Performer, employee) &&
                                                      Equals(a.ForwardedFrom, assignment)  &&
                                                      a.Status == Status.InProcess);
        if (forwardedAssignmentCount < activeAssignmentCount)
          return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Связать с основным документом документы из группы Приложения, если они не были связаны ранее.
    /// </summary>
    public virtual void RelateAddedAddendaToPrimaryDocument()
    {
      var primaryDocument = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      if (primaryDocument == null)
        return;
      
      var nonRelatedAddenda = Functions.Module.GetNonObsoleteDocumentsFromAttachments(primaryDocument, _obj.AddendaGroup.All);
      Functions.Module.RelateDocumentsToPrimaryDocumentAsAddenda(primaryDocument, nonRelatedAddenda);
    }
    
    /// <summary>
    /// Заполнить коллекцию согласующих.
    /// </summary>
    public virtual void SetApprovers()
    {
      // Получить последнее задание на доработку.
      var previousReworkAssignment = Functions.Module.GetLastCompletedReworkAssignment(_obj.Task);
      
      // Заполнить грид из заданий на согласование после последней доработки (если она была).
      var completedAssignments = this.GetCompletedApprovalAssignments(previousReworkAssignment).OrderBy(x => x.Completed);
      
      foreach (var assignment in completedAssignments)
      {
        var approvalBlock = Blocks.ApprovalBlocks.Get(_obj.Task.Scheme, assignment.BlockUid);
        this.AddApprover(Sungero.Company.Employees.Get(assignment.Performer.Id), assignment.BlockUid, approvalBlock.Title,
                         EntityApprovalAssignments.Info.Properties.Result.GetLocalizedValue(assignment.Result), previousReworkAssignment, null);
      }
      
      // Добавить в грид строки из последней доработки, которых нет на последнем круге согласования.
      if (previousReworkAssignment != null)
      {
        var notInsertedApprovers = previousReworkAssignment.Approvers.Where(a => a.BlockId != null &&
                                                                            !_obj.Approvers.Any(x => x.BlockId == a.BlockId && x.Approver.Id == a.Approver.Id)).OrderBy(x => x.Id);
        foreach (var approver in notInsertedApprovers)
          this.AddApprover(approver.Approver, approver.BlockId, approver.BlockName,
                           approver.AssignmentResult, previousReworkAssignment, approver.Action);
      }
    }
    
    /// <summary>
    /// Добавить согласующего.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <param name="blockUid">ИД блока согласования.</param>
    /// <param name="blockName">Название блока согласования.</param>
    /// <param name="assignmentResult">Результат выполнения задания на согласование.</param>
    /// <param name="previousReworkAssignment">Предыдущее задание на доработку.</param>
    /// <param name="previousAction">Действие из предыдущего задания на доработку.</param>
    public virtual void AddApprover(Sungero.Company.IEmployee employee, string blockUid, string blockName, string assignmentResult, IEntityReworkAssignment previousReworkAssignment, Enumeration? previousAction)
    {
      var approver = _obj.Approvers.AddNew();
      approver.BlockId = blockUid;
      approver.BlockName = blockName;
      approver.Approver = employee;
      approver.AssignmentResult = assignmentResult;
      var action = this.GetApproverAction(employee, assignmentResult, previousReworkAssignment, previousAction);
      approver.Action = action;
      
      var actionMessage = previousAction == null ? "Approver added from completed approval assignment: "
        : "Approver added from previous rework assignment: ";
      Logger.WithLogger(Constants.EntityReworkAssignment.EntityReworkAssignmentLoggerPostfix)
        .Debug(string.Format(actionMessage
                             + "Task id: {0}, assignment id {1}, Approver id: {2}, block id: {3}," 
                             + " assignment result: {4}, action: {5}, previous rework assignment id: {6}.",
                             _obj.Task.Id, _obj.Id, employee.Id, blockUid,
                             assignmentResult, action, previousReworkAssignment?.Id.ToString() ?? "null"));
    }
    
    /// <summary>
    /// Получить действие по умолчанию для согласующих.
    /// </summary>
    /// <param name="approver">Согласующий.</param>
    /// <param name="assignmentResult">Результат выполнения задания на согласование.</param>
    /// <param name="previousReworkAssignment">Предыдущее задание на доработку.</param>
    /// <param name="previousAction">Действие из предыдущего задания на доработку.</param>
    /// <returns>Действие по умолчанию.</returns>
    public virtual Enumeration GetApproverAction(Sungero.Company.IEmployee approver, string assignmentResult, IEntityReworkAssignment previousReworkAssignment, Enumeration? previousAction)
    {
      // Ничего не отправлять тем, кто переадресовал.
      if (assignmentResult == EntityApprovalAssignments.Info.Properties.Result.GetLocalizedValue(DocflowApproval.EntityApprovalAssignment.Result.Forward))
        return DocflowApproval.EntityReworkAssignmentApprovers.Action.DoNotSend;
      
      // При запрете изменить состав согласующих - отправить на согласование.
      var reworkBlock = this.GetReworkBlock();
      if (!(reworkBlock.AllowChangeApprovers ?? false))
        return DocflowApproval.EntityReworkAssignmentApprovers.Action.SendForApproval;
      
      // Выбрать действие по умолчанию, если это первая итерация согласования или на прошлой отправили на согласование.
      if (previousAction == null || previousAction == DocflowApproval.EntityReworkAssignmentApprovers.Action.SendForApproval)
      {
        if (assignmentResult == EntityApprovalAssignments.Info.Properties.Result.GetLocalizedValue(DocflowApproval.EntityApprovalAssignment.Result.Approved) ||
            assignmentResult == EntityApprovalAssignments.Info.Properties.Result.GetLocalizedValue(DocflowApproval.EntityApprovalAssignment.Result.WithSuggestions))
          return DocflowApproval.EntityReworkAssignmentApprovers.Action.SendNotice;
        else if (assignmentResult == EntityApprovalAssignments.Info.Properties.Result.GetLocalizedValue(DocflowApproval.EntityApprovalAssignment.Result.ForRework))
          return DocflowApproval.EntityReworkAssignmentApprovers.Action.SendForApproval;
      }
      
      // Отправить уведомление тем, кому после прошлой доработки оно не пришло.
      if (previousAction == DocflowApproval.EntityReworkAssignmentApprovers.Action.SendNotice)
      {
        var hasNotification = Workflow.Notices.GetAll(n => n.Task.Equals(_obj.Task) &&
                                                      n.TaskStartId == _obj.TaskStartId &&
                                                      n.Performer.Equals(approver) &&
                                                      n.Created > previousReworkAssignment.Created).Any();
        if (!hasNotification)
          return DocflowApproval.EntityReworkAssignmentApprovers.Action.SendNotice;
      }
      
      return DocflowApproval.EntityReworkAssignmentApprovers.Action.DoNotSend;
    }
    
    /// <summary>
    /// Получить список завершенных заданий согласования после последней доработки (если она была).
    /// </summary>
    /// <param name="previousReworkAssignment">Предыдущее задание на доработку.</param>
    /// <returns>Список завершенных заданий.</returns>
    public virtual System.Linq.IQueryable<IEntityApprovalAssignment> GetCompletedApprovalAssignments(IEntityReworkAssignment previousReworkAssignment)
    {
      var completedAssignments = EntityApprovalAssignments.GetAll(a => a.Task.Equals(_obj.Task) &&
                                                                  a.Status == Workflow.AssignmentBase.Status.Completed &&
                                                                  a.TaskStartId == _obj.TaskStartId);
      if (previousReworkAssignment != null)
        completedAssignments = completedAssignments.Where(a => a.Created > previousReworkAssignment.Created);
      return completedAssignments;
    }
  }
}