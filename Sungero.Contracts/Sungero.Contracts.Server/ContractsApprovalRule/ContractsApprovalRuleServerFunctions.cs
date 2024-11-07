using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Contracts.ContractsApprovalRule;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;

namespace Sungero.Contracts.Server
{
  partial class ContractsApprovalRuleFunctions
  {
    #region Проверка возможности существования маршрутов правила
    
    /// <summary>
    /// Проверка возможности существования маршрута правила.
    /// </summary>
    /// <param name="route">Маршрут.</param>
    /// <param name="ruleConditions">Условие.</param>
    /// <param name="conditionStep">Этап.</param>
    /// <returns>Возможность существования.</returns>
    public override bool CheckRoutePossibility(List<Docflow.Structures.ApprovalRuleBase.RouteStep> route,
                                               List<Docflow.Structures.ApprovalRuleBase.ConditionRouteStep> ruleConditions,
                                               Docflow.Structures.ApprovalRuleBase.RouteStep conditionStep)
    {
      var possibleStage = base.CheckRoutePossibility(route, ruleConditions, conditionStep);
      var conditionType = _obj.Conditions.First(x => x.Number == conditionStep.StepNumber).Condition.ConditionType;
      
      // Проверка условий по типовому договору.
      if (conditionType == Sungero.Contracts.ContractCondition.ConditionType.Standard)
      {
        var standardConditions = this.GetStandardConditionsInRoute(route).Where(x => x.StepNumber != conditionStep.StepNumber).ToList();
        possibleStage = this.CheckStandardConditions(standardConditions, conditionStep);
      }
      
      // Проверка условий по рамочному договору.
      if (conditionType == Sungero.Contracts.ContractCondition.ConditionType.IsFrameContract)
      {
        var frameworkConditions = this.GetIsFrameworkContractConditionsInRoute(route).Where(x => x.StepNumber != conditionStep.StepNumber).ToList();
        possibleStage = this.CheckIsFrameworkContractConditions(frameworkConditions, conditionStep);
      }

      return possibleStage;
    }

    /// <summary>
    /// Проверить возможность существования данного маршрута с условиями по типовому договору.
    /// </summary>
    /// <param name="allConditions">Все условия в данном маршруте.</param>
    /// <param name="condition">Текущее условие.</param>
    /// <returns>Возможность существования данного маршрута.</returns>
    public virtual bool CheckStandardConditions(List<Docflow.Structures.ApprovalRuleBase.RouteStep> allConditions,
                                                Docflow.Structures.ApprovalRuleBase.RouteStep condition)
    {
      foreach (var previousCondition in allConditions.TakeWhile(x => !Equals(x, condition)))
        return previousCondition.Branch == condition.Branch;
      
      return true;
    }

    /// <summary>
    /// Проверить возможность существования данного маршрута с условиями по рамочному договору.
    /// </summary>
    /// <param name="allConditions">Все условия в данном маршруте.</param>
    /// <param name="condition">Текущее условие.</param>
    /// <returns>Возможность существования данного маршрута.</returns>
    public virtual bool CheckIsFrameworkContractConditions(List<Docflow.Structures.ApprovalRuleBase.RouteStep> allConditions,
                                                           Docflow.Structures.ApprovalRuleBase.RouteStep condition)
    {
      foreach (var previousCondition in allConditions.TakeWhile(x => !Equals(x, condition)))
        return previousCondition.Branch == condition.Branch;
      
      return true;
    }
    
    /// <summary>
    /// Получить все условия по стандартному договору в данном маршруте.
    /// </summary>
    /// <param name="route">Маршрут.</param>
    /// <returns>Условия.</returns>
    public virtual List<Docflow.Structures.ApprovalRuleBase.RouteStep> GetStandardConditionsInRoute(List<Docflow.Structures.ApprovalRuleBase.RouteStep> route)
    {
      return route.Where(e => _obj.Conditions.Any(x => Equals(x.Number, e.StepNumber) && x.Condition.ConditionType ==
                                                  Sungero.Contracts.ContractCondition.ConditionType.Standard)).ToList();
    }
    
    /// <summary>
    /// Получить все условия по рамочному договору в данном маршруте.
    /// </summary>
    /// <param name="route">Маршрут.</param>
    /// <returns>Условия.</returns>
    public virtual List<Docflow.Structures.ApprovalRuleBase.RouteStep> GetIsFrameworkContractConditionsInRoute(List<Docflow.Structures.ApprovalRuleBase.RouteStep> route)
    {
      return route.Where(e => _obj.Conditions.Any(x => Equals(x.Number, e.StepNumber) && x.Condition.ConditionType ==
                                                  Sungero.Contracts.ContractCondition.ConditionType.IsFrameContract)).ToList();
    }

    #endregion
    
    public override List<IApprovalRoleBase> GetSupportedApprovalRolesForRework()
    {
      return ApprovalRoleBases.GetAll().Where(r => r.Type != Docflow.ApprovalRole.Type.Initiator &&
                                              r.Type != Docflow.ApprovalRole.Type.Approvers &&
                                              r.Type != Docflow.ApprovalRole.Type.Addressee &&
                                              r.Type != Docflow.ApprovalRole.Type.AddrAssistant &&
                                              r.Type != Docflow.ApprovalRole.Type.Signatory &&
                                              r.Type != Docflow.ApprovalRole.Type.SignAssistant)
        .ToList();
    }
  }
}