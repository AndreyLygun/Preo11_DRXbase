using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.FinancialArchiveUI.Client
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Импорт формализованного документа из файла.
    /// </summary>
    [LocalizeFunction("ImportAndShowDocumentFromFileFunctionName", "ImportAndShowDocumentFromFileFunctionDescription")]
    public virtual void ImportAndShowDocumentFromFile()
    {
      FinancialArchive.PublicFunctions.Module.ImportAndShowDocumentFromFileDialog();
    }
    
    /// <summary>
    /// Поиск документов по реквизитам.
    /// </summary>
    [LocalizeFunction("SearchByRequisitesFunctionName", "SearchByRequisitesFunctionDescription")]
    public virtual void SearchByRequisites()
    {
      var query = Docflow.PublicFunctions.Module.FinancialDocumentDialogSearch();
      if (query != null)
        query.Show();
    }
    
    /// <summary>
    /// Поиск документов по реквизитам и их выгрузка.
    /// </summary>
    [LocalizeFunction("SearchByRequisitesAndExportFunctionName", "SearchByRequisitesAndExportFunctionDescription")]
    public virtual void SearchByRequisitesAndExport()
    {
      Docflow.PublicFunctions.Module.ExportFinancialDocumentDialogWithSearch();
    }
    
    /// <summary>
    /// Проверка доступности модуля "Финансовый архив" по лицензии и вхождению в роль "Ответственные за финансовый архив" для текущего пользователя.
    /// </summary>
    /// <returns>True, если модуль доступен согласно лицензии.</returns>
    [Public]
    public virtual bool IsFinancialArchieveAvailableForCurrentUserByLicense()
    {
      var moduleGuid = Docflow.PublicConstants.AccountingDocumentBase.FinancialArchiveUIGuid;
      return Docflow.PublicFunctions.Module.Remote.IsModuleAvailableForCurrentUserByLicense(moduleGuid);
    }
    
    /// <summary>
    /// Создать финансовый документ.
    /// </summary>
    [LocalizeFunction("CreateFinancialDocumentFunctionName", "CreateFinancialDocumentFunctionDescription")]
    public virtual void CreateFinancialDocument()
    {
      if (!FinancialArchive.ContractStatements.AccessRights.CanCreate() &&
          !FinancialArchive.IncomingTaxInvoices.AccessRights.CanCreate() &&
          !FinancialArchive.OutgoingTaxInvoices.AccessRights.CanCreate() &&
          !FinancialArchive.Waybills.AccessRights.CanCreate() &&
          !FinancialArchive.UniversalTransferDocuments.AccessRights.CanCreate())
      {
        Dialogs.NotifyMessage(Sungero.FinancialArchiveUI.Resources.NoRightsToCreateFinancialDocument);
        return;
      }
      
      Docflow.AccountingDocumentBases.CreateDocumentWithCreationDialog(FinancialArchive.ContractStatements.Info,
                                                                       FinancialArchive.IncomingTaxInvoices.Info,
                                                                       FinancialArchive.OutgoingTaxInvoices.Info,
                                                                       FinancialArchive.Waybills.Info,
                                                                       FinancialArchive.UniversalTransferDocuments.Info);
    }
  }
}