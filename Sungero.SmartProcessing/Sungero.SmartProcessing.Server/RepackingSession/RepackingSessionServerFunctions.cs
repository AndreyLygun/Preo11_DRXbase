using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.OfficialDocument;
using Sungero.Domain.Shared;
using Sungero.SmartProcessing.RepackingSession;

namespace Sungero.SmartProcessing.Server
{
  partial class RepackingSessionFunctions
  {
    #region URL сессии перекомплектования.
    
    /// <summary>
    /// Получить URL сессии перекомплектования.
    /// </summary>
    /// <returns>URL сессии перекомплектования.</returns>
    [Remote]
    public virtual string GetUrl()
    {
      return string.Format(Constants.RepackingSession.UrlTemplate,
                           this.GetClientUrl(),
                           _obj.SessionId);
    }

    /// <summary>
    /// Получить URL клиента перекомплектования.
    /// </summary>
    /// <returns>URL клиента перекомплектования.</returns>
    public virtual string GetClientUrl()
    {
      var userUrl = new Uri(Hyperlinks.Get(Users.Current));
      return string.Format(Constants.RepackingSession.ClientUrlTemplate,
                           userUrl.Scheme,
                           userUrl.Host,
                           userUrl.Port,
                           this.GetConfigSettingsWebHostPathBase());
    }
    
    /// <summary>
    /// Получить относительный путь к вебклиенту.
    /// </summary>
    /// <returns>Путь к вебклиенту.</returns>
    public virtual string GetConfigSettingsWebHostPathBase()
    {
      return (new ConfigSettings.ConfigSettingsGetter()).Get<string>(Constants.RepackingSession.WebHostPathBaseParamName);
    }
    
    #endregion
    
    #region Удаление документа.
    
    /// <summary>
    /// Обработка удаленных из перекомплектования документов.
    /// </summary>
    /// <param name="deletedDocuments">Список ИД удаленных документов.</param>
    public virtual void DeleteDocumentsAfterRepacking(List<long> deletedDocuments)
    {
      Logger.DebugFormat("Repacking. DeleteDocumentsAfterRepacking (session={0})", _obj.SessionId);
      var idsForRemoveFromAttachments = new List<long>();
      foreach (var documentId in deletedDocuments)
      {
        var document = Docflow.OfficialDocuments.GetAll().Where(x => x.Id == documentId).FirstOrDefault();
        if (document == null)
          continue;
        
        if (!document.AccessRights.CanDeleteVersion())
        {
          this.AddNewErrorText(Resources.RepackingDocumentSaveErrorFormat(document.Id));
          Logger.DebugFormat("Repacking. DeleteDocumentsAfterRepacking (session={0}). Cannot delete version. Document (ID = {0})", document.Id);
          continue;
        }
        
        try
        {
          var removeFromAttachments = Functions.Module.TryMakeDocumentDeleted(document);
          if (removeFromAttachments)
            idsForRemoveFromAttachments.Add(documentId);
        }
        catch (Exception ex)
        {
          this.AddNewErrorText(Resources.RepackingDocumentSaveErrorFormat(document.Id));
          Logger.ErrorFormat("Repacking. DeleteDocumentsAfterRepacking (session={0}). Delete failed: {1}", ex, _obj.SessionId, ex.Message);
        }
      }
      
      var assignment = VerificationAssignments.GetAll().Where(t => t.Id == _obj.AssignmentId.Value).FirstOrDefault();
      if (assignment != null)
      {
        var errorsText = Functions.Module.RemoveAttachmentsFromVerificationTask(assignment, idsForRemoveFromAttachments);
        if (!string.IsNullOrEmpty(errorsText))
          this.AddNewErrorText(errorsText);
      }
    }
    
    #endregion
    
    #region Изменение документа.
    
