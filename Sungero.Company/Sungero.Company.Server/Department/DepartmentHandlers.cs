using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Company;
using Sungero.Company.Department;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class DepartmentUiFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.UiFilteringEventArgs e)
    {
      query = base.Filtering(query, e);
      if (Functions.Module.IsRecipientRestrict())
        query = Functions.Department.RestrictDepartments(query).Cast<T>();
      
      return query;
    }
  }

  partial class DepartmentCreatingFromServerHandler
  {
    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      // Отменить заполнение сотрудников.
      e.Without(_source.Info.Properties.RecipientLinks);
    }
  }

  partial class DepartmentHeadOfficePropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> HeadOfficeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      // Фильтровать головное подразделение по нашим организациям.
      if (_obj.BusinessUnit != null)
        query = query.Where(d => d.BusinessUnit.Equals(_obj.BusinessUnit));
      
      return query.Where(d => !Equals(d, _obj));
    }
  }

  partial class DepartmentServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // При исключении работника из подразделения проверить, что оно для него не основное.
      var deletedEmployees = _obj.State.Properties.RecipientLinks.Deleted.Select(x => x.Member);
      var changedEmployees = _obj.State.Properties.RecipientLinks.Changed.Select(x => x.Member);
      var changedOriginalEmployees = _obj.State.Properties.RecipientLinks.Changed
        .Select(x => x.State.Properties.Member.OriginalValue)
        .Where(x => x != null);
      var removedFromDepartmentEmployees = deletedEmployees
        .Union(changedOriginalEmployees)
        .Except(changedEmployees);
      // Отфильтровать сотрудников, находящихся в процессе удаления.
      // TODO Zamerov нужен нормальный признак IsDeleted, 50908
      removedFromDepartmentEmployees = removedFromDepartmentEmployees.Where(x => !(x as Sungero.Domain.Shared.IChangeTracking).ChangeTracker.IsDeleted);
      foreach (var employee in removedFromDepartmentEmployees)
      {
        if (Equals(employee.Department, _obj) &&
            !_obj.RecipientLinks.Any(x => Equals(x.Member, employee)))
        {
          e.AddError(Departments.Resources.YouCantDeleteEmployeeOflastDivision);
          return;
        }
      }
      
      // Проверить код подразделения на пробелы, если свойство изменено.
      if (!string.IsNullOrEmpty(_obj.Code))
      {
        // При изменении кода e.AddError сбрасывается.
        var codeIsChanged = _obj.State.Properties.Code.IsChanged;
        _obj.Code = _obj.Code.Trim();
        
        if (codeIsChanged && Regex.IsMatch(_obj.Code, @"\s"))
          e.AddError(_obj.Info.Properties.Code, Company.Resources.NoSpacesInCode);
      }
    }

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      Functions.Department.SynchronizeManagerInRole(_obj);
      
      // Актуализировать системные замещения при сохранении подразделения.
      this.UpdateSystemSubstitutions();
    }

    public override void Deleting(Sungero.Domain.DeletingEventArgs e)
    {
      if (_obj.Manager == null)
        return;
      
      if (_obj.RecipientLinks.Any())
      {
        var members = _obj.RecipientLinks.Select(r => r.Member).ToList().Select(m => Users.As(m)).Where(m => m != null).ToList();
        Functions.Module.DeleteUnnecessarySystemSubstitutionsTransaction(members, _obj.Manager);
      }
      
      if (_obj.HeadOffice != null && _obj.HeadOffice.Manager != null)
        Functions.Module.DeleteUnnecessarySystemSubstitutionsTransaction(new[] { _obj.Manager }, _obj.HeadOffice.Manager);
      
      if (_obj.HeadOffice == null && _obj.BusinessUnit != null && _obj.BusinessUnit.CEO != null)
        Functions.Module.DeleteUnnecessarySystemSubstitutionsTransaction(new[] { _obj.Manager }, _obj.BusinessUnit.CEO);
      Functions.Department.SynchronizeManagerInRole(_obj);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      var activeBusinessUnits = Company.BusinessUnits.GetAll().Where(oc => oc.Status == CoreEntities.DatabookEntry.Status.Active);
      
      if (activeBusinessUnits.Count() == 1)
        _obj.BusinessUnit = activeBusinessUnits.FirstOrDefault();
    }

    #region Работа с замещениями

    public virtual void UpdateSystemSubstitutions()
    {
      // Если статус подразделения Закрытая, при этом статус не менялся или подразделение только что создано,
      // то создавать\удалять замещения не надо.
      if (_obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed &&
          (!_obj.State.Properties.Status.IsChanged && Equals(_obj.State.Properties.Status.OriginalValue, _obj.Status) ||
           _obj.State.IsInserted))
        return;

      var systemSubstitutionsForCreate = new List<Structures.Module.Substitution>();
      var systemSubstitutionsForDelete = new List<Structures.Module.Substitution>();
      
      // Статус подразделения Действующая и не менялся.
      if (_obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
          !_obj.State.Properties.Status.IsChanged &&
          Equals(_obj.State.Properties.Status.OriginalValue, _obj.Status))
      {
        // Обработка системных замещений при изменении руководителя.
        if (_obj.State.Properties.Manager.IsChanged && !Equals(_obj.State.Properties.Manager.OriginalValue, _obj.Manager))
        {
          this.CollectToCreateSystemSubstitutionsForChangedManager(systemSubstitutionsForCreate);
          this.CollectToDeleteSystemSubstitutionsForChangedManager(systemSubstitutionsForDelete);
        }
        
        // Обработка системных замещений при изменении состава подразделения.
        if (_obj.State.Properties.RecipientLinks.IsChanged && 
            (_obj.State.Properties.RecipientLinks.Changed.Any() || _obj.State.Properties.RecipientLinks.Deleted.Any()))
        {
          this.CollectToCreateSystemSubstitutionsForChangedDepartmentStructure(systemSubstitutionsForCreate);
          this.CollectToDeleteSystemSubstitutionsForChangedDepartmentStructure(systemSubstitutionsForDelete);
        }
        
        // Обработка системных замещений при изменении головного подразделения.
        if (_obj.State.Properties.HeadOffice.IsChanged && !Equals(_obj.State.Properties.HeadOffice.OriginalValue, _obj.HeadOffice))
        {
          this.CollectToCreateSystemSubstitutionsForChangedHeadOffice(systemSubstitutionsForCreate);
          this.CollectToDeleteSystemSubstitutionsForChangedHeadOffice(systemSubstitutionsForDelete);
        }
        
        // Обработка системных замещений при изменении НОР.
        if (_obj.State.Properties.BusinessUnit.IsChanged && !Equals(_obj.State.Properties.BusinessUnit.OriginalValue, _obj.BusinessUnit))
        {
          this.CollectToCreateSystemSubstitutionsForChangedBusinessUnit(systemSubstitutionsForCreate);
          this.CollectToDeleteSystemSubstitutionsForChangedBusinessUnit(systemSubstitutionsForDelete);
        }
      }
      
      // Подразделение стало действующим или оно только что созданное и действующее.
      if (_obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
          _obj.State.Properties.Status.IsChanged &&
          !Equals(_obj.State.Properties.Status.OriginalValue, _obj.Status))
      {
        this.CollectToCreateSystemSubstitutionsForChangedManager(systemSubstitutionsForCreate);
        this.CollectToCreateSystemSubstitutionsForChangedDepartmentStructure(systemSubstitutionsForCreate);
        this.CollectToCreateSystemSubstitutionsForChangedHeadOffice(systemSubstitutionsForCreate);
        this.CollectToCreateSystemSubstitutionsForChangedBusinessUnit(systemSubstitutionsForCreate);
      }
      
      // Подразделение стало закрытым.
      if (_obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed &&
          _obj.State.Properties.Status.IsChanged &&
          !Equals(_obj.State.Properties.Status.OriginalValue, _obj.Status))
      {
        this.CollectToDeleteSystemSubstitutionsForChangedManager(systemSubstitutionsForDelete);
        this.CollectToDeleteSystemSubstitutionsForChangedDepartmentStructure(systemSubstitutionsForDelete);
        this.CollectToDeleteSystemSubstitutionsForChangedHeadOffice(systemSubstitutionsForDelete);
        this.CollectToDeleteSystemSubstitutionsForChangedBusinessUnit(systemSubstitutionsForDelete);
      }
      
      if (systemSubstitutionsForCreate.Any() || systemSubstitutionsForDelete.Any())
        this.CommitSystemSubstitutions(systemSubstitutionsForCreate, systemSubstitutionsForDelete);
    }
    
    /// <summary>
    /// Собрать коллекцию системных замещений для их создания при изменении руководителя подразделения.
    /// </summary>
    /// <param name="systemSubstitutionsForCreate">Создаваемые замещения.</param>
    public virtual void CollectToCreateSystemSubstitutionsForChangedManager(List<Structures.Module.Substitution> systemSubstitutionsForCreate)
    {
      if (_obj.Manager == null)
        return;
      
      if (_obj.RecipientLinks.Any())
      {
        var members = _obj.RecipientLinks.Select(r => r.Member).ToList().Select(m => Users.As(m)).Where(m => m != null).ToList();
        
        // Создание замещений для нового руководителя.
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                 _obj.Manager,
                                                 members);
      }
      
      // Создание замещений на руководителей дочерних подразделений.
      var childDepartmentManagers = Departments.GetAll()
        .Where(d => d.HeadOffice.Equals(_obj))
        .Where(d => d.Status != Sungero.Company.Department.Status.Closed)
        .Select(d => d.Manager)
        .Where(m => m != null).ToList();
      
      if (childDepartmentManagers.Any())
      {
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                 _obj.Manager,
                                                 childDepartmentManagers);
      }
      
      // Создание замещения руководителя головного подразделения.
      if (_obj.HeadOffice != null &&
          _obj.HeadOffice.Status != Sungero.Company.Department.Status.Closed &&
          _obj.HeadOffice.Manager != null)
      {
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                 _obj.HeadOffice.Manager,
                                                 new[] { _obj.Manager });
      }
      
      // Создание замещения руководителя НОР.
      if (_obj.HeadOffice == null &&
          _obj.BusinessUnit != null && 
          _obj.BusinessUnit.Status != Sungero.Company.BusinessUnit.Status.Closed && 
          _obj.BusinessUnit.CEO != null)
      {
        
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                 _obj.BusinessUnit.CEO,
                                                 new[] { _obj.Manager });
      }
    }

    /// <summary>
    /// Собрать коллекцию системных замещений для их удаления при изменении руководителя подразделения.
    /// </summary>
    /// <param name="systemSubstitutionsForDelete">Удаляемые замещения.</param>
    public virtual void CollectToDeleteSystemSubstitutionsForChangedManager(List<Structures.Module.Substitution> systemSubstitutionsForDelete)
    {
      if (_obj.State.Properties.Manager.OriginalValue == null)
        return;
            
      if (_obj.RecipientLinks.Any())
      {
        var members = _obj.RecipientLinks.Select(r => r.Member).ToList().Select(m => Users.As(m)).Where(m => m != null).ToList();
        
        // Удаление замещений существующего руководителя.
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                 _obj.State.Properties.Manager.OriginalValue,
                                                 members);
      }
      
      // Удаление замещений на руководителей дочерних подразделений.
      var childDepartmentManagers = Departments.GetAll()
        .Where(d => d.HeadOffice.Equals(_obj))
        .Select(d => d.Manager)
        .Where(m => m != null)
        .ToList();
      
      if (childDepartmentManagers.Any())
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                 _obj.State.Properties.Manager.OriginalValue,
                                                 childDepartmentManagers);
      
      // Удаление замещения руководителя головного подразделения.
      if (_obj.HeadOffice != null &&
          _obj.HeadOffice.Manager != null)
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                 _obj.HeadOffice.Manager,
                                                 new[] { _obj.State.Properties.Manager.OriginalValue });
      
      // Удаление замещения руководителя НОР.
      if (_obj.HeadOffice == null &&
          _obj.BusinessUnit != null && 
          _obj.BusinessUnit.CEO != null)
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                 _obj.BusinessUnit.CEO,
                                                 new[] { _obj.State.Properties.Manager.OriginalValue });
    }
    
    /// <summary>
    /// Собрать коллекцию системных замещений для их создания при изменении состава подразделения.
    /// </summary>
    /// <param name="systemSubstitutionsForCreate">Создаваемые замещения.</param>
    public virtual void CollectToCreateSystemSubstitutionsForChangedDepartmentStructure(List<Structures.Module.Substitution> systemSubstitutionsForCreate)
    {
      if (_obj.Manager == null)
        return;
      
      if (_obj.State.Properties.RecipientLinks.Added.Any())
      {
        var addedMembers = _obj.State.Properties.RecipientLinks.Added.Select(r => r.Member).ToList().Select(m => Users.As(m))
          .Where(m => m != null).ToList();
        
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                 _obj.Manager,
                                                 addedMembers);
      }
      
      var changedRecipientLinks = _obj.State.Properties.RecipientLinks.Changed.Where(r => !r.State.IsInserted).ToList();
      
      if (changedRecipientLinks.Any())
      {
        var addedMembers = changedRecipientLinks.Select(r => r.Member).ToList().Select(m => Users.As(m))
          .Where(m => m != null).ToList();

        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                 _obj.Manager,
                                                 addedMembers);
      }
    }
    
    /// <summary>
    /// Собрать коллекцию системных замещений для их удаления при изменении состава подразделения.
    /// </summary>
    /// <param name="systemSubstitutionsForDelete">Удаляемые замещения.</param>
    public virtual void CollectToDeleteSystemSubstitutionsForChangedDepartmentStructure(List<Structures.Module.Substitution> systemSubstitutionsForDelete)
    {
      if (_obj.State.Properties.Manager.OriginalValue == null)
        return;
      
      if (_obj.State.Properties.RecipientLinks.Deleted.Any())
      {
        var deletedMembers = _obj.State.Properties.RecipientLinks.Deleted.Select(r => r.Member).ToList().Select(m => Users.As(m))
          .Where(m => m != null).ToList();
        
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                 _obj.State.Properties.Manager.OriginalValue,
                                                 deletedMembers);
      }
      
      var changedRecipientLinks = _obj.State.Properties.RecipientLinks.Changed.Where(r => !r.State.IsInserted).ToList();
      
      if (changedRecipientLinks.Any())
      {
        var deletedMembers = changedRecipientLinks.Select(r => r.State.Properties.Member.OriginalValue).ToList().Select(m => Users.As(m))
          .Where(m => m != null).ToList();
        
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                 _obj.State.Properties.Manager.OriginalValue,
                                                 deletedMembers);
      }
    }
    
    /// <summary>
    /// Собрать коллекцию системных замещений для их создания при изменении головного подразделения.
    /// </summary>
    /// <param name="systemSubstitutionsForCreate">Создаваемые замещения.</param>
    public virtual void CollectToCreateSystemSubstitutionsForChangedHeadOffice(List<Structures.Module.Substitution> systemSubstitutionsForCreate)
    {
      if (_obj.Manager == null)
        return;
      
      // Создание замещений для руководителя нового головного подразделения.
      if (_obj.HeadOffice != null && 
          _obj.HeadOffice.Status != Sungero.Company.Department.Status.Closed &&
          _obj.HeadOffice.Manager != null)
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                 _obj.HeadOffice.Manager,
                                                 new[] { _obj.Manager });

      // Создание замещений для руководителя НОР.
      if (_obj.HeadOffice == null &&
          _obj.BusinessUnit != null && 
          _obj.BusinessUnit.Status != Sungero.Company.BusinessUnit.Status.Closed && 
          _obj.BusinessUnit.CEO != null)
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                 _obj.BusinessUnit.CEO,
                                                 new[] { _obj.Manager });
    }

    /// <summary>
    /// Собрать коллекцию системных замещений для их удаления при изменении головного подразделения.
    /// </summary>
    /// <param name="systemSubstitutionsForDelete">Удаляемые замещения.</param>
    public virtual void CollectToDeleteSystemSubstitutionsForChangedHeadOffice(List<Structures.Module.Substitution> systemSubstitutionsForDelete)
    {
      if (_obj.State.Properties.Manager.OriginalValue == null)
        return;
      
      // Удаление замещений руководителя старого головного подразделения.
      if (_obj.State.Properties.HeadOffice.OriginalValue != null &&
          _obj.State.Properties.HeadOffice.OriginalValue.Manager != null)
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                 _obj.State.Properties.HeadOffice.OriginalValue.Manager,
                                                 new[] { _obj.State.Properties.Manager.OriginalValue });
      
      // Удаление замещений для руководителя НОР.
      if (_obj.HeadOffice != null &&
          _obj.State.Properties.HeadOffice.OriginalValue == null &&
          _obj.BusinessUnit != null &&
          _obj.BusinessUnit.CEO != null)
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                 _obj.BusinessUnit.CEO,
                                                 new[] { _obj.State.Properties.Manager.OriginalValue });
    }
    
    /// <summary>
    /// Собрать коллекцию системных замещений для их создания при изменении НОР.
    /// </summary>
    /// <param name="systemSubstitutionsForCreate">Создаваемые замещения.</param>
    public virtual void CollectToCreateSystemSubstitutionsForChangedBusinessUnit(List<Structures.Module.Substitution> systemSubstitutionsForCreate)
    {
      // Создание замещений для руководителя новой НОР при изменении НОР.
      if (_obj.HeadOffice == null &&
          _obj.Manager != null &&
          _obj.BusinessUnit != null && 
          _obj.BusinessUnit.Status != Sungero.Company.BusinessUnit.Status.Closed &&
          _obj.BusinessUnit.CEO != null)
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForCreate,
                                                 _obj.BusinessUnit.CEO,
                                                 new[] { _obj.Manager });
    }
    
    /// <summary>
    /// Собрать коллекцию системных замещений для их удаления при изменении НОР.
    /// </summary>
    /// <param name="systemSubstitutionsForDelete">Удаляемые замещения.</param>
    public virtual void CollectToDeleteSystemSubstitutionsForChangedBusinessUnit(List<Structures.Module.Substitution> systemSubstitutionsForDelete)
    {
      // Удаление замещений руководителя старой НОР при изменении НОР.
      if (_obj.State.Properties.HeadOffice.OriginalValue == null &&
          _obj.State.Properties.Manager.OriginalValue != null &&
          _obj.State.Properties.BusinessUnit.OriginalValue != null &&
          _obj.State.Properties.BusinessUnit.OriginalValue.CEO != null)
        this.UpdateSystemSubstitutionsCollection(systemSubstitutionsForDelete,
                                                 _obj.State.Properties.BusinessUnit.OriginalValue.CEO,
                                                 new[] { _obj.State.Properties.Manager.OriginalValue });
    }
    
    /// <summary>
    /// Обновление списка системных замещений.
    /// </summary>
    /// <param name="currentSystemSubstitutions">Текущий список системных замещений.</param>
    /// <param name="user">Замещающий.</param>
    /// <param name="substitutedUsers">Замещаемый пользователь.</param>
    public virtual void UpdateSystemSubstitutionsCollection(List<Structures.Module.Substitution> currentSystemSubstitutions,
                                                     IUser user, IEnumerable<IUser> substitutedUsers)
    {
      foreach (var substituted in substitutedUsers)
        currentSystemSubstitutions.Add(Structures.Module.Substitution.Create(user, substituted));
    }

    /// <summary>
    /// Сохранить изменения системных замещений.
    /// </summary>
    /// <param name="systemSubstitutionsForCreate">Список для создания системных замещений.</param>
    /// <param name="systemSubstitutionsForDelete">Список для удаления системных замещений.</param>
    public virtual void CommitSystemSubstitutions(IEnumerable<Structures.Module.Substitution> systemSubstitutionsForCreate,
                                           IEnumerable<Structures.Module.Substitution> systemSubstitutionsForDelete)
    {
      foreach (var element in systemSubstitutionsForDelete.Except(systemSubstitutionsForCreate).Distinct())
        Sungero.Company.Functions.Module.DeleteUnnecessarySystemSubstitutionTransaction(element.SubstitutedUser, element.User);
      
      foreach (var element in systemSubstitutionsForCreate.Distinct())
        Sungero.Company.Functions.Module.CreateSystemSubstitution(element.SubstitutedUser, element.User);
    }

    #endregion
  }
}