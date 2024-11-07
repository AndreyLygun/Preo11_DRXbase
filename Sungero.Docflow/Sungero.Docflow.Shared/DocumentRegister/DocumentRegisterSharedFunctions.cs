using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentRegister;
using FormatElement = Sungero.Docflow.DocumentRegisterNumberFormatItems.Element;

namespace Sungero.Docflow.Shared
{
  partial class DocumentRegisterFunctions
  {
    
    #region Журнал регистрации. Получение периода. Получение списка фильтрованных журналов
    
    /// <summary>
    /// Получить начало действия текущего периода журнала.
    /// </summary>
    /// <param name="registrationDate">Дата.</param>
    /// <returns>Начало периода, null для сквозной нумерации.</returns>
    [Public]
    public DateTime? GetBeginPeriod(DateTime registrationDate)
    {
      var year = this.GetCurrentYear(registrationDate);

      if (_obj.NumberingPeriod == NumberingPeriod.Year)
        return Calendar.GetDate(year, 1, 1);
      
      if (_obj.NumberingPeriod == NumberingPeriod.Quarter)
      {
        var quarter = this.GetCurrentQuarter(registrationDate);
        var firstMonth = ((quarter - 1) * 3) + 1;
        return Calendar.GetDate(year, firstMonth, 1);
      }
      
      if (_obj.NumberingPeriod == NumberingPeriod.Month)
      {
        var month = this.GetCurrentMonth(registrationDate);
        return Calendar.GetDate(year, month, 1);
      }
      
      if (_obj.NumberingPeriod == NumberingPeriod.Day)
      {
        return registrationDate.BeginningOfDay();
      }
      
      return null;
    }
    
    /// <summary>
    /// Получить конец действия текущего периода журнала.
    /// </summary>
    /// <param name="registrationDate">Дата.</param>
    /// <returns>Конец периода, null для сквозной нумерации.</returns>
    [Public]
    public DateTime? GetEndPeriod(DateTime registrationDate)
    {
      var periodBegin = this.GetBeginPeriod(registrationDate);
      if (_obj.NumberingPeriod == NumberingPeriod.Year)
        return periodBegin.Value.EndOfYear();
      if (_obj.NumberingPeriod == NumberingPeriod.Quarter)
        return periodBegin.Value.AddMonths(2).EndOfMonth();
      if (_obj.NumberingPeriod == NumberingPeriod.Month)
        return periodBegin.Value.EndOfMonth();
      if (_obj.NumberingPeriod == NumberingPeriod.Day)
        return periodBegin.Value.EndOfDay();
      
      return null;
    }

    /// <summary>
    /// Получить день периода действия текущего журнала.
    /// </summary>
    /// <param name="registrationDate">Текущая дата.</param>
    /// <returns>День периода для текущей даты.</returns>
    [Public]
    public int GetCurrentDay(DateTime registrationDate)
    {
      return _obj.NumberingPeriod == Docflow.DocumentRegister.NumberingPeriod.Day ? registrationDate.Day : 0;
    }
    
    /// <summary>
    /// Получить месяц периода действия текущего журнала.
    /// </summary>
    /// <param name="registrationDate">Текущая дата.</param>
    /// <returns>Месяц периода для текущей даты.</returns>
    [Public]
    public int GetCurrentMonth(DateTime registrationDate)
    {
      // Для разрезов не по месяцу вернуть ноль, для отличия их от разрезов по месяцу.
      // Вернуть null нельзя, т.к. параметр запроса будет считаться пустым.
      return _obj.NumberingPeriod == Docflow.DocumentRegister.NumberingPeriod.Day ||
        _obj.NumberingPeriod == Docflow.DocumentRegister.NumberingPeriod.Month ? registrationDate.Month : 0;
    }
    
    /// <summary>
    /// Получить квартал периода действия текущего журнала.
    /// </summary>
    /// <param name="registrationDate">Текущая дата.</param>
    /// <returns>Квартал периода для текущей даты.</returns>
    [Public]
    public int GetCurrentQuarter(DateTime registrationDate)
    {
      // Для разрезов не по кварталу вернуть 0, для отличия их от разрезов по кварталу.
      // Вернуть null нельзя, т.к. параметр запроса будет считаться пустым.
      if (_obj.NumberingPeriod != Docflow.DocumentRegister.NumberingPeriod.Quarter)
        return 0;
      
      if (registrationDate.Month <= 3)
        return 1;

      if (registrationDate.Month <= 6)
        return 2;

      if (registrationDate.Month <= 9)
        return 3;

      return 4;
    }

    /// <summary>
    /// Получить год периода действия текущего журнала.
    /// </summary>
    /// <param name="registrationDate">Текущая дата.</param>
    /// <returns>Год периода для текущей даты.</returns>
    [Public]
    public int GetCurrentYear(DateTime registrationDate)
    {
      return _obj.NumberingPeriod == Docflow.DocumentRegister.NumberingPeriod.Day ||
        _obj.NumberingPeriod == Docflow.DocumentRegister.NumberingPeriod.Month ||
        _obj.NumberingPeriod == Docflow.DocumentRegister.NumberingPeriod.Quarter ||
        _obj.NumberingPeriod == Docflow.DocumentRegister.NumberingPeriod.Year ? registrationDate.Year : 9999;
    }

    /// <summary>
    /// Получить квартал.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <returns>Квартал периода для текущей даты.</returns>
    [Public]
    public static string ToQuarterString(DateTime date)
    {
      if (date.Month <= 3)
        return "I";

      if (date.Month <= 6)
        return "II";

      if (date.Month <= 9)
        return "III";

      return "IV";
    }
    
    #endregion
    
    #region Работа с номером документа
    
    /// <summary>
    /// Заполнить пример номера журнала в соответствии с форматом.
    /// </summary>
    public virtual void FillValueExample()
    {
      _obj.ValueExample = this.GetValueExample();
    }
    
    /// <summary>
    /// Получить пример номера журнала в соответствии с форматом.
    /// </summary>
    /// <returns>Пример номера журнала.</returns>
    public virtual string GetValueExample()
    {
      var registrationIndexExample = "1";
      var leadingDocNumberExample = "1";
      var departmentCodeExample = DocumentRegisters.Resources.NumberFormatDepartmentCode;
      var caseFileIndexExample = DocumentRegisters.Resources.NumberFormatCaseFile;
      var businessUnitCodeExample = DocumentRegisters.Resources.NumberFormatBUCode;
      var docKindCodeExample = DocumentRegisters.Resources.NumberFormatDocKindCode;
      var counterpartyCodeExample = DocumentRegisters.Resources.NumberFormatCounterpartyCode;
      
      var useObsoleteRegNumberGeneration = Functions.Module.Remote.GetDocflowParamsBoolValue(PublicConstants.Module.UseObsoleteRegNumberGenerationParamName);
      if (useObsoleteRegNumberGeneration)
        return Functions.DocumentRegister.GenerateRegistrationNumber(_obj, Calendar.UserNow, registrationIndexExample, leadingDocNumberExample,
                                                                     departmentCodeExample, businessUnitCodeExample, caseFileIndexExample,
                                                                     docKindCodeExample, counterpartyCodeExample, "0");
      
      var formatItems = this.GetBasicFormatItemsValues(Calendar.UserNow);
      formatItems = this.FillFormatItemsValuesForExample(formatItems, Calendar.UserNow);
      
      var prefixAndPostfix = this.GenerateRegistrationNumberPrefixAndPostfix(formatItems);
      var indexWithLeadingZeros = this.AppendLeadingZerosToIndex(registrationIndexExample);
      return string.Format("{0}{1}{2}", prefixAndPostfix.Prefix, indexWithLeadingZeros, prefixAndPostfix.Postfix);
    }
    
