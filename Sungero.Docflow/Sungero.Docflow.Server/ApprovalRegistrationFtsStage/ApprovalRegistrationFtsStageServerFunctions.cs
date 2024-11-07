using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRegistrationFtsStage;

namespace Sungero.Docflow.Server
{
  partial class ApprovalRegistrationFtsStageFunctions
  {
    public override Sungero.Docflow.Structures.ApprovalFunctionStageBase.ExecutionResult Execute(IApprovalTask approvalTask)
    {
      if (!Sungero.Docflow.PublicFunctions.Module.IsPoAKonturLicenseEnable())
        return this.GetErrorResult(Sungero.Docflow.ApprovalRegistrationFtsStages.Resources.NoLicenseForRegisterToFts);
      
      Logger.DebugFormat("ApprovalRegistrationFtsStage. Start execute registration document in fts for task id: {0}, start id: {1}.", approvalTask.Id, approvalTask.StartId);
      
      var result = base.Execute(approvalTask);
      
      var mainDocument = approvalTask.DocumentGroup.OfficialDocuments.SingleOrDefault();
      
      Logger.DebugFormat("ApprovalRegistrationFtsStage. Try validate fpoa document before registration in FTS. Document Id {0}, task id {1}", mainDocument?.Id, approvalTask.Id);
      var validationError = Sungero.Docflow.PublicFunctions.Module.ValidatePoaDocumentBeforeRegistration(mainDocument);
      if (!string.IsNullOrEmpty(validationError))
      {
        Logger.DebugFormat("ApprovalRegistrationFtsStage. Fpoa document is invalid. Document Id {0}, task id {1}, validation error message: {2}",
                           mainDocument?.Id, approvalTask.Id, validationError);
        return this.GetErrorResult(validationError);
      }
      
      if (FormalizedPowerOfAttorneys.Is(mainDocument) &&
          Functions.FormalizedPowerOfAttorney.IsRevokedInService(FormalizedPowerOfAttorneys.As(mainDocument)))
      {
        Logger.DebugFormat("ApprovalRegistrationFtsStage. Fpoa document is already revoked in the service. Document Id {0}, task id {1}",
                           mainDocument?.Id, approvalTask.Id);
        return this.GetErrorResult(FormalizedPowerOfAttorneys.Resources.AlreadyRevoked);
      }
      
      try
      {
        Logger.DebugFormat("ApprovalRegistrationFtsStage. Try to send fpoa document for registration to FTS. Document Id {0}", mainDocument.Id);
        var sendingError = Sungero.Docflow.PublicFunctions.Module.SendPoaDocumentForRegistrationToFts(mainDocument, approvalTask.Id);
        if (!string.IsNullOrEmpty(sendingError))
        {
          Logger.DebugFormat("ApprovalRegistrationFtsStage. Failed to send fpoa document for registration to FTS. Document Id {0}, error message: {1}",
                             mainDocument.Id, sendingError);
          return this.GetErrorResult(sendingError);
        }
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("ApprovalRegistrationFtsStage. Registration in FTS error: {0}. Document Id {1}", ex, ex.Message, mainDocument.Id);
        return this.GetRetryResult(string.Empty);
      }
      
      Logger.DebugFormat("ApprovalRegistrationFtsStage. Done execute registration in FTS for task id {0}, document id: {1}", approvalTask.Id, mainDocument.Id);
      
      return result;
    }
    
    /// <summary>
    /// Проверить возможность регистрации в реестре ФНС.
    /// </summary>
    /// <param name="mainDocument">Документ.</param>
    /// <returns>Сообщение ошибки регистрации.</returns>
    [Obsolete("Метод не используется с 24.04.2024 и версии 4.10. Логика валидации перенесена в метод Sungero.Docflow.Shared.ModuleFunctions.ValidatePoaDocumentBeforeRegistration.")]
    public virtual string GetValidationErrorRegistrationFts(IOfficialDocument mainDocument)
    {
      return Sungero.Docflow.PublicFunctions.Module.ValidatePoaDocumentBeforeRegistration(mainDocument);
    }
    
