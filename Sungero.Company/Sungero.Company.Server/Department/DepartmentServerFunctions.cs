using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.Department;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Server
{
  partial class DepartmentFunctions
  {

    /// <summary>
    /// Получить удаляемых из подразделения сотрудников.
    /// </summary>
    /// <returns>Удаляемые сотрудники.</returns>
    public IQueryable<IEmployee> GetDeletedEmployees()
    {
      var employees = _obj.State.Properties.RecipientLinks.Deleted
        .Select(r => r.Member)
        .Where(m => m != null)
        .ToList();
      return Employees.GetAll().Where(r => employees.Contains(r));
    }

    /// <summary>
    /// Синхронизировать руководителя в роль "Руководители подразделений".
    /// </summary>
    /// <remarks>Не учитывает удаление подразделения.</remarks>
    public virtual void SynchronizeManagerInRole()
    {
      this.SynchronizeManagerInRole(false);
    }

    /// <summary>
    /// Синхронизировать руководителя в роль "Руководители подразделений".
    /// </summary>
    /// <param name="isDeleted">Признак удаления текущей сущности.</param>
    public virtual void SynchronizeManagerInRole(bool isDeleted)
    {
      var managerRole = Roles.GetAll(r => r.Sid == Constants.Module.DepartmentManagersRole).SingleOrDefault();
      if (managerRole == null)
        return;
      
      var originalManager = _obj.State.Properties.Manager.OriginalValue;
      var manager = _obj.Manager;
      var managerChanged = !Equals(originalManager, manager);
      if (manager != null && manager.IncludedIn(managerRole) && originalManager != null && 
          !managerChanged && _obj.State.Properties.Status.OriginalValue == _obj.Status && !isDeleted)
        return;
      
      // Добавить руководителя в роль "Руководители подразделений".
      var departmentClosed = _obj.Status == CoreEntities.DatabookEntry.Status.Closed;
      var ceoRole = Functions.Module.GetCEORole();
      var managerRoleRecipients = managerRole.RecipientLinks;
      if (manager != null && !departmentClosed && !manager.IncludedIn(managerRole) && !manager.IncludedIn(ceoRole))
        managerRoleRecipients.AddNew().Member = manager;

      // У ролей "Руководители организаций" и "Руководители подразделений" пересекаются права,
      // поэтому если руководитель входит в роль "Руководители организаций",
      // нужно удалить его из роли "Руководители подразделений".
      if (originalManager != null)
      {
        var originalManagerIsCeo = originalManager.IncludedIn(ceoRole);
        var originalManagerIsOtherDepartmentManager = Departments
          .GetAll(d => d.Status == CoreEntities.DatabookEntry.Status.Active &&
                  Equals(originalManager, d.Manager) &&
                  d.Id != _obj.Id)
          .Any();
        
        if ((isDeleted || departmentClosed || originalManagerIsCeo || managerChanged) &&
            !originalManagerIsOtherDepartmentManager)
        {
          while (managerRoleRecipients.Any(r => Equals(r.Member, originalManager)))
            managerRoleRecipients.Remove(managerRoleRecipients.First(r => Equals(r.Member, originalManager)));
        }
      }
    }
    
    /// <summary>
    /// Получить подразделения.
    /// </summary>
    /// <returns>Подразделения.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IDepartment> GetDepartments()
    {
      return Departments.GetAll();
    }
    
    /// <summary>
    /// Получить подразделение по ид.
    /// </summary>
    /// <param name="id">Ид подразделения.</param>
    /// <returns>Подразделение.</returns>
    [Remote(IsPure = true), Public]
    public static IDepartment GetDepartment(long id)
    {
      return Departments.GetAll().FirstOrDefault(d => d.Id == id);
    }
    
    /// <summary>
    /// Получить подразделения с незаполненным кодом.
    /// </summary>
    /// <returns>Подразделения с незаполненным кодом.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IDepartment> GetDepartmentsWithNullCode()
    {
      return Departments.GetAll().Where(d => d.Status == Status.Active && d.Code == null);
    }
    
    /// <summary>
    /// Получить подразделения с учётом видимости орг. структуры.
    /// </summary>
    /// <returns>Подразделения.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IDepartment> GetVisibleDepartments()
    {
      var allDepartments = Departments.GetAll();
      if (Functions.Module.IsRecipientRestrict())
        return RestrictDepartments(allDepartments);
      
      return allDepartments;
    }
    
    /// <summary>
    /// Ограничить список подразделений, оставив только доступные в режиме ограниченной видимости орг. структуры.
    /// </summary>
    /// <param name="departments">Список подразделений.</param>
    /// <returns>Только те подразделения из списка, которые доступны в режиме ограниченной видимости орг. структуры.</returns>
    public static IQueryable<IDepartment> RestrictDepartments(IQueryable<IDepartment> departments)
    {
      var visibleRecipientIds = Functions.Module.GetVisibleRecipientIds(Constants.Module.DepartmentTypeGuid);
      return departments.Where(c => visibleRecipientIds.Contains(c.Id));
    }
    
    /// <summary>
    /// Получить ИД подчиненных подразделений.
    /// </summary>
    /// <returns>ИД подчиненных подразделений.</returns>
    [Remote(IsPure = true), Public]
    public virtual List<long> GetSubordinateDepartmentIds()
    {
      var result = new List<long>();
      var subordinateDepartments = Departments.GetAll(x => !Equals(x, _obj) && Equals(x.HeadOffice, _obj)).ToList();
      result.AddRange(subordinateDepartments.Select(x => x.Id));
      
      foreach (var department in subordinateDepartments)
        // Вызов через Functions позволяет передать аргументом подразделение, для которого должна быть выполнена функция.
        result.AddRange(Functions.Department.GetSubordinateDepartmentIds(department));
      
      return result;
    }
    
    /// <summary>
    /// Получить подразделение из настроек сотрудника.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Подразделение.</returns>
    /// <remarks>Используется на слое.</remarks>
    [Public]
    public static Company.IDepartment GetDepartment(Company.IEmployee employee)
    {
      if (employee == null)
        return null;
      
      var department = Company.Departments.Null;
      var settings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(employee);
      if (settings != null)
        department = settings.Department;
      if (department == null)
        department = employee.Department;
      return department;
    }
  }
}