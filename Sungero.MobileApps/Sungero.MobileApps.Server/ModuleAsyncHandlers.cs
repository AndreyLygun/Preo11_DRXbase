using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.MobileApps.MobileDevice;

namespace Sungero.MobileApps.Server
{
  public class ModuleAsyncHandlers
  {
    /// <summary>
    /// Обновляет информацию об устройстве - ОС, приложение и название.
    /// </summary>
    /// <param name="args">Параметры вызова асинхронного обработчика.</param>
    public virtual void UpdateMobileDeviceInfoHandler(Sungero.MobileApps.Server.AsyncHandlerInvokeArgs.UpdateMobileDeviceInfoHandlerInvokeArgs args)
    {
      var deviceId = args.Id;
      var device = MobileDevices.GetAll().FirstOrDefault(x => x.Id == args.Id);

      if (device == null)
      {
        Logger.DebugFormat("UpdateMobileDeviceInfoHandler: mobile device with id {0} not found.", deviceId);
        return;
      }

      if (!Locks.TryLock(device))
      {
        Logger.DebugFormat("UpdateMobileDeviceInfoHandler: mobile device with id {0} is locked.", deviceId);

        args.Retry = true;
        return;
      }

      device.OsVersion = args.OsVersion;      
      device.AppVersion = args.AppVersion;
      device.DeviceName = args.DeviceName;
      device.Save();

      Locks.Unlock(device);

      Logger.DebugFormat("UpdateMobileDeviceInfoHandler: mobile device with id {0} changed info.", deviceId);
    }

    /// <summary>
    /// Обновляет статус устройства.
    /// </summary>
    /// <param name="args">Параметры вызова асинхронного обработчика.</param>
    public virtual void UpdateMobileDeviceStatusHandler(Sungero.MobileApps.Server.AsyncHandlerInvokeArgs.UpdateMobileDeviceStatusHandlerInvokeArgs args)
    {
      var deviceId = args.Id;
      var deviceIntStatus = args.IntStatus;

      var device = MobileDevices.GetAll().FirstOrDefault(x => x.Id == args.Id);

      if (device == null)
      {
        Logger.DebugFormat("UpdateMobileDeviceStatusHandler: mobile device with id {0} not found.", deviceId);
        return;
      }

      if (!Locks.TryLock(device))
      {
        Logger.DebugFormat("UpdateMobileDeviceStatusHandler: mobile device with id {0} is locked.", deviceId);

        args.Retry = true;
        return;
      }

      Functions.MobileDevice.SetStatusFromCode(device, deviceIntStatus);    
      device.Save();
      
      Locks.Unlock(device);

      Logger.DebugFormat("UpdateMobileDeviceStatusHandler: mobile device with id {0} changed device status to {1}.", deviceId, device.DeviceStatus.ToString());
    }
  }
}