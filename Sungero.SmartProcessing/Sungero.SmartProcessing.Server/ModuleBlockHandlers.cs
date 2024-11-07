using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;

namespace Sungero.SmartProcessing.Server.SmartProcessingBlocks
{
  partial class AnalyzeDocTypeAndFactsRecognitionBlockHandlers
  {

    public virtual void AnalyzeDocTypeAndFactsRecognitionBlockExecute()
    {
      var documents = _obj.AllAttachments
        .Where(a => Sungero.Docflow.OfficialDocuments.Is(a))
        .Select(a => Sungero.Docflow.OfficialDocuments.As(a));

      foreach (var document in documents)
      {
        if (document.VerificationState == Docflow.OfficialDocument.VerificationState.Completed)
          Docflow.PublicFunctions.OfficialDocument.StoreVerifiedPropertiesValues(document);

        Functions.Module.UpdateEntityRecognitionInfo(document);
      }
    }
  }

}