using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Topic;

namespace Sungero.Docflow.Server
{
  partial class TopicFunctions
  {
    /// <summary>
    /// Проверить, есть ли подрубрики у рубрики.
    /// </summary>
    /// <returns>True - подрубрики есть, иначе - false.</returns>
    [Public]
    public virtual bool HasSubtopics()
    {
      return Topics.GetAll(x => Equals(x.Parent, _obj)).Any();
    }
    
    /// <summary>
    /// Получить документы по рубрике.
    /// </summary>
    /// <returns>Документы.</returns>
    [Remote(IsPure = true)]
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> GetDocumentsWithTopic()
    {
      if (_obj.Parent == null)
        return Sungero.Docflow.OfficialDocuments.GetAll(x => Equals(x.Topic, _obj));
      
      return Sungero.Docflow.OfficialDocuments.GetAll(x => Equals(x.Subtopic, _obj));
    }
  }
}