using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.Shared.Job;

namespace Sungero.Exchange.Client
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Запустить фоновый процесс "Электронный обмен. Получение сообщений".
    /// </summary>
    [LocalizeFunction("GetMessagesFunctionName", "GetMessagesFunctionDescription")]
    public static void GetMessages()
    {
      Functions.Module.Remote.RequeueMessagesGet();
    }
    
    /// <summary>
    /// Получить сертификат сервиса обмена для текущего сотрудника, используя системный диалог выбора сертификата.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Сертификат.</returns>
    public static ICertificate GetCurrentUserExchangeCertificate(ExchangeCore.IBoxBase box, Company.IEmployee employee)
    {
      var certificates = Functions.Module.Remote.GetExchangeCertificatesForEmployee(box, employee);
      certificates = certificates.GroupBy(x => x.Thumbprint).Select(x => x.First()).ToList();
      
      if (certificates.Count > 1)
        return certificates.ShowSelectCertificate();
      
      return certificates.FirstOrDefault();
    }
    
    /// <summary>
    /// Получить сертификат сервиса обмена для сотрудника, используя системный диалог выбора сертификата.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Сертификат.</returns>
    [Public]
    public virtual ICertificate GetUserExchangeCertificate(ExchangeCore.IBoxBase box, Company.IEmployee employee)
    {
      return GetCurrentUserExchangeCertificate(box, employee);
    }
    
    /// <summary>
    /// Отправить уведомление об уточнении документа.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="receiver">Получатель.</param>
    /// <param name="note">Комментарий.</param>
    /// <param name="throwError">Не гасить ошибку.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="certificate">Сертификат для подписания УОУ.</param>
    /// <param name="isInvoiceAmendmentRequest">True для УОУ, False для отказа.</param>
    /// <returns>Строка с ошибкой отправки уведомления. Пусто - если отправка успешная.</returns>
    [Public]
    public static string SendAmendmentRequest(List<Docflow.IOfficialDocument> documents, Parties.ICounterparty receiver, string note, bool throwError,
                                              ExchangeCore.IBoxBase box, ICertificate certificate, bool isInvoiceAmendmentRequest)
    {
      return PublicFunctions.Module.GenerateAndSendAmendmentRequest(documents, receiver, note, throwError, box, certificate, isInvoiceAmendmentRequest);
    }
    
    /// <summary>
    /// Отправить извещения о получении документа.
    /// </summary>
    [Public, LocalizeFunction("SignAndSendDeliveryConfirmationFunctionName", "")]
    public virtual void SignAndSendDeliveryConfirmation()
    {
      var userBoxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote
        .GetConnectedBoxes()
        .Where(b => b.CertificateReceiptNotifications != null && Equals(b.CertificateReceiptNotifications.Owner, Users.Current))
        .ToList();
      
      if (!userBoxes.Any())
      {
        var error = Resources.SendDeliveryConfirmationBoxesNotFoundFormat(Users.Current.Name);
        throw AppliedCodeException.Create(error);
      }
      
      var aggregate = new List<Exception>();
      foreach (var box in userBoxes)
      {
        try
        {
          var error = this.SendDeliveryConfirmation(box, box.CertificateReceiptNotifications, true);
          if (!string.IsNullOrWhiteSpace(error))
            aggregate.Add(AppliedCodeException.Create(error));
        }
        catch (Exception ex)
        {
          Logger.Error(ReceiptNotificationSendingTasks.Resources.ReceiptNotificationAssignmentError, ex);
          aggregate.Add(ex);
        }
      }
      if (aggregate.Any())
      {
        var result = aggregate.Count == 1 ? aggregate.Single() : new AggregateException(aggregate);
        throw result;
      }
    }
    
    /// <summary>
    /// Отправить извещение о получении документа.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="certificate">Сертификат для подписания ИОП.</param>
    /// <param name="bulkMode">Режим для большой нагрузки.
    /// Если true - будут выполняться генерации ИОП в сервисе обмена и подписываться все доступные ИОП-ы.
    /// Если false - только одна пачка ИОП будет подписана, если совсем нечего подписывать - будет сгенерирована.</param>
    /// <returns>Строка с ошибкой отправки извещения. Пусто - если отправка успешная.</returns>
    [Public]
    public virtual string SendDeliveryConfirmation(ExchangeCore.IBoxBase box,
                                                   ICertificate certificate,
                                                   bool bulkMode)
    {
      var partSize = 25;
      var skip = 0;
      var rootBox = ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box);
      var documentInfos = Functions.Module.Remote.GetDocumentInfosWithoutReceiptNotificationPart(rootBox, skip, partSize, false);
      if (!documentInfos.Any())
        return string.Empty;
      
      if (certificate == null)
        certificate = Functions.Module.GetCurrentUserExchangeCertificate(box, Company.Employees.Current);

      return PublicFunctions.Module.GenerateAndSendDeliveryConfirmation(rootBox, certificate, documentInfos, bulkMode, partSize);
    }
    
    /// <summary>
    /// Отправка документа, либо ответа контрагенту.
    /// </summary>
    /// <param name="document">Документ, по которому требуется отправка ответа или отправка самого документа.</param>
    [Public]
    public virtual void SendResultToCounterparty(Docflow.IOfficialDocument document)
    {
      this.SendResultToCounterparty(document, null, new List<Docflow.IOfficialDocument>());
    }
    
    /// <summary>
    /// Отправка документа, либо ответа контрагенту с учетом выбранного сервиса обмена и приложений в задаче на согласование.
    /// </summary>
    /// <param name="document">Документ, по которому требуется отправка ответа или отправка самого документа.</param>
    /// <param name="service">Сервис обмена.</param>
    /// <param name="addenda">Приложения.</param>
    [Public]
    public virtual void SendResultToCounterparty(Docflow.IOfficialDocument document,
                                                 ExchangeCore.IExchangeService service,
                                                 List<Docflow.IOfficialDocument> addenda)
    {
      var isNonformalizedTaxInvoice = false;
      if (FinancialArchive.IncomingTaxInvoices.Is(document) || FinancialArchive.OutgoingTaxInvoices.Is(document))
        isNonformalizedTaxInvoice = Docflow.AccountingDocumentBases.As(document).IsFormalized != true;

      if (isNonformalizedTaxInvoice)
      {
        Dialogs.NotifyMessage(Resources.NonFormalizedTaxInvoiceSendingError);
        return;
      }
      
      if (CancellationAgreements.Is(document))
      {
        var error = Exchange.Functions.Module.Remote.ValidateBeforeSendingCancellationAgreement(document.LeadingDocument);
        if (!string.IsNullOrEmpty(error))
        {
          Dialogs.NotifyMessage(error);
          return;
        }
      }
      
      var lockInfo = document != null ? Locks.GetLockInfo(document) : null;
      if (lockInfo != null && lockInfo.IsLockedByOther)
      {
        Dialogs.NotifyMessage(lockInfo.LockedMessage);
        return;
      }
      
      var sendToCounterpartyDocumentInfo = Functions.Module.Remote.GetInfoForSendToCounterparty(document);
      if (sendToCounterpartyDocumentInfo.HasError)
      {
        Dialogs.NotifyMessage(sendToCounterpartyDocumentInfo.Error);
        return;
      }
      
      if (sendToCounterpartyDocumentInfo.AnswerIsSent)
      {
        Dialogs.NotifyMessage(Resources.AnswerIsAlreadySent);
        return;
      }
      
      if (document.LastVersion.Body.Size >= Constants.Module.ExchangeDocumentMaxSize)
      {
        Dialogs.NotifyMessage(Resources.DocumentOversized);
        return;
      }
      
      if (!document.AccessRights.CanUpdate())
      {
        Dialogs.NotifyMessage(Resources.NoRightsForDocument);
        return;
      }
      
      var isForcedLocked = false;
      if (!lockInfo.IsLocked)
        isForcedLocked = Locks.TryLock(document);
      if (!lockInfo.IsLocked && !isForcedLocked)
      {
        var lockInfoError = document != null ? Locks.GetLockInfo(document) : null;
        Dialogs.NotifyMessage(lockInfoError.LockedMessage);
        return;
      }
      
      if (Docflow.AccountingDocumentBases.Is(document))
      {
        var accountingDocument = Docflow.AccountingDocumentBases.As(document);
        if (Sungero.Exchange.PublicFunctions.Module.Remote.IsSbisUniversalTransferDocument970(accountingDocument))
        {
          Dialogs.NotifyMessage(Docflow.Resources.ActionNotAvailableForSbis);
          return;
        }
      }
      
      if (sendToCounterpartyDocumentInfo.IsSignedByCounterparty)
        this.SendAnswerToCounterparty(document, sendToCounterpartyDocumentInfo, addenda);
      else
        this.SendDocumentToCounterparty(document, sendToCounterpartyDocumentInfo, service, addenda);
      
      if (isForcedLocked)
        Locks.Unlock(document);
    }
    
    /// <summary>
    /// Отправка последней версии документа контрагенту.
    /// </summary>
    /// <param name="document">Документ для отправки.</param>
    /// <param name="documentInfo">Информация о документе, связанная с коммуникацией с контрагентом.</param>
    /// <param name="service">Сервис обмена.</param>
    /// <param name="addenda">Приложения.</param>
    public virtual void SendDocumentToCounterparty(Docflow.IOfficialDocument document,
                                                   Sungero.Exchange.Structures.Module.SendToCounterpartyInfo documentInfo,
                                                   ExchangeCore.IExchangeService service,
                                                   List<Docflow.IOfficialDocument> addenda)
    {
      if (!documentInfo.CanApprove && !documentInfo.HasApprovalSignature)
      {
        Dialogs.NotifyMessage(Resources.SendCounterpartyNotApproved);
        return;
      }
      
      var dialog = Dialogs.CreateInputDialog(Resources.SendCounterpartyDialogTitle);
      dialog.HelpCode = Constants.Module.HelpCodes.SendDocument;

      var counterparty = dialog.AddSelect(Resources.SendCounterpartyReceiver, true, documentInfo.DefaultCounterparty)
        .From(documentInfo.Counterparties)
        .Where(x => x.Status == CoreEntities.DatabookEntry.Status.Active);
      
      var defaultBox = documentInfo.Boxes.FirstOrDefault(x => Equals(x.ExchangeService, service)) ?? documentInfo.DefaultBox;
      var isBoxChanged = !Equals(defaultBox, documentInfo.DefaultBox);
      documentInfo.CounterpartyDepartments = isBoxChanged ?
        Functions.Module.Remote.GetCounterpartyDepartments(documentInfo.DefaultCounterparty, defaultBox) :
        documentInfo.CounterpartyDepartments;
      var counterpartyDepartmentBox = dialog.AddSelect(Resources.SendCounterpartyDepartment, false, ExchangeCore.CounterpartyDepartmentBoxes.Null)
        .From(documentInfo.CounterpartyDepartments)
        .Where(x => x.Status == CoreEntities.DatabookEntry.Status.Active);
      counterpartyDepartmentBox.IsEnabled = documentInfo.DefaultCounterparty != null && defaultBox != null;
      
      var box = dialog.AddSelect(Resources.SendCounterpartySender, true, defaultBox)
        .From(documentInfo.Boxes)
        .Where(x => x.Status == CoreEntities.DatabookEntry.Status.Active);
      box.IsEnabled = !(Docflow.AccountingDocumentBases.Is(document) && Docflow.AccountingDocumentBases.As(document).IsFormalized == true);
      
      var users = documentInfo.Certificates.Certificates.Select(c => c.Owner).ToList();
      var signatureOwner = dialog.AddSelect(Docflow.OfficialDocuments.Info.Properties.OurSignatory.LocalizedName, false, users.FirstOrDefault())
        .From(users);
      signatureOwner.IsEnabled = documentInfo.IsSignedByUs;
      
      var allowedAddenda = documentInfo.Addenda.Select(a => a.Addendum).ToList();
      var documentIsCancellationAgreement = CancellationAgreements.Is(document);
      var defaultBoxWorkWithSbis = documentInfo.DefaultBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis;
      var selectAllAddendaAsDefault = defaultBoxWorkWithSbis && documentIsCancellationAgreement && allowedAddenda.Any();
      var defaultSelectedAddenda = selectAllAddendaAsDefault ? allowedAddenda.ToArray() : addenda.Intersect(allowedAddenda).ToArray();
      var selectedAddenda = dialog.AddSelectMany(Resources.SendCounterpartyAddenda, false, defaultSelectedAddenda)
        .From(allowedAddenda);
      selectedAddenda.IsEnabled = documentInfo.HasAddendaToSend;
      
      var needSign = dialog.AddBoolean(Resources.SendCounterpartyNeedSign, false);
      if (Docflow.AccountingDocumentBases.Is(document) && Docflow.AccountingDocumentBases.As(document).IsFormalized == true)
      {
        var accDocument = Docflow.AccountingDocumentBases.As(document);
        var isSF = accDocument.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Schf;
        var isWaybill = FinancialArchive.Waybills.Is(document);
        var isContractStatement = FinancialArchive.ContractStatements.Is(document);
        var isUPD = FinancialArchive.UniversalTransferDocuments.Is(document);
        var isSbis = Exchange.PublicFunctions.Module.IsSbisExchangeService(accDocument.BusinessUnitBox);
        var isSbisSF = isSF && isSbis;
        needSign.Value = isWaybill || isUPD || isContractStatement || isSbisSF;
        needSign.IsEnabled = !(isWaybill || isSF || isUPD || isContractStatement || isSbisSF);
      }
      else
        needSign.Value = Docflow.ContractualDocumentBases.Is(document);
      
      var comment = dialog.AddMultilineString(Resources.SendCounterpartyComment, false);
      
      if (documentIsCancellationAgreement)
      {
        counterparty.Value = documentInfo.DefaultCounterparty;
        counterparty.IsEnabled = false;
        
        counterpartyDepartmentBox.Value = documentInfo.CounterpartyDepartmentBox;
        counterpartyDepartmentBox.IsEnabled = false;
        
        box.Value = documentInfo.DefaultBox;
        box.IsEnabled = false;
        
        selectedAddenda.IsEnabled = false;
        needSign.Value = true;
        needSign.IsEnabled = false;
        comment.IsEnabled = false;
      }
      
      var sendButton = dialog.Buttons.AddCustom(Resources.SendCounterpartySendButton);
      dialog.Buttons.Default = sendButton;
      dialog.Buttons.AddCancel();
      
      dialog.SetOnRefresh(args =>
                          {
                            if (args.IsValid && documentInfo.Certificates.CanSign && signatureOwner.Value == null)
                              args.AddInformation(Resources.SendCounterpartySignAndSendHint);
                            
                            if (selectedAddenda.Value != null)
                            {
                              var exchangeDocumentsSize = selectedAddenda.Value.Select(s => s.LastVersion.Body.Size).Sum() + document.LastVersion.Body.Size;
                              if (exchangeDocumentsSize >= Constants.Module.ExchangeDocumentMaxSize)
                                args.AddError(Resources.AddendaOversized);
                            }
                            if (counterparty.Value != null)
                            {
                              var boxes = counterparty.Value.ExchangeBoxes
                                .Where(b => b.Box.Status == ExchangeCore.BusinessUnitBox.Status.Active &&
                                       b.Box.ConnectionStatus == ExchangeCore.BusinessUnitBox.ConnectionStatus.Connected &&
                                       b.Status == Parties.CounterpartyExchangeBoxes.Status.Active && b.IsDefault == true);
                              if (!boxes.Any())
                                args.AddError(Resources.BoxesNotFound);
                            }
                          });
      
      counterparty.SetOnValueChanged(args =>
                                     {
                                       if (args.NewValue != args.OldValue)
                                       {
                                         var newCounterpartyList = documentInfo.Counterparties;
                                         if (args.NewValue != null)
                                           newCounterpartyList = documentInfo.Counterparties.Where(c => c.Equals(args.NewValue)).ToList();
                                         
                                         documentInfo.Boxes = Functions.Module.Remote.GetConnectedExchangeBoxesToCounterparty(document, newCounterpartyList);
                                         box = box.From(documentInfo.Boxes);
                                         
                                         // Если ящик не был выбран или был выбран ящик, через который нет обмена с выбранным контрагентом, заполнить поле первым подходящим ящиком.
                                         if (box.Value == null || !documentInfo.Boxes.Contains(box.Value))
                                           box.Value = documentInfo.Boxes.FirstOrDefault();
                                         
                                         counterpartyDepartmentBox.Value = ExchangeCore.CounterpartyDepartmentBoxes.Null;
                                         counterpartyDepartmentBox.IsEnabled = args.NewValue != null && box?.Value != null;
                                         if (counterpartyDepartmentBox.IsEnabled)
                                         {
                                           documentInfo.CounterpartyDepartments = Functions.Module.Remote.GetCounterpartyDepartments(args.NewValue, box.Value);
                                           counterpartyDepartmentBox.From(documentInfo.CounterpartyDepartments);
                                         }
                                       }
                                     });
      
      box.SetOnValueChanged(args =>
                            {
                              if (args.NewValue != args.OldValue)
                              {
                                documentInfo = Functions.Module.Remote.FillCertificates(document, args.NewValue, documentInfo);
                                
                                users = documentInfo.Certificates.Certificates.Select(c => c.Owner).ToList();
                                signatureOwner = signatureOwner.From(users);
                                signatureOwner.Value = users.FirstOrDefault();
                                signatureOwner.IsEnabled = documentInfo.IsSignedByUs;
                                
                                counterpartyDepartmentBox.Value = ExchangeCore.CounterpartyDepartmentBoxes.Null;
                                counterpartyDepartmentBox.IsEnabled = args.NewValue != null && counterparty?.Value != null;
                                if (counterpartyDepartmentBox.IsEnabled)
                                {
                                  documentInfo.CounterpartyDepartments = Functions.Module.Remote.GetCounterpartyDepartments(counterparty.Value, args.NewValue);
                                  counterpartyDepartmentBox.From(documentInfo.CounterpartyDepartments);
                                }
                              }
                            });
      
      dialog.SetOnButtonClick(args =>
                              {
                                if (args.Button == sendButton && args.IsValid)
                                {
                                  // Провалидировать параметры.
                                  var error = this.ValidateBeforeSendDocumentToCounterparty(document, documentInfo, selectedAddenda.Value?.ToList(),
                                                                                            counterparty.Value, signatureOwner.Value, box.Value);
                                  if (!string.IsNullOrWhiteSpace(error))
                                  {
                                    args.AddError(error);
                                    return;
                                  }
                                  
                                  var notSigned = signatureOwner.Value == null;
                                  ICertificate certificateToRejectFirstVersion = null;
                                  ICertificate certificate = null;
                                  if (notSigned)
                                  {
                                    var signDialog = Dialogs.CreateTaskDialog(Resources.SendCounterpartySignAndSendQuestion);
                                    var signButtons = signDialog.Buttons.AddCustom(Resources.SendCounterpartySignAndSendButton);
                                    signDialog.Buttons.AddCancel();
                                    if (signDialog.Show() == signButtons)
                                    {
                                      certificate = GetCurrentUserExchangeCertificate(box.Value, Company.Employees.Current);
                                      certificateToRejectFirstVersion = certificate;
                                      error = this.SignBeforeSendDocumentToCounterparty(document, documentInfo, selectedAddenda.Value?.ToList(),
                                                                                        certificate, false);
                                      if (!string.IsNullOrWhiteSpace(error))
                                      {
                                        args.AddError(error);
                                        return;
                                      }
                                    }
                                    else
                                    {
                                      return;
                                    }
                                  }
                                  else
                                  {
                                    certificate = documentInfo.Certificates.Certificates.FirstOrDefault(c => Equals(c.Owner, signatureOwner.Value));
                                  }
                                  error = this.SendDocumentToCounterparty(document, documentInfo, selectedAddenda.Value?.ToList(),
                                                                          counterparty.Value, counterpartyDepartmentBox.Value,
                                                                          box.Value, certificate, needSign.Value.Value, comment.Value);
                                  if (!string.IsNullOrWhiteSpace(error))
                                  {
                                    args.AddError(error);
                                    return;
                                  }
                                  this.TryRejectCounterpartyVersion(document, documentInfo, selectedAddenda.Value?.ToList(),
                                                                    counterparty.Value, counterpartyDepartmentBox.Value,
                                                                    box.Value, certificateToRejectFirstVersion);
                                  
                                  Dialogs.NotifyMessage(Resources.SendCounterpartySuccessfully);
                                }
                              });
      dialog.Show();
    }
    
    /// <summary>
    /// Провалидировать параметры перед отправкой документа контрагенту.
    /// </summary>
    /// <param name="document">Документ для отправки.</param>
    /// <param name="documentInfo">Информация о документе, связанная с коммуникацией с контрагентом.</param>
    /// <param name="selectedAddenda">Выбранные приложения.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="signatureOwner">Подписал.</param>
    /// <param name="box">Аб. ящик нашей организации.</param>
    /// <returns>Сообщение об ошибке или пустая строка, если ошибок нет.</returns>
    public virtual string ValidateBeforeSendDocumentToCounterparty(Docflow.IOfficialDocument document,
                                                                   Sungero.Exchange.Structures.Module.SendToCounterpartyInfo documentInfo,
                                                                   List<Docflow.IOfficialDocument> selectedAddenda,
                                                                   Parties.ICounterparty counterparty,
                                                                   IUser signatureOwner,
                                                                   ExchangeCore.IBusinessUnitBox box)
    {
      var hasExchangeWithCounterparty = counterparty.ExchangeBoxes
        .Any(c => Equals(c.Box, box) &&
             Equals(c.Status, Parties.CounterpartyExchangeBoxes.Status.Active) &&
             c.IsDefault == true);
      if (!hasExchangeWithCounterparty)
        return Exchange.Resources.NoExchangeThroughThisService;

      if (signatureOwner == null && documentInfo.IsSignedByUs && !documentInfo.Certificates.CanSign)
        return Resources.SendCounterpartyCertificateNotSelected;
      
      var error = this.GetDocumentLockedError(document);
      if (!string.IsNullOrWhiteSpace(error))
        return error;
      
      foreach (var addendumDocument in selectedAddenda)
      {
        error = this.GetDocumentLockedError(addendumDocument);
        if (!string.IsNullOrWhiteSpace(error))
          return error;
      }
      
      if (Functions.ExchangeDocumentInfo.Remote.LastVersionSended(document, box, counterparty))
        return Resources.DocumentIsAlreadySentToCounterparty;
      
      var notSigned = signatureOwner == null;
      if (notSigned)
      {
        if (!documentInfo.Certificates.CanSign)
        {
          if (selectedAddenda.Any())
            return Resources.SendCounterpartyWithAddendaWhenDocumentNotSigned;
          else
            return Resources.SendCounterpartyCanNotSign;
        }
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Подписать документ и его приложений перед отправкой контрагенту.
    /// </summary>
    /// <param name="document">Документ для отправки.</param>
    /// <param name="documentInfo">Информация о документе, связанная с коммуникацией с контрагентом.</param>
    /// <param name="selectedAddenda">Выбранные приложения.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="box">Аб. ящик нашей организации.</param>
    /// <param name="certificate">Сертификат, которым подписаны документы.</param>
    /// <returns>Сообщение об ошибке или пустая строка, если ошибок нет.</returns>
    [Obsolete("Метод не используется с 13.02.2023 и версии 4.9. Используйте метод SignBeforeSendDocumentToCounterparty с параметром, что отправляется ответ по документу.")]
    public virtual string SignBeforeSendDocumentToCounterparty(Docflow.IOfficialDocument document,
                                                               Sungero.Exchange.Structures.Module.SendToCounterpartyInfo documentInfo,
                                                               List<Docflow.IOfficialDocument> selectedAddenda,
                                                               Parties.ICounterparty counterparty,
                                                               ExchangeCore.IBusinessUnitBox box,
                                                               ICertificate certificate)
    {
      return this.SignBeforeSendDocumentToCounterparty(document, documentInfo, selectedAddenda, certificate, true);
    }
    
    /// <summary>
    /// Подписать документ и его приложений перед отправкой контрагенту.
    /// </summary>
    /// <param name="document">Документ для отправки.</param>
    /// <param name="documentInfo">Информация о документе, связанная с коммуникацией с контрагентом.</param>
    /// <param name="selectedAddenda">Выбранные приложения.</param>
    /// <param name="certificate">Сертификат, которым подписаны документы.</param>
    /// <param name="isSendAswer">Признак, что отправляется ответ по документу.</param>
    /// <returns>Сообщение об ошибке или пустая строка, если ошибок нет.</returns>
    public virtual string SignBeforeSendDocumentToCounterparty(Docflow.IOfficialDocument document,
                                                               Sungero.Exchange.Structures.Module.SendToCounterpartyInfo documentInfo,
                                                               List<Docflow.IOfficialDocument> selectedAddenda,
                                                               ICertificate certificate,
                                                               bool isSendAswer)
    {
      try
      {
        var error = this.GetDocumentLockedError(document);
        if (!string.IsNullOrWhiteSpace(error))
          return error;
        
        var lockedDocuments = new List<Docflow.IOfficialDocument>();
        foreach (var addendum in selectedAddenda)
        {
          if (addendum.LastVersion == null)
            continue;
          
          var bodyAddendum = addendum.LastVersion.PublicBody;
          if (bodyAddendum == null || bodyAddendum.Size == 0)
            continue;
          
          // Поставить блокировку на PublicBody для обхода проблемы с блокировкой тела при преобразовании в pdf (89996).
          var lockInfoAddendum = Locks.GetLockInfo(bodyAddendum);
          if (lockInfoAddendum != null)
          {
            if (lockInfoAddendum.IsLockedByOther)
              return lockInfoAddendum.LockedMessage;
            
            if (!lockInfoAddendum.IsLocked)
            {
              Locks.TryLock(bodyAddendum);
              lockedDocuments.Add(addendum);
            }
          }
        }
        
        if (certificate == null || !Docflow.PublicFunctions.Module
            .ApproveWithAddenda(document, selectedAddenda, certificate, null, true, true, string.Empty))
        {
          UnlockAddenda(lockedDocuments);
          return isSendAswer ? Resources.NotificationNoCertificates : Resources.SendCounterpartyCanNotSign;
        }
        UnlockAddenda(lockedDocuments);
      }
      catch (Exception ex)
      {
        return ex.Message;
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Отправить документ и приложения контрагенту.
    /// </summary>
    /// <param name="document">Документ для отправки.</param>
    /// <param name="documentInfo">Информация о документе, связанная с коммуникацией с контрагентом.</param>
    /// <param name="selectedAddenda">Выбранные приложения.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="counterpartyDepartmentBox">Абонентский ящик подразделения контрагента.</param>
    /// <param name="box">Аб. ящик нашей организации.</param>
    /// <param name="certificate">Сертификат, которым подписаны документы.</param>
    /// <param name="needSign">Требовать подписание от контрагента.</param>
    /// <param name="comment">Комментарий к сообщению в сервисе.</param>
    /// <returns>Сообщение об ошибке или пустая строка, если ошибок нет.</returns>
    public virtual string SendDocumentToCounterparty(Docflow.IOfficialDocument document,
                                                     Sungero.Exchange.Structures.Module.SendToCounterpartyInfo documentInfo,
                                                     List<Docflow.IOfficialDocument> selectedAddenda,
                                                     Parties.ICounterparty counterparty,
                                                     ExchangeCore.ICounterpartyDepartmentBox counterpartyDepartmentBox,
                                                     ExchangeCore.IBusinessUnitBox box,
                                                     ICertificate certificate,
                                                     bool needSign,
                                                     string comment)
    {
      try
      {
        Docflow.PublicFunctions.OfficialDocument.Remote.SendDocuments(document, selectedAddenda, counterparty,
                                                                      counterpartyDepartmentBox?.DepartmentId, box, null,
                                                                      certificate, needSign, comment);
        if (Equals(certificate.Owner, Company.Employees.Current))
          Functions.Module.SendDeliveryConfirmation(box, certificate, false);
        else
          Functions.Module.SendDeliveryConfirmation(box, null, false);
      }
      catch (AppliedCodeException ex)
      {
        return ex.Message;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Error sending document: ", ex);
        return Resources.ErrorWhileSendingDocToCounterpartyDetailed;
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Попытаться отказать контрагенту по первой версии, когда отправляем вторую.
    /// </summary>
    /// <param name="document">Документ для отправки.</param>
    /// <param name="documentInfo">Информация о документе, связанная с коммуникацией с контрагентом.</param>
    /// <param name="selectedAddenda">Выбранные приложения.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="counterpartyDepartmentBox">Абонентский ящик подразделения контрагента.</param>
    /// <param name="box">Аб. ящик нашей организации.</param>
    /// <param name="certificateToRejectFirstVersion">Сертификат.</param>
    public virtual void TryRejectCounterpartyVersion(Docflow.IOfficialDocument document,
                                                     Sungero.Exchange.Structures.Module.SendToCounterpartyInfo documentInfo,
                                                     List<Docflow.IOfficialDocument> selectedAddenda,
                                                     Parties.ICounterparty counterparty,
                                                     ExchangeCore.ICounterpartyDepartmentBox counterpartyDepartmentBox,
                                                     ExchangeCore.IBusinessUnitBox box,
                                                     ICertificate certificateToRejectFirstVersion)
    {
      if (documentInfo.NeedRejectFirstVersion)
      {
        if (certificateToRejectFirstVersion == null)
          certificateToRejectFirstVersion = GetCurrentUserExchangeCertificate(box, Company.Employees.Current);
        
        TryRejectCounterpartyVersion(document, counterparty, box, certificateToRejectFirstVersion);
      }

      var addendaToReject =
        documentInfo.Addenda
        .Where(a => a.NeedRejectFirstVersion)
        .Select(a => a.Addendum)
        .Where(a => selectedAddenda.Contains(a))
        .ToList();
      foreach (var addendum in addendaToReject)
      {
        if (certificateToRejectFirstVersion == null)
          certificateToRejectFirstVersion = GetCurrentUserExchangeCertificate(box, Company.Employees.Current);
        
        TryRejectCounterpartyVersion(addendum, counterparty, box, certificateToRejectFirstVersion);
      }
    }
    
    private static bool IsLocked(Docflow.IOfficialDocument document, CommonLibrary.BaseInputDialogEventArgs x)
    {
      var lockInfo = document != null ? Locks.GetLockInfo(document) : null;
      if (lockInfo != null && lockInfo.IsLockedByOther)
      {
        x.AddError(lockInfo.LockedMessage);
        return true;
      }
      return false;
    }
    
    /// <summary>
    /// Получить сообщение об ошибке при блокировке документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Сообщение об ошибке или пустая строка, если ошибок нет.</returns>
    public virtual string GetDocumentLockedError(Docflow.IOfficialDocument document)
    {
      var lockInfo = document != null ? Locks.GetLockInfo(document) : null;
      if (lockInfo != null && lockInfo.IsLockedByOther)
        return lockInfo.LockedMessage;
      
      return string.Empty;
    }
    
    /// <summary>
    /// Получить сообщение об ошибке при блокировке сведения о документе обмена.
    /// </summary>
    /// <param name="documentInfo">Сведения о документе обмена.</param>
    /// <returns>Сообщение об ошибке или пустая строка, если ошибок нет.</returns>
    public virtual string GetDocumentInfoLockedError(IExchangeDocumentInfo documentInfo)
    {
      var lockInfo = documentInfo != null ? Locks.GetLockInfo(documentInfo) : null;
      if (lockInfo != null && lockInfo.IsLocked)
        return Sungero.Exchange.Resources.ExhcnageDocumentInfoLockedMessageFormat(documentInfo.Id, lockInfo.OwnerName);
      
      return string.Empty;
    }

    private static void UnlockAddenda(List<Docflow.IOfficialDocument> selectedAddenda)
    {
      foreach (Docflow.IOfficialDocument docAddendum in selectedAddenda)
      {
        foreach (var version in docAddendum.Versions)
        {
          var lockInfoAddendum = Locks.GetLockInfo(version.PublicBody);
          if (lockInfoAddendum != null && lockInfoAddendum.IsLockedByMe)
          {
            Locks.Unlock(version.PublicBody);
          }
        }
      }
    }

    /// <summary>
    /// Попытаться отказать контрагенту по первой версии, когда отправляем вторую.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="party">Контрагент.</param>
    /// <param name="box">Ящик.</param>
    /// <param name="certificate">Сертификат.</param>
    private static void TryRejectCounterpartyVersion(Docflow.IOfficialDocument document, Parties.ICounterparty party,
                                                     ExchangeCore.IBusinessUnitBox box, ICertificate certificate)
    {
      try
      {
        if (document.Versions.Count < 2)
          return;
        
        if (certificate != null)
          SendAmendmentRequest(new List<Docflow.IOfficialDocument>() { document }, party, string.Empty, false, box, certificate, false);
      }
      catch (Exception)
      {
        // Мягкая попытка отправки, не удалось - и не надо.
      }
    }

    /// <summary>
    /// Отправка ответа контрагенту.
    /// </summary>
    /// <param name="document">Документ, по которому требуется отправка ответа.</param>
    /// <param name="documentInfo">Информация о документе, связанная с коммуникацией с контрагентом.</param>
    /// <param name="addenda">Приложения.</param>
    public virtual void SendAnswerToCounterparty(Docflow.IOfficialDocument document,
                                                 Sungero.Exchange.Structures.Module.SendToCounterpartyInfo documentInfo,
                                                 List<Docflow.IOfficialDocument> addenda)
    {
      var dialog = Dialogs.CreateInputDialog(Resources.SendAnswerToCounterpartyDialogTitle);
      dialog.HelpCode = Constants.Module.HelpCodes.SendAnswerOnDocument;
      
      var positiveResult = Resources.SendCounterpartyPositiveResult;
      var amendmentResult = Resources.SendCounterpartyNegativeResult;
      var invoiceAmendmentResult = Resources.SendCounterpartyRejectResult;
      
      var allowedResults = new List<string>();
      if (documentInfo.CanSendSignAsAnswer)
        allowedResults.Add(positiveResult);
      if (documentInfo.CanSendInvoiceAmendmentRequestAsAnswer)
        allowedResults.Add(invoiceAmendmentResult);
      if (documentInfo.CanSendAmendmentRequestAsAnswer)
        allowedResults.Add(amendmentResult);
      
      var formParams = ((Domain.Shared.IExtendedEntity)document).Params;
      var signAndSend = formParams.ContainsKey(Exchange.PublicConstants.Module.DefaultSignResult) &&
        (bool)formParams[Exchange.PublicConstants.Module.DefaultSignResult];
      var signResult = dialog.AddSelect(Resources.SendCounterpartyResult, true, allowedResults.FirstOrDefault())
        .From(allowedResults.ToArray());
      
      var counterparty = dialog.AddSelect(Resources.SendCounterpartyReceiver, true, documentInfo.DefaultCounterparty);
      counterparty.IsEnabled = false;
      
      var counterpartyDepartmentBox = dialog.AddSelect(Resources.SendCounterpartyDepartment, false, documentInfo.CounterpartyDepartmentBox);
      counterpartyDepartmentBox.IsEnabled = false;
      
      var box = dialog.AddSelect(Resources.SendCounterpartySender, true, documentInfo.DefaultBox);
      box.IsEnabled = false;
      
      var users = new List<Sungero.CoreEntities.IUser>();
      if (documentInfo.Certificates.Certificates != null)
        users = documentInfo.Certificates.Certificates.Select(c => c.Owner).ToList();
      
      var signatureOwner = dialog.AddSelect(Docflow.OfficialDocuments.Info.Properties.OurSignatory.LocalizedName, false, users.FirstOrDefault())
        .From(users);
      signatureOwner.IsEnabled = documentInfo.IsSignedByUs;

      var documentIsCancellationAgreement = CancellationAgreements.Is(document);
      var exchangeDocumentInfo = Functions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(document);
      var isSbis = Exchange.PublicFunctions.Module.IsSbisExchangeService(exchangeDocumentInfo.RootBox);
      
      var allowedAddenda = documentInfo.Addenda.Select(a => a.Addendum).ToList();
      var defaultSelectedAddenda = isSbis && documentIsCancellationAgreement && allowedAddenda.Any() ? allowedAddenda.ToArray() : addenda.Intersect(allowedAddenda).ToArray();
      var selectedAddenda = dialog.AddSelectMany(Resources.SendCounterpartyAddenda, false, defaultSelectedAddenda)
        .From(allowedAddenda);
      selectedAddenda.IsEnabled = documentInfo.HasAddendaToSend && !documentIsCancellationAgreement;
      
      var comment = dialog.AddMultilineString(Resources.SendCounterpartyComment, false);
      comment.IsEnabled = false;
      
      var sendButton = dialog.Buttons.AddCustom(Resources.SendCounterpartySendButton);
      dialog.Buttons.Default = sendButton;
      dialog.Buttons.AddCancel();
      
      var currentUserSelectedCertificate = Certificates.Null;
      
      if (!documentInfo.IsSignedByUs && !documentInfo.Certificates.MyCertificates.Any())
      {
        Dialogs.NotifyMessage(Resources.NotificationNoCertificates);
        return;
      }

      comment.IsRequired = (isSbis || documentIsCancellationAgreement) && comment.IsEnabled;
      
      var accountingDocument = Docflow.AccountingDocumentBases.As(document);
      
      var sbisUtd970AllowedAddenda = allowedAddenda
        .Select(a => Sungero.Docflow.AccountingDocumentBases.As(a))
        .Where(a => a != null && Sungero.Exchange.PublicFunctions.Module.Remote.IsSbisUniversalTransferDocument970(a))
        .ToList();
      
      signResult.SetOnValueChanged(x =>
                                   {
                                     if (x.NewValue != x.OldValue)
                                     {
                                       if (x.NewValue != positiveResult)
                                       {
                                         signatureOwner.Value = null;
                                         signatureOwner.IsEnabled = false;
                                         
                                         comment.IsEnabled = true;
                                         comment.IsRequired = isSbis || documentIsCancellationAgreement;
                                       }
                                       else
                                       {
                                         signatureOwner.Value = users.FirstOrDefault();
                                         signatureOwner.IsEnabled = documentInfo.IsSignedByUs;
                                         comment.Value = string.Empty;
                                         
                                         comment.IsEnabled = false;
                                         comment.IsRequired = false;
                                       }
                                     }
                                   });
      
      dialog.SetOnRefresh(x =>
                          {
                            if (documentInfo.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Accepted)
                              x.AddInformation(Sungero.Exchange.Resources.SendToCounterpartyDialog_BuyerAcceptanceStatusAccepted);
                            else if (documentInfo.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.PartiallyAccepted)
                              x.AddInformation(Sungero.Exchange.Resources.SendToCounterpartyDialog_BuyerAcceptanceStatusPartiallyAccepted);
                            else if (documentInfo.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Rejected)
                              x.AddInformation(Sungero.Exchange.Resources.SendToCounterpartyDialog_BuyerAcceptanceStatusRejected);
                            else if (accountingDocument != null && accountingDocument.IsFormalized == true &&
                                     accountingDocument.ExchangeState == Docflow.OfficialDocument.ExchangeState.SignRequired && accountingDocument.BuyerTitleId == null &&
                                     !(isSbis && accountingDocument.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Schf))
                              x.AddInformation(Sungero.Exchange.Resources.SendToCounterpartyDialog_BuyerTitleIsEmpty);
                            
                            if (selectedAddenda.Value != null && selectedAddenda.Value.Any() &&
                                documentInfo.Addenda.Where(a => a.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Rejected)
                                .Any(ad => selectedAddenda.Value.Contains(ad.Addendum)))
                              x.AddWarning(Sungero.Exchange.Resources.SendToCounterpartyDialog_AddendaWithBuyerAcceptanceStatusRejected);
                            
                            if (selectedAddenda.Value != null && selectedAddenda.Value.Any() &&
                                sbisUtd970AllowedAddenda.Any(ad => selectedAddenda.Value.Contains(ad)))
                              x.AddError(Sungero.Exchange.Resources.SendToCounterpartyDialog_SbisUtd970Addenda);
                          });
      
      dialog.SetOnButtonClick(args =>
                              {
                                if (args.Button == sendButton && args.IsValid)
                                {
                                  // Провалидировать параметры.
                                  var error = this.ValidateBeforeSendAnswerToCounterparty(document, documentInfo, selectedAddenda.Value?.ToList(),
                                                                                          counterparty.Value, signatureOwner.Value, box.Value, comment.Value, signResult.Value);
                                  if (!string.IsNullOrWhiteSpace(error))
                                  {
                                    args.AddError(error);
                                    return;
                                  }
                                  
                                  if (comment.Value != null && comment.Value.Length > 1000)
                                  {
                                    args.AddError(Exchange.ExchangeDocumentProcessingAssignments.Resources.TextOverlong, comment);
                                    return;
                                  }
                                  
                                  if (signResult.Value == Resources.SendCounterpartyPositiveResult)
                                  {
                                    #region Отправка подписи
                                    
                                    var isBuyerTitleEmpty = accountingDocument != null && accountingDocument.IsFormalized == true &&
                                      accountingDocument.BuyerTitleId == null;
                                    var isSignatureEmpty = signatureOwner.Value == null;
                                    if (isSignatureEmpty || isBuyerTitleEmpty)
                                    {
                                      var signDialog = Dialogs.CreateTaskDialog(Resources.SendCounterpartySignAndSendQuestion);
                                      var signButtons = signDialog.Buttons.AddCustom(Resources.SendCounterpartySignAndSendButton);
                                      signDialog.Buttons.AddCancel();
                                      if (signDialog.Show() == signButtons)
                                      {
                                        currentUserSelectedCertificate = GetCurrentUserExchangeCertificate(box.Value, Company.Employees.Current);
                                        error = this.SignBeforeSendDocumentToCounterparty(document, documentInfo, selectedAddenda.Value?.ToList(),
                                                                                          currentUserSelectedCertificate, true);
                                        if (!string.IsNullOrWhiteSpace(error))
                                        {
                                          args.AddError(error);
                                          return;
                                        }
                                        
                                        signatureOwner.Value = Users.Current;
                                        documentInfo.Certificates = Functions.Module.Remote.GetDocumentCertificatesToBox(document, box.Value);
                                      }
                                      else
                                      {
                                        return;
                                      }
                                    }
                                    
                                    var certificate = signatureOwner == null ? null :
                                      documentInfo.Certificates.Certificates.Single(c => Equals(c.Owner, signatureOwner.Value));
                                    this.SendReplySignToCounterparty(document, selectedAddenda.Value?.ToList(),
                                                                     counterparty.Value, box.Value, certificate, args);
                                    
                                    #endregion
                                  }
                                  else
                                  {
                                    // Отправка отказа в подписании.
                                    this.SendAmendmentRequestToCounterparty(document, selectedAddenda.Value?.ToList(),
                                                                            counterparty.Value, box.Value, comment.Value, signResult.Value, args);
                                  }
                                }
                              });
      dialog.Show();
    }
    
    /// <summary>
    /// Провалидировать параметры перед отправкой ответа контрагенту.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="documentInfo">Информация о документе, связанная с коммуникацией с контрагентом.</param>
    /// <param name="selectedAddenda">Выбранные приложения.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="signatureOwner">Подписал.</param>
    /// <param name="box">Аб. ящик нашей организации.</param>
    /// <param name="comment">Комментарий к сообщению в сервисе.</param>
    /// <param name="signResult">Результат, который будет отправлен контрагенту.</param>
    /// <returns>Сообщение об ошибке или пустая строка, если ошибок нет.</returns>
    public virtual string ValidateBeforeSendAnswerToCounterparty(Docflow.IOfficialDocument document,
                                                                 Sungero.Exchange.Structures.Module.SendToCounterpartyInfo documentInfo,
                                                                 List<Docflow.IOfficialDocument> selectedAddenda,
                                                                 Parties.ICounterparty counterparty,
                                                                 IUser signatureOwner,
                                                                 ExchangeCore.IBusinessUnitBox box,
                                                                 string comment,
                                                                 string signResult)
    {
      var hasExchangeWithCounterparty = counterparty.ExchangeBoxes
        .Any(c => Equals(c.Box, box) &&
             Equals(c.Status, Parties.CounterpartyExchangeBoxes.Status.Active) &&
             c.IsDefault == true);
      if (!hasExchangeWithCounterparty)
        return Exchange.Resources.NoExchangeThroughThisService;
      
      var documentLockError = this.GetDocumentLockedError(document);
      if (!string.IsNullOrWhiteSpace(documentLockError))
        return documentLockError;
      
      var documentIsCancellationAgreement = CancellationAgreements.Is(document);
      var exchangeDocumentInfo = Functions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(document);
      var exchangeDocumentInfoLockError = this.GetDocumentInfoLockedError(exchangeDocumentInfo);
      if (!string.IsNullOrWhiteSpace(exchangeDocumentInfoLockError))
        return exchangeDocumentInfoLockError;
      
      var isSbis = Exchange.PublicFunctions.Module.IsSbisExchangeService(exchangeDocumentInfo.RootBox);
      var allowedAddendaCount = documentInfo.Addenda.Select(a => a.Addendum).Count();
      if (isSbis && documentIsCancellationAgreement && allowedAddendaCount != selectedAddenda.Count())
        return Sungero.Exchange.Resources.PackageOfCancellationAgreementsNotFull;
      
      if (signResult == Resources.SendCounterpartyPositiveResult)
      {
        if (signatureOwner == null && documentInfo.IsSignedByUs && !documentInfo.Certificates.CanSign)
          return Resources.SendCounterpartyCertificateNotSelected;
        
        var accountingDocument = Docflow.AccountingDocumentBases.As(document);
        var isBuyerTitleEmpty = accountingDocument != null && accountingDocument.IsFormalized == true && accountingDocument.BuyerTitleId == null;
        var isSignatureEmpty = signatureOwner == null;
        if (isSignatureEmpty || isBuyerTitleEmpty)
        {
          if (!documentInfo.Certificates.CanSign)
            return Resources.NotificationNoCertificates;
        }
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Отправить ответ по документу и приложениям контрагенту.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="selectedAddenda">Выбранные приложения.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="box">Аб. ящик нашей организации.</param>
    /// <param name="certificate">Сертификат, которым подписаны документы.</param>
    /// <param name="args">Аргументы события диалога.</param>
    /// <returns>True - если отправка успешная. Иначе - false.</returns>
    public virtual bool SendReplySignToCounterparty(Docflow.IOfficialDocument document,
                                                    List<Docflow.IOfficialDocument> selectedAddenda,
                                                    Parties.ICounterparty counterparty,
                                                    ExchangeCore.IBusinessUnitBox box,
                                                    ICertificate certificate,
                                                    CommonLibrary.BaseInputDialogEventArgs args)
    {
      try
      {
        var documentsToSend = new List<Docflow.IOfficialDocument>() { document };
        documentsToSend.AddRange(selectedAddenda);
        Functions.Module.Remote.SendAnswers(documentsToSend, counterparty, box, certificate, false);
        if (Equals(certificate.Owner, Company.Employees.Current))
          Functions.Module.SendDeliveryConfirmation(box, certificate, false);
        else
          Functions.Module.SendDeliveryConfirmation(box, null, false);
      }
      catch (AppliedCodeException ex)
      {
        Logger.ErrorFormat("Error sending sign: ", ex);
        args.AddError(ex.Message);
        return false;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Error sending sign: ", ex);
        args.AddError(Resources.ErrorWhileSendingSignToCounterparty);
        return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Отправить уведомление об уточнении документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="selectedAddenda">Выбранные приложения.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="comment">Комментарий к сообщению в сервисе.</param>
    /// <param name="signResult">Результат, который будет отправлен контрагенту.</param>
    /// <param name="args">Аргументы события диалога.</param>
    /// <returns>True - если отправка успешная. Иначе - false.</returns>
    public virtual bool SendAmendmentRequestToCounterparty(Docflow.IOfficialDocument document,
                                                           List<Docflow.IOfficialDocument> selectedAddenda,
                                                           Parties.ICounterparty counterparty,
                                                           ExchangeCore.IBusinessUnitBox box,
                                                           string comment, string signResult,
                                                           CommonLibrary.BaseInputDialogEventArgs args)
    {
      var currentUserSelectedCertificate = GetCurrentUserExchangeCertificate(box, Company.Employees.Current);
      if (currentUserSelectedCertificate == null)
      {
        args.AddError(Resources.RejectCertificateNotFound);
        return false;
      }
      
      var invoiceAmendmentResult = Resources.SendCounterpartyRejectResult;
      var documentsToSend = new List<Docflow.IOfficialDocument>() { document };
      documentsToSend.AddRange(selectedAddenda);
      var error = SendAmendmentRequest(documentsToSend, counterparty, comment, false,
                                       box, currentUserSelectedCertificate,
                                       signResult == invoiceAmendmentResult);
      
      Functions.Module.SendDeliveryConfirmation(box, currentUserSelectedCertificate, false);
      if (!string.IsNullOrWhiteSpace(error))
      {
        args.AddError(error);
        return false;
      }
      else
      {
        Dialogs.NotifyMessage(Resources.SendAnswerCounterpartySuccessfully);
      }
      
      return true;
    }
    
    /// <summary>
    /// Перегенерировать Public Body.
    /// </summary>
    /// <param name="documentId">ИД документа.</param>
    public static void GeneratePublicBody(string documentId)
    {
      Functions.Module.Remote.GeneratePublicBody(long.Parse(documentId));
    }

    /// <summary>
    /// Создать QueueItem.
    /// </summary>
    /// <param name="businessUnitBoxId">ИД абонентского ящика.</param>
    /// <param name="messageId">ИД сообщения.</param>
    public static void CreateQueueItem(string businessUnitBoxId, string messageId)
    {
      Functions.Module.Remote.CreateQueueItem(long.Parse(businessUnitBoxId), messageId);
    }
  }
}