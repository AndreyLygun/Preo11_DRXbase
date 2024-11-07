using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.DocflowApproval.AdvancedAssignment;

namespace Sungero.DocflowApproval.Client
{
  partial class AdvancedAssignmentFunctions
  {
    #region Проверки перед выполнением задания
    
    /// <summary>
    /// Валидация задания перед выполнением.
    /// </summary>
    /// <param name="eventArgs">Аргументы действия.</param>
    /// <returns>True - если ошибок нет, иначе - False.</returns>
    public virtual bool ValidateBeforeComplete(Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var isValid = true;
      if (Functions.AdvancedAssignment.AreDocumentsLockedByMe(_obj))
      {
        eventArgs.AddError(Resources.SaveDocumentsBeforeComplete);
        isValid = false;
      }
      
      return isValid;
    }
    
    /// <summary>
    /// Валидация задания перед переадресацией.
    /// </summary>
    /// <param name="eventArgs">Аргументы действия.</param>
    /// <returns>True - если ошибок нет, иначе - False.</returns>
    public virtual bool ValidateBeforeForward(Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      if (Functions.AdvancedAssignment.AreDocumentsLockedByMe(_obj))
      {
        eventArgs.AddError(Resources.SaveDocumentsBeforeComplete);
        return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Валидация задания перед отправкой на доработку.
    /// </summary>
    /// <param name="eventArgs">Аргументы действия.</param>
    /// <returns>True - если ошибок нет, иначе - False.</returns>
    public virtual bool ValidateBeforeRework(Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var isValid = true;
      if (!Functions.Module.ValidateBeforeRework(_obj, eventArgs))
        isValid = false;
      
      if (Functions.AdvancedAssignment.AreDocumentsLockedByMe(_obj))
      {
        eventArgs.AddError(Resources.SaveDocumentsBeforeComplete);
        isValid = false;
      }
      
      // Валидация заполненности ответственного за доработку.
      if (_obj.State.Properties.ReworkPerformer.IsVisible && _obj.ReworkPerformer == null)
      {
        eventArgs.AddError(DocflowApproval.Resources.CantSendForReworkWithoutPerformer);
        isValid = false;
      }
      
      return isValid;
    }
    
    #endregion
    
    /// <summary>
    /// Показать диалог переадресации.
    /// </summary>
    /// <returns>True, если запрос был подтвержден.</returns>
    public virtual bool ShowForwardingDialog()
    {
      var notAvailablePerformers = Functions.AdvancedAssignment.Remote.GetActiveAndFutureAssignmentsPerformers(_obj).ToList();
      var dialogResult = Docflow.PublicFunctions.Module.ShowForwardDialog(notAvailablePerformers);
      
      if (dialogResult.ForwardButtonIsPressed)
      {
        _obj.ForwardTo = dialogResult.ForwardTo;
        return true;
      }
      
      return false;
    }
  }
}