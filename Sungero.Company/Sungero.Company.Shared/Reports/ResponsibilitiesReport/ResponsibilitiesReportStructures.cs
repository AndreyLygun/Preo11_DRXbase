using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Structures.ResponsibilitiesReport
{
  /// <summary>
  /// Строка отчета.
  /// </summary>
  [Public]
  partial class ResponsibilitiesReportTableLine
  {    
    public string ModuleName { get; set; }
    
    public string Responsibility { get; set; }
    
    public string Record { get; set; }
    
    public long? RecordId { get; set; }
    
    public string RecordHyperlink { get; set; }
    
    public int Priority { get; set; }
    
    public bool IsMain { get; set; }
    
    public string ReportSessionId { get; set; }
  }  
}