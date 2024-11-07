using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceTask;

namespace Sungero.RecordManagement
{
  partial class AcquaintanceTaskSharedHandlers
  {

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue.Length > AcquaintanceTasks.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, AcquaintanceTasks.Info.Properties.Subject.Length);
    }

    public virtual void DocumentGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      _obj.Subject = _obj.DocumentGroup.All.Any()
        ? AcquaintanceTasks.Resources.AcquaintanceTaskSubjectFormat(_obj.DocumentGroup.All.First().DisplayValue)
        : Docflow.Resources.AutoformatTaskSubject;
      
      // Очистка вложений.
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, null);
      var document = Docflow.OfficialDocuments.As(e.Attachment);
      if (document != null)
        Docflow.PublicFunctions.OfficialDocument.RemoveRelatedDocumentsFromAttachmentGroup(document, _obj.OtherGroup);
    }

    public virtual void DocumentGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      _obj.Subject = AcquaintanceTasks.Resources.AcquaintanceTaskSubjectFormat(e.Attachment.DisplayValue);

      // Добавление вложений.
      var document = Docflow.OfficialDocuments.As(e.Attachment);
      if (!_obj.State.IsCopied)
        Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);
      
      Docflow.PublicFunctions.OfficialDocument.DocumentAttachedInMainGroup(document, _obj);
    }
    
    public virtual void DeadlineChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      _obj.MaxDeadline = e.NewValue;
    }
  }
}