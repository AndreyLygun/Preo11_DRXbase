using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccessRightsRule;

namespace Sungero.Docflow.Shared
{
  partial class AccessRightsRuleFunctions
  {
    /// <summary>
    /// Получить доступные категории договоров.
    /// </summary>
    /// <returns>Список категорий договоров.</returns>
    public virtual List<IDocumentGroupBase> GetDocumentGroups()
    {
      var kinds = _obj.DocumentKinds.Select(k => k.DocumentKind).ToList();
      var contractKinds = Functions.DocumentKind.GetAvailableDocumentKinds(typeof(Contracts.IContractBase)).ToList();
      if (kinds.Any() && kinds.All(k => contractKinds.Contains(k)))
        return Contracts.PublicFunctions.ContractCategory.GetFilteredContractCategoris(kinds);
      return new List<IDocumentGroupBase>();
    }
    
    /// <summary>
    /// Определить изменились ли критерии или участники правила.
    /// </summary>
    /// <returns>True - изменились, False - иначе.</returns>
    public virtual bool CriteriaOrMembersChanged()
    {
      if (!_obj.State.IsChanged)
        return false;
      
      var significantPropsCount = _obj.State.Properties.ChangedCount;
      
      if (_obj.State.Properties.Name.IsChanged)
        significantPropsCount--;
      if (_obj.State.Properties.Note.IsChanged)
        significantPropsCount--;
      if (_obj.State.Properties.BulkProcessingState.IsChanged)
        significantPropsCount--;
      if (_obj.State.Properties.Members.IsChanged &&
          _obj.State.Properties.Members.Added.Count() == 0 &&
          _obj.State.Properties.Members.Changed.Count() == 0 &&
          _obj.State.Properties.Members.Deleted.Count() == 0)
        significantPropsCount--;
      
      return significantPropsCount > 0;
    }
    
    /// <summary>
    /// Запланировать выдачу прав по правилу.
    /// </summary>
    public virtual void PlanBulkProcessing()
    {
      _obj.BulkProcessingState = Docflow.AccessRightsRule.BulkProcessingState.Planned;
      _obj.LaunchId = Guid.NewGuid().ToString();
    }
    
    /// <summary>
    /// Прекратить выдачу прав по правилу.
    /// </summary>
    public virtual void CancelBulkProcessing()
    {
      _obj.BulkProcessingState = null;
      _obj.LaunchId = string.Empty;
    }
    
  }
}