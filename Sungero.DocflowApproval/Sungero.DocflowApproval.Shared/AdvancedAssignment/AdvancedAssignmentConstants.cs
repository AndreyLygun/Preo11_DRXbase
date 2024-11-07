using System;
using Sungero.Core;

namespace Sungero.DocflowApproval.Constants
{
  public static class AdvancedAssignment
  {
    /// <summary>
    /// ИД диалога подтверждения для результата "Выполнить".
    /// </summary>
    public const string CompleteConfirmDialogID = "D9FD8D7D-C121-4725-840B-66D1531C2E37";
    
    /// <summary>
    /// ИД диалога подтверждения для результата "На доработку".
    /// </summary>
    public const string ReworkConfirmDialogID = "67826682-B990-43BA-ABDB-189F23EADE15";
    
    /// <summary>
    /// Название параметра, указывающего на то, что можно переадресовывать задание.
    /// </summary>
    public const string AllowForwardParamName = "AllowForward";
    
    /// <summary>
    /// Название параметра, указывающего на то, что задание можно отправлять на доработку.
    /// </summary>
    public const string AllowSendForReworkParamName = "AllowSendForRework";
    
    /// <summary>
    /// Название параметра, указывающего на то, что можно выбирать ответственного за доработку задания.
    /// </summary>
    public const string AllowChangeReworkPerformerParamName = "AllowChangeReworkPerformer";
    
    /// <summary>
    /// Название параметра, указывающего на то, что на основной документ не хватает прав.
    /// </summary>
    public const string NeedRightsToOfficialDocumentParamName = "NeedRightsToOfficialDocument";
  }
}