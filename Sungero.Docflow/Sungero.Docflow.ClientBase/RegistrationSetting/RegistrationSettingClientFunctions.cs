using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RegistrationSetting;

namespace Sungero.Docflow.Client
{
  partial class RegistrationSettingFunctions
  {
    /// <summary>
    /// Получить указанные в настройке подразделения с незаполненным кодом.
    /// </summary>
    /// <returns>Подразделения с незаполненным кодом.</returns>
    public virtual System.Collections.Generic.IEnumerable<IDepartment> GetDepartmentsFromSettingWithNullCode()
    {
      return _obj.Departments.Select(d => d.Department)
        .Where(d => d.Status == Status.Active && d.Code == null);
    }
    
    /// <summary>
    /// Получить указанные в настройке наши организации с незаполненным кодом.
    /// </summary>
    /// <returns>Наши организации с незаполненным кодом.</returns>
    public virtual System.Collections.Generic.IEnumerable<IBusinessUnit> GetBusinessUnitsFromSettingWithNullCode()
    {
      return _obj.BusinessUnits.Select(b => b.BusinessUnit)
        .Where(b => b.Status == Status.Active && b.Code == null);
    }
    
    /// <summary>
    /// Получить указанные в настройке виды документов с незаполненным кодом.
    /// </summary>
    /// <returns>Виды документов с незаполненным кодом.</returns>
    public virtual System.Collections.Generic.IEnumerable<IDocumentKind> GetDocumentKindsFromSettingWithNullCode()
    {
      return _obj.DocumentKinds.Select(k => k.DocumentKind)
        .Where(k => k.Status == Status.Active && k.Code == null);
    }
  }
}