using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.VerificationAssignment;

namespace Sungero.SmartProcessing
{
  partial class VerificationAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (_obj.Result == Result.Complete)
        e.Result = VerificationAssignments.Resources.Checked;
      else if (_obj.Result == Result.Forward)
        e.Result = VerificationAssignments.Resources.ForwardedFormat(Company.PublicFunctions.Employee.GetShortName(_obj.Addressee, DeclensionCase.Dative, true));
    }
  }

}