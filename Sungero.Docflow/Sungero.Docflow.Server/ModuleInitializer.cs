using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CommonLibrary;
using Sungero.Commons;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalStage;
using Sungero.Docflow.DocumentKind;
using Sungero.Domain;
using Sungero.Domain.Initialization;
using Sungero.Domain.LinqExpressions;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using Sungero.Workflow;
using AppMonitoringType = Sungero.Content.AssociatedApplication.MonitoringType;
using Init = Sungero.Docflow.Constants.Module.Initialize;

namespace Sungero.Docflow.Server
{
  public partial class ModuleInitializer
  {
    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.FirstInitializing, Constants.Module.Init.Docflow.Name, Version.Parse(Constants.Module.Init.Docflow.FirstInitVersion));
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.Initializing49, Constants.Module.Init.Docflow.Name, Version.Parse(Constants.Module.Init.Docflow.Version49));
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.Initializing410, Constants.Module.Init.Docflow.Name, Version.Parse(Constants.Module.Init.Docflow.Version410));
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.Initializing411, Constants.Module.Init.Docflow.Name, Version.Parse(Constants.Module.Init.Docflow.Version411));
    }
    
    /// <summary>
    /// Начальная инициализация модуля после установки.
    /// </summary>
    public virtual void FirstInitializing()
    {
      // Выдача прав всем пользователям.
      var allUsers = Roles.AllUsers;
      if (allUsers != null)
      {
        // Справочники.
        InitializationLogger.Debug("Init: Grant rights on databooks to all users.");
        GrantRightsOnDatabooks(allUsers);
        
        // Документы.
        InitializationLogger.Debug("Init: Grant rights on documents to all users.");
        GrantRightsOnDocuments(allUsers);
        
        // Задачи.
        InitializationLogger.Debug("Init: Grant rights on tasks to all users.");
        GrantRightsOnTasks(allUsers);
        
        // Спец.папки.
        InitializationLogger.Debug("Init: Grant rights on special folders to all users.");
        GrantRightsOnFolders(allUsers);
        
        // Отчеты.
        InitializationLogger.Debug("Init: Grant right on reports to all users.");
        Reports.AccessRights.Grant(Reports.GetApprovalRulesConsolidatedReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
        Reports.AccessRights.Grant(Reports.GetRegistrationSettingReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
        Reports.AccessRights.Grant(Reports.GetEmployeeAssignmentsReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
        Reports.AccessRights.Grant(Reports.GetEnvelopeE65Report().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
        Reports.AccessRights.Grant(Reports.GetEnvelopeC4Report().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
        Reports.AccessRights.Grant(Reports.GetEnvelopeC5Report().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
        Reports.AccessRights.Grant(Reports.GetEnvelopeC65Report().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
        Reports.AccessRights.Grant(Reports.GetMailRegisterReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
        Reports.AccessRights.Grant(Reports.GetExchangeServiceDocumentReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
      }
      
      // Создание типов файлов и приложений обработчиков.
      InitializationLogger.Debug("Init: Create file types and associated apps.");
      CreateFileTypesAndAssociatedApps();
      
      // Создание типа прав для видов документов.
      if (!AccessRightsTypes.GetAll().Where(x => Equals(x.EntityTypeGuid, DocumentKind.ClassTypeGuid) && Equals(x.Sid, Constants.DocumentKind.DocumentKindChoiseAccessRightType)).Any())
        CreateDocumentKindAccessRights();
      
      // Создание типов прав модуля.
      InitializationLogger.Debug("Init: Create access rights.");
      CreateDocumentAccessRights();
      CreateFolderChangeAccessRights();
      
      InitializationLogger.Debug("Init: Update system access rights.");
      UpdateSystemAccessRights();
      
      // Создание ролей.
      InitializationLogger.Debug("Init: Create roles.");
      CreateRoles();
      
      // Выдача прав на отчеты.
      InitializationLogger.Debug("Init: Grant right on reports.");
      GrantRightsOnReports();

      // Выдача прав роли "Делопроизводители".
      InitializationLogger.Debug("Init: Grant right on registration for clerks.");
      GrantRightsToClerksRole();
      
      // Выдача прав роли "Ответственные за настройку регистрации".
      InitializationLogger.Debug("Init: Grant right on registration for registration managers.");
      GrantRightToRegistrationManagersRole();
      
      // Выдача прав роли "Ответственные за контрагентов".
      InitializationLogger.Debug("Init: Grant right on counterparties for counterparties responsible.");
      GrantRightsToCounterpartiesResponsibleRole();
      
      // Выдача прав роли "Ответственные за внесение данных по документам, удостоверяющим личность".
      InitializationLogger.Debug("Init: Grant right on persons to responsible role for updating data on identity documents.");
      GrantRightsToIdentificationDocumentsManagersRole();
      
      // Выдача прав роли "Подписывающие".
      InitializationLogger.Debug("Init: Grant right on endorsement operation to signing role.");
      GrantRightsToSigningRole();
      
      // Назначить права роли "Пользователи с правами на удаление документов".
      InitializationLogger.Debug("Init: Grant rights on delete documents operation to role");
      GrantRightsToDocumentDeleteRole();
      
      // Назначить права роли "Руководители проектов".
      InitializationLogger.Debug("Init: Grant rights on projects");
      GrantRightsOnProjects();
      
      CreateDocumentTypes();
      CreateDocumentSendActions();
      CreateDocumentKinds();
      CreateMailDeliveryMethods();
      CreateDefaultApprovalRoles();
      CreateDefaultApprovalRules();
      CreateApprovalConvertPdfStage();
      CreateApprovalRegistrationFtsStage();
      CreateApprovalReviewTaskStage();
      CreateDocumentRegistersAndSettings();
      CreateDefaultCurrencies();
      CreateDefaultVatRates();
      CreateDefaultRelationTypes();
      CreateDocumentRegisterNumberTable();
      CreateParametersTable();
      CreateAssignmentIndices();
      CreateEDocIndices();
      CreateOfficialDocumentTrackingIndices();
      CreateTaskIndices();
      InitializeExchangeServiceUsersRole();
      CreateReportsTables();
      
      InitializationLogger.Debug("Init: Create or update Sungero_Docflow_Params.");
      AddCompanyDataServiceParam();
      AddDisableMailNotificationParam();
      AddSummaryMailNotificationsBunchCountParam();
      AddSummaryMailEmployeesBunchSizeParam();
      AddMailAssignmentsBunchSizeParam();
      AddAccessRightsRuleProcessingBatchSizeParam();
      AddDaysToStoreDocumentComparisonInfoParam();
      AddAccessRightsBulkProcessingParams();
      AddProcessMessagesRetriesCountParam();
      AddProcessHistoricalMessagesRetriesCountParam();
      AddConvertExchangeDocumentToPdfRetriesCountParam();
      AddHistoricalMessageQueueItemLimitParam();
      AddIndexDocumentsForFullTextSearchJobQueueItemsLimitParam();
      AddIndexingDocumentsForFullTextSearchRetriesLimitParam();
      AddBulkIndexingDocumentsLimitParam();
      AddFPoAQueueItemBatchSizeParam();
      AddFPoAGetOperationRetriesMaxCountParam();
      AddSearchFPoAInFtsRegistryTemplateParam();
      AddFPoAIdentityDocumentCodesParam();
      AddSubstitutionClosingDaysCountParam();
      AddBulkTransferAccesRightsButchSizeParam();
      
      InitializationLogger.Debug("Init: Create smart processing settings.");
      CreateSmartProcessingSettings();
      
      InitializationLogger.Debug("Init: Fill smart processing rules.");
      this.FillSmartProcessingRules();
      
      InitializationLogger.Debug("Init: Convert document templates.");
      ConvertDocumentTemplates();
    }
    
    public virtual void Initializing49()
    {
      // Код инициализации для версии 4.9.0
    }
    
    public virtual void Initializing410()
    {
      CreateReportsTables410();
      InitSendForDocumentFlowAction();
    }
    
    public virtual void Initializing411()
    {
      var allUsers = Roles.AllUsers;
      GrantRightsOnMediumTypes(allUsers);
      CreateMediumTypes();
      FillDocumentKindsMediumTypes();
      
      CreateRole(Docflow.Resources.RoleNameManagerInterfaceUsers, Docflow.Resources.DescriptionManagerInterfaceUsersRole, Constants.Module.RoleGuid.ManagerInterfaceUsersRole);
      
      GrantRightsOnMarksAndMarkKinds(allUsers);
      CreateDefaultMarkKinds();
      CreateMarkIndices();
      AddUseObsoletePdfConversionParam();
    }
    
    #region Выдача прав на спец. папки
    
    /// <summary>
    /// Выдать права на спец. папки модуля.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightsOnFolders(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant right on contracts special folders to all users.");

      Sungero.Docflow.SpecialFolders.ApprovalRules.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Docflow.SpecialFolders.StoragePolicy.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Parties.SpecialFolders.BlockedCounterparties.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Parties.SpecialFolders.InvitedCounterparties.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Company.SpecialFolders.ManagersAssistants.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Docflow.SpecialFolders.PowerOfAttorneyList.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      
      Sungero.Docflow.SpecialFolders.ApprovalRules.AccessRights.Save();
      Sungero.Docflow.SpecialFolders.StoragePolicy.AccessRights.Save();
      Sungero.Parties.SpecialFolders.BlockedCounterparties.AccessRights.Save();
      Sungero.Parties.SpecialFolders.InvitedCounterparties.AccessRights.Save();
      Sungero.Company.SpecialFolders.ManagersAssistants.AccessRights.Save();
      Sungero.Docflow.SpecialFolders.PowerOfAttorneyList.AccessRights.Save();
    }
    
    #endregion
    
    #region Выдача прав Всем пользователям
    
    /// <summary>
    /// Выдать права всем пользователям на справочники.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightsOnDatabooks(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on databooks to all users.");
      
      // Системные сущности.
      CoreEntities.AccessRightsTypes.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      CoreEntities.Roles.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      CoreEntities.WorkingTime.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      CoreEntities.RelationTypes.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      CoreEntities.Certificates.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      CoreEntities.Substitutions.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      CoreEntities.TimeZones.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      CoreEntities.AccessRightsTypes.AccessRights.Save();
      CoreEntities.Roles.AccessRights.Save();
      CoreEntities.WorkingTime.AccessRights.Save();
      CoreEntities.RelationTypes.AccessRights.Save();
      CoreEntities.Certificates.AccessRights.Save();
      CoreEntities.Substitutions.AccessRights.Save();
      CoreEntities.TimeZones.AccessRights.Save();

      // Контент.
      Content.AssociatedApplications.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Content.ElectronicDocumentTemplates.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Content.FilesTypes.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Content.AssociatedApplications.AccessRights.Save();
      Content.ElectronicDocumentTemplates.AccessRights.Save();
      Content.FilesTypes.AccessRights.Save();

      // Модуль "Общие справочники".
      Commons.Countries.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Commons.Regions.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Commons.Cities.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Commons.Currencies.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Commons.VatRates.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Commons.Countries.AccessRights.Save();
      Commons.Regions.AccessRights.Save();
      Commons.Cities.AccessRights.Save();
      Commons.Currencies.AccessRights.Save();
      Commons.VatRates.AccessRights.Save();
      
      // Модуль "Контрагенты".
      Parties.Counterparties.AccessRights.Revoke(allUsers, DefaultAccessRightsTypes.Read);
      Parties.Counterparties.AccessRights.Save();
      Parties.Counterparties.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Parties.Contacts.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Parties.DueDiligenceWebsites.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Parties.Counterparties.AccessRights.Save();
      Parties.Contacts.AccessRights.Save();
      Parties.DueDiligenceWebsites.AccessRights.Save();
      Parties.PublicInitializationFunctions.Module.GrantRightsOnCounterpartyKind(allUsers);
      Parties.IdentityDocumentKinds.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Parties.IdentityDocumentKinds.AccessRights.Save();
      
      // Модуль "Компания".
      Company.JobTitles.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Company.Departments.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Company.Employees.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Company.BusinessUnits.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Company.ManagersAssistants.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Company.VisibilitySettings.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Company.JobTitles.AccessRights.Save();
      Company.Departments.AccessRights.Save();
      Company.Employees.AccessRights.Save();
      Company.BusinessUnits.AccessRights.Save();
      Company.ManagersAssistants.AccessRights.Save();
      Company.VisibilitySettings.AccessRights.Save();
      
      // Модуль "Документооборот".
      Docflow.CaseFiles.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.FileRetentionPeriods.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.DocumentRegisters.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.RegistrationGroups.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.DocumentKinds.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.DocumentTypes.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.DocumentGroupBases.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.MailDeliveryMethods.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.PersonalSettings.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Change);
      Docflow.ApprovalRuleBases.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.ApprovalStageBases.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.RegistrationSettings.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.ConditionBases.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.SignatureSettings.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.ApprovalRoleBases.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.AccessRightsRules.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.DistributionLists.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      Docflow.StoragePolicyBases.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.StampSettings.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      GrantRightsOnTopic(allUsers);
      
      Docflow.CaseFiles.AccessRights.Save();
      Docflow.FileRetentionPeriods.AccessRights.Save();
      Docflow.DocumentRegisters.AccessRights.Save();
      Docflow.RegistrationGroups.AccessRights.Save();
      Docflow.DocumentKinds.AccessRights.Save();
      Docflow.DocumentTypes.AccessRights.Save();
      Docflow.DocumentGroupBases.AccessRights.Save();
      Docflow.MailDeliveryMethods.AccessRights.Save();
      Docflow.PersonalSettings.AccessRights.Save();
      Docflow.ApprovalRuleBases.AccessRights.Save();
      Docflow.ApprovalStageBases.AccessRights.Save();
      Docflow.RegistrationSettings.AccessRights.Save();
      Docflow.ConditionBases.AccessRights.Save();
      Docflow.SignatureSettings.AccessRights.Save();
      Docflow.ApprovalRoleBases.AccessRights.Save();
      Docflow.AccessRightsRules.AccessRights.Save();
      Docflow.DistributionLists.AccessRights.Save();
      Docflow.StoragePolicyBases.AccessRights.Save();
      Docflow.StampSettings.AccessRights.Save();
      
      // Интеллектуальная обработка.
      Docflow.SmartProcessingSettings.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.SmartProcessingSettings.AccessRights.Save();
      
      // Сравнение версий.
      Docflow.DocumentComparisonInfos.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      Docflow.DocumentComparisonInfos.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права группе пользователей на рубрики.
    /// </summary>
    /// <param name="users">Группа пользователей.</param>
    public static void GrantRightsOnTopic(IRole users)
    {
      // Права на тип выдаются один раз, инициализация не перевыдает.
      var hasGrantedAccessRights = Docflow.PublicFunctions.Module.HasGrantedAccessRights(Topic.ClassTypeGuid);
      if (!hasGrantedAccessRights)
      {
        Docflow.Topics.AccessRights.Grant(users, DefaultAccessRightsTypes.Read);
        Docflow.Topics.AccessRights.Save();
      }
    }
    
    /// <summary>
    /// Выдать права группе пользователей на виды носителей документа.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightsOnMediumTypes(IRole allUsers)
    {
      Docflow.MediumTypes.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.MediumTypes.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права группе пользователей на отметки документа и их виды.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightsOnMarksAndMarkKinds(IRole allUsers)
    {
      Docflow.MarkKinds.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Docflow.MarkKinds.AccessRights.Save();
      Docflow.Marks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.FullAccess);
      Docflow.Marks.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права всем пользователям на документы.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightsOnDocuments(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on documents to all users.");
      
      Docflow.SimpleDocuments.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      Docflow.MinutesBases.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      Docflow.Memos.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      Docflow.Addendums.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      Docflow.PowerOfAttorneys.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      Docflow.PowerOfAttorneyRevocations.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      Docflow.CounterpartyDocuments.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      Docflow.SimpleDocuments.AccessRights.Save();
      Docflow.MinutesBases.AccessRights.Save();
      Docflow.Memos.AccessRights.Save();
      Docflow.Addendums.AccessRights.Save();
      Docflow.PowerOfAttorneys.AccessRights.Save();
      Docflow.PowerOfAttorneyRevocations.AccessRights.Save();
      Docflow.CounterpartyDocuments.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права всем пользователям на задачи.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightsOnTasks(IRole allUsers)
    {
      // Задачи платформы.
      Workflow.SimpleTasks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      Workflow.SimpleTasks.AccessRights.Save();

      // Задачи модуля "Документооборот".
      Docflow.ApprovalTasks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      Docflow.FreeApprovalTasks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      Docflow.CheckReturnTasks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      Docflow.DeadlineExtensionTasks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      Docflow.ApprovalTasks.AccessRights.Save();
      Docflow.FreeApprovalTasks.AccessRights.Save();
      Docflow.CheckReturnTasks.AccessRights.Save();
      Docflow.DeadlineExtensionTasks.AccessRights.Save();
    }
    
    #endregion
    
    #region Выдача прав на операции обмена для документов
    
    /// <summary>
    /// Инициализация роли "Пользователи с правами на работу через сервис обмена".
    /// </summary>
    public static void InitializeExchangeServiceUsersRole()
    {
      InitializationLogger.Debug("Init: Initialize Exchange Service Users role");
      
      var exchangeServiceUsersRole = ExchangeCore.PublicFunctions.Module.GetExchangeServiceUsersRole();
      if (exchangeServiceUsersRole == null)
      {
        InitializationLogger.Debug("Init: No service users role found");
        return;
      }
      
      Docflow.OfficialDocuments.AccessRights.Grant(exchangeServiceUsersRole, Constants.Module.DefaultAccessRightsTypeSid.SendByExchange);
      Docflow.OfficialDocuments.AccessRights.Save();
    }
    
    #endregion
    
    #region Создание типов прав модуля. Изменение системных прав
    
    public static void UpdateSystemAccessRights()
    {
      Sungero.CoreEntities.Server.LocalizationUpdater.UpdateLocalizedData(TenantInfo.Culture);
    }
    
    [Public]
    public static void CreateFolderChangeAccessRights()
    {
      InitializationLogger.Debug("Init: Create access rights for folder");
      CreateAccessRightsForFolder(Constants.Module.FolderTypeGuid);
    }
    
    public static void CreateAccessRightsForFolder(string entityTypeGuid)
    {
      // Создать тип прав "Изменение содержимого папки".
      var mask = FolderOperations.Read ^ FolderOperations.GetFolderContent ^ FolderOperations.ChangeFolderContent ^ FolderOperations.DelegateAccess;
      CreateAccessRightsType(entityTypeGuid, Resources.FolderChangeRightTypeName.ToString(), mask, mask, CoreEntities.AccessRightsType.AccessRightsTypeArea.Both,
                             Constants.Module.DefaultAccessRightsTypeSid.ChangeContent, false);
    }
    
    /// <summary>
    /// Создать типы прав для регистрации и работы с документами.
    /// </summary>
    public static void CreateDocumentAccessRights()
    {
      InitializationLogger.Debug("Init: Create access rights for document type OfficialDocument");
      CreateAccessRightsForDocumentType(OfficialDocument.ClassTypeGuid);
      
      // Создать тип прав "Импорт электронной доверенности".
      var mask = OfficialDocumentOperations.Create ^ OfficialDocumentOperations.Approve;
      CreateAccessRightsType(FormalizedPowerOfAttorney.ClassTypeGuid.ToString(),
                             Resources.ImportFPoAAccessRightsTypeName.ToString(),
                             mask, mask,
                             CoreEntities.AccessRightsType.AccessRightsTypeArea.Type,
                             Constants.Module.DefaultAccessRightsTypeSid.ImportFormalizedPowerOfAttorney,
                             false);
    }

    /// <summary>
    /// Создать права для типа документа.
    /// </summary>
    /// <param name="entityTypeGuid">Guid типа документа.</param>
    public static void CreateAccessRightsForDocumentType(Guid entityTypeGuid)
    {
      // Создать тип прав "Регистрация".
      CreateAccessRightsType(entityTypeGuid.ToString(), Resources.RegistrationRightTypeName.ToString(), OfficialDocumentOperations.Register,
                             OfficialDocumentOperations.Register, CoreEntities.AccessRightsType.AccessRightsTypeArea.Type,
                             Constants.Module.DefaultAccessRightsTypeSid.Register, false, Docflow.Resources.RegistrationRightsTypeDescription);
      
      // Создать тип прав "Удаление".
      CreateAccessRightsType(entityTypeGuid.ToString(), Resources.DeleteRightTypeName.ToString(), OfficialDocumentOperations.Delete,
                             OfficialDocumentOperations.Delete, CoreEntities.AccessRightsType.AccessRightsTypeArea.Type,
                             Constants.Module.DefaultAccessRightsTypeSid.Delete, false);
      
      // Создать тип прав "Отправка через сервис обмена".
      CreateAccessRightsType(entityTypeGuid.ToString(), Resources.SendByExchangeRightTypeName.ToString(), OfficialDocumentOperations.SendByExchange,
                             OfficialDocumentOperations.SendByExchange, CoreEntities.AccessRightsType.AccessRightsTypeArea.Type,
                             Constants.Module.DefaultAccessRightsTypeSid.SendByExchange, false);
      
      // Создать переопределенный тип прав "Полный доступ".
      var mask = OfficialDocumentOperations.Full ^ OfficialDocumentOperations.Approve ^
        OfficialDocumentOperations.Register ^ OfficialDocumentOperations.Delete ^ OfficialDocumentOperations.SendByExchange;
      CreateAccessRightsType(entityTypeGuid.ToString(), Resources.FullRightTypeName.ToString(), mask,
                             mask, CoreEntities.AccessRightsType.AccessRightsTypeArea.Both,
                             DefaultAccessRightsTypes.FullAccess, true);
    }

    /// <summary>
    /// Создать типы прав для работы с видами документов.
    /// </summary>
    public static void CreateDocumentKindAccessRights()
    {
      InitializationLogger.Debug("Init: Create access rights for document kind");
      CreateAccessRightsType(DocumentKind.ClassTypeGuid.ToString(), Resources.SelectInDocumentRightTypeName.ToString(), DocumentKindOperations.SelectInDocument,
                             DocumentKindOperations.SelectInDocument, CoreEntities.AccessRightsType.AccessRightsTypeArea.Both,
                             Constants.DocumentKind.DocumentKindChoiseAccessRightType, false);
    }
    
    /// <summary>
    /// Создать тип прав.
    /// </summary>
    /// <param name="entityTypeGuid">Guid типа сущности.</param>
    /// <param name="operationName">Имя операции.</param>
    /// <param name="operationSet">Набор операций типа прав.</param>
    /// <param name="grantedMask">Маска разрешенных операций.</param>
    /// <param name="area">Область типа прав: экземпляр, тип или оба.</param>
    /// <param name="sid">Идентификатор типа прав.</param>
    /// <param name="isOverride">Признак того, является ли тип прав переопределением базового.</param>
    /// <param name="description">Описание.</param>
    [Public]
    public static void CreateAccessRightsType(string entityTypeGuid, string operationName, int operationSet,
                                              int grantedMask, Enumeration area, Guid sid, bool isOverride, string description = "")
    {
      InitializationLogger.DebugFormat("Init: Create access rights type {0} from {1}", operationName, entityTypeGuid);
      
      var queryFormat = Queries.Module.CreateAccessRightsType;
      // Параметры:
      // {0} - Имя операции.
      // {1} - Sid операции.
      // {2} - GUID типа сущности.
      // {3} - Набор операций типа прав.
      // {4} - Маска разрешенных операций.
      // {5} - Область типа прав: экземпляр, тип или оба.
      // {6} - Описание.
      // {7} - Признак того, является ли тип прав переопределением базового.
      var command = string.Format(queryFormat, operationName, sid.ToString(), entityTypeGuid, operationSet, grantedMask, area.Value, description, isOverride ? 1 : 0);
      Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
      Sungero.Domain.Security.AccessRightsBase.ClearCachedAccessRightsTypes(new Guid(entityTypeGuid));
    }
    
    #endregion
    
    #region Создание ролей и выдачи им прав
    
    /// <summary>
    /// Создать предопределенные роли.
    /// </summary>
    public static void CreateRoles()
    {
      InitializationLogger.Debug("Init: Create Default Roles");
      
      CreateRole(Docflow.Resources.RoleNameSigning, Docflow.Resources.DescriptionSignatoryRole, Constants.Module.RoleGuid.SigningRole);
      CreateRole(Docflow.Resources.RoleNameClerks, Docflow.Resources.DescriptionClerkRole, Constants.Module.RoleGuid.ClerksRole);
      CreateRole(Docflow.Resources.RoleNameRegistrationManagers, Docflow.Resources.DescriptionRegistrationManagerRole, Constants.Module.RoleGuid.RegistrationManagersRole);
      CreateRole(Docflow.Resources.RoleNameBusinessUnitHeads, Docflow.Resources.DescriptionBusinessUnitManagerRole, Constants.Module.RoleGuid.BusinessUnitHeadsRole);
      CreateRole(Docflow.Resources.RoleNameDepartmentManagers, Docflow.Resources.DescriptionDepartmentManagerRole, Constants.Module.RoleGuid.DepartmentManagersRole);
      CreateRole(Docflow.Resources.RoleNameCounterpartiesResponsible, Docflow.Resources.DescriptionResponsibleForCounterpartiesRole, Constants.Module.RoleGuid.CounterpartiesResponsibleRole);
      CreateRole(Docflow.Resources.RoleNameDocumentDelete, Docflow.Resources.DescriptionDocumentDeleteRole, Constants.Module.RoleGuid.DocumentDeleteRole);
      CreateRole(Docflow.Resources.RoleNameRegistrationIncomingDocument, Docflow.Resources.DescriptionIncomingDocumentsRegistrarRole, Constants.Module.RoleGuid.RegistrationIncomingDocument);
      CreateRole(Docflow.Resources.RoleNameRegistrationOutgoingDocument, Docflow.Resources.DescriptionOutgoingDocumentsRegistrarRole, Constants.Module.RoleGuid.RegistrationOutgoingDocument);
      CreateRole(Docflow.Resources.RoleNameRegistrationInternalDocument, Docflow.Resources.DescriptionInternalDocumentsRegistrarRole, Constants.Module.RoleGuid.RegistrationInternalDocument);
      CreateRole(Docflow.Resources.RoleNameProjectManagers, Docflow.Resources.DescriptionProjectManagersRole, Constants.Module.RoleGuid.ProjectManagersRoleGuid);
      CreateRole(Docflow.Resources.RoleNameUsersWithAssignmentCompletionRights, Docflow.Resources.DescriptionUsersWithAssignmentCompletionRightsRole,
                 Constants.Module.RoleGuid.UsersWithAssignmentCompletionRightsRole);
      CreateRole(Docflow.Resources.RoleNameIdentificationDocumentsManagers, Docflow.Resources.DescriptionIdentificationDocumentsManagersRole,
                 Constants.Module.RoleGuid.IdentificationDocumentsManagersRole);
      CreateRole(Docflow.Resources.RoleNameSystemSetupResponsible, Docflow.Resources.DescriptionSystemSetupResponsibleRole, Constants.Module.RoleGuid.SystemSetupResponsibleRole);
    }
    
    /// <summary>
    /// Создать роль.
    /// </summary>
    /// <param name="roleName">Название роли.</param>
    /// <param name="roleDescription">Описание роли.</param>
    /// <param name="roleGuid">Guid роли. Игнорирует имя.</param>
    /// <returns>Новая роль.</returns>
    [Public]
    public static IRole CreateRole(string roleName, string roleDescription, Guid roleGuid)
    {
      return CreateRole(roleName, roleDescription, roleGuid, new List<IRecipient>());
    }
    
    /// <summary>
    /// Создать роль.
    /// </summary>
    /// <param name="roleName">Название роли.</param>
    /// <param name="roleDescription">Описание роли.</param>
    /// <param name="roleGuid">Guid роли. Игнорирует имя.</param>
    /// <param name="recipients">Состав участников роли.</param>
    /// <returns>Новая роль.</returns>
    [Public]
    public static IRole CreateRole(string roleName, string roleDescription, Guid roleGuid, List<IRecipient> recipients)
    {
      InitializationLogger.DebugFormat("Init: Create Role {0}", roleName);
      var role = Roles.GetAll(r => r.Sid == roleGuid).FirstOrDefault();
      
      if (role == null)
      {
        role = Roles.Create();
        role.Name = roleName;
        role.Description = roleDescription;
        role.Sid = roleGuid;
        role.IsSystem = true;
        
        if (recipients.Any())
          AddRecipientsToRole(role, recipients);
        
        role.Save();
      }
      else
      {
        if (role.Name != roleName)
        {
          InitializationLogger.DebugFormat("Role '{0}'(Sid = {1}) renamed as '{2}'", role.Name, role.Sid, roleName);
          role.Name = roleName;
          role.Save();
        }
        if (role.Description != roleDescription)
        {
          InitializationLogger.DebugFormat("Role '{0}'(Sid = {1}) update Description '{2}'", role.Name, role.Sid, roleDescription);
          role.Description = roleDescription;
          role.Save();
        }
      }
      
      // Платформа требует наличие ExternalLink у роли, чтобы ее можно было указать в поле "Кто видит" при настройке обложек модулей.
      var externalLink = Docflow.PublicFunctions.Module.GetExternalLink(Sungero.CoreEntities.Server.Role.ClassTypeGuid, roleGuid);
      if (externalLink == null)
        Docflow.PublicFunctions.Module.CreateExternalLink(role, roleGuid);
      
      return role;
    }
    
    /// <summary>
    /// Добавить участников в указанную роль.
    /// </summary>
    /// <param name="role">Роль.</param>
    /// <param name="recipients">Список участников.</param>
    [Public]
    public static void AddRecipientsToRole(IRole role, List<IRecipient> recipients)
    {
      foreach (var recipient in recipients)
      {
        if (!recipient.IncludedIn(role))
          role.RecipientLinks.AddNew().Member = recipient;
      }
    }
    
    /// <summary>
    /// Выдать права на создание проектов для роли "Руководители проектов".
    /// </summary>
    public static void GrantRightsOnProjects()
    {
      var role = GetProjectManagersRole();
      if (role == null)
        return;

      Docflow.ProjectBases.AccessRights.Grant(role, DefaultAccessRightsTypes.Create);
      Docflow.ProjectBases.AccessRights.Save();
    }
    
    /// <summary>
    /// Получить роль "Руководители проектов".
    /// </summary>
    /// <returns>Роль "Руководители проектов".</returns>
    [Public]
    public static IRole GetProjectManagersRole()
    {
      return Roles.GetAll(g => g.Sid == Constants.Module.RoleGuid.ProjectManagersRoleGuid).FirstOrDefault();
    }
    
    /// <summary>
    /// Получить роль "Ответственные за контрагентов".
    /// </summary>
    /// <returns>Роль "Ответственные за контрагентов".</returns>
    [Public]
    public static IRole GetCounterpartyResponsibleRole()
    {
      return Roles.GetAll(r => r.Sid == Constants.Module.RoleGuid.CounterpartiesResponsibleRole).FirstOrDefault();
    }

    /// <summary>
    /// Назначить права роли "Пользователи с правами на удаление документов".
    /// </summary>
    public static void GrantRightsToDocumentDeleteRole()
    {
      InitializationLogger.Debug("Init: Grant rights on delete documents operation to role");
      
      var role = Roles.GetAll().SingleOrDefault(n => n.Sid == Constants.Module.RoleGuid.DocumentDeleteRole);
      if (role == null)
        return;
      
      Docflow.OfficialDocuments.AccessRights.Grant(role, Constants.Module.DefaultAccessRightsTypeSid.Delete);
      Docflow.OfficialDocuments.AccessRights.Save();
    }
    
    /// <summary>
    /// Назначить права роли "Делопроизводители".
    /// </summary>
    public static void GrantRightsToClerksRole()
    {
      InitializationLogger.Debug("Init: Grant rights on documents and databooks to clerks");
      
      var clerks = Functions.DocumentRegister.GetClerks();
      if (clerks == null)
        return;
      
      // Модуль "Документооборот".
      Docflow.CaseFiles.AccessRights.Grant(clerks, DefaultAccessRightsTypes.Read);
      Docflow.FileRetentionPeriods.AccessRights.Grant(clerks, DefaultAccessRightsTypes.Read);
      Docflow.MailDeliveryMethods.AccessRights.Grant(clerks, DefaultAccessRightsTypes.Read);
      Docflow.CaseFiles.AccessRights.Save();
      Docflow.FileRetentionPeriods.AccessRights.Save();
      Docflow.MailDeliveryMethods.AccessRights.Save();
      
      Reports.AccessRights.Grant(Reports.GetSkippedNumbersReport().Info, clerks, DefaultReportAccessRightsTypes.Execute);
    }
    
    /// <summary>
    /// Назначить права роли "Ответственные за контрагентов".
    /// </summary>
    public static void GrantRightsToCounterpartiesResponsibleRole()
    {
      InitializationLogger.Debug("Init: Grant rights on counterparties to responsible role.");
      
      var counterpartiesResponsible = Roles.GetAll().SingleOrDefault(n => n.Sid == Constants.Module.RoleGuid.CounterpartiesResponsibleRole);
      if (counterpartiesResponsible == null)
        return;
      
      // Модуль "Общие справочники".
      Commons.Countries.AccessRights.Grant(counterpartiesResponsible, DefaultAccessRightsTypes.Change);
      Commons.Regions.AccessRights.Grant(counterpartiesResponsible, DefaultAccessRightsTypes.Change);
      Commons.Cities.AccessRights.Grant(counterpartiesResponsible, DefaultAccessRightsTypes.Change);
      Commons.Countries.AccessRights.Save();
      Commons.Regions.AccessRights.Save();
      Commons.Cities.AccessRights.Save();
      
      // Модуль "Контрагенты".
      Parties.Counterparties.AccessRights.Grant(counterpartiesResponsible, DefaultAccessRightsTypes.FullAccess);
      Parties.Contacts.AccessRights.Grant(counterpartiesResponsible, DefaultAccessRightsTypes.FullAccess);
      Parties.Counterparties.AccessRights.Save();
      Parties.Contacts.AccessRights.Save();
    }
    
    /// <summary>
    /// Назначить права роли "Ответственные за внесение данных по документам, удостоверяющим личность".
    /// </summary>
    public static void GrantRightsToIdentificationDocumentsManagersRole()
    {
      InitializationLogger.Debug("Init: Grant rights on persons to responsible role for updating data on identity documents.");
      
      var identificationDocumentsManagers = Roles.GetAll().SingleOrDefault(n => n.Sid == Constants.Module.RoleGuid.IdentificationDocumentsManagersRole);
      if (identificationDocumentsManagers == null)
        return;
      
      Parties.People.AccessRights.Grant(identificationDocumentsManagers, DefaultAccessRightsTypes.Change);
      Parties.People.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права роли "Ответственные за настройку регистрации".
    /// </summary>
    public static void GrantRightToRegistrationManagersRole()
    {
      InitializationLogger.Debug("Init: Grant rights on logs and registration settings to registration managers.");
      
      var registrationManagers = Roles.GetAll().SingleOrDefault(n => n.Sid == Constants.Module.RoleGuid.RegistrationManagersRole);
      if (registrationManagers == null)
        return;
      
      // Модуль "Документооборот".
      Docflow.DocumentRegisters.AccessRights.Grant(registrationManagers, DefaultAccessRightsTypes.FullAccess);
      Docflow.CaseFiles.AccessRights.Grant(registrationManagers, DefaultAccessRightsTypes.FullAccess);
      Docflow.MailDeliveryMethods.AccessRights.Grant(registrationManagers, DefaultAccessRightsTypes.FullAccess);
      Docflow.FileRetentionPeriods.AccessRights.Grant(registrationManagers, DefaultAccessRightsTypes.FullAccess);
      Docflow.RegistrationSettings.AccessRights.Grant(registrationManagers, DefaultAccessRightsTypes.FullAccess);
      Docflow.DocumentKinds.AccessRights.Grant(registrationManagers, DefaultAccessRightsTypes.Change);
      Docflow.DocumentRegisters.AccessRights.Save();
      Docflow.CaseFiles.AccessRights.Save();
      Docflow.MailDeliveryMethods.AccessRights.Save();
      Docflow.FileRetentionPeriods.AccessRights.Save();
      Docflow.RegistrationSettings.AccessRights.Save();
      Docflow.DocumentKinds.AccessRights.Save();
      
      // Выдача прав на саму роль и связанные с ней - делопроизводители и регистрирующие договоры.
      var roles = Roles.GetAll(r => r.Sid == Constants.Module.RoleGuid.ClerksRole ||
                               r.Sid == Constants.Module.RoleGuid.RegistrationManagersRole ||
                               r.Sid == Constants.Module.RoleGuid.RegistrationContractualDocument)
        .ToList();
      foreach (var role in roles)
      {
        role.AccessRights.Grant(registrationManagers, DefaultAccessRightsTypes.Change);
        role.Save();
      }
    }
    
    /// <summary>
    /// Выдать права роли "Подписывающие".
    /// </summary>
    public static void GrantRightsToSigningRole()
    {
      InitializationLogger.Debug("Init: Grant endorsement rights on documents to signing role.");
      
      var signingRole = Roles.GetAll().SingleOrDefault(r => r.Sid == Constants.Module.RoleGuid.SigningRole);
      if (signingRole == null)
        return;

      // Права на документы.
      Docflow.OfficialDocuments.AccessRights.Grant(signingRole, DefaultAccessRightsTypes.Approve);
      Docflow.OfficialDocuments.AccessRights.Save();
    }
    
    #endregion
    
    #region Создание типов файлов и приложений-обработчиков
    
    /// <summary>
    /// Создание типов файлов и приложений-обработчиков.
    /// </summary>
    /// <remarks>Только при первом запуске, потом чужие настройки не трогаем.</remarks>
    public static void CreateFileTypesAndAssociatedApps()
    {
      if (Sungero.Content.FilesTypes.GetAll().Any() || Sungero.Content.AssociatedApplications.GetAll().Any())
        return;
      
      var fileType = CreateFileType(Resources.Initialize_FileTypes_Text);
      Functions.Module.CreateAssociatedApplication("doc", Resources.Initialize_FileTypes_Text_doc, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("docx", Resources.Initialize_FileTypes_Text_docx, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("pdf", Resources.Initialize_FileTypes_Text_pdf, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("txt", Resources.Initialize_FileTypes_Text_txt, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("rtf", Resources.Initialize_FileTypes_Text_rtf, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("chm", Resources.Initialize_FileTypes_Text_chm, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("xps", Resources.Initialize_FileTypes_Text_xps, AppMonitoringType.Process, fileType, false);
      Functions.Module.CreateAssociatedApplication("fb2", Resources.Initialize_FileTypes_Text_fb2, AppMonitoringType.Process, fileType, true);
      Functions.Module.CreateAssociatedApplication("djvu", Resources.Initialize_FileTypes_Text_djvu, AppMonitoringType.Process, fileType, true);
      Functions.Module.CreateAssociatedApplication("hlp", Resources.Initialize_FileTypes_Text_hlp, AppMonitoringType.Process, fileType, true);
      Functions.Module.CreateAssociatedApplication("odt", Resources.Initialize_FileTypes_Text_odt, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("ott", Resources.Initialize_FileTypes_Text_odt, AppMonitoringType.ByProcessAndWindow, fileType, false);
      
      fileType = CreateFileType(Resources.Initialize_FileTypes_Media);
      Functions.Module.CreateAssociatedApplication("wmv", Resources.Initialize_FileTypes_Media_wmv, AppMonitoringType.ByProcessAndWindow, fileType, true);
      Functions.Module.CreateAssociatedApplication("swf", Resources.Initialize_FileTypes_Media_swf, AppMonitoringType.ByProcessAndWindow, fileType, true);
      Functions.Module.CreateAssociatedApplication("avi", Resources.Initialize_FileTypes_Media_avi, AppMonitoringType.ByProcessAndWindow, fileType, true);
      Functions.Module.CreateAssociatedApplication("mp3", Resources.Initialize_FileTypes_Media_mp3, AppMonitoringType.Process, fileType, true);
      Functions.Module.CreateAssociatedApplication("flv", Resources.Initialize_FileTypes_Media_flv, AppMonitoringType.Process, fileType, true);
      
      fileType = CreateFileType(Resources.Initialize_FileTypes_Other);
      Functions.Module.CreateAssociatedApplication("mpp", Resources.Initialize_FileTypes_Other_mpp, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("oft", Resources.Initialize_FileTypes_Other_oft, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("xml", Resources.Initialize_FileTypes_Other_xml, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("vcs", Resources.Initialize_FileTypes_Other_vcs, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("pub", Resources.Initialize_FileTypes_Other_pub, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("lnk", Resources.Initialize_FileTypes_Other_lnk, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("msg", Resources.Initialize_FileTypes_Other_msg, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("one", Resources.Initialize_FileTypes_Other_one, AppMonitoringType.Process, fileType, false);
      Functions.Module.CreateAssociatedApplication("nfo", Resources.Initialize_FileTypes_Other_nfo, AppMonitoringType.Process, fileType, false);
      
      // Dmitriev_IA: Создание приложения-обработчика Unknown application.
      Functions.Module.CreateAssociatedApplication("*", Resources.Initialize_FileTypes_Other_unknown, AppMonitoringType.Manual, fileType, false);
      var unknownApp = Sungero.Content.AssociatedApplications.GetAll().FirstOrDefault(a => a.Extension == "*");
      if (unknownApp != null)
      {
        unknownApp.Sid = Constants.Module.UnknownAppSid;
        unknownApp.Save();
      }
      
      fileType = CreateFileType(Resources.Initialize_FileTypes_Archive);
      Functions.Module.CreateAssociatedApplication("zip", Resources.Initialize_FileTypes_Archive_zip, AppMonitoringType.ByProcessAndWindow, fileType, true);
      Functions.Module.CreateAssociatedApplication("7z", Resources.Initialize_FileTypes_Archive_7z, AppMonitoringType.ByProcessAndWindow, fileType, true);
      Functions.Module.CreateAssociatedApplication("rar", Resources.Initialize_FileTypes_Archive_rar, AppMonitoringType.ByProcessAndWindow, fileType, true);
      
      fileType = CreateFileType(Resources.Initialize_FileTypes_Picture);
      Functions.Module.CreateAssociatedApplication("jpeg", Resources.Initialize_FileTypes_Picture_jpeg, AppMonitoringType.ByProcessAndWindow, fileType, true);
      Functions.Module.CreateAssociatedApplication("jpg", Resources.Initialize_FileTypes_Picture_jpg, AppMonitoringType.ByProcessAndWindow, fileType, true);
      Functions.Module.CreateAssociatedApplication("png", Resources.Initialize_FileTypes_Picture_png, AppMonitoringType.ByProcessAndWindow, fileType, true);
      Functions.Module.CreateAssociatedApplication("tiff", Resources.Initialize_FileTypes_Picture_tiff, AppMonitoringType.Process, fileType, true);
      Functions.Module.CreateAssociatedApplication("cdr", Resources.Initialize_FileTypes_Picture_cdr, AppMonitoringType.Process, fileType, true);
      Functions.Module.CreateAssociatedApplication("ico", Resources.Initialize_FileTypes_Picture_ico, AppMonitoringType.Process, fileType, true);
      Functions.Module.CreateAssociatedApplication("tif", Resources.Initialize_FileTypes_Picture_tif, AppMonitoringType.Process, fileType, true);
      Functions.Module.CreateAssociatedApplication("bmp", Resources.Initialize_FileTypes_Picture_bmp, AppMonitoringType.Process, fileType, true);
      Functions.Module.CreateAssociatedApplication("gif", Resources.Initialize_FileTypes_Picture_gif, AppMonitoringType.Process, fileType, true);
      
      fileType = CreateFileType(Resources.Initialize_FileTypes_Table);
      Functions.Module.CreateAssociatedApplication("xls", Resources.Initialize_FileTypes_Table_xls, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("xlt", Resources.Initialize_FileTypes_Table_xlt, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("xlsm", Resources.Initialize_FileTypes_Table_xlsm, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("xlsx", Resources.Initialize_FileTypes_Table_xlsx, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("ods", Resources.Initialize_FileTypes_Table_ods, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("ots", Resources.Initialize_FileTypes_Table_ods, AppMonitoringType.ByProcessAndWindow, fileType, false);
      
      fileType = CreateFileType(Resources.Initialize_FileTypes_Presentation);
      Functions.Module.CreateAssociatedApplication("ppt", Resources.Initialize_FileTypes_Presentation_ppt, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("pptx", Resources.Initialize_FileTypes_Presentation_pptx, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("pps", Resources.Initialize_FileTypes_Presentation_pps, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("ppsx", Resources.Initialize_FileTypes_Presentation_ppsx, AppMonitoringType.Process, fileType, false);
      Functions.Module.CreateAssociatedApplication("odp", Resources.Initialize_FileTypes_Presentation_odp, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("otp", Resources.Initialize_FileTypes_Presentation_odp, AppMonitoringType.ByProcessAndWindow, fileType, false);
      
      fileType = CreateFileType(Resources.Initialize_FileTypes_Schema);
      Functions.Module.CreateAssociatedApplication("vsd", Resources.Initialize_FileTypes_Schema_vsd, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("mm", Resources.Initialize_FileTypes_Schema_mm, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("mmap", Resources.Initialize_FileTypes_Schema_mmap, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("xmind", Resources.Initialize_FileTypes_Schema_xmind, AppMonitoringType.Process, fileType, false);
      
      fileType = CreateFileType(Resources.Initialize_FileTypes_WebPage);
      Functions.Module.CreateAssociatedApplication("mht", Resources.Initialize_FileTypes_WebPage_mht, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("url", Resources.Initialize_FileTypes_WebPage_url, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("htm", Resources.Initialize_FileTypes_WebPage_htm, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("html", Resources.Initialize_FileTypes_WebPage_html, AppMonitoringType.ByProcessAndWindow, fileType, false);
      Functions.Module.CreateAssociatedApplication("sht", Resources.Initialize_FileTypes_WebPage_sht, AppMonitoringType.Process, fileType, false);
      Functions.Module.CreateAssociatedApplication("css", Resources.Initialize_FileTypes_WebPage_css, AppMonitoringType.Process, fileType, false);
    }
    
    /// <summary>
    /// Создать тип файла.
    /// </summary>
    /// <param name="name">Название типа.</param>
    /// <returns>Тип файла.</returns>
    public static Sungero.Content.IFilesType CreateFileType(string name)
    {
      var fileType = Sungero.Content.FilesTypes.GetAll(t => t.Name == name).FirstOrDefault();
      if (fileType != null)
        return fileType;
      
      fileType = Sungero.Content.FilesTypes.Create();
      fileType.Name = name;
      fileType.Save();
      return fileType;
    }
    
    #endregion
    
    #region Создание способов доставки
    
    public static void CreateMailDeliveryMethods()
    {
      CreateMailDeliveryMethod(MailDeliveryMethods.Resources.ExchangeMethod, Constants.MailDeliveryMethod.Exchange);
      
      // Если уже есть созданные вручную способы доставки, то новые не создавать.
      if (Docflow.MailDeliveryMethods.GetAll(m => m.Sid == null).Any())
        return;
      
      CreateMailDeliveryMethod(MailDeliveryMethods.Resources.MailMethod, null);
      CreateMailDeliveryMethod(MailDeliveryMethods.Resources.CourierMethod, null);
      CreateMailDeliveryMethod(MailDeliveryMethods.Resources.FaxMethod, null);
      CreateMailDeliveryMethod(MailDeliveryMethods.Resources.EmailMethod, null);
    }
    
    /// <summary>
    /// Создать способ доставки.
    /// </summary>
    /// <param name="name">Название.</param>
    /// <param name="sid">Уникальный ИД, регистрозависимый.</param>
    /// <returns>Способ доставки.</returns>
    [Public]
    public static IMailDeliveryMethod CreateMailDeliveryMethod(string name, string sid)
    {
      var method = string.IsNullOrEmpty(sid) ? Docflow.MailDeliveryMethods.GetAll(m => m.Name == name).FirstOrDefault() :
        Docflow.MailDeliveryMethods.GetAll(m => m.Sid == sid).FirstOrDefault();
      if (method == null)
      {
        method = MailDeliveryMethods.Create();
        method.Sid = sid;
      }
      method.Name = name;
      method.Save();
      
      return method;
    }
    
    #endregion
    
    #region Создание видов и типов документов
    
    /// <summary>
    /// Создать типы документов для документооборота.
    /// </summary>
    public static void CreateDocumentTypes()
    {
      InitializationLogger.Debug("Init: Create document types");
      CreateDocumentType(Docflow.Resources.SimpleDocumentTypeName, SimpleDocument.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Inner, true);
      CreateDocumentType(Docflow.Resources.MemoTypeName, Memo.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Inner, true);
      CreateDocumentType(Docflow.Resources.AddendumTypeName, Addendum.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Inner, true);
      CreateDocumentType(Docflow.Resources.ExchangeDocumentTypeName, ExchangeDocument.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Incoming, false);
      CreateDocumentType(Docflow.Resources.PowerOfAttorneyTypeName, PowerOfAttorney.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Inner, true);
      CreateDocumentType(Docflow.Resources.FormalizedPowerOfAttorneyTypeName, FormalizedPowerOfAttorney.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Inner, true);
      CreateDocumentType(Docflow.Resources.PowerOfAttorneyRevocationTypeName, PowerOfAttorneyRevocation.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Inner, true);
      CreateDocumentType(Docflow.Resources.CounterpartyDocumentTypeName, CounterpartyDocument.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Inner, true);
    }
    
    /// <summary>
    /// Создать виды документов для документооборота.
    /// </summary>
    public static void CreateDocumentKinds()
    {
      InitializationLogger.Debug("Init: Create document kinds.");
      
      var registrable = Docflow.DocumentKind.NumberingType.Registrable;
      var numerable = Docflow.DocumentKind.NumberingType.Numerable;
      var notNumerable = Docflow.DocumentKind.NumberingType.NotNumerable;
      
      var actions = new Domain.Shared.IActionInfo[] {
        Docflow.OfficialDocuments.Info.Actions.SendActionItem,
        Docflow.OfficialDocuments.Info.Actions.SendForFreeApproval,
        Docflow.OfficialDocuments.Info.Actions.SendForDocumentFlow,
        Docflow.OfficialDocuments.Info.Actions.SendForAcquaintance };
      
      CreateDocumentKind(Docflow.Resources.SimpleDocumentKindName, Docflow.Resources.SimpleDocumentKindShortName,
                         notNumerable, DocumentFlow.Inner, false, false, SimpleDocument.ClassTypeGuid,
                         new Domain.Shared.IActionInfo[] { OfficialDocuments.Info.Actions.SendActionItem, OfficialDocuments.Info.Actions.SendForFreeApproval, OfficialDocuments.Info.Actions.SendForAcquaintance },
                         Init.SimpleDocumentKind);
      CreateDocumentKind(Docflow.Resources.MemoKindName, Docflow.Resources.MemoKindShortName, numerable,
                         DocumentFlow.Inner, true, true, Memo.ClassTypeGuid,
                         actions,
                         Init.MemoKind);
      CreateDocumentKind(Docflow.Resources.AddendumKindName, Docflow.Resources.AddendumKindShortName,
                         notNumerable, DocumentFlow.Inner, true, false, Addendum.ClassTypeGuid,
                         new Domain.Shared.IActionInfo[] { OfficialDocuments.Info.Actions.SendForFreeApproval },
                         Init.AddendumKind);
      CreateDocumentKind(Docflow.Resources.ExchangeDocumentKindName, Docflow.Resources.ExchangeDocumentKindShortName,
                         notNumerable, DocumentFlow.Incoming, true, false, ExchangeDocument.ClassTypeGuid,
                         new Domain.Shared.IActionInfo[] { }, Init.ExchangeKind);
      CreateDocumentKind(Docflow.Resources.PowerOfAttorneyKindName, Docflow.Resources.PowerOfAttorneyKindShortName,
                         numerable, DocumentFlow.Inner, true, true, PowerOfAttorney.ClassTypeGuid,
                         new Domain.Shared.IActionInfo[] { OfficialDocuments.Info.Actions.SendForFreeApproval, OfficialDocuments.Info.Actions.SendForDocumentFlow },
                         Init.PowerOfAttorneyKind);
      CreateDocumentKind(Docflow.Resources.CounterpartyDocumentKindName, Docflow.Resources.CounterpartyDocumentKindShortName,
                         notNumerable, DocumentFlow.Inner, true, false, CounterpartyDocument.ClassTypeGuid,
                         new Domain.Shared.IActionInfo[] { OfficialDocuments.Info.Actions.SendForFreeApproval },
                         Init.CounterpartyDocumentDefaultKind);
      CreateFormalizedPowerOfAttorneyDocumentKind();
      CreateDocumentKind(Docflow.Resources.PowerOfAttorneyRevocationKindName, Docflow.Resources.PowerOfAttorneyRevocationKindShortName,
                         notNumerable, DocumentFlow.Inner, true, false, PowerOfAttorneyRevocation.ClassTypeGuid,
                         new Domain.Shared.IActionInfo[] { OfficialDocuments.Info.Actions.SendForFreeApproval, OfficialDocuments.Info.Actions.SendForDocumentFlow },
                         Init.PowerOfAttorneyRevocationKind);
    }
    
    /// <summary>
    /// Заполнить виды носителей документов по умолчанию в видах документов.
    /// </summary>
    public static void FillDocumentKindsMediumTypes()
    {
      var electronicMediumType = Docflow.Functions.MediumType.GetNativeElectronicMediumType();
      FillDocumentKindMediumType(Init.ExchangeKind, electronicMediumType);
      FillDocumentKindMediumType(Init.PowerOfAttorneyRevocationKind, electronicMediumType);
      FillDocumentKindMediumType(Init.FormalizedPowerOfAttorneyKind, electronicMediumType);
      
      var paperMediumType = Docflow.Functions.MediumType.GetNativePaperMediumType();
      FillDocumentKindMediumType(Init.PowerOfAttorneyKind, paperMediumType);
    }
    
    /// <summary>
    /// Заполнить вид носителя документа по умолчанию в виде документа.
    /// </summary>
    /// <param name="documentKindInitializationGuid">ИД инициализации вида документа.</param>
    /// <param name="mediumType">Вид носителя документа по умолчанию.</param>
    [Public]
    public static void FillDocumentKindMediumType(Guid documentKindInitializationGuid, IMediumType mediumType)
    {
      var externalLink = Docflow.PublicFunctions.Module.GetExternalLink(DocumentKind.ClassTypeGuid, documentKindInitializationGuid);
      if (externalLink == null)
        return;
      
      var documentKindId = externalLink.EntityId;
      var documentKind = DocumentKinds.GetAll(k => k.Id == documentKindId).FirstOrDefault();
      if (documentKind == null || mediumType == null || documentKind.DefaultMedium != null)
        return;
      
      InitializationLogger.DebugFormat("Init: Fill document kind {0} default medium type as {1}", documentKind.Name, mediumType.Name);
      
      documentKind.DefaultMedium = mediumType;
      documentKind.Save();
    }
    
    /// <summary>
    /// Создать вид документа для электронной доверенности.
    /// </summary>
    public static void CreateFormalizedPowerOfAttorneyDocumentKind()
    {
      // Определить, создавался ли ранее журнал регистрации для доверенностей.
      var registerExternalLink = Docflow.PublicFunctions.Module.GetExternalLink(DocumentRegister.ClassTypeGuid, Init.PowerOfAttorneyKind);
      
      // Если журнал для доверенностей ранее не создавался (новая поставка), то сделать вид документа автонумеруемым.
      var isAutonumerable = registerExternalLink == null;
      CreateDocumentKind(Docflow.Resources.FormalizedPowerOfAttorneyKindName, Docflow.Resources.FormalizedPowerOfAttorneyKindShortName,
                         Docflow.DocumentKind.NumberingType.Numerable, DocumentFlow.Inner, true, isAutonumerable, FormalizedPowerOfAttorney.ClassTypeGuid,
                         new Domain.Shared.IActionInfo[] { OfficialDocuments.Info.Actions.SendForFreeApproval, OfficialDocuments.Info.Actions.SendForDocumentFlow },
                         Init.FormalizedPowerOfAttorneyKind);
    }
    
    /// <summary>
    /// Создать вид документа.
    /// </summary>
    /// <param name="name">Имя.</param>
    /// <param name="shortName">Сокращенное имя.</param>
    /// <param name="numerationType">Нумерация.</param>
    /// <param name="direction">Документопоток.</param>
    /// <param name="autoFormattedName">Признак автоформирования имени.</param>
    /// <param name="autoNumerable">Признак автонумерации.</param>
    /// <param name="typeGuid">Доступный тип документа.</param>
    /// <param name="actions">Действия отправки по умолчанию.</param>
    /// <param name="entityId">ИД инициализации.</param>
    [Public]
    public static void CreateDocumentKind(string name, string shortName, Enumeration numerationType, Enumeration direction, bool autoFormattedName,
                                          bool autoNumerable, Guid typeGuid, Domain.Shared.IActionInfo[] actions, Guid entityId)
    {
      CreateDocumentKind(name, shortName, numerationType, direction, autoFormattedName, autoNumerable, typeGuid, actions, entityId, true);
    }
    
    /// <summary>
    /// Создать вид документа.
    /// </summary>
    /// <param name="name">Имя.</param>
    /// <param name="shortName">Сокращенное имя.</param>
    /// <param name="numerationType">Нумерация.</param>
    /// <param name="direction">Документопоток.</param>
    /// <param name="autoFormattedName">Признак автоформирования имени.</param>
    /// <param name="autoNumerable">Признак автонумерации.</param>
    /// <param name="typeGuid">Доступный тип документа.</param>
    /// <param name="actions">Действия отправки по умолчанию.</param>
    /// <param name="entityId">ИД инициализации.</param>
    /// <param name="isDefault">Признак вида документа по умолчанию.</param>
    [Public]
    public static void CreateDocumentKind(string name, string shortName, Enumeration numerationType, Enumeration direction, bool autoFormattedName,
                                          bool autoNumerable, Guid typeGuid, Domain.Shared.IActionInfo[] actions, Guid entityId, bool isDefault)
    {
      CreateDocumentKind(name, shortName, numerationType, direction, autoFormattedName, autoNumerable, typeGuid, actions, false, false, entityId, isDefault);
    }
    
    /// <summary>
    /// Создать вид документа.
    /// </summary>
    /// <param name="name">Имя.</param>
    /// <param name="shortName">Сокращенное имя.</param>
    /// <param name="numerationType">Нумерация.</param>
    /// <param name="direction">Документопоток.</param>
    /// <param name="autoFormattedName">Признак автоформирования имени.</param>
    /// <param name="autoNumerable">Признак автонумерации.</param>
    /// <param name="typeGuid">Доступный тип документа.</param>
    /// <param name="actions">Действия отправки по умолчанию.</param>
    /// <param name="projectAccounting">Признак ведения учета документа по проектам.</param>
    /// <param name="grantRightsToProject">Выдавать права участникам проектов на экземпляры вида документа.</param>
    /// <param name="entityId">ИД инициализации.</param>
    /// <param name="isDefault">Признак вида документа по умолчанию.</param>
    [Public]
    public static void CreateDocumentKind(string name, string shortName, Enumeration numerationType, Enumeration direction,
                                          bool autoFormattedName, bool autoNumerable, Guid typeGuid, Domain.Shared.IActionInfo[] actions,
                                          bool projectAccounting, bool grantRightsToProject, Guid entityId, bool isDefault)
    {
      CreateDocumentKind(name, shortName, numerationType, direction, autoFormattedName, autoNumerable,
                         typeGuid, actions, projectAccounting, grantRightsToProject, entityId, isDefault, null);
    }
    
    /// <summary>
    /// Создать вид документа.
    /// </summary>
    /// <param name="name">Имя.</param>
    /// <param name="shortName">Сокращенное имя.</param>
    /// <param name="numerationType">Нумерация.</param>
    /// <param name="direction">Документопоток.</param>
    /// <param name="autoFormattedName">Признак автоформирования имени.</param>
    /// <param name="autoNumerable">Признак автонумерации.</param>
    /// <param name="typeGuid">Доступный тип документа.</param>
    /// <param name="actions">Действия отправки по умолчанию.</param>
    /// <param name="projectAccounting">Признак ведения учета документа по проектам.</param>
    /// <param name="grantRightsToProject">Выдавать права участникам проектов на экземпляры вида документа.</param>
    /// <param name="entityId">ИД инициализации.</param>
    /// <param name="isDefault">Признак вида документа по умолчанию.</param>
    /// <param name="defaultMediumType">Вид носителя документа по умолчанию.</param>
    [Public]
    public static void CreateDocumentKind(string name, string shortName, Enumeration numerationType, Enumeration direction,
                                          bool autoFormattedName, bool autoNumerable, Guid typeGuid, Domain.Shared.IActionInfo[] actions,
                                          bool projectAccounting, bool grantRightsToProject, Guid entityId, bool isDefault, IMediumType defaultMediumType)
    {
      var externalLink = Docflow.PublicFunctions.Module.GetExternalLink(DocumentKind.ClassTypeGuid, entityId);
      if (externalLink != null)
        return;
      
      var documentType = GetDocumentTypeByTypeGuid(typeGuid);
      
      InitializationLogger.DebugFormat("Init: Create document kind {0}", name);
      
      var documentKind = DocumentKinds.Create();
      documentKind.Name = name;
      documentKind.ShortName = shortName;
      documentKind.DocumentFlow = direction;
      documentKind.NumberingType = numerationType;
      documentKind.GenerateDocumentName = autoFormattedName;
      documentKind.AutoNumbering = autoNumerable;
      documentKind.ProjectsAccounting = projectAccounting;
      documentKind.GrantRightsToProject = grantRightsToProject;
      documentKind.DocumentType = documentType;
      documentKind.IsDefault = isDefault;
      documentKind.DefaultMedium = defaultMediumType;
      
      // Перебиваем действия, если они были явно переданы.
      if (actions != null && actions.Any())
      {
        documentKind.AvailableActions.Clear();
        foreach (var action in actions)
          documentKind.AvailableActions.AddNew().Action = Functions.Module.GetSendAction(action);
      }

      documentKind.Save();
      
      Docflow.PublicFunctions.Module.CreateExternalLink(documentKind, entityId);
    }
    
    /// <summary>
    /// Создать тип документа.
    /// </summary>
    /// <param name="name">Имя.</param>
    /// <param name="documentTypeGuid">Идентификатор типа документа.</param>
    /// <param name="direction">Документопоток.</param>
    /// <param name="isRegistrationAllowed">Признак допустимости регистрации типа.</param>
    [Public]
    public static void CreateDocumentType(string name, Guid documentTypeGuid, Enumeration direction, bool isRegistrationAllowed)
    {
      CreateDocumentType(name, documentTypeGuid, direction, Sungero.CoreEntities.DatabookEntry.Status.Active, isRegistrationAllowed);
    }
    
    /// <summary>
    /// Создать тип документа.
    /// </summary>
    /// <param name="name">Имя.</param>
    /// <param name="documentTypeGuid">Идентификатор типа документа.</param>
    /// <param name="direction">Документопоток.</param>
    /// <param name="status">Состояние.</param>
    /// <param name="isRegistrationAllowed">Признак допустимости регистрации типа.</param>
    [Public]
    public static void CreateDocumentType(string name, Guid documentTypeGuid, Enumeration direction, Enumeration status, bool isRegistrationAllowed)
    {
      InitializationLogger.DebugFormat("Init: Create document type {0}", name);
      
      var documentType = GetDocumentTypeByTypeGuid(documentTypeGuid);
      if (documentType == null)
        documentType = DocumentTypes.Create();

      documentType.Name = name;
      documentType.DocumentTypeGuid = documentTypeGuid.ToString();
      documentType.DocumentFlow = direction;
      documentType.IsRegistrationAllowed = isRegistrationAllowed;
      documentType.Status = status;
      documentType.Save();
    }
    
    /// <summary>
    /// Создать external link для вида документа, если он не существует.
    /// </summary>
    /// <param name="documentTypeGuid">Идентификатор типа.</param>
    /// <param name="entityId">ИД инициализации.</param>
    [Public]
    public static void CreateDocumentTypeExternalLink(Guid documentTypeGuid, Guid entityId)
    {
      var externalLink = Docflow.PublicFunctions.Module.GetExternalLink(Sungero.Docflow.Server.DocumentType.ClassTypeGuid, entityId);
      if (externalLink == null)
      {
        var entity = GetDocumentTypeByTypeGuid(documentTypeGuid);
        if (entity != null)
          Docflow.PublicFunctions.Module.CreateExternalLink(entity, entityId);
      }
    }
    
    [Public]
    public static IDocumentType GetDocumentTypeByTypeGuid(Guid typeGuid)
    {
      return DocumentTypes.GetAll(t => Equals(t.DocumentTypeGuid, typeGuid.ToString())).FirstOrDefault();
    }
    
    #endregion
    
    #region Создание справочника с действиями отправки
    
    public static void InitSendForDocumentFlowAction()
    {
      CreateDocumentSendAction(OfficialDocuments.Info.Actions.SendForDocumentFlow, Docflow.Resources.SendForDocumentFlowActionName);
    }
    
    /// <summary>
    /// Создать действия отправки.
    /// </summary>
    public static void CreateDocumentSendActions()
    {
      InitializationLogger.Debug("Init: Create document send actions");

      CreateDocumentSendAction(OfficialDocuments.Info.Actions.SendForReview, Docflow.Resources.SendForReviewActionName);
      CreateDocumentSendAction(OfficialDocuments.Info.Actions.SendActionItem, Docflow.Resources.SendActionItemActionName);
      CreateDocumentSendAction(OfficialDocuments.Info.Actions.SendForFreeApproval, Docflow.Resources.SendForFreeApprovalActionName);
      CreateDocumentSendAction(OfficialDocuments.Info.Actions.SendForApproval, Docflow.Resources.SendForApprovalActionName);
      CreateDocumentSendAction(OfficialDocuments.Info.Actions.SendForAcquaintance, Docflow.Resources.SendForAcquaintanceActionName);
      InitSendForDocumentFlowAction();
    }

    /// <summary>
    /// Создать действие отправки.
    /// </summary>
    /// <param name="action">Действие.</param>
    /// <param name="name">Имя.</param>
    [Public]
    public static void CreateDocumentSendAction(Domain.Shared.IActionInfo action, string name)
    {
      var actionGuid = Functions.Module.GetActionGuid(action);
      var sendAction = DocumentSendActions.GetAll(d => d.ActionGuid == actionGuid).FirstOrDefault();
      if (sendAction == null)
      {
        sendAction = DocumentSendActions.Create();
        InitializationLogger.DebugFormat("Init: Create document send action {0} for action {1}", name, action.Name);
      }

      sendAction.Name = name;
      sendAction.ActionGuid = actionGuid;
      sendAction.Save();
    }
    
    #endregion
    
    #region Создание правила согласования и этапов по умолчанию
    
    /// <summary>
    /// Создать правила согласования по умолчанию.
    /// </summary>
    public static void CreateDefaultApprovalRules()
    {
      InitializationLogger.Debug("Init: Create default approval rules.");
      
      CreateOutgoingDefaultApprovalRuleWithConditions();
      CreateInternalDefaultApprovalRuleWithConditions();
      CreateMemoDefaultApprovalRuleWithConditions();
    }
    
    /// <summary>
    /// Создать правило по умолчанию для исходящих документов с условиями.
    /// </summary>
    public static void CreateOutgoingDefaultApprovalRuleWithConditions()
    {
      InitializationLogger.DebugFormat("Init: Create default approval rule '{0}' for outgoing documents.", Resources.DefaultApprovalRuleNameOutgoing);
      
      var stages = new List<Enumeration>
      { StageType.Manager, StageType.Approvers, StageType.Print, StageType.Sign, StageType.Register, StageType.Sending, StageType.Notice };
      var outgoingRule = CreateDefaultRule(Resources.DefaultApprovalRuleNameOutgoing,
                                           Sungero.Docflow.ApprovalRuleBase.DocumentFlow.Outgoing,
                                           stages);
      // Добавить условие по способу отправки и непосредственный руководитель - подписывающий, для созданного правила.
      if (outgoingRule == null)
        return;
      
      var condition = Conditions.Create();
      condition.ConditionType = Docflow.ConditionBase.ConditionType.DeliveryMethod;
      var newDeliveryMethod = condition.DeliveryMethods.AddNew();
      newDeliveryMethod.DeliveryMethod = Docflow.MailDeliveryMethods.GetAll(m => m.Sid == Constants.MailDeliveryMethod.Exchange).FirstOrDefault();
      condition.Save();
      var printStageNumber = stages.IndexOf(StageType.Print) + 1;
      AddConditionToRule(outgoingRule, condition, printStageNumber);
      
      var rolesCompareCondition = ModuleInitialization.Module.CreateRoleCompareSignatoryAndInitManagerCondition(Conditions.Create());
      var managerStageNumber = stages.IndexOf(StageType.Manager) + 1;
      AddConditionToRule(outgoingRule, rolesCompareCondition, managerStageNumber);
    }
    
    /// <summary>
    /// Создать правило по умолчанию для внутренних документов с условиями.
    /// </summary>
    public static void CreateInternalDefaultApprovalRuleWithConditions()
    {
      InitializationLogger.DebugFormat("Init: Create default approval rule '{0}' for internal documents.", Resources.DefaultApprovalRuleNameInternal);
      
      var stages = new List<Enumeration>
      { StageType.Manager, StageType.Approvers, StageType.Print, StageType.Sign, StageType.Register, StageType.Notice };
      var internalRule = CreateDefaultRule(Resources.DefaultApprovalRuleNameInternal,
                                           Sungero.Docflow.ApprovalRuleBase.DocumentFlow.Inner,
                                           stages);
      // Добавить условие непосредственный руководитель - подписывающий, для созданного правила.
      if (internalRule == null)
        return;
      
      var rolesCompareCondition = ModuleInitialization.Module.CreateRoleCompareSignatoryAndInitManagerCondition(Conditions.Create());
      var managerStageNumber = stages.IndexOf(StageType.Manager) + 1;
      AddConditionToRule(internalRule, rolesCompareCondition, managerStageNumber);
    }
    
    /// <summary>
    /// Создать правило по умолчанию для служебных записок с условиями.
    /// </summary>
    public static void CreateMemoDefaultApprovalRuleWithConditions()
    {
      InitializationLogger.DebugFormat("Init: Create default approval rule '{0}' for memos.", Resources.DefaultApprovalRuleNameMemo);
      
      // Количество шагов между этапами в правиле для СЗ по умолчанию.
      var executionToReviewTaskStepsCount = 3;
      var executionToReviewedNoticeStepsCount = 2;
      
      var memoStages = new List<Enumeration>
      { StageType.Manager, StageType.Approvers, StageType.Review, StageType.Execution };
      var memoRule = CreateMemoDefaultRule(Resources.DefaultApprovalRuleNameMemo, memoStages);
      if (memoRule == null)
        return;
      
      // Добавить этап рассмотрения несколькими адресатами.
      AddReviewTaskStage(memoRule);
      Docflow.PublicFunctions.ApprovalRuleBase.CreateAutoTransitions(memoRule);
      
      /* Добавить условие несколько адресатов.
       * Условие добавляется вначале, чтобы при сохранении не сработала валидация
       * наличия этапа рассмотрения и этапа рассмотрения несколькими адресатами в одной ветке.
       */
      var manyAddresseesCondition = Conditions.Create();
      manyAddresseesCondition.ConditionType = Docflow.Condition.ConditionType.ManyAddressees;
      manyAddresseesCondition.Save();
      
      var approversStageNumber = memoStages.IndexOf(StageType.Approvers) + 1;
      var reviewTaskStageNumber = memoStages.IndexOf(StageType.Execution) + executionToReviewTaskStepsCount;
      var reviewStageNumber = memoStages.IndexOf(StageType.Review) + 1;
      var reviewedNoticeStageNumber = memoStages.IndexOf(StageType.Execution) + executionToReviewedNoticeStepsCount;
      
      // Удалить переход между уведомлением и этапом рассмотрения несколькими адресатами.
      var transitionToRemove = memoRule.Transitions.Where(t => t.SourceStage == reviewedNoticeStageNumber).FirstOrDefault();
      memoRule.Transitions.Remove(transitionToRemove);
      
      Sungero.Docflow.Functions.ApprovalRuleBase.AddConditionToRule(memoRule, manyAddresseesCondition,
                                                                    approversStageNumber,
                                                                    reviewTaskStageNumber,
                                                                    reviewStageNumber);
      
      // Добавить условие непосредственный руководитель - адресат, для созданного правила.
      var rolesCompareCondition = ModuleInitialization.Module.CreateRoleCompareSignatoryAndInitManagerCondition(Conditions.Create());
      rolesCompareCondition.ApprovalRole = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.Addressee);
      var managerStageNumber = memoStages.IndexOf(StageType.Manager) + 1;
      AddConditionToRule(memoRule, rolesCompareCondition, managerStageNumber);
    }
    
    /// <summary>
    /// Создать правило по умолчанию для служебных записок.
    /// </summary>
    /// <param name="ruleName">Имя правила.</param>
    /// <param name="stages">Этапы.</param>
    /// <returns>Созданное правило.</returns>
    [Public]
    public static IApprovalRule CreateMemoDefaultRule(string ruleName, List<Enumeration> stages)
    {
      var hasNotDefaultRule = ApprovalRuleBases.GetAll().Any(r => r.IsDefaultRule != true);
      
      var memoDocumentKind = DocumentKinds.GetAll().FirstOrDefault(k => k.DocumentType.DocumentTypeGuid == Memo.ClassTypeGuid.ToString());
      
      var hasDefaultRule = ApprovalRuleBases.GetAll()
        .Where(r => r.DocumentFlow == Sungero.Docflow.ApprovalRuleBase.DocumentFlow.Inner)
        .Any(r => r.DocumentKinds.Any(d => Equals(d.DocumentKind, memoDocumentKind)));
      
      if (hasNotDefaultRule || hasDefaultRule)
        return null;
      
      var rule = ApprovalRules.Create();
      rule.Status = Sungero.Docflow.ApprovalRuleBase.Status.Active;
      rule.Name = ruleName;
      rule.DocumentFlow = Sungero.Docflow.ApprovalRuleBase.DocumentFlow.Inner;
      rule.IsDefaultRule = true;
      
      SetRuleStages(rule, stages);
      
      // Добавить фильтр по виду документа = служебная записка.
      if (memoDocumentKind != null && !rule.DocumentKinds.Any())
        rule.DocumentKinds.AddNew().DocumentKind = memoDocumentKind;
      
      // Добавить этап уведомления.
      if (!rule.Stages.Any(s => s.StageType == StageType.Notice))
      {
        var stage = ApprovalStages.Create();
        stage.StageType = StageType.Notice;
        stage.Name = Resources.ReviewedNoticeName;
        stage.Subject = Resources.ReviewedNoticeSubject;
        stage.ApprovalRoles.AddNew().ApprovalRole = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.Initiator);
        stage.Save();
        
        rule.Stages.AddNew().Stage = stage;
      }
      Functions.ApprovalRuleBase.CreateAutoTransitions(rule);
      rule.Save();
      return rule;
    }
    
    /// <summary>
    /// Создать правило по умолчанию.
    /// </summary>
    /// <param name="ruleName">Имя правила.</param>
    /// <param name="documentFlow">Документопоток.</param>
    /// <param name="stages">Этапы.</param>
    /// <returns>Созданное правило, если правило создано не было - то null.</returns>
    [Public]
    public static IApprovalRule CreateDefaultRule(string ruleName, Enumeration documentFlow, List<Enumeration> stages)
    {
      var hasNotDefaultRule = ApprovalRuleBases.GetAll().Any(r => r.IsDefaultRule != true);
      var hasDefaultRule = ApprovalRuleBases.GetAll().Any(r => r.DocumentFlow == documentFlow);
      
      if (hasNotDefaultRule || hasDefaultRule)
        return null;
      
      var rule = ApprovalRules.Create();
      rule.Status = Sungero.Docflow.ApprovalRuleBase.Status.Active;
      rule.Name = ruleName;
      rule.DocumentFlow = documentFlow;
      rule.IsDefaultRule = true;
      
      SetRuleStages(rule, stages);
      Functions.ApprovalRuleBase.CreateAutoTransitions(rule);
      rule.Save();
      
      return rule;
    }
    
    /// <summary>
    /// Добавить условие по типу способа отправки.
    /// </summary>
    /// <param name="rule">Правило.</param>
    /// <param name="condition">Условие.</param>
    /// <param name="stageNumber">Номер этапа, который необходимо пропустить.</param>
    [Public]
    public static void AddConditionToRule(IApprovalRuleBase rule, IConditionBase condition, int stageNumber)
    {
      if (rule == null || condition == null)
        return;
      
      var targetStageTransition = rule.Transitions.Where(t => t.TargetStage == stageNumber).FirstOrDefault();
      var sourceStageTransition = rule.Transitions.Where(t => t.SourceStage == stageNumber).FirstOrDefault();
      var beforeStageNumber = targetStageTransition != null ? targetStageTransition.SourceStage ?? 0 : 0;
      var afterStageNumber = sourceStageTransition != null ? sourceStageTransition.TargetStage ?? 0 : 0;
      
      Sungero.Docflow.Functions.ApprovalRuleBase.AddConditionToRule(rule, condition, beforeStageNumber, afterStageNumber, stageNumber);
    }
    
    /// <summary>
    /// Создать условие "Подписывающий - непосредственный руководитель?".
    /// </summary>
    /// <param name="condition">Новое условие нужного типа - обычное или договорное.</param>
    /// <returns>Созданное условие.</returns>
    [Public]
    public static IConditionBase CreateRoleCompareSignatoryAndInitManagerCondition(IConditionBase condition)
    {
      condition.ConditionType = Docflow.ConditionBase.ConditionType.RolesComparer;
      condition.ApprovalRole = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.Signatory);
      condition.ApprovalRoleForComparison = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.InitManager);
      condition.Save();
      return condition;
    }
    
    /// <summary>
    /// Заполнить правило по умолчанию этапами.
    /// </summary>
    /// <param name="rule">Правило.</param>
    /// <param name="stages">Этапы.</param>
    [Public]
    public static void SetRuleStages(IApprovalRuleBase rule, List<Enumeration> stages)
    {
      if (rule == null)
        return;
      
      var alreadyExistStages = rule.Stages.Select(s => s.Stage).ToList();

      // Заполняем этапами.
      rule.Stages.Clear();
      foreach (var stage in stages)
      {
        rule.Stages.AddNew().Stage = alreadyExistStages.FirstOrDefault(s => s.StageType == stage) ??
          ApprovalStages.GetAll(s => s.StageType == stage).OrderBy(s => s.Id).FirstOrDefault() ??
          CreateApprovalStage(stage);
      }
    }
    
    /// <summary>
    /// Добавить этап преобразования в PDF в правило.
    /// </summary>
    /// <param name="rule">Правило.</param>
    [Public]
    public static void AddConvertPdfStage(IApprovalRuleBase rule)
    {
      if (rule == null)
        return;
      
      // Заполняем этапами.
      rule.Stages.AddNew().StageBase = ApprovalConvertPdfStages.GetAll().OrderBy(s => s.Id).FirstOrDefault() ??
        CreateApprovalConvertPdfStage();
    }
    
    /// <summary>
    /// Добавить этап рассмотрения несколькими адресатами в правило.
    /// </summary>
    /// <param name="rule">Правило.</param>
    [Public]
    public static void AddReviewTaskStage(IApprovalRuleBase rule)
    {
      if (rule == null)
        return;
      
      rule.Stages.AddNew().StageBase = ApprovalReviewTaskStages.GetAll().OrderBy(s => s.Id).FirstOrDefault() ??
        CreateApprovalReviewTaskStage();
    }
    
    /// <summary>
    /// Добавить этап передачи счета в бухгалтерию в правило.
    /// </summary>
    /// <param name="rule">Правило.</param>
    [Public]
    public static void AddGiveInvoiceApprovalStage(IApprovalRuleBase rule)
    {
      if (rule == null)
        return;
      
      var stageName = Sungero.Contracts.Resources.GiveInvoiceApprovalStageName;
      
      var giveInvoiceStage = Docflow.ApprovalStages.GetAll().FirstOrDefault(s => s.Name == stageName);
      if (giveInvoiceStage == null)
      {
        giveInvoiceStage = ApprovalStages.Create();
        giveInvoiceStage.StageType = StageType.SimpleAgr;
        giveInvoiceStage.DeadlineInDays = 1;
        giveInvoiceStage.ApprovalRoles.AddNew().ApprovalRole = Docflow.PublicFunctions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.Initiator);
        giveInvoiceStage.Subject = Sungero.Contracts.Resources.GiveInAccountant;
        giveInvoiceStage.Name = stageName;
        giveInvoiceStage.Save();
      }
      
      // Добавить этап передачи в правило.
      rule.Stages.AddNew().Stage = giveInvoiceStage;
    }
    
    /// <summary>
    /// Создать этап согласования.
    /// </summary>
    /// <param name="stageType">Тип этапа.</param>
    /// <returns>Этап согласования.</returns>
    public static IApprovalStage CreateApprovalStage(Enumeration stageType)
    {
      InitializationLogger.DebugFormat("Init: Create agreement stage {0}", stageType);
      
      var stage = ApprovalStages.Create();
      stage.StageType = stageType;
      stage.DeadlineInDays = 1;
      if (stageType == StageType.Manager || stageType == StageType.Print || stageType == StageType.Sign ||
          stageType == StageType.Register || stageType == StageType.Sending || stageType == StageType.Review ||
          stageType == StageType.Execution)
        stage.ReworkType = null;
      else
        stage.ReworkType = Docflow.ApprovalStage.ReworkType.AfterAll;
      
      // Создаем этапы согласования как этапы с доп. согласующими.
      if (stageType == StageType.Approvers)
      {
        stage.AllowAdditionalApprovers = true;
        stage.Name = Resources.ApproveAdditionalApprovers;
      }
      else
        stage.Name = stage.StageTypeAllowedItems.GetLocalizedValue(stage.StageType, System.Threading.Thread.CurrentThread.CurrentUICulture);
      
      // Дополнительные параметры для типа уведомления.
      if (stageType == StageType.Notice)
      {
        stage.Subject = ApprovalTasks.Resources.SignedNoticeSubject;
        stage.ApprovalRoles.AddNew().ApprovalRole = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.Initiator);
        stage.Name = Resources.SignedNoticeName;
      }
      
      // Дополнительные параметры для задания на возврат документа от контрагента.
      if (stageType == StageType.CheckReturn)
        stage.ApprovalRoles.AddNew().ApprovalRole = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.DocRegister);
      
      // Для подписания не требовать усиленной подписи.
      if (stageType == StageType.Sign)
        stage.NeedStrongSign = false;
      
      stage.Save();
      return stage;
    }
    
    /// <summary>
    /// Создать этап преобразования в PDF.
    /// </summary>
    /// <returns>Этап преобразования в PDF.</returns>
    public static IApprovalConvertPdfStage CreateApprovalConvertPdfStage()
    {
      InitializationLogger.DebugFormat("Init: Create approval stage to convert Pdf.");
      var stage = ApprovalConvertPdfStages.GetAll().OrderBy(s => s.Id).FirstOrDefault();
      if (stage != null)
        return stage;
      
      stage = ApprovalConvertPdfStages.Create();
      stage.Name = Sungero.Docflow.Resources.StageConvertPdfName;
      stage.TimeoutInHours = Sungero.Docflow.Constants.Module.DefaultApprovalConvertPdfTimeout;
      stage.Save();
      return stage;
    }
    
    /// <summary>
    /// Создать этап регистрации эл. доверенности/заявления на отзыв в реестре ФНС.
    /// </summary>
    /// <returns>Этап регистрации эл. доверенности/заявления на отзыв в реестре ФНС.</returns>
    public static IApprovalRegistrationFtsStage CreateApprovalRegistrationFtsStage()
    {
      InitializationLogger.DebugFormat("Init: Create approval stage for registration in FTS.");
      var stage = ApprovalRegistrationFtsStages.GetAll().OrderBy(s => s.Id).FirstOrDefault();
      if (stage != null)
        return stage;
      
      stage = ApprovalRegistrationFtsStages.Create();
      stage.Name = Sungero.Docflow.Resources.StageRegistrationInFts;
      stage.TimeoutInHours = Sungero.Docflow.Constants.ApprovalRegistrationFtsStage.DefaultTimeoutInHours;
      stage.TimeoutAction = Sungero.Docflow.ApprovalReviewTaskStage.TimeoutAction.Repeat;
      stage.Save();
      return stage;
    }
    
    /// <summary>
    /// Создать этап рассмотрения несколькими адресатами.
    /// </summary>
    /// <returns>Этап рассмотрения несколькими адресатами.</returns>
    public static IApprovalReviewTaskStage CreateApprovalReviewTaskStage()
    {
      InitializationLogger.DebugFormat("Init: Create approval stage for document review.");
      var stage = ApprovalReviewTaskStages.GetAll().OrderBy(s => s.Id).FirstOrDefault();
      if (stage != null)
        return stage;
      
      stage = ApprovalReviewTaskStages.Create();
      stage.Name = Sungero.Docflow.ApprovalReviewTaskStages.Resources.StageReviewTaskDefaultName;
      stage.WaitReviewTaskCompletion = false;
      stage.TimeoutAction = Sungero.Docflow.ApprovalReviewTaskStage.TimeoutAction.Repeat;
      stage.TimeoutInHours = Sungero.Docflow.Constants.ApprovalReviewTaskStage.DefaultTimeoutInHours;
      stage.DeadlineInDays = Sungero.Docflow.Constants.ApprovalReviewTaskStage.DefaultDeadlineInDays;
      stage.Save();
      return stage;
    }
    
    #endregion
    
    #region Создание настроек регистрации по умолчанию
    
    /// <summary>
    /// Создать журналы и настройки регистрации.
    /// </summary>
    public static void CreateDocumentRegistersAndSettings()
    {
      InitializationLogger.Debug("Init: Create default document registers and settings for docflow.");
      
      var memosRegister = CreateYearSectionDocumentRegister(Resources.RegistersAndSettingsMemosName,
                                                            Resources.RegistersAndSettingsMemosIndex,
                                                            Init.MemoRegister);
      var powerOfAttorneyRegister = CreateYearSectionDocumentRegister(Resources.RegistersAndSettingsPowerOfAttorneyName,
                                                                      Resources.RegistersAndSettingsPowerOfAttorneyIndex,
                                                                      Init.PowerOfAttorneyKind);
      CreateNumerationSetting(Memo.ClassTypeGuid, Docflow.RegistrationSetting.DocumentFlow.Inner, memosRegister);
      CreatePowerOfAttorneySetting(powerOfAttorneyRegister);
    }
    
    /// <summary>
    /// Создать журнал для внутреннего документа с разрезом нумерации по году.
    /// </summary>
    /// <param name="name">Название.</param>
    /// <param name="index">Индекс.</param>
    /// <param name="entityId">ИД инициализации.</param>
    /// <returns>Журнал.</returns>
    [Public]
    public static IDocumentRegister CreateYearSectionDocumentRegister(string name, string index, Guid entityId)
    {
      var documentRegister = CreateNumerationDocumentRegister(name, index, Docflow.DocumentRegister.DocumentFlow.Inner, entityId);
      
      if (documentRegister != null &&
          documentRegister.NumberingPeriod != Docflow.DocumentRegister.NumberingPeriod.Year)
        documentRegister.NumberingPeriod = Docflow.DocumentRegister.NumberingPeriod.Year;
      
      if (documentRegister != null &&
          documentRegister.NumberingSection != Docflow.DocumentRegister.NumberingSection.NoSection)
        documentRegister.NumberingSection = Docflow.DocumentRegister.NumberingSection.NoSection;
      
      return documentRegister;
    }
    
    /// <summary>
    /// Создать журнал для внутреннего документа.
    /// </summary>
    /// <param name="name">Название.</param>
    /// <param name="index">Индекс.</param>
    /// <param name="documentFlow">Документопоток.</param>
    /// <param name="entityId">ИД инициализации.</param>
    /// <returns>Журнал.</returns>
    [Public]
    public static IDocumentRegister CreateNumerationDocumentRegister(string name, string index, Enumeration documentFlow, Guid entityId)
    {
      var externalLink = Docflow.PublicFunctions.Module.GetExternalLink(DocumentRegister.ClassTypeGuid, entityId);
      if (externalLink != null)
        return DocumentRegisters.Null;
      
      var existingRegister = DocumentRegisters.GetAll(l => l.Name == name).FirstOrDefault();
      if (existingRegister != null)
      {
        Docflow.PublicFunctions.Module.CreateExternalLink(existingRegister, entityId);
        return existingRegister;
      }
      
      InitializationLogger.DebugFormat("Init: Create document register {0}", name);
      var documentRegister = DocumentRegisters.Create();
      documentRegister.Name = name;
      documentRegister.Index = index;
      documentRegister.RegisterType = Docflow.DocumentRegister.RegisterType.Numbering;
      documentRegister.NumberOfDigitsInNumber = 1;
      documentRegister.DocumentFlow = documentFlow;
      documentRegister.NumberingPeriod = Docflow.DocumentRegister.NumberingPeriod.Continuous;
      documentRegister.NumberingSection = Docflow.DocumentRegister.NumberingSection.NoSection;

      documentRegister.Save();
      
      Docflow.PublicFunctions.Module.CreateExternalLink(documentRegister, entityId);
      return documentRegister;
    }
    
    /// <summary>
    /// Создать настройку регистрации для доверенностей.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    [Public]
    public static void CreatePowerOfAttorneySetting(IDocumentRegister documentRegister)
    {
      if (documentRegister == null)
        return;
      
      var name = documentRegister.Name;
      InitializationLogger.DebugFormat("Init: Create numeration setting {0} for {1}", name, PowerOfAttorney.ClassTypeGuid);
      
      var settings = RegistrationSettings.GetAll().FirstOrDefault(s => s.Name == name) ?? RegistrationSettings.Create();
      settings.Name = name;
      settings.DocumentFlow = Docflow.RegistrationSetting.DocumentFlow.Inner;
      settings.SettingType = Docflow.RegistrationSetting.SettingType.Numeration;
      var allKinds = DocumentKinds
        .GetAll(k => k.DocumentType.DocumentTypeGuid == PowerOfAttorney.ClassTypeGuid.ToString() ||
                k.DocumentType.DocumentTypeGuid == FormalizedPowerOfAttorney.ClassTypeGuid.ToString())
        .ToList();
      foreach (var kind in allKinds)
        if (!settings.DocumentKinds.Any(k => Equals(k.DocumentKind, kind)))
          settings.DocumentKinds.AddNew().DocumentKind = kind;
      
      // Если настройка дублирует существующую - прекратить инициализацию настройки.
      if (Docflow.PublicFunctions.RegistrationSetting.Remote.GetDoubleSettings(settings).Any())
      {
        Docflow.PublicFunctions.Module.Remote.EvictEntityFromSession(settings);
        return;
      }
      
      settings.DocumentRegister = documentRegister;
      settings.Save();
    }
    
    /// <summary>
    /// Создать настройки регистрации.
    /// </summary>
    /// <param name="documentGuid">GUID типа документа.</param>
    /// <param name="documentFlow">Документопоток.</param>
    /// <param name="documentRegister">Журнал.</param>
    [Public]
    public static void CreateNumerationSetting(Guid documentGuid, Enumeration documentFlow, IDocumentRegister documentRegister)
    {
      if (documentRegister == null)
        return;
      
      var name = documentRegister.Name;
      InitializationLogger.DebugFormat("Init: Create numeration setting {0} for {1}", name, documentGuid);
      
      var settings = RegistrationSettings.GetAll().FirstOrDefault(s => s.Name == name) ?? RegistrationSettings.Create();
      settings.Name = name;
      settings.DocumentFlow = documentFlow;
      settings.SettingType = Docflow.RegistrationSetting.SettingType.Numeration;
      var allKinds = DocumentKinds.GetAll(k => k.DocumentType.DocumentTypeGuid == documentGuid.ToString()).ToList();
      foreach (var kind in allKinds)
        if (!settings.DocumentKinds.Any(k => Equals(k.DocumentKind, kind)))
          settings.DocumentKinds.AddNew().DocumentKind = kind;
      
      // Если настройка дублирует существующую - прекратить инициализацию настройки.
      if (Docflow.PublicFunctions.RegistrationSetting.Remote.GetDoubleSettings(settings).Any())
      {
        Docflow.PublicFunctions.Module.Remote.EvictEntityFromSession(settings);
        return;
      }
      
      settings.DocumentRegister = documentRegister;
      settings.Save();
    }
    
    #endregion
    
    #region Создание валют
    
    /// <summary>
    /// Создать базовые валюты.
    /// </summary>
    public static void CreateDefaultCurrencies()
    {
      InitializationLogger.Debug("Init: Create default currencies.");
      
      CreateCurrency(Resources.CurrencyFullNameRUB, Resources.CurrencyAlphaCodeRUB, Resources.CurrencyNumericCodeRUB, Resources.CurrencyShortNameRUB, Resources.CurrencyFractionNameRUB, true);
      CreateCurrency(Resources.CurrencyFullNameUSD, Resources.CurrencyAlphaCodeUSD, Resources.CurrencyNumericCodeUSD, Resources.CurrencyShortNameUSD, Resources.CurrencyFractionNameUSD, false);
      CreateCurrency(Resources.CurrencyFullNameEUR, Resources.CurrencyAlphaCodeEUR, Resources.CurrencyNumericCodeEUR, Resources.CurrencyShortNameEUR, Resources.CurrencyFractionNameEUR, false);
    }
    
    /// <summary>
    /// Создать валюту.
    /// </summary>
    /// <param name="name">Полное название валюты.</param>
    /// <param name="alphaCode">Буквенный код валюты.</param>
    /// <param name="numericCode">Цифровой код валюты.</param>
    /// <param name="shortName">Сокращенное название валюты.</param>
    /// <param name="fractionName">Название дробной части валюты.</param>
    /// <param name="isDefault">Признак валюты по умолчанию.</param>
    public static void CreateCurrency(string name, string alphaCode, string numericCode, string shortName, string fractionName, bool isDefault)
    {
      InitializationLogger.DebugFormat("Init: Create currency {0}", name);
      
      // Не создаются валюты с тем же кодом. Новые свойства дозаполняем.
      var existingCurrency = Currencies.GetAll(c => c.NumericCode == numericCode).FirstOrDefault();
      if (existingCurrency != null)
      {
        if (string.IsNullOrWhiteSpace(existingCurrency.ShortName))
          existingCurrency.ShortName = shortName;
        if (string.IsNullOrWhiteSpace(existingCurrency.FractionName))
          existingCurrency.FractionName = fractionName;
        existingCurrency.Save();
        return;
      }
      
      var currency = Currencies.Create();
      currency.Name = name;
      currency.AlphaCode = alphaCode;
      currency.NumericCode = numericCode;
      currency.ShortName = shortName;
      currency.FractionName = fractionName;
      currency.IsDefault = isDefault;
      currency.Save();
    }
    
    #endregion
    
    #region Создание ставок НДС
    
    /// <summary>
    /// Создать базовые ставки НДС.
    /// </summary>
    public static void CreateDefaultVatRates()
    {
      InitializationLogger.Debug("Init: Create default VAT Rates.");
      
      if (VatRates.GetAll().Count() > 0)
      {
        InitializationLogger.Debug("VAT Rates are already created.");
        return;
      }
      
      CreateVatRate(Constants.Module.VatRateWithoutVatSid,
                    Sungero.Commons.VatRates.Resources.DefaultVatRateWithoutVatName,
                    Constants.Module.DefaultVatRateWithoutVat);
      CreateVatRate(Constants.Module.VatRateZeroPercentSid,
                    Sungero.Commons.VatRates.Resources.DefaultVatRateZeroPercentName,
                    Constants.Module.DefaultVatRateZeroPercent);
      CreateVatRate(Constants.Module.VatRateTenPercentSid,
                    Sungero.Commons.VatRates.Resources.DefaultVatRateTenPercentName,
                    Constants.Module.DefaultVatRateTenPercent);
      CreateVatRate(Constants.Module.VatRateTwentyPercentSid,
                    Sungero.Commons.VatRates.Resources.DefaultVatRateTwentyPercentName,
                    Constants.Module.DefaultVatRateTwentyPercent);
    }
    
    /// <summary>
    /// Создать ставку НДС.
    /// </summary>
    /// <param name="sid">Sid.</param>
    /// <param name="name">Наименование ставки НДС.</param>
    /// <param name="rate">Ставка НДС в %.</param>
    public static void CreateVatRate(string sid, string name, int rate)
    {
      InitializationLogger.DebugFormat("Init: Create Vat Rate {0}", name);
      
      var vatRate = VatRates.Create();
      vatRate.Sid = sid;
      vatRate.Name = name;
      vatRate.Rate = rate;
      vatRate.Save();
    }
    
    #endregion
    
    #region Создание ролей согласования
    
    /// <summary>
    /// Создать базовые роли согласования.
    /// </summary>
    public static void CreateDefaultApprovalRoles()
    {
      InitializationLogger.Debug("Init: Create default approval roles.");
      
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.Addressee, Docflow.Resources.RoleDescriptionAddressee);
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.Addressees, Sungero.Docflow.Resources.RoleDescriptionAddressees);
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.AddrAssistant, Docflow.Resources.RoleDescriptionAddresseeAssistant);
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.DocRegister, Docflow.Resources.RoleDescriptionDocumentRegister);
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.Initiator, Docflow.Resources.RoleDescriptionInitiator);
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.InitManager, Docflow.Resources.RoleDescriptionInitiatorManager);
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.OutDocRegister, Docflow.Resources.RoleDescriptionOutgoingDocumentRegister);
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.PrintResp, Docflow.Resources.RoleDescriptionPrintResponsible);
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.SignAssistant, Docflow.Resources.RoleDescriptionSignatoryAssistant);
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.Signatory, Docflow.Resources.RoleDescriptionSignatory);
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.Approvers, Docflow.Resources.RoleDescriptionApprovers);
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.CompResponsible, Docflow.Resources.RoleDescriptionCompanyResponsible);
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.DepartManager, Docflow.Resources.RoleDescriptionDepartmentManager);
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.DocDepManager, Docflow.Resources.RoleDescriptionDocumentDepartmentManager);
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.ChiefAccountingOfficer, Docflow.Resources.RoleDescriptionCAO);
    }
    
    /// <summary>
    /// Создать роль согласования.
    /// </summary>
    /// <param name="roleType">Тип роли.</param>
    /// <param name="description">Описание роли.</param>
    public static void CreateApprovalRole(Enumeration roleType, string description)
    {
      InitializationLogger.DebugFormat("Init: Create approval role {0}", ApprovalRoleBases.Info.Properties.Type.GetLocalizedValue(roleType));
      
      var role = ApprovalRoles.GetAll().Where(r => Equals(r.Type, roleType)).FirstOrDefault();
      if (role == null)
        role = ApprovalRoles.Create();
      
      role.Type = roleType;
      role.Description = description;
      role.Save();
    }
    
    #endregion
    
    #region Создание типов связей
    
    /// <summary>
    /// Создать базовые типы связей.
    /// </summary>
    public static void CreateDefaultRelationTypes()
    {
      InitializationLogger.Debug("Init: Create default relation types.");
      
      // Приложение к документу.
      var addendum = CreateRelationType(Constants.Module.AddendumRelationName, Resources.RelationAddendumSourceTitle,
                                        Resources.RelationAddendumTargetTitle, Resources.RelationAddendumSourceTitle,
                                        Resources.RelationAddendumDescription, true, false, false, true);
      addendum.Mapping.Clear();
      var addendumRow = addendum.Mapping.AddNew();
      addendumRow.Source = Content.ElectronicDocuments.Info;
      addendumRow.Target = Content.ElectronicDocuments.Info;
      addendumRow = addendum.Mapping.AddNew();
      addendumRow.Source = Content.ElectronicDocuments.Info;
      addendumRow.Target = Docflow.Addendums.Info;
      addendumRow.RelatedProperty = Docflow.Addendums.Info.Properties.LeadingDocument;
      addendum.Save();
      
      // Ответное письмо.
      var response = CreateRelationType(Constants.Module.ResponseRelationName, Resources.RelationResponseSourceTitle,
                                        Resources.RelationResponseTargetTitle, Resources.RelationResponseSourceTitle,
                                        Resources.RelationResponseDescription, false, false, true, false);
      response.Mapping.Clear();
      var responseRow = response.Mapping.AddNew();
      responseRow.Source = RecordManagement.IncomingLetters.Info;
      responseRow.Target = RecordManagement.OutgoingLetters.Info;
      responseRow = response.Mapping.AddNew();
      responseRow.Source = RecordManagement.OutgoingLetters.Info;
      responseRow.Target = RecordManagement.IncomingLetters.Info;
      response.Save();
      
      // Корректировка.
      var correction = CreateRelationType(Constants.Module.CorrectionRelationName, Resources.RelationCorrectionSourceTitle,
                                          Resources.RelationCorrectionTargetTitle, Resources.RelationCorrectionSourceTitle,
                                          Resources.RelationCorrectionTargetTitle, true, false, false, true);
      correction.Mapping.Clear();
      var correctionRow = correction.Mapping.AddNew();
      correctionRow.Source = Sungero.Docflow.AccountingDocumentBases.Info;
      correctionRow.Target = Sungero.Docflow.AccountingDocumentBases.Info;
      correctionRow.RelatedProperty = Sungero.Docflow.AccountingDocumentBases.Info.Properties.Corrected;
      correction.Save();
      
      // Обновить системные типы связей.
      UpdateSystemRelationTypes();
    }
    
    public static void UpdateSystemRelationTypes()
    {
      // Основание.
      var basis = UpdateSystemRelationType(Constants.Module.BasisRelationName,
                                           Resources.RelationBasisSourceTitle, Resources.RelationBasisTargetTitle,
                                           Resources.RelationBasisSourceDescription, Resources.RelationBasisTargetDescription);
      if (basis == null)
        InitializationLogger.Debug("Init: No relation type named Basis.");
      
      if (basis != null)
      {
        basis.Mapping.Clear();
        var basisRow = basis.Mapping.AddNew();
        basisRow.Source = Meetings.Minuteses.Info;
        basisRow.Target = Content.ElectronicDocuments.Info;
        basisRow = basis.Mapping.AddNew();
        basisRow.Source = Content.ElectronicDocuments.Info;
        basisRow.Target = Content.ElectronicDocuments.Info;
        basis.Save();
      }
      
      // Переписка.
      var correspondence = UpdateSystemRelationType(Constants.Module.CorrespondenceRelationName,
                                                    Resources.RelationCorrespondenceSourceTitle, Resources.RelationCorrespondenceTargetTitle,
                                                    Resources.RelationCorrespondenceSourceDescription, Resources.RelationCorrespondenceTargetDescription);
      if (correspondence == null)
        InitializationLogger.Debug("Init: No relation type named Correspondence.");
      
      if (correspondence != null)
      {
        correspondence.Mapping.Clear();
        var correspondenceRow = correspondence.Mapping.AddNew();
        correspondenceRow.Source = Content.ElectronicDocuments.Info;
        correspondenceRow.Target = RecordManagement.IncomingLetters.Info;
        correspondenceRow = correspondence.Mapping.AddNew();
        correspondenceRow.Source = Content.ElectronicDocuments.Info;
        correspondenceRow.Target = RecordManagement.OutgoingLetters.Info;
        correspondenceRow = correspondence.Mapping.AddNew();
        correspondenceRow.Source = Content.ElectronicDocuments.Info;
        correspondenceRow.Target = Docflow.Memos.Info;
        correspondence.Save();
      }
      
      // Прочие.
      UpdateSystemRelationType(Constants.Module.SimpleRelationName,
                               Resources.RelationSimpleRelationSourceTitle, Resources.RelationSimpleRelationTargetTitle,
                               Resources.RelationSimpleRelationSourceDescription, Resources.RelationSimpleRelationTargetDescription);
      
      // Отменяет.
      UpdateSystemRelationType(Constants.Module.CancelRelationName,
                               Resources.RelationCancelSourceTitle, Resources.RelationCancelTargetTitle,
                               Resources.RelationCancelSourceDescription, Resources.RelationCancelTargetDescription);
    }
    
    public static IRelationType UpdateSystemRelationType(string name, string sourceTitle, string targetTitle,
                                                         string sourceDescription, string targetDescription)
    {
      var relationType = RelationTypes.GetAll(r => r.Name == name).FirstOrDefault();
      if (relationType == null)
      {
        InitializationLogger.DebugFormat("Init: No relation type named {0}.", name);
        return null;
      }
      
      relationType.SourceDescription = sourceDescription;
      relationType.TargetDescription = targetDescription;
      relationType.TargetTitle = targetTitle;
      relationType.SourceTitle = sourceTitle;
      relationType.Save();
      return relationType;
    }
    
    /// <summary>
    /// Создать тип связи.
    /// </summary>
    /// <param name="name">Название связи.</param>
    /// <param name="sourceTitle">Название источника.</param>
    /// <param name="targetTitle">Название назначения.</param>
    /// <param name="sourceDescription">Описание источника.</param>
    /// <param name="targetDescription">Описание назначения.</param>
    /// <param name="hasDirection">Признак направления.</param>
    /// <param name="needRight">Признак требования прав на изменение источника.</param>
    /// <param name="useSource">Показывать выбор связи источника.</param>
    /// <param name="useTarget">Показывать выбор связи назначения.</param>
    /// <returns>Созданная связь.</returns>
    [Public]
    public static IRelationType CreateRelationType(string name, string sourceTitle, string targetTitle,
                                                   string sourceDescription, string targetDescription, bool hasDirection = true,
                                                   bool needRight = true, bool useSource = false, bool useTarget = true)
    {
      InitializationLogger.DebugFormat("Init: Create relation type {0}, from {1} to {2}", name, sourceTitle, targetTitle);
      
      var relationType = RelationTypes.GetAll(r => r.Name == name).FirstOrDefault() ?? RelationTypes.Create();
      relationType.Name = name;
      relationType.SourceDescription = sourceDescription;
      relationType.TargetDescription = targetDescription;
      relationType.TargetTitle = targetTitle;
      relationType.SourceTitle = sourceTitle;
      relationType.HasDirection = hasDirection;
      relationType.NeedSourceUpdateRights = needRight;
      relationType.UseSource = useSource;
      relationType.UseTarget = useTarget;
      relationType.IsSystem = true;
      return relationType;
    }
    
    #endregion
    
    #region Создание таблицы очередных номеров журналов регистрации
    
    [Public]
    public static void CreateDocumentRegisterNumberTable()
    {
      var createTable = Queries.Module.CreateSungeroDocregisterCurrentNumbersTable;
      var createIndices = Queries.Module.CreateIndicesSungeroDocRegisterCurrentNumber;
      var createGetNextNumberProcedure = Queries.Module.CreateProcedureSungeroDocRegisterGetNextNumber;
      var createSetCurrentNumberProcedure = Queries.Module.CreateProcedureSungeroDocRegisterSetCurrentNumber;
      
      Docflow.PublicFunctions.Module.ExecuteSQLCommand(createTable);
      Docflow.PublicFunctions.Module.ExecuteSQLCommand(createIndices);
      Docflow.PublicFunctions.Module.ExecuteSQLCommand(createGetNextNumberProcedure);
      Docflow.PublicFunctions.Module.ExecuteSQLCommand(createSetCurrentNumberProcedure);
    }
    
    #endregion
    
    #region Создание таблицы параметров
    
    /// <summary>
    /// Создать таблицу Sungero_Docflow_Params.
    /// </summary>
    [Public]
    public static void CreateParametersTable()
    {
      var createTableQuery = Queries.Module.CreateParametersTable;
      Docflow.PublicFunctions.Module.ExecuteSQLCommand(createTableQuery);
    }
    
    #endregion
    
    #region Создание форм документов
    
    /// <summary>
    /// Создать формы документов.
    /// </summary>
    public static void CreateMediumTypes()
    {
      InitializationLogger.Debug("Init: Create default medium types.");
      CreateMediumType(MediumTypes.Resources.ElectronicMediumTypeName, Constants.MediumType.ElectronicMediumTypeSid, CoreEntities.DatabookEntry.Status.Active);
      CreateMediumType(MediumTypes.Resources.PaperMediumTypeName, Constants.MediumType.PaperMediumTypeSid, CoreEntities.DatabookEntry.Status.Active);
      CreateMediumType(MediumTypes.Resources.MixedMediumTypeName, Constants.MediumType.MixedMediumTypeSid, CoreEntities.DatabookEntry.Status.Closed);
    }
    
    /// <summary>
    /// Создать вид носителя документа.
    /// </summary>
    /// <param name="name">Название.</param>
    /// <param name="sid">Уникальный ИД, регистрозависимый.</param>
    /// <param name="status">Состояние.</param>
    /// <returns>Вид носителя документа.</returns>
    [Public]
    public static IMediumType CreateMediumType(string name, string sid, Enumeration status)
    {
      InitializationLogger.DebugFormat("Init: Create medium type {0}", name);
      
      var mediumType = MediumTypes.GetAll(m => m.Sid == sid).FirstOrDefault();
      if (mediumType == null)
      {
        mediumType = MediumTypes.Create();
        mediumType.Sid = sid;
      }
      
      mediumType.Name = name;
      mediumType.Status = status;
      mediumType.Save();
      
      CreateMediumTypeExternalLink(mediumType, Guid.Parse(sid));
      
      return mediumType;
    }
    
    /// <summary>
    /// Создать ExternalLink для вида носителя документа, если он не существует.
    /// </summary>
    /// <param name="mediumType">Вид носителя документа.</param>
    /// <param name="externalEntityId">Уникальный идентификатор ExternalLink вида носителя документа.</param>
    public static void CreateMediumTypeExternalLink(Sungero.Docflow.IMediumType mediumType, Guid externalEntityId)
    {
      var externalLink = Docflow.PublicFunctions.Module.GetExternalLink(MediumType.ClassTypeGuid, externalEntityId);
      if (externalLink == null)
      {
        InitializationLogger.DebugFormat("Init: Create \"{0}\" medium type external link", mediumType.Name);
        Docflow.PublicFunctions.Module.CreateExternalLink(mediumType, externalEntityId);
      }
    }
    
    #endregion
    
    #region Создание видов отметок
    
    public static void CreateDefaultMarkKinds()
    {
      InitializationLogger.Debug("Init: Create default mark kinds.");
      CreateMarkKind(MarkKinds.Resources.SignatureMarkKindName, Constants.MarkKind.ElectronicSignatureMarkKindSid,
                     Constants.MarkKind.ElectronicSignatureMarkKindClassName, Constants.MarkKind.ElectronicSignatureMarkKindFunctionName);
      CreateMarkKind(MarkKinds.Resources.ReceiptMarkKindName, Constants.MarkKind.ReceiptMarkKindSid,
                     Constants.MarkKind.ReceiptMarkKindClassName, Constants.MarkKind.ReceiptMarkKindFunctionName);
      CreateMarkKind(MarkKinds.Resources.RegistrationDateMarkKindName, Constants.MarkKind.RegistrationDateMarkKindSid,
                     Constants.MarkKind.RegDateMarkKindClassName, Constants.MarkKind.RegDateMarkKindFunctionName);
      CreateMarkKind(MarkKinds.Resources.RegistrationNumberMarkKindName, Constants.MarkKind.RegistrationNumberMarkKindSid,
                     Constants.MarkKind.RegNumberMarkKindClassName, Constants.MarkKind.RegNumberMarkKindFunctionName);
    }
    
    [Public]
    public static IMarkKind CreateMarkKind(string name, string sid, string markContentClassName, string markContentFunctionName)
    {
      InitializationLogger.DebugFormat("Init: Create mark kind {0}", name);
      
      var markKind = MarkKinds.GetAll(mk => mk.Sid == sid).FirstOrDefault();
      if (markKind == null)
      {
        markKind = MarkKinds.Create();
        markKind.Sid = sid;
        markKind.MarkContentClassName = markContentClassName;
        markKind.MarkContentFunctionName = markContentFunctionName;
      }
      
      markKind.Name = name;
      markKind.Save();
      
      return markKind;
    }
    
    #endregion
    
    #region Добавление в таблицу параметров
    
    /// <summary>
    /// Добавить в таблицу параметров признак, определяющий способ преобразования в Pdf с отметками.
    /// </summary>
    public static void AddUseObsoletePdfConversionParam()
    {
      InitializationLogger.Debug("Init: Adding 'UseObsoletePdfConversion' parameter.");
      Docflow.PublicFunctions.Module.InsertDocflowParam(Constants.Module.UseObsoletePdfConversionParamName, "false");
    }
    
    /// <summary>
    /// Добавить в таблицу параметров адрес веб-сервиса проверки контрагентов.
    /// </summary>
    public static void AddCompanyDataServiceParam()
    {
      Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.CompanyDataServiceKey, Constants.Module.CompanyDataServiceDefaultURL);
    }
    
    /// <summary>
    /// Добавить в таблицу параметров настройку для рассылки по умолчанию.
    /// </summary>
    public static void AddDisableMailNotificationParam()
    {
      InitializationLogger.Debug("Init: Adding summary mail notifications disable parameter.");
      Docflow.PublicFunctions.Module.InsertDocflowParam(Constants.Module.DisableMailNotification, "false");
    }
    
    /// <summary>
    /// Добавить в таблицу параметров срок хранения результатов сравнения по умолчанию.
    /// </summary>
    public static void AddDaysToStoreDocumentComparisonInfoParam()
    {
      Docflow.PublicFunctions.Module.InsertDocflowParam(Constants.DocumentComparisonInfo.DaysToStoreParamName,
                                                        Constants.DocumentComparisonInfo.DefaultDaysToStore.ToString());
    }
    
    /// <summary>
    /// Добавить в таблицу параметров количество пользователей для отправки писем со сводкой по умолчанию.
    /// </summary>
    public static void AddSummaryMailEmployeesBunchSizeParam()
    {
      Docflow.PublicFunctions.Module.InsertDocflowParam(Constants.Module.SummaryMailEmployeesBunchSizeParamName,
                                                        Constants.Module.SummaryMailEmployeesBunchSize.ToString());
    }
    
    /// <summary>
    /// Добавить в таблицу параметров количество заданий для отправки писем о новых/просроченных заданиях по умолчанию.
    /// </summary>
    public static void AddMailAssignmentsBunchSizeParam()
    {
      Docflow.PublicFunctions.Module.InsertDocflowParam(Constants.Module.MailAssignmentsBunchSizeParamName,
                                                        Constants.Module.MailAssignmentsBunchSize.ToString());
    }

    #endregion

    #region Создание индексов
    
    public static void CreateTaskIndices()
    {
      var tableName = Sungero.Docflow.Constants.Module.SugeroWFTaskTableName;
      var indexName = "idx_Assignment_Task_Discr_ExecutionState_IsCompound_Status";
      var indexQuery = string.Format(Queries.Module.SungeroWFTaskIndex1Query, tableName, indexName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);

      indexName = "idx_Assignment_Task_Discriminator_Status";
      indexQuery = string.Format(Queries.Module.SungeroWFTaskIndex2Query, tableName, indexName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
    }
    
    public static void CreateAssignmentIndices()
    {
      var tableName = Sungero.Docflow.Constants.Module.SungeroWFAssignmentTableName;
      var indexName = "idx_Asg_Discriminator_Performer_Author_MTask_ComplBy_Created";
      var indexQuery = string.Format(Queries.Module.SungeroWFAssignmentIndex0Query, tableName, indexName);
      
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_Asg_Discr_Perf_Status_Deadline_Complted_Created";
      indexQuery = string.Format(Queries.Module.SungeroWFAssignmentIndex1Query, tableName, indexName);
      
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
    }
    
    public static void CreateEDocIndices()
    {
      var tableName = Sungero.Docflow.Constants.Module.SugeroContentEDocTableName;

      var indexName = "idx_EDoc_Discriminator_Created_LifeCycleState";
      var indexQuery = string.Format(Queries.Module.SungeroContentEDocIndex0Query, tableName, indexName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);

      indexName = "idx_EDoc_Discriminator_RegState_RegDate_RegNumber_Created";
      indexQuery = string.Format(Queries.Module.SungeroContentEDocIndex1Query, tableName, indexName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);

      indexName = "idx_EDoc_Index_DocRegister_Id_Discriminator";
      indexQuery = string.Format(Queries.Module.SungeroContentEDocIndex2Query, tableName, indexName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);

      indexName = "idx_EDoc_Discriminator_DocumentKind";
      indexQuery = string.Format(Queries.Module.SungeroContentEDocIndex4Query, tableName, indexName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);

      indexName = "idx_EDoc_DocKind_DocDate_Modified_Storage";
      indexQuery = string.Format(Queries.Module.SungeroContentEDocIndex7Query, tableName, indexName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_Topic";
      indexQuery = string.Format(Queries.Module.SungeroContentEDocIndex8Query, tableName, indexName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_Subtopic";
      indexQuery = string.Format(Queries.Module.SungeroContentEDocIndex9Query, tableName, indexName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_LeadDoc_Discriminator";
      indexQuery = string.Format(Queries.Module.SungeroContentEDocIndex10Query, tableName, indexName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_DocumentDate_Discriminator";
      var columnName = "DocumentDate_Docflow_Sungero";
      indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_CaseFile_Discriminator";
      columnName = "CaseFile_Docflow_Sungero";
      indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_SchedRetDate_Discriminator";
      columnName = "SchedRetDate_Docflow_Sungero";
      indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_DaysToFinishWo_Discriminator";
      columnName = "DaysToFinishWo_Docflow_Sungero";
      indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_IssuedToParty_Discriminator";
      columnName = "IssuedToParty_Docflow_Sungero";
      indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_RegDate_Discriminator";
      columnName = "RegDate_Docflow_Sungero";
      indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_AccCParty_Discriminator";
      columnName = "AccCParty_Docflow_Sungero";
      indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_BusinessUnit_Discriminator";
      columnName = "BusinessUnit_Docflow_Sungero";
      indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_Department_Discriminator";
      columnName = "Department_Docflow_Sungero";
      indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_Counterparty_Discriminator";
      columnName = "Counterparty_Docflow_Sungero";
      indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_DocRegister_Discriminator";
      columnName = "DocRegister_Docflow_Sungero";
      indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_DocumentGroup_Discriminator";
      columnName = "DocumentGroup_Docflow_Sungero";
      indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_InCorr_Discriminator";
      columnName = "InCorr_Docflow_Sungero";
      indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_RespEmpl_Discriminator";
      columnName = "RespEmpl_Contrac_Sungero";
      indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_EDoc_RegState_Discriminator";
      columnName = "RegState_Docflow_Sungero";
      indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
    }
    
    public static void CreateOfficialDocumentTrackingIndices()
    {
      var tableName = Sungero.Docflow.Constants.Module.SugeroOffDocTrackingTableName;
      
      var indexName = "idx_Tracking_ReturnDeadline_Discriminator";
      var columnName = "ReturnDeadline";
      var indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_Tracking_ReturnResult_Discriminator";
      columnName = "ReturnResult";
      indexQuery = string.Format(Queries.Module.FieldWithDiscriminatorIndexQuery, tableName, indexName, columnName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
    }
    
    public static void CreateMarkIndices()
    {
      var tableName = Sungero.Docflow.Constants.Module.SungeroMarkTableName;
      var indexName = "idx_Mark_DocumentId_VersionId";
      var indexQuery = string.Format(Queries.Module.SungeroDocflowMarkIndexQuery, tableName, indexName);
      Functions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
    }
    
    #endregion
    
    #region Отчеты
    
    /// <summary>
    /// Создать таблицы для отчетов.
    /// </summary>
    public static void CreateReportsTables()
    {
      var approvalRuleCardReportTableName = Constants.ApprovalRuleCardReport.CriteriaTableName;
      var approvalRuleCardReportConditionTableName = Constants.ApprovalRuleCardReport.ConditionTableName;
      var approvalRuleCardReportSignatureSettingsTableName = Constants.ApprovalRuleCardReport.SignatureSettingsTableName;
      var approvalRulesConsolidatedReportTableName = Constants.ApprovalRulesConsolidatedReport.SourceTableName;
      var skippedIndexes = Constants.SkippedNumbersReport.SkipsTableName;
      var availableDocs = Constants.SkippedNumbersReport.AvailableDocumentsTableName;
      var exchangeServiceDocumentsTableName = Constants.ExchangeServiceDocumentReport.SourceTableName;
      var apprSheetReportTableName = Constants.ApprovalSheetReport.SourceTableName;
      var envelopesReportsTableName = Constants.EnvelopeC4Report.EnvelopesTableName;
      var regSettingsReportTableName = Constants.RegistrationSettingReport.SourceTableName;
      var mailRegisterReportTableName = Constants.MailRegisterReport.SourceTableName;
      var exchangeOrderReportTableName = Constants.ExchangeOrderReport.SourceTableName;
      var distributionSheetReportTableName = Sungero.Docflow.Constants.DistributionSheetReport.SourceTableName;
      var emplAsgCompletionReportTableName = Sungero.Docflow.Constants.EmployeesAssignmentCompletionReport.SourceTableName;
      var employeeAssignmentsReportTableName = Sungero.Docflow.Constants.EmployeeAssignmentsReport.SourceTableName;
      var depAsgCompletionReportTableName = Sungero.Docflow.Constants.DepartmentsAssignmentCompletionReport.SourceTableName;
      
      Docflow.PublicFunctions.Module.DropReportTempTables(new[] {
                                                            approvalRuleCardReportTableName,
                                                            approvalRuleCardReportConditionTableName,
                                                            approvalRuleCardReportSignatureSettingsTableName,
                                                            approvalRulesConsolidatedReportTableName,
                                                            skippedIndexes,
                                                            availableDocs,
                                                            envelopesReportsTableName,
                                                            exchangeServiceDocumentsTableName,
                                                            regSettingsReportTableName,
                                                            apprSheetReportTableName,
                                                            mailRegisterReportTableName,
                                                            exchangeOrderReportTableName,
                                                            distributionSheetReportTableName,
                                                            emplAsgCompletionReportTableName,
                                                            employeeAssignmentsReportTableName,
                                                            depAsgCompletionReportTableName
                                                          });
      
      Functions.Module.ExecuteSQLCommandFormat(Queries.ApprovalRuleCardReport.CreateCriteriaSourceTable, new[] { approvalRuleCardReportTableName });
      Functions.Module.ExecuteSQLCommandFormat(Queries.ApprovalRuleCardReport.CreateConditionsSourceTable, new[] { approvalRuleCardReportConditionTableName });
      Functions.Module.ExecuteSQLCommandFormat(Queries.ApprovalRuleCardReport.CreateSignatureSettingsTable, new[] { approvalRuleCardReportSignatureSettingsTableName });
      Functions.Module.ExecuteSQLCommandFormat(Queries.ApprovalRulesConsolidatedReport.CreateSourceTable, new[] { approvalRulesConsolidatedReportTableName });
      Functions.Module.ExecuteSQLCommandFormat(Queries.SkippedNumbersReport.SkippedIndexes, new[] { skippedIndexes });
      Functions.Module.ExecuteSQLCommandFormat(Queries.SkippedNumbersReport.AvaliableDocuments, new[] { availableDocs });
      Functions.Module.ExecuteSQLCommandFormat(Queries.ApprovalSheetReport.CreateReportTable, new[] { apprSheetReportTableName });
      Functions.Module.ExecuteSQLCommandFormat(Queries.ExchangeServiceDocumentReport.CreateSourceTable, new[] { exchangeServiceDocumentsTableName });
      Functions.Module.ExecuteSQLCommandFormat(Queries.EnvelopeC4Report.CreateEnvelopesTable, new[] { envelopesReportsTableName });
      Functions.Module.ExecuteSQLCommandFormat(Queries.RegistrationSettingReport.CreateSourceTable, new[] { regSettingsReportTableName });
      Functions.Module.ExecuteSQLCommandFormat(Queries.MailRegisterReport.CreateSourceTable, new[] { mailRegisterReportTableName });
      Functions.Module.ExecuteSQLCommandFormat(Queries.ExchangeOrderReport.CreateSourceTable, new[] { exchangeOrderReportTableName });
      Functions.Module.ExecuteSQLCommandFormat(Queries.DistributionSheetReport.CreateSourceTable, new[] { distributionSheetReportTableName });
      Functions.Module.ExecuteSQLCommandFormat(Queries.EmployeesAssignmentCompletionReport.CreateSourceTable, new[] { emplAsgCompletionReportTableName });
      Functions.Module.ExecuteSQLCommandFormat(Queries.EmployeeAssignmentsReport.CreateSourceTable, new[] { employeeAssignmentsReportTableName });
      Functions.Module.ExecuteSQLCommandFormat(Queries.DepartmentsAssignmentCompletionReport.CreateSourceTable, new[] { depAsgCompletionReportTableName });
    }

    /// <summary>
    /// Создать таблицы для отчетов.
    /// </summary>
    public static void CreateReportsTables410()
    {
      var documentUsageReportEmployeeTableName = Sungero.Docflow.Constants.DocumentUsageReport.EmployeeTableName;
      var documentUsageReportHistoryTableName = Sungero.Docflow.Constants.DocumentUsageReport.HistoryTableName;
      
      Docflow.PublicFunctions.Module.DropReportTempTables(new[] {
                                                            documentUsageReportEmployeeTableName,
                                                            documentUsageReportHistoryTableName
                                                          });
      
      Functions.Module.ExecuteSQLCommandFormat(Queries.DocumentUsageReport.CreateEmployeeTable, new[] { documentUsageReportEmployeeTableName });
      Functions.Module.ExecuteSQLCommandFormat(Queries.DocumentUsageReport.CreateHistoryTable, new[] { documentUsageReportHistoryTableName });
    }
    
    /// <summary>
    /// Выдать права на отчеты.
    /// </summary>
    public static void GrantRightsOnReports()
    {
      // Выдача прав на отчеты роли "Руководители наших организаций".
      var businessUnitManagers = Roles.GetAll(r => r.Sid == Docflow.Constants.Module.RoleGuid.BusinessUnitHeadsRole).SingleOrDefault();
      if (businessUnitManagers != null)
      {
        Reports.AccessRights.Grant(Reports.GetEmployeesAssignmentCompletionReport().Info, businessUnitManagers, DefaultReportAccessRightsTypes.Execute);
        Reports.AccessRights.Grant(Reports.GetDepartmentsAssignmentCompletionReport().Info, businessUnitManagers, DefaultReportAccessRightsTypes.Execute);
      }
      
      // Выдача прав на отчеты роли "Руководители подразделений".
      var departmentManagers = Roles.GetAll(r => r.Sid == Docflow.Constants.Module.RoleGuid.DepartmentManagersRole).SingleOrDefault();
      if (departmentManagers != null)
      {
        Reports.AccessRights.Grant(Reports.GetEmployeesAssignmentCompletionReport().Info, departmentManagers, DefaultReportAccessRightsTypes.Execute);
        Reports.AccessRights.Grant(Reports.GetDepartmentsAssignmentCompletionReport().Info, departmentManagers, DefaultReportAccessRightsTypes.Execute);
      }
      
      // Выдача прав на отчеты роли "Пользователи с расширенным доступом к исполнительской дисциплине".
      var usersWithAssignmentCompletionRights = Roles.GetAll(r => r.Sid == Docflow.Constants.Module.RoleGuid.UsersWithAssignmentCompletionRightsRole).SingleOrDefault();
      if (usersWithAssignmentCompletionRights != null)
      {
        Reports.AccessRights.Grant(Reports.GetEmployeesAssignmentCompletionReport().Info, usersWithAssignmentCompletionRights, DefaultReportAccessRightsTypes.Execute);
        Reports.AccessRights.Grant(Reports.GetDepartmentsAssignmentCompletionReport().Info, usersWithAssignmentCompletionRights, DefaultReportAccessRightsTypes.Execute);
      }
      
    }
    
    #endregion
    
    /// <summary>
    /// Проверить разрешение на запуск инициализации.
    /// </summary>
    [Public]
    public static void CheckRightsToInitialize()
    {
      if (!Users.Current.IncludedIn(Roles.Administrators))
        throw AppliedCodeException.Create("Only administrator can run initialization.");
      
      var tenantCulture = Sungero.Core.TenantInfo.Culture;
      if (!tenantCulture.Equals(System.Threading.Thread.CurrentThread.CurrentCulture))
        throw AppliedCodeException.Create(string.Format("Set '{0}' language in '_ConfigSettings.xml'.", tenantCulture));
    }
    
    /// <summary>
    /// Получить первое значение зарезервированного диапазона идентификаторов.
    /// </summary>
    /// <param name="tableName">Название таблицы.</param>
    /// <param name="count">Количество зарезервированных идентификаторов.</param>
    /// <returns>Первое значение зарезервированного диапазона идентификаторов.</returns>
    private static long GetStartNewId(string tableName, int count)
    {
      var result = Docflow.Functions.Module.ExecuteScalarSQLCommand(Queries.Module.ReservIdsQuery, new[] { tableName, count.ToString() });
      return Convert.ToInt64(result);
    }
    
    public static void ConvertDocumentTemplates()
    {
      var baseType = new Guid("9abcf1b7-f630-4a82-9912-7f79378ab199");
      var templates = Sungero.Content.ElectronicDocumentTemplates.GetAll()
        .Where(t => t.TypeDiscriminator == baseType)
        .ToList();
      foreach (var template in templates)
      {
        // При смене типа теряется поле DocumentType баг 101302.
        var documentType = template.DocumentType;
        var documentTemplate = template.ConvertTo(DocumentTemplates.Info);
        DocumentTemplates.As(documentTemplate).DocumentType = documentType;
        documentTemplate.Save();
        Logger.Debug(string.Format("Convert template {0} OK!", documentTemplate.Name));
      }
    }
    
    #region Добавление в таблицу параметров значения количества писем в пакете
    
    public static void AddSummaryMailNotificationsBunchCountParam()
    {
      InitializationLogger.Debug("Init: Adding summary mail notifications bunch count.");
      
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.SummaryMailNotificationsBunchCountParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.SummaryMailNotificationsBunchCountParamName,
                                                                  Constants.Module.SummaryMailNotificationsBunchCount.ToString());
    }
    
    #endregion
    
    #region Интеллектуальная обработка
    
    /// <summary>
    /// Создать настройки интеллектуальной обработки документов.
    /// </summary>
    public static void CreateSmartProcessingSettings()
    {
      var smartProcessingSettings = PublicFunctions.SmartProcessingSetting.GetSettings();
      if (smartProcessingSettings == null)
        PublicFunctions.SmartProcessingSetting.Remote.CreateSettings();
    }
    
    #region Заполнение правил обработки
    
    /// <summary>
    /// Заполнить правила в настройках интеллектуальной обработки.
    /// </summary>
    [Public]
    public virtual void FillSmartProcessingRules()
    {
      var smartProcessingSettings = Sungero.Docflow.PublicFunctions.SmartProcessingSetting.GetSettings();
      if (smartProcessingSettings != null && !smartProcessingSettings.ProcessingRules.Any())
      {
        const string ModuleName = "Sungero.SmartProcessing";
        // Класс, грамматика, функция обработки.
        var processRules = new List<string[]>()
        {
          new[] { "Письмо", "Letter", Constants.Module.ProcessingFunctionName.CreateIncomingLetter },
          new[] { "Акт выполненных работ", "ContractStatement", Constants.Module.ProcessingFunctionName.CreateContractStatement },
          new[] { "Товарная накладная", "Waybill", Constants.Module.ProcessingFunctionName.CreateWaybill },
          new[] { "Счет-фактура", "TaxInvoice", Constants.Module.ProcessingFunctionName.CreateTaxInvoice },
          new[] { "Корректировочный счет-фактура", "TaxinvoiceCorrection", Constants.Module.ProcessingFunctionName.CreateTaxInvoiceCorrection },
          new[] { "Универсальный передаточный документ", "GeneralTransferDocument", Constants.Module.ProcessingFunctionName.CreateUniversalTransferDocument },
          new[] { "Универсальный корректировочный документ", "GeneralCorrectionDocument", Constants.Module.ProcessingFunctionName.CreateUniversalTransferCorrectionDocument },
          new[] { "Входящий счет на оплату", "IncomingInvoice", Constants.Module.ProcessingFunctionName.CreateIncomingInvoice },
          new[] { "Договор", "Contract", Constants.Module.ProcessingFunctionName.CreateContract },
          new[] { "Дополнительное соглашение", "SupAgreement", Constants.Module.ProcessingFunctionName.CreateSupAgreement },
          new[] { string.Empty, string.Empty, Constants.Module.ProcessingFunctionName.CreateSimpleDocument }
        };
        
        foreach (var processRule in processRules)
          this.AddProcessRule(smartProcessingSettings, processRule[0], processRule[1], ModuleName, processRule[2]);
        
        smartProcessingSettings.Save();
      }
    }
    
    /// <summary>
    /// Добавить правило в настройках интеллектуальной обработки.
    /// </summary>
    /// <param name="smartProcessingSettings">Настройка интеллектуальной обработки.</param>
    /// <param name="className">Наименование класса из классификатора Ario.</param>
    /// <param name="grammarName">Наименование правила для извлечения фактов Ario.</param>
    /// <param name="moduleName">Наименование модуля, в котором находится функция обработки.</param>
    /// <param name="functionName">Наименование функции обработки.</param>
    [Public]
    public virtual void AddProcessRule(Docflow.ISmartProcessingSetting smartProcessingSettings,
                                       string className, string grammarName, string moduleName, string functionName)
    {
      var ruleSetting = smartProcessingSettings.ProcessingRules.AddNew();
      ruleSetting.ClassName = className;
      ruleSetting.GrammarName = grammarName;
      ruleSetting.ModuleName = moduleName;
      ruleSetting.FunctionName = functionName;
    }
    
    #endregion
    
    #endregion
    
    #region Добавление в таблицу параметров значений для обработки правил назначения прав.
    
    public static void AddAccessRightsBulkProcessingParams()
    {
      AddAccessRightsRuleProcessingBatchSizeParam();
      AddAccessRightsQueueItemsLimitParam();
      AddAccessRightsQueueItemsRetriesLimitParam();
    }
    
    public static void AddAccessRightsRuleProcessingBatchSizeParam()
    {
      InitializationLogger.Debug("Init: Adding documents batch size for access rights rule processing.");
      
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.AccessRightsBulkProcessing.BatchSizeParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.AccessRightsBulkProcessing.BatchSizeParamName,
                                                                  Constants.Module.AccessRightsBulkProcessing.BatchSize.ToString());
    }
    
    public static void AddAccessRightsQueueItemsLimitParam()
    {
      InitializationLogger.Debug("Init: Adding queue items limit for access rights processing.");
      
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.AccessRightsBulkProcessing.JobQueueItemsLimitParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.AccessRightsBulkProcessing.JobQueueItemsLimitParamName,
                                                                  Constants.Module.AccessRightsBulkProcessing.JobQueueItemsLimit.ToString());
    }
    
    public static void AddAccessRightsQueueItemsRetriesLimitParam()
    {
      InitializationLogger.Debug("Init: Adding queue item's retries limit for access rights processing.");
      
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.AccessRightsBulkProcessing.RetriesLimitParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.AccessRightsBulkProcessing.RetriesLimitParamName,
                                                                  Constants.Module.AccessRightsBulkProcessing.RetriesLimit.ToString());
    }
    
    #endregion
    
    #region МКДО
    
    public static void AddProcessHistoricalMessagesRetriesCountParam()
    {
      InitializationLogger.Debug("Init: Adding historical message max retries count for async handler ProcessMessages.");
      
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.ProcessHistoricalMessageRetriesMaxCountParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.ProcessHistoricalMessageRetriesMaxCountParamName,
                                                                  Constants.Module.ProcessHistoricalMessageRetriesMaxCount.ToString());
    }
    
    public static void AddProcessMessagesRetriesCountParam()
    {
      InitializationLogger.Debug("Init: Adding max retries count for async handler ProcessMessages.");
      
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.ProcessMessagesRetriesMaxCountParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.ProcessMessagesRetriesMaxCountParamName,
                                                                  Constants.Module.ProcessMessagesRetriesMaxCount.ToString());
    }
    
    public static void AddHistoricalMessageQueueItemLimitParam()
    {
      InitializationLogger.Debug("Init: Adding historical message queue item limit for job GetHistoricalMessages.");
      
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.HistoricalMessageQueueItemLimitParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.HistoricalMessageQueueItemLimitParamName,
                                                                  Constants.Module.HistoricalMessageQueueItemLimit.ToString());
    }
    
    public static void AddConvertExchangeDocumentToPdfRetriesCountParam()
    {
      InitializationLogger.Debug("Init: Adding max retries count for async handler ConvertExchangeDocumentToPdf.");
      
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.ConvertExchangeDocumentToPdfRetriesMaxCountParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.ConvertExchangeDocumentToPdfRetriesMaxCountParamName,
                                                                  Constants.Module.ConvertExchangeDocumentToPdfRetriesMaxCount.ToString());
    }
    
    #endregion
    
    #region Добавление в таблицу параметров для индексации полнотекстового поиска.

    public static void AddIndexDocumentsForFullTextSearchJobQueueItemsLimitParam()
    {
      InitializationLogger.Debug("Init: Adding queue items limit for document indexing.");
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.DocumentIndexing.QueueItemsInProcessLimitParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.DocumentIndexing.QueueItemsInProcessLimitParamName,
                                                                  Constants.Module.DocumentIndexing.DefaultQueueItemsInProcessLimit.ToString());
    }
    
    public static void AddBulkIndexingDocumentsLimitParam()
    {
      InitializationLogger.Debug("Init: Adding documents limit for bulk indexing.");
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.DocumentIndexing.BulkIndexingDocumentsLimitParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.DocumentIndexing.BulkIndexingDocumentsLimitParamName,
                                                                  Constants.Module.DocumentIndexing.DefaultBulkIndexingDocumentsLimit.ToString());
    }
    
    public static void AddIndexingDocumentsForFullTextSearchRetriesLimitParam()
    {
      InitializationLogger.Debug("Init: Adding rety limit for document indexing.");
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.DocumentIndexing.RetryLimitParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.DocumentIndexing.RetryLimitParamName,
                                                                  Constants.Module.DocumentIndexing.RetryLimit.ToString());
    }
    
    #endregion
    
    #region МЧД
    
    /// <summary>
    /// Добавление в таблицу параметров значения количества эл. доверенностей в пакете.
    /// </summary>
    public static void AddFPoAQueueItemBatchSizeParam()
    {
      InitializationLogger.Debug("Init: Adding batch size queue item limit for async handler SyncFormalizedPoAWithService.");
      
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.FPoAQueueItemBatchSizeParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.FPoAQueueItemBatchSizeParamName,
                                                                  Constants.Module.FPoAQueueItemBatchSize.ToString());
    }
    
    /// <summary>
    /// Добавление в таблицу параметров значения максимального количества переповторов асинхронного обработчика для получения результата операции с доверенностью.
    /// </summary>
    public static void AddFPoAGetOperationRetriesMaxCountParam()
    {
      InitializationLogger.Debug("Init: Adding async handler max retries count for getting formalized poa operation.");
      
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.FPoAGetOperationRetriesMaxCountParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.FPoAGetOperationRetriesMaxCountParamName,
                                                                  Constants.Module.FPoAGetOperationRetriesMaxCount.ToString());
    }
    
    /// <summary>
    /// Добавление в таблицу параметров значения шаблона адреса поиска доверенности на сайте ФНС.
    /// </summary>
    public static void AddSearchFPoAInFtsRegistryTemplateParam()
    {
      InitializationLogger.Debug("Init: Adding Fts registry search template for open in Fts registry action.");
      
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.SearchFPoAInFtsRegistryTemplateParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.SearchFPoAInFtsRegistryTemplateParamName,
                                                                  Constants.Module.SearchFPoAInFtsRegistryTemplate);
    }
    
    /// <summary>
    /// Добавление в таблицу параметров кодов документов, удостоверяющих личность, поддерживаемых в эл. доверенности.
    /// </summary>
    public static void AddFPoAIdentityDocumentCodesParam()
    {
      InitializationLogger.Debug("Init: Adding supported identity document codes for formalized PoA.");
      
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.FPoAIdentityDocumentCodesParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.FPoAIdentityDocumentCodesParamName,
                                                                  Constants.Module.FPoAIdentityDocumentCodes);
    }
    
    #endregion
    
    #region Замещения
    
    public static void AddBulkTransferAccesRightsButchSizeParam()
    {
      InitializationLogger.Debug("Init: Adding bulk transfer access rights session batch size.");
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.BulkTransferAccesRightsSessionBatchSizeParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.BulkTransferAccesRightsSessionBatchSizeParamName,
                                                                  Constants.Module.BulkTransferAccesRightsSessionBatchSize.ToString());
    }
    
    /// <summary>
    /// Добавление в таблицу параметров для вычисления даты закрытия системного замещения.
    /// </summary>
    public static void AddSubstitutionClosingDaysCountParam()
    {
      InitializationLogger.Debug("Init: Adding closing days count for substitution.");
      
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.SubstitutionAccessRightsGrantBufferDaysCountParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.SubstitutionAccessRightsGrantBufferDaysCountParamName,
                                                                  Constants.Module.SubstitutionAccessRightsGrantBufferDaysCount.ToString());
      
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.SubstitutionAccessRightsWaitingDaysCountParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.SubstitutionAccessRightsWaitingDaysCountParamName,
                                                                  Constants.Module.SubstitutionAccessRightsWaitingDaysCount.ToString());
    }
    
    #endregion
  }
}
