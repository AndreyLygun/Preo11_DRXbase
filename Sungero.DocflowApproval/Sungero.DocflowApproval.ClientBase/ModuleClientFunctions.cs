using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Workflow;

namespace Sungero.DocflowApproval.Client
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Подписать документы.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="addenda">Приложения к документу.</param>
    /// <param name="signatory">Подписант.</param>
    /// <param name="endorse">True - согласовать, False - не согласовать.</param>
    /// <param name="needStrongSignature">Требуется ли усиленная подпись.</param>
    /// <param name="signingComment">Комментарий при подписании.</param>
    /// <returns>Если подписание успешно - пустая строка, иначе - текст возникшей ошибки.</returns>
    public virtual string EndorseDocuments(IElectronicDocument document, List<IElectronicDocument> addenda, IEmployee signatory,
                                           bool endorse, bool needStrongSignature, string signingComment)
    {
      // Чтобы продолжить подписание, хотя бы у одного документа должны быть версии.
      if (!document.HasVersions && !addenda.Any(a => a.HasVersions))
        return string.Empty;
      
      var certificate = Certificates.Null;
      if (endorse && needStrongSignature)
      {
        var certificates = Functions.Module.GetCurrentUsersCertificatesForSigning(document);
        if (!certificates.Any())
          return Docflow.ApprovalTasks.Resources.CertificateNeeded;
        
        certificate = Functions.Module.SelectCertificateForSigning(document, certificates);
        if (certificate == null)
          return Docflow.ApprovalTasks.Resources.ToPerformNeedSignDocument;
      }
      
      return Functions.Module.TryEndorseDocumentWithAddenda(document, addenda, endorse, certificate, signatory, signingComment);
    }
    
    /// <summary>
    /// Согласовать документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="addenda">Приложения.</param>
    /// <param name="endorse">Признак согласования документа: True - согласовать документ, False - не согласовывать.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <param name="signatory">Подписант.</param>
    /// <param name="comment">Комментарий.</param>
    /// <returns>Если подписание успешно - пустая строка, иначе - текст возникшей ошибки.</returns>
    public virtual string TryEndorseDocumentWithAddenda(IElectronicDocument document,
                                                        List<IElectronicDocument> addenda,
                                                        bool endorse,
                                                        ICertificate certificate,
                                                        Company.IEmployee signatory,
                                                        string comment)
    {
      if (!document.HasVersions && !endorse)
        return string.Empty;
      
      try
      {
        var isSigned = endorse ?
          this.EndorseWithAddenda(document, addenda, certificate, signatory, comment) :
          Signatures.NotEndorse(document.LastVersion, null, comment, signatory);
        
        if (isSigned)
          return string.Empty;
        else
          return ApprovalTasks.Resources.ToPerformNeedSignDocument;
      }
      catch (CommonLibrary.Exceptions.PlatformException ex)
      {
        Logger.ErrorFormat("Failed to endorse document with addenda. Document id = '{0}' ", ex, document.Id);
        if (!ex.IsInternal)
        {
          var message = ex.Message.EndsWith(".") ? ex.Message : string.Format("{0}.", ex.Message);
          return message;
        }
        else
          throw;
      }
    }
    
    /// <summary>
    /// Согласовать документ с приложениями.
    /// </summary>
    /// <param name="mainDocument">Документ.</param>
    /// <param name="addenda">Приложения.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <param name="signatory">За кого выполняется согласование.</param>
    /// <param name="comment">Комментарий.</param>
    /// <returns>True, если сам документ был согласован или не имеет версий. Факт согласования приложений неважен.</returns>
    [Public]
    public virtual bool EndorseWithAddenda(IElectronicDocument mainDocument, List<IElectronicDocument> addenda,
                                           ICertificate certificate, IEmployee signatory, string comment)
    {
      if (!this.TryEndorseDocument(mainDocument, certificate, comment, signatory))
        return false;
      
      foreach (var addendum in addenda)
        this.TryEndorseDocument(addendum, certificate, comment, signatory);
      
      return true;
    }
    
    /// <summary>
    /// Получить подписанта на основе прав на утверждение замещающего и замещаемого.
    /// </summary>
    /// <param name="document">Подписываемый документ.</param>
    /// <param name="substitute">Замещающий.</param>
    /// <param name="signatory">Подписант.</param>
    /// <returns>Подписант, если права на подписание есть и у замещающего, и у подписанта, иначе замещающий.</returns>
    [Public]
    public virtual Company.IEmployee GetDocumentSignatory(IElectronicDocument document, IEmployee substitute, IEmployee signatory)
    {
      if (Equals(signatory, substitute))
        return signatory;
      
      var canSignatoryApprove = false;
      var canSubstituteApprove = false;
      
      if (OfficialDocuments.Is(document))
      {
        var officialDocument = OfficialDocuments.As(document);
        canSignatoryApprove = Docflow.PublicFunctions.OfficialDocument.Remote.CanSignByEmployee(officialDocument, signatory);
        canSubstituteApprove = Docflow.PublicFunctions.OfficialDocument.Remote.CanSignByEmployee(officialDocument, substitute);
      }
      else
      {
        canSignatoryApprove = document.AccessRights.CanApprove(signatory);
        canSubstituteApprove = document.AccessRights.CanApprove(substitute);
      }
      
      return canSignatoryApprove && canSubstituteApprove
        ? signatory
        : substitute;
    }
    
    /// <summary>
    /// Проверить наличие сертификатов перед согласованием документа.
    /// </summary>
    /// <param name="task">Задача на согласование.</param>
    /// <param name="document">Документ.</param>
    /// <param name="approver">Согласующий.</param>
    /// <returns>Если сертификаты найдены - пустая строка, иначе - текст ошибки об отсутствии сертификатов.</returns>
    public virtual string ValidateCertificatesBeforeApproval(ITask task, IElectronicDocument document, IEmployee approver)
    {
      if (Functions.Module.Remote.EmployeeIsApprover(task, approver) &&
          Functions.Module.Remote.NeedStrongSignature(task))
      {
        var certificates = Functions.Module.GetCurrentUsersCertificatesForSigning(document);
        if (!certificates.Any())
          return Docflow.ApprovalTasks.Resources.CertificateNeeded;
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Получить сертификаты текущего пользователя для подписания документа.
    /// </summary>
    /// <param name="document">Подписываемый документ.</param>
    /// <returns>Сертификаты для подписания документа.</returns>
    [Public]
    public List<ICertificate> GetCurrentUsersCertificatesForSigning(IElectronicDocument document)
    {
      var certificates = new List<ICertificate>();
      
      if (Docflow.OfficialDocuments.Is(document))
      {
        var officialDocument = Docflow.OfficialDocuments.As(document);
        certificates = Docflow.PublicFunctions.Module.Remote.GetCertificates(officialDocument);
      }
      else
        certificates = Docflow.PublicFunctions.Module.Remote.GetCurrentUserValidCertificates().ToList();
      
      return certificates;
    }
    
    /// <summary>
    /// Выбрать сертификат для подписания.
    /// </summary>
    /// <param name="document">Подписываемый документ.</param>
    /// <param name="certificates">Список доступных сертификатов.</param>
    /// <returns>Выбранный сертификат для подписания.</returns>
    [Public]
    public ICertificate SelectCertificateForSigning(IElectronicDocument document, List<ICertificate> certificates)
    {
      var certificate = Certificates.Null;
      
      if (Docflow.OfficialDocuments.Is(document))
      {
        var officialDocument = Docflow.OfficialDocuments.As(document);
        certificate = Docflow.PublicFunctions.OfficialDocument
          .ValidateAndRetrieveCertificateFromSigningReason(officialDocument, certificates);
      }
      
      if (certificate == null && certificates.Any())
      {
        certificate = certificates.Count() > 1
          ? certificates.ShowSelectCertificate()
          : certificates.First();
      }
      
      return certificate;
    }
    
    /// <summary>
    /// Попытаться подписать документ и приложения.
    /// </summary>
    /// <param name="mainDocument">Документ.</param>
    /// <param name="addenda">Приложения.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <param name="comment">Комментарий к подписи.</param>
    /// <param name="signatory">Сотрудник, от чьего имени происходит подписание.</param>
    /// <returns>Если подписание успешно - пустая строка, иначе - текст возникшей ошибки.</returns>
    [Public]
    public virtual string TrySignDocumentWithAddenda(IElectronicDocument mainDocument, List<IElectronicDocument> addenda,
                                                     ICertificate certificate, string comment, IEmployee signatory)
    {
      try
      {
        return this.SignDocumentWithAddenda(mainDocument, addenda, certificate, comment, signatory)
          ? string.Empty
          : Docflow.ApprovalTasks.Resources.ToPerformNeedSignDocument;
      }
      catch (CommonLibrary.Exceptions.PlatformException ex)
      {
        if (!ex.IsInternal)
        {
          Logger.DebugFormat("Failed to approve document with addenda. Document id = '{0}'. {1}", mainDocument.Id, ex);
          var message = ex.Message.Trim().EndsWith(".") ? ex.Message : string.Format("{0}.", ex.Message);
          return message;
        }
        else
        {
          Logger.ErrorFormat("Failed to approve document with addenda. Document id = '{0}' ", ex, mainDocument.Id);
          throw;
        }
      }
    }
    
    /// <summary>
    /// Подписать документ и приложения.
    /// </summary>
    /// <param name="mainDocument">Документ.</param>
    /// <param name="addenda">Приложения.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <param name="comment">Комментарий к подписи.</param>
    /// <param name="signatory">Сотрудник, от чьего имени происходит подписание.</param>
    /// <returns>True - если подписание основного документа успешно, иначе - false.</returns>
    [Public]
    public virtual bool SignDocumentWithAddenda(IElectronicDocument mainDocument, List<IElectronicDocument> addenda,
                                                ICertificate certificate, string comment, IEmployee signatory)
    {
      if (!this.TryApproveDocument(mainDocument, certificate, comment, signatory))
        return false;
      
      foreach (var addendum in addenda)
      {
        if (!this.TryApproveDocument(addendum, certificate, comment, signatory))
          this.TryEndorseDocument(addendum, certificate, comment, signatory);
      }
      
      return true;
    }
    
    /// <summary>
    /// Попытаться утвердить документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <param name="comment">Комментарий к подписи.</param>
    /// <param name="signatory">Сотрудник, от чьего имени происходит подписание.</param>
    /// <returns>True - если утверждение удалось, иначе - false.</returns>
    [Public]
    public virtual bool TryApproveDocument(IElectronicDocument document, ICertificate certificate,
                                           string comment, IEmployee signatory)
    {
      if (!document.HasVersions)
        return true;
      
      try
      {
        return this.ApproveDocument(document, certificate, comment, signatory);
      }
      catch (Sungero.Domain.Shared.Exceptions.ChildEntityNotFoundException ex)
      {
        throw AppliedCodeException.Create(OfficialDocuments.Resources.SigningVersionWasDeleted, ex);
      }
    }
    
    /// <summary>
    /// Утвердить документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <param name="comment">Комментарий к подписи.</param>
    /// <param name="signatory">Сотрудник, от чьего имени происходит подписание.</param>
    /// <returns>True - если утверждение удалось, иначе - false.</returns>
    [Public]
    public virtual bool ApproveDocument(IElectronicDocument document, ICertificate certificate,
                                        string comment, IEmployee signatory)
    {
      var canApprove = string.IsNullOrEmpty(Functions.Module.Remote.CheckCurrentEmployeeRightsToApprove(document));
      if (!canApprove)
        return false;

      var accountingDocument = AccountingDocumentBases.As(document);
      if (accountingDocument != null && accountingDocument.IsFormalized == true)
      {
        Docflow.PublicFunctions.AccountingDocumentBase.GenerateDefaultSellerTitle(accountingDocument);
        Docflow.PublicFunctions.AccountingDocumentBase.GenerateDefaultBuyerTitle(accountingDocument);
      }
      
      return Signatures.Approve(document.LastVersion, certificate, comment, signatory);
    }
    
    /// <summary>
    /// Попытаться согласовать документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <param name="comment">Комментарий к подписи.</param>
    /// <param name="signatory">Сотрудник, от чьего имени происходит подписание.</param>
    /// <returns>True - если согласование удалось, иначе - false.</returns>
    [Public]
    public virtual bool TryEndorseDocument(IElectronicDocument document, ICertificate certificate,
                                           string comment, IEmployee signatory)
    {
      if (!document.HasVersions)
        return true;
      
      try
      {
        return Signatures.Endorse(document.LastVersion, certificate, comment, signatory);
      }
      catch (Sungero.Domain.Shared.Exceptions.ChildEntityNotFoundException ex)
      {
        Logger.DebugFormat("Failed to endorse document. Document id = '{0}'. {1}", document.Id, ex);
        throw AppliedCodeException.Create(OfficialDocuments.Resources.SigningVersionWasDeleted, ex);
      }
      catch (Exception ex)
      {
        Logger.DebugFormat("Failed to endorse document. Document id = '{0}'. {1}", document.Id, ex);
        throw AppliedCodeException.Create(Docflow.ApprovalTasks.Resources.ToPerformNeedSignDocument);
      }
    }
    
    /// <summary>
    /// Показать диалог подтверждения выполнения без создания поручений.
    /// </summary>
    /// <param name="assignment">Задание, которое выполняется.</param>
    /// <param name="document">Документ.</param>
    /// <param name="e">Аргументы.</param>
    /// <returns>True, если диалог был, иначе false.</returns>
    public static bool ShowConfirmationDialogForCreatingActionItem(IAssignment assignment, IOfficialDocument document, Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var hasSubActionItems = Functions.Module.HasSubActionItems(assignment.Task, Workflow.Task.Status.InProcess);
      if (hasSubActionItems)
        return false;

      var dialogText = Resources.CompleteWithoutCreatingActionItem;
      var dialog = Dialogs.CreateTaskDialog(dialogText, MessageType.Question);
      dialog.Buttons.AddYes();
      dialog.Buttons.Default = DialogButtons.Yes;
      var createActionItemButton = dialog.Buttons.AddCustom(Resources.CreateActionItem);
      dialog.Buttons.AddNo();
      
      var result = dialog.Show();
      if (result == DialogButtons.Yes)
        return true;
      
      if (result == DialogButtons.No || result == DialogButtons.Cancel)
        e.Cancel();
      
      var actionItem = RecordManagement.ActionItemExecutionTasks.As(Functions.Module.CreateActionItemExecution(document, assignment.Id));
      
      if (actionItem != null)
      {
        actionItem.WaitForParentAssignment = true;
        actionItem.ShowModal();
      }

      hasSubActionItems = Functions.Module.HasSubActionItems(assignment.Task, Workflow.Task.Status.InProcess);
      if (hasSubActionItems)
        return true;
      
      var hasDraftSubActionItem = Functions.Module.HasSubActionItems(assignment.Task, Workflow.Task.Status.Draft);
      e.AddError(hasDraftSubActionItem ? Resources.ActionItemsShouldBeStarted : Resources.ActionItemsShouldBeCreated);
      e.Cancel();
      return true;
    }
    
    /// <summary>
    /// Создать поручение.
    /// </summary>
    /// <param name="document">Документ, по которому создается поручение.</param>
    /// <param name="parentAssignmentId">ИД задания, от которого создается поручение.</param>
    /// <returns>Поручение.</returns>
    public virtual ITask CreateActionItemExecution(IOfficialDocument document, long parentAssignmentId)
    {
      return RecordManagement.PublicFunctions.Module.Remote.CreateActionItemExecution(document, parentAssignmentId);
    }
    
    /// <summary>
    /// Проверить задание перед отправкой на доработку.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="eventArgs">Аргументы действия.</param>
    /// <returns>True - если ошибок нет, иначе - False.</returns>
    public virtual bool ValidateBeforeRework(Workflow.IAssignment assignment, Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      if (!eventArgs.Validate())
        return false;
      
      if (string.IsNullOrWhiteSpace(assignment.ActiveText))
      {
        eventArgs.AddError(DocflowApproval.Resources.NeedCommentToSendForRework);
        return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Показать хинт о доступности сервиса обмена на событии изменения значения контрола.
    /// </summary>
    /// <param name="state">Состояние свойства.</param>
    /// <param name="info">Информация о свойстве.</param>
    /// <param name="deliveryMethod">Сервис обмена.</param>
    /// <param name="e">Аргументы события Изменение значения контрола.</param>
    /// <param name="document">Документ, для которого проверяется доступность отправки через сервис обмена.</param>
    public void ShowExchangeHint(Domain.Shared.IPropertyState state, Domain.Shared.IPropertyInfo info,
                                 IMailDeliveryMethod deliveryMethod, Sungero.Presentation.ValueInputEventArgs<IMailDeliveryMethod> e,
                                 IOfficialDocument document)
    {
      if (this.NeedShowExchangeHint(state, deliveryMethod, e.Params, document))
        e.AddInformation(info, ApprovalTasks.Resources.ExchangeDeliveryExist);
    }
    
    /// <summary>
    /// Узнать, нужно ли показывать хинт о доступности сервиса обмена.
    /// </summary>
    /// <param name="state">Состояние свойства.</param>
    /// <param name="deliveryMethod">Сервис обмена.</param>
    /// <param name="param">Параметр, в котором хранится информация о необходимости показать хинт.</param>
    /// <param name="document">Документ, для которого проверяется доступность отправки через сервис обмена.</param>
    /// <returns>Признак необходимости показать хинт. True - если нужно показать хинт, иначе - false.</returns>
    public bool NeedShowExchangeHint(Domain.Shared.IPropertyState state, IMailDeliveryMethod deliveryMethod,
                                     Domain.Shared.ParamsDictionary param, IOfficialDocument document)
    {
      var isVisibleAndEnabled = state.IsVisible && state.IsEnabled;
      var exchangeDeliveryMethodNotSelected = deliveryMethod == null || deliveryMethod.Sid != Constants.Module.ExchangeDeliveryMethodSid;
      if (isVisibleAndEnabled && exchangeDeliveryMethodNotSelected)
      {
        var needShowHint = false;
        if (!param.TryGetValue(Constants.Module.NeedToShowExchangeServiceHint, out needShowHint))
        {
          needShowHint = Functions.Module.Remote.GetExchangeServices(document).DefaultService != null;
          param.AddOrUpdate(Constants.Module.NeedToShowExchangeServiceHint, needShowHint);
        }
        
        return needShowHint;
      }
      return false;
    }
    
  }
}