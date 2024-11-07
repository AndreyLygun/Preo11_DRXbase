using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalCheckReturnAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalCheckReturnAssignmentClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      // Инструкция из этапа контроля возврата.
      e.Instruction = Functions.ApprovalTask.Remote.GetCollapsedStageInstructions(ApprovalTasks.As(_obj.Task),
                                                                                  Sungero.Docflow.ApprovalStage.StageType.CheckReturn,
                                                                                  _obj.StageNumber);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
    }

  }
}