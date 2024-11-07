using System;
using Sungero.Core;

namespace Sungero.DocflowApproval.Constants
{
  public static class EntityReworkAssignment
  {
    /// <summary>
    /// ИД диалога подтверждения при выполнении задания на доработку с результатом "Исправлено".
    /// </summary>
    public const string ForReapprovalConfirmDialogID = "1AB0C604-0E98-42EB-BF56-9030115C33D4";
    
    /// <summary>
    /// ИД диалога подтверждения при прекращении задания на доработке.
    /// </summary>
    public const string AbortConfirmDialogID = "69308086-7371-4D1A-847B-3A1D11404ADA";
    
    public static class Operation
    {
      // Продление срока.
      public const string DeadlineExtend = "DeadlineExtend";
    }
    
    // Указать способ доставки.
    public const string SpecifyDeliveryMethodParamName = "SpecifyDeliveryMethod";
    
    /// <summary>
    /// Постфикс логгера задания на доработку.
    /// </summary>
    public const string EntityReworkAssignmentLoggerPostfix = "EntityReworkAssignment";
    
    /// <summary>
    /// Название параметра, указывающего на то, что можно изменить состав согласующих.
    /// </summary>
    public const string CanChangeApprovers = "CanChangeApprovers";
    
    /// <summary>
    /// Название параметра, указывающего на то, что можно изменить новый срок согласования.
    /// </summary>
    public const string CanChangeApprovalDeadline = "CanChangeApprovalDeadline";
  }
}