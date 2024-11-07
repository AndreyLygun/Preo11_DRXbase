using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain;
using Sungero.Domain.Shared;

namespace Sungero.Company.Server
{
  public class ModuleFunctions
  {
    #region Замещения

    /// <summary>
    /// Создать системные замещения.
    /// </summary>
    /// <param name="substitutedUsers">Список пользователей, для которых надо создать замещение.</param>
    /// <param name="substitute">Замещающий пользователь.</param>
    public virtual void CreateSystemSubstitutions(System.Collections.Generic.IEnumerable<IUser> substitutedUsers,
                                                  IUser substitute)
    {
      foreach (var user in substitutedUsers)
        this.CreateSystemSubstitution(user, substitute);
    }
    
    /// <summary>
    /// Создать системное замещение.
    /// </summary>
    /// <param name="substitutedUser">Замещаемый пользователь.</param>
    /// <param name="substitute">Замещающий пользователь.</param>
    public virtual void CreateSystemSubstitution(IUser substitutedUser, IUser substitute)
    {
      if (Equals(substitutedUser, substitute))
        return;
      
      var similarSubstitutions = Substitutions.GetAll()
        .Where(s => Equals(s.Substitute, substitute) && Equals(s.User, substitutedUser) && s.IsSystem == true);
      if (similarSubstitutions.Any())
        return;
      
      var substitution = Substitutions.Create();
      substitution.User = substitutedUser;
      substitution.Substitute = substitute;
      substitution.StartDate = Calendar.Now;
      if (substitutedUser.Status == CoreEntities.DatabookEntry.Status.Closed)
        substitution.EndDate = GetClosedUserSubstitutionEndDate(substitutedUser);
      substitution.IsSystem = true;
    }
    
    /// <summary>
    /// Получить дату закрытия системного замещения для закрытого пользователя.
    /// </summary>
    /// <param name="substitutedUser">Замещаемый пользователь.</param>
    /// <returns>Дата закрытия системного замещения.</returns>
    public static DateTime GetClosedUserSubstitutionEndDate(IUser substitutedUser)
    {
      var bufferDays = Docflow.PublicFunctions.Module.Remote.GetDocflowParamsIntegerValue(Docflow.PublicConstants.Module.SubstitutionAccessRightsGrantBufferDaysCountParamName);
      bufferDays = bufferDays > 0 ? bufferDays : Docflow.PublicConstants.Module.SubstitutionAccessRightsGrantBufferDaysCount;
      var waitingDays = Docflow.PublicFunctions.Module.Remote.GetDocflowParamsIntegerValue(Docflow.PublicConstants.Module.SubstitutionAccessRightsWaitingDaysCountParamName);
      waitingDays = waitingDays > 0 ? waitingDays : Docflow.PublicConstants.Module.SubstitutionAccessRightsWaitingDaysCount;
      var substituteEndDaysCount = bufferDays + waitingDays;
      
      var substitution = Substitutions.GetAll()
        .Where(s => Equals(s.User, substitutedUser) && s.EndDate != null && s.EndDate >= Calendar.Now.AddDays(bufferDays)
               && s.IsSystem == true)
        .OrderByDescending(o => o.EndDate).FirstOrDefault();
      
      if (substitution != null)
        return substitution.EndDate.Value;
      
      return Calendar.Now.AddDays(substituteEndDaysCount);
    }
    
    /// <summary>
    /// Обновить системные замещения сотрудника.
    /// </summary>
    /// <param name="substitutedUser">Замещаемый сотрудник.</param>
    public virtual void UpdateEmployeeSystemSubstitutions(IEmployee substitutedUser)
    {
      var systemSubstitutions = Substitutions.GetAll()
        .Where(s => Equals(s.User, substitutedUser) && s.IsSystem == true);
      
      if (substitutedUser.Status == Company.Employee.Status.Closed)
      {
        var bufferDays = Docflow.PublicFunctions.Module.Remote.GetDocflowParamsIntegerValue(Docflow.PublicConstants.Module.SubstitutionAccessRightsGrantBufferDaysCountParamName);
        var waitingDays = Docflow.PublicFunctions.Module.Remote.GetDocflowParamsIntegerValue(Docflow.PublicConstants.Module.SubstitutionAccessRightsWaitingDaysCountParamName);
        
        bufferDays = bufferDays > 0 ? bufferDays : Docflow.PublicConstants.Module.SubstitutionAccessRightsGrantBufferDaysCount;
        waitingDays = waitingDays > 0 ? waitingDays : Docflow.PublicConstants.Module.SubstitutionAccessRightsWaitingDaysCount;
        var substituteEndDaysCount = bufferDays + waitingDays;
        
        foreach (var substitution in systemSubstitutions.Where(s => s.EndDate == null && s.Status == Sungero.CoreEntities.DatabookEntry.Status.Active))
        {
          substitution.Status = Sungero.CoreEntities.DatabookEntry.Status.Active;
          substitution.EndDate = Calendar.Now.AddDays(substituteEndDaysCount);
        }
      }
      else
      {
        foreach (var substitution in systemSubstitutions.Where(s => s.EndDate != null))
        {
          substitution.Status = Sungero.CoreEntities.DatabookEntry.Status.Active;
          substitution.EndDate = null;
          substitution.StartDate = Calendar.Now;
        }
      }
    }

    /// <summary>
    /// Удалить системные замещения.
    /// </summary>
    /// <param name="substitutedUsers">Список пользователей, для которых надо удалить замещение.</param>
    /// <param name="substitute">Замещающий пользователь.</param>
    [Obsolete("Метод не используется с 23.11.2023 и версии 4.9. Используйте метод DeleteUnnecessarySystemSubstitutions.")]
    public static void DeleteSystemSubstitutions(System.Collections.Generic.IEnumerable<IUser> substitutedUsers,
                                                 IUser substitute)
    {
      Functions.Module.DeleteUnnecessarySystemSubstitutionsTransaction(substitutedUsers, substitute);
    }
    
    /// <summary>
    /// Удалить системное замещение.
    /// </summary>
    /// <param name="substitutedUser">Пользователь, для которого надо удалить замещение.</param>
    /// <param name="substitute">Замещающий пользователь.</param>
    [Obsolete("Метод не используется с 23.11.2023 и версии 4.9. Используйте метод DeleteUnnecessarySystemSubstitution.")]
    public static void DeleteSystemSubstitution(IUser substitutedUser, IUser substitute)
    {
      Functions.Module.DeleteUnnecessarySystemSubstitutionTransaction(substitutedUser, substitute);
    }
    
    /// <summary>
    /// Удалить неиспользуемые системные замещения.
    /// </summary>
    /// <param name="substitutedUsers">Список пользователей, для которых надо удалить замещение.</param>
    /// <param name="substitute">Замещающий пользователь.</param>
    /// <remarks>
    /// Функцию следует вызывать в контексте событий (в транзакции) Saving, Saved, Deleting сущности.
    /// Сохранение экземпляра SystemSubstitutionQueueItems делать нельзя, оно будет выполнено при коммите транзакции платформой.
    /// </remarks>
    public virtual void DeleteUnnecessarySystemSubstitutionsTransaction(System.Collections.Generic.IEnumerable<IUser> substitutedUsers,
                                                                        IUser substitute)
    {
      var deletedSubstitutions = Substitutions.GetAll()
        .Where(s => Equals(s.Substitute, substitute) && substitutedUsers.Contains(s.User) && s.IsSystem == true)
        .ToList();
      
      foreach (var deletedSubstitution in deletedSubstitutions)
      {
        var queueItem = SystemSubstitutionQueueItems.Create();
        queueItem.SubstitutedUserId = deletedSubstitution.User.Id;
        queueItem.SubstituteId = deletedSubstitution.Substitute.Id;
      }
    }

    /// <summary>
    /// Удалить неиспользуемое системное замещение.
    /// </summary>
    /// <param name="substitutedUser">Пользователь, для которого надо удалить замещение.</param>
    /// <param name="substitute">Замещающий пользователь.</param>
    /// <remarks>
    /// Функцию следует вызывать в контексте событий (в транзакции) Saving, Saved, Deleting сущности.
    /// Сохранение экземпляра SystemSubstitutionQueueItems делать нельзя, оно будет выполнено при коммите транзакции платформой.
    /// </remarks>
    public virtual void DeleteUnnecessarySystemSubstitutionTransaction(IUser substitutedUser, IUser substitute)
    {
      this.DeleteUnnecessarySystemSubstitutionsTransaction(new[] { substitutedUser }, substitute);
    }
    
    /// <summary>
    /// Получить пользователей, которых замещает указанный.
    /// </summary>
    /// <param name="substitute">Заместитель.</param>
    /// <returns>Пользователи, которых замещает указанный.</returns>
    [Public]
    public virtual List<IUser> GetUsersSubstitutedBy(IUser substitute)
    {
      if (substitute == null)
        return new List<IUser>();
      return GetSubstitutionsBySubstitute(substitute)
        .Select(x => x.User)
        .ToList();
    }
    
    /// <summary>
    /// Получить пользователей, которых замещает указанный.
    /// </summary>
    /// <param name="substitute">Заместитель.</param>
    /// <param name="isSystemSubstitution">Признак является ли замещение системным.</param>
    /// <returns>Пользователи, которых замещает указанный.</returns>
    [Public]
    public virtual List<IUser> GetUsersSubstitutedBy(IUser substitute, bool isSystemSubstitution)
    {
      if (substitute == null)
        return new List<IUser>();
      return GetSubstitutionsBySubstitute(substitute)
        .Where(x => x.IsSystem == isSystemSubstitution)
        .Select(x => x.User)
        .ToList();
    }
    
    /// <summary>
    /// Получить список замещений, в которых текущий пользователь указан в качестве замещающего.
    /// </summary>
    /// <param name="substitute">Заместитель.</param>
    /// <returns>Список замещений.</returns>
    private static IQueryable<ISubstitution> GetSubstitutionsBySubstitute(IUser substitute)
    {
      return Substitutions.GetAll()
        .Where(x => Equals(x.Substitute, substitute))
        .Where(x => !x.StartDate.HasValue || x.StartDate <= Calendar.Today)
        .Where(x => !x.EndDate.HasValue || x.EndDate >= Calendar.Today);
    }
    
