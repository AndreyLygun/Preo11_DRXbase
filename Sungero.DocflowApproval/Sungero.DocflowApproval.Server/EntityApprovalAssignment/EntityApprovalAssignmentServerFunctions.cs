using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.EntityApprovalAssignment;

namespace Sungero.DocflowApproval.Server
{
  partial class EntityApprovalAssignmentFunctions
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
      
      var block = this.GetApprovalBlock();
      entityBoolParams.Add(Constants.EntityApprovalAssignment.AllowApproveWithSuggestionsParamName, this.GetAllowApproveWithSuggestionsPropertyValue(block));
      entityBoolParams.Add(Constants.EntityApprovalAssignment.AllowChangePropertiesParamName, this.GetAllowChangePropertiesPropertyValue(block));
      entityBoolParams.Add(Constants.EntityApprovalAssignment.AllowChangeReworkPerformerParamName, this.GetAllowChangeReworkPerformerPropertyValue(block));
      entityBoolParams.Add(Constants.EntityApprovalAssignment.HideDocumentSummaryParamName, this.GetHideDocumentSummaryPropertyValue(block));
      entityBoolParams.Add(Constants.EntityApprovalAssignment.NeedStrongSignatureParamName, this.GetNeedStrongSignaturePropertyValue(block));
      entityBoolParams.Add(Constants.EntityApprovalAssignment.AllowAddApproversParamName, this.GetAllowAddApproversPropertyValue(block));
      
      entityBoolParams.Add(Constants.Module.IsSendingToCounterpartyEnabledInSchemeParamName, Functions.Module.IsSendingToCounterpartyEnabledInScheme(_obj.Task));
      entityBoolParams.Add(Constants.Module.HasAnyDocumentReviewInSchemeParamName, RecordManagement.PublicFunctions.Module.Remote.HasAnyTypeDocumentReviewBlockInScheme(_obj.Task));
      
      foreach (var parameter in entityBoolParams)
        Sungero.Commons.PublicFunctions.Module.AddOrUpdateEntityParams(_obj, parameter.Key, parameter.Value);
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
    /// Получить блок согласования.
    /// </summary>
    /// <returns>Блок согласования.</returns>
    public virtual IApprovalBlockSchemeBlock GetApprovalBlock()
    {
      return Blocks.ApprovalBlocks.Get(_obj.Task.Scheme, _obj.BlockUid);
    }
    
    /// <summary>
    /// Разрешить согласование с замечаниями.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <returns>True - разрешено, False - нет или block = null.</returns>
    public virtual bool GetAllowApproveWithSuggestionsPropertyValue(IApprovalBlockSchemeBlock block)
    {
      if (block == null)
        return false;
      
      return block.AllowApproveWithSuggestions.GetValueOrDefault();
    }

    /// <summary>
    /// Разрешить изменение параметров.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <returns>True - разрешено, False - нет или block = null.</returns>
    public virtual bool GetAllowChangePropertiesPropertyValue(IApprovalBlockSchemeBlock block)
    {
      if (block == null)
        return false;
      
      return block.AllowChangeProperties.GetValueOrDefault();
    }
    
    /// <summary>
    /// Разрешить выбор ответственного за доработку.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <returns>True - разрешено, False - нет или block = null.</returns>
    public virtual bool GetAllowChangeReworkPerformerPropertyValue(IApprovalBlockSchemeBlock block)
    {
      if (block == null)
        return false;
      
      return block.AllowChangeReworkPerformer.GetValueOrDefault();
    }

    /// <summary>
    /// Скрыть реквизиты документа.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <returns>True - скрывать, False - нет или block = null.</returns>
    public virtual bool GetHideDocumentSummaryPropertyValue(IApprovalBlockSchemeBlock block)
    {
      if (block == null)
        return false;
      
      return block.HideDocumentSummary.GetValueOrDefault();
    }
    
    /// <summary>
    /// Требовать усиленную подпись.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <returns>True - требовать, False - нет или block = null.</returns>
    public virtual bool GetNeedStrongSignaturePropertyValue(IApprovalBlockSchemeBlock block)
    {
      if (block == null)
        return false;
      
      return block.NeedStrongSignature.GetValueOrDefault();
    }
    
    /// <summary>
    /// Разрешить добавление согласующих.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <returns>True - разрешить, False - нет или block = null.</returns>
    public virtual bool GetAllowAddApproversPropertyValue(IApprovalBlockSchemeBlock block)
    {
      if (block == null)
        return false;
      
      return block.AllowAddApprovers.GetValueOrDefault();
    }
    
