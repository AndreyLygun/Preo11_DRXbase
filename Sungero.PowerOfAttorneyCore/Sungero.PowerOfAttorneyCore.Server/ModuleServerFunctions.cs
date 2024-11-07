using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.PowerOfAttorneyServiceExtensions;
using Sungero.PowerOfAttorneyServiceExtensions.Model;

namespace Sungero.PowerOfAttorneyCore.Server
{
  public class ModuleFunctions
  {
    
    #region Асинхронная операция регистрации МЧД
    
    /// <summary>
    /// Отправить доверенность на регистрацию в ФНС.
    /// </summary>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="powerOfAttorneyXml">Тело xml-файла доверенности.</param>
    /// <param name="powerOfAttorneySignature">Тело утверждающей подписи.</param>
    /// <returns>Результат отправки: ИД операции регистрации в сервисе доверенностей или ошибка.</returns>
    [Public]
    public virtual PowerOfAttorneyCore.Structures.Module.IResponseResult SendPowerOfAttorneyForRegistration(Company.IBusinessUnit businessUnit,
                                                                                                            byte[] powerOfAttorneyXml,
                                                                                                            byte[] powerOfAttorneySignature)
    {
      var serviceConnection = this.GetPowerOfAttorneyServiceConnection(businessUnit);
      var konturApiConnector = this.GetKonturConnector(serviceConnection);
      
      if (konturApiConnector == null)
      {
        var sendingResult = Structures.Module.ResponseResult.Create();
        sendingResult.ErrorType = Constants.Module.PowerOfAttorneyServiceErrors.ConnectionError;
        return sendingResult;
      }
      
      konturApiConnector.ResiliencySettings = new HttpResiliencySettings()
      {
        OperationTimeout = Constants.Module.PoARegisterOperationTimeout,
        RequestTimeout = Constants.Module.PoARegisterRequestTimeout
      };
      
      var response = konturApiConnector.SendPoAForRegistration(serviceConnection.OrganizationId, powerOfAttorneyXml, powerOfAttorneySignature);
      
      return this.ProcessOperationResponse(response, serviceConnection.OrganizationId, "SendPowerOfAttorneyForRegistration");
    }
    
    /// <summary>
    /// Получить статус регистрации.
    /// </summary>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="operationId">ИД операции.</param>
    /// <returns>Статус регистрации доверенности.</returns>
    [Public]
    public virtual PowerOfAttorneyCore.Structures.Module.IResponseResult GetRegistrationState(Company.IBusinessUnit businessUnit, string operationId)
    {
      var serviceConnection = this.GetPowerOfAttorneyServiceConnection(businessUnit);
      var konturApiConnector = this.GetKonturConnector(serviceConnection);
      
      if (konturApiConnector == null)
      {
        var sendingResult = Structures.Module.ResponseResult.Create();
        sendingResult.ErrorType = Constants.Module.PowerOfAttorneyServiceErrors.ConnectionError;
        return sendingResult;
      }
      
      konturApiConnector.ResiliencySettings = new HttpResiliencySettings()
      {
        OperationTimeout = Constants.Module.PoAAsyncActionsOperationTimeout,
        RequestTimeout = Constants.Module.PoAAsyncActionsRequestTimeout
      };
      
      var response = konturApiConnector.GetPoARegistrationState(serviceConnection.OrganizationId, operationId);
      
      return this.ProcessOperationResponse(response, serviceConnection.OrganizationId, "GetRegistrationState");
    }
    
    #endregion
    
    #region Синхронная валидация МЧД
    
    /// <summary>
    /// Проверить состояние эл. доверенности.
    /// </summary>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="unifiedRegistrationNumber">Единый рег. номер доверенности.</param>
    /// <param name="agent">Представитель.</param>
    /// <param name="powerOfAttorneyXml">Тело xml-файла доверенности.</param>
    /// <param name="powerOfAttorneySignature">Тело утверждающей подписи.</param>
    /// <returns>Результат валидации доверенности.</returns>
    [Public, Obsolete("Используйте метод CheckPowerOfAttorneyState с параметром Основной доверитель.")]
    public virtual PowerOfAttorneyCore.Structures.Module.IPowerOfAttorneyValidationState CheckPowerOfAttorneyState(Company.IBusinessUnit businessUnit,
                                                                                                                   string unifiedRegistrationNumber,
                                                                                                                   PowerOfAttorneyCore.Structures.Module.IAgent agent,
                                                                                                                   byte[] powerOfAttorneyXml,
                                                                                                                   byte[] powerOfAttorneySignature)
    {
      if (businessUnit == null)
      {
        Logger.ErrorFormat("CheckPowerOfAttorneyState. BusinessUnit is null (FPoA unified registration number = {0}).", unifiedRegistrationNumber);
        var validationResult = this.CreateEmptyValidationState();
        this.AddErrorToValidationState(validationResult, Constants.Module.EmptyRequestParametersError, null, null);
        return validationResult;
      }
      
      var principal = PowerOfAttorneyCore.Structures.Module.Principal.Create();
      principal.TIN = businessUnit.TIN;
      principal.TRRC = businessUnit.TRRC;
      
      return this.CheckPowerOfAttorneyState(businessUnit, unifiedRegistrationNumber, principal, agent, powerOfAttorneyXml, powerOfAttorneySignature);
    }
    