    /// <summary>
    /// Удалить элемент очереди удаления системных замещений.
    /// </summary>
    /// <param name="queueItem">Элемент очереди.</param>
    public virtual void DeleteSystemSubstitutionQueueItem(ISystemSubstitutionQueueItem queueItem)
    {
      SystemSubstitutionQueueItems.Delete(queueItem);
    }
    
    /// <summary>
    /// Проверить необходимость системного замещения Руководитель отдела -> Cотрудник отдела.
    /// </summary>
    /// <param name="substitutedUserId">ID замещаемого пользователя.</param>
    /// <param name="substituteId">ID замещающего пользователя.</param>
    /// <returns>True, если должно быть системное замещение.</returns>
    public virtual bool IsEmployeeSubordinateToManager(long substitutedUserId, long substituteId)
    {
      return Departments.GetAll().Any(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
                                      d.Manager.Id == substituteId &&
                                      d.RecipientLinks.Any(r => r.Member.Id == substitutedUserId));
    }
    
    /// <summary>
    /// Проверить необходимость системного замещения Руководитель вышестоящего подразделения -> Руководитель подразделения.
    /// </summary>
    /// <param name="substitutedUserId">ID замещаемого пользователя.</param>
    /// <param name="substituteId">ID замещающего пользователя.</param>
    /// <returns>True, если должно быть системное замещение.</returns>
    public virtual bool IsManagerSubordinateToHeadOfficeManager(long substitutedUserId, long substituteId)
    {
      return Departments.GetAll().Any(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
                                      d.Manager.Id == substitutedUserId &&
                                      d.HeadOffice != null &&
                                      d.HeadOffice.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
                                      d.HeadOffice.Manager.Id == substituteId);
    }

    /// <summary>
    /// Проверить необходимость системного замещения Руководитель НОР -> Руководитель головного подразделения.
    /// </summary>
    /// <param name="substitutedUserId">ID замещаемого пользователя.</param>
    /// <param name="substituteId">ID замещающего пользователя.</param>
    /// <returns>True, если должно быть системное замещение.</returns>
    public virtual bool IsManagerSubordinateToCeo(long substitutedUserId, long substituteId)
    {
      return Departments.GetAll().Any(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
                                      d.Manager.Id == substitutedUserId &&
                                      d.HeadOffice == null &&
                                      d.BusinessUnit != null &&
                                      d.BusinessUnit.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
                                      d.BusinessUnit.CEO.Id == substituteId);
    }
    
    /// <summary>
    /// Удалить устаревшие системные замещения.
    /// </summary>
    [Public]
    public virtual void DeleteObsoleteSystemSubstitutions()
    {
      var batchSize = Constants.Module.DeleteSystemSubstitutionBatchSize;
      var queueItemsBatch = SystemSubstitutionQueueItems.GetAll()
        .OrderBy(s => s.Id)
        .Take(batchSize)
        .ToList();
      
      var lastDeletedQueueItemId = 0L;
      
      while (queueItemsBatch.Any())
      {
        lastDeletedQueueItemId = queueItemsBatch.Last().Id;
        foreach (var queueItem in queueItemsBatch)
        {
          var substitutedUserId = queueItem.SubstitutedUserId.Value;
          var substituteId = queueItem.SubstituteId.Value;
          try
          {
            var substitutions = Substitutions.GetAll()
              .Where(s => s.Substitute.Id == substituteId &&
                     s.User.Id == substitutedUserId &&
                     s.IsSystem == true)
              .ToList();
            
            if (!substitutions.Any())
            {
              Logger.DebugFormat("SystemSubstitutionQueueItem (ID={0}). User (ID={1}) has no system substitution by Substitute (ID={2}).", queueItem.Id, substitutedUserId, substituteId);
              Functions.Module.DeleteSystemSubstitutionQueueItem(queueItem);
              continue;
            }
            
            if (this.IsEmployeeSubordinateToManager(substitutedUserId, substituteId))
            {
              Logger.DebugFormat("SystemSubstitutionQueueItem (ID={0}). Employee (ID={1}) subordinate to Manager (ID={2})", queueItem.Id, substitutedUserId, substituteId);
              Functions.Module.DeleteSystemSubstitutionQueueItem(queueItem);
              continue;
            }
            
            if (this.IsManagerSubordinateToHeadOfficeManager(substitutedUserId, substituteId))
            {
              Logger.DebugFormat("SystemSubstitutionQueueItem (ID={0}). Manager (ID={1}) subordinate to head office Manager (ID={2})", queueItem.Id, substitutedUserId, substituteId);
              Functions.Module.DeleteSystemSubstitutionQueueItem(queueItem);
              continue;
            }
            
            if (this.IsManagerSubordinateToCeo(substitutedUserId, substituteId))
            {
              Logger.DebugFormat("SystemSubstitutionQueueItem (ID={0}). Manager (ID={1}) subordinate to CEO (ID={2})", queueItem.Id, substitutedUserId, substituteId);
              Functions.Module.DeleteSystemSubstitutionQueueItem(queueItem);
              continue;
            }
            
            foreach (var substitution in substitutions)
              Substitutions.Delete(substitution);

            Functions.Module.DeleteSystemSubstitutionQueueItem(queueItem);
          }
          catch (Exception ex)
          {
            Logger.ErrorFormat("An error was encountered while processing the system substitution. Substitute (ID={0}), User (ID={1})",
                               ex, substituteId, substitutedUserId);
          }
        }
        
        queueItemsBatch = SystemSubstitutionQueueItems.GetAll()
          .OrderBy(s => s.Id)
          .Where(i => i.Id > lastDeletedQueueItemId)
          .Take(batchSize)
          .ToList();
      }
    }
    
    /// <summary>
    /// Получить информацию о системном замещении.
    /// </summary>
    /// <param name="managerId">Идентификатор руководителя.</param>
    /// <param name="employeeId">Идентификатор сотрудника.</param>
    /// <returns>Информация о системном замещении.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public virtual string GetSystemSubstitutionInfo(long managerId, long employeeId)
    {
      var loggerPrefix = "GetSystemSubstitutionInfoCommand. ";
      var managerInfo = this.GetEmployeeInfo(managerId);
      var employeeInfo = this.GetEmployeeInfo(employeeId);
      if (string.IsNullOrEmpty(managerInfo) || string.IsNullOrEmpty(employeeInfo))
        return string.Format("Error*Employee with ID {0} not found.", string.IsNullOrEmpty(managerInfo) ? employeeId : managerId);
      
      var substitution = Substitutions.GetAll().Where(s => s.User.Id == employeeId && s.Substitute.Id == managerId && s.IsSystem == true).FirstOrDefault();
      return this.CreateSystemSubstitutionInfoMessage(substitution, managerInfo, employeeInfo, loggerPrefix);
    }
    
    /// <summary>
    /// Получить информацию о системном замещении.
    /// </summary>
    /// <param name="substitution">Замещение.</param>
    /// <param name="managerInfo">Информация о руководителе.</param>
    /// <param name="employeeInfo">Информация о сотруднике.</param>
    /// <param name="loggerPrefix">Префикс логгера.</param>
    /// <returns>Информация о системном замещении.</returns>
    private string CreateSystemSubstitutionInfoMessage(ISubstitution substitution, string managerInfo, string employeeInfo, string loggerPrefix)
    {
      if (substitution == null)
        return string.Format("Warning*Manager {0} does not have a system substitution for employee {1}.", managerInfo, employeeInfo);
      
      var messageHeaders = "Id`Manager`Employee`Start date`End date`Status`Ar transfer status|";
      var substitutionInfo = this.GetSubstitutionInfo(substitution);
      var accessRightsTransferStatistic = this.GetAccessRightsTransferStatisticInfo(substitution, loggerPrefix);
      var message = "Info*System substitution:|" + messageHeaders + substitutionInfo + "|" + accessRightsTransferStatistic;

      return message;
    }
    
    /// <summary>
    /// Получить строку со статистикой по передаваемым правам.
    /// </summary>
    /// <param name="substitution">Замещение.</param>
    /// <param name="loggerPrefix">Префикс логгера.</param>
    /// <returns>Статистика по передаваемым правам.</returns>
    private string GetAccessRightsTransferStatisticInfo(ISubstitution substitution, string loggerPrefix)
    {
      try
      {
        var transferAccessRightsSession = AccessRightsTransferSessions.GetAll()
          .Where(s => s.EmployeeId == substitution.User.Id && s.SubstituteId == substitution.Substitute.Id)
          .FirstOrDefault();
        if (transferAccessRightsSession == null)
        {
          throw new Exception("TransferSessionNotFound.");
        }

        Guid parsedSessionId;
        if (Guid.TryParse(transferAccessRightsSession.SessionId, out parsedSessionId) == false)
          throw new Exception("Session ID must be GUID type.");

        var message = string.Empty;
        var sessionStatus = Functions.Module.GetAccessRightsSessionStatus(parsedSessionId, loggerPrefix);

        switch (sessionStatus?.JobStatus)
        {
          case BackgroundJobExecutionStatus.New:
            message = Environment.NewLine + "The access rights transfer has been queued.";
            break;
            
          case BackgroundJobExecutionStatus.Done:
            message = Environment.NewLine + "The access rights transfer has been completed, the status will be updated later.";
            break;
            
          case BackgroundJobExecutionStatus.Processing:
            var transferEntitiesCount = sessionStatus.Value.InitialEntitiesCount ?? 0;
            var processedEntitiesCount = sessionStatus.Value.RemainingEntitiesCount ?? 0;
            var skippedEntitiesCount = sessionStatus.Value.SkippedEntitiesCount ?? 0;

            message = string.Format(Environment.NewLine +
                                    "Total objects for access rights transfer: {0}, processed: {1}, skipped: {2}.",
                                    transferEntitiesCount, processedEntitiesCount, skippedEntitiesCount);
            break;

          default:
            throw new Exception("Session status is null or not recognized.");
            break;
        }
        
        return message;
      }
      catch (Exception ex)
      {
        var loggerMessage = loggerPrefix + string.Format("Error message: {0}.", ex.Message);
        Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Debug(loggerMessage);

        return "Error*Errors occurred while transfer access rights. Read the log files for more information";
      }
    }
    
