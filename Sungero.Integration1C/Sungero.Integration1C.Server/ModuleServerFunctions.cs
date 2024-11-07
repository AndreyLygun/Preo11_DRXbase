using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.SessionExtensions;
using Sungero.Domain.Shared;
using Sungero.Parties;
using Sungero.Workflow;

namespace Sungero.Integration1C.Server
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Проверить возможность подключения из 1С к Directum RX.
    /// </summary>
    /// <returns>True если подключение из 1С возможно, иначе False.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public virtual bool CheckConnection()
    {
      return Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Constants.Module.Integration1CGuid);
    }
    
    #region Получение сущностей для синхронизации в 1С    
    
    /// <summary>
    /// Получить ИД договоров, подходящих для синхронизации в 1С:
    /// - новые и измененные с момента последней синхронизации,
    /// - отфильтрованные по состоянию,
    /// - относящиеся к синхронизированному контрагенту.
    /// </summary>
    /// <param name="contractExtEntityType">Тип записи внешней системы для договоров.</param>
    /// <param name="counterpartyExtEntityType">Тип записи внешней системы для контрагентов.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Список ИД договоров.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public virtual List<long> GetContractIdsForSync(string contractExtEntityType, string counterpartyExtEntityType, string systemId)
    {
      var contractTypeGuid = typeof(Contracts.IContract).GetFinalType().GetTypeGuid();
      var companyTypeGuid = typeof(Parties.ICompany).GetFinalType().GetTypeGuid();
      var personTypeGuid = typeof(Parties.IPerson).GetFinalType().GetTypeGuid();
      var bankTypeGuid = typeof(Parties.IBank).GetFinalType().GetTypeGuid();

      var counterpartyTypeGuids = new List<Guid>() { companyTypeGuid, personTypeGuid, bankTypeGuid };
      List<string> counterpartyTypeGuidStrings = counterpartyTypeGuids.Select(e => e.ToString().ToLower()).ToList();
      
      var lifeCycleStates = new List<Enumeration>
      {
        Sungero.Contracts.Contract.LifeCycleState.Active,
        Sungero.Contracts.Contract.LifeCycleState.Closed,
        Sungero.Contracts.Contract.LifeCycleState.Terminated
      };
      
      using (var session = new Domain.Session())
      {
        var entities = this.GetChangedEntities(session, contractTypeGuid, contractExtEntityType, systemId);
        
        var entityIds = entities
          .Where(e => Contracts.Contracts.Is(e) &&
                      lifeCycleStates.Contains(Contracts.Contracts.As(e).LifeCycleState.Value) &&
                      Contracts.Contracts.As(e).Counterparty != null &&
                      Commons.ExternalEntityLinks.GetAll().Any(x => x.ExtSystemId == systemId &&
                                                                    counterpartyTypeGuidStrings.Contains(x.EntityType.ToLower()) &&
                                                                    x.ExtEntityType == counterpartyExtEntityType &&
                                                                    x.EntityId == Contracts.Contracts.As(e).Counterparty.Id &&
                                                                    x.IsDeleted != true))
          .Select(e => e.Id)
          .ToList();
        return entityIds;
      }
    }    
    
    /// <summary>
    /// Получить ИД доп. соглашений, подходящих для синхронизации в 1С:
    /// - новые и измененные с момента последней синхронизации,
    /// - отфильтрованные по состоянию,
    /// - относящиеся к синхронизированному контрагенту.
    /// </summary>
    /// <param name="contractExtEntityType">Тип записи внешней системы для договоров.</param>
    /// <param name="counterpartyExtEntityType">Тип записи внешней системы для контрагентов.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Список ИД доп. соглашений.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public virtual List<long> GetSupAgreementIdsForSync(string contractExtEntityType, string counterpartyExtEntityType, string systemId)
    {
      var supAgreementTypeGuid = typeof(Contracts.ISupAgreement).GetFinalType().GetTypeGuid();
      var companyTypeGuid = typeof(Parties.ICompany).GetFinalType().GetTypeGuid();
      var personTypeGuid = typeof(Parties.IPerson).GetFinalType().GetTypeGuid();
      var bankTypeGuid = typeof(Parties.IBank).GetFinalType().GetTypeGuid();

      var counterpartyTypeGuids = new List<Guid>() { companyTypeGuid, personTypeGuid, bankTypeGuid };
      List<string> counterpartyTypeGuidStrings = counterpartyTypeGuids.Select(e => e.ToString().ToLower()).ToList();
      
      var lifeCycleStates = new List<Enumeration>
      {
        Sungero.Contracts.SupAgreement.LifeCycleState.Active,
        Sungero.Contracts.SupAgreement.LifeCycleState.Closed,
        Sungero.Contracts.SupAgreement.LifeCycleState.Terminated
      };
      
      using (var session = new Domain.Session())
      {
        var entities = this.GetChangedEntities(session, supAgreementTypeGuid, contractExtEntityType, systemId);
        
        var entityIds = entities
          .Where(e => Contracts.SupAgreements.Is(e) &&
                 lifeCycleStates.Contains(Contracts.SupAgreements.As(e).LifeCycleState.Value)  &&
                 Contracts.SupAgreements.As(e).Counterparty != null &&
                 Commons.ExternalEntityLinks.GetAll().Any(x => x.ExtSystemId == systemId &&
                                                               counterpartyTypeGuidStrings.Contains(x.EntityType.ToLower()) &&
                                                               x.ExtEntityType == counterpartyExtEntityType &&
                                                               x.EntityId == Contracts.SupAgreements.As(e).Counterparty.Id &&
                                                               x.IsDeleted != true))
          .Select(e => e.Id)
          .ToList();
        return entityIds;
      }
    }
    
    /// <summary>
    /// Получить ИД организаций, подходящих для синхронизации в 1С:
    /// - новые и измененные с момента последней синхронизации,
    /// - отфильтрованные по состоянию.
    /// </summary>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <param name="strictMode">Строгий режим: true - новые организации возвращаются только с заполненным ИНН, false - заполненность ИНН у организаций не проверяется.</param>
    /// <returns>Список ИД организаций.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public virtual List<long> GetCompanyIdsForSync(string extEntityType, string systemId, bool strictMode)
    {
      var companyTypeGuid = typeof(Parties.ICompany).GetFinalType().GetTypeGuid();
      
      using (var session = new Domain.Session())
      {
        var entities = this.GetChangedEntities(session, companyTypeGuid, extEntityType, systemId);
        
        entities = entities.Where(e => Parties.Companies.Is(e) &&
                                       Parties.Companies.As(e).Status == Sungero.Parties.Company.Status.Active);
        
        if (strictMode)
          entities = this.FilterCounterpartiesForStrictMode(entities, extEntityType, companyTypeGuid, systemId);
        
        return entities.Select(e => e.Id).ToList();
      }
    }

    /// <summary>
    /// Получить ИД персон, подходящих для синхронизации в 1С:
    /// - новые и измененные с момента последней синхронизации,
    /// - отфильтрованные по состоянию.
    /// </summary>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <param name="strictMode">Строгий режим: true - новые персоны возвращаются только с заполненным ИНН, false - заполненность ИНН у персон не проверяется.</param>
    /// <returns>Список ИД персон.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public virtual List<long> GetPersonIdsForSync(string extEntityType, string systemId, bool strictMode)
    {
      var personTypeGuid = typeof(Parties.IPerson).GetFinalType().GetTypeGuid();
      
      using (var session = new Domain.Session())
      {
        var entities = this.GetChangedEntities(session, personTypeGuid, extEntityType, systemId);
        
        entities = entities.Where(e => Parties.People.Is(e) &&
                                       Parties.People.As(e).Status == Sungero.Parties.Person.Status.Active);

        if (strictMode)
          entities = this.FilterCounterpartiesForStrictMode(entities, extEntityType, personTypeGuid, systemId);
          
        return entities.Select(e => e.Id).ToList();
      }
    }
    
    /// <summary>
    /// Получить ИД банков, подходящих для синхронизации в 1С:
    /// - новые и измененные с момента последней синхронизации,
    /// - отфильтрованные по состоянию.
    /// </summary>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Список ИД банков.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public virtual List<long> GetBankIdsForSync(string extEntityType, string systemId)
    {
      var bankTypeGuid = typeof(Parties.IBank).GetFinalType().GetTypeGuid();
      
      using (var session = new Domain.Session())
      {
        var entities = this.GetChangedEntities(session, bankTypeGuid, extEntityType, systemId);
        
        var entityIds = entities
          .Where(e => Parties.Banks.Is(e) &&
                 Parties.Banks.As(e).Status == Sungero.Parties.Bank.Status.Active)
          .Select(e => e.Id)
          .ToList();
        return entityIds;
      }
    }
    
    /// <summary>
    /// Получить ИД банков, подходящих для синхронизации в 1С:
    /// - новые и измененные с момента последней синхронизации,
    /// - отфильтрованные по состоянию,
    /// - указаны в договорах и доп. соглашениях, как контрагенты.
    /// </summary>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <param name="strictMode">Строгий режим: true - новые банки возвращаются только с заполненным ИНН, false - заполненность ИНН у банков не проверяется.</param>
    /// <returns>Список ИД банков.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public virtual List<long> GetContractBankIdsForSync(string extEntityType, string systemId, bool strictMode)
    {
      var bankTypeGuid = typeof(Parties.IBank).GetFinalType().GetTypeGuid();
      
      using (var session = new Domain.Session())
      {
        var entities = this.GetChangedEntities(session, bankTypeGuid, extEntityType, systemId);
        
        var contractBankIds = Parties.PublicFunctions.Module.GetBankIdsFromContracts();
        
        entities = entities.Where(e => Parties.Banks.Is(e) &&
                                       Parties.Banks.As(e).Status == Sungero.Parties.Person.Status.Active &&
                                       contractBankIds.Contains(e.Id));
        
        if (strictMode)
          entities = this.FilterCounterpartiesForStrictMode(entities, extEntityType, bankTypeGuid, systemId);
        
        return entities.Select(e => e.Id).ToList();
      }
    }
    
    /// <summary>
    /// Получить ИД контактов, подходящих для синхронизации в 1С:
    /// - новые и измененные с момента последней синхронизации,
    /// - отфильтрованные по состоянию,
    /// - относящиеся к синхронизированному контрагенту.
    /// </summary>
    /// <param name="contactExtEntityType">Тип записи внешней системы для контактов.</param>
    /// <param name="counterpartyExtEntityType">Тип записи внешней системы для контрагентов.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Список ИД контактов.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public virtual List<long> GetContactIdsForSync(string contactExtEntityType, string counterpartyExtEntityType, string systemId)
    {
      var contactTypeGuid = typeof(Parties.IContact).GetFinalType().GetTypeGuid();
      var companyTypeGuid = typeof(Parties.ICompany).GetFinalType().GetTypeGuid();
      var bankTypeGuid = typeof(Parties.IBank).GetFinalType().GetTypeGuid();

      var counterpartyTypeGuids = new List<Guid>() { companyTypeGuid, bankTypeGuid };
      List<string> counterpartyTypeGuidStrings = counterpartyTypeGuids.Select(e => e.ToString().ToLower()).ToList();
      
      using (var session = new Domain.Session())
      {
        var entities = this.GetChangedEntities(session, contactTypeGuid, contactExtEntityType, systemId);
        
        var entityIds = entities
          .Where(e => Parties.Contacts.Is(e) &&
                 Parties.Contacts.As(e).Status == Sungero.Parties.Contact.Status.Active &&
                 Commons.ExternalEntityLinks.GetAll().Any(x => x.ExtSystemId == systemId &&
                                                               counterpartyTypeGuidStrings.Contains(x.EntityType.ToLower()) &&
                                                               x.ExtEntityType == counterpartyExtEntityType &&
                                                               x.EntityId == Parties.Contacts.As(e).Company.Id &&
                                                               x.IsDeleted != true))

          .Select(e => e.Id)
          .ToList();
        return entityIds;
      }
    }
    
    /// <summary>
    /// Получить ИД сущностей, измененных с момента последней синхронизации.
    /// </summary>
    /// <param name="entityTypeGuids">Список гуидов типов сущностей.</param>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Список ИД сущностей.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public List<long> GetChangedEntitiesIdsFromSyncDate(List<Guid> entityTypeGuids, string extEntityType, string systemId)
    {
      if (entityTypeGuids == null || entityTypeGuids.Count == 0)
        return new List<long>();
      
      using (var session = new Domain.Session())
      {
        var entities = new List<Sungero.Domain.Shared.IEntity>().AsQueryable();
        foreach (var entityTypeGuid in entityTypeGuids)
          entities = entities.Union(this.GetChangedEntities(session, entityTypeGuid, extEntityType, systemId));
        
        var entityIds = entities
          .Select(e => e.Id)
          .ToList();
        return entityIds;
      }
    }
    
    /// <summary>
    /// Отфильтровать контрагентов для строгого режима: новые контрагенты возвращаются только с заполненным ИНН, измененные - без проверки на заполненность ИНН.
    /// </summary>
    /// <param name="entities">Список контрагентов.</param>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="entityTypeGuid">Guid типа сущности.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Список отфильтрованных контрагентов.</returns>
    public virtual IQueryable<Sungero.Domain.Shared.IEntity> FilterCounterpartiesForStrictMode(IQueryable<Sungero.Domain.Shared.IEntity> entities,
                                                                                               string extEntityType, Guid entityTypeGuid, string systemId)
    {
      var newEntityIds = entities.Where(e => !Commons.ExternalEntityLinks.GetAll().Any(x => x.ExtSystemId == systemId &&
                                                                                       x.EntityType.ToLower() == entityTypeGuid.ToString().ToLower() &&
                                                                                       x.ExtEntityType == extEntityType &&
                                                                                       x.EntityId == e.Id))
                                 .Select(e => e.Id)
                                 .ToList();
      entities = entities.Where(e => (newEntityIds.Contains(e.Id) &&
                                      (Parties.Counterparties.As(e).TIN != null && Parties.Counterparties.As(e).TIN != string.Empty)) ||
                                     !newEntityIds.Contains(e.Id));
      return entities;
    }    
   
    /// <summary>
    /// Завершить синхронизацию сущности в систему 1С.
    /// </summary>
    /// <param name="entityTypeGuid">Гуид типа сущности.</param>
    /// <param name="entityId">ИД сущности.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <param name="isSuccess">Результат синхронизации сущности в систему 1С: true - успешно, false - неуспешно.</param>
    /// <remarks>Метод создан для возможности перекрытия логики завершения синхронизации сущности в систему 1С. 
    /// Например, в случае использования элементов очереди - в методе нужно удалить элемент очереди, относящийся к синхронизированной сущности.</remarks>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual void FinalizeSyncEntity(Guid entityTypeGuid, long entityId, string systemId, bool isSuccess)
    {

    }
    
    /// <summary>
    /// Получить сущности, измененные с момента последней синхронизации.
    /// </summary>
    /// <param name="entityTypeGuids">Список гуидов типов сущностей.</param>
    /// <param name="processedEntitiesCount">Количество обработанных записей.</param>
    /// <param name="entitiesCountForProcessing">Размер пакета.</param>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Список сущностей.</returns>
    [Remote(IsPure = true)]
    public List<Sungero.Domain.Shared.IEntity> GetChangedEntitiesFromSyncDateRemote(List<Guid> entityTypeGuids,
                                                                                    int processedEntitiesCount,
                                                                                    int entitiesCountForProcessing,
                                                                                    string extEntityType,
                                                                                    string systemId)
    {
      using (var session = new Domain.Session())
      {
        var entities = new List<Sungero.Domain.Shared.IEntity>().AsQueryable();
        
        foreach (var entityTypeGuid in entityTypeGuids)
        {
          entities = entities.Union(this.GetChangedEntities(session, entityTypeGuid, extEntityType, systemId));
        }
        var entitiesBatch = entities
          .Skip(processedEntitiesCount)
          .Take(entitiesCountForProcessing)
          .ToList();
        
        return entitiesBatch;
      }
    }
    
    /// <summary>
    /// Получить количество сущностей, измененных с момента последней синхронизации.
    /// </summary>
    /// <param name="entityTypeGuids">Список гуидов типов сущностей.</param>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Количество сущностей.</returns>
    [Remote(IsPure = true)]
    public int GetChangedEntitiesFromSyncDateRemoteCount(List<Guid> entityTypeGuids,
                                                         string extEntityType,
                                                         string systemId)
    {
      using (var session = new Domain.Session())
      {
        var totalCount = 0;
        
        foreach (var entityTypeGuid in entityTypeGuids)
          totalCount += this.GetChangedEntities(session, entityTypeGuid, extEntityType, systemId).Count();
        return totalCount;
      }
    }

    /// <summary>
    /// Получить банковские счета, измененные с момента последней синхронизации.
    /// </summary>
    /// <param name="entityTypeGuids">Список гуидов типов сущностей.</param>
    /// <param name="processedEntitiesCount">Количество обработанных записей.</param>
    /// <param name="entitiesCountForProcessing">Размер пакета.</param>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Список сущностей.</returns>
    [Remote(IsPure = true)]
    public List<Sungero.Domain.Shared.IEntity> GetChangedBankAccountsFromSyncDateRemote(List<Guid> entityTypeGuids,
                                                                                        int processedEntitiesCount,
                                                                                        int entitiesCountForProcessing,
                                                                                        string extEntityType,
                                                                                        string systemId)
    {
      using (var session = new Domain.Session())
      {
        var entities = new List<Sungero.Domain.Shared.IEntity>().AsQueryable();
        
        foreach (var entityTypeGuid in entityTypeGuids)
        {
          entities = entities.Union(this.GetChangedEntities(session, entityTypeGuid, extEntityType, systemId));
        }
        entities = entities.Where(x => Counterparties.Is(x) &&
                                  Counterparties.As(x).Status == Sungero.Parties.Counterparty.Status.Active &&
                                  Counterparties.As(x).Bank != null &&
                                  !string.IsNullOrEmpty(Counterparties.As(x).Account));
        
        var entitiesBatch = entities
          .Skip(processedEntitiesCount)
          .Take(entitiesCountForProcessing)
          .ToList();
        
        return entitiesBatch;
      }
    }
    
    /// <summary>
    /// Получить количество банковских счетов, измененных с момента последней синхронизации.
    /// </summary>
    /// <param name="entityTypeGuids">Список гуидов типов сущностей.</param>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Количество сущностей.</returns>
    [Remote(IsPure = true)]
    public int GetChangedBankAccountsFromSyncDateRemoteCount(List<Guid> entityTypeGuids,
                                                             string extEntityType,
                                                             string systemId)
    {
      using (var session = new Domain.Session())
      {
        var totalCount = 0;
        
        foreach (var entityTypeGuid in entityTypeGuids)
        {
          var entities = this.GetChangedEntities(session, entityTypeGuid, extEntityType, systemId).ToList();
          totalCount += entities.Where(x => Counterparties.Is(x) &&
                                       Counterparties.As(x).Status == Sungero.Parties.Counterparty.Status.Active &&
                                       Counterparties.As(x).Bank != null &&
                                       !string.IsNullOrEmpty(Counterparties.As(x).Account))
            .Count();
        }
        return totalCount;
      }
    }

    /// <summary>
    /// Получить сущности, измененные с момента последней синхронизации, в рамках сессии.
    /// </summary>
    /// <param name="session">Сессия.</param>
    /// <param name="entityTypeGuid">Guid типа сущности.</param>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Список сущностей, измененных с момента последней синхронизации.</returns>
    private IQueryable<Sungero.Domain.Shared.IEntity> GetChangedEntities(Sungero.Domain.Session session,
                                                                         Guid entityTypeGuid,
                                                                         string extEntityType,
                                                                         string systemId)
    {
      var entityTypeGuidString = entityTypeGuid.ToString().ToLower();
      var entityType = Sungero.Domain.Shared.TypeExtension.GetTypeByGuid(entityTypeGuid);
      var entityTypeMetadata = entityType.GetEntityMetadata();
      var originalTypeGuid = entityTypeGuid.GetOriginalTypeGuid();
      var historyType = entityTypeMetadata.HistoryTypeGuid.GetTypeByGuid();
      var user = Users.Current;
      
      var entities = session.GetAll(entityType)
        .Where(s => Domain.Session.GetAllFromCurrentSession(historyType).Cast<IHistory>()
               .Where(h => h.EntityType == originalTypeGuid && h.EntityId == s.Id)
               .Where(h => (!session.GetAll<Commons.IExternalEntityLink>()
                            .Any(el => el.EntityId == h.EntityId && el.EntityType.ToLower() == entityTypeGuidString &&
                                 el.ExtEntityType == extEntityType && el.ExtSystemId == systemId) &&
                            !session.GetAll<Commons.IExternalEntityLink>()
                            .Any(el => el.ExtEntityId == h.EntityId.ToString() &&
                                 el.ExtSystemId == Constants.Module.InternalLinkSystemId) ||
                            !Equals(h.User, user) &&
                            (h.Action == Sungero.CoreEntities.History.Action.Create || h.Action == Sungero.CoreEntities.History.Action.Update) &&
                            session.GetAll<Commons.IExternalEntityLink>()
                            .Any(el => el.EntityId == h.EntityId && el.EntityType.ToLower() == entityTypeGuidString &&
                                 el.ExtEntityType == extEntityType && Equals(el.ExtSystemId, systemId) &&
                                 (!el.SyncDate.HasValue || h.HistoryDate >= el.SyncDate))))
               .Any())
        .OrderBy(s => s.Id);

      return entities;
    }

    #endregion
    
    #region Уведомления о результатах синхронизации
    
    /// <summary>
    /// Отправить уведомление о результатах синхронизации в 1С простой задачей.
    /// </summary>
    /// <param name="title">Заголовок уведомления.</param>
    /// <param name="text">Содержание уведомления с результатами синхронизации.</param>
    [Remote]
    public void SendNotificationBySimpleTask(string title, string text)
    {
      var role = Roles.GetAll(x => x.Sid == Integration1C.Constants.Module.SynchronizationResponsibleRoleGuid).FirstOrDefault();
      if (role == null || !role.RecipientLinks.Any())
        return;
      
      var task = Workflow.SimpleTasks.CreateWithNotices(title, role);
      task.ActiveText = text;
      task.Save();
      task.Start();
    }
    
    /// <summary>
    /// Получить документ с результатами синхронизации за сегодня.
    /// </summary>
    /// <param name="fileExists">Признак, что документ с результатами синхронизации существует в системе.</param>
    /// <returns>Документ с сегодняшними результатами синхронизации.</returns>
    [Remote(IsPure = true)]
    public Docflow.ISimpleDocument GetTodayDocumentRemote(bool fileExists)
    {
      if (!fileExists)
      {
        var document = Docflow.SimpleDocuments.Create();
        document.Name = "Результаты синхронизации за " + Calendar.UserToday.Date.ToShortDateString();
        var version = document.Versions.AddNew();
        version.AssociatedApplication = Content.AssociatedApplications.GetAll(app => app.Extension == "txt").FirstOrDefault();
        document.Save();
        
        return document;
      }
      else
      {
        var document = Docflow.SimpleDocuments.GetAll().Where(doc => doc.Name == ("Результаты синхронизации за " + Calendar.UserToday.Date.ToShortDateString())).FirstOrDefault();
        return document;
      }
    }
    
    /// <summary>
    /// Проверить, что протокол результатов синхронизации за сегодня существует в системе.
    /// </summary>
    /// <returns>True, если сегодняшний протокол существует, иначе False.</returns>
    [Remote(IsPure = true)]
    public bool IsSummaryProtocolExistRemote()
    {
      return Docflow.SimpleDocuments.GetAll().Any(doc => doc.Name == ("Результаты синхронизации за " + Calendar.UserToday.Date.ToShortDateString()));
    }
    
    /// <summary>
    /// Обновить дату последней синхронизации с 1С.
    /// </summary>
    /// <param name="date">Дата синхронизации, на которую обновить.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    [Remote]
    public void UpdateLastNotificationDate(DateTime date, string systemId)
    {
      var key = Constants.Module.LastNotifyOfSyncDateParamName;
      var systemIdBytes = System.Text.Encoding.Default.GetBytes(systemId);
      var systemIdCode = string.Empty;
      foreach (var b in systemIdBytes)
        systemIdCode = string.Format("{0}{1}", systemIdCode, b.ToString("X2"));
      
      var command = string.Format(Queries.Module.SelectLastNotificationDate, key);
      var executionResult = Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
      var noticesValues = string.Empty;
      if (!(executionResult is DBNull) && executionResult != null)
        noticesValues = executionResult.ToString();
      
      if (string.IsNullOrWhiteSpace(noticesValues))
      {
        var value = string.Format("{0}{1}", date.ToString("yyyy-MM-dd"), systemIdCode);
        Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.InsertLastNotificationDate, new[] { key, value });
      }
      else
      {
        var parts = noticesValues.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        var dates = new Dictionary<string, string>();
        foreach (var part in parts)
        {
          var value = part.Substring(0, 10);
          var code = part.Remove(0, 10);
          dates.Add(code, value);
        }
        
        if (dates.ContainsKey(systemIdCode))
          dates[systemIdCode] = date.ToString("yyyy-MM-dd");
        else
          dates.Add(systemIdCode, date.ToString("yyyy-MM-dd"));
        
        var result = string.Empty;
        foreach (var d in dates)
          result = string.Format("{0}&{1}{2}", result, d.Value, d.Key);
        result = result.Trim('&');
        
        Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.UpdateLastNotificationDate, new[] { key, result });
      }
    }
    
    /// <summary>
    /// Получить дату последней синхронизации с 1С
    /// из уведомления о результатах синхронизации.
    /// </summary>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Дата последней синхронизации, либо пустая строка в случае ее отсутствия.</returns>
    [Remote(IsPure = true)]
    public string GetLastNotificationDate(string systemId)
    {
      var key = Constants.Module.LastNotifyOfSyncDateParamName;
      var systemIdBytes = System.Text.Encoding.Default.GetBytes(systemId);
      var systemIdCode = string.Empty;
      foreach (var b in systemIdBytes)
        systemIdCode = string.Format("{0}{1}", systemIdCode, b.ToString("X2"));
      
      try
      {
        var command = string.Format(Queries.Module.SelectLastNotificationDate, key);
        var executionResult = Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var noticesValues = string.Empty;
        if (!(executionResult is DBNull) && executionResult != null)
          noticesValues = executionResult.ToString();
        
        if (string.IsNullOrWhiteSpace(noticesValues))
          return string.Empty;

        var parts = noticesValues.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        var dates = new Dictionary<string, string>();
        foreach (var part in parts)
        {
          var value = part.Substring(0, 10);
          var code = part.Remove(0, 10);
          dates.Add(code, value);
        }
        
        if (dates.ContainsKey(systemIdCode))
          return dates[systemIdCode];
        else
          return string.Empty;
      }
      catch (Exception)
      {
        return string.Empty;
      }
    }
    
    #endregion
  }  
}