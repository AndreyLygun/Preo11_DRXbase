using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using Sungero.Contracts.SupAgreement;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using DeclensionCase = Sungero.Core.DeclensionCase;

namespace Sungero.Contracts.Server
{
  partial class SupAgreementFunctions
  {
    /// <summary>
    /// Получить дубли доп. соглашения.
    /// </summary>
    /// <param name="supAgreement">Доп. соглашение.</param>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="registrationDate">Дата регистрации.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="contractualDocument">Договорной документ.</param>
    /// <returns>Дубли.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<ISupAgreement> GetDuplicates(ISupAgreement supAgreement,
                                                          Sungero.Company.IBusinessUnit businessUnit,
                                                          string registrationNumber,
                                                          DateTime? registrationDate,
                                                          Sungero.Parties.ICounterparty counterparty,
                                                          IOfficialDocument contractualDocument)
    {
      return SupAgreements.GetAll()
        .Where(l => Equals(supAgreement.DocumentKind, l.DocumentKind))
        .Where(l => Equals(businessUnit, l.BusinessUnit))
        .Where(l => registrationDate == l.RegistrationDate)
        .Where(l => registrationNumber == l.RegistrationNumber)
        .Where(l => Equals(counterparty, l.Counterparty))
        .Where(l => Equals(contractualDocument, l.LeadingDocument))
        .Where(l => !Equals(supAgreement, l));
    }

    /// <summary>
    /// Получить часть имени доп.соглашения, игнорируя права доступа.
    /// </summary>
    /// <param name="supAgreementId">ИД доп.соглашения.</param>
    /// <returns>Часть имени.</returns>
    [Remote(IsPure = true)]
    public static string GetNamePartBySupAgreementIgnoreAccessRights(long supAgreementId)
    {
      return Functions.SupAgreement.GetNamePartBySupAgreement(SupAgreements.As(Functions.ContractualDocument.GetIgnoreAccessRights(supAgreementId)));
    }
    
    /// <summary>
    /// Получает правила согласования для доп. соглашений.
    /// </summary>
    /// <returns>Правила согласования, удовлетворяющие доп. соглашению + правила ведущего договорного документа.</returns>
    [Remote]
    public override List<IApprovalRuleBase> GetApprovalRules()
    {
      var rules = base.GetApprovalRules().OrderByDescending(r => r.Priority).ToList();
      
      // Если заполнен ведущий договор или доп. соглашение - добавить его правила для выбора.
      if (_obj.LeadingDocument != null)
      {
        var leadingDocumentRules = new List<IApprovalRuleBase>();
        
        if (ContractBases.Is(_obj.LeadingDocument))
          leadingDocumentRules = Docflow.PublicFunctions.OfficialDocument.Remote.GetApprovalRules(_obj.LeadingDocument);
        else
        {
          // Для доп. соглашения получаем правила напрямую, чтобы не уйти в рекурсивные проверки ведущих и их регламентов.
          leadingDocumentRules = Docflow.PublicFunctions.ApprovalRuleBase.Remote.GetAvailableRulesByDocument(_obj.LeadingDocument)
            .OrderByDescending(r => r.Priority)
            .ToList();
        }
        
        // #57673 Регламенты для доп. соглашений приоритетнее договорных.
        // Если регламент подходит для обоих видов, то явное указание вида приоритетнее неявного.
        var intersect = rules.Intersect(leadingDocumentRules).ToList();
        rules = rules.Except(leadingDocumentRules).ToList();
        
        var supAgreementKinds = Docflow.PublicFunctions.DocumentKind.GetAvailableDocumentKinds(typeof(ISupAgreement)).ToList();
        rules.AddRange(intersect.Where(r => r.DocumentKinds.Any(k => supAgreementKinds.Contains(k.DocumentKind))));
        rules = rules.OrderByDescending(r => r.Priority).ToList();
        
        leadingDocumentRules = leadingDocumentRules.Except(rules).ToList();
        rules.AddRange(leadingDocumentRules.Select(r => ContractsApprovalRules.As(r)).Where(r => r != null).OrderByDescending(r => r.Priority).ToList());
      }
      return rules;
    }
    
