using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace Sungero.PowerOfAttorneyCore.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.FirstInitializing, Constants.Module.Init.PowerOfAttorneyCore.Name, Version.Parse(Constants.Module.Init.PowerOfAttorneyCore.FirstInitVersion));
      
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.Initializing49, Constants.Module.Init.PowerOfAttorneyCore.Name, Version.Parse(Constants.Module.Init.PowerOfAttorneyCore.Version49));
    }
    
    /// <summary>
    /// Начальная инициализация модуля после установки.
    /// </summary>
    public virtual void FirstInitializing()
    {
      InitializationLogger.Debug("Init: Grant rights on databooks to all users.");
      PowerOfAttorneyCore.PowerOfAttorneyServiceConnections.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Read);
      PowerOfAttorneyCore.PowerOfAttorneyServiceConnections.AccessRights.Save();
    }
    
    public virtual void Initializing49()
    {
      PowerOfAttorneyCore.PowerOfAttorneyClassifiers.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Read);
      PowerOfAttorneyCore.PowerOfAttorneyClassifiers.AccessRights.Save();
      PowerOfAttorneyCore.PowerOfAttorneyClassifierGroups.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Read);
      PowerOfAttorneyCore.PowerOfAttorneyClassifierGroups.AccessRights.Save();
    }
  }
}
