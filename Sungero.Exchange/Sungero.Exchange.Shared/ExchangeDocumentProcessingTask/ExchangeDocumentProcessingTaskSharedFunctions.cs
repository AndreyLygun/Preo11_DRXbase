using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ExchangeDocumentProcessingTask;

namespace Sungero.Exchange.Shared
{
  partial class ExchangeDocumentProcessingTaskFunctions
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