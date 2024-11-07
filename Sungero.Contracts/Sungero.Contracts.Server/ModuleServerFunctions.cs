using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Contracts.ContractBase;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.RelationType;
using Sungero.Docflow;
using Sungero.Docflow.ApprovalStage;
using Sungero.Docflow.DocumentKind;
using Sungero.Domain.Shared;
using Init = Sungero.Contracts.Constants.Module.Initialize;

namespace Sungero.Contracts.Server
{
  public class ModuleFunctions
  {
    #region Remote CRUD
    
    /// <summary>
    /// Создать дополнительное соглашение.
    /// </summary>
    /// <returns>Созданное доп. соглашение.</returns>
    [Remote]
    public ISupAgreement CreateSupAgreemnt()
    {
      return SupAgreements.Create();
    }
    
    /// <summary>
    /// Создать акт к договорному документу.
    /// </summary>
    /// <returns>Созданный акт.</returns>
    [Remote]
    public Sungero.FinancialArchive.IContractStatement CreateContractStatement()
    {
      return Sungero.FinancialArchive.ContractStatements.Create();
    }
    
    /// <summary>
    /// Создать входящий счет.
    /// </summary>
    /// <returns>Созданный входящий счет.</returns>
    [Remote]
    public IIncomingInvoice CreateIncomingInvoice()
    {
      return IncomingInvoices.Create();
    }
    
    /// <summary>
    /// Создать исходящий счет.
    /// </summary>
    /// <returns>Созданный исходящий счет.</returns>
    [Remote]
    public IOutgoingInvoice CreateOutgoingInvoice()
    {
      return OutgoingInvoices.Create();
    }
    
    #endregion
    
    #region Спец. папки

    /// <summary>
    /// Получить виды документов с документопотоком "Договоры".
    /// </summary>
    /// <returns>Виды документов.</returns>
    [Remote]
    public static global::System.Linq.IQueryable<Sungero.Docflow.IDocumentKind> GetDocumentKinds()
    {
      return DocumentKinds.GetAll().Where(k => k.DocumentFlow == DocumentFlow.Contracts);
    }
    
    #endregion
    
    #region Обложка
    
    /// <summary>
    /// Получение списка документов, удовлетворяющих условиям поиска.
    /// </summary>
    /// <returns>Массив строк для выбора.</returns>
    [Remote(IsPure = true), Public]
    public static List<IDocumentRegister> GetContractualDocumentRegisters()
    {
      return DocumentRegisters.GetAll()
        .Where(r => r.DocumentFlow == Sungero.Docflow.DocumentRegister.DocumentFlow.Contracts).ToList();
    }
    
    /// <summary>
    /// Получение списка договорных документов, удовлетворяющих условиям поиска по регистрационным данным.
    /// </summary>
    /// <param name="number">Номер регистрации документа.</param>
    /// <param name="dateFrom">Дата регистрации документа от.</param>
    /// <param name="dateTo">Дата регистрации документа по.</param>
    /// <param name="documentRegister">Журнал регистрации.</param>
    /// <param name="caseFile">Дело.</param>
    /// <param name="responsibleEmployee">Сотрудник.</param>
    /// <returns>Выборка договоров, удовлетворяющих условиям.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IContractualDocument> GetFilteredRegisteredDocuments(
      string number, DateTime? dateFrom, DateTime? dateTo,
      IDocumentRegister documentRegister, ICaseFile caseFile, IEmployee responsibleEmployee)
    {
      if (dateTo != null)
        dateTo = dateTo.Value.AddDays(1);
      return ContractualDocuments.GetAll()
        .Where(l => number == null || l.RegistrationNumber.Contains(number))
        .Where(l => dateFrom == null || l.RegistrationDate >= dateFrom)
        .Where(l => dateTo == null || l.RegistrationDate < dateTo)
        .Where(l => documentRegister == null || l.DocumentRegister.Equals(documentRegister))
        .Where(l => caseFile == null || l.CaseFile.Equals(caseFile))
        .Where(l => responsibleEmployee == null || l.ResponsibleEmployee.Equals(responsibleEmployee));
    }
    
