using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Person;

namespace Sungero.Parties.Shared
{
  partial class PersonFunctions
  {
    
    #region Проверка дублей
    
    /// <summary>
    /// Получить дубли персоны.
    /// </summary>
    /// <param name="excludeClosed">Исключить закрытые записи.</param>
    /// <returns>Дубли персоны.</returns>
    public override List<ICounterparty> GetDuplicates(bool excludeClosed)
    {
      // TODO Dmitriev_IA: На 53202, 69259 добавить поиск по имени.
      //                   По умолчанию для Person ищем по ИНН.
      return Functions.Module.Remote.GetDuplicateCounterparties(_obj.TIN, string.Empty, string.Empty, _obj.Id, excludeClosed)
        .Where(x => People.Is(x))
        .ToList();
    }
    
    #endregion
    
    /// <summary>
    /// Получить форматированный СНИЛС.
    /// </summary>
    /// <returns>Форматированный СНИЛС, если не удалось отформатировать СНИЛС из персоны.</returns>
    [Public]
    public virtual string GetFormattedInila()
    {
      if (string.IsNullOrEmpty(_obj.INILA))
        return string.Empty;
      
      var regexForDigit = @"^\d+$";
      var regexInilaFormat = @"(\d{3})(\d{3})(\d{3})(\d{2})";
      var trimmedInila = RemoveInilaSpecialSymbols(_obj.INILA);
      
      if (!string.IsNullOrEmpty(_obj.INILA) && Regex.IsMatch(trimmedInila, regexForDigit) && trimmedInila.Length == 11)
        return Regex.Replace(trimmedInila, regexInilaFormat, "$1-$2-$3 $4");
      
      return _obj.INILA;
    }
    
    /// <summary>
    /// Очистить СНИЛС от пробелов, дефисов и тире.
    /// </summary>
    /// <param name="inila">СНИЛС.</param>
    /// <returns>СНИЛС без пробелов, дефисов и тире.</returns>
    [Public]
    public static string RemoveInilaSpecialSymbols(string inila)
    {
      return inila.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("—", string.Empty);
    }
    
    /// <summary>
    /// Получить пол персоны в соответствии с фамилией, именем, отчеством.
    /// </summary>
    /// <returns>Пол персоны.</returns>
    /// <remarks>Определение пола поддерживается только для русской локали.</remarks>
    [Public]
    public virtual Enumeration? GetGender()
    {
      Enumeration? personSex = null;
      
      // Определение пола поддерживается только для русской локали.
      if (System.Threading.Thread.CurrentThread.CurrentUICulture.Equals(System.Globalization.CultureInfo.CreateSpecificCulture("ru-RU")))
      {
        // Уточнить пол по отчеству.
        if (!string.IsNullOrEmpty(_obj.MiddleName))
        {
          var middleName = _obj.MiddleName.Trim().ToLower();
          if (middleName.EndsWith("ич") || middleName.EndsWith("оглы"))
            personSex = Sex.Male;

          if (middleName.EndsWith("на") || middleName.EndsWith("кызы"))
            personSex = Sex.Female;
        }
        
        // Уточнить пол по фамилии.
        if (!personSex.HasValue && !string.IsNullOrEmpty(_obj.LastName))
        {
          var lastName = _obj.LastName.Trim().ToLower();
          personSex = lastName.EndsWith("ва") || lastName.EndsWith("на") || lastName.EndsWith("ая") ? Sex.Female : Sex.Male;
        }
      }
      
      return personSex;
    }

    /// <summary>
    /// Задать ФИО в соответствии с фамилией, именем и отчеством, а также Фамилию и инициалы.
    /// </summary>
    public void FillName()
    {
      if (string.IsNullOrEmpty(_obj.FirstName) || string.IsNullOrEmpty(_obj.LastName))
        return;

      using (TenantInfo.Culture.SwitchTo())
      {
        if (string.IsNullOrEmpty(_obj.MiddleName))
          _obj.Name = People.Resources.FullNameWithoutMiddleFormat(_obj.FirstName, _obj.LastName);
        else
          _obj.Name = People.Resources.FullNameFormat(_obj.FirstName, _obj.MiddleName, _obj.LastName);
      }
      
      // Короткое наименование для отчетов.
      _obj.ShortName = PublicFunctions.Module.GetSurnameAndInitialsInTenantCulture(_obj.FirstName, _obj.MiddleName, _obj.LastName);
    }
    
