using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace Sungero.Integration1C.Server
{
  public partial class ModuleInitializer
  {
    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.FirstInitializing, Constants.Module.Init.Integration1C.Name, Version.Parse(Constants.Module.Init.Integration1C.FirstInitVersion));
      
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.Initializing49, Constants.Module.Init.Integration1C.Name, Version.Parse(Constants.Module.Init.Integration1C.Version49));
    }
    
    /// <summary>
    /// Начальная инициализация модуля после установки.
    /// </summary>
    public virtual void FirstInitializing()
    {
      // Создание ролей.
      InitializationLogger.Debug("Init: Create roles.");
      CreateRoles();
    }
    
    public virtual void Initializing49()
    {
      // Код инициализации для версии 4.9.0
    }
    
    /// <summary>
    /// Создать предопределенные роли.
    /// </summary>
    public static void CreateRoles()
    {
      InitializationLogger.Debug("Init: Create Default Roles");      
      Docflow.PublicInitializationFunctions.Module.CreateRole(Resources.Synchronization1CResponsibleRoleName, Resources.Synchronization1CResponsibleRoleDescription, Integration1C.Constants.Module.SynchronizationResponsibleRoleGuid);
    }
  }
}
