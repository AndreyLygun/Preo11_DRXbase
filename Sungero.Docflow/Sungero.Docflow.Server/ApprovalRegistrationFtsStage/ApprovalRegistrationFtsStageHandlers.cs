using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRegistrationFtsStage;

namespace Sungero.Docflow
{
  partial class ApprovalRegistrationFtsStageServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (_obj.State.IsCopied)
        return;
      
      _obj.TimeoutAction = Docflow.ApprovalFunctionStageBase.TimeoutAction.Repeat;
    }
  }

}