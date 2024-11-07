using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace Sungero.MobileApps.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.FirstInitializing, Constants.Module.Init.MobileApps.Name, Version.Parse(Constants.Module.Init.MobileApps.FirstInitVersion));
      
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.Initializing49, Constants.Module.Init.MobileApps.Name, Version.Parse(Constants.Module.Init.MobileApps.Version49));
    }
    
    /// <summary>
    /// Начальная инициализация модуля после установки.
    /// </summary>
    public virtual void FirstInitializing()
    {
      CreateMobileDevicesIndex();
    }
    
    public virtual void Initializing49()
    {
      // Код инициализации для версии 4.9.0
    }

    #region Создание прикладных индексов в бд

    public static void CreateMobileDevicesIndex()
    {
      var tableName = Constants.Module.SungeroMobAppsMobileDeviceTableName;
      var indexName = Constants.Module.SungeroMobileDeviceIndex0;
      var indexQuery = string.Format(Queries.Module.SungeroMobileDeviceIndex0Query, tableName, indexName);

      Docflow.PublicFunctions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
    }

    #endregion
  }

}
