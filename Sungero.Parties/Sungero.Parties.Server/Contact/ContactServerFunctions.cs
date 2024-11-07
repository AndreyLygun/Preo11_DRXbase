using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Contact;

namespace Sungero.Parties.Server
{
  partial class ContactFunctions
  {

    /// <summary>
    /// Создать асинхронное событие обновления имени контакта из персоны.
    /// </summary>
    /// <param name="personId">ИД персоны.</param>
    [Public]
    public static void CreateUpdateContactNameAsyncHandler(long personId)
    {
      var asyncUpdateContactName = Sungero.Parties.AsyncHandlers.UpdateContactName.Create();
      asyncUpdateContactName.PersonId = personId;
      asyncUpdateContactName.ExecuteAsync();
    }
    
    /// <summary>
    /// Получить дубли контактов.
    /// </summary>
    /// <returns>Контакты, дублирующие текущего по ФИО.</returns>
    [Remote(IsPure = true)]
    public IQueryable<IContact> GetDuplicates()
    {
      return Contacts.GetAll()
        .Where(contact =>
               !Equals(_obj, contact) &&
               Equals(contact.Name, _obj.Name) &&
               Equals(contact.Company, _obj.Company) &&
               contact.Status != Sungero.Parties.Contact.Status.Closed);
    }
    
    /// <summary>
    /// Получить контактное лицо по имени.
    /// </summary>
    /// <param name="name">Имя контакта.</param>
    /// <param name="counterparty">Контрагент, владелец контакта.</param>
    /// <returns>Найденный контакт, если он только один, иначе - null.</returns>
    [Public]
    public static Parties.IContact GetContactByName(string name, ICounterparty counterparty)
    {
      var contacts = GetContactsByName(name, name, counterparty)
        .Where(x => x.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed);
      
      return contacts.Count() == 1 ? contacts.FirstOrDefault() : null;
    }
    
    /// <summary>
    /// Получить контактные лица по имени.
    /// </summary>
    /// <param name="name">Имя в формате "Фамилия И.О." или "Фамилия Имя Отчество".</param>
    /// <param name="personShortName">Имя в формате "Фамилия И.О.".</param>
    /// <param name="counterparty">Контрагент, владелец контакта.</param>
    /// <returns>Коллекция контактных лиц.</returns>
    [Public]
    public static IQueryable<IContact> GetContactsByName(string name, string personShortName, ICounterparty counterparty)
    {
      var nonBreakingSpace = new string('\u00A0', 1);
      var space = new string('\u0020', 1);
      
      name = name.ToLower().Replace(nonBreakingSpace, space).Replace(". ", ".");
      
      var contacts = Contacts.GetAll()
        .Where(x => (x.Name.ToLower().Replace(nonBreakingSpace, space).Replace(". ", ".") == name) ||
               (x.Person != null && string.Equals(x.Person.ShortName,
                                                  personShortName,
                                                  StringComparison.InvariantCultureIgnoreCase)));
      
      if (counterparty != null)
        return contacts.Where(c => c.Company.Equals(counterparty));
      
      return contacts;
    }

    /// <summary>
    /// Получить контактное лицо контрагента по ФИО с использованием нечеткого поиска.
    /// </summary>
    /// <param name="fullName">ФИО/фамилия с инициалами(-ом)/фамилия.</param>
    /// <param name="counterpartyId">ИД компании для фильтрации.</param>
    /// <returns>Найденный контакт.</returns>
    [Public]
    public static Parties.IContact GetContactsByNameFuzzy(string fullName, long counterpartyId)
    {
      var counterpartyIds = new List<long>();
      if (counterpartyId > 0)
        counterpartyIds.Add(counterpartyId);
      
      return GetContactsByNameFuzzy(fullName, counterpartyIds);
    }

