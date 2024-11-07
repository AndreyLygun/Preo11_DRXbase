using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement
{
  partial class AcquaintanceReportClientHandlers
  {
    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      // Не строить отчет, если он вызван не от документа или не от задачи.
      if (AcquaintanceReport.Document == null && AcquaintanceReport.Task == null)
        e.Cancel = true;
      
      // Не показывать диалог, если задан документ, версия и по кому формировать отчет.
      if (AcquaintanceReport.Document != null &&
          !string.IsNullOrWhiteSpace(AcquaintanceReport.DocumentVersion) &&
          !string.IsNullOrWhiteSpace(AcquaintanceReport.EmployeesAcquaintanceStatus))
        return;
      
      // Не показывать диалог, если задана задача и по кому формировать отчет.
      if (AcquaintanceReport.Task != null &&
          !string.IsNullOrWhiteSpace(AcquaintanceReport.EmployeesAcquaintanceStatus))
        return;
      
      // Показать диалог запроса параметров.
      var dialog = Dialogs.CreateInputDialog(Reports.Resources.AcquaintanceReport.AcquaintanceReportName);
      dialog.HelpCode = Constants.AcquaintanceReport.DialogHelpCode;
      
      // Контрол "Подразделение".
      var department = dialog.AddSelect(Resources.Department, false, Company.Departments.Null);
      var includeSubDepartments = dialog.AddBoolean(Reports.Resources.AcquaintanceReport.IncludeSubDepartments, true);
      
      // Контрол отбора по статусу выполнения.
      var acquaintanceStatuses = new string[] { Reports.Resources.AcquaintanceReport.ForAllPerformers,
        Reports.Resources.AcquaintanceReport.ForNotAcquaintedPerformers,
        Reports.Resources.AcquaintanceReport.ForAcquaintedPerformers };
      var dialogEmployeesAcquaintanceStatus = dialog.AddSelect(Reports.Resources.AcquaintanceReport.DialogAcquaintanceStatuses,
                                                               true,
                                                               Reports.Resources.AcquaintanceReport.ForAllPerformers)
        .From(acquaintanceStatuses);
      // Контрол "Версия".
      CommonLibrary.IDropDownDialogValue dialogVersion = null;
      var versionsWithTasks = new List<Content.IElectronicDocumentVersions>();
      if (this.AcquaintanceReport.Document != null && this.AcquaintanceReport.Document.HasVersions)
      {
        var versions = this.AcquaintanceReport.Document.Versions;
        var tasks = Docflow.PublicFunctions.OfficialDocument.Remote.GetAcquaintanceTasks(this.AcquaintanceReport.Document);
        
        foreach (var version in versions)
        {
          var filteredTasks = tasks
            .Where(t => t.AcquaintanceVersions.Any() && 
                   t.AcquaintanceVersions.First(v => v.IsMainDocument == true).Number == version.Number)
            .ToList();
          if (filteredTasks.Any())
            versionsWithTasks.Add(version);
        }
        
        if (versionsWithTasks.Any())
        {
          var dialogVersions = versionsWithTasks.Select(v => string.Format("{0}. {1}", v.Number.ToString(), v.Note.ToString()))
            .ToArray();
          dialogVersion = dialog.AddSelect(Docflow.Resources.Version, true, dialogVersions.LastOrDefault()).From(dialogVersions);
        }
      }
      
      dialog.Buttons.AddOkCancel();
      if (dialog.Show() == DialogButtons.Ok)
      {
        if (dialogVersion != null && dialogVersion.Value != null)
        {
          var selectedVersion = versionsWithTasks.First(v => string.Format("{0}. {1}", v.Number.ToString(), v.Note.ToString()) == dialogVersion.Value);
          AcquaintanceReport.DocumentVersion = selectedVersion.Number.ToString();
        }
        
        AcquaintanceReport.Department = department.Value;
        AcquaintanceReport.EmployeesAcquaintanceStatus = dialogEmployeesAcquaintanceStatus.Value;
        AcquaintanceReport.IncludeSubDepartments = includeSubDepartments.Value;
      }
      else
        e.Cancel = true;
    }
  }
}