    /// <summary>
    /// Проверить состояние эл. доверенности.
    /// </summary>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="unifiedRegistrationNumber">Единый рег. номер доверенности.</param>
    /// <param name="mainPrincipal">Основной доверитель.</param>
    /// <param name="agent">Представитель.</param>
    /// <param name="powerOfAttorneyXml">Тело xml-файла доверенности.</param>
    /// <param name="powerOfAttorneySignature">Тело утверждающей подписи.</param>
    /// <returns>Результат валидации доверенности.</returns>
    [Public, Obsolete("Метод больше не используется с 31.07.2024 и версии 4.6, для проверки статуса доверенности используйте метод GetPowerOfAttorneyState")]
    public virtual PowerOfAttorneyCore.Structures.Module.IPowerOfAttorneyValidationState CheckPowerOfAttorneyState(Company.IBusinessUnit businessUnit,
                                                                                                                   string unifiedRegistrationNumber,
                                                                                                                   PowerOfAttorneyCore.Structures.Module.IPrincipal mainPrincipal,
                                                                                                                   PowerOfAttorneyCore.Structures.Module.IAgent agent,
                                                                                                                   byte[] powerOfAttorneyXml,
                                                                                                                   byte[] powerOfAttorneySignature)
    {
      var validationResult = this.CreateEmptyValidationState();
      if (businessUnit == null || string.IsNullOrWhiteSpace(unifiedRegistrationNumber) || mainPrincipal == null || agent == null || powerOfAttorneyXml == null || powerOfAttorneySignature == null)
      {
        Logger.ErrorFormat("CheckPowerOfAttorneyState. One or more required parameters are null (FPoA unified registration number = {0}).", unifiedRegistrationNumber);
        this.AddErrorToValidationState(validationResult, Constants.Module.EmptyRequestParametersError, null, null);
        return validationResult;
      }
      
      var serviceConnection = this.GetPowerOfAttorneyServiceConnection(businessUnit);
      var konturApiConnector = this.GetKonturConnector(serviceConnection);
      var organizationId = serviceConnection?.OrganizationId;
      
      // Если нет подключения к сервису доверенностей - соединение отсутствует.
      if (konturApiConnector == null)
      {
        Logger.ErrorFormat("CheckPowerOfAttorneyState. Service connector is null (FPoA unified registration number = {0}).", unifiedRegistrationNumber);
        this.AddErrorToValidationState(validationResult, Constants.Module.PowerOfAttorneyServiceErrors.ConnectionError, null, null);
        return validationResult;
      }
      
      // Подготовка данных для запроса состояния в сервисе.
      var poaContent = Convert.ToBase64String(powerOfAttorneyXml);
      var signatureContent = Convert.ToBase64String(powerOfAttorneySignature);
      var poaFiles = PowerOfAttorneyServiceExtensions.Model.PoAFiles.Create(poaContent, signatureContent);
      var principal = PowerOfAttorneyServiceExtensions.Model.Principal.Create(mainPrincipal.TIN, mainPrincipal.TRRC);
      var representative = PowerOfAttorneyServiceExtensions.Model.Representative.CreateRepresentative(agent.TIN, agent.INILA, agent.TINUl, agent.TRRC,
                                                                                                      agent.Name, agent.Surname, agent.Middlename);
      
      konturApiConnector.ResiliencySettings = new HttpResiliencySettings()
      {
        OperationTimeout = Constants.Module.CheckPoAStateOperationTimeout,
        RequestTimeout = Constants.Module.CheckPoAStateRequestTimeout
      };
      
      var response = konturApiConnector.ValidatePoA(organizationId, principal, representative, null, poaFiles);
      
      // Если нет информативного ответа.
      if (response == null || response.ErrorType == Sungero.PowerOfAttorneyServiceExtensions.Model.ErrorType.NullResponseError)
      {
        Logger.ErrorFormat("CheckPowerOfAttorneyState. Response is null (FPoA unified registration number = {0}).", unifiedRegistrationNumber);
        this.AddErrorToValidationState(validationResult, Constants.Module.NullResponseError, null, Constants.Module.NullResponseError);
        return validationResult;
      }

      var errorType = string.Empty;
      var errorCode = string.Empty;
      var errorMessage = string.Empty;
      
      // Если нет результата или невалидный ответ.
      if (response.Result == null || !response.IsSuccess)
      {
        errorType = !string.IsNullOrEmpty(response.ErrorType.ToString()) ? response.ErrorType.ToString() : Constants.Module.PowerOfAttorneyServiceErrors.ProcessingError;
        errorMessage = response.ErrorText;
        this.AddErrorToValidationState(validationResult, errorType, null, errorMessage);
        
        Logger.ErrorFormat("CheckPowerOfAttorneyState. Error while validating power of attorney (FPoA unified registration number = {0}, error type = {1}, error message: {2}).",
                           unifiedRegistrationNumber, errorType, errorMessage);
        return validationResult;
      }
      
      // Если получили неактуальный ответ.
      if (!response.Result.SystemSyncInfo.IsActual)
      {
        // Если есть время последнего обновления состояния в ФНС, и ФНС доступен - указываем время последнего обновления,
        // иначе - указываем, что состояние ранее не обновлялось или ФНС недоступен.
        Logger.ErrorFormat("CheckPowerOfAttorneyState. State is outdated, state - {1}, sync date - {2}. Power of attorney not found or FTS service is unavailable (FPoA unified registration number = {0}).",
                           unifiedRegistrationNumber, response.Result.PoAValidationResult.Status, response.Result.SystemSyncInfo.SyncedAt);
        
        errorType = Constants.Module.PowerOfAttorneyServiceErrors.ProcessingError;
        errorCode = DateTime.Compare(response.Result.SystemSyncInfo.SyncedAt, DateTime.MinValue) != 0 ?
          Constants.Module.PowerOfAttorneyServiceErrors.StateIsOutdated :
          Constants.Module.PowerOfAttorneyServiceErrors.PoANotFound;
        this.AddErrorToValidationState(validationResult, errorType, errorCode, null);
      }
      
      // Если есть ошибки - доверенность не валидна на текущий момент, но может быть валидна в будущем.
      if (response.Result.PoAValidationResult?.Errors != null)
      {
        foreach (var validationError in response.Result.PoAValidationResult.Errors)
        {
          errorType = Constants.Module.PowerOfAttorneyServiceErrors.ProcessingError;
          errorCode = validationError.Code;
          errorMessage = validationError.Message;
          this.AddErrorToValidationState(validationResult, errorType, errorCode, errorMessage);
          
          Logger.DebugFormat("CheckPowerOfAttorneyState. Error while validating power of attorney (FPoA unified registration number = {0}, state = {1}, error code = {2}, message: {3}).",
                             unifiedRegistrationNumber, response.Result.PoAValidationResult.Status, errorCode, errorMessage);
        }
      }
      
      validationResult.Result = response.Result.PoAValidationResult?.Status;
      Logger.DebugFormat("CheckPowerOfAttorneyState. Validating power of attorney done. State - {1}, sync date - {2}. (FPoA unified registration number = {0}).",
                         unifiedRegistrationNumber, validationResult.Result, response.Result.SystemSyncInfo.SyncedAt);
      return validationResult;
    }
    