    /// <summary>
    /// Генерировать регистрационный номер для документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="date">Дата регистрации.</param>
    /// <param name="index">Индекс.</param>
    /// <returns>Сгенерированный регистрационный номер.</returns>
    [Public]
    public virtual string GenerateRegistrationNumber(IOfficialDocument document, DateTime date, string index)
    {
      var useObsoleteRegNumberGeneration = Functions.Module.Remote.GetDocflowParamsBoolValue(PublicConstants.Module.UseObsoleteRegNumberGenerationParamName);
      var formatItems = GetNumberFormatItemsValues(document);
      var prefixAndPostfix = useObsoleteRegNumberGeneration
        ? Functions.DocumentRegister.GenerateRegistrationNumberPrefixAndPostfix(_obj, date,
                                                                                formatItems.LeadingDocumentNumber, formatItems.DepartmentCode, formatItems.BusinessUnitCode,
                                                                                formatItems.CaseFileIndex, formatItems.DocumentKindCode, formatItems.CounterpartyCode,
                                                                                false)
        : Functions.DocumentRegister.GenerateRegistrationNumberPrefixAndPostfix(_obj, document, date);
      var indexWithLeadingZeros = this.AppendLeadingZerosToIndex(index);
      return string.Format("{0}{1}{2}", prefixAndPostfix.Prefix, indexWithLeadingZeros, prefixAndPostfix.Postfix);
    }
    
    /// <summary>
    /// Дополнить индекс лидирующими нулями.
    /// </summary>
    /// <param name="index">Индекс.</param>
    /// <returns>Индекс с лидирующими нулями в пределах числа цифр в номере.</returns>
    public virtual string AppendLeadingZerosToIndex(string index)
    {
      var indexLeadingSymbol = Constants.OfficialDocument.DefaultIndexLeadingSymbol;
      
      var registrationNumber = string.Empty;
      if (index.Length < _obj.NumberOfDigitsInNumber)
        registrationNumber = string.Concat(Enumerable.Repeat(indexLeadingSymbol, (_obj.NumberOfDigitsInNumber - index.Length) ?? 0));
      registrationNumber += index;
      
      return registrationNumber;
    }
    
    /// <summary>
    /// Генерировать регистрационный номер для документа.
    /// </summary>
    /// <param name="date">Дата регистрации.</param>
    /// <param name="index">Номер.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="leadingDocumentNumber">Номер ведущего документа.</param>
    /// <returns>Сгенерированный регистрационный номер.</returns>
    [Public]
    [Obsolete("Метод не используется с 02.07.2024 и версии 4.11. Используйте метод GenerateRegistrationNumber с документом в параметрах.")]
    public virtual string GenerateRegistrationNumber(DateTime date, string index, string departmentCode, string businessUnitCode,
                                                     string caseFileIndex, string docKindCode, string counterpartyCode, string leadingDocumentNumber)
    {
      return Functions.DocumentRegister.GenerateRegistrationNumber(_obj, date, index, leadingDocumentNumber, departmentCode, businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, "0");
    }
    
    /// <summary>
    /// Генерировать регистрационный номер для диалога регистрации.
    /// </summary>
    /// <param name="date">Дата регистрации.</param>
    /// <param name="index">Номер.</param>
    /// <param name="leadingDocumentNumber">Номер ведущего документа.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="indexLeadingSymbol">Символ для заполнения ведущих значений индекса в номере.</param>
    /// <returns>Сгенерированный регистрационный номер.</returns>
    [Obsolete("Метод не используется с 02.07.2024 и версии 4.11. Используйте метод GenerateRegistrationNumber с документом в параметрах.")]
    public virtual string GenerateRegistrationNumberFromDialog(DateTime date, string index, string leadingDocumentNumber,
                                                               string departmentCode, string businessUnitCode, string caseFileIndex,
                                                               string docKindCode, string counterpartyCode, string indexLeadingSymbol = "0")
    {
      return Functions.DocumentRegister.GenerateRegistrationNumber(_obj, date, index, leadingDocumentNumber, departmentCode, businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, indexLeadingSymbol);
    }
    
    /// <summary>
    /// Генерировать регистрационный номер для документа.
    /// </summary>
    /// <param name="date">Дата регистрации.</param>
    /// <param name="index">Номер.</param>
    /// <param name="leadingDocumentNumber">Номер ведущего документа.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="indexLeadingSymbol">Символ для заполнения ведущих значений индекса в номере.</param>
    /// <returns>Сгенерированный регистрационный номер.</returns>
    [Public]
    [Obsolete("Метод не используется с 02.07.2024 и версии 4.11. Используйте метод GenerateRegistrationNumber с документом в параметрах.")]
    public virtual string GenerateRegistrationNumber(DateTime date, string index, string leadingDocumentNumber,
                                                     string departmentCode, string businessUnitCode, string caseFileIndex,
                                                     string docKindCode, string counterpartyCode, string indexLeadingSymbol)
    {
      var prefixAndPostfix = Functions.DocumentRegister.GenerateRegistrationNumberPrefixAndPostfix(_obj, date, leadingDocumentNumber, departmentCode,
                                                                                                   businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, false);
      var indexWithLeadingZeros = this.AppendLeadingZerosToIndex(index);
      return string.Format("{0}{1}{2}", prefixAndPostfix.Prefix, indexWithLeadingZeros, prefixAndPostfix.Postfix);
    }
    
    /// <summary>
    /// Генерировать префикс и постфикс регистрационного номера документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="date">Дата.</param>
    /// <returns>Сгенерированный регистрационный номер.</returns>
    public virtual Structures.DocumentRegister.RegistrationNumberParts GenerateRegistrationNumberPrefixAndPostfix(IOfficialDocument document, DateTime date)
    {
      var formatItems = this.GetBasicFormatItemsValues(date);
      formatItems = this.FillFormatItemsValues(formatItems, document, date);
      return this.GenerateRegistrationNumberPrefixAndPostfix(formatItems);
    }
    
    /// <summary>
    /// Сгенерировать Regex-шаблон для рег. номера.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="date">Дата регистрации.</param>
    /// <param name="indexTemplate">Шаблон порядкового номера.</param>
    /// <returns>Шаблон рег. номера.</returns>
    public virtual string GenerateRegNumberRegexTemplate(IOfficialDocument document, DateTime date, string indexTemplate)
    {
      var formatItems = this.GetBasicFormatItemsValues(date);
      formatItems = this.FillFormatItemsValues(formatItems, document, date);
      return this.GenerateRegNumberRegexTemplate(document, date, indexTemplate, formatItems);
    }
    
