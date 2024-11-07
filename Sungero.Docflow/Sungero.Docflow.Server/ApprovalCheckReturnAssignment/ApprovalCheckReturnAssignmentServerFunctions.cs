using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalCheckReturnAssignment;

namespace Sungero.Docflow.Server
{
  partial class ApprovalCheckReturnAssignmentFunctions
  {
    #region Контроль состояния
    
    /// <summary>
    /// Построить регламент.
    /// </summary>
    /// <returns>Регламент.</returns>
    [Remote(IsPure = true)]
    [LocalizeFunction("GetStagesStateViewFunctionName", "GetStagesStateViewFunctionDescription")]
    public Sungero.Core.StateView GetStagesStateView()
    {
      return PublicFunctions.ApprovalRuleBase.GetStagesStateView(_obj);
    }
    
    #endregion
    
    /// <summary>
    /// Продлить срок задания.
    /// </summary>
    /// <param name="newDeadline">Новый срок.</param>
    /// <returns>True - продление срока задания прошло успешно, False - неуспешно.</returns>
    public virtual bool ExtendAssignmentDeadline(DateTime newDeadline)
    {
      _obj.Deadline = newDeadline;
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
        Functions.OfficialDocument.ExtendTrackingDeadline(document, newDeadline, _obj.Task);
      return true;
    }
  }
}