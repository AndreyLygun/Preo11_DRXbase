using System;
using Sungero.Core;

namespace Sungero.SmartProcessing.Constants
{
  public static class VerificationTask
  {
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на верификацию.
    /// </summary>
    public static class VerificationAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Проверено".
      /// </summary>
      public const string Complete = "F48688B8-1950-456B-B012-39094935107F";
    }
    
    /// <summary>
    /// Постфикс логгера задачи на верификацию.
    /// </summary>
    public const string VerificationTaskLoggerPostfix = "VerificationTask";
  }
}