    /// <summary>
    /// Изменение тел документов по результатам перекомплектования.
    /// </summary>
    /// <param name="changedDocuments">Информация с изменениями.</param>
    /// <param name="builderGuid">Гуид сборщика.</param>
    public virtual void ChangeDocumentsAfterRepacking(List<Structures.Module.IChangedDocument> changedDocuments, Guid builderGuid)
    {
      foreach (var changedDocument in changedDocuments)
      {
        var document = Sungero.Docflow.OfficialDocuments.GetAll().FirstOrDefault(x => x.Id == changedDocument.OriginalDocumentId);
        if (document == null)
        {
          this.AddNewErrorText(Resources.RepackingDocumentNotFoundErrorFormat(changedDocument.OriginalDocumentId));
          continue;
        }
        var pages = changedDocument.Pages;
        this.BuildNewVersion(builderGuid, document, pages);
        var versionCreatedSuccessfully = this.TrySaveDocument(document);
        if (versionCreatedSuccessfully)
        {
          var originalDocument = _obj.OriginalDocuments.Where(x => x.DocumentId == changedDocument.OriginalDocumentId).FirstOrDefault();
          if (originalDocument != null && document.LastVersion != null)
            originalDocument.ResultVersionNumber = document.LastVersion.Number;
        }
        
        this.ReorderFactsHighlights(document, changedDocument);
      }
    }
    
    /// <summary>
    /// Изменить позиции фактов у перемещенных страниц.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="changedDocument">Информация с изменениями в документе.</param>
    public virtual void ReorderFactsHighlights(IOfficialDocument document, Structures.Module.IChangedDocument changedDocument)
    {
      var recognitionInfo = Sungero.Commons.PublicFunctions.EntityRecognitionInfo.Remote.GetEntityRecognitionInfo(document);
      if (recognitionInfo != null)
      {
        var pagesFromOriginalDocument = changedDocument.Pages.Where(l => l.DocumentId == changedDocument.OriginalDocumentId);
        var pagesNewOrder = pagesFromOriginalDocument.ToDictionary(l => (l.Number + 1).ToString(), l => (changedDocument.Pages.IndexOf(l) + 1).ToString());
        var factsIdsFromDeletedPages = Commons.PublicFunctions.EntityRecognitionInfo.UpdatePagesInPositions(recognitionInfo, pagesNewOrder);
        if (factsIdsFromDeletedPages.Any())
          Commons.PublicFunctions.EntityRecognitionInfo.ClearFactAndPropertyLink(recognitionInfo, factsIdsFromDeletedPages);
        try
        {
          if (recognitionInfo.State.IsChanged)
            recognitionInfo.Save();
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("Repacking. ReorderFactsHighlights. Cannot save Entity Recognition Info after facts position update (ID = {0})", ex, recognitionInfo.Id);
        }
      }
    }
    
    #endregion
    
    #region Создание нового документа.
    
    /// <summary>
    /// Создание нового документа по результатам перекомплектования.
    /// </summary>
    /// <param name="newDocuments">Информация о новых документах.</param>
    /// <param name="builderGuid">Гуид сборщика.</param>
    /// <param name="task">Задача на верификацию.</param>
    /// <param name="documentsForDeletion">Список ИД документов на удаление.</param>
    public virtual void CreateNewDocumentsAfterRepacking(List<Structures.Module.INewDocument> newDocuments,
                                                         Guid builderGuid,
                                                         IVerificationTask task,
                                                         List<long> documentsForDeletion)
    {
      var documents = new List<IOfficialDocument>();
      var saveAsSimpleDocuments = new List<Structures.Module.INewDocument>();
      var simpleDocumentTypeId = typeof(Docflow.ISimpleDocument).GetFinalType().GetTypeGuid().ToString();
      
      var taskDocuments = Functions.VerificationTask.GetAttachedDocumentsWithoutDeleted(task, documentsForDeletion);
      var leadingDocument = Functions.VerificationTask.GetLeadingDocumentByRelations(task);
      if (leadingDocument == null ||
          leadingDocument != null && documentsForDeletion.Contains(leadingDocument.Id))
        leadingDocument = Functions.Module.GetNewLeadingDocumentByType(newDocuments, taskDocuments);
      
      foreach (var newDocument in newDocuments.OrderBy(d => d.IsLeading != true))
      {
        var document = this.CreateNewDocumentAfterRepacking(newDocument, leadingDocument, builderGuid);
        if (document != null)
        {
          _obj.NewDocuments.AddNew().DocumentId = document.Id;
          documents.Add(document);
          if (newDocument.IsLeading == true)
            leadingDocument = document;
        }
        else if (newDocument.TypeId != simpleDocumentTypeId)
          saveAsSimpleDocuments.Add(newDocument);
      }
      
      // Если были ошибки при создании, создать простой документ.
      foreach (var newDocument in saveAsSimpleDocuments)
      {
        newDocument.TypeId = simpleDocumentTypeId;
        var document = this.CreateNewDocumentAfterRepacking(newDocument, leadingDocument, builderGuid);
        if (document != null)
        {
          _obj.NewDocuments.AddNew().DocumentId = document.Id;
          documents.Add(document);
        }
        else
          Logger.ErrorFormat("Repacking. CreateNewDocumentsAfterRepacking. Cannot create new document (Name = {0}, task Id = {1})", newDocument.Name, task.Id.ToString());
      }
      
      if (task != null && documents.Any())
      {
        try
        {
          Functions.VerificationTask.AddAttachments(task, documents);
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("Repacking. AddAttachmentsToVerificationTask. Cannot add attachments to task (ID = {0})", ex, task.Id);
          this.AddNewErrorText(Resources.RepackingAddAttachmentErrorFormat(task.Id));
        }
      }
    }
    
