using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.SigningAssignment;

namespace Sungero.DocflowApproval.Client
{
  partial class SigningAssignmentFunctions
  {
    #region Проверки перед выполнением задания
    
    /// <summary>
    /// Валидация задания перед подписанием.
    /// </summary>
    /// <param name="eventArgs">Аргументы действия.</param>
    /// <returns>True - если ошибок нет, иначе - False.</returns>
    public virtual bool ValidateBeforeSign(Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var errors = Functions.SigningAssignment.Remote.ValidateBeforeSign(_obj);
      return this.AddErrorsToEventArgs(errors, eventArgs);
    }
    
    /// <summary>
    /// Валидация задания перед отказом в подписании.
    /// </summary>
    /// <param name="eventArgs">Аргументы действия.</param>
    /// <returns>True - если ошибок нет, иначе - False.</returns>
    public virtual bool ValidateBeforeReject(Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var errors = Functions.SigningAssignment.Remote.ValidateBeforeReject(_obj);
      return this.AddErrorsToEventArgs(errors, eventArgs);
    }
    
    /// <summary>
    /// Добавить ошибки в аргументы действия.
    /// </summary>
    /// <param name="errors">Список ошибок.</param>
    /// <param name="eventArgs">Аргументы действия.</param>
    /// <returns>True - если ошибок нет, иначе - False.</returns>
    public virtual bool AddErrorsToEventArgs(List<string> errors, Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      if (!errors.Any())
        return true;
      
      foreach (var error in errors)
        eventArgs.AddError(error);
      
      return false;
    }
    
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
      
      if (Functions.SigningAssignment.AreDocumentsLockedByMe(_obj))
      {
        eventArgs.AddError(Resources.SaveDocumentsBeforeComplete);
        isValid = false;
      }
      
      if (_obj.State.Properties.ReworkPerformer.IsVisible && _obj.ReworkPerformer == null)
      {
        eventArgs.AddError(DocflowApproval.Resources.CantSendForReworkWithoutPerformer);
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
      if (Functions.SigningAssignment.AreDocumentsLockedByMe(_obj))
      {
        eventArgs.AddError(Resources.SaveDocumentsBeforeComplete);
        return false;
      }
      
      return true;
    }
    
    #endregion
    
    /// <summary>
    /// Подписать документы из задания.
    /// </summary>
    /// <returns>Если подписание успешно - пустая строка, иначе - текст возникшей ошибки.</returns>
    public virtual string SignDocuments()
    {
      var mainDocument = _obj.DocumentGroup.ElectronicDocuments.First();
      var addenda = _obj.AddendaGroup.ElectronicDocuments.ToList();
      
      // Чтобы продолжить подписание, хотя бы у одного документа должны быть версии.
      if (!mainDocument.HasVersions && !addenda.Any(a => a.HasVersions))
        return string.Empty;
      
      var certificate = Certificates.Null;
      if (Functions.SigningAssignment.NeedStrongSignature(_obj))
      {
        var certificates = Functions.Module.GetCurrentUsersCertificatesForSigning(mainDocument);
        if (!certificates.Any())
          return Docflow.ApprovalTasks.Resources.CertificateNeededToSign;
        
        certificate = Functions.Module.SelectCertificateForSigning(mainDocument, certificates);
        if (certificate == null)
          return Docflow.ApprovalTasks.Resources.ToPerformNeedSignDocument;
      }
      
      var comment = this.GetCommentForSignature();
      
      var currentEmployee = Company.Employees.Current;
      var performer = Company.Employees.As(_obj.Performer);
      var signatory = Functions.Module.GetDocumentSignatory(mainDocument, currentEmployee, performer);
      
      return Functions.Module.TrySignDocumentWithAddenda(mainDocument, addenda, certificate, comment, signatory);
    }
    
    /// <summary>
    /// Подписать документы из задания с результатом "Не согласовано".
    /// </summary>
    /// <returns>Если подписание успешно - пустая строка, иначе - текст возникшей ошибки.</returns>
    public virtual string NotEndorseDocuments()
    {
      var mainDocument = _obj.DocumentGroup.ElectronicDocuments.First();
      var addenda = Functions.Module.GetNonObsoleteDocumentsFromAttachments(mainDocument, _obj.AddendaGroup.All);
      
      var comment = this.GetCommentForSignature();
      
      var currentEmployee = Company.Employees.Current;
      var performer = Company.Employees.As(_obj.Performer);
      var signatory = Functions.Module.GetDocumentSignatory(mainDocument, currentEmployee, performer);
      
      return Functions.Module.TryEndorseDocumentWithAddenda(mainDocument, addenda, false, null, signatory, comment);
    }
    
    /// <summary>
    /// Получить комментарий к подписи.
    /// </summary>
    /// <returns>Комментарий к подписи.</returns>
    public virtual string GetCommentForSignature()
    {
      return _obj.ActiveText;
    }
    
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
    /// Показать диалог переадресации задания.
    /// </summary>
    /// <returns>True - если пользователь нажал "Переадресовать", иначе - False.</returns>
    public virtual bool ShowForwardingDialog()
    {
      var dialog = Dialogs.CreateInputDialog(Docflow.Resources.ForwardAssignment);
      dialog.HelpCode = Docflow.PublicConstants.Module.HelpCodes.ForwardDialog;
      var forwardButton = dialog.Buttons.AddCustom(Docflow.Resources.Forward);
      dialog.Buttons.AddCancel();
      
      var mainDocument = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      var employees = Functions.SigningAssignment.GetForwardEmployees(_obj);
      var forwardTo = dialog.AddSelect(Docflow.Resources.ForwardTo, true, Employees.Null).From(employees);

      dialog.SetOnRefresh((args) =>
                          {
                            if (forwardTo.Value == null || mainDocument == null)
                              return;
                            
                            if (!OfficialDocuments.Is(mainDocument) && !mainDocument.AccessRights.CanApprove(forwardTo.Value))
                              args.AddError(SigningAssignments.Resources.EmployeeHasNoRightToApproveFormat(forwardTo.Value), forwardTo);
                          });
      
      if (dialog.Show() == forwardButton)
      {
        _obj.ForwardTo = forwardTo.Value;
        return true;
      }
      
      return false;
    }
    
  }
}