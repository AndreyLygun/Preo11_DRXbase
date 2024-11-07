using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.IncomingInvitationTask;

namespace Sungero.ExchangeCore.Shared
{
  partial class IncomingInvitationTaskFunctions
  {
    /// <summary>
    /// Заполнить вариант процесса для новой схемы.
    /// </summary>
    [Public]
    public virtual void FillProcessKind()
    {
      // Виртуальная функция, которая позволяет переопределить вариант процесса для no-code.
    }
  }
}