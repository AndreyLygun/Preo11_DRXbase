using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalTask;

namespace Sungero.Docflow
{
  partial class FreeApprovalTaskObserversObserverPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> ObserversObserverFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return (IQueryable<T>)RecordManagement.PublicFunctions.Module.ObserversFiltering(query);
    }
  }

  partial class FreeApprovalTaskApproversApproverPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ApproversApproverFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return (IQueryable<T>)RecordManagement.PublicFunctions.Module.ObserversFiltering(query);
    }
  }

  partial class FreeApprovalTaskCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Map(_info.Properties.Subject, _source.Subject);
    }
  }

  partial class FreeApprovalTaskServerHandlers
  {

    public override void BeforeRestart(Sungero.Workflow.Server.BeforeRestartEventArgs e)
    {
     
    }

    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      if (!Sungero.Docflow.Functions.FreeApprovalTask.ValidateFreeApprovalTaskStart(_obj, e))
        return;
      
      var primaryDocument = _obj.ForApprovalGroup.ElectronicDocuments.FirstOrDefault();     
      var addendaToRelate = Sungero.DocflowApproval.PublicFunctions.Module.GetNonObsoleteDocumentsFromAttachments(primaryDocument, _obj.AddendaGroup.All);
      Sungero.DocflowApproval.PublicFunctions.Module.RelateDocumentsToPrimaryDocumentAsAddenda(primaryDocument, addendaToRelate);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.NeedsReview = false;
      
      if (!_obj.State.IsCopied)
      {
        _obj.ReceiveOnCompletion = ReceiveOnCompletion.Assignment;
        _obj.ReceiveNotice = true;
        _obj.Subject = Docflow.Resources.AutoformatTaskSubject;
        
        // Получить ресурсы в культуре тенанта.
        using (TenantInfo.Culture.SwitchTo())
          _obj.ActiveText = FreeApprovalTasks.Resources.ApprovalText;
      }
      
    }
  }

}