    /// <summary>
    /// Получить правила согласования по умолчанию для доп. соглашения.
    /// </summary>
    /// <returns>Правила согласования по умолчанию.</returns>
    /// <remarks>Если подходящих правил нет или их несколько, то вернется null.</remarks>
    [Remote, Public]
    public override IApprovalRuleBase GetDefaultApprovalRule()
    {
      var availableApprovalRules = this.GetApprovalRules();
      if (availableApprovalRules.Count() == 1)
        return availableApprovalRules.First();

      if (availableApprovalRules.Any())
      {
        var currentSupAgreementKind = _obj.DocumentKind;
        var supAgreementRules = availableApprovalRules.Where(r => r.DocumentKinds.Any(k => Equals(k.DocumentKind, currentSupAgreementKind)));
        var approvalRules = supAgreementRules.Any() ? supAgreementRules : availableApprovalRules;
        var maxPriority = approvalRules.Select(a => a.Priority).OrderByDescending(a => a).FirstOrDefault();
        var defaultApprovalRules = approvalRules.Where(a => Equals(a.Priority, maxPriority));
        if (defaultApprovalRules.Count() == 1)
          return defaultApprovalRules.First();
      }
      return null;
    }
    
    /// <summary>
    /// Сводка по документу.
    /// </summary>
    /// <returns>Сводка.</returns>
    public override StateView GetDocumentSummary()
    {
      var documentSummary = StateView.Create();
      var block = documentSummary.AddBlock();
      
      // Краткое имя документа.
      var documentName = _obj.DocumentKind.Name;
      if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
        documentName += OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
      
      if (_obj.RegistrationDate != null)
        documentName += OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
      
      block.AddLabel(documentName);
      
      // Типовое/Не типовое.
      var isStandardLabel = _obj.IsStandard.Value ? SupAgreements.Resources.IsStandartSupAgreement : SupAgreements.Resources.IsNotStandartSupAgreement;
      block.AddLabel(string.Format("({0})", isStandardLabel));
      block.AddLineBreak();
      block.AddEmptyLine();
      
      // НОР.
      block.AddLabel(string.Format("{0}: ", _obj.Info.Properties.BusinessUnit.LocalizedName));
      if (_obj.BusinessUnit != null)
        block.AddLabel(Hyperlinks.Get(_obj.BusinessUnit));
      else
        block.AddLabel("-");
      
      block.AddLineBreak();
      
      // Контрагент.
      block.AddLabel(string.Format("{0}: ", _obj.Info.Properties.Counterparty.LocalizedName));
      if (_obj.Counterparty != null)
      {
        block.AddLabel(Hyperlinks.Get(_obj.Counterparty));
        if (_obj.Counterparty.Nonresident == true)
          block.AddLabel(string.Format("({0})", _obj.Counterparty.Info.Properties.Nonresident.LocalizedName).ToLower());
      }
      else
      {
        block.AddLabel("-");
      }
      
      block.AddLineBreak();
      
      // Содержание.
      var subject = !string.IsNullOrEmpty(_obj.Subject) ? _obj.Subject : "-";
      block.AddLabel(string.Format("{0}: {1}", _obj.Info.Properties.Subject.LocalizedName, subject));
      block.AddLineBreak();
      
      // Сумма.
      var amount = this.GetTotalAmountDocumentSummary(_obj.TotalAmount);
      var amountText = string.Format("{0}: {1}", _obj.Info.Properties.TotalAmount.LocalizedName, amount);
      block.AddLabel(amountText);
      block.AddLineBreak();

      // Валюта.
      var currencyText = string.Format("{0}: {1}", _obj.Info.Properties.Currency.LocalizedName, _obj.Currency);
      block.AddLabel(currencyText);
      block.AddLineBreak();
      
      // Срок действия.
      var validity = "-";
      var validFrom = _obj.ValidFrom.HasValue
        ? string.Format("{0} {1} ", ContractBases.Resources.From, _obj.ValidFrom.Value.ToShortDateString())
        : string.Empty;
      var validTill = _obj.ValidTill.HasValue
        ? string.Format("{0} {1}", ContractBases.Resources.Till, _obj.ValidTill.Value.ToShortDateString())
        : string.Empty;
      if (!string.IsNullOrEmpty(validFrom) || !string.IsNullOrEmpty(validTill))
        validity = string.Format("{0}{1}", validFrom, validTill);
      
      var validityText = string.Format("{0}: {1}", ContractBases.Resources.Validity, validity);
      block.AddLabel(validityText);
      block.AddEmptyLine();
      
      // Примечание.
      var note = !string.IsNullOrEmpty(_obj.Note) ? _obj.Note : "-";
      block.AddLabel(string.Format("{0}: {1}", _obj.Info.Properties.Note.LocalizedName, note));
      
      return documentSummary;
    }
    
    /// <summary>
    /// Изменить статус документа на "В разработке".
    /// </summary>
    public override void SetLifeCycleStateDraft()
    {
      base.SetLifeCycleStateDraft();
      
      if (_obj.LifeCycleState == Sungero.Contracts.SupAgreement.LifeCycleState.Terminated)
      {
        Logger.DebugFormat("UpdateLifeCycleState: Document {0} changed LifeCycleState to 'Draft'.", _obj.Id);
        _obj.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Draft;
      }
    }
  }
}