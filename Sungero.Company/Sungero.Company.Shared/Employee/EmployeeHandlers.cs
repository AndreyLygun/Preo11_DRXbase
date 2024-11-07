using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.Employee;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class EmployeeSharedHandlers
  {

    public override void SignatoryInfoObtaining(Sungero.Domain.Shared.SignatoryInfoObtainingEventArgs e)
    {
      var result = _obj.JobTitle != null ? _obj.JobTitle.Name.Trim() : string.Empty;
      
      if (_obj.Department != null)
      {
        var department = string.IsNullOrEmpty(_obj.Department.ShortName) ? _obj.Department.Name : _obj.Department.ShortName;
        
        if (string.IsNullOrEmpty(result))
          result = department;
        else
          result = string.Format("{0} ({1})", result, department);
      }
      
      e.AdditionalInfo = result + "|";
    }

    public virtual void NeedNotifyAssignmentsSummaryChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      Sungero.Company.PublicFunctions.Employee.SetRequiredProperties(_obj);
    }

    public virtual void DepartmentChanged(Sungero.Company.Shared.EmployeeDepartmentChangedEventArgs e)
    {
      if ((e.NewValue == null || !Equals(e.NewValue, e.OldValue)) && _obj.JobTitle != null &&
          _obj.JobTitle.Department != null && !Equals(_obj.JobTitle.Department, e.NewValue))
      {
        _obj.JobTitle = null;
      }
    }

    public virtual void JobTitleChanged(Sungero.Company.Shared.EmployeeJobTitleChangedEventArgs e)
    {
      var jobTitle = e.NewValue;
      if (jobTitle != null && _obj.Department == null && jobTitle.Department != null)
        _obj.Department = jobTitle.Department;
    }

    public virtual void PersonChanged(Sungero.Company.Shared.EmployeePersonChangedEventArgs e)
    {
      Functions.Employee.UpdateName(_obj, e.NewValue);
    }

    public virtual void NeedNotifyNewAssignmentsChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      Sungero.Company.PublicFunctions.Employee.SetRequiredProperties(_obj);
    }

    public virtual void NeedNotifyExpiredAssignmentsChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      Sungero.Company.PublicFunctions.Employee.SetRequiredProperties(_obj);
    }
  }
}