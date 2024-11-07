using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Client;

namespace Sungero.Company.Client
{
  public class ModuleFunctions
  {
    #region Обложка

    /// <summary>
    /// Показать настройки видимости организационной структуры.
    /// </summary>
    [LocalizeFunction("ShowVisibilitySettingsFunctionName", "ShowVisibilitySettingsFunctionDescription")]
    public virtual void ShowVisibilitySettings()
    {
      if (!VisibilitySettings.AccessRights.CanUpdate())
      {
        Dialogs.ShowMessage(Resources.VisibilitySettingsNotAvailable);
        return;
      }
      
      Functions.Module.Remote.GetVisibilitySettings().Show();
    }
    
    /// <summary>
    /// Создать и показать карточку сотрудника.
    /// </summary>
    [LocalizeFunction("CreateEmployeeFunctionName", "")]
    public virtual void CreateEmployee()
    {
      Functions.Module.Remote.CreateEmployee().Show();
    }

    /// <summary>
    /// Создать и показать карточку нашей организации.
    /// </summary>
    [LocalizeFunction("CreateBusinessUnitFunctionName", "")]
    public virtual void CreateBusinessUnit()
    {
      Functions.Module.Remote.CreateBusinessUnit().Show();
    }

    /// <summary>
    /// Создать и показать карточку подразделения.
    /// </summary>
    [LocalizeFunction("CreateDepartmentFunctionName", "")]
    public virtual void CreateDepartment()
    {
      Functions.Module.Remote.CreateDepartment().Show();
    }

    #endregion
    
    /// <summary>
    /// Получить все действующие несистемные группы и роли.
    /// </summary>
    /// <returns>Действующие несистемные группы и роли.</returns>
    [Public, LocalizeFunction("GetAllActiveNoSystemGroupsFunctionName", "GetAllActiveNoSystemGroupsFunctionDescription")]
    public static IQueryable<IRecipient> GetAllActiveNoSystemGroups()
    {
      // Dmitriev_IA: Для Desktop-клиента не отрабатывает серверная фильтрация IQueryable запроса. Bug 98921.
      var systemRecipientsSid = PublicFunctions.Module.GetSystemRecipientsSidWithoutAllUsers(true);
      systemRecipientsSid.Add(Sungero.Domain.Shared.SystemRoleSid.AllUsers);
      return Functions.Module.Remote.GetAllRecipients()
        .Where(x => x.Status == CoreEntities.DatabookEntry.Status.Active &&
               !systemRecipientsSid.Contains(x.Sid.Value) && Groups.Is(x));
    }

  }
}