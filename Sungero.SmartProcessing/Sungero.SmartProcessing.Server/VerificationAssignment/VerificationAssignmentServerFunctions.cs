using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Shared;
using Sungero.SmartProcessing.VerificationAssignment;

namespace Sungero.SmartProcessing.Server
{
  partial class VerificationAssignmentFunctions
  {
    /// <summary>
    /// Переадресовать задание.
    /// </summary>
    /// <param name="task">Задача.</param>
    [Public]
    public virtual void ForwardAssigment(IVerificationTask task)
    {
      if (_obj.Result == SmartProcessing.VerificationAssignment.Result.Forward)
      {
        _obj.Forward(_obj.Addressee, ForwardingLocation.Next, _obj.NewDeadline);

        // Проброс значений в задачу, если схема настраивается не в проводнике.
        if (Docflow.PublicFunctions.Module.IsTaskUsingOldScheme(task))
        {
          task.Deadline = _obj.NewDeadline;
          task.Addressee = _obj.Addressee;
        }
        Logger.WithLogger(Constants.VerificationTask.VerificationTaskLoggerPostfix)
          .Debug(string.Format("Task {0} forwarded to {1}. New deadline {2}.",
                               task.Id, _obj.Addressee.Id, _obj.NewDeadline));
      }
    }
  }
}
