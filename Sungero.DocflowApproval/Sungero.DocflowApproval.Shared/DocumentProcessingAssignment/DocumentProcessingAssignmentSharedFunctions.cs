using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.DocflowApproval.DocumentProcessingAssignment;
using CommonsPublicFuncs = Sungero.Commons.PublicFunctions;

namespace Sungero.DocflowApproval.Shared
{
  partial class DocumentProcessingAssignmentFunctions
  {
    #region Получение закешированных параметров видимости и доступности с EntityParams
    
    /// <summary>
    /// Закешировать свойства блока и других признаков в параметры сущности, если их нет в кеше.
    /// </summary>
    public virtual void FillEntityParamsIfEmpty()
    {
      var anyParameterIsMissing =
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.Module.NeedShowNoRightsHintParamName) ||
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.DocumentProcessingAssignment.AllowChangeReworkPerformerParamName) ||
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.DocumentProcessingAssignment.AllowSendForReworkParamName);
      
      if (anyParameterIsMissing)
        Functions.DocumentProcessingAssignment.Remote.FillEntityParams(_obj);
    }
    
    /// <summary>
    /// Пользователь без прав на основной документ.
    /// </summary>
    /// <returns>True - на документ не хватает прав, False - права есть, или нет документа.</returns>
    public virtual bool IsUserWithoutRightsOnMainDocument()
    {
      return CommonsPublicFuncs.Module
        .GetBooleanEntityParamsValue(_obj, Constants.Module.NeedShowNoRightsHintParamName);
    }    
    
    /// <summary>
    /// Разрешен ли выбор ответственного за доработку.
    /// </summary>
    /// <returns>True - разрешен, False - нет.</returns>
    public virtual bool CanChangeReworkPerformer()
    {
      return CommonsPublicFuncs.Module
        .GetBooleanEntityParamsValue(_obj, Constants.DocumentProcessingAssignment.AllowChangeReworkPerformerParamName);
    }
    
    /// <summary>
    /// Разрешена ли отправка на доработку.
    /// </summary>
    /// <returns>True - разрешена, False - нет.</returns>
    public virtual bool CanSendForRework()
    {
      return CommonsPublicFuncs.Module
        .GetBooleanEntityParamsValue(_obj, Constants.DocumentProcessingAssignment.AllowSendForReworkParamName);
    }

    #endregion

    /// <summary>
    /// Проверить, не заблокированы ли документы текущим пользователем.
    /// </summary>
    /// <returns>True - хотя бы один заблокирован, False - все свободны.</returns>
    public virtual bool AreDocumentsLockedByMe()
    {
      var documents = new List<IElectronicDocument>();
      documents.Add(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      documents.AddRange(_obj.AddendaGroup.ElectronicDocuments);
      
      return Functions.Module.IsAnyDocumentLockedByCurrentEmployee(documents);
    }

  }
}