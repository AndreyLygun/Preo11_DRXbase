using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.PowerOfAttorneyCore.Client
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Создать и отобразить подключение нашей организации к сервису доверенностей.
    /// </summary>
    [LocalizeFunction("CreateAndShowServiceAttorneyConnectionFunctionName", "CreateAndShowServiceAttorneyConnectionFunctionDescription")]
    public virtual void CreateAndShowServiceAttorneyConnection()
    {
      Functions.Module.Remote.CreateAttorneyServiceConnection().Show();
    }
  }
}