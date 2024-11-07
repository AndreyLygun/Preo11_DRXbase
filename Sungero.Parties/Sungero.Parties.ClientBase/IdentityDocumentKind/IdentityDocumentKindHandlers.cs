using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.IdentityDocumentKind;

namespace Sungero.Parties
{
  partial class IdentityDocumentKindClientHandlers
  {

    public virtual void IdentityAuthorityCodePatternValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (e.NewValue != null)
        e.NewValue = e.NewValue.Trim();
    }

    public virtual void IdentityNumberPatternValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (e.NewValue != null)
        e.NewValue = e.NewValue.Trim();
    }

    public virtual void IdentitySeriesPatternValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (e.NewValue != null)
        e.NewValue = e.NewValue.Trim();
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      _obj.State.Properties.IdentitySeriesPattern.IsEnabled = _obj.SpecifyIdentitySeries == true;
      _obj.State.Properties.IdentityAuthorityCodePattern.IsEnabled = _obj.SpecifyIdentityAuthorityCode == true;
    }

  }
}