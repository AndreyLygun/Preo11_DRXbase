using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;
using Init = Sungero.Exchange.Constants.Module.Initialize;

namespace Sungero.Exchange.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.FirstInitializing, Constants.Module.Init.Exchange.Name, Version.Parse(Constants.Module.Init.Exchange.FirstInitVersion));
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.Initializing49, Constants.Module.Init.Exchange.Name, Version.Parse(Constants.Module.Init.Exchange.Version49));
      Sungero.Commons.PublicInitializationFunctions.Module.ModuleVersionInit(this.Initializing411, Constants.Module.Init.Exchange.Name, Version.Parse(Constants.Module.Init.Exchange.Version411));
    }
    
    /// <summary>
    /// Начальная инициализация модуля после установки.
    /// </summary>
    public virtual void FirstInitializing()
    {
      var allUsers = Roles.AllUsers;
      if (allUsers != null)
      {
        // Документы.
        InitializationLogger.Debug("Init: Grant rights on documents to all users.");
        GrantRightsOnDocuments(allUsers);
      }
      
      CreateDocumentTypes();
      CreateDocumentKinds();
    }
    
    public virtual void Initializing49()
    {
      // Код инициализации для версии 4.9.0
    }
    
    public virtual void Initializing411()
    {
      FillDocumentKindsMediumTypes();
    }
    
    /// <summary>
    /// Выдать права всем пользователям на документы.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightsOnDocuments(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on documents to all users.");
      
      Exchange.CancellationAgreements.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      Exchange.CancellationAgreements.AccessRights.Save();
    }
    
    #region Создание видов и типов документов

    /// <summary>
    /// Создать типы документов для документооборота.
    /// </summary>
    public static void CreateDocumentTypes()
    {
      InitializationLogger.Debug("Init: Create document types");

      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(Sungero.Exchange.Resources.CancellationAgreementTypeName,
                                                                      CancellationAgreement.ClassTypeGuid,
                                                                      Docflow.DocumentType.DocumentFlow.Contracts, true);
    }

    /// <summary>
    /// Создать виды документов для документооборота.
    /// </summary>
    public static void CreateDocumentKinds()
    {
      InitializationLogger.Debug("Init: Create document kinds.");
      
      var approvalActions = new Domain.Shared.IActionInfo[]
      {
        Docflow.OfficialDocuments.Info.Actions.SendForFreeApproval,
        Docflow.OfficialDocuments.Info.Actions.SendForDocumentFlow
      };

      var notNumerable = Docflow.DocumentKind.NumberingType.NotNumerable;
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Sungero.Exchange.Resources.CancellationAgreementKindName,
                                                                      Sungero.Exchange.Resources.CancellationAgreementKindName,
                                                                      notNumerable, Docflow.DocumentRegister.DocumentFlow.Contracts,
                                                                      true, false, CancellationAgreement.ClassTypeGuid, approvalActions,
                                                                      Init.CancellationAgreementKind);
    }
    
    /// <summary>
    /// Заполнить формы документов по умолчанию в видах.
    /// </summary>
    public static void FillDocumentKindsMediumTypes()
    {
      var electronicMediumType = Docflow.PublicFunctions.MediumType.Remote.GetNativeElectronicMediumType();
      Docflow.PublicInitializationFunctions.Module.FillDocumentKindMediumType(Init.CancellationAgreementKind, electronicMediumType);
    }

    #endregion
  }

}
