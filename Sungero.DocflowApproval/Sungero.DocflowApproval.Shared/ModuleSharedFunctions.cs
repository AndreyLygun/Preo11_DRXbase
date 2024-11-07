using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Shared;
using Sungero.RecordManagement;
using Sungero.Workflow;

namespace Sungero.DocflowApproval.Shared
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Создать задачу на ознакомление подзадачей.
    /// </summary>
    /// <param name="parentAssignment">Родительское задание.</param>
    /// <returns>Задача на ознакомление.</returns>
    public virtual IAcquaintanceTask CreateAcquaintanceTaskAsSubtask(IAssignment parentAssignment)
    {
      var newAcqTask = AcquaintanceTasks.CreateAsSubtask(parentAssignment);
      return newAcqTask;
    }
    
    /// <summary>
    /// Получить действующие документы из группы вложений.
    /// </summary>
    /// <param name="primaryDocument">Основной документ.</param>
    /// <param name="groupAttachments">Вложения группы.</param>
    /// <returns>Список действующих документов.</returns>
    [Public]
    public virtual List<IElectronicDocument> GetNonObsoleteDocumentsFromAttachments(
      IElectronicDocument primaryDocument, System.Collections.Generic.ICollection<IEntity> groupAttachments)
    {
      var documents = groupAttachments
        .Select(x => Content.ElectronicDocuments.As(x))
        .Where(x => x != null);
      
      var obsoleteDocuments = documents
        .OfType<Sungero.Docflow.IOfficialDocument>()
        .Where(x => Docflow.PublicFunctions.OfficialDocument.IsObsolete(x));
      
      return documents.Except(obsoleteDocuments)
        .Where(x => !Equals(x, primaryDocument))
        .ToList();
    }
    
    /// <summary>
    /// Проверить наличие блокировок на документ перед подписанием.
    /// </summary>
    /// <param name="document">Электронный документ.</param>
    /// <returns>Текст с информацией о существующей блокировке, если она есть, иначе - пустая строка.</returns>
    public virtual string CheckDocumentLocksBeforeSigning(IElectronicDocument document)
    {
      var lockInfo = Locks.GetLockInfo(document);
      var canSignLockedDocument = Functions.Module.CanSignLockedDocument(document);

      if (lockInfo != null && lockInfo.IsLockedByOther && !canSignLockedDocument)
        return lockInfo.LockedMessage;

      if (document.LastVersion != null)
      {
        var lockInfoVersion = Locks.GetLockInfo(document.LastVersion.Body);
        if (lockInfoVersion != null && lockInfoVersion.IsLockedByOther)
          return lockInfoVersion.LockedMessage;
      }

      return string.Empty;
    }
    
    /// <summary>
    /// Получить признак возможности подписания документа при заблокированной карточке.
    /// </summary>
    /// <param name="document">Электронный документ.</param>
    /// <returns>Признак возможности подписания документа при заблокированной карточке.</returns>
    public virtual bool CanSignLockedDocument(IElectronicDocument document)
    {
      var hasCallContext = CallContext.CalledFrom(SigningAssignments.Info);
      var hasParams = ((Domain.Shared.IExtendedEntity)document).Params.ContainsKey(Docflow.PublicConstants.OfficialDocument.CanSignLockedDocument);
      return hasCallContext || hasParams;
    }
    
    /// <summary>
    /// Проверить наличие подчиненных поручений.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="status">Статус поручения.</param>
    /// <returns>True, если есть подпоручения, иначе false.</returns>
    public static bool HasSubActionItems(ITask task, Enumeration status)
    {
      return RecordManagement.PublicFunctions.ActionItemExecutionTask.Remote.HasSubActionItems(task, status);
    }
    
    /// <summary>
    /// Проверка заблокированности документов текущим сотрудником.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <returns>True - хотя бы один заблокирован, False - все свободны.</returns>
    /// <remarks>Системный пользователь не является сотрудником.</remarks>
    [Public]
    public virtual bool IsAnyDocumentLockedByCurrentEmployee(List<IElectronicDocument> documents)
    {
      if (Users.Current.IsSystem == true)
        return false;
      
      return this.IsAnyDocumentLockedByMe(documents);
    }
    
    /// <summary>
    /// Проверка заблокированности документов текущим пользователем.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <returns>True - хотя бы один заблокирован, False - все свободны.</returns>
    public virtual bool IsAnyDocumentLockedByMe(List<IElectronicDocument> documents)
    {
      return documents.Where(x => x != null)
        .Any(x => x.HasVersions && Locks.GetLockInfo(x.LastVersion.Body).IsLockedByMe ||
             x.HasPublicBody && Locks.GetLockInfo(x.LastVersion.PublicBody).IsLockedByMe ||
             Locks.GetLockInfo(x).IsLockedByMe);
    }
    
    /// <summary>
    /// Определить доступность поля "Адресаты" в задании.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True - доступно, False - иначе.</returns>
    public virtual bool AssignmentAddresseesIsEnabled(IOfficialDocument document)
    {
      if (document == null)
        return true;
      
      return Sungero.Docflow.PublicFunctions.OfficialDocument.TaskAdresseesFieldIsEnabled(document);
    }

  }
}