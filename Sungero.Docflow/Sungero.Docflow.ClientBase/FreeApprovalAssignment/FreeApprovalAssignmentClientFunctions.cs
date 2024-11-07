using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalAssignment;

namespace Sungero.Docflow.Client
{
  partial class FreeApprovalAssignmentFunctions
  {
    /// <summary>
    /// Подтвердить выполнение задания.
    /// </summary>
    /// <param name="action">Действие.</param>
    /// <returns>True, если пользователь нажал "Да" в диалоге подтверждения, иначе False.</returns>
    public bool ConfirmCompleteAssignment(Domain.Shared.IActionInfo action)
    {
      var confirmationMessage = action.ConfirmationMessage;
      if (_obj.AddendaGroup.ElectronicDocuments.Any())
        confirmationMessage = Docflow.FreeApprovalAssignments.Resources.ApprovalConfirmationMessage;
      
      // Замена стандартного диалога подтверждения выполнения действия.
      return Docflow.Functions.Module.ShowConfirmationDialog(confirmationMessage, null, null,
                                                             Constants.FreeApprovalTask.FreeApprovalAssignmentConfirmDialogID.Approved);
    }
    
    /// <summary>
    /// Показать диалог переадресации.
    /// </summary>
    /// <returns>True, если запрос был подтвержден.</returns>
    public virtual bool ShowForwardingDialog()
    {
      var notAvailablePerformers = Functions.Module.Remote.GetParallelAssignmentsPerformers(_obj).ToList();

      var dialogResult = Docflow.PublicFunctions.Module.ShowForwardDialog(notAvailablePerformers, _obj.Deadline, TimeSpan.Zero);
      if (dialogResult.ForwardButtonIsPressed)
      {
        _obj.Addressee = dialogResult.ForwardTo;
        _obj.AddresseeDeadline = dialogResult.Deadline;
        return true;
      }
      
      return false;
    }
  }
}