    #endregion
    
    /// <summary>
    /// Проверить, можно ли переадресовать согласование сотруднику.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>True, если можно переадресовать, False - если нельзя.</returns>
    [Remote(IsPure = true)]
    public virtual bool CanForwardTo(Company.IEmployee employee)
    {
      if (Equals(_obj.Performer, employee))
        return false;
      
      var assignments = EntityApprovalAssignments
        .GetAll(a => Equals(a.Task, _obj.Task) &&
                Equals(a.TaskStartId, _obj.TaskStartId) &&
                Equals(a.IterationId, _obj.IterationId))
        .ToList();
      
      if (assignments.Any(a => Equals(a.Performer, employee) && a.Status == Status.InProcess))
        return false;
      
      var lastEmployeeAssignment = assignments.OrderByDescending(a => a.Created).FirstOrDefault(a => Equals(a.Performer, employee));
      if (lastEmployeeAssignment == null)
      {
        // Если сотруднику еще не приходили задания, смотрим, есть ли он в списке исполнителей блока.
        // Если сотрудник в списке есть, значит задание сформируется позже, переадресовывать ему нельзя.
        var approverBlockPerformers = this.GetApprovalBlocksPerformers();
        return !approverBlockPerformers.Contains(employee);
      }
      else
      {
        // Если сотруднику ранее уже приходили задания, и они были завершены, учитываем только переадресацию сотрудника в последующих заданиях.
        var assignmentsAfterLastEmployeeAssignments = assignments.Where(a => a.Created > lastEmployeeAssignment.Created);
        foreach (var assignment in assignmentsAfterLastEmployeeAssignments)
        {
          if (assignment.ForwardedTo.Contains(employee))
            return false;
        }
      }
      
      return true;
    }

    /// <summary>
    /// Получить исполнителей активных или будущих заданий на согласование.
    /// </summary>
    /// <returns>Исполнители заданий.</returns>
    [Public, Remote(IsPure = true)]
    public virtual IQueryable<IRecipient> GetActiveAndFutureAssignmentsPerformers()
    {
      var blockPerformers = this.GetApprovalBlocksPerformers();
      return Docflow.PublicFunctions.Module.Remote.GetActiveAndFutureAssignmentsPerformers(_obj, blockPerformers);
    }
    
    /// <summary>
    /// Получить будущих, текущих и прошлых исполнителей блока согласования.
    /// </summary>
    /// <returns>Развернутые до сотрудников исполнители блока согласования.</returns>
    [Remote(IsPure = true)]
    public virtual List<Company.IEmployee> GetApprovalBlocksPerformers()
    {
      var approvalBlock = Blocks.ApprovalBlocks.Get(_obj.Task.Scheme, _obj.BlockUid);
      return Company.PublicFunctions.Module.GetEmployeesFromRecipients(approvalBlock.Performers.ToList());
    }
    
    /// <summary>
    /// Получить активные задания на согласование в рамках текущей задачи и итерации.
    /// </summary>
    /// <returns>Активные задания на согласование.</returns>
    [Remote(IsPure = true)]
    public virtual IQueryable<IEntityApprovalAssignment> GetActiveAssignments()
    {
      return EntityApprovalAssignments.GetAll(a => Equals(a.Task, _obj.Task) &&
                                              Equals(a.TaskStartId, _obj.TaskStartId) &&
                                              Equals(a.IterationId, _obj.IterationId) &&
                                              a.Status == Status.InProcess);
    }
    
    /// <summary>
    /// Построить сводку по документу.
    /// </summary>
    /// <returns>Сводка по документу.</returns>
    [Remote(IsPure = true)]
    [LocalizeFunction("GetDocumentSummaryFunctionName", "GetDocumentSummaryFunctionDescription")]
    public StateView GetDocumentSummary()
    {
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      var stateView = Docflow.PublicFunctions.Module.GetDocumentSummary(officialDocument);
      if (!stateView.Blocks.Any())
        stateView.AddDefaultLabel(Resources.NoDataToDisplay);
      
      return stateView;
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
    /// Получить список сотрудников, у которых можно запросить продление срока.
    /// </summary>
    /// <returns>Список сотрудников.</returns>
    public virtual List<IUser> GetAssigneesForDeadlineExtension()
    {
      return Functions.Module.GetDeadlineAssignees(_obj);
    }

  }
}