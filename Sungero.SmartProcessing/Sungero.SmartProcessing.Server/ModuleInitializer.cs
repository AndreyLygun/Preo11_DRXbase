using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace Sungero.SmartProcessing.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.FirstInitializing, Constants.Module.Init.SmartProcessing.Name, Version.Parse(Constants.Module.Init.SmartProcessing.FirstInitVersion));
      
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.Initializing49, Constants.Module.Init.SmartProcessing.Name, Version.Parse(Constants.Module.Init.SmartProcessing.Version49));
    }
    
    /// <summary>
    /// Начальная инициализация модуля после установки.
    /// </summary>
    public virtual void FirstInitializing()
    {
      // Выдача прав всем пользователям.
      var allUsers = Roles.AllUsers;
      if (allUsers != null)
      {
        // Справочники.
        InitializationLogger.Debug("Init: Grant rights on databooks to all users.");
        GrantRightsOnDatabooks(allUsers);
      }
      
      AddLowerFMeasureLimitParam();
      AddFirstPageClassifierLowerFMeasureLimitParam();
      AddCsvTrainingTokensPerPageLimitParam();
      AddTextExtractionTasksLimitParam();
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
      SmartProcessing.RepackingSessions.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Change);
      SmartProcessing.RepackingSessions.AccessRights.Save();
    }
    
    /// <summary>
    /// Добавить в таблицу параметров минимальное значение F1-меры для публикации модели.
    /// </summary>
    public static void AddLowerFMeasureLimitParam()
    {
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.LowerFMeasureLimitParamName) == null)
        Docflow.PublicFunctions.Module.InsertDocflowParam(Constants.Module.LowerFMeasureLimitParamName,
                                                          Constants.Module.DefaultLowerFMeasureLimit.ToString());
    }
    
    /// <summary>
    /// Добавить в таблицу параметров минимальное значение F1-меры для публикации модели классификатора первых страниц.
    /// </summary>
    public static void AddFirstPageClassifierLowerFMeasureLimitParam()
    {
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.FirstPageClassifierLowerFMeasureLimitParamName) == null)
        Docflow.PublicFunctions.Module.InsertDocflowParam(Constants.Module.FirstPageClassifierLowerFMeasureLimitParamName,
                                                          Constants.Module.DefaultFirstPageClassifierLowerFMeasureLimit.ToString());
    }
    
    /// <summary>
    /// Добавить в таблицу параметров максимальное количество токенов на страницу для дообучения классификатора первых страниц.
    /// </summary>
    public static void AddCsvTrainingTokensPerPageLimitParam()
    {
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.CsvTrainingTokensPerPageLimitParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.CsvTrainingTokensPerPageLimitParamName,
                                                                  Constants.Module.DefaultCsvTrainingTokensPerPageLimit.ToString());
    }

    /// <summary>
    /// Добавить в таблицу параметров значение по умолчанию для максимального числа асинхронных задач на извлечение текста в Ario.
    /// </summary>
    public static void AddTextExtractionTasksLimitParam()
    {
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.TextExtractionTasksLimitParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.TextExtractionTasksLimitParamName,
                                                                  Constants.Module.DefaultTextExtractionTasksLimit.ToString());
    }

  }

}
