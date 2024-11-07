using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.StatusReportRequestTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server.StatusReportRequestTaskBlocks
{
  partial class AcceptReportBlockHandlers
  {

    public virtual void AcceptReportBlockStartAssignment(Sungero.RecordManagement.IReportRequestCheckAssignment assignment)
    {
      _obj.MaxDeadline = assignment.Deadline;
    }

    public virtual void AcceptReportBlockCompleteAssignment(Sungero.RecordManagement.IReportRequestCheckAssignment assignment)
    {
      // Вернуть комментарий к отчету.
      _obj.ReportNote = assignment.ActiveText;
    }

  }

  partial class RequestReportBlockHandlers
  {
    
    public virtual void RequestReportBlockStartAssignment(Sungero.RecordManagement.IReportRequestAssignment assignment)
    {
      _obj.MaxDeadline = assignment.Deadline;
      if (!string.IsNullOrEmpty(_obj.ReportNote))
        assignment.IsRework = true;
      
      assignment.ActiveText = _obj.Report;
    }

    public virtual void RequestReportBlockCompleteAssignment(Sungero.RecordManagement.IReportRequestAssignment assignment)
    {
      _obj.Report = assignment.ActiveText;
    }
  }

}