using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalAssignment;

namespace Sungero.Docflow.Client
{
  partial class ApprovalAssignmentFunctions
  {
    /// <summary>
    /// Показывать сводку по документу.
    /// </summary>
    /// <returns>True, если в задании нужно показывать сводку по документу.</returns>
    [Public]
    public virtual bool NeedViewDocumentSummary()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document == null)
        return false;
      
      return Docflow.PublicFunctions.OfficialDocument.NeedViewDocumentSummary(document);
    }
    
    /// <summary>
    /// Показать диалог переадресации.
    /// </summary>
    /// <returns>True, если запрос был подтвержден.</returns>
    public virtual bool ShowForwardingDialog()
    {
      var excludedPerformers = Functions.ApprovalAssignment.Remote.GetAllCurrentIterationApprovers(_obj)
        .Select(x => Recipients.As(x)).ToList();
      excludedPerformers.Add(_obj.Performer);
      
      var dialogResult = Docflow.PublicFunctions.Module.ShowForwardDialog(excludedPerformers);
      if (dialogResult.ForwardButtonIsPressed)
      {
        _obj.Addressee = dialogResult.ForwardTo;
        return true;
      }
      return false;
    }
    
  }
}