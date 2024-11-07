using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.MobileApps.MobileDevice;

namespace Sungero.MobileApps.Client
{
  partial class MobileDeviceActions
  {
    public virtual void RequestLogs(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var delay = Functions.MobileDevice.Remote.GetLogRequestDelay(_obj);
      if (delay > 0)
      {
        Dialogs.NotifyMessage(string.Format(Sungero.MobileApps.MobileDevices.Resources.LogRequestEstimateSeconds, delay));
        return;
      }
      
      Functions.MobileDevice.Remote.RequestLogs(_obj);
      Dialogs.NotifyMessage(Sungero.MobileApps.MobileDevices.Resources.LogRequestHasBeenSent);
    }
    
    public virtual bool CanRequestLogs(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.DeviceStatus == DeviceStatus.Enabled;
    }
    
    public virtual void DeleteData(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.MobileDevice.Remote.DeleteData(_obj);
      Dialogs.NotifyMessage(Sungero.MobileApps.MobileDevices.Resources.DataRemovalHasBeenSent);
    }
    
    public virtual bool CanDeleteData(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.DeviceStatus == DeviceStatus.Enabled;
    }
    
    public virtual void ResetSession(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.MobileDevice.Remote.ResetSeance(_obj);
      Dialogs.NotifyMessage(Sungero.MobileApps.MobileDevices.Resources.ResetSessionHasBeenSent);
    }
    
    public virtual bool CanResetSession(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.DeviceStatus == DeviceStatus.Enabled;
    }
  }
}