    /// <summary>
    /// Получить ФИО в указанном падеже.
    /// </summary>
    /// <param name="declensionCase">Падеж.</param>
    /// <returns>ФИО.</returns>
    [Public]
    public virtual string GetFullName(Sungero.Core.DeclensionCase declensionCase)
    {
      var gender = CommonLibrary.Gender.NotDefined;
      if (_obj.Sex != null)
        gender = _obj.Sex == Sungero.Parties.Person.Sex.Female ?
          CommonLibrary.Gender.Feminine :
          CommonLibrary.Gender.Masculine;
      
      // Для фамилий типа Ардо (Иванова) неправильно склоняется через API. Баг 32895.
      var fullName = CommonLibrary.PersonFullName.Create(_obj.LastName, _obj.FirstName, _obj.MiddleName);
      var fullNameInDeclension = CommonLibrary.Padeg.ConvertPersonFullNameToTargetDeclension(fullName,
                                                                                             (CommonLibrary.DeclensionCase)(int)declensionCase,
                                                                                             gender);
      
      var middleName = string.IsNullOrWhiteSpace(_obj.MiddleName) ? string.Empty : fullNameInDeclension.MiddleName;
      using (TenantInfo.Culture.SwitchTo())
        return Sungero.Parties.People.Resources.FullNameFormat(fullNameInDeclension.FirstName, middleName, fullNameInDeclension.LastName, "\u00A0");
    }
    
    /// <summary>
    /// Преобразует всю строку в строчные буквы, первый символ в прописную букву.
    /// </summary>
    /// <param name="source">Исходная строка.</param>
    /// <returns>Скорректированная строка.</returns>
    public static string SetUppercaseFirstLetter(string source)
    {
      return string.Format("{0}{1}", source.Substring(0, 1).ToUpper(), source.Substring(1).ToLower());
    }
    
    /// <summary>
    /// Проверка ИНН на валидность.
    /// </summary>
    /// <param name="tin">Строка с ИНН.</param>
    /// <returns>Текст ошибки. Пустая строка для верного ИНН.</returns>
    public override string CheckTin(string tin)
    {
      return Functions.Counterparty.CheckTin(tin, false);
    }
    
    /// <summary>
    /// Получить имя персоны в формате Фамилия И.О. в нужном падеже.
    /// </summary>
    /// <param name="declension">Падеж.</param>
    /// <returns>Имя персоны.</returns>
    [Public]
    public virtual string GetLastNameAndInitials(Sungero.Core.DeclensionCase declension)
    {
      var personName = CommonLibrary.PersonFullName.Create(_obj.LastName, _obj.FirstName, _obj.MiddleName, CommonLibrary.PersonFullNameDisplayFormat.LastNameAndInitials);
      Sungero.Core.Enumeration? sex = _obj.Sex != null ? _obj.Sex : this.GetGender();
      Sungero.Core.Gender? gender = null;
      if (sex.HasValue)
        gender = sex == Parties.Person.Sex.Male ? Sungero.Core.Gender.Masculine : Sungero.Core.Gender.Feminine;
      if (gender.HasValue)
        return CaseConverter.ConvertPersonFullNameToTargetDeclension(personName, declension, gender.Value);
      return CaseConverter.ConvertPersonFullNameToTargetDeclension(personName, declension);
    }
    
    /// <summary>
    /// Установить обязательность свойств документа, удостоверяющего личность.
    /// </summary>
    public virtual void SetRequiredIdentityProperties()
    {
      if (!Functions.Person.CheckCanEditIdentityDocuments(_obj))
        return;
      
      _obj.State.Properties.IdentityNumber.IsRequired = _obj.IdentityKind != null;
      _obj.State.Properties.IdentityDateOfIssue.IsRequired = _obj.IdentityKind != null;
      _obj.State.Properties.IdentityAuthority.IsRequired = _obj.IdentityKind != null;
      
      _obj.State.Properties.IdentitySeries.IsRequired = _obj.IdentityKind?.SpecifyIdentitySeries == true;
      _obj.State.Properties.IdentityExpirationDate.IsRequired = _obj.IdentityKind?.SpecifyIdentityExpirationDate == true;
      _obj.State.Properties.IdentityAuthorityCode.IsRequired = _obj.IdentityKind?.SpecifyIdentityAuthorityCode == true;
      _obj.State.Properties.BirthPlace.IsRequired = _obj.IdentityKind?.SpecifyBirthPlace == true;
    }
    