    /// <summary>
    /// Получение договорных документов для контрагента.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="dateFrom">Дата регистрации документа от.</param>
    /// <param name="dateTo">Дата регистрации документа по.</param>
    /// <returns>Выборка договорных документов, удовлетворяющих условиям.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IContractualDocument> GetContractualDocsWithCounterparty(Parties.ICounterparty counterparty,
                                                                                      DateTime? dateFrom, DateTime? dateTo)
    {
      if (dateTo != null)
        dateTo = dateTo.Value.AddDays(1);

      return ContractualDocuments.GetAll()
        .Where(r => dateFrom == null || r.RegistrationDate >= dateFrom)
        .Where(r => dateTo == null || r.RegistrationDate < dateTo)
        .Where(r => r.Counterparty.Equals(counterparty));
    }
    
    #endregion
    
    #region Рассылка по пролонгации
    
    /// <summary>
    /// Сотрудники, которых необходимо уведомить о сроке договора.
    /// </summary>
    /// <param name="contract">Договор.</param>
    /// <returns>Список сотрудников.</returns>
    public virtual List<IUser> GetNotificationPerformers(IContractBase contract)
    {
      var performer = contract.ResponsibleEmployee ?? Employees.As(contract.Author);
      var performers = new List<IUser>() { };
      
      if (performer == null)
        return performers;
      
      var manager = Docflow.PublicFunctions.Module.Remote.GetManager(performer);
      
      var performerPersonalSetting = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(performer).MyContractsNotification;
      
      if (performerPersonalSetting == true)
        performers.Add(performer);
      if (manager != null)
      {
        var managerPersonalSetting = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(manager).MySubordinatesContractsNotification;
        if (managerPersonalSetting == true)
          performers.Add(manager);
      }
      
      return performers;
    }
    
    #endregion
    
    /// <summary>
    /// Отфильтровать договора в зависимости от ЖЦ.
    /// </summary>
    /// <param name="query">Выборка договоров.</param>
    /// <returns>Отфильтрованные договора.</returns>
    public static IQueryable<IContractBase> FilterContractsByLifeCycleState(IQueryable<IContractBase> query)
    {
      return query.Where(c => !Equals(c.LifeCycleState, Sungero.Contracts.ContractBase.LifeCycleState.Obsolete) &&
                         !Equals(c.LifeCycleState, Sungero.Contracts.ContractBase.LifeCycleState.Terminated) &&
                         !Equals(c.LifeCycleState, Sungero.Contracts.ContractBase.LifeCycleState.Closed));
    }
    
    /// <summary>
    /// Найти договор. Применяется при переходе по ссылке из 1С.
    /// </summary>
    /// <param name="uuid">Uuid договора в 1С.</param>
    /// <param name="number">Номер договора.</param>
    /// <param name="date">Дата договора.</param>
    /// <param name="businessUnitTIN">ИНН НОР.</param>
    /// <param name="businessUnitTRRC">КПП НОР.</param>
    /// <param name="counterpartyUuid">Uuid контрагента в 1С.</param>
    /// <param name="counterpartyTIN">ИНН контрагента.</param>
    /// <param name="counterpartyTRRC">КПП контрагента.</param>
    /// <param name="sysid">Код инстанса 1С.</param>
    /// <returns>Список найденных договоров.</returns>
    [Remote(IsPure = true)]
    public static List<IContractualDocument> FindContract(string uuid, string number, string date,
                                                          string businessUnitTIN, string businessUnitTRRC,
                                                          string counterpartyUuid, string counterpartyTIN, string counterpartyTRRC,
                                                          string sysid)
    {
      // Найти документ среди синхронизированных ранее.
      if (!string.IsNullOrWhiteSpace(uuid) && !string.IsNullOrWhiteSpace(sysid))
      {
        // Получить GUID типа Договор и доп.соглашение из разработки.
        var etalonContractTypeGuid = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(typeof(IContract).GetFinalType()).NameGuid.ToString();
        var etalonSubAgreementTypeGuid = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(typeof(ISupAgreement).GetFinalType()).NameGuid.ToString();
        
        var extLinks = Commons.PublicFunctions.Module.GetExternalEntityLinks(uuid, sysid)
          .Where(x => x.EntityType == etalonContractTypeGuid || x.EntityType == etalonSubAgreementTypeGuid)
          .ToList();
        var contractIds = extLinks.Where(x => x.EntityType.ToUpper() == etalonContractTypeGuid.ToUpper()).Select(x => x.EntityId).ToList();
        var supAgreeIds = extLinks.Where(x => x.EntityType.ToUpper() == etalonSubAgreementTypeGuid.ToUpper()).Select(x => x.EntityId).ToList();
        
        var existDocuments = new List<IContractualDocument>();
        existDocuments.AddRange(Contracts.GetAll().Where(x => contractIds.Contains(x.Id)));
        existDocuments.AddRange(SupAgreements.GetAll().Where(x => supAgreeIds.Contains(x.Id)));
        
        if (existDocuments.Any())
          return existDocuments;
      }
      
      var result = ContractualDocuments.GetAll();
      
      // Фильтр по НОР.
      if (string.IsNullOrWhiteSpace(businessUnitTIN) || string.IsNullOrWhiteSpace(businessUnitTRRC))
        return new List<IContractualDocument>();
      
      var businessUnit = Sungero.Company.BusinessUnits.GetAll().FirstOrDefault(x => x.TIN == businessUnitTIN && x.TRRC == businessUnitTRRC);
      if (businessUnit == null)
        return new List<IContractualDocument>();
      else
        result = result.Where(x => Equals(x.BusinessUnit, businessUnit));
      
      // Фильтр по номеру.
      if (string.IsNullOrEmpty(number))
      {
        var emptyNumberSynonyms = new List<string> { "б/н", "бн", "б-н", "б.н.", "б\\н" };
        result = result.Where(x => emptyNumberSynonyms.Contains(x.RegistrationNumber.ToLower()));
      }
      else
      {
        result = result.Where(x => x.RegistrationNumber == number);
      }
      
      // Фильтр по дате.
      DateTime parsedDate;
      if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParseExact(date,
                                                                     "dd'.'MM'.'yyyy",
                                                                     System.Globalization.CultureInfo.InvariantCulture,
                                                                     System.Globalization.DateTimeStyles.None,
                                                                     out parsedDate))
        result = result.Where(x => x.RegistrationDate == parsedDate);
      
      // Фильтр по контрагенту.
      var counterparties = Sungero.Parties.PublicFunctions.Module.Remote.FindCounterparty(counterpartyUuid, counterpartyTIN, counterpartyTRRC, sysid);
      if (counterparties.Any())
        result = result.Where(x => counterparties.Contains(x.Counterparty));
      
      return result.ToList();
    }
    
    /// <summary>
    /// Запустить фоновый процесс "Договоры. Рассылка задач об окончании срока действия договоров".
    /// </summary>
    [Public, Remote]
    public static void RequeueSendNotificationForExpiringContracts()
    {
      Jobs.SendNotificationForExpiringContracts.Enqueue();
    }
    
    /// <summary>
    /// Запустить фоновый процесс "Договоры. Рассылка задач о выполнении работ по договору".
    /// </summary>
    [Public, Remote]
    public static void RequeueSendTaskForContractMilestones()
    {
      Jobs.SendTaskForContractMilestones.Enqueue();
    }
    
    /// <summary>
    /// Получить ответственного за договор.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Сотрудник.</returns>
    [Public]
    public virtual IEmployee GetPerformerContractResponsible(IOfficialDocument document)
    {
      if (ContractualDocuments.Is(document))
        return ContractualDocuments.As(document).ResponsibleEmployee;
      else if (Docflow.AccountingDocumentBases.Is(document))
        return Docflow.AccountingDocumentBases.As(document).ResponsibleEmployee;
      return null;
    }
    
    #region Фильтрация
    
    #region Исходящие счета на оплату
    
    /// <summary>
    /// Отфильтровать исходящие счета на оплату по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Исходящие счета для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные исходящие счета.</returns>
    /// <remarks>Условия, которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<Sungero.Contracts.IOutgoingInvoice> OutgoingInvoicesApplyStrongFilter(IQueryable<Sungero.Contracts.IOutgoingInvoice> query, Sungero.Contracts.IOutgoingInvoiceFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "Контрагент".
      if (filter.Counterparty != null)
        query = query.Where(x => Equals(x.Counterparty, filter.Counterparty));
      
      // Фильтр "Подразделение".
      if (filter.Department != null)
        query = query.Where(x => Equals(x.Department, filter.Department));
      
      // Фильтр "Дата счета".
      if (filter.Last30daysInvoice || filter.Last7daysInvoice)
        query = this.OutgoingInvoicesApplyFilterByDate(query, filter);
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать исходящие счета на оплату по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">Исходящие счета для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные исходящие счета.</returns>
    /// <remarks>Условия, которые используют индексы, но не максимально оптимально.</remarks>
    public virtual IQueryable<Sungero.Contracts.IOutgoingInvoice> OutgoingInvoicesApplyOrdinaryFilter(IQueryable<Sungero.Contracts.IOutgoingInvoice> query, Sungero.Contracts.IOutgoingInvoiceFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "НОР".
      if (filter.BusinessUnit != null)
        query = query.Where(x => Equals(x.BusinessUnit, filter.BusinessUnit));
      
      // Фильтр "Состояние".
      if ((filter.DraftState || filter.ActiveState || filter.PaidState || filter.ObsoleteState) &&
          !(filter.DraftState && filter.ActiveState && filter.PaidState && filter.ObsoleteState))
      {
        query = query.Where(x => filter.DraftState && x.LifeCycleState == Sungero.Contracts.OutgoingInvoice.LifeCycleState.Draft ||
                            filter.ActiveState && x.LifeCycleState == Sungero.Contracts.OutgoingInvoice.LifeCycleState.Active ||
                            filter.PaidState && x.LifeCycleState == Sungero.Contracts.OutgoingInvoice.LifeCycleState.Paid ||
                            filter.ObsoleteState && x.LifeCycleState == Sungero.Contracts.OutgoingInvoice.LifeCycleState.Obsolete);
      }
      
      // Фильтр "Дата счета".
      if (filter.ManualPeriodInvoice)
        query = this.OutgoingInvoicesApplyFilterByDate(query, filter);
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать исходящие счета на оплату по неоптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Исходящие счета для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные исходящие счета.</returns>
    /// <remarks>Условия, которые могут выполняться долго (например те, которые не могут использовать индексы).</remarks>
    public virtual IQueryable<Sungero.Contracts.IOutgoingInvoice> OutgoingInvoicesApplyWeakFilter(IQueryable<Sungero.Contracts.IOutgoingInvoice> query, Sungero.Contracts.IOutgoingInvoiceFilterState filter)
    {
      return query;
    }
    
    /// <summary>
    /// Отфильтровать исходящие счета на оплату по дате счета.
    /// </summary>
    /// <param name="query">Исходящие счета для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные исходящие счета.</returns>
    public virtual IQueryable<Sungero.Contracts.IOutgoingInvoice> OutgoingInvoicesApplyFilterByDate(IQueryable<Sungero.Contracts.IOutgoingInvoice> query, Sungero.Contracts.IOutgoingInvoiceFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      var beginDate = Calendar.UserToday.AddDays(-30);
      var endDate = Calendar.UserToday;
      
      if (filter.Last7daysInvoice)
        beginDate = Calendar.UserToday.AddDays(-7);
      
      if (filter.ManualPeriodInvoice)
      {
        beginDate = filter.DateRangeInvoiceFrom ?? Calendar.SqlMinValue;
        endDate = filter.DateRangeInvoiceTo ?? Calendar.SqlMaxValue;
      }
      
      query = Docflow.PublicFunctions.Module.OfficialDocumentsApplyFilterByDate(query, beginDate, endDate)
        .Cast<IOutgoingInvoice>();
      
      return query;
    }
    
    /// <summary>
    /// Определить, нужно ли использовать предварительную фильтрацию для исходящих счетов на оплату.
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True, если нужно использовать предварительную фильтрацию.</returns>
    public virtual bool UsePrefilterOutgoingInvoices(Sungero.Contracts.IOutgoingInvoiceFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.Department != null ||
         filter.Counterparty != null ||
         filter.Last7daysInvoice ||
         filter.Last30daysInvoice);
      return hasStrongFilter;
    }
    
    #endregion
    
    #region Входящие счета на оплату
    
    /// <summary>
    /// Отфильтровать входящие счета на оплату по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Входящие счета для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные входящие счета.</returns>
    /// <remarks>Условия, которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<Sungero.Contracts.IIncomingInvoice> IncomingInvoicesApplyStrongFilter(IQueryable<Sungero.Contracts.IIncomingInvoice> query, Sungero.Contracts.IIncomingInvoiceFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "Контрагент".
      if (filter.Counterparty != null)
        query = query.Where(x => Equals(x.Counterparty, filter.Counterparty));
      
      // Фильтр "Подразделение".
      if (filter.Department != null)
        query = query.Where(x => Equals(x.Department, filter.Department));
      
      // Фильтр "Дата счета".
      if (filter.Last30daysInvoice || filter.Last7daysInvoice)
        query = this.IncomingInvoicesApplyFilterByDate(query, filter);

      return query;
    }
    
    /// <summary>
    /// Отфильтровать входящие счета на оплату по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">Входящие счета для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные входящие счета.</returns>
    /// <remarks>Условия, которые используют индексы, но не максимально оптимально.</remarks>
    public virtual IQueryable<Sungero.Contracts.IIncomingInvoice> IncomingInvoicesApplyOrdinaryFilter(IQueryable<Sungero.Contracts.IIncomingInvoice> query, Sungero.Contracts.IIncomingInvoiceFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "НОР".
      if (filter.BusinessUnit != null)
        query = query.Where(x => Equals(x.BusinessUnit, filter.BusinessUnit));
      
      // Состояние.
      if ((filter.Draft || filter.OnApproval || filter.PayAccepted || filter.PayRejected || filter.PayComplete) &&
          !(filter.Draft && filter.OnApproval && filter.PayAccepted && filter.PayRejected && filter.PayComplete))
      {
        query = query.Where(x => (filter.Draft && x.LifeCycleState == Sungero.Contracts.IncomingInvoice.LifeCycleState.Draft && x.InternalApprovalState == null) ||
                            (filter.OnApproval && x.LifeCycleState != Sungero.Contracts.IncomingInvoice.LifeCycleState.Obsolete &&
                             (x.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.PendingSign ||
                              x.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnRework ||
                              x.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnApproval)) ||
                            (filter.PayAccepted && x.LifeCycleState == Sungero.Contracts.IncomingInvoice.LifeCycleState.Active) ||
                            (filter.PayRejected && x.LifeCycleState == Sungero.Contracts.IncomingInvoice.LifeCycleState.Obsolete) ||
                            (filter.PayComplete && x.LifeCycleState == Sungero.Contracts.IncomingInvoice.LifeCycleState.Paid));
      }
      
      // Фильтр "Дата счета".
      if (filter.ManualPeriodInvoice)
        query = this.IncomingInvoicesApplyFilterByDate(query, filter);
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать входящие счета на оплату по неоптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Входящие счета для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные входящие счета.</returns>
    /// <remarks>Условия, которые могут выполняться долго (например те, которые не могут использовать индексы).</remarks>
    public virtual IQueryable<Sungero.Contracts.IIncomingInvoice> IncomingInvoicesApplyWeakFilter(IQueryable<Sungero.Contracts.IIncomingInvoice> query, Sungero.Contracts.IIncomingInvoiceFilterState filter)
    {
      return query;
    }
    
    /// <summary>
    /// Отфильтровать входящие счета на оплату по дате счета.
    /// </summary>
    /// <param name="query">Входящие счета для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные входящие счета.</returns>
    public virtual IQueryable<Sungero.Contracts.IIncomingInvoice> IncomingInvoicesApplyFilterByDate(IQueryable<Sungero.Contracts.IIncomingInvoice> query, Sungero.Contracts.IIncomingInvoiceFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      var beginDate = Calendar.UserToday.AddDays(-30);
      var endDate = Calendar.UserToday;
      
      if (filter.Last7daysInvoice)
        beginDate = Calendar.UserToday.AddDays(-7);
      
      if (filter.ManualPeriodInvoice)
      {
        beginDate = filter.DateRangeInvoiceFrom ?? Calendar.SqlMinValue;
        endDate = filter.DateRangeInvoiceTo ?? Calendar.SqlMaxValue;
      }
      
      query = Docflow.PublicFunctions.Module.OfficialDocumentsApplyFilterByDate(query, beginDate, endDate)
        .Cast<IIncomingInvoice>();
      
      return query;
    }
    
    /// <summary>
    /// Определить, нужно ли использовать предварительную фильтрацию для входящих счетов на оплату.
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True, если нужно использовать предварительную фильтрацию.</returns>
    public virtual bool UsePrefilterIncomingInvoices(Sungero.Contracts.IIncomingInvoiceFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.Department != null ||
         filter.Counterparty != null ||
         filter.Last7daysInvoice ||
         filter.Last30daysInvoice);
      return hasStrongFilter;
    }
    
    #endregion
    
    #region Список "Документы у контрагентов"
    
    /// <summary>
    /// Отфильтровать действующие виды документов с документопотоком "Договоры".
    /// </summary>
    /// <param name="query">Фильтруемые виды документов.</param>
    /// <param name="withoutActs">True, если получить наследников договоров и доп. соглашений. Иначе - все договорные виды документов.</param>
    /// <returns>Виды документов.</returns>
    [Public]
    public static IQueryable<Docflow.IDocumentKind> ContractsFilterContractsKind(IQueryable<Docflow.IDocumentKind> query, bool withoutActs)
    {
      query = query
        .Where(d => d.Status == CoreEntities.DatabookEntry.Status.Active)
        .Where(d => d.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts);
      
      if (withoutActs)
      {
        var supKinds = Docflow.PublicFunctions.DocumentKind.GetAvailableDocumentKinds(typeof(ISupAgreement));
        var contractKinds = Docflow.PublicFunctions.DocumentKind.GetAvailableDocumentKinds(typeof(IContractBase));
        query = query.Where(k => supKinds.Contains(k) || contractKinds.Contains(k));
      }
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договорные и финансовые документы по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные документы.</returns>
    /// <remarks>Условия, которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> ContractsAtContractorsApplyStrongFilter(IQueryable<Sungero.Docflow.IOfficialDocument> query, Sungero.Contracts.FolderFilterState.IContractsAtContractorsFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "Категория".
      if (filter.Category != null)
        query = query.Where(c => !ContractBases.Is(c) || (ContractBases.Is(c) && Equals(c.DocumentGroup, filter.Category)));
      
      // Фильтр "Контрагент".
      if (filter.Contractor != null)
        query = query.Where(c => (Docflow.ContractualDocumentBases.Is(c) && Equals(Docflow.ContractualDocumentBases.As(c).Counterparty, filter.Contractor)) ||
                            (Docflow.AccountingDocumentBases.Is(c) && Equals(Docflow.AccountingDocumentBases.As(c).Counterparty, filter.Contractor)));
      
      // Фильтр "Ответственный за возврат".
      if (filter.Responsible != null)
        query = query.Where(c => Equals(c.ResponsibleForReturnEmployee, filter.Responsible));
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договорные и финансовые документы по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">Документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные документы.</returns>
    /// <remarks>Условия, которые используют индексы, но не максимально оптимально.</remarks>
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> ContractsAtContractorsApplyOrdinaryFilter(IQueryable<Sungero.Docflow.IOfficialDocument> query, Sungero.Contracts.FolderFilterState.IContractsAtContractorsFilterState filter)
    {
      // Фильтр "Вид документа".
      if (filter.DocumentKind != null)
        query = query.Where(c => Equals(c.DocumentKind, filter.DocumentKind));
      
      // Фильтр "Наша организация".
      if (filter.BusinessUnit != null)
        query = query.Where(c => Equals(c.BusinessUnit, filter.BusinessUnit));
      
      // Фильтр "Только просроченные".
      if (filter.OnlyOverdue)
      {
        var today = Calendar.UserToday;
        query = query.Where(c => c.ScheduledReturnDateFromCounterparty < today);
      }
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договорные и финансовые документы по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">Документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные документы.</returns>
    /// <remarks>Условия, которые могут выполняться долго (например, те которые не могут использовать индексы).</remarks>
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> ContractsAtContractorsApplyWeakFilter(IQueryable<Sungero.Docflow.IOfficialDocument> query, Sungero.Contracts.FolderFilterState.IContractsAtContractorsFilterState filter)
    {
      return query;
    }
    
    /// <summary>
    /// Определить, нужно ли использовать предварительную фильтрацию в списке "Документы у контрагентов".
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True, если нужно использовать предварительную фильтрацию.</returns>
    public virtual bool UsePrefilterContractsAtContractors(Sungero.Contracts.FolderFilterState.IContractsAtContractorsFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.Category != null ||
         filter.Contractor != null ||
         filter.Responsible != null);
      return hasStrongFilter;
    }
    
    #endregion
    
    #region Реестр договоров
    
    /// <summary>
    /// Отфильтровать договорные и финансовые документы по типам и признаку "Требуется возврат от контрагента".
    /// </summary>
    /// <param name="query">Документы для фильтрации.</param>
    /// <returns>Отфильтрованные документы.</returns>
    /// <remarks>Фильтрует по условиям, попадающим в индекс, но не сокращающим выборку на порядки.
    /// Рекомендуется использовать в событии фильтрации.</remarks>
    [Public]
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> ContractsAtContractorsApplyInvariantFilter(IQueryable<Sungero.Docflow.IOfficialDocument> query)
    {
      query = query.Where(c => c.IsHeldByCounterParty.Value &&
                          (ContractBases.Is(c) || SupAgreements.Is(c) ||
                           FinancialArchive.ContractStatements.Is(c) || FinancialArchive.Waybills.Is(c) ||
                           FinancialArchive.UniversalTransferDocuments.Is(c) || Exchange.CancellationAgreements.Is(c)));
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договорные документы по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Договорные документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договорные документы.</returns>
    /// <remarks>Условия, которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractualDocumentsApplyStrongFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.Contracts.FolderFilterState.IContractsListFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "Категория".
      if (filter.Category != null)
        query = query.Where(c => ContractBases.Is(c) && Equals(c.DocumentGroup, filter.Category));
      
      // Фильтр "Контрагент".
      if (filter.Contractor != null)
        query = query.Where(c => Equals(c.Counterparty, filter.Contractor));
      
      // Фильтр "Подразделение".
      if (filter.Department != null)
        query = query.Where(c => Equals(c.Department, filter.Department));
      
      // Фильтрация по дате договора
      if (filter.Last30days)
        query = this.ContractualDocumentsApplyFilterByDate(query, filter);
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договорные документы по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">Договорные документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договорные документы.</returns>
    /// <remarks>Условия, которые используют индексы, но не максимально оптимально.</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractualDocumentsApplyOrdinaryFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.Contracts.FolderFilterState.IContractsListFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр по состоянию.
      var statuses = new List<Enumeration>();
      if (filter.Draft)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Draft);
      if (filter.Active)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Active);
      if (filter.Executed)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Closed);
      if (filter.Terminated)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Terminated);
      if (filter.Cancelled)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Obsolete);

      // Фильтр по состоянию.
      if (statuses.Any())
        query = query.Where(q => q.LifeCycleState != null && statuses.Contains(q.LifeCycleState.Value));
      
      // Фильтр "Вид документа".
      if (filter.DocumentKind != null)
        query = query.Where(c => Equals(c.DocumentKind, filter.DocumentKind));
      
      // Фильтр "Наша организация".
      if (filter.BusinessUnit != null)
        query = query.Where(c => Equals(c.BusinessUnit, filter.BusinessUnit));
      
      // Фильтрация по дате договора
      if (filter.Last365days || filter.ManualPeriod)
        query = this.ContractualDocumentsApplyFilterByDate(query, filter);
      
      // Фильтр по типу документа
      if (filter.Contracts != filter.SupAgreements)
      {
        if (filter.Contracts)
          query = query.Where(d => ContractBases.Is(d));

        if (filter.SupAgreements)
          query = query.Where(d => SupAgreements.Is(d));
      }
      else
        query = query.Where(d => ContractBases.Is(d) || SupAgreements.Is(d));
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договорные документы по неоптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Договорные документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договорные документы.</returns>
    /// <remarks>Условия, которые могут выполняться долго (например, те которые не могут использовать индексы).</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractualDocumentsApplyWeakFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.Contracts.FolderFilterState.IContractsListFilterState filter)
    {
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договорные документы по дате договора.
    /// </summary>
    /// <param name="query">Договорные документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договорные документы.</returns>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractualDocumentsApplyFilterByDate(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.Contracts.FolderFilterState.IContractsListFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      var beginDate = Calendar.UserToday.AddDays(-30);
      var endDate = Calendar.UserToday;
      
      if (filter.Last365days)
        beginDate = Calendar.UserToday.AddDays(-365);
      
      if (filter.ManualPeriod)
      {
        beginDate = filter.DateRangeFrom ?? Calendar.SqlMinValue;
        endDate = filter.DateRangeTo ?? Calendar.SqlMaxValue;
      }

      query = Docflow.PublicFunctions.Module.OfficialDocumentsApplyFilterByDate(query, beginDate, endDate)
        .Cast<Sungero.Contracts.IContractualDocument>();
      
      return query;
    }
    
    /// <summary>
    /// Определить, нужно ли использовать предварительную фильтрацию для договорных документов.
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True, если нужно использовать предварительную фильтрацию.</returns>
    public virtual bool UsePrefilterContractualDocuments(Sungero.Contracts.FolderFilterState.IContractsListFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.Category != null ||
         filter.Contractor != null ||
         filter.Department != null ||
         filter.Last30days);
      return hasStrongFilter;
    }
    
    #endregion
    
    #region История договоров
    
    /// <summary>
    /// Отфильтровать историю договоров по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">История договоров для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованная история договоров.</returns>
    /// <remarks>Условия которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractsHistoryApplyStrongFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.Contracts.FolderFilterState.IContractsHistoryFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "Категория".
      if (filter.Category != null)
        query = query.Where(c => Equals(c.DocumentGroup, filter.Category));
      
      // Фильтр "Ответственный".
      if (filter.Responsible != null)
        query = query.Where(c => Equals(c.ResponsibleEmployee, filter.Responsible));
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать историю договоров по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">История договоров для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованная история договоров.</returns>
    /// <remarks>Условия которые используют индексы но не максимально оптимально.</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractsHistoryApplyOrdinaryFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.Contracts.FolderFilterState.IContractsHistoryFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      #region Фильтр по состоянию и датам
      
      DateTime beginPeriod = Calendar.SqlMinValue;
      DateTime endPeriod = Calendar.UserToday.EndOfDay().FromUserTime();
      
      if (filter.Last30days)
        beginPeriod = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-30));
      
      if (filter.Last365days)
        beginPeriod = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-365));
      
      if (filter.ManualPeriod)
      {
        if (filter.DateRangeFrom.HasValue)
          beginPeriod = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(filter.DateRangeFrom.Value);
        
        endPeriod = filter.DateRangeTo.HasValue ? filter.DateRangeTo.Value.EndOfDay().FromUserTime() : Calendar.SqlMaxValue;
      }
      
      Enumeration? operation = null;
      var lifeCycleStates = new List<Enumeration>();
      
      if (filter.Concluded)
      {
        operation = new Enumeration(ContractsUI.PublicConstants.Module.SetToActiveOperationName);
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Active);
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Closed);
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Terminated);
      }
      
      if (filter.Executed)
      {
        operation = new Enumeration(ContractsUI.PublicConstants.Module.SetToClosedOperationName);
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Closed);
      }
      
      if (filter.Terminated)
      {
        operation = new Enumeration(ContractsUI.PublicConstants.Module.SetToTerminatedOperationName);
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Terminated);
      }
      
      if (filter.Cancelled)
      {
        operation = new Enumeration(ContractsUI.PublicConstants.Module.SetToObsoleteOperationName);
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Obsolete);
      }
      
      // Использовать можно только один WhereDocumentHistory, т.к. это отдельный подзапрос (+join).
      query = query.Where(d => d.LifeCycleState.HasValue && lifeCycleStates.Contains(d.LifeCycleState.Value))
        .WhereDocumentHistory(h => h.Operation == operation && h.HistoryDate.Between(beginPeriod, endPeriod));
      
      #endregion
      
      // Фильтр "Вид документа".
      if (filter.DocumentKind != null)
        query = query.Where(c => Equals(c.DocumentKind, filter.DocumentKind));
      
      // Фильтр "Наша организация".
      if (filter.BusinessUnit != null)
        query = query.Where(c => Equals(c.BusinessUnit, filter.BusinessUnit));
      
      // Фильтр "Тип документа".
      if (filter.SupAgreements || filter.Contracts)
        query = query.Where(d => filter.Contracts && ContractBases.Is(d) || filter.SupAgreements && SupAgreements.Is(d));
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать историю договоров по неоптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">История договоров для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованная история договоров.</returns>
    /// <remarks>Условия которые могут выполняться долго (например те которые не могут использовать индексы).</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractsHistoryApplyWeakFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.Contracts.FolderFilterState.IContractsHistoryFilterState filter)
    {
      return query;
    }
    
    /// <summary>
    /// Определить нужно ли использовать предварительную фильтрацию для истории договоров.
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True если нужно использовать предварительную фильтрацию.</returns>
    public virtual bool UsePrefilterContractsHistory(Sungero.Contracts.FolderFilterState.IContractsHistoryFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.Category != null ||
         filter.Responsible != null);
      return hasStrongFilter;
    }
    
    /// <summary>
    /// Отфильтровать договоры на завершении по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Договорные документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договорные документы.</returns>
    /// <remarks>Условия, которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ExpiringSoonContractualDocumentsApplyStrongFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.Contracts.FolderFilterState.IExpiringSoonContractsFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "Категория".
      if (filter.Category != null)
        query = query.Where(c => ContractBases.Is(c) && Equals(c.DocumentGroup, filter.Category));
      
      // Фильтр "Контрагент".
      if (filter.Contractor != null)
        query = query.Where(c => Equals(c.Counterparty, filter.Contractor));
      
      // Фильтр "Ответственный".
      if (filter.Responsible != null)
        query = query.Where(c => Equals(c.ResponsibleEmployee, filter.Responsible));
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договорные на завершении по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">Договорные документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договорные документы.</returns>
    /// <remarks>Условия, которые используют индексы, но не максимально оптимально.</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ExpiringSoonContractualDocumentsApplyOrdinaryFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.Contracts.FolderFilterState.IExpiringSoonContractsFilterState filter)
    {
      if (query == null || filter == null)
        return query;
      
      // Фильтр "Вид документа".
      if (filter.DocumentKind != null)
        query = query.Where(d => Equals(d.DocumentKind, filter.DocumentKind));
      
      // Фильтр "Наша организация".
      if (filter.BusinessUnit != null)
        query = query.Where(d => Equals(d.BusinessUnit, filter.BusinessUnit));
      
      // Фильтр по типу документа
      if (filter.Contracts && !filter.SupAgreements)
        return query.Where(d => ContractBases.Is(d));
      if (filter.SupAgreements && !filter.Contracts)
        return query.Where(d => SupAgreements.Is(d));
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договоры на завершении по неоптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Договорные документы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договорные документы.</returns>
    /// <remarks>Условия, которые могут выполняться долго (например, те которые не могут использовать индексы).</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ExpiringSoonContractualDocumentsApplyWeakFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.Contracts.FolderFilterState.IExpiringSoonContractsFilterState filter)
    {
      return query;
    }
    
    /// <summary>
    /// Определить, нужно ли использовать предварительную фильтрацию для договоров на завершении.
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True, если нужно использовать предварительную фильтрацию.</returns>
    public virtual bool UsePrefilterExpiringSoonContractualDocuments(Sungero.Contracts.FolderFilterState.IExpiringSoonContractsFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.Category != null ||
         filter.Contractor != null ||
         filter.Responsible != null);
      return hasStrongFilter;
    }
    
    /// <summary>
    /// Отфильтровать действующие договоры, у которых осталось 14 (либо указанное в договоре число) дней до окончания документа.
    /// </summary>
    /// <param name="query">Договорные документы для фильтрации.</param>
    /// <returns>Отфильтрованные договорные документы.</returns>
    [Public]
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ExpiringSoonContractualDocumentsApplyInvariantFilter(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      var today = Calendar.UserToday;
      query = query.Where(d => d.LifeCycleState == Sungero.Docflow.OfficialDocument.LifeCycleState.Active)
        .Where(d => SupAgreements.Is(d) || ContractBases.Is(d) && (ContractBases.As(d).DaysToFinishWorks == null ||
                                                                   ContractBases.As(d).DaysToFinishWorks <= Docflow.PublicConstants.Module.MaxDaysToFinish))
        .Where(d => (ContractBases.Is(d) && today.AddDays(ContractBases.As(d).DaysToFinishWorks ?? 14) >= d.ValidTill) ||
               (SupAgreements.Is(d) && today.AddDays(14) >= d.ValidTill));
      
      return query;
    }
    
    #endregion
    
    #region Список "Документы у сотрудников"
    
    /// <summary>
    /// Отфильтровать список документов находящихся у сотрудников по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Список документов находящихся у сотрудников для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованный список документов.</returns>
    /// <remarks>Условия которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> IssuanceJournalApplyStrongFilter(IQueryable<Sungero.Docflow.IOfficialDocument> query, Sungero.Contracts.FolderFilterState.IIssuanceJournalFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр по делу.
      if (filter.File != null)
        query = query.Where(x => Equals(x.CaseFile, filter.File));
      
      // Фильтр по сотруднику.
      if (filter.Employee != null)
        query = query.Where(x => x.Tracking.Any(p => !p.ReturnDate.HasValue && Equals(p.DeliveredTo, filter.Employee)));
      
      // Фильтр по подразделению.
      if (filter.Department != null)
        query = query.Where(x => x.Tracking.Any(p => !p.ReturnDate.HasValue &&
                                                p.DeliveredTo != null &&
                                                Equals(p.DeliveredTo.Department, filter.Department)));
      
      // Фильтрация по сроку возврата.
      if (filter.EndDay)
        query = this.IssuanceJournalApplyFilterByDate(query, filter);
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать список документов находящихся у сотрудников по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">Список документов находящихся у сотрудников для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованный список документов.</returns>
    /// <remarks>Условия которые используют индексы но не максимально оптимально.</remarks>
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> IssuanceJournalApplyOrdinaryFilter(IQueryable<Sungero.Docflow.IOfficialDocument> query, Sungero.Contracts.FolderFilterState.IIssuanceJournalFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Ограничение по типу документа в списке.
      query = query.Where(x => (Sungero.Docflow.ContractualDocumentBases.Is(x) || Sungero.Docflow.AccountingDocumentBases.Is(x)));
      
      // Фильтр по виду документа.
      if (filter.DocumentKind != null)
        query = query.Where(x => Equals(x.DocumentKind, filter.DocumentKind));
      
      // Фильтрация по сроку возврата.
      if (filter.EndWeek || filter.EndMonth || filter.Manual)
        query = this.IssuanceJournalApplyFilterByDate(query, filter);
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать список документов находящихся у сотрудников по неоптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Список документов находящихся у сотрудников для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованный список документов.</returns>
    /// <remarks>Условия которые могут выполняться долго (например те которые не могут использовать индексы).</remarks>
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> IssuanceJournalApplyWeakFilter(IQueryable<Sungero.Docflow.IOfficialDocument> query, Sungero.Contracts.FolderFilterState.IIssuanceJournalFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Ограничение по признаку "Требуется возврат".
      query = query.Where(x => x.IsReturnRequired == true);
      
      // Фильтр по статусу.
      if (filter.Overdue)
        query = query
          .Where(x => x.Tracking.Any(d => (d.ReturnDate > d.ReturnDeadline) || (!d.ReturnDate.HasValue && d.ReturnDeadline < Calendar.UserToday)));
      
      // Фильтр по группе регистрации.
      if (filter.RegistrationGroup != null)
        query = query.Where(x => x.DocumentRegister != null && Equals(x.DocumentRegister.RegistrationGroup, filter.RegistrationGroup));
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать список документов находящихся у сотрудников по сроку возврата.
    /// </summary>
    /// <param name="query">Список документов находящихся у сотрудников для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованный список документов.</returns>
    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> IssuanceJournalApplyFilterByDate(IQueryable<Sungero.Docflow.IOfficialDocument> query, Sungero.Contracts.FolderFilterState.IIssuanceJournalFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      var today = Calendar.UserToday;
      
      // Исключить строки из таблиц Выдачи с Результатом возврата: "Возвращен".
      var returned = Docflow.OfficialDocumentTracking.ReturnResult.Returned;
      
      // Фильтр по сроку возврата.
      if (filter.EndDay)
        query = query.Where(x => x.Tracking.Any(p => !Equals(p.ReturnResult, returned) &&
                                                p.ReturnDeadline < today.AddDays(1)));

      if (filter.EndWeek)
        query = query.Where(x => x.Tracking.Any(p => !Equals(p.ReturnResult, returned) &&
                                                p.ReturnDeadline <= today.EndOfWeek()));
      
      if (filter.EndMonth)
        query = query.Where(x => x.Tracking.Any(p => !Equals(p.ReturnResult, returned) &&
                                                p.ReturnDeadline <= today.EndOfMonth()));
      
      if (filter.Manual)
      {
        var dateFrom = filter.ReturnPeriodDataRangeFrom;
        var dateTo = filter.ReturnPeriodDataRangeTo;
        
        if (dateFrom.HasValue && !dateTo.HasValue)
          query = query.Where(x => x.Tracking.Any(p => !Equals(p.ReturnResult, returned) && p.ReturnDeadline >= dateFrom.Value));
        
        if (dateTo.HasValue && !dateFrom.HasValue)
          query = query.Where(x => x.Tracking.Any(p => !Equals(p.ReturnResult, returned) && p.ReturnDeadline <= dateTo.Value));
        
        if (dateFrom.HasValue && dateTo.HasValue)
          query = query.Where(x => x.Tracking.Any(p => !Equals(p.ReturnResult, returned) &&
                                                  p.ReturnDeadline.Between(dateFrom.Value, dateTo.Value)));
      }
      
      return query;
    }
    
    /// <summary>
    /// Определить нужно ли использовать предварительную фильтрацию для списка документов находящихся у сотрудников.
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True если нужно использовать предварительную фильтрацию.</returns>
    public virtual bool UsePrefilterIssuanceJournal(Sungero.Contracts.FolderFilterState.IIssuanceJournalFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.File != null ||
         filter.Employee != null ||
         filter.Department != null ||
         filter.EndDay);
      return hasStrongFilter;
    }
    
    #endregion
    
    #endregion
  }
}