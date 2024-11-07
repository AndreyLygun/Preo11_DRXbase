using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.CheckReturnTask;
using Sungero.Workflow;

namespace Sungero.Docflow.Server.CheckReturnTaskBlocks
{
  partial class CheckDocumentReturnBlockHandlers
  {

    public virtual void CheckDocumentReturnBlockStartAssignment(Sungero.Docflow.ICheckReturnCheckAssignment assignment)
    {
      _obj.MaxDeadline = assignment.Deadline;
    }

    public virtual void CheckDocumentReturnBlockCompleteAssignment(Sungero.Docflow.ICheckReturnCheckAssignment assignment)
    {
      var documentIsReturned = assignment.Result == Docflow.CheckReturnCheckAssignment.Result.Returned;
      Functions.CheckReturnTask.SetReturnResult(_obj, assignment.Performer, documentIsReturned);
    }
  }

  partial class ReturnDocumentBlockHandlers
  {

    public virtual void ReturnDocumentBlockStartAssignment(Sungero.Docflow.ICheckReturnAssignment assignment)
    {
      _obj.MaxDeadline = assignment.Deadline;
    }

    public virtual void ReturnDocumentBlockCompleteAssignment(Sungero.Docflow.ICheckReturnAssignment assignment)
    {
      Functions.CheckReturnTask.SetReturnResult(_obj, assignment.Performer, true);
    }
  }

}