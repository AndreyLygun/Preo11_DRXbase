using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewReworkAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class ReviewReworkAssignmentFunctions
  {
    /// <summary>
    /// Проверить, что текущий сотрудник может готовить проект резолюции.
    /// </summary>
    /// <returns>True, если сотрудник может готовить проект резолюции, иначе - False.</returns>
    public virtual bool CanPrepareDraftResolution()
    {
      var canPrepareResolution = false;
      var formParams = ((Sungero.Domain.Shared.IExtendedEntity)_obj).Params;
      if (formParams.ContainsKey(PublicConstants.DocumentReviewTask.CanPrepareDraftResolutionParamName))
      {
        object paramValue;
        formParams.TryGetValue(PublicConstants.DocumentReviewTask.CanPrepareDraftResolutionParamName, out paramValue);
        bool.TryParse(paramValue.ToString(), out canPrepareResolution);
        return canPrepareResolution;
      }
      
      if (Company.Employees.Current != null)
        canPrepareResolution = Company.PublicFunctions.Employee.Remote.CanPrepareDraftResolution(Company.Employees.Current);
      formParams.Add(PublicConstants.DocumentReviewTask.CanPrepareDraftResolutionParamName, canPrepareResolution);
      return canPrepareResolution;
    }
    
    /// <summary>
    /// Показать диалог переадресации.
    /// </summary>
    /// <returns>True, если запрос был подтвержден.</returns>
    public virtual bool ShowForwardingDialog()
    {
      var documentReviewTask = DocumentReviewTasks.As(_obj.Task);
      var hasActionItemsToDelete = _obj.ResolutionGroup.ActionItemExecutionTasks.Where(x => !Equals(x.AssignedBy, _obj.Addressee)).Any() &&
        _obj.State.Attachments.ResolutionGroup.IsVisible;
      var dialogText = hasActionItemsToDelete
        ? ReviewReworkAssignments.Resources.ConfirmDeleteDraftResolutionAssignment
        : null;
      
      var dialogResult = Functions.Module.ShowForwardDialog(new List<IRecipient>() { _obj.Performer, documentReviewTask.Addressee }, dialogText);
      
      if (dialogResult.ForwardButtonIsPressed)
      {
        _obj.Addressee = dialogResult.ForwardTo;
        return true;
      }
      
      return false;
    }
  }
}