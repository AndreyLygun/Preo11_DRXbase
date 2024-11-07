using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Topic;

namespace Sungero.Docflow
{
  partial class TopicServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var hasSubtopics = Docflow.Functions.Topic.HasSubtopics(_obj);
      if (_obj.Parent != null && hasSubtopics)
        e.AddError(Topics.Resources.TopicAlreadyUsedAsLeading);
    }
  }

  partial class TopicParentPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ParentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = query.Where(x => x.Parent == null && x.Id != _obj.Id);
      return query;
    }
  }

}