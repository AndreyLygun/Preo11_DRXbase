using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.SmartProcessing.VerificationAssignment;

namespace Sungero.SmartProcessing.Shared
{
  partial class VerificationAssignmentFunctions
  {
    /// <summary>
    /// Получить текст инструкции для задания.
    /// </summary>
    /// <returns>Текст инструкции.</returns>
    public virtual string GetInstruction()
    {
      var instructionSteps = new List<string>()
      {
        VerificationAssignments.Resources.InstructionStep1,
        VerificationAssignments.Resources.InstructionStep2,
        VerificationAssignments.Resources.InstructionStep3,
        VerificationAssignments.Resources.InstructionStep4,
        VerificationAssignments.Resources.InstructionStep5,
        VerificationAssignments.Resources.InstructionStep6,
        VerificationAssignments.Resources.InstructionStep7
      };
      return string.Join(Environment.NewLine, instructionSteps);
    }

    /// <summary>
    /// Получить документы для перекомплектования.
    /// </summary>
    /// <returns>Список документов.</returns>
    public virtual List<IOfficialDocument> GetDocumentsSuitableForRepacking()
    {
      Logger.Debug("Repacking. GetDocumentsSuitableForRepacking");
      var documents = this.GetOrderedDocuments();

      var notSuitableDocuments = new List<IOfficialDocument>();
      notSuitableDocuments.AddRange(this.GetEncryptedDocuments(documents));
      notSuitableDocuments.AddRange(this.GetInaccesssibleDocuments(documents));
      notSuitableDocuments.AddRange(this.GetNotSuitableExtensionDocuments(documents));
      notSuitableDocuments.AddRange(this.GetDocumentsWithoutVersion(documents));
      notSuitableDocuments.AddRange(this.GetDocumentsWithoutBody(documents));
      
      documents = documents.Except(notSuitableDocuments).ToList();
      this.LogDocumentsSuitableForRepackingFilter("GetDocumentsSuitableForRepacking", documents);
      if (!documents.Any())
        Logger.Debug("Repacking. No documents suitable for repacking.");
      return documents;
    }
    
    /// <summary>
    /// Получить список документов, отсортированных по порядку вложений в задаче.
    /// </summary>
    /// <returns>Список документов.</returns>
    public virtual List<IOfficialDocument> GetOrderedDocuments()
    {
      var mainTask = VerificationTasks.As(_obj.Task);
      var orderedAttachmentsIds = Functions.VerificationTask.Remote.GetOrderedAttachments(mainTask);
      return _obj.AllAttachments
        .OrderBy(x => orderedAttachmentsIds.IndexOf(x.Id))
        .Where(x => OfficialDocuments.Is(x))
        .Select(x => OfficialDocuments.As(x))
        .ToList();
    }
    
    /// <summary>
    /// Отфильтровать зашифрованные документы.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <returns>Список документов.</returns>
    public virtual System.Collections.Generic.IEnumerable<IOfficialDocument> GetEncryptedDocuments(System.Collections.Generic.IEnumerable<IOfficialDocument> documents)
    {
      var encryptedDocuments = documents.Where(d => d.IsEncrypted);
      this.LogDocumentsSuitableForRepackingFilter("GetEncryptedDocuments", encryptedDocuments);
      return encryptedDocuments;
    }

    /// <summary>
    /// Отфильтровать документы, на которые нет прав на изменение.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <returns>Список документов.</returns>
    public virtual System.Collections.Generic.IEnumerable<IOfficialDocument> GetInaccesssibleDocuments(System.Collections.Generic.IEnumerable<IOfficialDocument> documents)
    {
      var inaccessibleDocuments = documents.Where(d => !d.AccessRights.CanUpdate());
      this.LogDocumentsSuitableForRepackingFilter("GetInaccesssibleDocuments", inaccessibleDocuments);
      return inaccessibleDocuments;
    }
    
    /// <summary>
    /// Отфильтровать документы с неподходящим расширением.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <returns>Список документов.</returns>
    public virtual System.Collections.Generic.IEnumerable<IOfficialDocument> GetNotSuitableExtensionDocuments(System.Collections.Generic.IEnumerable<IOfficialDocument> documents)
    {
      var notSuitableExtensionDocuments = documents.Where(d => d.LastVersion != null &&
                                                          d.LastVersion.AssociatedApplication.Extension.ToLower() != Docflow.PublicConstants.OfficialDocument.PdfExtension);
      this.LogDocumentsSuitableForRepackingFilter("GetNotSuitableExtensionDocuments", notSuitableExtensionDocuments);
      return notSuitableExtensionDocuments;
    }
    
    /// <summary>
    /// Отфильтровать документы без версии.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <returns>Список документов.</returns>
    public virtual System.Collections.Generic.IEnumerable<IOfficialDocument> GetDocumentsWithoutVersion(System.Collections.Generic.IEnumerable<IOfficialDocument> documents)
    {
      var documentsWithoutVersion = documents.Where(d => d.LastVersion == null);
      this.LogDocumentsSuitableForRepackingFilter("GetDocumentsWithoutVersion", documentsWithoutVersion);
      return documentsWithoutVersion;
    }

    /// <summary>
    /// Отфильтровать документы без тела.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <returns>Список документов.</returns>
    public virtual System.Collections.Generic.IEnumerable<IOfficialDocument> GetDocumentsWithoutBody(System.Collections.Generic.IEnumerable<IOfficialDocument> documents)
    {
      var documentsWithoutBody = documents.Where(d => d.LastVersion != null &&
                                                 d.LastVersion.Body.Size == 0 &&
                                                 d.LastVersion.PublicBody.Size == 0);
      this.LogDocumentsSuitableForRepackingFilter("GetDocumentsWithoutBody", documentsWithoutBody);
      return documentsWithoutBody;
    }
    
    /// <summary>
    /// Вывести в лог сообщение о фильтрации документов для перекомплектования.
    /// </summary>
    /// <param name="name">Наименование фильтра.</param>
    /// <param name="documents">Отфильтрованные документы.</param>
    public virtual void LogDocumentsSuitableForRepackingFilter(string name, System.Collections.Generic.IEnumerable<IOfficialDocument> documents)
    {
      if (documents.Any())
        Logger.DebugFormat(Constants.VerificationAssignment.LogDocumentsSuitableForRepackingFilterMessageFormat,
                           name,
                           string.Join(", ", documents.Select(d => d.Id)));
    }
  }
}