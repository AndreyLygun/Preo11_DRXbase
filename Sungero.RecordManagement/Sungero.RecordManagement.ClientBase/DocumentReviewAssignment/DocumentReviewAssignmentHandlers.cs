using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DocumentReviewAssignment;

namespace Sungero.RecordManagement
{
  partial class DocumentReviewAssignmentClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if (_obj.AssistantPrepareResolution == false)
        e.HideAction(_obj.Info.Actions.DraftResRework);
      
      // Скрывать группу с подчиненными поручениями и предметное отображение, при отсутствии черновиков проектов резолюции.
      var withDraftResolution = _obj.ResolutionGroup.ActionItemExecutionTasks.Any();
      _obj.State.Attachments.ResolutionGroup.IsVisible = withDraftResolution;
      _obj.State.Controls.StateView.IsVisible = withDraftResolution;
      
      // Скрывать результат выполнения "Вернуть инициатору" для задач, стартованных в рамках согласования по регламенту или согласования по процессу.
      if (DocumentReviewTasks.Is(_obj.Task) && Functions.DocumentReviewTask.ReviewStartedFromApproval(DocumentReviewTasks.As(_obj.Task)))
        e.HideAction(_obj.Info.Actions.DocsRework);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var canReadDocument = Functions.DocumentReviewAssignment.HasDocumentAndCanRead(_obj);
      if (!canReadDocument)
        e.AddError(Docflow.Resources.NoRightsToDocument);
      
      if (_obj.Status == Workflow.Assignment.Status.InProcess && _obj.ResolutionGroup.ActionItemExecutionTasks.Any(x => !PublicFunctions.ActionItemExecutionTask.IsDeadlineAssigned(x)))
        e.AddWarning(Sungero.RecordManagement.Resources.FindResolutionWithoutDeadline);
    }

  }
}