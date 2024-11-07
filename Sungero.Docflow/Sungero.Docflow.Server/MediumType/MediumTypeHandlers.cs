using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.MediumType;

namespace Sungero.Docflow
{
  partial class MediumTypeCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.Sid);
    }
  }

  partial class MediumTypeServerHandlers
  {
    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      // Нельзя удалить форму документа, созданную в инициализации.
      if (Functions.MediumType.IsNativeMediumType(_obj))
        e.AddError(MediumTypes.Resources.CantDeleteMediumType);
    }
  }

}