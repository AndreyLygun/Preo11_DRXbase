using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.SigningAssignment;
using CommonsPublicFuncs = Sungero.Commons.PublicFunctions;

namespace Sungero.DocflowApproval.Shared
{
  partial class SigningAssignmentFunctions
  {
    #region Получение закешированных параметров видимости и доступности с EntityParams
    
    /// <summary>
    /// Закешировать свойства блока и других признаков в параметры сущности, если их нет в кеше.
    /// </summary>
    public virtual void FillEntityParamsIfEmpty()
    {
      var anyParameterIsMissing =
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.Module.NeedShowNoRightsHintParamName) ||
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.SigningAssignment.AllowChangeReworkPerformerParamName) ||
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.SigningAssignment.NeedStrongSignatureParamName) ||
        !CommonsPublicFuncs.Module.EntityParamsContainsKey(_obj, Constants.SigningAssignment.AllowForwardParamName);
      
      if (anyParameterIsMissing)
        Functions.SigningAssignment.Remote.FillEntityParams(_obj);
    }
    
    /// <summary>
    /// Пользователь без прав на основной документ.
    /// </summary>
    /// <returns>True - на документ не хватает прав, False - права есть, или нет документа.</returns>
    public virtual bool IsUserWithoutRightsOnMainDocument()
    {
      return CommonsPublicFuncs.Module
        .GetBooleanEntityParamsValue(_obj, Constants.Module.NeedShowNoRightsHintParamName);
    }
    
    /// <summary>
    /// Разрешено ли выбирать ответственного за доработку.
    /// </summary>
    /// <returns>True - разрешено, False - нет.</returns>
    public virtual bool CanChangeReworkPerformer()
    {
      return CommonsPublicFuncs.Module
        .GetBooleanEntityParamsValue(_obj, Constants.SigningAssignment.AllowChangeReworkPerformerParamName);
    }

    /// <summary>
    /// Необходимо ли требовать усиленную подпись.
    /// </summary>
    /// <returns>True - необходимо требовать, False - нет.</returns>
    public virtual bool NeedStrongSignature()
    {
      return CommonsPublicFuncs.Module
        .GetBooleanEntityParamsValue(_obj, Constants.SigningAssignment.NeedStrongSignatureParamName);
    }
    
    /// <summary>
    /// Разрешена ли переадресация.
    /// </summary>
    /// <returns>True - разрешена, False - нет.</returns>
    public virtual bool CanForward()
    {
      return CommonsPublicFuncs.Module
        .GetBooleanEntityParamsValue(_obj, Constants.SigningAssignment.AllowForwardParamName);
    }
    
    #endregion
    
    /// <summary>
    /// Проверить, не заблокированы ли документы текущим пользователем.
    /// </summary>
    /// <returns>True - хотя бы один заблокирован, False - все свободны.</returns>
    public virtual bool AreDocumentsLockedByMe()
    {
      var documents = new List<IElectronicDocument>();
      documents.Add(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      documents.AddRange(_obj.AddendaGroup.ElectronicDocuments);
      
      return Functions.Module.IsAnyDocumentLockedByCurrentEmployee(documents);
    }
    
    /// <summary>
    /// Получить сотрудников, которым можно переадресовать задание.
    /// </summary>
    /// <returns>Сотрудники, которым можно переадресовать задание.</returns>
    public virtual IQueryable<IEmployee> GetForwardEmployees()
    {
      var block = Blocks.SigningBlocks.Get(_obj.Task.Scheme, _obj.BlockUid);
      var blockPerformers = Company.PublicFunctions.Module.GetEmployeesFromRecipients(block.Performers.ToList());
      var performers = Docflow.PublicFunctions.Module.Remote.GetActiveAndFutureAssignmentsPerformers(_obj, blockPerformers);
      var employees = Employees.GetAll(emp => emp.Status == Sungero.Company.Employee.Status.Active && !performers.Contains(emp));

      var mainDocument = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      if (OfficialDocuments.Is(mainDocument))
        employees = Docflow.PublicFunctions.OfficialDocument.FilterSignatories(OfficialDocuments.As(mainDocument), employees);
      
      return employees;
    }

  }
}