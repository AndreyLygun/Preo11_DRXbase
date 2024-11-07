using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class AcquaintanceAssignmentFunctions
  {
    /// <summary>
    /// Открыть тело документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="versionNumber">Номер версии.</param>
    public virtual void ShowDocument(Sungero.Docflow.IOfficialDocument document, int versionNumber)
    {
      if (document != null)
      {
        var version = document.Versions.FirstOrDefault(v => v.Number == versionNumber);
        if (version != null)
          version.Open();
      }
    }
  }
}