using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineExtensionTask;
using Sungero.Workflow;

namespace Sungero.Docflow.Server.DeadlineExtensionTaskBlocks
{
  partial class NotifyAboutDeadlineExtensionBlockHandlers
  {

    public virtual void NotifyAboutDeadlineExtensionBlockStartNotice(Sungero.Docflow.IDeadlineExtensionNotification notice)
    {
      Functions.DeadlineExtensionTask.ProcessExtendDeadlineNotice(_obj, notice);
    }
  }

  partial class ProcessTaskDeadlineExtensionBlockHandlers
  {
    public virtual void ProcessTaskDeadlineExtensionBlockExecute()
    {
      Functions.DeadlineExtensionTask.ProcessTaskDeadlineExtension(_obj);
    }
  }

  partial class ProcessAssignmentDeadlineExtensionBlockHandlers
  {
    public virtual void ProcessAssignmentDeadlineExtensionBlockExecute()
    {
      var parentAssignment = Assignments.As(_obj.ParentAssignment);
      if (parentAssignment != null && Functions.Module.IsCompetitive(parentAssignment))
        Functions.DeadlineExtensionTask.ProcessCompetitiveAssignmentsDeadlineExtension(_obj, parentAssignment);
      else
        Functions.DeadlineExtensionTask.ProcessAssignmentDeadlineExtension(_obj);
    }
  }

  partial class AcceptDeadlineRejectionBlockHandlers
  {

    public virtual void AcceptDeadlineRejectionBlockStartAssignment(Sungero.Docflow.IDeadlineRejectionAssignment assignment)
    {
      _obj.MaxDeadline = assignment.Deadline;
      assignment.CurrentDeadline = _obj.CurrentDeadline;
      assignment.NewDeadline = _obj.NewDeadline;
    }

    public virtual void AcceptDeadlineRejectionBlockCompleteAssignment(Sungero.Docflow.IDeadlineRejectionAssignment assignment)
    {
      _obj.NewDeadline = assignment.NewDeadline;
    }
  }

  partial class RequestDeadlineExtensionBlockHandlers
  {

    public virtual void RequestDeadlineExtensionBlockStartAssignment(Sungero.Docflow.IDeadlineExtensionAssignment assignment)
    {
      _obj.MaxDeadline = assignment.Deadline;
      assignment.ScheduledDate = _obj.CurrentDeadline;
      assignment.NewDeadline = _obj.NewDeadline;
    }
    
    public virtual void RequestDeadlineExtensionBlockCompleteAssignment(Sungero.Docflow.IDeadlineExtensionAssignment assignment)
    {
      _obj.NewDeadline = assignment.NewDeadline;
      if (assignment.Result == Sungero.Docflow.DeadlineExtensionAssignment.Result.Forward)
      {
        assignment.Forward(assignment.ForwardTo, ForwardingLocation.Next);
        _obj.Assignee = assignment.ForwardTo;
      }
      
    }
  }
}