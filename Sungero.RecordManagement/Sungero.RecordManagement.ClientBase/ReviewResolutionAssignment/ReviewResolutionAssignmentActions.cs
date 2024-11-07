using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewResolutionAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class ReviewResolutionAssignmentActions
  {

    public virtual void CreateActionItem(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.Save();
      
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      var assignedBy = Docflow.PublicFunctions.Module.Remote.GetResolutionAuthor(_obj.Task);
      var task = Functions.Module.Remote.CreateActionItemExecutionWithResolution(document,
                                                                                 _obj.Id,
                                                                                 _obj.ResolutionText,
                                                                                 assignedBy);
      
      Functions.ActionItemExecutionTask.SynchronizeDocumentGroupsToActionItem(task,
                                                                              document,
                                                                              _obj.AddendaGroup.OfficialDocuments.Select(x => Sungero.Content.ElectronicDocuments.As(x)).ToList(),
                                                                              _obj.OtherGroup.All.ToList());
      
      task.ShowModal();
    }

    public virtual bool CanCreateActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status.Value == Workflow.Task.Status.InProcess &&
        Docflow.PublicFunctions.Module.IsWorkStarted(_obj) &&
        Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

    public virtual void AddAssignment(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var confirmationAccepted = Functions.Module.ShowConfirmationDialogCreationActionItem(_obj, _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault(), e);
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(), confirmationAccepted ? null : e.Action,
                                                                                            Constants.DocumentReviewTask.ReviewResolutionAssignmentConfirmDialogID))
        e.Cancel();
    }

    public virtual bool CanAddAssignment(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

  }
}