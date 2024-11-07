using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.DocflowApproval.AdvancedAssignment;

namespace Sungero.DocflowApproval
{

  partial class AdvancedAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (Functions.AdvancedAssignment.AreDocumentsLockedByMe(_obj))
      {
        e.AddError(Resources.SaveDocumentsBeforeComplete);
        return;
      }
      
      if (_obj.Result == Result.ForRework)
        e.Result = AdvancedAssignments.Resources.ForRework;
      else if (_obj.Result == Result.Forward)
        e.Result = DocflowApproval.Resources
          .ForwardedFormat(Company.PublicFunctions.Employee.GetShortName(_obj.ForwardTo, DeclensionCase.Dative, true));
    }
  }

}