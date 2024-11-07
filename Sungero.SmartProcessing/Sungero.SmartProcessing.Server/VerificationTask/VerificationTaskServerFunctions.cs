using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using Sungero.SmartProcessing.VerificationTask;
using Sungero.Workflow;

namespace Sungero.SmartProcessing.Server
{
  partial class VerificationTaskFunctions
  {
    #region Предметное отображение "Задачи"
    
    /// <summary>
    /// Получить модель контрола состояния задачи на верификацию.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Модель контрола состояния задачи на верификацию.</returns>
    public Sungero.Core.StateView GetStateView(IOfficialDocument document)
    {
      if (_obj.AllAttachments.Any(d => Equals(document, d)))
        return this.GetStateView();
      else
        return StateView.Create();
    }
    
    /// <summary>
    /// Получить модель контрола состояния задачи на верификацию.
    /// </summary>
    /// <returns>Модель контрола состояния задачи на верификацию.</returns>
    [Remote(IsPure = true)]
    [LocalizeFunction("GetStateViewFunctionName", "GetStateViewFunctionDescription")]
    public Sungero.Core.StateView GetStateView()
    {
      var stateView = StateView.Create();
      
      // Добавить блок информации к блоку задачи.
      var taskHeader = VerificationTasks.Resources.StateViewTaskBlockHeader;
      this.AddInformationBlock(stateView, taskHeader, _obj.Started.Value);
      
      // Блок информации о задаче.
      var taskBlock = this.AddTaskBlock(stateView);
      
      // Получить все задания по задаче.
      var taskAssignments = VerificationAssignments.GetAll(a => Equals(a.Task, _obj)).OrderBy(a => a.Created).ToList();
      
      // Статус задачи.
      var status = _obj.Info.Properties.Status.GetLocalizedValue(_obj.Status);
      
      var lastAssignment = taskAssignments.OrderByDescending(a => a.Created).FirstOrDefault();
      
      if (!string.IsNullOrWhiteSpace(status))
        Docflow.PublicFunctions.Module.AddInfoToRightContent(taskBlock, status);
      
      // Блоки информации о заданиях.
      foreach (var assignment in taskAssignments)
      {
        var assignmentBlock = this.GetAssignmentBlock(assignment);
        
        taskBlock.AddChildBlock(assignmentBlock);
      }
      
      return stateView;
    }
    
    /// <summary>
    /// Добавить блок информации о действии.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <param name="text">Текст действия.</param>
    /// <param name="date">Дата действия.</param>
    [Public]
    public void AddInformationBlock(object stateView, string text, DateTime date)
    {
      // Создать блок с пояснением.
      var block = (stateView as StateView).AddBlock();
      block.Entity = _obj;
      block.DockType = DockType.Bottom;
      block.ShowBorder = false;
      
      // Иконка.
      block.AssignIcon(VerificationTasks.Resources.VerificationDocumentIcon, StateBlockIconSize.Small);
      
      // Текст блока.
      block.AddLabel(text);
      var style = Docflow.PublicFunctions.Module.CreateStyle(false, true);
      block.AddLabel(string.Format("{0}: {1}", Docflow.OfficialDocuments.Resources.StateViewDate.ToString(),
                                   Docflow.PublicFunctions.Module.ToShortDateShortTime(date.ToUserTime())), style);
    }
    
    /// <summary>
    /// Добавить блок задачи на верификацию.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <returns>Новый блок.</returns>
    public Sungero.Core.StateBlock AddTaskBlock(Sungero.Core.StateView stateView)
    {
      // Создать блок задачи.
      var taskBlock = stateView.AddBlock();
      
      // Добавить ссылку на задачу и иконку.
      taskBlock.Entity = _obj;
      taskBlock.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Large);
      
      // Определить схлопнутость.
      taskBlock.IsExpanded = _obj.Status == Workflow.Task.Status.InProcess;
      taskBlock.AddLabel(Resources.VerificationTaskThreadSubject, Sungero.Docflow.PublicFunctions.Module.CreateHeaderStyle());
      
      return taskBlock;
    }
    
