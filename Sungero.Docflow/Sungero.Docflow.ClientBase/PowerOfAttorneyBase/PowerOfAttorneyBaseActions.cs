using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorneyBase;

namespace Sungero.Docflow.Client
{
  partial class PowerOfAttorneyBaseActions
  {
    public virtual void ChangeManyRepresentatives(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.IsManyRepresentatives == true)
      {
        if (_obj.AgentType == null)
          _obj.AgentType = AgentType.Employee;
        _obj.IsManyRepresentatives = false;
        return;
      }
      
      _obj.IsManyRepresentatives = true;
      if (_obj.Representatives.Count == 0)
        Sungero.Docflow.Functions.PowerOfAttorneyBase.CopyAgentToRepresentatives(_obj);
      Dialogs.NotifyMessage(Sungero.Docflow.PowerOfAttorneyBases.Resources.FillRepresentativeListOnAgentsTab);
    }

    public virtual bool CanChangeManyRepresentatives(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var isRequisitiesChanging = false;
      e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.RepeatRegister, out isRequisitiesChanging);
      
      var canEditCard = _obj.AccessRights.CanUpdate() &&
        (Functions.Module.IsLockedByMe(_obj) || _obj.State.IsInserted);
      
      var isNotRegistered = _obj.RegistrationState == null || _obj.RegistrationState == RegistrationState.NotRegistered;
      var canChangeProperties = !_obj.HasVersions || isRequisitiesChanging || isNotRegistered;
      
      return canEditCard &&
        canChangeProperties &&
        _obj.LastVersionApproved != true;
    }
    
    public virtual void FindActiveSignatureSetting(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var signSettings = Functions.PowerOfAttorneyBase.Remote.GetActiveSignatureSettingsByPoA(_obj);
      signSettings.Show();
    }

    public virtual bool CanFindActiveSignatureSetting(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void FindSignatureSetting(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var signSettings = Functions.PowerOfAttorneyBase.Remote.GetSignatureSettingsByPoA(_obj);
      signSettings.Show();
    }

    public virtual bool CanFindSignatureSetting(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

  }

}