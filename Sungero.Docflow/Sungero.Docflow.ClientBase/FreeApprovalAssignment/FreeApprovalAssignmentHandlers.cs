using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalAssignment;

namespace Sungero.Docflow
{
  partial class FreeApprovalAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var canReadDocument = Functions.FreeApprovalTask.HasDocumentAndCanRead(FreeApprovalTasks.As(_obj.Task));
      
      if (!canReadDocument)
        e.AddError(Docflow.Resources.NoRightsToDocument);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      var schemeVersion = _obj.Task.GetStartedSchemeVersion();
      if (schemeVersion == LayerSchemeVersions.V1 || schemeVersion == LayerSchemeVersions.V2)
      {
        e.HideAction(_obj.Info.Actions.Forward);
        e.HideAction(_obj.Info.Actions.AddApprover);
      }
    }
  }

}