using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.BusinessUnitBox;

namespace Sungero.ExchangeCore.Server
{
  partial class BusinessUnitBoxFunctions
  {
    #region Авторизация и приглашения
    
    /// <summary>
    /// Получить ящики, у которых настроено подключение к сервису.
    /// </summary>
    /// <returns>Ящики.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IBusinessUnitBox> GetConnectedBoxes()
    {
      return BusinessUnitBoxes.GetAll(b => Equals(b.ConnectionStatus, ConnectionStatus.Connected));
    }
    
    /// <summary>
    /// Получить все действующие ящики.
    /// </summary>
    /// <returns>Ящики.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IBusinessUnitBox> GetActiveBoxes()
    {
      return BusinessUnitBoxes.GetAll(b => b.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
    }

    /// <summary>
    /// Авторизация в сервисе обмена.
    /// </summary>
    /// <param name="password">Пароль.</param>
    /// <returns>Пустую строку, если авторизация успешна, иначе текст ошибки.</returns>
    [Remote]
    public string Login(string password)
    {
      var result = this.LoginWithoutSave(Encryption.Encrypt(password));
      if (string.IsNullOrWhiteSpace(result))
        _obj.Save();
      
      return result;
    }
    
    private string LoginWithoutSave(string encryptedPassword)
    {
      try
      {
        this.CheckExchangeConnectorLicense();
      }
      catch (AppliedCodeException ex)
      {
        return ex.Message;
      }
      
      if (_obj.ExchangeService == null)
        return BusinessUnitBoxes.Resources.ExchangeServiceNotFound;
      
      NpoComputer.DCX.ClientApi.Client.Initialize((l, msg, args) =>
                                                  {
                                                    if (l == NpoComputer.DCX.Common.LogLevel.Error || l == NpoComputer.DCX.Common.LogLevel.Fatal)
                                                      Logger.ErrorFormat(msg, args);
                                                    else
                                                      Logger.DebugFormat(msg, args);
                                                  }, Logger.ErrorFormat);
      NpoComputer.DCX.Common.ExchangeSystem exchangeServiceType;
      var serviceName = _obj.ExchangeService.ExchangeProvider.Value.Value;
      if (!NpoComputer.DCX.Common.ExchangeSystem.TryParse(serviceName, out exchangeServiceType))
      {
        return BusinessUnitBoxes.Resources.ExchangeServiceNotSupportedFormat(serviceName);
      }
      
      NpoComputer.DCX.ClientApi.Client client = null;

      try
      {
        var serviceSettings = new NpoComputer.DCX.Common.ServiceSettings()
        {
          OurOrganizationInn = _obj.BusinessUnit.TIN,
          OurOrganizationKpp = _obj.BusinessUnit.TRRC,
          ServiceUrl = _obj.ExchangeService.Uri
        };
        
        client = new NpoComputer.DCX.ClientApi.Client(exchangeServiceType, serviceSettings, null);
      }
      catch (Exception ex)
      {
        var innerExceptionText = ex.InnerException != null
          ? string.Format("{0}", ex.InnerException.Message)
          : string.Empty;
        Logger.ErrorFormat("Exchange. Create client error. {0}. {1}", ex.Message, innerExceptionText);
        
        return BusinessUnitBoxes.Resources.ExchangeServiceConnectionError;
      }
      
      try
      {
        this.Login(client, _obj.Login, Encryption.Decrypt(encryptedPassword));
      }
      catch (Exception ex)
      {
        var innerExceptionText = ex.InnerException != null
          ? string.Format("{0}", ex.InnerException.Message)
          : string.Empty;
        Logger.ErrorFormat("Exchange. Connection error. {0}. {1}", ex.Message, innerExceptionText);
        return ex.Message;
      }
      
      if (!string.IsNullOrWhiteSpace(_obj.OrganizationId) && !Equals(_obj.OrganizationId, client.OurSubscriber.Organization.OrganizationId))
        return BusinessUnitBoxes.Resources.LoginChangedToOtherOrganization;
      
      // TODO проверка регистрации сертификата ответственного на сервере обмена.
      if (encryptedPassword != _obj.Password)
        _obj.Password = encryptedPassword;
      if (_obj.Status == CoreEntities.DatabookEntry.Status.Active && _obj.ConnectionStatus != ConnectionStatus.Connected)
        _obj.ConnectionStatus = ConnectionStatus.Connected;
      if (string.IsNullOrWhiteSpace(_obj.OrganizationId))
        _obj.OrganizationId = client.OurSubscriber.Organization.OrganizationId;
      if (string.IsNullOrWhiteSpace(_obj.FtsId))
        _obj.FtsId = client.OurSubscriber.Organization.FnsParticipantId;
      return string.Empty;
    }
    
    /// <summary>
    /// Проверить, приобретена ли лицензия на коннектор к сервису обмена.
    /// </summary>
    private void CheckExchangeConnectorLicense()
    {
      var moduleGuid = Guid.Empty;
      if (_obj.ExchangeService.ExchangeProvider == Sungero.ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
        moduleGuid = Constants.BusinessUnitBox.ExchangeCoreDiadocGiud;
      else if (_obj.ExchangeService.ExchangeProvider == Sungero.ExchangeCore.ExchangeService.ExchangeProvider.Sbis)
        moduleGuid = Constants.BusinessUnitBox.ExchangeCoreSBISGiud;
      
      if (moduleGuid != Guid.Empty && !Sungero.Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(moduleGuid))
        throw AppliedCodeException.Create(BusinessUnitBoxes.Resources.ConnectorNoLicenseFormat(_obj.ExchangeService.Info.Properties.ExchangeProvider.GetLocalizedValue(_obj.ExchangeService.ExchangeProvider)));
    }
    
    /// <summary>
    /// Подключиться к СОД по паролю.
    /// </summary>
    /// <param name="client">Клиент для подключения.</param>
    /// <param name="login">Логин пользователя в СОД.</param>
    /// <param name="password">Пароль пользователя в СОД.</param>
    public virtual void Login(NpoComputer.DCX.ClientApi.Client client, string login, string password)
    {
      client.Login(login, password);
    }
    
    /// <summary>
    /// Проверить возможность подключения.
    /// </summary>
    /// <returns>Текст проблемы, если она обнаружена.</returns>
    /// <remarks>Может менять статус подключения и сохранять сущность.</remarks>
    [Public, Remote]
    public string CheckConnection()
    {
      var needSave = !_obj.State.IsChanged;
      var result = this.LoginWithoutSave(_obj.Password);
      if (string.IsNullOrWhiteSpace(result))
      {
        if (_obj.Status == CoreEntities.DatabookEntry.Status.Active && _obj.ConnectionStatus != ConnectionStatus.Connected)
          _obj.ConnectionStatus = ConnectionStatus.Connected;
      }
      else
      {
        if (_obj.Status == CoreEntities.DatabookEntry.Status.Active && _obj.ConnectionStatus != ConnectionStatus.Error)
        {
          if (string.IsNullOrEmpty(_obj.Password))
            _obj.ConnectionStatus = ConnectionStatus.Waiting;
          else
            _obj.ConnectionStatus = ConnectionStatus.Error;
        }
      }

      if (needSave && _obj.State.IsChanged)
        _obj.Save();
      
      return result;
    }
    
    /// <summary>
    /// Отправить приглашение КА.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="invitationNote">Текст приглашения.</param>
    /// <returns>Текст ошибки или пустая строка.</returns>
    [Remote, Public]
    public string SendInvitation(Parties.ICounterparty counterparty, string invitationNote)
    {
      return this.SendInvitation(counterparty, string.Empty, string.Empty, invitationNote);
    }
    
    /// <summary>
    /// Отправить приглашение.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="operatorCode">Трехбуквенный префикс оператора ЭДО.</param>
    /// <param name="departmentCode">Ид филиала в сервисе обмена.</param>
    /// <param name="invitationNote">Комментарий.</param>
    /// <returns>Текст ошибки или пустая строка.</returns>
    [Remote, Public]
    public virtual string SendInvitation(Parties.ICounterparty counterparty, string operatorCode, string departmentCode, string invitationNote)
    {
      NpoComputer.DCX.ClientApi.Client client = null;
      List<NpoComputer.DCX.Common.Organization> organizations = null;
      var result = string.Empty;
      try
      {
        client = this.GetClient();
        organizations = this.GetOrganizations(counterparty, operatorCode, departmentCode);
        if (!organizations.Any())
          return Sungero.ExchangeCore.BusinessUnitBoxes.Resources.InvalidOrganizationParametersError;
        
        foreach (var organization in organizations.OrderBy(o => o.IsRoaming))
        {
          var hasExchange = counterparty.ExchangeBoxes.Any(b => Equals(b.Box, _obj) &&
                                                           Equals(b.OrganizationId, organization.OrganizationId) &&
                                                           Equals(b.CounterpartyBranchId ?? string.Empty, organization.DepartmentCode ?? string.Empty) &&
                                                           Equals(b.FtsId, organization.FnsParticipantId));
          if (hasExchange)
            return Sungero.ExchangeCore.BusinessUnitBoxes.Resources.CounterpartyExchangeExist;
          
          var company = Parties.CompanyBases.As(counterparty);
          var trrc = company != null ? company.TRRC : string.Empty;
          if (!string.Equals(organization.Inn, counterparty.TIN) || !string.IsNullOrEmpty(trrc) && !string.Equals(organization.Kpp, trrc) ||
              !string.IsNullOrEmpty(departmentCode) && !string.Equals(organization.DepartmentCode, departmentCode) ||
              !string.IsNullOrEmpty(operatorCode) && !string.Equals(organization.OperatorCode.ToLower(), operatorCode.ToLower()))
            return Sungero.ExchangeCore.BusinessUnitBoxes.Resources.InvalidOrganizationParametersError;
          
          if (string.IsNullOrEmpty(organization.DepartmentCode))
            client.SendInvitationRequest(organization, invitationNote);
          else if (_obj.ExchangeService.ExchangeProvider == Sungero.ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
          {
            var status = client.GetContactStatus(organization);
            
            if (this.GetCounterpartyExchangeStatus(status) != Parties.CounterpartyExchangeBoxes.Status.Active)
              return Sungero.ExchangeCore.BusinessUnitBoxes.Resources.NotEstablishedExchangeWithHeadOrganization;
          }
          
          this.CounterpartyStatusRefresh(organization, client, counterparty, invitationNote, string.IsNullOrWhiteSpace(result), null);
        }
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Error on send invitation to counterparty {0}.", ex, counterparty.Id);
        result = ex.Message;
      }
      
      return result;
    }

    /// <summary>
    /// Отправить приглашение КА.
    /// </summary>
    /// <param name="organizationId">ИД организации в сервисе обмена.</param>
    /// <param name="counterpartyName">Наименование контрагента для отображения в ошибках.</param>
    /// <param name="invitationNote">Текст приглашения.</param>
    /// <returns>Текст ошибки или пустая строка.</returns>
    [Remote, Public]
    public string SendInvitation(string organizationId, string counterpartyName, string invitationNote)
    {
      NpoComputer.DCX.ClientApi.Client client = null;
      var result = string.Empty;
      try
      {
        client = this.GetClient();
        client.SendInvitationRequest(new NpoComputer.DCX.Common.Organization { OrganizationId = organizationId }, invitationNote);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Error on send invitation to counterparty {0}.", ex, counterpartyName);
        result = ex.Message;
      }
      
      return result;
    }
    
    /// <summary>
    /// Проверка возможности отправить приглашение контрагенту через ящик.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <returns>ИД организации, если найдена в сервисе.</returns>
    /// <remarks>Только полное совпадение по ИНН и КПП.</remarks>
    [Remote(IsPure = true), Public]
    public List<string> CanSendInvitationFrom(Parties.ICounterparty counterparty)
    {
      return this.GetOrganizations(counterparty, null, null).Select(o => o.OrganizationId).ToList();
    }
    
    /// <summary>
    /// Получить список организаций из сервиса обмена по переданным реквизитам.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="operatorCode">Трехбуквенный префикс оператора ЭДО.</param>
    /// <param name="departmentCode">Идентификатор филиала.</param>
    /// <returns>Список организаций из сервиса обмена.</returns>
    private List<NpoComputer.DCX.Common.Organization> GetOrganizations(Parties.ICounterparty counterparty, string operatorCode, string departmentCode)
    {
      var company = Parties.CompanyBases.As(counterparty);
      var trrc = company != null ? company.TRRC : string.Empty;
      var client = this.GetClient();
      var organizations = client.FindOrganizationsByInnKpp(counterparty.TIN, trrc, operatorCode, departmentCode);
      return organizations;
    }

    /// <summary>
    /// Обновить состояние эл. обмена в карточке контрагента.
    /// </summary>
    /// <param name="organization">Контрагент из сервиса обмена.</param>
    /// <param name="client">Клиент.</param>
    /// <param name="counterparty">Контрагент в RX.</param>
    /// <param name="invitationText">Текст приглашения к обмену.</param>
    /// <param name="existNote">Признак наличия текста приглашения.</param>
    /// <param name="boxLineId">Ид строки электронного обмена контрагента.</param>
    private void CounterpartyStatusRefresh(NpoComputer.DCX.Common.Organization organization,
                                           NpoComputer.DCX.ClientApi.Client client,
                                           Parties.ICounterparty counterparty,
                                           string invitationText, bool existNote,
                                           long? boxLineId)
    {
      if (client != null && client.IsLoggedIn() && organization != null)
      {
        var organizationId = organization.OrganizationId;
        var status = client.GetContactStatus(organization);
        var boxLine = counterparty.ExchangeBoxes.SingleOrDefault(b => Equals(b.Id, boxLineId));
        if (boxLine == null)
          boxLine = counterparty.ExchangeBoxes.SingleOrDefault(b => Equals(b.Box, _obj) &&
                                                               Equals(b.OrganizationId, organizationId) &&
                                                               Equals(b.CounterpartyBranchId ?? string.Empty, organization.DepartmentCode ?? string.Empty) &&
                                                               Equals(b.FtsId, organization.FnsParticipantId)) ??
            counterparty.ExchangeBoxes.AddNew();
        boxLine.Box = _obj;
        boxLine.Status = this.GetCounterpartyExchangeStatus(status);
        boxLine.OrganizationId = organizationId;
        boxLine.IsRoaming = organization.IsRoaming;
        boxLine.FtsId = organization.FnsParticipantId;
        boxLine.CounterpartyBranchId = organization.DepartmentCode;
        
        if (!string.IsNullOrEmpty(organization.DepartmentCode))
        {
          var department = client.GetDepartment(organizationId, organization.DepartmentCode);
          if (department != null)
            boxLine.CounterpartyParentBranchId = department.ParentDepartmentId;
          else
            boxLine.CounterpartyParentBranchId = null;
        }
        else
          boxLine.CounterpartyParentBranchId = null;
        
        if (existNote)
          boxLine.InvitationText = invitationText;

        this.SetIsDefault(counterparty);

        boxLine.CounterpartyBox = organization.IsRoaming ?
          BusinessUnitBoxes.Resources.IsRoamingCounterpartyBoxFormat(organization.ExchangeServiceName) :
          BusinessUnitBoxes.Resources.IsMainCounterpartyBoxFormat(_obj.ExchangeService.Name);

        if (counterparty.ExchangeBoxes.Any(b => Equals(b.Box, _obj) && b.IsDefault == true))
          // Отключаем событие До сохранения, чтобы не сработала валидация дублей.
          using (EntityEvents.Disable(counterparty.Info.Events.BeforeSave))
            counterparty.Save();
      }
    }
    
    /// <summary>
    /// Изменить реквизиты контрагента для электронного обмена.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="counterpartyId">Ид контрагента в сервисе обмена.</param>
    /// <param name="counterpartyFtsId">ФНС Ид контрагента.</param>
    /// <param name="counterpartyBranchId">Код филиала контрагента.</param>
    /// <param name="boxLineId">Ид строки электронного обмена контрагента.</param>
    /// <returns>Текст ошибки при изменении реквизитов. Пусто - если реквизиты изменены.</returns>
    [Remote, Public]
    public virtual string UpdateExchange(Parties.ICounterparty counterparty, string counterpartyId, string counterpartyFtsId,
                                         string counterpartyBranchId, long? boxLineId)
    {
      var boxNotActiveMessage = Functions.BusinessUnitBox.CheckBusinessUnitBoxActive(_obj);
      if (!string.IsNullOrWhiteSpace(boxNotActiveMessage))
        return boxNotActiveMessage;
      
      var hasDouble = Parties.Counterparties.GetAll()
        .SelectMany(s => s.ExchangeBoxes.Where(b => Equals(b.Box, _obj) &&
                                               !Equals(b.Id, boxLineId) &&
                                               Equals(b.OrganizationId, counterpartyId) &&
                                               Equals(b.CounterpartyBranchId ?? string.Empty, counterpartyBranchId ?? string.Empty))).Any();
      if (hasDouble)
        return Sungero.ExchangeCore.BusinessUnitBoxes.Resources.CounterpartyExchangeExist;

      var result = string.Empty;
      try
      {
        var client = this.GetClient();
        var organization = client.GetContact(counterpartyId, counterpartyBranchId, counterpartyFtsId.Trim().Substring(0, 3)).Organization;
        
        // Cравнение реквизитов из сервиса и диалога.
        var company = Parties.CompanyBases.As(counterparty);
        var trrc = company != null ? company.TRRC : string.Empty;
        
        if (organization != null && (!string.Equals(organization.Inn, counterparty.TIN) || !string.IsNullOrEmpty(trrc) && !string.Equals(organization.Kpp, trrc)))
          return Sungero.Parties.Counterparties.Resources.ExchangeDialogCounterpartyChangeIncorrectOrganisationId;
        
        if (organization == null || !string.IsNullOrEmpty(counterpartyBranchId) && !string.Equals(organization.DepartmentCode, counterpartyBranchId) ||
            !string.IsNullOrEmpty(counterpartyFtsId) && !string.Equals(organization.FnsParticipantId, counterpartyFtsId))
          return Sungero.ExchangeCore.BusinessUnitBoxes.Resources.OrganizationNotFoundInExchangeService;

        this.CounterpartyStatusRefresh(organization, client, counterparty, string.Empty, false, boxLineId);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Error in box {0} on update exchange from counterparty {1}.", ex, _obj.Id, counterparty.Id);
        result = ex.Message;
      }

      return result;
    }
    
    /// <summary>
    /// Принять приглашение от контрагента.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="counterpartyId">Ид контрагента в сервисе обмена.</param>
    /// <param name="invitationNote">Комментарий к запросу.</param>
    /// <returns>Строка с ошибкой при принятии приглашения. Пусто - если приглашение принято.</returns>
    [Remote, Public]
    public string AcceptInvitation(Parties.ICounterparty counterparty, string counterpartyId, string invitationNote)
    {
      var boxNotActiveMessage = Functions.BusinessUnitBox.CheckBusinessUnitBoxActive(_obj);
      if (!string.IsNullOrWhiteSpace(boxNotActiveMessage))
        return boxNotActiveMessage;
      
      NpoComputer.DCX.ClientApi.Client client = null;
      NpoComputer.DCX.Common.Organization organization = null;
      var result = string.Empty;
      try
      {
        client = this.GetClient();
        organization = client.GetContact(counterpartyId).Organization;
        client.AcceptInvitation(organization, invitationNote);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Error in box {0} on invitation accept from counterparty {1}.", ex, _obj.Id, counterparty.Id);
        result = Sungero.ExchangeCore.BusinessUnitBoxes.Resources.InvitationAcceptError;
      }
      finally
      {
        this.CounterpartyStatusRefresh(organization, client, counterparty, invitationNote, string.IsNullOrWhiteSpace(result), null);
      }
      
      return result;
    }
    
    /// <summary>
    /// Отклонить приглашение контрагента.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="counterpartyId">Ид контрагента в сервисе обмена.</param>
    /// <param name="invitationNote">Комментарий к запросу.</param>
    /// <returns>Строка с ошибкой при отклонении приглашения. Пусто - если приглашение отклонено.</returns>
    [Remote, Public]
    public string RejectInvitation(Parties.ICounterparty counterparty, string counterpartyId, string invitationNote)
    {
      var boxNotActiveMessage = Functions.BusinessUnitBox.CheckBusinessUnitBoxActive(_obj);
      if (!string.IsNullOrWhiteSpace(boxNotActiveMessage))
        return boxNotActiveMessage;
      
      NpoComputer.DCX.ClientApi.Client client = null;
      NpoComputer.DCX.Common.Organization organization = null;
      var result = string.Empty;
      try
      {
        client = this.GetClient();
        organization = client.GetContact(counterpartyId).Organization;
        client.RejectInvitation(organization, invitationNote);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Error in box {0} on invitation reject from counterparty {1}.", ex, _obj.Id, counterparty.Id);
        result = ex.Message;
      }
      finally
      {
        this.CounterpartyStatusRefresh(organization, client, counterparty, invitationNote, string.IsNullOrWhiteSpace(result), null);
      }
      
      return result;
    }
    
    /// <summary>
    /// Сконвертировать статус КА из DCX в статус КА в RX.
    /// </summary>
    /// <param name="status">Статус в DCX.</param>
    /// <returns>Статус RX.</returns>
    public Enumeration GetCounterpartyExchangeStatus(NpoComputer.DCX.Common.ContactStatus status)
    {
      switch (status)
      {
        case NpoComputer.DCX.Common.ContactStatus.ApprovingByCounteragent:
          return Parties.CounterpartyExchangeBoxes.Status.ApprovingByCA;
        case NpoComputer.DCX.Common.ContactStatus.ApprovingByUs:
          return Parties.CounterpartyExchangeBoxes.Status.ApprovingByUs;
        case NpoComputer.DCX.Common.ContactStatus.Active:
          return Parties.CounterpartyExchangeBoxes.Status.Active;
        case NpoComputer.DCX.Common.ContactStatus.Closed:
          return Parties.CounterpartyExchangeBoxes.Status.Closed;
        default:
          throw new Exception("Invalid value for ContactStatus");
      }
    }
    
    /// <summary>
    /// Получить клиента для вызова из другого модуля.
    /// </summary>
    /// <returns>Подключенный к сервису клиент.</returns>
    [Public]
    public object GetPublicClient()
    {
      return this.GetClient();
    }
    
    /// <summary>
    /// Получить клиента для ящика.
    /// </summary>
    /// <returns>Подключенный к сервису клиент.</returns>
    /// <exception cref="AppliedCodeException">СО не поддерживается или логин\пароль не прошли авторизацию.</exception>
    public NpoComputer.DCX.ClientApi.Client GetClient()
    {
      if (string.IsNullOrEmpty(_obj.Password))
        throw AppliedCodeException.Create(BusinessUnitBoxes.Resources.WrongPassword);
      
      this.CheckExchangeConnectorLicense();
      
      if (_obj.ExchangeService == null)
        throw AppliedCodeException.Create(BusinessUnitBoxes.Resources.ExchangeServiceNotFound);
      
      NpoComputer.DCX.ClientApi.Client.Initialize((l, msg, args) =>
                                                  {
                                                    if (l == NpoComputer.DCX.Common.LogLevel.Error || l == NpoComputer.DCX.Common.LogLevel.Fatal)
                                                      Logger.ErrorFormat(msg, args);
                                                    else
                                                      Logger.DebugFormat(msg, args);
                                                  }, Logger.ErrorFormat);
      NpoComputer.DCX.Common.ExchangeSystem exchangeServiceType;
      var serviceName = _obj.ExchangeService.ExchangeProvider.Value.Value;
      if (!NpoComputer.DCX.Common.ExchangeSystem.TryParse(serviceName, out exchangeServiceType))
      {
        throw AppliedCodeException.Create(BusinessUnitBoxes.Resources.ExchangeServiceNotSupportedFormat(serviceName));
      }
      
      NpoComputer.DCX.ClientApi.Client client = null;

      try
      {
        NpoComputer.DCX.Common.ConnectorSettings connectorSetting = new NpoComputer.DCX.Common.ConnectorSettings(true);
        client = this.GetClient(exchangeServiceType, connectorSetting);
      }
      catch (NpoComputer.DCX.Common.Exceptions.ConnectionException ex)
      {
        throw AppliedCodeException.Create(BusinessUnitBoxes.Resources.FailedConnectionWithServiceFormat(_obj.ExchangeService.Name), ex);
      }
      catch (Exception ex)
      {
        throw AppliedCodeException.Create(ex.Message, ex);
      }
      
      return client;
    }
    
    /// <summary>
    /// Получить клиента.
    /// </summary>
    /// <param name="exchangeServiceType">Сервис обмена.</param>
    /// <param name="connectorSetting">Настройки подключения.</param>
    /// <returns>Подключенный к сервису клиент.</returns>
    public virtual NpoComputer.DCX.ClientApi.Client GetClient(NpoComputer.DCX.Common.ExchangeSystem exchangeServiceType, NpoComputer.DCX.Common.ConnectorSettings connectorSetting)
    {
      return NpoComputer.DCX.ClientApi.Client.Get(exchangeServiceType,
                                                  _obj.BusinessUnit.TIN, _obj.BusinessUnit.TRRC,
                                                  _obj.ExchangeService.Uri,
                                                  new Uri(_obj.ExchangeService.LogonUrl).GetLeftPart(UriPartial.Authority),
                                                  _obj.Login, Encryption.Decrypt(_obj.Password), connectorSetting);
    }

    #endregion
    
    #region Синхронизация ящиков
    
    /// <summary>
    /// Синхронизация ящиков.
    /// </summary>
    public void SyncBoxStatus()
    {
      ((Domain.Shared.IExtendedEntity)_obj).Params[Constants.BoxBase.JobRunned] = true;
      this.CheckConnection();
    }
    
    #endregion
    
    #region Синхронизация КА
    
    /// <summary>
    /// Синхронизация контрагентов с сервисом обмена.
    /// </summary>
    /// <returns>True - если синхронизация завершилась успешно.</returns>
    [Public]
    public virtual bool SyncBoxCounterparties()
    {
      try
      {
        var lastSync = Functions.Module.GetLastSyncDate(_obj);
        var client = Functions.BusinessUnitBox.GetClient(_obj);
        var lastSyncTicks = lastSync.Ticks / TimeSpan.TicksPerMillisecond;
        var allContacts = client.GetContacts();
        var counterparties = allContacts
          .Where(l => (l.StatusChangeDate.Value.Ticks / TimeSpan.TicksPerMillisecond) > lastSyncTicks)
          .ToList();
        
        foreach (var counterparty in counterparties)
        {
          if (CounterpartyQueueItems.GetAll(c => c.ExternalId == counterparty.Organization.OrganizationId && Equals(c.Box, _obj)).Any())
            continue;

          var queueItem = CounterpartyQueueItems.Create();
          queueItem.ExternalId = counterparty.Organization.OrganizationId;
          queueItem.Box = _obj;
          queueItem.RootBox = _obj;
          queueItem.ProcessingStatus = ExchangeCore.CounterpartyQueueItem.ProcessingStatus.NotProcessed;
          queueItem.Save();
          var logMessage = string.Format("Create queue item for counterparty OrganizationId: '{0}', TIN: '{1}', TRRC: '{2}'",
                                         counterparty.Organization.OrganizationId, counterparty.Organization.Inn, counterparty.Organization.Kpp);
          Exchange.PublicFunctions.Module.LogDebugFormat(queueItem, logMessage);
        }
        
        if (counterparties.Any())
          Functions.Module.UpdateLastSyncDate(counterparties.OrderByDescending(c => c.StatusChangeDate).First().StatusChangeDate.Value, _obj);
        
        var queueItems = CounterpartyQueueItems.GetAll(q => Equals(q.Box, _obj)).ToList();
        
        // Дозагрузка контрагентов из очереди.
        var addedItems = queueItems.Where(x => !counterparties.Any(c => c.Organization.OrganizationId == x.ExternalId)).ToList();
        foreach (var queueItem in addedItems)
        {
          Transactions.Execute(
            () =>
            {
              var contact = allContacts.FirstOrDefault(c => c.Organization.OrganizationId == queueItem.ExternalId);
              if (contact == null)
                contact = client.GetContact(queueItem.ExternalId, queueItem.CounterpartyBranchId, string.Empty);
              
              counterparties.Add(contact);
            });
        }
        
        // Обновить дату сихронизации для Сбис, если в очереди есть КА для синхронизации.
        if (_obj.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis && counterparties.Any())
          Functions.Module.UpdateLastSyncDate(Calendar.Now, _obj);
        
        var counterpartiesConflict = new Dictionary<NpoComputer.DCX.Common.IContact, List<Parties.ICounterparty>>();
        var counterpartiesExchangeBoxConflict = new Dictionary<NpoComputer.DCX.Common.Organization, List<Parties.ICounterparty>>();
        
        foreach (var party in counterparties
                 .OrderBy(c => c.Status != NpoComputer.DCX.Common.ContactStatus.Active)
                 .ThenBy(c => c.Organization.IsRoaming))
        {
          // ИД филиала может быть null или string.Empty, считаем что они равны.
          var queueItem = queueItems.FirstOrDefault(q => q.ExternalId == party.Organization.OrganizationId &&
                                                    (q.CounterpartyBranchId ?? string.Empty) == (party.Organization.DepartmentCode ?? string.Empty));
          Exchange.PublicFunctions.Module.LogDebugFormat(queueItem, "Start processing sync counterparty.");
          var logMessage = string.Format(
            "Service contact: OrganizationId: '{0}', TIN: '{1}', TRRC: '{2}', DepartmentCode: '{3}', OrganizationType: '{4}'",
            party.Organization.OrganizationId, party.Organization.Inn, party.Organization.Kpp, party.Organization.DepartmentCode,
            party.Organization.OrganizationType);
          Exchange.PublicFunctions.Module.LogDebugFormat(queueItem, logMessage);
          
          if (queueItem != null)
          {
            Transactions.Execute(
              () =>
              {
                var specificCounterparty = Parties.Counterparties.Null;
                var doubles = new List<Parties.ICounterparty>();
                
                var organizationId = party.Organization.OrganizationId;
                var isSbis = Exchange.PublicFunctions.Module.IsSbisExchangeService(_obj);
                
                /* Поиск организации с эл. обменом по идентификатору организации с сервиса обмена.
                 * ИД организации для СБИС состоит из ИНН/КПП, оно не уникально для некоторых филиалов,
                 * поэтому используется код филиала для более точного поиска и исключения создания потенциальных дублей организаций.
                 */
                if (isSbis)
                {
                  specificCounterparty = this.FindSbisCounterparty(party, queueItem, out doubles);
                  if (doubles.Any())
                    counterpartiesExchangeBoxConflict.Add(party.Organization, doubles);
                }
                else
                  specificCounterparty = Parties.Counterparties
                    .GetAll(c => c.ExchangeBoxes.Any(b => Equals(b.Box, _obj) && Equals(b.OrganizationId, organizationId) &&
                                                     (b.CounterpartyBranchId == null || b.CounterpartyBranchId == string.Empty)))
                    .FirstOrDefault();
                
                if (specificCounterparty != null)
                {
                  Functions.BusinessUnitBox.UpdateExchangeStatus(_obj, specificCounterparty, party, queueItem);
                  queueItem.Counterparty = specificCounterparty;
                }
                else
                {
                  // Поиск организаций по ИНН, КПП, если ничего не найдено с установленным обменом.
                  if (!doubles.Any())
                  {
                    // Передаётся пустой список, т.к. Parties.Counterparties.GetAll() при передаче в метод получает все организации из БД.
                    doubles = Functions.BusinessUnitBox.TryCompareCounterparty(_obj, party, Enumerable.Empty<Parties.ICounterparty>(), queueItem);
                    
                    if (doubles.Any())
                    {
                      counterpartiesConflict.Add(party, doubles);
                      return;
                    }
                  }
                }
                
                queueItem.ProcessingStatus = ExchangeCore.CounterpartyQueueItem.ProcessingStatus.TaskSendWaiting;
                queueItem.Save();
              });
          }
          Exchange.PublicFunctions.Module.LogDebugFormat(queueItem, "End processing sync counterparty.");
        }
        
        if (counterpartiesConflict.Any())
          Functions.BusinessUnitBox.CreateConflictTask(_obj, counterpartiesConflict);
        if (counterpartiesExchangeBoxConflict.Any())
          Functions.BusinessUnitBox.CreateConflictTask(_obj, counterpartiesExchangeBoxConflict, true);
        
        var allQueueItems = CounterpartyQueueItems.GetAll(q => Equals(q.Box, _obj))
          .Where(q => Equals(q.ProcessingStatus, ExchangeCore.CounterpartyQueueItem.ProcessingStatus.TaskSendWaiting)).ToList();
        if (allQueueItems.Any(x => x.SyncResult != null))
        {
          var begin = 0;
          var total = Constants.BusinessUnitBox.CounterpartySyncBatchSize;
          var queueItemsForNotice = allQueueItems.Skip(begin).Take(total).ToList();
          
          while (queueItemsForNotice.Any())
          {
            Transactions.Execute(
              () =>
              {
                Functions.BusinessUnitBox.CreateNotice(_obj, queueItemsForNotice);
              });

            begin += total;
            queueItemsForNotice = allQueueItems.Skip(begin).Take(total).ToList();
          }
        }
        
        Functions.BusinessUnitBox.FillCounterpartiesFtsIds(_obj, client, allContacts);
        
        return true;
      }
      catch (Exception ex)
      {
        Exchange.PublicFunctions.Module.LogErrorFormat(_obj, "Sync counterparties error.", ex);
        return false;
      }
    }
    
    /// <summary>
    /// Дозаполнить ФНС ИД у существующих контрагентов.
    /// </summary>
    /// <param name="client">Клиент.</param>
    /// <param name="contacts">Контакты из сервиса.</param>
    public virtual void FillCounterpartiesFtsIds(NpoComputer.DCX.ClientApi.Client client, List<NpoComputer.DCX.Common.IContact> contacts)
    {
      var counterpartiesWithEmptyFtsId = Parties.Counterparties.GetAll(c => c.ExchangeBoxes.Any(x => Equals(x.Box, _obj) &&
                                                                                                (x.FtsId == null || x.FtsId == string.Empty) &&
                                                                                                (x.CounterpartyBranchId == null || x.CounterpartyBranchId == string.Empty)));
      foreach (var counterparty in counterpartiesWithEmptyFtsId)
      {
        foreach (var exchangeBox in counterparty.ExchangeBoxes.Where(x => Equals(x.Box, _obj) &&
                                                                     (x.FtsId == null || x.FtsId == string.Empty) &&
                                                                     (x.CounterpartyBranchId == null || x.CounterpartyBranchId == string.Empty)))
        {
          var contact = contacts.FirstOrDefault(c => c.Organization.OrganizationId == exchangeBox.OrganizationId);
          if (contact == null)
            contact = client.GetContact(exchangeBox.OrganizationId);
          
          exchangeBox.FtsId = contact.Organization.FnsParticipantId;
        }
        
        counterparty.Save();
      }
    }
    
    /// <summary>
    /// Обновить статус обмена в контрагенте.
    /// </summary>
    /// <param name="counterparty">Контрагент из RX.</param>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    public virtual void UpdateExchangeStatus(Parties.ICounterparty counterparty, NpoComputer.DCX.Common.IContact contact,
                                             ICounterpartyQueueItem queueItem)
    {
      var counterpartyExchangeBox = this.GetCounterpartyExchangeBox(counterparty, contact, queueItem);
      
      var exchangeStatus = Functions.BusinessUnitBox.GetCounterpartyExchangeStatus(_obj, contact.Status);
      var needSendInvitation = false;
      
      var incomingTask = IncomingInvitationTasks.GetAll().Where(x => Equals(x.Box, _obj) &&
                                                                Equals(x.Counterparty, counterparty) &&
                                                                Equals(x.OrganizationId, contact.Organization.OrganizationId) &&
                                                                Equals(x.Status, Workflow.Task.Status.InProcess)).FirstOrDefault();
      var incomingAssignment = IncomingInvitationAssignments.GetAll().Where(x => Equals(x.Task, incomingTask) &&
                                                                            Equals(x.Status, Workflow.AssignmentBase.Status.InProcess)).FirstOrDefault();
      var noticeLine = BusinessUnitBoxes.Resources.NoticeLineFormat(string.Empty, contact.Organization.Inn, contact.Organization.Kpp);
      
      #region Нам пришло приглашение
      
      if (exchangeStatus == Parties.CounterpartyExchangeBoxes.Status.ApprovingByUs)
      {
        // Создаем отложенно, контрагент должен быть валиден и сохранен.
        if (incomingTask == null)
          needSendInvitation = true;
      }
      
      #endregion
      
      #region Мы отправили приглашение
      
      if (exchangeStatus == Parties.CounterpartyExchangeBoxes.Status.ApprovingByCA)
      {
        if (counterpartyExchangeBox.Status == Parties.CounterpartyExchangeBoxes.Status.ApprovingByUs)
        {
          if (incomingTask != null)
            incomingTask.Abort();
        }
      }
      
      #endregion
      
      #region Обмен установлен
      
      if (exchangeStatus == Parties.CounterpartyExchangeBoxes.Status.Active)
      {
        if (incomingAssignment != null)
        {
          incomingAssignment.ActiveText = contact.Comment;
          incomingAssignment.Save();
          incomingAssignment.Complete(ExchangeCore.IncomingInvitationAssignment.Result.Accept);
        }
      }
      
      #endregion
      
      #region Обмен запрещен
      
      if (exchangeStatus == Parties.CounterpartyExchangeBoxes.Status.Closed)
      {
        if (incomingAssignment != null)
        {
          incomingAssignment.ActiveText = contact.Comment;
          incomingAssignment.Save();
          incomingAssignment.Complete(ExchangeCore.IncomingInvitationAssignment.Result.Reject);
        }
      }
      
      #endregion
      
      counterpartyExchangeBox.Status = exchangeStatus;
      counterpartyExchangeBox.FtsId = contact.Organization.FnsParticipantId;
      
      if (!string.IsNullOrEmpty(contact.Comment) && contact.Comment.Length > counterpartyExchangeBox.Info.Properties.InvitationText.Length)
        counterpartyExchangeBox.InvitationText = contact.Comment.Substring(0, counterpartyExchangeBox.Info.Properties.InvitationText.Length);
      else
        counterpartyExchangeBox.InvitationText = contact.Comment;
      
      counterpartyExchangeBox.IsRoaming = contact.Organization.IsRoaming;
      counterpartyExchangeBox.CounterpartyBox = contact.Organization.IsRoaming ?
        BusinessUnitBoxes.Resources.IsRoamingCounterpartyBoxFormat(contact.Organization.ExchangeServiceName) :
        BusinessUnitBoxes.Resources.IsMainCounterpartyBoxFormat(_obj.ExchangeService.Name);
      
      Functions.BusinessUnitBox.SetIsDefault(_obj, counterparty);
      
      counterparty.Save();
      
      if (queueItem != null)
      {
        queueItem.SyncResult = exchangeStatus;
        queueItem.Note = noticeLine;
        queueItem.Save();
      }
      
      if (needSendInvitation)
        Functions.IncomingInvitationTask.Create(counterparty, _obj, contact.Organization.OrganizationId, contact.Comment);
      
      Logger.DebugFormat("Updated exchange status {0} for counterparty Id {1}", exchangeStatus, counterparty.Id);
    }
    
    /// <summary>
    /// Получить запись эл. обмена КА.
    /// </summary>
    /// <param name="counterparty">Контрагент из RX.</param>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <returns>Запись эл. обмена КА.</returns>
    public virtual Parties.ICounterpartyExchangeBoxes GetCounterpartyExchangeBox(Parties.ICounterparty counterparty,
                                                                                 NpoComputer.DCX.Common.IContact contact,
                                                                                 ICounterpartyQueueItem queueItem)
    {
      var counterpartyExchangeBoxes = new List<Sungero.Parties.ICounterpartyExchangeBoxes>();
      if (string.IsNullOrEmpty(queueItem.CounterpartyBranchId))
      {
        counterpartyExchangeBoxes = counterparty.ExchangeBoxes
          .Where(b => Equals(b.Box, _obj)
                 && Equals(b.OrganizationId, contact.Organization.OrganizationId)
                 && string.IsNullOrEmpty(b.CounterpartyBranchId))
          .ToList();
      }
      else
      {
        // ИД филиала может быть null или string.Empty, считаем что они равны.
        counterpartyExchangeBoxes = counterparty.ExchangeBoxes
          .Where(b => Equals(b.Box, _obj)
                 && Equals(b.OrganizationId, contact.Organization.OrganizationId)
                 && (b.CounterpartyBranchId ?? string.Empty) == (queueItem.CounterpartyBranchId ?? string.Empty))
          .ToList();
      }
      
      // Если у КА настроен эл. обмен через сервис обмена и через роуминг, то в выборку может попасть несколько записей.
      // В этом случае необходимо брать запись с ИД участника документооборота организации.
      if (counterpartyExchangeBoxes.Count() > 1)
        return counterpartyExchangeBoxes.Single(b => Equals(b.FtsId, contact.Organization.FnsParticipantId));
      else
        return counterpartyExchangeBoxes.Single();
    }
    
    /// <summary>
    /// Пересчитать признак IsDefault.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    public virtual void SetIsDefault(Parties.ICounterparty counterparty)
    {
      var boxLines = counterparty.ExchangeBoxes.Where(b => Equals(b.Box, _obj));
      var defaultLine = boxLines.SingleOrDefault(b => b.IsDefault == true);
      if (defaultLine == null || defaultLine.Status != Parties.CompanyBaseExchangeBoxes.Status.Active)
      {
        var activeLine = boxLines
          .OrderBy(l => l.Status != Parties.CompanyBaseExchangeBoxes.Status.Active)
          .ThenBy(l => l.IsRoaming == true)
          .First();
        if (activeLine != null)
          activeLine.IsDefault = true;
      }
    }
    
    /// <summary>
    /// Сопоставить контрагента из сервиса обмена с контрагентом из RX.
    /// </summary>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <param name="counterparties">Список всех контрагентов из RX.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <returns>Если сопоставить контрагентов не удалось, вернуть список контрагентов, помешавших найти однозначное соответствие.</returns>
    public virtual List<Parties.ICounterparty> TryCompareCounterparty(NpoComputer.DCX.Common.IContact contact, System.Collections.Generic.IEnumerable<Parties.ICounterparty> counterparties,
                                                                      ICounterpartyQueueItem queueItem)
    {
      var counterpartiesWithSameTin = counterparties.Any() ?
        counterparties.Where(c => Equals(c.TIN, contact.Organization.Inn) &&
                             Equals(c.Status, Sungero.CoreEntities.DatabookEntry.Status.Active)).ToList() :
        Parties.Counterparties.GetAll(c => Equals(c.TIN, contact.Organization.Inn) &&
                                      Equals(c.Status, Sungero.CoreEntities.DatabookEntry.Status.Active)).ToList();
      var allCompaniesWithSameTin = counterpartiesWithSameTin.Select(c => Parties.CompanyBases.As(c)).Where(c => c != null);
      var companiesWithSameTinTrrc = allCompaniesWithSameTin.Where(c => Equals(c.TRRC, contact.Organization.Kpp)).ToList();
      var companiesWithSameTinWithoutTrrc = allCompaniesWithSameTin.Where(c => string.IsNullOrWhiteSpace(c.TRRC)).ToList();
      Parties.ICounterparty comparedCounterparty = null;
      var doubles = new List<Parties.ICounterparty>();

      if (counterpartiesWithSameTin.Count == 0)
      {
        Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Counterparty for contact {0} with TIN not exists.", contact.Organization.OrganizationId));
        comparedCounterparty = Functions.BusinessUnitBox.CreateCounterparty(_obj, contact);
      }
      else
      {
        if (string.IsNullOrWhiteSpace(contact.Organization.Kpp))
        {
          // Нашли явно организацию без кпп или банк.
          if (companiesWithSameTinWithoutTrrc.Count == 1)
          {
            comparedCounterparty = companiesWithSameTinWithoutTrrc.Single();
            Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Find one counterparty with same TIN, id {0}.", comparedCounterparty.Id));
          }
          
          // Организаций с таким ИНН нет, нашли персону.
          if (!allCompaniesWithSameTin.Any() && counterpartiesWithSameTin.Count == 1)
          {
            comparedCounterparty = counterpartiesWithSameTin.Single();
            Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Find one person with same TIN, id {0}.", comparedCounterparty.Id));
          }
          // Если не удалось найти подходящий справочник.
          if (comparedCounterparty == null)
          {
            if (companiesWithSameTinWithoutTrrc.Any())
            {
              doubles.AddRange(companiesWithSameTinWithoutTrrc);
              Exchange.PublicFunctions.Module.LogDebugFormat("Find many counterparties with same TIN, without TRRC. Organization without TRRC.");
            }
            else
            {
              doubles.AddRange(counterpartiesWithSameTin.Where(p => !Parties.CompanyBases.Is(p)));
              Exchange.PublicFunctions.Module.LogDebugFormat("Find many counterparties with same TIN. Organization without TRRC.");
            }
          }
        }
        else
        {
          // Нашли явно организацию с этим кпп.
          if (companiesWithSameTinTrrc.Count == 1)
          {
            comparedCounterparty = companiesWithSameTinTrrc.Single();
            Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Find one counterparty with same TIN, TRRC, id {0}.", comparedCounterparty.Id));
          }
          
          // Если не удалось найти подходящий справочник.
          if (comparedCounterparty == null)
          {
            if (companiesWithSameTinTrrc.Any())
            {
              // Найдено несколько организаций с такими же ИНН и КПП.
              doubles.AddRange(companiesWithSameTinTrrc);
              Exchange.PublicFunctions.Module.LogDebugFormat("Find many counterparties with same TIN, TRRC. Organization with TRRC.");
            }
            else
            {
              // Найдено несколько организаций с таким же ИНН.
              // Дублями считаются только организации с таким же ИНН и без установленного обмена.
              var companiesWithoutExchange = allCompaniesWithSameTin.Where(c => !c.ExchangeBoxes.Any());
              if (companiesWithoutExchange.Any())
              {
                doubles.AddRange(companiesWithoutExchange);
                Exchange.PublicFunctions.Module.LogDebugFormat("Find counterparties with same TIN with other TRRC.");
              }
              else
              {
                Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Counterparty for contact {0} with TIN and exhange box not exists.",
                                                                             contact.Organization.OrganizationId));
                comparedCounterparty = Functions.BusinessUnitBox.CreateCounterparty(_obj, contact);
              }
            }
          }
        }
      }

      if (comparedCounterparty != null)
      {
        this.CreateCounterpartyExchangeBox(contact, comparedCounterparty, queueItem);
      }
      return doubles;
    }
    
