using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using ExchDocumentType = Sungero.Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType;
using MessageType = Sungero.Exchange.ExchangeDocumentInfo.MessageType;
using ReportResources = Sungero.Docflow.Reports.Resources;

namespace Sungero.Docflow
{
  partial class ExchangeOrderReportServerHandlers
  {
    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.ExchangeOrderReport.SourceTableName, ExchangeOrderReport.SessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var reportSessionId = System.Guid.NewGuid().ToString();
      ExchangeOrderReport.SessionId = reportSessionId;
      
      var document = ExchangeOrderReport.Entity;
      var reportRows = Exchange.PublicFunctions.Module.GetExchangeOrderReportRows(document);
      
      foreach (var element in reportRows)
        element.ReportSessionId = reportSessionId;

      Functions.Module.WriteStructuresToTable(Constants.ExchangeOrderReport.SourceTableName, reportRows);
    }
    
    private static string DateFormat(DateTime? datetime)
    {
      if (datetime == null)
        return null;
      
      return Functions.Module.ToTenantTime(datetime.Value).ToUserTime().ToString("g");
    }
  }
}