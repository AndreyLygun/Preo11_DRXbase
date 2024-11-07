using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalTask;

namespace Sungero.Docflow
{

  partial class FreeApprovalTaskSharedHandlers
  {
    public virtual void AddendaGroupPopulating(Sungero.Workflow.Interfaces.AttachmentGroupPopulatingEventArgs e)
    {
      e.PopulateFrom(_obj.ForApprovalGroup, documentGroup =>
                     PublicFunctions.Module.GetActualAddenda(documentGroup, Functions.FreeApprovalTask.GetRemovedAddenda(_obj)));
    }
    
    public virtual void AddendaGroupCreated(Sungero.Workflow.Interfaces.AttachmentCreatedEventArgs e)
    {
      // Метод оставлен для совместимости при перекрытии.
    }
    
    public virtual void AddendaGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      // Метод оставлен для совместимости при перекрытии.
    }
    
    public virtual void AddendaGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      // Метод оставлен для совместимости при перекрытии.
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue.Length > FreeApprovalTasks.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, FreeApprovalTasks.Info.Properties.Subject.Length);
    }

    public virtual void ForApprovalGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      // Очистить группу "Дополнительно".
      var document = OfficialDocuments.As(e.Attachment);
      if (OfficialDocuments.Is(document))
        Functions.OfficialDocument.RemoveRelatedDocumentsFromAttachmentGroup(OfficialDocuments.As(document), _obj.OtherGroup);
      
      _obj.Subject = Docflow.Resources.AutoformatTaskSubject;
    }

    public virtual void ForApprovalGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      var document = _obj.ForApprovalGroup.ElectronicDocuments.First();
      
      // Получить ресурсы в культуре тенанта.
      using (TenantInfo.Culture.SwitchTo())
        _obj.Subject = Functions.Module.TrimSpecialSymbols(FreeApprovalTasks.Resources.TaskSubject, document.Name);

      if (!_obj.State.IsCopied)
      {
        if (OfficialDocuments.Is(document))
          Functions.OfficialDocument.AddRelatedDocumentsToAttachmentGroup(OfficialDocuments.As(document), _obj.OtherGroup);
      }
      
      if (OfficialDocuments.Is(document))
        Functions.OfficialDocument.DocumentAttachedInMainGroup(OfficialDocuments.As(document), _obj);
    }

  }
}