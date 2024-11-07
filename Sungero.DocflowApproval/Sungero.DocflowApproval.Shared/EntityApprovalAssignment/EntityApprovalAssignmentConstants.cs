using System;
using Sungero.Core;

namespace Sungero.DocflowApproval.Constants
{
  public static class EntityApprovalAssignment
  {
    /// <summary>
    /// ИД диалога подтверждения для результата "Согласовать".
    /// </summary>
    public const string ApprovedConfirmDialogID = "0DCE4788-AE1C-4BA5-B34A-FD0F803057D9";
    
    /// <summary>
    /// ИД диалога подтверждения для результата "Согласовать с замечаниями".
    /// </summary>
    public const string WithSuggestionsConfirmDialogID = "C01A276C-CA61-4443-B0F8-CCD1C5C10C28";
    
    /// <summary>
    /// ИД диалога подтверждения при отправке на доработку.
    /// </summary>
    public const string ForReworkConfirmDialogID = "C9922970-9C35-4883-99C7-48056E20FC59";
    
    public const string AddApprover = "AddApprover";
    
    #region Кэшируемые параметры
    
    public const string IsIncomingDocument = "IsIncomingDocument";
    
    #endregion
    
    // Разрешить согласование с замечаниями.
    public const string AllowApproveWithSuggestionsParamName = "AllowApproveWithSuggestions";
    
    // Разрешить изменение параметров.
    public const string AllowChangePropertiesParamName = "AllowChangeProperties";
    
    // Разрешить выбор ответственного за доработку.
    public const string AllowChangeReworkPerformerParamName = "AllowChangeReworkPerformer";
    
    // Скрыть реквизиты документа.
    public const string HideDocumentSummaryParamName = "HideDocumentSummary";
    
    // Требовать усиленную подпись.
    public const string NeedStrongSignatureParamName = "NeedStrongSignature";
    
    // Разрешить добавление согласующих.
    public const string AllowAddApproversParamName = "AllowAddApprovers";
    
    /// <summary>
    /// Постфикс логгера задания на согласование.
    /// </summary>
    public const string EntityApprovalAssignmentLoggerPostfix = "EntityApprovalAssignment";
  }
}