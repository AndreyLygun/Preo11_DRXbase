using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineExtensionTask;
using Sungero.Workflow;

namespace Sungero.Docflow.Server
{
  partial class DeadlineExtensionTaskRouteHandlers
  {

    #region -  3 - Решение на продление срока
    
    public virtual void StartBlock3(Sungero.Docflow.Server.DeadlineExtensionAssignmentArguments e)
    {
      if (_obj.ParentAssignment.Status != Workflow.AssignmentBase.Status.InProcess)
        return;
      
      e.Block.Subject = Functions.DeadlineExtensionTask.GetDeadlineExtensionSubject(_obj, DeadlineExtensionTasks.Resources.RequestExtensionDeadline);
      e.Block.Performers.Add(_obj.Assignee);
      e.Block.ScheduledDate = _obj.CurrentDeadline;
      e.Block.NewDeadline = _obj.NewDeadline;
      e.Block.RelativeDeadlineDays = 1;
    }

    public virtual void StartAssignment3(Sungero.Docflow.IDeadlineExtensionAssignment assignment, Sungero.Docflow.Server.DeadlineExtensionAssignmentArguments e)
    {
      _obj.MaxDeadline = assignment.Deadline;
      
      // "От".
      assignment.Author = _obj.Author;

      // Выдать права на изменение для возможности прекращения подзадач.
      if (RecordManagement.ActionItemExecutionAssignments.As(_obj.ParentAssignment) != null)
        Sungero.RecordManagement.PublicFunctions.ActionItemExecutionTask.Remote.GrantAccessRightToAssignment(assignment, _obj);

    }

    public virtual void CompleteAssignment3(Sungero.Docflow.IDeadlineExtensionAssignment assignment, Sungero.Docflow.Server.DeadlineExtensionAssignmentArguments e)
    {
      _obj.NewDeadline = assignment.NewDeadline;
      if (assignment.Result == Sungero.Docflow.DeadlineExtensionAssignment.Result.Forward)
      {
        assignment.Forward(assignment.ForwardTo, ForwardingLocation.Next);
        _obj.Assignee = assignment.ForwardTo;
      }
      
    }

    public virtual void EndBlock3(Sungero.Docflow.Server.DeadlineExtensionAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region -  4 - Принятие результата запроса продления срока
    
    public virtual void StartBlock4(Sungero.Docflow.Server.DeadlineRejectionAssignmentArguments e)
    {
      if (_obj.ParentAssignment.Status != Workflow.AssignmentBase.Status.InProcess)
        return;
      
      e.Block.Subject = Functions.DeadlineExtensionTask.GetDeadlineExtensionSubject(_obj, DeadlineExtensionTasks.Resources.ExtensionDeadlineDenied);
      e.Block.Performers.Add(_obj.Author);
      e.Block.CurrentDeadline = _obj.CurrentDeadline;
      e.Block.NewDeadline = _obj.NewDeadline;
      e.Block.RelativeDeadlineDays = 1;
    }

    public virtual void StartAssignment4(Sungero.Docflow.IDeadlineRejectionAssignment assignment, Sungero.Docflow.Server.DeadlineRejectionAssignmentArguments e)
    {
      _obj.MaxDeadline = assignment.Deadline;
      
      // "От".
      assignment.Author = _obj.Assignee;
    }

    public virtual void CompleteAssignment4(Sungero.Docflow.IDeadlineRejectionAssignment assignment, Sungero.Docflow.Server.DeadlineRejectionAssignmentArguments e)
    {
      // Сохранить срок.
      _obj.NewDeadline = assignment.NewDeadline;
    }

    public virtual void EndBlock4(Sungero.Docflow.Server.DeadlineRejectionAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion

    #region -  5 - Продление срока (сценарий)
    
    public virtual void Script5Execute()
    {
      Functions.DeadlineExtensionTask.ProcessAssignmentDeadlineExtension(_obj);
      Functions.DeadlineExtensionTask.ProcessTaskDeadlineExtension(_obj);
    }
    
    #endregion

    #region -  6 - Уведомление о продлении срока
    
    public virtual void StartBlock6(Sungero.Docflow.Server.DeadlineExtensionNotificationArguments e)
    {
      if (_obj.ParentAssignment.Status != Workflow.AssignmentBase.Status.InProcess)
        return;
      
      var desiredDeadline = _obj.NewDeadline.Value;
      var desiredDeadlineLabel = Functions.DeadlineExtensionTask.GetDesiredDeadlineLabel(desiredDeadline);
      var subjectFormat = DeadlineExtensionTasks.Resources.ExtensionDeadlineFormat(desiredDeadlineLabel);
      var subject = Functions.DeadlineExtensionTask.GetDeadlineExtensionSubject(_obj, subjectFormat);
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
      
      e.Block.PreviousDeadline = _obj.CurrentDeadline;
      e.Block.NewDeadline = desiredDeadline;
      
      var performers = Functions.DeadlineExtensionTask.GetPerformersForNotification(_obj);
      foreach (var performer in performers)
        e.Block.Performers.Add(performer);
    }

    public virtual void StartNotice6(Sungero.Docflow.IDeadlineExtensionNotification notice, Sungero.Docflow.Server.DeadlineExtensionNotificationArguments e)
    {
      // "От".
      notice.Author = _obj.Assignee;
      Functions.DeadlineExtensionTask.ProcessExtendDeadlineNotice(_obj, notice);
    }

    public virtual void EndBlock6(Sungero.Docflow.Server.DeadlineExtensionNotificationEndBlockEventArguments e)
    {
      
    }
    
    #endregion

    public virtual void StartReviewAssignment2(Sungero.Workflow.IReviewAssignment reviewAssignment)
    {
      
    }

  }
}