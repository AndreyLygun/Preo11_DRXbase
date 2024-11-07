using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.ExchangeCore.Structures.Module
{
  /// <summary>
  /// Информация для штампа с подписями.
  /// </summary>
  [Public(Isolated = true)]
  partial class DpadSignatureStamp
  {
    public int StampWidth { get; set; }
    
    public Sungero.ExchangeCore.Structures.Module.IDpadPageStampInfo PageStampInfo { get; set; }
    
    public List<Sungero.ExchangeCore.Structures.Module.IDpadSignatureInfo> Signatures { get; set; }
  }
  
  /// <summary>
  /// Информация о подписи.
  /// </summary>
  [Public(Isolated = true)]
  partial class DpadSignatureInfo
  {
    public string CertificateIssuer { get; set; }
    
    public string CertificateSerialNumber { get; set; }
    
    public string FormalizedPoATitle { get; set; }
    
    public string FormalizedPoAUnifiedNumber { get; set; }
    
    public string OrganizationInfo { get; set; }
    
    public string PersonInfo { get; set; }
    
    public string SigningDate { get; set; }
    
    public int SignIcon { get; set; }
    
    public string Status { get; set; }
  }
  
  /// <summary>
  /// Информация о документе.
  /// </summary>
  [Public(Isolated = true)]
  partial class DpadPageStampInfo
  {
    public string Title { get; set; }
    
    public string DocumentId { get; set; }
  }
}