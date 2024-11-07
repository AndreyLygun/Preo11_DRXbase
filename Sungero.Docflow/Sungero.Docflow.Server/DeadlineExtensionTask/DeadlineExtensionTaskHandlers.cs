using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineExtensionTask;

namespace Sungero.Docflow
{
  partial class DeadlineExtensionTaskAssigneePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> AssigneeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      var allUsers = Functions.DeadlineExtensionTask.GetAssigneesForDeadlineExtensionFromAssignment(_obj);    
      return query.Where(x => allUsers.Contains(x));
    }
  }

  partial class DeadlineExtensionTaskServerHandlers
  {

    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      Sungero.Docflow.Functions.DeadlineExtensionTask.ValidateDeadlineExtensionTaskStart(_obj, e);
    }
  }

}