    /// <summary>
    /// Получить информацию о системных замещениях руководителя.
    /// </summary>
    /// <param name="managerId">Идентификатор руководителя.</param>
    /// <returns>Информация о системных замещениях руководителя.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public virtual string GetManagerSystemSubstitutionInfo(long managerId)
    {
      var loggerPrefix = "GetManagerSystemSubstitutionInfoCommand. ";
      var managerInfo = this.GetEmployeeInfo(managerId);
      if (string.IsNullOrEmpty(managerInfo))
        return string.Format("Error*Employee with ID {0} not found.", managerId);
      
      var substitutions = Substitutions.GetAll()
        .Where(s => s.Substitute.Id == managerId &&
               s.IsSystem == true)
        .OrderBy(s => s.Id)
        .ToList();
      return this.CreateManagerSystemSubstitutionInfoMessage(substitutions, managerId, loggerPrefix);
    }
    
    /// <summary>
    /// Создать строку с информацией о системных замещениях руководителя.
    /// </summary>
    /// <param name="substitutions">Замещения.</param>
    /// <param name="managerId">Идентификатор руководителя.</param>
    /// <param name="loggerPrefix">Префикс логгера.</param>
    /// <returns>Информация о системных замещениях руководителя.</returns>
    private string CreateManagerSystemSubstitutionInfoMessage(List<ISubstitution> substitutions, long managerId, string loggerPrefix)
    {
      var managerInfo = this.GetEmployeeInfo(managerId);
      if (!substitutions.Any())
        return string.Format("Warning*Manager {0} does not have a system substitutions.", managerInfo);
      
      var messageHeaders = "Id`Manager`Employee`Start date`End date`Status`Ar transfer status|";
      var message = new System.Text.StringBuilder("System substitution:|");
      message.Append(messageHeaders);
      foreach (var substitution in substitutions)
      {
        var substitutionInfo = this.GetSubstitutionInfo(substitution);
        message.Append(substitutionInfo).Append("|");
      }
      
      var readyForCopySubstitutionsCount = this.GetSubstitutionsForTransferAccessRights(managerId, loggerPrefix).Count;
      var activeSubstitutionsCount = substitutions
        .Where(s => s.Status == Sungero.CoreEntities.DatabookEntry.Status.Active).Count();
      var inProcessSubstitutionsCount = new Regex("In progress").Matches(message.ToString()).Count;
      var closedSubstitutionsCount = substitutions
        .Where(s => s.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed).Count();
      var messageFooter = string.Format("All system substitutions count: {0}|Active: {1}| - ready for copy access rights: " +
                                        "{2}| - access rights copy in process: {3}|Closed: {4}", substitutions.Count(), activeSubstitutionsCount,
                                        readyForCopySubstitutionsCount, inProcessSubstitutionsCount, closedSubstitutionsCount);
      
      return message.Append(messageFooter).ToString();
    }
    
    /// <summary>
    /// Получить строку с информацией по замещению.
    /// </summary>
    /// <param name="substitution">Замещение.</param>
    /// <returns>Строка с информацией по замещению.</returns>
    private string GetSubstitutionInfo(ISubstitution substitution)
    {
      var transferAccessRightsSession = AccessRightsTransferSessions.GetAll()
        .Where(s => s.EmployeeId == substitution.User.Id &&
               s.SubstituteId  == substitution.Substitute.Id)
        .FirstOrDefault();
      
      var substitutionStartDate = substitution.StartDate.HasValue ? substitution.StartDate.Value.ToString("dd.MM.yyyy") : "-";
      var substitutionEndDate = substitution.EndDate.HasValue ? substitution.EndDate.Value.ToString("dd.MM.yyyy") : "-";
      var transferAccessRightsSessionStatus = transferAccessRightsSession != null ? "In progress" : "-";
      var employeeInfo = this.GetEmployeeInfo(substitution.User.Id);
      var managerInfo = this.GetEmployeeInfo(substitution.Substitute.Id);
      
      var substitutionInfo = string.Format("{0}`{1}`{2}`{3}`{4}`{5}`{6}", substitution.Id, managerInfo, employeeInfo,
                                           substitutionStartDate, substitutionEndDate,
                                           substitution.Status, transferAccessRightsSessionStatus);
      return substitutionInfo;
    }
    
    #endregion

    #region Обложка

    /// <summary>
    /// Создать сотрудника.
    /// </summary>
    /// <returns>Новый сотрудник.</returns>
    [Remote]
    public static IEmployee CreateEmployee()
    {
      return Employees.Create();
    }

    /// <summary>
    /// Создать нашу организацию.
    /// </summary>
    /// <returns>Новая НОР.</returns>
    [Remote]
    public static IBusinessUnit CreateBusinessUnit()
    {
      return BusinessUnits.Create();
    }

    /// <summary>
    /// Создать подразделение.
    /// </summary>
    /// <returns>Новое подразделение.</returns>
    [Remote]
    public static IDepartment CreateDepartment()
    {
      return Departments.Create();
    }
    
    /// <summary>
    /// Получить настройки видимости организационной структуры.
    /// </summary>
    /// <returns>Настройки видимости организационной структуры.</returns>
    [Remote]
    public virtual IVisibilitySetting GetVisibilitySettings()
    {
      return VisibilitySettings.GetAll().SingleOrDefault();
    }

    #endregion
    
    /// <summary>
    /// Получить роль "Руководители наших организаций".
    /// </summary>
    /// <returns>Роль "Руководители наших организаций".</returns>
    [Remote(IsPure = true)]
    public static IRole GetCEORole()
    {
      return Roles.GetAll(r => r.Sid == Constants.Module.BusinessUnitHeadsRole).SingleOrDefault();
    }
    
    /// <summary>
    /// Получить сотрудника по id.
    /// </summary>
    /// <param name="id">Id.</param>
    /// <returns>Сотрудник.</returns>
    [Public, Remote(IsPure = true)]
    public static IEmployee GetEmployeeById(long id)
    {
      return Employees.Get(id);
    }
    
    /// <summary>
    /// Получить список сотрудников по id.
    /// </summary>
    /// <param name="ids">Список Id.</param>
    /// <returns>Список сотрудников.</returns>
    [Public, Remote(IsPure = true)]
    public static List<IEmployee> GetEmployeesByIds(List<long> ids)
    {
      return Employees.GetAll(e => ids.Contains(e.Id)).ToList();
    }
    
    /// <summary>
    /// Получить подразделение по id.
    /// </summary>
    /// <param name="id">Id.</param>
    /// <returns>Подразделение.</returns>
    [Public, Remote(IsPure = true)]
    public static IDepartment GetDepartmentById(long id)
    {
      return Departments.Get(id);
    }
    
    /// <summary>
    /// Получить сертификаты сотрудника.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Список сертификатов.</returns>
    [Public, Remote(IsPure = true)]
    public static IQueryable<ICertificate> GetCertificatesOfEmployee(IEmployee employee)
    {
      return Certificates.GetAll(x => Equals(x.Owner, employee));
    }
    
    /// <summary>
    /// Получить неавтоматизированных сотрудников без замещения.
    /// </summary>
    /// <param name="employees"> Список сотрудников для обработки.</param>
    /// <returns> Список неавтоматизированных сотрудников без замещения.</returns>
    [Public, Remote(IsPure = true)]
    public IQueryable<Sungero.Company.IEmployee> GetNotAutomatedEmployees(List<Sungero.Company.IEmployee> employees)
    {
      var notAutomatedEmployeesWoSubstitution = new List<Sungero.Company.IEmployee>();
      var signOutEmployees = employees.Where(r => r.Login == null || r.Login.Status == CoreEntities.DatabookEntry.Status.Closed)
        .ToList();
      
      foreach (var employee in signOutEmployees)
      {
        var substitutions = Substitutions.GetAll()
          .Where(x => Equals(x.User, employee) &&
                 x.IsSystem != true &&
                 x.Status == CoreEntities.DatabookEntry.Status.Active &&
                 (!x.StartDate.HasValue || Calendar.Today >= x.StartDate) &&
                 (!x.EndDate.HasValue || Calendar.Today <= x.EndDate))
          .ToList();
        
        if (!substitutions.Any())
          notAutomatedEmployeesWoSubstitution.Add(employee);

        var substitutors = substitutions.Select(x => x.Substitute);
        if (!substitutors.Any(x => x.Login != null && x.Login.Status != CoreEntities.DatabookEntry.Status.Closed))
          notAutomatedEmployeesWoSubstitution.Add(employee);
      }
      
      return notAutomatedEmployeesWoSubstitution.AsQueryable();
    }
    
    /// <summary>
    /// Получить сотрудников по списку с раскрытием групп и ролей.
    /// </summary>
    /// <param name="recipients">Список субъектов прав.</param>
    /// <returns>Список, раскрытый до сотрудников.</returns>
    /// <remarks>Находится в серверных функциях, т.к. для GetAllUsersInGroup() нет клиентской реализации.</remarks>
    [Public, Remote(IsPure = true)]
    public static List<IEmployee> GetEmployeesFromRecipientsRemote(List<IRecipient> recipients)
    {
      return GetEmployeesFromRecipients(recipients).Distinct().ToList();
    }
    
    /// <summary>
    /// Получить сотрудников по списку с раскрытием групп и ролей.
    /// </summary>
    /// <param name="recipients">Список субъектов прав.</param>
    /// <returns>Список, раскрытый до сотрудников.</returns>
    /// <remarks>Продублировано GetEmployeesFromRecipients без атрибута Remote,
    /// т.к. Remote в вебе отрабатывает с запаковкой/распаковкой, даже если вызывается с сервера. Это дополнительные накладные расходы.
    /// Находится в серверных функциях, т.к. для GetAllUsersInGroup() нет клиентской реализации.</remarks>
    [Public]
    public static List<IEmployee> GetEmployeesFromRecipients(List<IRecipient> recipients)
    {
      var performers = new List<IEmployee>();
      
      foreach (var recipient in recipients)
      {
        if (Employees.Is(recipient))
        {
          performers.Add(Employees.As(recipient));
        }
        else if (Groups.Is(recipient))
        {
          var groupRecipient = Groups.GetAllUsersInGroup(Groups.As(recipient))
            .Where(r => Employees.Is(r) && r.Status == CoreEntities.DatabookEntry.Status.Active)
            .Select(r => Employees.As(r));
          foreach (var employee in groupRecipient)
            performers.Add(employee);
        }
      }
      
      return performers;
    }
    