    /// <summary>
    /// Сгенерировать Regex-шаблон для рег. номера.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="date">Дата регистрации.</param>
    /// <param name="indexTemplate">Шаблон порядкового номера.</param>
    /// <param name="formatItems">Словарь значений базовых элементов номера.</param>
    /// <returns>Шаблон рег. номера.</returns>
    public virtual string GenerateRegNumberRegexTemplate(IOfficialDocument document, DateTime date, string indexTemplate,
                                                         System.Collections.Generic.Dictionary<Enumeration, string> formatItems)
    {
      var numberTemplate = string.Empty;
      var previousSeparator = string.Empty;
      var orderedNumberFormatItems = _obj.NumberFormatItems.Where(f => f.Element.HasValue).OrderBy(f => f.Number);
      var lastItem = orderedNumberFormatItems.LastOrDefault();
      foreach (var item in orderedNumberFormatItems)
      {
        var element = item.Element;
        var itemValue = string.Empty;
        formatItems.TryGetValue(element.Value, out itemValue);
        
        // Не добавлять разделитель для пустого кода контрагента/категории или № ведущего.
        var isCPartyCode = element == FormatElement.CPartyCode;
        var isCategoryCode = element == FormatElement.CategoryCode;
        var isLeadingNumber = element == FormatElement.LeadingNumber;
        if (string.IsNullOrEmpty(itemValue) && (isLeadingNumber || isCategoryCode) || isCPartyCode)
        {
          // Разделитель до пустого элемента, если элемент последний в номере.
          if (lastItem == null || item.Number != lastItem.Number)
            numberTemplate += previousSeparator;
          
          // Разделитель после пустого элемента.
          previousSeparator = string.Empty;
        }
        else
        {
          numberTemplate += previousSeparator;
          previousSeparator = !string.IsNullOrEmpty(item.Separator) ? Regex.Escape(item.Separator) : string.Empty;
        }
        
        // Вместо кода контрагента допускается любой набор не пробельных символов (bug 72460).
        if (element == FormatElement.Number)
          numberTemplate += indexTemplate;
        else if (isCPartyCode)
          numberTemplate += Constants.DocumentRegister.CounterpartyCodeRegex;
        else
          numberTemplate += !string.IsNullOrEmpty(itemValue) ? Regex.Escape(itemValue) : string.Empty;
      }
      numberTemplate += previousSeparator;
      
      return numberTemplate;
    }
    
    /// <summary>
    /// Получить словарь значений базовых элементов номера.
    /// </summary>
    /// <param name="date">Дата регистрации.</param>
    /// <returns>Словарь со значениями базовых элементов номера.</returns>
    public virtual System.Collections.Generic.Dictionary<Enumeration, string> GetBasicFormatItemsValues(DateTime date)
    {
      var formatItems = new Dictionary<Enumeration, string>();
      formatItems.Add(FormatElement.Number, string.Empty);
      formatItems.Add(FormatElement.Log, _obj.Index);
      if (_obj.RegistrationGroup != null)
        formatItems.Add(FormatElement.RegistrPlace, _obj.RegistrationGroup.Index);
      formatItems.Add(FormatElement.Year2Place, date.ToString("yy"));
      formatItems.Add(FormatElement.Year4Place, date.ToString("yyyy"));
      formatItems.Add(FormatElement.Month, date.ToString("MM"));
      formatItems.Add(FormatElement.Quarter, ToQuarterString(date));
      formatItems.Add(FormatElement.Day, date.ToString("dd"));
      
      return formatItems;
    }
    
    /// <summary>
    /// Заполнить значения элементов номера по документу.
    /// </summary>
    /// <param name="formatItems">Словарь значений базовых элементов номера.</param>
    /// <param name="document">Документ.</param>
    /// <param name="date">Дата регистрации.</param>
    /// <returns>Словарь со значениями элементов номера.</returns>
    public virtual System.Collections.Generic.Dictionary<Enumeration, string> FillFormatItemsValues(System.Collections.Generic.Dictionary<Enumeration, string> formatItems,
                                                                                                    IOfficialDocument document, DateTime date)
    {
      var formatItemsValues = GetNumberFormatItemsValues(document);
      formatItems.Add(FormatElement.LeadingNumber, formatItemsValues.LeadingDocumentNumber);
      formatItems.Add(FormatElement.DepartmentCode, formatItemsValues.DepartmentCode);
      formatItems.Add(FormatElement.BUCode, formatItemsValues.BusinessUnitCode);
      formatItems.Add(FormatElement.CaseFile, formatItemsValues.CaseFileIndex);
      formatItems.Add(FormatElement.DocKindCode, formatItemsValues.DocumentKindCode);
      formatItems.Add(FormatElement.CPartyCode, formatItemsValues.CounterpartyCode);
      formatItems.Add(FormatElement.CategoryCode, Functions.OfficialDocument.GetCategoryCode(document));
      return formatItems;
    }
    
    /// <summary>
    /// Заполнить значения элементов номера по документу для примера номера.
    /// </summary>
    /// <param name="formatItems">Словарь значений базовых элементов номера.</param>
    /// <param name="date">Дата регистрации.</param>
    /// <returns>Словарь со значениями элементов номера.</returns>
    public virtual System.Collections.Generic.Dictionary<Enumeration, string> FillFormatItemsValuesForExample(System.Collections.Generic.Dictionary<Enumeration, string> formatItems,
                                                                                                              DateTime date)
    {
      var leadingDocNumberExample = "1";
      formatItems.Add(FormatElement.LeadingNumber, leadingDocNumberExample);
      formatItems.Add(FormatElement.DepartmentCode, DocumentRegisters.Resources.NumberFormatDepartmentCode);
      formatItems.Add(FormatElement.BUCode, DocumentRegisters.Resources.NumberFormatBUCode);
      formatItems.Add(FormatElement.CaseFile, DocumentRegisters.Resources.NumberFormatCaseFile);
      formatItems.Add(FormatElement.DocKindCode, DocumentRegisters.Resources.NumberFormatDocKindCode);
      formatItems.Add(FormatElement.CPartyCode, DocumentRegisters.Resources.NumberFormatCounterpartyCode);
      formatItems.Add(FormatElement.CategoryCode, DocumentRegisters.Resources.NumberFormatCategoryCode);
      return formatItems;
    }
    
    /// <summary>
    /// Получить значения элементов формата номера.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Значения элементов формата номера.</returns>
    public static Docflow.Structures.DocumentRegister.NumberFormatItemsValues GetNumberFormatItemsValues(IOfficialDocument document)
    {
      var result = Docflow.Structures.DocumentRegister.NumberFormatItemsValues.Create();
      
      result.LeadingDocumentId = Functions.OfficialDocument.GetLeadDocumentId(document);
      result.DepartmentId = document.Department != null ? document.Department.Id : 0;
      result.BusinessUnitId = document.BusinessUnit != null ? document.BusinessUnit.Id : 0;
      result.LeadingDocumentNumber = Functions.OfficialDocument.GetLeadDocumentNumber(document);
      result.DepartmentCode = document.Department != null ? document.Department.Code : string.Empty;
      result.BusinessUnitCode = document.BusinessUnit != null ? document.BusinessUnit.Code : string.Empty;
      result.CaseFileIndex = document.CaseFile != null ? document.CaseFile.Index : string.Empty;
      result.DocumentKindCode = document.DocumentKind != null ? document.DocumentKind.Code : string.Empty;
      result.CounterpartyCode = Functions.OfficialDocument.GetCounterpartyCode(document);
      
      return result;
    }
    
