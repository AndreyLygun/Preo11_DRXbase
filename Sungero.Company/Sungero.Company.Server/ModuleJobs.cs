using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// Копирование прав руководителю.
    /// </summary>
    public virtual void TransferSubstitutedAccessRights()
    {
      var loggerPrefix = "TransferSubstitutedAccessRightsJob. ";
      Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix)
        .Debug(loggerPrefix + "Start job.");
      var managers = Substitutions.GetAll()
        .Where(s => s.EndDate != null &&
               s.Status == CoreEntities.DatabookEntry.Status.Active &&
               s.IsSystem == true &&
               Employees.Is(s.Substitute))
        .Select(s => s.Substitute).Distinct();
      
      if (!managers.Any())
        Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix)
          .Debug(loggerPrefix + "No suitable managers.");
      
      foreach (var manager in managers)
      {
        var managerId = Employees.As(manager).Id;
        var result = Functions.Module.TransferManagerAccessRights(managerId, true, loggerPrefix);
        if (!string.IsNullOrEmpty(result))
          Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix)
            .Debug(loggerPrefix + result);
      }
      Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix)
        .Debug(loggerPrefix + "Done.");
    }

    /// <summary>
    /// Удаление устаревших системных замещений.
    /// </summary>
    public virtual void DeleteObsoleteSystemSubstitutions()
    {
      Logger.Debug("DeleteObsoleteSystemSubstitutions. Start.");
      PublicFunctions.Module.DeleteObsoleteSystemSubstitutions();
      Logger.Debug("DeleteObsoleteSystemSubstitutions. Done.");
    }

  }
}