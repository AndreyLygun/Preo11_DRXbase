using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.DocumentFlowTask;

namespace Sungero.DocflowApproval
{
  partial class DocumentFlowTaskSharedHandlers
  {

    public override void ProcessKindChanged(Sungero.Workflow.Shared.TaskProcessKindChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      Functions.DocumentFlowTask.Remote.FillEntityParams(_obj);
      
      Functions.DocumentFlowTask.UpdateFieldsAvailability(_obj);
      
      if (_obj.DocumentGroup.ElectronicDocuments.Any())
        PublicFunctions.DocumentFlowTask.SetDefaultDeliveryMethod(_obj);
      Functions.DocumentFlowTask.SetDefaultAddressees(_obj);
    }
    
    public virtual void DeliveryMethodChanged(Sungero.DocflowApproval.Shared.DocumentFlowTaskDeliveryMethodChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      if (e.NewValue == null || e.NewValue.Sid != Constants.Module.ExchangeDeliveryMethodSid)
      {
        _obj.ExchangeService = null;
        _obj.State.Properties.ExchangeService.IsEnabled = false;
      }
      else
      {
        _obj.State.Properties.ExchangeService.IsEnabled = true;
        var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
        _obj.ExchangeService = Functions.Module.Remote.GetExchangeServices(officialDocument).DefaultService;
      }
      
      Functions.DocumentFlowTask.UpdateDeliveryMethodAvailability(_obj);
    }

    public virtual void AddendaGroupPopulating(Sungero.Workflow.Interfaces.AttachmentGroupPopulatingEventArgs e)
    {
      e.PopulateFrom(_obj.DocumentGroup, documentGroup =>
                     {
                       var mainDocument = documentGroup.All
                         .Select(att => Content.ElectronicDocuments.As(att))
                         .FirstOrDefault(doc => doc != null);
                       
                       if (mainDocument == null)
                         return Enumerable.Empty<Sungero.Content.IElectronicDocument>();

                       // Документы, связанные связью "Приложение" с основным документом.
                       return Docflow.PublicFunctions.Module.GetAllAddenda(mainDocument);
                     });
    }

    public virtual void DocumentGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      _obj.Subject = DocflowApproval.Resources.AutoformatTaskSubject;
      
      // Очистить группу Дополнительно.
      if (OfficialDocuments.Is(e.Attachment))
        Docflow.PublicFunctions.OfficialDocument.RemoveRelatedDocumentsFromAttachmentGroup(OfficialDocuments.As(e.Attachment), _obj.OtherGroup);
      
      Functions.DocumentFlowTask.HideAndClearDeliveryFields(_obj);
      var param = ((Domain.Shared.IExtendedEntity)_obj).Params;
      if (param.ContainsKey(Constants.DocumentFlowTask.NeedToShowExchangeServiceHint))
        param.Remove(Constants.DocumentFlowTask.NeedToShowExchangeServiceHint);
      
      if (Memos.Is(e.Attachment))
        _obj.State.Properties.Addressees.IsEnabled = true;
      _obj.State.Properties.Addressees.IsVisible = false;
      _obj.Addressees.Clear();
    }

    public virtual void DocumentGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      var document = _obj.DocumentGroup.ElectronicDocuments.First();
      
      using (TenantInfo.Culture.SwitchTo())
        _obj.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentFlowTasks.Resources.TaskSubject, document.Name);
      
      Functions.DocumentFlowTask.SetDefaultProcessKind(_obj);
      
      var officialDocument = OfficialDocuments.As(document);
      if (officialDocument != null)
      {
        if (!_obj.State.IsCopied)
          Docflow.PublicFunctions.OfficialDocument.AddRelatedDocumentsToAttachmentGroup(officialDocument, _obj.OtherGroup);
        
        Functions.DocumentFlowTask.UpdateFieldsAvailability(_obj);
        
        PublicFunctions.DocumentFlowTask.SetDefaultDeliveryMethod(_obj);
        Functions.DocumentFlowTask.SetDefaultAddressees(_obj);
      }
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue.Length > DocumentFlowTasks.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, DocumentFlowTasks.Info.Properties.Subject.Length);
      
      if (string.IsNullOrWhiteSpace(e.NewValue))
        _obj.Subject = DocflowApproval.Resources.AutoformatTaskSubject;
    }

  }
}