    /// <summary>
    /// Создать пустой результат валидации доверенности.
    /// </summary>
    /// <returns>Результат валидации доверенности.</returns>
    [Obsolete("Метод больше не используется с 31.07.2024 и версии 4.6")]
    public virtual PowerOfAttorneyCore.Structures.Module.IPowerOfAttorneyValidationState CreateEmptyValidationState()
    {
      var validationState = Structures.Module.PowerOfAttorneyValidationState.Create();
      validationState.Errors = new List<Sungero.PowerOfAttorneyCore.Structures.Module.IValidationOperationError>();
      return validationState;
    }
    
    /// <summary>
    /// Добавить ошибку в результат валидации доверенности.
    /// </summary>
    /// <param name="validationState">Результат валидации доверенности.</param>
    /// <param name="errorType">Тип ошибки.</param>
    /// <param name="errorCode">Код ошибки.</param>
    /// <param name="errorMessage">Текст сообщения об ошибке.</param>
    [Obsolete("Метод больше не используется с 31.07.2024 и версии 4.6")]
    public virtual void AddErrorToValidationState(PowerOfAttorneyCore.Structures.Module.IPowerOfAttorneyValidationState validationState, string errorType, string errorCode, string errorMessage)
    {
      if (validationState == null || validationState.Errors == null)
        return;
      
      var error = Structures.Module.ValidationOperationError.Create();
      error.Type = errorType;
      error.Code = errorCode;
      error.Message = errorMessage;
      validationState.Errors.Add(error);
    }
    
    #endregion
    
    #region Асинхронная операция валидации МЧД
    
    /// <summary>
    /// Отправить запрос на проверку состояния эл. доверенности.
    /// </summary>
    /// <param name="serviceConnection">Подключение к сервису доверенностей.</param>
    /// <param name="businessUnit">НОР - доверитель.</param>
    /// <param name="unifiedRegistrationNumber">Единый рег. номер доверенности.</param>
    /// <param name="agent">Представитель.</param>
    /// <param name="powerOfAttorneyXml">Тело xml-файла доверенности.</param>
    /// <param name="powerOfAttorneySignature">Тело утверждающей подписи.</param>
    /// <returns>ИД операции в сервисе доверенностей.</returns>
    [Public, Obsolete("Используйте метод EnqueuePoAValidation с параметром Основной доверитель.")]
    public virtual PowerOfAttorneyCore.Structures.Module.IResponseResult EnqueuePoAValidation(IPowerOfAttorneyServiceConnection serviceConnection,
                                                                                              Company.IBusinessUnit businessUnit, string unifiedRegistrationNumber,
                                                                                              PowerOfAttorneyCore.Structures.Module.IAgent agent,
                                                                                              byte[] powerOfAttorneyXml, byte[] powerOfAttorneySignature)
    {
      if (businessUnit == null)
      {
        Logger.ErrorFormat("EnqueuePoAValidation. BusinessUnit is null (FPoA unified registration number = {0}).", unifiedRegistrationNumber);
        var sendingResult = Structures.Module.ResponseResult.Create();
        sendingResult.ErrorType = Constants.Module.EmptyRequestParametersError;
        sendingResult.ErrorCode = null;
        return sendingResult;
      }
      
      var principal = PowerOfAttorneyCore.Structures.Module.Principal.Create();
      principal.TIN = businessUnit.TIN;
      principal.TRRC = businessUnit.TRRC;
      return this.EnqueuePoAValidation(serviceConnection, businessUnit, unifiedRegistrationNumber, principal, agent, powerOfAttorneyXml, powerOfAttorneySignature);
    }
    
