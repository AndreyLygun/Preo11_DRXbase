using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.MobileApps.MobileDevice;

namespace Sungero.MobileApps.Shared
{
  partial class MobileDeviceFunctions
  {
    /// <summary>
    /// Устанавливает статус устройства на основе числового значения.
    /// </summary>
    /// <param name="intStatus">Числовое значение статуса.</param>
    public virtual void SetStatusFromCode(int intStatus)
    {
      switch (intStatus)
      {
        case 0: _obj.DeviceStatus = DeviceStatus.None;
                break;
        case 1: _obj.DeviceStatus = DeviceStatus.Enabled;
                break;
        case 2: _obj.DeviceStatus = DeviceStatus.Disabled;
                break;
      }
    }

    /// <summary>
    /// Заполнить название устройства.
    /// </summary>
    public virtual void FillName()
    {
      _obj.Name = string.Format("{0} - {1}", _obj.Employee?.DisplayValue, _obj.DeviceName);
    }
  }
}