    /// <summary>
    /// Создать электронный обмен для контрагента.
    /// </summary>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <param name="сounterparty">Контрагент.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    public virtual void CreateCounterpartyExchangeBox(NpoComputer.DCX.Common.IContact contact, Parties.ICounterparty сounterparty,
                                                      ICounterpartyQueueItem queueItem)
    {
      var newCounterpartyExchangeBox = сounterparty.ExchangeBoxes.AddNew();
      newCounterpartyExchangeBox.Box = _obj;
      newCounterpartyExchangeBox.OrganizationId = contact.Organization.OrganizationId;
      newCounterpartyExchangeBox.CounterpartyBranchId = queueItem.CounterpartyBranchId;
      newCounterpartyExchangeBox.IsRoaming = contact.Organization.IsRoaming;
      newCounterpartyExchangeBox.CounterpartyBox = contact.Organization.IsRoaming ?
        BusinessUnitBoxes.Resources.IsRoamingCounterpartyBoxFormat(contact.Organization.ExchangeServiceName) :
        BusinessUnitBoxes.Resources.IsMainCounterpartyBoxFormat(_obj.ExchangeService.Name);
      Functions.BusinessUnitBox.UpdateExchangeStatus(_obj, сounterparty, contact, queueItem);
      if (queueItem != null)
        queueItem.Counterparty = сounterparty;
      Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Сounterparty {0}, status updated.", сounterparty.Id));
    }
    