    /// <summary>
    /// Количество строк данных для отчета полномочий сотрудника по всем модулям.
    /// </summary>
    /// <param name="employee">Сотрудник для обработки.</param>
    /// <returns>Количество.</returns>
    /// <remarks>Функция введена для тестирования. Если вернулась пустая строка - значит, в результате выполнения функции произошли ошибки.</remarks>
    [Public, Remote]
    public static string GetCountResponsibilitiesReportData(IEmployee employee)
    {
      try
      {
        var allReportData = GetAllResponsibilitiesReportData(employee);
        return allReportData.Count.ToString();
      }
      catch
      {
        return string.Empty;
      }
    }
    
    /// <summary>
    /// Данные для отчета полномочий сотрудника по всем модулям.
    /// </summary>
    /// <param name="employee">Сотрудник для обработки.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public static List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> GetAllResponsibilitiesReportData(IEmployee employee)
    {
      var result = new List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine>();
      
      if (employee == null)
        return result;
      
      var companyData = Sungero.Company.Functions.Module.GetResponsibilitiesReportData(employee);
      var exchangeData = ExchangeCore.PublicFunctions.Module.GetResponsibilitiesReportData(employee);
      var docflowData = Sungero.Docflow.PublicFunctions.Module.GetResponsibilitiesReportData(employee);
      var projectData = Sungero.Projects.PublicFunctions.Module.GetResponsibilitiesReportData(employee);
      var meetingsData = Sungero.Meetings.PublicFunctions.Module.GetResponsibilitiesReportData(employee);
      var partiesData = Sungero.Parties.PublicFunctions.Module.GetResponsibilitiesReportData(employee);
      result.AddRange(companyData);
      result.AddRange(exchangeData);
      result.AddRange(docflowData);
      result.AddRange(projectData);
      result.AddRange(meetingsData);
      result.AddRange(partiesData);
      return result;
    }
    
    /// <summary>
    /// Дополнить набор данных для отчета "Полномочия и зоны ответственности сотрудника".
    /// </summary>
    /// <param name="source">Исходный набор данных.</param>
    /// <param name="entity">Сущность, которая добавляется к набору.</param>
    /// <param name="moduleName">Имя модуля.</param>
    /// <param name="modulePriority">Приоритет модуля.</param>
    /// <param name="sectionName">Имя раздела, к которому относятся сущности.</param>
    /// <returns>Набор данных для отчета "Полномочия и зоны ответственности сотрудника".</returns>
    [Public]
    public static List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> AppendResponsibilitiesReportResult(
      List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> source,
      IEntity entity,
      string moduleName,
      int modulePriority,
      string sectionName)
    {
      var entities = new List<IEntity>();
      if (entity != null)
        entities.Add(entity);
      
      return AppendResponsibilitiesReportResult(source, entities, moduleName, modulePriority, sectionName, null);
    }
    
    /// <summary>
    /// Дополнить набор данных для отчета "Полномочия и зоны ответственности сотрудника".
    /// </summary>
    /// <param name="source">Исходный набор данных.</param>
    /// <param name="entities">Сущности, которые добавляются к набору.</param>
    /// <param name="moduleName">Имя модуля.</param>
    /// <param name="modulePriority">Приоритет модуля.</param>
    /// <param name="sectionName">Имя раздела, к которому относятся сущности.</param>
    /// <param name="mainEntity">Основная сущность (будет выделена жирным).</param>
    /// <returns>Набор данных для отчета "Полномочия и зоны ответственности сотрудника".</returns>
    [Public]
    public static List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> AppendResponsibilitiesReportResult(
      List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> source,
      System.Collections.Generic.IEnumerable<IEntity> entities,
      string moduleName,
      int modulePriority,
      string sectionName,
      IEntity mainEntity = null)
    {
      var responsibilityPriority = modulePriority + source.Count;
      var entitiesWithPresentation = entities.ToDictionary<IEntity, IEntity, string>(x => x, x => x.DisplayValue);
      source = AppendResponsibilitiesReportResult(source, entitiesWithPresentation, moduleName, responsibilityPriority, sectionName, mainEntity);
      
      return source;
    }
    
    /// <summary>
    /// Дополнить набор данных для отчета "Полномочия и зоны ответственности сотрудника".
    /// </summary>
    /// <param name="source">Исходный набор данных.</param>
    /// <param name="entitiesWithPresentation">Сущности и их отображаемые значения, которые добавляются к набору.</param>
    /// <param name="moduleName">Имя модуля.</param>
    /// <param name="responsibilityPriority">Приоритет вида ответственности.</param>
    /// <param name="sectionName">Имя раздела, к которому относятся сущности.</param>
    /// <param name="mainEntity">Основная сущность (будет выделена жирным).</param>
    /// <returns>Набор данных для отчета "Полномочия и зоны ответственности сотрудника".</returns>
    [Public]
    public static List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> AppendResponsibilitiesReportResult(
      List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> source,
      System.Collections.Generic.IDictionary<IEntity, string> entitiesWithPresentation,
      string moduleName,
      int responsibilityPriority,
      string sectionName,
      IEntity mainEntity = null)
    {
      if (!entitiesWithPresentation.Any())
      {
        var emptyTableLine = Company.PublicFunctions.Module.CreateResponsibilitiesReportTableLine(moduleName, string.Empty, string.Empty, null, false);
        emptyTableLine.Responsibility = sectionName;
        emptyTableLine.Priority = responsibilityPriority;
        source.Add(emptyTableLine);
      }
      foreach (var entityWithPresentation in entitiesWithPresentation)
      {
        var entityName = entityWithPresentation.Value;
        var entityPriority = entitiesWithPresentation.Count > 1 ? responsibilityPriority + 1 : responsibilityPriority;
        var isMain = false;
        if (Equals(entityWithPresentation.Key, mainEntity) && entitiesWithPresentation.Count > 1)
        {
          isMain = true;
          entityPriority = responsibilityPriority;
        }
        
        var newTableLineRecord = Company.PublicFunctions.Module.CreateResponsibilitiesReportTableLine(moduleName,
                                                                                                      sectionName,
                                                                                                      entityName,
                                                                                                      entityWithPresentation.Key,
                                                                                                      isMain);
        newTableLineRecord.Priority = entityPriority;
        source.Add(newTableLineRecord);
      }
      
      return source;
    }
    
    /// <summary>
    /// Данные для отчета полномочий сотрудника из модуля Компания.
    /// </summary>
    /// <param name="employee">Сотрудник для обработки.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public virtual List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> GetResponsibilitiesReportData(IEmployee employee)
    {
      var result = new List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine>();
      // HACK: Получаем отображаемое имя модуля.
      var moduleGuid = new CompanyModule().Id;
      var moduleName = Sungero.Metadata.Services.MetadataSearcher.FindModuleMetadata(moduleGuid).GetDisplayName();
      var modulePriority = Company.Constants.ResponsibilitiesReport.CompanyPriority;
      
      // Должность.
      result = AppendResponsibilitiesReportResult(result, employee.JobTitle, moduleName, modulePriority, Resources.Jobtitle);

      // Подразделения.
      if (Departments.AccessRights.CanRead())
      {
        var employeeDepartments = Departments.GetAll()
          .Where(d => d.RecipientLinks.Any(e => Equals(e.Member, employee)))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        result = AppendResponsibilitiesReportResult(result, employeeDepartments, moduleName, modulePriority, Resources.Departments, employee.Department);
      }

      // НОР.
      if (Departments.AccessRights.CanRead() &&
          BusinessUnits.AccessRights.CanRead())
      {
        var businessUnits = Departments.GetAll()
          .Where(d => d.RecipientLinks.Any(e => Equals(e.Member, employee)))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
          .Select(b => b.BusinessUnit)
          .Where(b => b.Status == Sungero.CoreEntities.DatabookEntry.Status.Active).Distinct();
        result = AppendResponsibilitiesReportResult(result, businessUnits, moduleName, modulePriority, Resources.BusinessUnits, employee.Department.BusinessUnit);
      }
      
      // Руководитель подразделений.
      if (Departments.AccessRights.CanRead())
      {
        var managerOfDepartments = Departments.GetAll().Where(d => Equals(d.Manager, employee))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        result = AppendResponsibilitiesReportResult(result, managerOfDepartments, moduleName, modulePriority, Resources.ManagerOfDepartmens);
      }
      
      // Руководители НОР.
      if (BusinessUnits.AccessRights.CanRead())
      {
        var businessUnitsCEO = BusinessUnits.GetAll().Where(b => Equals(b.CEO, employee))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        result = AppendResponsibilitiesReportResult(result, businessUnitsCEO, moduleName, modulePriority, Resources.BusinessUnitsCEO);
      }
      
      // Главный бухгалтер.
      if (BusinessUnits.AccessRights.CanRead())
      {
        var businessUnitsCAO = BusinessUnits.GetAll().Where(b => Equals(b.CAO, employee))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        result = AppendResponsibilitiesReportResult(result, businessUnitsCAO, moduleName, modulePriority, Resources.BusinessUnitsCAO);
      }
      
      // Роли.
      if (Roles.AccessRights.CanRead())
      {
        var roles = Roles.GetAll().Where(r => r.RecipientLinks.Any(e => Equals(e.Member, employee)))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        result = AppendResponsibilitiesReportResult(result, roles, moduleName, modulePriority, Resources.Roles);
      }
      
      // Помощники руководителей.
      if (ManagersAssistants.AccessRights.CanRead())
      {
        var managersAssistants = ManagersAssistants.GetAll()
          .Where(m => Equals(m.Assistant, employee) || Equals(m.Manager, employee))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
          .ToDictionary<IEntity, IEntity, string>(x => x,
                                                  x => string.Format("{0}: {1}{2}{3}: {4}{5}{6}",
                                                                     Resources.Manager,
                                                                     PublicFunctions.Employee.GetShortName(ManagersAssistants.As(x).Manager, false),
                                                                     Environment.NewLine,
                                                                     Resources.Assistant,
                                                                     PublicFunctions.Employee.GetShortName(ManagersAssistants.As(x).Assistant, false),
                                                                     Environment.NewLine,
                                                                     this.CreateAssistantResponsibilityString(ManagersAssistants.As(x))));
        result = AppendResponsibilitiesReportResult(result, managersAssistants, moduleName, modulePriority + result.Count, Resources.ManagersAssistants);
      }
      
      // Замещения.
      if (Substitutions.AccessRights.CanRead())
      {
        var substitutions = Substitutions.GetAll()
          .Where(s => (Equals(s.Substitute, employee) ||
                       Equals(s.User, employee)) &&
                 s.Status == CoreEntities.DatabookEntry.Status.Active &&
                 (!s.StartDate.HasValue || s.StartDate <= Calendar.UserToday) &&
                 (!s.EndDate.HasValue || s.EndDate >= Calendar.UserToday))
          .Where(s => s.IsSystem != true)
          .ToDictionary<IEntity, IEntity, string>(x => x, x => CreateSubstitutionPresentation(Substitutions.As(x)));
        result = AppendResponsibilitiesReportResult(result, substitutions, moduleName, modulePriority + result.Count, Resources.Substitutions);
      }
      
      return result;
    }
    
