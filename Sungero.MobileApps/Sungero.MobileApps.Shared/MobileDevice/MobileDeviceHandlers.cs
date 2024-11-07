using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.MobileApps.MobileDevice;

namespace Sungero.MobileApps
{
  partial class MobileDeviceSharedHandlers
  {
    public virtual void EmployeeChanged(Sungero.MobileApps.Shared.MobileDeviceEmployeeChangedEventArgs e)
    {
      Functions.MobileDevice.FillName(_obj);
    }
    
    public virtual void DeviceNameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      Functions.MobileDevice.FillName(_obj);
    }
  }
}