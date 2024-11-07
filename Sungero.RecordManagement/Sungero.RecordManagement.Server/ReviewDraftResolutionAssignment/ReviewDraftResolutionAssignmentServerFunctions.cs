using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewDraftResolutionAssignment;

namespace Sungero.RecordManagement.Server
{
  partial class ReviewDraftResolutionAssignmentFunctions
  {
    /// <summary>
    /// Построить модель представления.
    /// </summary>
    /// <returns>Xml представление контрола состояние.</returns>
    [Remote(IsPure = true)]
    [LocalizeFunction("GetStateViewFunctionName", "GetStateViewFunctionDescription")]
    public Sungero.Core.StateView GetStateView()
    {
      return Functions.Module.GetStateViewForDraftResolution(_obj.ResolutionGroup.ActionItemExecutionTasks.ToList());
    }
  }
}