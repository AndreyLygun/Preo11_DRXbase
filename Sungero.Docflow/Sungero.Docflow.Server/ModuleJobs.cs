using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using DocumentIndexQueueItemStatus = Sungero.Docflow.DocumentFullTextSearchQueueItem.ProcessingStatus;

namespace Sungero.Docflow.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// Индексация документов для полнотекстового поиска.
    /// </summary>
    public virtual void IndexDocumentsForFullTextSearch()
    {
      Logger.Debug("IndexDocumentsForFullTextSearch. Start Job.");
      
      if (!Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Sungero.Commons.PublicConstants.Module.IntelligenceGuid))
      {
        Logger.Debug("IndexDocumentsForFullTextSearch. Module license \"Intelligence\" not found.");
        return;
      }
      
      var arioConnector = Functions.Module.GetArioConnector();
      if (arioConnector == null)
        return;
      if (!Functions.Module.IsArioEnabled(arioConnector))
      {
        Logger.Debug("IndexDocumentsForFullTextSearch. Ario is not available.");
        return;
      }
      
      if (!Functions.Module.IsDocumentsFullTextSearchEnabled())
      {
        Logger.Debug("IndexDocumentsForFullTextSearch. Documents full text search is not available.");
        return;
      }
      
      if (!Commons.PublicFunctions.Module.IsElasticsearchEnabled())
      {
        Logger.Debug("IndexDocumentsForFullTextSearch. Elasticsearch is not available.");
        return;
      }
      
      var currentRunDate = Calendar.Now;
      var lastRunDate = Functions.Module.GetLastIndexingRunDate();

      var documentsIdsFromLastRun = Functions.Module.GetDocumentsForFullTextSearch(lastRunDate);
      var documentsFromLastRunCount = documentsIdsFromLastRun.Count();
      Logger.DebugFormat("IndexDocumentsForFullTextSearch. Documents suitable for indexing: {0}.", documentsFromLastRunCount);
      
      if (documentsFromLastRunCount > 0)
      {
        var alreadyScheduledQueueItems = Functions.Module.GetAlreadyScheduledDocumentFullTextSearchQueueItems(documentsIdsFromLastRun).ToList();
        Functions.Module.IncreasePriorityForDocumentFullTextSearchQueueItems(alreadyScheduledQueueItems);
        documentsIdsFromLastRun = documentsIdsFromLastRun.Where(d => !alreadyScheduledQueueItems.Any(q => q.DocumentId == d)).ToList();
        Functions.Module.CreateDocumentFullTextSearchQueueItems(documentsIdsFromLastRun, Functions.Module.GetDocumentFullTextSearchQueueItemDefaultPriority());
      }
      
      var queueItemsInProcessCount = DocumentFullTextSearchQueueItems.GetAll(x => x.ProcessingStatus == DocumentIndexQueueItemStatus.InProcess).Count();
      var queueItemsFreeSlotsCount = Functions.Module.GetIndexDocumentsForFullTextSearchJobQueueItemsLimit() - queueItemsInProcessCount;
      Logger.DebugFormat("IndexDocumentsForFullTextSearch. Currently indexing: {0}.", queueItemsInProcessCount);
      
      if (queueItemsFreeSlotsCount > 0)
      {
        var queueItemsToProcess = DocumentFullTextSearchQueueItems.GetAll()
          .Where(x => x.ProcessingStatus == DocumentIndexQueueItemStatus.Scheduled)
          .OrderByDescending(x => x.Priority)
          .Take(queueItemsFreeSlotsCount)
          .ToList();
        
        if (queueItemsToProcess.Any())
          Logger.DebugFormat("IndexDocumentsForFullTextSearch. Start indexing for {0} documents async.", queueItemsToProcess.Count);
        
        foreach (var queueItem in queueItemsToProcess)
        {
          queueItem.ProcessingStatus = Docflow.DocumentFullTextSearchQueueItem.ProcessingStatus.InProcess;
          queueItem.Save();
          
          var asyncIndexing = AsyncHandlers.IndexDocumentForFullTextSearch.Create();
          asyncIndexing.QueueItemId = queueItem.Id;
          asyncIndexing.ExecuteAsync();
        }
      }
      
      Functions.Module.UpdateLastIndexingRunDate(currentRunDate);

      Logger.Debug("IndexDocumentsForFullTextSearch. Done job.");
    }

    /// <summary>
    /// Агент синхронизации статуса эл. доверенностей.
    /// </summary>
    [Public]
    public virtual void SyncFormalizedPowerOfAttorneyState()
    {
      if (!Sungero.Docflow.PublicFunctions.Module.IsPoAKonturLicenseEnable())
        return;
      
      Logger.Debug("SyncFormalizedPowerOfAttorneyState. Execute.");
      
      var activeServiceConnections = Sungero.PowerOfAttorneyCore.PublicFunctions.Module.GetActiveServiceConnections();
      if (!activeServiceConnections.Any())
        Logger.DebugFormat("SyncFormalizedPowerOfAttorneyState. No active service connections.");
      
      foreach (var connection in activeServiceConnections)
      {
        Functions.Module.SyncFormalizedPoAState(connection);
      }
      
      Logger.Debug("SyncFormalizedPowerOfAttorneyState. Done.");
    }

    /// <summary>
    /// Удаление результатов сравнения документов.
    /// </summary>
    public virtual void DeleteComparisonInfos()
    {
      var comparisonInfosToDelete = PublicFunctions.Module.GetDocumentComparisonInfosToDelete();
      if (!comparisonInfosToDelete.Any())
        return;
      
      foreach (var comparisonInfo in comparisonInfosToDelete)
      {
        if (!Locks.GetLockInfo(comparisonInfo).IsLockedByOther)
          DocumentComparisonInfos.Delete(comparisonInfo);
      }
    }
    
    /// <summary>
    /// Автоматическая выдача прав на документы.
    /// </summary>
    public virtual void GrantAccessRightsToDocuments()
    {
      var logMessagePrefix = "AccessRightsBulkProcessing. GrantAccessRightsToDocumentsJob.";
      Logger.DebugFormat("{0} Start.", logMessagePrefix);
      var hasQueueItems = AccessRightsBulkQueueItems
        .GetAll(x => x.ProcessingStatus == Docflow.AccessRightsBulkQueueItem.ProcessingStatus.Scheduled).Any();
      if (!hasQueueItems)
        Logger.DebugFormat("GrantAccessRightsToDocuments: Queue items count: 0");
      
      var queueItemsInProcessCount = AccessRightsBulkQueueItems.GetAll(x => x.ProcessingStatus == Docflow.AccessRightsBulkQueueItem.ProcessingStatus.InProcess).Count();
      var accessRightsQueueItemsLimit = Docflow.Functions.Module.GetAccessRightsBulkProcessingJobQueueItemsLimit();
      var queueItemsFreeSlotsCount = accessRightsQueueItemsLimit - queueItemsInProcessCount;
      if (queueItemsFreeSlotsCount > 0 && hasQueueItems)
      {
        var rules = AccessRightsRules.GetAll()
          .Where(x => x.GrantRightsOnExistingDocuments == true &&
                 x.Status == CoreEntities.DatabookEntry.Status.Active &&
                 (x.BulkProcessingState == Docflow.AccessRightsRule.BulkProcessingState.Planned ||
                  x.BulkProcessingState == Docflow.AccessRightsRule.BulkProcessingState.InProcess) &&
                 x.LaunchId != null &&
                 x.LaunchId != string.Empty)
          .ToList();
        
        // Проверка правил на блокировку осуществляется после их выборки из БД, потому что нельзя использовать прикладные функции в коде транслируемом в SQL.
        var rulesLaunchIds = rules
          .Where(x => !Locks.GetLockInfo(x).IsLocked)
          .Select(x => x.LaunchId)
          .ToList();
        
        var lockedRulesIds = rules
          .Where(x => Locks.GetLockInfo(x).IsLocked)
          .Select(x => x.Id)
          .ToList();
        if (lockedRulesIds.Any())
          Logger.DebugFormat("{0} Locked rules ids: [{1}]", logMessagePrefix, string.Join(", ", lockedRulesIds));
        
        var queueItems = AccessRightsBulkQueueItems
          .GetAll(x => x.ProcessingStatus == Docflow.AccessRightsBulkQueueItem.ProcessingStatus.Scheduled && rulesLaunchIds.Contains(x.LaunchId))
          .OrderByDescending(x => x.Priority)
          .Take(queueItemsFreeSlotsCount)
          .ToList();
        Logger.DebugFormat("{0} Queue items count: {1}.", logMessagePrefix, queueItems.Count);
        
        var rulesInProcessIds = queueItems
          .Where(x => x.RuleId.HasValue && x.RuleId > 0)
          .Select(x => x.RuleId.Value)
          .Distinct()
          .ToList();
        foreach (var ruleId in rulesInProcessIds)
        {
          try
          {
            var rule = rules.Where(r => r.Id == ruleId).First();
            rule.BulkProcessingState = Docflow.AccessRightsRule.BulkProcessingState.InProcess;
            rule.Save();
          }
          catch (Exception ex)
          {
            queueItems.RemoveAll(x => x.RuleId == ruleId);
            Logger.DebugFormat("{0} Skip grant rights by rule (Id={1}). Exception: ({2})", logMessagePrefix, ruleId, ex.Message);
          }
        }

        foreach (var queueItem in queueItems)
        {
          if (Functions.AccessRightsBulkQueueItem.SetProcessingStatusInProcess(queueItem))
            Functions.Module.CreateGrantAccessRightsToDocumentAsyncHandlerBulk(queueItem.Id);
        }
      }
      else if (queueItemsFreeSlotsCount <= 0)
        Logger.DebugFormat("{0} Already max queue items in process: {1}.", logMessagePrefix, queueItemsInProcessCount);

      Functions.Module.StartClearAccessRightsRulesState();
      Functions.Module.ClearAccessRightsBulkQueueItems();
      
      Logger.DebugFormat("{0} Done", logMessagePrefix);
    }
    
    /// <summary>
    /// Перемещение содержимого документа в хранилище.
    /// </summary>
    public virtual void TransferDocumentsByStoragePolicy()
    {

      var hasNotDefaultStorage = Sungero.CoreEntities.Storages.GetAll(s => s.IsDefault != true).Any();
      if (!hasNotDefaultStorage)
      {
        Logger.DebugFormat("TransferDocumentsByStoragePolicy: has only default storage.");
        return;
      }
      
      var hasStoragePolicies = Docflow.StoragePolicyBases.GetAll(p => p.Status == Docflow.StoragePolicyBase.Status.Active).Any();
      if (!hasStoragePolicies)
      {
        Logger.DebugFormat("TransferDocumentsByStoragePolicy: has no active storage policies.");
        return;
      }
      
      var now = Calendar.Now;
      var policiesToUpdateRetentionDate = RetentionPolicies.GetAll().Where(p => p.Status == Docflow.RetentionPolicy.Status.Active &&
                                                                           (p.NextRetention == null || p.NextRetention <= now)).ToList();
      
      Sungero.Docflow.Functions.Module.CreateStoragePolicySettings(now);
      
      var documentsToSetStorageList = Sungero.Docflow.Functions.Module.GetDocumentsToTransfer();
      
      Sungero.Docflow.Functions.Module.ExecuteSetDocumentStorage(documentsToSetStorageList);
      
      Logger.DebugFormat("TransferDocumentsByStoragePolicy: drop storage policy settings.");
      var commandText = string.Format(Docflow.Queries.Module.DropTable, Constants.Module.StoragePolicySettingsTableName);
      Sungero.Docflow.Functions.Module.ExecuteSQLCommand(commandText);
      
      foreach (var policy in policiesToUpdateRetentionDate)
      {
        try
        {
          policy.LastRetention = now;
          policy.NextRetention = Sungero.Docflow.PublicFunctions.RetentionPolicy.GetNextRetentionDate(policy.RepeatType, policy.IntervalType, policy.Interval, now);
          policy.Save();
          Logger.DebugFormat("TransferDocumentsByStoragePolicy: update storage policy {0}.", policy.Id);
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("TransferDocumentsByStoragePolicy: cannot update storage policy {0}.", ex, policy.Id);
        }
      }
      
    }

    /// <summary>
    /// Агент рассылки уведомления об окончании срока действия доверенностей.
    /// </summary>
    public virtual void SendNotificationForExpiringPowerOfAttorney()
    {
      var createTableCommand = Queries.Module.CreateTableForExpiringPowerOfAttorney;
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(createTableCommand);
      
      var notifyParams = PublicFunctions.Module.GetDefaultExpiringDocsNotificationParams(Constants.Module.ExpiringPowerOfAttorneyLastNotificationKey,
                                                                                         Constants.Module.ExpiringPowerOfAttorneyTableName);
      
      var alreadySentDocs = PublicFunctions.Module.GetDocumentsWithSendedTask(notifyParams.ExpiringDocTableName);
      
      var powerOfAttorneyIds = PowerOfAttorneyBases.GetAll()
        .Where(p => !alreadySentDocs.Contains(p.Id))
        .Where(p => p.LifeCycleState == Sungero.Docflow.PowerOfAttorneyBase.LifeCycleState.Active ||
               p.LifeCycleState == Sungero.Docflow.PowerOfAttorneyBase.LifeCycleState.Draft)
        .Where(p => p.IssuedTo != null || p.PreparedBy != null)
        .Where(p => notifyParams.LastNotificationReserve.AddDays(p.DaysToFinishWorks.HasValue ? p.DaysToFinishWorks.Value : 0) < p.ValidTill  &&
               p.ValidTill <= notifyParams.TodayReserve.AddDays(p.DaysToFinishWorks.HasValue ? p.DaysToFinishWorks.Value : 0))
        .Where(p => p.DaysToFinishWorks == null || p.DaysToFinishWorks <= Constants.Module.MaxDaysToFinish)
        .Select(p => p.Id)
        .ToList();

      Logger.DebugFormat("Powers of Attorney to send notification count = {0}.", powerOfAttorneyIds.Count());
      
      for (int i = 0; i < powerOfAttorneyIds.Count(); i = i + notifyParams.BatchCount)
      {
        var result = Transactions.Execute(
          () =>
          {
            var powerOfAttorneyPart = PowerOfAttorneyBases.GetAll(p => powerOfAttorneyIds.Contains(p.Id)).Skip(i).Take(notifyParams.BatchCount).ToList();
            powerOfAttorneyPart = powerOfAttorneyPart.Where(p => notifyParams.LastNotification.ToUserTime(p.PreparedBy ?? p.IssuedTo).AddDays(p.DaysToFinishWorks.HasValue ? p.DaysToFinishWorks.Value : 0) < p.ValidTill &&
                                                            p.ValidTill <= Calendar.GetUserToday(p.PreparedBy ?? p.IssuedTo).AddDays(p.DaysToFinishWorks.HasValue ? p.DaysToFinishWorks.Value : 0))
              .ToList();
            
            if (!powerOfAttorneyPart.Any())
              return;
            
            PublicFunctions.Module.ClearIdsFromExpiringDocsTable(notifyParams.ExpiringDocTableName,
                                                                 powerOfAttorneyPart.Select(x => x.Id).ToList());
            PublicFunctions.Module.AddExpiringDocumentsToTable(notifyParams.ExpiringDocTableName,
                                                               powerOfAttorneyPart.Select(x => x.Id).ToList());
            
            foreach (var powerOfAttorney in powerOfAttorneyPart)
            {
              var subject = Docflow.PublicFunctions.Module.TrimQuotes(Resources.ExpiringPowerOfAttorneySubjectFormat(powerOfAttorney.DisplayValue));
              if (subject.Length > Workflow.SimpleTasks.Info.Properties.Subject.Length)
                subject = subject.Substring(0, Workflow.SimpleTasks.Info.Properties.Subject.Length);
              
              var activeText = Docflow.PublicFunctions.Module.TrimQuotes(Resources.ExpiringPowerOfAttorneyTextFormat(powerOfAttorney.ValidTill.Value.ToShortDateString(),
                                                                                                                     powerOfAttorney.DisplayValue));
              
              var performers = Functions.Module.GetNotificationPoAPerformers(powerOfAttorney);
              performers = performers.Where(p => p != null).Distinct().ToList();
              
              var attachments = new List<Sungero.Content.IElectronicDocument>();
              attachments.Add(powerOfAttorney);
              
              notifyParams.TaskParams.Document = powerOfAttorney;
              notifyParams.TaskParams.Subject = subject;
              notifyParams.TaskParams.ActiveText = activeText;
              notifyParams.TaskParams.Performers = performers;
              notifyParams.TaskParams.Attachments = attachments;
              PublicFunctions.Module.TrySendExpiringDocNotifications(notifyParams);
            }
          });
      }
      
      if (PublicFunctions.Module.IsAllNotificationsStarted(notifyParams.ExpiringDocTableName))
      {
        PublicFunctions.Module.UpdateLastNotificationDate(notifyParams);
        PublicFunctions.Module.ClearExpiringTable(notifyParams.ExpiringDocTableName, false);
      }
    }
    
    /// <summary>
    /// Агент рассылки уведомления об окончании срока действия доверенностей.
    /// </summary>
    public virtual void SendNotificationForExpiringPowersOfAttorney()
    {
    }

    /// <summary>
    /// Выдать права на документ по правилу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="rule">Правило.</param>
    /// <returns>Возвращает true, если права удалось выдать, false - если надо повторить позже.</returns>
    public static bool TryGrantAccessRightsToDocumentByRule(IOfficialDocument document, IAccessRightsRule rule)
    {
      Logger.DebugFormat("TryGrantAccessRightsToDocumentByRule: document {0}, rule {1}", document.Id, rule.Id);
      
      var isChanged = false;
      foreach (var member in rule.Members)
      {
        if (!document.AccessRights.IsGrantedDirectly(Docflow.PublicFunctions.Module.GetRightTypeGuid(member.RightType), member.Recipient))
        {
          if (Locks.GetLockInfo(document).IsLockedByOther)
            return false;

          document.AccessRights.Grant(member.Recipient, Docflow.PublicFunctions.Module.GetRightTypeGuid(member.RightType));
          isChanged = true;
        }
      }
      if (isChanged)
      {
        ((Domain.Shared.IExtendedEntity)document).Params[Constants.OfficialDocument.DontUpdateModified] = true;
        document.Save();
      }
      
      return true;
    }
    
    /// <summary>
    /// Получить из списка правил подходящие для документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="rules">Правила.</param>
    /// <returns>Подходящие правила.</returns>
    public static List<IAccessRightsRule> GetAvailableRules(IOfficialDocument document, List<IAccessRightsRule> rules)
    {
      var documentGroup = Functions.OfficialDocument.GetDocumentGroup(document);
      
      return rules
        .Where(s => s.Status == Docflow.AccessRightsRule.Status.Active)
        .Where(s => !s.DocumentKinds.Any() || s.DocumentKinds.Any(k => Equals(k.DocumentKind, document.DocumentKind)))
        .Where(s => !s.BusinessUnits.Any() || s.BusinessUnits.Any(u => Equals(u.BusinessUnit, document.BusinessUnit)))
        .Where(s => !s.Departments.Any() || s.Departments.Any(k => Equals(k.Department, document.Department)))
        .Where(s => !s.DocumentGroups.Any() || s.DocumentGroups.Any(k => Equals(k.DocumentGroup, documentGroup))).ToList();
    }
    
    /// <summary>
    /// Получить документы по правилу.
    /// </summary>
    /// <param name="rule">Правило.</param>
    /// <returns>Документы по правилу.</returns>
    private static IEnumerable<long> GetDocumentsByRule(IAccessRightsRule rule)
    {
      var documentKinds = rule.DocumentKinds.Select(t => t.DocumentKind).ToList();
      var businessUnits = rule.BusinessUnits.Select(t => t.BusinessUnit).ToList();
      var departments = rule.Departments.Select(t => t.Department).ToList();
      
      var documents = OfficialDocuments.GetAll()
        .Where(d => !documentKinds.Any() || documentKinds.Contains(d.DocumentKind))
        .Where(d => !businessUnits.Any() || businessUnits.Contains(d.BusinessUnit))
        .Where(d => !departments.Any() || departments.Contains(d.Department));
      
      if (rule.DocumentGroups.Any())
        return FilterDocumentsByGroups(rule, documents);
      else
        return documents.Select(d => d.Id);
    }
    
    /// <summary>
    /// Получить ведущие документы.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Ведущие документы.</returns>
    private static List<long> GetLeadingDocuments(IOfficialDocument document)
    {
      var documents = new List<long>() { document.Id };
      var leadingDocuments = new List<long>();
      while (document.LeadingDocument != null && !documents.Contains(document.LeadingDocument.Id))
      {
        documents.Add(document.LeadingDocument.Id);
        leadingDocuments.Add(document.LeadingDocument.Id);
      }
      return leadingDocuments;
    }

    /// <summary>
    /// Фильтр для категорий договоров.
    /// </summary>
    /// <param name="rule">Правило.</param>
    /// <param name="query">Ленивый запрос документов.</param>
    /// <returns>Относительно ленивый запрос с категориями.</returns>
    private static IEnumerable<long> FilterDocumentsByGroups(IAccessRightsRule rule, IQueryable<IOfficialDocument> query)
    {
      foreach (var document in query)
      {
        var documentGroup = Functions.OfficialDocument.GetDocumentGroup(document);
        if (rule.DocumentGroups.Any(k => Equals(k.DocumentGroup, documentGroup)))
          yield return document.Id;
      }
    }

    /// <summary>
    /// Рассылка электронных писем о заданиях.
    /// </summary>
    public virtual void SendMailNotification()
    {
      Logger.Debug("SendMailNotification. Start.");
      Functions.Module.SendMailNotification();
      Logger.Debug("SendMailNotification. Done.");
    }
    
    /// <summary>
    /// Рассылка электронных писем со сводкой о заданиях.
    /// </summary>
    public virtual void SendSummaryMailNotifications()
    {
      Functions.Module.SummaryMailLogDebug("Start job SendSummaryMailNotifications.");
      Functions.Module.SendSummaryMailNotification();
      Functions.Module.SummaryMailLogDebug("End job SendSummaryMailNotifications.");
    }
    
  }
}