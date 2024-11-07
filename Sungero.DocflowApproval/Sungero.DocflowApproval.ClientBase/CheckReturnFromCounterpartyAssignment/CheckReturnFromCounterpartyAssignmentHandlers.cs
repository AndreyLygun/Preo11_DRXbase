using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.DocflowApproval.CheckReturnFromCounterpartyAssignment;

namespace Sungero.DocflowApproval
{
  partial class CheckReturnFromCounterpartyAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var needShowHint = false;
      if (e.Params.TryGetValue(Constants.Module.NeedShowNoRightsHintParamName, out needShowHint) && needShowHint)
        e.AddError(Docflow.Resources.NoRightsToDocument);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      // При первом обращении к вложениям они кэшируются с учетом прав на сущности,
      // последующие обращения, в том числе через AllowRead, работают с закешированными сущностями и правами.
      // Если первое обращение было через AllowRead, то последующий код будет работать так, будто есть права, и наоборот,
      // если кэширование было без прав на сущности, то в AllowRead вложений не получить.
      // Корректность доступных действий важнее функциональности ниже, поэтому обеспечиваем работу NeedRightsToMainDocument
      // с серверными вложениями, а не из кэша.
      // BUGS 319348, 320495.
      if (_obj.Status == Status.InProcess && Functions.CheckReturnFromCounterpartyAssignment.Remote.NeedRightsToMainDocument(_obj))
      {
        e.HideAction(_obj.Info.Actions.Signed);
        e.HideAction(_obj.Info.Actions.NotSigned);
        e.AddError(Docflow.Resources.NoRightsToDocument);
        e.Params.AddOrUpdate(Constants.Module.NeedShowNoRightsHintParamName, true);
      }
      
      var document = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      if (!Sungero.Docflow.OfficialDocuments.Is(document))
        return;
      
      var exchangeState = Sungero.Docflow.OfficialDocuments.As(document).ExchangeState;
      if (exchangeState == Sungero.Docflow.OfficialDocument.ExchangeState.Signed)
        e.HideAction(_obj.Info.Actions.NotSigned);
      
      if (exchangeState == Sungero.Docflow.OfficialDocument.ExchangeState.Rejected || 
          exchangeState == Sungero.Docflow.OfficialDocument.ExchangeState.Terminated || 
          exchangeState == Sungero.Docflow.OfficialDocument.ExchangeState.Obsolete)
      {
        e.HideAction(_obj.Info.Actions.Signed);
      }
      
    }
  }

}