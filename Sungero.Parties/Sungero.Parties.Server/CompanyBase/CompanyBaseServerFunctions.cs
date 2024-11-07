using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.CompanyBase;

namespace Sungero.Parties.Server
{
  partial class CompanyBaseFunctions
  {

    /// <summary>
    /// Заполнить реквизиты контрагента из сервиса.
    /// </summary>
    /// <param name="specifiedPSRN">ОГРН выбранной организации.</param>
    /// <returns>Статус запроса, список представлений организаций, список контактов.</returns>
    [Remote]
    public Structures.CompanyBase.FoundCompanies FillFromService(string specifiedPSRN)
    {
      var url = Functions.Module.GetCompanyDataServiceURL();
      if (string.IsNullOrEmpty(url))
        return Structures.CompanyBase.FoundCompanies.Create(CompanyBases.Resources.ErrorNotFound, null, null, 0);
      
      // Указать ОГРН выбранного контрагента.
      var psrn = _obj.PSRN;
      if (!string.IsNullOrWhiteSpace(specifiedPSRN))
        psrn = specifiedPSRN;

      // Запрос в сервис.
      var searchResult = Sungero.CompanyData.Client.Search(psrn, _obj.TIN, _obj.Name, url);
      switch (searchResult.StatusCode)
      {
        case HttpStatusCode.OK:
          break;
        case HttpStatusCode.Unauthorized:
          return Structures.CompanyBase.FoundCompanies.Create(CompanyBases.Resources.ErrorUnauthorized, null, null, 0);
        case HttpStatusCode.Forbidden:
          return Structures.CompanyBase.FoundCompanies.Create(CompanyBases.Resources.ErrorForbidden, null, null, 0);
        case (HttpStatusCode)429:
          return Structures.CompanyBase.FoundCompanies.Create(CompanyBases.Resources.ErrorTooManyRequests, null, null, 0);
        case HttpStatusCode.ServiceUnavailable:
        case HttpStatusCode.BadGateway:
        case HttpStatusCode.NotFound:
          return Structures.CompanyBase.FoundCompanies.Create(CompanyBases.Resources.ErrorNotFound, null, null, 0);
        case HttpStatusCode.PaymentRequired:
          return Structures.CompanyBase.FoundCompanies.Create(CompanyBases.Resources.ErrorNoLicense, null, null, 0);
        default:
          return Structures.CompanyBase.FoundCompanies.Create(CompanyBases.Resources.ErrorInService, null, null, 0);
      }
      
      Logger.DebugFormat("{0} counterparties found in service", searchResult.Count.ToString());

      // Ничего не нашли.
      if (searchResult.Count < 1)
      {
        // Пустая структура, чтобы можно было отделить результат, когда ничего не найдено, от случая, когда сервис вернул ошибку.
        var emptyListOfCompanies = new List<Structures.CompanyBase.CompanyDisplayValue>();
        return Structures.CompanyBase.FoundCompanies.Create(CompanyBases.Resources.ErrorCompanyNotFoundInService, emptyListOfCompanies, null, 0);
      }
      
      // Подготовить ответ.
      var result = Structures.CompanyBase.FoundCompanies.Create();
      result.CompanyDisplayValues = searchResult.Companies
        .Select(r =>
                {
                  var dialogText = string.IsNullOrWhiteSpace(r.Kpp)
                    ? CompanyBases.Resources.CompanySelectDialogTextFormat(r.ShortName, r.Inn)
                    : CompanyBases.Resources.CompanySelectDialogTextWithTRRCFormat(r.ShortName, r.Inn, r.Kpp);
                  return Structures.CompanyBase.CompanyDisplayValue.Create(dialogText, r.Ogrn);
                })
        .ToList();
      
      result.Amount = searchResult.Total;
      
      // Нашли ровно один. Сразу заполняем реквизиты.
      if (searchResult.Count == 1)
      {
        var company = searchResult.Companies.First();
        _obj.Name = company.ShortName;
        _obj.LegalName = company.LegalName;
        _obj.PSRN = company.Ogrn;
        _obj.TIN = company.Inn;
        _obj.TRRC = company.Kpp;
        _obj.NCEO = company.Okpo;
        _obj.NCEA = Functions.CompanyBase.GetFormatOkved(company);
        _obj.LegalAddress = company.Address;
        _obj.Region = Commons.PublicFunctions.Region.GetRegionFromAddress(company.Address);
        _obj.City = Commons.PublicFunctions.City.GetCityFromAddress(company.Address);
        if (!Equals(company.State, Constants.CompanyBase.ActiveCounterpartyStateInService))
        {
          if (string.IsNullOrEmpty(_obj.Note))
            _obj.Note = company.State;
          else if (!_obj.Note.Contains(company.State))
            _obj.Note = string.Format("{0}\r\n{1}", _obj.Note, company.State);
        }
        result.FoundContacts = company.Managers
          .Select(t => Structures.CompanyBase.FoundContact.Create(t.FullName, t.JobTitle, t.Phone))
          .ToList();
      }

      return result;
    }
    
