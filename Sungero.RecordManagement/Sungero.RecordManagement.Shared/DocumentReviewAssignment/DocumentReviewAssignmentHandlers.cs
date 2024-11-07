using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DocumentReviewAssignment;

namespace Sungero.RecordManagement
{
  partial class DocumentReviewAssignmentSharedHandlers
  {

    public virtual void ResolutionGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      // В качестве проектов резолюции нельзя добавить поручения-непроекты.
      if (_obj.ResolutionGroup.ActionItemExecutionTasks.Any(a => a.IsDraftResolution != true))
      {
        foreach (var actionItem in _obj.ResolutionGroup.ActionItemExecutionTasks.Where(a => a.IsDraftResolution != true))
          _obj.ResolutionGroup.ActionItemExecutionTasks.Remove(actionItem);
        throw AppliedCodeException.Create(DocumentReviewTasks.Resources.FindNotDraftResolution);
      }
      
      if (!Docflow.PublicFunctions.Module.IsWorkStarted(_obj))
        throw AppliedCodeException.Create(DocumentReviewTasks.Resources.CannotAddDraftResolutionIfWorkNotStarted);
    }

    public virtual void ResolutionGroupCreated(Sungero.Workflow.Interfaces.AttachmentCreatedEventArgs e)
    {
      if (!Docflow.PublicFunctions.Module.IsWorkStarted(_obj))
        throw AppliedCodeException.Create(DocumentReviewTasks.Resources.CannotAddDraftResolutionIfWorkNotStarted);
      
      if (!DocumentReviewTasks.Is(_obj.Task))
        return;
      
      Functions.DocumentReviewTask.FillDraftResolutionProperties(DocumentReviewTasks.As(_obj.Task),
                                                                 _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault(),
                                                                 _obj.AddendaGroup.OfficialDocuments.Select(x => Sungero.Content.ElectronicDocuments.As(x)).ToList(),
                                                                 _obj.OtherGroup.All.ToList(),
                                                                 ActionItemExecutionTasks.As(e.Attachment));
    }
  }
}