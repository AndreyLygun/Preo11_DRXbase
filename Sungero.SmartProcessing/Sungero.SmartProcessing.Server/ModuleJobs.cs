using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using Sungero.SmartProcessing;
using Sungero.Workflow;

namespace Sungero.SmartProcessing.Server
{
  public class ModuleJobs
  {
    
    /// <summary>
    /// Запуск обучения классификатора первых страниц.
    /// </summary>
    public virtual void StartFirstPageClassifierTraining()
    {
      // Обучение классификатора возможно при наличии лицензии на модуль "Интеллектуальные функции".
      if (!Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Sungero.Commons.PublicConstants.Module.IntelligenceGuid))
      {
        Logger.Debug("ClassifierTraining. StartFirstPageClassifierTrainingJob. Module license \"Intelligence\" not found.");
        return;
      }
      
      // Старт обучения классификатора первых страниц.
      Functions.Module.StartClassifierTraining(Sungero.Commons.ClassifierTrainingSession.ClassifierType.FirstPage);
    }

    /// <summary>
    /// Запуск обучения классификатора по типам документов.
    /// </summary>
    public virtual void StartClassifierTraining()
    {
      // Обучение классификатора возможно при наличии лицензии на модуль "Интеллектуальные функции".
      if (!Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Sungero.Commons.PublicConstants.Module.IntelligenceGuid))
      {
        Logger.Debug("ClassifierTraining. StartClassifierTrainingJob. Module license \"Intelligence\" not found.");
        return;
      }
      
      // Старт обучения классификатора по типам документов.
      Functions.Module.StartClassifierTraining(Sungero.Commons.ClassifierTrainingSession.ClassifierType.DocType);
    }

    /// <summary>
    /// Фоновый процесс для удаления пакетов бинарных образов документов, которые отправлены на верификацию.
    /// </summary>
    public virtual void DeleteBlobPackages()
    {
      // Удаление BlobPackage со статусом Processed.
      var processedBlobPackages = BlobPackages.GetAll().Where(x => x.ProcessState == SmartProcessing.BlobPackage.ProcessState.Processed);
      foreach (var blobPackage in processedBlobPackages)
      {
        var blobs = blobPackage.Blobs.Select(x => x.Blob);
        var mailBodyBlob = blobPackage.MailBodyBlob;
        BlobPackages.Delete(blobPackage);
        foreach (var blob in blobs)
          Blobs.Delete(blob);
        
        if (mailBodyBlob != null)
          Blobs.Delete(mailBodyBlob);
      }
    }

  }
}