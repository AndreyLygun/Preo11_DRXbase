using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SimpleDocument;

namespace Sungero.Docflow
{
  partial class SimpleDocumentServerHandlers
  {

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      base.Saving(e);
      
      // Удалить связь с ведущим документом.
      int leadingDocumentId;
      if (e.Params.TryGetValue(Constants.Module.LeadingDocumentIdParamName, out leadingDocumentId))
      {
        var leadingDocument = OfficialDocuments.Get(leadingDocumentId);
        _obj.Relations.RemoveFrom(Constants.Module.AddendumRelationName, leadingDocument);
        _obj.Relations.Save();
        e.Params.Remove(Constants.Module.LeadingDocumentIdParamName);
      }
    }
  }

  partial class SimpleDocumentConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      // Очистить статус LifeCycleState, значения которого нет в целевом документе - ограничение платформы.
      if (!PublicFunctions.OfficialDocument.IsSupportedLifeCycleState(_source))
        e.Without(_info.Properties.LifeCycleState);
    }
    
  }
}