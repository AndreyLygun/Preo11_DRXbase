using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.EntityRecognitionInfo;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons
{
  partial class EntityRecognitionInfoClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var isUserAdministrator = Users.Current.IncludedIn(Roles.Administrators);
      _obj.State.Properties.DocTypeClassifierTrainingStatus.IsEnabled = isUserAdministrator && !string.IsNullOrEmpty(_obj.VerifiedClass);
      _obj.State.Properties.FirstPageClassifierTrainingStatus.IsEnabled = isUserAdministrator && _obj.VerifiedVersionNumber.HasValue;
      
    }
  }
}