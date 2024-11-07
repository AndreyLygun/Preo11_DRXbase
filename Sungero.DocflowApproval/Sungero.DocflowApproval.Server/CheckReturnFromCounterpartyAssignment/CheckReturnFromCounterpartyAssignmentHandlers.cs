using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.DocflowApproval.CheckReturnFromCounterpartyAssignment;

namespace Sungero.DocflowApproval
{
  partial class CheckReturnFromCounterpartyAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (Functions.CheckReturnFromCounterpartyAssignment.AreDocumentsLockedByMe(_obj))
      {
        e.AddError(Resources.SaveDocumentsBeforeComplete);
        return;
      }
    }
  }

}