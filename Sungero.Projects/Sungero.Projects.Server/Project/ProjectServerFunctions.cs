using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Projects.Project;

namespace Sungero.Projects.Server
{
  partial class ProjectFunctions
  {
    /// <summary>
    /// Создать проект.
    /// </summary>
    /// <returns>Проект.</returns>
    [Remote]
    public static IProject CreateProject()
    {
      return Projects.Create();
    }
  }
}