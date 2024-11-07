using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.ExchangeService;

namespace Sungero.ExchangeCore.Shared
{
  partial class ExchangeServiceFunctions
  {
    /// <summary>
    /// Проверить адрес сервиса.
    /// </summary>
    /// <returns>Текст ошибки, если она была обнаружена.</returns>
    public virtual string ValidateExchangeAddress()
    {
      // Проверка, что адрес соответсвует формату URL.
      if (!System.Uri.IsWellFormedUriString(_obj.Uri, UriKind.Absolute))
        return Sungero.ExchangeCore.ExchangeServices.Resources.InvalidExcnageServiceUrl;
      
      return string.Empty;
    }
  }
}