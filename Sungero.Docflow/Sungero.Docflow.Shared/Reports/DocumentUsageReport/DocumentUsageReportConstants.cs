using System;

namespace Sungero.Docflow.Constants
{
  public static class DocumentUsageReport
  {
    /// <summary>
    /// Код диалога.
    /// </summary>
    public const string HelpCode = "Sungero_Docflow_DocumentUsageReportDialog";
    
    /// <summary>
    /// Имя таблицы для выборки сотрудников.
    /// </summary>
    public const string EmployeeTableName = "Sungero_Reports_DocumentUsageReport_Employees";
    
    /// <summary>
    /// Имя таблицы для выборки истории по документам в разрезе сотрудников.
    /// </summary>
    public const string HistoryTableName = "Sungero_Reports_DocumentUsageReport_History";
    
    /// <summary>
    /// Формат даты для запроса InsertIntoHistoryTable.
    /// </summary>
    public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
  }
}