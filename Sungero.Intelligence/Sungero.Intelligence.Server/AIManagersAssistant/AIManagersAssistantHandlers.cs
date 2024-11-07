using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Intelligence.AIManagersAssistant;

namespace Sungero.Intelligence
{
  partial class AIManagersAssistantCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.Classifiers);
      base.CreatingFrom(e);
    }
  }

  partial class AIManagersAssistantServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      if (!_obj.State.IsCopied)
      {
        _obj.PreparesActionItemDrafts = true;
        _obj.PreparesResolution = true;
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var isDuplicates = AIManagersAssistants.GetAll(x => !Equals(x, _obj) &&
                                                     Equals(x.Manager, _obj.Manager) &&
                                                     x.Status == Sungero.Intelligence.AIManagersAssistant.Status.Active).Any();
      if (isDuplicates)
        e.AddError(AIManagersAssistants.Resources.AIAssistantOnlyOne);
      
      if (_obj.PreparesActionItemDrafts != true && _obj.PreparesResolution != true)
        e.AddError(Sungero.Company.ManagersAssistants.Resources.NoAuthoritiesError);
      
      var maxLimit = Constants.AIManagersAssistant.MaxClassificationLimit;
      var wrongLimitSettings = _obj.Classifiers.Where(x => x.LowerClassificationLimit < 0 || x.LowerClassificationLimit > maxLimit);
      foreach (var item in wrongLimitSettings)
        e.AddError(item, _obj.Info.Properties.Classifiers.Properties.LowerClassificationLimit,
                   AIManagersAssistants.Resources.WrongClassificationLimitFormat(maxLimit),
                   new[] { _obj.Info.Properties.Classifiers.Properties.LowerClassificationLimit });
      
      base.BeforeSave(e);
    }
  }
}