using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DocumentReviewAssignment;

namespace Sungero.RecordManagement.Server
{
  partial class DocumentReviewAssignmentFunctions
  {
    /// <summary>
    /// Получить подготовленные к старту поручения из проекта резолюции в текущем задании на рассмотрение.
    /// </summary>
    /// <returns>Список поручений из проекта резолюции в текущем задании на рассмотрение.</returns>
    /// <remarks>Проекты резолюции не стартуются сразу, так как Remote-функция 
    /// выполняется в отдельной сессии и это может привести к ошибкам 
    /// при длительном выполнении (см. Bug 282735).</remarks>
    [Remote, Public]
    public virtual List<IActionItemExecutionTask> GetDraftResolutionPreparedForStart()
    {
      var result = new List<IActionItemExecutionTask>();
      
      // Синхронизация для пробрасывания в проект резолюции последних изменений из задания на рассмотрение.
      var primaryDocument = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      var addendaDocuments = _obj.AddendaGroup.OfficialDocuments.Select(x => ElectronicDocuments.As(x)).ToList();
      var otherAttachments = _obj.OtherGroup.All.ToList();
      
      var draftResolution = _obj.ResolutionGroup.ActionItemExecutionTasks.Where(t => t.Status == RecordManagement.ActionItemExecutionTask.Status.Draft).ToList();
      Functions.Module.PrepareDraftResolutionForStart(draftResolution,
                                                      _obj,
                                                      primaryDocument,
                                                      addendaDocuments,
                                                      otherAttachments);
      result.AddRange(draftResolution);
      
      return result;
    } 
    
    /// <summary>
    /// Построить модель представления.
    /// </summary>
    /// <returns>Xml представление контрола состояние.</returns>
    [Remote(IsPure = true)]
    [LocalizeFunction("GetStateViewFunctionName", "GetStateViewFunctionDescription")]
    public Sungero.Core.StateView GetStateView()
    {
      return Functions.Module.GetStateViewForDraftResolution(_obj.ResolutionGroup.ActionItemExecutionTasks.ToList());
    }
  }
}