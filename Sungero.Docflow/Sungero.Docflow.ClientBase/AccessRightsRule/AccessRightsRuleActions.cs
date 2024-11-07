using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccessRightsRule;

namespace Sungero.Docflow.Client
{
  partial class AccessRightsRuleActions
  {
    public virtual void CancelBulkProcessing(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Sungero.Docflow.PublicFunctions.Module.ShowConfirmationDialog(Sungero.Docflow.AccessRightsRules.Resources.CancelBulkProcessing, null, null, null))
        return;
      
      var logMessagePrefix = string.Format("AccessRightsBulkProcessing. CancelBulkProcessing. Rule(ID={0},Launch={1}).", _obj.Id, _obj.LaunchId);

      try
      {
        e.Params.AddOrUpdate(Constants.AccessRightsRule.BulkProcessingCanceled, true);
        Functions.AccessRightsRule.CancelBulkProcessing(_obj);
        _obj.Save();
        Logger.DebugFormat("{0} Break access rights granting process.", logMessagePrefix);
      }
      catch (Exception ex)
      {
        Logger.DebugFormat("{0} Cannot break access rights granting process. {1}", logMessagePrefix, ex.ToString());
      }
    }

    public virtual bool CanCancelBulkProcessing(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() && _obj.BulkProcessingState == AccessRightsRule.BulkProcessingState.InProcess;
    }
  }
}