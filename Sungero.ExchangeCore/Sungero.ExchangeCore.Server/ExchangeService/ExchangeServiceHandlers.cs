using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.ExchangeService;

namespace Sungero.ExchangeCore
{
  partial class ExchangeServiceServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var exchangeUrlValidationMessage = Functions.ExchangeService.ValidateExchangeAddress(_obj);
      if (!string.IsNullOrEmpty(exchangeUrlValidationMessage))
        e.AddError(_obj.Info.Properties.Uri, exchangeUrlValidationMessage);
    }
  }
}