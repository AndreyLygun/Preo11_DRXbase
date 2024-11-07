using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using Sungero.SmartProcessing.VerificationTask;
using Sungero.Workflow;

namespace Sungero.SmartProcessing.Server
{
  partial class VerificationTaskRouteHandlers
  {

    public virtual void Script6Execute()
    {
      Functions.VerificationTask.CreateNewDocumentRecognitionInfo(_obj);
    }

    public virtual void Script5Execute()
    {
      var documents = _obj.AllAttachments
        .Where(a => Sungero.Docflow.OfficialDocuments.Is(a))
        .Select(a => Sungero.Docflow.OfficialDocuments.As(a))
        .Where(d => d.VerificationState == Docflow.OfficialDocument.VerificationState.Completed)
        .ToList();
      
      foreach (var document in documents)
        Docflow.PublicFunctions.OfficialDocument.StoreVerifiedPropertiesValues(document);
    }
    
    public virtual void Script4Execute()
    {
      // Если при верификации изменен тип документа, заполнить статус обучения классификатора в результате распознавания.
      var documents = _obj.AllAttachments
        .Where(a => Sungero.Docflow.OfficialDocuments.Is(a))
        .Select(a => Docflow.OfficialDocuments.As(a));
      
      foreach (var document in documents)
        Functions.Module.UpdateEntityRecognitionInfo(document);
    }

    public virtual void EndBlock3(Sungero.SmartProcessing.Server.VerificationAssignmentEndBlockEventArguments e)
    {
      Functions.VerificationTask.SetCompleteVerificationState(_obj);
    }

    public virtual void StartBlock3(Sungero.SmartProcessing.Server.VerificationAssignmentArguments e)
    {
      e.Block.ThreadSubject = Sungero.SmartProcessing.Resources.VerificationAssignmentThreadSubject;
      
      // Заполнить тему задачи.
      e.Block.Subject = _obj.AllAttachments.Count() > 1
        ? Sungero.SmartProcessing.VerificationTasks.Resources.PackageAssignmentSubjectFormatFormat(_obj.LeadingDocumentName)
        : Sungero.SmartProcessing.VerificationTasks.Resources.DocumentAssignmentSubjectFormatFormat(_obj.LeadingDocumentName);
      
      if (e.Block.Subject.Length > Tasks.Info.Properties.Subject.Length)
        e.Block.Subject = e.Block.Subject.Substring(0, Tasks.Info.Properties.Subject.Length);
      
      this.GrantAccessRights(_obj.Assignee, e);
      
      // Отправить запрос на подготовку предпросмотра для документов.
      Docflow.PublicFunctions.Module.PrepareAllAttachmentsPreviews(_obj);
      Functions.VerificationTask.PrepareAllAttachmentsRepackingPreviews(_obj);
    }

    public virtual void StartAssignment3(Sungero.SmartProcessing.IVerificationAssignment assignment, Sungero.SmartProcessing.Server.VerificationAssignmentArguments e)
    {
      if (_obj.Addressee != null)
        this.GrantAccessRights(_obj.Addressee, e);
      
      assignment.Deadline = _obj.Deadline;
    }
    
    public virtual void CompleteAssignment3(Sungero.SmartProcessing.IVerificationAssignment assignment, Sungero.SmartProcessing.Server.VerificationAssignmentArguments e)
    {
      PublicFunctions.VerificationAssignment.ForwardAssigment(assignment, _obj);
    }
    
    /// <summary>
    /// Выдача прав исполнителю на задачу, ее вложения, и связанные с вложениями документы.
    /// </summary>
    /// <param name="performer">Исполнитель.</param>
    /// <param name="e">Аргументы.</param>
    public virtual void GrantAccessRights(IEmployee performer, Sungero.SmartProcessing.Server.VerificationAssignmentArguments e)
    {
      e.Block.Performers.Add(performer);
      
      Functions.VerificationTask.GrantAccessRights(_obj, performer);
    }
  }
}