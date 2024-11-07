using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OutgoingDocumentBase;

namespace Sungero.Docflow.Shared
{
  partial class OutgoingDocumentBaseFunctions
  {
    #region Несколько ответных писем
    
    /// <summary>
    /// Очистить ответные документы и заполнить из одиночного свойства "В ответ на".
    /// </summary>
    public virtual void ClearAndFillFirstResponseDocument()
    {
      _obj.InResponseToDocuments.Clear();
      if (_obj.InResponseTo != null)
      {
        var newItem = _obj.InResponseToDocuments.AddNew();
        newItem.Document = _obj.InResponseTo;
      }
    }
    
    /// <summary>
    /// Заполнить "В ответ на" из коллекции ответных документов.
    /// </summary>
    public virtual void FillInResponseToFromInResponseToDocuments()
    {
      var firstItem = _obj.InResponseToDocuments.FirstOrDefault(d => d.Document != null);
      if (firstItem == null)
        return;
      
      if (_obj.InResponseTo == null)
        _obj.InResponseTo = firstItem.Document;
      else if (!_obj.InResponseToDocuments.Any(d => Equals(_obj.InResponseTo, d.Document)))
        _obj.InResponseTo = _obj.InResponseToDocuments.First().Document;
    }
    
    /// <summary>
    /// Заполнить первый элемент коллекции ответных документов.
    /// </summary>
    public virtual void FillFirstResponseDocument()
    {
      if (_obj.InResponseTo != null && !_obj.InResponseToDocuments.Any())
      {
        var newItem = _obj.InResponseToDocuments.AddNew();
        newItem.Document = _obj.InResponseTo;
      }
    }
    
    #endregion
    
    /// <summary>
    /// Изменить отображение панели регистрации.
    /// </summary>
    /// <param name="needShow">Признак отображения.</param>
    /// <param name="repeatRegister">Признак повторной регистрации\изменения реквизитов.</param>
    public override void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
    {
      base.ChangeRegistrationPaneVisibility(needShow, repeatRegister);
      
      var isManyAddressees = _obj.IsManyAddressees.HasValue && _obj.IsManyAddressees.Value;
      
      _obj.State.Properties.DeliveryMethod.IsEnabled = !isManyAddressees;
      
      _obj.State.Properties.SentDate.IsEnabled = !isManyAddressees;
      _obj.State.Properties.SentDate.IsVisible = needShow;
      
      _obj.State.Properties.TrackNumber.IsEnabled = !isManyAddressees;
      _obj.State.Properties.TrackNumber.IsVisible = needShow;
    }
    
    /// <summary>
    /// Добавить в группу вложений входящее письмо, в ответ на которое было создано исходящее.
    /// </summary>
    /// <param name="group">Группа вложений.</param>
    public override void AddRelatedDocumentsToAttachmentGroup(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup group)
    {
      foreach (var document in _obj.InResponseToDocuments.Select(d => d.Document).Distinct())
        if (document != null && !group.All.Contains(document))
          group.All.Add(document);
    }
    
    /// <summary>
    /// Получить контрагентов по документу.
    /// </summary>
    /// <returns>Контрагенты.</returns>
    public override List<Sungero.Parties.ICounterparty> GetCounterparties()
    {
      if (_obj.Addressees == null)
        return null;
      
      return new List<Sungero.Parties.ICounterparty>(_obj.Addressees.OrderBy(a => a.Number).Select(a => a.Correspondent));
    }
    
    /// <summary>
    /// Получить ответственного за документ.
    /// </summary>
    /// <returns>Ответственный за документ.</returns>
    public override Sungero.Company.IEmployee GetDocumentResponsibleEmployee()
    {
      if (_obj.PreparedBy != null)
        return _obj.PreparedBy;
      
      return base.GetDocumentResponsibleEmployee();
    }
    
    /// <summary>
    /// Очистить лист рассылки и заполнить первого адресата из карточки.
    /// </summary>
    public void ClearAndFillFirstAddressee()
    {
      _obj.Addressees.Clear();
      if (_obj.Correspondent != null)
      {
        var newAddressee = _obj.Addressees.AddNew();
        newAddressee.Correspondent = _obj.Correspondent;
        newAddressee.Addressee = _obj.Addressee;
        newAddressee.DeliveryMethod = _obj.DeliveryMethod;
        newAddressee.SentDate = _obj.SentDate;
        newAddressee.TrackNumber = _obj.TrackNumber;
        newAddressee.Number = 1;
      }
    }
    
