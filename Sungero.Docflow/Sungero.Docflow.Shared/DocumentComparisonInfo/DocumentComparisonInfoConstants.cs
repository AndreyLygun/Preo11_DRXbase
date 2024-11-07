using System;
using Sungero.Core;

namespace Sungero.Docflow.Constants
{
  public static class DocumentComparisonInfo
  {
    /// <summary>
    /// Имя параметра "Срок хранения результатов сравнения".
    /// </summary>
    public const string DaysToStoreParamName = "DaysToStoreDocumentComparisonInfo";
    
    /// <summary>
    /// Срок хранения результатов сравнения по умолчанию в рабочих днях.
    /// </summary>
    public const int DefaultDaysToStore = 3;
    
    /// <summary>
    /// Минимальный размер версии для сравнения в байтах.
    /// </summary>
    public const int MinimumVersionSize = 0;
  }
}