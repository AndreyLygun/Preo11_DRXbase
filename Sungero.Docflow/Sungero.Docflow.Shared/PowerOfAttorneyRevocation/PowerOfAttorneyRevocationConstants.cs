using System;
using Sungero.Core;

namespace Sungero.Docflow.Constants
{
  public static class PowerOfAttorneyRevocation
  {
    
    public static class Operation
    {
      /// <summary>
      /// Изменение статуса "В реестре ФНС".
      /// </summary>
      public const string FtsStateChange = "FtsStateChange";
      
      /// <summary>
      /// Очистка статуса "В реестре ФНС".
      /// </summary>
      public const string FtsStateClear = "FtsStateClear";
    }
    
    /// <summary>
    /// Статус регистрации отзыва эл. доверенности.
    /// </summary>
    public static class FPoARevocationRegistrationStatus
    {
      /// <summary>
      /// В очереди на регистрацию.
      /// </summary>
      public const string Queued = "queued";
      
      /// <summary>
      /// Обработка регистрации.
      /// </summary>
      public const string Processing = "processing";

      /// <summary>
      /// Регистрация успешно завершена.
      /// </summary>
      public const string Done = "done";
      
      /// <summary>
      /// При регистрации возникли ошибки.
      /// </summary>
      public const string Error = "error";
    }
    
    /// <summary>
    /// Коды ошибок при регистрации отзыва эл. доверенности.
    /// </summary>
    [Sungero.Core.Public]
    public static class FPoARevocationRegistrationErrors
    {
      /// <summary>
      /// Код ошибки "недоступность ФНС".
      /// </summary>
      [Sungero.Core.Public]
      public const string ExternalSystemIsUnavailableError = "externalsystemisunavailable";
      
      /// <summary>
      /// Код ошибки "повторная регистрация".
      /// </summary>
      [Sungero.Core.Public]
      public const string RepeatedRegistrationError = "poaisrevoked";
      
      /// <summary>
      /// Код ошибки "подписана не тем сертификатом".
      /// </summary>
      [Sungero.Core.Public]
      public const string DifferentSignatureError = "conflictDifferentSignature";
      
      /// <summary>
      /// Код ошибки "доверенность не найдена".
      /// </summary>
      [Sungero.Core.Public]
      public const string PoaNotFoundError = "poanotfound";
      
      /// <summary>
      /// Код ошибки "данные подписанта в xml не совпадают с ЕГРЮЛ (СНИЛС, ИНН, ФИО, ОГРН)".
      /// </summary>
      [Sungero.Core.Public]
      public const string ExternalSystemBadResponseError = "externalsystembadresponse";
    }
    
    /// <summary>
    /// Код диалога переформирования заявления на отзыв эл. доверенности.
    /// </summary>
    public const string ReCreatePowerOfAttorneyRevocationHelpCode = "Sungero_ReCreatePowerOfAttorneyRevocationDialog";
  }
}