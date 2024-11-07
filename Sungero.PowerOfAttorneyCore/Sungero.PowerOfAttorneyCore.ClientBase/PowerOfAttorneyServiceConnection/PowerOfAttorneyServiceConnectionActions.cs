using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyCore.PowerOfAttorneyServiceConnection;

namespace Sungero.PowerOfAttorneyCore.Client
{
  partial class PowerOfAttorneyServiceConnectionActions
  {
    public virtual void ForceSave(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      e.Params.AddOrUpdate(Constants.PowerOfAttorneyServiceConnection.ForceSaveParamName, true);
      _obj.Save();
    }

    public virtual bool CanForceSave(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void CheckConnection(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      var organizationId = Functions.Module.Remote.GetOrganizationIdFromService(_obj);
      if (string.IsNullOrEmpty(organizationId))
      {
        if (_obj.ConnectionStatus != ConnectionStatus.Error)
          _obj.ConnectionStatus = ConnectionStatus.Error;
        
        e.AddWarning(PowerOfAttorneyServiceConnections.Resources.PowerOfAttorneyConnectionError);
        return;
      }
      else
      {
        if (_obj.OrganizationId != organizationId)
          _obj.OrganizationId = organizationId;
        
        if (_obj.ConnectionStatus != ConnectionStatus.Connected)
          _obj.ConnectionStatus = ConnectionStatus.Connected;
        
        Dialogs.NotifyMessage(PowerOfAttorneyServiceConnections.Resources.PowerOfAttorneyConnectionEstablished);
      }
    }

    public virtual bool CanCheckConnection(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate();
    }

  }

}