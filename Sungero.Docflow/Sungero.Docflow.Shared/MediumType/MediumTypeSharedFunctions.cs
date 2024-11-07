using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.MediumType;

namespace Sungero.Docflow.Shared
{
  partial class MediumTypeFunctions
  {
    /// <summary>
    /// Определить, является ли тип носителя документа созданным при инициализации.
    /// </summary>
    /// <returns>True, если тип носителя создан при инициализации, иначе - false.</returns>
    public virtual bool IsNativeMediumType()
    {
      return !string.IsNullOrWhiteSpace(_obj.Sid);
    }
  }
}