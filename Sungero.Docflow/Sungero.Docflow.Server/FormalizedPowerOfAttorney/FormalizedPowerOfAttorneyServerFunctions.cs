using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FormalizedPowerOfAttorney;
using Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums;
using Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2;
using Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3;
using Sungero.Parties;
using Sungero.PowerOfAttorneyCore;
using PoAServiceDeliveryStatus = Sungero.PowerOfAttorneyCore.PublicConstants.Module.PowerOfAttorneyDeliveryStatus;
using PoAServiceErrors = Sungero.PowerOfAttorneyCore.PublicConstants.Module.PowerOfAttorneyServiceErrors;
using PoAServiceStateStatus = Sungero.PowerOfAttorneyCore.PublicConstants.Module.PowerOfAttorneyStateStatus;
using PowersType = Sungero.Docflow.FormalizedPowerOfAttorney.PowersType;
using XmlElementNames = Sungero.Docflow.Constants.FormalizedPowerOfAttorney.XmlElementNames;
using XmlFPoAInfoAttributeNames = Sungero.Docflow.Constants.FormalizedPowerOfAttorney.XmlFPoAInfoAttributeNames;
using XmlIssuedToAttributeNames = Sungero.Docflow.Constants.FormalizedPowerOfAttorney.XmlIssuedToAttributeNames;
using ДоверенностьДокументДовер = Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДовер;
using ДоверенностьДокументПередов = Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередов;

namespace Sungero.Docflow.Server
{
  partial class FormalizedPowerOfAttorneyFunctions
  {
    
    #region Заявление на отзыв МЧД

    /// <summary>
    /// Получить заявление на отзыв эл. доверенности без учета прав доступа.
    /// </summary>
    /// <returns>Заявление на отзыв эл. доверенности.</returns>
    [Public, Remote]
    public virtual Sungero.Docflow.IPowerOfAttorneyRevocation GetRevocation()
    {
      IPowerOfAttorneyRevocation revocation = null;
      AccessRights.AllowRead(() =>
                             {
                               revocation = PowerOfAttorneyRevocations.GetAll().FirstOrDefault(r => Equals(r.FormalizedPowerOfAttorney, _obj));
                             });
      return revocation;
    }
    
    /// <summary>
    /// Создать заявление на отзыв эл. доверенности.
    /// </summary>
    /// <param name="reason">Причина отзыва доверенности.</param>
    /// <returns>Заявление на отзыв эл. доверенности.</returns>
    [Public, Remote]
    public virtual Sungero.Docflow.IPowerOfAttorneyRevocation CreateRevocation(string reason)
    {
      var revocation = PowerOfAttorneyRevocations.Create();
      Functions.PowerOfAttorneyRevocation.FillRevocationProperties(revocation, _obj, reason);

      if (!Functions.PowerOfAttorneyRevocation.GenerateRevocationBody(revocation))
        return null;
      
      revocation.Relations.Add(Docflow.PublicConstants.Module.SimpleRelationName, _obj);
      revocation.Save();
      Functions.PowerOfAttorneyRevocation.GenerateRevocationPdf(revocation);
      return revocation;
    }
    
    #endregion
    
    #region Генерация МЧД
    
    /// <summary>
    /// Сформировать тело эл. доверенности.
    /// </summary>
    /// <returns>True - если генерация завершилась успешно.</returns>
    [Public, Remote]
    public virtual bool GenerateFormalizedPowerOfAttorneyBody()
    {
      var unifiedRegistrationNumber = Guid.NewGuid();
      var xml = this.CreateFormalizedPowerOfAttorneyXml(unifiedRegistrationNumber);
      var isValidXml = this.ValidateGeneratedFormalizedPowerOfAttorneyXml(xml);
      
      if (isValidXml)
      {
        _obj.UnifiedRegistrationNumber = unifiedRegistrationNumber.ToString();
        this.AddNewVersionIfLastVersionApproved();
        
        // Сохраняем сущность, чтобы избежать формирования некорректной версии в случае, если сущность создана копированием (Bug 283975).
        if (_obj.State.IsCopied)
          _obj.Save();
        
        Functions.OfficialDocument.WriteBytesToDocumentLastVersionBody(_obj, xml, Constants.FormalizedPowerOfAttorney.XmlExtension);
        _obj.LifeCycleState = Docflow.FormalizedPowerOfAttorney.LifeCycleState.Draft;
        _obj.FtsListState = null;
        _obj.FtsRejectReason = string.Empty;
        
        // Удаляем параметр, чтобы не вызывать асинхронный обработчик по выдаче прав на документ, так как это вызывает ошибку (Bug 275290).
        // Асинхронный обработчик запускается после выполнения всех операций по документу.
        var documentParams = ((Sungero.Domain.Shared.IExtendedEntity)_obj).Params;
        if (documentParams.ContainsKey(Sungero.Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToDocumentAsync))
          documentParams.Remove(Sungero.Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToDocumentAsync);
        
        _obj.Save();
      }
      else
      {
        Logger.DebugFormat("Generate formalized power of attorney body validation error. Document id: {0}", _obj.Id);
        return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Создать тело эл. доверенности.
    /// </summary>
    /// <param name="unifiedRegistrationNumber">Единый регистрационный номер доверенности.</param>
    /// <returns>Тело эл. доверенности.</returns>
    public virtual Docflow.Structures.Module.IByteArray CreateFormalizedPowerOfAttorneyXml(Guid unifiedRegistrationNumber)
    {
      if (_obj.FormatVersion == FormatVersion.Version002)
        return _obj.IsDelegated == true
          ? this.CreateRetrustXmlV2(unifiedRegistrationNumber)
          : this.CreateFormalizedPowerOfAttorneyXmlV2(unifiedRegistrationNumber);
      
      if (_obj.FormatVersion == FormatVersion.Version003)
        return _obj.IsDelegated == true
          ? this.CreateRetrustXmlV3(unifiedRegistrationNumber)
          : this.CreateFormalizedPowerOfAttorneyXmlV3(unifiedRegistrationNumber);
      
      Logger.Debug("Unsupported power of attorney format version.");
      return null;
    }
    
    /// <summary>
    /// Получить значение атрибута "ИдФайл".
    /// </summary>
    /// <param name="unifiedRegistrationNumber">Единый регистрационный номер доверенности.</param>
    /// <returns>Значение атрибута "ИдФайл".</returns>
    public virtual string GetFileIdAttribute(Guid unifiedRegistrationNumber)
    {
      return string.Format("{0}_{1}", Calendar.UserNow.ToString("yyyyMMdd"), unifiedRegistrationNumber);
    }
    
    private CitizenshipFlag GetCitizenshipFlag(Sungero.Commons.ICountry country)
    {
      if (country == null)
        return CitizenshipFlag.None;
      
      if (country.Code == Constants.FormalizedPowerOfAttorney.RussianFederationCountryCode)
        return CitizenshipFlag.Russia;

      return CitizenshipFlag.Other;
    }

    /// <summary>
    /// Проверить сформированную xml доверенности.
    /// </summary>
    /// <param name="xml">Тело эл. доверенности.</param>
    /// <returns>True - если проверка xml прошла успешно.</returns>
    [Public]
    public virtual bool ValidateGeneratedFormalizedPowerOfAttorneyXml(Docflow.Structures.Module.IByteArray xml)
    {
      if (xml == null || xml.Bytes == null || xml.Bytes.Length == 0)
        return false;
      
      var validationResult = FormalizeDocumentsParser.Extension.ValidatePowerOfAttorneyXml(xml.Bytes);
      var isValidXml = !validationResult.Any();
      if (!isValidXml)
      {
        Logger.WithProperty("details", string.Join(Environment.NewLine, validationResult))
          .Error("ValidateGeneratedFormalizedPowerOfAttorneyXml. Validation error. Document id: {id}", _obj.Id);
      }
      
      return isValidXml;
    }
    
    /// <summary>
    /// Создать новую версию, если последняя утверждена.
    /// </summary>
    [Public]
    public virtual void AddNewVersionIfLastVersionApproved()
    {
      if (!_obj.HasVersions)
        return;
      
      if (_obj.LastVersionApproved == true)
        _obj.Versions.AddNew();
    }
    
    #endregion
    
    #region Импорт МЧД из xml
    
    /// <summary>
    /// Загрузить тело эл. доверенности из XML и импортировать внешнюю подпись.
    /// </summary>
    /// <param name="xml">Структура с XML.</param>
    /// <param name="signature">Структура с подписью.</param>
    [Remote, Public]
    public virtual void ImportFormalizedPowerOfAttorneyFromXmlAndSign(Docflow.Structures.Module.IByteArray xml,
                                                                      Docflow.Structures.Module.IByteArray signature)
    {
      Functions.FormalizedPowerOfAttorney.SetJustImportedParam(_obj);
      
      this.ValidateFormalizedPowerOfAttorneyXml(xml);
      
      signature = this.ConvertSignatureFromBase64(signature);
      this.VerifyExternalSignature(xml, signature);
      
      this.FillFormalizedPowerOfAttorney(xml);
      this.VerifyDocumentUniqueness();
      
      Functions.OfficialDocument.WriteBytesToDocumentLastVersionBody(_obj, xml, Constants.FormalizedPowerOfAttorney.XmlExtension);
      
      // Удаляем параметр, чтобы не вызывать асинхронный обработчик по выдаче прав на документ, так как это вызывает ошибку (Bug 275290).
      // Асинхронный обработчик запускается после выполнения всех операций по документу.
      var documentParams = ((Sungero.Domain.Shared.IExtendedEntity)_obj).Params;
      if (documentParams.ContainsKey(Sungero.Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToDocumentAsync))
        documentParams.Remove(Sungero.Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToDocumentAsync);
      
      // Сохранение необходимо для импорта подписи.
      _obj.Save();
      
      // Сохранение записи об импорте xml-файла в историю.
      var importFromXmlOperationText = Constants.FormalizedPowerOfAttorney.Operation.ImportFromXml;
      var importFromXmlComment = Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.ImportFromXmlHistoryComment;
      _obj.History.Write(new Enumeration(importFromXmlOperationText), null, importFromXmlComment, _obj.LastVersion.Number);
      
      this.ImportSignature(xml, signature);
      this.CheckSignature();
    }
    
    /// <summary>
    /// Проверка уникальности эл. доверенности по рег.номеру.
    /// </summary>
    /// <remarks>Если эл. доверенности с таким же рег.номером существуют, то генерируется ошибка.</remarks>
    public virtual void VerifyDocumentUniqueness()
    {
      var duplicates = this.GetFormalizedPowerOfAttorneyDuplicates();
      if (duplicates.Any())
        throw new AppliedCodeException(FormalizedPowerOfAttorneys.Resources.DuplicatesDetected);
    }
    
    /// <summary>
    /// Декодировать подпись из base64.
    /// </summary>
    /// <param name="signature">Подпись.</param>
    /// <returns>Декодированная подпись.</returns>
    [Public]
    public virtual Docflow.Structures.Module.IByteArray ConvertSignatureFromBase64(Docflow.Structures.Module.IByteArray signature)
    {
      var signatureInfo = ExternalSignatures.GetSignatureInfo(signature.Bytes);
      // Если подпись передали в закодированном виде, попытаться раскодировать.
      if (signatureInfo.SignatureFormat == SignatureFormat.Hash)
      {
        try
        {
          var byteString = System.Text.Encoding.UTF8.GetString(signature.Bytes);
          var signatureBytes = Convert.FromBase64String(byteString);
          signature = Docflow.Structures.Module.ByteArray.Create(signatureBytes);
        }
        catch
        {
          Logger.Error("Import formalized power of attorney. Failed to import signature: cannot decode given signature.");
          throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.SignatureImportFailed);
        }
      }
      
      return signature;
    }
    
    /// <summary>
    /// Проверить подпись на достоверность.
    /// </summary>
    /// <param name="xml">Подписанные данные.</param>
    /// <param name="signature">Подпись.</param>
    [Public]
    public virtual void VerifyExternalSignature(Docflow.Structures.Module.IByteArray xml, Docflow.Structures.Module.IByteArray signature)
    {
      using (var xmlStream = new System.IO.MemoryStream(xml.Bytes))
      {
        var signatureInfo = ExternalSignatures.Verify(signature.Bytes, xmlStream);
        if (signatureInfo.Errors.Any())
        {
          Logger.ErrorFormat("Import formalized power of attorney. Failed to import signature: {0}", string.Join("\n", signatureInfo.Errors.Select(x => x.Message)));
          throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.SignatureImportFailed);
        }
      }
    }
    
    /// <summary>
    /// Заполнить свойства эл. доверенности.
    /// </summary>
    /// <param name="xml">Тело эл. доверенности.</param>
    [Public]
    public virtual void FillFormalizedPowerOfAttorney(Docflow.Structures.Module.IByteArray xml)
    {
      var version = Sungero.FormalizeDocumentsParser.Extension.GetPoAVersion(xml.Bytes);
      
      if (version == PoAVersion.V002)
      {
        this.FillFPoAV2(xml);
        return;
      }
      
      if (version == PoAVersion.V003)
      {
        this.FillFPoAV3(xml);
        return;
      }
      
      this.FillFPoADefault(xml);
    }
    
    /// <summary>
    /// Заполнить поля доверенности из десериализованного объекта.
    /// </summary>
    /// <param name="xml">Тело доверенности.</param>
    public virtual void FillFPoADefault(Docflow.Structures.Module.IByteArray xml)
    {
      System.Xml.Linq.XDocument xdoc;
      using (var memoryStream = new System.IO.MemoryStream(xml.Bytes))
        xdoc = System.Xml.Linq.XDocument.Load(memoryStream);
      
      var poaInfo = this.TryGetPoAInfoElement(xdoc);
      if (poaInfo == null)
      {
        Logger.Error("Import formalized power of attorney. Failed to parse given XML as formalized power of attorney.");
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.XmlLoadFailed);
      }
      
      this.FillFormatVersionFromXml(xdoc);
      
      this.FillUnifiedRegistrationNumberFromXml(xdoc,
                                                poaInfo,
                                                XmlFPoAInfoAttributeNames.UnifiedRegistrationNumber);
      
      this.FillValidDatesFromXml(xdoc,
                                 poaInfo,
                                 XmlFPoAInfoAttributeNames.ValidFrom,
                                 XmlFPoAInfoAttributeNames.ValidTill);
      
      // Получить регистрационные данные из xml и попытаться пронумеровать документ.
      // Если в xml нет даты регистрации, но есть номер, взять текущую дату в качестве даты регистрации.
      string number = this.GetAttributeValueByName(poaInfo, XmlFPoAInfoAttributeNames.RegistrationNumber);
      DateTime? date = this.GetDateFromXml(poaInfo, XmlFPoAInfoAttributeNames.RegistrationDate) ?? Calendar.Today;
      this.FillRegistrationData(number, date);
      this.FillIssuedToFromXml(xdoc);
      this.FillDocumentName(xdoc);
    }
    
    /// <summary>
    /// Получить XML-элемент с информацией об эл. доверенности.
    /// </summary>
    /// <param name="xdoc">XML-документ.</param>
    /// <returns>XML-элемент с информацией о доверенности.</returns>
    [Public]
    public virtual System.Xml.Linq.XElement TryGetPoAInfoElement(System.Xml.Linq.XDocument xdoc)
    {
      var poaFormat = this.GetPoAFormatVersionFromXml(xdoc);
      switch (poaFormat)
      {
        case "001":
          return xdoc.Element(XmlElementNames.PowerOfAttorney)
            ?.Element(XmlElementNames.Document)
            ?.Element(XmlElementNames.PowerOfAttorneyInfo);
        case "002":
        default:
          return xdoc.Element(XmlElementNames.PowerOfAttorney)
            ?.Element(XmlElementNames.Document)
            ?.Element(XmlElementNames.PowerOfAttorneyVersion2)
            ?.Element(XmlElementNames.PowerOfAttorneyInfo);
      }
    }
    
