using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.OfficialDocument;
using Sungero.DocflowApproval.SigningAssignment;

namespace Sungero.DocflowApproval.Server
{
  partial class SigningAssignmentFunctions
  {
    #region Кеширование параметров видимости и доступности в EntityParams
    
    /// <summary>
    /// Закешировать свойства блока и другие признаки в параметры сущности.
    /// </summary>
    [Remote]
    public virtual void FillEntityParams()
    {
      var entityBoolParams = new Dictionary<string, bool>();
      
      // При первом обращении к вложениям они кэшируются с учетом прав на сущности,
      // последующие обращения, в том числе через AllowRead, работают с закешированными сущностями и правами.
      // Если первое обращение было через AllowRead, то последующий код будет работать так, будто есть права, и наоборот,
      // если кэширование было без прав на сущности, то в AllowRead вложений не получить.
      // Корректность доступных действий важнее функциональности ниже, поэтому обеспечиваем работу NeedRightsToMainDocument
      // с серверными вложениями, а не из кэша.
      // BUGS 319348, 320495.
      entityBoolParams.Add(Constants.Module.NeedShowNoRightsHintParamName, this.NeedRightsToMainDocument());
      
      var block = this.GetSigningBlock();
      entityBoolParams.Add(Constants.SigningAssignment.AllowChangeReworkPerformerParamName, this.GetAllowChangeReworkPerformerPropertyValue(block));
      entityBoolParams.Add(Constants.SigningAssignment.NeedStrongSignatureParamName, this.GetNeedStrongSignaturePropertyValue(block));
      entityBoolParams.Add(Constants.SigningAssignment.AllowForwardParamName, this.GetAllowForwardPropertyValue(block));
      
      foreach (var parameter in entityBoolParams)
        Sungero.Commons.PublicFunctions.Module.AddOrUpdateEntityParams(_obj, parameter.Key, parameter.Value);
    }
    
    /// <summary>
    /// Проверить основной документ на нехватку прав.
    /// </summary>
    /// <returns>True - на документ не хватает прав. False - права есть, или их выдавать не нужно.</returns>
    [Remote(IsPure = true)]
    public virtual bool NeedRightsToMainDocument()
    {
      var document = Sungero.Content.ElectronicDocuments.Null;
      AccessRights.AllowRead(
        () =>
        {
          document = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
        });
      
      return document != null && !document.AccessRights.CanRead();
    }
    
    /// <summary>
    /// Получить блок подписания.
    /// </summary>
    /// <returns>Блок подписания.</returns>
    public virtual ISigningBlockSchemeBlock GetSigningBlock()
    {
      return Blocks.SigningBlocks.Get(_obj.Task.Scheme, _obj.BlockUid);
    }
    
    /// <summary>
    /// Разрешить выбор ответственного за доработку.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <returns>True - разрешено, False - нет или block = null.</returns>
    public virtual bool GetAllowChangeReworkPerformerPropertyValue(ISigningBlockSchemeBlock block)
    {
      if (block == null)
        return false;
      
      return block.AllowChangeReworkPerformer.GetValueOrDefault();
    }
    
    /// <summary>
    /// Требовать усиленную подпись.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <returns>True - требовать, False - нет или block = null.</returns>
    public virtual bool GetNeedStrongSignaturePropertyValue(ISigningBlockSchemeBlock block)
    {
      if (block == null)
        return false;
      
      return block.NeedStrongSignature.GetValueOrDefault();
    }
    
    /// <summary>
    /// Разрешить переадресацию.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <returns>True - разрешена переадресация, иначе False.</returns>
    public virtual bool GetAllowForwardPropertyValue(ISigningBlockSchemeBlock block)
    {
      if (block == null)
        return false;
      
      return block.AllowForward.GetValueOrDefault();
    }
    
    #endregion
    
    /// <summary>
    /// Проверить возможность подписать документ.
    /// </summary>
    /// <returns>Список ошибок.</returns>
    [Remote(IsPure = true)]
    public virtual List<string> ValidateBeforeSign()
    {
      var errors = new List<string>();
      
      if (Functions.SigningAssignment.AreDocumentsLockedByMe(_obj))
        errors.Add(Resources.SaveDocumentsBeforeComplete);
      
      var document = _obj.DocumentGroup.ElectronicDocuments.First();
      errors.Add(Functions.Module.CheckCurrentEmployeeRightsToApprove(document));
      errors.Add(Functions.Module.CheckDocumentLocksBeforeSigning(document));

      var addenda = _obj.AddendaGroup.ElectronicDocuments
        .Where(a => a.HasVersions)
        .ToList();
      foreach (var addendum in addenda)
        errors.Add(Functions.Module.CheckDocumentLocksBeforeSigning(addendum));

      return errors.Where(e => !Equals(e, string.Empty)).ToList();
    }
    
    /// <summary>
    /// Получить список сотрудников, у которых можно запросить продление срока.
    /// </summary>
    /// <returns>Список сотрудников.</returns>
    public virtual List<IUser> GetAssigneesForDeadlineExtension()
    {
      return Functions.Module.GetDeadlineAssignees(_obj);
    }
    