    /// <summary>
    /// Сформировать строку с ОКВЭД.
    /// </summary>
    /// <param name="company">Компания с сервиса.</param>
    /// <returns>Список ОКВЭД организации.</returns>
    public static string GetFormatOkved(CompanyData.CompaniesDTO.CompanyDTO company)
    {
      if (company.AdditionalOkveds == null || !company.AdditionalOkveds.Any())
        return company.MainOkved.Code;
      
      var separator = "; ";
      var okveds = new List<string>() { company.MainOkved.Code };
      okveds.AddRange(company.AdditionalOkveds.Select(x => x.Code).Take(6));
      return string.Join(separator, okveds.ToArray());
    }
    
    /// <summary>
    /// Получить JSON-строку для индексирования в поисковой системе.
    /// </summary>
    /// <returns>JSON-строка.</returns>
    [Public]
    public virtual string GetIndexingJson()
    {
      var emails = this.PrepareEmailForIndex(_obj.Email);
      var homepages = this.PrepareHomepageForIndex(_obj.Homepage);
      var phones = this.PreparePhonesForIndex(_obj.Phones);
      return string.Format(Constants.CompanyBase.ElasticsearchIndexTemplate,
                           _obj.Id,
                           Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(_obj.LegalName),
                           Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(_obj.Name),
                           _obj.HeadCompany != null ? Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(_obj.HeadCompany.DisplayValue) : string.Empty,
                           _obj.TIN,
                           _obj.TRRC,
                           _obj.PSRN,
                           homepages,
                           emails,
                           phones,
                           Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(_obj.LegalAddress),
                           Sungero.Core.Calendar.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                           _obj.Status.Value.Value);
    }
    
    /// <summary>
    /// Подготовить и отформатировать email для индексирования.
    /// </summary>
    /// <param name="email">Email.</param>
    /// <returns>Отформатированный email.</returns>
    public virtual string PrepareEmailForIndex(string email)
    {
      var result = string.Empty;
      if (!string.IsNullOrWhiteSpace(email))
      {
        var emails = new List<string>();
        var emailsMatch = Regex.Matches(email, @"^\S+@\S+.\S+");
        foreach (Match emailMatch in emailsMatch)
        {
          if (PublicFunctions.Module.EmailIsValid(emailMatch.Value))
            emails.Add(emailMatch.Value);
        }
        result = string.Join(" ", emails);
      }
      return result;
    }
    
    /// <summary>
    /// Подготовить и отформатировать сайт для индексирования.
    /// </summary>
    /// <param name="homepage">Сайт.</param>
    /// <returns>Отформатированный сайт.</returns>
    public virtual string PrepareHomepageForIndex(string homepage)
    {
      var result = string.Empty;
      if (!string.IsNullOrWhiteSpace(homepage))
      {
        var sites = new List<string>();
        foreach (var siteValue in homepage.Split(new char[] { ' ', ';', ',' }))
        {
          var site = Parties.PublicFunctions.Module.NormalizeSite(siteValue);
          if (!string.IsNullOrEmpty(site))
            sites.Add(site);
        }
        result = string.Join(" ", sites);
      }
      return result;
    }
    
    /// <summary>
    /// Подготовить и отформатировать телефоны для индексирования.
    /// </summary>
    /// <param name="phones">Телефоны.</param>
    /// <returns>Отформатированные телефоны.</returns>
    public virtual string PreparePhonesForIndex(string phones)
    {
      var result = string.Empty;
      if (!string.IsNullOrWhiteSpace(phones))
      {
        var phonesProcessed = new List<string>();
        var phonesMatch = Regex.Matches(phones, @"((8|\+7)[\- ]?)?(\(?\d{3,4}\)?[\- ]?)?[\d\- ]{5,10}");
        
        foreach (Match phoneMatch in phonesMatch)
        {
          var phone = Parties.PublicFunctions.Module.NormalizePhone(phoneMatch.Value);
          if (!string.IsNullOrEmpty(phone))
            phonesProcessed.Add(phone);
        }
        result = string.Join(" ", phonesProcessed);
      }
      return result;
    }
  }
}