    /// <summary>
    /// Удалить из коллекции ответных документов не от текущих корреспондентов.
    /// </summary>
    public virtual void RemoveDocumentsOfDeletedCorrespondents()
    {
      var documentsToRemove = _obj.InResponseToDocuments
        .Where(d => d.Document != null && !_obj.Addressees.Any(a => Equals(d.Document.Correspondent, a.Correspondent)))
        .ToList();
      
      foreach (var document in documentsToRemove)
        _obj.InResponseToDocuments.Remove(document);
    }
    
    /// <summary>
    /// Сменить доступность поля Контрагент. Доступность зависит от статуса.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    /// <param name="counterpartyCodeInNumber">Признак вхождения кода контрагента в формат номера. TRUE - входит.</param>
    /// <param name="enabledState">Признак доступности поля в зависимости от статуса.</param>
    public override void ChangeCounterpartyPropertyAccess(bool isEnabled, bool counterpartyCodeInNumber, bool enabledState)
    {
      var properties = _obj.State.Properties;
      
      if (_obj.IsManyAddressees == false)
      {
        if (_obj.Correspondent != null)
          properties.Correspondent.IsEnabled = isEnabled && !counterpartyCodeInNumber && enabledState;
        else
          properties.Correspondent.IsEnabled = isEnabled && !counterpartyCodeInNumber;
      }
      if (_obj.IsManyAddressees == true)
        properties.Addressees.Properties.Correspondent.IsEnabled = isEnabled && !counterpartyCodeInNumber && enabledState;
      
      _obj.State.Properties.IsManyAddressees.IsEnabled = isEnabled && !counterpartyCodeInNumber && enabledState;
    }
    
    /// <summary>
    /// Получить контактную информацию для отчета Лист рассылки.
    /// </summary>
    /// <param name="addresseesItem">Элемент коллекции адресатов.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Контактная информация.</returns>
    [Public]
    public static string GetContactsInformation(Docflow.IOutgoingDocumentBaseAddressees addresseesItem, IOutgoingDocumentBase document)
    {
      if (addresseesItem.DeliveryMethod != null && addresseesItem.DeliveryMethod.Sid == Constants.MailDeliveryMethod.Exchange)
      {
        var boxes = addresseesItem.Correspondent.ExchangeBoxes
          .Where(b => b.Status == Sungero.Parties.CounterpartyExchangeBoxes.Status.Active)
          .Where(b => Equals(b.Box.BusinessUnit, document.BusinessUnit))
          .Select(b => b.Box.ExchangeService.Name).Distinct();
        return boxes.Any() ? string.Join(", ", boxes) : string.Empty;
      }
      
      var result = new List<string>();
      
      var postalAddress = string.IsNullOrEmpty(addresseesItem.Correspondent.PostalAddress)
        ? addresseesItem.Correspondent.LegalAddress
        : addresseesItem.Correspondent.PostalAddress;
      if (!string.IsNullOrEmpty(postalAddress))
        result.Add(string.Format(Docflow.Reports.Resources.DistributionSheetReport.ContactsInformationPostalAddressTemplate, postalAddress));
      
      var fax = addresseesItem.Addressee != null
        ? addresseesItem.Addressee.Fax
        : string.Empty;
      if (!string.IsNullOrEmpty(fax))
        result.Add(string.Format(Docflow.Reports.Resources.DistributionSheetReport.ContactsInformationFaxTemplate, fax));
      
      var email = addresseesItem.Addressee != null && !string.IsNullOrEmpty(addresseesItem.Addressee.Email)
        ? addresseesItem.Addressee.Email
        : addresseesItem.Correspondent.Email;
      if (!string.IsNullOrEmpty(email))
        result.Add(string.Format(Docflow.Reports.Resources.DistributionSheetReport.ContactsInformationEmailTemplate, email));
      
      return result.Any() ? string.Join(Environment.NewLine, result) : string.Empty;
    }
    
    /// <summary>
    /// Отключение родительской функции, т.к. здесь не нужна доступность рег.номера и даты.
    /// </summary>
    public override void EnableRegistrationNumberAndDate()
    {
      
    }
    
  }
}