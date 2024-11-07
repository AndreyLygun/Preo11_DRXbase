using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ExchangeDocumentProcessingAssignment;

namespace Sungero.Exchange.Client
{
  partial class ExchangeDocumentProcessingAssignmentActions
  {
    public virtual void SendForDocumentFlow(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Определить главный документ.
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      var needSigningAttachments = _obj.NeedSigning.All.Select(a => Content.ElectronicDocuments.As(a)).ToList();
      if (!needSigningAttachments.Any(a => Exchange.CancellationAgreements.Is(a)))
        attachments = attachments.Where(a => !Exchange.CancellationAgreements.Is(a)).ToList();
      
      var mainDocument = Functions.ExchangeDocumentProcessingAssignment
        .ShowMainDocumentChoosingDialog(attachments, needSigningAttachments, Docflow.OfficialDocuments.Info.Actions.SendForDocumentFlow);
      if (mainDocument == null)
        return;
      var mainOfficialDocument = Docflow.OfficialDocuments.As(mainDocument);
      
      // Создать задачу.
      var documentFlowTask = Sungero.DocflowApproval.PublicFunctions.Module.Remote.CreateDocumentFlowTask(mainOfficialDocument);
      
      // Добавить вложения, которые не были добавлены при создании задачи.
      foreach (var attachment in attachments.Where(att => !documentFlowTask.Attachments.Any(x => Equals(x, att))))
      {
        if (Docflow.PublicFunctions.OfficialDocument.NeedToAttachDocument(attachment, mainOfficialDocument))
          documentFlowTask.OtherGroup.All.Add(attachment);
      }
      
      documentFlowTask.Show();
    }

    public virtual bool CanSendForDocumentFlow(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      if (!Docflow.PublicFunctions.Module.IsWorkStarted(_obj))
        return false;
      
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      if (!_obj.NeedSigning.All.Any(a => Exchange.CancellationAgreements.Is(a)))
        attachments = attachments.Where(a => !Exchange.CancellationAgreements.Is(a)).ToList();
      var documentsList = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments, Docflow.OfficialDocuments.Info.Actions.SendForDocumentFlow);
      return documentsList.Any();
    }

    public virtual void SendForExecution(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Определить главный документ.
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      var needSigningAttachments = _obj.NeedSigning.All.Select(a => Content.ElectronicDocuments.As(a)).ToList();
      if (!_obj.NeedSigning.All.Any(a => Exchange.CancellationAgreements.Is(a)))
        attachments = attachments.Where(a => !Exchange.CancellationAgreements.Is(a)).ToList();
      var mainDocument = Functions.ExchangeDocumentProcessingAssignment.ShowMainDocumentChoosingDialog(attachments, needSigningAttachments, Docflow.OfficialDocuments.Info.Actions.SendActionItem);
      if (mainDocument == null)
        return;
      var mainOfficialDocument = Docflow.OfficialDocuments.As(mainDocument);
      
      // Создать задачу.
      var actionItemTask = Sungero.RecordManagement.PublicFunctions.Module.Remote.CreateActionItemExecution(mainOfficialDocument);

      // Добавить вложения, которые не были добавлены при создании задачи.
      foreach (var attachment in attachments.Where(att => !actionItemTask.Attachments.Any(x => Equals(x, att))))
      {
        if (Docflow.PublicFunctions.OfficialDocument.NeedToAttachDocument(attachment, mainOfficialDocument))
          actionItemTask.OtherGroup.All.Add(attachment);
      }

      actionItemTask.Show();
    }