    /// <summary>
    /// Создать новый документ.
    /// </summary>
    /// <param name="newDocument">Информация о новом документе.</param>
    /// <param name="leadingDocument">Ведущий документ.</param>
    /// <param name="builderGuid">Гуид сборщика.</param>
    /// <returns>Новый документ. Null - если документ не удалось создать.</returns>
    public virtual IOfficialDocument CreateNewDocumentAfterRepacking(Structures.Module.INewDocument newDocument,
                                                                     IOfficialDocument leadingDocument,
                                                                     Guid builderGuid)
    {
      try
      {
        var document = this.CreateDocumentByTypeGuid(newDocument.TypeId);
        if (document != null)
        {
          Functions.Module.FillDocumentKind(document);
          this.BuildNewVersion(builderGuid, document, newDocument.Pages);
          
          var properties = new Dictionary<string, object>();
          if (leadingDocument != null)
          {
            properties.Add(document.Info.Properties.LeadingDocument.Name, leadingDocument);
            document.Relations.AddFrom(Docflow.PublicConstants.Module.AddendumRelationName, leadingDocument);
          }
          if (!string.IsNullOrWhiteSpace(newDocument.Name))
            properties.Add(document.Info.Properties.Name.Name, newDocument.Name);
          Docflow.PublicFunctions.OfficialDocument.FillRequiredProperties(document, properties);
          document.VerificationState = VerificationState.InProcess;
          if (string.IsNullOrWhiteSpace(newDocument.Name))
            document.Name = Docflow.PublicFunctions.OfficialDocument.GetGeneratedDocumentName(document);
          
          // Добавить параметр для корректной записи в историю.
          var documentParams = ((Domain.Shared.IExtendedEntity)document).Params;
          var historyParamName = Docflow.PublicConstants.OfficialDocument.AddHistoryCommentRepackingAddNewDocument;
          documentParams.Add(historyParamName, true);
          var successfullySaved = this.TrySaveDocument(document);
          documentParams.Remove(historyParamName);
          if (!successfullySaved)
            return null;
        }
        return document;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Repacking. CreateDocument. Cannot create document for document type guid {0}", ex, newDocument.TypeId);
        var simpleDocumentTypeId = typeof(Docflow.ISimpleDocument).GetFinalType().GetTypeGuid().ToString();
        if (newDocument.TypeId == simpleDocumentTypeId)
          this.AddNewErrorText(RepackingSessions.Resources.RepackingNewDocumentSaveErrorFormat(newDocument.Name, newDocument.TypeId));
        return null;
      }
    }

    /// <summary>
    /// Создать документ указанного типа.
    /// </summary>
    /// <param name="typeGuid">Гуид типа документа.</param>
    /// <returns>Документ.</returns>
    public virtual IOfficialDocument CreateDocumentByTypeGuid(string typeGuid)
    {
      var typeDoc = Sungero.Domain.Shared.TypeExtension.GetTypeByGuid(Guid.Parse(typeGuid));
      var document = Sungero.Docflow.OfficialDocuments.Null;
      using (var session = new Sungero.Domain.Session())
      {
        document = Sungero.Docflow.OfficialDocuments.As(session.Create(typeDoc));
      }
      return document;
    }
    
    #endregion

