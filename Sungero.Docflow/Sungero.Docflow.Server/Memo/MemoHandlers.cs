using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Memo;

namespace Sungero.Docflow
{
  partial class MemoCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      
      var isManyAddressees = _source.IsManyAddressees.HasValue && _source.IsManyAddressees.Value;
      if (!isManyAddressees && _source.Addressee != null && _source.Addressee.Status == Company.Employee.Status.Closed)
      {
        e.Without(_info.Properties.Addressee);
        e.Without(_info.Properties.Addressees);
      }
      
      var containsClosedAddressees = _source.Addressees.Any(a => a.Addressee.Status == Company.Employee.Status.Closed);
      if (isManyAddressees && containsClosedAddressees)
      {
        var activeAddressees = _source.Addressees.Where(a => a.Addressee.Status == Company.Employee.Status.Active).ToArray();
        e.Map(_info.Properties.Addressees, activeAddressees);
      }
    }
  }

  partial class MemoAddresseesDepartmentPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> AddresseesDepartmentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Addressee == null)
        return query;
      
      return query.Where(x => x.RecipientLinks.Any(r => Equals(r.Member, _obj.Addressee)));
    }
  }

  partial class MemoOurSignatoryPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> OurSignatoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query;
    }
  }

  partial class MemoServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      // Пропуск выполнения обработчика в случае отсутствия прав на изменение, например при выдаче прав на чтение пользователем, который сам имеет права на чтение.
      if (!_obj.AccessRights.CanUpdate())
        return;

      // Заполнить адресата на главной первым адресатом из коллекции для отображения в списке.
      if (_obj.IsManyAddressees == true)
        Functions.Memo.FillAddresseeFromAddressees(_obj);
      
      Functions.Memo.SetManyAddresseesLabel(_obj);

      var addresseesLimit = Functions.Memo.GetAddresseesLimit(_obj);
      if (_obj.Addressees.Count > addresseesLimit)
        e.AddError(Memos.Resources.TooManyAddresseesFormat(addresseesLimit));
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (_obj.IsManyAddressees == null)
        _obj.IsManyAddressees = false;
      
      _obj.State.Properties.ManyAddresseesPlaceholder.IsEnabled = false;
      
      // Заполнить "Подписал".
      var employee = Company.Employees.Current;
      if (_obj.OurSignatory == null)
        _obj.OurSignatory = employee;
    }
  }
}