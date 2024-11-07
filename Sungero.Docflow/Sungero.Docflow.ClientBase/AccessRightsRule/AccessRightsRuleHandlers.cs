using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccessRightsRule;

namespace Sungero.Docflow
{
  partial class AccessRightsRuleClientHandlers
  {
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var bulkProcessingIsInProcess = _obj.BulkProcessingState == AccessRightsRule.BulkProcessingState.InProcess;
      
      _obj.State.Properties.Status.IsEnabled = !bulkProcessingIsInProcess;
      
      _obj.State.Properties.DocumentKinds.IsEnabled = !bulkProcessingIsInProcess;
      _obj.State.Properties.BusinessUnits.IsEnabled = !bulkProcessingIsInProcess;
      _obj.State.Properties.Departments.IsEnabled = !bulkProcessingIsInProcess;
      
      var availableDocumentGroups = Functions.AccessRightsRule.GetDocumentGroups(_obj);
      _obj.State.Properties.DocumentGroups.IsEnabled = !bulkProcessingIsInProcess && availableDocumentGroups.Any();
      
      _obj.State.Properties.GrantRightsOnLeadingDocument.IsEnabled = !bulkProcessingIsInProcess;
      _obj.State.Properties.GrantRightsOnExistingDocuments.IsEnabled = !bulkProcessingIsInProcess;
      
      _obj.State.Properties.Members.IsEnabled = !bulkProcessingIsInProcess;
      
      if (bulkProcessingIsInProcess)
        e.AddInformation(Sungero.Docflow.AccessRightsRules.Resources.BulkProcessingIsInProcess, _obj.Info.Actions.CancelBulkProcessing);
    }
  }
}