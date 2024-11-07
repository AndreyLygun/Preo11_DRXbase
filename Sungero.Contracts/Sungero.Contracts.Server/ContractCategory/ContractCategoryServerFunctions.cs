using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractCategory;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts.Server
{
  partial class ContractCategoryFunctions
  {
    /// <summary>
    /// Получить категории с незаполненным кодом.
    /// </summary>
    /// <returns>Категории с незаполненным кодом.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IContractCategory> GetContractCategoriesWithNullCode()
    {
      return ContractCategories.GetAll().Where(c => c.Status == Status.Active && c.Code == null);
    }
  }
}