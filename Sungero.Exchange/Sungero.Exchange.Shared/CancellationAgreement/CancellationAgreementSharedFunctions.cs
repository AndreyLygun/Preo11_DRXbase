using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Exchange.CancellationAgreement;

namespace Sungero.Exchange.Shared
{
  partial class CancellationAgreementFunctions
  {
    /// <summary>
    /// Установить обязательность свойств в зависимости от заполненных данных.
    /// </summary>
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      _obj.State.Properties.Subject.IsRequired = false;
      _obj.State.Properties.Department.IsRequired = false;
    }
    
    /// <summary>
    /// Сменить доступность реквизитов документа.
    /// </summary>
    /// <param name="isEnabled">True, если свойства должны быть доступны.</param>
    /// <param name="repeatRegister">Перерегистрация.</param>
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool repeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, repeatRegister);
      _obj.State.Properties.BusinessUnit.IsEnabled = false;
      _obj.State.Properties.Counterparty.IsEnabled = false;
    }
    
    /// <summary>
    /// Изменить доступность свойства "Вид носителя документа".
    /// </summary>
    public override void ChangeMediumTypePropertyAccess()
    {
      _obj.State.Properties.Medium.IsEnabled = false;
    }
    
    /// <summary>
    /// Изменить отображение панели регистрации.
    /// </summary>
    /// <param name="needShow">Признак отображения.</param>
    /// <param name="repeatRegister">Признак повторной регистрации\изменения реквизитов.</param>
    public override void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
    {
      base.ChangeRegistrationPaneVisibility(needShow, repeatRegister);
      
      var properties = _obj.State.Properties;
      
      var isNumerable = _obj.DocumentKind != null && _obj.DocumentKind.NumberingType == Sungero.Docflow.DocumentKind.NumberingType.Numerable;
      var isRegistrable = _obj.DocumentKind != null && _obj.DocumentKind.NumberingType == Sungero.Docflow.DocumentKind.NumberingType.Registrable;
      
      properties.RegistrationNumber.IsVisible = needShow && (isNumerable || isRegistrable);
      properties.RegistrationDate.IsVisible = needShow && (isNumerable || isRegistrable);
      properties.DocumentRegister.IsVisible = needShow && (isNumerable || isRegistrable);
      properties.DeliveryMethod.IsVisible = needShow && (isNumerable || isRegistrable);
    }
    
    /// <summary>
    /// Поведение панели регистрации по умолчанию.
    /// </summary>
    /// <returns>True, если панель должна быть отображена при создании документа.</returns>
    public override bool DefaultRegistrationPaneVisibility()
    {
      return true;
    }
    
    /// <summary>
    /// Заполнить имя соглашения об аннулировании.
    /// </summary>
    public override void FillName()
    {
      _obj.Name = Sungero.Exchange.CancellationAgreements.Resources.CancellationAgreementNameTemplateFormat(_obj.LeadingDocument);
    }

    /// <summary>
    /// Обновить жизненный цикл документа.
    /// </summary>
    /// <param name="registrationState">Статус регистрации.</param>
    /// <param name="approvalState">Статус согласования.</param>
    /// <param name="counterpartyApprovalState">Статус согласования с контрагентом.</param>
    public override void UpdateLifeCycle(Enumeration? registrationState,
                                         Enumeration? approvalState,
                                         Enumeration? counterpartyApprovalState)
    {
      // Не проверять статусы для пустых параметров.
      if (_obj == null)
        return;
      
      var documentInfo = Functions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(_obj);
      var isOneSidedCancellationAgreement = false;
      if (documentInfo != null)
      {
        isOneSidedCancellationAgreement = documentInfo.NeedSign == false &&
          (approvalState == InternalApprovalState.Signed ||
           counterpartyApprovalState == ExternalApprovalState.Signed);
      }
      
      var lifeCycleMustBeActive = _obj.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Draft &&
        approvalState == Docflow.OfficialDocument.InternalApprovalState.Signed &&
        counterpartyApprovalState == Docflow.OfficialDocument.ExternalApprovalState.Signed;
      
      if (lifeCycleMustBeActive || isOneSidedCancellationAgreement)
        _obj.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Active;
    }
    
    /// <summary>
    /// Добавить связанный с соглашением об аннулировании основной документ в группу вложений.
    /// </summary>
    /// <param name="group">Группа вложений.</param>
    public override void AddRelatedDocumentsToAttachmentGroup(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup group)
    {
      if (group != null && _obj.LeadingDocument != null && !Docflow.ExchangeDocuments.Is(_obj.LeadingDocument) &&
          !group.All.Contains(_obj.LeadingDocument))
        group.All.Add(_obj.LeadingDocument);
    }
    
    /// <summary>
    /// Определить ответственного за документ.
    /// </summary>
    /// <returns>Ответственный за документ.</returns>
    public override Sungero.Company.IEmployee GetDocumentResponsibleEmployee()
    {
      if (_obj.PreparedBy != null)
        return _obj.PreparedBy;
      
      return base.GetDocumentResponsibleEmployee();
    }
  }
}