using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.SmartProcessing.Structures.RepackingSession
{
  /// <summary>
  /// Документ сессии перекомплектования.
  /// </summary>
  partial class RepackingDocument
  {
    /// <summary>
    /// Документ.
    /// </summary>
    public Sungero.Docflow.IOfficialDocument Document { get; set; }
    
    /// <summary>
    /// Версия.
    /// </summary>
    public Sungero.Content.IElectronicDocumentVersions Version { get; set; }
  }
  
}