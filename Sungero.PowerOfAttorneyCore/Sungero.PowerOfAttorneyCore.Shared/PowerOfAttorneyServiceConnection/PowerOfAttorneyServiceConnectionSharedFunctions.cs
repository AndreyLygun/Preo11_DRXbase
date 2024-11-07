using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyCore.PowerOfAttorneyServiceConnection;

namespace Sungero.PowerOfAttorneyCore.Shared
{
  partial class PowerOfAttorneyServiceConnectionFunctions
  {
    /// <summary>
    /// Сформировать имя подключения к сервису доверенностей.
    /// </summary>
    public virtual void FillName()
    {
      if (_obj.BusinessUnit != null && _obj.ServiceApp != null)
        _obj.Name = string.Format("{0} ({1})", _obj.BusinessUnit, _obj.ServiceApp);
    }
  }
}