    /// <summary>
    /// Сформировать текстовую информацию о полномочиях ассистента руководителя.
    /// </summary>
    /// <param name="managersAssistant">Запись справочника Ассистенты руководителя.</param>
    /// <returns>Текстовая информация о полномочиях ассистента руководителя.</returns>
    public virtual string CreateAssistantResponsibilityString(Company.IManagersAssistant managersAssistant)
    {
      var responsibilities = new List<string>();
      
      if (managersAssistant.IsAssistant == true)
        responsibilities.Add(ManagersAssistants.Info.Properties.IsAssistant.LocalizedName);
      
      if (managersAssistant.PreparesResolution == true)
        responsibilities.Add(ManagersAssistants.Info.Properties.PreparesResolution.LocalizedName);
      
      if (managersAssistant.SendActionItems == true)
        responsibilities.Add(ManagersAssistants.Info.Properties.SendActionItems.LocalizedName);
      
      if (managersAssistant.PreparesAssignmentCompletion == true)
        responsibilities.Add(ManagersAssistants.Info.Properties.PreparesAssignmentCompletion.LocalizedName);
      
      var separator = string.Format(";{0}", Environment.NewLine);
      return string.Format("{0}.", string.Join(separator, responsibilities));
    }
    
    /// <summary>
    /// Сформировать текстовую информацию о замещении.
    /// </summary>
    /// <param name="substitution">Замещение.</param>
    /// <returns>Текстовая информация о замещении.</returns>
    public static string CreateSubstitutionPresentation(ISubstitution substitution)
    {
      var startDate = substitution.StartDate.HasValue ? string.Format("{0} {1}", Resources.From, substitution.StartDate.Value.ToShortDateString()) : string.Empty;
      var endDate = substitution.EndDate.HasValue ? string.Format("{0} {1}", Resources.To, substitution.EndDate.Value.ToShortDateString()) : string.Empty;
      var period = string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate) ? Resources.Permanently : string.Format("{0} {1}", startDate, endDate).Trim();
      return string.Format("{0}: {1}{2}{3}: {4}{2}{5}: {6}",
                           Resources.Substitute,
                           PublicFunctions.Employee.GetShortName(GetEmployeeById(substitution.Substitute.Id), false),
                           Environment.NewLine,
                           Resources.Employee,
                           PublicFunctions.Employee.GetShortName(GetEmployeeById(substitution.User.Id), false),
                           Resources.Period,
                           period);
    }
    
    /// <summary>
    /// Строит строку данных для отчета о полномочиях.
    /// </summary>
    /// <param name="moduleName">Имя модуля.</param>
    /// <param name="responsibility"> Справочник/роль.</param>
    /// <param name="record">Запись справочника, текст.</param>
    /// <param name="element">Запись справочника, объект.</param>
    /// <param name="isMain">Признак основного элемента.</param>
    /// <returns>Строка данных отчета о полномочиях.</returns>
    [Public]
    public static Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine CreateResponsibilitiesReportTableLine(string moduleName,
                                                                                                                          string responsibility,
                                                                                                                          string record,
                                                                                                                          Sungero.Domain.Shared.IEntity element,
                                                                                                                          bool isMain)
    {
      var newTableLine = new Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine();
      newTableLine.ModuleName = moduleName;
      newTableLine.Responsibility = responsibility;
      newTableLine.Record = !string.IsNullOrEmpty(record) ? record : "-";
      if (element != null)
      {
        newTableLine.RecordId = element.Id;
        newTableLine.RecordHyperlink = Hyperlinks.Get(element);
      }
      newTableLine.IsMain = isMain;
      return newTableLine;
    }
    
    /// <summary>
    /// Получить все объекты IRecipient.
    /// </summary>
    /// <returns>Все объекты IRecipient в виде запроса.</returns>
    [Remote(IsPure = true)]
    public IQueryable<IRecipient> GetAllRecipients()
    {
      // Dmitriev_IA: Используется в GetAllActiveNoSystemGroups().
      // Серверная фильтрация IQueryable не работает для desktop-клиента. Bug 98921.
      return Sungero.CoreEntities.Recipients.GetAll();
    }
    
    /// <summary>
    /// Получить режим ограничения видимости оргструктуры.
    /// </summary>
    /// <returns>True, если режим ограничения видимости оргструктуры, иначе False.</returns>
    [Public, Remote]
    public virtual bool IsRecipientRestrict()
    {
      var visibilitySettings = VisibilitySettings.GetAll().SingleOrDefault();
      if (visibilitySettings == null)
        return false;
      
      if (visibilitySettings.NeedRestrictVisibility != true)
        return false;
      
      if (Employees.Current == null)
        return false;
      
      var unrestrictedRecipients = visibilitySettings.UnrestrictedRecipients.Select(r => r.Recipient.Id).ToList();
      var headRecipients = Functions.Module.GetHeadRecipientsByEmployee(Employees.Current.Id);
      headRecipients.Add(Employees.Current.Id);
      
      return !unrestrictedRecipients.Any(r => headRecipients.Contains(r));
    }
    
    /// <summary>
    /// Включен ли режим ограничения видимости оргструктуры.
    /// </summary>
    /// <returns>True, если режим ограничения видимости оргструктуры включен, иначе False.</returns>
    [Public, Remote]
    public virtual bool IsRecipientRestrictModeOn()
    {
      var visibilitySettings = VisibilitySettings.GetAll().SingleOrDefault();
      if (visibilitySettings == null)
        return false;
      
      return visibilitySettings.NeedRestrictVisibility == true;
    }
    
    /// <summary>
    /// Получить список доступных реципиентов согласно настройке.
    /// </summary>
    /// <param name="recipientTypeGuid">GUID типа сущности.</param>
    /// <returns>Список Ид реципиентов.</returns>
    [Public, Remote]
    public virtual List<long> GetVisibleRecipientIds(string recipientTypeGuid)
    {
      if (Employees.Current == null)
        return new List<long>();
      
      var recipientIds = Functions.Module.GetAllVisisbleRecipientsIds(Employees.Current.Id, recipientTypeGuid);
      return recipientIds;
    }
    
    /// <summary>
    /// Получить головные Наши организации/подразделения для сотрудника.
    /// </summary>
    /// <param name="currentEmployeeId">ИД сотрудника.</param>
    /// <returns>Список ИД реципиентов.</returns>
    [Public]
    public virtual List<long> GetHeadRecipientsByEmployee(long currentEmployeeId)
    {
      var parameters = string.Format("{0}", currentEmployeeId);
      return this.GetRecipientIdsFromStoredProcedure(Constants.Module.GetHeadRecipientsByEmployeeProcedureName, parameters);
    }
    
    /// <summary>
    /// Получить список ИД реципиентов Нашей организации/подразделения.
    /// </summary>
    /// <param name="currentRecipientId">ИД реципиента.</param>
    /// <param name="recipientTypeGuid">GUID типа сущности.</param>
    /// <returns>Раскрытый список ИД реципиентов.</returns>
    [Public]
    public virtual List<long> GetAllVisisbleRecipientsIds(long currentRecipientId, string recipientTypeGuid)
    {
      var parameters = string.Format("{0}, '{1}'", currentRecipientId, recipientTypeGuid);
      return this.GetRecipientIdsFromStoredProcedure(Constants.Module.GetAllVisibleRecipientsProcedureName, parameters);
    }

    /// <summary>
    /// Создать настройки видимости организационной структуры.
    /// </summary>
    public virtual void CreateVisibilitySettings()
    {
      var visibilitySettings = VisibilitySettings.Create();
      visibilitySettings.Name = VisibilitySettings.Info.LocalizedName;
      visibilitySettings.Save();
    }
    
    /// <summary>
    /// Исключить системных реципиентов из списка.
    /// </summary>
    /// <param name="query">Запрос.</param>
    /// <param name="isRecipientsStatusActive">Возвращать только действующих реципиентов.</param>
    /// <returns>Отфильтрованный результат запроса.</returns>
    public IQueryable<Sungero.CoreEntities.IRecipient> ExcludeSystemRecipients(IQueryable<Sungero.CoreEntities.IRecipient> query, bool isRecipientsStatusActive)
    {
      var systemRecipientsSid = PublicFunctions.Module.GetSystemRecipientsSidWithoutAllUsers(false);
      if (isRecipientsStatusActive)
        query = query.Where(x => x.Status == CoreEntities.DatabookEntry.Status.Active);
      
      return query.Where(x => (Employees.Is(x) || Groups.Is(x)) && !systemRecipientsSid.Contains(x.Sid.Value));
    }
    
