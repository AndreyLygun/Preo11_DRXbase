using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Intelligence.AIManagersAssistant;

namespace Sungero.Intelligence
{

  partial class AIManagersAssistantClassifiersSharedCollectionHandlers
  {

    public virtual void ClassifiersAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.LowerClassificationLimit = Constants.AIManagersAssistant.LowerClassificationLimit;
      _added.ClassifierType = AIManagersAssistantClassifiers.ClassifierType.Assignee;
      _added.IsModelActive = AIManagersAssistantClassifiers.IsModelActive.No;
    }
  }

  partial class AIManagersAssistantSharedHandlers
  {

  }
}