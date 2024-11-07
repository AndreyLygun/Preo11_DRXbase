using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement
{
  partial class IncomingDocumentsProcessingReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      // Очистка временной таблицы.
      Docflow.PublicFunctions.Module.DeleteReportData(IncomingDocumentsProcessingReport.DocumentsTableName, IncomingDocumentsProcessingReport.ReportSessionId);
      
      // Удалить временные таблицы.
      Docflow.PublicFunctions.Module.DropReportTempTables(new[] { IncomingDocumentsProcessingReport.AvailableIdsTableName,
                                                            IncomingDocumentsProcessingReport.TasksTableName,
                                                            IncomingDocumentsProcessingReport.HyperlinksTableName });
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      // Удалить временные таблицы.
      IncomingDocumentsProcessingReport.DocumentsTableName = Constants.IncomingDocumentsProcessingReport.IncomingDocumentsProcessingReportTableName;
      var incomingDocumentsProcessingReportShortName = "IncDocPro";
      IncomingDocumentsProcessingReport.AvailableIdsTableName = Docflow.PublicFunctions.Module.GetReportTableName(incomingDocumentsProcessingReportShortName, Users.Current.Id);
      IncomingDocumentsProcessingReport.TasksTableName = Docflow.PublicFunctions.Module.GetReportTableName(incomingDocumentsProcessingReportShortName, Users.Current.Id, "tasks");
      IncomingDocumentsProcessingReport.HyperlinksTableName = Docflow.PublicFunctions.Module.GetReportTableName(incomingDocumentsProcessingReportShortName, Users.Current.Id, "hyperlinks");
      Docflow.PublicFunctions.Module.DropReportTempTables(new[] { IncomingDocumentsProcessingReport.AvailableIdsTableName,
                                                            IncomingDocumentsProcessingReport.TasksTableName,
                                                            IncomingDocumentsProcessingReport.HyperlinksTableName });
      
      // Создать временную таблицу с ИД доступных задач.
      var availableDocumentsIds = Sungero.Docflow.IncomingDocumentBases.GetAll()
        .Where(d => d.RegistrationState == Docflow.OfficialDocument.RegistrationState.Registered)
        .Where(d => d.RegistrationDate >= IncomingDocumentsProcessingReport.BeginDate.Value)
        .Where(d => d.RegistrationDate <= IncomingDocumentsProcessingReport.EndDate.Value)
        .OrderByDescending(d => d.RegistrationDate)
        .Select(d => d.Id);
      Docflow.PublicFunctions.Module.CreateIncomingDocumentsReportTempTable(IncomingDocumentsProcessingReport.AvailableIdsTableName, availableDocumentsIds);
      
      // Костыль для передачи кода культуры в CONVERT, потому что FORMAT в SQL 2008 не работает.
      var culture = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
      var cultureCode = 104;
      var cultureCodePG = "DD.MM.YYYY";
      if (culture == "en-US")
      {
        cultureCode = 101;
        cultureCodePG = "MM/DD/YYYY";
      }
      if (culture == "en-GB")
      {
        cultureCode = 103;
        cultureCodePG = "DD/MM/YYYY";
      }
      IncomingDocumentsProcessingReport.ReportSessionId = System.Guid.NewGuid().ToString();
      
      var commandParams = new string[] {
        IncomingDocumentsProcessingReport.AvailableIdsTableName,
        IncomingDocumentsProcessingReport.TasksTableName };
      
      var commandText = Queries.IncomingDocumentsProcessingReport.TasksQuery;
      Functions.Module.ExecuteSQLCommandFormat(commandText, commandParams);
      
      commandText = Queries.IncomingDocumentsProcessingReport.UpdateExecutionDate;
      commandParams = new string[] { IncomingDocumentsProcessingReport.TasksTableName };
      Functions.Module.ExecuteSQLCommandFormat(commandText, commandParams);
      
      commandParams = new string[] {
        IncomingDocumentsProcessingReport.AvailableIdsTableName,
        IncomingDocumentsProcessingReport.DocumentsTableName,
        IncomingDocumentsProcessingReport.TasksTableName,
        Sungero.RecordManagement.Reports.Resources.IncomingDocumentsProcessingReport.OnReview,
        Sungero.RecordManagement.Reports.Resources.IncomingDocumentsProcessingReport.Sending,
        Sungero.RecordManagement.Reports.Resources.IncomingDocumentsProcessingReport.WithoutExecut,
        Sungero.RecordManagement.Reports.Resources.IncomingDocumentsProcessingReport.OnExecution,
        Sungero.RecordManagement.Reports.Resources.IncomingDocumentsProcessingReport.Executed,
        Sungero.RecordManagement.Reports.Resources.IncomingDocumentsProcessingReport.Aborted,
        IncomingDocumentsProcessingReport.ReportSessionId,
        cultureCode.ToString(),
        cultureCodePG };
      
      commandText = Queries.IncomingDocumentsProcessingReport.DataQuery;
      Functions.Module.ExecuteSQLCommandFormat(commandText, commandParams);
      
      var hyperlinks = new List<Structures.IncomingDocumentsProcessingReport.Hyperlinks>();
      var hyperlinksCreateTableQuery = string.Format(Queries.IncomingDocumentsProcessingReport.CreateHyperlinksTableQuery,
                                                     IncomingDocumentsProcessingReport.HyperlinksTableName);
      var docIdSelectQuery = string.Format(Queries.IncomingDocumentsProcessingReport.DocIdSelectQuery,
                                           IncomingDocumentsProcessingReport.DocumentsTableName,
                                           IncomingDocumentsProcessingReport.ReportSessionId);
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = hyperlinksCreateTableQuery;
        command.ExecuteNonQuery();
        
        // Для всех строк вычислить и записать в таблицу гиперссылку.
        command.CommandText = docIdSelectQuery;
        var reader = command.ExecuteReader();
        while (reader.Read())
        {
          var docId = reader.GetInt64(0);
          var documentHyperlink = Hyperlinks.Get(Sungero.Docflow.IncomingDocumentBases.Info, docId);
          hyperlinks.Add(Structures.IncomingDocumentsProcessingReport.Hyperlinks.Create(docId, documentHyperlink));
        }
      }
      
      Docflow.PublicFunctions.Module.WriteStructuresToTable(IncomingDocumentsProcessingReport.HyperlinksTableName, hyperlinks);
      
      var updateCommand = string.Format(Queries.IncomingDocumentsProcessingReport.UpdateHyperlinksQuery,
                                        IncomingDocumentsProcessingReport.DocumentsTableName,
                                        IncomingDocumentsProcessingReport.HyperlinksTableName,
                                        IncomingDocumentsProcessingReport.ReportSessionId);
      Docflow.PublicFunctions.Module.ExecuteSQLCommand(updateCommand);
    }
  }
}
