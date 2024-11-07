using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.DocflowApproval.AdvancedAssignment;

namespace Sungero.DocflowApproval.Shared
{
  partial class AdvancedAssignmentFunctions
  {
    /// <summary>
    /// Определить, можно ли переадресовывать задание.
    /// </summary>
    /// <returns>True - можно, False - иначе.</returns>
    public virtual bool CanForward()
    {
      return Sungero.Commons.PublicFunctions.Module.GetBooleanEntityParamsValue(_obj, DocflowApproval.Constants.AdvancedAssignment.AllowForwardParamName);
    }
    
    /// <summary>
    /// Определить, можно ли отправлять задание на доработку.
    /// </summary>
    /// <returns>True - можно, False - иначе.</returns>
    public virtual bool CanSendForRework()
    {
      return Sungero.Commons.PublicFunctions.Module.GetBooleanEntityParamsValue(_obj, DocflowApproval.Constants.AdvancedAssignment.AllowSendForReworkParamName);
    }
    
    /// <summary>
    /// Определить, можно ли выбирать ответственного за доработку задания.
    /// </summary>
    /// <returns>True - можно, False - иначе.</returns>
    public virtual bool CanChangeReworkPerformer()
    {
      return Sungero.Commons.PublicFunctions.Module.GetBooleanEntityParamsValue(_obj, DocflowApproval.Constants.AdvancedAssignment.AllowChangeReworkPerformerParamName);
    }
    
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