    /// <summary>
    /// Генерировать префикс и постфикс регистрационного номера документа.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <param name="leadingDocumentNumber">Ведущий документ.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="counterpartyCodeIsMetasymbol">Признак того, что код контрагента нужен в виде метасимвола.</param>
    /// <returns>Сгенерированный регистрационный номер.</returns>
    [Obsolete("Метод не используется с 02.07.2024 и версии 4.11. Используйте метод GenerateRegistrationNumberPrefixAndPostfix с документом в параметрах.")]
    public virtual Structures.DocumentRegister.RegistrationNumberParts GenerateRegistrationNumberPrefixAndPostfix(DateTime date, string leadingDocumentNumber,
                                                                                                                  string departmentCode, string businessUnitCode,
                                                                                                                  string caseFileIndex, string docKindCode,
                                                                                                                  string counterpartyCode, bool counterpartyCodeIsMetasymbol)
    {
      var formatItems = this.GetBasicFormatItemsValues(date);
      formatItems.Add(FormatElement.LeadingNumber, leadingDocumentNumber);
      formatItems.Add(FormatElement.DepartmentCode, departmentCode);
      formatItems.Add(FormatElement.BUCode, businessUnitCode);
      formatItems.Add(FormatElement.CaseFile, caseFileIndex);
      formatItems.Add(FormatElement.DocKindCode, docKindCode);
      var counterpartyCodeItem = counterpartyCodeIsMetasymbol
        ? DocumentRegisters.Resources.NumberFormatCounterpartyCodeMetasymbol
        : counterpartyCode;
      formatItems.Add(FormatElement.CPartyCode, counterpartyCodeItem);
      
      return this.GenerateRegistrationNumberPrefixAndPostfix(formatItems);
    }
    
    /// <summary>
    /// Генерировать префикс и постфикс регистрационного номера документа.
    /// </summary>
    /// <param name="formatElements">Элементы формата номера.</param>
    /// <returns>Сгенерированный регистрационный номер.</returns>
    public virtual Structures.DocumentRegister.RegistrationNumberParts GenerateRegistrationNumberPrefixAndPostfix(System.Collections.Generic.Dictionary<Enumeration, string> formatElements)
    {
      var counterpartyCode = string.Empty;
      formatElements.TryGetValue(FormatElement.CPartyCode, out counterpartyCode);
      var categoryCode = string.Empty;
      formatElements.TryGetValue(FormatElement.CategoryCode, out categoryCode);
      var leadingNumber = string.Empty;
      formatElements.TryGetValue(FormatElement.LeadingNumber, out leadingNumber);
      var counterpartyCodeIsMetasymbol = counterpartyCode == DocumentRegisters.Resources.NumberFormatCounterpartyCodeMetasymbol;
      
      var prefix = string.Empty;
      var postfix = string.Empty;
      var numberElement = string.Empty;
      var orderedNumberFormatItems = _obj.NumberFormatItems.OrderBy(f => f.Number);
      foreach (var item in orderedNumberFormatItems)
      {
        var element = item.Element;
        if (!element.HasValue)
          continue;
        
        var itemValue = string.Empty;
        if (formatElements.TryGetValue(element.Value, out itemValue))
        {
          if (element == FormatElement.Number)
          {
            prefix = numberElement;
            numberElement = string.Empty;
          }
          else
            numberElement += itemValue;
        }
        
        // Не добавлять разделитель для пустого кода контрагента.
        if (string.IsNullOrEmpty(counterpartyCode) || counterpartyCodeIsMetasymbol)
        {
          // Разделитель после пустого кода контрагента.
          if (element == FormatElement.CPartyCode)
            continue;
          
          // Разделитель до кода контрагента, если код контрагента последний в номере.
          var nextItem = orderedNumberFormatItems.Where(f => f.Number > item.Number).FirstOrDefault();
          var lastItem = orderedNumberFormatItems.LastOrDefault();
          if (nextItem != null && nextItem.Element == FormatElement.CPartyCode &&
              lastItem != null && lastItem.Number == nextItem.Number)
            continue;
        }
        
        // Не добавлять разделитель для пустого кода категории.
        if (string.IsNullOrEmpty(categoryCode))
        {
          // Разделитель после пустого кода категории.
          if (element == FormatElement.CategoryCode)
            continue;
          
          // Разделитель до кода категории, если код категории последний в номере.
          var nextItem = orderedNumberFormatItems.Where(f => f.Number > item.Number).FirstOrDefault();
          var lastItem = orderedNumberFormatItems.LastOrDefault();
          if (nextItem != null && nextItem.Element == FormatElement.CategoryCode &&
              lastItem != null && lastItem.Number == nextItem.Number)
            continue;
        }
        
        // Не добавлять разделитель для пустого № ведущего документа.
        if (string.IsNullOrEmpty(leadingNumber))
        {
          // Разделитель после пустого № ведущего.
          if (element == FormatElement.LeadingNumber)
            continue;
          
          // Разделитель до № ведущего, если № ведущего последний в номере.
          var nextItem = orderedNumberFormatItems.Where(f => f.Number > item.Number).FirstOrDefault();
          var lastItem = orderedNumberFormatItems.LastOrDefault();
          if (nextItem != null && nextItem.Element == FormatElement.LeadingNumber &&
              lastItem != null && lastItem.Number == nextItem.Number)
            continue;
        }
        
        // Добавить разделитель.
        numberElement += item.Separator;
      }
      
      postfix = numberElement;
      return Structures.DocumentRegister.RegistrationNumberParts.Create(prefix, postfix);
    }
    
    /// <summary>
    /// Получить формат номера журнала регистрации для отчета.
    /// </summary>
    /// <returns>Формат номера для отчета.</returns>
    /// <remarks>Используется в SkippedNumbersReport.</remarks>
    public virtual string GetReportNumberFormat()
    {
      var numberFormat = string.Empty;

      foreach (var item in _obj.NumberFormatItems.OrderBy(x => x.Number))
      {
        var elementName = string.Empty;
        var element = item.Element;
        if (element == FormatElement.Number)
          elementName = DocumentRegisters.Resources.NumberFormatNumber;
        else if (element == FormatElement.Year2Place || element == FormatElement.Year4Place)
          elementName = DocumentRegisters.Resources.NumberFormatYear;
        else if (element == FormatElement.Quarter)
          elementName = DocumentRegisters.Resources.NumberFormatQuarter;
        else if (element == FormatElement.Month)
          elementName = DocumentRegisters.Resources.NumberFormatMonth;
        else if (element == FormatElement.Day)
          elementName = DocumentRegisters.Resources.NumberFormatDay;
        else if (element == FormatElement.LeadingNumber)
          elementName = DocumentRegisters.Resources.NumberFormatLeadingNumber;
        else if (element == FormatElement.Log)
          elementName = DocumentRegisters.Resources.NumberFormatLog;
        else if (element == FormatElement.RegistrPlace)
          elementName = DocumentRegisters.Resources.NumberFormatRegistrPlace;
        else if (element == FormatElement.CaseFile)
          elementName = DocumentRegisters.Resources.NumberFormatCaseFile;
        else if (element == FormatElement.DepartmentCode)
          elementName = DocumentRegisters.Resources.NumberFormatDepartmentCode;
        else if (element == FormatElement.BUCode)
          elementName = DocumentRegisters.Resources.NumberFormatBUCode;
        else if (element == FormatElement.DocKindCode)
          elementName = DocumentRegisters.Resources.NumberFormatDocKindCode;
        else if (element == FormatElement.CPartyCode)
          elementName = DocumentRegisters.Resources.NumberFormatCounterpartyCode;
        else if (element == FormatElement.CategoryCode)
          elementName = DocumentRegisters.Resources.NumberFormatCategoryCode;

        numberFormat += elementName + item.Separator;
      }

      return numberFormat;
    }
    
