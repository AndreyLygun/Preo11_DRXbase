using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.SmartProcessing.VerificationTask;

namespace Sungero.SmartProcessing.Shared
{
  partial class VerificationTaskFunctions
  {
    /// <summary>
    /// Проверить наличие документа в задаче и наличие прав на него.
    /// </summary>
    /// <returns>True, если с документом можно работать.</returns>
    public virtual bool HasDocumentAndCanRead()
    {
      return _obj.AllAttachments.Any();
    }
    
    /// <summary>
    /// Получить главный документ комплекта, исходя из связей документов.
    /// </summary>
    /// <returns>Главный документ комплекта.</returns>
    public virtual IOfficialDocument GetLeadingDocumentByRelations()
    {
      // Главный документ комплекта:
      // -- Не привязан к другим документам этого комплекта
      // -- К нему привязаны другие документы комплекта.
      var attachedDocuments = this.GetAttachedDocuments();
      var attachedDocumentsIds = attachedDocuments.Select(x => x.Id).ToList();
      var leadingDocuments = attachedDocuments.Where(x => !x.Relations.GetRelatedFromDocuments().Any(y => attachedDocumentsIds.Contains(y.Id)));
      var leadingDocumentsIds = leadingDocuments.Select(x => x.Id).ToList();
      var subordinateDocuments = attachedDocuments.Where(x => !leadingDocumentsIds.Contains(x.Id));
      return leadingDocuments
        .Where(x => subordinateDocuments.Any(y => y.Relations.GetRelatedFromDocuments().Any(z => z.Id == x.Id)))
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить вложенные документы кроме тех, которые будут удалены.
    /// </summary>
    /// <param name="documentsForDeletion">Список ИД документов, которые будут удалены.</param>
    /// <returns>Список вложенных документы кроме тех, которые будут удалены.</returns>
    public virtual List<IOfficialDocument> GetAttachedDocumentsWithoutDeleted(List<long> documentsForDeletion)
    {
      return this.GetAttachedDocuments()
        .Where(x => !documentsForDeletion.Contains(x.Id))
        .ToList();
    }
    
    /// <summary>
    /// Получить вложенные документы.
    /// </summary>
    /// <returns>Вложенные документы.</returns>
    public virtual List<IOfficialDocument> GetAttachedDocuments()
    {
      return _obj.AllAttachments
        .Where(x => OfficialDocuments.Is(x))
        .Select(x => OfficialDocuments.As(x))
        .ToList();
    }
  }
}