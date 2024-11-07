using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.DocumentFlowTask;

namespace Sungero.DocflowApproval
{
  partial class DocumentFlowTaskExchangeServicePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ExchangeServiceFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      var services = Functions.Module.GetExchangeServices(officialDocument).Services;
      query = query.Where(s => services.Contains(s));
      return query;
    }
  }

  partial class DocumentFlowTaskAddApproversApproverPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> AddApproversApproverFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = query.Where(c => c.Status == CoreEntities.DatabookEntry.Status.Active);
      
      // Отфильтровать всех пользователей.
      query = query.Where(x => x.Sid != Sungero.Domain.Shared.SystemRoleSid.AllUsers);
      
      // Отфильтровать служебные роли.
      return (IQueryable<T>)RecordManagement.PublicFunctions.Module.ObserversFiltering(query);
    }
  }

  partial class DocumentFlowTaskObserversObserverPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> ObserversObserverFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return (IQueryable<T>)RecordManagement.PublicFunctions.Module.ObserversFiltering(query);
    }
  }

  partial class DocumentFlowTaskServerHandlers
  {

    public override void BeforeRestart(Sungero.Workflow.Server.BeforeRestartEventArgs e)
    {
      long processKindId = 0;
      if (_obj.ProcessKind == null && e.Params.TryGetValue(Constants.DocumentFlowTask.LastProcessKindIdParamName, out processKindId))
        _obj.ProcessKind = Sungero.Workflow.ProcessKinds.GetAll(x => x.Id == processKindId).FirstOrDefault();
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (!_obj.State.IsCopied)
      {
        _obj.Subject = DocflowApproval.Resources.AutoformatTaskSubject;
        
        using (TenantInfo.Culture.SwitchTo())
          _obj.ActiveText = DocumentFlowTasks.Resources.ApprovalText;
      }
    }

    public override void BeforeAbort(Sungero.Workflow.Server.BeforeAbortEventArgs e)
    {
      // Если прекращён черновик, прикладную логику по прекращению выполнять не надо.
      if (_obj.State.Properties.Status.OriginalValue == Workflow.Task.Status.Draft)
        return;
      
      Functions.DocumentFlowTask.SendApprovalAbortNotice(_obj);
      
      bool setObsolete;
      e.Params.TryGetValue(Constants.DocumentFlowTask.NeedSetDocumentObsoleteParamName, out setObsolete);
      
      if (Functions.DocumentFlowTask.CanAbortSynchronously(_obj))
        Functions.DocumentFlowTask.ProcessTaskAbort(_obj, setObsolete);
      else
        Functions.DocumentFlowTask.ProcessTaskAbortAsync(_obj, setObsolete);
    }

    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      var signingBlockErrors = Functions.DocumentFlowTask.ValidateSigningBlocksPerformers(_obj);
      foreach (var error in signingBlockErrors)
        e.AddError(error);
      
      if (signingBlockErrors.Any())
        return;
      
      Functions.DocumentFlowTask.PreserveAuthorOriginalAttachmentsRights(_obj);
      
      var primaryDocument = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      var addendaToRelate = Functions.Module.GetNonObsoleteDocumentsFromAttachments(primaryDocument, _obj.AddendaGroup.All);
      Functions.Module.RelateDocumentsToPrimaryDocumentAsAddenda(primaryDocument, addendaToRelate);
      
      var officialDocument = OfficialDocuments.As(primaryDocument);
      if (officialDocument == null)
        return;
      
      Functions.DocumentFlowTask.UpdateOfficialDocumentStateOnStart(_obj, officialDocument);
    }

  }
}