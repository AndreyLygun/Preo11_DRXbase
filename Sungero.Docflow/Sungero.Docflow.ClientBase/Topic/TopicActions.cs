using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Topic;

namespace Sungero.Docflow.Client
{
  partial class TopicActions
  {
    public virtual void SearchDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Topic.Remote.GetDocumentsWithTopic(_obj).Show(_obj.Name);
    }

    public virtual bool CanSearchDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

  }

}