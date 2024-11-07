using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Intelligence.AIManagersAssistant;

namespace Sungero.Intelligence.Client
{
  partial class AIManagersAssistantAnyChildEntityCollectionActions
  {
    public override void DeleteChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.DeleteChildEntity(e);
    }

    public override bool CanDeleteChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return Functions.AIManagersAssistant.CollectionIsAIAssistantClassifiers(_all, e.RootEntity)
        ? false
        : base.CanDeleteChildEntity(e);
    }

  }

  partial class AIManagersAssistantAnyChildEntityActions
  {
    public override void CopyChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.CopyChildEntity(e);
    }

    public override bool CanCopyChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return Functions.AIManagersAssistant.CollectionIsAIAssistantClassifiers(_all, e.RootEntity)
        ? false
        : base.CanCopyChildEntity(e);
    }

    public override void AddChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.AddChildEntity(e);
    }

    public override bool CanAddChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return Functions.AIManagersAssistant.CollectionIsAIAssistantClassifiers(_all, e.RootEntity)
        ? false
        : base.CanAddChildEntity(e);
    }

  }

  partial class AIManagersAssistantActions
  {
    public virtual void TrainClassifierModel(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Проверить возможность автосохранения карточки (используется после создания классификатора и перед стартом подготовки данных).
      if (!e.Validate())
        return;
      
      // Если подготовка данных запущена, закрыть карточку, чтобы не перезапускать АО из-за ее блокировки.
      if (Functions.AIManagersAssistant.ShowClassifierTrainingDialog(_obj))
        e.CloseFormAfterAction = true;
    }

    public virtual bool CanTrainClassifierModel(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Users.Current.IncludedIn(Roles.Administrators) && !_obj.State.IsInserted && _obj.Status == Status.Active;
    }

    public virtual void UnpublishClassifierModel(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      bool isConfirmed = Dialogs.CreateConfirmDialog(AIManagersAssistants.Resources.ResetClassifierResults, AIManagersAssistants.Resources.ResetClassifierResultsDescription).Show();
      if (!isConfirmed)
        return;

      var errorMessage = PublicFunctions.AIManagersAssistant.Remote.ResetClassifierTraining(_obj, Intelligence.AIManagersAssistantClassifiers.ClassifierType.Assignee);
      if (string.IsNullOrEmpty(errorMessage))
      {
        var classifier = _obj.Classifiers.First(x => x.ClassifierType == Intelligence.AIManagersAssistantClassifiers.ClassifierType.Assignee);
        Dialogs.NotifyMessage(string.Format(SmartProcessing.Resources.UnpublishModelSuccess, classifier.ClassifierId.Value, classifier.ClassifierName));
      }
      else
      {
        Dialogs.NotifyMessage(errorMessage);
      }
    }

    public virtual bool CanUnpublishClassifierModel(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Users.Current.IncludedIn(Roles.Administrators) &&
        !_obj.State.IsInserted &&
        _obj.Status == Status.Active &&
        _obj.Classifiers.Any(x => x.ClassifierId.HasValue &&
                             x.ClassifierType == Intelligence.AIManagersAssistantClassifiers.ClassifierType.Assignee &&
                             x.ModelId.HasValue);
    }

  }

}