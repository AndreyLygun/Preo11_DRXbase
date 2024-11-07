using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace Sungero.Commons.Server
{
  public partial class ModuleInitializer
  {
    #region Метки инициализации
    
    /// <summary>
    /// Проверить наличие метки инициализации в таблице Sungero_Docflow_Params.
    /// </summary>
    /// <param name="name">Наименование метки.</param>
    /// <returns>True - отметка есть, False - иначе.</returns>
    [Public]
    public static bool HasInitializationMarkInParams(string name)
    {
      if (!Docflow.PublicFunctions.Module.DocflowParamsTableExist())
        return false;
      return Docflow.PublicFunctions.Module.HasDocflowParamsKey(name);
    }
    
    /// <summary>
    /// Установить метку инициализации в таблице Sungero_Docflow_Params.
    /// </summary>
    /// <param name="name">Наименование метки.</param>
    /// <param name="version">Версия.</param>
    [Public]
    public static void SetInitializationMarkInParams(string name, System.Version version)
    {
      if (!Docflow.PublicFunctions.Module.DocflowParamsTableExist())
        Docflow.PublicInitializationFunctions.Module.CreateParametersTable();
      
      var value = string.Empty;
      if (version != null)
        value = version.ToString();
      InitializationLogger.DebugFormat("Init: Set initialization mark. Name: {0} Version: {1}", name, version);
      Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(name, value);
    }
    
    /// <summary>
    /// Получить версию метки в таблице Sungero_Docflow_Params.
    /// </summary>
    /// <param name="name">Наименование метки.</param>
    /// <returns>Версия метки. Null, если метка отсутствует, не имеет версии или содержит значение отличное от строкового представления класса System.Version.</returns>
    [Public]
    public static System.Version GetInitializationMarkVersionInParams(string name)
    {
      if (!HasInitializationMarkInParams(name))
        return null;
      var value = Docflow.PublicFunctions.Module.Remote.GetDocflowParamsStringValue(name);
      System.Version version;
      if (!System.Version.TryParse(value, out version))
        return null;
      return version;
    }
    
    #endregion

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.FirstInitializing, Constants.Module.Init.Commons.Name, Version.Parse(Constants.Module.Init.Commons.FirstInitVersion));
      
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.Initializing49, Constants.Module.Init.Commons.Name, Version.Parse(Constants.Module.Init.Commons.Version49));
    }
    
    /// <summary>
    /// Обертка для выполнения инициализации определенной версии модуля.
    /// </summary>
    /// <param name="initAction"> Метод, выполняющий инициализацию указанной версии модуля.</param>
    /// <param name="moduleInitMark">Название параметра, в котором хранится версия для которой была выполнена последняя инициализация.</param>
    /// <param name="initCodeVersion">Версия модуля, для которой предназначена указанная инициализация.</param>
    [Public]
    public static void ModuleVersionInit(System.Action initAction, string moduleInitMark, System.Version initCodeVersion)
    {
      var installedVersion = Sungero.Commons.PublicInitializationFunctions.Module.GetInitializationMarkVersionInParams(moduleInitMark);
      if (installedVersion != null && installedVersion > initCodeVersion)
      {
        InitializationLogger.DebugFormat("Init: Skip initialization. Name: {0} Version: {1}", moduleInitMark, initCodeVersion);
        return;
      }
      
      initAction();
      
      Sungero.Commons.PublicInitializationFunctions.Module.SetInitializationMarkInParams(moduleInitMark, initCodeVersion);
    }
    
    /// <summary>
    /// Начальная инициализация модуля после установки.
    /// </summary>
    public virtual void FirstInitializing()
    {
      GrantRigthsToExternalEntityLinks();
      CreateExternalEntityLinksIndexes();
      CreateCountryRegionsCitiesFromFIAS();
    }
    
    public virtual void Initializing49()
    {
      // Код инициализации для версии 4.9.0
    }
    
    /// <summary>
    /// Выдать права на чтение справочника ExternalEntityLink всем пользователям.
    /// </summary>
    private static void GrantRigthsToExternalEntityLinks()
    {
      // Получить роль "Все пользователи", не создавая зависимость от модуля Docflow.
      var allUsers = Roles.AllUsers;
      
      ExternalEntityLinks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ExternalEntityLinks.AccessRights.Save();
    }
    
    /// <summary>
    /// Создать прикладные индексы для справочника ExternalEntityLink.
    /// </summary>
    private static void CreateExternalEntityLinksIndexes()
    {
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.Module.idx_EEntLink_EId_EType_EEType_ESId_SD);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.Module.idx_EEntLink_EEId_ESId);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.Module.idx_EELinks_Discr_EId_EEType_ESysId_SyncDate);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.Module.idx_EEntityLinks_Disc_ExtEntityId_ExtSystemId);
    }
    
    /// <summary>
    /// Создать страну, регионы и города согласно ФИАС.
    /// </summary>
    public static void CreateCountryRegionsCitiesFromFIAS()
    {
      if (Functions.Module.IsServerCultureRussian() && !Countries.GetAll().Any())
      {
        InitializationLogger.DebugFormat("Init: Create country, regions and cities.");
        Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.Module.CreateCountryRegionsCitiesFromFIAS);
      }
    }
    
  }
}
