using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace Sungero.Intelligence.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.FirstInitializing, Constants.Module.Init.Intelligence.Name, Version.Parse(Constants.Module.Init.Intelligence.FirstInitVersion));
      
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.Initializing49, Constants.Module.Init.Intelligence.Name, Version.Parse(Constants.Module.Init.Intelligence.Version49));
    }
    
    /// <summary>
    /// Начальная инициализация модуля после установки.
    /// </summary>
    public virtual void FirstInitializing()
    {
      var allUsers = Roles.AllUsers;
      if (allUsers != null)
      {
        // Справочники.
        GrantRightsOnDatabooks(allUsers);
      }
    }
    
    public virtual void Initializing49()
    {
      // Код инициализации для версии 4.9.0
    }
    
    /// <summary>
    /// Выдать права всем пользователям на справочники.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightsOnDatabooks(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on databooks to all users.");
      
      // Модуль Интеллектуальные функции.
      Intelligence.AIManagersAssistants.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Intelligence.AIManagersAssistants.AccessRights.Save();

    }
  }
}
