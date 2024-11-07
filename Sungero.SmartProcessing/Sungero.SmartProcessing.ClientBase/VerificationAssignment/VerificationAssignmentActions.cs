using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Shared.Validation;
using Sungero.SmartProcessing.VerificationAssignment;

namespace Sungero.SmartProcessing.Client
{
  partial class VerificationAssignmentActions
  {
    public virtual void CloseRepacking(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var session = Functions.RepackingSession.Remote.GetActiveSessionByAssignmentId(_obj.Id);
      if (session != null)
        Functions.Module.Remote.FinalizeRepackingSession(session);

      var hints = ((IValidationObject)_obj).ValidationResult;
      var hintGuid = hints.Messages.Where(x => x.Body.ToString().Contains(VerificationAssignments.Resources.CompleteWithActiveRepackingSession)).Select(x => x.MessageId).FirstOrDefault();
      hints.Clear(hintGuid);
    }

    public virtual bool CanCloseRepacking(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void SendForDocumentFlow(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      
      // Проверить заполненность обязательных полей во всех документах комплекта.
      if (!Functions.VerificationAssignment.ValidateRequiredProperties(_obj, attachments, e))
        return;
      
      // Определить главный документ.
      var mainOfficialDocument = Functions.VerificationAssignment
        .GetMainDocument(_obj, attachments, Docflow.OfficialDocuments.Info.Actions.SendForDocumentFlow);
      // Если главный документ не выбран.
      if (mainOfficialDocument == null)
        return;

      // Если по главному документу ранее были запущены задачи, то вывести соответствующий диалог.
      var createdTasks = DocflowApproval.PublicFunctions.Module.Remote.GetDocumentFlowTasks(mainOfficialDocument);
      if (createdTasks.Any())
        if (!Docflow.PublicFunctions.Module.RequestUserToConfirmDocumentFlowTaskCreation(createdTasks))
          return;
      
      var task = DocflowApproval.PublicFunctions.Module.Remote.CreateDocumentFlowTask(mainOfficialDocument);
      // Добавить вложения, которые не были добавлены при создании задачи.
      foreach (var attachment in attachments.Where(att => !task.Attachments.Any(x => Equals(x, att))))
      {
        if (Docflow.PublicFunctions.OfficialDocument.NeedToAttachDocument(attachment, mainOfficialDocument))
          task.OtherGroup.All.Add(attachment);
      }
      task.Show();
    }

    public virtual bool CanSendForDocumentFlow(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      if (!Docflow.PublicFunctions.Module.IsWorkStarted(_obj))
        return false;
      
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).ToList();
      var suitableDocuments = Docflow.PublicFunctions.OfficialDocument
        .GetSuitableDocuments(attachments, Docflow.OfficialDocuments.Info.Actions.SendForDocumentFlow);
      return suitableDocuments.Any();
    }

    public virtual void Repacking(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Commons.PublicConstants.Module.IntelligenceGuid))
      {
        Dialogs.NotifyMessage(VerificationAssignments.Resources.NoLicenseToRepacking);
        return;
      }
      
      if (_obj.State.IsChanged)
      {
        e.AddWarning(VerificationAssignments.Resources.SaveAssignmentBeforeRepacking);
        return;
      }

      var documents = Functions.VerificationAssignment.GetDocumentsSuitableForRepacking(_obj);
      var documentsAndVersions = documents.Select(x => Structures.RepackingSession.RepackingDocument.Create(x, x.LastVersion)).ToList();
      if (!documentsAndVersions.Any())
      {
        Dialogs.NotifyMessage(VerificationAssignments.Resources.NoDocumentsSuitableForRepacking);
        return;
      }

      var activeRepackingSession = Functions.RepackingSession.Remote.GetActiveSessionByAssignmentId(_obj.Id);

