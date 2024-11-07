using System;
using Sungero.Core;

namespace Sungero.SmartProcessing.Constants
{
  public static class VerificationAssignment
  {
    /// <summary>
    /// Коды диалогов в справке.
    /// </summary>
    public static class HelpCodes
    {
      /// <summary>
      /// Код диалога удаления документов.
      /// </summary>
      public const string DeleteDocumentsDialog = "Sungero_DeleteDocumentsDialog";
    }
    
    /// <summary>
    /// Параметр "Возможность удаления документов". 
    /// </summary>
    [Sungero.Core.Public]
    public const string CanDeleteParamName = "CanDelete";
    
    /// <summary>
    /// Имя параметра для отображения хинта "Для отображения результатов перекомплектования обновите карточку".
    /// </summary>
    public const string ShowRepackingResultsNotificationParamName = "ShowRepackingResultsNotification";
    
    /// <summary>
    /// Формат сообщения лога о фильтрации документов для перекомплектования.
    /// </summary>
    public const string LogDocumentsSuitableForRepackingFilterMessageFormat = "Repacking. {0}: [{1}]";
  }
}