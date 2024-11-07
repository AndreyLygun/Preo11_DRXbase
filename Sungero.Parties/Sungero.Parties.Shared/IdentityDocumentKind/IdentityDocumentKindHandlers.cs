using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.IdentityDocumentKind;

namespace Sungero.Parties
{
  partial class IdentityDocumentKindSharedHandlers
  {

    public virtual void SpecifyIdentityAuthorityCodeChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue != true)
        _obj.IdentityAuthorityCodePattern = null;
    }

    public virtual void SpecifyIdentitySeriesChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue != true)
        _obj.IdentitySeriesPattern = null;
    }

    public virtual void NameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(_obj.ShortName))
        _obj.ShortName = e.NewValue;
    }

  }
}