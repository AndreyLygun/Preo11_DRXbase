using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.DocflowApproval.EntityReworkAssignment;

namespace Sungero.DocflowApproval.Client
{
  partial class EntityReworkAssignmentFunctions
  {

    /// <summary>
    /// Открыть вкладку "Состав согласующих".
    /// </summary>       
    public void ChangeApprovers()
    {
      _obj.State.Pages.Approvers.Activate();
    }
    
    #region Проверки перед выполнением задания
    
    /// <summary>
    /// Валидация задания перед повторной отправкой.
    /// </summary>
    /// <param name="eventArgs">Аргументы действия.</param>
    /// <returns>True - если ошибок нет, иначе - False.</returns>
    public virtual bool ValidateBeforeForReapproval(Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var isValid = true;
      if (Functions.EntityReworkAssignment.AreDocumentsLockedByMe(_obj))
      {
        eventArgs.AddError(Resources.SaveDocumentsBeforeComplete);
        isValid = false;
      }
      
      var document = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      var performer = Sungero.Company.Employees.As(_obj.Performer);
      var certificatesError = Functions.Module.ValidateCertificatesBeforeApproval(_obj.Task, document, performer);
      if (!string.IsNullOrEmpty(certificatesError))
      {
        eventArgs.AddError(certificatesError);
        isValid = false;
      }
      
      if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.NewDeadline, Calendar.Now))
      {
        eventArgs.AddError(EntityReworkAssignments.Resources.ImpossibleSpecifyDeadlineLessThanToday);
        isValid = false;
      }
      
      return isValid;
    }
    
    /// <summary>
    /// Валидация задания перед переадресацией.
    /// </summary>
    /// <param name="eventArgs">Аргументы действия.</param>
    /// <returns>True - если ошибок нет, иначе - False.</returns>
    public virtual bool ValidateBeforeForward(Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      if (Functions.EntityReworkAssignment.AreDocumentsLockedByMe(_obj))
      {
        eventArgs.AddError(Resources.SaveDocumentsBeforeComplete);
        return false;
      }
      
      return true;
    }
    
    #endregion
    
    /// <summary>
    /// Вызвать диалог продления срока задания.
    /// </summary>
    /// <param name="oldDeadline">Старый срок.</param>
    /// <returns>Новый срок в случае нажатия кнопки "Продлить", иначе null.</returns>
    public static DateTime? GetNewDeadline(DateTime? oldDeadline)
    {
      var dialog = Dialogs.CreateInputDialog(EntityReworkAssignments.Resources.DeadlineExtension);
      dialog.HelpCode = Constants.Module.HelpCodes.DeadlineExtensionDialog;
      var newDeadline = dialog.AddDate(EntityReworkAssignments.Resources.NewDeadline, true).AsDateTime();
      newDeadline.Value = oldDeadline < Calendar.Now ? Calendar.Now.AddWorkingDays(Users.Current, 3) : oldDeadline.Value.AddWorkingDays(Users.Current, 3);
      
      dialog.Buttons.AddCustom(EntityReworkAssignments.Resources.ExtendButton);
      dialog.Buttons.AddCancel();
      dialog.SetOnButtonClick((args) =>
                              {
                                if (!Docflow.PublicFunctions.Module.CheckDeadline(newDeadline.Value, oldDeadline))
                                  args.AddError(EntityReworkAssignments.Resources.NewDeadlineIsIncorrect);
                              });
      
      if (dialog.Show() != DialogButtons.Cancel)
        return newDeadline.Value;
      return null;
    }
    
    /// <summary>
    /// Подписать документы из задания.
    /// </summary>
    /// <returns>Если подписание успешно - пустая строка, иначе - текст возникшей ошибки.</returns>
    public virtual string EndorseDocuments()
    {
      var mainDocument = _obj.DocumentGroup.ElectronicDocuments.First();
      var addenda = Functions.Module.GetNonObsoleteDocumentsFromAttachments(mainDocument, _obj.AddendaGroup.All);
      var performer = Company.Employees.As(_obj.Performer);
      var needStrongSignature = Functions.Module.Remote.NeedStrongSignature(_obj.Task);
      return Functions.Module.EndorseDocuments(mainDocument, addenda, performer, true, needStrongSignature, string.Empty);
    }
    
    /// <summary>
    /// Показать диалог переадресации.
    /// </summary>
    /// <returns>True, если запрос был подтвержден.</returns>
    public virtual bool ShowForwardingDialog()
    {
      var notAvailablePerformers = Functions.EntityReworkAssignment.Remote.GetActiveAndFutureAssignmentsPerformers(_obj).ToList();
      var dialogResult = Docflow.PublicFunctions.Module.ShowForwardDialog(notAvailablePerformers);
      
      if (dialogResult.ForwardButtonIsPressed)
      {
        _obj.ForwardTo = dialogResult.ForwardTo;
        return true;
      }
      
      return false;
    }
  }
}