using System;
using Sungero.Core;

namespace Sungero.DocflowApproval.Constants
{
  public static class DocumentProcessingAssignment
  {
    /// <summary>
    /// ИД диалога подтверждения при выполнении задания.
    /// </summary>
    public const string CompleteConfirmDialogID = "3F18F926-E4D2-42C9-86EA-461924A0E5A2";
    
    /// <summary>
    /// ИД диалога подтверждения при отправке на доработку.
    /// </summary>
    public const string ForReworkConfirmDialogID = "8804F90B-40CE-40FD-98CE-1E7DBDF196AB";
    
    // Имя параметра: пропустить ли обновление на событиях.
    public const string SkipRefreshEventsParamName = "ATFormSkipRefreshEvents";
    
    // Разрешить выбор ответственного за доработку.
    public const string AllowChangeReworkPerformerParamName = "AllowChangeReworkPerformer";
    
    // Разрешить отправку на доработку.
    public const string AllowSendForReworkParamName = "AllowSendForRework";
  }
}