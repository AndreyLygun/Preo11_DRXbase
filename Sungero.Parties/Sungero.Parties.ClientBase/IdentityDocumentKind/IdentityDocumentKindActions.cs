using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.IdentityDocumentKind;

namespace Sungero.Parties.Client
{
  partial class IdentityDocumentKindActions
  {
    public override void DeleteEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.DeleteEntity(e);
    }

    public override bool CanDeleteEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanDeleteEntity(e) && string.IsNullOrWhiteSpace(_obj.SID);
    }
  }
}