    /// <summary>
    /// Получить XML-элемент с информацией об эл. доверенности.
    /// </summary>
    /// <param name="xdoc">XML-документ.</param>
    /// <param name="poaElementName">Имя элемента, содержащего доверенность.</param>
    /// <param name="documentElementName">Имя элемента, содержащего документ.</param>
    /// <param name="poaInfoElementName">Имя элемента, содержащего информацию о доверенности.</param>
    /// <returns>XML-элемент с информацией о доверенности.</returns>
    [Public, Obsolete("Метод не используется с 30.08.2023 и версии 4.8. Используйте метод TryGetPoAInfoElement(XDocument).")]
    public virtual System.Xml.Linq.XElement TryGetPoAInfoElement(System.Xml.Linq.XDocument xdoc,
                                                                 string poaElementName,
                                                                 string documentElementName,
                                                                 string poaInfoElementName)
    {
      try
      {
        return xdoc.Element(poaElementName).Element(documentElementName).Element(poaInfoElementName);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Import formalized power of attorney. Failed to parse given XML as formalized power of attorney: {0}",
                           ex.Message);
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.XmlLoadFailed);
      }
    }
    
    /// <summary>
    /// Заполнить версию формата эл. доверенности.
    /// </summary>
    /// <param name="xdoc">XML-документ.</param>
    public virtual void FillFormatVersionFromXml(System.Xml.Linq.XDocument xdoc)
    {
      var fpoaFormat = this.GetPoAFormatVersionFromXml(xdoc);
      
      switch (fpoaFormat)
      {
        case "001":
          {
            _obj.FormatVersion = null;
            break;
          }
        case "002":
          {
            _obj.FormatVersion = FormatVersion.Version002;
            break;
          }
        case "EMCHD_1":
          {
            _obj.FormatVersion = FormatVersion.Version003;
            break;
          }
        default:
          {
            Logger.Error("Import formalized power of attorney. Unsupported formalized power of attorney format version.");
            throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.XmlLoadFailed);
          }
      }
    }
    
    /// <summary>
    /// Заполнить единый рег. номер эл. доверенности из xml-файла.
    /// </summary>
    /// <param name="xdoc">Тело доверенности в xml-формате.</param>
    /// <param name="powerOfAttorneyInfo">Xml-элемент с информацией об эл. доверенности.</param>
    /// <param name="poaUnifiedRegNumberAttributeName">Имя атрибута, содержащего единый рег.номер доверенности.</param>
    [Public]
    public virtual void FillUnifiedRegistrationNumberFromXml(System.Xml.Linq.XDocument xdoc,
                                                             System.Xml.Linq.XElement powerOfAttorneyInfo,
                                                             string poaUnifiedRegNumberAttributeName)
    {
      var unifiedRegNumber = this.GetAttributeValueByName(powerOfAttorneyInfo, poaUnifiedRegNumberAttributeName);
      _obj.UnifiedRegistrationNumber = GetUniformGuid(unifiedRegNumber);
    }
    
    private static string GetUniformGuid(string guidStr)
    {
      Guid guid;
      if (!Guid.TryParse(guidStr, out guid))
      {
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.XmlLoadFailed,
                                          FormalizedPowerOfAttorneys.Resources.ErrorValidateUnifiedRegistrationNumber);
      }
      
      return guid.ToString();
    }
    
    /// <summary>
    /// Заполнить дату начала и окончания действия эл. доверенности из xml-файла.
    /// </summary>
    /// <param name="xdoc">Тело доверенности в xml-формате.</param>
    /// <param name="powerOfAttorneyInfo">Xml-элемент с информацией об эл. доверенности.</param>
    /// <param name="poaValidFromAttributeName">Имя атрибута, содержащего дату начала действия доверенности.</param>
    /// <param name="poaValidTillAttributeName">Имя атрибута, содержащего дату окончания действия доверенности.</param>
    [Public]
    public virtual void FillValidDatesFromXml(System.Xml.Linq.XDocument xdoc,
                                              System.Xml.Linq.XElement powerOfAttorneyInfo,
                                              string poaValidFromAttributeName,
                                              string poaValidTillAttributeName)
    {
      DateTime? validFrom;
      DateTime? validTill;
      try
      {
        validFrom = this.GetDateFromXml(powerOfAttorneyInfo, poaValidFromAttributeName);
        validTill = this.GetDateFromXml(powerOfAttorneyInfo, poaValidTillAttributeName);
      }
      catch (Exception ex)
      {
        Logger.Error("Import formalized power of attorney. Failed to parse validity dates from xml.", ex);
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.XmlLoadFailed);
      }
      if (validFrom == null || validTill == null)
      {
        Logger.Error("Import formalized power of attorney. Failed to parse validity dates from xml.");
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.XmlLoadFailed);
      }
      _obj.ValidFrom = validFrom;
      _obj.ValidTill = validTill;
    }
    
    /// <summary>
    /// Заполнить рег. данные эл. доверенности в зависимости от настроек вида документа.
    /// </summary>
    /// <param name="number">Регистрационный номер.</param>
    /// <param name="date">Дата регистрации.</param>
    /// <remarks>Если вид документа ненумеруемый, данные не будут заполнены.</remarks>
    [Public]
    public virtual void FillRegistrationData(string number, DateTime? date)
    {
      if (string.IsNullOrEmpty(number) || !date.HasValue)
        return;
      
      // Проверить настройки RX на возможность нумерации документа.
      if (_obj.DocumentKind == null || _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable)
        return;
      
      if (_obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Numerable)
      {
        var matchingRegistersIds = Functions.OfficialDocument.GetDocumentRegistersIdsByDocument(_obj, Docflow.RegistrationSetting.SettingType.Numeration);
        if (matchingRegistersIds.Count == 1)
        {
          var register = DocumentRegisters.Get(matchingRegistersIds.First());
          Functions.OfficialDocument.RegisterDocument(_obj, register, date, number, false, false);
          return;
        }
      }
      if (_obj.AccessRights.CanRegister())
      {
        _obj.RegistrationDate = date;
        _obj.RegistrationNumber = number;
      }
      else
      {
        var registrationDataString = FormalizedPowerOfAttorneys.Resources.FormalizedPowerOfAttorneyFormat(_obj.DocumentKind.ShortName,
                                                                                                          number,
                                                                                                          date.Value.Date.ToString("d"));
        _obj.Note = registrationDataString + Environment.NewLine + _obj.Note;
      }
      return;
    }
    
    /// <summary>
    /// Заполнить поле Кому эл. доверенности из xml-файла.
    /// </summary>
    /// <param name="xdoc">Тело доверенности в xml-формате.</param>
    [Public]
    public virtual void FillIssuedToFromXml(System.Xml.Linq.XDocument xdoc)
    {
      // Не перезаполнять Кому.
      if (_obj.IssuedTo != null)
        return;
      
      // Не заполнять Кому, если тип представителя не Сотрудник.
      if (_obj.AgentType != Docflow.FormalizedPowerOfAttorney.AgentType.Employee)
        return;
      
      // Получить ИНН, СНИЛС и ФИО из xml.
      var issuedToInfoFromXml = this.GetIssuedToInfoFromXml(xdoc);
      this.FillIssuedTo(issuedToInfoFromXml);
    }
    
    /// <summary>
    /// Заполнить поле Кому эл. доверенности.
    /// </summary>
    /// <param name="info">Структура с информацией о представителе.</param>
    public virtual void FillIssuedTo(Structures.FormalizedPowerOfAttorney.IIssuedToInfo info)
    {
      _obj.IssuedTo = GetEmployee(info.TIN, info.INILA, info.FullName);
    }
    
    /// <summary>
    /// Получить сотрудника по реквизитам.
    /// </summary>
    /// <param name="tin">ИНН.</param>
    /// <param name="inila">СНИЛС.</param>
    /// <param name="fullName">Полное имя.</param>
    /// <returns>Сотрудник.</returns>
    private static Company.IEmployee GetEmployee(string tin, string inila, string fullName)
    {
      if (!string.IsNullOrWhiteSpace(tin))
      {
        var employees = Company.PublicFunctions.Employee.Remote.GetEmployeesByTIN(tin);
        if (employees.Count() == 1)
          return employees.FirstOrDefault();
      }
      
      if (!string.IsNullOrWhiteSpace(inila))
      {
        var employees = Company.PublicFunctions.Employee.Remote.GetEmployeesByINILA(inila);
        if (employees.Count() == 1)
          return employees.FirstOrDefault();
      }
      
      if (!string.IsNullOrWhiteSpace(fullName))
        return Company.PublicFunctions.Employee.Remote.GetEmployeeByName(fullName);

      return Company.Employees.Null;
    }
    
    /// <summary>
    /// Заполнить имя эл. доверенности.
    /// </summary>
    /// <param name="xdoc">Тело доверенности в xml-формате.</param>
    [Public]
    public virtual void FillDocumentName(System.Xml.Linq.XDocument xdoc)
    {
      this.SetDefaultDocumentName();
    }
    
    /// <summary>
    /// Заполнить имя эл. доверенности значением по умолчанию.
    /// </summary>
    public virtual void SetDefaultDocumentName()
    {
      // Заполнить пустое имя документа из сокращенного имени вида документа.
      if (string.IsNullOrWhiteSpace(_obj.Name) && _obj.DocumentKind != null && _obj.DocumentKind.GenerateDocumentName != true)
        _obj.Name = _obj.DocumentKind.ShortName;
    }
    
    /// <summary>
    /// Импортировать подпись.
    /// </summary>
    /// <param name="xml">Структура с подписанными данными.</param>
    /// <param name="signature">Структура с подписью.</param>
    /// <remarks>В случае если подпись без даты, которая в Sungero обязательна, будет выполнена попытка проставить подпись
    /// хоть как-нибудь. Подпись после этого будет отображаться как невалидная, но она хотя бы будет.
    /// Валидная подпись останется только в сервисе.</remarks>
    [Public]
    public virtual void ImportSignature(Docflow.Structures.Module.IByteArray xml, Docflow.Structures.Module.IByteArray signature)
    {
      var signatureBytes = signature.Bytes;

      // Получить подписавшего из сертификата.
      var certificateInfo = Docflow.PublicFunctions.Module.GetSignatureCertificateInfo(signatureBytes);
      var signatoryName = Docflow.PublicFunctions.Module.GetCertificateSignatoryName(certificateInfo.SubjectInfo);
      
      // Импортировать подпись.
      Signatures.Import(_obj, SignatureType.Approval, signatoryName, signatureBytes, _obj.LastVersion);
    }
    
    /// <summary>
    /// Проверить, что документ подписан. Если нет, сгенерировать исключение.
    /// </summary>
    [Public]
    public virtual void CheckSignature()
    {
      Sungero.Domain.Shared.ISignature importedSignature;
      importedSignature = Signatures.Get(_obj.LastVersion)
        .Where(s => s.IsExternal == true && s.SignCertificate != null)
        .OrderByDescending(x => x.Id)
        .FirstOrDefault();
      
      if (importedSignature == null)
      {
        Logger.DebugFormat("Can't find signature on document with version id: '{0}'", _obj.LastVersion.Id);
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.SignatureImportFailed);
      }
    }
    
    /// <summary>
    /// Получить дату из информации об эл. доверенности из xml-файла.
    /// </summary>
    /// <param name="element">Элемент с датой.</param>
    /// <param name="attributeName">Наименование атрибута для даты.</param>
    /// <returns>Дата.</returns>
    [Public]
    public virtual DateTime? GetDateFromXml(System.Xml.Linq.XElement element, string attributeName)
    {
      var dateValue = this.GetAttributeValueByName(element, attributeName);
      if (string.IsNullOrEmpty(dateValue))
        return null;
      
      DateTime date;
      if (Calendar.TryParseDate(dateValue, out date))
        return date;
      
      return Convert.ToDateTime(dateValue);
    }
    
    /// <summary>
    /// Получить из xml информацию об уполномоченном представителе.
    /// </summary>
    /// <param name="xdoc">Тело доверенности в xml-формате.</param>
    /// <returns>Структура с информацией.</returns>
    [Public]
    public virtual Structures.FormalizedPowerOfAttorney.IIssuedToInfo GetIssuedToInfoFromXml(System.Xml.Linq.XDocument xdoc)
    {
      var result = Structures.FormalizedPowerOfAttorney.IssuedToInfo.Create();
      
      // Получить элементы, связанные с уполномоченным представителем.
      var representativeElements = this.GetRepresentativeElements(xdoc);
      
      // Не искать по сотрудникам, если в xml нет узлов или больше одного узла с уполномоченным представителем, который является физ. лицом.
      if (representativeElements == null || !representativeElements.Any() || representativeElements.Count() > 1)
        return result;
      
      var representativeElement = representativeElements.FirstOrDefault();
      if (representativeElement == null)
        return result;
      
      // Получить ИНН, СНИЛС и ФИО уполномоченного представителя.
      var individualElement = representativeElement.Element(XmlElementNames.Individual);
      if (individualElement == null)
        return result;
      
      var tin = this.GetAttributeValueByName(individualElement, XmlIssuedToAttributeNames.TIN);
      var inila = this.GetAttributeValueByName(individualElement, XmlIssuedToAttributeNames.INILA);
      var fullName = this.GetIssuedToFullNameFromXml(individualElement);
      
      return Structures.FormalizedPowerOfAttorney.IssuedToInfo.Create(fullName, tin, inila);
    }
    
    /// <summary>
    /// Получить XML-элемент c информацией об уполномоченном представителе.
    /// </summary>
    /// <param name="xdoc">Тело доверенности в xml-формате.</param>
    /// <returns>XML-элемент с информацией об уполномоченном представителе.</returns>
    [Public]
    public virtual List<System.Xml.Linq.XElement> GetRepresentativeElements(System.Xml.Linq.XDocument xdoc)
    {
      var poaFormat = this.GetPoAFormatVersionFromXml(xdoc);
      // Получить элементы, связанные с уполномоченным представителем.
      switch (poaFormat)
      {
        case "001":
          return xdoc?.Element(XmlElementNames.PowerOfAttorney)
            ?.Element(XmlElementNames.Document)
            ?.Element(XmlElementNames.AuthorizedRepresentative)
            ?.Elements(XmlElementNames.Representative)
            ?.ToList();
        case "002":
        default:
          return xdoc?.Element(XmlElementNames.PowerOfAttorney)
            ?.Element(XmlElementNames.Document)
            ?.Element(XmlElementNames.PowerOfAttorneyVersion2)
            ?.Elements(XmlElementNames.AuthorizedRepresentative)
            ?.ToList();
      }
    }
    
    /// <summary>
    /// Получить версию формата эл. доверенности из xml-файла.
    /// </summary>
    /// <param name="xdoc">Тело доверенности в xml-формате.</param>
    /// <returns>Версия формата эл. доверенности.</returns>
    [Public]
    public virtual string GetPoAFormatVersionFromXml(System.Xml.Linq.XDocument xdoc)
    {
      var versionFormatElement = xdoc?.Element(XmlElementNames.PowerOfAttorney);
      
      try
      {
        return this.GetAttributeValueByName(versionFormatElement, Constants.FormalizedPowerOfAttorney.XmlFPoAFormatVersionAttributeName);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Import formalized power of attorney. Failed to parse given XML as formalized power of attorney: {0}",
                           ex.Message);
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.XmlLoadFailed);
      }
    }
    
    /// <summary>
    /// Получить имя того, кому выдана эл. доверенность из xml-файла.
    /// </summary>
    /// <param name="individualElement">Элемент xml с информацией о полномочном представителе.</param>
    /// <returns>ФИО.</returns>
    [Public]
    public virtual string GetIssuedToFullNameFromXml(System.Xml.Linq.XElement individualElement)
    {
      var individualNameElement = individualElement.Element(XmlIssuedToAttributeNames.IndividualName);
      if (individualNameElement == null)
        return string.Empty;
      
      // Собрать полные ФИО из фамилии, имени и отчества.
      var parts = new List<string>();
      var surname = this.GetAttributeValueByName(individualNameElement, XmlIssuedToAttributeNames.LastName);
      if (!string.IsNullOrWhiteSpace(surname))
        parts.Add(surname);
      var name = this.GetAttributeValueByName(individualNameElement, XmlIssuedToAttributeNames.FirstName);
      if (!string.IsNullOrWhiteSpace(name))
        parts.Add(name);
      var patronymic = this.GetAttributeValueByName(individualNameElement, XmlIssuedToAttributeNames.MiddleName);
      if (!string.IsNullOrWhiteSpace(patronymic))
        parts.Add(patronymic);
      
      var fullName = string.Join(" ", parts);
      return fullName;
    }
    
    /// <summary>
    /// Получить значение атрибута по имени.
    /// </summary>
    /// <param name="element">Элемент, которому принадлежит атрибут.</param>
    /// <param name="attributeName">Имя атрибута.</param>
    /// <returns>Значение или пустая строка, если атрибут не найден.</returns>
    [Public]
    public virtual string GetAttributeValueByName(System.Xml.Linq.XElement element, string attributeName)
    {
      var attribute = element?.Attribute(attributeName);
      return attribute == null ? string.Empty : attribute.Value;
    }
    
    private static string GetFullName(string lastName, string firstName, string middleName)
    {
      var parts = new string[] { lastName, firstName, middleName };
      return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
    
    #endregion
    
    #region Регистрация МЧД
    
    /// <summary>
    /// Зарегистрировать эл. доверенность в ФНС.
    /// </summary>
    /// <returns>Результат отправки: ИД операции регистрации в сервисе доверенностей или ошибка.</returns>
    [Public, Remote]
    public virtual PowerOfAttorneyCore.Structures.Module.IResponseResult RegisterFormalizedPowerOfAttorneyWithService()
    {
      return this.RegisterFormalizedPowerOfAttorneyWithService(null);
    }
    
    /// <summary>
    /// Зарегистрировать эл. доверенность в ФНС.
    /// </summary>
    /// <param name="taskId">Ид задачи, если регистрация происходит в контексте задачи.</param>
    /// <returns>Результат отправки: ИД операции регистрации в сервисе доверенностей или ошибка.</returns>
    [Public, Remote]
    public virtual PowerOfAttorneyCore.Structures.Module.IResponseResult RegisterFormalizedPowerOfAttorneyWithService(long? taskId)
    {
      // Отправка запроса на регистрацию.
      var powerOfAttorneyBytes = Functions.Module.GetBinaryData(_obj.LastVersion.Body);
      var signature = Functions.OfficialDocument.GetSignatureFromOurSignatory(_obj, _obj.LastVersion.Id);
      var signatureBytes = signature?.GetDataSignature();
      var sendingResult = PowerOfAttorneyCore.PublicFunctions.Module.SendPowerOfAttorneyForRegistration(_obj.BusinessUnit,
                                                                                                        powerOfAttorneyBytes,
                                                                                                        signatureBytes);
      if (!string.IsNullOrWhiteSpace(sendingResult.ErrorCode) || !string.IsNullOrWhiteSpace(sendingResult.ErrorType))
      {
        this.HandleRegistrationError(sendingResult.ErrorCode);
        return sendingResult;
      }

      // Успешная отправка на регистрацию.
      _obj.LifeCycleState = Docflow.FormalizedPowerOfAttorney.LifeCycleState.Draft;
      _obj.FtsListState = Docflow.FormalizedPowerOfAttorney.FtsListState.OnRegistration;
      _obj.RegisteredSignatureId = signature.Id;
      _obj.Save();
      
      var queueItem = PowerOfAttorneyQueueItems.Create();
      queueItem.OperationType = Docflow.PowerOfAttorneyQueueItem.OperationType.Registration;
      queueItem.DocumentId = _obj.Id;
      queueItem.OperationId = sendingResult.OperationId;
      queueItem.TaskId = taskId;
      queueItem.Save();
      
      sendingResult.QueueItem = queueItem;

      var documentHyperlink = Hyperlinks.Get(_obj);
      var startMessage = FormalizedPowerOfAttorneys.Resources.FormalizedPowerOfAttorneySentForRegistrationSuccessfully;
      var completeMessage = FormalizedPowerOfAttorneys.Resources.FormalizedPowerOfAttorneyRegistrationCompletedFormat(documentHyperlink, Environment.NewLine);
      var errorMessage = FormalizedPowerOfAttorneys.Resources.FormalizedPowerOfAttorneyRegistrationErrorFormat(documentHyperlink, Environment.NewLine);

      // Запуск АО мониторинга регистрации доверенности в сервисе доверенностей.
      var getFPoAStateHandler = AsyncHandlers.SetFPoARegistrationState.Create();
      getFPoAStateHandler.QueueItemId = queueItem.Id;
      getFPoAStateHandler.ExecuteAsync(startMessage, completeMessage, errorMessage, Users.Current);
      return sendingResult;
    }
    
    /// <summary>
    /// Заполнить Состояние, статус В реестре ФНС и сообщение об ошибке по коду ошибки.
    /// </summary>
    /// <param name="errorCode">Код ошибки.</param>
    [Public]
    public virtual void HandleRegistrationError(string errorCode)
    {
      _obj.LifeCycleState = Sungero.Docflow.FormalizedPowerOfAttorney.LifeCycleState.Draft;
      _obj.FtsListState = Sungero.Docflow.FormalizedPowerOfAttorney.FtsListState.Rejected;
      _obj.RegisteredSignatureId = null;
      
      var reasonsDictionary = this.GetErrorCodeAndReasonMapping();
      _obj.FtsRejectReason = reasonsDictionary
        .Where(x => string.Equals(x.Key, errorCode, StringComparison.InvariantCultureIgnoreCase))
        .Select(x => x.Value)
        .FirstOrDefault();
      if (string.IsNullOrEmpty(_obj.FtsRejectReason))
        _obj.FtsRejectReason = Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.DefaultErrorMessage;
      
      _obj.Save();
      
      Logger.Error($"HandleRegistrationError. Power of attorney registration error = {errorCode} (PoA id = {_obj.Id}).");
    }
    
    public virtual System.Collections.Generic.Dictionary<string, string> GetErrorCodeAndReasonMapping()
    {
      var result = new Dictionary<string, string>();
      
      result.Add(Constants.FormalizedPowerOfAttorney.FPoARegistrationErrors.ExternalSystemIsUnavailableError,
                 FormalizedPowerOfAttorneys.Resources.ExternalSystemIsUnavailableErrorMessage);
      result.Add(Constants.FormalizedPowerOfAttorney.FPoARegistrationErrors.InvalidCertificateError,
                 FormalizedPowerOfAttorneys.Resources.DifferentSignatureErrorMessage);
      result.Add(Constants.FormalizedPowerOfAttorney.FPoARegistrationErrors.DifferentSignatureError,
                 FormalizedPowerOfAttorneys.Resources.DifferentSignatureErrorMessage);
      result.Add(Constants.FormalizedPowerOfAttorney.FPoARegistrationErrors.UnsupportedPoA,
                 FormalizedPowerOfAttorneys.Resources.UnsupportedPoAErrorMessage);
      
      return result;
    }

    #endregion

    #region Поиск дублей

    /// <summary>
    /// Получить дубли эл. доверенности.
    /// </summary>
    /// <returns>Дубли эл. доверенности.</returns>
    [Public, Remote(IsPure = true)]
    public virtual List<IFormalizedPowerOfAttorney> GetFormalizedPowerOfAttorneyDuplicates()
    {
      var duplicates = new List<IFormalizedPowerOfAttorney>();
      if (string.IsNullOrEmpty(_obj.UnifiedRegistrationNumber))
      {
        return duplicates;
      }

      AccessRights.AllowRead(
        () =>
        {
          duplicates = FormalizedPowerOfAttorneys
            .GetAll()
            .Where(f => !Equals(f, _obj) &&
                   f.UnifiedRegistrationNumber == _obj.UnifiedRegistrationNumber)
            .ToList();
        });
      return duplicates;
    }
    
    #endregion
    
    #region Печатная форма
    
    /// <summary>
    /// Сгенерировать PDF из тела доверенности.
    /// </summary>
    [Public, Remote(IsPure = true), Obsolete("Метод не используется с 31.08.2023 и версии 4.8. Используйте метод ConvertToPdfWithSignatureMark.")]
    public virtual void GenerateFormalizedPowerOfAttorneyPdf()
    {
      this.ConvertToPdfAndAddSignatureMark(_obj.LastVersion.Id);
      
      PublicFunctions.Module.LogPdfConverting("Signature mark. Added interactively", _obj, _obj.LastVersion);
    }
    
    /// <summary>
    /// Преобразовать документ в PDF с наложением отметки об ЭП.
    /// </summary>
    /// <returns>Результат преобразования.</returns>
    /// <remarks>Перед преобразованием валидируются документ и подпись на версии.</remarks>
    [Remote]
    public override Structures.OfficialDocument.IConversionToPdfResult ConvertToPdfWithSignatureMark()
    {
      var versionId = _obj.LastVersion.Id;
      var result = this.ValidateDocumentBeforeConversion(versionId);
      if (result.HasErrors)
        return result;
      
      result = this.ConvertToPdfAndAddSignatureMark(versionId);
      return result;
    }
    
    /// <summary>
    /// Преобразовать документ в PDF и поставить отметку об ЭП, если есть утверждающая подпись.
    /// </summary>
    /// <param name="versionId">ИД версии документа.</param>
    /// <returns>Результат преобразования в PDF.</returns>
    [Remote]
    public override Structures.OfficialDocument.IConversionToPdfResult ConvertToPdfAndAddSignatureMark(long versionId)
    {
      var signatureMark = string.Empty;
      
      var signature = Functions.OfficialDocument.GetSignatureFromOurSignatory(_obj, versionId);
      if (signature != null)
        signatureMark = Functions.Module.GetSignatureMarkAsHtml(_obj, signature);
      
      return this.GeneratePublicBodyWithSignatureMark(versionId, signatureMark);
    }
    
    /// <summary>
    /// Получить электронную подпись для простановки отметки.
    /// </summary>
    /// <param name="versionId">Номер версии.</param>
    /// <returns>Электронная подпись.</returns>
    [Public]
    public override Sungero.Domain.Shared.ISignature GetSignatureForMark(long versionId)
    {
      return this.GetSignatureFromOurSignatory(versionId);
    }
    
    /// <summary>
    /// Получить тело и расширение версии для преобразования в PDF с отметкой об ЭП.
    /// </summary>
    /// <param name="version">Версия для генерации.</param>
    /// <param name="isSignatureMark">Признак отметки об ЭП. True - отметка об ЭП, False - отметка о поступлении.</param>
    /// <returns>Тело версии документа и расширение.</returns>
    /// <remarks>Для преобразования в PDF эл. доверенности необходимо сначала получить ее в виде html.</remarks>
    [Public]
    public override Structures.OfficialDocument.IVersionBody GetBodyToConvertToPdf(Sungero.Content.IElectronicDocumentVersions version, bool isSignatureMark)
    {
      return this.GetBodyToConvertToPdfWithMarks(version);
    }
    
    /// <summary>
    /// Получить тело и расширение версии для преобразования в PDF с отметками.
    /// </summary>
    /// <param name="version">Версия для преобразования.</param>
    /// <returns>Тело версии документа и расширение.</returns>
    /// <remarks>Для преобразования в PDF эл. доверенности необходимо сначала получить ее в виде html.</remarks>
    public override Sungero.Docflow.Structures.OfficialDocument.IVersionBody GetBodyToConvertToPdfWithMarks(Sungero.Content.IElectronicDocumentVersions version)
    {
      var result = Structures.OfficialDocument.VersionBody.Create();
      if (version == null)
        return result;
      
      var html = this.GetFormalizedPowerOfAttorneyAsHtml(version);
      if (string.IsNullOrWhiteSpace(html))
        return result;

      result.Body = System.Text.Encoding.UTF8.GetBytes(html);
      result.Extension = Constants.FormalizedPowerOfAttorney.HtmlExtension;
      return result;
    }
    
    /// <summary>
    /// Получить эл. доверенность в виде html.
    /// </summary>
    /// <param name="version">Версия, на основании которой формируется html.</param>
    /// <returns>Эл. доверенность в виде html.</returns>
    [Public]
    public virtual string GetFormalizedPowerOfAttorneyAsHtml(Sungero.Content.IElectronicDocumentVersions version)
    {
      if (version == null)
        return string.Empty;
      
      // Получить модель эл. доверенности из xml.
      using (var body = new System.IO.MemoryStream())
      {
        // Выключить error-логирование при доступе к зашифрованным бинарным данным.
        AccessRights.SuppressSecurityEvents(() => version.Body.Read().CopyTo(body));
        return FormalizeDocumentsParser.Extension.ProducePoAHtml(body.ToArray(), this.GetNameMapping());
      }
    }
    
    /// <summary>
    /// Получить класс с заполненным словарем кодов документов и его сокращенного названия.
    /// </summary>
    /// <returns>Класс с заполненным словарем: Key - код документа, Value - сокращенное имя документа.</returns>
    private Sungero.FormalizeDocumentsParser.PowerOfAttorney.NameMapping GetNameMapping()
    {
      var nameMapping = new Sungero.FormalizeDocumentsParser.PowerOfAttorney.NameMapping();
      nameMapping.IdentityDocuments = new Dictionary<string, string>();
      var kinds = IdentityDocumentKinds.GetAll(idk => idk.Status == CoreEntities.DatabookEntry.Status.Active);
      
      foreach (var kind in kinds)
      {
        if (!nameMapping.IdentityDocuments.ContainsKey(kind.Code))
          nameMapping.IdentityDocuments.Add(kind.Code, kind.ShortName.ToLower());
      }
      
      return nameMapping;
    }
    
    /// <summary>
    /// Определить, поддерживается ли преобразование в PDF для переданного расширения.
    /// </summary>
    /// <param name="extension">Расширение.</param>
    /// <returns>True, если поддерживается, иначе False.</returns>
    /// <remarks>МЧД имеют расширение XML, которое всегда поддерживается.</remarks>
    [Public]
    public override bool CheckPdfConvertibilityByExtension(string extension)
    {
      return true;
    }

    #endregion
    
    #region Проверка состояния в сервисе
    
    /// <summary>
    /// Синхронизировать статус эл. доверенности в реестре ФНС.
    /// </summary>
    [Public, Remote]
    public virtual void SyncFormalizedPowerOfAttorneyFtsListState()
    {
      var batchGuid = Guid.NewGuid().ToString();
      Functions.Module.CreateFormalizedPoAQueueItemBatch(new List<long>() { _obj.Id }, batchGuid);
      
      var documentHyperlink = Hyperlinks.Get(_obj);
      var completeMessage = FormalizedPowerOfAttorneys.Resources.SyncFtsListStateSuccessNotificationFormat(documentHyperlink, Environment.NewLine);
      Functions.Module.ExecuteSyncFormalizedPoAWithService(batchGuid, _obj.BusinessUnit.Id, completeMessage);
      Logger.DebugFormat("ExecuteSyncFormalizedPoAWithServiceAsyncHandler. FormalizedPoABatchGuid: '{0}'.", batchGuid);
    }
    
    /// <summary>
    /// Проверить состояние эл. доверенности в сервисе доверенностей и установить актуальный статус.
    /// </summary>
    /// <returns>Результат проверки состояния в виде локализованной строки.</returns>
    [Public, Remote]
    public virtual string SyncFormalizedPowerOfAttorneyState()
    {
      var synchronizationResult = this.TrySyncFormalizedPowerOfAttorneyState();
      
      if (synchronizationResult.PoaNotFound)
      {
        this.SyncFormalizedPowerOfAttorneyFtsListState();
      }
      
      return synchronizationResult.ResultMessage;
    }
    
    /// <summary>
    /// Попытаться проверить состояние эл. доверенности в сервисе доверенностей и установить актуальный статус.
    /// </summary>
    /// <returns>Результат проверки состояния.</returns>
    [Public]
    public virtual Sungero.Docflow.Structures.FormalizedPowerOfAttorney.IFPoAStateSynchronizationResult TrySyncFormalizedPowerOfAttorneyState()
    {
      var result = Sungero.Docflow.Structures.FormalizedPowerOfAttorney.FPoAStateSynchronizationResult.Create();
      if (this.RegistrationInProcess())
      {
        result.ResultMessage = FormalizedPowerOfAttorneys.Resources.FPoARegistrationInProcess;
        return result;
      }
      
      if (this.RevocationInProcess())
      {
        result.ResultMessage = FormalizedPowerOfAttorneys.Resources.FPoARevocationInProcess;
        return result;
      }
      
      var synchronizationInfo = PowerOfAttorneyCore.PublicFunctions.Module.GetPowerOfAttorneyState(_obj.BusinessUnit, _obj.UnifiedRegistrationNumber);
      
      if (string.Equals(synchronizationInfo.PoAStatus, PoAServiceStateStatus.NotFound, StringComparison.InvariantCultureIgnoreCase))
      {
        result.PoaNotFound = true;
        result.ResultMessage = Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.FPoAStateSyncWaitOrContactAdmin;
        return result;
      }
      
      try
      {
        this.ProcessStateResult(synchronizationInfo);
        result.ResultMessage = FormalizedPowerOfAttorneys.Resources.FPoAStateHasBeenUpdated;
        result.IsSuccess = true;
      }
      catch (AppliedCodeException ex)
      {
        result.ResultMessage = ex.LocalizedMessage;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("TrySyncFormalizedPowerOfAttorneyState. FPoA state synchronization failed: {0}", ex.Message);
        result.ResultMessage = Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.FPoAStateCheckFailed;
      }
      
      return result;
    }
    
    /// <summary>
    /// Проверить состояние эл. доверенности в ФНС и установить актуальный статус.
    /// </summary>
    /// <returns>Результат проверки.</returns>
    [Public, Remote, Obsolete("Метод больше не используется с 25.07.2024 и версии 4.6, для проверки статуса МЧД используйте метод SyncFormalizedPowerOfAttorneyState")]
    public virtual string CheckFormalizedPowerOfAttorneyState()
    {
      // Если доверенность в процессе регистрации или отзыва - не выполнять запрос в сервис.
      var registrationInProcess = this.RegistrationInProcess();
      if (registrationInProcess)
        return FormalizedPowerOfAttorneys.Resources.FPoARegistrationInProcess;
      
      var revocationInProcess = this.RevocationInProcess();
      if (revocationInProcess)
        return FormalizedPowerOfAttorneys.Resources.FPoARevocationInProcess;
      
      var validationState = this.GetValidationStateFromService();
      try
      {
        this.ProcessValidationResult(validationState);
        return FormalizedPowerOfAttorneys.Resources.FPoAStateHasBeenUpdated;
      }
      catch (AppliedCodeException ex)
      {
        return ex.LocalizedMessage;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("CheckFormalizedPowerOfAttorneyState. FPoA validation failed: {0}", ex.Message);
        return Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.FPoAStateCheckFailed;
      }
    }
    
    /// <summary>
    /// Получить результат валидации эл. доверенности в сервисе доверенностей.
    /// </summary>
    /// <returns>Результат валидации эл. доверенности в сервисе.</returns>
    [Obsolete("Метод больше не используется с 31.07.2024 и версии 4.6, для получения информации о статусе доверенности используйте")]
    public virtual PowerOfAttorneyCore.Structures.Module.IPowerOfAttorneyValidationState GetValidationStateFromService()
    {
      var agent = this.CreateAgent();
      var principal = this.CreatePrincipal();
      var powerOfAttorneyXml = Docflow.PublicFunctions.Module.GetBinaryData(_obj.LastVersion.Body);
      var signature = Docflow.PublicFunctions.FormalizedPowerOfAttorney.GetRegisteredSignature(_obj);
      var signatureBytes = signature?.GetDataSignature();
      var validationState = PowerOfAttorneyCore.PublicFunctions.Module.CheckPowerOfAttorneyState(_obj.BusinessUnit, _obj.UnifiedRegistrationNumber,
                                                                                                 principal, agent, powerOfAttorneyXml, signatureBytes);
      return validationState;
    }
    
    /// <summary>
    /// Проверить отозвана ли доверенность в сервисе.
    /// </summary>
    /// <returns>True - доверенность отозвана.</returns>
    public bool IsRevokedInService()
    {
      var state = PowerOfAttorneyCore.PublicFunctions.Module.GetPowerOfAttorneyState(_obj.BusinessUnit, _obj.UnifiedRegistrationNumber);
      return string.Equals(state.PoAStatus, PoAServiceStateStatus.Revoked, StringComparison.InvariantCultureIgnoreCase);
    }
    
    /// <summary>
    /// Обработать результат валидации эл. доверенности в сервисе доверенностей.
    /// </summary>
    /// <param name="validationState">Результат валидации эл. доверенности в сервисе.</param>
    [Obsolete("Метод больше не используется с 31.07.2024 и версии 4.6, для проверки статуса МЧД используйте метод SyncFormalizedPowerOfAttorneyState")]
    public virtual void ProcessValidationResult(PowerOfAttorneyCore.Structures.Module.IPowerOfAttorneyValidationState validationState)
    {
      if (validationState.Errors.Any(x => string.Equals(x.Type, PoAServiceErrors.ConnectionError, StringComparison.InvariantCultureIgnoreCase)))
        throw AppliedCodeException.Create(PowerOfAttorneyCore.Resources.PowerOfAttorneyNoConnection);
      
      if (validationState.Errors.Any(x => string.Equals(x.Type, PowerOfAttorneyCore.PublicConstants.Module.EmptyRequestParametersError, StringComparison.InvariantCultureIgnoreCase)))
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.FPoASetRevokedStateFailed);
      
      if (string.IsNullOrEmpty(validationState.Result))
        throw AppliedCodeException.Create(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.FPoAStateCheckFailed);
      
      if (validationState.Result == Constants.FormalizedPowerOfAttorney.FPoAState.Valid)
      {
        // Ответ не получен за таймаут или сразу вернулся ответ, что данные не актуальны.
        // При переповторе данные могут успеть актуализироваться.
        if (this.PowerOfAttorneyValidationStateHasErrorWithCode(validationState.Errors, PoAServiceErrors.StateIsOutdated))
          throw AppliedCodeException.Create(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.FPoAStateCheckFailed);
        
        // Эл. доверенность не найдена.
        if (this.PowerOfAttorneyValidationStateHasErrorWithCode(validationState.Errors, PoAServiceErrors.PoANotFound))
          throw AppliedCodeException.Create(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.FPoANotFound);
        
        // Эл. доверенность валидна.
        Functions.FormalizedPowerOfAttorney.SetLifeCycleAndFtsListStates(_obj, LifeCycleState.Active, FtsListState.Registered);
        _obj.Save();
      }
      else
      {
        // Эл. доверенность не найдена.
        if (this.PowerOfAttorneyValidationStateHasErrorWithCode(validationState.Errors, PoAServiceErrors.PoANotFound))
          throw AppliedCodeException.Create(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.FPoANotFound);
        
        // Эл. доверенность отозвана.
        if (this.PowerOfAttorneyValidationStateHasErrorWithCode(validationState.Errors, Constants.FormalizedPowerOfAttorney.FPoAState.Revoked))
        {
          // Запросить причину и дату подписания отзыва.
          var isRevokedStateHasBeenSet = PublicFunctions.FormalizedPowerOfAttorney.SetRevokedState(_obj);
          if (!isRevokedStateHasBeenSet)
            throw AppliedCodeException.Create(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.FPoASetRevokedStateFailed);
          
          PublicFunctions.FormalizedPowerOfAttorney.Remote.CreateSetSignatureSettingsValidTillAsyncHandler(_obj, _obj.ValidTill.Value);
          return;
        }
        
        // Срок действия эл. доверенности истек.
        if (this.PowerOfAttorneyValidationStateHasErrorWithCode(validationState.Errors, Constants.FormalizedPowerOfAttorney.FPoAState.Expired))
        {
          Functions.FormalizedPowerOfAttorney.SetLifeCycleAndFtsListStates(_obj, LifeCycleState.Obsolete, FtsListState.Registered);
          _obj.Save();
          return;
        }
        
        // Срок действия эл. доверенности ещё не наступил.
        if (this.PowerOfAttorneyValidationStateHasErrorWithCode(validationState.Errors, Constants.FormalizedPowerOfAttorney.FPoAState.NotValidYet))
        {
          Functions.FormalizedPowerOfAttorney.SetLifeCycleAndFtsListStates(_obj, LifeCycleState.Active, FtsListState.Registered);
          _obj.Save();
          return;
        }
        
        throw AppliedCodeException.Create(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.FPoAAttributesNotMatchXml);
      }
    }
    
    /// <summary>
    /// Обработать информацию о состоянии эл. доверенности в сервисе доверенностей.
    /// </summary>
    /// <param name="stateResult">Результат проверки состояния эл. доверенности в сервисе.</param>
    public virtual void ProcessStateResult(PowerOfAttorneyCore.Structures.Module.IPowerOfAttorneyState stateResult)
    {
      if (stateResult.ErrorCodes.Any())
        this.HandleStateResultErrors(stateResult.ErrorCodes);
      
      // Эл. доверенность зарегистрирована в ФНС.
      if (string.Equals(stateResult.PoADeliveryStatus, PoAServiceDeliveryStatus.Delivered, StringComparison.InvariantCultureIgnoreCase) ||
          string.Equals(stateResult.PoADeliveryStatus, PoAServiceDeliveryStatus.Source, StringComparison.InvariantCultureIgnoreCase))
      {
        this.HandleStateResultForRegisteredPowerOfAttorney(stateResult);
        return;
      }
      
      // Эл. доверенность в процессе регистрации в ФНС.
      if (string.Equals(stateResult.PoADeliveryStatus, PoAServiceDeliveryStatus.Queued, StringComparison.InvariantCultureIgnoreCase))
      {
        // Не обрабатываем этот кейс.
        return;
      }
      
      // Ошибка регистрации эл. доверенности в ФНС.
      if (string.Equals(stateResult.PoADeliveryStatus, PoAServiceDeliveryStatus.Error, StringComparison.InvariantCultureIgnoreCase))
      {
        Functions.FormalizedPowerOfAttorney.SetLifeCycleAndFtsListStates(_obj, LifeCycleState.Draft, FtsListState.Rejected);
        _obj.Save();
        return;
      }
    }
    
    /// <summary>
    /// Обработать ошибки, полученные в процессе получении данных о состояния эл. доверенности от сервиса.
    /// </summary>
    /// <param name="errorCodes">Список кодов ошибок.</param>
    protected void HandleStateResultErrors(List<string> errorCodes)
    {
      if (errorCodes.Any(x => string.Equals(x, PoAServiceErrors.ConnectionError, StringComparison.InvariantCultureIgnoreCase)))
        throw AppliedCodeException.Create(PowerOfAttorneyCore.Resources.PowerOfAttorneyNoConnection);

      if (errorCodes.Any(x => string.Equals(x, PowerOfAttorneyCore.PublicConstants.Module.EmptyRequestParametersError, StringComparison.InvariantCultureIgnoreCase)))
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.FPoASetRevokedStateFailed);
      
      if (errorCodes.Any(x => string.Equals(x, PoAServiceErrors.ProcessingError, StringComparison.InvariantCultureIgnoreCase)))
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.FPoASetRevokedStateFailed);
      
      throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.FPoASetRevokedStateFailed);
    }
    
    /// <summary>
    /// Обработать информацию о состоянии зарегистрированной эл. доверенности.
    /// </summary>
    /// <param name="stateResult">Результат проверки состояния эл. доверенности в сервисе.</param>
    protected void HandleStateResultForRegisteredPowerOfAttorney(PowerOfAttorneyCore.Structures.Module.IPowerOfAttorneyState stateResult)
    {
      // Действующая эл. доверенность.
      if (string.Equals(stateResult.PoAStatus, PoAServiceStateStatus.Active, StringComparison.InvariantCultureIgnoreCase) ||
          string.Equals(stateResult.PoAStatus, PoAServiceStateStatus.Created, StringComparison.InvariantCultureIgnoreCase))
      {
        Functions.FormalizedPowerOfAttorney.SetLifeCycleAndFtsListStates(_obj, LifeCycleState.Active, FtsListState.Registered);
        _obj.Save();
        return;
      }
      
      // Срок действия эл. доверенности истёк.
      if (string.Equals(stateResult.PoAStatus, PoAServiceStateStatus.Expired, StringComparison.InvariantCultureIgnoreCase))
      {
        Functions.FormalizedPowerOfAttorney.SetLifeCycleAndFtsListStates(_obj, LifeCycleState.Obsolete, FtsListState.Registered);
        _obj.Save();
        return;
      }
      
      // Эл. доверенность отозвана.
      if (string.Equals(stateResult.PoAStatus, PoAServiceStateStatus.Revoked, StringComparison.InvariantCultureIgnoreCase))
      {
        try 
        {
          var isRevokedStateHasBeenSet = PublicFunctions.FormalizedPowerOfAttorney.SetRevokedState(_obj, stateResult.RevocationReason, stateResult.RevocationDate ?? Calendar.UserToday);
          if (!isRevokedStateHasBeenSet)
            throw AppliedCodeException.Create(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.FPoASetRevokedStateFailed);
          
          PublicFunctions.FormalizedPowerOfAttorney.Remote.CreateSetSignatureSettingsValidTillAsyncHandler(_obj, _obj.ValidTill.Value);
          return;
        }
        catch (Exception)
        {
          throw AppliedCodeException.Create(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.FPoASetRevokedStateFailed);
        }
      }
    }
    
    /// <summary>
    /// Проверить, находится ли эл. доверенность в процессе регистрации.
    /// </summary>
    /// <returns>True - если в процессе регистрации, иначе - false.</returns>
    public virtual bool RegistrationInProcess()
    {
      if (_obj.FtsListState == Docflow.FormalizedPowerOfAttorney.FtsListState.OnRegistration)
      {
        var registrationQueueItem = PowerOfAttorneyQueueItems.GetAll()
          .Where(q => q.DocumentId == _obj.Id && q.OperationType == Docflow.PowerOfAttorneyQueueItem.OperationType.Registration)
          .FirstOrDefault();
        
        return registrationQueueItem != null;
      }
      
      return false;
    }
    
    /// <summary>
    /// Проверить, находится ли эл. доверенность в процессе отзыва.
    /// </summary>
    /// <returns>True - если в процессе отзыва, иначе - false.</returns>
    public virtual bool RevocationInProcess()
    {
      var revocation = PowerOfAttorneyRevocations.GetAll().Where(r => Equals(r.FormalizedPowerOfAttorney, _obj)).FirstOrDefault();
      if (revocation != null)
      {
        var revocationQueueItem = PowerOfAttorneyQueueItems.GetAll()
          .Where(q => q.DocumentId == revocation.Id && q.OperationType == Docflow.PowerOfAttorneyQueueItem.OperationType.Revocation)
          .FirstOrDefault();
        
        return revocationQueueItem != null;
      }
      
      return false;
    }
    
    /// <summary>
    /// Создать асинхронное событие установки "Действует по" во всех правах подписи,
    /// где в качестве документа-основания указана эл. доверенность.
    /// </summary>
    /// <param name="validTill">Дата, по которую действует эл. доверенность.</param>
    /// <remarks>Выполняется асинхронно.</remarks>
    [Public, Remote]
    public virtual void CreateSetSignatureSettingsValidTillAsyncHandler(DateTime validTill)
    {
      var signatureSettingIds = Docflow.SignatureSettings.GetAll().Where(x => Equals(x.Document, _obj)).Select(x => x.Id);
      foreach (var settingId in signatureSettingIds)
      {
        var asyncSetSignatureSettingValidTillHandler = Docflow.AsyncHandlers.SetSignatureSettingValidTill.Create();
        asyncSetSignatureSettingValidTillHandler.SignatureSettingId = settingId;
        asyncSetSignatureSettingValidTillHandler.ValidTill = validTill;
        asyncSetSignatureSettingValidTillHandler.ExecuteAsync();
      }
    }
    
    /// <summary>
    /// Зарегистрировать операцию валидации доверенности на сервисе.
    /// </summary>
    /// <param name="serviceConnection">Подключение к сервису доверенностей.</param>
    /// <param name="queueItem">Элемент очереди синхронизации эл. доверенностей.</param>
    /// <returns>True - если нужно продолжить дальнейшую обработку элемента очереди.</returns>
    [Public, Obsolete("Метод больше не используется с 31.07.2024 и версии 4.6, вместо создания операции валидации используйте метод создани операции импорта EnqueueImportOperation")]
    public virtual bool EnqueueValidation(Sungero.PowerOfAttorneyCore.IPowerOfAttorneyServiceConnection serviceConnection, IPowerOfAttorneyQueueItem queueItem)
    {
      queueItem = PowerOfAttorneyQueueItems.GetAll().FirstOrDefault(x => Equals(x, queueItem));
      if (queueItem == null)
      {
        Logger.DebugFormat("EnqueueValidation: Queue item is null. Formalized power of attorney id {0}.", _obj.Id);
        return false;
      }
      
      if (!_obj.HasVersions)
      {
        Logger.DebugFormat("EnqueueValidation: Formalized power of attorney with id {0} has no version.", _obj.Id);
        return false;
      }
      
      var contentBytes = Docflow.PublicFunctions.Module.GetBinaryData(_obj.LastVersion.Body);
      
      if (contentBytes == null || contentBytes.Length == 0)
      {
        Logger.DebugFormat("EnqueueValidation: Document body is empty. Formalized power of attorney id {0}.", _obj.Id);
        return false;
      }

      var signature = Docflow.PublicFunctions.FormalizedPowerOfAttorney.GetRegisteredSignature(_obj);
      var signatureBytes = signature?.GetDataSignature();
      
      if (signatureBytes == null)
      {
        Logger.DebugFormat("EnqueueValidation: Signature is empty. Formalized power of attorney id {0}.", _obj.Id);
        return false;
      }
      
      var agent = this.CreateAgent();
      var principal = this.CreatePrincipal();
      var sendingResult = PowerOfAttorneyCore.PublicFunctions.Module.EnqueuePoAValidation(serviceConnection, _obj.BusinessUnit, _obj.UnifiedRegistrationNumber, principal, agent, contentBytes, signatureBytes);
      
      // Проверить, что queueItem ещё существует.
      queueItem = PowerOfAttorneyQueueItems.GetAll().FirstOrDefault(x => Equals(x, queueItem));
      if (queueItem == null)
      {
        Logger.DebugFormat("EnqueueValidation: Queue item is null. Formalized power of attorney id {0}.", _obj.Id);
        return false;
      }
      
      if (!string.IsNullOrEmpty(sendingResult.OperationId))
      {
        Logger.DebugFormat("EnqueueValidation: Operation id {0} successfully received. Formalized power of attorney id {1}.", sendingResult.OperationId, _obj.Id);
        queueItem.OperationId = sendingResult.OperationId;
        queueItem.Save();
      }
      else
      {
        Logger.DebugFormat("EnqueueValidation: Operation id is empty. Formalized power of attorney id {0}.", _obj.Id);
      }
      
      return true;
    }
    
    /// <summary>
    /// Зарегистрировать операцию импорта эл. доверенности из ФНС в Контур.
    /// </summary>
    /// <param name="serviceConnection">Подключение к сервису доверенностей.</param>
    /// <param name="queueItem">Элемент очереди синхронизации эл. доверенностей.</param>
    /// <returns>True - если нужно продолжить дальнейшую обработку элемента очереди.</returns>
    [Public]
    public virtual bool EnqueueImportOperation(Sungero.PowerOfAttorneyCore.IPowerOfAttorneyServiceConnection serviceConnection, IPowerOfAttorneyQueueItem queueItem)
    {
      queueItem = PowerOfAttorneyQueueItems.GetAll().FirstOrDefault(x => Equals(x, queueItem));
      if (queueItem == null)
      {
        Logger.DebugFormat("EnqueueImportOperation: Queue item is null. Formalized power of attorney id {0}.", _obj.Id);
        return false;
      }
      
      var principalTin = _obj.BusinessUnit.TIN;
      var representativeTin = this.GetRepresentativeTin();
      
      if (string.IsNullOrEmpty(principalTin) || string.IsNullOrEmpty(representativeTin) || string.IsNullOrEmpty(_obj.UnifiedRegistrationNumber))
      {
        Logger.DebugFormat("EnqueueImportOperation: Not enough data to make a request. Formalized power of attorney id {0}.", _obj.Id);
        return false;
      }
      
      var sendingResult = PowerOfAttorneyCore.PublicFunctions.Module.EnqueuePoAImportOperation(serviceConnection, _obj.UnifiedRegistrationNumber, principalTin, representativeTin);
      
      // Проверить, что queueItem ещё существует.
      queueItem = PowerOfAttorneyQueueItems.GetAll().FirstOrDefault(x => Equals(x, queueItem));
      if (queueItem == null)
      {
        Logger.DebugFormat("EnqueueImportOperation: Queue item is null. Formalized power of attorney id {0}.", _obj.Id);
        return false;
      }
      
      if (!string.IsNullOrEmpty(sendingResult.OperationId))
      {
        Logger.DebugFormat("EnqueueImportOperation: Operation id {0} successfully received. Formalized power of attorney id {1}.", sendingResult.OperationId, _obj.Id);
        queueItem.OperationId = sendingResult.OperationId;
        queueItem.Save();
      }
      else
      {
        Logger.DebugFormat("EnqueueImportOperation: Operation id is empty. Formalized power of attorney id {0}.", _obj.Id);
      }
      
      return true;
    }
    
    /// <summary>
    /// Получить ИНН первого представителя в списке представителей.
    /// </summary>
    /// <returns>ИНН представителя.</returns>
    protected string GetRepresentativeTin()
    {
      var representative = _obj.Representatives.FirstOrDefault();
      if (representative == null)
        return string.Empty;
      
      return representative.IssuedTo?.TIN ?? representative.Agent?.TIN;
    }
    
    /// <summary>
    /// Получить подпись, с которой была зарегистрирована эл. доверенность в реестре ФНС.
    /// </summary>
    /// <returns>Подпись, с которой была зарегистрирована эл. доверенность в реестре ФНС.</returns>
    /// <remarks>Если не заполнено свойство RegisteredSignatureId, то возвращается последняя подпись.</remarks>
    [Public]
    public virtual Sungero.Domain.Shared.ISignature GetRegisteredSignature()
    {
      if (_obj.RegisteredSignatureId.HasValue)
        return Signatures.Get(_obj).FirstOrDefault(x => x.Id == _obj.RegisteredSignatureId.Value);
      return Docflow.PublicFunctions.OfficialDocument.GetSignatureFromOurSignatory(_obj, _obj.LastVersion.Id);
    }
    
    /// <summary>
    /// Сформировать представителя в зависимости от типа.
    /// </summary>
    /// <returns>Представитель.</returns>
    public virtual PowerOfAttorneyCore.Structures.Module.IAgent CreateAgent()
    {
      var agent = PowerOfAttorneyCore.Structures.Module.Agent.Create();
      var representative = People.Null;
      
      if ((_obj.AgentType == Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Person ||
           _obj.AgentType == Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Employee) &&
          _obj.IssuedToParty != null)
      {
        representative = People.As(_obj.IssuedToParty);
        agent.Name = representative?.FirstName;
        agent.Middlename = representative?.MiddleName;
        agent.Surname = representative?.LastName;
        agent.TIN = representative?.TIN;
        agent.INILA = representative?.INILA;
      }
      else if (_obj.AgentType == Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Entrepreneur &&
               _obj.Representative != null && _obj.IssuedToParty != null)
      {
        representative = People.As(_obj.Representative);
        agent.Name = representative?.FirstName;
        agent.Middlename = representative?.MiddleName;
        agent.Surname = representative?.LastName;
        agent.TIN = representative?.TIN;
        agent.INILA = representative?.INILA;
      }
      else if (_obj.AgentType == Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.LegalEntity &&
               _obj.IssuedToParty != null)
      {
        if (_obj.Representative != null && _obj.FormatVersion == FormatVersion.Version002)
        {
          representative = People.As(_obj.Representative);
          agent.Name = representative?.FirstName;
          agent.Middlename = representative?.MiddleName;
          agent.Surname = representative?.LastName;
          agent.INILA = representative?.INILA;
        }
        var legalEntity = CompanyBases.As(_obj.IssuedToParty);
        agent.TINUl = legalEntity?.TIN;
        agent.TRRC = legalEntity?.TRRC;
      }
      else
      {
        Logger.ErrorFormat("CreateAgent. Power of attorney validation error: AgentType is incorrect.");
        agent = null;
      }
      
      return agent;
    }

    /// <summary>
    /// Сформировать доверителя в зависимости от типа.
    /// </summary>
    /// <returns>Доверитель.</returns>
    public virtual PowerOfAttorneyCore.Structures.Module.IPrincipal CreatePrincipal()
    {
      var principal = PowerOfAttorneyCore.Structures.Module.Principal.Create();
      
      if (_obj.IsDelegated == true)
      {
        var mainPrincipal = CompanyBases.As(_obj.MainPoAPrincipal);
        principal.TIN = mainPrincipal?.TIN;
        principal.TRRC = mainPrincipal?.TRRC;
      }
      else
      {
        principal.TIN = _obj.BusinessUnit?.TIN;
        principal.TRRC = _obj.BusinessUnit?.TRRC;
      }
      
      return principal;
    }
    
    /// <summary>
    /// Обновить статус валидации доверенности на сервисе.
    /// </summary>
    /// <param name="serviceConnection">Подключение к сервису доверенностей.</param>
    /// <param name="queueItem">Элемент очереди синхронизации эл. доверенностей.</param>
    /// <returns>True - если нужно продолжить дальнейшую обработку элемента очереди.</returns>
    /// <remarks>Функция обновляет только значения полей элемента очереди синхронизации.</remarks>
    [Public, Obsolete("Метод больше не используется с 31.07.2024 и версии 4.6")]
    public virtual bool UpdateValidationServiceStatus(Sungero.PowerOfAttorneyCore.IPowerOfAttorneyServiceConnection serviceConnection, IPowerOfAttorneyQueueItem queueItem)
    {
      if (queueItem == null)
      {
        Logger.DebugFormat("UpdateValidationServiceStatus: Queue item is null (PoA id = {0}).", _obj.Id);
        return false;
      }
      
      var validationState = PowerOfAttorneyCore.PublicFunctions.Module.GetPoAValidationState(serviceConnection, queueItem.OperationId);
      
      // Ошибка обработки элемента синхронизации эл. доверенностей в сервисе.
      if (validationState.OperationStatus == Constants.FormalizedPowerOfAttorney.FPoARegistrationStatus.Error)
      {
        return this.TryProcessPowerOfAttorneyValidationStateErrors(false, validationState.Errors, queueItem);
      }
      
      if (validationState.OperationStatus == Constants.FormalizedPowerOfAttorney.FPoARegistrationStatus.Done)
      {
        if (validationState.Result == Constants.FormalizedPowerOfAttorney.FPoAState.Valid)
        {
          // Эл. доверенность является действительной на текущий момент.
          Logger.DebugFormat("UpdateValidationServiceStatus: Formalized power of attorney with id {0} is valid, no processing required.", _obj.Id);
          queueItem.FormalizedPoAServiceStatus = Sungero.Docflow.PowerOfAttorneyQueueItem.FormalizedPoAServiceStatus.Valid;
          queueItem.Save();
          return true;
        }
        else if (validationState.Result == Constants.FormalizedPowerOfAttorney.FPoAState.Invalid)
        {
          // Эл. доверенность является недействительной на текущий момент.
          return this.TryProcessPowerOfAttorneyValidationStateErrors(true, validationState.Errors, queueItem);
        }
      }
      
      Logger.DebugFormat("UpdateValidationServiceStatus: Formalized power of attorney with id {0} is processing in service (operation status = {1}).",
                         _obj.Id, validationState.OperationStatus);
      return true;
    }
    
    /// <summary>
    /// Попытаться обработать ошибки валидации эл. доверенности в сервисе.
    /// </summary>
    /// <param name="isAsyncOperationComplete">Обработка асинхронной операции в сервисе завершена.</param>
    /// <param name="errors">Список ошибок валидации.</param>
    /// <param name="queueItem">Элемент очереди синхронизации эл. доверенностей.</param>
    /// <returns>True - если нужно продолжить дальнейшую обработку элемента очереди.</returns>
    [Obsolete("Метод больше не используется с 31.07.2024 и версии 4.6")]
    private bool TryProcessPowerOfAttorneyValidationStateErrors(bool isAsyncOperationComplete, List<PowerOfAttorneyCore.Structures.Module.IValidationOperationError> errors, IPowerOfAttorneyQueueItem queueItem)
    {
      var isFurtherProcessingRequired = true;
      
      if (this.PowerOfAttorneyValidationStateHasErrorWithCode(errors, Constants.FormalizedPowerOfAttorney.FPoAState.PoANotFoundError))
      {
        // Эл. доверенность не найдена в реестре ФНС.
        queueItem.RejectCode = Constants.FormalizedPowerOfAttorney.FPoAState.PoANotFoundError;
        Logger.DebugFormat("TryProcessPowerOfAttorneyValidationStateErrors: Formalized power of attorney with id {0} not found in FTS register.", _obj.Id);
      }
      else if (this.PowerOfAttorneyValidationStateHasErrorWithCode(errors, Constants.FormalizedPowerOfAttorney.FPoAState.Revoked))
      {
        // Эл. доверенность отозвана.
        queueItem.RejectCode = Constants.FormalizedPowerOfAttorney.FPoAState.Revoked;
        Logger.DebugFormat("TryProcessPowerOfAttorneyValidationStateErrors: Formalized power of attorney with id {0} was revoked in FTS register, trying to update validation service status.", _obj.Id);
      }
      else if (this.PowerOfAttorneyValidationStateHasErrorWithCode(errors, Constants.FormalizedPowerOfAttorney.FPoAState.Expired))
      {
        // Срок действия эл. доверенности истек.
        queueItem.RejectCode = Constants.FormalizedPowerOfAttorney.FPoAState.Expired;
        Logger.DebugFormat("TryProcessPowerOfAttorneyValidationStateErrors: Formalized power of attorney with id {0} was expired, trying to update validation service status.", _obj.Id);
      }
      else if (this.PowerOfAttorneyValidationStateHasErrorWithCode(errors, Constants.FormalizedPowerOfAttorney.FPoAState.NotValidYet))
      {
        // Срок действия эл. доверенности ещё не наступил.
        queueItem.RejectCode = Constants.FormalizedPowerOfAttorney.FPoAState.NotValidYet;
        Logger.DebugFormat("TryProcessPowerOfAttorneyValidationStateErrors: Formalized power of attorney with id {0} is not valid yet, trying to update validation service status.", _obj.Id);
      }
      else
      {
        if (isAsyncOperationComplete)
          // Не удалось определить причину невалидности эл. доверенности.
          Logger.DebugFormat("TryProcessPowerOfAttorneyValidationStateErrors: Formalized power of attorney with id {0} is invalid.", _obj.Id);
        else
          // Не удалось установить причину ошибки, возникшей в процессе валидации.
          Logger.DebugFormat("TryProcessPowerOfAttorneyValidationStateErrors: Formalized power of attorney with id {0} validation error.", _obj.Id);
        
        isFurtherProcessingRequired = false;
      }
      
      queueItem.FormalizedPoAServiceStatus = Sungero.Docflow.PowerOfAttorneyQueueItem.FormalizedPoAServiceStatus.Invalid;
      queueItem.Save();
      return isFurtherProcessingRequired;
    }
    
    /// <summary>
    /// Проверить, есть ли среди ошибок валидации ошибка с определенным кодом.
    /// </summary>
    /// <param name="errors">Список ошибок валидации.</param>
    /// <param name="code">Искомый код ошибки.</param>
    /// <returns>True, если в списке ошибок есть ошибка с указанным кодом, иначе - false.</returns>
    [Obsolete("Метод больше не используется с 31.07.2024 и версии 4.6")]
    private bool PowerOfAttorneyValidationStateHasErrorWithCode(List<PowerOfAttorneyCore.Structures.Module.IValidationOperationError> errors, string code)
    {
      return errors.Any(x => string.Equals(x.Code, code, StringComparison.InvariantCultureIgnoreCase));
    }
    
    /// <summary>
    /// Установить эл. доверенность в отозванное состояние.
    /// </summary>
    /// <returns>True - если доверенность успешно перешла в отозванное состояние.</returns>
    [Public, Obsolete("Метод больше не используется с 12.08.2024 и версии 4.6")]
    public virtual bool SetRevokedState()
    {
      var serviceConnection = Sungero.PowerOfAttorneyCore.PublicFunctions.Module.GetPowerOfAttorneyServiceConnection(_obj.BusinessUnit);
      return this.SetRevokedState(serviceConnection);
    }
    
    /// <summary>
    /// Установить эл. доверенность в отозванное состояние.
    /// </summary>
    /// <param name="serviceConnection">Подключение к сервису доверенностей.</param>
    /// <returns>True - если доверенность успешно перешла в отозванное состояние.</returns>
    [Public, Obsolete("Метод больше не используется с 12.08.2024 и версии 4.6")]
    public virtual bool SetRevokedState(Sungero.PowerOfAttorneyCore.IPowerOfAttorneyServiceConnection serviceConnection)
    {
      var revocationInfo = PowerOfAttorneyCore.PublicFunctions.Module.Remote.GetPowerOfAttorneyRevocationInfo(serviceConnection, _obj.UnifiedRegistrationNumber);
      
      if (revocationInfo == null)
      {
        Logger.DebugFormat("SetRevokedState. Cannot obtain revocation data from service, trying to set power of attorney with id {0} to revoked state.", _obj.Id);
        return this.SetRevokedState(string.Empty, Calendar.UserToday);
      }
      
      Logger.DebugFormat("SetRevokedState. Revocation data for power of attorney with id {0}: reason {1}, date {2}.", _obj.Id, revocationInfo.Reason, revocationInfo.Date);
      return this.SetRevokedState(revocationInfo.Reason, revocationInfo.Date);
    }
    
    /// <summary>
    /// Установить эл. доверенность в отозванное состояние.
    /// </summary>
    /// <param name="reason">Причина отзыва.</param>
    /// <param name="revocationDate">Дата отзыва.</param>
    /// <returns>True - если доверенность успешно перешла в отозванное состояние.</returns>
    [Public]
    public virtual bool SetRevokedState(string reason, DateTime revocationDate)
    {
      Logger.DebugFormat("SetRevokedState. Business unit id = {0}. Unified registration number = {1}.", _obj.BusinessUnit?.Id,  _obj.UnifiedRegistrationNumber);
      
      try
      {
        if (_obj.LifeCycleState != Sungero.Docflow.FormalizedPowerOfAttorney.LifeCycleState.Obsolete)
          _obj.LifeCycleState = Sungero.Docflow.FormalizedPowerOfAttorney.LifeCycleState.Obsolete;
        if (_obj.FtsListState != Sungero.Docflow.FormalizedPowerOfAttorney.FtsListState.Revoked)
          _obj.FtsListState = Sungero.Docflow.FormalizedPowerOfAttorney.FtsListState.Revoked;
        // Нельзя установить дату меньше, чем "Действует с".
        if (_obj.ValidTill != revocationDate && _obj.ValidTill > revocationDate)
          _obj.ValidTill = _obj.ValidFrom > revocationDate ? _obj.ValidFrom : revocationDate;
        
        if (!string.IsNullOrEmpty(reason) && (string.IsNullOrEmpty(_obj.Note) || !_obj.Note.Contains(reason)))
        {
          var reasonPrefix = string.IsNullOrWhiteSpace(_obj.Note) ? string.Empty : Environment.NewLine;
          _obj.Note += Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.FormalizedPowerOfAttorneyRevocationReasonFormat(reasonPrefix, reason);
        }
        if (_obj.State.IsChanged)
          _obj.Save();
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("SetRevokedState. Failed to set revoked state (PoA id = {0}).", ex, _obj.Id);
        return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Установить состояние эл. доверенности.
    /// </summary>
    /// <param name="lifeCycleState">Состояние жизненного цикла.</param>
    /// <param name="ftsListState">Состояние в реестре ФНС.</param>
    /// <returns>True - если доверенность успешно перешла в состояние.</returns>
    [Public]
    public virtual bool TrySetLifeCycleAndFtsListStates(Enumeration? lifeCycleState, Enumeration? ftsListState)
    {
      try
      {
        Functions.FormalizedPowerOfAttorney.SetLifeCycleAndFtsListStates(_obj, lifeCycleState, ftsListState);
        _obj.Save();
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("TrySetLifeCycleStateAndFtsListState. Failed to set LifeCycleState and FtsListState (PoA id = {0}).", ex, _obj.Id);
        return false;
      }
      
      return true;
    }
    
    #endregion

    #region Валидации доверенности
    
    /// <summary>
    /// Проверить эл. доверенность перед отправкой запроса к сервису доверенностей.
    /// </summary>
    /// <returns>Сообщение об ошибке или пустая строка, если ошибок нет.</returns>
    [Public, Remote]
    public virtual string ValidateFormalizedPoABeforeSending()
    {
      var validationError = this.ValidateBodyAndSignature();

      if (!string.IsNullOrEmpty(validationError))
        return validationError;

      if (!Sungero.PowerOfAttorneyCore.PublicFunctions.Module.HasPowerOfAttorneyServiceConnection(_obj.BusinessUnit))
        return Sungero.PowerOfAttorneyCore.Resources.ServiceConnectionNotConfigured;
      
      // Валидация xml по схеме.
      try
      {
        var body = Docflow.PublicFunctions.Module.GetBinaryData(_obj.LastVersion.Body);
        var xml = Docflow.Structures.Module.ByteArray.Create(body);
        this.ValidateFormalizedPowerOfAttorneyXml(xml);
      }
      catch
      {
        return FormalizedPowerOfAttorneys.Resources.XmlLoadFailed;
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Проверить валидность xml-файла эл. доверенности.
    /// </summary>
    /// <param name="xml">Тело эл. доверенности.</param>
    [Public]
    public virtual void ValidateFormalizedPowerOfAttorneyXml(Docflow.Structures.Module.IByteArray xml)
    {
      var version = Sungero.FormalizeDocumentsParser.Extension.GetPoAVersion(xml.Bytes);
      if (version == PoAVersion.V001)
        return;
      if (!this.ValidateGeneratedFormalizedPowerOfAttorneyXml(xml))
      {
        Logger.Error("Import formalized power of attorney. Failed to load XML");
        throw AppliedCodeException.Create(FormalizedPowerOfAttorneys.Resources.XmlLoadFailed);
      }
    }
    
    /// <summary>
    /// Проверить, отключена ли валидация рег.номера.
    /// </summary>
    /// <returns>Для МЧД всегда отключена.</returns>
    public override bool IsNumberValidationDisabled()
    {
      return true;
    }
    
    #endregion
    
    #region История смены состояний
    
    public override System.Collections.Generic.IEnumerable<Sungero.Docflow.Structures.OfficialDocument.HistoryOperation> StatusChangeHistoryOperations(Sungero.Content.DocumentHistoryEventArgs e)
    {
      foreach (var operation in base.StatusChangeHistoryOperations(e))
      {
        yield return operation;
      }
      
      if (_obj.FtsListState != _obj.State.Properties.FtsListState.OriginalValue)
      {
        if (_obj.FtsListState != null)
          yield return Sungero.Docflow.Structures.OfficialDocument.HistoryOperation.Create(
            Constants.FormalizedPowerOfAttorney.Operation.FtsStateChange,
            Sungero.Docflow.FormalizedPowerOfAttorneys.Info.Properties.FtsListState.GetLocalizedValue(_obj.FtsListState));
        else
          yield return Sungero.Docflow.Structures.OfficialDocument.HistoryOperation.Create(
            Constants.FormalizedPowerOfAttorney.Operation.FtsStateClear, null);
      }
    }
    
    #endregion
    
    /// <summary>
    /// Создать простую задачу с уведомлением по отзыву электронной доверенности.
    /// </summary>
    [Public]
    public virtual void SendNoticeForRevokedFormalizedPoA()
    {
      // Получение параметров для задачи с уведомлением.
      var subject = FormalizedPowerOfAttorneys.Resources.TitleForNoticeFormat(_obj.Name);
      var performers = this.GetRevokedPoANotificationReceivers();
      
      // Проверка на корректность параметров.
      if (performers.Count == 0)
      {
        Logger.DebugFormat("SendNoticeForRevokedFormalizedPoA. No users to receive notification (PoA id = {0}).", _obj.Id);
        return;
      }
      
      try
      {
        var task = Workflow.SimpleTasks.CreateWithNotices(subject, performers, new[] { _obj });
        task.ActiveText = _obj.Note;
        task.Start();
        Logger.DebugFormat("SendNoticeForRevokedFormalizedPoA. Notice of revocation was sent successfully (task id = {0}, recipient id = {1}).", task.Id, string.Join<long>(", ", performers.Select(u => u.Id)));
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("SendNoticeForRevokedFormalizedPoA. Error sending notice of revocation: {0}.", ex);
      }
    }
    
    /// <summary>
    /// Получить список адресатов уведомления об отзыве  электронной доверенности.
    /// </summary>
    /// <returns> Список адресатов.</returns>
    public virtual List<IUser> GetRevokedPoANotificationReceivers()
    {
      var issuedTo = _obj.IssuedTo;
      var preparedBy = _obj.PreparedBy;
      var issuedToManager = Company.Employees.Null;
      if (issuedTo != null)
        issuedToManager = PublicFunctions.Module.Remote.GetManager(issuedTo);
      
      var performers = new List<IUser>();
      
      if (issuedTo != null && issuedTo.Status == Sungero.Company.Employee.Status.Active)
      {
        var needNotice = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(issuedTo).MyRevokedFormalizedPoANotification;
        if (needNotice == true)
          performers.Add(issuedTo);
      }
      
      if (preparedBy != null && preparedBy.Status == Sungero.Company.Employee.Status.Active)
      {
        var needNotice = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(preparedBy).MyRevokedFormalizedPoANotification;
        if (needNotice == true)
          performers.Add(preparedBy);
      }
      
      if (issuedToManager != null && issuedToManager.Status == Sungero.Company.Employee.Status.Active)
      {
        var needNotice = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(issuedToManager).MySubordinatesRevokedFormalizedPoANotification;
        if (needNotice == true)
          performers.Add(issuedToManager);
      }

      return performers.Distinct().ToList();
    }
    
    /// <summary>
    /// Заполнить подписывающего в карточке документа.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    [Remote]
    public override void SetDocumentSignatory(Company.IEmployee employee)
    {
      if (_obj.OurSignatory != null)
        return;
      base.SetDocumentSignatory(employee);
    }
    
    /// <summary>
    /// Заполнить основание в карточке документа.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <param name="e">Аргументы события подписания.</param>
    /// <param name="changedSignatory">Признак смены подписывающего.</param>
    public override void SetOurSigningReason(Company.IEmployee employee, Sungero.Domain.BeforeSigningEventArgs e, bool changedSignatory)
    {
      if (!Equals(_obj.OurSignatory, employee))
        return;
      base.SetOurSigningReason(employee, e, changedSignatory);
    }
    
    /// <summary>
    /// Заполнить Единый рег. № из эл. доверенности в подпись.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <param name="signature">Подпись.</param>
    /// <param name="certificate">Сертификат для подписания.</param>
    public override void SetUnifiedRegistrationNumber(Company.IEmployee employee, Sungero.Domain.Shared.ISignature signature, ICertificate certificate)
    {
      if (signature.SignCertificate == null)
        return;

      var changedSignatory = !Equals(_obj.OurSignatory, employee);
      var signingReason = this.GetSuitableOurSigningReason(employee, certificate, changedSignatory);
      this.SetUnifiedRegistrationNumber(signingReason, signature, certificate);
    }
    
    /// <summary>
    /// Проверить блокировку электронной доверенности.
    /// </summary>
    /// <returns>True - заблокирована, иначе - false.</returns>
    [Public]
    public virtual bool FormalizedPowerOfAttorneyIsLocked()
    {
      if (Locks.GetLockInfo(_obj).IsLocked)
      {
        Logger.DebugFormat("ProcessPowerOfAttorneyQueueItem: Formalized power of attorney with id {0} is locked.", _obj.Id);
        return true;
      }
      return false;
    }
    
    /// <summary>
    /// Получить URL системы которая предоставляет возможность просмотра МЧД.
    /// </summary>
    /// <param name="documentVersion">Версия документа.</param>
    /// <returns>URL системы которая предоставляет возможность просмотра МЧД.</returns>
    [Public]
    public virtual string GetSystemUrlFromBodyXml(Sungero.Content.IElectronicDocumentVersions documentVersion)
    {
      if (documentVersion == null || documentVersion.Body.Size == 0)
      {
        Logger.Debug("GetSystemUrlFromBodyXml. Document version is empty.");
        return null;
      }
      
      byte[] bytes;
      
      using (var body = new System.IO.MemoryStream())
      {
        // Выключить error-логирование при доступе к зашифрованным бинарным данным.
        AccessRights.SuppressSecurityEvents(() => documentVersion.Body.Read().CopyTo(body));
        bytes = body.ToArray();
      }
      
      var version = Sungero.FormalizeDocumentsParser.Extension.GetPoAVersion(bytes);
      
      if (version == PoAVersion.V003)
        return this.GetSystemUrlFromPoAV003(bytes);
      
      if (version == PoAVersion.V002)
        return this.GetSystemUrlFromPoAV002(bytes);
      
      Logger.DebugFormat("GetSystemUrlFromBodyXml. Failed to parse fpoa system url from xml. Document id: {0}, document version id: {1}",
                         documentVersion.RootEntity.Id, documentVersion.Id);
      return null;
    }
    
    /// <summary>
    /// Получить URL системы которая предоставляет возможность просмотра МЧД из доверенности версии 003.
    /// </summary>
    /// <param name="formalizedPoAXml">Массив байт с доверенностью.</param>
    /// <returns>URL системы которая предоставляет возможность просмотра МЧД.</returns>
    private string GetSystemUrlFromPoAV003(byte[] formalizedPoAXml)
    {
      var fpoa = Sungero.FormalizeDocumentsParser.Extension.DeserializePoAV3(formalizedPoAXml);
      
      if (PoAV3Builder.IsMainPoA(fpoa))
      {
        var dover = PoAV3Builder.Довер(fpoa);
        return dover?.СвДов?.СведСист;
      }
      else
      {
        var peredov = PoAV3Builder.Передов(fpoa);
        return peredov?.СвПереДовер?.СведСист;
      }
    }
    
    /// <summary>
    /// Получить URL системы которая предоставляет возможность просмотра МЧД из доверенности версии 002.
    /// </summary>
    /// <param name="formalizedPoAXml">Массив байт с доверенностью.</param>
    /// <returns>URL системы которая предоставляет возможность просмотра МЧД.</returns>
    private string GetSystemUrlFromPoAV002(byte[] formalizedPoAXml)
    {
      var fpoa = Sungero.FormalizeDocumentsParser.Extension.DeserializePoAV2(formalizedPoAXml);
      
      if (PoAV2Builder.IsMainPoA(fpoa))
      {
        var dover = PoAV2Builder.GetDover(fpoa);
        return dover?.СвДов?.СведСистОтм;
      }
      else
      {
        var peredov = PoAV2Builder.GetPeredov(fpoa);
        return peredov?.СвДовПер?.СвПереДовер?.СведСистОтм;
      }
    }
    
    /// <summary>
    /// Получить идентификатор системы в которой осуществляется хранение МЧД.
    /// </summary>
    /// <returns>Идентификатор системы в которой осуществляется хранение МЧД.</returns>
    [Public]
    public virtual string GetStorageSystemId()
    {
      return PublicConstants.FormalizedPowerOfAttorney.FPoAStorageSystemId;
    }
    
    #region Генерация МЧД V2
    
    #region Доверенность
    
    /// <summary>
    /// Создать тело эл. доверенности по формату 002.
    /// </summary>
    /// <param name="unifiedRegistrationNumber">Уникальный рег. номер доверенности.</param>
    /// <returns>Тело эл. доверенности.</returns>
    public virtual Docflow.Structures.Module.IByteArray CreateFormalizedPowerOfAttorneyXmlV2(Guid unifiedRegistrationNumber)
    {
      var fpoa = PoAV2Builder.CreateEmptyPoA();
      fpoa.ИдФайл = Constants.FormalizedPowerOfAttorney.FPoAV2FilePrefix + this.GetFileIdAttribute(unifiedRegistrationNumber);
      var dover = PoAV2Builder.CreatePoAElement();
      fpoa.Документ.Item = dover;
      
      this.FillFPoAInfoV2(PoAV2Builder.GetPoAInfo(fpoa), unifiedRegistrationNumber);
      
      this.AddLegalEntityPrincipalV2(fpoa);
      
      this.AddAgentV2(fpoa);
      
      this.FillPowersV2(fpoa);
      
      var xml = Sungero.FormalizeDocumentsParser.Extension.GetPowerOfAttorneyXmlV2(fpoa);
      return Docflow.Structures.Module.ByteArray.Create(xml);
    }
    
    /// <summary>
    /// Добавить доверителя - юридическое лицо.
    /// </summary>
    /// <param name="fpoa">Эл. доверенность 002 формата.</param>
    public virtual void AddLegalEntityPrincipalV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.Доверенность fpoa)
    {
      var legalEntityPrincipal = PoAV2Builder.CreateLegalEntityPrincipal(fpoa);
      var principal = PoAV2PrincipalItemConverter.GetAsLegalEntity(legalEntityPrincipal).СвРосОрг;
      var head = PoAV2PrincipalItemConverter.GetAsLegalEntity(legalEntityPrincipal).ЛицоБезДов.СвФЛ;
      var signatory = legalEntityPrincipal.Подписант;
      
      // Заполнение элемента "Сведения о российском юридическом лице (СвРосОрг)".
      if (_obj.BusinessUnit != null)
      {
        principal.НаимОрг = _obj.BusinessUnit.LegalName;
        principal.ИННЮЛ = _obj.BusinessUnit.TIN;
        principal.ОГРН = _obj.BusinessUnit.PSRN;
        principal.КПП = _obj.BusinessUnit.TRRC;
        principal.АдрРФ = _obj.BusinessUnit.LegalAddress;
      }
      
      // Заполнение элемента "Сведения о лице, действующем от имени юридического лица без доверенности (ЛицоБезДов)".
      if (_obj.BusinessUnit.CEO != null && _obj.BusinessUnit.CEO.Person != null)
      {
        head.ИННФЛ = _obj.BusinessUnit.CEO.Person.TIN;
        head.СНИЛС = Parties.PublicFunctions.Person.GetFormattedInila(_obj.BusinessUnit.CEO.Person);
        
        if (_obj.BusinessUnit.CEO.Person.DateOfBirth.HasValue)
          head.СведФЛ.ДатаРожд = _obj.BusinessUnit.CEO.Person.DateOfBirth.Value;
        
        if (_obj.BusinessUnit.CEO.JobTitle != null)
          head.Должность = _obj.BusinessUnit.CEO.JobTitle.Name;
      }
      
      if (_obj.OurSigningReason != null)
        head.НаимДокПолн = _obj.OurSigningReason.Name;

      // TODO Сделать признак наличия гражданства для иностранцев.
      head.СведФЛ.ПрГражд = PoAV2Enums.CitizenshipFlagToNative(CitizenshipFlag.Russia);
      
      // Заполнение элемента "Сведения о физическом лице, подписывающем доверенность от имени доверителя (Подписант)".
      if (_obj.OurSignatory != null)
      {
        signatory.Имя = _obj.OurSignatory.Person.FirstName;
        signatory.Фамилия = _obj.OurSignatory.Person.LastName;
        signatory.Отчество = _obj.OurSignatory.Person.MiddleName;
      }
    }
    
    /// <summary>
    /// Добавить представителя.
    /// </summary>
    /// <param name="fpoa">Эл. доверенность 002 формата.</param>
    public virtual void AddAgentV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.Доверенность fpoa)
    {
      var individualAgents = _obj.Representatives.Where(r => r.AgentType == Docflow.PowerOfAttorneyBaseRepresentatives.AgentType.Person);
      foreach (var individualAgent in individualAgents)
        this.AddIndividualAgentV2(fpoa, Sungero.Parties.People.As(individualAgent.IssuedTo));
      
      this.AddLegalEntityAgentV2(fpoa);
      
      this.AddEntrepreneurAgentV2(fpoa);
    }
    
    /// <summary>
    /// Добавить представителя - юр. лицо.
    /// </summary>
    /// <param name="fpoa">Эл. доверенность 002 формата.</param>
    public virtual void AddLegalEntityAgentV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.Доверенность fpoa)
    {
      var legalEntityAgents = _obj.Representatives.Where(r => r.AgentType == Docflow.PowerOfAttorneyBase.AgentType.LegalEntity);
      foreach (var legalEntityAgent in legalEntityAgents)
        this.AddLegalEntityAgentV2(fpoa, legalEntityAgent.IssuedTo, legalEntityAgent.Agent);
    }
    
    /// <summary>
    /// Добавить представителя - юр. лицо.
    /// </summary>
    /// <param name="fpoa">Эл. доверенность 002 формата.</param>
    /// <param name="issuedTo">Кому выдана.</param>
    /// <param name="representative">Представитель.</param>
    protected void AddLegalEntityAgentV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.Доверенность fpoa,
                                         ICounterparty issuedTo, IPerson representative)
    {
      var legalEntityAgent = PoAV2Builder.CreateLegalEntityAgent(fpoa);
      var legalEntity = PoAV2Builder.GetLegalEntityAgent(legalEntityAgent);
      var legalEntityInfo = legalEntity.СвОрг;

      if (issuedTo != null)
      {
        legalEntityInfo.НаимОрг = issuedTo.Name;
        legalEntityInfo.ОГРН = issuedTo.PSRN;
        
        if (CompanyBases.Is(issuedTo))
          legalEntityInfo.КПП = CompanyBases.As(issuedTo).TRRC;
        
        legalEntityInfo.ИННЮЛ = issuedTo.TIN;
        
        if (issuedTo.LegalAddress != null && issuedTo.Region?.Code != null)
        {
          var address = PoAV2Builder.GetLegalEntityAddress(legalEntityInfo);
          address.АдрРФ = issuedTo.LegalAddress;
          address.Регион = issuedTo.Region?.Code;
        }
      }
      
      if (representative != null)
      {
        var agentRepresentative = PoAV2Builder.GetLegalEntityAgentRepresentative(legalEntity);
        agentRepresentative.ФИО.Имя = representative.FirstName;
        agentRepresentative.ФИО.Отчество = representative.MiddleName;
        agentRepresentative.ФИО.Фамилия = representative.LastName;
        agentRepresentative.ИННФЛ = representative.TIN;
        agentRepresentative.СНИЛС = Parties.PublicFunctions.Person.GetFormattedInila(representative);
        
        if (representative.DateOfBirth.HasValue)
          agentRepresentative.СведФЛ.ДатаРожд = representative.DateOfBirth.Value;
        
        agentRepresentative.СведФЛ.КонтактТлф = representative.Phones;
        
        var citizenshipFlag = this.GetCitizenshipFlag(representative.Citizenship);
        agentRepresentative.СведФЛ.ПрГражд = PoAV2Enums.CitizenshipFlagToNative(citizenshipFlag);
        agentRepresentative.СведФЛ.Гражданство = citizenshipFlag == CitizenshipFlag.Other ? representative.Citizenship.Code : null;
      }
    }
    
    /// <summary>
    /// Добавить представителя - ИП.
    /// </summary>
    /// <param name="fpoa">Эл. доверенность 002 формата.</param>
    public virtual void AddEntrepreneurAgentV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.Доверенность fpoa)
    {
      var entrepreneurAgents = _obj.Representatives.Where(r => r.AgentType == Docflow.PowerOfAttorneyBase.AgentType.Entrepreneur);
      foreach (var entrepreneurAgent in entrepreneurAgents)
        this.AddEntrepreneurAgentV2(fpoa, entrepreneurAgent.IssuedTo, entrepreneurAgent.Agent);
    }
    
    /// <summary>
    /// Добавить представителя - ИП.
    /// </summary>
    /// <param name="fpoa">Эл. доверенность 002 формата.</param>
    /// <param name="issuedTo">Кому выдана.</param>
    /// <param name="representative">Представитель.</param>
    protected void AddEntrepreneurAgentV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.Доверенность fpoa,
                                          ICounterparty issuedTo, IPerson representative)
    {
      var entrepreneurAgent = PoAV2Builder.CreateEntrepreneurAgent(fpoa);
      var agent = PoAV2Builder.GetEntrepreneurAgent(entrepreneurAgent);
      
      if (issuedTo != null)
      {
        agent.НаимИП = issuedTo.Name;
        agent.ОГРНИП = issuedTo.PSRN;
      }
      
      if (representative != null)
      {
        agent.ФИО.Имя = representative.FirstName;
        agent.ФИО.Отчество = representative.MiddleName;
        agent.ФИО.Фамилия = representative.LastName;
        agent.СНИЛС = Parties.PublicFunctions.Person.GetFormattedInila(representative);
        agent.ИННФЛ = representative.TIN;
        
        if (representative.DateOfBirth.HasValue)
          agent.СведФЛ.ДатаРожд = representative.DateOfBirth.Value;
        
        agent.СведФЛ.КонтактТлф = representative.Phones;
        
        var citizenshipFlag = this.GetCitizenshipFlag(representative.Citizenship);
        agent.СведФЛ.ПрГражд = PoAV2Enums.CitizenshipFlagToNative(citizenshipFlag);
        agent.СведФЛ.Гражданство = citizenshipFlag == CitizenshipFlag.Other ? representative.Citizenship.Code : null;
      }
    }
    
    #endregion
    
    #region Передоверие
    
    /// <summary>
    /// Создать тело эл. доверенности "В рамках передоверия", формат 002.
    /// </summary>
    /// <param name="unifiedRegistrationNumber">Уникальный рег. номер доверенности.</param>
    /// <returns>Тело эл. доверенности.</returns>
    public virtual Docflow.Structures.Module.IByteArray CreateRetrustXmlV2(Guid unifiedRegistrationNumber)
    {
      var fpoa = PoAV2Builder.CreateEmptyPoA();
      fpoa.ИдФайл = Constants.FormalizedPowerOfAttorney.FPoAV2FilePrefix + this.GetFileIdAttribute(unifiedRegistrationNumber);
      
      fpoa.Документ.Item = PoAV2Builder.CreateRetrustElement();
      
      this.FillMainPoAInfoForRetrustV2(fpoa);
      this.FillFPoAInfoV2(PoAV2Builder.GetPoAInfo(fpoa), unifiedRegistrationNumber);
      this.AddLegalEntityPrincipalForRetrustV2(fpoa);
      this.AddAgentV2(fpoa);
      this.FillPowersV2(fpoa);
      
      var xml = Sungero.FormalizeDocumentsParser.Extension.GetPowerOfAttorneyXmlV2(fpoa);
      return Docflow.Structures.Module.ByteArray.Create(xml);
    }
    
    /// <summary>
    /// Заполнить информацию о корневой доверенности для передоверия (формат 002).
    /// </summary>
    /// <param name="fpoa">Эл. доверенность.</param>
    private void FillMainPoAInfoForRetrustV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.Доверенность fpoa)
    {
      var retrust = PoAV2Builder.GetPeredov(fpoa);
      retrust.СвДовПер.СвОснДовер.НомДовер0 = _obj.MainPoAUnifiedNumber;
      retrust.СвДовПер.НомДоверN = _obj.MainPoAUnifiedNumber;
      
      var principal = PoAV2Builder.GetMainPoAPrincipalForRetrust(fpoa);
      
      principal.НаимОрг = _obj.MainPoAPrincipal.Name;
      principal.ИННЮЛ = _obj.MainPoAPrincipal.TIN;
      principal.ОГРН = _obj.MainPoAPrincipal.PSRN;
      principal.АдрРФ = _obj.MainPoAPrincipal.LegalAddress;
      principal.КПП = Companies.As(_obj.MainPoAPrincipal)?.TRRC;
    }
    
    /// <summary>
    /// Заполнить информацию о доверителе в передоверии (формат 002).
    /// </summary>
    /// <param name="fpoa">Эл. доверенность.</param>
    private void AddLegalEntityPrincipalForRetrustV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.Доверенность fpoa)
    {
      var principalInfo = PoAV2Builder.CreateLegalEntityPrincipalForRetrust(fpoa);
      var item = PoAV2Builder.GetLegalEntityPrincipalForRetrust(principalInfo);
      var signatory = principalInfo.Подписант;
      
      if (_obj.BusinessUnit != null)
      {
        item.НаимОрг = _obj.BusinessUnit.LegalName;
        item.ИННЮЛ = _obj.BusinessUnit.TIN;
        item.ОГРН = _obj.BusinessUnit.PSRN;
        item.КПП = _obj.BusinessUnit.TRRC;
        item.АдрРег.Регион = _obj.BusinessUnit.Region?.Code;
        item.АдрРег.АдрРФ = _obj.BusinessUnit.LegalAddress;
      }
      
      // Заполнение элемента "Сведения о физическом лице, подписывающем доверенность от имени доверителя (Подписант)".
      if (_obj.OurSignatory != null)
      {
        signatory.Имя = _obj.OurSignatory.Person.FirstName;
        signatory.Фамилия = _obj.OurSignatory.Person.LastName;
        signatory.Отчество = _obj.OurSignatory.Person.MiddleName;
      }
    }
    
    #endregion
    
    #region Общая часть
    
    /// <summary>
    /// Заполнить основные сведения доверенности (передоверия).
    /// </summary>
    /// <param name="info">Элемент со сведениями доверенности.</param>
    /// <param name="unifiedRegistrationNumber">Единый регистрационный номер доверенности.</param>
    public virtual void FillFPoAInfoV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.СвДовТип info, Guid unifiedRegistrationNumber)
    {
      info.НомДовер = unifiedRegistrationNumber.ToString();
      
      if (_obj.ValidFrom.HasValue)
        info.ДатаВыдДовер = _obj.ValidFrom.Value;
      
      if (_obj.ValidTill.HasValue)
        info.ДатаКонДовер = _obj.ValidTill.Value;
      
      var retrustValue = _obj.DelegationType == Sungero.Docflow.FormalizedPowerOfAttorney.DelegationType.WithDelegation ? Retrust.FreeRetrust : Retrust.NoRetrust;
      info.ПрПередов = PoAV2Enums.RetrustToNative(retrustValue);
      if (_obj.DelegationType != Sungero.Docflow.FormalizedPowerOfAttorney.DelegationType.NoDelegation)
      {
        info.ПрУтрПолн = PoAV2Enums.PowersLostTypeToNative(PowersLostType.PowersNotLost);
        info.ПрУтрПолнSpecified = true;
      }
      
      info.ВнНомДовер = _obj.RegistrationNumber;
      
      if (_obj.RegistrationDate.HasValue)
      {
        info.ДатаВнРегДовер = _obj.RegistrationDate.Value;
        info.ДатаВнРегДоверSpecified = true;
      }
      
      info.Безотзыв.ПрБезотзыв = PoAV2Enums.RevocableToNative(true);
      info.СведСистОтм = $"{Constants.FormalizedPowerOfAttorney.XmlGeneralInfoAttributeValues.SourceSystemInfo}{unifiedRegistrationNumber}";
      
      if (_obj.Representatives.Count() > 1)
        info.ПрСовмПолн = PoAV2Enums.JointPowersToNative(JointPowers.Individual);
    }

    /// <summary>
    /// Добавить представителя - физ. лицо.
    /// </summary>
    /// <param name="fpoa">Эл. доверенность 002 формата.</param>
    /// <param name="person">Кому выдана.</param>
    public virtual void AddIndividualAgentV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.Доверенность fpoa,
                                             Sungero.Parties.IPerson person)
    {
      PoAV2Builder.CreateIndividualAgent(fpoa);
      var agent = PoAV2Builder.GetIndividualAgent(fpoa);
      agent.ИННФЛ = person.TIN;
      agent.СНИЛС = Parties.PublicFunctions.Person.GetFormattedInila(person);
      agent.ФИО.Имя = person.FirstName;
      agent.ФИО.Фамилия = person.LastName;
      agent.ФИО.Отчество = person.MiddleName;
      
      agent.СведФЛ.ДатаРожд = person.DateOfBirth.Value;
      
      var citizenshipFlag = this.GetCitizenshipFlag(person.Citizenship);
      agent.СведФЛ.ПрГражд = PoAV2Enums.CitizenshipFlagToNative(citizenshipFlag);
      agent.СведФЛ.Гражданство = citizenshipFlag == CitizenshipFlag.Other ? person.Citizenship.Code : null;
    }
    
    /// <summary>
    /// Заполнить полномочия доверенности (передоверия).
    /// </summary>
    /// <param name="fpoa">Эл. доверенность 002 формата.</param>
    public virtual void FillPowersV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.Доверенность fpoa)
    {
      PoAV2Builder.CreatePowers(fpoa, _obj.Powers != null ? new string[] { _obj.Powers } : new string[0]);
    }
    
    #endregion
    
    #endregion
    
    #region Заполнение карточки МЧД V2
    
    /// <summary>
    /// Заполнить поля доверенности из десериализованного объекта.
    /// </summary>
    /// <param name="xml">Тело доверенности.</param>
    [Public]
    public virtual void FillFPoAV2(Docflow.Structures.Module.IByteArray xml)
    {
      var fpoa = Sungero.FormalizeDocumentsParser.Extension.DeserializePoAV2(xml.Bytes);
      
      this.FillDocumentNameV2(fpoa);
      _obj.FormatVersion = FormatVersion.Version002;
      
      if (PoAV2Builder.IsMainPoA(fpoa))
      {
        var dover = PoAV2Builder.GetDover(fpoa);
        this.FillFPoAV2(dover);
      }
      else
      {
        var peredov = PoAV2Builder.GetPeredov(fpoa);
        this.FillFPoAV2(peredov);
      }

      this.FillRepresentativeV2(fpoa);
    }
    
    /// <summary>
    /// Заполнить поля доверенности из десериализованного объекта.
    /// </summary>
    /// <param name="dover">Модель главного узла доверенности.</param>
    public virtual void FillFPoAV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДовер dover)
    {
      _obj.UnifiedRegistrationNumber = GetUniformGuid(dover.СвДов.НомДовер);
      var internalRegistrationDate = dover.СвДов.ДатаВнРегДоверSpecified
        ? dover.СвДов.ДатаВнРегДовер
        : Calendar.Today;
      this.FillRegistrationData(dover.СвДов.ВнНомДовер, internalRegistrationDate);

      _obj.ValidFrom = dover.СвДов.ДатаВыдДовер;
      _obj.ValidTill = dover.СвДов.ДатаКонДовер;
      
      _obj.DelegationType = this.GetDeligationTypeV2(dover);

      if (dover?.СвДоверит?.Count() == 1)
        this.FillPrincipalV2(dover.СвДоверит.FirstOrDefault());
      this.FillImportedPowersV2(dover);
    }
    
    /// <summary>
    /// Заполнить поля доверенности из десериализованного объекта.
    /// </summary>
    /// <param name="peredov">Модель главного узла доверенности со сведениями о передоверии.</param>
    public virtual void FillFPoAV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументПередов peredov)
    {
      _obj.DelegationType = this.GetDeligationTypeV2(peredov);
      _obj.IsDelegated = true;

      _obj.UnifiedRegistrationNumber = GetUniformGuid(peredov.СвДовПер.СвПереДовер.НомДовер);
      var internalRegistrationDate = peredov.СвДовПер.СвПереДовер.ДатаВнРегДоверSpecified
        ? peredov.СвДовПер.СвПереДовер.ДатаВнРегДовер
        : Calendar.Today;
      this.FillRegistrationData(peredov.СвДовПер.СвПереДовер.ВнНомДовер, internalRegistrationDate);
      
      this.FillImportedPowersV2(peredov);

      if (peredov?.СвЛицПередПолн?.Count() == 1)
        this.FillPrincipalV2(peredov?.СвЛицПередПолн.FirstOrDefault());
      
      _obj.ValidFrom = peredov.СвДовПер.СвПереДовер.ДатаВыдДовер;
      _obj.ValidTill = peredov.СвДовПер.СвПереДовер.ДатаКонДовер;
      
      this.FillRetrustMainPoAV2(peredov);
    }

    /// <summary>
    /// Заполнить полномочия доверенности из десериализованного объекта.
    /// </summary>
    /// <param name="dover">Модель главного узла доверенности.</param>
    public virtual void FillImportedPowersV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДовер dover)
    {
      _obj.Powers = this.ParsePowers(dover);
    }

    /// <summary>
    /// Парсинг типа передоверия.
    /// </summary>
    /// <param name="dover">Доверенность.</param>
    /// <returns>Тип передоверия.</returns>
    public virtual Enumeration GetDeligationTypeV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДовер dover)
    {
      return dover.СвДов.ПрПередов == PoAV2Enums.RetrustToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.Retrust.NoRetrust)
        ? DelegationType.NoDelegation
        : DelegationType.WithDelegation;
    }
    
    /// <summary>
    /// Парсинг типа передоверия.
    /// </summary>
    /// <param name="peredov">Доверенность.</param>
    /// <returns>Тип передоверия.</returns>
    public virtual Enumeration GetDeligationTypeV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументПередов peredov)
    {
      return peredov.СвДовПер.СвПереДовер.ПрПередов == PoAV2Enums.RetrustToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.Retrust.NoRetrust)
        ? DelegationType.NoDelegation
        : DelegationType.WithDelegation;
    }

    /// <summary>
    /// Парсинг полномочий доверенности.
    /// </summary>
    /// <param name="dover">Доверенность.</param>
    /// <returns>Полномочия.</returns>
    public virtual string ParsePowers(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДовер dover)
    {
      return string.Join(string.Empty, dover.СвПолн.Select(x => x.Item));
    }

    #region Заполнение доверителя V2
    
    /// <summary>
    /// Заполнить поля доверителя.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillPrincipalV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДоверСвДоверит principal)
    {
      if (principal == null)
        return;
      
      var principalType = principal.ТипДовер;
      if (principalType == PoAV2Enums.PrincipalTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.PrincipalType.LegalEntity))
      {
        this.FillLegalEntityPrincipalBusinessUnitV2(principal);
        this.FillLegalEntityPrincipalOurSignatoryV2(principal);
      }
      if (principalType == PoAV2Enums.PrincipalTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.PrincipalType.Entrepreneur))
      {
        this.FillEntrepreneurPrincipalBusinessUnitV2(principal);
        this.FillEntrepreneurPrincipalOurSignatoryV2(principal);
      }
      if (principalType == PoAV2Enums.PrincipalTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.PrincipalType.Individual))
      {
        this.FillIndividualPrincipalBusinessUnitV2(principal);
        this.FillIndividualPrincipalOurSignatoryV2(principal);
      }
    }
    
    /// <summary>
    /// Заполнить поля доверителя для передоверия.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillPrincipalV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументПередовСвЛицПередПолн principal)
    {
      if (principal == null)
        return;
      
      var principalType = principal.ТипЛицПрдПолн;
      if (principalType == RetrustV2Enums.PrincipalTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.PrincipalType.LegalEntity))
      {
        this.FillLegalEntityPrincipalBusinessUnitV2(principal);
        return;
      }
      if (principalType == RetrustV2Enums.PrincipalTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.PrincipalType.Entrepreneur))
      {
        this.FillEntrepreneurPrincipalBusinessUnitV2(principal);
        return;
      }
      if (principalType == RetrustV2Enums.PrincipalTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.PrincipalType.Individual))
      {
        this.FillIndividualPrincipalBusinessUnitV2(principal);
        this.FillIndividualPrincipalOurSignatoryV2(principal);
      }
    }
    
    #region Доверитель - юридическое лицо
    
    /// <summary>
    /// Заполнить НОР для доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillLegalEntityPrincipalBusinessUnitV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДоверСвДоверит principal)
    {
      var tin = this.GetLegalEntityPrincipalTinV2(principal);
      var trrc = this.GetLegalEntityPrincipalTrrcV2(principal);
      _obj.BusinessUnit = this.GetLegalEntityPrincipalBusinessUnit(tin, trrc);
    }
    
    /// <summary>
    /// Получить ИНН для организации доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ИНН доверителя.</returns>
    public virtual string GetLegalEntityPrincipalTinV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДоверСвДоверит principal)
    {
      var legalEntityPrincipal = PoAV2PrincipalItemConverter.GetAsLegalEntity(principal);
      return legalEntityPrincipal?.СвРосОрг?.ИННЮЛ;
    }
    
    /// <summary>
    /// Получить КПП для организации доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>КПП доверителя.</returns>
    public virtual string GetLegalEntityPrincipalTrrcV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДоверСвДоверит principal)
    {
      var legalEntityPrincipal = PoAV2PrincipalItemConverter.GetAsLegalEntity(principal);
      return legalEntityPrincipal?.СвРосОрг?.КПП;
    }
    
    /// <summary>
    /// Заполнить подписанта для доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillLegalEntityPrincipalOurSignatoryV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДоверСвДоверит principal)
    {
      var tin = this.GetLegalEntitySignatoryTinV2(principal);
      var inila = this.GetLegalEntitySignatoryInilaV2(principal);
      
      _obj.OurSignatory = this.GetPrincipalSignatory(tin, inila);
    }
    
    /// <summary>
    /// Получить ИНН сотрудника-подписанта для доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ИНН подписанта.</returns>
    public virtual string GetLegalEntitySignatoryTinV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДоверСвДоверит principal)
    {
      var legalEntityPrincipal = PoAV2PrincipalItemConverter.GetAsLegalEntity(principal);
      return legalEntityPrincipal?.ЛицоБезДов?.СвФЛ?.ИННФЛ;
    }
    
    /// <summary>
    /// Получить СНИЛС сотрудника-подписанта для доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>СНИЛС подписанта.</returns>
    public virtual string GetLegalEntitySignatoryInilaV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДоверСвДоверит principal)
    {
      var legalEntityPrincipal = PoAV2PrincipalItemConverter.GetAsLegalEntity(principal);
      return legalEntityPrincipal?.ЛицоБезДов?.СвФЛ?.СНИЛС;
    }

    #endregion
    
    #region Доверитель передоверия - юридическое лицо

    /// <summary>
    /// Заполнить НОР для доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillLegalEntityPrincipalBusinessUnitV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументПередовСвЛицПередПолн principal)
    {
      var tin = this.GetLegalEntityPrincipalTinV2(principal);
      var trrc = this.GetLegalEntityPrincipalTrrcV2(principal);
      _obj.BusinessUnit = this.GetLegalEntityPrincipalBusinessUnit(tin, trrc);
    }
    
    /// <summary>
    /// Получить ИНН для организации доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ИНН доверителя.</returns>
    public virtual string GetLegalEntityPrincipalTinV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументПередовСвЛицПередПолн principal)
    {
      var legalEntityPrincipal = RetrustV2PrincipalItemConverter.GetAsLegalEntity(principal);
      return legalEntityPrincipal?.ИННЮЛ;
    }
    
    /// <summary>
    /// Получить КПП для организации доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>КПП доверителя.</returns>
    public virtual string GetLegalEntityPrincipalTrrcV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументПередовСвЛицПередПолн principal)
    {
      var legalEntityPrincipal = RetrustV2PrincipalItemConverter.GetAsLegalEntity(principal);
      return legalEntityPrincipal?.КПП;
    }
    
    #endregion
    
    #region Доверитель - ИП
    
    /// <summary>
    /// Заполнить НОР для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillEntrepreneurPrincipalBusinessUnitV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДоверСвДоверит principal)
    {
      var tin = this.GetEntrepreneurPrincipalTinV2(principal);
      var psrn = this.GetEntrepreneurPrincipalPsrnV2(principal);
      
      _obj.BusinessUnit = this.GetEntrepreneurPrincipalBusinessUnit(tin, psrn);
    }
    
    /// <summary>
    /// Заполнить подписанта для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillEntrepreneurPrincipalOurSignatoryV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДоверСвДоверит principal)
    {
      var tin = this.GetEntrepreneurPrincipalTinV2(principal);
      var inila = this.GetEntrepreneurPrincipalInilaV2(principal);
      
      _obj.OurSignatory = this.GetPrincipalSignatory(tin, inila);
    }
    
    /// <summary>
    /// Получить ИНН для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ИНН доверителя.</returns>
    public virtual string GetEntrepreneurPrincipalTinV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДоверСвДоверит principal)
    {
      var entrepreneurPrincipal = PoAV2PrincipalItemConverter.GetAsEntrepreneur(principal);
      return entrepreneurPrincipal?.ИННФЛ;
    }
    
    /// <summary>
    /// Получить ОГРН для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ОГРН доверителя.</returns>
    public virtual string GetEntrepreneurPrincipalPsrnV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДоверСвДоверит principal)
    {
      var entrepreneurPrincipal = PoAV2PrincipalItemConverter.GetAsEntrepreneur(principal);
      return entrepreneurPrincipal?.ОГРНИП;
    }
    
    /// <summary>
    /// Получить СНИЛС для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>СНИЛС доверителя.</returns>
    public virtual string GetEntrepreneurPrincipalInilaV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДоверСвДоверит principal)
    {
      var entrepreneurPrincipal = PoAV2PrincipalItemConverter.GetAsEntrepreneur(principal);
      return entrepreneurPrincipal?.СНИЛС;
    }
    
    #endregion
    
    #region Доверитель передоверия - ИП
    
    /// <summary>
    /// Заполнить НОР для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillEntrepreneurPrincipalBusinessUnitV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументПередовСвЛицПередПолн principal)
    {
      var tin = this.GetEntrepreneurPrincipalTinV2(principal);
      var psrn = this.GetEntrepreneurPrincipalPsrnV2(principal);
      
      _obj.BusinessUnit = this.GetEntrepreneurPrincipalBusinessUnit(tin, psrn);
    }
    
    /// <summary>
    /// Получить ИНН для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ИНН доверителя.</returns>
    public virtual string GetEntrepreneurPrincipalTinV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументПередовСвЛицПередПолн principal)
    {
      var entrepreneurPrincipal = RetrustV2PrincipalItemConverter.GetAsEntrepreneur(principal);
      return entrepreneurPrincipal?.ИННФЛ;
    }
    
    /// <summary>
    /// Получить ОГРН для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ОГРН доверителя.</returns>
    public virtual string GetEntrepreneurPrincipalPsrnV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументПередовСвЛицПередПолн principal)
    {
      var entrepreneurPrincipal = RetrustV2PrincipalItemConverter.GetAsEntrepreneur(principal);
      return entrepreneurPrincipal?.ОГРНИП;
    }
    
    #endregion
    
    #region Доверитель - физическое лицо
    
    /// <summary>
    /// Заполнить НОР для доверителя - физического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillIndividualPrincipalBusinessUnitV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДоверСвДоверит principal)
    {
      var tin = this.GetIndividualPrincipalTinV2(principal);
      var inila = this.GetIndividualPrincipalInilaV2(principal);
      
      var signatory = this.GetPrincipalSignatory(tin, inila);
      var department = signatory != null && signatory.Department != null && signatory.Department.Status == CoreEntities.DatabookEntry.Status.Active
        ? signatory.Department
        : null;
      _obj.BusinessUnit = department != null && department.BusinessUnit != null && department.BusinessUnit.Status == CoreEntities.DatabookEntry.Status.Active
        ? department.BusinessUnit
        : null;
    }
    
    /// <summary>
    /// Заполнить подписанта для доверителя - физического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillIndividualPrincipalOurSignatoryV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДоверСвДоверит principal)
    {
      var tin = this.GetIndividualPrincipalTinV2(principal);
      var inila = this.GetIndividualPrincipalInilaV2(principal);

      _obj.OurSignatory = this.GetPrincipalSignatory(tin, inila);
    }
    
    /// <summary>
    /// Получить ИНН для доверителя - физического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ИНН доверителя.</returns>
    public virtual string GetIndividualPrincipalTinV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДоверСвДоверит principal)
    {
      var individualPrincipal = PoAV2PrincipalItemConverter.GetAsIndividual(principal);
      return individualPrincipal?.ИННФЛ;
    }
    
    /// <summary>
    /// Получить СНИЛС для доверителя - физического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>СНИЛС доверителя.</returns>
    public virtual string GetIndividualPrincipalInilaV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументДоверСвДоверит principal)
    {
      var individualPrincipal = PoAV2PrincipalItemConverter.GetAsIndividual(principal);
      return individualPrincipal?.СНИЛС;
    }
    
    #endregion
    
    #region Доверитель передоверия - физическое лицо
    
    /// <summary>
    /// Заполнить НОР для доверителя - физического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillIndividualPrincipalBusinessUnitV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументПередовСвЛицПередПолн principal)
    {
      var tin = this.GetIndividualPrincipalTinV2(principal);
      var inila = this.GetIndividualPrincipalInilaV2(principal);
      
      var signatory = this.GetPrincipalSignatory(tin, inila);
      var department = signatory != null && signatory.Department != null && signatory.Department.Status == CoreEntities.DatabookEntry.Status.Active
        ? signatory.Department
        : null;
      _obj.BusinessUnit = department != null && department.BusinessUnit != null && department.BusinessUnit.Status == CoreEntities.DatabookEntry.Status.Active
        ? department.BusinessUnit
        : null;
    }

    /// <summary>
    /// Заполнить подписанта для доверителя - физического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillIndividualPrincipalOurSignatoryV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументПередовСвЛицПередПолн principal)
    {
      var tin = this.GetIndividualPrincipalTinV2(principal);
      var inila = this.GetIndividualPrincipalInilaV2(principal);

      _obj.OurSignatory = this.GetPrincipalSignatory(tin, inila);
    }
    
    /// <summary>
    /// Получить ИНН для доверителя - физического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ИНН доверителя.</returns>
    public virtual string GetIndividualPrincipalTinV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументПередовСвЛицПередПолн principal)
    {
      var individualPrincipal = RetrustV2PrincipalItemConverter.GetAsIndividual(principal);
      return individualPrincipal?.ИННФЛ;
    }
    
    /// <summary>
    /// Получить СНИЛС для доверителя - физического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>СНИЛС доверителя.</returns>
    public virtual string GetIndividualPrincipalInilaV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументПередовСвЛицПередПолн principal)
    {
      var individualPrincipal = RetrustV2PrincipalItemConverter.GetAsIndividual(principal);
      return individualPrincipal?.СНИЛС;
    }
    
    #endregion
    
    #endregion
    
    #region Заполнение вкладки На основании
    
    /// <summary>
    /// Заполнить вкладку На основании.
    /// </summary>
    /// <param name="retrust">Модель главного узла доверенности со сведениями о передоверии.</param>
    public virtual void FillRetrustMainPoAV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументПередов retrust)
    {
      var unifiedNumber = retrust.СвДовПер?.СвОснДовер?.НомДовер0;
      var mainPoA = FormalizedPowerOfAttorneys.GetAll(f => f.UnifiedRegistrationNumber == unifiedNumber).FirstOrDefault();
      if (mainPoA != null)
        _obj.MainPoA = mainPoA;
      else
      {
        _obj.MainPoAUnifiedNumber = retrust?.СвДовПер?.СвОснДовер?.НомДовер0;
        _obj.MainPoAPrincipal = this.GetMainPoAPrincipalV2(retrust);
      }
    }
    
    /// <summary>
    /// Получить доверителя корневой доверенности.
    /// </summary>
    /// <param name="retrust">Модель главного узла доверенности со сведениями о передоверии.</param>
    /// <returns>Доверитель корневой доверенности.</returns>
    private ICounterparty GetMainPoAPrincipalV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументПередов retrust)
    {
      var mainPoAInfo = retrust?.СвДовПер?.СвОснДовер?.СвДовер0.FirstOrDefault();
      var mainPoAPrincipal = mainPoAInfo?.Item as Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.СвРосОргТип;
      if (mainPoAPrincipal != null)
      {
        var counterparties = Parties.Companies.GetAll(c => c.TIN == mainPoAPrincipal.ИННЮЛ &&
                                                      c.TRRC == mainPoAPrincipal.КПП &&
                                                      c.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        if (counterparties.Count() == 1)
          return counterparties.FirstOrDefault();
      }
      return null;
    }
    
    #endregion
    
    /// <summary>
    /// Заполнить полномочия доверенности из десериализованного объекта.
    /// </summary>
    /// <param name="peredov">Модель главного узла доверенности со сведениями о передоверии.</param>
    public virtual void FillImportedPowersV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.ДоверенностьДокументПередов peredov)
    {
      _obj.Powers = string.Join(string.Empty, peredov.СвПолн.Select(x => x.Item));
    }
    
    #region Заполнение представителя(ей) V2
    
    /// <summary>
    /// Заполнить раздел представителя.
    /// </summary>
    /// <param name="fpoa">Десериализованный объект доверенности.</param>
    public virtual void FillRepresentativeV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.Доверенность fpoa)
    {
      var isMainPoa = Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.PoAV2Builder.IsMainPoA(fpoa);
      
      var representatives = isMainPoa
        ? Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.PoAV2Builder.GetDover(fpoa).СвУпПред
        : Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.PoAV2Builder.GetPeredov(fpoa).СвЛицПолучПолн;
      
      this.FillRepresentativesFromListV2(representatives);
    }
    
    /// <summary>
    /// Заполнить представителей из списка.
    /// </summary>
    /// <param name="representatives">Десериализованный список представителей.</param>
    private void FillRepresentativesFromListV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.СвУпПредТип[] representatives)
    {
      if (representatives.Count() == 0)
        return;

      _obj.IsManyRepresentatives = representatives.Count() > 1;
      
      if (representatives.Count() == 1)
        this.SetRepresentativeToMainPropertiesV2(representatives[0]);
      else
        for (var i = 0; i < representatives.Count(); i++)
          this.AddRepresentativeToTableV2(representatives[i]);
    }
    
    #region Заполнение представителя на основной вкладке
    
    /// <summary>
    /// Заполнить представителя на основной вкладке.
    /// </summary>
    /// <param name="representative">Десериализованный представитель.</param>
    private void SetRepresentativeToMainPropertiesV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.СвУпПредТип representative)
    {
      _obj.AgentType = ExtractAgentTypeV2(representative);
      
      if (_obj.AgentType == Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Person)
        this.FixAgentTypeIfEmployeeV2(representative);
      
      if (_obj.AgentType == Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Employee)
      {
        var individual = AgentItemConverter.GetAsIndividual(representative);
        var info = Structures.FormalizedPowerOfAttorney.IssuedToInfo.Create(null, individual.ИННФЛ, individual.СНИЛС);
        this.FillIssuedTo(info);
      }
      else
      {
        _obj.IssuedToParty = this.ComputeIssuedToV2(representative, _obj.AgentType);
        _obj.Representative = this.ComputeAgentIfRequiredV2(representative, _obj.AgentType);
      }
    }
    
    /// <summary>
    /// Исправить Физ. Лицо, если найден Сотрудник.
    /// </summary>
    /// <param name="representative">Десериализованный представитель.</param>
    private void FixAgentTypeIfEmployeeV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.СвУпПредТип representative)
    {
      var individual = AgentItemConverter.GetAsIndividual(representative);
      var employee = GetEmployee(individual.ИННФЛ, individual.СНИЛС, null);
      
      if (!Equals(employee, Company.Employees.Null))
        _obj.AgentType = Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Employee;
    }
    
    #endregion
    
    #region Заполнение представителя в таблице
    
    /// <summary>
    /// Добавить представителя в табличную часть.
    /// </summary>
    /// <param name="representative">Десериализованный представитель.</param>
    private void AddRepresentativeToTableV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.СвУпПредТип representative)
    {
      var newRow = _obj.Representatives.AddNew();
      
      newRow.AgentType = ExtractAgentTypeV2(representative);
      newRow.IssuedTo = this.ComputeIssuedToV2(representative, newRow.AgentType);
      newRow.Agent = this.ComputeAgentIfRequiredV2(representative, newRow.AgentType);
    }
    
    #endregion
    
    #region Заполнение представителя - общие методы
    
    /// <summary>
    /// Извлечь тип представителя.
    /// </summary>
    /// <param name="representative">Десериализованный представитель.</param>
    /// <returns>Тип представителя.</returns>
    private static Enumeration ExtractAgentTypeV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.СвУпПредТип representative)
    {
      if (representative.ТипПред ==
          PoAV2Enums.AgentTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.AgentType.LegalEntity))
      {
        return Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.LegalEntity;
      }
      
      if (representative.ТипПред ==
          PoAV2Enums.AgentTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.AgentType.Entrepreneur))
      {
        return Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Entrepreneur;
      }
      
      return Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Person;
    }
    
    /// <summary>
    /// Вычислить Кому переданы полномочия.
    /// </summary>
    /// <param name="representative">Десериализованный представитель.</param>
    /// <param name="agentType">Тип представителя.</param>
    /// <returns>Представитель: ЮЛ, ИП, ФЛ.</returns>
    private ICounterparty ComputeIssuedToV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.СвУпПредТип representative,
                                            Enumeration? agentType)
    {
      if (agentType == Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Person)
      {
        var individual = AgentItemConverter.GetAsIndividual(representative);
        return this.GetPersonRepresentative(individual.ИННФЛ, individual.СНИЛС);
      }
      
      if (agentType == Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.LegalEntity)
      {
        var legalEntity = AgentItemConverter.GetAsLegalEntity(representative);
        return this.GetLegalEntityRepresentative(legalEntity.СвОрг.ИННЮЛ, legalEntity.СвОрг.КПП);
      }
      
      var entrepreneur = AgentItemConverter.GetAsEntrepreneur(representative);
      return this.GetEnterpreneurRepresentative(entrepreneur.ИННФЛ, entrepreneur.ОГРНИП);
    }
    
    /// <summary>
    /// Вычислить контактное лицо ЮЛ или ИП.
    /// </summary>
    /// <param name="representative">Десериализованный представитель.</param>
    /// <param name="agentType">Тип представителя.</param>
    /// <returns>Персона.</returns>
    private IPerson ComputeAgentIfRequiredV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.СвУпПредТип representative,
                                             Enumeration? agentType)
    {
      if (agentType == Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Entrepreneur)
      {
        var entrepreneur = AgentItemConverter.GetAsEntrepreneur(representative);
        return this.GetPersonRepresentative(entrepreneur.ИННФЛ, entrepreneur.СНИЛС);
      }
      
      if (agentType == Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.LegalEntity)
      {
        var legalEntity = AgentItemConverter.GetAsLegalEntity(representative);
        return this.GetPersonRepresentative(legalEntity.СвФЛ[0].ИННФЛ, legalEntity.СвФЛ[0].СНИЛС);
      }
      return null;
    }
    
    #endregion
    
    #endregion
    
    /// <summary>
    /// Заполнить имя эл. доверенности.
    /// </summary>
    /// <param name="fpoa">Десериализованный объект доверенности.</param>
    public virtual void FillDocumentNameV2(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV2.Доверенность fpoa)
    {
      this.SetDefaultDocumentName();
    }
    
    #endregion
    
    #region Генерация МЧД V3
    
    #region Доверенность
    
    /// <summary>
    /// Создать тело эл. доверенности, формат 003.
    /// </summary>
    /// <param name="unifiedRegistrationNumber">Единый регистрационный номер доверенности.</param>
    /// <returns>Тело эл. доверенности.</returns>
    public virtual Docflow.Structures.Module.IByteArray CreateFormalizedPowerOfAttorneyXmlV3(Guid unifiedRegistrationNumber)
    {
      var poa = PoAV3Builder.CreateEmptyPoA();
      poa.ИдФайл = Constants.FormalizedPowerOfAttorney.FPoAV3FilePrefix + this.GetFileIdAttribute(unifiedRegistrationNumber);
      
      this.FillPowersV3(poa);
      this.FillPoaInfoV3(poa, unifiedRegistrationNumber);
      this.AddLegalEntityPrincipalV3(poa);
      this.AddAgentV3(poa);
      
      var xml = Sungero.FormalizeDocumentsParser.Extension.GetPowerOfAttorneyXmlV3(poa);
      return Docflow.Structures.Module.ByteArray.Create(xml);
    }
    
    /// <summary>
    /// Заполнить полномочия доверенности.
    /// </summary>
    /// <param name="poa">Эл. доверенность 003 формата.</param>
    public virtual void FillPowersV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.Доверенность poa)
    {
      var dover = PoAV3Builder.Довер(poa);
      dover.СвПолн.ПрСовмПолн = PoAV3Enums.JointPowersToNative(JointPowers.Individual);
      if (_obj.DelegationType != Sungero.Docflow.FormalizedPowerOfAttorney.DelegationType.NoDelegation)
      {
        dover.СвПолн.ПрУтрПолн = PoAV3Enums.PowersLostTypeToNative(PowersLostType.PowersNotLost);
        dover.СвПолн.ПрУтрПолнSpecified = true;
      }
      
      if (_obj.PowersType == Sungero.Docflow.FormalizedPowerOfAttorney.PowersType.Classifier)
        this.FillStructuredPowersV3(dover);
      
      if (_obj.PowersType == Sungero.Docflow.FormalizedPowerOfAttorney.PowersType.FreeForm)
        this.FillTextPowersV3(dover);
    }
    
    /// <summary>
    /// Заполнить полномочия в доверенности по классификатору.
    /// </summary>
    /// <param name="poaContent">Атрибут xml хранящий содержимое доверенности.</param>
    public virtual void FillStructuredPowersV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДовер poaContent)
    {
      if (poaContent == null)
        return;
      this.FillStructuredPowersInternalV3(poaContent.СвПолн);
    }
    
    /// <summary>
    /// Заполнить полномочия в доверенности в свободной форме.
    /// </summary>
    /// <param name="poaContent">Атрибут xml хранящий содержимое доверенности.</param>
    public virtual void FillTextPowersV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДовер poaContent)
    {
      if (poaContent == null)
        return;
      this.FillTextPowersInternalV3(poaContent.СвПолн);
    }
    
    /// <summary>
    /// Заполнить основные сведения доверенности.
    /// </summary>
    /// <param name="poa">Эл. доверенность 003 формата.</param>
    /// <param name="unifiedRegistrationNumber">Единый регистрационный номер доверенности.</param>
    public virtual void FillPoaInfoV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.Доверенность poa, Guid unifiedRegistrationNumber)
    {
      var info = PoAV3Builder.Довер(poa).СвДов;
      info.НомДовер = unifiedRegistrationNumber.ToString();
      
      if (_obj.ValidFrom.HasValue)
        info.ДатаВыдДовер = _obj.ValidFrom.Value;
      
      if (_obj.ValidTill.HasValue)
        info.СрокДейст = _obj.ValidTill.Value;
      
      var retrustValue = _obj.DelegationType == Sungero.Docflow.FormalizedPowerOfAttorney.DelegationType.WithDelegation
        ? Retrust.FreeRetrust
        : Retrust.NoRetrust;
      
      info.ПрПередов = PoAV3Enums.RetrustToNative(retrustValue);
      
      info.ВнНомДовер = _obj.RegistrationNumber;
      
      info.ВидДовер = PoAV3Enums.RevocableToNative(true);
      info.СведСист = $"{Constants.FormalizedPowerOfAttorney.XmlGeneralInfoAttributeValues.SourceSystemInfo}{unifiedRegistrationNumber}";
    }
    
    /// <summary>
    /// Добавить представителя - ИП.
    /// </summary>
    /// <param name="poa">Эл. доверенность 003 формата.</param>
    public virtual void AddEntrepreneurAgentV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.Доверенность poa)
    {
      var entrepreneurAgents = _obj.Representatives.Where(r => r.AgentType == Docflow.PowerOfAttorneyBase.AgentType.Entrepreneur);
      foreach (var entrepreneurAgent in entrepreneurAgents)
        this.AddEntrepreneurAgentV3(poa, entrepreneurAgent.IssuedTo, entrepreneurAgent.Agent);
    }

    /// <summary>
    /// Добавить представителя - ИП.
    /// </summary>
    /// <param name="poa">Эл. доверенность 003 формата.</param>
    /// <param name="issuedTo">Кому выдана.</param>
    /// <param name="representative">Представитель.</param>
    protected void AddEntrepreneurAgentV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.Доверенность poa,
                                               ICounterparty issuedTo, IPerson representative)
    {
      var agent = PoAV3Builder.CreateEntrepreneurAgent(poa);
      agent.ОГРНИП = issuedTo?.PSRN;
      
      if (representative != null)
      {
        agent.СведФЛ.ФИО.Имя = representative.FirstName;
        agent.СведФЛ.ФИО.Отчество = representative.MiddleName;
        agent.СведФЛ.ФИО.Фамилия = representative.LastName;
        agent.СНИЛС = Parties.PublicFunctions.Person.GetFormattedInila(representative);
        agent.ИННФЛ = representative.TIN;
        
        if (representative.DateOfBirth.HasValue)
          agent.СведФЛ.ДатаРожд = representative.DateOfBirth.Value;
        
        agent.СведФЛ.ДатаРождSpecified = true;
        agent.СведФЛ.КонтактТлф = representative.Phones;
        var flag = this.GetCitizenshipFlag(representative.Citizenship);
        agent.СведФЛ.ПрГражд = PoAV3Enums.CitizenshipFlagToNative(flag);
        agent.СведФЛ.Гражданство = flag == CitizenshipFlag.Other ? representative.Citizenship.Code : null;
      }
      
      this.AddIdentificationV3(agent.СведФЛ.УдЛичнФЛ, representative);
    }
    
    /// <summary>
    /// Добавить представителя - юр. лицо.
    /// </summary>
    /// <param name="poa">Эл. доверенность 003 формата.</param>
    public virtual void AddLegalEntityAgentV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.Доверенность poa)
    {
      var legalEntityAgents = _obj.Representatives.Where(r => r.AgentType == Docflow.PowerOfAttorneyBase.AgentType.LegalEntity);
      foreach (var legalEntityAgent in legalEntityAgents)
        this.AddLegalEntityAgentV3(poa, legalEntityAgent.IssuedTo);
    }
    
    /// <summary>
    /// Добавить представителя - юр. лицо.
    /// </summary>
    /// <param name="poa">Эл. доверенность 003 формата.</param>
    /// <param name="issuedTo">Кому выдана.</param>
    protected void AddLegalEntityAgentV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.Доверенность poa,
                                              ICounterparty issuedTo)
    {
      var agent = PoAV3Builder.CreateLegalEntityAgent(poa);
      
      if (issuedTo != null)
      {
        agent.НаимОрг = issuedTo.Name;
        agent.ОГРН = issuedTo.PSRN;
        
        if (CompanyBases.Is(issuedTo))
          agent.КПП = CompanyBases.As(issuedTo).TRRC;
        
        agent.ИННЮЛ = issuedTo.TIN;
        
        if (issuedTo.LegalAddress != null && issuedTo.Region?.Code != null)
        {
          agent.АдрРег.Item = issuedTo.LegalAddress;
          agent.АдрРег.Регион = issuedTo.Region?.Code;
        }
      }
    }
    
    #endregion
    
    #region Передоверие
    
    /// <summary>
    /// Создать тело эл. доверенности "В рамках передоверия", формат 003.
    /// </summary>
    /// <param name="unifiedRegistrationNumber">Единый регистрационный номер доверенности.</param>
    /// <returns>Тело эл. доверенности.</returns>
    public virtual Docflow.Structures.Module.IByteArray CreateRetrustXmlV3(Guid unifiedRegistrationNumber)
    {
      var poa = PoAV3Builder.CreateEmptyRetrust();
      poa.ИдФайл = Constants.FormalizedPowerOfAttorney.FPoAV3FilePrefix + this.GetFileIdAttribute(unifiedRegistrationNumber);
      
      this.FillMainPoAInfoForRetrustV3(poa);
      this.FillRetrustInfoV3(poa, unifiedRegistrationNumber);
      this.AddLegalEntityPrincipalV3(poa);
      this.AddAgentV3(poa);
      this.FillPowersForRetrustV3(poa);
      
      var xml = Sungero.FormalizeDocumentsParser.Extension.GetPowerOfAttorneyXmlV3(poa);
      return Docflow.Structures.Module.ByteArray.Create(xml);
    }
    
    /// <summary>
    /// Заполнить информацию о корневой доверенности для передоверия (формат 003).
    /// </summary>
    /// <param name="poa">Эл. доверенность.</param>
    private void FillMainPoAInfoForRetrustV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.Доверенность poa)
    {
      var retrust = PoAV3Builder.Передов(poa);
      
      retrust.СвПервДовер.НомДоверПерв = _obj.MainPoAUnifiedNumber;
      retrust.СвПервДовер.ВнНомДоверПерв = _obj.MainPoARegistrationNumber;
      
      if (_obj.MainPoAValidFrom.HasValue)
        retrust.СвПервДовер.ДатаВыдДовер = _obj.MainPoAValidFrom.Value;
      if (_obj.MainPoAValidTill.HasValue)
        retrust.СвПервДовер.СрокДейст = _obj.MainPoAValidTill.Value;
      
      var principal = PoAV3Builder.CreateMainPoAPrincipalForRetrust(poa);
      principal.НаимОрг = _obj.MainPoAPrincipal.Name;
      principal.ИННЮЛ = _obj.MainPoAPrincipal.TIN;
      principal.ОГРН = _obj.MainPoAPrincipal.PSRN;
      principal.АдрРег.Регион = _obj.MainPoAPrincipal.Region?.Code;
      principal.АдрРег.Item = _obj.MainPoAPrincipal.LegalAddress;
      principal.КПП = Companies.As(_obj.MainPoAPrincipal)?.TRRC;
      
    }
    
    /// <summary>
    /// Заполнить информацию о передоверии (формат 003).
    /// </summary>
    /// <param name="poa">Эл. доверенность.</param>
    /// <param name="unifiedRegistrationNumber">Уникальный номер доверенности.</param>
    private void FillRetrustInfoV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.Доверенность poa, Guid unifiedRegistrationNumber)
    {
      var retrust = PoAV3Builder.Передов(poa);
      
      retrust.СвПереДовер.НомДовер = unifiedRegistrationNumber.ToString();
      retrust.СвПереДовер.ВнНомДовер = _obj.RegistrationNumber;
      
      if (_obj.ValidFrom.HasValue)
        retrust.СвПереДовер.ДатаВыдДовер = _obj.ValidFrom.Value;
      if (_obj.ValidTill.HasValue)
        retrust.СвПереДовер.СрокДейст = _obj.ValidTill.Value;
      
      retrust.СвПереДовер.СведСист = $"{Constants.FormalizedPowerOfAttorney.XmlGeneralInfoAttributeValues.SourceSystemInfo}{unifiedRegistrationNumber}";
    }
    
    /// <summary>
    /// Заполнить информацию о полномочиях передоверия (формат 003).
    /// </summary>
    /// <param name="poa">Эл. доверенность.</param>
    private void FillPowersForRetrustV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.Доверенность poa)
    {
      var retrust = PoAV3Builder.Передов(poa);
      retrust.СвПолн.ПрСовмПолн = PoAV3Enums.JointPowersToNative(JointPowers.Individual);
      
      if (_obj.PowersType == Sungero.Docflow.FormalizedPowerOfAttorney.PowersType.Classifier)
        this.FillStructuredPowersInternalV3(retrust.СвПолн);
      
      if (_obj.PowersType == Sungero.Docflow.FormalizedPowerOfAttorney.PowersType.FreeForm)
        this.FillTextPowersInternalV3(retrust.СвПолн);
    }

    #endregion
    
    #region Общая часть
    
    /// <summary>
    /// Добавить доверителя - юридическое лицо.
    /// </summary>
    /// <param name="poa">Эл. доверенность 003 формата.</param>
    public virtual void AddLegalEntityPrincipalV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.Доверенность poa)
    {
      var lep = PoAV3Builder.CreateLegalEntityPrincipal(poa);
      
      // Заполнение элемента "Сведения о российском юридическом лице (СвРосОрг)".
      if (_obj.BusinessUnit != null)
      {
        var org = lep.СвРосОрг;
        org.НаимОрг = _obj.BusinessUnit.LegalName;
        org.ИННЮЛ = _obj.BusinessUnit.TIN;
        org.ОГРН = _obj.BusinessUnit.PSRN;
        org.КПП = _obj.BusinessUnit.TRRC;
        org.АдрРег.Item = _obj.BusinessUnit.LegalAddress;
        org.АдрРег.Регион = _obj.BusinessUnit.Region?.Code;
      }
      
      var head = lep.ЛицоБезДов[0].СвФЛ;
      // Заполнение элемента "Сведения о лице, действующем от имени юридического лица без доверенности (ЛицоБезДов)".
      if (_obj.BusinessUnit.CEO != null && _obj.BusinessUnit.CEO.Person != null)
      {
        
        head.ИННФЛ = _obj.BusinessUnit.CEO.Person.TIN;
        head.СНИЛС = Parties.PublicFunctions.Person.GetFormattedInila(_obj.BusinessUnit.CEO.Person);
        
        if (_obj.BusinessUnit.CEO.Person.DateOfBirth.HasValue)
        {
          head.СведФЛ.ДатаРожд = _obj.BusinessUnit.CEO.Person.DateOfBirth.Value;
          head.СведФЛ.ДатаРождSpecified = true;
        }
        
        if (_obj.BusinessUnit.CEO.JobTitle != null)
          head.Должность = _obj.BusinessUnit.CEO.JobTitle.Name;
      }

      // TODO Сделать признак наличия гражданства для иностранцев.
      head.СведФЛ.ПрГражд = PoAV3Enums.CitizenshipFlagToNative(CitizenshipFlag.Russia);
      
      // Заполнение элемента "Сведения о физическом лице, подписывающем доверенность от имени доверителя (Подписант)".
      if (_obj.OurSignatory != null)
      {
        head.СведФЛ.ФИО.Имя = _obj.OurSignatory.Person.FirstName;
        head.СведФЛ.ФИО.Фамилия = _obj.OurSignatory.Person.LastName;
        head.СведФЛ.ФИО.Отчество = _obj.OurSignatory.Person.MiddleName;
      }
    }
    
    /// <summary>
    /// Добавить представителя.
    /// </summary>
    /// <param name="poa">Эл. доверенность 003 формата.</param>
    public virtual void AddAgentV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.Доверенность poa)
    {
      var individualAgents = _obj.Representatives.Where(r => r.AgentType == Docflow.PowerOfAttorneyBaseRepresentatives.AgentType.Person);
      foreach (var individualAgent in individualAgents)
        this.AddIndividualAgentV3(poa, Sungero.Parties.People.As(individualAgent.IssuedTo));

      this.AddLegalEntityAgentV3(poa);

      this.AddEntrepreneurAgentV3(poa);
    }
    
    /// <summary>
    /// Добавить представителя - физ. лицо.
    /// </summary>
    /// <param name="poa">Эл. доверенность 003 формата.</param>
    /// <param name="person">Кому выдана.</param>
    public virtual void AddIndividualAgentV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.Доверенность poa, Sungero.Parties.IPerson person)
    {
      var agent = PoAV3Builder.CreateIndividualAgent(poa);
      agent.ИННФЛ = person.TIN;
      agent.СНИЛС = Parties.PublicFunctions.Person.GetFormattedInila(person);
      agent.СведФЛ.ФИО.Имя = person.FirstName;
      agent.СведФЛ.ФИО.Фамилия = person.LastName;
      agent.СведФЛ.ФИО.Отчество = person.MiddleName;
      
      agent.СведФЛ.ДатаРожд = person.DateOfBirth.Value;
      agent.СведФЛ.ДатаРождSpecified = true;
      var flag = this.GetCitizenshipFlag(person.Citizenship);
      agent.СведФЛ.ПрГражд = PoAV3Enums.CitizenshipFlagToNative(flag);
      agent.СведФЛ.Гражданство = flag == CitizenshipFlag.Other ? person.Citizenship.Code : null;
      
      this.AddIdentificationV3(agent.СведФЛ.УдЛичнФЛ, person);
    }
    
    /// <summary>
    /// Добавить документ, удостоверяющий личность, для физ. лица.
    /// </summary>
    /// <param name="identityDocument">Сведения о документе, удостоверяющем личность.</param>
    /// <param name="person">Физ. лицо.</param>
    public virtual void AddIdentificationV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.СведФЛТипУдЛичнФЛ identityDocument, Sungero.Parties.IPerson person)
    {
      identityDocument.КодВидДок = person.IdentityKind.Code;
      identityDocument.ДатаДок = person.IdentityDateOfIssue.Value;
      identityDocument.ВыдДок =
        person.IdentityAuthority.Length <= PublicConstants.FormalizedPowerOfAttorney.IdentityAuthorityMaxLength ?
        person.IdentityAuthority :
        person.IdentityAuthority.Substring(0, PublicConstants.FormalizedPowerOfAttorney.IdentityAuthorityMaxLength);
      identityDocument.КодВыдДок = person.IdentityAuthorityCode;
      identityDocument.СерНомДок = string.Join("-", new[] { person.IdentitySeries, person.IdentityNumber }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
    
    /// <summary>
    /// Заполнить информацию о полномочиях из списка (формат 003).
    /// </summary>
    /// <param name="powersElement">Полномочия.</param>
    private void FillStructuredPowersInternalV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.СвПолнТип powersElement)
    {
      powersElement.ТипПолн = PoAV3Enums.PowersTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.PowersType.MachineReadable);
      
      var powers = _obj.StructuredPowers.Select(p => p.Power).Distinct().ToList();
      powersElement.МашПолн = powers.Select(p => new СвПолнТипМашПолн { НаимПолн = p.Name, КодПолн = p.Code }).ToArray();
    }
    
    /// <summary>
    /// Заполнить информацию о полномочиях в свободной форме (формат 003).
    /// </summary>
    /// <param name="powersElement">Полномочия.</param>
    private void FillTextPowersInternalV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.СвПолнТип powersElement)
    {
      powersElement.ТипПолн = PoAV3Enums.PowersTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.PowersType.HumanReadable);
      powersElement.ТекстПолн = _obj.Powers;
    }
    
    #endregion
    
    #endregion
    
    #region Заполнение карточки МЧД V3
    
    /// <summary>
    /// Заполнить поля доверенности версии 003 из десериализованного объекта.
    /// </summary>
    /// <param name="xml">Тело доверенности.</param>
    [Public]
    public virtual void FillFPoAV3(Docflow.Structures.Module.IByteArray xml)
    {
      var poa = Sungero.FormalizeDocumentsParser.Extension.DeserializePoAV3(xml.Bytes);

      this.FillDocumentNameV3(poa);
      _obj.FormatVersion = FormatVersion.Version003;
      _obj.IsNotarized = this.IsNotarized(poa);
      
      if (PoAV3Builder.IsMainPoA(poa))
      {
        var dover = PoAV3Builder.Довер(poa);
        this.FillFPoAV3(dover);
      }
      else
      {
        var peredov = PoAV3Builder.Передов(poa);
        this.FillFPoAV3(peredov);
      }

      this.FillRepresentativeV3(poa);
    }
    
    /// <summary>
    /// Заполнить поля доверенности из десериализованного объекта.
    /// </summary>
    /// <param name="dover">Модель главного узла доверенности.</param>
    public virtual void FillFPoAV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДовер dover)
    {
      _obj.UnifiedRegistrationNumber = GetUniformGuid(dover.СвДов.НомДовер);
      _obj.ValidFrom = dover.СвДов.ДатаВыдДовер;
      _obj.ValidTill = dover.СвДов.СрокДейст;
      _obj.DelegationType = this.GetDelegationTypeV3(dover);
      
      var registrationDate = dover.СвДов.ДатаВнРегДоверSpecified ? dover.СвДов.ДатаВнРегДовер : Calendar.Today;
      this.FillRegistrationData(dover.СвДов.ВнНомДовер, registrationDate);

      if (dover?.СвДоверит?.Count() == 1)
        this.FillPrincipalV3(dover.СвДоверит.FirstOrDefault());
      this.FillImportedPowersV3(dover);
    }

    /// <summary>
    /// Заполнить поля доверенности из десериализованного объекта.
    /// </summary>
    /// <param name="peredov">Модель главного узла доверенности со сведениями о передоверии.</param>
    public virtual void FillFPoAV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередов peredov)
    {
      _obj.UnifiedRegistrationNumber = GetUniformGuid(peredov.СвПереДовер.НомДовер);
      var registrationDate = peredov.СвПереДовер.ДатаВнРегДоверSpecified ? peredov.СвПереДовер.ДатаВнРегДовер : Calendar.Today;
      this.FillRegistrationData(peredov.СвПереДовер.ВнНомДовер, registrationDate);
      
      _obj.DelegationType = this.GetDelegationTypeV3(peredov);
      _obj.IsDelegated = true;

      this.FillImportedPowersV3(peredov);
      
      if (peredov?.СвПередПолн?.Count() == 1)
        this.FillPrincipalV3(peredov?.СвПередПолн.FirstOrDefault());

      _obj.ValidFrom = peredov.СвПереДовер.ДатаВыдДовер;
      _obj.ValidTill = peredov.СвПереДовер.СрокДейст;
      
      this.FillRetrustMainPoAV3(peredov);
    }
    
    #region Заполнение доверителя V3
    
    /// <summary>
    /// Заполнить поля доверителя.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillPrincipalV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      if (principal == null)
        return;
      
      var principalType = principal.ТипДоверит;
      if (principalType == PoAV3Enums.PrincipalTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.PrincipalType.LegalEntity))
      {
        this.FillLegalEntityPrincipalBusinessUnitV3(principal);
        this.FillLegalEntityPrincipalOurSignatoryV3(principal);
      }
      if (principalType == PoAV3Enums.PrincipalTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.PrincipalType.Entrepreneur))
      {
        this.FillEntrepreneurPrincipalBusinessUnitV3(principal);
        this.FillEntrepreneurPrincipalOurSignatoryV3(principal);
      }
      if (principalType == PoAV3Enums.PrincipalTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.PrincipalType.Individual))
      {
        this.FillIndividualPrincipalBusinessUnitV3(principal);
        this.FillIndividualPrincipalOurSignatoryV3(principal);
      }
    }

    /// <summary>
    /// Заполнить поля доверителя для передоверия.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillPrincipalV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      if (principal == null)
        return;
      
      var principalType = principal.ТипПерПолн;
      
      if (principalType == RetrustV3Enums.PrincipalTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.PrincipalType.LegalEntity))
      {
        this.FillLegalEntityPrincipalBusinessUnitV3(principal);
        this.FillLegalEntityPrincipalOurSignatoryV3(principal);
        return;
      }
      
      if (principalType == RetrustV3Enums.PrincipalTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.PrincipalType.Entrepreneur))
      {
        this.FillEntrepreneurPrincipalBusinessUnitV3(principal);
        this.FillEntrepreneurPrincipalOurSignatoryV3(principal);
        return;
      }
      
      if (principalType == RetrustV3Enums.PrincipalTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.PrincipalType.Individual))
      {
        this.FillIndividualPrincipalBusinessUnitV3(principal);
        this.FillIndividualPrincipalOurSignatoryV3(principal);
      }
    }
    
    #region Доверитель - юридическое лицо
    
    /// <summary>
    /// Заполнить НОР для доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillLegalEntityPrincipalBusinessUnitV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      var tin = this.GetLegalEntityPrincipalTinV3(principal);
      var trrc = this.GetLegalEntityPrincipalTrrcV3(principal);
      _obj.BusinessUnit = this.GetLegalEntityPrincipalBusinessUnit(tin, trrc);
    }
    
    /// <summary>
    /// Получить ИНН для организации доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ИНН доверителя.</returns>
    public virtual string GetLegalEntityPrincipalTinV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      return principal?.Доверит?.РосОргДовер?.СвРосОрг?.ИННЮЛ;
    }
    
    /// <summary>
    /// Получить КПП для организации доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>КПП доверителя.</returns>
    public virtual string GetLegalEntityPrincipalTrrcV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      return principal?.Доверит?.РосОргДовер?.СвРосОрг?.КПП;
    }
    
    /// <summary>
    /// Заполнить подписанта для доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillLegalEntityPrincipalOurSignatoryV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      var tin = this.GetLegalEntitySignatoryTinV3(principal);
      var inila = this.GetLegalEntitySignatoryInilaV3(principal);
      
      _obj.OurSignatory = this.GetPrincipalSignatory(tin, inila);
    }
    
    /// <summary>
    /// Получить ИНН сотрудника-подписанта для доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ИНН подписанта.</returns>
    public virtual string GetLegalEntitySignatoryTinV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      var legalPrincipal = this.GetLegalPrincipalRepresentativeV3(principal);
      return legalPrincipal?.СвФЛ?.ИННФЛ;
    }
    
    /// <summary>
    /// Получить СНИЛС сотрудника-подписанта для доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>СНИЛС подписанта.</returns>
    public virtual string GetLegalEntitySignatoryInilaV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      var legalPrincipal = this.GetLegalPrincipalRepresentativeV3(principal);
      return legalPrincipal?.СвФЛ?.СНИЛС;
    }
    
    /// <summary>
    /// Получить сведения о представителе доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>Сведения о представителе.</returns>
    public virtual Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ЛицоБезДовТип GetLegalPrincipalRepresentativeV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      var allPrincipals = principal?.Доверит?.РосОргДовер?.ЛицоБезДов;
      var count = allPrincipals != null ? allPrincipals.Count() : 0;
      if (count != 1)
      {
        Logger.DebugFormat("GetLegalPrincipalRepresentativeV3. Cannot get legal principal representative. All principals count = {0}.", count);
        return null;
      }
      
      return allPrincipals.FirstOrDefault();
    }
    
    #endregion
    
    #region Доверитель передоверия - юридическое лицо
    
    /// <summary>
    /// Заполнить НОР для доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillLegalEntityPrincipalBusinessUnitV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      var tin = this.GetLegalEntityPrincipalTinV3(principal);
      var trrc = this.GetLegalEntityPrincipalTrrcV3(principal);
      _obj.BusinessUnit = this.GetLegalEntityPrincipalBusinessUnit(tin, trrc);
    }
    
    /// <summary>
    /// Получить ИНН для организации доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ИНН доверителя.</returns>
    public virtual string GetLegalEntityPrincipalTinV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      return principal?.ПередПолн?.РосОргПерПолн?.СвРосОрг?.ИННЮЛ;
    }
    
    /// <summary>
    /// Получить КПП для организации доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>КПП доверителя.</returns>
    public virtual string GetLegalEntityPrincipalTrrcV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      return principal?.ПередПолн?.РосОргПерПолн?.СвРосОрг?.КПП;
    }
    
    /// <summary>
    /// Заполнить подписанта для доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillLegalEntityPrincipalOurSignatoryV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      var tin = this.GetLegalEntitySignatoryTinV3(principal);
      var inila = this.GetLegalEntitySignatoryInilaV3(principal);
      
      _obj.OurSignatory = this.GetPrincipalSignatory(tin, inila);
    }
    
    /// <summary>
    /// Получить ИНН сотрудника-подписанта для доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ИНН подписанта.</returns>
    public virtual string GetLegalEntitySignatoryTinV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      var legalPrincipal = this.GetLegalPrincipalRepresentativeV3(principal);
      return legalPrincipal?.СвФЛ?.ИННФЛ;
    }
    
    /// <summary>
    /// Получить СНИЛС сотрудника-подписанта для доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>СНИЛС подписанта.</returns>
    public virtual string GetLegalEntitySignatoryInilaV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      var legalPrincipal = this.GetLegalPrincipalRepresentativeV3(principal);
      return legalPrincipal?.СвФЛ?.СНИЛС;
    }
    
    /// <summary>
    /// Получить сведения о представителе доверителя - юридического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>Сведения о представителе.</returns>
    public virtual Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ЛицоБезДовТип GetLegalPrincipalRepresentativeV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      var allPrincipals = principal?.ПередПолн?.РосОргПерПолн?.ЛицоБезДов;
      var count = allPrincipals != null ? allPrincipals.Count() : 0;
      if (count != 1)
      {
        Logger.DebugFormat("GetLegalPrincipalRepresentativeV3. Cannot get legal principal representative. All principals count = {0}.", count);
        return null;
      }
      
      return allPrincipals.FirstOrDefault();
    }
    
    #endregion
    
    #region Доверитель - ИП
    
    /// <summary>
    /// Заполнить НОР для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillEntrepreneurPrincipalBusinessUnitV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      var tin = this.GetEntrepreneurPrincipalTinV3(principal);
      var psrn = this.GetEntrepreneurPrincipalPsrnV3(principal);
      
      _obj.BusinessUnit = this.GetEntrepreneurPrincipalBusinessUnit(tin, psrn);
    }
    
    /// <summary>
    /// Заполнить подписанта для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillEntrepreneurPrincipalOurSignatoryV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      var tin = this.GetEntrepreneurPrincipalTinV3(principal);
      var inila = this.GetEntrepreneurPrincipalInilaV3(principal);
      
      _obj.OurSignatory = this.GetPrincipalSignatory(tin, inila);
    }
    
    /// <summary>
    /// Получить ИНН для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ИНН доверителя.</returns>
    public virtual string GetEntrepreneurPrincipalTinV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      return principal?.Доверит?.ИПДовер?.ИННФЛ;
    }
    
    /// <summary>
    /// Получить ОГРН для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ОГРН доверителя.</returns>
    public virtual string GetEntrepreneurPrincipalPsrnV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      return principal?.Доверит?.ИПДовер?.ОГРНИП;
    }
    
    /// <summary>
    /// Получить СНИЛС для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>СНИЛС доверителя.</returns>
    public virtual string GetEntrepreneurPrincipalInilaV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      return principal?.Доверит?.ИПДовер?.СНИЛС;
    }
    
    #endregion
    
    #region Доверитель передоверия - ИП
    
    /// <summary>
    /// Заполнить НОР для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillEntrepreneurPrincipalBusinessUnitV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      var tin = this.GetEntrepreneurPrincipalTinV3(principal);
      var psrn = this.GetEntrepreneurPrincipalPsrnV3(principal);
      
      _obj.BusinessUnit = this.GetEntrepreneurPrincipalBusinessUnit(tin, psrn);
    }
    
    /// <summary>
    /// Заполнить подписанта для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillEntrepreneurPrincipalOurSignatoryV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      var tin = this.GetEntrepreneurPrincipalTinV3(principal);
      var inila = this.GetEntrepreneurPrincipalInilaV3(principal);
      
      _obj.OurSignatory = this.GetPrincipalSignatory(tin, inila);
    }
    
    /// <summary>
    /// Получить ИНН для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ИНН доверителя.</returns>
    public virtual string GetEntrepreneurPrincipalTinV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      return principal?.ПередПолн?.ИППерПолн?.ИННФЛ;
    }
    
    /// <summary>
    /// Получить ОГРН для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ОГРН доверителя.</returns>
    public virtual string GetEntrepreneurPrincipalPsrnV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      return principal?.ПередПолн?.ИППерПолн?.ОГРНИП;
    }
    
    /// <summary>
    /// Получить СНИЛС для доверителя - ИП.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>СНИЛС доверителя.</returns>
    public virtual string GetEntrepreneurPrincipalInilaV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      return principal?.ПередПолн?.ИППерПолн?.СНИЛС;
    }
    
    #endregion
    
    #region Доверитель - физическое лицо
    
    /// <summary>
    /// Заполнить НОР для доверителя - физического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillIndividualPrincipalBusinessUnitV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      var tin = this.GetIndividualPrincipalTinV3(principal);
      var inila = this.GetIndividualPrincipalInilaV3(principal);
      
      var signatory = this.GetPrincipalSignatory(tin, inila);
      var department = signatory != null && signatory.Department != null && signatory.Department.Status == CoreEntities.DatabookEntry.Status.Active
        ? signatory.Department
        : null;
      _obj.BusinessUnit = department != null && department.BusinessUnit != null && department.BusinessUnit.Status == CoreEntities.DatabookEntry.Status.Active
        ? department.BusinessUnit
        : null;
    }
    
    /// <summary>
    /// Заполнить подписанта для доверителя - физического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillIndividualPrincipalOurSignatoryV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      var tin = this.GetIndividualPrincipalTinV3(principal);
      var inila = this.GetIndividualPrincipalInilaV3(principal);

      _obj.OurSignatory = this.GetPrincipalSignatory(tin, inila);
    }
    
    /// <summary>
    /// Получить ИНН для доверителя - физического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ИНН доверителя.</returns>
    public virtual string GetIndividualPrincipalTinV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      return principal?.Доверит?.ФЛДовер?.ИННФЛ;
    }
    
    /// <summary>
    /// Получить СНИЛС для доверителя - физического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>СНИЛС доверителя.</returns>
    public virtual string GetIndividualPrincipalInilaV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДоверСвДоверит principal)
    {
      return principal?.Доверит?.ФЛДовер?.СНИЛС;
    }
    
    #endregion
    
    #region Доверитель передоверия - физическое лицо
    
    /// <summary>
    /// Заполнить НОР для доверителя - физического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillIndividualPrincipalBusinessUnitV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      var tin = this.GetIndividualPrincipalTinV3(principal);
      var inila = this.GetIndividualPrincipalInilaV3(principal);
      
      var signatory = this.GetPrincipalSignatory(tin, inila);
      var department = signatory != null && signatory.Department != null && signatory.Department.Status == CoreEntities.DatabookEntry.Status.Active
        ? signatory.Department
        : null;
      _obj.BusinessUnit = department != null && department.BusinessUnit != null && department.BusinessUnit.Status == CoreEntities.DatabookEntry.Status.Active
        ? department.BusinessUnit
        : null;
    }
    
    /// <summary>
    /// Заполнить подписанта для доверителя - физического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    public virtual void FillIndividualPrincipalOurSignatoryV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      var tin = this.GetIndividualPrincipalTinV3(principal);
      var inila = this.GetIndividualPrincipalInilaV3(principal);

      _obj.OurSignatory = this.GetPrincipalSignatory(tin, inila);
    }
    
    /// <summary>
    /// Получить ИНН для доверителя - физического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>ИНН доверителя.</returns>
    public virtual string GetIndividualPrincipalTinV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      return principal?.ПередПолн?.ФЛПерПолн?.ИННФЛ;
    }
    
    /// <summary>
    /// Получить СНИЛС для доверителя - физического лица.
    /// </summary>
    /// <param name="principal">Десериализованный объект доверителя.</param>
    /// <returns>СНИЛС доверителя.</returns>
    public virtual string GetIndividualPrincipalInilaV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередовСвПередПолн principal)
    {
      return principal?.ПередПолн?.ФЛПерПолн?.СНИЛС;
    }
    
    #endregion
    
    #endregion
    
    #region Заполнение вкладки На основании V3
    
    /// <summary>
    /// Заполнить вкладку На основании.
    /// </summary>
    /// <param name="retrust">Модель главного узла доверенности со сведениями о передоверии.</param>
    public virtual void FillRetrustMainPoAV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередов retrust)
    {
      var unifiedNumber = retrust?.СвПервДовер?.НомДоверПерв;
      var mainPoA = FormalizedPowerOfAttorneys.GetAll(f => f.UnifiedRegistrationNumber == unifiedNumber).FirstOrDefault();
      if (mainPoA != null)
        _obj.MainPoA = mainPoA;
      else
      {
        _obj.MainPoAUnifiedNumber = unifiedNumber;
        _obj.MainPoARegistrationNumber = retrust?.СвПервДовер?.ВнНомДоверПерв;
        _obj.MainPoAValidFrom = retrust?.СвПервДовер?.ДатаВыдДовер;
        _obj.MainPoAValidTill = retrust?.СвПервДовер?.СрокДейст;
        _obj.MainPoAPrincipal = this.GetMainPoAPrincipalV3(retrust);
      }
    }
    
    /// <summary>
    /// Получить доверителя корневой доверенности.
    /// </summary>
    /// <param name="retrust">Модель главного узла доверенности со сведениями о передоверии.</param>
    /// <returns>Доверитель корневой доверенности.</returns>
    private ICounterparty GetMainPoAPrincipalV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередов retrust)
    {
      var mainPoAInfo = retrust?.СвПервДовер?.СвДоверПерв.FirstOrDefault();
      var legalEntityInfo = mainPoAInfo?.ДоверитПерв?.РосОргДовер;
      if (legalEntityInfo != null)
      {
        var counterparties = Parties.Companies.GetAll(c => c.TIN == legalEntityInfo.ИННЮЛ &&
                                                      c.TRRC == legalEntityInfo.КПП &&
                                                      c.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        if (counterparties.Count() == 1)
          return counterparties.FirstOrDefault();
      }
      
      var entrepreneurInfo = mainPoAInfo?.ДоверитПерв?.ИПДовер;
      if (entrepreneurInfo != null)
      {
        var entrepreneurs = Parties.Counterparties.GetAll(c => c.TIN == entrepreneurInfo.ИННФЛ &&
                                                          c.PSRN == entrepreneurInfo.ОГРНИП &&
                                                          c.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        if (entrepreneurs.Count() == 1)
          return entrepreneurs.FirstOrDefault();
      }
      
      var individualInfo = mainPoAInfo.ДоверитПерв?.ФЛДовер;
      if (individualInfo != null)
      {
        var individuals = Parties.People.GetAll(c => c.TIN == individualInfo.ИННФЛ && c.INILA == individualInfo.СНИЛС);
        if (individuals.Count() == 1)
          return individuals.FirstOrDefault();
      }
      return null;
    }
    
    #endregion
    
    /// <summary>
    /// Заполнение полей полномочий для доверенности версии 003 из десериализованного объекта.
    /// </summary>
    /// <param name="dover">Модель главного узла доверенности.</param>
    public virtual void FillImportedPowersV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДовер dover)
    {
      _obj.PowersType = this.GetPowersTypeV3(dover);
      this.ParsePowers(dover);
    }

    /// <summary>
    /// Заполнение полей полномочий для доверенности версии 003 из десериализованного объекта.
    /// </summary>
    /// <param name="peredov">Модель главного узла доверенности со сведениями о передоверии.</param>
    public virtual void FillImportedPowersV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередов peredov)
    {
      _obj.PowersType = this.ParsePowersTypes(peredov);
      this.ParsePowers(peredov);
    }

    /// <summary>
    /// Парсинг типа полномочий.
    /// </summary>
    /// <param name="peredov">Передоверие.</param>
    /// <returns>Тип полномочий.</returns>
    public virtual Enumeration ParsePowersTypes(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередов peredov)
    {
      return peredov.СвПолн.ТипПолн == СвПолнТипТипПолн.Item0 ? PowersType.FreeForm : PowersType.Classifier;
    }
    
    /// <summary>
    /// Парсинг типа полномочий.
    /// </summary>
    /// <param name="dover">Доверенность.</param>
    /// <returns>Тип полномочий.</returns>
    public virtual Enumeration GetPowersTypeV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДовер dover)
    {
      return dover.СвПолн.ТипПолн == СвПолнТипТипПолн.Item0 ? PowersType.FreeForm : PowersType.Classifier;
    }

    /// <summary>
    /// Парсинг разрешения на передоверие.
    /// </summary>
    /// <param name="peredov">Передоверие.</param>
    /// <returns>Разрешение на передоверие.</returns>
    public virtual Enumeration GetDelegationTypeV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередов peredov)
    {
      return peredov.СвПереДовер.ПрПередов == PoAV3Enums.RetrustToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.Retrust.NoRetrust)
        ? DelegationType.NoDelegation
        : DelegationType.WithDelegation;
    }
    
    /// <summary>
    /// Парсинг разрешения на передоверие.
    /// </summary>
    /// <param name="dover">Доверенность.</param>
    /// <returns>Разрешение на передоверие.</returns>
    public virtual Enumeration GetDelegationTypeV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДовер dover)
    {
      return dover.СвДов.ПрПередов == PoAV3Enums.RetrustToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.Retrust.NoRetrust)
        ? DelegationType.NoDelegation
        : DelegationType.WithDelegation;
    }
    
    /// <summary>
    /// Парсинг полномочий.
    /// </summary>
    /// <param name="dover">Десериализованная xml-доверенность.</param>
    public virtual void ParsePowers(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДовер dover)
    {
      if (_obj.PowersType == PowersType.FreeForm)
      {
        _obj.Powers = dover.СвПолн.ТекстПолн;
      }
      else
      {
        this.FillImportedStructuredPowersV3(dover);
      }
    }

    /// <summary>
    /// Импорт структурированных полномочий из xml.
    /// </summary>
    /// <param name="dover">Доверенность из xml.</param>
    public virtual void FillImportedStructuredPowersV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументДовер dover)
    {
      if (dover == null) return;
      _obj.StructuredPowers.Clear();
      foreach (var srcMachinePower in dover.СвПолн.МашПолн)
      {
        var powerClass = this.GetPowerClass(srcMachinePower);
        if (powerClass == null) continue;
        
        IFormalizedPowerOfAttorneyStructuredPowers structuredPower = _obj.StructuredPowers.AddNew();
        structuredPower.Power = powerClass;
      }
    }

    /// <summary>
    /// Получить класс полномочия на основе полномочия из xml.
    /// </summary>
    /// <param name="power">Структурированное полномочие из xml.</param>
    /// <returns>Класс полномочия.</returns>
    public virtual IPowerOfAttorneyClassifier GetPowerClass(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.СвПолнТипМашПолн power)
    {
      if (power == null)
      {
        Logger.DebugFormat("GetPowerClass. Input 'power' is null");
        return null;
      }
      IPowerOfAttorneyClassifier powerClass;
      if (string.IsNullOrWhiteSpace(power.МнПолн))
        powerClass = PowerOfAttorneyClassifiers.GetAll()
          .FirstOrDefault(x => x.Code == power.КодПолн && x.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
      else
        powerClass = PowerOfAttorneyClassifiers.GetAll()
          .FirstOrDefault(x => x.Code == power.КодПолн && x.Mnemonic == power.МнПолн &&
                          x.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
      Logger.DebugFormat("GetPowerClass. Input: КодПолн: {0}; МнПолн: {1}. Finded powerClassId: {2}",
                         power.КодПолн, power.МнПолн, powerClass?.Id);
      return powerClass;
    }

    /// <summary>
    /// Парсинг полномочий.
    /// </summary>
    /// <param name="peredov">Десериализованная xml-передоверенность.</param>
    public virtual void ParsePowers(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередов peredov)
    {
      if (_obj.PowersType == PowersType.FreeForm)
      {
        _obj.Powers = peredov.СвПолн.ТекстПолн;
      }
      else
      {
        this.FillImportedStructuredPowersV3(peredov);
      }
    }

    /// <summary>
    /// Импорт структурированных полномочий из xml.
    /// </summary>
    /// <param name="peredov">Передоверие из xml.</param>
    public virtual void FillImportedStructuredPowersV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.ДоверенностьДокументПередов peredov)
    {
      if (peredov == null) return;
      _obj.StructuredPowers.Clear();
      foreach (var srcMachinePower in peredov.СвПолн.МашПолн)
      {
        var powerClass = this.GetPowerClass(srcMachinePower);
        if (powerClass == null) continue;
        
        IFormalizedPowerOfAttorneyStructuredPowers structuredPower = _obj.StructuredPowers.AddNew();
        structuredPower.Power = powerClass;
      }
    }
    
    #region Заполнение представителя(ей) V3
    
    /// <summary>
    /// Заполнить раздел представителя.
    /// </summary>
    /// <param name="fpoa">Десериализованный объект доверенности.</param>
    public virtual void FillRepresentativeV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.Доверенность fpoa)
    {
      var isMainPoa = Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.PoAV3Builder.IsMainPoA(fpoa);
      
      var representatives = isMainPoa
        ? Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.PoAV3Builder.Довер(fpoa).СвУпПред
        : Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.PoAV3Builder.Передов(fpoa).СвПолучПолн;
      
      this.FillRepresentativesFromListV3(representatives);
    }
    
    /// <summary>
    /// Заполнить представителей из списка.
    /// </summary>
    /// <param name="representatives">Десериализованный список представителей.</param>
    private void FillRepresentativesFromListV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.СвУпПредТип[] representatives)
    {
      if (representatives.Count() == 0)
        return;

      _obj.IsManyRepresentatives = representatives.Count() > 1;
      
      if (representatives.Count() == 1)
        this.SetRepresentativeToMainPropertiesV3(representatives[0]);
      else
        for (var i = 0; i < representatives.Count(); i++)
          this.AddRepresentativeToTableV3(representatives[i]);
    }
    
    #region Заполнение представителя на основной вкладке
    
    /// <summary>
    /// Заполнить представителя на основной вкладке.
    /// </summary>
    /// <param name="representative">Десериализованный представитель.</param>
    private void SetRepresentativeToMainPropertiesV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.СвУпПредТип representative)
    {
      _obj.AgentType = ExtractAgentTypeV3(representative);

      if (_obj.AgentType == Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Person)
        this.FixAgentTypeIfEmployeeV3(representative);

      if (_obj.AgentType == Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Employee)
      {
        var individual = representative.Пред.СведФизЛ;
        var info = Structures.FormalizedPowerOfAttorney.IssuedToInfo.Create(null, individual.ИННФЛ, individual.СНИЛС);
        this.FillIssuedTo(info);
      }
      else
      {
        _obj.IssuedToParty = this.ComputeIssuedToV3(representative.Пред, _obj.AgentType);
        _obj.Representative = this.ComputeAgentIfRequiredV3(representative.Пред, _obj.AgentType);
      }
    }
    
    /// <summary>
    /// Исправить Физ. Лицо, если найден Сотрудник.
    /// </summary>
    /// <param name="representative">Десериализованный представитель.</param>
    private void FixAgentTypeIfEmployeeV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.СвУпПредТип representative)
    {
      var individual = representative.Пред.СведФизЛ;
      var employee = GetEmployee(individual.ИННФЛ, individual.СНИЛС, null);

      if (!Equals(employee, Company.Employees.Null))
        _obj.AgentType = Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Employee;
    }
    
    #endregion
    
    #region Заполнение представителя в таблице
    
    /// <summary>
    /// Добавить представителя в табличную часть.
    /// </summary>
    /// <param name="representative">Десериализованный представитель.</param>
    private void AddRepresentativeToTableV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.СвУпПредТип representative)
    {
      var newRow = _obj.Representatives.AddNew();

      newRow.AgentType = ExtractAgentTypeV3(representative);
      newRow.IssuedTo = this.ComputeIssuedToV3(representative.Пред, newRow.AgentType);
      newRow.Agent = this.ComputeAgentIfRequiredV3(representative.Пред, newRow.AgentType);
    }
    
    #endregion
    
    #region Заполнение представителя - общие методы
    
    /// <summary>
    /// Извлечь тип представителя.
    /// </summary>
    /// <param name="representative">Десериализованный представитель.</param>
    /// <returns>Тип представителя.</returns>
    private static Enumeration ExtractAgentTypeV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.СвУпПредТип representative)
    {
      if (representative.ТипПред ==
          PoAV3Enums.AgentTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.AgentType.LegalEntity))
      {
        return Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.LegalEntity;
      }
      
      if (representative.ТипПред ==
          PoAV3Enums.AgentTypeToNative(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAEnums.AgentType.Entrepreneur))
      {
        return Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Entrepreneur;
      }
      
      return Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Person;
    }
    
    /// <summary>
    /// Вычислить Кому переданы полномочия.
    /// </summary>
    /// <param name="representative">Десериализованный представитель.</param>
    /// <param name="agentType">Тип представителя.</param>
    /// <returns>Представитель: ЮЛ, ИП, ФЛ.</returns>
    private ICounterparty ComputeIssuedToV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.СвУпПредТипПред representative,
                                            Enumeration? agentType)
    {
      if (agentType == Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Person)
        return this.GetPersonRepresentative(representative.СведФизЛ.ИННФЛ, representative.СведФизЛ.СНИЛС);
      
      if (agentType == Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.LegalEntity)
        return this.GetLegalEntityRepresentative(representative.СведОрг.ИННЮЛ, representative.СведОрг.КПП);
      
      return this.GetEnterpreneurRepresentative(representative.СведИП.ИННФЛ, representative.СведИП.ОГРНИП);
    }
    
    /// <summary>
    /// Вычислить контактное лицо ЮЛ или ИП.
    /// </summary>
    /// <param name="representative">Десериализованный представитель.</param>
    /// <param name="agentType">Тип представителя.</param>
    /// <returns>Персона.</returns>
    private IPerson ComputeAgentIfRequiredV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.СвУпПредТипПред representative,
                                             Enumeration? agentType)
    {
      return (agentType == Sungero.Docflow.FormalizedPowerOfAttorney.AgentType.Entrepreneur)
        ? this.GetPersonRepresentative(representative.СведИП.ИННФЛ, representative.СведИП.СНИЛС)
        : null;
    }
    
    #endregion
    
    #endregion
    
    /// <summary>
    /// Заполнить имя эл. доверенности.
    /// </summary>
    /// <param name="poa">Десериализованный объект доверенности.</param>
    public virtual void FillDocumentNameV3(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.Доверенность poa)
    {
      this.SetDefaultDocumentName();
    }

    /// <summary>
    /// Является ли доверенность нотариальной.
    /// </summary>
    /// <param name="powerOfAttorney">Доверенность.</param>
    /// <returns>True - Нотариальная, False - Не нотариальная.</returns>
    private bool IsNotarized(Sungero.FormalizeDocumentsParser.PowerOfAttorney.Model.PoAV3.Доверенность powerOfAttorney)
    {
      if (powerOfAttorney == null ||
          powerOfAttorney.ПрЭлФорм == null ||
          powerOfAttorney.ПрЭлФорм.Length <= Constants.FormalizedPowerOfAttorney.IsNotarizedSignPosition)
        return false;
      
      var poaNotarizedSign = powerOfAttorney.ПрЭлФорм.Substring(Constants.FormalizedPowerOfAttorney.IsNotarizedSignPosition, 1);
      return poaNotarizedSign == Constants.FormalizedPowerOfAttorney.IsNotarizedSignValue;
    }
    
    #endregion

    #region Заполнение карточки МЧД (V2, V3). Получение данных
    
    #region Получение НОР для доверителя
    
    /// <summary>
    /// Получить НОР для доверителя - юридического лица.
    /// </summary>
    /// <param name="tin">ИНН доверителя.</param>
    /// <param name="trrc">КПП доверителя.</param>
    /// <returns>НОР доверителя.</returns>
    public virtual Company.IBusinessUnit GetLegalEntityPrincipalBusinessUnit(string tin, string trrc)
    {
      if (string.IsNullOrWhiteSpace(tin) || string.IsNullOrWhiteSpace(trrc))
      {
        Logger.DebugFormat("GetLegalEntityPrincipalBusinessUnit. Cannot find business unit. Tin or Trrc is empty (Tin = '{0}' Trrc = '{1}').", tin, trrc);
        return null;
      }
      
      var businessUnits = Company.PublicFunctions.BusinessUnit.GetBusinessUnits(tin, trrc);
      var count = businessUnits.Count();
      var result = count == 1 ? businessUnits.FirstOrDefault() : null;
      Logger.DebugFormat("GetLegalEntityPrincipalBusinessUnit. Business units count = {0} for Tin = '{1}' and Trrc = '{2}'. Selected business unit {3} (Id = {4})",
                         count, tin, trrc, result?.Name, result?.Id);
      return result;
    }
    
    /// <summary>
    /// Получить НОР для доверителя - ИП.
    /// </summary>
    /// <param name="tin">ИНН доверителя.</param>
    /// <param name="psrn">ОГРН доверителя.</param>
    /// <returns>НОР доверителя.</returns>
    public virtual Company.IBusinessUnit GetEntrepreneurPrincipalBusinessUnit(string tin, string psrn)
    {
      if (string.IsNullOrWhiteSpace(tin) || string.IsNullOrWhiteSpace(psrn))
      {
        Logger.DebugFormat("GetEntrepreneurPrincipalBusinessUnit. Cannot find business unit. Tin or Psrn is empty (Tin = '{0}' Psrn = '{1}').", tin, psrn);
        return null;
      }
      
      var businessUnits = Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnits().Where(x => x.TIN == tin && x.PSRN == psrn).ToList();
      var count = businessUnits.Count();
      var result = count == 1 ? businessUnits.FirstOrDefault() : null;
      Logger.DebugFormat("GetEntrepreneurPrincipalBusinessUnit. Business units count = {0} for Tin = '{1}' and Psrn = '{2}'. Selected business unit {3} (Id = {4})",
                         count, tin, psrn, result?.Name, result?.Id);
      return result;
    }
    
    #endregion
    
    #region Получение контрагента для представителя
    
    /// <summary>
    /// Получить контрагента для представителя ЮЛ.
    /// </summary>
    /// <param name="tin">ИНН представителя.</param>
    /// <param name="trrc">КПП представителя.</param>
    /// <returns>Контрагент представителя.</returns>
    protected virtual ICounterparty GetLegalEntityRepresentative(string tin, string trrc)
    {
      if (string.IsNullOrWhiteSpace(tin) || string.IsNullOrWhiteSpace(trrc))
      {
        Logger.DebugFormat("GetLegalEntityRepresentative. Cannot find counterparty. Tin or Trrc is empty (Tin = '{0}' Trrc = '{1}').", tin, trrc);
        return null;
      }
      
      var counterpartiesQuery = Counterparties.GetAll()
        .Where(x => Equals(x.TIN, tin) &&
               Equals(Parties.Companies.As(x).TRRC, trrc) &&
               Equals(x.Status, Sungero.Parties.Counterparty.Status.Active));
      
      var count = counterpartiesQuery.Count();
      var result = count == 1 ? counterpartiesQuery.FirstOrDefault() : null;
      Logger.DebugFormat("GetLegalEntityRepresentative. Counterparties count = {0} for Tin = '{1}' and Trrc = '{2}'. Selected counterparty {3} (Id = {4})",
                         count, tin, trrc, result?.Name, result?.Id);
      return result;
    }
    
    /// <summary>
    /// Получить контрагента для представителя ИП.
    /// </summary>
    /// <param name="tin">ИНН представителя.</param>
    /// <param name="psrn">ОГРН представителя.</param>
    /// <returns>Контрагент представителя.</returns>
    protected virtual ICounterparty GetEnterpreneurRepresentative(string tin, string psrn)
    {
      if (string.IsNullOrWhiteSpace(tin) || string.IsNullOrWhiteSpace(psrn))
      {
        Logger.DebugFormat("GetEnterpreneurRepresentative. Cannot find counterparty. Tin or Psrn is empty (Tin = '{0}' Psrn = '{1}').", tin, psrn);
        return null;
      }
      
      var counterpartiesQuery = Counterparties.GetAll()
        .Where(x => Equals(x.TIN, tin) &&
               Equals(x.PSRN, psrn) &&
               Equals(x.Status, Sungero.Parties.Counterparty.Status.Active));
      
      var count = counterpartiesQuery.Count();
      var result = count == 1 ? counterpartiesQuery.FirstOrDefault() : null;
      Logger.DebugFormat("GetEnterpreneurRepresentative. Counterparties count = {0} for Tin = '{1}' and Psrn = '{2}'. Selected counterparty {3} (Id = {4})",
                         count, tin, psrn, result?.Name, result?.Id);
      return result;
    }
    
    /// <summary>
    /// Получить персону для представителя ФЛ.
    /// </summary>
    /// <param name="tin">ИНН представителя.</param>
    /// <param name="inila">СНИЛС представителя.</param>
    /// <returns>Персона представителя.</returns>
    protected virtual IPerson GetPersonRepresentative(string tin, string inila)
    {
      if (string.IsNullOrWhiteSpace(tin) || string.IsNullOrWhiteSpace(inila))
      {
        Logger.DebugFormat("GetPersonRepresentative. Cannot find person. Tin or Inila is empty (Tin = '{0}' Inila = '{1}').", tin, inila);
        return null;
      }
      
      var people = People.GetAll().Where(x => Equals(x.TIN, tin));
      var count = people.Count();
      
      var result = count == 1 ? people.First() : null;
      Logger.DebugFormat("GetPersonRepresentative. People count = {0} for Tin = '{1}'. Selected person {2} (Id = {3})", count, tin, result?.Name, result?.Id);
      
      if (count == 1)
        return result;
      
      Logger.DebugFormat("GetPersonRepresentative. Try to find the person by Inila.");
      
      var peopleWithInila = People.GetAll().Where(x => x.INILA != null && x.INILA != string.Empty);
      
      var matchedPeople = new List<IPerson>();
      var clearedInila = Sungero.Parties.PublicFunctions.Person.RemoveInilaSpecialSymbols(inila);
      
      foreach (var person in peopleWithInila)
      {
        var clearedPersonInila = Sungero.Parties.PublicFunctions.Person.RemoveInilaSpecialSymbols(person.INILA);
        if (clearedPersonInila == clearedInila)
          matchedPeople.Add(person);
      }
      
      count = matchedPeople.Count;
      result = count == 1 ? matchedPeople.First() : null;
      Logger.DebugFormat("GetPersonRepresentative. People count = {0} for Inila = '{1}'. Selected person {2} (Id = {3})", count, inila, result?.Name, result?.Id);
      return result;
    }
    
    #endregion
    
    /// <summary>
    /// Получить сотрудника-подписанта для доверителя.
    /// </summary>
    /// <param name="tin">ИНН сотрудника.</param>
    /// <param name="inila">СНИЛС сотрудника.</param>
    /// <returns>Подписант для доверителя.</returns>
    public virtual Company.IEmployee GetPrincipalSignatory(string tin, string inila)
    {
      if (string.IsNullOrWhiteSpace(tin) || string.IsNullOrWhiteSpace(inila))
      {
        Logger.DebugFormat("GetPrincipalSignatory. Cannot find employee. Tin or Inila is empty (Tin = '{0}' Inila = '{1}').", tin, inila);
        return null;
      }
      
      var employees = Company.PublicFunctions.Employee.Remote.GetEmployeesByTIN(tin).Intersect(Company.PublicFunctions.Employee.Remote.GetEmployeesByINILA(inila)).ToList();
      var count = employees.Count();
      var employee = count == 1 ? employees.FirstOrDefault() : null;
      Logger.DebugFormat("GetPrincipalSignatory. Employees count = {0} for Tin = '{1}' and Inila = '{2}'. Selected employee {3} (Id = {4})",
                         count, tin, inila, employee?.Name, employee?.Id);
      return employee;
    }
    
    #endregion
    
    #region Используется в SetFPoARevocationState
    
    /// <summary>
    /// Отзыв дочерних эл. доверенностей после успешной регистрации заявления на отзыв в ФНС.
    /// </summary>
    /// <param name="withLeadFormalizedPoA">Включить в список обрабатываемых доверенностей ведущую доверенность.</param>
    public virtual void RevocateChildFormalizedPowerOfAttorneys(bool withLeadFormalizedPoA)
    {
      var formalizedPoAs = Functions.FormalizedPowerOfAttorney.GetActiveChildPowerOfAttorneys(_obj);
      if (withLeadFormalizedPoA)
        formalizedPoAs.Add(_obj);
      
      var businessUnitIds = formalizedPoAs.Select(x => x.BusinessUnit.Id).Distinct().ToList();
      foreach (var businessUnitId in businessUnitIds)
      {
        var batchGuid = Guid.NewGuid().ToString();
        var formalizedPoAIds = formalizedPoAs.Where(x => x.BusinessUnit.Id == businessUnitId).Select(x => x.Id).ToList();
        Functions.Module.CreateFormalizedPoAQueueItemBatch(formalizedPoAIds, batchGuid);
        
        Functions.Module.ExecuteSyncFormalizedPoAWithService(batchGuid, businessUnitId, string.Empty);
        
        Logger.DebugFormat("RevocateChildFormalizedPowerOfAttorneys. FormalizedPoABatchGuid: '{0}'.", batchGuid);
      }
    }
    
    /// <summary>
    /// Получить список действующих дочерних доверенностей.
    /// </summary>
    /// <returns>Список дочерних доверенностей.</returns>
    public virtual List<IFormalizedPowerOfAttorney> GetActiveChildPowerOfAttorneys()
    {
      return FormalizedPowerOfAttorneys.GetAll()
        .Where(x => x.LifeCycleState == Docflow.FormalizedPowerOfAttorney.LifeCycleState.Active &&
               x.BusinessUnit != null && x.MainPoAUnifiedNumber == _obj.UnifiedRegistrationNumber).ToList();
    }
    
    #endregion
  }
}
