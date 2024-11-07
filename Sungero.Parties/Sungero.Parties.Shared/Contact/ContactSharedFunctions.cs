using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Contact;

namespace Sungero.Parties.Shared
{
  partial class ContactFunctions
  {

    /// <summary>
    /// Обновить ФИО контакта.
    /// </summary>
    /// <param name="person">Персона.</param>
    public virtual void UpdateName(Parties.IPerson person)
    {
      if (person != null && !Equals(person.Name, _obj.Name))
      {
        _obj.Name = person.Name;
        _obj.Phone = person.Phones;
        _obj.Email = person.Email;
      }
    }
    
    /// <summary>
    /// Проверить дубли контактов.
    /// </summary>
    /// <returns>True, если дубликаты имеются, иначе - false.</returns>
    public bool HaveDuplicates()
    {
      return
        !string.IsNullOrWhiteSpace(_obj.Name) &&
        _obj.Status != Sungero.Parties.Contact.Status.Closed &&
        Functions.Contact.Remote.GetDuplicates(_obj).Any();
    }
  }
}