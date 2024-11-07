using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyCore.PowerOfAttorneyServiceApp;

namespace Sungero.PowerOfAttorneyCore.Client
{
  partial class PowerOfAttorneyServiceAppActions
  {
    public virtual void SetAPIKey(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var dialog = Dialogs.CreateInputDialog(Sungero.PowerOfAttorneyCore.PowerOfAttorneyServiceApps.Resources.InputAPIKey);
      var apiKey = dialog.AddPasswordString(_obj.Info.Properties.APIKey.LocalizedName, true);
      apiKey.MaxLength(Constants.PowerOfAttorneyServiceApp.ApiKeyMaxLength);
      
      var saveButton = dialog.Buttons.AddCustom(PowerOfAttorneyServiceApps.Resources.Save);
      dialog.Buttons.AddCancel();
      dialog.Buttons.Default = saveButton;
      
      dialog.SetOnButtonClick(
        x =>
        {
          if (x.Button == saveButton && x.IsValid)
          {
            _obj.APIKey = apiKey.Value;
          }
        });
      dialog.Show();
    }

    public virtual bool CanSetAPIKey(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate();
    }

  }

}