    /// <summary>
    /// Сохранить документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True - если документ успешно сохранен, иначе - false.</returns>
    public virtual bool TrySaveDocument(IOfficialDocument document)
    {
      try
      {
        document.Save();
        return true;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Repacking. SaveDocument. Cannot save document (ID = {0})", ex, document.Id);
        this.AddNewErrorText(Resources.RepackingDocumentSaveErrorFormat(document.Id));
        return false;
      }
    }
    
    /// <summary>
    /// Создать новую версию документа на основании переданных страниц.
    /// </summary>
    /// <param name="builderGuid">Guid сборщика PDF документов.</param>
    /// <param name="document">Документ.</param>
    /// <param name="pages">Страницы.</param>
    public virtual void BuildNewVersion(Guid builderGuid, IOfficialDocument document, List<Structures.Module.IRepackingPage> pages)
    {
      Stream newBody = null;
      try
      {
        newBody = Sungero.SmartProcessing.IsolatedFunctions.Repacking.BuildDocument(builderGuid, pages);
        if (newBody != null)
          document.CreateVersionFrom(newBody, Docflow.PublicConstants.OfficialDocument.PdfExtension);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Repacking. BuildNewVersion. Cannot create new document body (ID = {0})", ex, document.Id);
        this.AddNewErrorText(Resources.RepackingBodyCreationErrorFormat(document.Id));
      }
    }
    
    /// <summary>
    /// Записать текст ошибки в текущую сессию.
    /// </summary>
    /// <param name="text">Текст.</param>
    public virtual void AddNewErrorText(string text)
    {
      _obj.Errors.AddNew().Text = text;
    }
    
    /// <summary>
    /// Сохранить сессию перекомплектования.
    /// </summary>
    public virtual void SaveSession()
    {
      try
      {
        _obj.Save();
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Repacking. SaveSession. Cannot save repacking session (ID = {0})", ex, _obj.Id);
      }
    }
    
    /// <summary>
    /// Получить список документов для текущей сессии.
    /// </summary>
    /// <returns>Список из документов и их версий.</returns>
    public virtual List<Structures.RepackingSession.RepackingDocument> GetDocumentsWithVersions()
    {
      var repackingDocuments = new List<Structures.RepackingSession.RepackingDocument>();
      foreach (var sessionDocument in _obj.OriginalDocuments)
      {
        var document = OfficialDocuments.GetAll(x => x.Id == sessionDocument.DocumentId).FirstOrDefault();
        var version = document.Versions.Where(x => x.Number == sessionDocument.VersionNumber).FirstOrDefault();
        repackingDocuments.Add(Structures.RepackingSession.RepackingDocument.Create(document, version));
      }
      return repackingDocuments;
    }
    
    /// <summary>
    /// Получить активную сессию перекомплектования.
    /// </summary>
    /// <param name="assignmentId">Ид задания на верификацию.</param>
    /// <returns>Активная сессия перекомплектования.</returns>
    [Remote]
    public static IRepackingSession GetActiveSessionByAssignmentId(long assignmentId)
    {
      return RepackingSessions.GetAll(x => x.AssignmentId == assignmentId && x.Status == SmartProcessing.RepackingSession.Status.Active).FirstOrDefault();
    }
    
    /// <summary>
    /// Переименовать созданные простые документы после перекомплектования.
    /// </summary>
    /// <param name="task">Задача на верификацию.</param>
    public void RenameDocumentsAfterRepacking(IVerificationTask task)
    {
      var documents = Functions.VerificationTask.GetAttachedDocuments(task);
      if (documents.Count() > 1)
      {
        var documentName = SmartProcessing.Resources.AddendumName;
        var leadingDocument = Functions.VerificationTask.GetLeadingDocumentByRelations(task);
        if (leadingDocument != null && leadingDocument.DocumentKind.DocumentType.DocumentTypeGuid == typeof(Docflow.ISimpleDocument).GetFinalType().GetTypeGuid().ToString())
          documentName = SmartProcessing.Resources.DocumentDefaultName;
        var lastNumber = Functions.Module.GetLastAttachmentNumber(documents);
        
        var createdDocumentIds = _obj.NewDocuments.Select(x => x.DocumentId).ToList();
        SmartProcessing.Functions.Module.RenameSimpleDocuments(createdDocumentIds, documentName, lastNumber);
      }
    }
  }
}