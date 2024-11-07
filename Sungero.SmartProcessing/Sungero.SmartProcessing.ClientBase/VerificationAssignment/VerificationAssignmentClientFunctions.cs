using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.VerificationAssignment;

namespace Sungero.SmartProcessing.Client
{
  partial class VerificationAssignmentFunctions
  {
    /// <summary>
    /// Проверить заполнение обязательных полей во всех документах комплекта.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True - если обязательные поля заполнены, иначе - false.</returns>
    public virtual bool ValidateRequiredProperties(System.Collections.Generic.IEnumerable<Content.IElectronicDocument> documents,
                                                   Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var isAnyDocumentsWithEmptyProperties = documents
        .Where(a => Docflow.OfficialDocuments.Is(a))
        .Select(d => Docflow.OfficialDocuments.As(d))
        .Any(x => Sungero.Docflow.PublicFunctions.OfficialDocument.HasEmptyRequiredProperties(x));
      if (isAnyDocumentsWithEmptyProperties)
      {
        e.AddError(VerificationAssignments.Resources.InvalidDocumentWhenSendInWork, _obj.Info.Actions.ShowInvalidDocuments);
        return false;
      }
      return true;
    }
    
    /// <summary>
    /// Получить главный документ комплекта.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="action">Действие.</param>
    /// <returns>Главный документ.</returns>
    public virtual Docflow.IOfficialDocument GetMainDocument(System.Collections.Generic.IEnumerable<Content.IElectronicDocument> documents,
                                                             Domain.Shared.IActionInfo action)
    {
      var suitableDocuments = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(documents, action);
      var defaultMainDocuments = Functions.Module
        .GetTopPriorityDocuments(suitableDocuments.Select(d => Docflow.OfficialDocuments.As(d)));
      var mainDocument = Docflow.PublicFunctions.OfficialDocument
        .ChooseMainDocument(suitableDocuments, defaultMainDocuments);
      if (mainDocument != null && Docflow.OfficialDocuments.Is(mainDocument))
        return Docflow.OfficialDocuments.As(mainDocument);
      
      return Docflow.OfficialDocuments.Null;
    }
    
    /// <summary>
    /// Показать диалог переадресации.
    /// </summary>
    /// <returns>True, если запрос был подтвержден.</returns>
    public virtual bool ShowForwardingDialog()
    {
      var excludedPerformers = new List<IRecipient>();
      excludedPerformers.Add(_obj.Performer);
      
      var dialogResult = Docflow.PublicFunctions.Module.ShowForwardDialog(excludedPerformers, _obj.Deadline, TimeSpan.FromHours(4));
      if (dialogResult.ForwardButtonIsPressed)
      {
        _obj.Addressee = dialogResult.ForwardTo;
        _obj.NewDeadline = dialogResult.Deadline.Value;
        return true;
      }
      
      return false;
    }
  }
}