    /// <summary>
    /// Проверить возможность построения номера по разрезам журнала.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Сообщение об ошибке. Пустая строка, если возможно сформировать номер.</returns>
    public virtual string CheckDocumentRegisterSections(IOfficialDocument document)
    {
      var departmentValidationErrors = Functions.DocumentRegister.GetDepartmentValidationError(_obj, document);
      if (!string.IsNullOrEmpty(departmentValidationErrors))
        return departmentValidationErrors;
      
      var businessUnitValidationErrors = Functions.DocumentRegister.GetBusinessUnitValidationError(_obj, document);
      if (!string.IsNullOrEmpty(businessUnitValidationErrors))
        return businessUnitValidationErrors;
      
      var docKindCodeValidationErrors = Functions.DocumentRegister.GetDocumentKindValidationError(_obj, document);
      if (!string.IsNullOrEmpty(docKindCodeValidationErrors))
        return docKindCodeValidationErrors;
      
      var categoryCodeValidationErrors = Functions.DocumentRegister.GetCategoryValidationError(_obj, document);
      if (!string.IsNullOrEmpty(categoryCodeValidationErrors))
        return categoryCodeValidationErrors;
      
      var docCaseFileValidationErrors = Functions.DocumentRegister.GetCaseFileValidationError(_obj, document);
      if (!string.IsNullOrEmpty(docCaseFileValidationErrors))
        return docCaseFileValidationErrors;
      
      return string.Empty;
    }
    
    /// <summary>
    /// Проверить регистрационный номер на валидность.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Сообщение об ошибке. Пустая строка, если номер соответствует журналу.</returns>
    public virtual string CheckRegistrationNumberFormat(IOfficialDocument document)
    {
      // Возможен корректировочный постфикс или нет (возможен, если необходимо проверять на уникальность).
      var correctingPostfixInNumberIsAvailable = Functions.OfficialDocument.CheckRegistrationNumberUnique(document);
      
      var useObsoleteRegNumberGeneration = Functions.Module.Remote.GetDocflowParamsBoolValue(PublicConstants.Module.UseObsoleteRegNumberGenerationParamName);
      if (useObsoleteRegNumberGeneration)
      {
        var formatItems = GetNumberFormatItemsValues(document);
        return this.CheckRegistrationNumberFormat(document.RegistrationDate, document.RegistrationNumber,
                                                  formatItems.DepartmentCode, formatItems.BusinessUnitCode, formatItems.CaseFileIndex,
                                                  formatItems.DocumentKindCode, formatItems.CounterpartyCode, formatItems.LeadingDocumentNumber,
                                                  correctingPostfixInNumberIsAvailable);
      }
      
      return this.CheckRegistrationNumberFormat(document,
                                                document.RegistrationDate,
                                                document.RegistrationNumber,
                                                correctingPostfixInNumberIsAvailable);
    }
    
    /// <summary>
    /// Проверить регистрационный номер на валидность.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="registrationDate">Дата регистрации.</param>
    /// <param name="registrationNumber">Номер регистрации.</param>
    /// <param name="searchCorrectingPostfix">Искать корректировочный постфикс.</param>
    /// <returns>Сообщение об ошибке. Пустая строка, если номер соответствует журналу.</returns>
    /// <remarks>Пример: 5/1-П/2020, где 5 - порядковый номер, П - индекс журнала, 2020 - год, /1 - корректировочный постфикс.</remarks>
    public virtual string CheckRegistrationNumberFormat(IOfficialDocument document, DateTime? registrationDate, string registrationNumber,
                                                        bool searchCorrectingPostfix)
    {
      if (string.IsNullOrWhiteSpace(registrationNumber))
        return DocumentRegisters.Resources.EnterRegistrationNumber;
      
      // Регулярное выражение для рег. индекса.
      // "([0-9]+)" определяет, где искать индекс в номере.
      // "([\.\/-][0-9]+)?" определяет, где искать корректировочный постфикс в номере.
      // Пустые скобки в выражении @"([0-9]+)()" означают корректировочный постфикс,
      // чтобы количество групп в результате регулярного выражения было всегда одинаковым, независимо от того, нужно искать корректировочный постфикс или нет.
      var indexTemplate = searchCorrectingPostfix ? @"([0-9]+)([\.\/-][0-9]+)?" : @"([0-9]+)()";
      
      // Перед проверкой правильности формата дополнительно проверить наличие непечатных символов в строке ("\s").
      if (Regex.IsMatch(registrationNumber, @"\s"))
        return DocumentRegisters.Resources.NoSpaces;
      
      if (!GetRegexMatchFromRegistrationNumber(_obj, document, registrationDate ?? Calendar.UserToday, registrationNumber, indexTemplate,
                                               string.Empty, string.Empty)
          .Success)
      {
        // Шаблон номера, состоящий из символов "*".
        var numberTemplate = string.Concat(Enumerable.Repeat("*", _obj.NumberOfDigitsInNumber.Value));
        var example = this.GenerateRegistrationNumber(document, registrationDate.Value, numberTemplate);
        return Docflow.Resources.RegistrationNumberNotMatchFormatFormat(example);
      }
      
      return string.Empty;
    }