    /// <summary>
    /// Найти организацию RX с установленным обменом, соответствующую организации СБИС.
    /// </summary>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <param name="doubles">Список потенциальных дублей.</param>
    /// <returns>Найденная организация в RX. Если найдено несколько - в doubles будет список дублей.</returns>
    public virtual Parties.ICounterparty FindSbisCounterparty(NpoComputer.DCX.Common.IContact contact, ICounterpartyQueueItem queueItem,
                                                              out List<Parties.ICounterparty> doubles)
    {
      doubles = new List<Parties.ICounterparty>();
      var specificCounterparty = Parties.Counterparties.Null;
      var counterpartyOrganizationId = contact.Organization.OrganizationId;
      
      var organizationCounterparties = Parties.Counterparties
        .GetAll(c => c.ExchangeBoxes.Any(b => Equals(b.Box, _obj) && Equals(b.OrganizationId, counterpartyOrganizationId))).ToList();
      // ИД филиала может быть null или string.Empty, считаем что они равны.
      var organizationsWithSameBranchId = organizationCounterparties
        .Where(c => c.ExchangeBoxes.Any(b => Equals(b.Box, _obj) &&
                                        (b.CounterpartyBranchId ?? string.Empty) == (queueItem.CounterpartyBranchId ?? string.Empty))).ToList();
      
      // Если нашли в системе дубли эл. обмена - вернуть информацию по дублям, прекратить обработку текущей организации.
      if (organizationsWithSameBranchId.Count > 1)
      {
        Exchange.PublicFunctions.Module.LogDebugFormat("Found many counterparties with same organization id and branch id.");
        doubles = organizationsWithSameBranchId;
        return specificCounterparty;
      }
      else
        specificCounterparty = organizationsWithSameBranchId.FirstOrDefault();
      
      // Не найдено полное совпадение по ИД организации и ИД филиала.
      if (specificCounterparty == null)
      {
        if (organizationCounterparties.Count > 1)
        {
          Exchange.PublicFunctions.Module.LogDebugFormat("Found many counterparties with same organization id.");
          doubles = organizationCounterparties;
          return specificCounterparty;
        }
        else
          specificCounterparty = organizationCounterparties.FirstOrDefault();
        
        // Не найдено совпадение по ИД организации.
        if (specificCounterparty == null)
        {
          // ИД филиала может быть null или string.Empty, считаем что они равны.
          var organizationsWithSameTINAndBranchId = Parties.Counterparties
            .GetAll(c => c.TIN == contact.Organization.Inn &&
                    c.ExchangeBoxes.Any(b => Equals(b.Box, _obj) &&
                                        (b.CounterpartyBranchId ?? string.Empty) == (queueItem.CounterpartyBranchId ?? string.Empty)));
          
          if (organizationsWithSameTINAndBranchId.Any())
          {
            Exchange.PublicFunctions.Module.LogDebugFormat("Found counterparties with same TIN and branch id.");
            doubles = organizationsWithSameTINAndBranchId.ToList();
            return specificCounterparty;
          }
        }
        else
          // Найдена организация, но обмен установлен с другим филиалом или головой.
          this.CreateCounterpartyExchangeBox(contact, specificCounterparty, queueItem);
      }
      
      return specificCounterparty;
    }
    
