using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.RecordManagement.AcquaintanceTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server.AcquaintanceTaskBlocks
{
  partial class GrantAccessRightsToPerformersBlockHandlers
  {

    public virtual void GrantAccessRightsToPerformersBlockExecute()
    {
      var documents = _obj.DocumentGroup.OfficialDocuments
        .Concat(_obj.AddendaGroup.OfficialDocuments)
        .Concat(_obj.OtherGroup.All)
        .AsQueryable<IEntity>();
      
      var recipients = _obj.Observers.Select(x => x.Observer).ToList();
      recipients.AddRange(_obj.Performers.Select(p => p.Performer));
      
      // Выдать права на просмотр исполнителям и наблюдателям, не разворачивая группы.
      Docflow.PublicFunctions.Module.GrantOptimalReadAccessRights(documents, recipients.AsQueryable<IRecipient>());
    }
  }

  partial class FinishAcquaintanceBlockHandlers
  {

    public virtual void FinishAcquaintanceBlockStartAssignment(Sungero.RecordManagement.IAcquaintanceFinishAssignment assignment)
    {
      // Для ознакомления под подпись указать пояснение.
      if (_obj.IsElectronicAcquaintance == false)
        assignment.Description = AcquaintanceTasks.Resources.SelfSignAcquaintanceDecription;
      else
        assignment.Description = AcquaintanceTasks.Resources.ElectronicAcquaintanceDecription;
    }
  }

  partial class DocumentAcquaintanceBlockHandlers
  {

    public virtual void DocumentAcquaintanceBlockCompleteAssignment(Sungero.RecordManagement.IAcquaintanceAssignment assignment)
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

    public virtual void DocumentAcquaintanceBlockStartAssignment(Sungero.RecordManagement.IAcquaintanceAssignment assignment)
    {
      // Для ознакомления под подпись указать пояснение.
      if (_obj.IsElectronicAcquaintance == false)
        assignment.Description = AcquaintanceTasks.Resources.FromSignAssignmentDesription;
    }

    public virtual void DocumentAcquaintanceBlockStart()
    {
      // Запомнить участников ознакомления.
      Functions.AcquaintanceTask.StoreAcquainters(_obj);
      
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
  }
}