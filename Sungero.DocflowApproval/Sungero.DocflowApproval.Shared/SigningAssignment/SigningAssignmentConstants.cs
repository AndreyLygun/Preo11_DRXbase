using System;
using Sungero.Core;

namespace Sungero.DocflowApproval.Constants
{
  public static class SigningAssignment
  {
    /// <summary>
    /// ИД диалога подтверждения при подписании.
    /// </summary>
    public const string SignConfirmDialogID = "0BAFBAD0-0D2D-4271-B6B6-FDD13AFFAEBA";
    
    /// <summary>
    /// ИД диалога подтверждения при отказе.
    /// </summary>
    public const string RejectConfirmDialogID = "9A3ED687-F732-45E8-B0F4-52DBCA781C88";
    
    /// <summary>
    /// ИД диалога подтверждения при отправке на доработку.
    /// </summary>
    public const string ForReworkConfirmDialogID = "9ce54e79-7d44-4e02-b6b1-61c5f1911e12";
    
    // Разрешить выбор ответственного за доработку.
    public const string AllowChangeReworkPerformerParamName = "AllowChangeReworkPerformer";
    
    // Требовать усиленную подпись.
    public const string NeedStrongSignatureParamName = "NeedStrongSignature";
    
    // Разрешить переадресацию.
    public const string AllowForwardParamName = "AllowForward";
  }
}