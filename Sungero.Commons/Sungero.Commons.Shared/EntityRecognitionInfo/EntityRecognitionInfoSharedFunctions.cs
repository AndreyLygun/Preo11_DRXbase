using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.EntityRecognitionInfo;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons.Shared
{
  partial class EntityRecognitionInfoFunctions
  {
    /// <summary>
    /// Заполнить статус обучения классификатора.
    /// </summary>
    /// <param name="trainingStatus">Статус обучения.</param>
    /// <param name="classifierType">Тип классификатора.</param>
    public virtual void SetClassifierTrainingStatus(Enumeration? trainingStatus, Enumeration? classifierType)
    {
      if (classifierType == Commons.ClassifierTrainingSession.ClassifierType.DocType)
        Functions.EntityRecognitionInfo.SetDocTypeClassifierTrainingStatus(_obj, trainingStatus);

      if (classifierType == Commons.ClassifierTrainingSession.ClassifierType.FirstPage)
        Functions.EntityRecognitionInfo.SetFirstPageClassifierTrainingStatus(_obj, trainingStatus);
    }
    
    /// <summary>
    /// Заполнить статус обучения классификатора по типам документов.
    /// </summary>
    /// <param name="trainingStatus">Статус обучения.</param>
    public virtual void SetDocTypeClassifierTrainingStatus(Enumeration? trainingStatus)
    {
      if (_obj.DocTypeClassifierTrainingStatus != trainingStatus)
        _obj.DocTypeClassifierTrainingStatus = trainingStatus;
    }
    
    /// <summary>
    /// Заполнить статус обучения классификатора первых страниц.
    /// </summary>
    /// <param name="trainingStatus">Статус обучения.</param>
    public virtual void SetFirstPageClassifierTrainingStatus(Enumeration? trainingStatus)
    {
      if (_obj.FirstPageClassifierTrainingStatus != trainingStatus)
        _obj.FirstPageClassifierTrainingStatus = trainingStatus;
    }
    
    /// <summary>
    /// Заполнить сессию обучения.
    /// </summary>
    /// <param name="trainingSession">Сессия обучения.</param>
    public virtual void SetClassifierTrainingSession(IClassifierTrainingSession trainingSession)
    {
      var classifierType = trainingSession.ClassifierType;
      
      if (classifierType == Commons.ClassifierTrainingSession.ClassifierType.DocType &&
          !Equals(_obj.DocTypeClassifierTrainingSession, trainingSession))
        _obj.DocTypeClassifierTrainingSession = trainingSession;
      
      if (classifierType == Commons.ClassifierTrainingSession.ClassifierType.FirstPage &&
          !Equals(_obj.FirstPageClassifierTrainingSession, trainingSession))
        _obj.FirstPageClassifierTrainingSession = trainingSession;
    }

    /// <summary>
    /// Сбросить сессию обучения.
    /// </summary>
    /// <param name="classifierType">Тип классификатора.</param>
    public virtual void ResetClassifierTrainingSession(Enumeration? classifierType)
    {
      if (classifierType == Commons.ClassifierTrainingSession.ClassifierType.DocType && _obj.DocTypeClassifierTrainingSession != null)
        _obj.DocTypeClassifierTrainingSession = ClassifierTrainingSessions.Null;
      
      if (classifierType == Commons.ClassifierTrainingSession.ClassifierType.FirstPage && _obj.FirstPageClassifierTrainingSession != null)
        _obj.FirstPageClassifierTrainingSession = ClassifierTrainingSessions.Null;
    }
  }
}