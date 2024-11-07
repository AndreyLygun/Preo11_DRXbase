using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.RecordManagement.AcquaintanceTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server
{
  partial class AcquaintanceTaskRouteHandlers
  {
    
    #region 6. Уведомление инициатору о завершении ознакомления

    public virtual void StartBlock6(Sungero.RecordManagement.Server.AcquaintanceCompleteNotificationArguments e)
    {
      e.Block.Performers.Add(_obj.Author);
      e.Block.Subject = AcquaintanceTasks.Resources.AcquaintanceCompletedSubjectFormat(_obj.DocumentGroup.OfficialDocuments.First().Name);
    }

    #endregion

    #region 5. Нужно задание на завершение ознакомления?
    
    public virtual bool Decision5Result()
    {
      return _obj.ReceiveOnCompletion == ReceiveOnCompletion.Assignment;
    }
    
    #endregion
    
    #region 3. Ознакомление
    
    public virtual void StartBlock3(Sungero.RecordManagement.Server.AcquaintanceAssignmentArguments e)
    {
      if (_obj.Deadline.HasValue)
        e.Block.AbsoluteDeadline = _obj.Deadline.Value;
      
      e.Block.IsParallel = true;
      e.Block.Subject = AcquaintanceTasks.Resources.AcquaintanceAssignmentSubjectFormat(_obj.DocumentGroup.OfficialDocuments.First().DisplayValue);
      var participants = Functions.AcquaintanceTask.GetParticipants(_obj);
      foreach (var participant in participants)
        e.Block.Performers.Add(participant);
      
      // Запомнить участников ознакомления.
      Functions.AcquaintanceTask.StoreAcquainters(_obj);
      
      // Выдать права на просмотр исполнителям и наблюдателям.
      var documents = _obj.DocumentGroup.OfficialDocuments
        .Concat(_obj.AddendaGroup.OfficialDocuments)
        .Concat(_obj.OtherGroup.All)
        .AsQueryable<IEntity>();
      var recipients = _obj.Observers.Select(x => x.Observer).ToList();
      recipients.AddRange(_obj.Performers.Select(p => p.Performer));
      Docflow.PublicFunctions.Module.GrantOptimalReadAccessRights(documents, recipients.AsQueryable<IRecipient>());
      
      // Отправить запрос на подготовку предпросмотра для документов.
      Docflow.PublicFunctions.Module.PrepareAllAttachmentsPreviews(_obj);
      
      // Запомнить номер версии и хеш для отчета.
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      if (document != null)
      {
        _obj.AcquaintanceVersions.Clear();
        Functions.AcquaintanceTask.StoreAcquaintanceVersion(_obj, document, true);
        var addenda = _obj.AddendaGroup.OfficialDocuments;
        foreach (var addendum in addenda)
          Functions.AcquaintanceTask.StoreAcquaintanceVersion(_obj, addendum, false);
      }
    }
    
    public virtual void StartAssignment3(Sungero.RecordManagement.IAcquaintanceAssignment assignment, Sungero.RecordManagement.Server.AcquaintanceAssignmentArguments e)
    {
      // Для ознакомления под подпись указать пояснение.
      if (_obj.IsElectronicAcquaintance == false)
        assignment.Description = AcquaintanceTasks.Resources.FromSignAssignmentDesription;
    }

    public virtual void CompleteAssignment3(Sungero.RecordManagement.IAcquaintanceAssignment assignment, Sungero.RecordManagement.Server.AcquaintanceAssignmentArguments e)
    {
      // Запомнить номер версии и хеш для отчета.
      var mainDocumentTaskVersionNumber = _obj.AcquaintanceVersions
        .Where(a => a.IsMainDocument == true)
        .Select(a => a.Number)
        .FirstOrDefault();
      
      var mainDocument = _obj.DocumentGroup.OfficialDocuments.First();
      Functions.AcquaintanceAssignment.StoreAcquaintanceVersion(assignment, mainDocument, true, mainDocumentTaskVersionNumber);
      
      var addenda = _obj.AddendaGroup.OfficialDocuments;
      foreach (var addendum in addenda)
        Functions.AcquaintanceAssignment.StoreAcquaintanceVersion(assignment, addendum, false, null);
    }
    
    #endregion
    
    #region 4. Завершение работ по ознакомлению
    
    public virtual void StartBlock4(Sungero.RecordManagement.Server.AcquaintanceFinishAssignmentArguments e)
    {
      e.Block.RelativeDeadlineDays = 2;
      e.Block.Performers.Add(_obj.Author);
      e.Block.Subject = AcquaintanceTasks.Resources.AcquaintanceFinishAssignmentSubjectFormat(_obj.DocumentGroup.OfficialDocuments.First().DisplayValue);
    }
    
    public virtual void StartAssignment4(Sungero.RecordManagement.IAcquaintanceFinishAssignment assignment, Sungero.RecordManagement.Server.AcquaintanceFinishAssignmentArguments e)
    {
      // Для ознакомления под подпись указать пояснение.
      if (_obj.IsElectronicAcquaintance == false)
        assignment.Description = AcquaintanceTasks.Resources.SelfSignAcquaintanceDecription;
      else
        assignment.Description = AcquaintanceTasks.Resources.ElectronicAcquaintanceDecription;
    }
    
    #endregion
  }
}