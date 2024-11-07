using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyCore.PowerOfAttorneyClassifier;

namespace Sungero.PowerOfAttorneyCore.Server
{
  partial class PowerOfAttorneyClassifierFunctions
  {
    /// <summary>
    /// Получить все дубли текущей записи полномочия.
    /// </summary>
    /// <param name="excludeClosed">Признак поиска дублей без учёта закрытых записей.</param>
    /// <returns>Список дублей.</returns>
    /// <remarks>Дублями считаются записи с совпадающими значениям свойств "Код", "Мнемоника" или "Внешний ИД".</remarks>
    [Remote(IsPure = true)]
    public List<IPowerOfAttorneyClassifier> GetDuplicates(bool excludeClosed)
    {
      if (string.IsNullOrEmpty(_obj.Code) && string.IsNullOrEmpty(_obj.Mnemonic) && string.IsNullOrEmpty(_obj.Autokey))
        return Enumerable.Empty<IPowerOfAttorneyClassifier>().ToList();
      
      var duplicates = PowerOfAttorneyClassifiers
        .GetAll()
        .Where(d => d.Id != _obj.Id &&
               (!string.IsNullOrEmpty(_obj.Code) && Equals(d.Code, _obj.Code) ||
                !string.IsNullOrEmpty(_obj.Mnemonic) && Equals(d.Mnemonic, _obj.Mnemonic) ||
                !string.IsNullOrEmpty(_obj.Autokey) && Equals(d.Autokey, _obj.Autokey)));
      
      if (excludeClosed)
        duplicates = duplicates.Where(x => x.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed);
      
      return duplicates.ToList();
    }
  }
}