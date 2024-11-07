using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyCore.PowerOfAttorneyClassifierGroup;

namespace Sungero.PowerOfAttorneyCore.Server
{
  partial class PowerOfAttorneyClassifierGroupFunctions
  {
    /// <summary>
    /// Получить все дубли текущей записи группы полномочий.
    /// </summary>
    /// <param name="excludeClosed">Признак поиска дублей без учёта закрытых записей.</param>
    /// <returns>Список дублей.</returns>
    /// <remarks>Дублями считаются записи с совпадающими значениям свойств "Код" или "Внешний GUID".</remarks>
    [Remote(IsPure = true)]
    public virtual List<IPowerOfAttorneyClassifierGroup> GetDuplicates(bool excludeClosed)
    {
      if (string.IsNullOrEmpty(_obj.Code) && string.IsNullOrEmpty(_obj.ExternalGuid))
        return Enumerable.Empty<IPowerOfAttorneyClassifierGroup>().ToList();
      
      var duplicates = PowerOfAttorneyClassifierGroups
        .GetAll()
        .Where(d => d.Id != _obj.Id &&
               (!string.IsNullOrEmpty(_obj.Code) && Equals(d.Code, _obj.Code) ||
                !string.IsNullOrEmpty(_obj.ExternalGuid) && Equals(d.ExternalGuid, _obj.ExternalGuid)));
      
      if (excludeClosed)
        duplicates = duplicates.Where(x => x.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed);
      
      return duplicates.ToList();
    }
  }
}