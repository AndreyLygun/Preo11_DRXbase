using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorneyRevocation;

namespace Sungero.Docflow.Client
{
  partial class PowerOfAttorneyRevocationFunctions
  {
    
    /// <summary>
    /// Показать диалог переформирования заявления на отзыв эл. доверенности.
    /// </summary>
    [Public]
    public virtual void ShowReCreateRevocationDialog()
    {
      var dialog = Dialogs.CreateInputDialog(Sungero.Docflow.PowerOfAttorneyRevocations.Resources.ReCreateRevocationDialogTitle);
      var reason = dialog.AddMultilineString(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.RevocationDialogReasonFieldName, true);
      reason.MaxLength(Constants.FormalizedPowerOfAttorney.FPoARevocationReasonMaxLength);
      var createButton = dialog.Buttons.AddCustom(Sungero.Docflow.PowerOfAttorneyRevocations.Resources.ReCreateRevocationDialogButtonName);
      dialog.Buttons.AddCancel();
      dialog.HelpCode = Constants.PowerOfAttorneyRevocation.ReCreatePowerOfAttorneyRevocationHelpCode;
      
      dialog.SetOnButtonClick(
        b =>
        {
          if (b.Button == createButton && b.IsValid)
          {
            // Дополнительная проверка, так как обязательное поле допускает заполнение пробелами.
            if (string.IsNullOrWhiteSpace(reason.Value))
            {
              b.AddError(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.FillRevocationReason);
            }
            else
            {
              bool hasError = false;
              
              try
              {
                hasError = !Functions.PowerOfAttorneyRevocation.Remote.ReCreateRevocation(_obj, reason.Value.Trim());
              }
              catch (Exception ex)
              {
                hasError = true;
                Logger.ErrorFormat("ShowReCreateRevocationDialog. Failed to create power of attorney revocation. Document Id {0}", ex, _obj.Id);
              }
              
              if (hasError)
                b.AddError(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.PowerOfAttorneyRevocationCreationFailed);
            }
          }
        });
      
      if (dialog.Show() == createButton)
      {
        Dialogs.NotifyMessage(Sungero.Docflow.PowerOfAttorneyRevocations.Resources.PowerOfAttorneyRevocationReCreationSuccess);
      }
    }

  }
}