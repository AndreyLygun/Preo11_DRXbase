using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.DocumentProcessingAssignment;

namespace Sungero.DocflowApproval.Client
{
  partial class DocumentProcessingAssignmentActions
  {
    public virtual void CreateAcquaintance(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var subTask = Functions.Module.CreateAcquaintanceTaskAsSubtask(_obj);
      if (subTask != null)
      {
        var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
        RecordManagement.PublicFunctions.AcquaintanceTask
          .SyncAttachmentsToGroups(subTask,
                                   officialDocument,
                                   _obj.AddendaGroup.ElectronicDocuments.ToList(),
                                   _obj.OtherGroup.All.ToList());
        subTask.ShowModal();
      }
    }

    public virtual bool CanCreateAcquaintance(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status.Value == Workflow.Task.Status.InProcess && Docflow.PublicFunctions.Module.IsWorkStarted(_obj);
    }

    public virtual void CreateActionItem(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Сохраняем задание, чтобы только что добавленные документы тоже синхронизировались в поручение.
      _obj.Save();
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      var actionItem = Functions.Module.CreateActionItemExecution(officialDocument, _obj.Id);
      if (actionItem != null)
        actionItem.ShowModal();
    }

    public virtual bool CanCreateActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.CreateActionItems == true &&
        _obj.Status.Value == Workflow.Task.Status.InProcess &&
        Docflow.PublicFunctions.Module.IsWorkStarted(_obj);
    }

    public virtual void ApprovalForm(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!_obj.DocumentGroup.ElectronicDocuments.Any())
      {
        e.AddError(Docflow.Resources.NoRightsToDocument);
        return;
      }
      
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.Single());
      Docflow.PublicFunctions.Module.RunApprovalSheetReport(officialDocument);
    }

    public virtual bool CanApprovalForm(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return (_obj.PrintDocument == true || _obj.RegisterDocument == true || _obj.SendToCounterparty == true) &&
        _obj.DocumentGroup.ElectronicDocuments.Any(d => OfficialDocuments.Is(d) && d.HasVersions);
    }

    public virtual void CreateCoverLetter(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!_obj.DocumentGroup.ElectronicDocuments.Any())
      {
        e.AddError(Docflow.Resources.NoRightsToDocument);
        return;
      }
      
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (officialDocument == null)
        return;
      Functions.DocumentProcessingAssignment.CreateCoverLetter(officialDocument, _obj.OtherGroup);
    }

    public virtual bool CanCreateCoverLetter(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (officialDocument == null)
        return false;
      
      return _obj.SendToCounterparty == true &&
        _obj.Status == Status.InProcess &&
        officialDocument.DocumentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts &&
        Docflow.PublicFunctions.Module.IsWorkStarted(_obj);
    }

    public virtual void SendByMail(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var mainDocument = _obj.DocumentGroup.ElectronicDocuments.First();
      var officialDocument = OfficialDocuments.As(mainDocument);
      
      if (officialDocument == null)
      {
        Functions.DocumentProcessingAssignment.CreateEmail(_obj, mainDocument);
      }
      else
      {
        var addenda = _obj.AddendaGroup.ElectronicDocuments
          .Where(d => OfficialDocuments.Is(d) && d.HasVersions)
          .Cast<IOfficialDocument>()
          .ToList();
        
        var other = _obj.OtherGroup.All
          .Where(x => OfficialDocuments.Is(x))
          .Cast<IOfficialDocument>()
          .Where(x => x.HasVersions)
          .ToList();
        
        var relatedDocuments = new List<IOfficialDocument>();
        relatedDocuments.AddRange(addenda);
        relatedDocuments.AddRange(other);
        
        Sungero.Docflow.PublicFunctions.OfficialDocument.SelectRelatedDocumentsAndCreateEmail(officialDocument, relatedDocuments);
      }
    }

    public virtual bool CanSendByMail(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.SendToCounterparty == true &&
        _obj.Status == Status.InProcess &&
        Docflow.PublicFunctions.Module.IsWorkStarted(_obj) &&
        _obj.DocumentGroup.ElectronicDocuments.Any(d => d.HasVersions);
    }

    public virtual void SendViaExchangeService(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!_obj.DocumentGroup.ElectronicDocuments.Any())
      {
        e.AddError(Docflow.Resources.NoRightsToDocument);
        return;
      }
      
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (officialDocument != null)
        Functions.DocumentProcessingAssignment.SendToCounterparty(_obj, officialDocument);
    }

    public virtual bool CanSendViaExchangeService(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (officialDocument == null)
        return false;
      
      return _obj.SendToCounterparty == true &&
        _obj.Status == Status.InProcess &&
        Docflow.PublicFunctions.Module.IsWorkStarted(_obj) &&
        Functions.DocumentProcessingAssignment.CanSendToCounterparty(officialDocument);
    }

    public virtual void ForRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.DocumentProcessingAssignment.ValidateBeforeRework(_obj, e))
        e.Cancel();
      
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                 null,
                                                                 null,
                                                                 Constants.DocumentProcessingAssignment.ForReworkConfirmDialogID))
        e.Cancel();
      
    }

    public virtual bool CanForRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void Complete(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.DocumentProcessingAssignment.ValidateBeforeComplete(_obj, e))
        e.Cancel();
      
      var needShowStandardDialog = true;
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (officialDocument != null)
        needShowStandardDialog = this.CheckSendingToCounterparty(officialDocument, e);
      
      // Проверить подчиненные поручения, если стоит галочка создания поручений.
      if (_obj.CreateActionItems == true)
      {
        var dialogWasShown = Functions.Module.ShowConfirmationDialogForCreatingActionItem(_obj, officialDocument, e);
        needShowStandardDialog = dialogWasShown == false;
      }
      
      // Если подтверждение выполнения не было запрошено, то показать стандартный диалог подтверждения.
      if (needShowStandardDialog)
      {
        var userResponse = Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                                 null,
                                                                                 null,
                                                                                 Constants.DocumentProcessingAssignment.CompleteConfirmDialogID);
        if (userResponse == false)
        {
          e.Cancel();
        }
      }
    }
    
    public virtual bool CanComplete(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }
    
    private bool CheckSendingToCounterparty(IOfficialDocument officialDocument, Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!this.IsTryingToCompleteWithoutSend(officialDocument))
        return true;
      
      var userResponse = Functions.DocumentProcessingAssignment.AskUserToCompleteAssignmentWithoutSend();
      switch (userResponse)
      {
        case Constants.Module.CompleteWithoutSend.Complete:
          break;
        case Constants.Module.CompleteWithoutSend.Cancel:
          e.Cancel();
          break;
        case Constants.Module.CompleteWithoutSend.Send:
          Functions.DocumentProcessingAssignment.SendToCounterparty(_obj, officialDocument);
          
          // Если отправка так и не была выполнена - отменяем выполнение.
          if (!Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.LastVersionSended(officialDocument))
            e.Cancel();
          break;
        default:
          break;
      }
      return false;
    }
    
    private bool IsTryingToCompleteWithoutSend(IOfficialDocument officialDocument)
    {
      if (_obj.SendToCounterparty != true ||
          _obj.DeliveryMethod?.Sid != Constants.Module.ExchangeDeliveryMethodSid ||
          !Functions.DocumentProcessingAssignment.CanSendToCounterparty(officialDocument) ||
          Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.LastVersionSended(officialDocument))
      {
        return false;
      }
      
      return true;
    }

  }
}