    /// <summary>
    /// Добавить блок задания на верификацию.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Новый блок.</returns>
    public Sungero.Core.StateBlock GetAssignmentBlock(IAssignment assignment)
    {
      // Стили.
      var performerDeadlineStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle();
      var boldStyle = Docflow.PublicFunctions.Module.CreateStyle(true, false);
      var grayStyle = Docflow.PublicFunctions.Module.CreateStyle(false, true);
      var separatorStyle = Docflow.PublicFunctions.Module.CreateSeparatorStyle();
      var noteStyle = Docflow.PublicFunctions.Module.CreateNoteStyle();
      
      var block = StateView.Create().AddBlock();
      block.Entity = assignment;
      
      // Иконка.
      this.SetIcon(block, VerificationAssignments.As(assignment));
      
      // Заголовок.
      block.AddLabel(VerificationTasks.Resources.StateViewAssignmentBlockHeader, boldStyle);
      block.AddLineBreak();
      
      // Кому.
      var assigneeShortName = Company.PublicFunctions.Employee.GetShortName(Company.Employees.As(assignment.Performer), false);
      var performerInfo = string.Format("{0}: {1}", Docflow.OfficialDocuments.Resources.StateViewTo, assigneeShortName);
      block.AddLabel(performerInfo, performerDeadlineStyle);
      
      // Срок.
      if (assignment.Deadline.HasValue)
      {
        var deadlineLabel = Docflow.PublicFunctions.Module.ToShortDateShortTime(assignment.Deadline.Value.ToUserTime());
        block.AddLabel(string.Format("{0}: {1}", Docflow.OfficialDocuments.Resources.StateViewDeadline, deadlineLabel), performerDeadlineStyle);
      }
      
      // Текст задания.
      var comment = Docflow.PublicFunctions.Module.GetAssignmentUserComment(Assignments.As(assignment));
      if (!string.IsNullOrWhiteSpace(comment))
      {
        // Разделитель.
        block.AddLineBreak();
        block.AddLabel(Docflow.PublicFunctions.Module.GetSeparatorText(), separatorStyle);
        block.AddLineBreak();
        block.AddEmptyLine(Docflow.PublicFunctions.Module.GetEmptyLineMargin());
        
        block.AddLabel(comment, noteStyle);
      }
      
      // Статус.
      var status = AssignmentBases.Info.Properties.Status.GetLocalizedValue(assignment.Status);
      
      // Для непрочитанных заданий указать это.
      if (assignment.IsRead == false)
        status = Docflow.ApprovalTasks.Resources.StateViewUnRead.ToString();
      
      // Для исполненных заданий указать результат, с которым они исполнены, кроме "Проверено".
      if (assignment.Status == Workflow.AssignmentBase.Status.Completed
          && assignment.Result != SmartProcessing.VerificationAssignment.Result.Complete)
        status = SmartProcessing.VerificationAssignments.Info.Properties.Result.GetLocalizedValue(assignment.Result);
      
      if (!string.IsNullOrWhiteSpace(status))
        Docflow.PublicFunctions.Module.AddInfoToRightContent(block, status);
      
      // Задержка исполнения.
      if (assignment.Deadline.HasValue &&
          assignment.Status == Workflow.AssignmentBase.Status.InProcess)
        Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(block, assignment.Deadline.Value, assignment.Performer);
      
      return block;
    }
    
    /// <summary>
    /// Установить иконку.
    /// </summary>
    /// <param name="block">Блок, для которого требуется установить иконку.</param>
    /// <param name="assignment">Задание, от которого построен блок.</param>
    private void SetIcon(StateBlock block, IVerificationAssignment assignment)
    {
      var iconSize = StateBlockIconSize.Large;
      
      // Иконка по умолчанию.
      block.AssignIcon(StateBlockIconType.OfEntity, iconSize);

      // Прекращено, остановлено по ошибке.
      if (assignment.Status == Workflow.AssignmentBase.Status.Aborted ||
          assignment.Status == Workflow.AssignmentBase.Status.Suspended)
      {
        block.AssignIcon(StateBlockIconType.Abort, iconSize);
        return;
      }
      
      if (assignment.Result == null)
        return;
      
      // Проверено.
      if (assignment.Result == SmartProcessing.VerificationAssignment.Result.Complete)
      {
        block.AssignIcon(StateBlockIconType.Completed, iconSize);
        return;
      }
      
      // Переадресовано.
      if (assignment.Result == SmartProcessing.VerificationAssignment.Result.Forward)
      {
        block.AssignIcon(Sungero.Docflow.FreeApprovalTasks.Resources.Forward, iconSize);
      }
    }
    
