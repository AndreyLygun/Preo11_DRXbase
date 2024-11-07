using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.DocumentProcessingAssignment;

namespace Sungero.DocflowApproval.Server
{
  partial class DocumentProcessingAssignmentFunctions
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
      
      var block = this.GetDocumentProcessingBlock();
      
      entityBoolParams.Add(Constants.DocumentProcessingAssignment.AllowChangeReworkPerformerParamName, this.GetAllowChangeReworkPerformerPropertyValue(block));
      entityBoolParams.Add(Constants.DocumentProcessingAssignment.AllowSendForReworkParamName, this.GetAllowSendForReworkPropertyValue(block));
      
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
    /// Получить блок обработки документа.
    /// </summary>
    /// <returns>Блок обработки документа.</returns>
    public virtual IDocumentProcessingBlockSchemeBlock GetDocumentProcessingBlock()
    {
      return Blocks.DocumentProcessingBlocks.Get(_obj.Task.Scheme, _obj.BlockUid);
    }
    
    /// <summary>
    /// Разрешить выбор ответственного за доработку.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <returns>True - разрешено, False - нет или block = null.</returns>
    public virtual bool GetAllowChangeReworkPerformerPropertyValue(IDocumentProcessingBlockSchemeBlock block)
    {
      if (block == null)
        return false;
      
      return block.AllowChangeReworkPerformer.GetValueOrDefault();
    }
    
    /// <summary>
    /// Разрешить отправку на доработку.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <returns>True - разрешено, False - нет или block = null.</returns>
    public virtual bool GetAllowSendForReworkPropertyValue(IDocumentProcessingBlockSchemeBlock block)
    {
      if (block == null)
        return false;
      
      return block.AllowSendForRework.GetValueOrDefault();
    }
    
    #endregion
    
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

  }
}