    /// <summary>
    /// Получить список ИД реципиентов через хранимую процедуру.
    /// </summary>
    /// <param name="procedureName">Имя хранимой процедуры.</param>
    /// <param name="parameters">Параметры хранимой процедуры.</param>
    /// <returns>Список ИД реципиентов.</returns>
    private List<long> GetRecipientIdsFromStoredProcedure(string procedureName, string parameters)
    {
      var recipientIds = new List<long>();
      var commandText = string.Format(Queries.Module.ExecuteStoredProcedure, procedureName, parameters);
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = commandText;
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
            recipientIds.Add(reader.GetInt64(0));
        }
      }
      return recipientIds;
    }
    
    /// <summary>
    /// Сформировать всплывающую подсказку о сотруднике.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Всплывающая подсказка о сотруднике.</returns>
    [Public]
    public virtual Sungero.Core.IDigestModel GetEmployeePopup(IEmployee employee)
    {
      return Functions.Employee.GetEmployeePopup(employee);
    }
    
    /// <summary>
    /// Получить действующих ассистентов руководителя.
    /// </summary>
    /// <returns>Действующие ассистенты руководителя.</returns>
    [Public, Remote]
    public virtual IQueryable<IManagersAssistant> GetActiveManagerAssistants()
    {
      return ManagersAssistants.GetAll()
        .Where(m => m.Status == CoreEntities.DatabookEntry.Status.Active &&
               m.Assistant.Status == CoreEntities.DatabookEntry.Status.Active);
    }
    
    /// <summary>
    /// Получить помощников руководителя.
    /// </summary>
    /// <returns>Помощники руководителя.</returns>
    [Public, Remote]
    public virtual IQueryable<IManagersAssistant> GetAssistants()
    {
      return this.GetActiveManagerAssistants()
        .Where(m => m.IsAssistant == true);
    }
    
    /// <summary>
    /// Получить ассистентов, кто готовит резолюцию для руководителя.
    /// </summary>
    /// <returns>Ассистенты, которые готовят резолюцию для руководителя.</returns>
    [Public, Remote]
    public virtual IQueryable<IManagersAssistant> GetResolutionPreparers()
    {
      return this.GetActiveManagerAssistants()
        .Where(m => m.PreparesResolution == true);
    }
    
    /// <summary>
    /// Установить пароль для учетной записи.
    /// </summary>
    /// <param name="loginId">Id учетной записи.</param>
    /// <param name="password">Пароль.</param>
    [Public(WebApiRequestType = RequestType.Post)]
    public void SetLoginPassword(long loginId, string password)
    {
      var login = Logins.Get(loginId);
      var credentials = this.GetCredentials(password).Split(new string[] { "|" }, StringSplitOptions.None);
      Sungero.Domain.Shared.RemoteFunctionExecutor.Execute(Guid.Parse("55f542e9-4645-4f8d-999e-73cc71df62fd"), "SetLoginPassword", login, credentials[0], credentials[1]);
    }
    
    /// <summary>
    /// Создать логин.
    /// </summary>
    /// <param name="loginName">Логин.</param>
    /// <param name="password">Пароль.</param>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual void CreateLogin(string loginName, string password)
    {
      var login = Logins.Create();
      login.LoginName = loginName;
      login.TypeAuthentication = Sungero.CoreEntities.Login.TypeAuthentication.Password;
      var credentials = this.GetCredentials(password).Split(new string[] { "|" }, StringSplitOptions.None);
      Sungero.Domain.Shared.RemoteFunctionExecutor.Execute(Guid.Parse("55f542e9-4645-4f8d-999e-73cc71df62fd"), "SetLoginPassword", login, credentials[0], credentials[1]);
    }
    
    /// <summary>
    /// Получить реквизиты для входа по паролю.
    /// </summary>
    /// <param name="password">Пароль.</param>
    /// <returns>Реквизиты для входа.</returns>
    private string GetCredentials(string password)
    {
      var salt = CommonLibrary.Hashing.PasswordHashManager.Instance.GenerateSalt();
      var passwordHash = CommonLibrary.Hashing.PasswordHashManager.Instance.GenerateHash(CommonLibrary.StringUtils.ToSecureString(password));
      passwordHash = CommonLibrary.Hashing.PasswordHashManager.Instance.AddSaltToHash(passwordHash, salt);
      var passwordHashString = System.Convert.ToBase64String(passwordHash);
      var saltString = System.Convert.ToBase64String(salt);
      
      return string.Join("|", passwordHashString, saltString);
    }
    
    /// <summary>
    /// Проверить, что текущий пользователь - администратор.
    /// </summary>
    /// <returns>Если пользователь администратор - true, иначе - false.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public virtual bool IsCurrentUserAdmin()
    {
      return Users.Current.IncludedIn(Roles.Administrators);
    }
    
    #region Работа с ElasticSearch
    
    /// <summary>
    /// Переиндексация НОР.
    /// </summary>
    [Public]
    public virtual void ReindexBusinessUnits()
    {
      Logger.Debug("ElasticsearchReindex. ReindexBusinessUnits. Start.");
      
      Logger.Debug("ElasticsearchReindex. ReindexBusinessUnits. Recreate index...");
      var indexName = Commons.PublicFunctions.Module.GetIndexName(BusinessUnits.Info.Name);
      var synonyms = Commons.PublicFunctions.Module.GetLegalFormSynonyms();
      Commons.PublicFunctions.Module.ElasticsearchCreateIndex(indexName, string.Format(Constants.BusinessUnit.ElasticsearchIndexConfig, synonyms));
      
      long lastId = 0;
      while (true)
      {
        var businessUnits = BusinessUnits.GetAll(l => l.Id > lastId)
          .OrderBy(l => l.Id)
          .Take(Commons.PublicConstants.Module.MaxQueryIds)
          .ToList();
        
        if (!businessUnits.Any())
          break;
        
        lastId = businessUnits.Last().Id;
        Logger.DebugFormat("ElasticsearchReindex. ReindexBusinessUnits. Indexing businessunits, id from {0} to {1}...",
                           businessUnits.First().Id, lastId);
        
        var jsonStrings = businessUnits
          .Select(x => string.Format("{0}{1}{2}", Commons.PublicConstants.Module.BulkOperationIndexToTarget,
                                     Environment.NewLine,
                                     Company.Functions.BusinessUnit.GetIndexingJson(x)));

        var bulkJson = string.Format("{0}{1}", string.Join(Environment.NewLine, jsonStrings), Environment.NewLine);
        
        Commons.PublicFunctions.Module.ElasticsearchBulk(indexName, bulkJson);
      }
      Logger.Debug("ElasticsearchReindex. ReindexBusinessUnits. Finish.");
    }
    
    /// <summary>
    /// Переиндексация сотрудников.
    /// </summary>
    [Public]
    public virtual void ReindexEmployees()
    {
      Logger.Debug("ElasticsearchReindex. ReindexEmployees. Start.");
      
      Logger.Debug("ElasticsearchReindex. ReindexEmployees. Recreate index...");
      var indexName = Commons.PublicFunctions.Module.GetIndexName(Employees.Info.Name);
      Commons.PublicFunctions.Module.ElasticsearchCreateIndex(indexName, Constants.Employee.ElasticsearchIndexConfig);
      
      long lastId = 0;
      while (true)
      {
        var employees = Employees.GetAll(l => l.Id > lastId)
          .OrderBy(l => l.Id)
          .Take(Commons.PublicConstants.Module.MaxQueryIds)
          .ToList();
        
        if (!employees.Any())
          break;
        
        lastId = employees.Last().Id;
        Logger.DebugFormat("ElasticsearchReindex. ReindexEmployees. Indexing employees, id from {0} to {1}...",
                           employees.First().Id, lastId);
        
        var jsonStrings = employees
          .Select(x => string.Format("{0}{1}{2}", Commons.PublicConstants.Module.BulkOperationIndexToTarget,
                                     Environment.NewLine,
                                     Company.Functions.Employee.GetIndexingJson(x)));

        var bulkJson = string.Format("{0}{1}", string.Join(Environment.NewLine, jsonStrings), Environment.NewLine);
        
        Commons.PublicFunctions.Module.ElasticsearchBulk(indexName, bulkJson);
      }
      Logger.Debug("ElasticsearchReindex. ReindexEmployees. Finish.");
    }
    
    /// <summary>
    /// Обновить синонимы в индексе НОР.
    /// </summary>
    /// <param name="synonyms">Список синонимов.</param>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual void UpdateBusinessUnitsIndexSynonyms(string synonyms)
    {
      if (Commons.PublicFunctions.Module.IsElasticsearchEnabled())
      {
        Logger.Debug("Company. UpdateBusinessUnitsIndexSynonyms start.");
        
        var businessUnitIndexName = Commons.PublicFunctions.Module.GetIndexName(BusinessUnits.Info.Name);
        synonyms = Commons.PublicFunctions.Module.SynonymsParse(synonyms);
        var businessUnitIndexConfig = string.Format(Constants.BusinessUnit.ElasticsearchIndexConfig, synonyms);
        
        Commons.PublicFunctions.Module.ElasticsearchCloseIndex(businessUnitIndexName);
        Commons.PublicFunctions.Module.ElasticsearchUpdateIndexSettings(businessUnitIndexName, businessUnitIndexConfig);
        Commons.PublicFunctions.Module.ElasticsearchOpenIndex(businessUnitIndexName);
        
        Logger.Debug("Company. UpdateBusinessUnitsIndexSynonyms finish.");
      }
    }
    #endregion
    
    #region Передача прав
    
    /// <summary>
    /// Получить информацию о сотруднике.
    /// </summary>
    /// <param name="id">Id сотрудника.</param>
    /// <returns>Информация о сотруднике.</returns>
    public virtual string GetEmployeeInfo(long id)
    {
      var employee = Employees.GetAll(e => e.Id == id).FirstOrDefault();
      if (employee != null)
        return string.Format("{0} (Id {1})", employee.Person?.ShortName, employee.Id);
      return string.Empty;
    }
    
    /// <summary>
    /// Запустить передачу прав от закрытого сотрудника руководителю.
    /// </summary>
    /// <param name="employeeId">Идентификатор сотрудника.</param>
    /// <param name="managerId">Идентификатор руководителя.</param>
    /// <returns>Результат выполнения команды или текст ошибки.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual string TransferSubstitutedAccessRights(long employeeId, long managerId)
    {
      var loggerPrefix = "TransferSubstitutedAccessRightsCommand. ";
      var errorMessage = string.Empty;
      try
      {
        var sessionId = this.TryGetActiveTransferAccessRightsSession(employeeId, managerId);
        if (string.IsNullOrEmpty(sessionId))
        {
          this.ValidateEmployeeBeforeTransferAccessRights(employeeId);
          this.ValidateManagerBeforeTransferAccessRights(managerId);
          this.ValidateSubstitutionBeforeTransferAccessRights(employeeId, managerId);
          
          sessionId = this.RunTransferSubstitutedAccessRights(employeeId, managerId, loggerPrefix);
          this.ExecuteCheckTransferAccessRightsAsyncHandler(sessionId, loggerPrefix);
        }
        else
        {
          var message = loggerPrefix + string.Format("Access rights transfer has already been started. Session Id {0}" +
                                                     " for employee with id = {1} and manager with id = {2}.", sessionId, employeeId, managerId);
          Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Debug(message);
          errorMessage = "Access rights transfer has already been started.";
        }
      }
      catch (Exception ex)
      {
        var message = loggerPrefix + string.Format("Error message: {0}", ex.Message);
        Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Error(message);
        errorMessage = ex.Message;
      }
      return this.GetTransferMessageResult(employeeId, managerId, errorMessage);
    }

    /// <summary>
    /// Запустить передачу прав закрытых сотрудников руководителя.
    /// </summary>
    /// <param name="managerId">ИД руководителя.</param>
    /// <param name="isAgent">Флаг вызова из фонового процесса.</param>
    /// <param name="loggerPrefix">Префикс логгера.</param>
    /// <returns>Результат запуска передачи прав.</returns>
    public virtual string TransferManagerAccessRights(long managerId, bool isAgent, string loggerPrefix)
    {
      var loggerMessage = string.Empty;
      try
      {
        this.ValidateManagerBeforeTransferAccessRights(managerId);
        
        var totalSubstitutions = this.GetSubstitutionsForTransferAccessRights(managerId, loggerPrefix);
        var failedNumberOfSubstitutions = 0;
        var successedNumberOfSubstitutions = 0;
        var runningSessionsCount = 0;
        var butchSize = 0;
        if (!isAgent)
        {
          butchSize = Docflow.PublicFunctions.Module.Remote.GetDocflowParamsIntegerValue(Docflow.PublicConstants.Module.BulkTransferAccesRightsSessionBatchSizeParamName);
          butchSize = butchSize > 0 ? butchSize : Docflow.PublicConstants.Module.BulkTransferAccesRightsSessionBatchSize;
        }
        var substitutionsForProcessing = isAgent ? totalSubstitutions : totalSubstitutions.Take(butchSize);
        foreach (var substitution in substitutionsForProcessing)
        {
          try
          {
            var sessionId = this.TryGetActiveTransferAccessRightsSession(substitution.User.Id, managerId);
            if (!string.IsNullOrEmpty(sessionId))
            {
              runningSessionsCount++;
              loggerMessage = loggerPrefix + string.Format("Access rights transfer has already been started. " +
                                                           "Session Id {0} for employee with id = {1} and manager with id = {2}.",
                                                           sessionId, substitution.User.Id, substitution.Substitute.Id);
              Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Debug(loggerMessage);
              continue;
            }
            
            this.ValidateEmployeeBeforeTransferAccessRights(substitution.User.Id);
            this.ValidateSubstitutionBeforeTransferAccessRights(substitution.User.Id, substitution.Substitute.Id);
            
            sessionId = this.RunTransferSubstitutedAccessRights(substitution.User.Id, substitution.Substitute.Id, loggerPrefix);

            loggerMessage = loggerPrefix +
              string.Format("Transfer access rights for manager = {0} and employee = {1} is ready. SesionId = {2}",
                            substitution.Substitute.Id, substitution.User.Id, sessionId);
            Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Debug(loggerMessage);
            
            this.ExecuteCheckTransferAccessRightsAsyncHandler(sessionId, loggerPrefix);
            successedNumberOfSubstitutions++;
          }
          catch (Exception ex)
          {
            loggerMessage = loggerPrefix +
              string.Format("Transfer access rights for manager = {0} and employee = {1} failed. Error: {2}",
                            substitution.Substitute.Id, substitution.User.Id, ex.Message);
            if (isAgent)
              Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Debug(loggerMessage);
            else
              Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Error(loggerMessage);
            failedNumberOfSubstitutions++;
          }
        }
        return this.GetBulkTransferMessageResult(managerId, successedNumberOfSubstitutions, failedNumberOfSubstitutions, totalSubstitutions.Count);
      }
      catch (Exception ex)
      {
        var errorMessage = loggerPrefix + string.Format("Error*Error message: {0}", ex.Message);
        if (!isAgent)
        {
          Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Error(errorMessage);
          errorMessage = "Error*Errors occurred while transfer access rights. Read the log files for more information";
        }
        return errorMessage;
      }
    }
    
    /// <summary>
    /// Запустить передачу прав от всех закрытых сотрудников поздразделения выбранному руководителю подразделения.
    /// </summary>
    /// <param name="managerId">Идентификатор руководителя.</param>
    /// <returns>Результат выполнения команды или текст ошибки.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual string BulkTransferSubstitutedAccessRights(long managerId)
    {
      var loggerPrefix = "BulkTransferSubstitutedAccessRightsCommand. ";
      return this.TransferManagerAccessRights(managerId, false, loggerPrefix);
    }

    /// <summary>
    /// Получить результат передачи прав от всех закрытых сотрудников поздразделения выбранному руководителю подразделения.
    /// </summary>
    /// <param name="managerId">Идентификатор руководителя.</param>
    /// <param name="processedSubstitutionsCount">Количество обработанных замещений.</param>
    /// <param name="failedNumberOfSubstitutions">Количество ошибок при обработке замещений.</param>
    /// <param name="totalSubstitutionsCount">Общее количество замещений.</param>
    /// <returns>Результат передачи прав.</returns>
    public virtual string GetBulkTransferMessageResult(long managerId, int processedSubstitutionsCount, int failedNumberOfSubstitutions, int totalSubstitutionsCount)
    {
      // Вывод результата с указанием типа сообщения.
      var manager = Employees.GetAll().Where(e => e.Id == managerId).FirstOrDefault();
      var managerShortName = Functions.Employee.GetShortName(manager, false);
      if (processedSubstitutionsCount == 0)
        return string.Format("Warning*There are no active system substitutions found for employee {0} (Id {1}) among other employees.", managerShortName, managerId);
      
      if (failedNumberOfSubstitutions > 0 && failedNumberOfSubstitutions == processedSubstitutionsCount)
        return string.Format("Error*Errors occurred while transfer access rights. Read the log files for more information.|To: {0}, Id {1}", managerShortName, managerId);
      
      var employeesInfo = totalSubstitutionsCount == failedNumberOfSubstitutions + processedSubstitutionsCount ?
        string.Format("{0} substituted employees", processedSubstitutionsCount.ToString())
        : string.Format("{0} substituted employees of {1}", processedSubstitutionsCount, totalSubstitutionsCount);
      if (failedNumberOfSubstitutions > 0 && failedNumberOfSubstitutions < processedSubstitutionsCount)
        return string.Format("Success*Access rights transfer started.|From: {0}|To: {1}, Id {2}|Error*Errors occurred while transfer access rights. Read the log files for more information.|System substitution will be closed after access rights are transferred.", employeesInfo, managerShortName, managerId);

      return string.Format("Success*Access rights transfer started.|From: {0}|To: {1}, Id {2}|System substitution will be closed after access rights are transferred.", employeesInfo, managerShortName, managerId);
    }
    
    /// <summary>
    /// Получить результат передачи прав от сотрудника руководителю.
    /// </summary>
    /// <param name="employeeId">Идентификатор сотрудника.</param>
    /// <param name="managerId">Идентификатор руководителя.</param>
    /// <param name="errorMessage">Сообщение с ошибкой.</param>
    /// <returns>Результат передачи прав.</returns>
    public virtual string GetTransferMessageResult(long employeeId, long managerId, string errorMessage)
    {
      var message = string.Empty;
      var managerInfo = this.GetEmployeeInfo(managerId);
      var employeeInfo = this.GetEmployeeInfo(employeeId);
      var usersInfoMessagePart = string.Empty;
      if (managerInfo != string.Empty && employeeInfo != string.Empty)
        usersInfoMessagePart = string.Format("|From: {0}|To: {1}", employeeInfo, managerInfo);

      if (string.IsNullOrEmpty(errorMessage))
        message = string.Format("Success*Access rights transfer started.{0}|System substitution will be closed after access rights are transferred.", usersInfoMessagePart);
      else if (errorMessage.Contains("Not found active system substitutions for employee with id"))
        message = string.Format("Warning*Manager {0} does not have an active system substitution for employee {1}.", managerInfo, employeeInfo);
      else if (errorMessage.Contains("Access rights transfer has already been started."))
        message = string.Format("Error*{0}{1}", errorMessage, usersInfoMessagePart);
      else
        message = "Error*Errors occurred while transfer access rights. Read the log files for more information." + usersInfoMessagePart;
      return message;

    }
    
    /// <summary>
    /// Получить список замещений для переноса прав.
    /// </summary>
    /// <param name="managerId">Идентификатор руководителя.</param>
    /// <param name="loggerPrefix">Префикс логгера.</param>
    /// <returns>Список замещений, которые подходят для переноса прав.</returns>
    public virtual List<ISubstitution> GetSubstitutionsForTransferAccessRights(long managerId, string loggerPrefix)
    {
      var bufferDaysParam = Docflow.PublicFunctions.Module.Remote.GetDocflowParamsIntegerValue(Docflow.PublicConstants.Module.SubstitutionAccessRightsGrantBufferDaysCountParamName);
      var bufferDays = bufferDaysParam > 0 ? bufferDaysParam : Docflow.PublicConstants.Module.SubstitutionAccessRightsGrantBufferDaysCount;
      var transferAccesRightsEndDate = Calendar.Now.AddDays(bufferDays);
      
      var processingSessionsEmployeeIds = Company.AccessRightsTransferSessions.GetAll(ts => ts.SubstituteId == managerId).Select(ts => ts.EmployeeId);
      var substitutions = Substitutions.GetAll()
        .Where(s => s.Substitute.Id == managerId &&
               !processingSessionsEmployeeIds.Contains(s.User.Id) &&
               s.IsSystem == true &&
               s.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
               s.User.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed &&
               Employees.Is(s.User) &&
               s.EndDate.HasValue &&
               s.EndDate <= transferAccesRightsEndDate)
        .OrderBy(g => g.EndDate)
        .ToList();
      
      if (!substitutions.Any())
      {
        var logMessage = loggerPrefix + string.Format("Substitutions for transfer access rights not found for managerId = {0}", managerId);
        Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Debug(logMessage);
      }
      else
      {
        var selectedSubstitutionIdUserId = substitutions.ToDictionary(a => a.Id, a => a.User.Id);
        var selectedString = string.Join(Environment.NewLine, selectedSubstitutionIdUserId);
        var logMessage = loggerPrefix + string.Format("For managerId = {0} selected substitutions [SubstitutionId, EmployeeId]: {1}", managerId, selectedString);
        Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Debug(logMessage);
      }
      return substitutions;
    }
    
    /// <summary>
    /// Получить сессию передачи прав.
    /// </summary>
    /// <param name="employeeId">Идентификатор сотрудника.</param>
    /// <param name="managerId">Идентификатор руководителя.</param>
    /// <returns>ИД запущенной сессии или пустую строку.</returns>
    public virtual string TryGetActiveTransferAccessRightsSession(long employeeId, long managerId)
    {
      var session = AccessRightsTransferSessions.GetAll().Where(s => s.EmployeeId == employeeId && s.SubstituteId == managerId);
      if (session.Any())
        return session.First().SessionId;
      return string.Empty;
    }

    /// <summary>
    /// Проверить наличие несистемных замещений.
    /// </summary>
    /// <param name="employeeId">Идентификатор сотрудника.</param>
    /// <param name="managerId">Идентификатор руководителя.</param>
    public virtual void ValidateSubstitutionBeforeTransferAccessRights(long employeeId, long managerId)
    {
      var activeSubstitutionsQuery = Substitutions.GetAll()
        .Where(s => s.Substitute.Id == managerId &&
               s.User.Id == employeeId &&
               s.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
      
      var nonSystemSubstitutionsQuery = activeSubstitutionsQuery.Where(s => s.IsSystem != true);
      if (nonSystemSubstitutionsQuery.Any())
        throw AppliedCodeException.Create(string.Format(
          "Found active non system substitution for employee with id = {0} and manager with id = {1}.",
          employeeId, managerId));

      var systemSubstitutionsQuery = activeSubstitutionsQuery.Where(s => s.IsSystem == true);
      if (!systemSubstitutionsQuery.Any())
        throw AppliedCodeException.Create(string.Format(
          "Not found active system substitutions for employee with id = {0} and manager with id = {1}.",
          employeeId, managerId));
    }

    /// <summary>
    /// Проверить сотрудника перед передачей прав по замещению.
    /// </summary>
    /// <param name="employeeId">Идентификатор руководителя.</param>
    public virtual void ValidateEmployeeBeforeTransferAccessRights(long employeeId)
    {
      var employee = Employees.GetAll().Where(e => e.Id == employeeId).FirstOrDefault();
      if (employee == null)
        throw AppliedCodeException.Create(string.Format("Employee with ID {0} not found.", employeeId));

      if (employee.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed)
        throw AppliedCodeException.Create(string.Format(
          "The employee's databook record with ID {0} is active. Select an employee whose record is closed.",
          employeeId));
    }

    /// <summary>
    /// Проверить руководителя перед передачей прав по замещению.
    /// </summary>
    /// <param name="managerId">Идентификатор руководителя.</param>
    public virtual void ValidateManagerBeforeTransferAccessRights(long managerId)
    {
      var manager = Employees.GetAll().Where(e => e.Id == managerId).FirstOrDefault();
      if (manager == null)
        throw AppliedCodeException.Create(string.Format("Manager with ID {0} not found.", managerId));

      if (manager.Status != Sungero.CoreEntities.DatabookEntry.Status.Active)
        throw AppliedCodeException.Create(string.Format(
          "The manager's databook record with ID {0} is closed. Select a manager whose record is active.",
          managerId));
    }

    /// <summary>
    /// Запустить асинхронный обработчик, который следит за завершением сессии передача прав.
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии.</param>
    /// <param name="loggerPrefix">Префикс логгера.</param>
    public virtual void ExecuteCheckTransferAccessRightsAsyncHandler(string sessionId, string loggerPrefix)
    {
      var checkHandler = Sungero.Company.AsyncHandlers.CheckTransferSubstitutedAccessRights.Create();
      checkHandler.SessionId = sessionId;
      checkHandler.ExecuteAsync();
      var loggerMessage = loggerPrefix +
        string.Format("ExecuteCheckCopyAccessRightsAsyncHandler Started for SessionId: '{0}'.", sessionId);
      Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Debug(loggerMessage);
    }
    
    /// <summary>
    /// Начать сессию передачи прав по замещению.
    /// </summary>
    /// <param name="employeeId">Идентификатор сотрудника.</param>
    /// <param name="managerId">Идентификатор руководителя.</param>
    /// <param name="loggerPrefix">Префикс логгера.</param>
    /// <returns>Идентификатор сессии.</returns>
    public virtual string RunTransferSubstitutedAccessRights(long employeeId, long managerId, string loggerPrefix)
    {
      var employee = Employees.GetAll().Where(e => e.Id == employeeId).FirstOrDefault();
      var manager = Employees.GetAll().Where(e => e.Id == managerId).FirstOrDefault();
      var sessionId = AccessRights.CopyAsync(employee, manager);

      if (sessionId == Guid.Empty || sessionId == null)
        throw new Exception(string.Format("Can't add transfer rights to queue from employeeId = {0} to managerId = {1}. " +
                                          "SessionID is empty.", employeeId, managerId));
      
      var session = AccessRightsTransferSessions.Create();
      session.SessionId = sessionId.ToString();
      session.EmployeeId = employeeId;
      session.SubstituteId = managerId;
      session.Save();

      var loggerMessage = loggerPrefix +
        string.Format("Transfer rights from {0} to {1} added to queue. Session ID = {2}", employeeId, managerId, sessionId);
      Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Debug(loggerMessage);
      return sessionId.ToString();
    }
    
    /// <summary>
    /// Проверить состояние сессии передачи прав по замещению.
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии.</param>
    /// <param name="loggerPrefix">Префикс логгера.</param>
    /// <returns>Объект, содержащий информацию о статусе процесса копирования прав.</returns>
    public virtual Sungero.Core.AccessRightsCopyingStatus? GetAccessRightsSessionStatus(Guid sessionId, string loggerPrefix)
    {
      Sungero.Core.AccessRightsCopyingStatus? status;
      status = AccessRights.CopyingStatus(sessionId);
      
      var loggerMessage = loggerPrefix + "Get access rights copying status.";
      Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Debug(loggerMessage);
      
      return status;
    }

    /// <summary>
    /// Отменить передачу прав для сотрудника.
    /// </summary>
    /// <param name="employeeId">Идентификатор сотрудника.</param>
    public virtual void CancelTransferRightsForEmployee(long employeeId)
    {
      var loggerMessageFirstPart = string.Format("CancelTransferRightsForEmployee. EmployeeId={0}. ", employeeId);
      
      var sessionsQuery = AccessRightsTransferSessions.GetAll().Where(s => s.EmployeeId == employeeId);
      
      this.CancelTransferRights(sessionsQuery, loggerMessageFirstPart);
    }
    
    /// <summary>
    /// Отменить передачу прав.
    /// </summary>
    /// <param name="sessionsQuery">Запрос на выборку подходящих сессий.</param>
    /// <param name="loggerMessageFirstPart">Первая часть сообщения для логирования.</param>
    public virtual void CancelTransferRights(IQueryable<IAccessRightsTransferSession> sessionsQuery, string loggerMessageFirstPart)
    {
      if (sessionsQuery.Any())
      {
        foreach (var session in sessionsQuery.ToList())
        {
          try
          {
            AccessRights.CancelCopy(Guid.Parse(session.SessionId));
            var loggerMessage = string.Format(loggerMessageFirstPart + "SessionId={0} canceled.", session.SessionId);
            Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Debug(loggerMessage);
          }
          catch (Exception ex)
          {
            var loggerMessage = string.Format(loggerMessageFirstPart + "SessionId={0}. Canceling failed. ErrorMsg={1}",
                                              session.SessionId, ex.Message);
            Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Error(loggerMessage);
          }
        }
      }
      else
        Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix)
          .Debug(loggerMessageFirstPart + "Transfer sessions not found.");
      
    }
    
    /// <summary>
    /// Записать в историю сотрудника информацию о факте передачи прав.
    /// </summary>
    /// <param name="session">Сессия.</param>
    /// <param name="logMessageFirstPart">Первая часть сообщения логирования.</param>
    /// <returns>True - если запись в историю добавилась или не требуется, иначе - false.</returns>
    public virtual bool TryWriteEmployeeTransferAccessRightsHistory(IAccessRightsTransferSession session,
                                                                    string logMessageFirstPart)
    {
      var employee = Employees.GetAll(s => s.Id == session.EmployeeId).FirstOrDefault();
      var manager = Employees.GetAll(s => s.Id == session.SubstituteId).FirstOrDefault();
      if (employee != null && manager != null)
      {
        if (!Locks.TryLock(employee))
        {
          var loggerMessage = string.Format(logMessageFirstPart + "Could not update history. Employee (id={0}) is locked.",
                                            session.EmployeeId);
          Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix).Debug(loggerMessage);
          return false;
        }

        var operation = new Enumeration(Constants.Module.TransferAccessRights);
        var comment = Company.Functions.Employee.GetShortName(manager, false);
        employee.History.Write(operation, operation, comment);
        employee.Save();
        
        if (Locks.GetLockInfo(employee).IsLocked)
          Locks.Unlock(employee);
      }
      return true;
    }
    
    /// <summary>
    /// Проверить, что сессия существует.
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии.</param>
    /// <param name="logMessageFirstPart">Первая часть сообщения логирования.</param>
    /// <returns>True - если сессия существует, иначе - false.</returns>
    public virtual bool SessionExists(string sessionId, string logMessageFirstPart)
    {
      var sessionExists = AccessRightsTransferSessions.GetAll().Where(a => a.SessionId == sessionId).Any();
      if (!sessionExists)
      {
        Logger.WithLogger(Constants.Module.SubstitutionsAccessRightsTransferLoggerPostfix)
          .Debug(logMessageFirstPart + "Transfer session not found.");
        return false;
      }
      return true;
    }
    
    #endregion
  }
}