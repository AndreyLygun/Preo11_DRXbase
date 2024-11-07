using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Shared;
using Sungero.Metadata;

namespace Sungero.SmartProcessing.Shared
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Получить приоритеты типов документов для определения ведущего документа в комплекте.
    /// </summary>
    /// <returns>Словарь с приоритетами типов.</returns>
    [Public]
    public virtual System.Collections.Generic.IDictionary<System.Type, int> GetPackageDocumentTypePriorities()
    {
      var leadingDocumentPriority = new Dictionary<System.Type, int>();
      leadingDocumentPriority.Add(typeof(RecordManagement.IIncomingLetter).GetFinalType(), 9);
      leadingDocumentPriority.Add(typeof(Contracts.IContract).GetFinalType(), 8);
      leadingDocumentPriority.Add(typeof(Contracts.ISupAgreement).GetFinalType(), 7);
      leadingDocumentPriority.Add(typeof(Sungero.FinancialArchive.IContractStatement).GetFinalType(), 6);
      leadingDocumentPriority.Add(typeof(Sungero.FinancialArchive.IWaybill).GetFinalType(), 5);
      leadingDocumentPriority.Add(typeof(Sungero.FinancialArchive.IUniversalTransferDocument).GetFinalType(), 4);
      leadingDocumentPriority.Add(typeof(Sungero.FinancialArchive.IIncomingTaxInvoice).GetFinalType(), 3);
      leadingDocumentPriority.Add(typeof(Sungero.Contracts.IIncomingInvoice).GetFinalType(), 2);
      leadingDocumentPriority.Add(typeof(Sungero.FinancialArchive.IOutgoingTaxInvoice).GetFinalType(), 1);
      leadingDocumentPriority.Add(typeof(Docflow.ISimpleDocument).GetFinalType(), 0);
      
      return leadingDocumentPriority;
    }

    /// <summary>
    /// Установить блокировки на документы.
    /// </summary>
    /// <param name="repackingDocuments">Список из документов и их версий.</param>
    /// <returns>True - если установили блокировки на все документы, иначе - False.</returns>
    public virtual bool TryLockRepackingSessionDocuments(List<Structures.RepackingSession.RepackingDocument> repackingDocuments)
    {
      var hasLocks = repackingDocuments
        .Where(x => Locks.GetLockInfo(x.Document).IsLocked || Locks.GetLockInfo(x.Version.Body).IsLocked)
        .Any();

      if (hasLocks)
        return false;

      foreach (var repackingDocument in repackingDocuments)
      {
        Locks.TryLock(repackingDocument.Document);
        Locks.TryLock(repackingDocument.Version.Body);
      }
      
      return true;
    }
    
    /// <summary>
    /// Снять блокировки с документов.
    /// </summary>
    /// <param name="repackingDocuments">Список из документов и их версий.</param>
    public virtual void UnlockDocumentsWithVersions(List<Structures.RepackingSession.RepackingDocument> repackingDocuments)
    {
      foreach (var repackingDocument in repackingDocuments)
      {
        var document = repackingDocument.Document;
        if (document != null)
        {
          if (Locks.GetLockInfo(document).IsLockedByMe)
            Locks.Unlock(document);
          
          if (document.Versions.Any())
          {
            if (Locks.GetLockInfo(document.LastVersion.Body).IsLockedByMe)
              Locks.Unlock(document.LastVersion.Body);

            if (repackingDocument.Version != null && Locks.GetLockInfo(repackingDocument.Version.Body).IsLockedByMe)
              Locks.Unlock(repackingDocument.Version.Body);
          }
        }
      }
    }
    
    /// <summary>
    /// Очистить статус верификации документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    public virtual void ClearVerificationState(IOfficialDocument document)
    {
      document.VerificationState = null;
    }
    
    /// <summary>
    /// Удалить версии документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    public virtual void DeleteDocumentVersions(IOfficialDocument document)
    {
      if (document.Versions.Any())
      {
        foreach (var version in document.Versions)
        {
          if (Locks.GetLockInfo(version.Body).IsLockedByMe)
            Locks.Unlock(version.Body);
        }
        document.Versions.Clear();
      }
    }
    
    /// <summary>
    /// Очистить связи.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <remarks>Очищает связи SimpleRelationName и AddendumRelationName, т.к. только они используются в пакетах.</remarks>
    public virtual void ClearDocumentRelations(Docflow.IOfficialDocument document)
    {
      if (!document.HasRelations)
        return;
      
      var relations = new Dictionary<string, IEnumerable<Content.IElectronicDocument>>();
      var relationTypes = new List<string>() { Docflow.PublicConstants.Module.SimpleRelationName, Docflow.PublicConstants.Module.AddendumRelationName };
      foreach (var relationType in relationTypes)
      {
        var relationDocuments = document.Relations.GetRelatedDocuments(relationType)
          .Where(l => OfficialDocuments.Is(l) &&
                 !Equals(OfficialDocuments.As(l).LeadingDocument, document));
        if (relationDocuments.Any())
          relations.Add(relationType, relationDocuments);
      }
      
      foreach (var relation in relations)
        foreach (var documentForRemove in relation.Value)
          document.Relations.Remove(relation.Key, documentForRemove);
      
      relations.Clear();
      
      foreach (var relationType in relationTypes)
      {
        var relationDocuments = document.Relations.GetRelatedFromDocuments(relationType)
          .Where(l => OfficialDocuments.Is(l) &&
                 !Equals(OfficialDocuments.As(l).LeadingDocument, document));
        if (relationDocuments.Any())
          relations.Add(relationType, relationDocuments);
      }
      
      foreach (var relation in relations)
        foreach (var documentForRemove in relation.Value)
          document.Relations.RemoveFrom(relation.Key, documentForRemove);
      document.State.Properties.LeadingDocument.IsRequired = false;
      document.LeadingDocument = null;
    }

    /// <summary>
    /// Конвертировать документ в простой и устаревший.
    /// </summary>
    /// <param name="document">Исходный документ.</param>
    public virtual void ConvertDocumentToSimpleObsolete(IOfficialDocument document)
    {
      try
      {
        // Очистить неактуальные коллекции для корректной работы NHibernate при последующем преобразовании типа документа (Bug 312437).
        if (Contracts.ContractualDocuments.Is(document))
          Contracts.ContractualDocuments.As(document).Milestones.Clear();
        
        var result = Docflow.SimpleDocuments.Is(document) ? document : Docflow.OfficialDocuments.As(document.ConvertTo(Docflow.SimpleDocuments.Info));
        Docflow.PublicFunctions.OfficialDocument.FillName(result);
        if (string.IsNullOrEmpty(result.Name))
          result.Name = result.DocumentKind.Name;

        result.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Obsolete;
        result.Save();
        Logger.DebugFormat("ConvertDocumentToSimpleObsolete. Document (Id = {0}) has been converted to simple and become obsolete.", document.Id);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("ConvertDocumentToSimpleObsolete. Cannot convert document (Id = {0}) to simple.", ex, document.Id);
      }
    }
    
    /// <summary>
    /// Удалить вложения из задания и задачи на верификацию.
    /// </summary>
    /// <param name="assignment">Задание на верификацию.</param>
    /// <param name="deletedDocuments">ИД удаляемых документов.</param>
    /// <returns>Сообщение об ошибках при удалении, если они были.</returns>
    public virtual string RemoveAttachmentsFromVerificationTask(IVerificationAssignment assignment, List<long> deletedDocuments)
    {
      if (!deletedDocuments.Any())
        return string.Empty;
      
      var task = assignment.Task;
      
      try
      {
        var taskAttachments = task.AllAttachments.Where(x => deletedDocuments.Contains(x.Id)).ToList();
        if (taskAttachments.Any())
        {
          foreach (var attachment in taskAttachments)
            task.Attachments.Remove(attachment);
          task.Save();
        }
        
        var assignmentAttachments = assignment.AllAttachments.Where(x => deletedDocuments.Contains(x.Id)).ToList();
        if (assignmentAttachments.Any())
        {
          foreach (var attachment in assignmentAttachments)
            assignment.Attachments.Remove(attachment);
          assignment.Save();
        }
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Repacking. RemoveAttachmentsFromVerificationTask. Cannot remove attachments from task (ID = {0})", ex, task.Id);
        return Resources.RepackingDeleteAttachmentErrorFormat(task.Id);
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Получить максимальный номер из вложений с типом Простой документ.
    /// </summary>
    /// <param name="documents">Список документов.</param>
    /// <returns>Максимальный номер из вложений.</returns>
    public virtual int GetLastAttachmentNumber(List<IOfficialDocument> documents)
    {
      var lastNumber = 0;
      foreach (var document in documents.Where(x => x.DocumentKind.DocumentType.DocumentTypeGuid == typeof(Docflow.ISimpleDocument).GetFinalType().GetTypeGuid().ToString()))
      {
        var numberSymbolIndex = document.Name.IndexOf(SmartProcessing.Constants.Module.NumberSign);
        if (numberSymbolIndex != 0)
        {
          int currentNumber;
          int.TryParse(document.Name.Substring(numberSymbolIndex + 1).Trim(), out currentNumber);
          lastNumber = (currentNumber > lastNumber) ? currentNumber : lastNumber;
        }
      }
      
      return lastNumber;
    }
    
    /// <summary>
    /// Проверить, можно ли сконвертировать документ в простой.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True - если можно, иначе - False.</returns>
    public virtual bool CanConvertDocument(IOfficialDocument document)
    {
      if (Docflow.PublicFunctions.OfficialDocument.CheckDeleteEntityAccessRights(document) &&
          !Docflow.PublicFunctions.OfficialDocument.Remote.HasSpecifiedTypeRelations(document) &&
          document.AccessRights.StrictMode == AccessRightsStrictMode.None &&
          !Docflow.PublicFunctions.OfficialDocument.Remote.HasApprovalTasksWithCurrentDocument(document))
        return true;
      
      return false;
    }
  }
}