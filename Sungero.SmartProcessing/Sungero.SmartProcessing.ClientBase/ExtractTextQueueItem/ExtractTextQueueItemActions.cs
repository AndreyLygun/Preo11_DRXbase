using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.ExtractTextQueueItem;

namespace Sungero.SmartProcessing.Client
{
  partial class ExtractTextQueueItemActions
  {
    public virtual void ShowExtractedText(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var resultName = string.Format("{0}.{1}", _obj.Name, "txt");
      _obj.ExtractedText.Open(resultName);
    }

    public virtual bool CanShowExtractedText(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.ProcessingStatus == ProcessingStatus.Success;
    }

  }

}