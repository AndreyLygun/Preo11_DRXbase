using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.MarkKind;

namespace Sungero.Docflow
{
  partial class MarkKindCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.Sid);
    }
  }

  partial class MarkKindServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.OnBlankPage = false;
    }

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      if (!string.IsNullOrEmpty(_obj.Sid))
        e.AddError(Sungero.Docflow.MarkKinds.Resources.CanNotDeleteMarkKind);
    }
  }
}