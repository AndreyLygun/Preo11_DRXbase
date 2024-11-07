using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.AdvancedAssignment;

namespace Sungero.DocflowApproval.Server
{
  partial class AdvancedAssignmentFunctions
  {
    #region Заполнение свойств задания из блока
    
    /// <summary>
    /// Заполнить параметры блока в параметры задания.
    /// </summary>
    [Remote]
    public virtual void FillAssignmentBlockParams()
    {
      var block = this.GetAssignmentBlock();
      this.AddOrUpdateAssignmentBlockAllowForwardParam(block);
      this.AddOrUpdateAssignmentBlockAllowSendForReworkParam(block);
      this.AddOrUpdateAssignmentBlockAllowChangeReworkPerformerParam(block);
    }
    
    /// <summary>
    /// Получить исполнителей активных или будущих расширенных заданий. 
    /// </summary>
    /// <returns>Исполнители заданий.</returns>
    [Remote(IsPure = true)]
    public virtual IQueryable<IRecipient> GetActiveAndFutureAssignmentsPerformers()
    {
      var advancedAssignmentBlock = this.GetAssignmentBlock();
      var blockPerformers = Company.PublicFunctions.Module.GetEmployeesFromRecipients(advancedAssignmentBlock.Performers.ToList());
      return Docflow.PublicFunctions.Module.Remote.GetActiveAndFutureAssignmentsPerformers(_obj, blockPerformers);
    }
    
    /// <summary>
    /// Получить блок задания.
    /// </summary>
    /// <returns>Блок задания.</returns>
    public virtual IAdvancedAssignmentBlockSchemeBlock GetAssignmentBlock()
    {
      return Blocks.AdvancedAssignmentBlocks.Get(_obj.Task.Scheme, _obj.BlockUid);
    }
    
    /// <summary>
    /// Добавить или обновить значение параметра, указывающего на то, что можно переадресовывать задание.
    /// </summary>
    /// <param name="block">Блок задания.</param>
    public virtual void AddOrUpdateAssignmentBlockAllowForwardParam(IAdvancedAssignmentBlockSchemeBlock block)
    {
      Sungero.Commons.PublicFunctions.Module.AddOrUpdateEntityParams(_obj,
                                                                     Constants.AdvancedAssignment.AllowForwardParamName,
                                                                     this.GetAssignmentBlockAllowForwardProperty(block));
    }
    
    /// <summary>
    /// Определить, можно ли переадресовывать задание, исходя из значения соответствующего свойства блока.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <returns>True - можно, False - иначе.</returns>
    public virtual bool GetAssignmentBlockAllowForwardProperty(IAdvancedAssignmentBlockSchemeBlock block)
    {
      if (block == null)
        return false;
      return block.AllowForward.GetValueOrDefault();
    }
    
    /// <summary>
    /// Добавить или обновить значение параметра, указывающего на то, что задание можно отправлять на доработку.
    /// </summary>
    /// <param name="block">Блок задания.</param>
    public virtual void AddOrUpdateAssignmentBlockAllowSendForReworkParam(IAdvancedAssignmentBlockSchemeBlock block)
    {
      Sungero.Commons.PublicFunctions.Module.AddOrUpdateEntityParams(_obj,
                                                                     Constants.AdvancedAssignment.AllowSendForReworkParamName,
                                                                     this.GetAssignmentBlockAllowSendForReworkProperty(block));
    }
    
    /// <summary>
    /// Определить, можно ли отправлять задание на доработку, исходя из значения соответствующего свойства блока.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <returns>True - можно, False - иначе.</returns>
    public virtual bool GetAssignmentBlockAllowSendForReworkProperty(IAdvancedAssignmentBlockSchemeBlock block)
    {
      if (block == null)
        return false;
      return block.AllowSendForRework.GetValueOrDefault();
    }
    
    /// <summary>
    /// Добавить или обновить значение параметра, указывающего на то, что можно выбирать ответственного за доработку задания.
    /// </summary>
    /// <param name="block">Блок задания.</param>
    public virtual void AddOrUpdateAssignmentBlockAllowChangeReworkPerformerParam(IAdvancedAssignmentBlockSchemeBlock block)
    {
      Sungero.Commons.PublicFunctions.Module.AddOrUpdateEntityParams(_obj,
                                                                     Constants.AdvancedAssignment.AllowChangeReworkPerformerParamName,
                                                                     this.GetAssignmentBlockAllowChangeReworkPerformerProperty(block));
    }
    
    /// <summary>
    /// Определить, можно ли выбирать ответственного за доработку задания, исходя из значения соответствующего свойства блока.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <returns>True - можно, False - иначе.</returns>
    public virtual bool GetAssignmentBlockAllowChangeReworkPerformerProperty(IAdvancedAssignmentBlockSchemeBlock block)
    {
      if (block == null)
        return false;
      return block.AllowChangeReworkPerformer.GetValueOrDefault();
    }
    
    #endregion
    
    /// <summary>
    /// Получить активные расширенные задания в рамках текущей задачи и итерации.
    /// </summary>
    /// <returns>Активные задания на согласование.</returns>
    public virtual IQueryable<IAdvancedAssignment> GetActiveAssignments()
    {
      return AdvancedAssignments.GetAll()
        .Where(a => Equals(a.Task, _obj.Task) &&
                    Equals(a.TaskStartId, _obj.TaskStartId) &&
                    Equals(a.IterationId, _obj.IterationId) &&
                    a.Status == Status.InProcess);
    }
    
    /// <summary>
    /// Проверить основной документ на нехватку прав.
    /// </summary>
    /// <returns>True - на документ не хватает прав. False - права есть, или их выдавать не нужно.</returns>
    [Remote(IsPure = true)]
    public virtual bool NeedRightsToOfficialDocument()
    {
      var document = OfficialDocuments.Null;
      AccessRights.AllowRead(
        () =>
        {
          document = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
        });
      
      return document != null && !document.AccessRights.CanRead();
    }
    
    /// <summary>
    /// Получить список сотрудников, у которых можно запросить продление срока.
    /// </summary>
    /// <returns>Список сотрудников.</returns>
    public virtual List<IUser> GetAssigneesForDeadlineExtension()
    {
      return Functions.Module.GetDeadlineAssigneesWithoutPerformer(_obj);
    }
    
    /// <summary>
    /// Связать с основным документом документы из группы Приложения, если они не были связаны ранее.
    /// </summary>
    public virtual void RelateAddedAddendaToPrimaryDocument()
    {
      var primaryDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (primaryDocument == null)
        return;
      
      var addenda = Functions.Module.GetNonObsoleteDocumentsFromAttachments(primaryDocument, _obj.AddendaGroup.All);
      Functions.Module.RelateDocumentsToPrimaryDocumentAsAddenda(primaryDocument, addenda);
    }

  }
}