    /// <summary>
    /// Получить результат отправки эл. доверенности/заявления на отзыв в реестр ФНС.
    /// </summary>
    /// <param name="mainDocument">Документ.</param>
    /// <param name="approvalTaskId">ИД задачи на согласование.</param>
    /// <returns>Результат отправки эл. доверенности/заявления на отзыв в реестр ФНС.</returns>
    [Obsolete("Метод не используется с 24.04.2024 и версии 4.10. Логика отправки на регистрацию перенесена в метод Sungero.Docflow.Shared.ModuleFunctions.SendPoaDocumentForRegistrationToFts.")]
    public virtual string GetSendingToFtsError(IOfficialDocument mainDocument, long approvalTaskId)
    {
      return Sungero.Docflow.PublicFunctions.Module.SendPoaDocumentForRegistrationToFts(mainDocument, approvalTaskId);
    }
    
    public override Sungero.Docflow.Structures.ApprovalFunctionStageBase.ExecutionResult CheckCompletionState(IApprovalTask approvalTask)
    {
      var result = base.CheckCompletionState(approvalTask);
      var mainDocument = approvalTask.DocumentGroup.OfficialDocuments.SingleOrDefault();
      
      if (FormalizedPowerOfAttorneys.Is(mainDocument) &&
          FormalizedPowerOfAttorneys.As(mainDocument).FtsListState == Sungero.Docflow.FormalizedPowerOfAttorney.FtsListState.Revoked)
        return this.GetErrorResult(FormalizedPowerOfAttorneys.Resources.FormalizedPowerOfAttorneySendForRegistrationError);
      
      var isFtsRegistrationComplete = Sungero.Docflow.PublicFunctions.Module.CheckPoaDocumentRegistrationIsComplete(mainDocument);
      if (!isFtsRegistrationComplete)
      {
        Logger.DebugFormat("ApprovalRegistrationFtsStage. Retry check registration in FTS. Approval task Id {0}, Document Id {1}.",
                           approvalTask.Id, mainDocument.Id);
        return this.GetRetryResult(string.Empty);
      }
      
      var ftsRegistrationError = Sungero.Docflow.PublicFunctions.Module.GetPoaDocumentFtsRegistrationError(mainDocument);
      if (string.IsNullOrEmpty(ftsRegistrationError))
      {
        Logger.DebugFormat("ApprovalRegistrationFtsStage. Registration in FTS is done. Approval task Id {0}, Document Id {1}.",
                           approvalTask.Id, mainDocument.Id);
        return this.GetSuccessResult();
      }
      else
      {
        Logger.DebugFormat("ApprovalRegistrationFtsStage. Registration error: {0}, Approval task Id {1}, Document Id {2}.",
                           ftsRegistrationError, approvalTask.Id, mainDocument.Id);
        return this.GetErrorResult(ftsRegistrationError);
      }
    }
    
    /// <summary>
    /// Проверить, что сервис дал ответ по регистрации документа в реестре ФНС.
    /// </summary>
    /// <param name="mainDocument">Документ.</param>
    /// <returns>True - статус регистрации в реестре ФНС "Зарегистрирован"/"Ошибка регистрации", иначе - false.</returns>
    [Obsolete("Метод не используется с 24.04.2024 и версии 4.10. Логика проверки завершенности регистрации перенесена в метод Sungero.Docflow.Shared.ModuleFunctions.CheckPoaDocumentRegistrationIsComplete.")]
    public virtual bool HasAnswerFromFts(IOfficialDocument mainDocument)
    {
      return Sungero.Docflow.PublicFunctions.Module.CheckPoaDocumentRegistrationIsComplete(mainDocument);
    }
    
    /// <summary>
    /// Получить ошибку регистрации в ФНС эл. доверенности/заявления на отзыв.
    /// </summary>
    /// <param name="mainDocument">Документ.</param>
    /// <returns>Ошибка регистрации в ФНС.</returns>
    [Obsolete("Метод не используется с 24.04.2024 и версии 4.10. Логика получения ошибки регистрации перенесена в метод Sungero.Docflow.Shared.ModuleFunctions.GetPoaDocumentFtsRegistrationError.")]
    public virtual string GetFtsRegistrationError(IOfficialDocument mainDocument)
    {
      return Sungero.Docflow.PublicFunctions.Module.GetPoaDocumentFtsRegistrationError(mainDocument);
    }
  }
}