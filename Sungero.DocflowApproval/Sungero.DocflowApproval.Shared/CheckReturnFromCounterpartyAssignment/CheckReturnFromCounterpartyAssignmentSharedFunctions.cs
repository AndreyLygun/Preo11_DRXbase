using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.DocflowApproval.CheckReturnFromCounterpartyAssignment;

namespace Sungero.DocflowApproval.Shared
{
  partial class CheckReturnFromCounterpartyAssignmentFunctions
  {

    /// <summary>
    /// Проверить возможность выполнения задания.
    /// </summary>
    /// <returns>True, если можно выполнить задание, иначе - false.</returns>
    public virtual bool CanCompleteAssignment()
    {
      var mainDocument = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      if (!Sungero.Docflow.OfficialDocuments.Is(mainDocument))
        return true;
      
      var documentExchangeState = Sungero.Docflow.OfficialDocuments.As(mainDocument).ExchangeState;
      return documentExchangeState != Sungero.Docflow.OfficialDocument.ExchangeState.SignAwaited;
    }
    
    /// <summary>
    /// Проверить, не заблокированы ли документы текущим пользователем.
    /// </summary>
    /// <returns>True - хотя бы один заблокирован, False - все свободны.</returns>
    public virtual bool AreDocumentsLockedByMe()
    {
      var documents = new List<IElectronicDocument>();
      documents.Add(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      documents.AddRange(_obj.AddendaGroup.ElectronicDocuments);
      
      return Functions.Module.IsAnyDocumentLockedByCurrentEmployee(documents);
    }

  }
}