using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.DocumentProcessingAssignment;

namespace Sungero.DocflowApproval.Client
{
  partial class DocumentProcessingAssignmentFunctions
  {
    #region Проверки перед выполнением задания
    
    /// <summary>
    /// Валидация задания перед выполнением.
    /// </summary>
    /// <param name="eventArgs">Аргументы действия.</param>
    /// <returns>True - если ошибок нет, иначе - False.</returns>
    public virtual bool ValidateBeforeComplete(Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var isValid = true;
      if (Functions.DocumentProcessingAssignment.AreDocumentsLockedByMe(_obj))
      {
        eventArgs.AddError(Resources.SaveDocumentsBeforeComplete);
        isValid = false;
      }
      
      // Проверить зарегистрированность документа, если стоит галочка регистрации.
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (officialDocument != null && _obj.RegisterDocument == true)
      {
        var registrationState = officialDocument.RegistrationState;
        if (registrationState == null || registrationState != Docflow.OfficialDocument.RegistrationState.Registered)
        {
          eventArgs.AddError(DocumentProcessingAssignments.Resources.ToCompleteNeedRegisterDocument);
          isValid = false;
        }
      }
      
      return isValid;
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
      
      if (Functions.DocumentProcessingAssignment.AreDocumentsLockedByMe(_obj))
      {
        eventArgs.AddError(Resources.SaveDocumentsBeforeComplete);
        isValid = false;
      }
      
      if (_obj.State.Properties.ReworkPerformer.IsVisible == true && _obj.ReworkPerformer == null)
      {
        eventArgs.AddError(DocflowApproval.Resources.CantSendForReworkWithoutPerformer);
        isValid = false;
      }
      
      return isValid;
    }
    
    #endregion
    
    /// <summary>
    /// Проверить возможность отправки документа контрагенту.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если можно отправить, иначе - false.</returns>
    public static bool CanSendToCounterparty(IOfficialDocument document)
    {
      return !document.State.IsInserted && !document.State.IsChanged &&
        document.AccessRights.CanUpdate() &&
        document.AccessRights.CanSendByExchange() &&
        document.HasVersions &&
        !Locks.GetLockInfo(document.LastVersion.Body).IsLocked;
    }
    
    /// <summary>
    /// Отправка документа, либо ответа контрагенту с учетом выбранного сервиса обмена и приложений в задаче на согласование.
    /// </summary>
    /// <param name="document">Документ.</param>
    public void SendToCounterparty(Sungero.Docflow.IOfficialDocument document)
    {
      var addenda = _obj.AddendaGroup.ElectronicDocuments
        .Where(x => OfficialDocuments.Is(x))
        .Select(x => OfficialDocuments.As(x))
        .ToList();
      Exchange.PublicFunctions.Module.SendResultToCounterparty(document, _obj.ExchangeService, addenda);
    }
    
    /// <summary>
    /// Запросить подтверждение выполнения задания без отправки документа.
    /// </summary>
    /// <returns>Код ответа пользователя: завершить задание, отменить, послать документ.</returns>
    public static string AskUserToCompleteAssignmentWithoutSend()
    {
      var dialog = Dialogs.CreateTaskDialog(DocumentProcessingAssignments.Resources.ExecuteWithoutSendToCounterparty, MessageType.Warning);
      dialog.Buttons.AddYes();
      dialog.Buttons.Default = DialogButtons.Yes;
      var send = dialog.Buttons.AddCustom(DocumentProcessingAssignments.Info.Actions.SendViaExchangeService.LocalizedName);
      dialog.Buttons.AddNo();
      
      var result = dialog.Show();
      if (result == DialogButtons.Yes)
        return Constants.Module.CompleteWithoutSend.Complete;
      
      if (result == DialogButtons.No || result == DialogButtons.Cancel)
        return Constants.Module.CompleteWithoutSend.Cancel;
      
      return Constants.Module.CompleteWithoutSend.Send;
    }

    /// <summary>
    /// Создать сопроводительное письмо.
    /// </summary>
    /// <param name="document">Документ, к которому создается сопроводительное письмо.</param>
    /// <param name="attachmentsGroup">Группа вложений.</param>
    public static void CreateCoverLetter(IOfficialDocument document, Workflow.Interfaces.IWorkflowEntityAttachmentGroup attachmentsGroup)
    {
      var correspondence = document.Relations.GetRelatedDocuments(Constants.Module.CorrespondenceRelationName)
        .Where(r => attachmentsGroup.All.Contains(r)).ToList();
      
      if (correspondence.Count == 1)
      {
        var dialog = Dialogs.CreateTaskDialog(DocumentProcessingAssignments.Resources.CoverLetterAlreadyExists,
                                              DocumentProcessingAssignments.Resources.OpenCoverLetterCardQuestion,
                                              MessageType.Question);
        dialog.Buttons.AddYesNo();
        dialog.Buttons.Default = DialogButtons.No;
        if (dialog.Show() == DialogButtons.Yes)
          correspondence.First().ShowModal();
      }
      else if (correspondence.Any())
      {
        Dialogs.ShowMessage(DocumentProcessingAssignments.Resources.SeveralCoverLettersAlreadyExist, MessageType.Information);
      }
      else
      {
        var letter = Docflow.PublicFunctions.Module.CreateCoverLetter(document);
        if (letter == null)
          return;
        
        // Открываем модально, чтобы следующая проверка прошла уже после того, как мы закончили создавать письмо, иначе условие не выполнится никогда.
        letter.ShowModal();
        // Если письмо сохранили - добавляем в задание.
        if (!letter.State.IsChanged)
          attachmentsGroup.All.Add(letter);
      }
    }
    
    /// <summary>
    /// Создать электронное письмо с документом.
    /// </summary>
    /// <param name="document">Документ для вложения.</param>
    public virtual void CreateEmail(IElectronicDocument document)
    {
      if (!Docflow.PublicFunctions.Module.AllowCreatingEmailWithLockedVersions(new List<IElectronicDocument>() { document }))
        return;
      
      var subject = Sungero.Docflow.OfficialDocuments.Resources.SendByEmailSubjectPrefixFormat(document.Name);
      var documents = new List<IElectronicDocument>() { document };
      
      Docflow.PublicFunctions.Module.CreateEmail(string.Empty, subject, documents);
    }
  }
}