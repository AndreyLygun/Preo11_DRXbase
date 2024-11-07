using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.StatusReportRequestTask;

namespace Sungero.RecordManagement
{
  partial class StatusReportRequestTaskSharedHandlers
  {
    
    public virtual void AssigneeChanged(Sungero.RecordManagement.Shared.StatusReportRequestTaskAssigneeChangedEventArgs e)
    {
      _obj.Subject = Functions.StatusReportRequestTask.GetStatusReportRequestSubject(_obj, StatusReportRequestTasks.Resources.ReportRequestTaskSubject, e.NewValue);
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (e.NewValue.Length > StatusReportRequestTasks.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, StatusReportRequestTasks.Info.Properties.Subject.Length);
    }
  }
}