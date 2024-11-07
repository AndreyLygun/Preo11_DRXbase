using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Projects.Server
{

  partial class ProjectDocumentsFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> ProjectDocumentsPreFiltering(IQueryable<Sungero.Docflow.IOfficialDocument> query)
    {
      if (Functions.Module.UsePrefilterProjectDocuments(_filter))
      {
        query = Functions.Module.ProjectDocumentsApplyStrongFilter(query, _filter);
        query = Functions.Module.ProjectDocumentsApplyOrdinaryFilter(query, _filter);
      }
      
      return query;
    }

    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> ProjectDocumentsDataQuery(IQueryable<Sungero.Docflow.IOfficialDocument> query)
    {
      if (!Functions.Module.UsePrefilterProjectDocuments(_filter))
        query = Functions.Module.ProjectDocumentsApplyOrdinaryFilter(query, _filter);
      
      query = Functions.Module.ProjectDocumentsApplyWeakFilter(query, _filter);
      
      return query;
    }
    
    public virtual IQueryable<Sungero.Docflow.IDocumentKind> ProjectDocumentsDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      query = query.Where(dk => dk.Status == CoreEntities.DatabookEntry.Status.Active && dk.ProjectsAccounting == true);
      return query;
    }
  }
  
  partial class ProjectsHandlers
  {
  }
}
