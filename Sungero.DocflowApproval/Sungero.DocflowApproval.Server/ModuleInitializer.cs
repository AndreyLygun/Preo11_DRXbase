using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace Sungero.DocflowApproval.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.FirstInitializing, Constants.Module.Init.DocflowApproval.Name, Version.Parse(Constants.Module.Init.DocflowApproval.FirstInitVersion));
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.Initializing49, Constants.Module.Init.DocflowApproval.Name, Version.Parse(Constants.Module.Init.DocflowApproval.Version49));
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.Initializing410, Constants.Module.Init.DocflowApproval.Name, Version.Parse(Constants.Module.Init.DocflowApproval.Version410));
    }
    
    /// <summary>
    /// Начальная инициализация модуля после установки.
    /// </summary>
    public virtual void FirstInitializing()
    {
      var allUsers = Roles.AllUsers;
      if (allUsers != null)
      {
        // Выдача прав всем пользователям.
        GrantRightsOnTasks(allUsers);
      }
    }
    
    public virtual void Initializing49()
    {
      // Код инициализации для версии 4.9.0
    }
    
    public virtual void Initializing410()
    {
      this.CreateCaoComputedRoleExternalLink();
      this.CreateExchangeDeliveryMethodExternalLink();
      this.CreateEmailDeliveryMethodExternalLink();
      this.CreateProcessKindDocumentTypeExternalLink(Constants.Module.Initialize.ProjectDocumentTypeGuid,
                                                     Sungero.Projects.Resources.ProjectTypeName,
                                                     Docflow.DocumentType.DocumentFlow.Inner,
                                                     Constants.Module.Initialize.ProjectDocumentExternalEntityId);
      this.CreateProcessKindDocumentTypeExternalLink(Constants.Module.Initialize.OutgoingLetterTypeGuid,
                                                     Sungero.RecordManagement.Resources.OutgoingLetterTypeName,
                                                     Docflow.DocumentType.DocumentFlow.Outgoing,
                                                     Constants.Module.Initialize.OutgoingLetterExternalEntityId);
      this.CreateProcessKindDocumentTypeExternalLink(Constants.Module.Initialize.UniversalTransferDocumentTypeGuid,
                                                     Sungero.FinancialArchive.Resources.UniversalTransferDocumentTypeName,
                                                     Docflow.DocumentType.DocumentFlow.Contracts,
                                                     Constants.Module.Initialize.UniversalTransferDocumentExternalEntityId);
      this.CreateProcessKindDocumentTypeExternalLink(Constants.Module.Initialize.ContractStatementTypeGuid,
                                                     Sungero.FinancialArchive.Resources.ContractStatementTypeName,
                                                     Docflow.DocumentType.DocumentFlow.Contracts,
                                                     Constants.Module.Initialize.ContractStatementExternalEntityId);
      this.CreateProcessKindDocumentTypeExternalLink(Constants.Module.Initialize.WaybillTypeGuid,
                                                     Sungero.FinancialArchive.Resources.WaybillDocumentTypeName,
                                                     Docflow.DocumentType.DocumentFlow.Contracts,
                                                     Constants.Module.Initialize.WaybillExternalEntityId);
      this.CreateProcessKindDocumentTypeExternalLink(Constants.Module.Initialize.CancellationAgreementTypeGuid,
                                                     Sungero.Exchange.Resources.CancellationAgreementTypeName,
                                                     Docflow.DocumentType.DocumentFlow.Contracts,
                                                     Constants.Module.Initialize.CancellationAgreementExternalEntityId);
      this.CreateProcessKindDocumentTypeExternalLink(Constants.Module.Initialize.MemoTypeGuid,
                                                     Sungero.Docflow.Resources.MemoTypeName,
                                                     Docflow.DocumentType.DocumentFlow.Inner,
                                                     Constants.Module.Initialize.MemoExternalEntityId);
      this.CreateProcessKindDocumentTypeExternalLink(Constants.Module.Initialize.IncomingInvoiceTypeGuid,
                                                     Sungero.Contracts.Resources.IncomingInvoiceTypeName,
                                                     Docflow.DocumentType.DocumentFlow.Incoming,
                                                     Constants.Module.Initialize.IncomingInvoiceExternalEntityId);
      this.CreateProcessKindDocumentTypeExternalLink(Constants.Module.Initialize.ContractTypeGuid,
                                                     Sungero.Contracts.Resources.ContractTypeName,
                                                     Docflow.DocumentType.DocumentFlow.Contracts,
                                                     Constants.Module.Initialize.ContractExternalEntityId);
      this.CreateProcessKindDocumentTypeExternalLink(Constants.Module.Initialize.SupAgreementTypeGuid,
                                                     Sungero.Contracts.Resources.SupAgreementTypeName,
                                                     Docflow.DocumentType.DocumentFlow.Contracts,
                                                     Constants.Module.Initialize.SupAgreementExternalEntityId);
    }
    
    #region Создание ExternalLink для вариантов процессов по умолчанию
    
    public virtual void CreateCaoComputedRoleExternalLink()
    {
      InitializationLogger.Debug("Init: Create CAO computed role external link");
      var computedRole = Sungero.CoreEntities.ComputedRoles.GetAll()
        .Where(x => x.Uuid == Constants.Module.Initialize.CaoComputedRoleUuid)
        .FirstOrDefault();
      if (computedRole != null && !Docflow.PublicFunctions.Module.GetExternalLinks(computedRole).Any())
        Docflow.PublicFunctions.Module.CreateExternalLink(computedRole, Constants.Module.Initialize.CaoComputedRoleExternalEntityId);
    }
    
    /// <summary>
    /// Создать ExternalLink для типа документа.
    /// </summary>
    /// <param name="documentTypeClassGuid">Guid типа документа.</param>
    /// <param name="documentTypeName">Наименование типа документа.</param>
    /// <param name="documentFlow">Документопоток.</param>
    /// <param name="externalEntityId">Уникальный идентификатор ExternalLink типа документа.</param>
    /// <remarks>Если запись справочника Docflow.DocumentType для типа документа отсутствует,
    /// то она будет создана со Status = Closed.</remarks>
    public virtual void CreateProcessKindDocumentTypeExternalLink(Guid documentTypeClassGuid,
                                                                  string documentTypeName,
                                                                  Enumeration documentFlow,
                                                                  Guid externalEntityId)
    {
      InitializationLogger.DebugFormat("Init: Create \"{0}\" document type external link", documentTypeName);
      var documentType = Docflow.PublicInitializationFunctions.Module.GetDocumentTypeByTypeGuid(documentTypeClassGuid);
      if (documentType == null)
      {
        Docflow.PublicInitializationFunctions.Module.CreateDocumentType(documentTypeName,
                                                                        documentTypeClassGuid,
                                                                        documentFlow,
                                                                        Sungero.CoreEntities.DatabookEntry.Status.Closed,
                                                                        true);
      }
      Docflow.PublicInitializationFunctions.Module.CreateDocumentTypeExternalLink(documentTypeClassGuid, externalEntityId);
    }
    
    /// <summary>
    /// Создать ExternalLink для способа доставки "Эл. почта", если он не существует.
    /// </summary>
    public virtual void CreateEmailDeliveryMethodExternalLink()
    {
      var entity = Sungero.Docflow.MailDeliveryMethods.GetAll(d => d.Sid == Constants.Module.Initialize.EmailDeliveryMethodSid.ToString()).FirstOrDefault();
      if (entity == null)
        entity = Sungero.Docflow.MailDeliveryMethods.GetAll(d => d.Name == Sungero.Docflow.MailDeliveryMethods.Resources.EmailMethod).FirstOrDefault();
      if (entity == null)
        entity = Sungero.Docflow.PublicInitializationFunctions.Module.CreateMailDeliveryMethod(Sungero.Docflow.MailDeliveryMethods.Resources.EmailMethod,
                                                                                               Constants.Module.Initialize.EmailDeliveryMethodSid.ToString());
      else if (entity.Sid == null)
      {
        entity.Sid = Constants.Module.Initialize.EmailDeliveryMethodSid.ToString();
        entity.Save();
      }
      this.CreateMailDeliveryMethodExternalLink(entity, Constants.Module.Initialize.EmailDeliveryMethodSid);
    }
    
    /// <summary>
    /// Создать ExternalLink для способа доставки "Сервис эл. обмена", если он не существует.
    /// </summary>
    public virtual void CreateExchangeDeliveryMethodExternalLink()
    {
      var entity = Sungero.Docflow.MailDeliveryMethods.GetAll(d => d.Sid == Constants.Module.Initialize.ExchangeDeliveryMethodSid.ToString()).First();
      this.CreateMailDeliveryMethodExternalLink(entity, Constants.Module.Initialize.ExchangeDeliveryMethodSid);
    }
    
    /// <summary>
    /// Создать ExternalLink для способа доставки, если он не существует.
    /// </summary>
    /// <param name="mailDeliveryMethod">Способ доставки.</param>
    /// <param name="externalEntityId">Уникальный идентификатор ExternalLink способа доставки.</param>
    public virtual void CreateMailDeliveryMethodExternalLink(Sungero.Docflow.IMailDeliveryMethod mailDeliveryMethod, Guid externalEntityId)
    {
      var externalLink = Docflow.PublicFunctions.Module.GetExternalLink(Constants.Module.Initialize.MailDeliveryMethodTypeGuid, externalEntityId);
      if (externalLink == null)
      {
        InitializationLogger.DebugFormat("Init: Create \"{0}\" mail delivery method external link", mailDeliveryMethod.Name);
        Docflow.PublicFunctions.Module.CreateExternalLink(mailDeliveryMethod, externalEntityId);
      }
    }
    
    #endregion
    
    /// <summary>
    /// Выдать права всем пользователям на задачи.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightsOnTasks(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on tasks to all users.");
      DocumentFlowTasks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      DocumentFlowTasks.AccessRights.Save();
    }
  }
}
