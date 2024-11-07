using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Person;

namespace Sungero.Parties
{
  partial class PersonSharedHandlers
  {

    public virtual void MiddleNameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // Установить пол, если он не был заполнен при открытии карточки.
      if (!_obj.State.Properties.Sex.OriginalValue.HasValue)
        _obj.Sex = Functions.Person.GetGender(_obj);

      Functions.Person.FillName(_obj);
    }

    public virtual void FirstNameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      Functions.Person.FillName(_obj);
    }

    public virtual void LastNameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // Установить пол, если он не был заполнен при открытии карточки.
      if (!_obj.State.Properties.Sex.OriginalValue.HasValue)
        _obj.Sex = Functions.Person.GetGender(_obj);
      
      Functions.Person.FillName(_obj);
    }
  }
}