    /// <summary>
    /// Проверить регистрационный номер на валидность.
    /// </summary>
    /// <param name="registrationDate">Дата регистрации.</param>
    /// <param name="registrationNumber">Номер регистрации.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="leadDocNumber">Номер ведущего документа.</param>
    /// <param name="searchCorrectingPostfix">Искать корректировочный постфикс.</param>
    /// <returns>Сообщение об ошибке. Пустая строка, если номер соответствует журналу.</returns>
    /// <remarks>Пример: 5/1-П/2020, где 5 - порядковый номер, П - индекс журнала, 2020 - год, /1 - корректировочный постфикс.</remarks>
    [Obsolete("Метод не используется с 02.07.2024 и версии 4.11. Используйте метод CheckRegistrationNumberFormat с документом в параметрах.")]
    public virtual string CheckRegistrationNumberFormat(DateTime? registrationDate, string registrationNumber,
                                                        string departmentCode, string businessUnitCode, string caseFileIndex, string docKindCode, string counterpartyCode,
                                                        string leadDocNumber, bool searchCorrectingPostfix)
    {
      if (string.IsNullOrWhiteSpace(registrationNumber))
        return DocumentRegisters.Resources.EnterRegistrationNumber;
      
      // Регулярное выражение для рег. индекса.
      // "([0-9]+)" определяет, где искать индекс в номере.
      // "([\.\/-][0-9]+)?" определяет, где искать корректировочный постфикс в номере.
      // Пустые скобки в выражении @"([0-9]+)()" означают корректировочный постфикс,
      // чтобы количество групп в результате регулярного выражения было всегда одинаковым, независимо от того, нужно искать корректировочный постфикс или нет.
      var indexTemplate = searchCorrectingPostfix ? @"([0-9]+)([\.\/-][0-9]+)?" : @"([0-9]+)()";
      
      // Перед проверкой правильности формата дополнительно проверить наличие непечатных символов в строке ("\s").
      if (Regex.IsMatch(registrationNumber, @"\s"))
        return DocumentRegisters.Resources.NoSpaces;
      
      if (!GetRegexMatchFromRegistrationNumber(_obj, registrationDate ?? Calendar.UserToday, registrationNumber, indexTemplate,
                                               departmentCode, businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, leadDocNumber,
                                               string.Empty, string.Empty)
          .Success)
      {
        // Шаблон номера, состоящий из символов "*".
        var numberTemplate = string.Concat(Enumerable.Repeat("*", _obj.NumberOfDigitsInNumber.Value));
        var example = this.GenerateRegistrationNumber(registrationDate.Value, numberTemplate, departmentCode, businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, "0");
        return Docflow.Resources.RegistrationNumberNotMatchFormatFormat(example);
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Получить сравнение рег.номера с шаблоном.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="document">Документ.</param>
    /// <param name="date">Дата.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="indexTemplate">Шаблон номера.</param>
    /// <param name="numberPostfix">Постфикс номера.</param>
    /// <param name="additionalPrefix">Дополнительный префикс номера.</param>
    /// <returns>Сравнение рег.номера с шаблоном.</returns>
    internal static System.Text.RegularExpressions.Match GetRegexMatchFromRegistrationNumber(IDocumentRegister documentRegister, IOfficialDocument document,
                                                                                             DateTime date, string registrationNumber,
                                                                                             string indexTemplate, string numberPostfix, string additionalPrefix)
    {
      var useObsoleteRegNumberGeneration = Functions.Module.Remote.GetDocflowParamsBoolValue(PublicConstants.Module.UseObsoleteRegNumberGenerationParamName);
      var formatItems = GetNumberFormatItemsValues(document);
      string template;
      if (useObsoleteRegNumberGeneration)
      {
        var prefixAndPostfix = Functions.DocumentRegister.GenerateRegistrationNumberPrefixAndPostfix(documentRegister, date,
                                                                                                     formatItems.LeadingDocumentNumber, formatItems.DepartmentCode, formatItems.BusinessUnitCode,
                                                                                                     formatItems.CaseFileIndex, formatItems.DocumentKindCode, formatItems.CounterpartyCode,
                                                                                                     true);
        template = string.Format("{0}{1}{2}{3}", Regex.Escape(prefixAndPostfix.Prefix),
                                 indexTemplate,
                                 Regex.Escape(prefixAndPostfix.Postfix),
                                 numberPostfix);
        
        // Заменить метасимвол для кода контрагента на соответствующее регулярное выражение.
        var metaCounterpartyCode = Regex.Escape(DocumentRegisters.Resources.NumberFormatCounterpartyCodeMetasymbol);
        template = template.Replace(metaCounterpartyCode, Constants.DocumentRegister.CounterpartyCodeRegex);
      }
      else
      {
        template = Functions.DocumentRegister.GenerateRegNumberRegexTemplate(documentRegister, document, date, indexTemplate);
        template += numberPostfix;
      }
      
      // Совпадение в начале строки.
      var numberTemplate = string.Format("^{0}", template);
      var match = Regex.Match(registrationNumber, numberTemplate);
      if (match.Success)
        return match;
      
      // Совпадение в конце строки.
      numberTemplate = string.Format("{0}{1}$", additionalPrefix, template);
      return Regex.Match(registrationNumber, numberTemplate);
    }
    
    /// <summary>
    /// Получить сравнение рег.номера с шаблоном.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="date">Дата.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="indexTemplate">Шаблон номера.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="leadDocNumber">Номер ведущего документа.</param>
    /// <param name="numberPostfix">Постфикс номера.</param>
    /// <param name="additionalPrefix">Дополнительный префикс номера.</param>
    /// <returns>Индекс.</returns>
    [Obsolete("Метод не используется с 02.07.2024 и версии 4.11. Используйте виртуальный метод GetRegexMatchFromRegistrationNumber.")]
    internal static Match GetRegexMatchFromRegistrationNumber(IDocumentRegister documentRegister, DateTime date, string registrationNumber,
                                                              string indexTemplate, string departmentCode, string businessUnitCode,
                                                              string caseFileIndex, string docKindCode, string counterpartyCode, string leadDocNumber,
                                                              string numberPostfix, string additionalPrefix)
    {
      var prefixAndPostfix = Functions.DocumentRegister.GenerateRegistrationNumberPrefixAndPostfix(documentRegister, date, leadDocNumber, departmentCode,
                                                                                                   businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, true);
      var template = string.Format("{0}{1}{2}{3}", Regex.Escape(prefixAndPostfix.Prefix),
                                   indexTemplate,
                                   Regex.Escape(prefixAndPostfix.Postfix),
                                   numberPostfix);
      
      // Заменить метасимвол для кода контрагента на соответствующее регулярное выражение.
      var metaCounterpartyCode = Regex.Escape(DocumentRegisters.Resources.NumberFormatCounterpartyCodeMetasymbol);
      template = template.Replace(metaCounterpartyCode, Constants.DocumentRegister.CounterpartyCodeRegex);
      
      // Совпадение в начале строки.
      var numberTemplate = string.Format("^{0}", template);
      var match = Regex.Match(registrationNumber, numberTemplate);
      if (match.Success)
        return match;
      
      // Совпадение в конце строки.
      numberTemplate = string.Format("{0}{1}$", additionalPrefix, template);
      return Regex.Match(registrationNumber, numberTemplate);
    }

    /// <summary>
    /// Получить индекс рег. номера.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="document">Документ.</param>
    /// <param name="date">Дата.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="searchCorrectingPostfix">Искать корректировочный постфикс.</param>
    /// <returns>Индекс.</returns>
    /// <remarks>Пример: 5/1-П/2020, где 5 - порядковый номер, П - индекс журнала, 2020 - год, /1 - корректировочный постфикс.</remarks>
    public static int GetIndexFromRegistrationNumber(IDocumentRegister documentRegister, IOfficialDocument document, DateTime date, string registrationNumber, bool searchCorrectingPostfix)
    {
      return ParseRegistrationNumber(documentRegister, document, date, registrationNumber, searchCorrectingPostfix).Index;
    }
    
    /// <summary>
    /// Получить индекс рег. номера.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="date">Дата.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="leadDocNumber">Номер ведущего документа.</param>
    /// <param name="searchCorrectingPostfix">Искать корректировочный постфикс.</param>
    /// <returns>Индекс.</returns>
    /// <remarks>Пример: 5/1-П/2020, где 5 - порядковый номер, П - индекс журнала, 2020 - год, /1 - корректировочный постфикс.</remarks>
    [Obsolete("Метод не используется с 02.07.2024 и версии 4.11. Используйте виртуальный метод GetIndexFromRegistrationNumber.")]
    public static int GetIndexFromRegistrationNumber(IDocumentRegister documentRegister, DateTime date, string registrationNumber,
                                                     string departmentCode, string businessUnitCode, string caseFileIndex, string docKindCode, string counterpartyCode,
                                                     string leadDocNumber, bool searchCorrectingPostfix)
    {
      return ParseRegistrationNumber(documentRegister, date, registrationNumber, departmentCode, businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, leadDocNumber, searchCorrectingPostfix).Index;
    }
    
    /// <summary>
    /// Выделить составные части рег.номера.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="document">Документ.</param>
    /// <param name="date">Дата.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="searchCorrectingPostfix">Искать корректировочный постфикс.</param>
    /// <returns>Индекс рег.номера.</returns>
    /// <remarks>Пример: 5/1-П/2020, где 5 - порядковый номер, П - индекс журнала, 2020 - год, /1 - корректировочный постфикс.</remarks>
    public static Structures.DocumentRegister.RegistrationNumberIndex ParseRegistrationNumber(IDocumentRegister documentRegister, IOfficialDocument document, DateTime date,
                                                                                              string registrationNumber, bool searchCorrectingPostfix)
    {
      // "(.*?)" определяет место, в котором находятся искомые данные.
      var releasingExpression = "(.*?)$";
      // Регулярное выражение для рег. индекса.
      // "([0-9]+)" определяет, где искать индекс в номере.
      // "([\.\/-][0-9]+)?" определяет, где искать корректировочный постфикс в номере.
      // Пустые скобки в выражении @"([0-9]+)()" означают корректировочный постфикс,
      // чтобы количество групп в результате регулярного выражения было всегда одинаковым, независимо от того, нужно искать корректировочный постфикс или нет.
      var indexExpression = searchCorrectingPostfix ? @"([0-9]+)([\.\/-][0-9]+)?" : @"([0-9]+)()";
      
      // Распарсить рег.номер на составляющие.
      var registrationNumberMatch = GetRegexMatchFromRegistrationNumber(documentRegister, document, date, registrationNumber, indexExpression,
                                                                        releasingExpression, string.Empty);
      var indexOfNumber = registrationNumberMatch.Groups[1].Value;
      var correctingPostfix = registrationNumberMatch.Groups[2].Value;
      var postfix = registrationNumberMatch.Groups[3].Value;
      
      int index;
      index = int.TryParse(indexOfNumber, out index) ? index : 0;
      return Structures.DocumentRegister.RegistrationNumberIndex.Create(index, postfix, correctingPostfix);
    }
    
    /// <summary>
    /// Выделить составные части рег.номера.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="date">Дата.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="leadDocNumber">Номер ведущего документа.</param>
    /// <param name="searchCorrectingPostfix">Искать корректировочный постфикс.</param>
    /// <returns>Индекс рег.номера.</returns>
    /// <remarks>Пример: 5/1-П/2020, где 5 - порядковый номер, П - индекс журнала, 2020 - год, /1 - корректировочный постфикс.</remarks>
    [Obsolete("Метод не используется с 02.07.2024 и версии 4.11. Используйте метод ParseRegistrationNumber с документом в параметрах.")]
    public static Structures.DocumentRegister.RegistrationNumberIndex ParseRegistrationNumber(IDocumentRegister documentRegister, DateTime date, string registrationNumber,
                                                                                              string departmentCode, string businessUnitCode, string caseFileIndex, string docKindCode, string counterpartyCode,
                                                                                              string leadDocNumber, bool searchCorrectingPostfix)
    {
      // "(.*?)" определяет место, в котором находятся искомые данные.
      var releasingExpression = "(.*?)$";
      // Регулярное выражение для рег. индекса.
      // "([0-9]+)" определяет, где искать индекс в номере.
      // "([\.\/-][0-9]+)?" определяет, где искать корректировочный постфикс в номере.
      // Пустые скобки в выражении @"([0-9]+)()" означают корректировочный постфикс,
      // чтобы количество групп в результате регулярного выражения было всегда одинаковым, независимо от того, нужно искать корректировочный постфикс или нет.
      var indexExpression = searchCorrectingPostfix ? @"([0-9]+)([\.\/-][0-9]+)?" : @"([0-9]+)()";
      
      // Распарсить рег.номер на составляющие.
      var registrationNumberMatch = GetRegexMatchFromRegistrationNumber(documentRegister, date, registrationNumber, indexExpression,
                                                                        departmentCode, businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, leadDocNumber,
                                                                        releasingExpression, string.Empty);
      var indexOfNumber = registrationNumberMatch.Groups[1].Value;
      var correctingPostfix = registrationNumberMatch.Groups[2].Value;
      var postfix = registrationNumberMatch.Groups[3].Value;
      
      int index;
      index = int.TryParse(indexOfNumber, out index) ? index : 0;
      return Structures.DocumentRegister.RegistrationNumberIndex.Create(index, postfix, correctingPostfix);
    }
    
    /// <summary>
    /// Проверить совпадение рег.номеров.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="document">Документ.</param>
    /// <param name="date">Дата.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="registrationNumberSample">Пример рег. номера.</param>
    /// <param name="searchCorrectingPostfix">Искать корректировочный постфикс.</param>
    /// <returns>True, если совпадают.</returns>
    /// <remarks>Пример: 5/1-П/2020, где 5 - порядковый номер, П - индекс журнала, 2020 - год, /1 - корректировочный постфикс.</remarks>
    public static bool AreRegistrationNumbersEqual(IDocumentRegister documentRegister, IOfficialDocument document, DateTime date, string registrationNumber,
                                                   string registrationNumberSample, bool searchCorrectingPostfix)
    {
      var indexAndPostfix = ParseRegistrationNumber(documentRegister, document, date, registrationNumberSample, searchCorrectingPostfix);
      var maxLeadZeroIndexCount = 9 - indexAndPostfix.Index.ToString().Count();
      var indexRegexTemplate = "[0]{0," + maxLeadZeroIndexCount + "}" + indexAndPostfix.Index + Regex.Escape(indexAndPostfix.CorrectingPostfix);
      
      var numberPostfix = Regex.Escape(indexAndPostfix.Postfix) + "$";
      return GetRegexMatchFromRegistrationNumber(documentRegister, document, date, registrationNumber, indexRegexTemplate, numberPostfix, "^").Success;
    }
    
    /// <summary>
    /// Проверить совпадение рег.номеров.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="date">Дата.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="leadDocNumber">Номер ведущего документа.</param>
    /// <param name="registrationNumberSample">Пример рег. номера.</param>
    /// <param name="searchCorrectingPostfix">Искать корректировочный постфикс.</param>
    /// <returns>True, если совпадают.</returns>
    /// <remarks>Пример: 5/1-П/2020, где 5 - порядковый номер, П - индекс журнала, 2020 - год, /1 - корректировочный постфикс.</remarks>
    [Obsolete("Метод не используется с 02.07.2024 и версии 4.11. Используйте метод AreRegistrationNumbersEqual.")]
    public static bool IsEqualsRegistrationNumbers(IDocumentRegister documentRegister, DateTime date, string registrationNumber,
                                                   string departmentCode, string businessUnitCode, string caseFileIndex, string docKindCode, string counterpartyCode,
                                                   string leadDocNumber, string registrationNumberSample,
                                                   bool searchCorrectingPostfix)
    {
      var indexAndPostfix = ParseRegistrationNumber(documentRegister, date, registrationNumberSample, departmentCode, businessUnitCode,
                                                    caseFileIndex, docKindCode, counterpartyCode, leadDocNumber, searchCorrectingPostfix);
      var maxLeadZeroIndexCount = 9 - indexAndPostfix.Index.ToString().Count();
      var indexRegexTemplate = "[0]{0," + maxLeadZeroIndexCount + "}" + indexAndPostfix.Index + Regex.Escape(indexAndPostfix.CorrectingPostfix);
      
      var numberPostfix = Regex.Escape(indexAndPostfix.Postfix) + "$";
      return GetRegexMatchFromRegistrationNumber(documentRegister, date, registrationNumber, indexRegexTemplate, departmentCode,
                                                 businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, leadDocNumber,
                                                 numberPostfix, "^").Success;
    }
    
    #endregion
    
    #region Регистрация. Проверка составных частей формата номера
    
    /// <summary>
    /// Проверить заполненность подразделения и кода подразделения.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Текст ошибки, либо string.Empty.</returns>
    public static string GetDepartmentValidationError(IDocumentRegister documentRegister, IOfficialDocument document)
    {
      // Проверить необходимость кода подразделения.
      if (documentRegister == null || !documentRegister.NumberFormatItems.Any(n => n.Element == FormatElement.DepartmentCode))
        return string.Empty;
      
      // Проверить наличие подразделения.
      if (document.Department == null)
        return CreateValidationError(documentRegister, Docflow.Resources.FillDepartment);
      
      // Проверить наличие кода у подразделения.
      if (string.IsNullOrWhiteSpace(document.Department.Code))
        return CreateValidationError(documentRegister, Docflow.Resources.FillDepartmentCode);
      
      return string.Empty;
    }
    
    /// <summary>
    /// Проверить заполненность НОР и кода НОР.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Текст ошибки, либо string.Empty.</returns>
    public static string GetBusinessUnitValidationError(IDocumentRegister documentRegister, IOfficialDocument document)
    {
      // Проверить необходимость кода НОР.
      if (documentRegister == null || !documentRegister.NumberFormatItems.Any(n => n.Element == FormatElement.BUCode))
        return string.Empty;
      
      // Проверить наличие НОР.
      if (document.BusinessUnit == null)
        return CreateValidationError(documentRegister, Docflow.Resources.FillBusinessUnit);
      
      // Проверить наличие кода у НОР.
      if (string.IsNullOrWhiteSpace(document.BusinessUnit.Code))
        return CreateValidationError(documentRegister, Docflow.Resources.FillBusinessUnitCode);
      
      return string.Empty;
    }
    
    /// <summary>
    /// Проверить заполненность кода вида документа.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Текст ошибки, либо string.Empty.</returns>
    public static string GetDocumentKindValidationError(IDocumentRegister documentRegister, IOfficialDocument document)
    {
      // Проверить необходимость кода вида документа.
      if (documentRegister == null || !documentRegister.NumberFormatItems.Any(n => n.Element == FormatElement.DocKindCode))
        return string.Empty;
      
      // Проверить наличие вида документа.
      if (document.DocumentKind == null)
        return CreateValidationError(documentRegister, Docflow.Resources.FillDocumentKind);
      
      // Проверить наличие кода у вида документа.
      if (string.IsNullOrWhiteSpace(document.DocumentKind.Code))
        return CreateValidationError(documentRegister, Docflow.Resources.FillDocumentKindCode);
      
      return string.Empty;
    }
    
    /// <summary>
    /// Проверить заполненность кода категории.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Текст ошибки, либо string.Empty.</returns>
    public static string GetCategoryValidationError(IDocumentRegister documentRegister, IOfficialDocument document)
    {
      // Код категории не требуется, если у вида документа нет категорий.
      if (documentRegister == null || document == null || document.DocumentGroup == null ||
          !Functions.DocumentRegister.NumberFormatContains(documentRegister, FormatElement.CategoryCode))
        return string.Empty;
      
      var categoryCode = Functions.OfficialDocument.GetCategoryCode(document);
      if (string.IsNullOrWhiteSpace(categoryCode))
        return CreateValidationError(documentRegister, Docflow.Resources.FillCategoryCode);
      
      return string.Empty;
    }
    
    /// <summary>
    /// Проверить заполненность дела.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Текст ошибки, либо string.Empty.</returns>
    public static string GetCaseFileValidationError(IDocumentRegister documentRegister, IOfficialDocument document)
    {
      // Проверить необходимость дела.
      if (documentRegister == null || !documentRegister.NumberFormatItems.Any(n => n.Element == FormatElement.CaseFile))
        return string.Empty;
      
      // Проверить наличие дела.
      if (document.CaseFile == null)
        return CreateValidationError(documentRegister, Docflow.Resources.FillCaseFile);
      
      // Проверить наличие кода у дела.
      if (string.IsNullOrEmpty(document.CaseFile.Index))
        return CreateValidationError(documentRegister, Docflow.Resources.FillCaseFileCode);
      
      return string.Empty;
    }
    
    /// <summary>
    /// Сформировать ошибку валидации.
    /// </summary>
    /// <param name="documentRegister">Журнал регистрации.</param>
    /// <param name="errorModel">Шаблон ошибки.</param>
    /// <returns>Ошибка валидации.</returns>
    public static string CreateValidationError(IDocumentRegister documentRegister, string errorModel)
    {
      var isNumbering = documentRegister.RegisterType == Docflow.DocumentRegister.RegisterType.Numbering;
      return string.Format(errorModel, isNumbering ? Docflow.Resources.numberWord : Docflow.Resources.registerWord);
    }
    
    #endregion
    
    /// <summary>
    /// Получить журнал по умолчанию для документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="filteredDocRegistersIds">Список ИД доступных журналов.</param>
    /// <param name="settingType">Тип настройки.</param>
    /// <returns>Журнал регистрации по умолчанию.</returns>
    /// <remarks>Журнал подбирается сначала из настройки регистрации, потом из персональных настроек пользователя.
    /// Если в настройках не указан журнал, или указан недействующий, то вернётся первый журнал из доступных для документа.
    /// Если доступных журналов несколько, то вернётся пустое значение.</remarks>
    [Public]
    public static IDocumentRegister GetDefaultDocRegister(IOfficialDocument document, List<long> filteredDocRegistersIds, Enumeration? settingType)
    {
      var defaultDocRegister = DocumentRegisters.Null;

      if (document == null)
        return defaultDocRegister;

      var registrationSetting = Docflow.PublicFunctions.RegistrationSetting.GetSettingByDocument(document, settingType);
      if (registrationSetting != null && filteredDocRegistersIds.Contains(registrationSetting.DocumentRegister.Id))
        return registrationSetting.DocumentRegister;
      
      var personalSettings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(null);
      if (personalSettings != null)
      {
        var documentKind = document.DocumentKind;

        if (documentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Incoming)
          defaultDocRegister = personalSettings.IncomingDocRegister;
        if (documentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Outgoing)
          defaultDocRegister = personalSettings.OutgoingDocRegister;
        if (documentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Inner)
          defaultDocRegister = personalSettings.InnerDocRegister;
        if (documentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts)
          defaultDocRegister = personalSettings.ContractDocRegister;
      }

      if (defaultDocRegister == null || !filteredDocRegistersIds.Contains(defaultDocRegister.Id) || defaultDocRegister.Status != CoreEntities.DatabookEntry.Status.Active)
      {
        defaultDocRegister = null;
        if (filteredDocRegistersIds.Count() == 1)
          defaultDocRegister = Functions.DocumentRegister.Remote.GetDocumentRegister(filteredDocRegistersIds.First());
      }
      
      return defaultDocRegister;
    }

    /// <summary>
    /// Установить обязательность свойств в зависимости от заполненных данных.
    /// </summary>
    public virtual void SetRequiredProperties()
    {
      _obj.State.Properties.RegistrationGroup.IsRequired = _obj.Info.Properties.RegistrationGroup.IsRequired ||
        _obj.RegisterType == RegisterType.Registration;
      _obj.State.Properties.NumberFormatItems.IsRequired = true;
    }
    
    /// <summary>
    /// Отфильтровать документопотоки согласно настройкам выбранной группы регистрации.
    /// </summary>
    /// <param name="query">Все доступные документопотоки.</param>
    /// <returns>Отфильтрованные документопотоки.</returns>
    public List<Enumeration> GetFilteredDocumentFlows(IQueryable<Enumeration> query)
    {
      var group = _obj.RegistrationGroup;
      if (group != null)
      {
        if (group.CanRegisterIncoming != true)
          query = query.Where(f => f != DocumentFlow.Incoming);
        if (group.CanRegisterInternal != true)
          query = query.Where(f => f != DocumentFlow.Inner);
        if (group.CanRegisterOutgoing != true)
          query = query.Where(f => f != DocumentFlow.Outgoing);
        if (group.CanRegisterContractual != true)
          query = query.Where(f => f != DocumentFlow.Contracts);
      }
      return query.ToList();
    }
    
    /// <summary>
    /// Проверить, содержится ли в формате номера указанный элемент.
    /// </summary>
    /// <param name="element">Элемент.</param>
    /// <returns>True - содержится, False - нет.</returns>
    [Public]
    public bool NumberFormatContains(Enumeration element)
    {
      return _obj.NumberFormatItems.Any(x => x.Element == element);
    }
    
  }
}