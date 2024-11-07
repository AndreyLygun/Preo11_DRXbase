using System;
using Sungero.Core;

namespace Sungero.Docflow.Constants
{
  public static class MarkKind
  {
    // Sid отметки об ЭП.
    [Sungero.Core.Public]
    public const string ElectronicSignatureMarkKindSid = "E195526C-DF17-42B4-B588-8C7C46E28A27";
    
    // Полное имя класса, из которого вызывается метод получения отметки об ЭП.
    [Sungero.Core.Public]
    public const string ElectronicSignatureMarkKindClassName = "Sungero.Docflow.Functions.OfficialDocument";
    
    // Имя метода получения отметки об ЭП.
    [Sungero.Core.Public]
    public const string ElectronicSignatureMarkKindFunctionName = "GetSignatureMarkAsHtml";
    
    // Sid отметки о поступлении.
    [Sungero.Core.Public]
    public const string ReceiptMarkKindSid = "6A7B90CB-7E64-4A97-AB0B-F8C3F476C843";
    
    // Полное имя класса, из которого вызывается метод получения отметки о поступлении.
    [Sungero.Core.Public]
    public const string ReceiptMarkKindClassName = "Sungero.Docflow.Functions.OfficialDocument";
    
    // Имя метода получения отметки о поступлении.
    [Sungero.Core.Public]
    public const string ReceiptMarkKindFunctionName = "GetRegistrationStampAsHtml";
    
    // Sid отметки "Дата регистрации".
    [Sungero.Core.Public]
    public const string RegistrationDateMarkKindSid = "FB49FB00-76D8-4FD0-BEFA-FD33C9F94EA2";
    
    // Полное имя класса, из которого вызывается метод получения отметки "Дата регистрации".
    [Sungero.Core.Public]
    public const string RegDateMarkKindClassName = "Sungero.Docflow.Functions.OfficialDocument";
    
    // Имя метода получения отметки "Дата регистрации".
    [Sungero.Core.Public]
    public const string RegDateMarkKindFunctionName = "GetRegistrationDateMarkContent";
    
    // Sid отметки "Регистрационный номер".
    [Sungero.Core.Public]
    public const string RegistrationNumberMarkKindSid = "B618C700-665A-4825-A523-EBDDEA5CDB32";
    
    // Полное имя класса, из которого вызывается метод получения отметки "Регистрационный номер".
    [Sungero.Core.Public]
    public const string RegNumberMarkKindClassName = "Sungero.Docflow.Functions.OfficialDocument";
    
    // Имя метода получения отметки "Регистрационный номер".
    [Sungero.Core.Public]
    public const string RegNumberMarkKindFunctionName = "GetRegistrationNumberMarkContent";
  }
}