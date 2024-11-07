using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentComparisonInfo;

namespace Sungero.Docflow.Client
{
  partial class DocumentComparisonInfoActions
  {

    public virtual void OpenResultPdf(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.ShowDocumentComparisonResults(_obj);
    }

    public virtual bool CanOpenResultPdf(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      // Открыть Результаты сравнения может только инициатор.
      return Equals(_obj.Author, Users.Current) && _obj.ProcessingStatus == ProcessingStatus.Compared;
    }

  }

}