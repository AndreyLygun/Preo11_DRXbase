using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ReceiptNotificationSendingTask;

namespace Sungero.Exchange.Shared
{
  partial class ReceiptNotificationSendingTaskFunctions
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