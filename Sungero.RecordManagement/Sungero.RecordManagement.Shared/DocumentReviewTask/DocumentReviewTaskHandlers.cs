using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.RecordManagement.DocumentReviewTask;

namespace Sungero.RecordManagement
{
  partial class DocumentReviewTaskSharedHandlers
  {

    public virtual void AddendaGroupPopulating(Sungero.Workflow.Interfaces.AttachmentGroupPopulatingEventArgs e)
    {
      e.PopulateFrom(_obj.DocumentForReviewGroup, documentGroup => PublicFunctions.Module.GetActualAddendaForDocumentReviewTask(documentGroup, Functions.DocumentReviewTask.GetRemovedAddenda(_obj)));
    }

    public virtual void AddendaGroupCreated(Sungero.Workflow.Interfaces.AttachmentCreatedEventArgs e)
    {
      // Метод оставлен для совместимости при перекрытии.
    }

    public virtual void AddendaGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      // Метод оставлен для совместимости при перекрытии.
    }

    public virtual void AddendaGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      // Метод оставлен для совместимости при перекрытии.
    }

    public virtual void ResolutionGroupCreated(Sungero.Workflow.Interfaces.AttachmentCreatedEventArgs e)
    {
      Functions.DocumentReviewTask.FillDraftResolutionProperties(_obj,
                                                                 _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault(),
                                                                 _obj.AddendaGroup.OfficialDocuments.Select(x => Sungero.Content.ElectronicDocuments.As(x)).ToList(),
                                                                 _obj.OtherGroup.All.ToList(),
                                                                 ActionItemExecutionTasks.As(e.Attachment));
    }

    public virtual void ResolutionGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      // В качестве проектов резолюции нельзя добавить поручения-непроекты.
      if (_obj.ResolutionGroup.ActionItemExecutionTasks.Any(a => a.IsDraftResolution != true))
      {
        foreach (var actionItem in _obj.ResolutionGroup.ActionItemExecutionTasks.Where(a => a.IsDraftResolution != true))
          _obj.ResolutionGroup.ActionItemExecutionTasks.Remove(actionItem);
        throw AppliedCodeException.Create(DocumentReviewTasks.Resources.CanAddOnlyDraftResolutionToTask);
      }
    }
    
    public virtual void DocumentForReviewGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      // Сброс на тему по умолчанию.
      using (TenantInfo.Culture.SwitchTo())
        _obj.Subject = Docflow.Resources.AutoformatTaskSubject;
    }

    public virtual void DocumentForReviewGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      var document = OfficialDocuments.As(e.Attachment);
      
      // Задать тему.
      using (TenantInfo.Culture.SwitchTo())
        _obj.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.Consideration, document.Name);
      
      // Задать адресатов.
      Functions.DocumentReviewTask.SynchronizeAddressees(_obj, document);
      
      /* Задать срок рассмотрения в рабочих часах инициатора.
       * Так как в схеме задачи для рассмотрения руководителем устанавливается относительный срок,
       * равный количеству рабочих часов инициатора между стартом задачи и сроком.
       */
      if (!_obj.Deadline.HasValue)
      {
        _obj.Deadline = Docflow.PublicFunctions.DocumentKind.GetConsiderationDate(document.DocumentKind, Users.Current) ??
          Calendar.Now.AddWorkingDays(Users.Current, Functions.Module.Remote.GetDocumentReviewDefaultDays());
      }
      
      Docflow.PublicFunctions.OfficialDocument.DocumentAttachedInMainGroup(document, _obj);
      Docflow.PublicFunctions.OfficialDocument.AddRelatedDocumentsToAttachmentGroup(document, _obj.OtherGroup);
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue.Length > DocumentReviewTasks.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, DocumentReviewTasks.Info.Properties.Subject.Length);
    }

    public virtual void DeadlineChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      // Продублировать срок в крайний срок для списков.
      _obj.MaxDeadline = e.NewValue;
    }
  }
}