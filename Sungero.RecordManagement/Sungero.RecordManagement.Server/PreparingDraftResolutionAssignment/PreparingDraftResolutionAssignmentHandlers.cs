using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.PreparingDraftResolutionAssignment;

namespace Sungero.RecordManagement
{
  partial class PreparingDraftResolutionAssignmentServerHandlers
  {

    public override void BeforeReturnUncompleted(Sungero.Workflow.Server.BeforeReturnUncompletedEventArgs e)
    {
      var allActionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.ToList();
      var actionItemsToDelete = Functions.Module.GetActionItemsAddedToAssignment(_obj, allActionItems, Company.Employees.Current, 
                                                                                 PublicConstants.PreparingDraftResolutionAssignment.ResolutionGroupName);
      foreach (var actionItemToDelete in actionItemsToDelete)
        _obj.ResolutionGroup.ActionItemExecutionTasks.Remove(actionItemToDelete);
    }

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (_obj.Result.Value == Result.Forward)
        e.Result = DocumentReviewTasks.Resources.ForwardFormat(Company.PublicFunctions.Employee.GetShortName(_obj.Addressee, DeclensionCase.Dative, true));
      if (_obj.Result.Value == Result.SendForReview)
        e.Result = PreparingDraftResolutionAssignments.Resources.SentForReview;
      if (_obj.Result.Value == Result.AddAssignment)
        e.Result = ReviewManagerAssignments.Resources.AssignmentCreated;
      if (_obj.Result.Value == Result.ForRework)
        e.Result = ReviewDraftResolutionAssignments.Resources.ReworkResolution;
    }
  }

}