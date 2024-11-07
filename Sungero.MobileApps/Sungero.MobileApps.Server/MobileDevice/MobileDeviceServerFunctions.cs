using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.MobileApps.MobileDevice;
using Sungero.MobileApps.Structures.MobileDevice;
using Sungero.MobileAppsExtensions;

namespace Sungero.MobileApps.Server
{
  partial class MobileDeviceFunctions
  {
    /// <summary>
    /// Отправляет сообщение о необходимости запроса клиентских логов.
    /// </summary>
    [Public, Remote(IsPure = true)]
    public virtual void RequestLogs()
    {
      var operation = new Enumeration(Constants.MobileDevice.RequestLogs);
      _obj.History.Write(operation, operation, null);
      _obj.Save();
      
      var args = Structures.MobileDevice.MobileDeviceRequestLogsEventArgs.Create(_obj.Employee.Id, _obj.DeviceId);
      Sungero.MobileAppsExtensions.MessageQueueSender.Send(Constants.Module.MobileAppQueueName, args);
    }
    
    /// <summary>
    /// Получает задержку в секундах до следующего запроса клиентских логов.
    /// </summary>
    /// <returns>Задержка в секундах до следующего запроса клиентских логов.</returns>
    [Public, Remote(IsPure = true)]
    public virtual int GetLogRequestDelay()
    {
      var operation = new Enumeration(Constants.MobileDevice.RequestLogs);
      var lastRequest = _obj.History.GetAll().FirstOrDefault(t => t.HistoryDate >= Calendar.Now.AddSeconds(-Constants.MobileDevice.RequestTimeout) && t.Operation == operation);

      if (lastRequest == null)
        return 0;
      
      return Constants.MobileDevice.RequestTimeout - (int)Math.Round((Calendar.Now - lastRequest.HistoryDate.Value).TotalSeconds);
    }
    
    /// <summary>
    /// Отправляет сообщение о необходимости удаления данных с устройства.
    /// </summary>
    [Public, Remote(IsPure = true)]
    public virtual void DeleteData()
    {
      var operation = new Enumeration(Constants.MobileDevice.DeleteData);
      
      _obj.DeviceStatus = DeviceStatus.Disabled;      
      _obj.History.Write(operation, operation, null);
      _obj.Save();
      
      var args = Structures.MobileDevice.MobileDeviceDeleteDataEventArgs.Create(_obj.Employee.Id, _obj.DeviceId);
      Sungero.MobileAppsExtensions.MessageQueueSender.Send(Constants.Module.MobileAppQueueName, args);
    }
       
    /// <summary>
    /// Отправляет сообщение о необходимости сброса сеанса.
    /// </summary>
    [Public, Remote(IsPure = true)]
    public virtual void ResetSeance()
    {
      var operation = new Enumeration(Constants.MobileDevice.ResetSeance);
      _obj.History.Write(operation, operation, null);
      _obj.Save();
      
      var args = Structures.MobileDevice.MobileDeviceResetSeanceEventArgs.Create(_obj.Employee.Id, _obj.DeviceId);
      Sungero.MobileAppsExtensions.MessageQueueSender.Send(Constants.Module.MobileAppQueueName, args);
    }
  }
}