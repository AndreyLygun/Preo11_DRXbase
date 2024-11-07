using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.OfficialDocument;
using Sungero.DocflowApproval.CheckReturnFromCounterpartyAssignment;

namespace Sungero.DocflowApproval.Server
{
  partial class CheckReturnFromCounterpartyAssignmentFunctions
  {

    /// <summary>
    /// Связать с основным документом документы из группы Приложения, если они не были связаны ранее.
    /// </summary>
    public virtual void RelateAddedAddendaToPrimaryDocument()
    {
      var primaryDocument = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      if (primaryDocument == null)
        return;
      
      Logger.DebugFormat("CheckReturnAssignment (ID = {0}). Add relation with type Addendum to primary document (ID = {1})",
                         _obj.Id, primaryDocument.Id);
      
      var nonRelatedAddenda = Functions.Module.GetNonObsoleteDocumentsFromAttachments(primaryDocument, _obj.AddendaGroup.All);
      Functions.Module.RelateDocumentsToPrimaryDocumentAsAddenda(primaryDocument, nonRelatedAddenda);
    }
    
    /// <summary>
    /// Проверить основной документ на нехватку прав.
    /// </summary>
    /// <returns>True - на документ не хватает прав. False - права есть, или их выдавать не нужно.</returns>
    [Remote(IsPure = true)]
    public virtual bool NeedRightsToMainDocument()
    {
      var document = Sungero.Content.ElectronicDocuments.Null;
      AccessRights.AllowRead(
        () =>
        {
          document = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
        });
      
      return document != null && !document.AccessRights.CanRead();
    }

    /// <summary>
    /// Получить список сотрудников, у которых можно запросить продление срока.
    /// </summary>
    /// <returns>Список сотрудников.</returns>
    public virtual List<IUser> GetAssigneesForDeadlineExtension()
    {
      return Functions.Module.GetDeadlineAssigneesWithoutPerformer(_obj);
    }
    
    /// <summary>
    /// Продлить срок задания.
    /// </summary>
    /// <param name="newDeadline">Новый срок.</param>
    /// <returns>True - продление срока задания прошло успешно, False - неуспешно.</returns>
    public virtual bool ExtendAssignmentDeadline(DateTime newDeadline)
    {
      _obj.Deadline = newDeadline;

      var document = Docflow.OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (document != null)
        Docflow.PublicFunctions.OfficialDocument.ExtendTrackingDeadline(document, newDeadline, _obj.Task);
      return true;
    }
    
    /// <summary>
    /// Проверить, нужно ли выполнить автоматически задание на контроль возврата.
    /// </summary>
    /// <returns>True, если задание нужно выполнить, иначе - false.</returns>
    public virtual bool NeedAutoCompleteAssignment()
    {
      var document = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (document == null)
        return false;
      
      return Exchange.CancellationAgreements.Is(document) && document.ExchangeState == ExchangeState.Sent;
    }

  }
}