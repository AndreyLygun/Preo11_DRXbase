using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.SigningAssignment;

namespace Sungero.DocflowApproval
{
  partial class SigningAssignmentClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      Functions.SigningAssignment.Remote.FillEntityParams(_obj);
      
      if (_obj.Status == Status.InProcess && Functions.SigningAssignment.IsUserWithoutRightsOnMainDocument(_obj))
      {
        e.HideAction(_obj.Info.Actions.Sign);
        e.HideAction(_obj.Info.Actions.Reject);
        e.HideAction(_obj.Info.Actions.ApprovalForm);
        e.HideAction(_obj.Info.Actions.Forward);
        e.AddError(Docflow.Resources.NoRightsToDocument);
      }
      
      // Скрыть переадресацию, если в блоке она не разрешена.
      var isForwardingAllowed = Functions.SigningAssignment.CanForward(_obj);
      if (!isForwardingAllowed)
        e.HideAction(_obj.Info.Actions.Forward);
    }
    
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.SigningAssignment.FillEntityParamsIfEmpty(_obj);
      
      if (_obj.Status == Status.InProcess && Functions.SigningAssignment.IsUserWithoutRightsOnMainDocument(_obj))
        e.AddError(Docflow.Resources.NoRightsToDocument);
      
      _obj.State.Properties.ReworkPerformer.IsVisible = Functions.SigningAssignment.CanChangeReworkPerformer(_obj);
      
      // Скрыть контрол состояния со сводкой, если сводка пустая.
      _obj.State.Controls.DocumentSummary.IsVisible = Functions.SigningAssignment.NeedViewDocumentSummary(_obj);
      
      // Скрыть контрол листа согласования, если нет официального документа.
      var mainDocumentIsOfficial = OfficialDocuments.Is(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      _obj.State.Controls.ApprovalList.IsVisible = mainDocumentIsOfficial;
    }
  }
}