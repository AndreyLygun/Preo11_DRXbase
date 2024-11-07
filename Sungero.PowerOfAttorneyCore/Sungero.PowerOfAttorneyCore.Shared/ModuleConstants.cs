using System;
using Sungero.Core;

namespace Sungero.PowerOfAttorneyCore.Constants
{
  public static class Module
  {
    /// <summary>
    /// Версия API сервиса доверенностей Контур.
    /// </summary>
    public const string KonturPowerOfAttorneyServiceVersion = "v1";
    
    /// <summary>
    /// Таймауты на регистрацию доверенности и отзыва.
    /// </summary>
    public const int PoARegisterOperationTimeout = 5;
    public const int PoARegisterRequestTimeout = 5;
    
    /// <summary>
    /// Таймауты на проверку состояния.
    /// </summary>
    public const int CheckPoAStateOperationTimeout = 10;
    public const int CheckPoAStateRequestTimeout = 10;
    
    /// <summary>
    /// Таймауты на длительные/асинхронные Http операции по доверенности.
    /// </summary>
    public const int PoAAsyncActionsOperationTimeout = 60;
    public const int PoAAsyncActionsRequestTimeout = 10;
    
    // Код ошибки в ответе от сервиса доверенностей, если доверенность не была найдена.
    [Sungero.Core.Public]
    public const string PoANotFoundErrorCode = "PoaNotFound";
    
    // Идентификатор информационной системы ФНС в метаданных доверенности, полученных от сервиса доверенностей.
    [Sungero.Core.Public]
    public const string FtsSystemName = "fns";
    
    /// <summary>
    /// Ошибки при работе с сервисом доверенностей.
    /// </summary>
    [Sungero.Core.Public]
    public static class PowerOfAttorneyServiceErrors
    {
      // Ошибка в настройке подключения к сервису доверенностей.
      [Sungero.Core.Public]
      public const string ConnectionError = "ConnectionError";
      
      // Ошибки соединения (408, 502, 503, 504).
      [Sungero.Core.Public]
      public const string NetworkError = "NetworkError";
      
      // Полученные данные не актуальны (не удалось получить ответ от сервиса за таймаут).
      [Sungero.Core.Public]
      public const string StateIsOutdated = "stateIsOutdated";
      
      // Полученные данные не актуальны (не удалось найти доверенность в сервисе за таймаут).
      [Sungero.Core.Public]
      public const string PoANotFound = "poaNotFound";
      
      // В результате выполнения запроса возникли ошибки.
      [Sungero.Core.Public]
      public const string ProcessingError = "ProcessingError";
    }
    
    /// <summary>
    /// В результате выполнения запроса response вернулся null.
    /// </summary>
    public const string NullResponseError = "NullResponseError";
    
    /// <summary>
    /// Статус обработки запроса - ошибка.
    /// </summary>
    public const string ErrorOperationStatus = "error";
    
    /// <summary>
    /// Константы инициализации модуля.
    /// </summary>
    public static class Init
    {
      public static class PowerOfAttorneyCore
      {
        /// <summary>
        /// Название параметра для хранения проинициализированной версии кода модуля.
        /// </summary>
        public const string Name = "InitPowerOfAttorneyCoreUpdate";
        
        public const string FirstInitVersion = "4.8.0.0";
        public const string Version49 = "4.9.0.0";
      }
    }
    
    /// <summary>
    /// Не заполнены данные в запросе.
    /// </summary>
    [Sungero.Core.Public]
    public const string EmptyRequestParametersError = "EmptyRequestParametersError";
    
    /// <summary>
    /// Префикс для списка ошибок из ответа сервиса.
    /// </summary>
    public const string ResponseErrorMessagesPrefix = "details: ";
    
    /// <summary>
    /// Сообщение об ошибке при пустом ответе из сервиса.
    /// </summary>
    public const string EmptyResponseErrorMessage = "Request result is empty";
    
    /// <summary>
    /// Статусы эл. доверенности в сервисе доверенностей.
    /// </summary>
    [Sungero.Core.Public]
    public static class PowerOfAttorneyStateStatus
    {
      /// <summary>
      /// Эл. доверенность активна в данный момент.
      /// </summary>
      [Sungero.Core.Public]
      public const string Active = "Active";
      
      /// <summary>
      /// Эл. доверенность создана и валидна.
      /// </summary>
      [Sungero.Core.Public]
      public const string Created = "Created";
      
      /// <summary>
      /// Срок действия эл. доверенности истек.
      /// </summary>
      [Sungero.Core.Public]
      public const string Expired = "Expired";
      
      /// <summary>
      /// Эл. доверенность отозвана.
      /// </summary>
      [Sungero.Core.Public]
      public const string Revoked = "Revoked";
      
      /// <summary>
      /// Эл. доверенность не найдена в сервисе.
      /// </summary>
      /// <remarks>Кастомный статус RX, в API сервиса доверенностей отсутствует.</remarks>
      [Sungero.Core.Public]
      public const string NotFound = "NotFound";
    }
    
    /// <summary>
    /// Статусы доставки эл. доверенности в блокчейн ФНС.
    /// </summary>
    [Sungero.Core.Public]
    public static class PowerOfAttorneyDeliveryStatus
    {
      /// <summary>
      /// Эл. доверенность доставлена в ФНС.
      /// </summary>
      [Sungero.Core.Public]
      public const string Delivered = "Delivered";
      
      /// <summary>
      /// Эл. доверенность была создана в ФНС.
      /// </summary>
      [Sungero.Core.Public]
      public const string Source = "Source";
      
      /// <summary>
      /// Не доставлять эл. доверенность в ФНС.
      /// </summary>
      [Sungero.Core.Public]
      public const string DoNotDeliver = "DoNotDeliver";
      
      /// <summary>
      /// Эл. доверенность в процессе доставки в ФНС.
      /// </summary>
      [Sungero.Core.Public]
      public const string Queued = "Queued";
      
      /// <summary>
      /// Ошибка доставки эл. доверенности в ФНС.
      /// </summary>
      [Sungero.Core.Public]
      public const string Error = "Error";
    }
  }
}