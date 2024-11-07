using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Contracts.ContractCategory;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{
  partial class ContractCategoryServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Проверить код на пробелы, если свойство изменено.
      if (!string.IsNullOrEmpty(_obj.Code) && _obj.State.Properties.Code.IsChanged)
      {
        // При изменении кода e.AddError сбрасывается.
        _obj.Code = _obj.Code.Trim();
        
        if (Regex.IsMatch(_obj.Code, @"\s"))
          e.AddError(_obj.Info.Properties.Code, Docflow.Resources.NoSpacesInCode);
      }
    }
  }

  partial class ContractCategoryDocumentKindsDocumentKindPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> DocumentKindsDocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var availableDocumentKinds = Functions.ContractCategory.GetAllowedDocumentKinds();

      query = base.DocumentKindsDocumentKindFiltering(query, e);
      return query.Where(a => availableDocumentKinds.Contains(a));
    }
  }

}