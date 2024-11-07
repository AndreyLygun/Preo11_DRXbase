using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using Sungero.Company;
using Sungero.Contracts.ContractualDocument;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Parties;
using DeclensionCase = Sungero.Core.DeclensionCase;

namespace Sungero.Contracts.Server
{
  partial class ContractualDocumentFunctions
  {
    /// <summary>
    /// Получить часть имени договорного документа, игнорируя права доступа.
    /// </summary>
    /// <param name="contractId">ИД договорного документа.</param>
    /// <returns>Часть имени.</returns>
    [Remote(IsPure = true)]
    public static string GetNamePartIgnoreAccessRights(long contractId)
    {
      return Functions.ContractualDocument.GetNamePartByContractualDocument(GetIgnoreAccessRights(contractId));
    }
    
    /// <summary>
    /// Получить договорной документ, игнорируя права доступа.
    /// </summary>
    /// <param name="documentId">ИД документа.</param>
    /// <returns>Договорной документ.</returns>
    public static IContractualDocument GetIgnoreAccessRights(long documentId)
    {
      // HACK Котегов: использование внутренней сессии для обхода прав доступа.
      Logger.DebugFormat("GetIgnoreAccessRights: contractId {0}", documentId);
      using (var session = new Sungero.Domain.Session())
      {
        var innerSession = (Sungero.Domain.ISession)session.GetType()
          .GetField("InnerSession", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(session);
        
        return ContractualDocuments.As((Sungero.Domain.Shared.IEntity)innerSession.Get(typeof(IContractualDocument), documentId));
      }
    }
    
    /// <summary>
    /// Получить номер регистрации, игнорируя права доступа.
    /// </summary>
    /// <param name="documentId">Договорной документ.</param>
    /// <returns>Номер регистрации.</returns>
    [Remote(IsPure = true), Public]
    public static string GetRegistrationNumberIgnoreAccessRights(long documentId)
    {
      return Functions.ContractualDocument.GetIgnoreAccessRights(documentId).RegistrationNumber;
    }

    /// <summary>
    /// Получить для договорного документа сумму прописью с валютой.
    /// </summary>
    /// <param name="contractualDocument">Договорной документ.</param>
    /// <returns>Сумма прописью с валютой.</returns>
    [Converter("TotalAmountInCurrencyToWords")]
    public static string TotalAmountInCurrencyToWords(IContractualDocument contractualDocument)
    {
      
      if (contractualDocument.TotalAmount == null || contractualDocument.Currency == null)
        return null;
      
      return Docflow.PublicFunctions.Module.GetAmountWithCurrencyInWords(contractualDocument.TotalAmount.Value, contractualDocument.Currency);
    }
    
    /// <summary>
    /// Проверить, связан ли документ специализированной связью.
    /// </summary>
    /// <returns>True - если связан, иначе - false.</returns>
    [Remote(IsPure = true)]
    public override bool HasSpecifiedTypeRelations()
    {
      var hasSpecifiedTypeRelations = false;
      AccessRights.AllowRead(
        () =>
        {
          hasSpecifiedTypeRelations = IncomingInvoices.GetAll().Any(x => Equals(x.Contract, _obj)) ||
            SupAgreements.GetAll().Any(x => Equals(x.LeadingDocument, _obj));
        });
      return base.HasSpecifiedTypeRelations() || hasSpecifiedTypeRelations;
    }
  }
}