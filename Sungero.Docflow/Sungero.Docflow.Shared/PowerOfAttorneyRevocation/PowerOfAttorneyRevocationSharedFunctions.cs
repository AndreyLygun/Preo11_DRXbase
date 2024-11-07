using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorneyRevocation;

namespace Sungero.Docflow.Shared
{
  partial class PowerOfAttorneyRevocationFunctions
  {
    /// <summary>
    /// Сменить доступность реквизитов документа.
    /// </summary>
    /// <param name="isEnabled">True, если свойства должны быть доступны.</param>
    /// <param name="repeatRegister">Перерегистрация.</param>
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool repeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, repeatRegister);
      _obj.State.Properties.BusinessUnit.IsEnabled = false;
      _obj.State.Properties.OurSignatory.IsEnabled = false;
    }
    
    /// <summary>
    /// Изменить доступность свойства "Вид носителя документа".
    /// </summary>
    public override void ChangeMediumTypePropertyAccess()
    {
      _obj.State.Properties.Medium.IsEnabled = false;
    }
    
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
    /// Заполнить имя документа.
    /// </summary>
    public override void FillName()
    {
      _obj.Name = PowerOfAttorneyRevocations.Resources.PowerOfAttorneyRevocationNameTemplateFormat(_obj.FormalizedPowerOfAttorney?.UnifiedRegistrationNumber);
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
      properties.FtsListState.IsVisible = needShow;
      properties.FtsRejectReason.IsVisible = needShow && _obj.FtsListState == FtsListState.Rejected;
      
      properties.ExecutionState.IsVisible = false;
      properties.ControlExecutionState.IsVisible = false;
    }
    
    /// <summary>
    /// Поведение панели по умолчанию.
    /// </summary>
    /// <returns>True, если панель должна быть отображена при создании документа.</returns>
    public override bool DefaultRegistrationPaneVisibility()
    {
      return true;
    }
    
    /// <summary>
    /// Изменение состояния документа для ненумеруемых документов.
    /// </summary>
    public override void SetLifeCycleState()
    {
      // Отзыв эл. доверенности становится действующим только если он зарегистрирован в ФНС,
      // независимо от того, является ли он автонумеруемым.
      return;
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
      // Не обновлять жизненный цикл в зависимости от других статусов.
    }
    
    /// <summary>
    /// Добавить связанную с заявлением на отзыв эл. доверенность в группу вложений.
    /// </summary>
    /// <param name="group">Группа вложений.</param>
    public override void AddRelatedDocumentsToAttachmentGroup(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup group)
    {
      if (group != null && _obj.FormalizedPowerOfAttorney != null && !group.All.Contains(_obj.FormalizedPowerOfAttorney))
        group.All.Add(_obj.FormalizedPowerOfAttorney);
    }
    
    /// <summary>
    /// Проверить, что процесс регистрации заявления на отзыв эл. доверенности завершен.
    /// </summary>
    /// <returns>True, если процесс завершен.</returns>
    [Public]
    public virtual bool CheckRegistrationProcessIsComplete()
    {
      return _obj.FtsListState == Docflow.PowerOfAttorneyRevocation.FtsListState.Registered ||
        _obj.FtsListState == Docflow.PowerOfAttorneyRevocation.FtsListState.Rejected;
    }
  }
}