using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.DocflowApproval.CheckReturnFromCounterpartyAssignment;

namespace Sungero.DocflowApproval.Client
{
  partial class CheckReturnFromCounterpartyAssignmentFunctions
  {
    #region Проверки перед выполнением задания
    
    /// <summary>
    /// Валидация задания перед выполнением с результатом "Документ подписан контрагентом".
    /// </summary>
    /// <param name="eventArgs">Аргументы действия.</param>
    /// <returns>True - если ошибок нет, иначе - False.</returns>
    public virtual bool ValidateBeforeSigned(Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var isValid = true;
      if (Functions.CheckReturnFromCounterpartyAssignment.AreDocumentsLockedByMe(_obj))
      {
        eventArgs.AddError(Resources.SaveDocumentsBeforeComplete);
        isValid = false;
      }
      
      if (!Functions.CheckReturnFromCounterpartyAssignment.CanCompleteAssignment(_obj))
      {
        eventArgs.AddError(CheckReturnFromCounterpartyAssignments.Resources.CannotCompleteAssignmentByExchange);
        isValid = false;
      }
      
      return isValid;
    }
    
    /// <summary>
    /// Валидация задания перед выполнением с результатом "Документ не подписан контрагентом".
    /// </summary>
    /// <param name="eventArgs">Аргументы действия.</param>
    /// <returns>True - если ошибок нет, иначе - False.</returns>
    public virtual bool ValidateBeforeNotSigned(Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var isValid = true;
      if (Functions.CheckReturnFromCounterpartyAssignment.AreDocumentsLockedByMe(_obj))
      {
        eventArgs.AddError(Resources.SaveDocumentsBeforeComplete);
        isValid = false;
      }
      
      if (!Functions.CheckReturnFromCounterpartyAssignment.CanCompleteAssignment(_obj))
      {
        eventArgs.AddError(CheckReturnFromCounterpartyAssignments.Resources.CannotCompleteAssignmentByExchange);
        isValid = false;
      }
      
      return isValid;
    }
    
    #endregion

  }
}