    /// <summary>
    /// Отправить запрос на проверку состояния эл. доверенности.
    /// </summary>
    /// <param name="serviceConnection">Подключение к сервису доверенностей.</param>
    /// <param name="businessUnit">НОР - доверитель.</param>
    /// <param name="unifiedRegistrationNumber">Единый рег. номер доверенности.</param>
    /// <param name="mainPrincipal">Основной доверитель.</param>
    /// <param name="agent">Представитель.</param>
    /// <param name="powerOfAttorneyXml">Тело xml-файла доверенности.</param>
    /// <param name="powerOfAttorneySignature">Тело утверждающей подписи.</param>
    /// <returns>ИД операции в сервисе доверенностей.</returns>
    [Public, Obsolete("Метод больше не используется с 31.07.2024 и версии 4.6")]
    public virtual PowerOfAttorneyCore.Structures.Module.IResponseResult EnqueuePoAValidation(IPowerOfAttorneyServiceConnection serviceConnection,
                                                                                              Company.IBusinessUnit businessUnit, string unifiedRegistrationNumber,
                                                                                              PowerOfAttorneyCore.Structures.Module.IPrincipal mainPrincipal,
                                                                                              PowerOfAttorneyCore.Structures.Module.IAgent agent,
                                                                                              byte[] powerOfAttorneyXml, byte[] powerOfAttorneySignature)
    {
      var sendingResult = Structures.Module.ResponseResult.Create();
      if (serviceConnection == null || businessUnit == null || string.IsNullOrWhiteSpace(unifiedRegistrationNumber) || mainPrincipal == null || agent == null || powerOfAttorneyXml == null || powerOfAttorneySignature == null)
      {
        Logger.ErrorFormat("EnqueuePoAValidation. One or more required parameters are null (FPoA unified registration number = {0}).", unifiedRegistrationNumber);
        sendingResult.ErrorType = Constants.Module.EmptyRequestParametersError;
        sendingResult.ErrorCode = null;
        return sendingResult;
      }
      
      var principal = PowerOfAttorneyServiceExtensions.Model.Principal.Create(mainPrincipal.TIN, mainPrincipal.TRRC);
      var representative = PowerOfAttorneyServiceExtensions.Model.Representative.CreateRepresentative(agent.TIN, agent.INILA, agent.TINUl, agent.TRRC,
                                                                                                      agent.Name, agent.Surname, agent.Middlename);
      var poaIdentity = PowerOfAttorneyServiceExtensions.Model.PoAIdentity.Create(unifiedRegistrationNumber, businessUnit.TIN);
      
      var poaContent = Convert.ToBase64String(powerOfAttorneyXml, Base64FormattingOptions.InsertLineBreaks);
      var signatureContent = Convert.ToBase64String(powerOfAttorneySignature, Base64FormattingOptions.InsertLineBreaks);
      var poaFiles = PowerOfAttorneyServiceExtensions.Model.PoAFiles.Create(poaContent, signatureContent);
      
      var organizationId = serviceConnection?.OrganizationId;
      var connector = this.GetKonturConnector(serviceConnection);
      connector.ResiliencySettings = new HttpResiliencySettings()
      {
        OperationTimeout = Constants.Module.PoAAsyncActionsOperationTimeout,
        RequestTimeout = Constants.Module.PoAAsyncActionsRequestTimeout
      };
      
      var response = connector.EnqueuePoAValidation(organizationId, principal, representative, poaIdentity, poaFiles);
      
      return this.ProcessOperationResponse(response, organizationId, "EnqueuePoAValidation");
    }
    
    /// <summary>
    /// Получить состояние валидации эл. доверенности в сервисе.
    /// </summary>
    /// <param name="serviceConnection">Подключение к сервису доверенностей.</param>
    /// <param name="operationId">ИД операции в сервисе доверенностей.</param>
    /// <returns>Состояние валидации доверенности.</returns>
    [Public, Obsolete("Метод больше не используется с 31.07.2024 и версии 4.6")]
    public virtual Structures.Module.IPowerOfAttorneyValidationState GetPoAValidationState(IPowerOfAttorneyServiceConnection serviceConnection, string operationId)
    {
      var validationState = Structures.Module.PowerOfAttorneyValidationState.Create();
      validationState.Errors = new List<Sungero.PowerOfAttorneyCore.Structures.Module.IValidationOperationError>();
      var organizationId = serviceConnection?.OrganizationId;
      var connector = this.GetKonturConnector(serviceConnection);
      connector.ResiliencySettings = new HttpResiliencySettings()
      {
        OperationTimeout = Constants.Module.PoAAsyncActionsOperationTimeout,
        RequestTimeout = Constants.Module.PoAAsyncActionsRequestTimeout
      };
      
      var response = connector.GetPoAValidationOperationState(organizationId, operationId);
      
      if (response == null || response.ErrorType == Sungero.PowerOfAttorneyServiceExtensions.Model.ErrorType.NullResponseError)
      {
        Logger.ErrorFormat("GetPoAValidationState. Response is null (organizationId = {0}).", organizationId);
        var error = Structures.Module.ValidationOperationError.Create();
        error.Type = Constants.Module.NullResponseError;
        error.Code = null;
        
        validationState.Errors.Add(error);
        return validationState;
      }
      
      if (!response.IsSuccess || response.Result == null || response.Result.Status == Constants.Module.ErrorOperationStatus)
      {
        validationState.OperationStatus = response.Result?.Status;
        Logger.ErrorFormat("GetPoAValidationState. Error while getting power of attorney validation state (operation id = {0}, business unit id = {1}, error code = {2}).",
                           operationId, serviceConnection.BusinessUnit?.Id, response.Result?.Error?.Code);
        var error = Structures.Module.ValidationOperationError.Create();
        error.Type = !string.IsNullOrEmpty(response.ErrorType.ToString()) ?
          response.ErrorType.ToString() :
          Constants.Module.PowerOfAttorneyServiceErrors.ProcessingError;
        error.Code = response.Result?.Error?.Code;
        
        validationState.Errors.Add(error);
        return validationState;
      }

      validationState.OperationStatus = response.Result.Status;
      validationState.Result = response.Result.ValidationResult?.Status;
      
      // Если есть ошибки - доверенность в текущий момент времени не валидна.
      if (response.Result.ValidationResult?.Errors != null)
      {
        foreach (var validationError in response.Result.ValidationResult.Errors)
        {
          Logger.DebugFormat("GetPoAValidationState. Error while validating power of attorney (organizationId = {0}, error code = {1}, message: {2}).",
                             response.Result.ValidationResult.Status, validationError.Code, validationError.Message);
          
          var error = Structures.Module.ValidationOperationError.Create();
          error.Type = Constants.Module.PowerOfAttorneyServiceErrors.ProcessingError;
          error.Code = validationError.Code;
          error.Message = validationError.Message;
          validationState.Errors.Add(error);
        }
      }
      
      return validationState;
    }
    
    #endregion
    
    #region Асинхронная операция импорта МЧД (из ФНС в Контур)
    