    /// <summary>
    /// Получить контактное лицо по ФИО по списку контрагентов с использованием нечеткого поиска.
    /// </summary>
    /// <param name="fullName">ФИО/фамилия с инициалами(-ом)/фамилия.</param>
    /// <param name="counterpartyIds">Список ИД компаний для фильтрации.</param>
    /// <returns>Найденный контакт.</returns>
    [Public]
    public static Parties.IContact GetContactsByNameFuzzy(string fullName, List<long> counterpartyIds)
    {
      var contact = Parties.Contacts.Null;
      
      var name = Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(fullName);
      if (string.IsNullOrWhiteSpace(name))
        return contact;
      
      // Сформировать условие для нечеткого поиска по ФИО с фильтром по активным сущностям.
      var filter = Commons.PublicFunctions.Module.GetTermQuery("Status", CoreEntities.DatabookEntry.Status.Active.Value);

      if (counterpartyIds.Count == 1)
        filter = string.Format("{0},{1}", filter, Commons.PublicFunctions.Module.GetTermQuery("CompanyId", counterpartyIds.Single().ToString()));
      
      if (counterpartyIds.Count > 1)
        filter = string.Format("{0},{1}", filter,
                               Commons.PublicFunctions.Module.GetTermsQuery("CompanyId", counterpartyIds.Select(x => x.ToString()).ToList()));
      
      var matchInitials = Regex.Match(fullName, Constants.Module.InitialsRegex, RegexOptions.IgnoreCase);
      var splittedName = fullName.Split(' ');
      
      // Поиск по полным ФИО или фамилия+имя.
      var contactIds = new List<long>();
      if (!matchInitials.Success && splittedName.Length > 1)
      {
        // Четкий поиск.
        var querySearch = Commons.PublicFunctions.Module.GetBoolQuery(Commons.PublicFunctions.Module.GetMatchQuery("FullName", fullName, true),
                                                                      string.Empty,
                                                                      string.Format("[{0}]", filter));
        
        contactIds = Commons.PublicFunctions.Module.ExecuteElasticsearchQuery(Parties.Contacts.Info.Name, querySearch);
        
        // Нечеткий поиск.
        if (contactIds.Count == 0)
        {
          querySearch = Commons.PublicFunctions.Module.GetBoolQuery(Commons.PublicFunctions.Module.GetMatchFuzzyQuery("FullName", fullName, true),
                                                                    string.Empty,
                                                                    string.Format("[{0}]", filter));
          contactIds = Commons.PublicFunctions.Module.ExecuteElasticsearchQuery(Parties.Contacts.Info.Name, querySearch, Constants.Contact.ElasticsearchMinScore);
        }
        
        // Если были найдены записи - уточнять среди них на следующих шагах.
        if (contactIds.Count > 0)
        {
          if (contactIds.Count == 1)
          {
            contact = Parties.Contacts.GetAll(x => Equals(x.Id, contactIds.First())).FirstOrDefault();
            if (contact == null)
              Logger.ErrorFormat("SearchContactByNameFuzzy. Cant get contact by id {0}.", contactIds.First());
          }
          else
          {
            if (!string.IsNullOrEmpty(filter))
              filter = string.Format("{0},{1}", filter, Commons.PublicFunctions.Module.GetTermsQuery("Id", contactIds.Select(x => x.ToString()).ToList()));
            else
              filter = Commons.PublicFunctions.Module.GetTermsQuery("Id", contactIds.Select(x => x.ToString()).ToList());
          }
        }
      }
      
      if (contact == null)
      {
        // Поиск по фамилии и инициалам.
        if (!matchInitials.Success)
        {
          // Если не удалось найти по полному ФИО в поисковом запросе, возможно в индексе запись в виде фамилии с инициалами.
          // Попытка из поискового запроса выделить фамилию, а от ИО взять лишь инициалы.
          // Проверяется 2 возможных варианта в поисковом запросе: "ФИО" и "ИОФ".
          if (splittedName.Length > 1 && splittedName.Length < 4)
          {
            var foundContacts = new List<IContact>();
            for (var i = 0; i < 2; ++i)
            {
              var names = splittedName.ToList();
              var searchValue = i == 0 ? names.First() : names.Last();
              names.Remove(searchValue);
              var initialFirstName = names[0].Substring(0, 1);
              var initialMiddleName = names.Count > 1 ? names[1].Substring(0, 1) : string.Empty;

              var querySearch = Commons.PublicFunctions.Module.GetBoolQuery(Commons.PublicFunctions.Module.GetMatchQuery("FullName", searchValue, true),
                                                                            string.Empty,
                                                                            string.Format("[{0}]", filter));
              
              contactIds = Commons.PublicFunctions.Module.ExecuteElasticsearchQuery(Parties.Contacts.Info.Name, querySearch);
              var foundContact = GetSingleContactFilteredByInitials(contactIds, initialFirstName, initialMiddleName);
              if (foundContact != null)
                foundContacts.Add(foundContact);
            }
            // Если найден единственный контакт - вернуть его.
            // Если найдены по ФИО и по ИОФ, вернуть первый, но только если оба найдены в единственной организации.
            if (foundContacts.Any() && foundContacts.Select(x => x.Company).Distinct().Count() == 1)
              contact = foundContacts.First();
          }
        }
        else
        {
          // В поисковом запросе фамилия + инициалы, но найти не удалось. В индексе может находиться полное ФИО.
          // Попытка поискать по фамилии, убрав инициалы из поискового запроса.
          var searchValue = Regex.Replace(fullName, Constants.Module.InitialsRegex, string.Empty);
          var initialFirstName = matchInitials.Groups[1].Value;
          var initialMiddleName = matchInitials.Groups[2].Value;
          
          var querySearch = Commons.PublicFunctions.Module.GetBoolQuery(Commons.PublicFunctions.Module.GetMatchQuery("FullName", searchValue, true),
                                                                        string.Empty,
                                                                        string.Format("[{0}]", filter));
          
          contactIds = Commons.PublicFunctions.Module.ExecuteElasticsearchQuery(Parties.Contacts.Info.Name, querySearch);
          contact = GetSingleContactFilteredByInitials(contactIds, initialFirstName, initialMiddleName);
        }
      }
      
      return contact;
    }
    