    /// <summary>
    /// Создать контрагента.
    /// </summary>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <returns>Контрагент.</returns>
    public virtual Parties.ICounterparty CreateCounterparty(NpoComputer.DCX.Common.IContact contact)
    {
      var isPerson = NpoComputer.DCX.Common.OrganizationType.Individual == contact.Organization.OrganizationType;
      
      Parties.ICounterparty counterparty = null;
      var organization = contact.Organization;
      if (isPerson)
      {
        var person = Parties.People.Create();
        person.FirstName = string.IsNullOrEmpty(organization.FirstName) ? organization.Name : organization.FirstName;
        person.LastName = string.IsNullOrEmpty(organization.LastName) ? organization.Name : organization.LastName;
        person.MiddleName = string.IsNullOrEmpty(organization.MiddleName) ? string.Empty : organization.MiddleName;
        
        if (organization.RegistrationAddress != null)
          person.LegalAddress = Functions.BusinessUnitBox.ConvertAddressToPostalFormat(organization.RegistrationAddress);
        
        counterparty = person;
      }
      else
      {
        var company = Parties.Companies.Create();
        company.TRRC = organization.Kpp;
        var companyName = organization.Name;
        
        var namePropertyLength = company.Info.Properties.Name.Length;
        if (organization.Name.Length > namePropertyLength)
        {
          companyName = Sungero.Docflow.PublicFunctions.Module.CutText(organization.Name, namePropertyLength);
          company.Note += string.IsNullOrEmpty(company.Note) ? organization.Name : string.Format("{0}{1}", Environment.NewLine, organization.Name);
        }
        company.Name = companyName;
        
        company.LegalName = Sungero.Docflow.PublicFunctions.Module.CutText(organization.LegalName, company.Info.Properties.LegalName.Length);
        counterparty = company;
      }
      
      if (string.IsNullOrWhiteSpace(Parties.PublicFunctions.Counterparty.CheckTin(organization.Inn, !isPerson)))
        counterparty.TIN = organization.Inn;
      else
        counterparty.Note = BusinessUnitBoxes.Resources.TINNoteFormat(organization.Inn);
      
      if (!string.IsNullOrEmpty(organization.Bik))
      {
        var bank = Parties.Banks.GetAll(x => x.BIC == organization.Bik).FirstOrDefault();
        if (bank != null)
        {
          counterparty.Bank = bank;
          counterparty.Account = organization.CurrentAccount;
        }
      }
      
      if (organization.LegalAddress != null)
      {
        counterparty.LegalAddress = Functions.BusinessUnitBox.ConvertAddressToPostalFormat(organization.LegalAddress);
        Functions.BusinessUnitBox.SetCityAndRegion(_obj, organization.LegalAddress, counterparty);
      }
      
      if (organization.MailAddress != null)
      {
        counterparty.PostalAddress = Functions.BusinessUnitBox.ConvertAddressToPostalFormat(organization.MailAddress);
        if (counterparty.Region == null && counterparty.City == null)
          Functions.BusinessUnitBox.SetCityAndRegion(_obj, organization.MailAddress, counterparty);
      }
      
      using (TenantInfo.Culture.SwitchTo())
      {
        var phoneNumber = string.Empty;

        if (!string.IsNullOrEmpty(organization.Fax))
        {
          if (!string.IsNullOrEmpty(organization.PhoneNumber))
            phoneNumber = BusinessUnitBoxes.Resources.PhoneNumberFormat(organization.PhoneNumber);
          
          phoneNumber += BusinessUnitBoxes.Resources.FaxNumberFormat(organization.Fax);
        }
        else
          phoneNumber = organization.PhoneNumber;
        
        counterparty.Phones = phoneNumber;
      }

      counterparty.PSRN = organization.Ogrn;
      counterparty.Save();
      Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Created counterparty for contact {0}, id {1}.", contact.Organization.OrganizationId, counterparty.Id));
      return counterparty;
    }
    
    /// <summary>
    /// Очистить название населенного пункта от сокращений.
    /// </summary>
    /// <param name="localityName">Название населенного пункта.</param>
    /// <returns>Название, очищенное от сокращений.</returns>
    // TODO Dmitriev_IA: Подобную очистку нужно будет продумать еще и для Дома/Корпуса/Квартиры.
    private static string TrimAbbreviationsLocalityName(string localityName)
    {
      if (string.IsNullOrWhiteSpace(localityName))
        return string.Empty;
      
      var terms = localityName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
      var firstTerm = terms.FirstOrDefault();
      
      if (string.IsNullOrWhiteSpace(firstTerm))
        return string.Empty;
      
      var firstTermLowerCase = firstTerm.ToLower();
      
      // TODO Dmitriev_IA: Самая простая реализация, закрывающая очевидные кейсы. В дальнейшем наверняка придется переделать или дополнить.
      if (firstTermLowerCase == "г" ||
          firstTermLowerCase == "г." ||
          firstTermLowerCase == "гор" ||
          firstTermLowerCase == "гор." ||
          firstTermLowerCase == "город" ||
          firstTermLowerCase == "пгт" ||
          firstTermLowerCase == "пгт.")
        terms.Remove(firstTerm);
      
      return string.Join(" ", terms);
    }
    
    /// <summary>
    /// Преобразовать составляющие адреса DCX в адрес формата Почты России.
    /// </summary>
    /// <param name="addressTerms">Составляющие адреса DCX.</param>
    /// <returns>Адрес в формате Почты России.</returns>
    /// <remarks>Функция введена для тестирования.
    /// Необходимо, чтобы составляющие шли строго в следующем порядке:
    /// Почтовый индекс, Код региона, Город, Населенный пункт, Улица, Дом, Корпус, Квартира.</remarks>
    [Public, Remote(IsPure = true)]
    public static string ConvertAddressToPostalFormat(List<string> addressTerms)
    {
      if (addressTerms.Count < 8)
        return string.Empty;
      
      var address = new NpoComputer.DCX.Common.OrganizationAddress();
      address.PostalCode = addressTerms[0];
      address.RegionCode = addressTerms[1];
      address.City = addressTerms[2];
      address.Locality = addressTerms[3];
      address.Street = addressTerms[4];
      address.House = addressTerms[5];
      address.Building = addressTerms[6];
      address.Apartment = addressTerms[7];
      
      return Functions.BusinessUnitBox.ConvertAddressToPostalFormat(address);
    }
    
    /// <summary>
    /// Преобразовать адрес DCX в адрес формата Почты России.
    /// </summary>
    /// <param name="address">Адрес DCX.</param>
    /// <returns>Адрес в формате Почты России.</returns>
    public static string ConvertAddressToPostalFormat(NpoComputer.DCX.Common.OrganizationAddress address)
    {
      var addressTerms = new List<string>();
      
      var federalCities = new string[]
      {
        Constants.BusinessUnitBox.Moscow,
        Constants.BusinessUnitBox.SaintPetersburg,
        Constants.BusinessUnitBox.Sevastopol,
        Constants.BusinessUnitBox.Baikonur
      };
      
      var cityName = TrimAbbreviationsLocalityName(address.City);
      var localityName = TrimAbbreviationsLocalityName(address.Locality);
      
      // Сформировать наименование города или населенного пункта сервиса обмена.
      var city = string.Format("{0} {1}", Constants.BusinessUnitBox.City, cityName);
      var locality = string.Format("{0} {1}", Constants.BusinessUnitBox.Locality, localityName);
      
      // Обработать ситуацию, в которой город или населенный пункт перепутаны местами.
      var cityRev = string.Format("{0} {1}", Constants.BusinessUnitBox.City, localityName);
      var localityRev = string.Format("{0} {1}", Constants.BusinessUnitBox.Locality, cityName);
      
      // Найти соответствующий населенный пункт в RX.
      var cityRx = Commons.Cities.GetAll().FirstOrDefault(c => c.Name.Equals(city) || c.Name.Equals(cityRev));
      var localityRx = Commons.Cities.GetAll().FirstOrDefault(c => c.Name.Equals(locality) || c.Name.Equals(localityRev));
      
      // Подготовить составляющие части адреса в порядке почты России.
      // Улица.
      addressTerms.Add(!string.IsNullOrEmpty(address.Street) ?
                       address.Street :
                       string.Empty);
      // Дом.
      addressTerms.Add(!string.IsNullOrEmpty(address.House) ?
                       string.Format("{0} {1}", Constants.BusinessUnitBox.House, address.House) :
                       string.Empty);
      // Строение.
      addressTerms.Add(!string.IsNullOrEmpty(address.Building) ?
                       string.Format("{0} {1}", Constants.BusinessUnitBox.Building, address.Building) :
                       string.Empty);
      // Квартира.
      addressTerms.Add(!string.IsNullOrEmpty(address.Apartment) ?
                       string.Format("{0} {1}", Constants.BusinessUnitBox.Apartment, address.Apartment) :
                       string.Empty);
      
      // Населенный пункт, если смогли найти его в RX.
      if (localityRx != null)
        addressTerms.Add(localityRx.Name);
      else
        // Указать в качестве населенного пункта информацию, которая содержится в адресе из сервиса обмена,
        // если она не пуста и города с таким именем в RX не найдено.
        if (cityRx == null && !string.IsNullOrWhiteSpace(address.Locality))
          addressTerms.Add(address.Locality);
      
      // Город, если смогли найти его в RX.
      if (cityRx != null)
        addressTerms.Add(cityRx.Name);
      else
        // Указать в качестве города информацию, которая содержится в адресе из сервиса обмена,
        // если она не пуста и населенного пункта с таким именем в RX не найдено.
        if (localityRx == null && !string.IsNullOrWhiteSpace(address.City))
          addressTerms.Add(address.City);
      
      // Если город не является городом федерального значения, то заполняем регион. Иначе - регион пустой.
      if (!federalCities.Contains(city) && !federalCities.Contains(cityRev))
      {
        var region = Commons.Regions.GetAll().FirstOrDefault(r => r.Code.Equals(address.RegionCode));
        
        addressTerms.Add(region == null ? string.Empty : region.Name);
      }
      else
        addressTerms.Add(string.Empty);
      
      addressTerms.Add(!string.IsNullOrEmpty(address.PostalCode) ? address.PostalCode : string.Empty);
      
      // Убрать лишние запятые в кусках адреса пришедшего из СО, если таковые имеются.
      for (var i = 0; i < addressTerms.Count; i++)
        addressTerms[i] = addressTerms[i] == null ? addressTerms[i] : addressTerms[i].TrimEnd(new char[] { ',' });
      
      // Объединить куски адреса в одну строку, в порядке почты России.
      return string.Join(", ", addressTerms.Where(s => !string.IsNullOrWhiteSpace(s)).ToList()).TrimEnd();
    }
    