    /// <summary>
    /// Отправить запрос на создание асинхронной операции импорта эл. доверенности из ФНС в Контур.
    /// </summary>
    /// <param name="serviceConnection">Подключение к сервису доверенностей.</param>
    /// <param name="unifiedRegistrationNumber">Единый рег. номер доверенности.</param>
    /// <param name="principalTin">ИНН доверителя.</param>
    /// <param name="representativeTin">ИНН представителя.</param>
    /// <returns>ИД операции импорта в сервисе доверенностей.</returns>
    [Public]
    public virtual PowerOfAttorneyCore.Structures.Module.IResponseResult EnqueuePoAImportOperation(IPowerOfAttorneyServiceConnection serviceConnection, string unifiedRegistrationNumber,
                                                                                                   string principalTin, string representativeTin)
    {
      var sendingResult = Structures.Module.ResponseResult.Create();
      
      if (serviceConnection == null ||
          string.IsNullOrWhiteSpace(unifiedRegistrationNumber) ||
          string.IsNullOrWhiteSpace(principalTin) ||
          string.IsNullOrWhiteSpace(representativeTin))
      {
        Logger.ErrorFormat("EnqueuePoAImportOperation. One or more required parameters are null (FPoA unified registration number = {0}).", unifiedRegistrationNumber);
        sendingResult.ErrorType = Constants.Module.EmptyRequestParametersError;
        sendingResult.ErrorCode = null;
        return sendingResult;
      }
      
      var konturApiConnector = this.GetKonturConnector(serviceConnection);
      if (konturApiConnector == null)
      {
        sendingResult.ErrorType = Constants.Module.PowerOfAttorneyServiceErrors.ConnectionError;
        return sendingResult;
      }
      
      konturApiConnector.ResiliencySettings = new HttpResiliencySettings()
      {
        OperationTimeout = Constants.Module.PoAAsyncActionsOperationTimeout,
        RequestTimeout = Constants.Module.PoAAsyncActionsRequestTimeout
      };
      
      var response = konturApiConnector.EnqueuePoAImportOperation(serviceConnection.OrganizationId, principalTin, representativeTin, unifiedRegistrationNumber);
      
      return this.ProcessOperationResponse(response, serviceConnection.OrganizationId, "EnqueuePoAImportOperation");
    }
    
    /// <summary>
    /// Получить состояние операции импорта эл. доверенности из ФНС в Контур.
    /// </summary>
    /// <param name="serviceConnection">Подключение к сервису доверенностей.</param>
    /// <param name="operationId">ИД операции в сервисе доверенностей.</param>
    /// <returns>Состояние операции импорта эл. доверенности.</returns>
    [Public]
    public virtual PowerOfAttorneyCore.Structures.Module.IResponseResult GetPoAImportOperationState(IPowerOfAttorneyServiceConnection serviceConnection, string operationId)
    {
      var konturApiConnector = this.GetKonturConnector(serviceConnection);
      if (konturApiConnector == null)
      {
        var sendingResult = Structures.Module.ResponseResult.Create();
        sendingResult.ErrorType = Constants.Module.PowerOfAttorneyServiceErrors.ConnectionError;
        return sendingResult;
      }
      
      konturApiConnector.ResiliencySettings = new HttpResiliencySettings()
      {
        OperationTimeout = Constants.Module.PoAAsyncActionsOperationTimeout,
        RequestTimeout = Constants.Module.PoAAsyncActionsRequestTimeout
      };
      
      var response = konturApiConnector.GetPoAImportOperationState(serviceConnection.OrganizationId, operationId);
      
      return this.ProcessOperationResponse(response, serviceConnection.OrganizationId, "GetPoAImportOperationState");
    }
    
    #endregion
    
    #region Асинхронная операция регистрации отзыва
    
    /// <summary>
    /// Отправить запрос отзыва доверенности в ФНС.
    /// </summary>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="revocationXml">Тело xml-файла отзыва доверенности.</param>
    /// <param name="revocationSignature">Тело утверждающей подписи.</param>
    /// <returns>Результат отправки отзыва эл. доверенности.</returns>
    [Public]
    public virtual PowerOfAttorneyCore.Structures.Module.IResponseResult SendPowerOfAttorneyRevocation(Company.IBusinessUnit businessUnit,
                                                                                                       byte[] revocationXml,
                                                                                                       byte[] revocationSignature)
    {
      var serviceConnection = this.GetPowerOfAttorneyServiceConnection(businessUnit);
      var konturApiConnector = this.GetKonturConnector(serviceConnection);
      
      if (konturApiConnector == null)
      {
        var sendingResult = Structures.Module.ResponseResult.Create();
        sendingResult.ErrorType = Constants.Module.PowerOfAttorneyServiceErrors.ConnectionError;
        return sendingResult;
      }
      
      konturApiConnector.ResiliencySettings = new HttpResiliencySettings()
      {
        OperationTimeout = Constants.Module.PoARegisterOperationTimeout,
        RequestTimeout = Constants.Module.PoARegisterRequestTimeout
      };
      
      var response = konturApiConnector.SendRevocation(serviceConnection.OrganizationId, revocationXml, revocationSignature);
      
      return this.ProcessOperationResponse(response, serviceConnection.OrganizationId, "SendPowerOfAttorneyRevocation");
    }
    
