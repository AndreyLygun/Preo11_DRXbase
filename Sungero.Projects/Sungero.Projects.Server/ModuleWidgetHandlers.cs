using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Projects.Server
{
  partial class ProjectStagesWidgetHandlers
  {

    public virtual IQueryable<Sungero.Projects.IProjectCore> ProjectStagesOverdueFiltering(System.Linq.IQueryable<Sungero.Projects.IProjectCore> query)
    {
      return Functions.Module.GetProjectsToWidgets(_parameters.Performer, true, null);
    }

    public virtual IQueryable<Sungero.Projects.IProjectCore> ProjectStagesClosingFiltering(System.Linq.IQueryable<Sungero.Projects.IProjectCore> query)
    {
      return Functions.Module.GetProjectsToWidgets(_parameters.Performer, false, Sungero.Projects.ProjectCore.Stage.Completion);
    }

    public virtual IQueryable<Sungero.Projects.IProjectCore> ProjectStagesInWorkFiltering(System.Linq.IQueryable<Sungero.Projects.IProjectCore> query)
    {
      return Functions.Module.GetProjectsToWidgets(_parameters.Performer, false, Sungero.Projects.ProjectCore.Stage.Execution);
    }

    public virtual IQueryable<Sungero.Projects.IProjectCore> ProjectStagesInitiationFiltering(System.Linq.IQueryable<Sungero.Projects.IProjectCore> query)
    {
      return Functions.Module.GetProjectsToWidgets(_parameters.Performer, false, Sungero.Projects.ProjectCore.Stage.Initiation);
    }

    public virtual IQueryable<Sungero.Projects.IProjectCore> ProjectStagesAllProjectsFiltering(System.Linq.IQueryable<Sungero.Projects.IProjectCore> query)
    {
      return Functions.Module.GetProjectsToWidgets(_parameters.Performer, false, null);
    }
  }
}