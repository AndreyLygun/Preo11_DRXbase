using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.MediumType;

namespace Sungero.Docflow
{
  partial class MediumTypeClientHandlers
  {
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      // Нельзя изменить имя формы документа, созданной в инициализации.
      if (Functions.MediumType.IsNativeMediumType(_obj))
        _obj.State.Properties.Name.IsEnabled = false;
    }
  }
}