    /// <summary>
    /// Получить контакт с указанными инициалами.
    /// </summary>
    /// <param name="contactIds">Список ИД контактов.</param>
    /// <param name="initialFirstName">Инициал имени.</param>
    /// <param name="initialMiddleName">Инициал отчества.</param>
    /// <returns>Первый контакт, если все найденные контакты принадлежат одной организации, или null, если контакты принадлежат разным организациям.</returns>
    [Public]
    public static IContact GetSingleContactFilteredByInitials(List<long> contactIds, string initialFirstName, string initialMiddleName)
    {
      var contact = Parties.Contacts.Null;
      var contactsFiltered = new List<Parties.IContact>();
      foreach (var foundContact in Parties.Contacts.GetAll(x => contactIds.Contains(x.Id)))
      {
        if (EqualsInitials(foundContact.Name, initialFirstName, initialMiddleName))
          contactsFiltered.Add(foundContact);
      }
      
      // Если все найденные контакты принадлежат одной организации - вернуть первый контакт.
      if (contactsFiltered.Select(c => c.Company).Distinct().Count() == 1)
        contact = contactsFiltered.First();

      return contact;
    }
    
    /// <summary>
    /// Проверить соответствие имени(ФИО) инициалам.
    /// </summary>
    /// <param name="fullName">Имя (для полного проверяются 2 варианта: ФИО и ИОФ).</param>
    /// <param name="initialFirstName">Инициал имени.</param>
    /// <param name="initialMiddleName">Инициал отчества.</param>
    /// <returns>Результат проверки на соответствие.</returns>
    [Public]
    public static bool EqualsInitials(string fullName, string initialFirstName, string initialMiddleName)
    {
      var result = false;
      if (string.IsNullOrWhiteSpace(fullName))
        return result;
      
      if (!string.IsNullOrWhiteSpace(initialFirstName))
      {
        var matchInitialsIndex = Regex.Match(fullName.Trim(), Constants.Module.InitialsRegex, RegexOptions.IgnoreCase);
        if (matchInitialsIndex.Success)
        {
          if (matchInitialsIndex.Groups.Count >= 2)
            // ФИО записано с инициалами.
            result = initialFirstName == matchInitialsIndex.Groups[1].Value &&
              (string.IsNullOrWhiteSpace(initialMiddleName) || string.IsNullOrWhiteSpace(matchInitialsIndex.Groups[2].Value) || initialMiddleName == matchInitialsIndex.Groups[2].Value);
        }
        else
        {
          // Проверить по двум последовательностям в имени: ФИО / ИОФ.
          var nameSplitted = fullName.Trim().Split(' ');
          if (nameSplitted.Any(x => string.IsNullOrWhiteSpace(x)) || nameSplitted.Count() == 1)
            return result;
          if (!string.IsNullOrWhiteSpace(initialMiddleName) && nameSplitted.Count() >= 3)
          {
            // Оба инициала присутствуют.
            result = (nameSplitted[0].Substring(0, 1) == initialFirstName && nameSplitted[1].Substring(0, 1) == initialMiddleName) ||
              (nameSplitted[1].Substring(0, 1) == initialFirstName && nameSplitted[2].Substring(0, 1) == initialMiddleName);
          }
          else
          {
            // Присутствует только инициал имени.
            result = nameSplitted[0].Substring(0, 1) == initialFirstName || nameSplitted[1].Substring(0, 1) == initialFirstName;
          }
        }
      }
      
      return result;
    }
    
    /// <summary>
    /// Получить JSON-строку для индексирования в поисковой системе.
    /// </summary>
    /// <returns>JSON-строка.</returns>
    [Public]
    public virtual string GetIndexingJson()
    {
      return string.Format(Constants.Contact.ElasticsearchIndexTemplate,
                           _obj.Id,
                           _obj.Company.Id,
                           Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(_obj.Name),
                           Sungero.Core.Calendar.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                           _obj.Status.Value.Value);
    }
  }
}