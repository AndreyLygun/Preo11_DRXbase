using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyCore.PowerOfAttorneyClassifierGroup;

namespace Sungero.PowerOfAttorneyCore
{
  partial class PowerOfAttorneyClassifierGroupCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      e.Without(_info.Properties.ExternalGuid);
      e.Without(_info.Properties.Autokey);
      e.Without(_info.Properties.Code);
    }
  }

  partial class PowerOfAttorneyClassifierGroupParentPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ParentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(f => !f.Equals(_obj));
    }
  }

  partial class PowerOfAttorneyClassifierGroupServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed)
        return;
          
      var allDuplicates = Functions.PowerOfAttorneyClassifierGroup.GetDuplicates(_obj, true);
      if (allDuplicates.Any())
      {
        var firstDuplicate = allDuplicates.OrderByDescending(x => x.Id).First();
        var firstDuplicateShortName = firstDuplicate.DisplayValue.Length > 50 ?
          firstDuplicate.DisplayValue.Substring(0, 50) :
          firstDuplicate.DisplayValue;
        e.AddError(PowerOfAttorneyClassifierGroups.Resources.DuplicateFoundFormat(firstDuplicateShortName), _obj.Info.Actions.ShowDuplicates);
      }
    }

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      if (_obj.ExternalGuid != null)
        e.AddError(PowerOfAttorneyClassifierGroups.Resources.LoadedFromClassifier);
    }
  }

}