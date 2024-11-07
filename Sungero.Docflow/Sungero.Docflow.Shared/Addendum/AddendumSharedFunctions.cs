using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Addendum;

namespace Sungero.Docflow.Shared
{
  partial class AddendumFunctions
  {
    public override void FillName()
    {
      if (_obj.DocumentKind != null && !_obj.DocumentKind.GenerateDocumentName.Value && _obj.Name == OfficialDocuments.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (_obj.DocumentKind == null || !_obj.DocumentKind.GenerateDocumentName.Value)
        return;
      
      var name = string.Empty;
      
      /* Имя в формате:
        <Вид документа> №<номер> "<содержание>" к <имя ведущего документа>.
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (!string.IsNullOrEmpty(_obj.Subject))
          name += " " + _obj.Subject;
        
        if (_obj.LeadingDocument != null)
        {
          name += OfficialDocuments.Resources.NamePartForLeadDocument;
          name += Functions.Module.ReplaceFirstSymbolToLowerCase(GetDocumentName(_obj.LeadingDocument));
        }
      }
      
      if (string.IsNullOrWhiteSpace(name))
        name = OfficialDocuments.Resources.DocumentNameAutotext;
      else if (_obj.DocumentKind != null)
        name = _obj.DocumentKind.ShortName + name;
      
      name = Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
      
    }
    
    /// <summary>
    /// Получить наименование документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Наименование документа.</returns>
    public static string GetDocumentName(IOfficialDocument document)
    {
      return document.AccessRights.CanRead() ? document.Name : Functions.Addendum.Remote.GetLeadingDocument(document.Id).Name;
    }
    
    public override void RefreshDocumentForm()
    {
      base.RefreshDocumentForm();
      
      var isNotNumerable = _obj.DocumentKind == null || _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable;
      _obj.State.Properties.BusinessUnit.IsVisible = !isNotNumerable;
      _obj.State.Properties.Department.IsVisible = !isNotNumerable;
      _obj.State.Properties.OurSignatory.IsVisible = !isNotNumerable || this.GetShowOurSigningReasonParam();
      _obj.State.Properties.PreparedBy.IsVisible = !isNotNumerable;
      _obj.State.Properties.Assignee.IsVisible = !isNotNumerable;
    }
    
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);
      
      _obj.State.Properties.LeadingDocument.IsEnabled = isEnabled || isRepeatRegister;
    }
    
    public override string GetLeadDocumentNumber()
    {
      var doc = _obj.LeadingDocument;
      return doc.AccessRights.CanRead() ? doc.RegistrationNumber : Functions.Addendum.Remote.GetLeadingDocument(doc.Id).Number;
    }
    
    public override void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
    {
      base.ChangeRegistrationPaneVisibility(needShow, repeatRegister);
      
      var notNumerable = _obj.DocumentKind != null && _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable;
      var needShowRegistrationProperties = !notNumerable && needShow;
      
      _obj.State.Properties.RegistrationNumber.IsVisible = needShowRegistrationProperties;
      _obj.State.Properties.RegistrationDate.IsVisible = needShowRegistrationProperties;
    }
    
    /// <summary>
    /// Проверка на то, что документ является проектным.
    /// </summary>
    /// <param name="leadingDocumentIds">ИД ведущих документов.</param>
    /// <returns>True - если документ проектный, иначе - false.</returns>
    public override bool IsProjectDocument(List<long> leadingDocumentIds)
    {
      if (base.IsProjectDocument(leadingDocumentIds))
        return true;
      
      // Если текущий документ входит в список ранее вычисленных ведущих документов приложения,
      // то есть произошло зацикливание ведущих документов друг с другом,
      // и при этом ни один из них не определился как проектный, то значит документ не является проектным.
      if (leadingDocumentIds.Any() && leadingDocumentIds.Contains(_obj.Id))
        return false;
      
      leadingDocumentIds.Add(_obj.Id);
      
      // Проверить, что ведущий документ является проектным.
      return Functions.OfficialDocument.IsProjectDocument(_obj.LeadingDocument, leadingDocumentIds);
    }
    
    public override IProjectBase GetProject()
    {
      return base.GetProject() ?? Functions.OfficialDocument.GetProject(_obj.LeadingDocument);
    }

    /// <summary>
    /// Заполнить обязательные свойства для документа.
    /// </summary>
    /// <param name="properties">Свойства.</param>
    public override void FillRequiredProperties(System.Collections.Generic.IDictionary<string, object> properties)
    {
      base.FillRequiredProperties(properties);
      
      if (properties.ContainsKey(_obj.Info.Properties.LeadingDocument.Name))
        _obj.LeadingDocument = (IOfficialDocument)properties[_obj.Info.Properties.LeadingDocument.Name];
    }
    
    /// <summary>
    /// Проверка, заполнены ли обязательные и псевдообязательные свойства.
    /// </summary>
    /// <returns>True - если обязательные и псевдообязательные свойства не заполнены, иначе - false.</returns>
    public override bool HasEmptyRequiredProperties()
    {
      return string.IsNullOrEmpty(_obj.Subject);
    }
    
    /// <summary>
    /// Установить обязательность свойств в зависимости от заполненных данных.
    /// </summary>
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();

      // Изменить обязательность полей в зависимости от того, программная или визуальная работа.
      var isVisualMode = ((Domain.Shared.IExtendedEntity)_obj).Params.ContainsKey(Sungero.Docflow.PublicConstants.OfficialDocument.IsVisualModeParamName);

      // При визуальной работе обязательность содержания и корреспондента как в Addendum.
      // При программной работе содержание и корреспондент - необязательные.
      // Чтобы сбросить обязательность, если она изменилась в вызове текущего метода в базовой сущности.
      _obj.State.Properties.Subject.IsRequired = isVisualMode;
    }
  }
}