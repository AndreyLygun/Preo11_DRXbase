using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.DocumentFlowTask;

namespace Sungero.DocflowApproval.Client
{
  partial class DocumentFlowTaskFunctions
  {
    /// <summary>
    /// Вывод диалога запроса причины прекращения задачи согласования.
    /// </summary>
    /// <param name="activeText">Причина прекращения.</param>
    /// <param name="e">Аргумент события.</param>
    /// <param name="fromTask">Признак того, что проверка запускается из задачи.</param>
    /// <returns>True, если пользователь нажал Ok.</returns>
    public bool GetReasonBeforeAbort(string activeText, Sungero.Domain.Client.ExecuteActionArgs e, bool fromTask)
    {
      var dialog = Dialogs.CreateInputDialog(DocumentFlowTasks.Resources.Confirmation);
      var abortingReason = dialog.AddMultilineString(_obj.Info.Properties.AbortingReason.LocalizedName, true, activeText);
      CommonLibrary.IBooleanDialogValue needSetDocumentObsolete = null;
      IOfficialDocument officialDocument = null;

      if (Functions.DocumentFlowTask.HasDocumentAndCanRead(_obj))
      {
        officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.First());
        if (officialDocument != null)
        {
          var textToMarkDocumentAsObsolete = Docflow.PublicFunctions.OfficialDocument.GetTextToMarkDocumentAsObsolete(officialDocument);
          var defaultMarkDocumentAsObsoleteValue = Docflow.PublicFunctions.OfficialDocument.MarkDocumentAsObsolete(officialDocument);
          needSetDocumentObsolete = dialog.AddBoolean(textToMarkDocumentAsObsolete, defaultMarkDocumentAsObsoleteValue);
        }
      }
      
      dialog.SetOnButtonClick(args =>
                              {
                                if (string.IsNullOrWhiteSpace(abortingReason.Value))
                                  args.AddError(DocumentFlowTasks.Resources.EmptyAbortingReason, abortingReason);
                                
                                if (fromTask)
                                {
                                  var actualModified = Functions.DocumentFlowTask.Remote.GetDocumentFlowTaskModified(_obj);
                                  if (!Equals(_obj.Modified, actualModified))
                                  {
                                    if (needSetDocumentObsolete != null)
                                      needSetDocumentObsolete.IsEnabled = false;
                                    args.AddError(DocumentFlowTasks.Resources.CantUpdateTask);
                                  }
                                }
                              });
      
      if (dialog.Show() == DialogButtons.Ok)
      {
        _obj.AbortingReason = abortingReason.Value;
        if (needSetDocumentObsolete != null && needSetDocumentObsolete.Value.Value == true)
        {
          var isActive = officialDocument.LifeCycleState == Sungero.Docflow.OfficialDocument.LifeCycleState.Active;
          var isDraft = officialDocument.LifeCycleState == Sungero.Docflow.OfficialDocument.LifeCycleState.Draft;
          
          if (isActive || isDraft || officialDocument.LifeCycleState == null)
            ((Domain.Shared.IExtendedEntity)_obj).Params[Constants.DocumentFlowTask.NeedSetDocumentObsoleteParamName] = true;
        }
        return true;
      }
      return false;
    }
    
    /// <summary>
    /// Показать хинт при прекращении задачи на согласование.
    /// </summary>
    public virtual void AbortAsyncProcessingNotify()
    {
      if (!Functions.DocumentFlowTask.HasDocumentAndCanRead(_obj))
        return;
      
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.First());
      if (officialDocument == null)
        return;
      
      var needSetStateAsync = !officialDocument.AccessRights.CanUpdate() || Locks.GetLockInfo(officialDocument).IsLocked;
      if (needSetStateAsync)
        Dialogs.NotifyMessage(ApprovalTasks.Resources.DocumentStateWillBeUpdatedLater);
    }
    
    /// <summary>
    /// Подписать документы из задачи.
    /// </summary>
    /// <param name="initiator">Инициатор задачи.</param>
    /// <returns>Если подписание успешно - пустая строка, иначе - текст возникшей ошибки.</returns>
    public virtual string EndorseDocuments(IEmployee initiator)
    {
      var mainDocument = _obj.DocumentGroup.ElectronicDocuments.First();
      var addenda = Functions.Module.GetNonObsoleteDocumentsFromAttachments(mainDocument, _obj.AddendaGroup.All);
      var needStrongSignature = Functions.Module.Remote.NeedStrongSignature(_obj);
      return Functions.Module.EndorseDocuments(mainDocument, addenda, initiator, true, needStrongSignature, string.Empty);
    }
    
    /// <summary>
    /// Проверить возможность старта задачи.
    /// </summary>
    /// <param name="eventArgs">Аргумент обработчика вызова.</param>
    /// <returns>True - разрешить старт задачи, иначе false.</returns>
    public virtual bool ValidateDocumentFlowTaskStart(Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var isValid = true;
      if (!Sungero.Company.Employees.Is(_obj.Author))
      {
        eventArgs.AddError(DocumentFlowTasks.Resources.CantSendTaskByNonEmployee);
        isValid = false;
      }
      
      if (this.AreDocumentsLockedByMe(eventArgs))
        isValid = false;
      
      var signingBlockErrors = Functions.DocumentFlowTask.Remote.ValidateSigningBlocksPerformers(_obj);
      foreach (var error in signingBlockErrors)
        eventArgs.AddError(error);
      
      if (signingBlockErrors.Any())
        isValid = false;
      
      var document = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      var initiator = Sungero.Company.Employees.As(_obj.Author);
      var certificatesError = Functions.Module.ValidateCertificatesBeforeApproval(_obj, document, initiator);
      if (!string.IsNullOrEmpty(certificatesError))
      {
        eventArgs.AddError(certificatesError);
        isValid = false;
      }
      
      return isValid;
    }
    
    /// <summary>
    /// Проверить, не заблокированы ли документы текущим пользователем.
    /// </summary>
    /// <param name="eventArgs">Аргумент обработчика вызова.</param>
    /// <returns>True - хотя бы один заблокирован, False - все свободны.</returns>
    public virtual bool AreDocumentsLockedByMe(Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var documents = new List<IElectronicDocument>();
      documents.Add(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      documents.AddRange(_obj.AddendaGroup.ElectronicDocuments);
      
      if (Functions.Module.IsAnyDocumentLockedByCurrentEmployee(documents))
      {
        eventArgs.AddError(DocumentFlowTasks.Resources.SaveDocumentsBeforeStart);
        return true;
      }
      
      return false;
    }
    
    /// <summary>
    /// Выдать наблюдателям права на вложения с помощью платформенного диалога.
    /// </summary>
    /// <returns>True - если права были выданы или не требовалось выдавать, иначе - false.</returns>
    public virtual bool GrantReadAccessRightToObservers()
    {
      var observers = _obj.Observers.Select(o => o.Observer);
      var attachments = _obj.OtherGroup.All
        .Concat(_obj.AddendaGroup.ElectronicDocuments)
        .Concat(_obj.DocumentGroup.ElectronicDocuments)
        .ToList();
      
      if (!observers.Any() || !attachments.Any())
        return true;
      
      var attachmentsWithoutAccessRights = Docflow.PublicFunctions.Module.Remote.GetAttachmentsWithoutAccessRights(observers.ToList(), attachments);
      if (attachmentsWithoutAccessRights.Any())
      {
        if (!Workflow.Client.ModuleFunctions.ShowDialogGrantAccessRights(observers, attachmentsWithoutAccessRights))
          return false;
      }
      
      return true;
    }
    
  }
}