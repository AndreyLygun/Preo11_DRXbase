using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccessRightsRule;

namespace Sungero.Docflow.Server
{
  partial class AccessRightsRuleFunctions
  {
    /// <summary>
    /// Получить действующие правила назначения прав по виду документа.
    /// </summary>
    /// <param name="documentKind">Вид документа.</param>
    /// <returns>Правила назначения прав.</returns>
    [Public]
    public static IQueryable<IAccessRightsRule> GetAccessRightsRulesByDocumentKind(IDocumentKind documentKind)
    {
      var rules = AccessRightsRules.GetAll();
      return AccessRightsRules.GetAll(r => r.Status == Docflow.AccessRightsRule.Status.Active)
        .Where(r => r.DocumentKinds.Any(k => k.DocumentKind.Id == documentKind.Id));
    }
    
    /// <summary>
    /// Фильтр для категорий договоров.
    /// </summary>
    /// <param name="query">Ленивый запрос документов.</param>
    /// <returns>Относительно ленивый запрос с категориями.</returns>
    public virtual List<long> FilterDocumentsByGroups(IQueryable<IOfficialDocument> query)
    {
      var documentIds = new List<long>();
      foreach (var document in query)
      {
        var documentGroup = Functions.OfficialDocument.GetDocumentGroup(document);
        if (_obj.DocumentGroups.Any(k => Equals(k.DocumentGroup, documentGroup)))
          documentIds.Add(document.Id);
      }
      return documentIds;
    }
    
    /// <summary>
    /// Получить документы по правилу.
    /// </summary>
    /// <returns>Документы по правилу.</returns>
    public virtual List<long> GetDocumentsByRule()
    {
      var documentKinds = _obj.DocumentKinds.Select(t => t.DocumentKind).ToList();
      var businessUnits = _obj.BusinessUnits.Select(t => t.BusinessUnit).ToList();
      var departments = _obj.Departments.Select(t => t.Department).ToList();
      
      var documents = OfficialDocuments.GetAll()
        .Where(d => !documentKinds.Any() || documentKinds.Contains(d.DocumentKind))
        .Where(d => !businessUnits.Any() || businessUnits.Contains(d.BusinessUnit))
        .Where(d => !departments.Any() || departments.Contains(d.Department));
      
      var documentIds = new List<long>();
      if (_obj.DocumentGroups.Any())
        documentIds = this.FilterDocumentsByGroups(documents);
      else
        documentIds = documents.Select(d => d.Id).ToList();
      
      if (documentIds.Any() && _obj.GrantRightsOnLeadingDocument.GetValueOrDefault())
      {
        var childDocuments = Functions.Module.GetAllChildDocuments(documentIds);
        documentIds.AddRange(childDocuments);
      }
      return documentIds;
    }
  }
}