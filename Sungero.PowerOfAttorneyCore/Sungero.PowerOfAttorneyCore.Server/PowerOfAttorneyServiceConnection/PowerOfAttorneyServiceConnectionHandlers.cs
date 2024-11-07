using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyCore.PowerOfAttorneyServiceConnection;

namespace Sungero.PowerOfAttorneyCore
{
  partial class PowerOfAttorneyServiceConnectionCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.OrganizationId);
      e.Without(_info.Properties.ConnectionStatus);
    }
  }

  partial class PowerOfAttorneyServiceConnectionServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      Functions.PowerOfAttorneyServiceConnection.FillName(_obj);
      
      // Проверяем дубли только для активных записей.
      if (_obj.Status == Status.Active &&
          Functions.PowerOfAttorneyServiceConnection.GetDuplicates(_obj).Any())
      {
        e.AddError(_obj.Info.Properties.BusinessUnit, PowerOfAttorneyServiceConnections.Resources.DuplicateServiceConnection);
      }
      
      // Мягкая проверка подключения с возможностью сохранить запись.
      var organizationId = Functions.Module.GetOrganizationIdFromService(_obj);
      _obj.OrganizationId = organizationId;
      if (string.IsNullOrEmpty(organizationId))
      {
        _obj.ConnectionStatus = ConnectionStatus.Error;
        if (!e.Params.Contains(Constants.PowerOfAttorneyServiceConnection.ForceSaveParamName))
        {
          e.AddError(PowerOfAttorneyServiceConnections.Resources.PowerOfAttorneyConnectionError, _obj.Info.Actions.ForceSave);
          return;
        }
      }
      else
      {
        _obj.ConnectionStatus = ConnectionStatus.Connected;
      }
    }
    
  }

}