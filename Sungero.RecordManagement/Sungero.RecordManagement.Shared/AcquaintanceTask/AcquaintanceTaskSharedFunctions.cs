using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceTask;

namespace Sungero.RecordManagement.Shared
{
  partial class AcquaintanceTaskFunctions
  {

    /// <summary>
    /// Валидация старта задачи на ознакомление.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateAcquaintanceTaskStart(Sungero.Core.IValidationArgs e)
    {
      var errorMessages = Sungero.RecordManagement.Functions.AcquaintanceTask.Remote.GetStartValidationMessage(_obj);
      if (errorMessages.Any())
      {
        foreach (var error in errorMessages)
        {
          if (error.IsShowNotAutomatedEmployeesMessage)
            e.AddError(error.Message, _obj.Info.Actions.ShowNotAutomatedEmployees);
          else if (error.IsCantSendTaskByNonEmployeeMessage)
            e.AddError(_obj.Info.Properties.Author, error.Message);
          else
            e.AddError(error.Message);
        }
        return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Сохранить номер версии и хеш документа в задаче.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="isMainDocument">Признак главного документа.</param>
    public void StoreAcquaintanceVersion(IElectronicDocument document, bool isMainDocument)
    {
      var lastVersion = document.LastVersion;
      var mainDocumentVersion = _obj.AcquaintanceVersions.AddNew();
      mainDocumentVersion.IsMainDocument = isMainDocument;
      mainDocumentVersion.DocumentId = document.Id;
      if (lastVersion != null)
      {
        mainDocumentVersion.Number = lastVersion.Number;
        mainDocumentVersion.Hash = lastVersion.Body.Hash;
      }
      else
      {
        mainDocumentVersion.Number = 0;
        mainDocumentVersion.Hash = null;
      }
    }
    
    /// <summary>
    /// Доступность действия "Исключить из ознакомления".
    /// </summary>
    /// <returns>True - если доступно, иначе - False.</returns>
    public bool SchemeVersionSupportsExcludeFromAcquaintance()
    {
      // HACK Kovalev_MK: No-code. При старте задачи возникает ошибка с выбором версии схемы (228441).
      try
      {
        return _obj.GetStartedSchemeVersion() >= LayerSchemeVersions.V1;
      }
      catch
      {
        return true;
      }
    }

    /// <summary>
    /// Проверить наличие документа в задаче и наличие прав на него.
    /// </summary>
    /// <returns>True, если с документом можно работать.</returns>
    public virtual bool HasDocumentAndCanRead()
    {
      return _obj.DocumentGroup.OfficialDocuments.Any();
    }
    
    /// <summary>
    /// Заполнить участников из списка ознакомления.
    /// </summary>
    /// <param name="acquaintanceList">Список ознакомления.</param>
    [Public]
    public void FillFromAcquaintanceList(IAcquaintanceList acquaintanceList)
    {
      if (acquaintanceList == null)
        return;
      
      var participants = acquaintanceList.Participants.Where(p => p.Participant.Status == Company.Employee.Status.Active);
      foreach (var participant in participants)
      {
        var newParticipantRow = _obj.Performers.AddNew();
        newParticipantRow.Performer = participant.Participant;
      }
      foreach (var excludedParticipant in acquaintanceList.ExcludedParticipants)
      {
        var newExcludedPerformer = _obj.ExcludedPerformers.AddNew();
        newExcludedPerformer.ExcludedPerformer = excludedParticipant.ExcludedParticipant;
      }
    }
    
    /// <summary>
    /// Синхронизировать приложения в группы задачи.
    /// </summary>
    /// <param name="mainDocument">Основной документ.</param>
    /// <param name="addenda">Приложения.</param>
    /// <param name="otherAttachments">Прочие вложения.</param>
    [Public]
    public virtual void SyncAttachmentsToGroups(Sungero.Docflow.IOfficialDocument mainDocument,
                                                List<Sungero.Content.IElectronicDocument> addenda,
                                                List<Sungero.Domain.Shared.IEntity> otherAttachments)
    {
      if (mainDocument != null && !_obj.DocumentGroup.OfficialDocuments.Any())
        _obj.DocumentGroup.OfficialDocuments.Add(mainDocument);
      
      var nonObsoleteAddenda = addenda
        .OfType<Docflow.IOfficialDocument>()
        .Where(x => !Docflow.PublicFunctions.OfficialDocument.IsObsolete(x))
        .Except(_obj.AddendaGroup.OfficialDocuments);
      foreach (var document in nonObsoleteAddenda)
        _obj.AddendaGroup.OfficialDocuments.Add(document);
      
      foreach (var attachment in otherAttachments)
        _obj.OtherGroup.All.Add(attachment);
    }
  }
}