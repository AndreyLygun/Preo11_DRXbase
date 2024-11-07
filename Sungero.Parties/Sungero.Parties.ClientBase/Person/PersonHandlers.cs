using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Person;

namespace Sungero.Parties
{
  partial class PersonClientHandlers
  {

    public virtual void IdentityAuthorityCodeValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (!string.IsNullOrEmpty(e.NewValue))
      {
        var errorMessage = Functions.Person.GetIdentityAuthorityCodeFormatValidationError(_obj, e.NewValue);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.IdentityAuthorityCode, errorMessage);
      }
    }

    public virtual void IdentitySeriesValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (!string.IsNullOrEmpty(e.NewValue))
      {
        var errorMessage = Functions.Person.GetIdentitySeriesFormatValidationError(_obj, e.NewValue);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.IdentitySeries, errorMessage);
      }
    }

    public virtual void IdentityNumberValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (!string.IsNullOrEmpty(e.NewValue))
      {
        var errorMessage = Functions.Person.GetIdentityNumberFormatValidationError(_obj, e.NewValue);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.IdentityNumber, errorMessage);
      }
    }

    public virtual void IdentityKindValueInput(Sungero.Parties.Client.PersonIdentityKindValueInputEventArgs e)
    {
      if (e.NewValue != e.OldValue)
        Functions.Person.CleanDisabledIdentityProperties(_obj, e.NewValue);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      Functions.Person.ChangeIdentityPropertiesAccess(_obj);
      Functions.Person.SetRequiredIdentityProperties(_obj);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      
      if (Functions.Person.CheckCanEditIdentityDocuments(_obj))
      {
        _obj.State.Pages.IdentityDocument.IsVisible = true;
      }
    }
  }
}