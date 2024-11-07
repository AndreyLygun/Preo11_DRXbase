using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.IncomingDocumentBase;

namespace Sungero.Docflow.Shared
{
  partial class IncomingDocumentBaseFunctions
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
    
    /// <summary>
    /// Заполнить корреспондента по исходящему документу.
    /// </summary>
    /// <param name="outgoingDocument">Исходящий документ.</param>
    public virtual void FillCorrespondent(IOutgoingDocumentBase outgoingDocument)
    {
      if (_obj.Correspondent != null || outgoingDocument == null)
        return;
      
      var correspondents = outgoingDocument.Addressees.Select(a => a.Correspondent).ToList();
      _obj.Correspondent = outgoingDocument.IsManyAddressees.Value ? null : correspondents.FirstOrDefault();
    }
    
    #endregion
    
    /// <summary>
    /// Добавить в группу вложений исходящее письмо, в ответ на которое было создано входящее.
    /// </summary>
    /// <param name="group">Группа вложений.</param>
    public override void AddRelatedDocumentsToAttachmentGroup(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup group)
    {
      foreach (var document in _obj.InResponseToDocuments.Select(d => d.Document).Distinct())
        if (document != null && !group.All.Contains(document) && document.AccessRights.CanRead())
          group.All.Add(document);
    }
    
    /// <summary>
    /// Получить контрагентов по документу.
    /// </summary>
    /// <returns>Контрагенты.</returns>
    public override List<Sungero.Parties.ICounterparty> GetCounterparties()
    {
      if (_obj.Correspondent == null)
        return null;
      
      return new List<Sungero.Parties.ICounterparty>() { _obj.Correspondent };
    }
    
    /// <summary>
    /// Получить адресатов.
    /// </summary>
    /// <returns>Список адресатов.</returns>
    [Public]
    public override List<Company.IEmployee> GetAddressees()
    {
      return _obj.Addressees.Select(x => x.Addressee).Distinct().ToList();
    }
    
    /// <summary>
    /// Сменить доступность поля Контрагент.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    /// <param name="counterpartyCodeInNumber">Признак вхождения кода контрагента в формат номера. TRUE - входит.</param>
    public override void ChangeCounterpartyPropertyAccess(bool isEnabled, bool counterpartyCodeInNumber)
    {
      _obj.State.Properties.Correspondent.IsEnabled = isEnabled && !counterpartyCodeInNumber;
    }

    /// <summary>
    /// Отключение родительской функции, т.к. здесь не нужна доступность рег.номера и даты.
    /// </summary>
    public override void EnableRegistrationNumberAndDate()
    {
      
    }
    
    /// <summary>
    /// Очистить адресатов и заполнить первого адресата из карточки.
    /// </summary>
    public void ClearAndFillFirstAddressee()
    {
      _obj.Addressees.Clear();
      if (_obj.Addressee != null)
      {
        var newAddressee = _obj.Addressees.AddNew();
        newAddressee.Addressee = _obj.Addressee;
        newAddressee.Number = 1;
      }
    }
    
    /// <summary>
    /// Заполнить адресата из коллекции адресатов.
    /// </summary>
    public virtual void FillAddresseeFromAddressees()
    {
      var addressee = _obj.Addressees.OrderBy(a => a.Number).FirstOrDefault(a => a.Addressee != null);
      
      if (addressee != null)
      {
        if (!Equals(_obj.Addressee, addressee.Addressee))
          _obj.Addressee = addressee.Addressee;
      }
      else
      {
        if (_obj.Addressee != null)
          _obj.Addressee = null;
      }
    }
    
    /// <summary>
    /// Установить метку "Несколько адресатов".
    /// </summary>
    public virtual void SetManyAddresseesPlaceholder()
    {
      // Заполнить метку в локали тенанта.
      using (TenantInfo.Culture.SwitchTo())
        _obj.ManyAddresseesPlaceholder = OfficialDocuments.Resources.ManyAddresseesPlaceholder;
    }
    
    /// <summary>
    /// Получить описание для диалога отмены регистрации.
    /// </summary>
    /// <param name="settingType">Тип настройки.</param>
    /// <returns>Описание.</returns>
    public override string GetCancelRegistrationDialogDescription(Enumeration? settingType)
    {
      if (settingType == Docflow.RegistrationSetting.SettingType.Reservation)
        return Docflow.Resources.CancelReservationDescription;
      
      if (settingType == Docflow.RegistrationSetting.SettingType.Numeration)
        return IncomingDocumentBases.Resources.CancelNumberingDescription;
      
      return IncomingDocumentBases.Resources.CancelRegistrationDescription;
    }
  }
}