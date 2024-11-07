using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ExchangeDocumentProcessingTask;
using Sungero.ExchangeCore.PublicFunctions;
using Sungero.Workflow;

namespace Sungero.Exchange.Server.ExchangeDocumentProcessingTaskBlocks
{
  partial class ExchangeDocumentProcessingAssignmentBlockHandlers
  {

    public virtual void ExchangeDocumentProcessingAssignmentBlockStartAssignment(Sungero.Exchange.IExchangeDocumentProcessingAssignment assignment)
    {
      assignment.Counterparty = _obj.Counterparty;
      assignment.CounterpartyDepartmentBox = _obj.CounterpartyDepartmentBox;
      assignment.Box = _obj.Box;
      assignment.Deadline = _obj.MaxDeadline;
      assignment.ExchangeService = _obj.ExchangeService;
      assignment.BusinessUnitBox = ExchangeCore.PublicFunctions.BoxBase.GetRootBox(_obj.Box);
      
      if (_block.GrantRightsByDefault.GetValueOrDefault())
      {
        var performer = assignment.Performer;
        
        // Выдать права на вложения исполнителю.
        foreach (var document in _obj.AllAttachments)
        {
          if (!document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, performer))
            document.AccessRights.Grant(performer, DefaultAccessRightsTypes.FullAccess);
          
          // Выдать права на связанные документы.
          var attachedDocument = Sungero.Docflow.OfficialDocuments.As(document);
          var relatedDocuments = attachedDocument.Relations.GetRelatedFromDocuments().ToList();
          foreach (var relatedDocument in relatedDocuments)
          {
            if (!relatedDocument.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Read, performer) &&
                !relatedDocument.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, performer) &&
                !relatedDocument.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, performer))
              relatedDocument.AccessRights.Grant(performer, DefaultAccessRightsTypes.Read);
          }
        }
      }
    }

    public virtual void ExchangeDocumentProcessingAssignmentBlockCompleteAssignment(Sungero.Exchange.IExchangeDocumentProcessingAssignment assignment)
    {
      if (assignment.Addressee != null && assignment.Result == Sungero.Exchange.ExchangeDocumentProcessingAssignment.Result.ReAddress)
      {
        _obj.Addressee = assignment.Addressee;
        _obj.Deadline = assignment.NewDeadline;
        assignment.Forward(assignment.Addressee, ForwardingLocation.Next);
      }
    }
  }

}