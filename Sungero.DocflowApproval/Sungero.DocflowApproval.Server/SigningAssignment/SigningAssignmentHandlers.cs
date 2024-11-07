using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.SigningAssignment;

namespace Sungero.DocflowApproval
{

  partial class SigningAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (Functions.SigningAssignment.AreDocumentsLockedByMe(_obj))
      {
        e.AddError(Resources.SaveDocumentsBeforeComplete);
        return;
      }
      
      if (_obj.Result == Result.Sign)
        e.Result = SigningAssignments.Resources.DocumentSigned;
      else if (_obj.Result == Result.ForRework)
        e.Result = DocflowApproval.Resources.ForRework;
      else if (_obj.Result == Result.Reject)
        e.Result = Docflow.ApprovalTasks.Resources.AbortedSigning;
      else if (_obj.Result == Result.Forward)
        e.Result = DocflowApproval.Resources.ForwardedFormat(Company.PublicFunctions.Employee.GetShortName(_obj.ForwardTo, DeclensionCase.Dative, true));
    }
  }

}