    /// <summary>
    /// Получить статус регистрации отзыва МЧД.
    /// </summary>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="operationId">ИД операции.</param>
    /// <returns>Статус регистрации отзыва.</returns>
    [Public]
    public virtual PowerOfAttorneyCore.Structures.Module.IResponseResult GetRevocationState(Company.IBusinessUnit businessUnit, string operationId)
    {
      var serviceConnection = this.GetPowerOfAttorneyServiceConnection(businessUnit);
      var konturApiConnector = this.GetKonturConnector(serviceConnection);
      
      if (konturApiConnector == null)
      {
        var sendingResult = Structures.Module.ResponseResult.Create();
        sendingResult.ErrorType = Constants.Module.PowerOfAttorneyServiceErrors.ConnectionError;
        return sendingResult;
      }
      
      konturApiConnector.ResiliencySettings = new HttpResiliencySettings()
      {
        OperationTimeout = Constants.Module.PoAAsyncActionsOperationTimeout,
        RequestTimeout = Constants.Module.PoAAsyncActionsRequestTimeout
      };
      
      var response = konturApiConnector.GetRevocationState(serviceConnection.OrganizationId, operationId);
      
      return this.ProcessOperationResponse(response, serviceConnection.OrganizationId, "GetRevocationState");
    }
    
    #endregion
    
    #region Работа с метаданными МЧД
    
    /// <summary>
    /// Получить информацию о состоянии эл. доверенности.
    /// </summary>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="unifiedRegistrationNumber">Единый рег. номер доверенности.</param>
    /// <returns>Информация о состоянии эл. доверенности.</returns>
    [Public]
    public virtual PowerOfAttorneyCore.Structures.Module.IPowerOfAttorneyState GetPowerOfAttorneyState(Company.IBusinessUnit businessUnit, string unifiedRegistrationNumber)
    {
      var stateResult = Structures.Module.PowerOfAttorneyState.Create();
      stateResult.ErrorCodes = new List<string>();
      
      if (businessUnit == null || string.IsNullOrWhiteSpace(unifiedRegistrationNumber))
      {
        Logger.ErrorFormat("GetPowerOfAttorneyState. One or more required parameters are null (FPoA unified registration number = {0}).", unifiedRegistrationNumber);
        stateResult.ErrorCodes.Add(Constants.Module.EmptyRequestParametersError);
        return stateResult;
      }
      
      var serviceConnection = this.GetPowerOfAttorneyServiceConnection(businessUnit);
      var konturApiConnector = this.GetKonturConnector(serviceConnection);
      var organizationId = serviceConnection?.OrganizationId;
      
      if (konturApiConnector == null)
      {
        Logger.ErrorFormat("GetPowerOfAttorneyState. Service connector is null (FPoA unified registration number = {0}).", unifiedRegistrationNumber);
        stateResult.ErrorCodes.Add(Constants.Module.PowerOfAttorneyServiceErrors.ConnectionError);
        return stateResult;
      }
      
      konturApiConnector.ResiliencySettings = new HttpResiliencySettings()
      {
        OperationTimeout = Constants.Module.CheckPoAStateOperationTimeout,
        RequestTimeout = Constants.Module.CheckPoAStateRequestTimeout
      };
      
      var response = konturApiConnector.GetPoAMetadata(organizationId, unifiedRegistrationNumber);
      
      // Если нет информативного ответа.
      if (response == null || response.ErrorType == Sungero.PowerOfAttorneyServiceExtensions.Model.ErrorType.NullResponseError)
      {
        Logger.ErrorFormat("GetPowerOfAttorneyState. Response is null (FPoA unified registration number = {0}).", unifiedRegistrationNumber);
        stateResult.ErrorCodes.Add(Constants.Module.NullResponseError);
        return stateResult;
      }
      
      // Если эл. доверенность не найдена в сервисе.
      if (!string.IsNullOrEmpty(response.ErrorText) && response.ErrorText.Contains(Constants.Module.PoANotFoundErrorCode))
      {
        stateResult.PoAStatus = Constants.Module.PowerOfAttorneyStateStatus.NotFound;
        return stateResult;
      }

      // Если нет результата или невалидный ответ.
      if (response.Result == null || !response.IsSuccess)
      {
        var errorTypeText = !string.IsNullOrEmpty(response.ErrorType.ToString()) ? response.ErrorType.ToString() : Constants.Module.PowerOfAttorneyServiceErrors.ProcessingError;
        stateResult.ErrorCodes.Add(errorTypeText);
        Logger.ErrorFormat("GetPowerOfAttorneyState. Error while fetching power of attorney state info (FPoA unified registration number = {0}, error type = {1}, error message: {2}).",
                           unifiedRegistrationNumber, errorTypeText, response.ErrorText);
        return stateResult;
      }
      
      var fpoaFtsState = response.Result.PowerOfAttorney.State.Systems.FirstOrDefault(x => x.SystemName == Constants.Module.FtsSystemName);
      
      if (fpoaFtsState == null)
      {
        stateResult.ErrorCodes.Add(Constants.Module.PowerOfAttorneyServiceErrors.ProcessingError);
        Logger.ErrorFormat("GetPowerOfAttorneyState. Power of attorney state info does not contain FTS information (FPoA unified registration number = {0}).",
                           unifiedRegistrationNumber);
        return stateResult;
      }
      
      stateResult.PoAStatus = fpoaFtsState.StatusInfo?.Status;
      stateResult.PoADeliveryStatus = fpoaFtsState.DeliveryInfo?.Status;
      stateResult.RevocationDeliveryStatus = fpoaFtsState.RevocationDeliveryInfo?.Status;
      
      var revocationState = response.Result.PowerOfAttorney.State.RevocationInfo;
      if (revocationState != null)
      {
        stateResult.RevocationReason = revocationState.Reason;
        stateResult.RevocationDate = revocationState.Sign.SignedAt;
      }
      
      return stateResult;
    }
    
