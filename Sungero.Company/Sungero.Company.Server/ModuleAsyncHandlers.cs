using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Server
{
  public class ModuleAsyncHandlers
  {

    /// <summary>
    /// Отслеживание завершения сессии копирования прав.
    /// </summary>
    /// <param name="args">Параметры вызова асинхронного обработчика.</param>
    public virtual void CheckTransferSubstitutedAccessRights(Sungero.Company.Server.AsyncHandlerInvokeArgs.CheckTransferSubstitutedAccessRightsInvokeArgs args)
    {
      var logMessageFirstPart = string.Format("CheckTransferSubstitutedAccessRights. SessionId: {0}. Iteration: {1}. ",
                                              args.SessionId, args.RetryIteration);
      try
      {
        Guid parsedSessionId;
        if (Guid.TryParse(args.SessionId, out parsedSessionId) == false)
          throw new Exception("Session ID must be GUID type.");
        
        var sessionStatus = Functions.Module.GetAccessRightsSessionStatus(parsedSessionId, logMessageFirstPart);
        var sessionStatusString = sessionStatus.HasValue ? sessionStatus.Value.JobStatus.ToString() : "Null";
        logMessageFirstPart += string.Format("Session status: {0}. ", sessionStatusString);
        
        if (!Functions.Module.SessionExists(args.SessionId, logMessageFirstPart))
          return;
        
        var session = AccessRightsTransferSessions.GetAll().Where(a => a.SessionId == args.SessionId).First();
        
        if (!sessionStatus.HasValue ||
            sessionStatus.Value.JobStatus == BackgroundJobExecutionStatus.New ||
            sessionStatus.Value.JobStatus == BackgroundJobExecutionStatus.Initialization ||
            sessionStatus.Value.JobStatus == BackgroundJobExecutionStatus.Processing)
        {
          Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix)
            .Debug(logMessageFirstPart + "Retry.");
          args.Retry = true;
          return;
        }
        else if (sessionStatus.Value.JobStatus == BackgroundJobExecutionStatus.Error ||
                 sessionStatus.Value.JobStatus == BackgroundJobExecutionStatus.Canceled)
        {
          Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix)
            .Debug(logMessageFirstPart + "Stopped.");
          this.ProcessAccessRightsSessionResult(session, logMessageFirstPart, false);
          return;
        }
        else if (sessionStatus.Value.JobStatus == BackgroundJobExecutionStatus.Done)
        {
          if (!Functions.Module.TryWriteEmployeeTransferAccessRightsHistory(session, logMessageFirstPart))
          {
            Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix)
              .Debug(logMessageFirstPart + "Retry.");
            args.Retry = true;
            return;
          }
          
          Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix)
            .Debug(logMessageFirstPart + "Finished.");
          this.ProcessAccessRightsSessionResult(session, logMessageFirstPart, true);
        }
      }
      catch (Exception ex)
      {
        var loggerMessage = logMessageFirstPart + string.Format("Error message: {0}.", ex.Message);
        Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Debug(loggerMessage);
        args.Retry = true;
        return;
      }
    }
    
    /// <summary>
    /// Обработать результат выполнения передачи прав.
    /// </summary>
    /// <param name="session">Сессия.</param>
    /// <param name="logMessageFirstPart">Первая часть сообщения логирования.</param>
    /// <param name="isSuccessfull">Флаг успешного завершения.</param>
    public virtual void ProcessAccessRightsSessionResult(IAccessRightsTransferSession session, string logMessageFirstPart,
                                                         bool isSuccessfull)
    {
      if (isSuccessfull)
      {
        var systemSubstitution = Substitutions.GetAll()
          .Where(s => s.Substitute.Id == session.SubstituteId &&
                 s.User.Id == session.EmployeeId &&
                 s.IsSystem == true &&
                 s.Status == Sungero.CoreEntities.DatabookEntry.Status.Active).FirstOrDefault();

        if (systemSubstitution != null)
        {
          systemSubstitution.Status = Sungero.CoreEntities.DatabookEntry.Status.Closed;
          systemSubstitution.Save();
          
          var loggerMessage = string.Format(logMessageFirstPart + "Closed system substitution Id: {0}.",
                                            systemSubstitution.Id.ToString());
          Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Debug(loggerMessage);
        }
        else
        {
          Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix)
            .Debug(logMessageFirstPart + "Not found system substitution.");
        }
      }
      AccessRightsTransferSessions.Delete(session);
      Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix)
        .Debug(logMessageFirstPart + "Transfer session removed.");
    }

    /// <summary>
    /// Обновить имя сотрудника из персоны.
    /// </summary>
    /// <param name="args">Параметры вызова асинхронного обработчика.</param>
    public virtual void UpdateEmployeeName(Sungero.Company.Server.AsyncHandlerInvokeArgs.UpdateEmployeeNameInvokeArgs args)
    {
      long personId = args.PersonId;
      Logger.DebugFormat("UpdateEmployeeName: start update employee name. Person id: {0}.", personId);
      var employees = Company.Employees.GetAll(x => x.Person.Id == personId);
      
      if (!employees.Any())
      {
        Logger.DebugFormat("UpdateEmployeeName: employee not found. Person id: {0}.", personId);
        return;
      }
      
      foreach (var employee in employees)
      {
        try
        {
          Company.Functions.Employee.UpdateName(employee, employee.Person);
          employee.Save();
        }
        catch
        {
          Logger.DebugFormat("UpdateEmployeeName: could not update name. Employee id: {0}.", employee.Id);
          args.Retry = true;
          continue;
        }
        Logger.DebugFormat("UpdateEmployeeName: name updated successfully. Employee id: {0}. Person id: {1}.", employee.Id, personId);
      }
      
    }
    
  }
}