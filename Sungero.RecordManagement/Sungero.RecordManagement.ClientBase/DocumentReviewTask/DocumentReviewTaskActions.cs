using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DocumentReviewTask;

namespace Sungero.RecordManagement.Client
{
  partial class DocumentReviewTaskActions
  {
    public override void Abort(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null, Constants.DocumentReviewTask.AbortConfirmDialogID))
        return;
      
      base.Abort(e);
    }

    public override bool CanAbort(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanAbort(e);
    }

    public virtual void AddResolution(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      PublicFunctions.DocumentReviewTask.AddResolution(_obj);
    }

    public virtual bool CanAddResolution(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status.Value == Workflow.Task.Status.Draft && Functions.DocumentReviewTask.HasDocumentAndCanRead(_obj);
    }

    public override void Start(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!RecordManagement.Functions.DocumentReviewTask.ValidateDocumentReviewTaskStart(_obj, e))
        return;
      
      // Проверить, что все поручения выданы адресатами рассмотрения
      // и инициатор может готовить для них проекты резолюций.
      var actionItemAssigners = _obj.ResolutionGroup.ActionItemExecutionTasks.Select(a => a.AssignedBy).ToList();
      if (actionItemAssigners.Any(x => _obj.Addressees.All(a => !Equals(a.Addressee, x))) ||
          !Functions.DocumentReviewTask.Remote.CanAuthorPrepareResolutionForAddressees(_obj, actionItemAssigners))
      {
        e.AddError(RecordManagement.Resources.ActionItemsMustBeAssignedByAddressee);
        return;
      }
      
      // Вывести запрос прав на группу "Дополнительно".
      var grantRightDialogResult = Docflow.PublicFunctions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList());
      if (grantRightDialogResult == false)
        return;
      
      // Вывести стандартный диалог подтверждения выполнения действия.
      if (_obj.NeedDeleteActionItems != true &&
          grantRightDialogResult == null &&
          !Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null, Constants.DocumentReviewTask.StartConfirmDialogID))
        return;
      
      base.Start(e);
    }

    public override bool CanStart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanStart(e);
    }

  }
}