    /// <summary>
    /// Получить дату подписания и причину отзыва доверенности.
    /// </summary>
    /// <param name="serviceConnection">Подключение к сервису доверенностей.</param>
    /// <param name="unifiedRegistrationNumber">Единый регистрационный номер доверенности.</param>
    /// <returns>Дата подписания и причина отзыва доверенности.</returns>
    [Public, Remote, Obsolete("Метод больше не используется с 12.08.2024 и версии 4.6, для получения данных об отзыве МЧД используейте метод GetPowerOfAttorneyState()")]
    public virtual Structures.Module.IPowerOfAttorneyRevocationInfo GetPowerOfAttorneyRevocationInfo(IPowerOfAttorneyServiceConnection serviceConnection, string unifiedRegistrationNumber)
    {
      var konturApiConnector = this.GetKonturConnector(serviceConnection);
      if (konturApiConnector == null)
      {
        return null;
      }
      
      konturApiConnector.ResiliencySettings = new HttpResiliencySettings()
      {
        OperationTimeout = Constants.Module.PoAAsyncActionsOperationTimeout,
        RequestTimeout = Constants.Module.PoAAsyncActionsRequestTimeout
      };
      
      var response = konturApiConnector.GetPoARevocationInfo(serviceConnection.OrganizationId, unifiedRegistrationNumber);
      
      if (response == null || response.ErrorType == Sungero.PowerOfAttorneyServiceExtensions.Model.ErrorType.NullResponseError)
      {
        Logger.ErrorFormat("GetPowerOfAttorneyRevocationInfo. Response is null (organizationId = {0}).", serviceConnection.OrganizationId);
        return null;
      }
      
      var revocationInfo = Structures.Module.PowerOfAttorneyRevocationInfo.Create();
      
      if (response.IsSuccess && response.Result != null)
      {
        revocationInfo = Structures.Module.PowerOfAttorneyRevocationInfo.Create();
        revocationInfo.Date = response.Result.Sign.SignedAt ?? DateTime.MinValue;
        revocationInfo.Reason = response.Result.Reason;
      }
      else
      {
        Logger.ErrorFormat("GetPowerOfAttorneyRevocationInfo. Error while getting revocation info (FPoA unified registration number = {0}, business unit id = {1}).",
                           unifiedRegistrationNumber, serviceConnection.BusinessUnit?.Id);
        return null;
      }

      return revocationInfo;
    }
    
    #endregion
    
    #region Работа с подключение к сервису доверенностей
    
    /// <summary>
    /// Получить ИД НОР в сервисе доверенностей Контур.
    /// </summary>
    /// <param name="poaServiceConnection">Подключение к сервису доверенностей.</param>
    /// <returns>ИД НОР в сервисе доверенностей.</returns>
    [Public, Remote]
    public virtual string GetOrganizationIdFromService(IPowerOfAttorneyServiceConnection poaServiceConnection)
    {
      var konturApiConnector = this.GetKonturConnector(poaServiceConnection);
      
      try
      {
        return konturApiConnector.GetOrganizationId(poaServiceConnection.BusinessUnit.TIN);
      }
      catch (Exception ex)
      {
        Logger.Error("GetOrganizationIdFromService. Error connecting to power of attorney service.", ex);
        return string.Empty;
      }
    }
    
    /// <summary>
    /// Получить активные настройки подключения.
    /// </summary>
    /// <returns>Список активных настроек подключения.</returns>
    [Public]
    public virtual List<IPowerOfAttorneyServiceConnection> GetActiveServiceConnections()
    {
      return PowerOfAttorneyServiceConnections
        .GetAll(х => х.Status == Sungero.PowerOfAttorneyCore.PowerOfAttorneyServiceConnection.Status.Active &&
                х.ConnectionStatus == Sungero.PowerOfAttorneyCore.PowerOfAttorneyServiceConnection.ConnectionStatus.Connected)
        .ToList();
    }
    
    /// <summary>
    /// Создать подключение нашей организации к сервису доверенностей.
    /// </summary>
    /// <returns>Созданное подключение нашей организации к сервису доверенностей.</returns>
    [Remote]
    public static IPowerOfAttorneyServiceConnection CreateAttorneyServiceConnection()
    {
      // Создать подключение нашей организации к сервису доверенностей.
      return PowerOfAttorneyServiceConnections.Create();
    }
    
    /// <summary>
    /// Получить коннектор к сервису доверенностей.
    /// </summary>
    /// <param name="businessUnit">Наша организация.</param>
    /// <returns>Коннектор к сервису доверенностей.</returns>
    public virtual PowerOfAttorneyServiceExtensions.KonturConnector GetKonturConnector(Company.IBusinessUnit businessUnit)
    {
      var serviceConnection = this.GetPowerOfAttorneyServiceConnection(businessUnit);
      if (serviceConnection == null)
        return null;
      return this.GetKonturConnector(serviceConnection);
    }
    
    /// <summary>
    /// Получить коннектор к сервису доверенностей.
    /// </summary>
    /// <param name="poaServiceConnection">Подключение к сервису доверенностей.</param>
    /// <returns>Коннектор к сервису доверенностей.</returns>
    public virtual PowerOfAttorneyServiceExtensions.KonturConnector GetKonturConnector(IPowerOfAttorneyServiceConnection poaServiceConnection)
    {
      if (poaServiceConnection == null)
      {
        Logger.Error("GetKonturConnector. Service connection for organization is not specified.");
        return null;
      }
      
      var serviceApiVersion = Constants.Module.KonturPowerOfAttorneyServiceVersion;
      return PowerOfAttorneyServiceExtensions.KonturConnector.Get(poaServiceConnection.ServiceApp.Uri,
                                                                  serviceApiVersion,
                                                                  poaServiceConnection.ServiceApp.APIKey);
    }
    
    /// <summary>
    /// Получить подключение к сервису доверенностей.
    /// </summary>
    /// <param name="businessUnit">Наша организация.</param>
    /// <returns>Подключение к сервису доверенностей.</returns>
    [Public]
    public virtual IPowerOfAttorneyServiceConnection GetPowerOfAttorneyServiceConnection(Company.IBusinessUnit businessUnit)
    {
      return this.GetPowerOfAttorneyServiceConnectionQuery(businessUnit).FirstOrDefault();
    }
    
