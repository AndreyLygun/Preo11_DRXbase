using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentRegister;
using FormatElement = Sungero.Docflow.DocumentRegisterNumberFormatItems.Element;

namespace Sungero.Docflow
{
  partial class DocumentRegisterNumberFormatItemsClientHandlers
  {

    public virtual void NumberFormatItemsNumberValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (_obj.DocumentRegister.NumberFormatItems.Any(s => s.Number == e.NewValue))
        e.AddError(DocumentRegisters.Resources.NotUniqueNumber);
    }
    
    public virtual IEnumerable<Enumeration> NumberFormatItemsElementFiltering(IEnumerable<Enumeration> query)
    {
      if (_obj.DocumentRegister.RegisterType != Docflow.DocumentRegister.RegisterType.Registration)
        query = query.Where(x => x.Value != FormatElement.RegistrPlace.Value);
      if (_obj.DocumentRegister.DocumentFlow != Docflow.DocumentRegister.DocumentFlow.Contracts)
        query = query.Where(x => x.Value != FormatElement.CategoryCode.Value);
      
      return query;
    }
  }

  partial class DocumentRegisterClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      var databooksWithNullCode = Functions.Module.Remote.HasDatabooksWithNullCode();
      if (databooksWithNullCode.HasDepartmentWithNullCode)
        e.Params.AddOrUpdate(Constants.DocumentRegister.HasDepartmentWithNullCode, true);
      
      if (databooksWithNullCode.HasBusinessUnitWithNullCode)
        e.Params.AddOrUpdate(Constants.DocumentRegister.HasBusinessUnitWithNullCode, true);
      
      if (databooksWithNullCode.HasDocumentKindWithNullCode)
        e.Params.AddOrUpdate(Constants.DocumentRegister.HasDocumentKindWithNullCode, true);
      
      if (databooksWithNullCode.HasContractCategoriesWithNullCode)
        e.Params.AddOrUpdate(Constants.DocumentRegister.HasContractCategoriesWithNullCodeParamName, true);
    }

    public virtual IEnumerable<Enumeration> DocumentFlowFiltering(IEnumerable<Enumeration> query)
    {
      return Functions.DocumentRegister.GetFilteredDocumentFlows(_obj, query.AsQueryable());
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.DocumentRegister.SetRequiredProperties(_obj);
      if (!e.IsValid)
        return;
      
      if (_obj.AccessRights.CanUpdate())
      {
        var isNotUsed = _obj.State.IsInserted || !Functions.Module.CalculateParams(e, _obj.RegistrationGroup, false, false, true, false, _obj);
        _obj.State.Properties.RegistrationGroup.IsEnabled = isNotUsed && _obj.RegisterType != RegisterType.Numbering;
        _obj.State.Properties.RegisterType.IsEnabled = isNotUsed;
        _obj.State.Properties.DocumentFlow.IsEnabled = isNotUsed;

        var hasDocuments = !_obj.State.IsInserted && Functions.Module.CalculateParams(e, _obj.RegistrationGroup, false, false, false, true, _obj);
        _obj.State.Properties.NumberingPeriod.IsEnabled = !hasDocuments;
        _obj.State.Properties.NumberingSection.IsEnabled = !hasDocuments;
        
        if (hasDocuments)
          e.AddInformation(DocumentRegisters.Resources.SectionsDisabled, DocumentRegisters.Info.Actions.ShowRegisteredDocuments);
      }
      
      if (_obj.AccessRights.CanUpdate() && !_obj.State.IsInserted && _obj.RegistrationGroup != null &&
          !Functions.Module.CalculateParams(e, _obj.RegistrationGroup, true, true, false, false, null))
        foreach (var property in _obj.State.Properties)
          property.IsEnabled = false;
      
      // Проверить наличие разреза по ведущему документу, если он использован в номере.
      if (Functions.DocumentRegister.NumberFormatContains(_obj, FormatElement.LeadingNumber) &&
          _obj.NumberingSection != DocumentRegister.NumberingSection.LeadingDocument)
        e.AddWarning(DocumentRegisters.Resources.ForDocumentNumberMustAttendSection);
      
      // Если в формате номера есть код подразделения, то проверять, что у всех подразделений заполнены коды.
      if (Functions.DocumentRegister.NumberFormatContains(_obj, FormatElement.DepartmentCode) &&
          e.Params.Contains(Constants.DocumentRegister.HasDepartmentWithNullCode))
        e.AddWarning(DocumentRegisters.Resources.NeedFillDepartmentCodes, _obj.Info.Actions.ShowDepartments);
      
      // Если в формате номера есть код НОР, то проверять, что у всех НОР заполнены коды.
      if (Functions.DocumentRegister.NumberFormatContains(_obj, FormatElement.BUCode) &&
          e.Params.Contains(Constants.DocumentRegister.HasBusinessUnitWithNullCode))
        e.AddWarning(DocumentRegisters.Resources.NeedFillBusinessUnitCodes, _obj.Info.Actions.ShowBusinessUnits);
      
      // Если в формате номера есть код вида документа, то проверять, что у всех видов документа заполнены коды.
      if (Functions.DocumentRegister.NumberFormatContains(_obj, FormatElement.DocKindCode) &&
          e.Params.Contains(Constants.DocumentRegister.HasDocumentKindWithNullCode))
        e.AddWarning(DocumentRegisters.Resources.NeedFillDocumentKindCodes, _obj.Info.Actions.ShowDocumentKinds);
      
      // Если в формате номера есть код категории, то проверять, что у всех категорий заполнены коды.
      if (Functions.DocumentRegister.NumberFormatContains(_obj, FormatElement.CategoryCode) &&
          e.Params.Contains(Constants.DocumentRegister.HasContractCategoriesWithNullCodeParamName))
        e.AddWarning(DocumentRegisters.Resources.NeedFillContractCategoryCodes, _obj.Info.Actions.ShowContractCategoriesWithNullCode);
    }

    public virtual void NumberOfDigitsInNumberValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      // Показать сообщение о неверном вводе количества цифр в номере.
      if (e.NewValue > 9 || e.NewValue < 1)
        Dialogs.NotifyMessage(DocumentRegisters.Resources.NumberOfDigitsError);
    }
  }
}