    /// <summary>
    /// Установить доступность свойств документа, удостоверяющего личность.
    /// </summary>
    public virtual void ChangeIdentityPropertiesAccess()
    {
      if (!Functions.Person.CheckCanEditIdentityDocuments(_obj))
        return;
      
      _obj.State.Properties.IdentityNumber.IsEnabled = _obj.IdentityKind != null;
      _obj.State.Properties.IdentityDateOfIssue.IsEnabled = _obj.IdentityKind != null;
      _obj.State.Properties.IdentityAuthority.IsEnabled = _obj.IdentityKind != null;
      
      _obj.State.Properties.IdentitySeries.IsEnabled = _obj.IdentityKind?.SpecifyIdentitySeries == true;
      _obj.State.Properties.IdentityExpirationDate.IsEnabled = _obj.IdentityKind?.SpecifyIdentityExpirationDate == true;
      _obj.State.Properties.IdentityAuthorityCode.IsEnabled = _obj.IdentityKind?.SpecifyIdentityAuthorityCode == true;
      _obj.State.Properties.BirthPlace.IsEnabled = _obj.IdentityKind?.SpecifyBirthPlace == true;
    }
    
    /// <summary>
    /// Очищает данные свойств документа, удостоверяющего личность, если они стали недоступными для выбранного вида документа.
    /// </summary>
    /// <param name="identityKind">Вид документа, удостоверяющего личность.</param>
    public virtual void CleanDisabledIdentityProperties(IIdentityDocumentKind identityKind)
    {
      if (!Functions.Person.CheckCanEditIdentityDocuments(_obj))
        return;
      
      if (identityKind == null)
      {
        _obj.IdentityNumber = null;
        _obj.IdentityDateOfIssue = null;
        _obj.IdentityAuthority = null;
      }
      
      if (identityKind?.SpecifyIdentitySeries != true)
        _obj.IdentitySeries = null;
      
      if (identityKind?.SpecifyIdentityExpirationDate != true)
        _obj.IdentityExpirationDate = null;
      
      if (identityKind?.SpecifyIdentityAuthorityCode != true)
        _obj.IdentityAuthorityCode = null;
      
      if (identityKind?.SpecifyBirthPlace != true)
        _obj.BirthPlace = null;
    }
    
    /// <summary>
    /// Проверяет, есть ли у текущего пользователя право на редактирование документов, удостоверяющих личность.
    /// </summary>
    /// <returns>True, если у пользователя есть права на редактирование документов, удостоверяющих личность.</returns>
    public virtual bool CheckCanEditIdentityDocuments()
    {
      var personParams = ((Domain.Shared.IExtendedEntity)_obj).Params;
      if (personParams.ContainsKey(Sungero.Parties.Constants.Person.AllowEditIdentityDocumentsParamName))
        return (bool)personParams[Sungero.Parties.Constants.Person.AllowEditIdentityDocumentsParamName];
      
      var canEditIdentity = Functions.Module.Remote.CanEditIdentityDocuments();
      personParams.Add(Sungero.Parties.Constants.Person.AllowEditIdentityDocumentsParamName, canEditIdentity);
      return canEditIdentity;
    }
    