      if (activeRepackingSession == null)
      {
        if (Functions.Module.TryLockRepackingSessionDocuments(documentsAndVersions))
        {
          var repackingSession = SmartProcessing.Functions.Module.Remote.CreateRepackingSession(_obj.Id, documentsAndVersions);
          var url = Functions.RepackingSession.Remote.GetUrl(repackingSession);
          Hyperlinks.Open(url);
        }
        else
        {
          e.AddError(VerificationAssignments.Resources.AttachedDocumentsLocked);
          return;
        }
      }
      else
      {
        if (Functions.Module.TryLockRepackingSessionDocuments(documentsAndVersions))
        {
          activeRepackingSession.Status = SmartProcessing.RepackingSession.Status.Closed;
          activeRepackingSession.Save();
          
          var repackingSession = SmartProcessing.Functions.Module.Remote.CreateRepackingSession(_obj.Id, documentsAndVersions);
          var url = Functions.RepackingSession.Remote.GetUrl(repackingSession);
          Hyperlinks.Open(url);
        }
        else
          Dialogs.NotifyMessage(VerificationAssignments.Resources.RepackingIsInProgress);
      }
      e.Params.AddOrUpdate(Sungero.SmartProcessing.Constants.VerificationAssignment.ShowRepackingResultsNotificationParamName, true);
    }

    public virtual bool CanRepacking(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Sungero.Workflow.Assignment.Status.InProcess && Docflow.PublicFunctions.Module.IsWorkStarted(_obj);
    }

    public virtual void ShowInvalidDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var attachments = _obj.AllAttachments
        .Where(a => Content.ElectronicDocuments.Is(a))
        .Select(a => Content.ElectronicDocuments.As(a))
        .Distinct()
        .Select(d => Docflow.OfficialDocuments.As(d));
      
      var invalidDocuments = attachments
        .Where(x => Sungero.Docflow.PublicFunctions.OfficialDocument.HasEmptyRequiredProperties(x))
        .ToList();
      if (invalidDocuments.Count() > 0)
        invalidDocuments.Show();
    }

    public virtual bool CanShowInvalidDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void DeleteDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (RepackingSessions.GetAll(s => s.AssignmentId == _obj.Id && s.Status == SmartProcessing.RepackingSession.Status.Active).Any())
      {
        e.AddError(Sungero.SmartProcessing.VerificationAssignments.Resources.DeleteDocumentsDialogImpossible);
        return;
      }
      
      var documents = Functions.VerificationAssignment.GetOrderedDocuments(_obj)
        .Where(d => d.LifeCycleState != Docflow.OfficialDocument.LifeCycleState.Obsolete);
      var notSuitableDocuments = Functions.VerificationAssignment.GetInaccesssibleDocuments(_obj, documents);
      SmartProcessing.Client.ModuleFunctions.DeleteDocumentsDialog(_obj, documents.Except(notSuitableDocuments).ToList());
    }

    public virtual bool CanDeleteDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      if (_obj.Status != Sungero.Workflow.AssignmentBase.Status.InProcess || !Docflow.PublicFunctions.Module.IsWorkStarted(_obj))
        return false;
      
      return _obj.AllAttachments.Any(a => Content.ElectronicDocuments.Is(a));
    }

    public virtual void SendForExecution(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();

      // Проверить заполненность обязательных полей во всех документах комплекта.
      if (!Functions.VerificationAssignment.ValidateRequiredProperties(_obj, attachments, e))
        return;
      
      // Определить главный документ.
      var mainOfficialDocument = Functions.VerificationAssignment
        .GetMainDocument(_obj, attachments, Docflow.OfficialDocuments.Info.Actions.SendActionItem);
      if (mainOfficialDocument == null)
        return;
      
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
      var suitableDocuments = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments,
                                                                                            Docflow.OfficialDocuments.Info.Actions.SendActionItem);
      return suitableDocuments.Any();
    }

    public virtual void SendForReview(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();

      // Проверить заполненность обязательных полей во всех документах комплекта.
      if (!Functions.VerificationAssignment.ValidateRequiredProperties(_obj, attachments, e))
        return;
      
      // Определить главный документ.
      var mainOfficialDocument = Functions.VerificationAssignment
        .GetMainDocument(_obj, attachments, Docflow.OfficialDocuments.Info.Actions.SendForReview);
      if (mainOfficialDocument == null)
        return;
      
      // Если по главному документу ранее были запущены задачи, то вывести соответствующий диалог.
      if (!Docflow.PublicFunctions.OfficialDocument.NeedCreateReviewTask(mainOfficialDocument))
        return;

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
      var suitableDocuments = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments,
                                                                                            Docflow.OfficialDocuments.Info.Actions.SendForReview);
      return suitableDocuments.Any();
    }

    public virtual void SendForFreeApproval(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();

      // Проверить заполненность обязательных полей во всех документах комплекта.
      if (!Functions.VerificationAssignment.ValidateRequiredProperties(_obj, attachments, e))
        return;
      
      // Определить главный документ.
      var mainOfficialDocument = Functions.VerificationAssignment
        .GetMainDocument(_obj, attachments, Docflow.OfficialDocuments.Info.Actions.SendForFreeApproval);
      if (mainOfficialDocument == null)
        return;
      
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
      var suitableDocuments = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments,
                                                                                            Docflow.OfficialDocuments.Info.Actions.SendForFreeApproval);
      return suitableDocuments.Any();
    }

    public virtual void SendForApproval(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();

      // Проверить заполненность обязательных полей во всех документах комплекта.
      if (!Functions.VerificationAssignment.ValidateRequiredProperties(_obj, attachments, e))
        return;
      
      // Определить главный документ.
      var mainOfficialDocument = Functions.VerificationAssignment
        .GetMainDocument(_obj, attachments, Docflow.OfficialDocuments.Info.Actions.SendForApproval);
      if (mainOfficialDocument == null)
        return;
      
      // Если по главному документу ранее были запущены задачи, то вывести соответствующий диалог.
      if (!Docflow.PublicFunctions.OfficialDocument.NeedCreateApprovalTask(mainOfficialDocument))
        return;
      
      // Проверить наличие регламента.
      var availableApprovalRules = Docflow.PublicFunctions.ApprovalRuleBase.Remote.GetAvailableRulesByDocument(mainOfficialDocument);
      if (availableApprovalRules.Any())
      {
        var approvalTask = Docflow.PublicFunctions.Module.Remote.CreateApprovalTask(mainOfficialDocument);

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
      var suitableDocuments = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments,
                                                                                            Docflow.OfficialDocuments.Info.Actions.SendForApproval);
      return suitableDocuments.Any();
    }

    public virtual void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.VerificationAssignment.ShowForwardingDialog(_obj))
        e.Cancel();
    }

    public virtual bool CanForward(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Status == Status.InProcess &&
        Functions.VerificationTask.HasDocumentAndCanRead(VerificationTasks.As(_obj.Task)) &&
        !Docflow.PublicFunctions.Module.IsCompetitive(_obj);
    }

    public virtual void Complete(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var attachments = _obj.AllAttachments
        .Where(a => Content.ElectronicDocuments.Is(a))
        .Select(a => Content.ElectronicDocuments.As(a))
        .Distinct()
        .Select(d => Docflow.OfficialDocuments.As(d));
      
      var haveErrors = false;
      if (attachments.Any(x => Sungero.Docflow.PublicFunctions.OfficialDocument.HasEmptyRequiredProperties(x)))
      {
        e.AddError(VerificationAssignments.Resources.InvalidDocumentWhenCompleted,
                   _obj.Info.Actions.ShowInvalidDocuments);
        haveErrors = true;
      }

      var session = Functions.RepackingSession.Remote.GetActiveSessionByAssignmentId(_obj.Id);
      if (session != null)
      {
        e.AddError(VerificationAssignments.Resources.CompleteWithActiveRepackingSession, _obj.Info.Actions.CloseRepacking);
        haveErrors = true;
      }
      
      if (haveErrors)
        e.Cancel();
    }

    public virtual bool CanComplete(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }

}