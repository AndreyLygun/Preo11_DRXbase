using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DeadlineExtensionTask;

namespace Sungero.RecordManagement.Shared
{
  partial class DeadlineExtensionTaskFunctions
  {

    /// <summary>
    /// Получить тему задачи на продление срока.
    /// </summary>
    /// <param name="beginningSubject">Начальная тема задачи.</param>
    /// <returns>Сформированная тема задачи.</returns>
    [Public]
    public virtual string GetDeadlineExtensionSubject(CommonLibrary.LocalizedString beginningSubject)
    {
      // Добавить ">> " т.к. подзадача.
      using (TenantInfo.Culture.SwitchTo())
      {
        var subject = string.Format(">> {0}", beginningSubject);
        
        if (!string.IsNullOrWhiteSpace(_obj.ActionItem))
        {
          var resolution = Functions.ActionItemExecutionTask.FormatActionItemForSubject(_obj.ActionItem, _obj.DocumentsGroup.OfficialDocuments.Any());
          subject += string.Format(" {0}", resolution);
        }
        
        // Добавить имя документа, если поручение с документом.
        var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
        if (document != null)
          subject += ActionItemExecutionTasks.Resources.SubjectWithDocumentFormat(document.Name);
        
        return Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
      }
    }
  }
}