    /// <summary>
    /// Заполнить населенный пункт и регион контрагента.
    /// </summary>
    /// <param name="address">Адрес из сервиса обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    public void SetCityAndRegion(NpoComputer.DCX.Common.OrganizationAddress address,
                                 Sungero.Parties.ICounterparty counterparty)
    {
      var federalCities = new string[]
      {
        Constants.BusinessUnitBox.Moscow,
        Constants.BusinessUnitBox.SaintPetersburg,
        Constants.BusinessUnitBox.Sevastopol,
        Constants.BusinessUnitBox.Baikonur
      };
      
      var cityName = TrimAbbreviationsLocalityName(address.City);
      var localityName = TrimAbbreviationsLocalityName(address.Locality);
      
      // Сформировать наименование города или населенного пункта сервиса обмена.
      var city = string.Format("{0} {1}", Constants.BusinessUnitBox.City, cityName);
      var locality = string.Format("{0} {1}", Constants.BusinessUnitBox.Locality, localityName);
      
      // Обработать ситуацию, в которой город или населенный пункт перепутаны местами.
      var cityRev = string.Format("{0} {1}", Constants.BusinessUnitBox.City, localityName);
      var localityRev = string.Format("{0} {1}", Constants.BusinessUnitBox.Locality, cityName);
      
      // Если полученный город не совпадает с одним из городов федерального значения - найти регион по коду.
      Commons.IRegion regionRx = null;
      if (!federalCities.Contains(city) && !federalCities.Contains(cityRev))
        regionRx = Commons.Regions.GetAll().FirstOrDefault(r => r.Code.Equals(address.RegionCode));
      
      // Найти населенный пункт в RX.
      Commons.ICity cityRx = null;
      Commons.ICity localityRx = null;
      
      // Если регион известен - ищем населенный пункт в этом регионе.
      // Иначе ищем населенный пункт и пытаемся определить по нему регион.
      if (regionRx != null)
      {
        cityRx = Commons.Cities.GetAll().FirstOrDefault(c => (c.Name.Equals(city) || c.Name.Equals(cityRev)) && c.Region.Equals(regionRx));
        localityRx = Commons.Cities.GetAll().FirstOrDefault(c => (c.Name.Equals(locality) || c.Name.Equals(localityRev)) && c.Region.Equals(regionRx));
        
        counterparty.Region = regionRx;
        
        if (cityRx != null)
          counterparty.City = cityRx;
        if (localityRx != null)
          counterparty.City = localityRx;
      }
      else
      {
        cityRx = Commons.Cities.GetAll().FirstOrDefault(c => c.Name.Equals(city) || c.Name.Equals(cityRev));
        localityRx = Commons.Cities.GetAll().FirstOrDefault(c => c.Name.Equals(locality) || c.Name.Equals(localityRev));
        
        if (cityRx != null)
        {
          counterparty.City = cityRx;
          counterparty.Region = cityRx.Region;
        }
        if (localityRx != null)
        {
          counterparty.City = localityRx;
          counterparty.Region = localityRx.Region;
        }
      }
      
      counterparty.Save();
    }
    
    // TODO Dmitriev_IA: Возможно стоит удалить метод. Выше есть AddressInPostalFormat().
    private string GetAddress(NpoComputer.DCX.Common.OrganizationAddress address)
    {
      return string.Join(", ", new string[] { address.Street, address.House, address.Building, address.Apartment, address.City, address.PostalCode });
    }
    
    /// <summary>
    /// Создать уведомления о конфликтах синхронизации.
    /// </summary>
    /// <param name="conflicts">Список КА, которых не удалось однозначно определить в RX.</param>
    public virtual void CreateConflictTask(System.Collections.Generic.Dictionary<NpoComputer.DCX.Common.IContact, List<Parties.ICounterparty>> conflicts)
    {
      var duplicates = new System.Collections.Generic.Dictionary<NpoComputer.DCX.Common.Organization, List<Parties.ICounterparty>>();
      duplicates = conflicts.ToDictionary(c => c.Key.Organization, c => c.Value);
      this.CreateConflictTask(duplicates, false);
    }
    
    /// <summary>
    /// Создать задачу о конфликтах синхронизации.
    /// </summary>
    /// <param name="conflicts">Список КА, которых не удалось однозначно определить в RX.</param>
    /// <param name="isExchangeBoxConflict">Признак найденых дублей с эл. обменом.</param>
    public virtual void CreateConflictTask(System.Collections.Generic.Dictionary<NpoComputer.DCX.Common.Organization, List<Parties.ICounterparty>> conflicts,
                                           bool isExchangeBoxConflict)
    {
      if (!conflicts.Any())
        return;
      
      foreach (var conflict in conflicts)
      {
        var queueItem = CounterpartyQueueItems.GetAll(q => Equals(q.Box, _obj)).FirstOrDefault(q => q.ExternalId == conflict.Key.OrganizationId);
        this.CreateCounterpartyConflictTask(conflict.Key, conflict.Value, queueItem, isExchangeBoxConflict);
      }
    }
    
    /// <summary>
    /// Создать и отправить задачу решения конфликта синхронизации контрагента.
    /// </summary>
    /// <param name="serviceOrganization">Организация с сервиса обмена.</param>
    /// <param name="organizations">Дублирующие организации в RX.</param>
    /// <param name="queueItem">Элемент очереди синхронизации контрагентов, если есть.</param>
    /// <param name="isExchangeBoxConflict">Признак найденых дублей с эл. обменом.</param>
    public virtual void CreateCounterpartyConflictTask(NpoComputer.DCX.Common.Organization serviceOrganization,
                                                       List<Parties.ICounterparty> organizations, ICounterpartyQueueItem queueItem,
                                                       bool isExchangeBoxConflict)
    {
      // Если по данному контрагенту уже есть в работе задача на обработку конфликтов синхронизации - новую создавать не нужно.
      if ((queueItem != null && queueItem.MatchingTask != null &&
           Equals(queueItem.MatchingTask.Status, Sungero.Workflow.Task.Status.InProcess)) ||
          CounterpartyConflictProcessingTasks.GetAll(t => t.CounterpartyOrganizationId == serviceOrganization.OrganizationId &&
                                                     t.CounterpartyBranchId == serviceOrganization.DepartmentCode &&
                                                     Equals(t.Status, Sungero.Workflow.Task.Status.InProcess)).Any())
        return;
      
      var task = Functions.CounterpartyConflictProcessingTask.Create(_obj, serviceOrganization, organizations, isExchangeBoxConflict);
      if (queueItem != null)
      {
        queueItem.MatchingTask = task;
        queueItem.ProcessingStatus = ExchangeCore.CounterpartyQueueItem.ProcessingStatus.Error;
        queueItem.Note = ExchangeCore.BusinessUnitBoxes.Resources.NeedProcessSyncronizationConflict;
        queueItem.Save();
      }
      
      task.Save();
      task.Start();
    }
    
    /// <summary>
    /// Создать уведомление об изменении статуса обмена.
    /// </summary>
    /// <param name="queueItems">Список КА, для которых необходимо создать уведомления.</param>
    public virtual void CreateNotice(System.Collections.Generic.IList<ICounterpartyQueueItem> queueItems)
    {
      var task = Workflow.SimpleTasks.Create();
      var dateWithUTC = Sungero.Docflow.PublicFunctions.Module.GetDateWithUTCLabel(Calendar.Now);
      var subject = BusinessUnitBoxes.Resources.NoticeSubjectFormat(_obj.BusinessUnit.Name, _obj.ExchangeService.Name, dateWithUTC);
      task.Subject = Docflow.PublicFunctions.Module.CutText(subject, task.Info.Properties.Subject.Length);
      task.ThreadSubject = BusinessUnitBoxes.Resources.NoticeThreadSubject;
      var step = task.RouteSteps.AddNew();
      step.AssignmentType = Workflow.SimpleTask.AssignmentType.Notice;
      step.Performer = _obj.Responsible;

      Functions.BusinessUnitBox.AddLines(_obj, task, queueItems.Where(x => Equals(x.SyncResult, ExchangeCore.CounterpartyQueueItem.SyncResult.Active)), BusinessUnitBoxes.Resources.NoticeActiveSection);
      Functions.BusinessUnitBox.AddLines(_obj, task, queueItems.Where(x => Equals(x.SyncResult, ExchangeCore.CounterpartyQueueItem.SyncResult.ApprovingByUs)), BusinessUnitBoxes.Resources.NoticeApprovingByUsSection);
      Functions.BusinessUnitBox.AddLines(_obj, task, queueItems.Where(x => Equals(x.SyncResult, ExchangeCore.CounterpartyQueueItem.SyncResult.Closed)), BusinessUnitBoxes.Resources.NoticeClosedSection);
      Functions.BusinessUnitBox.AddLines(_obj, task, queueItems.Where(x => Equals(x.SyncResult, ExchangeCore.CounterpartyQueueItem.SyncResult.ApprovingByCA)), BusinessUnitBoxes.Resources.NoticeApprovingByCASection);
      
      task.Save();
      task.Start();
      
      foreach (var queueItem in queueItems)
      {
        if (queueItem.MatchingTask != null && Equals(queueItem.MatchingTask.Status, Workflow.Task.Status.InProcess))
        {
          var incomingAssignment = CounterpartyConflictProcessingAssignments.GetAll().Where(x => Equals(x.Task, queueItem.MatchingTask) &&
                                                                                            Equals(x.Status, Workflow.AssignmentBase.Status.InProcess)).FirstOrDefault();
          
          if (incomingAssignment != null)
          {
            incomingAssignment.ActiveText = BusinessUnitBoxes.Resources.SyncronizationConflictProcessed;
            incomingAssignment.Save();
            incomingAssignment.Complete(ExchangeCore.CounterpartyConflictProcessingAssignment.Result.Complete);
          }
        }
        
        // Для СБИСа могут быть дубли элемента очереди синхронизации контрагента.
        var duplicateQueueItems = CounterpartyQueueItems.GetAll(q => q.ExternalId == queueItem.ExternalId && Equals(q.Box, _obj) &&
                                                                Equals(q.ProcessingStatus, ExchangeCore.CounterpartyQueueItem.ProcessingStatus.NotProcessed) &&
                                                                !Equals(q.Id, queueItem.Id)).ToList();
        duplicateQueueItems.Add(queueItem);
        foreach (var counterpartyQueueItem in duplicateQueueItems)
          CounterpartyQueueItems.Delete(counterpartyQueueItem);
      }
    }
    
    /// <summary>
    /// Добавить текст уведомления.
    /// </summary>
    /// <param name="task">Уведомление.</param>
    /// <param name="queueItems">Элементы очереди для отправки уведомления.</param>
    /// <param name="header">Заголовок раздела.</param>
    public virtual void AddLines(Workflow.ISimpleTask task, System.Collections.Generic.IEnumerable<ICounterpartyQueueItem> queueItems, string header)
    {
      if (queueItems.Any())
      {
        task.ActiveText += header + Environment.NewLine;
        foreach (var queueItem in queueItems)
        {
          task.ActiveText += Constants.BusinessUnitBox.Delimiter;
          if (queueItem.Counterparty != null)
          {
            task.ActiveText += Hyperlinks.Get(queueItem.Counterparty);
            if (!task.Attachments.Contains(queueItem.Counterparty))
              task.Attachments.Add(queueItem.Counterparty);
          }
          
          var counterpartyBox = queueItem.Counterparty.ExchangeBoxes.Where(x => Equals(x.OrganizationId, queueItem.ExternalId)).Select(o => o.CounterpartyBox).FirstOrDefault();
          task.ActiveText += queueItem.Note + " " + counterpartyBox + Environment.NewLine;
        }
        task.ActiveText += Environment.NewLine;
      }
    }
    
    #endregion
    
    #region Синхронизация филиалов и подразделений КА
    
    /// <summary>
    /// Синхронизация филиалов и подразделений контрагентов с сервисом обмена.
    /// </summary>
    [Public]
    public virtual void SyncBoxCounterpartyBranches()
    {
      try
      {
        Exchange.PublicFunctions.Module.LogDebugFormat(_obj, "Execute SyncBoxCounterpartyBranches.");
        var client = Functions.BusinessUnitBox.GetClient(_obj);
        var contacts = client.GetContacts();
        var headCounterparties = this.GetHeadCounterparties(contacts);
        var synchronizedBoxes = new List<Sungero.Parties.ICounterpartyExchangeBoxes>();

        foreach (var headCounterparty in headCounterparties)
        {
          List<Sungero.Parties.ICounterpartyExchangeBoxes> exchangeBoxes;
          this.SyncCounterpartyOrgStructureUnits(client, headCounterparty, out exchangeBoxes);
          if (exchangeBoxes.Any())
            synchronizedBoxes.AddRange(exchangeBoxes);
        }
        
        var closedCounterpartyBoxes = this.CloseCounterpartyBranchesForClosedContacts();
        synchronizedBoxes.AddRange(closedCounterpartyBoxes);
        this.CloseCounterpartyDepartmentsForClosedContacts();
        this.CloseCounterpartyDepartmentsWithChangeBranchId();
        this.SendNotices(synchronizedBoxes);
        Exchange.PublicFunctions.Module.LogDebugFormat(_obj, "Done SyncBoxCounterpartyBranches.");
      }
      catch (Exception ex)
      {
        Exchange.PublicFunctions.Module.LogErrorFormat(_obj, "Sync counterparty branches and departments error for exchange box.", ex);
      }
    }
    
    /// <summary>
    /// Синхронизация филиалов и подразделений контрагента.
    /// </summary>
    /// <param name="client">Клиент.</param>
    /// <param name="headCounterparty">Головная организация контрагента.</param>
    /// <param name="synchronizedBoxes">Список синхронизированных аб. ящиков филиалов.</param>
    public virtual void SyncCounterpartyOrgStructureUnits(NpoComputer.DCX.ClientApi.Client client,
                                                          NpoComputer.DCX.Common.IContact headCounterparty,
                                                          out List<Sungero.Parties.ICounterpartyExchangeBoxes> synchronizedBoxes)
    {
      synchronizedBoxes = new List<Sungero.Parties.ICounterpartyExchangeBoxes>();
      try
      {
        this.LogDebugFormat(headCounterparty, "Execute sync counterparty org structure units.");
        
        var counterpartyOrgStructureUnits = client.GetCounteragentStructure(headCounterparty.Organization.OrganizationId);
        Exchange.PublicFunctions.Module.LogDebugFormat(_obj, string.Format("Counterparty org structure units count: {0}", counterpartyOrgStructureUnits.Count));
        
        synchronizedBoxes = this.SyncCounterpartyBranches(headCounterparty, counterpartyOrgStructureUnits);
        this.SyncCounterpartyDepartments(headCounterparty, counterpartyOrgStructureUnits);
        
        this.LogDebugFormat(headCounterparty, "Done sync counterparty org structure units.");
      }
      catch (Exception ex)
      {
        this.LogErrorFormat(headCounterparty, "Sync counterparty branches and departments error.", ex);
      }
    }
    
    /// <summary>
    /// Получить список контрагентов сервиса обмена, с которыми установлен эл. обмен в RX.
    /// </summary>
    /// <param name="contacts">Список контрагентов с сервиса, включая контрагентов, с которыми закрыт обмен.</param>
    /// <returns>Список контрагентов IContact.</returns>
    public virtual List<NpoComputer.DCX.Common.IContact> GetHeadCounterparties(List<NpoComputer.DCX.Common.IContact> contacts)
    {
      if (!contacts.Any())
        return contacts;
      
      var activeHeadCounterpartyOrgIds = Parties.Counterparties.GetAll()
        .SelectMany(c => c.ExchangeBoxes
                    .Where(b => Equals(b.Box, _obj) && b.Status == Parties.CounterpartyExchangeBoxes.Status.Active &&
                           (b.CounterpartyBranchId == null || b.CounterpartyBranchId == string.Empty))
                    .Select(b => b.OrganizationId))
        .Distinct()
        .ToList();
      
      return contacts.Where(c => activeHeadCounterpartyOrgIds.Contains(c.Organization.OrganizationId) && c.Organization.IsRoaming == false).ToList();
    }
    
    /// <summary>
    /// Синхронизация филиалов контрагента.
    /// </summary>
    /// <param name="headCounterparty">Головная организация контрагента.</param>
    /// <param name="counterpartyOrgStructureUnits">Все подразделения и филиалы контрагента из сервиса обмена.</param>
    /// <returns>Список обновленных аб. ящиков филиалов.</returns>
    public virtual List<Sungero.Parties.ICounterpartyExchangeBoxes> SyncCounterpartyBranches(NpoComputer.DCX.Common.IContact headCounterparty,
                                                                                             List<NpoComputer.DCX.Common.IDepartment> counterpartyOrgStructureUnits)
    {
      var counterpartyBranches = this.GetCounterpartyBranches(counterpartyOrgStructureUnits);
      var currentCounterpartyBranches = Parties.Counterparties.GetAll()
        .Where(p => p.ExchangeBoxes.Any(exb => Equals(exb.Box, _obj) && exb.OrganizationId == headCounterparty.Organization.OrganizationId &&
                                        (exb.CounterpartyBranchId != null && exb.CounterpartyBranchId != string.Empty)))
        .ToList();
      
      var synchronizedBoxes = new List<Sungero.Parties.ICounterpartyExchangeBoxes>();
      foreach (var counterpartyBranch in counterpartyBranches)
      {
        var synchronizedBox = this.SyncCounterpartyBranch(headCounterparty, counterpartyBranch, currentCounterpartyBranches);
        if (synchronizedBox != null)
          synchronizedBoxes.Add(synchronizedBox);
      }
      
      synchronizedBoxes.AddRange(this.SyncCounterpartyDeletedBranches(headCounterparty, counterpartyBranches, currentCounterpartyBranches));
      return synchronizedBoxes;
    }
    
    /// <summary>
    /// Синхронизация филиала контрагента.
    /// </summary>
    /// <param name="headCounterparty">Головная организация контрагента.</param>
    /// <param name="counterpartyBranch">Филиал контрагента.</param>
    /// <param name="currentCounterpartyBranches">Филиалы контрагента в RX.</param>
    /// <returns>Синхронизируемый аб. ящик филиала.</returns>
    public virtual Sungero.Parties.ICounterpartyExchangeBoxes SyncCounterpartyBranch(NpoComputer.DCX.Common.IContact headCounterparty,
                                                                                     NpoComputer.DCX.Common.IDepartment counterpartyBranch,
                                                                                     List<Parties.ICounterparty> currentCounterpartyBranches)
    {
      try
      {
        var counterparty = Parties.Counterparties.Null;
        Sungero.Parties.ICounterpartyExchangeBoxes counterpartyExchBox = null;
        
        var counterpartiesWithBranchId = currentCounterpartyBranches
          .Where(p => p.ExchangeBoxes.Any(exb => Equals(exb.Box, _obj) &&
                                          exb.OrganizationId == headCounterparty.Organization.OrganizationId
                                          && exb.CounterpartyBranchId == counterpartyBranch.Id)).ToList();
        
        // Если нашли в системе дубли эл. обмена - отправить задачу решения конфликтов и прекратить обработку текущего филиала.
        if (counterpartiesWithBranchId.Count > 1)
        {
          // Организация в контакте не содержит информацию о реквизитах филиала, поэтому собираем нужные реквизиты из головы и филиала.
          var serviceOrganization = this.ConvertBranchToOrganization(headCounterparty, counterpartyBranch);
          this.CreateCounterpartyConflictTask(serviceOrganization, counterpartiesWithBranchId, null, true);
          return counterpartyExchBox;
        }

        counterparty = counterpartiesWithBranchId.SingleOrDefault();
        // Филиал с эл. обменом найден, обновить данные.
        if (counterparty != null && Parties.CompanyBases.Is(counterparty))
        {
          var exchangeBox = counterparty.ExchangeBoxes.FirstOrDefault(exb => Equals(exb.Box, _obj) &&
                                                                      string.Equals(exb.CounterpartyBranchId, counterpartyBranch.Id) &&
                                                                      string.Equals(exb.OrganizationId, headCounterparty.Organization.OrganizationId));
          if (exchangeBox != null)
            counterpartyExchBox = this.UpdateCounterpartyExchangeBoxRecord(headCounterparty, counterpartyBranch, counterparty, exchangeBox);
        }
        else
        {
          // Филиал с эл. обменом не найден, поиск по ИНН и КПП.
          var counterpartiesWithSameTin = Parties.Counterparties.GetAll()
            .Where(c => Equals(c.TIN, headCounterparty.Organization.Inn) &&
                   Equals(c.Status, Sungero.CoreEntities.DatabookEntry.Status.Active)).ToList();
          
          var allCompanies = counterpartiesWithSameTin.Select(c => Parties.CompanyBases.As(c)).Where(c => c != null);
          var companiesWithSameTinTrrc = allCompanies.Where(c => Equals(c.TRRC, counterpartyBranch.Kpp)).ToList();

          // Не найдено подходящей организации, создать организацию и эл.обмен.
          if (companiesWithSameTinTrrc.Count == 0)
          {
            counterparty = this.CreateCounterpartyBranch(headCounterparty, counterpartyBranch);
            counterpartyExchBox = this.CreateCounterpartyExchangeBoxRecord(headCounterparty, counterpartyBranch, counterparty);
          }
          else if (companiesWithSameTinTrrc.Count == 1)
          {
            // Найдена одна организация, создать или обновить эл.обмен.
            counterparty = companiesWithSameTinTrrc.Single();
            var exchangeBox = counterparty.ExchangeBoxes.FirstOrDefault(b => Equals(b.Box, _obj) &&
                                                                        b.OrganizationId == headCounterparty.Organization.OrganizationId &&
                                                                        b.IsRoaming == headCounterparty.Organization.IsRoaming &&
                                                                        b.FtsId == headCounterparty.Organization.FnsParticipantId &&
                                                                        b.CounterpartyBranchId == counterpartyBranch.Id);
            if (exchangeBox == null)
              counterpartyExchBox = this.CreateCounterpartyExchangeBoxRecord(headCounterparty, counterpartyBranch, counterparty);
            else
              counterpartyExchBox = this.UpdateCounterpartyExchangeBoxRecord(headCounterparty, counterpartyBranch, counterparty, exchangeBox);
          }
          else
          {
            // Найдено несколько организаций, однозначно нельзя определить нужную, отправить задачу решения конфликта.
            // Организация в контакте не содержит информацию о реквизитах филиала, поэтому собираем нужные реквизиты из головы и филиала.
            var serviceOrganization = this.ConvertBranchToOrganization(headCounterparty, counterpartyBranch);
            this.CreateCounterpartyConflictTask(serviceOrganization, companiesWithSameTinTrrc.ToList<Parties.ICounterparty>(), null, false);
          }
        }
        
        return counterpartyExchBox;
      }
      catch (Exception ex)
      {
        this.LogErrorFormat(headCounterparty, counterpartyBranch, "Sync counterparty branch error.", ex);
      }
      
      return null;
    }
    
    /// <summary>
    /// Создать филиал организации контрагента.
    /// </summary>
    /// <param name="headCounterparty">Головное подразделение организации с сервиса обмена.</param>
    /// <param name="department">Филиал организации с сервиса обмена.</param>
    /// <returns>Филиал организации контрагента.</returns>
    public virtual Parties.ICounterparty CreateCounterpartyBranch(NpoComputer.DCX.Common.IContact headCounterparty,
                                                                  NpoComputer.DCX.Common.IDepartment department)
    {
      this.LogDebugFormat(headCounterparty, department, "Execute CreateCounterpartyBranch.");
      var newContact = new NpoComputer.DCX.Common.Contact();
      newContact.Organization = new NpoComputer.DCX.Common.Organization();
      newContact.Organization.OrganizationId = headCounterparty.Organization.OrganizationId;
      newContact.Organization.IsRoaming = headCounterparty.Organization.IsRoaming;
      newContact.Organization.OrganizationType = headCounterparty.Organization.OrganizationType;
      var counterpartyBranchName = this.GetCounterpartyBranchName(headCounterparty, department);
      newContact.Organization.Name = counterpartyBranchName;
      newContact.Organization.LegalName = counterpartyBranchName;
      newContact.Organization.Inn = headCounterparty.Organization.Inn;
      newContact.Organization.Kpp = department.Kpp;
      newContact.Organization.ExchangeServiceName = headCounterparty.Organization.ExchangeServiceName;
      newContact.Organization.LegalAddress = department.Address;
      
      var counterparty = Functions.BusinessUnitBox.CreateCounterparty(_obj, newContact);
      var company = Parties.Companies.As(counterparty);
      if (company != null)
      {
        company.HeadCompany = this.GetHeadCompany(headCounterparty);
        company.Save();
        return company;
      }
      
      return counterparty;
    }
    
    /// <summary>
    /// Преобразовать филиал в организацию.
    /// </summary>
    /// <param name="headCounterparty">Контакт с головной организацией из сервиса обмена.</param>
    /// <param name="department">Филиал(подразделение с КПП) с сервиса обмена.</param>
    /// <returns>Организация.</returns>
    public virtual NpoComputer.DCX.Common.Organization ConvertBranchToOrganization(NpoComputer.DCX.Common.IContact headCounterparty,
                                                                                   NpoComputer.DCX.Common.IDepartment department)
    {
      var serviceOrganization = new NpoComputer.DCX.Common.Organization();
      serviceOrganization.Name = department.Name ?? headCounterparty.Organization.Name;
      serviceOrganization.Inn = headCounterparty.Organization.Inn;
      serviceOrganization.Kpp = department.Kpp;
      serviceOrganization.OrganizationId = headCounterparty.Organization.OrganizationId;
      serviceOrganization.FnsParticipantId = headCounterparty.Organization.FnsParticipantId;
      serviceOrganization.DepartmentCode = department.Id;
      
      return serviceOrganization;
    }
    
    /// <summary>
    /// Синхронизировать статусы для удаленных в сервисе обмена филиалов.
    /// </summary>
    /// <param name="headCounterparty">Головное подразделение из сервиса обмена.</param>
    /// <param name="counterpartyBranches">Филиалы контрагента из сервиса обмена.</param>
    /// <param name="currentCounterpartyBranches">Филиалы контрагента в RX.</param>
    /// <returns>Список удаленных аб. ящиков филиалов.</returns>
    public virtual List<Sungero.Parties.ICounterpartyExchangeBoxes> SyncCounterpartyDeletedBranches(NpoComputer.DCX.Common.IContact headCounterparty,
                                                                                                    List<NpoComputer.DCX.Common.IDepartment> counterpartyBranches,
                                                                                                    List<Parties.ICounterparty> currentCounterpartyBranches)
    {
      var synchronizedBoxes = new List<Sungero.Parties.ICounterpartyExchangeBoxes>();
      
      this.LogDebugFormat(headCounterparty, "Execute SyncCounterpartyDeletedBranches.");
      // Филиалы головной организации которые есть в RX.
      var currentActiveCounterpartyBranches = currentCounterpartyBranches
        .Where(c => c.ExchangeBoxes.Any(b => Equals(b.Box, _obj) && b.OrganizationId == headCounterparty.Organization.OrganizationId &&
                                        (b.CounterpartyBranchId != null && b.CounterpartyBranchId != string.Empty) &&
                                        b.Status == Parties.CounterpartyExchangeBoxes.Status.Active))
        .ToList();
      
      if (!currentActiveCounterpartyBranches.Any())
        return new List<Sungero.Parties.ICounterpartyExchangeBoxes>();
      
      // Ид филиалов которые есть на сервисе.
      var counterpartyBranchIds = counterpartyBranches.Select(c => c.Id);
      
      // Филиалы головной организации которые есть в RX и нет в сервисе обмена.
      var counterpartyBranchesForClose = currentActiveCounterpartyBranches
        .Where(c => c.ExchangeBoxes.Where(exb => Equals(exb.Box, _obj) && exb.OrganizationId == headCounterparty.Organization.OrganizationId &&
                                          !string.IsNullOrEmpty(exb.CounterpartyBranchId) && exb.Status != Parties.CounterpartyExchangeBoxes.Status.Closed)
               .Select(exb => exb.CounterpartyBranchId)
               .Except(counterpartyBranchIds)
               .Any())
        .ToList();
      
      foreach (var counterpartyBranch in counterpartyBranchesForClose)
      {
        try
        {
          var exchangeBoxesForClose = counterpartyBranch.ExchangeBoxes
            .Where(b => Equals(b.Box, _obj) && b.OrganizationId == headCounterparty.Organization.OrganizationId &&
                   !string.IsNullOrEmpty(b.CounterpartyBranchId) && !counterpartyBranchIds.Contains(b.CounterpartyBranchId) &&
                   b.Status != Parties.CounterpartyExchangeBoxes.Status.Closed)
            .SingleOrDefault();

          if (exchangeBoxesForClose != null)
          {
            exchangeBoxesForClose.Status = Parties.CounterpartyExchangeBoxes.Status.Closed;
            counterpartyBranch.Save();
            synchronizedBoxes.Add(exchangeBoxesForClose);
            this.LogDebugFormat(headCounterparty, counterpartyBranch, "Closed exchange for counterparty branch.");
          }
        }
        catch (Exception ex)
        {
          this.LogErrorFormat(headCounterparty, counterpartyBranch, "Failed to process counterparty branch for close.", ex);
        }
      }
      
      return synchronizedBoxes;
    }
    
    /// <summary>
    /// Получить головную организацию из системы.
    /// </summary>
    /// <param name="headCounterparty">Головное подразделение организации с сервиса обмена.</param>
    /// <returns>Головная организация.</returns>
    public virtual Parties.ICompany GetHeadCompany(NpoComputer.DCX.Common.IContact headCounterparty)
    {
      var headCompany = Parties.Companies.GetAll()
        .Where(p => p.ExchangeBoxes.Any(exb => Equals(exb.Box, _obj) && exb.OrganizationId == headCounterparty.Organization.OrganizationId &&
                                        (exb.CounterpartyBranchId == null || exb.CounterpartyBranchId == string.Empty)))
        .FirstOrDefault();
      
      return headCompany;
    }
    
    /// <summary>
    /// Получить имя филиала организации.
    /// </summary>
    /// <param name="headCounterparty">Головное подразделение организации с сервиса обмена.</param>
    /// <param name="department">Филиал организации с сервиса обмена.</param>
    /// <returns>Имя филиала организации.</returns>
    /// <remarks>Метод создан для обеспечения возможности перекрытия логики именования филиала.</remarks>
    public virtual string GetCounterpartyBranchName(NpoComputer.DCX.Common.IContact headCounterparty,
                                                    NpoComputer.DCX.Common.IDepartment department)
    {
      return department.Name;
    }

    /// <summary>
    /// Создать электронный обмен с контрагентом.
    /// </summary>
    /// <param name="headCounterparty">Головное подразделение организации с сервиса обмена.</param>
    /// <param name="department">Филиал организации с сервиса обмена.</param>
    /// <param name="counterparty">Филиал организации контрагента.</param>
    /// <returns>Электронный обмен с контрагентом.</returns>
    public virtual Sungero.Parties.ICounterpartyExchangeBoxes CreateCounterpartyExchangeBoxRecord(NpoComputer.DCX.Common.IContact headCounterparty,
                                                                                                  NpoComputer.DCX.Common.IDepartment department,
                                                                                                  Sungero.Parties.ICounterparty counterparty)
    {
      this.LogDebugFormat(headCounterparty, department, counterparty, "Execute CreateCounterpartyExchangeBoxRecord.");
      var exchangeBox = counterparty.ExchangeBoxes.AddNew();
      exchangeBox.Box = _obj;
      exchangeBox.OrganizationId = headCounterparty.Organization.OrganizationId;
      exchangeBox.IsRoaming = headCounterparty.Organization.IsRoaming;
      exchangeBox.CounterpartyBox = headCounterparty.Organization.IsRoaming ?
        BusinessUnitBoxes.Resources.IsRoamingCounterpartyBoxFormat(headCounterparty.Organization.ExchangeServiceName) :
        BusinessUnitBoxes.Resources.IsMainCounterpartyBoxFormat(_obj.ExchangeService.Name);
      exchangeBox.Status = Parties.CounterpartyExchangeBoxes.Status.Active;
      exchangeBox.CounterpartyBranchId = department.Id;
      exchangeBox.CounterpartyParentBranchId = department.ParentDepartmentId;
      exchangeBox.FtsId = headCounterparty.Organization.FnsParticipantId;
      Functions.BusinessUnitBox.SetIsDefault(_obj, counterparty);
      counterparty.Save();
      return exchangeBox;
    }
    
    /// <summary>
    /// Обновить филиал при изменении его на сервисе обмена.
    /// </summary>
    /// <param name="headCounterparty">Головное подразделение организации с сервиса обмена.</param>
    /// <param name="department">Филиал организации из сервиса обмена.</param>
    /// <param name="counterparty">Филиал организации в RX.</param>
    /// <param name="exchangeBox">Абонентский ящик филиала.</param>
    /// <returns>Абонентский ящик обновленного филиала.</returns>
    /// <remarks>Возвращает null если филиал не был обновлен, либо обновлен только ИД родительского филиала.</remarks>
    public virtual Sungero.Parties.ICounterpartyExchangeBoxes UpdateCounterpartyExchangeBoxRecord(NpoComputer.DCX.Common.IContact headCounterparty,
                                                                                                  NpoComputer.DCX.Common.IDepartment department,
                                                                                                  Sungero.Parties.ICounterparty counterparty,
                                                                                                  Parties.ICounterpartyExchangeBoxes exchangeBox)
    {
      var changed = false;
      var hierarchyСhanged = false;
      if (!string.Equals(exchangeBox.CounterpartyParentBranchId, department.ParentDepartmentId))
      {
        exchangeBox.CounterpartyParentBranchId = department.ParentDepartmentId;
        hierarchyСhanged = true;
      }
      if (exchangeBox.Status == Parties.CompanyExchangeBoxes.Status.Closed)
      {
        exchangeBox.Status = Parties.CompanyExchangeBoxes.Status.Active;
        changed = true;
      }
      if (!Equals(exchangeBox.CounterpartyBranchId, department.Id))
      {
        exchangeBox.CounterpartyBranchId = department.Id;
        changed = true;
      }
      
      if (changed || hierarchyСhanged)
      {
        counterparty.Save();
        this.LogDebugFormat(headCounterparty, department, counterparty, "Update counterparty branch.");
        
        if (changed)
          return exchangeBox;
      }
      return null;
    }
    
    /// <summary>
    /// Синхронизация подразделений контрагента из сервиса обмена.
    /// </summary>
    /// <param name="headCounterparty">Головная организация контрагента в сервисе обмена.</param>
    /// <param name="counterpartyOrgStructureUnits">Все подразделения и филиалы контрагента в сервисе обмена.</param>
    public virtual void SyncCounterpartyDepartments(NpoComputer.DCX.Common.IContact headCounterparty, List<NpoComputer.DCX.Common.IDepartment> counterpartyOrgStructureUnits)
    {
      var departments = this.GetCounterpartyDepartments(counterpartyOrgStructureUnits);
      foreach (var department in departments)
      {
        this.SyncCounterpartyDepartment(headCounterparty, counterpartyOrgStructureUnits, department);
      }
      
      this.CloseCounterpartyDeletedDepartments(headCounterparty, departments);
    }
    
    /// <summary>
    /// Синхронизация одного подразделения контрагента из сервиса обмена.
    /// </summary>
    /// <param name="headCounterparty">Головная организация контрагента в сервисе обмена.</param>
    /// <param name="counterpartyOrgStructureUnits">Все подразделения и филиалы контрагента в сервисе обмена, к которым может принадлежать подразделение.</param>
    /// <param name="department">Подразделение контрагента в сервисе обмена.</param>
    public virtual void SyncCounterpartyDepartment(NpoComputer.DCX.Common.IContact headCounterparty,
                                                   List<NpoComputer.DCX.Common.IDepartment> counterpartyOrgStructureUnits,
                                                   NpoComputer.DCX.Common.IDepartment department)
    {
      var organizationId = headCounterparty.Organization.OrganizationId;
      // Головная организация и филиалы.
      var counterparties = Parties.Counterparties.GetAll(c => c.ExchangeBoxes.Any(b => Equals(b.Box, _obj) && Equals(b.OrganizationId, organizationId))).ToList();
      var counterpartyIds = counterparties.Select(x => x.Id).ToList();
      try
      {
        var counterparty = Sungero.Parties.Counterparties.Null;
        // Получить филиал, к которому относится подразделение.
        var parentBranch = this.GetParentBranch(department, counterpartyOrgStructureUnits);
        
        if (parentBranch != null)
          // Найти организацию-филиал.
          counterparty = counterparties.Where(c => c.ExchangeBoxes.Any(b => Equals(b.Box, _obj) &&
                                                                       Equals(b.OrganizationId, organizationId) &&
                                                                       Equals(b.CounterpartyBranchId, parentBranch.Id))).SingleOrDefault();
        else
          // Найти головную организацию.
          counterparty = counterparties.Where(c => c.ExchangeBoxes.Any(b => Equals(b.Box, _obj) &&
                                                                       Equals(b.OrganizationId, organizationId) &&
                                                                       (b.CounterpartyBranchId == null || b.CounterpartyBranchId == string.Empty))).SingleOrDefault();
        
        if (counterparty != null)
        {
          var parentBranchId = parentBranch?.Id ?? string.Empty;
          var counterpartyDepartmentBox = CounterpartyDepartmentBoxes.GetAll().Where(c => Equals(c.Box, _obj) &&
                                                                                     Equals(c.OrganizationId, organizationId) &&
                                                                                     Equals(c.DepartmentId, department.Id) &&
                                                                                     Equals(c.ParentBranchId ?? string.Empty, parentBranchId)).SingleOrDefault();
          if (counterpartyDepartmentBox != null)
          {
            this.UpdateCounterpartyDepartmentBox(counterpartyDepartmentBox, department, counterparty);
            this.LogDebugFormat(headCounterparty, department, "Department successfully updated.");
          }
          else
          {
            counterpartyDepartmentBox = this.CreateCounterpartyDepartmentBox(headCounterparty, department, counterparty, parentBranch);
            this.LogDebugFormat(headCounterparty, department, "Department successfully created.");
          }
        }
        else
        {
          var activeConflictProcessingTaskExist = false;
          // Организация может быть не найдена только для филиала.
          // Проверить наличие задачи на разрешение конфликта.
          if (parentBranch != null && !parentBranch.IsHead)
          {
            
            activeConflictProcessingTaskExist = CounterpartyConflictProcessingTasks
              .GetAll(t => t.CounterpartyOrganizationId == organizationId &&
                      t.CounterpartyBranchId == department.ParentDepartmentId &&
                      Equals(t.Status, Sungero.Workflow.Task.Status.InProcess))
              .Any();
          }
          else
          {
            activeConflictProcessingTaskExist = CounterpartyConflictProcessingTasks
              .GetAll(t => t.CounterpartyOrganizationId == organizationId &&
                      (t.CounterpartyBranchId == null || t.CounterpartyBranchId == string.Empty) &&
                      Equals(t.Status, Sungero.Workflow.Task.Status.InProcess))
              .Any();
          }
          
          if (activeConflictProcessingTaskExist)
          {
            this.LogDebugFormat(headCounterparty, department, "Department sync failed, need to resolve the sync conflicts of the parent organization.");
          }
          else
          {
            this.LogDebugFormat(headCounterparty, department, "Unable to sync department. The parent organization needs to be synchronized first.");
          }
        }
      }
      catch (Exception ex)
      {
        this.LogErrorFormat(headCounterparty, department, "Execute Sync department error.", ex);
      }
    }
    
    /// <summary>
    /// Закрыть подразделения контрагента, удалённые в сервисе обмена.
    /// </summary>
    /// <param name="counterparty">Контрагент в сервисе обмена.</param>
    /// <param name="departmentsFromService">Все подразделения контрагента в сервисе обмена.</param>
    public virtual void CloseCounterpartyDeletedDepartments(NpoComputer.DCX.Common.IContact counterparty,
                                                            List<NpoComputer.DCX.Common.IDepartment> departmentsFromService)
    {
      this.LogDebugFormat(counterparty, "Execute close irrelevant departments.");
      var departmentIds = departmentsFromService.Select(d => d.Id).ToList();
      var irrelevantDepartmentBoxes = CounterpartyDepartmentBoxes.GetAll().Where(c => Equals(c.Box, _obj) &&
                                                                                 Equals(c.OrganizationId, counterparty.Organization.OrganizationId) &&
                                                                                 !departmentIds.Contains(c.DepartmentId) &&
                                                                                 c.Status == ExchangeCore.CounterpartyDepartmentBox.Status.Active).ToList();
      this.LogDebugFormat(counterparty, $"Irrelevant departments count {irrelevantDepartmentBoxes.Count}.");
      this.CloseCounterpartyDepartmentBoxes(irrelevantDepartmentBoxes);
      this.LogDebugFormat(counterparty, "Done close irrelevant departments.");
    }
    
    /// <summary>
    /// Создать абонентский ящик подразделения контрагента.
    /// </summary>
    /// <param name="headCounterparty">Головная организация контрагента в сервисе обмена.</param>
    /// <param name="department">Подразделение контрагента в сервисе обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="parentBranch">ИД родительского филиала в сервисе обмена.</param>
    /// <returns>Абонентский ящик подразделения контрагента.</returns>
    public virtual ICounterpartyDepartmentBox CreateCounterpartyDepartmentBox(NpoComputer.DCX.Common.IContact headCounterparty,
                                                                              NpoComputer.DCX.Common.IDepartment department,
                                                                              Sungero.Parties.ICounterparty counterparty, NpoComputer.DCX.Common.IDepartment parentBranch)
    {
      this.LogDebugFormat(headCounterparty, department, counterparty, "Execute CreateCounterpartyDepartmentBox.");
      
      var counterpartyDepartmentBox = CounterpartyDepartmentBoxes.Create();
      counterpartyDepartmentBox.Box = _obj;
      counterpartyDepartmentBox.Counterparty = counterparty;
      counterpartyDepartmentBox.DepartmentId = department.Id;
      counterpartyDepartmentBox.ParentBranchId = parentBranch?.Id;
      counterpartyDepartmentBox.FtsId = headCounterparty.Organization.FnsParticipantId;
      counterpartyDepartmentBox.Name = Sungero.Docflow.PublicFunctions.Module.CutText(department.Name, CounterpartyDepartmentBoxes.Info.Properties.Name.Length);
      counterpartyDepartmentBox.OrganizationId = headCounterparty.Organization.OrganizationId;
      counterpartyDepartmentBox.Address = Functions.BusinessUnitBox.ConvertAddressToPostalFormat(department.Address);
      counterpartyDepartmentBox.Status = ExchangeCore.CounterpartyDepartmentBox.Status.Active;
      counterpartyDepartmentBox.Save();
      
      Exchange.PublicFunctions.Module.LogDebugFormat(counterpartyDepartmentBox, "Created counterparty department box.");
      
      return counterpartyDepartmentBox;
    }

    /// <summary>
    /// Обновить абонентский ящик подразделения контрагента.
    /// </summary>
    /// <param name="counterpartyDepartmentBox">Абонентский ящик подразделения контрагента.</param>
    /// <param name="department">Подразделение контрагента из сервиса обмена.</param>
    /// <param name="сounterparty">Контрагент.</param>
    public virtual void UpdateCounterpartyDepartmentBox(ICounterpartyDepartmentBox counterpartyDepartmentBox,
                                                        NpoComputer.DCX.Common.IDepartment department,
                                                        Sungero.Parties.ICounterparty сounterparty)
    {
      var changed = false;

      // Обновить наименование подразделения, если оно изменилось.
      var cutDepartmentName = Sungero.Docflow.PublicFunctions.Module.CutText(department.Name, CounterpartyDepartmentBoxes.Info.Properties.Name.Length);
      if (counterpartyDepartmentBox.Name != cutDepartmentName)
      {
        counterpartyDepartmentBox.Name = cutDepartmentName;
        changed = true;
      }

      // Обновить адрес подразделения, если он изменился.
      var departmentAddress = Functions.BusinessUnitBox.ConvertAddressToPostalFormat(department.Address);
      if (counterpartyDepartmentBox.Address != departmentAddress)
      {
        counterpartyDepartmentBox.Address = departmentAddress;
        changed = true;
      }

      // Обновить контрагента, если он изменился (например, подразделение переместили из одного филиала в другой).
      if (!Equals(counterpartyDepartmentBox.Counterparty, сounterparty))
      {
        counterpartyDepartmentBox.Counterparty = сounterparty;
        changed = true;
      }
      
      // Обновить состояние подразделения, если оно изменилось.
      if (counterpartyDepartmentBox.Status != ExchangeCore.CounterpartyDepartmentBox.Status.Active)
      {
        counterpartyDepartmentBox.Status = ExchangeCore.CounterpartyDepartmentBox.Status.Active;
        changed = true;
      }
      
      if (changed)
      {
        counterpartyDepartmentBox.Save();
        Exchange.PublicFunctions.Module.LogDebugFormat(counterpartyDepartmentBox, "Updated counterparty department box.");
      }
    }
    
    /// <summary>
    /// Закрыть эл. обмен в филиалах если был закрыт обмен в головной организации на сервисе.
    /// </summary>
    /// <returns>Список закрытых аб. ящиков филиалов.</returns>
    public virtual List<Sungero.Parties.ICounterpartyExchangeBoxes> CloseCounterpartyBranchesForClosedContacts()
    {
      Exchange.PublicFunctions.Module.LogDebugFormat(_obj, "Start close counterparty branches for closed head counterparties.");
      
      // Филиалы с установленным электронным обменом и закрытым обменом с головной организацией.
      var branchesToClose = Parties.Counterparties.GetAll()
        .SelectMany(e => e.ExchangeBoxes)
        .Where(branchBox => Equals(branchBox.Box, _obj) &&
               branchBox.Status != Parties.CounterpartyExchangeBoxes.Status.Closed &&
               (branchBox.CounterpartyBranchId != null && branchBox.CounterpartyBranchId != string.Empty) &&
               Parties.Counterparties.GetAll().SelectMany(x => x.ExchangeBoxes)
               .Where(headBox => string.Equals(headBox.OrganizationId, branchBox.OrganizationId) &&
                      Equals(headBox.Box, _obj) &&
                      headBox.Status == Parties.CounterpartyExchangeBoxes.Status.Closed &&
                      (headBox.CounterpartyBranchId == null || branchBox.CounterpartyBranchId == string.Empty)).Any())
        .Select(exBox => exBox.Counterparty)
        .Distinct()
        .ToList();
      
      return this.CloseCounterpartyBranches(branchesToClose);
    }
    
    /// <summary>
    /// Закрыть абонентские ящики подразделений контрагентов, с которыми закрыт обмен в сервисе.
    /// </summary>
    public virtual void CloseCounterpartyDepartmentsForClosedContacts()
    {
      Exchange.PublicFunctions.Module.LogDebugFormat(_obj, "Start close counterparty departments for closed head counterparties.");
      
      var departmentBoxesToClose = CounterpartyDepartmentBoxes.GetAll()
        .Where(d => d.Status == ExchangeCore.CounterpartyDepartmentBox.Status.Active && Equals(d.Box, _obj) &&
               Parties.Counterparties.GetAll().Any(p => Equals(p, d.Counterparty) &&
                                                   p.ExchangeBoxes.Any(e => e.OrganizationId == d.OrganizationId &&
                                                                       e.Status == Parties.CounterpartyExchangeBoxes.Status.Closed && Equals(e.Box, _obj))))
        .ToList();
      
      this.CloseCounterpartyDepartmentBoxes(departmentBoxesToClose);
    }
    
    /// <summary>
    /// Закрыть абонентские ящики подразделений контрагентов, для которых изменился родительский филиал.
    /// </summary>
    public virtual void CloseCounterpartyDepartmentsWithChangeBranchId()
    {
      Exchange.PublicFunctions.Module.LogDebugFormat(_obj, "Start close counterparty departments with change parent branch id.");
      
      var departmentBoxesToClose = CounterpartyDepartmentBoxes.GetAll()
        .Where(d => d.Status == ExchangeCore.CounterpartyDepartmentBox.Status.Active && Equals(d.Box, _obj) &&
               Parties.Counterparties.GetAll().Any(p => Equals(p, d.Counterparty) &&
                                                   (p.ExchangeBoxes.Any(e => e.OrganizationId == d.OrganizationId && Equals(e.Box, _obj) &&
                                                                        !string.Equals(e.CounterpartyBranchId, d.ParentBranchId)) ||
                                                    !p.ExchangeBoxes.Any(e => e.OrganizationId == d.OrganizationId && Equals(e.Box, _obj) &&
                                                                         string.Equals(e.CounterpartyBranchId, d.ParentBranchId)))))
        .ToList();
      
      this.CloseCounterpartyDepartmentBoxes(departmentBoxesToClose);
    }
    
    /// <summary>
    /// Закрыть электоронный обмен для филиалов.
    /// </summary>
    /// <param name="branchesToClose">Филиалы с которыми установлен обмен.</param>
    /// <returns>Список закрытых аб. ящиков филиалов.</returns>
    public virtual List<Sungero.Parties.ICounterpartyExchangeBoxes> CloseCounterpartyBranches(List<Sungero.Parties.ICounterparty> branchesToClose)
    {
      var closedBoxes = new List<Sungero.Parties.ICounterpartyExchangeBoxes>();
      foreach (var branch in branchesToClose)
      {
        var exchangeBox = branch.ExchangeBoxes.SingleOrDefault(b => Equals(b.Box, _obj) &&
                                                               b.Status != Parties.CounterpartyExchangeBoxes.Status.Closed &&
                                                               (b.CounterpartyBranchId != null && b.CounterpartyBranchId != string.Empty));
        if (exchangeBox != null)
        {
          exchangeBox.Status = Parties.CounterpartyExchangeBoxes.Status.Closed;
          branch.Save();
          closedBoxes.Add(exchangeBox);
          this.LogDebugFormat(branch, "Closed exchange for counterparty branch.");
        }
      }
      
      return closedBoxes;
    }
    
    /// <summary>
    /// Закрыть абонентские ящики подразделений контрагента.
    /// </summary>
    /// <param name="boxesToClose">Ящики, которые необходимо закрыть.</param>
    public virtual void CloseCounterpartyDepartmentBoxes(List<ICounterpartyDepartmentBox> boxesToClose)
    {
      foreach (var box in boxesToClose)
      {
        box.Status = ExchangeCore.CounterpartyDepartmentBox.Status.Closed;
        box.Save();
        
        Exchange.PublicFunctions.Module.LogDebugFormat(box, "Closed counterparty department box.");
      }
    }
    
    /// <summary>
    /// Получить филиал, к которому относится подразделение в сервисе обмена.
    /// </summary>
    /// <param name="currentDepartment">Подразделение контрагента в сервисе обмена.</param>
    /// <param name="counterpartyOrgStructureUnits">Все подразделения и филиалы контрагента из сервиса обмена.</param>
    /// <returns>Филиал или null (если подразделение относится к головной организации).</returns>
    public virtual NpoComputer.DCX.Common.IDepartment GetParentBranch(NpoComputer.DCX.Common.IDepartment currentDepartment,
                                                                      List<NpoComputer.DCX.Common.IDepartment> counterpartyOrgStructureUnits)
    {
      // Если текущее подразделение является головным, возвращаем null.
      if (currentDepartment.IsHead)
        return null;
      
      var parentDepartment = counterpartyOrgStructureUnits.Where(o => Equals(o.Id, currentDepartment.ParentDepartmentId)).FirstOrDefault();
      
      // Если не нашли родительское, возвращаем null.
      if (parentDepartment == null)
        return null;
      
      // Если родительское подразделение головное, возвращаем null.
      if (parentDepartment.IsHead)
        return null;
      
      // Если родительское подразделение - не головное и не филиал, то ищем дальше.
      if (!parentDepartment.IsHead && string.IsNullOrEmpty(parentDepartment.Kpp))
        parentDepartment = this.GetParentBranch(parentDepartment, counterpartyOrgStructureUnits);
      
      return parentDepartment;
    }
    
    /// <summary>
    /// Получить список филиалов организации с сервиса обмена.
    /// </summary>
    /// <param name="counterpartyOrgStructureUnits">Список элементов оргструктуры организации.</param>
    /// <returns>Список филиалов организации.</returns>
    public virtual List<NpoComputer.DCX.Common.IDepartment> GetCounterpartyBranches(List<NpoComputer.DCX.Common.IDepartment> counterpartyOrgStructureUnits)
    {
      return counterpartyOrgStructureUnits.Where(item => !string.IsNullOrEmpty(item.Kpp) && !item.IsHead).ToList();
    }
    
    /// <summary>
    /// Получить список подразделений организации с сервиса обмена.
    /// </summary>
    /// <param name="counterpartyOrgStructureUnits">Список элементов оргструктуры организации.</param>
    /// <returns>Список подразделений организации.</returns>
    public virtual List<NpoComputer.DCX.Common.IDepartment> GetCounterpartyDepartments(List<NpoComputer.DCX.Common.IDepartment> counterpartyOrgStructureUnits)
    {
      return counterpartyOrgStructureUnits.Where(item => string.IsNullOrEmpty(item.Kpp) && !item.IsHead).ToList();
    }
    
    /// <summary>
    /// Отправить уведомления о синхронизации филиалов.
    /// </summary>
    /// <param name="synchronizedBoxes">Список синхронизируемых аб. ящиков филиалов.</param>
    public virtual void SendNotices(List<Sungero.Parties.ICounterpartyExchangeBoxes> synchronizedBoxes)
    {
      if (synchronizedBoxes.Any())
      {
        var begin = 0;
        var total = Constants.BusinessUnitBox.CounterpartySyncBatchSize;
        var synchronizedBoxesForNotice = synchronizedBoxes.Take(total).ToList();
        
        while (synchronizedBoxesForNotice.Any())
        {
          Transactions.Execute(
            () =>
            {
              Functions.BusinessUnitBox.CreateBranchNotice(_obj, synchronizedBoxesForNotice);
            });

          begin += total;
          synchronizedBoxesForNotice = synchronizedBoxes.Skip(begin).Take(total).ToList();
        }
      }
    }
    
    /// <summary>
    /// Создать увдеомление о синхронизации филиалов.
    /// </summary>
    /// <param name="boxesForNotice">Список аб. ящиков филиалов для уведомления.</param>
    public virtual void CreateBranchNotice(List<Sungero.Parties.ICounterpartyExchangeBoxes> boxesForNotice)
    {
      var task = Workflow.SimpleTasks.Create();
      var dateWithUTC = Sungero.Docflow.PublicFunctions.Module.GetDateWithUTCLabel(Calendar.Now);
      var subject = Sungero.ExchangeCore.BusinessUnitBoxes.Resources.NoticeBranchSubjectFormat(_obj.BusinessUnit.Name, _obj.ExchangeService.Name, dateWithUTC);
      task.Subject = Sungero.Docflow.PublicFunctions.Module.CutText(subject, task.Info.Properties.Subject.Length);
      task.ThreadSubject = Sungero.ExchangeCore.BusinessUnitBoxes.Resources.NoticeBranchThreadSubject;
      var step = task.RouteSteps.AddNew();
      step.AssignmentType = Workflow.SimpleTask.AssignmentType.Notice;
      step.Performer = _obj.Responsible;
      
      var activeBoxes = boxesForNotice.Where(b => b.Status == Parties.CounterpartyExchangeBoxes.Status.Active).ToList();
      this.AddBranchLines(task, activeBoxes, Sungero.ExchangeCore.BusinessUnitBoxes.Resources.NoticeActiveBranchSection);
      
      var closedBoxes = boxesForNotice.Where(b => b.Status == Parties.CounterpartyExchangeBoxes.Status.Closed).ToList();
      this.AddBranchLines(task, closedBoxes, Sungero.ExchangeCore.BusinessUnitBoxes.Resources.NoticeClosedBranchSection);
      
      task.Save();
      task.Start();
    }
    
    /// <summary>
    /// Добавить текст уведомления по списку синхронизируемых филиалов.
    /// </summary>
    /// <param name="task">Уведомление.</param>
    /// <param name="boxesForNotice">Список синхронизируемых аб. ящиков филиалов для отправки уведомления.</param>
    /// <param name="header">Заголовок раздела.</param>
    public virtual void AddBranchLines(Workflow.ISimpleTask task, List<Sungero.Parties.ICounterpartyExchangeBoxes> boxesForNotice, string header)
    {
      if (boxesForNotice.Any())
      {
        task.ActiveText += header + Environment.NewLine;
        foreach (var boxForNotice in boxesForNotice)
        {
          task.ActiveText += Constants.BusinessUnitBox.Delimiter;
          if (boxForNotice.Counterparty != null)
          {
            task.ActiveText += BusinessUnitBoxes.Resources.NoticeLineFormat(Hyperlinks.Get(boxForNotice.Counterparty), boxForNotice.Counterparty.TIN,
                                                                            Sungero.Parties.CompanyBases.As(boxForNotice.Counterparty).TRRC);
            var exchangeBox = boxForNotice.Box;
            task.ActiveText += " " + exchangeBox + Environment.NewLine;
            if (!task.Attachments.Contains(boxForNotice.Counterparty))
              task.Attachments.Add(boxForNotice.Counterparty);
          }
        }
        task.ActiveText += Environment.NewLine;
      }
    }
    
    #endregion
    
    /// <summary>
    /// Получить сервисы обмена, для которых у заданной НОР уже есть абонентские ящики.
    /// </summary>
    /// <param name="businessUnit">НОР.</param>
    /// <returns>Сервисы обмена.</returns>
    [Remote(IsPure = true), Public]
    public List<IExchangeService> GetUsedServicesOfBox(IBusinessUnit businessUnit)
    {
      return BusinessUnitBoxes.GetAll()
        .Where(x => Equals(x.BusinessUnit, businessUnit) && x.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
        .Select(b => b.ExchangeService).Distinct().ToList();
    }
    
    /// <summary>
    /// Получить все сервисы обмена, для которых есть абонентские ящики.
    /// </summary>
    /// <returns>Сервисы обмена.</returns>
    [Remote(IsPure = true), Public]
    public static List<IExchangeService> GetUsedServicesOfBox()
    {
      return BusinessUnitBoxes.GetAll()
        .Where(x => x.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
        .Select(b => b.ExchangeService).Distinct().ToList();
    }

    /// <summary>
    /// Получить сервис обмена по умолчанию.
    /// </summary>
    /// <returns>Сервис обмена.</returns>
    [Remote, Public]
    public IExchangeService GetDefaultExchangeService()
    {
      var alreadyUsedServices = this.GetUsedServicesOfBox(_obj.BusinessUnit);
      var exchangeServices = ExchangeServices.GetAll(s => s.Status == Sungero.CoreEntities.DatabookEntry.Status.Active).Where(x => !alreadyUsedServices.Contains(x)).ToList();
      
      return exchangeServices.FirstOrDefault();
    }
    
    /// <summary>
    /// Получить сервис обмена ящика.
    /// </summary>
    /// <returns>Сервис обмена.</returns>
    public override IExchangeService GetExchangeService()
    {
      return _obj.ExchangeService;
    }
    
    /// <summary>
    /// Получить НОР ящика.
    /// </summary>
    /// <returns>Наша организация.</returns>
    [Public]
    public override Sungero.Company.IBusinessUnit GetBusinessUnit()
    {
      return _obj.BusinessUnit;
    }
    
    /// <summary>
    /// Получить все действующие сертификаты.
    /// </summary>
    /// <returns>Список действующих сертификатов.</returns>
    public virtual List<ICertificate> GetActualCertificates()
    {
      var certificates = Certificates.GetAll(c => c.Enabled == true &&
                                             (!c.NotAfter.HasValue || c.NotAfter >= Calendar.Now) &&
                                             (!c.NotBefore.HasValue || c.NotBefore <= Calendar.Now));
      
      return certificates.ToList();
    }

    /// <summary>
    /// Найти сертификаты, зарегистрированные на пользователя в системе.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Сертификаты пользователя.</returns>
    [Remote(IsPure = true), Public]
    public IQueryable<ICertificate> GetCertificatesOfEmployee(IEmployee employee)
    {
      return Certificates.GetAll().Where(x => Equals(x.Owner, employee) && x.Enabled == true);
    }
    
    /// <summary>
    /// Проверить, есть ли сертификаты ответственного в сервисе обмена.
    /// </summary>
    /// <param name="responsible">Ответственный.</param>
    /// <returns>Результат проверки.</returns>
    [Remote(IsPure = true), Public]
    public bool CheckResponsibleServiceCertificates(IEmployee responsible)
    {
      return (_obj.HasExchangeServiceCertificates == false) || _obj.ExchangeServiceCertificates.Select(x => x.Certificate).Any(x => x.Enabled == true && Equals(x.Owner, responsible));
    }
    
    /// <summary>
    /// Проверить, есть ли сертификаты ответственного в сервисе обмена и RX.
    /// </summary>
    /// <param name="responsible">Ответственный.</param>
    /// <returns>Результат проверки.</returns>
    [Remote(IsPure = true), Public]
    public bool CheckAllResponsibleCertificates(IEmployee responsible)
    {
      if (_obj.ConnectionStatus == Sungero.ExchangeCore.BusinessUnitBox.ConnectionStatus.Connected)
        return this.CheckResponsibleServiceCertificates(responsible);
      else
        return this.GetCertificatesOfEmployee(responsible).Any();
    }
    
    /// <summary>
    /// Проверить, совпадают ли ИНН/КПП нашей организации и ИНН/КПП в сервисе.
    /// </summary>
    /// <returns>Результат проверки.</returns>
    [Remote(IsPure = true)]
    public bool CheckBusinessUnitTinTRRC()
    {
      if (_obj.ConnectionStatus != Sungero.ExchangeCore.BusinessUnitBox.ConnectionStatus.Connected)
        return true;
      
      var client = this.GetClient();
      return Equals(client.OurSubscriber.Organization.Inn, _obj.BusinessUnit.TIN) && Equals(client.OurSubscriber.Organization.Kpp ?? string.Empty, _obj.BusinessUnit.TRRC ?? string.Empty);
    }
    
    /// <summary>
    /// Обновить список сертификатов сервиса обмена, имеющихся в системе RX, в свойство-коллекцию ящика.
    /// </summary>
    [Remote]
    public virtual void UpdateExchangeServiceCertificates()
    {
      var needSave = !_obj.State.IsChanged;
      var client = this.GetClient();
      if (!Equals(_obj.HasExchangeServiceCertificates, client.CanGetOurSubscriberCertificates))
        _obj.HasExchangeServiceCertificates = client.CanGetOurSubscriberCertificates;
      
      if (_obj.HasExchangeServiceCertificates == true)
      {
        var allCertificates = this.GetActualCertificates();
        var serviceCertificates = client.GetOrganizationCertificates(allCertificates.Select(x => x.X509Certificate).ToList()).ToList();
        var boxCertificates = _obj.ExchangeServiceCertificates.Select(x => x.Certificate).ToList();
        
        var exchangeCertificatesToDelete = boxCertificates
          .Where(x => !serviceCertificates.Any(c => c.Thumbprint.Equals(x.Thumbprint, StringComparison.InvariantCultureIgnoreCase)))
          .ToList();
        foreach (var certificateToDelete in exchangeCertificatesToDelete)
        {
          var certificateStringToDelete = _obj.ExchangeServiceCertificates
            .Where(x => Equals(x.Certificate, certificateToDelete)).FirstOrDefault();
          _obj.ExchangeServiceCertificates.Remove(certificateStringToDelete);
          
          boxCertificates.Remove(certificateToDelete);
        }
        
        var notInBoxCertificates = allCertificates
          .Where(x => !boxCertificates.Contains(x)).ToList();
        var exchangeCertificatesToAdd = notInBoxCertificates
          .Where(x => serviceCertificates.Any(c => c.Thumbprint.Equals(x.Thumbprint, StringComparison.InvariantCultureIgnoreCase)));
        foreach (var certificate in exchangeCertificatesToAdd)
        {
          _obj.ExchangeServiceCertificates.AddNew().Certificate = certificate;
        }
      }
      
      if (needSave && _obj.State.IsChanged)
        _obj.Save();
    }
    
    /// <summary>
    /// Обновить информацию об эл. доверенностях из сервиса обмена.
    /// </summary>
    public virtual void UpdateServiceFormalizedPoA()
    {
      var client = this.GetClient();
      var serviceFormalizedPoA = client.GetFormalizedPowersOfAttorney(Calendar.Today);
      var isChanged = false;

      foreach (var serviceFPoA in serviceFormalizedPoA)
      {
        if (!_obj.FormalizedPoAInfos.Any(b => string.Equals(b.UnifiedRegistrationNumber, serviceFPoA.UnifiedRegNumber)))
        {
          var item = _obj.FormalizedPoAInfos.AddNew();
          item.UnifiedRegistrationNumber = serviceFPoA.UnifiedRegNumber;
          item.Url = serviceFPoA.Link;
          item.Description = serviceFPoA.LinkTitle;
          isChanged = true;
        }
      }
      
      var unifiedRegNumbers = serviceFormalizedPoA.Select(f => f.UnifiedRegNumber).ToList();
      var obsoleteFPoAInfos = _obj.FormalizedPoAInfos.Where(f => !unifiedRegNumbers.Contains(f.UnifiedRegistrationNumber)).ToList();
      for (int i = obsoleteFPoAInfos.Count - 1; i >= 0; i--)
        _obj.FormalizedPoAInfos.Remove(obsoleteFPoAInfos.ElementAt(i));
      
      if (isChanged || obsoleteFPoAInfos.Any())
        _obj.Save();
    }
    
    /// <summary>
    /// Проверить на уникальность логин и сервис обмена.
    /// </summary>
    /// <returns>Список ошибок.</returns>
    [Remote(IsPure = true)]
    public List<string> CheckProperties()
    {
      return this.BeforeSaveCheckProperties().Select(p => p.Value).ToList();
    }
    
    /// <summary>
    /// Проверить на уникальность логин и сервис обмена.
    /// </summary>
    /// <returns>Список ошибок.</returns>
    public System.Collections.Generic.Dictionary<Sungero.Domain.Shared.IPropertyInfo, string> BeforeSaveCheckProperties()
    {
      var result = new Dictionary<Sungero.Domain.Shared.IPropertyInfo, string>();

      if (!this.LoginIsValid())
        result.Add(_obj.Info.Properties.Login, BusinessUnitBoxes.Resources.LoginIsAlreadyInUse);

      var duplicateBoxExists = BusinessUnitBoxes.GetAll()
        .Any(x => !Equals(x, _obj) && Equals(x.BusinessUnit, _obj.BusinessUnit) &&
             Equals(x.ExchangeService, _obj.ExchangeService));
      if (duplicateBoxExists)
        result.Add(_obj.Info.Properties.ExchangeService, BusinessUnitBoxes.Resources.DuplicateServiceExists);
      
      return result;
    }
    
    /// <summary>
    /// Проверить логин на валидность.
    /// </summary>
    /// <returns>True - если логин валидный, иначе false.</returns>
    public virtual bool LoginIsValid()
    {
      var loginIsAlreadyInUse = BusinessUnitBoxes.GetAll()
        .Any(x => !Equals(x, _obj) && x.Login == _obj.Login &&
             Equals(x.ExchangeService, _obj.ExchangeService));
      
      if (loginIsAlreadyInUse)
        return false;
      
      return true;
    }
    
    /// <summary>
    /// Зашифровать данные.
    /// </summary>
    /// <param name="data">Данные для шифрования.</param>
    /// <returns>Зашифрованные данные.</returns>
    [Remote(IsPure = true)]
    public static string GetEncryptedDataRemote(string data)
    {
      return Encryption.Encrypt(data);
    }
    
    /// <summary>
    /// Проверить сертификаты ящика.
    /// </summary>
    public virtual void ValidateCertificates()
    {
      var certificates = _obj.ExchangeServiceCertificates.Where(x => x.Certificate.Enabled == true && x.Certificate.NotAfter >= Calendar.Today).ToList();
      var hasErrors = false;
      foreach (var certificate in certificates)
      {
        var errors = certificate.Certificate.GetValidationErrors();
        foreach (var error in errors)
          Exchange.PublicFunctions.Module.LogErrorFormat(string.Format("Certificate with id {0} validation error {1}.", certificate.Certificate.Id, error));
        if (errors.Any())
          hasErrors = true;
        
        var expiryDate = Calendar.Today.AddDays(Constants.Module.ExpirePeriod);
        if (expiryDate >= certificate.Certificate.NotAfter)
          Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Certificate with id {0} is expiring {1}.", certificate.Certificate.Id, certificate.Certificate.NotAfter));
      }
      
      if (hasErrors)
        throw AppliedCodeException.Create(Sungero.ExchangeCore.BusinessUnitBoxes.Resources.CertificateValidationError);
    }
    
    /// <summary>
    /// Получение пакета документов пришедшего из системы обмена.
    /// </summary>
    /// <param name="messageId">ИД сообщения.</param>
    /// <param name="documentId">ИД документа в СО.</param>
    /// <returns>Пакет совместно обрабатываемых документов и сообщений из системы обмена.</returns>
    [Remote]
    public static Structures.BusinessUnitBox.ExchangeDocumentsPackage GetExchangeDocumentsPackage(string messageId, string documentId)
    {
      var packageInfo = Structures.BusinessUnitBox.ExchangeDocumentsPackage.Create();
      packageInfo.Messages = new List<IMessageQueueItem>() { };
      packageInfo.Documents = new List<Sungero.ExchangeCore.Structures.BusinessUnitBox.ExchangeProcessedDocument>() { };
      var infos = Exchange.ExchangeDocumentInfos.GetAll(i => i.ServiceMessageId.Contains(messageId) && i.ServiceDocumentId.Contains(documentId));
      foreach (var info in infos)
      {
        var processedDocument = Structures.BusinessUnitBox.ExchangeProcessedDocument.Create();
        processedDocument.DocumentInfo = info;
        
        if (info != null && info.Document != null)
        {
          AccessRights.AllowRead(
            () =>
            {
              processedDocument.Document = info.Document;
              processedDocument.DocumentId = info.Document.Id;
            });
          processedDocument.HasDocumentReadPermissions = info.Document.AccessRights.CanRead();
        }
        packageInfo.Documents.Add(processedDocument);
      }
      
      var messageQueueItems = ExchangeCore.MessageQueueItems.GetAll(
        m => m.ExternalId.Contains(messageId) &&
        m.Documents.Any(d => d.ExternalId.Contains(documentId) && d.Type == Sungero.ExchangeCore.MessageQueueItemDocuments.Type.Primary));
      packageInfo.Messages.AddRange(messageQueueItems);

      return packageInfo;
    }
    
    /// <summary>
    /// Получить активную сессию исторической загрузки сообщений с сервиса.
    /// </summary>
    /// <returns>Сессия исторической загрузки.</returns>
    [Public]
    public virtual IHistoricalMessagesDownloadSession GetHistoricalMessagesDownloadSessionInWork()
    {
      return ExchangeCore.HistoricalMessagesDownloadSessions
        .GetAll(s => Equals(s.BusinessUnitBox, _obj) &&
                s.DownloadingState == ExchangeCore.HistoricalMessagesDownloadSession.DownloadingState.InWork)
        .SingleOrDefault();
    }
    
    #region Логирование
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <param name="text">Сообщение.</param>
    public virtual void LogDebugFormat(NpoComputer.DCX.Common.IContact contact, string text)
    {
      var organizationInfo = this.GetCounterpartyLogInfo(contact, null, null);
      Exchange.PublicFunctions.Module.LogDebugFormat(_obj, string.Format("{0} {1}", text, organizationInfo));
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="text">Сообщение.</param>
    public virtual void LogDebugFormat(Parties.ICounterparty counterparty, string text)
    {
      var organizationInfo = this.GetCounterpartyLogInfo(null, null, counterparty);
      Exchange.PublicFunctions.Module.LogDebugFormat(_obj, string.Format("{0} {1}", text, organizationInfo));
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <param name="department">Подразделение контрагента из сервиса обмена.</param>
    /// <param name="text">Сообщение.</param>
    public virtual void LogDebugFormat(NpoComputer.DCX.Common.IContact contact, NpoComputer.DCX.Common.IDepartment department, string text)
    {
      var organizationInfo = this.GetCounterpartyLogInfo(contact, department, null);
      Exchange.PublicFunctions.Module.LogDebugFormat(_obj, string.Format("{0} {1}", text, organizationInfo));
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="text">Сообщение.</param>
    public virtual void LogDebugFormat(NpoComputer.DCX.Common.IContact contact, Sungero.Parties.ICounterparty counterparty, string text)
    {
      var organizationInfo = this.GetCounterpartyLogInfo(contact, null, counterparty);
      Exchange.PublicFunctions.Module.LogDebugFormat(_obj, string.Format("{0} {1}", text, organizationInfo));
    }

    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <param name="department">Подразделение контрагента из сервиса обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="text">Сообщение.</param>
    public virtual void LogDebugFormat(NpoComputer.DCX.Common.IContact contact, NpoComputer.DCX.Common.IDepartment department,
                                       Sungero.Parties.ICounterparty counterparty, string text)
    {
      var organizationInfo = this.GetCounterpartyLogInfo(contact, department, counterparty);
      Exchange.PublicFunctions.Module.LogDebugFormat(_obj, string.Format("{0} {1}", text, organizationInfo));
    }
    
    /// <summary>
    /// Записать сообщение об ошибке в лог.
    /// </summary>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <param name="department">Подразделение контрагента из сервиса обмена.</param>
    /// <param name="text">Сообщение.</param>
    /// <param name="ex">Исключение.</param>
    public virtual void LogErrorFormat(NpoComputer.DCX.Common.IContact contact, NpoComputer.DCX.Common.IDepartment department, string text, System.Exception ex)
    {
      var organizationInfo = this.GetCounterpartyLogInfo(contact, department, null);
      Exchange.PublicFunctions.Module.LogErrorFormat(_obj, string.Format("{0} {1}", text, organizationInfo), ex);
    }

    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="text">Сообщение.</param>
    /// <param name="ex">Исключение.</param>
    public virtual void LogErrorFormat(NpoComputer.DCX.Common.IContact contact, Sungero.Parties.ICounterparty counterparty, string text, System.Exception ex)
    {
      var organizationInfo = this.GetCounterpartyLogInfo(contact, null, counterparty);
      Exchange.PublicFunctions.Module.LogErrorFormat(_obj, string.Format("{0} {1}", text, organizationInfo), ex);
    }

    /// <summary>
    /// Записать сообщение об ошибке в лог.
    /// </summary>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <param name="text">Сообщение.</param>
    /// <param name="ex">Исключение.</param>
    public virtual void LogErrorFormat(NpoComputer.DCX.Common.IContact contact, string text, System.Exception ex)
    {
      var organizationInfo = this.GetCounterpartyLogInfo(contact, null, null);
      Exchange.PublicFunctions.Module.LogErrorFormat(_obj, string.Format("{0} {1}", text, organizationInfo), ex);
    }
    
    /// <summary>
    /// Получить информацию по контрагенту для вывода в лог.
    /// </summary>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <param name="department">Подразделение контрагента из сервиса обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <returns>Информация по контрагенту.</returns>
    public virtual string GetCounterpartyLogInfo(NpoComputer.DCX.Common.IContact contact, NpoComputer.DCX.Common.IDepartment department,
                                                 Sungero.Parties.ICounterparty counterparty)
    {
      var logText = new System.Text.StringBuilder();
      
      if (contact != null)
      {
        logText.AppendFormat("Service counterparty: OrganizationId: '{0}', TIN: '{1}', TRRC: '{2}'.",
                             contact.Organization.OrganizationId,
                             contact.Organization.Inn,
                             contact.Organization.Kpp);
      }
      
      if (department != null)
        logText.AppendFormat(" Service DepartmentId: '{0}', TRRC: '{1}'.", department.Id, department.Kpp);

      if (counterparty != null)
        logText.AppendFormat(" CounterpartyId: '{0}'.", counterparty.Id);

      return logText.ToString();
    }
    
    #endregion
  }
}