    /// <summary>
    /// Проверить возможность отказать в подписании документа.
    /// </summary>
    /// <returns>Список ошибок.</returns>
    [Remote(IsPure = true)]
    public virtual List<string> ValidateBeforeReject()
    {
      var errors = new List<string>();
      
      if (Functions.SigningAssignment.AreDocumentsLockedByMe(_obj))
        errors.Add(Resources.SaveDocumentsBeforeComplete);
      
      var document = _obj.DocumentGroup.ElectronicDocuments.First();
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
        errors.Add(ApprovalTasks.Resources.NeedTextForAbort);
      
      return errors.ToList();
    }
    
    /// <summary>
    /// Связать с основным документом документы из группы Приложения, если они не были связаны ранее.
    /// </summary>
    public virtual void RelateAddedAddendaToPrimaryDocument()
    {
      var primaryDocument = _obj.DocumentGroup.ElectronicDocuments.FirstOrDefault();
      if (primaryDocument == null)
        return;
      
      var nonRelatedAddenda = Functions.Module.GetNonObsoleteDocumentsFromAttachments(primaryDocument, _obj.AddendaGroup.All);
      Functions.Module.RelateDocumentsToPrimaryDocumentAsAddenda(primaryDocument, nonRelatedAddenda);
    }
    
    /// <summary>
    /// Обновить состояние документа и приложений после подписания.
    /// </summary>
    public virtual void UpdateDocumentAndAddendaStateAfterSign()
    {
      Functions.SigningAssignment.UpdateApprovalState(_obj, InternalApprovalState.Signed);
      Functions.SigningAssignment.UpdateAddendaApprovalState(_obj, InternalApprovalState.Signed);

      if (Employees.Is(_obj.CompletedBy))
      {
        var signatory = Employees.As(_obj.CompletedBy);
        Functions.SigningAssignment.SetDocumentSignatory(_obj, signatory);
        Functions.SigningAssignment.SetAddendaSignatory(_obj, signatory);
      }
    }
    
    /// <summary>
    /// Обновить статус согласования основного документа.
    /// </summary>
    /// <param name="state">Новый статус.</param>
    public virtual void UpdateApprovalState(Enumeration? state)
    {
      var document = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (document != null)
        Docflow.PublicFunctions.OfficialDocument.UpdateDocumentApprovalState(document, state, _obj.Task.Id);
    }
    
    /// <summary>
    /// Обновить статус согласования приложений.
    /// </summary>
    /// <param name="state">Новый статус.</param>
    public virtual void UpdateAddendaApprovalState(Enumeration? state)
    {
      var addenda = _obj.AddendaGroup.ElectronicDocuments
        .Where(a => OfficialDocuments.Is(a) && a.HasVersions)
        .Select(d => OfficialDocuments.As(d))
        .ToList();
      foreach (var document in addenda)
      {
        var hasApprovalSign = Docflow.PublicFunctions.Module.DocumentHasApprovalSignature(document, _obj.CompletedBy);
        if (hasApprovalSign)
          Docflow.PublicFunctions.OfficialDocument.UpdateDocumentApprovalState(document, state, _obj.Task.Id);
      }
    }
    
    /// <summary>
    /// Установить подписанта для основного документа.
    /// </summary>
    /// <param name="signatory">Подписывающий.</param>
    public virtual void SetDocumentSignatory(IEmployee signatory)
    {
      var document = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (document == null)
        return;
      Docflow.PublicFunctions.OfficialDocument.Remote.SetDocumentSignatory(document, signatory);
    }
    
    /// <summary>
    /// Установить подписанта для приложений.
    /// </summary>
    /// <param name="signatory">Подписывающий.</param>
    public virtual void SetAddendaSignatory(IEmployee signatory)
    {
      var addenda = _obj.AddendaGroup.ElectronicDocuments
        .Where(a => OfficialDocuments.Is(a) && a.HasVersions)
        .Select(d => OfficialDocuments.As(d))
        .ToList();
      foreach (var document in addenda)
      {
        var hasApprovalSign = Docflow.PublicFunctions.Module.DocumentHasApprovalSignature(document, _obj.CompletedBy);
        if (hasApprovalSign)
          Docflow.PublicFunctions.OfficialDocument.Remote.SetDocumentSignatory(document, signatory);
      }
    }
    
    /// <summary>
    /// Построить сводку по документу.
    /// </summary>
    /// <returns>Сводка по документу.</returns>
    [Remote(IsPure = true)]
    [LocalizeFunction("GetDocumentSummaryFunctionName", "GetDocumentSummaryFunctionDescription")]
    public StateView GetDocumentSummary()
    {
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      var stateView = Docflow.PublicFunctions.Module.GetDocumentSummary(officialDocument);
      if (!stateView.Blocks.Any())
        stateView.AddDefaultLabel(Resources.NoDataToDisplay);
      
      return stateView;
    }
    
    /// <summary>
    /// Получить модель контрола состояния листа согласования.
    /// </summary>
    /// <returns>Модель контрола состояния листа согласования.</returns>
    [Remote(IsPure = true)]
    [LocalizeFunction("GetApprovalListStateFunctionName", "GetApprovalListStateFunctionDescription")]
    public StateView GetApprovalListState()
    {
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      return Docflow.PublicFunctions.Module.Remote.CreateApprovalListStateView(officialDocument);
    }
    
  }
}