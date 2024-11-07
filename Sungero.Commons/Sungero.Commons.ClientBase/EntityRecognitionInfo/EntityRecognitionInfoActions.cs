using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.EntityRecognitionInfo;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons.Client
{
  partial class EntityRecognitionInfoActions
  {
    public virtual void ShowDocument(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var document = Functions.EntityRecognitionInfo.Remote.GetDocument(_obj);
      if (document != null)
        document.ShowModal();
      else
        Dialogs.NotifyMessage(EntityRecognitionInfos.Resources.NoRightsToDocument);
    }

    public virtual bool CanShowDocument(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.EntityId.HasValue;
    }

    public override void DeleteEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.DeleteEntity(e);
    }

    public override bool CanDeleteEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var isUserAdministrator = Users.Current.IncludedIn(Roles.Administrators);
      return isUserAdministrator && base.CanDeleteEntity(e);
    }

  }

}