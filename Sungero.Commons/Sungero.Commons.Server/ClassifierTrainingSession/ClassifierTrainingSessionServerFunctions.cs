using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.ClassifierTrainingSession;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons.Server
{
  partial class ClassifierTrainingSessionFunctions
  {
    /// <summary>
    /// Установить свойства сессии после обучения.
    /// </summary>
    /// <param name="trainingSessionStatus">Статус сессии дообучения.</param>
    /// <param name="fisherMeasure">F1-мера.</param>
    /// <param name="modelId">Id модели.</param>
    [Public]
    public virtual void SetSessionPropertiesAfterTraining(Enumeration trainingSessionStatus,
                                                          double? fisherMeasure,
                                                          int? modelId)
    {
      if (!this.IsLockedByOther())
      {
        if (fisherMeasure.HasValue)
          _obj.FMeasure = fisherMeasure.ToString();
        
        if (modelId.HasValue && trainingSessionStatus == Commons.ClassifierTrainingSession.TrainingStatus.Completed)
          _obj.NewModelId = modelId;
        
        _obj.TrainingStatus = trainingSessionStatus;
        if (_obj.State.IsChanged)
          _obj.Save();
      }
    }
    
    /// <summary>
    /// Установить свойства сессии дообучения.
    /// </summary>
    /// <param name="trainingSessionStatus">Статус сессии дообучения.</param>
    [Public]
    public virtual void SetSessionStatus(Enumeration trainingSessionStatus)
    {
      if (!this.IsLockedByOther())
      {
        _obj.TrainingStatus = trainingSessionStatus;
        if (_obj.State.IsChanged)
          _obj.Save();
      }
    }
    
    /// <summary>
    /// Проверить факт блокировки другим пользователем.
    /// </summary>
    /// <returns>Заблокировано другим пользователем.</returns>
    /// <remarks>Факт блокировки логируется.</remarks>
    private bool IsLockedByOther()
    {
      var lockInfo = Locks.GetLockInfo(_obj);
      if (lockInfo.IsLockedByOther)
      {
        Logger.DebugFormat("ClassifierTraining. IsLockedByOther. Classifier training session is locked by \"{0}\", sessionId={1}", lockInfo.OwnerName, _obj.Id);
        return true;
      }
      return false;
    }

  }
}