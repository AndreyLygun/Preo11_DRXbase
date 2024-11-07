using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractCondition;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;

namespace Sungero.Contracts.Shared
{
  partial class ContractConditionFunctions
  {
    /// <summary>
    /// Получить словарь поддерживаемых типов условий.
    /// </summary>
    /// <returns>
    /// Словарь.
    /// Ключ - GUID типа документа.
    /// Значение - список поддерживаемых условий.
    /// </returns>
    public override System.Collections.Generic.Dictionary<string, List<Enumeration?>> GetSupportedConditions()
    {
      var baseConditions = base.GetSupportedConditions();
      
      var contractualBases = Docflow.PublicFunctions.DocumentKind.GetDocumentGuids(typeof(IContractualDocumentBase));
      var accountings = Docflow.PublicFunctions.DocumentKind.GetDocumentGuids(typeof(IAccountingDocumentBase));
      var contractuals = Docflow.PublicFunctions.DocumentKind.GetDocumentGuids(typeof(IContractualDocument));
      var contracts = Docflow.PublicFunctions.DocumentKind.GetDocumentGuids(typeof(IContractBase));
      
      // Сумма, валюта, нерезидент - для всех договорных(базовых) и финансовых типов.
      foreach (var typeGuid in contractualBases.Concat(accountings))
      {
        baseConditions[typeGuid].AddRange(new List<Enumeration?> { ConditionType.AmountIsMore,
                                            ConditionType.Currency,
                                            ConditionType.Nonresident });
      }
      
      // Условие типовой - только для договорных типов.
      foreach (var typeGuid in contractuals)
        baseConditions[typeGuid].Add(ConditionType.Standard);

      // Условие рамочный - только для договоров.
      foreach (var typeGuid in contracts)
        baseConditions[typeGuid].Add(ConditionType.IsFrameContract);

      return baseConditions;
    }
    
    /// <summary>
    /// Проверить условие.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>True, если условие выполняется, и false - если не выполняется.</returns>
    public override Docflow.Structures.ConditionBase.ConditionResult CheckCondition(IOfficialDocument document, IApprovalTask task)
    {
      if (_obj.ConditionType == ConditionType.Standard)
        return this.CheckStandard(document, task);
      
      if (_obj.ConditionType == ConditionType.IsFrameContract)
        return this.CheckIsFrameworkContract(document, task);

      return base.CheckCondition(document, task);
    }
    
    /// <summary>
    /// Проверить условие "Типовой".
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>True, если документ типовой.</returns>
    public virtual Docflow.Structures.ConditionBase.ConditionResult CheckStandard(IOfficialDocument document, IApprovalTask task)
    {
      if (Sungero.Contracts.ContractualDocuments.Is(document))
      {
        var contractualDocument = Sungero.Contracts.ContractualDocuments.As(document);
        
        if (!contractualDocument.IsStandard.HasValue)
          return Docflow.Structures.ConditionBase.ConditionResult.Create(null, ContractConditions.Resources.TheStandardIsNotFilledInContractCard);
        
        return Docflow.Structures.ConditionBase.ConditionResult.Create(contractualDocument.IsStandard.Value, string.Empty);
      }

      return Docflow.Structures.ConditionBase.ConditionResult.Create(null, ContractConditions.Resources.SelectApprovalRuleWithoutStandartCondition);
    }
    
    /// <summary>
    /// Проверить условие "Рамочный".
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>True, если рамочный договор.</returns>
    public virtual Docflow.Structures.ConditionBase.ConditionResult CheckIsFrameworkContract(IOfficialDocument document, IApprovalTask task)
    {
      if (Sungero.Contracts.ContractBases.Is(document))
      {
        var contract = Sungero.Contracts.ContractBases.As(document);
        if (contract.IsFrameworkContract.HasValue)
          return Docflow.Structures.ConditionBase.ConditionResult.Create(contract.IsFrameworkContract.Value, string.Empty);
        
        return Docflow.Structures.ConditionBase.ConditionResult.Create(null, ContractConditions.Resources.IsFrameworkContractPropertyIsNotFilled);
      }
      
      return Docflow.Structures.ConditionBase.ConditionResult.Create(null, ContractConditions.Resources.SelectApprovalRuleWithoutIsFrameworkContractCondition);
    }
    
    /// <summary>
    /// Проверить сумму документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Результат проверки условия по сумме.</returns>
    /// <remarks>Общая сумма в не рамочном договоре должна быть заполнена.</remarks>
    public override Sungero.Docflow.Structures.ConditionBase.ConditionResult CheckAmountIsMore(IOfficialDocument document, IApprovalTask task)
    {
      if (ContractBases.Is(document))
      {
        var contract = ContractBases.As(document);
        if (!contract.TotalAmount.HasValue && contract.IsFrameworkContract != true)
          return Docflow.Structures.ConditionBase.ConditionResult.Create(null, ConditionBases.Resources.FillTotalAmountInContractCard);
      }
      return base.CheckAmountIsMore(document, task);
    }
    
    /// <summary>
    /// Получить текст уведомления о том, что условие нельзя использовать с некоторыми видами документов.
    /// </summary>
    /// <returns>Текст.</returns>
    /// <remarks>Условие "Типовой" применимо только к договорным документам.
    /// Условие "Рамочный" применимо только к договорам.</remarks>
    public override string GetNotUsableConditionHint()
    {
      if (_obj.ConditionType == ConditionType.Standard)
      {
        var localizedConditionType = _obj.Info.Properties.ConditionType.GetLocalizedValue(_obj.ConditionType);
        return ContractConditions.Resources.ConditionAvailableOnlyForContractualDocumentsFormat(localizedConditionType);
      }
      
      if (_obj.ConditionType == ConditionType.IsFrameContract)
      {
        var localizedConditionType = _obj.Info.Properties.ConditionType.GetLocalizedValue(_obj.ConditionType);
        return ContractConditions.Resources.ConditionAvailableOnlyForContractsFormat(localizedConditionType);
      }
      
      return base.GetNotUsableConditionHint();
    }
  }
}