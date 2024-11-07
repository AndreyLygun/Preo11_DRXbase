using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.VerificationTask;

namespace Sungero.SmartProcessing
{
  partial class VerificationTaskServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.ThreadSubject = Sungero.SmartProcessing.Resources.VerificationTaskThreadSubject;
    }
  }

}