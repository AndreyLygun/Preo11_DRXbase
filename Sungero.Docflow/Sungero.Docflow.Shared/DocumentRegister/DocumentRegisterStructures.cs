using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.DocumentRegister
{
  /// <summary>
  /// Индекс регистрационного номера.
  /// </summary>
  partial class RegistrationNumberIndex
  {
    public int Index { get; set; }
    
    public string Postfix { get; set; }
    
    public string CorrectingPostfix { get; set; }
  }

  /// <summary>
  /// Префикс и постфикс регистрационного номера документа.
  /// </summary>
  partial class RegistrationNumberParts
  {
    public string Prefix { get; set; }
    
    public string Postfix { get; set; }
  }

  /// <summary>
  /// Составные элементы формата номера.
  /// </summary>
  partial class NumberFormatItemsValues
  {
    // ИД ведущего документа.
    public long LeadingDocumentId { get; set; }
    
    // ИД подразделения.
    public long DepartmentId { get; set; }
    
    // ИД нашей организации.
    public long BusinessUnitId { get; set; }
    
    // Номер ведущего документа.
    public string LeadingDocumentNumber { get; set; }
    
    // Код подразделения.
    public string DepartmentCode { get; set; }
    
    // Код нашей организации.
    public string BusinessUnitCode { get; set; }
    
    // Индекс дела, в которое будет помещён документ.
    public string CaseFileIndex { get; set; }
    
    // Код вида документа.
    public string DocumentKindCode { get; set; }
    
    // Код контрагента.
    public string CounterpartyCode { get; set; }
    
    // Код категории.
    public string CategoryCode { get; set; }
  }
  
}