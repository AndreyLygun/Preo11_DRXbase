using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.VerificationAssignment;

namespace Sungero.SmartProcessing
{
  partial class VerificationAssignmentClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      // Определеяем использует ли задача no-code вариант процесса или старую схему.
      if (Docflow.PublicFunctions.Module.IsTaskUsingOldScheme(_obj.Task))
        e.Instruction = Functions.VerificationAssignment.GetInstruction(_obj);
    }

    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      e.Params.Remove(Sungero.SmartProcessing.PublicConstants.VerificationAssignment.CanDeleteParamName);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      e.Params.AddOrUpdate(Sungero.SmartProcessing.PublicConstants.VerificationAssignment.CanDeleteParamName,
                           Sungero.Docflow.OfficialDocuments.AccessRights.CanDelete());
      
      if (e.Params.Contains(Sungero.SmartProcessing.Constants.VerificationAssignment.ShowRepackingResultsNotificationParamName))
      {
        var needShowNotification = false;
        e.Params.TryGetValue(Sungero.SmartProcessing.Constants.VerificationAssignment.ShowRepackingResultsNotificationParamName, out needShowNotification);
        if (needShowNotification)
        {
          e.AddInformation(Sungero.SmartProcessing.VerificationAssignments.Resources.ShowRepackingResultsNotification);
          e.Params.Remove(Sungero.SmartProcessing.Constants.VerificationAssignment.ShowRepackingResultsNotificationParamName);
        }
      }
    }

  }
}