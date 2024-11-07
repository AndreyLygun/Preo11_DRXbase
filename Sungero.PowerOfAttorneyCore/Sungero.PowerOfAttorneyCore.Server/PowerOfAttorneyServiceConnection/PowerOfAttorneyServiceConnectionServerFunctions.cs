using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyCore.PowerOfAttorneyServiceConnection;

namespace Sungero.PowerOfAttorneyCore.Server
{
  partial class PowerOfAttorneyServiceConnectionFunctions
  {
    /// <summary>
    /// Найти дубликаты по НОР и сервису доверенности.
    /// </summary>
    /// <returns>Список дубликатов.</returns>
    public virtual List<IPowerOfAttorneyServiceConnection> GetDuplicates()
    {
      return PowerOfAttorneyServiceConnections.GetAll()
        .Where(x => !Equals(x, _obj) && Equals(x.BusinessUnit, _obj.BusinessUnit) && x.Status == Status.Active).ToList();
    }
  }
}