using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.DocflowApproval.DocumentProcessingAssignment;

namespace Sungero.DocflowApproval
{
  partial class DocumentProcessingAssignmentClientHandlers
  {
    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      Functions.DocumentProcessingAssignment.Remote.FillEntityParams(_obj);
      
      var allowSendForRework = Functions.DocumentProcessingAssignment.CanSendForRework(_obj);
      if (!allowSendForRework)
        e.HideAction(_obj.Info.Actions.ForRework);
      
      if (_obj.Status == Status.InProcess && Functions.DocumentProcessingAssignment.IsUserWithoutRightsOnMainDocument(_obj))
      {
        e.HideAction(_obj.Info.Actions.Complete);
        e.HideAction(_obj.Info.Actions.ApprovalForm);
        e.HideAction(_obj.Info.Actions.CreateCoverLetter);
        e.HideAction(_obj.Info.Actions.SendByMail);
        e.HideAction(_obj.Info.Actions.CreateAcquaintance);
        e.HideAction(_obj.Info.Actions.CreateActionItem);
        e.AddError(Docflow.Resources.NoRightsToDocument);
      }
      else if (_obj.CreateActionItems != true)
      {
        e.HideAction(_obj.Info.Actions.CreateAcquaintance);
      }
    }
    
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.DocumentProcessingAssignment.FillEntityParamsIfEmpty(_obj);
      
      if (_obj.Status == Status.InProcess && Functions.DocumentProcessingAssignment.IsUserWithoutRightsOnMainDocument(_obj))
        e.AddError(Docflow.Resources.NoRightsToDocument);
      
      var allowSendForRework = Functions.DocumentProcessingAssignment.CanSendForRework(_obj);
      _obj.State.Properties.ReworkPerformer.IsVisible = allowSendForRework &&
        Functions.DocumentProcessingAssignment.CanChangeReworkPerformer(_obj);
      
      _obj.State.Properties.DeliveryMethodDescription.IsVisible = _obj.SendToCounterparty == true && !string.IsNullOrEmpty(_obj.DeliveryMethodDescription);
    }
  }
}