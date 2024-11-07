using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Mark;

namespace Sungero.Docflow
{
  partial class MarkServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      Functions.Mark.FillName(_obj);
    }
  }

}