using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.EntityApprovalAssignment;

namespace Sungero.DocflowApproval.Client
{
  partial class EntityApprovalAssignmentFunctions
  {
    #region Проверки перед выполнением задания
    
    /// <summary>
    /// Валидация задания перед отправкой на доработку.
    /// </summary>
    /// <param name="eventArgs">Аргументы действия.</param>
    /// <returns>True - если ошибок нет, иначе - False.</returns>
    public virtual bool ValidateBeforeRework(Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var isValid = true;
      if (!Functions.Module.ValidateBeforeRework(_obj, eventArgs))
        isValid = false;
      
      if (Functions.EntityApprovalAssignment.AreDocumentsLockedByMe(_obj))
      {
        eventArgs.AddError(Resources.SaveDocumentsBeforeComplete);
        isValid = false;
      }
      
      if (_obj.State.Properties.ReworkPerformer.IsVisible == true && _obj.ReworkPerformer == null)
      {
        eventArgs.AddError(Resources.CantSendForReworkWithoutPerformer);
        isValid = false;
      }
      
      return isValid;
    }
    
    /// <summary>
    /// Валидация задания перед согласованием с замечаниями.
    /// </summary>
    /// <param name="eventArgs">Аргументы действия.</param>
    /// <returns>True - если ошибок нет, иначе - False.</returns>
    public virtual bool ValidateBeforeWithSuggestions(Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var isValid = true;
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        eventArgs.AddError(EntityApprovalAssignments.Resources.NeedCommentToApproveWithSuggestions);
        isValid = false;
      }
      
      if (!this.ValidateBeforeApproval(eventArgs))
        isValid = false;
      
      return isValid;
    }
    
    /// <summary>
    /// Валидация задания перед согласованием.
    /// </summary>
    /// <param name="eventArgs">Аргументы действия.</param>
    /// <returns>True - если ошибок нет, иначе - False.</returns>
    public virtual bool ValidateBeforeApproval(Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var isValid = true;
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.SingleOrDefault());
      if (officialDocument == null)
        return isValid;
      
      if (Functions.EntityApprovalAssignment.AreDocumentsLockedByMe(_obj))
      {
        eventArgs.AddError(Resources.SaveDocumentsBeforeComplete);
        isValid = false;
      }
      
      if (officialDocument.HasVersions &&
          Functions.EntityApprovalAssignment.NeedStrongSignature(_obj) &&
          !Docflow.PublicFunctions.Module.Remote.GetCertificates(officialDocument).Any())
      {
        eventArgs.AddError(Docflow.ApprovalTasks.Resources.CertificateNeeded);
        isValid = false;
      }
      
      return isValid;
    }
    
    /// <summary>
    /// Валидация задания перед переадресацией.
    /// </summary>
    /// <param name="eventArgs">Аргументы действия.</param>
    /// <returns>True - если ошибок нет, иначе - False.</returns>
    public virtual bool ValidateBeforeForward(Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var isValid = true;
      if (Functions.EntityApprovalAssignment.AreDocumentsLockedByMe(_obj))
      {
        eventArgs.AddError(Resources.SaveDocumentsBeforeComplete);
        isValid = false;
      }
      
      return isValid;
    }
    
    #endregion
    
    /// <summary>
    /// Показывать сводку по документу.
    /// </summary>
    /// <returns>True, если в задании нужно показывать сводку по документу.</returns>
    [Public]
    public virtual bool NeedViewDocumentSummary()
    {
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (officialDocument == null)
        return false;
      
      return Docflow.PublicFunctions.OfficialDocument.NeedViewDocumentSummary(officialDocument);
    }
    
    /// <summary>
    /// Подписать документы из задания.
    /// </summary>
    /// <param name="endorse">True - согласовать, False - не согласовать.</param>
    /// <param name="withSuggestions">True - согласование с замечаниями, False - обычное.</param>
    /// <returns>Если подписание успешно - пустая строка, иначе - текст возникшей ошибки.</returns>
    public virtual string EndorseDocuments(bool endorse, bool withSuggestions)
    {
      var mainDocument = _obj.DocumentGroup.ElectronicDocuments.First();
      var addenda = Functions.Module.GetNonObsoleteDocumentsFromAttachments(mainDocument, _obj.AddendaGroup.All);
      var performer = Company.Employees.As(_obj.Performer);
      var needStrongSignature = Functions.EntityApprovalAssignment.NeedStrongSignature(_obj);
      var comment = this.GetCommentForSignature(withSuggestions);
      return Functions.Module.EndorseDocuments(mainDocument, addenda, performer, endorse, needStrongSignature, comment);
    }
    
    /// <summary>
    /// Получить комментарий к подписи.
    /// </summary>
    /// <param name="withSuggestions">True - согласование с замечаниями, False - обычное.</param>
    /// <returns>Комментарий к подписи.</returns>
    public virtual string GetCommentForSignature(bool withSuggestions)
    {
      if (!withSuggestions)
        return _obj.ActiveText;
      
      return Docflow.PublicFunctions.Module.HasApproveWithSuggestionsMark(_obj.ActiveText)
        ? _obj.ActiveText
        : Docflow.PublicFunctions.Module.AddApproveWithSuggestionsMark(_obj.ActiveText);
    }
    
    /// <summary>
    /// Показать диалог переадресации.
    /// </summary>
    /// <returns>True, если запрос был подтвержден.</returns>
    public virtual bool ShowForwardingDialog()
    {
      var notAvailablePerformers = Functions.EntityApprovalAssignment.Remote.GetActiveAndFutureAssignmentsPerformers(_obj).ToList();

      var dialogResult = Docflow.PublicFunctions.Module.ShowForwardDialog(notAvailablePerformers, _obj.Deadline, TimeSpan.Zero);
      if (dialogResult.ForwardButtonIsPressed)
      {
        _obj.ForwardTo = dialogResult.ForwardTo;
        _obj.ForwardDeadline = dialogResult.Deadline;
        return true;
      }
      
      return false;
    }

  }
}