    /// <summary>
    /// Проверка свойств документа, удостоверяющего личность перед сохранением.
    /// </summary>
    /// <returns>Список ошибок.</returns>
    public virtual System.Collections.Generic.Dictionary<Sungero.Domain.Shared.IPropertyInfo, string> CheckIdentityProperties()
    {
      var result = new System.Collections.Generic.Dictionary<Sungero.Domain.Shared.IPropertyInfo, string>();
      
      if (!Functions.Person.CheckCanEditIdentityDocuments(_obj) || _obj.IdentityKind == null)
        return result;
      
      var numberError = this.GetIdentityNumberFormatValidationError(_obj.IdentityNumber);
      if (!string.IsNullOrEmpty(numberError))
        result.Add(_obj.Info.Properties.IdentityNumber, numberError);
      
      var seriesError = this.GetIdentitySeriesFormatValidationError(_obj.IdentitySeries);
      if (!string.IsNullOrEmpty(seriesError))
        result.Add(_obj.Info.Properties.IdentitySeries, seriesError);
      
      var authorityCodeError = this.GetIdentityAuthorityCodeFormatValidationError(_obj.IdentityAuthorityCode);
      if (!string.IsNullOrEmpty(authorityCodeError))
        result.Add(_obj.Info.Properties.IdentityAuthorityCode, authorityCodeError);
      
      if (_obj.IdentityDateOfIssue.HasValue && _obj.IdentityDateOfIssue.Value.Date > Calendar.UserToday.Date)
        result.Add(_obj.Info.Properties.IdentityDateOfIssue, Sungero.Parties.People.Resources.IdentityDocumentIssueDateMustBeEarlierThanYesterday);
      
      if (_obj.IdentityDateOfIssue.HasValue && _obj.IdentityExpirationDate.HasValue && _obj.IdentityDateOfIssue.Value.Date >= _obj.IdentityExpirationDate.Value.Date)
        result.Add(_obj.Info.Properties.IdentityExpirationDate, Sungero.Parties.People.Resources.IdentityDocumentExpirationDateMustBeGreaterThanIssueDate);
      
      return result;
    }
    
    /// <summary>
    /// Получить текст ошибки валидации формата номера документа, удостоверяющего личность.
    /// </summary>
    /// <param name="identityNumber">Строка с номером документа.</param>
    /// <returns>Пустая строка, если номер документа заполнен согласно формату, иначе - текст ошибки.</returns>
    public virtual string GetIdentityNumberFormatValidationError(string identityNumber)
    {
      var numberIsValid = this.ValidateIdentityDocumentFieldWithPattern(identityNumber, _obj.IdentityKind.IdentityNumberPattern);
      return numberIsValid ? string.Empty : Sungero.Parties.People.Resources.IdentityNumberNotMatchPattern;
    }
    
    /// <summary>
    /// Получить текст ошибки валидации формата серии документа, удостоверяющего личность.
    /// </summary>
    /// <param name="identitySeries">Строка с серией документа.</param>
    /// <returns>Пустая строка, если серия документа заполнена согласно формату, иначе - текст ошибки.</returns>
    public virtual string GetIdentitySeriesFormatValidationError(string identitySeries)
    {
      var seriesIsValid = this.ValidateIdentityDocumentFieldWithPattern(identitySeries, _obj.IdentityKind.IdentitySeriesPattern);
      return seriesIsValid ? string.Empty : Sungero.Parties.People.Resources.IdentitySeriesNotMatchPattern;
    }
    
    /// <summary>
    /// Получить текст ошибки валидации формата кода подразделения документа, удостоверяющего личность.
    /// </summary>
    /// <param name="identityAuthorityCode">Строка с кодом подразделения.</param>
    /// <returns>Пустая строка, если код подразделения заполнен согласно формату, иначе - текст ошибки.</returns>
    public virtual string GetIdentityAuthorityCodeFormatValidationError(string identityAuthorityCode)
    {
      var codeIsValid = this.ValidateIdentityDocumentFieldWithPattern(identityAuthorityCode, _obj.IdentityKind.IdentityAuthorityCodePattern);
      return codeIsValid ? string.Empty : Sungero.Parties.People.Resources.IdentityAuthorityCodeNotMatchPattern;
    }
    
    /// <summary>
    /// Проверить свойство документа, удостоверяющего личность, на соответствие регулярному выражению.
    /// </summary>
    /// <param name="fieldValue">Проверяемое значение.</param>
    /// <param name="pattern">Регулярное выражение.</param>
    /// <returns>True, если регулярное выражение не задано или если значение свойства соответствует регулярному выражению.</returns>
    public bool ValidateIdentityDocumentFieldWithPattern(string fieldValue, string pattern)
    {
      if (string.IsNullOrEmpty(pattern))
        return true;
      
      return Regex.IsMatch(fieldValue, pattern);
    }
  }
}