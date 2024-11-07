using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.HistoricalMessagesDownloadSession;

namespace Sungero.ExchangeCore
{
  partial class HistoricalMessagesDownloadSessionServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      _obj.Name = Sungero.ExchangeCore.HistoricalMessagesDownloadSessions.Resources.HistoricalMessageDownloadSessionNameFormat(_obj.Id, _obj.PeriodBegin.Value.ToString("dd.MM.yyyy"), _obj.PeriodEnd.Value.ToString("dd.MM.yyyy"));
    }
  }

}