    /// <summary>
    /// Проверить наличие настроенного подключения к сервису доверенностей.
    /// </summary>
    /// <param name="businessUnit">Наша организация.</param>
    /// <returns>True - если есть активное настроенное подключение.</returns>
    [Public]
    public virtual bool HasPowerOfAttorneyServiceConnection(Company.IBusinessUnit businessUnit)
    {
      return this.GetPowerOfAttorneyServiceConnectionQuery(businessUnit).Any();
    }
    
    /// <summary>
    /// Получить запрос с подключением к сервису доверенностей.
    /// </summary>
    /// <param name="businessUnit">Наша организация.</param>
    /// <returns>Запрос для получения подключения к сервису доверенностей.</returns>
    public virtual IQueryable<IPowerOfAttorneyServiceConnection> GetPowerOfAttorneyServiceConnectionQuery(Company.IBusinessUnit businessUnit)
    {
      return PowerOfAttorneyServiceConnections.GetAll()
        .Where(p => Equals(p.BusinessUnit, businessUnit) &&
               p.Status == PowerOfAttorneyCore.PowerOfAttorneyServiceConnection.Status.Active &&
               p.ConnectionStatus == PowerOfAttorneyCore.PowerOfAttorneyServiceConnection.ConnectionStatus.Connected);
    }
    
    #endregion
    
    /// <summary>
    /// Обработать ответ от сервиса доверенностей.
    /// </summary>
    /// <param name="response">Данные ответа от сервиса доверенностей.</param>
    /// <param name="organizationId">ИД организации в сервисе доверенностей (используется для логирования).</param>
    /// <param name="operationType">Тип выполняемой операции (используется для логирования).</param>
    /// <returns>Результат обработки ответа от сервиса доверенностей.</returns>
    protected PowerOfAttorneyCore.Structures.Module.IResponseResult ProcessOperationResponse(RequestResult<OperationResponse> response, string organizationId, string operationType)
    {
      var result = Structures.Module.ResponseResult.Create();
      
      if (response == null || response.ErrorType == Sungero.PowerOfAttorneyServiceExtensions.Model.ErrorType.NullResponseError)
      {
        Logger.ErrorFormat("{0}. Response is null (organizationId = {1}). {2}", operationType, organizationId, response.ErrorText);
        result.ErrorType = Constants.Module.NullResponseError;
        result.ErrorCode = null;
        return result;
      }
      
      result.StatusCode = response.StatusCode;
      
      var errorCode = response.Result?.Error?.Code;
      if (!response.IsSuccess || !string.IsNullOrEmpty(errorCode))
      {
        result.ErrorCode = errorCode;
        result.ErrorType = !string.IsNullOrEmpty(response.ErrorType.ToString()) ?
          response.ErrorType.ToString() :
          Constants.Module.PowerOfAttorneyServiceErrors.ProcessingError;
        result.State = Constants.Module.ErrorOperationStatus;
        
        var errorDetailsFromService = this.GetResponseErrorDetails(response);
        
        Logger.ErrorFormat("{0}. Error while processing operation response (organizationId = {1}, errorCode = {2}, error type = {3}, {4}).",
                           operationType, organizationId, errorCode, result.ErrorType, errorDetailsFromService);
        
        return result;
      }
      
      result.OperationId = response.Result.Id;
      result.State = response.Result.Status;
      
      Logger.DebugFormat("{0}. Operation response processed successfully (organizationId = {1}, operationId = {2}, poaNumber = {3}).",
                         operationType, organizationId, response.Result.Id, CreatePoANumberArgFromResponse(response));
      
      return result;
    }
    
    /// <summary>
    /// Получить детали ошибки из ответа сервиса.
    /// </summary>
    /// <param name="response">Ответ сервиса.</param>
    /// <returns>Форматированная строка с деталями ошибки.</returns>
    private string GetResponseErrorDetails(RequestResult<OperationResponse> response)
    {
      var errorMessagesFromService = string.Empty;
      if (response == null)
        return Constants.Module.EmptyResponseErrorMessage;
      
      if (response.Result?.Error?.Details != null)
      {
        errorMessagesFromService = this.GetErrorDetailsAsString(response.Result.Error.Details);
      }
      else if (!string.IsNullOrWhiteSpace(response.ErrorText))
      {
        errorMessagesFromService = Constants.Module.ResponseErrorMessagesPrefix + response.ErrorText;
      }
      
      return errorMessagesFromService;
    }
    
    /// <summary>
    /// Получить детали ошибки из ответа сервиса в виде форматированной строки.
    /// </summary>
    /// <param name="errorDetails">Детали ошибки.</param>
    /// <returns>Форматированная строка с деталями ошибки.</returns>
    private string GetErrorDetailsAsString(List<PowerOfAttorneyServiceExtensions.Model.Error> errorDetails)
    {
      var messageText = new StringBuilder();
      messageText.Append(Constants.Module.ResponseErrorMessagesPrefix);
      foreach (var error in errorDetails)
        messageText.AppendLine(error.Message);
      
      return messageText.ToString();
    }
    
    /// <summary>
    /// Создать аргумент (poaNumber) для журналирования регистрации доверенности.
    /// </summary>
    /// <param name="response">HTTP-ответ от Контура.</param>
    /// <returns>Номер регистрируемой доверенности.</returns>
    private static string CreatePoANumberArgFromResponse(
      Sungero.PowerOfAttorneyServiceExtensions.Model.RequestResult<Sungero.PowerOfAttorneyServiceExtensions.Model.OperationResponse> response)
    {
      if (response.Result?.Parameters?.PoAIdentity?.Number != null)
        return string.Format(", poaNumber = {0}", response.Result.Parameters.PoAIdentity.Number);
      
      return string.Empty;
    }
  }
}