    #endregion
    
    #region Запрос подготовки предпросмотра
    
    /// <summary>
    /// Отправить запрос на подготовку предпросмотра для документов из вложений задачи для перекомплектования.
    /// </summary>
    public virtual void PrepareAllAttachmentsRepackingPreviews()
    {
      var documents = _obj.AllAttachments
        .Where(x => Docflow.OfficialDocuments.Is(x))
        .Select(x => Docflow.OfficialDocuments.As(x))
        .ToList();
      foreach (var document in documents)
      {
        if (document != null && document.HasVersions)
          Sungero.Core.PreviewService.PreparePreview(document.LastVersion, Constants.Module.RepackingPreviewPluginName);
      }
    }
    
    #endregion
    
    /// <summary>
    /// Получить список ид документов, отсортированных по порядку вложений в задаче.
    /// </summary>
    /// <returns>Список ид документов.</returns>
    [Remote]
    public virtual List<long> GetOrderedAttachments()
    {
      var commandText = string.Format(Queries.VerificationTask.SelectTaskAttachments, _obj.Id);
      var documentsToSetStorageList = new List<long>();
      
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = commandText;
        var reader = command.ExecuteReader();
        while (reader.Read())
        {
          documentsToSetStorageList.Add(reader.GetInt64(0));
        }
        reader.Close();
      }
      return documentsToSetStorageList;
    }
    
    /// <summary>
    /// Связать документы комплекта.
    /// </summary>
    /// <param name="deletedDocuments">Список ИД удаленных документов.</param>
    public virtual void AddRelationsToLeadingDocument(List<long> deletedDocuments)
    {
      try
      {
        var documents = Functions.VerificationTask.GetAttachedDocumentsWithoutDeleted(_obj, deletedDocuments);

        if (!documents.Any() || documents.Count() == 1)
          return;
        
        // Получить документы которые не связаны.
        var documentIds = documents.Select(d => d.Id);
        var documentsRelations = Sungero.Content.DocumentRelations.GetAll()
          .Where(x => documentIds.Contains(x.Source.Id) &&
                 documentIds.Contains(x.Target.Id));
        var relatedDocumentIds = documentsRelations.Select(x => x.Source.Id).ToList()
          .Union(documentsRelations.Select(x => x.Target.Id).ToList());
        var withoutRelationDocuments = documents.Where(x => !relatedDocumentIds.Contains(x.Id));
        if (!withoutRelationDocuments.Any())
          return;
        
        var newDocuments = new List<Structures.Module.INewDocument>();
        var leadingDocument = Functions.VerificationTask.GetLeadingDocumentByRelations(_obj);
        if (leadingDocument == null ||
            leadingDocument != null && deletedDocuments.Contains(leadingDocument.Id))
          leadingDocument = Functions.Module.GetNewLeadingDocumentByType(newDocuments, documents);
        if (leadingDocument == null)
          return;
        
        foreach (var document in withoutRelationDocuments.Where(d => d.Id != leadingDocument.Id))
        {
          try
          {
            document.Relations.AddFrom(Docflow.PublicConstants.Module.AddendumRelationName, leadingDocument);
            document.Relations.Save();
          }
          catch (Exception ex)
          {
            Logger.ErrorFormat("Repacking. AddRelationsToLeadingDocument. Cannot add relations to document (ID = {0})", ex, document.Id);
          }
        }
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Repacking. AddRelationsToLeadingDocument. Cannot add relations to documents from task (ID = {0})", ex, _obj.Id);
      }
    }
    
    /// <summary>
    /// Добавить вложения в задачу на верификацию.
    /// </summary>
    /// <param name="documents">Новые документы.</param>
    public virtual void AddAttachments(List<IOfficialDocument> documents)
    {
      foreach (var document in documents)
        _obj.Attachments.Add(document);
      _obj.Save();
    }
    
    /// <summary>
    /// Получить тему по умолчанию для задания "Верификация".
    /// </summary>
    /// <param name="task">Задача на верификацию.</param>
    /// <returns>Тема для задания "Верификация".</returns>
    [ExpressionElement("VerificationAssignmentDefaultSubject", "")]
    public static string GetVerificationAssignmentDefaultSubject(IVerificationTask task)
    {
      var subject = task.AllAttachments.Count() > 1
        ? Sungero.SmartProcessing.VerificationTasks.Resources.PackageAssignmentSubjectFormatFormat(task.LeadingDocumentName).ToString()
        : Sungero.SmartProcessing.VerificationTasks.Resources.DocumentAssignmentSubjectFormatFormat(task.LeadingDocumentName).ToString();
      
      if (subject.Length > Tasks.Info.Properties.Subject.Length)
        subject = subject.Substring(0, Tasks.Info.Properties.Subject.Length);
      return subject;
    }
    
    /// <summary>
    /// Выдача прав исполнителю на задачу, ее вложения, и связанные с вложениями документы.
    /// </summary>
    /// <param name="performer">Исполнитель.</param>
    public virtual void GrantAccessRights(Company.IEmployee performer)
    {
      var loggerPrefix = string.Format("Task ID: {0}. Performer ID: {1}. ", _obj.Id, performer.Id);
      Logger.WithLogger(Constants.VerificationTask.VerificationTaskLoggerPostfix)
        .Debug(loggerPrefix + "Start granting access rights to attachments.");
      
      this.GrantAccessRightsToAttachments(performer);
      this.GrantAccessRightsToRelatedDocuments(performer);
      this.GrantAccessRightsToTask(performer);
      
      Logger.WithLogger(Constants.VerificationTask.VerificationTaskLoggerPostfix)
        .Debug(loggerPrefix + "Done granting access rights to attachments.");
    }
    
    /// <summary>
    /// Выдать права на вложения исполнителю.
    /// </summary>
    /// <param name="performer">Исполнитель.</param>
    public virtual void GrantAccessRightsToAttachments(Company.IEmployee performer)
    {
      foreach (var attachment in _obj.AllAttachments)
      {
        if (!attachment.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, performer))
        {
          var accesRights = attachment.AccessRights.Current.Where(ar => Equals(ar.Recipient, performer));
          if (accesRights.Any())
            attachment.AccessRights.RevokeAll(performer);
          
          attachment.AccessRights.Grant(performer, DefaultAccessRightsTypes.FullAccess);
          Logger.WithLogger(Constants.VerificationTask.VerificationTaskLoggerPostfix)
            .Debug(string.Format("Task ID: {0}. Performer ID: {1}. Document:{2}. Access rights granted: Full access.",
                                 _obj.Id, performer.Id, attachment.Id));
        }
      }
    }
    
    /// <summary>
    /// Выдать права на связанные документы исполнителю.
    /// </summary>
    /// <param name="performer">Исполнитель.</param>
    public virtual void GrantAccessRightsToRelatedDocuments(Company.IEmployee performer)
    {
      var attachedDocuments = _obj.AllAttachments
        .Where(a => Sungero.Docflow.OfficialDocuments.Is(a))
        .Select(d => Sungero.Docflow.OfficialDocuments.As(d));
      foreach (var attachedDocument in attachedDocuments)
      {
        var relatedDocuments = attachedDocument.Relations.GetRelatedFromDocuments().ToList();
        foreach (var relatedDocument in relatedDocuments)
        {
          var performerHasAccessRights = relatedDocument.AccessRights.Current
            .Where(ar => Equals(ar.Recipient, performer) && ar.AccessRightsType != DefaultAccessRightsTypes.Forbidden).Any();
          
          if (!performerHasAccessRights)
          {
            relatedDocument.AccessRights.Grant(performer, DefaultAccessRightsTypes.Read);
            Logger.WithLogger(Constants.VerificationTask.VerificationTaskLoggerPostfix)
              .Debug(string.Format("Task ID: {0}. Performer ID: {1}. Document {2}. Access rights granted: Read.",
                                   _obj.Id, performer.Id, relatedDocument.Id));
          }
        }
      }
    }
    
    /// <summary>
    /// Выдать права на задачу исполнителю.
    /// </summary>
    /// <param name="performer">Исполнитель.</param>
    public virtual void GrantAccessRightsToTask(Company.IEmployee performer)
    {
      _obj.AccessRights.Grant(performer, DefaultAccessRightsTypes.Change);
      Logger.WithLogger(Constants.VerificationTask.VerificationTaskLoggerPostfix)
        .Debug(string.Format("Task ID: {0}. Performer ID: {1}. Task: Access rights changed: Change.",
                             _obj.Id, performer.Id));
    }
    
    /// <summary>
    /// Перевести все документы комплекта в статус верификации "Завершена".
    /// </summary>
    public virtual void SetCompleteVerificationState()
    {
      var documents = _obj.AllAttachments
        .Where(a => Docflow.OfficialDocuments.Is(a))
        .Select(a => Docflow.OfficialDocuments.As(a))
        .Where(d => d.VerificationState == Docflow.OfficialDocument.VerificationState.InProcess)
        .Distinct()
        .ToList();
      
      foreach (var document in documents)
      {
        var hasEmptyRequiredProperties = Docflow.PublicFunctions.OfficialDocument.HasEmptyRequiredProperties(document);
        if (hasEmptyRequiredProperties)
        {
          Logger.WithLogger(Constants.VerificationTask.VerificationTaskLoggerPostfix)
            .Debug(Resources.DocumentSkippedByReasonFormat(document.Id, Resources.RequiredPropertyIsEmpty));
          continue;
        }
        
        document.VerificationState = Docflow.OfficialDocument.VerificationState.Completed;
        
        Logger.WithLogger(Constants.VerificationTask.VerificationTaskLoggerPostfix)
          .Debug(string.Format("Task {0}. Document {1} verification state changed: Completed.", _obj.Id, document.Id));
      }
    }
    
    /// <summary>
    /// Создать результат распознавания для нового документа после перекомплектования.
    /// </summary>
    public virtual void CreateNewDocumentRecognitionInfo()
    {
      var assignmentIds = VerificationAssignments.GetAll()
        .Where(a => Equals(a.MainTask, _obj) && a.Status == Workflow.Assignment.Status.Completed)
        .Select(x => x.Id)
        .ToList();
      
      var repackingSessions = SmartProcessing.RepackingSessions.GetAll()
        .Where(x => x.AssignmentId.HasValue && assignmentIds.Contains(x.AssignmentId.Value))
        .ToList();
      
      // Выбор последних версий новых документов по всем сессиям перекомплектования.
      var newDocumentIds = repackingSessions.SelectMany(x => x.NewDocuments.Select(d => d.DocumentId.Value)).ToList();
      var documents = Docflow.OfficialDocuments
        .GetAll(x => newDocumentIds.Contains(x.Id) && x.LifeCycleState != Docflow.OfficialDocument.LifeCycleState.Obsolete);
      foreach (var document in documents)
      {
        var version = repackingSessions
          .SelectMany(x => x.OriginalDocuments.Where(k => k.DocumentId == document.Id)).Max(x => x.ResultVersionNumber) ?? 1;
        if (document.Versions.Any(x => x.Number == version))
        {
          var recognitionInfo = Commons.EntityRecognitionInfos.Create();
          recognitionInfo.RecognizedClass = string.Empty;
          recognitionInfo.Name = string.Empty;
          recognitionInfo.EntityId = document.Id;
          recognitionInfo.EntityType = document.GetEntityMetadata().GetOriginal().NameGuid.ToString();
          recognitionInfo.VerifiedVersionNumber = version;
          recognitionInfo.FirstPageClassifierTrainingStatus =
            Commons.EntityRecognitionInfo.FirstPageClassifierTrainingStatus.Awaiting;
          recognitionInfo.Save();
          
          Logger.WithLogger(Constants.VerificationTask.VerificationTaskLoggerPostfix)
            .Debug(string.Format("Created recognition info {0} with entity {1}.",
                                 recognitionInfo.Id, recognitionInfo.EntityId));
        }
      }
    }
  }
}