    public virtual bool CanSendForExecution(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      if (!Docflow.PublicFunctions.Module.IsWorkStarted(_obj))
        return false;
      
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      if (!_obj.NeedSigning.All.Any(a => Exchange.CancellationAgreements.Is(a)))
        attachments = attachments.Where(a => !Exchange.CancellationAgreements.Is(a)).ToList();
      var documentsList = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments, Docflow.OfficialDocuments.Info.Actions.SendActionItem);
      return documentsList.Any();
    }

    public virtual void SendForReview(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Определить главный документ.
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      var needSigningAttachments = _obj.NeedSigning.All.Select(a => Content.ElectronicDocuments.As(a)).ToList();
      if (!_obj.NeedSigning.All.Any(a => Exchange.CancellationAgreements.Is(a)))
        attachments = attachments.Where(a => !Exchange.CancellationAgreements.Is(a)).ToList();
      var mainDocument = Functions.ExchangeDocumentProcessingAssignment.ShowMainDocumentChoosingDialog(attachments, needSigningAttachments, Docflow.OfficialDocuments.Info.Actions.SendForReview);
      if (mainDocument == null)
        return;
      var mainOfficialDocument = Docflow.OfficialDocuments.As(mainDocument);
      
      // Создать задачу.
      var task = RecordManagement.PublicFunctions.Module.Remote.CreateDocumentReview(mainOfficialDocument);
      var reviewTask = RecordManagement.DocumentReviewTasks.As(task);
      
      // Добавить вложения, которые не были добавлены при создании задачи.
      foreach (var attachment in attachments.Where(att => !reviewTask.Attachments.Any(x => Equals(x, att))))
      {
        if (Docflow.PublicFunctions.OfficialDocument.NeedToAttachDocument(attachment, mainOfficialDocument))
          reviewTask.OtherGroup.All.Add(attachment);
      }
      
      reviewTask.Show();
    }

    public virtual bool CanSendForReview(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      if (!Docflow.PublicFunctions.Module.IsWorkStarted(_obj))
        return false;
      
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      if (!_obj.NeedSigning.All.Any(a => Exchange.CancellationAgreements.Is(a)))
        attachments = attachments.Where(a => !Exchange.CancellationAgreements.Is(a)).ToList();
      var documentsList = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments, Docflow.OfficialDocuments.Info.Actions.SendForReview);
      return documentsList.Any();
    }

    public virtual void SendForFreeApproval(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Определить главный документ.
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      if (!_obj.NeedSigning.All.Any(a => Exchange.CancellationAgreements.Is(a)))
        attachments = attachments.Where(a => !Exchange.CancellationAgreements.Is(a)).ToList();
      var needSigningAttachments = _obj.NeedSigning.All.Select(a => Content.ElectronicDocuments.As(a)).ToList();
      var mainDocument = Functions.ExchangeDocumentProcessingAssignment.ShowMainDocumentChoosingDialog(attachments, needSigningAttachments, Docflow.OfficialDocuments.Info.Actions.SendForFreeApproval);
      if (mainDocument == null)
        return;
      var mainOfficialDocument = Docflow.OfficialDocuments.As(mainDocument);
      
      // Создать задачу.
      var freeApprovalTask = Sungero.Docflow.PublicFunctions.Module.Remote.CreateFreeApprovalTask(mainOfficialDocument);
      
      // Добавить вложения, которые не были добавлены при создании задачи.
      foreach (var attachment in attachments.Where(att => !freeApprovalTask.Attachments.Any(x => Equals(x, att))))
      {
        if (Docflow.PublicFunctions.OfficialDocument.NeedToAttachDocument(attachment, mainOfficialDocument))
          freeApprovalTask.OtherGroup.All.Add(attachment);
      }
      
      freeApprovalTask.Show();
    }

    public virtual bool CanSendForFreeApproval(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      if (!Docflow.PublicFunctions.Module.IsWorkStarted(_obj))
        return false;
      
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      if (!_obj.NeedSigning.All.Any(a => Exchange.CancellationAgreements.Is(a)))
        attachments = attachments.Where(a => !Exchange.CancellationAgreements.Is(a)).ToList();
      var documentsList = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments, Docflow.OfficialDocuments.Info.Actions.SendForFreeApproval);
      return documentsList.Any();
    }

    public virtual void SendForApproval(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Определить главный документ.
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      if (!_obj.NeedSigning.All.Any(a => Exchange.CancellationAgreements.Is(a)))
        attachments = attachments.Where(a => !Exchange.CancellationAgreements.Is(a)).ToList();
      var needSigningAttachments = _obj.NeedSigning.All.Select(a => Content.ElectronicDocuments.As(a)).ToList();
      var mainDocument = Functions.ExchangeDocumentProcessingAssignment
        .ShowMainDocumentChoosingDialog(attachments, needSigningAttachments, Docflow.OfficialDocuments.Info.Actions.SendForApproval);
      if (mainDocument == null)
        return;
      var mainOfficialDocument = Docflow.OfficialDocuments.As(mainDocument);
      
      // Проверить наличие регламента.
      var availableApprovalRules = Docflow.PublicFunctions.ApprovalRuleBase.Remote.GetAvailableRulesByDocument(mainOfficialDocument);
      if (availableApprovalRules.Any())
      {
        // Создать задачу.
        var approvalTask = Sungero.Docflow.PublicFunctions.Module.Remote.CreateApprovalTask(mainOfficialDocument);

        // Добавить вложения, которые не были добавлены при создании задачи.
        foreach (var attachment in attachments.Where(att => !approvalTask.Attachments.Any(x => Equals(x, att))))
        {
          if (Docflow.PublicFunctions.OfficialDocument.NeedToAttachDocument(attachment, mainOfficialDocument))
            approvalTask.OtherGroup.All.Add(attachment);
        }
        
        approvalTask.Show();
      }
      else
      {
        // Если по документу нет регламента, вывести сообщение.
        Dialogs.ShowMessage(Docflow.OfficialDocuments.Resources.NoApprovalRuleWarning, MessageType.Warning);
        return;
      }
    }

    public virtual bool CanSendForApproval(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      if (!Docflow.PublicFunctions.Module.IsWorkStarted(_obj))
        return false;
      
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      if (!_obj.NeedSigning.All.Any(a => Exchange.CancellationAgreements.Is(a)))
        attachments = attachments.Where(a => !Exchange.CancellationAgreements.Is(a)).ToList();
      var documentsList = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments, Docflow.OfficialDocuments.Info.Actions.SendForApproval);
      return documentsList.Any();
    }

    public virtual void Abort(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.Module.HasCurrentUserExchangeServiceCertificate(_obj.BusinessUnitBox))
      {
        e.AddError(Resources.RejectCertificateNotFoundReadressToResponsible);
        return;
      }
      
      if (string.IsNullOrEmpty(_obj.ActiveText))
      {
        e.AddError(ExchangeDocumentProcessingAssignments.Resources.NeedCommentToAbort);
        return;
      }
      else if (_obj.ActiveText.Length > 1000)
      {
        e.AddError(ExchangeDocumentProcessingAssignments.Resources.TextOverlong);
        return;
      }
      
      if (!Sungero.Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                         Constants.ExchangeDocumentProcessingTask.ExchangeDocumentProcessingAssignmentConfirmDialogID.Abort))
      {
        e.Cancel();
        return;
      }
      
      var certificate = Functions.Module.GetCurrentUserExchangeCertificate(_obj.Box, Company.Employees.Current);
      if (certificate == null)
      {
        e.AddError(Resources.RejectCertificateNotFoundReadressToResponsible);
        return;
      }
      
      if (!Functions.ExchangeDocumentProcessingAssignment.SendDeliveryConfirmation(_obj, certificate))
      {
        e.Cancel();
        return;
      }
      
      var documents = _obj.NeedSigning.All.Select(d => Docflow.OfficialDocuments.As(d)).Where(d => d != null).ToList();
      
      // СБИС требует вложения всех соглашений об аннулировании пакета.
      // Для каждого соглашения об аннулировании приходит отдельное задание на обработку.
      if (documents.Any(d => CancellationAgreements.Is(d)))
      {
        var cancellationAgreement = documents.Where(d => CancellationAgreements.Is(d)).First();
        var parentInfo = Functions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(cancellationAgreement.LeadingDocument);
        
        if (Exchange.PublicFunctions.Module.IsSbisExchangeService(parentInfo.RootBox))
        {
          var addenda = Functions.Module.Remote.GetSbisCancellationAgreementAddenda(CancellationAgreements.As(cancellationAgreement));
          if (addenda.Any())
          {
            e.AddError(Sungero.Exchange.ExchangeDocumentProcessingAssignments.Resources.PackageReject);
            return;
          }
        }
      }
      else
      {
        documents.AddRange(_obj.DontNeedSigning.All.Select(d => Docflow.OfficialDocuments.As(d)).Where(d => d != null).ToList());
      }
      
      var error = Exchange.PublicFunctions.Module.SendAmendmentRequest(documents, _obj.Counterparty, _obj.ActiveText, false, _obj.BusinessUnitBox, certificate, false);
      if (!string.IsNullOrWhiteSpace(error))
      {
        if (error == Resources.CertificateNotFound)
          e.AddError(Resources.RejectCertificateNotFoundReadressToResponsible);
        else if (error == Resources.AllAnswersIsAlreadySent)
          e.AddError(error);
        else
          e.AddError(Resources.CannotSendAmendmentNotice);
      }
    }

    public virtual bool CanAbort(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      var isIncoming = ExchangeDocumentProcessingTasks.As(_obj.Task).IsIncoming == true;
      return isIncoming && Docflow.OfficialDocuments.AccessRights.CanSendByExchange();
    }

    public virtual void ReAddress(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.ExchangeDocumentProcessingAssignment.ShowForwardingDialog(_obj))
        e.Cancel();
      
      if (!Functions.ExchangeDocumentProcessingAssignment.SendDeliveryConfirmation(_obj, null))
        e.Cancel();
    }

    public virtual bool CanReAddress(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return !Docflow.PublicFunctions.Module.IsCompetitive(_obj);
    }

    public virtual void Complete(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var attachmets = _obj.NeedSigning.All.ToList();
      attachmets.AddRange(_obj.DontNeedSigning.All.ToList());
      if (attachmets.Any(d => Sungero.Docflow.ExchangeDocuments.Is(d) &&
                         Functions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(Sungero.Docflow.ExchangeDocuments.As(d))
                         .RevocationStatus != Exchange.ExchangeDocumentInfo.RevocationStatus.Revoked))
      {
        e.AddError(ExchangeDocumentProcessingTasks.Resources.NotAllDocumentTypesAreChanged);
        return;
      }
      
      var areAllDocumentsInWork = Functions.ExchangeDocumentProcessingTask.Remote.AreAllDocumentsSendToWork(ExchangeDocumentProcessingTasks.As(_obj.Task));
      var dialogText = e.Action.ConfirmationMessage;
      var dialogID = Constants.ExchangeDocumentProcessingTask.ExchangeDocumentProcessingAssignmentConfirmDialogID.Complete;
      if (!areAllDocumentsInWork)
      {
        dialogText = ExchangeDocumentProcessingTasks.Resources.NotAllDocumentsSendedForProcessing;
        dialogID = Constants.ExchangeDocumentProcessingTask.ExchangeDocumentProcessingAssignmentConfirmDialogID.CompleteWithoutAllDocumentsSendedForProcessing;
      }

      if (!Sungero.Docflow.PublicFunctions.Module.ShowConfirmationDialog(dialogText, null, null, dialogID))
      {
        e.Cancel();
        return;
      }
      
      if (!Functions.ExchangeDocumentProcessingAssignment.SendDeliveryConfirmation(_obj, null))
      {
        e.Cancel();
        return;
      }

    }

    public virtual bool CanComplete(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }

}