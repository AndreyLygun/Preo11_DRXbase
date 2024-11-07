using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using Sungero.Commons;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OfficialDocument;
using Sungero.Docflow.Structures.Module;
using Sungero.Docflow.Structures.OfficialDocument;
using Sungero.Domain.Client;

namespace Sungero.Docflow.Client
{
  partial class OfficialDocumentFunctions
  {
    
    #region Преобразование в PDF с отметками
    
    /// <summary>
    /// Показать пользователю диалог настройки расположения отметок о регистрации и создать отметки по выбранным настройкам.
    /// </summary>
    /// <returns>True, если в диалоге нажали "Создать", иначе - false.</returns>
    public virtual bool CreateMarksFromDialog()
    {
      if (Functions.OfficialDocument.IsNotRegistered(_obj) &&
          _obj.LastVersionApproved.GetValueOrDefault())
      {
        var signatureMark = Functions.OfficialDocument.Remote.GetOrCreateSignatureMark(_obj);
        signatureMark.Save();
        return true;
      }
      
      var originalRegNumberMark = Functions.OfficialDocument.Remote.GetVersionMarks(_obj,
                                                                                    _obj.LastVersion.Id,
                                                                                    Constants.MarkKind.RegistrationNumberMarkKindSid)
        .FirstOrDefault();
      var originalRegDateMark = Functions.OfficialDocument.Remote.GetVersionMarks(_obj,
                                                                                  _obj.LastVersion.Id,
                                                                                  Constants.MarkKind.RegistrationDateMarkKindSid)
        .FirstOrDefault();
      
      var dialog = Dialogs.CreateInputDialog(OfficialDocuments.Resources.ConvertToPdfWithMarksDialogTitle);
      dialog.HelpCode = Constants.OfficialDocument.HelpCode.ConvertToPdfWithMarks;
      dialog.Text = _obj.LastVersionApproved.GetValueOrDefault() ?
        Sungero.Docflow.OfficialDocuments.Resources.ConvertToPdfWithMarksDialogText :
        Sungero.Docflow.OfficialDocuments.Resources.ConvertToPdfWithRegistrationMarksDialogText;
      var withRegistrationData = dialog.AddBoolean(OfficialDocuments.Resources.ConvertToPdfWithMarksDialogWithRegistrationData, true);
      var position = dialog.AddSelect(Sungero.Docflow.OfficialDocuments.Resources.ConvertToPdfWithMarksDialogPosition, true, 
                                      this.GetDefaultPositionIndex(originalRegNumberMark, originalRegDateMark))
        .From(Sungero.Docflow.OfficialDocuments.Resources.ConvertToPdfWithMarksDialogPositionByTemplate,
              Sungero.Docflow.OfficialDocuments.Resources.ConvertToPdfWithMarksDialogPositionByCoordinates);
      var regNumberXIndent = dialog.AddDouble(Sungero.Docflow.OfficialDocuments.Resources.ConvertToPdfWithMarksDialogRegNumberXIndent, false, originalRegNumberMark?.XIndent);
      var regNumberYIndent = dialog.AddDouble(Sungero.Docflow.OfficialDocuments.Resources.ConvertToPdfWithMarksDialogRegNumberYIndent, false, originalRegNumberMark?.YIndent);
      var regDateXIndent = dialog.AddDouble(Sungero.Docflow.OfficialDocuments.Resources.ConvertToPdfWithMarksDialogRegDateXIndent, false, originalRegDateMark?.XIndent);
      var regDateYIndent = dialog.AddDouble(Sungero.Docflow.OfficialDocuments.Resources.ConvertToPdfWithMarksDialogRegDateYIndent, false, originalRegDateMark?.YIndent);
      var addButton = dialog.Buttons.AddCustom(Sungero.Docflow.OfficialDocuments.Resources.ConvertToPdfWithMarksDialogCreateButton);
      dialog.Buttons.AddCancel();
      
      dialog.SetOnRefresh(
        args =>
        {
          withRegistrationData.IsVisible = _obj.LastVersionApproved.GetValueOrDefault();
          
          position.IsEnabled = withRegistrationData.Value.GetValueOrDefault();
          regNumberXIndent.IsEnabled = withRegistrationData.Value.GetValueOrDefault();
          regNumberYIndent.IsEnabled = withRegistrationData.Value.GetValueOrDefault();
          regDateXIndent.IsEnabled = withRegistrationData.Value.GetValueOrDefault();
          regDateYIndent.IsEnabled = withRegistrationData.Value.GetValueOrDefault();
          
          var coordinatesPosition = position.Value == Sungero.Docflow.OfficialDocuments.Resources.ConvertToPdfWithMarksDialogPositionByCoordinates;
          regNumberXIndent.IsVisible = coordinatesPosition;
          regNumberYIndent.IsVisible = coordinatesPosition;
          regDateXIndent.IsVisible = coordinatesPosition;
          regDateYIndent.IsVisible = coordinatesPosition;
          regNumberXIndent.IsRequired = coordinatesPosition;
          regNumberYIndent.IsRequired = coordinatesPosition;
          regDateXIndent.IsRequired = coordinatesPosition;
          regDateYIndent.IsRequired = coordinatesPosition;
          
          if (coordinatesPosition)
          {
            if (regNumberXIndent.Value.HasValue && regNumberXIndent.Value < 0)
              args.AddError(Docflow.Resources.MarkCoordinatesMustBePositive, regNumberXIndent);
            if (regNumberYIndent.Value.HasValue && regNumberYIndent.Value < 0)
              args.AddError(Docflow.Resources.MarkCoordinatesMustBePositive, regNumberYIndent);
            if (regDateXIndent.Value.HasValue && regDateXIndent.Value < 0)
              args.AddError(Docflow.Resources.MarkCoordinatesMustBePositive, regDateXIndent);
            if (regDateYIndent.Value.HasValue && regDateYIndent.Value < 0)
              args.AddError(Docflow.Resources.MarkCoordinatesMustBePositive, regDateYIndent);
          }
        });
      
      if (dialog.Show() == addButton)
      {
        if (_obj.LastVersionApproved.GetValueOrDefault())
        {
          var signatureMark = Functions.OfficialDocument.Remote.GetOrCreateSignatureMark(_obj);
          signatureMark.Save();
        }
        else
        {
          Functions.OfficialDocument.Remote.DeleteSignatureMark(_obj);
        }
        
        if (withRegistrationData.Value.GetValueOrDefault())
        {
          var coordinatesPosition = position.Value == Sungero.Docflow.OfficialDocuments.Resources.ConvertToPdfWithMarksDialogPositionByCoordinates;
          var regNumberMark = coordinatesPosition ?
            Functions.OfficialDocument.Remote.GetOrCreateLeftTopCoordinateBasedRegistrationNumberMark(_obj, 1, regNumberXIndent.Value ?? 0, regNumberYIndent.Value ?? 0) :
            Functions.OfficialDocument.Remote.GetOrCreateTagBasedRegistrationNumberMark(_obj);
          regNumberMark.Save();
          
          var regDateMark = coordinatesPosition ?
            Functions.OfficialDocument.Remote.GetOrCreateLeftTopCoordinateBasedRegistrationDateMark(_obj, 1, regDateXIndent.Value ?? 0, regDateYIndent.Value ?? 0) :
            Functions.OfficialDocument.Remote.GetOrCreateTagBasedRegistrationDateMark(_obj);
          regDateMark.Save();
        }
        else
        {
          Functions.OfficialDocument.Remote.DeleteRegistrationNumberMark(_obj);
          Functions.OfficialDocument.Remote.DeleteRegistrationDateMark(_obj);
        }
        
        return true;
      }
      
      return false;
    }
    
    /// <summary>
    /// Получить индекс значения по умолчанию для вида расположения отметок.
    /// </summary>
    /// <param name="regNumberMark">Отметка о рег. номере.</param>
    /// <param name="regDateMark">Отметка о рег. дате.</param>
    /// <returns>Индекс.</returns>
    public virtual int GetDefaultPositionIndex(IMark regNumberMark, IMark regDateMark)
    {
      var isRegNumberMarkWithoutTags = regNumberMark == null || !regNumberMark.Tags.Any();
      var isRegDateMarkWithoutTags = regDateMark == null || !regDateMark.Tags.Any();
      if (isRegNumberMarkWithoutTags && isRegDateMarkWithoutTags && (regNumberMark != null || regDateMark != null))
        return 1;
      
      return 0;
    }
    
    #endregion

    #region Регистрация, нумерация и резервирование

    /// <summary>
    /// Вызвать диалог регистрации.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="dialogParams">Параметры диалога.</param>
    /// <returns>Результаты диалога регистрации.</returns>
    public static DialogResult RunRegistrationDialog(IOfficialDocument document, IDialogParamsLite dialogParams)
    {
      var currentRegistrationNumber = dialogParams.CurrentRegistrationNumber;
      var hasCurrentNumber = !string.IsNullOrWhiteSpace(currentRegistrationNumber);
      var currentRegistrationDate = dialogParams.CurrentRegistrationDate;
      var defaultDate = currentRegistrationDate.HasValue ? currentRegistrationDate.Value : Calendar.UserToday;
      var numberValidationDisabled = dialogParams.IsNumberValidationDisabled;
      var isClerk = dialogParams.IsClerk;
      var operation = dialogParams.Operation;
      var useObsoleteRegNumberGeneration = Functions.Module.Remote.GetDocflowParamsBoolValue(PublicConstants.Module.UseObsoleteRegNumberGenerationParamName);
      var formatItems = Functions.DocumentRegister.GetNumberFormatItemsValues(document);
      
      var buttonName = string.Empty;
      var dialogTitle = string.Empty;
      var registrationNumberLabel = string.Empty;
      var helpCode = string.Empty;
      if (Equals(operation.Value, Docflow.RegistrationSetting.SettingType.Reservation.Value))
      {
        buttonName = OfficialDocuments.Resources.ReservationButtonName;
        dialogTitle = OfficialDocuments.Resources.ReservationTitle;
        registrationNumberLabel = OfficialDocuments.Resources.RegistrationNumber;
        helpCode = Constants.OfficialDocument.HelpCode.Reservation;
      }
      else if (Equals(operation.Value, Docflow.RegistrationSetting.SettingType.Numeration.Value))
      {
        buttonName = OfficialDocuments.Resources.AlternativeOkButtonName;
        dialogTitle = OfficialDocuments.Resources.AlternativeTitle;
        registrationNumberLabel = OfficialDocuments.Resources.AlternativeRegistrationNumber;
        helpCode = Constants.OfficialDocument.HelpCode.Numeration;
      }
      else
      {
        buttonName = OfficialDocuments.Resources.RegistrationButtonName;
        dialogTitle = OfficialDocuments.Resources.RegistrationTitle;
        registrationNumberLabel = OfficialDocuments.Resources.RegistrationNumber;
        helpCode = Constants.OfficialDocument.HelpCode.Registration;
      }
      
      var dialog = Dialogs.CreateInputDialog(dialogTitle);
      dialog.HelpCode = helpCode;
      var registers = DocumentRegisters.GetAll(dr => dialogParams.RegistersIds.Contains(dr.Id));
      var register = dialog.AddSelect(OfficialDocuments.Resources.DialogRegistrationLog, true, dialogParams.DefaultRegister).From(registers);
      var date = dialog.AddDate(OfficialDocuments.Resources.DialogRegistrationDate, true, defaultDate);
      var isManual = dialog.AddBoolean(OfficialDocuments.Resources.IsManualNumber, hasCurrentNumber);
      var number = dialog.AddString(registrationNumberLabel, true)
        .WithLabel(OfficialDocuments.Resources.IsPreliminaryNumber)
        .MaxLength(document.Info.Properties.RegistrationNumber.Length);
      number.Value = hasCurrentNumber ? currentRegistrationNumber : dialogParams.NextNumber;
      number.IsLabelVisible = !hasCurrentNumber;
      var hyperlink = dialog.AddHyperlink(OfficialDocuments.Resources.LogNumberList);
      var button = dialog.Buttons.AddCustom(buttonName);
      dialog.Buttons.Default = button;
      dialog.Buttons.AddCancel();
      
      // Отчет доступен в документах с валидацией регномера при регистрации или резервировании делопроизводителем.
      hyperlink.IsVisible = operation == Docflow.RegistrationSetting.SettingType.Registration ||
        (operation == Docflow.RegistrationSetting.SettingType.Reservation && isClerk);
      
      // Номер по умолчанию недоступен для изменения.
      number.IsEnabled = hasCurrentNumber;
      
      // Журнал выбирать может только делопроизводитель, остальные видят, но менять не могут.
      register.IsEnabled = isClerk;
      register.IsVisible = operation == Docflow.RegistrationSetting.SettingType.Registration ||
        operation == Docflow.RegistrationSetting.SettingType.Reservation;
      
      dialog.SetOnRefresh((e) =>
                          {
                            button.IsEnabled = false;
                            if (date.Value != null && date.Value < Calendar.SqlMinValue)
                            {
                              e.AddError(Sungero.Docflow.OfficialDocuments.Resources.SetCorrectDate, date);
                              return;
                            }
                            hyperlink.IsEnabled = register.Value != null;
                            if (register.Value == null || !date.Value.HasValue)
                              return;
                            if (number.Value != null && number.Value.Length > document.Info.Properties.RegistrationNumber.Length)
                            {
                              var message = string.Format(Docflow.Resources.PropertyLengthError,
                                                          document.Info.Properties.RegistrationNumber.LocalizedName,
                                                          document.Info.Properties.RegistrationNumber.Length);
                              e.AddError(message, number);
                              return;
                            }
                            var numberSectionsError = Functions.DocumentRegister.CheckDocumentRegisterSections(register.Value, document);
                            var hasSectionsError = !string.IsNullOrWhiteSpace(numberSectionsError);
                            // Возможен корректировочный постфикс или нет (возможен, если необходимо проверять на уникальность).
                            var correctingPostfixInNumberIsAvailable = Functions.OfficialDocument.CheckRegistrationNumberUnique(document);
                            var numberFormatError = string.Empty;
                            if (!hasSectionsError)
                            {
                              numberFormatError = useObsoleteRegNumberGeneration
                                ? Functions.DocumentRegister.CheckRegistrationNumberFormat(register.Value, date.Value, number.Value,
                                                                                           formatItems.DepartmentCode, formatItems.BusinessUnitCode,
                                                                                           formatItems.CaseFileIndex, formatItems.DocumentKindCode,
                                                                                           formatItems.CounterpartyCode, formatItems.LeadingDocumentNumber,
                                                                                           correctingPostfixInNumberIsAvailable)
                                : Functions.DocumentRegister.CheckRegistrationNumberFormat(register.Value, document, date.Value, number.Value,
                                                                                           correctingPostfixInNumberIsAvailable);
                            }
                            var hasFormatError = !string.IsNullOrWhiteSpace(numberFormatError);
                            
                            if (!hasSectionsError && !hasFormatError)
                            {
                              button.IsEnabled = true;
                              return;
                            }
                            
                            var error = hasSectionsError ? numberSectionsError : numberFormatError;
                            if (numberValidationDisabled && isManual.Value.Value)
                            {
                              button.IsEnabled = true;
                              e.AddWarning(error);
                            }
                            else
                              e.AddError(error, number);
                          });
      
      dialog.SetOnButtonClick((e) =>
                              {
                                if (!Equals(e.Button, button))
                                  return;
                                
                                if (e.IsValid && isManual.Value.Value && date.Value.HasValue && register.Value != null)
                                {
                                  var isRegistrationNumberUnique = useObsoleteRegNumberGeneration
                                    ? Functions.DocumentRegister.Remote.IsRegistrationNumberUnique(register.Value, document, number.Value, 0, date.Value.Value,
                                                                                                   formatItems.DepartmentCode, formatItems.BusinessUnitCode, formatItems.CaseFileIndex,
                                                                                                   formatItems.DocumentKindCode, formatItems.CounterpartyCode, formatItems.LeadingDocumentId)
                                    : Functions.DocumentRegister.Remote.IsRegistrationNumberUnique(register.Value, document, number.Value, 0, date.Value.Value);
                                  if (!isRegistrationNumberUnique)
                                    e.AddError(OfficialDocuments.Resources.RegistrationNumberIsNotUnique, number);
                                }
                              });

      register.SetOnValueChanged((e) =>
                                 {
                                   hyperlink.IsEnabled = e.NewValue != null && date.Value.HasValue;
                                   
                                   number.IsEnabled = isManual.Value.Value && e.NewValue != null && date.Value.HasValue;
                                   
                                   if (e.NewValue != null)
                                   {
                                     var previewDate = date.Value ?? Calendar.UserToday;
                                     var previewNumber = useObsoleteRegNumberGeneration
                                       ? Functions.DocumentRegister.Remote.GetNextNumber(e.NewValue, previewDate, formatItems.LeadingDocumentId, document,
                                                                                         formatItems.LeadingDocumentNumber, formatItems.DepartmentId, formatItems.BusinessUnitId,
                                                                                         formatItems.CaseFileIndex, formatItems.DocumentKindCode,
                                                                                         Constants.OfficialDocument.DefaultIndexLeadingSymbol)
                                       : Functions.DocumentRegister.Remote.GetNextNumber(e.NewValue, document, previewDate);
                                     number.Value = previewNumber;
                                   }
                                   else
                                     number.Value = string.Empty;
                                 });
      
      date.SetOnValueChanged((e) =>
                             {
                               if (e.NewValue != null && e.NewValue < Calendar.SqlMinValue)
                                 return;
                               
                               hyperlink.IsEnabled = e.NewValue != null && register.Value != null;
                               
                               number.IsEnabled = isManual.Value.Value && register.Value != null && e.NewValue.HasValue;
                               
                               if (!isManual.Value.Value)
                               {
                                 if (register.Value != null)
                                 {
                                   var previewDate = e.NewValue ?? Calendar.UserToday;
                                   var previewNumber = useObsoleteRegNumberGeneration
                                     ? Functions.DocumentRegister.Remote.GetNextNumber(register.Value, previewDate, formatItems.LeadingDocumentId, document,
                                                                                       formatItems.LeadingDocumentNumber, formatItems.DepartmentId, formatItems.BusinessUnitId,
                                                                                       formatItems.CaseFileIndex, formatItems.DocumentKindCode,
                                                                                       Constants.OfficialDocument.DefaultIndexLeadingSymbol)
                                     : Functions.DocumentRegister.Remote.GetNextNumber(register.Value, document, previewDate);
                                   number.Value = previewNumber;
                                 }
                                 else
                                   number.Value = string.Empty;
                               }
                             });
      
      isManual.SetOnValueChanged((e) =>
                                 {
                                   number.IsEnabled = e.NewValue.Value && register.Value != null && date.Value.HasValue;
                                   
                                   if (register.Value != null)
                                   {
                                     var previewDate = date.Value ?? Calendar.UserToday;
                                     var previewNumber = useObsoleteRegNumberGeneration
                                       ? Functions.DocumentRegister.Remote.GetNextNumber(register.Value, previewDate, formatItems.LeadingDocumentId, document,
                                                                                         formatItems.LeadingDocumentNumber, formatItems.DepartmentId, formatItems.BusinessUnitId,
                                                                                         formatItems.CaseFileIndex, formatItems.DocumentKindCode,
                                                                                         Constants.OfficialDocument.DefaultIndexLeadingSymbol)
                                       : Functions.DocumentRegister.Remote.GetNextNumber(register.Value, document, previewDate);
                                     number.Value = previewNumber;
                                   }
                                   else
                                     number.Value = string.Empty;
                                   number.IsLabelVisible = !e.NewValue.Value;
                                 });
      
      hyperlink.SetOnExecute(() =>
                             {
                               var report = Reports.GetSkippedNumbersReport();
                               report.DocumentRegisterId = register.Value.Id;
                               report.RegistrationDate = date.Value;
                               
                               // Если в журнале есть разрез по ведущему документу, подразделению или НОР, то заполнить из документа соответствующее свойство отчёта.
                               if (register.Value.NumberingSection == DocumentRegister.NumberingSection.LeadingDocument)
                                 report.LeadingDocument = document.LeadingDocument;
                               
                               if (register.Value.NumberingSection == DocumentRegister.NumberingSection.Department)
                                 report.Department = document.Department;
                               
                               if (register.Value.NumberingSection == DocumentRegister.NumberingSection.BusinessUnit)
                                 report.BusinessUnit = document.BusinessUnit;
                               
                               report.Open();
                             });
      
      if (dialog.Show() == button)
      {
        return DialogResult.Create(register.Value, date.Value.Value, isManual.Value.Value ? number.Value : string.Empty);
      }
      
      return null;
    }
    
    /// <summary>
    /// Зарегистрировать документ.
    /// </summary>
    /// <param name="e">Аргумент действия.</param>
    public void Register(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      // Регистрация документа с зарезервированным номером.
      if (_obj.RegistrationState == RegistrationState.Reserved)
      {
        this.RegisterWithReservedNumber(e);
        return;
      }
      
      // Список доступных журналов.
      var dialogParams = Functions.OfficialDocument.Remote.GetRegistrationDialogParams(_obj, Docflow.RegistrationSetting.SettingType.Registration);

      // Проверить возможность выполнения действия.
      if (dialogParams.RegistersIds == null || !dialogParams.RegistersIds.Any())
      {
        e.AddError(Sungero.Docflow.Resources.NoDocumentRegistersAvailable);
        return;
      }

      // Вызвать диалог.
      var result = Functions.OfficialDocument.RunRegistrationDialog(_obj, dialogParams);
      
      if (result != null)
      {
        Functions.OfficialDocument.RegisterDocument(_obj, result.Register, result.Date, result.Number, false, true);
        
        Dialogs.NotifyMessage(Docflow.Resources.SuccessRegisterNotice);
      }
      return;
    }
    
    /// <summary>
    /// Зарегистрировать документ с зарезервированным номером.
    /// </summary>
    /// <param name="e">Аргумент действия.</param>
    public void RegisterWithReservedNumber(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Валидация зарезервированных номеров, кроме тех, что добавлены в исключения.
      var numberValidationDisabled = Docflow.Functions.OfficialDocument.Remote.IsNumberValidationDisabled(_obj);
      if (!numberValidationDisabled)
      {
        var validationNumber = Functions.DocumentRegister.CheckRegistrationNumberFormat(_obj.DocumentRegister, _obj);
        if (!string.IsNullOrEmpty(validationNumber))
        {
          e.AddError(validationNumber);
          return;
        }
      }
      
      // Список ИД доступных журналов.
      var documentRegistersIds = Functions.OfficialDocument.GetDocumentRegistersIdsByDocument(_obj, Docflow.RegistrationSetting.SettingType.Registration);
      
      // Проверить возможность выполнения действия.
      if (_obj.DocumentRegister != null && !documentRegistersIds.Contains(_obj.DocumentRegister.Id))
      {
        e.AddError(Sungero.Docflow.Resources.NoRightToRegistrationInDocumentRegister);
        return;
      }

      var registrationData = string.Format("{0}:\n{1} - {2}\n{3} - {4}\n{5} - {6}",
                                           Docflow.Resources.ConfirmRegistrationWithFollowingData,
                                           Docflow.Resources.RegistrationNumber, _obj.RegistrationNumber,
                                           Docflow.Resources.RegistrationDate, _obj.RegistrationDate.Value.ToUserTime().ToShortDateString(),
                                           Docflow.Resources.DocumentRegister, _obj.DocumentRegister);

      // Диалог регистрации с зарезервированным номером.
      var reservedRegistration = Dialogs.CreateTaskDialog(Docflow.Resources.DocumentRegistration, registrationData);
      var reservedRegister = reservedRegistration.Buttons.AddCustom(Docflow.Resources.Register);
      reservedRegistration.Buttons.Default = reservedRegister;
      reservedRegistration.Buttons.AddCancel();

      if (reservedRegistration.Show() == reservedRegister)
      {
        Functions.OfficialDocument.RegisterDocument(_obj, _obj.DocumentRegister, _obj.RegistrationDate,
                                                    _obj.RegistrationNumber, false, true);
        Dialogs.NotifyMessage(Docflow.Resources.SuccessRegisterNotice);
      }
      else
        return;
    }
    
    /// <summary>
    /// Зарезервировать номер.
    /// </summary>
    /// <param name="e">Аргумент действия.</param>
    public void ReserveNumber(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      // Список доступных журналов.
      var dialogParams = Functions.OfficialDocument.Remote.GetRegistrationDialogParams(_obj, Docflow.RegistrationSetting.SettingType.Reservation);

      // Проверить возможность выполнения действия.
      if (dialogParams.RegistersIds == null || !dialogParams.RegistersIds.Any())
      {
        e.AddError(Sungero.Docflow.Resources.NoDocumentRegistersAvailableForReserve);
        return;
      }

      if (dialogParams.RegistersIds.Count > 1 && !dialogParams.IsClerk)
      {
        e.AddError(Sungero.Docflow.Resources.ReserveSettingsRequired);
        return;
      }

      // Вызвать диалог.
      var result = Functions.OfficialDocument.RunRegistrationDialog(_obj, dialogParams);

      if (result != null)
      {
        Functions.OfficialDocument.RegisterDocument(_obj, result.Register, result.Date, result.Number, true, true);
      }
      return;
    }

    /// <summary>
    /// Присвоить номер.
    /// </summary>
    /// <param name="e">Аргумент действия.</param>
    public void AssignNumber(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;

      // Список доступных журналов.
      var dialogParams = Functions.OfficialDocument.Remote.GetRegistrationDialogParams(_obj, Docflow.RegistrationSetting.SettingType.Numeration);

      // Проверить возможность выполнения действия.
      if (dialogParams.RegistersIds == null || !dialogParams.RegistersIds.Any())
      {
        e.AddError(Sungero.Docflow.Resources.NumberingSettingsRequired);
        return;
      }

      if (dialogParams.RegistersIds.Count > 1)
      {
        e.AddError(Sungero.Docflow.Resources.NumberingSettingsRequired);
        return;
      }

      // Вызвать диалог.
      var result = Functions.OfficialDocument.RunRegistrationDialog(_obj, dialogParams);
      
      if (result != null)
      {
        Functions.OfficialDocument.RegisterDocument(_obj, result.Register, result.Date, result.Number, false, true);
        
        Dialogs.NotifyMessage(Docflow.Resources.SuccessNumerationNotice);
      }
      return;
    }
    
    #endregion

    #region Отправка по email
    
    /// <summary>
    /// Получить связанные документы, имеющие версии.
    /// </summary>
    /// <returns>Список связанных документов.</returns>
    public virtual List<IOfficialDocument> GetRelatedDocumentsWithVersions()
    {
      var addendumRelatedDocuments = Docflow.Functions.OfficialDocument.Remote.GetRelatedDocumentsByRelationType(_obj, Docflow.Constants.Module.AddendumRelationName, true);
      var simpleRelatedDocuments = Docflow.Functions.OfficialDocument.Remote.GetRelatedDocumentsByRelationType(_obj, Docflow.Constants.Module.SimpleRelationName, true);
      
      addendumRelatedDocuments = addendumRelatedDocuments.OrderBy(x => x.Name).ToList();
      simpleRelatedDocuments = simpleRelatedDocuments.OrderBy(x => x.Name).ToList();
      
      var relatedDocuments = new List<IOfficialDocument>();
      relatedDocuments.AddRange(addendumRelatedDocuments);
      relatedDocuments.AddRange(simpleRelatedDocuments);
      
      // TODO Dmitirev_IA: Опасно для более 2000 документов.
      relatedDocuments = relatedDocuments.Distinct().ToList();
      return relatedDocuments;
    }
    
    /// <summary>
    /// Создание письма с вложенными документами.
    /// </summary>
    /// <param name="email">Почта для отправки письма.</param>
    /// <param name="attachments">Список вложений.</param>
    public virtual void CreateEmail(string email, List<IOfficialDocument> attachments)
    {
      var subject = Sungero.Docflow.OfficialDocuments.Resources.SendByEmailSubjectPrefixFormat(_obj.Name);
      
      var documents = new List<IElectronicDocument>() { _obj };
      documents.AddRange(attachments);
      
      Functions.Module.CreateEmail(email, subject, documents);
    }
    
    /// <summary>
    /// Получение информации о блокировке последней версии документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Информация о блокировке.</returns>
    [Obsolete("Метод не используется с 27.03.2024 и версии 4.10. Используйте одноименный модульный метод.")]
    public static Domain.Shared.LockInfo GetDocumentLastVersionLockInfo(IOfficialDocument document)
    {
      if (document == null || !document.HasVersions)
        return null;
      
      var body = document.LastVersion.Body;
      var publicBody = document.LastVersion.PublicBody;
      
      if (publicBody != null && publicBody.Id != null)
        return Locks.GetLockInfo(publicBody);
      
      return Locks.GetLockInfo(body);
    }
    
    /// <summary>
    /// Проверить информацию о блокировках тела и карточки документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Информация о блокировке.</returns>
    public static Domain.Shared.LockInfo GetDocumentLockInfo(IOfficialDocument document)
    {
      if (document == null)
        return null;
      
      var lockInfo = Functions.Module.GetDocumentLastVersionLockInfo(document);
      var canSignLockedDocument = Functions.OfficialDocument.CanSignLockedDocument(document);
      if ((lockInfo == null || !lockInfo.IsLocked) && !canSignLockedDocument)
        lockInfo = Locks.GetLockInfo(document);
      
      return lockInfo;
    }
    
    /// <summary>
    /// Проверить наличие блокировок последних версий документов.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <returns>Если заблокированы - True, свободны - False.</returns>
    [Obsolete("Метод не используется с 27.03.2024 и версии 4.10. Используйте модульный метод AllowCreatingEmailWithLockedVersions.")]
    public bool HaveLastVersionLocks(List<IOfficialDocument> documents)
    {
      var lockInfos = new List<Domain.Shared.LockInfo>();
      
      var lockDocumentName = string.Empty;
      foreach (var doc in documents)
      {
        var lockInfo = GetDocumentLastVersionLockInfo(doc);
        if (lockInfo != null && lockInfo.IsLocked)
        {
          lockInfos.Add(lockInfo);
          lockDocumentName = doc.Name;
        }
      }
      
      if (lockInfos.Count == 0)
        return false;
      
      string description = null;
      var text = string.Empty;
      var title = string.Empty;
      
      if (lockInfos.Count == 1)
      {
        var info = lockInfos.First();
        if (info != null)
          description = info.LockedMessage;
        text = Sungero.Docflow.OfficialDocuments.Resources.VersionBeingSentMightBeOutdatedFormat(lockDocumentName);
        title = Sungero.Docflow.OfficialDocuments.Resources.DocumentIsBeingEdited;
      }
      else
      {
        text = Sungero.Docflow.OfficialDocuments.Resources.VersionsBeingSentMightBeOutdated;
        title = Sungero.Docflow.OfficialDocuments.Resources.SeveralDocumentsAreBeingEdited;
      }
      
      var errDialog = Dialogs.CreateTaskDialog(text, description, MessageType.Information, title);
      var retry = errDialog.Buttons.AddRetry();
      var send = errDialog.Buttons.AddCustom(Sungero.Docflow.OfficialDocuments.Resources.DialogButtonSend);
      var cancel = errDialog.Buttons.AddCancel();
      var result = errDialog.Show();
      
      if (result == retry)
      {
        return this.HaveLastVersionLocks(documents);
      }
      
      if (result == send)
        return false;
      else
        return true;
    }
    
    /// <summary>
    /// Выбор связанных документов для отправки и создания письма.
    /// </summary>
    /// <param name="relatedDocuments">Связанные документы.</param>
    [Public]
    public virtual void SelectRelatedDocumentsAndCreateEmail(List<IOfficialDocument> relatedDocuments)
    {
      var addressees = Functions.OfficialDocument.GetEmailAddressees(_obj);
      if ((addressees == null || !addressees.Any()) && (relatedDocuments == null || !relatedDocuments.Any()))
      {
        if (Functions.Module.AllowCreatingEmailWithLockedVersions(new List<IElectronicDocument>() { _obj }))
          this.CreateEmail(string.Empty, relatedDocuments);
        return;
      }
      var dialog = Dialogs.CreateInputDialog(OfficialDocuments.Resources.SendByEmailDialogTitle);
      dialog.HelpCode = Constants.OfficialDocument.HelpCode.SendByEmail;
      dialog.Text = OfficialDocuments.Resources.SendByEmailDialogText;
      
      var addresseeString = dialog.AddString(OfficialDocuments.Resources.StateViewTo, false);
      var addresseesFromDocument = dialog
        .AddSelect(OfficialDocuments.Resources.StateViewTo, false)
        .From(addressees.Select(x => x.Label).ToArray());
      
      var mainDocument = dialog.AddSelect(OfficialDocuments.Resources.SendByEmailDialogMainDocument, true, _obj);
      mainDocument.IsEnabled = false;
      var selectedRelations = dialog
        .AddSelectMany(OfficialDocuments.Resources.SendByEmailDialogAttachments, false, OfficialDocuments.Null)
        .From(relatedDocuments);
      
      // Установить доступность и видимость полей диалога.
      selectedRelations.IsEnabled = relatedDocuments.Any();
      addresseesFromDocument.IsVisible = addressees.Any();
      addresseeString.IsVisible = !addressees.Any();
      var sendButton = dialog.Buttons.AddCustom(Sungero.Docflow.OfficialDocuments.Resources.SendButtonText);
      dialog.Buttons.AddCancel();
      
      if (dialog.Show() == sendButton)
      {
        var allDocs = selectedRelations.Value.ToList<IElectronicDocument>();
        allDocs.Add(_obj);
        if (Functions.Module.AllowCreatingEmailWithLockedVersions(allDocs))
        {
          var email = string.Empty;
          if (!string.IsNullOrWhiteSpace(addresseesFromDocument.Value))
            email = addressees.SingleOrDefault(x => addresseesFromDocument.Value == x.Label).Email;
          else if (!string.IsNullOrWhiteSpace(addresseeString.Value))
            email = addresseeString.Value;
          
          this.CreateEmail(email, selectedRelations.Value.ToList());
        }
      }
    }
    
    #endregion
    
    #region Диалог создания поручений по телу документа
    
    /// <summary>
    /// Отобразить диалог создания поручений по документу.
    /// </summary>
    /// <param name="e">Аргументы действия, чтобы показывать ошибки валидации.</param>
    [Public]
    public virtual void CreateActionItemsFromDocumentDialog(Sungero.Core.IValidationArgs e)
    {
      var currentUser = Sungero.Company.Employees.Current;
      if (currentUser == null || currentUser.IsSystem == true)
      {
        Dialogs.NotifyMessage(OfficialDocuments.Resources.ActionItemCreationDialogLoginAsEmployeeError);
        return;
      }

      var dialogHeightNormal = 160;
      var dialogHeightSmall = 80;
      var existingActionItems = PublicFunctions.OfficialDocument.Remote.GetCreatedActionItems(_obj);
      var draftActionItems = existingActionItems.Where(x => x.Status == RecordManagement.ActionItemExecutionTask.Status.Draft &&
                                                       x.ParentTask == null && x.ParentAssignment == null && x.IsDraftResolution != true).ToList();
      
      var dialogItems = new List<RecordManagement.IActionItemExecutionTask>();
      
      var hasNotDeletedActionItems = false;
      var hasBeenSent = false;
      var beforeExitDialogText = string.Empty;
      
      var stepExistingItems = existingActionItems.Any();
      
      if (!stepExistingItems)
      {
        if (!this.TryCreateActionItemsFromDocument(dialogItems, e))
          return;
      }

      var dialog = Dialogs.CreateInputDialog(OfficialDocuments.Resources.ActionItemCreationDialog);
      dialog.Height = dialogHeightSmall;
      dialog.HelpCode = Constants.OfficialDocument.HelpCode.CreateActionItems;
      
      var next = dialog.Buttons.AddCustom(OfficialDocuments.Resources.ActionItemCreationDialogContinueButtonText);
      var close = dialog.Buttons.AddCustom(OfficialDocuments.Resources.ActionItemCreationDialogCloseButtonText);
      var cancel = dialog.Buttons.AddCustom(OfficialDocuments.Resources.CancelButtonText);
      var existingLink = dialog.AddHyperlink(OfficialDocuments.Resources.ActionItemCreationDialogExistedActionItems);
      existingLink.IsVisible = false;
      var failedLink = dialog.AddHyperlink(OfficialDocuments.Resources.ActionItemCreationDialogNotFilledActionItems);
      failedLink.IsVisible = false;
      
      // Принудительно увеличиваем ширину диалога для корректного отображения кнопок.
      var fakeControl = dialog.AddString("123", false);
      fakeControl.IsVisible = false;
      
      Action<CommonLibrary.InputDialogRefreshEventArgs> refresh = _ =>
      {
        if (stepExistingItems)
        {
          dialog.Height = dialogHeightNormal;
          next.Name = OfficialDocuments.Resources.ActionItemCreationDialogContinueButtonText;
          close.IsVisible = false;
          cancel.IsVisible = true;
          
          var descriptionText = string.Empty;
          var prefix = string.Empty;
          var actionItemDraftExist = OfficialDocuments.Resources.ActionItemCreationDialogDraftExists +
            Environment.NewLine + Environment.NewLine;
          
          if (draftActionItems.Any())
          {
            prefix = actionItemDraftExist;
            descriptionText += OfficialDocuments.Resources.ActionItemCreationDialogDraftWillBeDelete +
              Environment.NewLine + Environment.NewLine;
          }
          
          if (existingActionItems.Where(с => с.Status != RecordManagement.ActionItemExecutionTask.Status.Draft).Any())
          {
            prefix = actionItemDraftExist;
            descriptionText += OfficialDocuments.Resources.ActionItemCreationDialogInProcessExists;
          }
          
          if (existingActionItems.Count() == 0)
          {
            descriptionText += OfficialDocuments.Resources.ActionItemCreationDialogNoDraftAndInProgressExist +
              Environment.NewLine + Environment.NewLine;
            descriptionText += OfficialDocuments.Resources.ActionItemCreationDialogToCreateActionItemsPressNext;
          }
          
          dialog.Text = prefix + descriptionText;
          
          existingLink.IsVisible = existingActionItems.Any();
        }
        else
        {
          close.IsVisible = true;
          cancel.IsVisible = false;
          
          failedLink.IsVisible = NeedFillPropertiesItems(dialogItems).Any();

          var isAllSent = dialogItems.All(d => d.Status != RecordManagement.ActionItemExecutionTask.Status.Draft);
          next.IsVisible = !isAllSent;
          
          existingLink.IsVisible = dialogItems.Any();
          existingLink.Title = dialogItems.Any() ?
            OfficialDocuments.Resources.ActionItemCreationDialogCreatedActionItems :
            existingLink.Title;
          
          next.Name = OfficialDocuments.Resources.ActionItemCreationDialogSendForExecutionButtonText;
          close.Name  = isAllSent ?
            OfficialDocuments.Resources.ActionItemCreationDialogCloseButtonText :
            OfficialDocuments.Resources.ActionItemCreationDialogDeleteAndCloseButtonText;
          
          dialog.Text = string.Empty;
          if (hasNotDeletedActionItems)
            dialog.Text += OfficialDocuments.Resources.ActionItemCreationDialogSomeActionItemsCouldNotBeDeleted +
              Environment.NewLine + Environment.NewLine;

          if (!hasBeenSent && dialogItems.Any())
            dialog.Text += OfficialDocuments.Resources.ActionItemCreationDialogSuccessfullyCreated +
              Environment.NewLine + Environment.NewLine;
          
          dialog.Text += OfficialDocuments.Resources
            .ActionItemCreationDialogCreateCompletedActionItemsFormat(dialogItems.Count) + Environment.NewLine;
          
          if (dialogItems.Where(i => i.Status == RecordManagement.ActionItemExecutionTask.Status.InProcess).Any())
            dialog.Text += string.Format("  - {0} - {1}{2}", OfficialDocuments.Resources.ActionItemCreationDialogSended,
                                         dialogItems.Count(i => i.Status == RecordManagement.ActionItemExecutionTask.Status.InProcess),
                                         Environment.NewLine);
          
          if (NeedFillPropertiesItems(dialogItems).Any())
          {
            dialog.Height = dialogHeightNormal;
            var dialogItemsNeedFillProperties = NeedFillPropertiesItems(dialogItems).ToList();
            
            var notFilledAssigneeCount = dialogItemsNeedFillProperties.Count(t => t.Assignee == null);
            if (notFilledAssigneeCount != 0)
              dialog.Text += string.Format("  - {0} - {1}{2}", OfficialDocuments.Resources.ActionItemCreationDialogNeedFillAssignee,
                                           notFilledAssigneeCount, Environment.NewLine);
            
            var notFilledDeadlineCount = dialogItemsNeedFillProperties.Count(t => t.Deadline == null && t.HasIndefiniteDeadline != true);
            if (notFilledDeadlineCount != 0)
              dialog.Text += string.Format("  - {0} - {1}{2}", OfficialDocuments.Resources.ActionItemCreationDialogNeedFillDeadline,
                                           notFilledDeadlineCount, Environment.NewLine);
            
            var notFilledActionItemCount = dialogItemsNeedFillProperties.Count(t => string.IsNullOrWhiteSpace(t.ActionItem));
            if (notFilledActionItemCount != 0)
              dialog.Text += string.Format("  - {0} - {1}{2}", OfficialDocuments.Resources.ActionItemCreationDialogNeedFillSubject,
                                           notFilledActionItemCount, Environment.NewLine);
          }
          
          // В Web перед закрытием диалога вызывается refresh. Исключаем кратковременное отображение некорректных данных в диалоге.
          if (!string.IsNullOrEmpty(beforeExitDialogText))
            dialog.Text = beforeExitDialogText;
        }
        
      };
      
      failedLink.SetOnExecute(() =>
                              {
                                // Список "Требуют заполнения".
                                NeedFillPropertiesItems(dialogItems).ToList().ShowModal();
                                dialogItems = RefreshDialogItems(dialogItems);
                                refresh.Invoke(null);
                              });
      
      existingLink.SetOnExecute(() =>
                                {
                                  // Список "Поручения".
                                  if (stepExistingItems)
                                  {
                                    existingActionItems.ToList().ShowModal();
                                    existingActionItems = PublicFunctions.OfficialDocument.Remote.GetCreatedActionItems(_obj);
                                    draftActionItems = existingActionItems
                                      .Where(m => m.Status == RecordManagement.ActionItemExecutionTask.Status.Draft &&
                                             m.ParentTask == null && m.ParentAssignment == null && m.IsDraftResolution != true).ToList();
                                    refresh.Invoke(null);
                                  }
                                  else
                                  {
                                    // Список "Созданные поручения".
                                    dialogItems.ToList().ShowModal();
                                    dialogItems = RefreshDialogItems(dialogItems);
                                    refresh.Invoke(null);
                                  }
                                });
      
      dialog.SetOnButtonClick(x =>
                              {
                                x.CloseAfterExecute = false;
                                
                                if (x.Button == next)
                                {
                                  if (stepExistingItems)
                                  {
                                    if (this.TryCreateActionItemsFromDocument(dialogItems, e))
                                    {
                                      if (!TryDeleteActionItemTasks(draftActionItems))
                                        hasNotDeletedActionItems = true;
                                      stepExistingItems = false;
                                      refresh.Invoke(null);
                                    }
                                    else
                                      x.CloseAfterExecute = true;
                                  }
                                  else
                                  {
                                    if (NeedFillPropertiesItems(dialogItems).Any())
                                    {
                                      x.AddError(OfficialDocuments.Resources.ActionItemCreationDialogNeedFillBeforeSending);
                                    }
                                    else
                                    {
                                      var tasksToStart = NoNeedFillPropertiesItems(dialogItems).ToList();
                                      PublicFunctions.OfficialDocument.StartActionItemTasksFromDialog(_obj, tasksToStart);
                                      hasBeenSent = true;
                                      x.CloseAfterExecute = true;
                                    }
                                  }
                                }
                                
                                if (x.Button == close)
                                {
                                  x.CloseAfterExecute = true;
                                  
                                  if (dialogItems.All(d => d.Status != RecordManagement.ActionItemExecutionTask.Status.Draft))
                                    return;
                                  
                                  if (TryDeleteActionItemTasks(dialogItems.Where(i => i.Status == RecordManagement.ActionItemExecutionTask.Status.Draft).ToList()))
                                    Dialogs.NotifyMessage(OfficialDocuments.Resources.ActionItemCreationDialogDraftWhereDeleted);
                                  else
                                  {
                                    hasNotDeletedActionItems = true;
                                    Dialogs.NotifyMessage(OfficialDocuments.Resources.ActionItemCreationDialogSomeActionItemsDraftNotDeleted);
                                  }
                                }
                              });
      dialog.SetOnRefresh(refresh);
      dialog.Show();
    }
    
    /// <summary>
    /// Создать поручения по документу.
    /// </summary>
    /// <param name="newActionItems">Созданные поручения.</param>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если поручения созданы успешно. False, если не создано ни одного или были ошибки.</returns>
    private bool TryCreateActionItemsFromDocument(List<RecordManagement.IActionItemExecutionTask> newActionItems,
                                                  IValidationArgs e)
    {
      try
      {
        newActionItems.Clear();
        newActionItems.AddRange(Functions.OfficialDocument.Remote.CreateActionItemsFromDocument(_obj));
      }
      catch (AppliedCodeException ex)
      {
        e.AddError(ex.Message);
        return false;
      }
      if (newActionItems.Count == 0)
      {
        e.AddInformation(OfficialDocuments.Resources.ActionItemCreationDialogOnlyByTags);
        return false;
      }
      return true;
    }
    
    /// <summary>
    /// Удаление поручений, созданных по документу.
    /// </summary>
    /// <param name="tasks">Список задач, которые необходимо удалить.</param>
    /// <returns>True, если все поручения были успешно удалены.</returns>
    private static bool TryDeleteActionItemTasks(List<RecordManagement.IActionItemExecutionTask> tasks)
    {
      var hasFailedTask = false;
      // Удаление производится по одной задаче из-за платформенного бага 62797.
      foreach (var task in tasks)
      {
        if (!Functions.OfficialDocument.Remote.TryDeleteActionItemTask(task.Id))
          hasFailedTask = true;
      }
      
      return !hasFailedTask;
    }
    
    /// <summary>
    /// Обновление списка поручений.
    /// </summary>
    /// <param name="items">Список поручений.</param>
    /// <returns>Обновленный список поручений.</returns>
    private static List<RecordManagement.IActionItemExecutionTask> RefreshDialogItems(List<RecordManagement.IActionItemExecutionTask> items)
    {
      return Functions.OfficialDocument.Remote.GetActionItemsExecutionTasks(items.Select(t => t.Id).ToList());
    }
    
    /// <summary>
    /// Выбрать из списка недозаполненные поручения.
    /// </summary>
    /// <param name="items">Список поручений.</param>
    /// <returns>Недозаполненные поручения.</returns>
    private static IEnumerable<RecordManagement.IActionItemExecutionTask> NeedFillPropertiesItems(List<RecordManagement.IActionItemExecutionTask> items)
    {
      return items.Where(t => t.IsCompoundActionItem != true && t.Status == RecordManagement.ActionItemExecutionTask.Status.Draft &&
                         (t.Assignee == null || (t.Deadline == null && t.HasIndefiniteDeadline != true) || string.IsNullOrWhiteSpace(t.ActionItem)));
    }
    
    /// <summary>
    /// Выбрать из списка корректно заполненные поручения.
    /// </summary>
    /// <param name="items">Список поручений.</param>
    /// <returns>Корректно заполненные поручения.</returns>
    private static IEnumerable<RecordManagement.IActionItemExecutionTask> NoNeedFillPropertiesItems(List<RecordManagement.IActionItemExecutionTask> items)
    {
      return items.Where(t => t.IsCompoundActionItem != true && t.Status == RecordManagement.ActionItemExecutionTask.Status.Draft &&
                         t.Assignee != null && (t.Deadline != null || t.HasIndefiniteDeadline == true) && !string.IsNullOrWhiteSpace(t.ActionItem) || t.IsCompoundActionItem == true);
      
    }
    
    #endregion
    
    #region Интеллектуальная обработка

    /// <summary>
    /// Включить режим верификации.
    /// </summary>
    [Public]
    public virtual void SwitchVerificationMode()
    {
      // Активировать / скрыть вкладку, подсветить свойства карточки и факты в теле только один раз при открытии.
      // Либо в событии Showing, либо в Refresh.
      // Вызов в Refresh необходим, т.к. при отмене изменений не вызывается Showing.
      if (!this.NeedHighlightPropertiesAndFacts())
        return;

      var formParams = ((Sungero.Domain.Shared.IExtendedEntity)_obj).Params;
      formParams.Add(PublicConstants.OfficialDocument.PropertiesAlreadyColoredParamName, true);
      
      // Активировать / скрыть вкладку.
      if (_obj.VerificationState != Docflow.OfficialDocument.VerificationState.InProcess)
      {
        _obj.State.Pages.PreviewPage.IsVisible = false;
        return;
      }
      _obj.State.Pages.PreviewPage.IsVisible = true;
      _obj.State.Pages.PreviewPage.Activate();
      
      this.SetHighlight();
    }
    
    /// <summary>
    /// Определить необходимость подсветки свойств в карточке и фактов.
    /// </summary>
    /// <returns>Признак необходимости подсветки.</returns>
    public virtual bool NeedHighlightPropertiesAndFacts()
    {
      var formParams = ((Sungero.Domain.Shared.IExtendedEntity)_obj).Params;
      return !formParams.ContainsKey(PublicConstants.OfficialDocument.PropertiesAlreadyColoredParamName);
    }

    /// <summary>
    /// Получить параметры отображения фокусировки подсветки.
    /// </summary>
    /// <returns>Параметры.</returns>
    public virtual IHighlightActivationStyle GetHighlightActivationStyle()
    {
      var highlightActivationStyle = HighlightActivationStyle.Create();
      highlightActivationStyle.UseBorder = PublicFunctions.Module.Remote.GetDocflowParamsStringValue(Constants.Module.HighlightActivationStyleParamNames.UseBorder);
      highlightActivationStyle.BorderColor = PublicFunctions.Module.Remote.GetDocflowParamsStringValue(Constants.Module.HighlightActivationStyleParamNames.BorderColor);
      highlightActivationStyle.BorderWidth = PublicFunctions.Module.Remote.GetDocflowParamsNumbericValue(Constants.Module.HighlightActivationStyleParamNames.BorderWidth);
      highlightActivationStyle.UseFilling = PublicFunctions.Module.Remote.GetDocflowParamsStringValue(Constants.Module.HighlightActivationStyleParamNames.UseFilling);
      highlightActivationStyle.FillingColor = PublicFunctions.Module.Remote.GetDocflowParamsStringValue(Constants.Module.HighlightActivationStyleParamNames.FillingColor);
      return highlightActivationStyle;
    }
    
    /// <summary>
    /// Получить список распознанных свойств документа.
    /// </summary>
    /// <param name="documentRecognitionInfo">Результат распознавания документа.</param>
    /// <returns>Список распознанных свойств документа.</returns>
    public virtual List<RecognizedProperty> GetRecognizedProperties(IEntityRecognitionInfo documentRecognitionInfo)
    {
      var result = new List<RecognizedProperty>();
      
      if (_obj == null || documentRecognitionInfo == null)
        return result;
      
      // Взять только заполненные свойства самого документа. Свойства-коллекции записываются через точку.
      var linkedFacts = documentRecognitionInfo.Facts
        .Where(x => !string.IsNullOrEmpty(x.PropertyName) && !x.PropertyName.Any(с => с == '.'));
      
      // Взять только неизмененные пользователем свойства.
      var type = _obj.GetType();
      foreach (var linkedFact in linkedFacts)
      {
        var propertyName = linkedFact.PropertyName;
        var property = type.GetProperties().Where(p => p.Name == propertyName).LastOrDefault();
        // Пропустить факт, если свойства не существует.
        if (property == null)
          continue;
        object propertyValue = property.GetValue(_obj);
        var propertyStringValue = Commons.PublicFunctions.Module.GetValueAsString(propertyValue);
        var propertyNotChanged = !string.IsNullOrWhiteSpace(propertyStringValue) &&
          !string.IsNullOrWhiteSpace(linkedFact.PropertyValue) &&
          (Equals(propertyStringValue, linkedFact.PropertyValue) ||
           this.CanCompareAsNumbers(propertyStringValue, linkedFact.PropertyValue) &&
           this.CompareAsNumbers(propertyStringValue, linkedFact.PropertyValue) == 0);
        
        // Пропустить факт, если подобранное по нему свойство изменено.
        if (!propertyNotChanged)
          continue;
        
        // Для свойства собрать вместе все Positions.
        var recognizedProperty = result.FirstOrDefault(x => x.Name == propertyName);
        if (recognizedProperty == null)
          result.Add(RecognizedProperty.Create(propertyName, linkedFact.Probability, linkedFact.Position));
        else
          recognizedProperty.Position = string.Join(Constants.Module.PositionsDelimiter.ToString(),
                                                    recognizedProperty.Position,
                                                    linkedFact.Position);
      }
      
      return result;
    }

    /// <summary>
    /// Подсветить указанные свойства в карточке документа и факты в теле.
    /// </summary>
    /// <param name="highlightActivationStyle">Параметры отображения фокусировки подсветки.</param>
    public virtual void SetHighlightPropertiesAndFacts(IHighlightActivationStyle highlightActivationStyle)
    {
      var greaterConfidenceLimitColor = Sungero.Core.Colors.Common.LightGreen;
      var lessConfidenceLimitColor = Sungero.Core.Colors.Common.LightYellow;
      var greaterConfidenceLimitPreviewColor = Sungero.Core.Colors.Common.LightGreen;
      var lessConfidenceLimitPreviewColor = Sungero.Core.Colors.Common.LightYellow;
      
      var documentRecognitionInfo = Sungero.Commons.PublicFunctions.EntityRecognitionInfo.Remote.GetEntityRecognitionInfo(_obj);
      var propertyAttributes = this.GetRecognizedProperties(documentRecognitionInfo);
      var smartProcessingSettings = PublicFunctions.SmartProcessingSetting.GetSettings();
      if (smartProcessingSettings == null)
      {
        Logger.DebugFormat("Warning. Smart Processing Setting not found when trying to highlight document properties. (ID: {0})", _obj.Id);
        return;
      }
      var upperConfidenceLimit = smartProcessingSettings.UpperConfidenceLimit;
      
      foreach (var propertyAttribute in propertyAttributes)
      {
        var propertyColor = lessConfidenceLimitColor;
        var previewColor = lessConfidenceLimitPreviewColor;
        var propertyName = propertyAttribute.Name;
        var propertyInfo = _obj.Info.Properties.GetType().GetProperties().Where(p => p.Name == propertyName).LastOrDefault();
        var propertyInfoValue = (Sungero.Domain.Shared.IInternalPropertyInfo)propertyInfo.GetReflectionPropertyValue(_obj.Info.Properties);
        
        if (propertyAttribute.Probability != null &&
            propertyAttribute.Probability >= upperConfidenceLimit)
        {
          propertyColor = greaterConfidenceLimitColor;
          previewColor = greaterConfidenceLimitPreviewColor;
        }
        
        // Подсветка полей карточки.
        if (propertyInfoValue != null)
          _obj.State.Properties[propertyName].HighlightColor = propertyColor;
        
        // Подсветка фактов в теле документа.
        var position = propertyAttribute.Position;
        if (!string.IsNullOrWhiteSpace(position))
        {
          var fieldsPositions = position.Split(Constants.Module.PositionsDelimiter);
          foreach (var fieldPosition in fieldsPositions)
            this.HighlightFactInPreview(_obj.State.Controls.Preview, fieldPosition, previewColor,
                                        (Sungero.Domain.Shared.IPropertyInfo)propertyInfoValue, highlightActivationStyle);
        }
      }
    }

    /// <summary>
    /// Подсветить записи свойства-коллекции в карточке документа и факты в предпросмотре.
    /// </summary>
    /// <param name="previewControl">Контрол предпросмотра.</param>
    /// <param name="documentRecognitionInfo">Результат распознавания документа.</param>
    /// <param name="collection">Коллекция.</param>
    /// <param name="highlightActivationStyle">Параметры отображения фокусировки подсветки.</param>
    public virtual void HighlightCollection(Sungero.Domain.Shared.IPreviewControlState previewControl,
                                            IEntityRecognitionInfo documentRecognitionInfo,
                                            Sungero.Domain.Shared.IChildEntityCollection<Sungero.Domain.Shared.IChildEntity> collection,
                                            IHighlightActivationStyle highlightActivationStyle)
    {
      if (documentRecognitionInfo == null)
      {
        Logger.DebugFormat("Warning. Recognition info not found when trying to highlight document properties. (ID: {0})", _obj.Id);
        return;
      }
      var greaterConfidenceLimitColor = Sungero.Core.Colors.Common.LightGreen;
      var lessConfidenceLimitColor = Sungero.Core.Colors.Common.LightYellow;
      var greaterConfidenceLimitPreviewColor = Sungero.Core.Colors.Common.LightGreen;
      var lessConfidenceLimitPreviewColor = Sungero.Core.Colors.Common.LightYellow;
      
      var smartProcessingSettings = PublicFunctions.SmartProcessingSetting.GetSettings();
      if (smartProcessingSettings == null)
      {
        Logger.DebugFormat("Warning. Smart Processing Setting not found when trying to highlight document properties. (ID: {0})", _obj.Id);
        return;
      }
      
      var upperConfidenceLimit = smartProcessingSettings.UpperConfidenceLimit;
      
      var recognizedFacts = documentRecognitionInfo.Facts;
      foreach (var record in collection)
      {
        var recognizedRecordFacts = recognizedFacts.Where(x => x.CollectionRecordId == record.Id &&
                                                          !string.IsNullOrEmpty(x.PropertyName) &&
                                                          x.PropertyName.Any(с => с == '.') &&
                                                          x.Probability != null);
        foreach (var recognizedRecordFact in recognizedRecordFacts)
        {
          var propertyColor = lessConfidenceLimitColor;
          var previewColor = lessConfidenceLimitPreviewColor;
          var probability = recognizedRecordFact.Probability;
          if (probability.HasValue && probability.Value >= upperConfidenceLimit)
          {
            propertyColor = greaterConfidenceLimitColor;
            previewColor = greaterConfidenceLimitPreviewColor;
          }

          var propertyName = recognizedRecordFact.PropertyName.Split('.').LastOrDefault();
          var property = record.GetType().GetProperties().Where(p => p.Name == propertyName).LastOrDefault();
          if (property != null)
          {
            object propertyValue = property.GetValue(record);
            var propertyStringValue = Commons.PublicFunctions.Module.GetValueAsString(propertyValue);
            if (!string.IsNullOrWhiteSpace(propertyStringValue) && Equals(propertyStringValue, recognizedRecordFact.PropertyValue))
            {
              record.State.Properties[propertyName].HighlightColor = propertyColor;
              var propertyInfo = record.Info.Properties.GetType().GetProperties().Where(p => p.Name == propertyName).LastOrDefault();
              var propertyInfoValue = (Sungero.Domain.Shared.IInternalPropertyInfo)propertyInfo.GetReflectionPropertyValue(record.Info.Properties);
              if (!string.IsNullOrEmpty(recognizedRecordFact.Position))
              {
                var propertyPositions = recognizedRecordFact.Position.Split(Constants.Module.PositionsDelimiter);
                foreach (var position in propertyPositions)
                  this.HighlightFactInPreview(previewControl, position, previewColor,
                                              record, (Sungero.Domain.Shared.IPropertyInfo)propertyInfoValue,
                                              highlightActivationStyle);
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Дополнительная подсветка.
    /// </summary>
    /// <param name="documentRecognitionInfo">Результат распознавания документа.</param>
    /// <param name="highlightActivationStyle">Параметры отображения фокусировки подсветки.</param>
    public virtual void SetAdditionalHighlight(IEntityRecognitionInfo documentRecognitionInfo,
                                               IHighlightActivationStyle highlightActivationStyle)
    {
      return;
    }

    /// <summary>
    /// Подсветить факт в предпросмотре.
    /// </summary>
    /// <param name="previewControl">Контрол предпросмотра.</param>
    /// <param name="position">Позиция.</param>
    /// <param name="color">Цвет.</param>
    public virtual void HighlightFactInPreview(Sungero.Domain.Shared.IPreviewControlState previewControl, string position, Sungero.Core.Color color)
    {
      var positions = position.Split(Constants.Module.PositionElementDelimiter);
      if (positions.Count() >= 7)
        previewControl.HighlightAreas.Add(color,
                                          int.Parse(positions[0]),
                                          double.Parse(positions[1].Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture),
                                          double.Parse(positions[2].Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture),
                                          double.Parse(positions[3].Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture),
                                          double.Parse(positions[4].Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture),
                                          double.Parse(positions[5].Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture),
                                          double.Parse(positions[6].Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture));
    }
    
    /// <summary>
    /// Подсветить факт в предпросмотре с фокусировкой по нажатию на свойство.
    /// </summary>
    /// <param name="previewControl">Контрол предпросмотра.</param>
    /// <param name="position">Позиция.</param>
    /// <param name="color">Цвет.</param>
    /// <param name="propertyInfo">Информация о свойстве.</param>
    /// <param name="highlightActivationStyle">Параметры отображения фокусировки подсветки.</param>
    public virtual void HighlightFactInPreview(Sungero.Domain.Shared.IPreviewControlState previewControl,
                                               string position, Sungero.Core.Color color, Sungero.Domain.Shared.IPropertyInfo propertyInfo,
                                               IHighlightActivationStyle highlightActivationStyle)
    {

      var area = this.AddHighlightArea(previewControl, position, color, highlightActivationStyle);
      if (area == null)
        return;
      
      area.SetRelatedProperty(propertyInfo);
    }
    
    /// <summary>
    /// Подсветить факт в предпросмотре с фокусировкой по нажатию на свойство в табличной части.
    /// </summary>
    /// <param name="previewControl">Контрол предпросмотра.</param>
    /// <param name="position">Позиция.</param>
    /// <param name="color">Цвет.</param>
    /// <param name="childEntity">Свойство-коллекция.</param>
    /// <param name="childPropertyInfo">Информация о свойстве в коллекции.</param>
    /// <param name="highlightActivationStyle">Параметры отображения фокусировки подсветки.</param>
    public virtual void HighlightFactInPreview(Sungero.Domain.Shared.IPreviewControlState previewControl,
                                               string position, Sungero.Core.Color color, Sungero.Domain.Shared.IChildEntity childEntity,
                                               Sungero.Domain.Shared.IPropertyInfo childPropertyInfo,
                                               IHighlightActivationStyle highlightActivationStyle)
    {
      var area = this.AddHighlightArea(previewControl, position, color, highlightActivationStyle);
      if (area == null)
        return;
      
      area.SetRelatedChildCollectionProperty(childEntity, childPropertyInfo);
    }
    
    /// <summary>
    /// Добавить область выделения в предпросмотре.
    /// </summary>
    /// <param name="previewControl">Контрол предпросмотра.</param>
    /// <param name="position">Позиции.</param>
    /// <param name="color">Цвет.</param>
    /// <param name="highlightActivationStyle">Параметры отображения фокусировки подсветки.</param>
    /// <returns>Область выделения в предпросмотре.</returns>
    public virtual Sungero.Domain.Shared.IPreviewHighlight AddHighlightArea(Sungero.Domain.Shared.IPreviewControlState previewControl,
                                                                            string position, Sungero.Core.Color color,
                                                                            IHighlightActivationStyle highlightActivationStyle)
    {
      var positions = position.Split(Constants.Module.PositionElementDelimiter);
      if (positions.Count() >= 7)
      {
        var area = previewControl.HighlightAreas.Add(int.Parse(positions[0]),
                                                     double.Parse(positions[1].Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture),
                                                     double.Parse(positions[2].Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture),
                                                     double.Parse(positions[3].Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture),
                                                     double.Parse(positions[4].Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture),
                                                     double.Parse(positions[5].Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture),
                                                     double.Parse(positions[6].Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture));
        // Установить подсветку согласно вероятности.
        area.Style.Color = color;
        
        // Установить поведение при фокусировке.
        // Рамка.
        var borderColor = TryParseColorCode(highlightActivationStyle.BorderColor);
        if (highlightActivationStyle.UseBorder != null)
        {
          area.ActivationStyle.BorderColor = borderColor != Sungero.Core.Colors.Empty ? borderColor : Colors.Common.Red;
          area.ActivationStyle.BorderWidth = highlightActivationStyle.BorderWidth > 0
            ? (int)highlightActivationStyle.BorderWidth
            : Constants.Module.HighlightActivationBorderDefaultWidth;
        }
        
        // Заливка цветом.
        var fillingColor = TryParseColorCode(highlightActivationStyle.FillingColor);
        if (highlightActivationStyle.UseFilling != null || highlightActivationStyle.UseBorder == null)
          area.ActivationStyle.Color = fillingColor != Sungero.Core.Colors.Empty ? fillingColor : Colors.Common.Blue;
        
        return area;
      }
      
      return null;
    }

    /// <summary>
    /// Получить цвет по коду.
    /// </summary>
    /// <param name="colorCode">Код цвета.</param>
    /// <returns>Цвет.</returns>
    public static Sungero.Core.Color TryParseColorCode(string colorCode)
    {
      var color = Sungero.Core.Colors.Empty;
      if (!string.IsNullOrWhiteSpace(colorCode))
      {
        try
        {
          color = Sungero.Core.Colors.Parse(colorCode);
        }
        catch
        {
        }
      }
      
      return color;
    }
    
    /// <summary>
    /// Проверить возможность проверки строк как чисел.
    /// </summary>
    /// <param name="firstString">Первая строка для сравнения.</param>
    /// <param name="secondString">Вторая строка для сравнения.</param>
    /// <returns>True - можно сравнивать как числа, иначе - False.</returns>
    private bool CanCompareAsNumbers(string firstString, string secondString)
    {
      
      firstString = firstString.Replace(',', '.');
      secondString = secondString.Replace(',', '.');
      double number;
      var numberStyles = System.Globalization.NumberStyles.Any;
      var invariantCulture = System.Globalization.CultureInfo.InvariantCulture;
      
      return double.TryParse(firstString, numberStyles, invariantCulture, out number) &&
        double.TryParse(secondString, numberStyles, invariantCulture, out number);
    }
    
    /// <summary>
    /// Сравнить строки как числа.
    /// </summary>
    /// <param name="firstString">Первая строка для сравнения.</param>
    /// <param name="secondString">Вторая строка для сравнения.</param>
    /// <returns>Значение, указывающее, каков относительный порядок сравниваемых объектов.</returns>
    /// <remarks>Ссылка: https://docs.microsoft.com/ru-ru/dotnet/api/system.icomparable.compareto?view=netframework-4.8.</remarks>
    private int CompareAsNumbers(string firstString, string secondString)
    {
      firstString = firstString.Replace(',', '.');
      secondString = secondString.Replace(',', '.');
      var numberStyles = System.Globalization.NumberStyles.Any;
      var invariantCulture = System.Globalization.CultureInfo.InvariantCulture;
      var firstNumber = double.Parse(firstString, numberStyles, invariantCulture);
      var secondNumber = double.Parse(secondString, numberStyles, invariantCulture);
      
      return firstNumber.CompareTo(secondNumber);
    }
    
    /// <summary>
    /// Управление подсветкой.
    /// </summary>
    public virtual void SetHighlight()
    {
      var highlightActivationStyle = this.GetHighlightActivationStyle();
      this.SetHighlightPropertiesAndFacts(highlightActivationStyle);
      
      var documentRecognitionInfo = Sungero.Commons.PublicFunctions.EntityRecognitionInfo.Remote.GetEntityRecognitionInfo(_obj);
      this.SetAdditionalHighlight(documentRecognitionInfo, highlightActivationStyle);
    }
    
    #endregion
    
    #region Действия отправки документов в заданиях
    
    /// <summary>
    /// Выбрать главный документ.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="probablyMainDocuments">Документы, которые вероятнее всего могут оказаться главными.</param>
    /// <returns>Документ.</returns>
    [Public]
    public static Sungero.Domain.Shared.IEntity ChooseMainDocument(System.Collections.Generic.IEnumerable<Content.IElectronicDocument> documents,
                                                                   System.Collections.Generic.IEnumerable<Content.IElectronicDocument> probablyMainDocuments)
    {
      // Если документ один, не показывать диалог выбора главного.
      if (documents.Count() == 1)
        return documents.FirstOrDefault();
      
      // Вывести диалог выбора главного документа.
      var dialogText = Resources.ChoosingDocumentDialogName;
      var dialog = Dialogs.CreateInputDialog(dialogText);
      var defaultDocument = documents.OrderByDescending(doc => probablyMainDocuments.Contains(doc)).FirstOrDefault();
      var mainDocument = dialog.AddSelect(Resources.MainDocumentField, true, defaultDocument).From(documents);
      dialog.Buttons.AddOkCancel();
      dialog.Buttons.Default = DialogButtons.Ok;
      var result = dialog.Show();
      
      if (result == DialogButtons.Cancel)
        return null;
      
      return mainDocument.Value;
    }
    
    /// <summary>
    /// Получение списка документов, к которым применимо действие.
    /// </summary>
    /// <param name="documents">Все вложения.</param>
    /// <param name="currentAction">Выбранное действие.</param>
    /// <returns>Список документов, к которым применимо действие.</returns>
    [Public]
    public static System.Collections.Generic.IEnumerable<Content.IElectronicDocument> GetSuitableDocuments(System.Collections.Generic.IEnumerable<Content.IElectronicDocument> documents, Domain.Shared.IActionInfo currentAction)
    {
      return documents.Where(doc => Docflow.OfficialDocuments.Is(doc) &&
                             !Docflow.ExchangeDocuments.Is(doc) &&
                             Docflow.PublicFunctions.OfficialDocument.CanExecuteSendAction(Docflow.OfficialDocuments.As(doc), currentAction)).ToList();
    }
    
    /// <summary>
    /// Определить, нужно ли добавлять документ во вложения задачи.
    /// </summary>
    /// <param name="attachment">Вложения.</param>
    /// <param name="mainOfficialDocument">Выбранный главный документ.</param>
    /// <returns>True, если нужно.</returns>
    [Public]
    public static bool NeedToAttachDocument(Content.IElectronicDocument attachment, Docflow.IOfficialDocument mainOfficialDocument)
    {
      if (Docflow.ExchangeDocuments.Is(attachment))
        return false;
      
      return !mainOfficialDocument.Relations.GetRelatedDocuments(Constants.Module.AddendumRelationName).Any(ad => ad.Id == attachment.Id);
    }
    
    /// <summary>
    /// Проверить возможность выполнения действия отправки.
    /// </summary>
    /// <param name="actionInfo">Действие.</param>
    /// <returns>Признак доступности действия.</returns>
    [Public]
    public bool CanExecuteSendAction(Domain.Shared.IActionInfo actionInfo)
    {
      if (_obj.DocumentKind == null)
        return false;
      
      return _obj.DocumentKind.AvailableActions.Any(a => a.Action.ActionGuid == Functions.Module.GetActionGuid(actionInfo));
    }
    
    /// <summary>
    /// Если по документу уже были запущены задачи на согласование по регламенту,
    /// то с помощью диалога определить, нужно ли создавать ещё одну.
    /// </summary>
    /// <returns>True, если нужно создать еще одну задачу на согласование. Иначе false.</returns>
    [Public]
    public bool NeedCreateApprovalTask()
    {
      var result = true;
      
      var createdTasks = Docflow.PublicFunctions.Module.Remote.GetApprovalTasks(_obj);
      if (createdTasks.Any())
      {
        result = false;
        
        var dialog = Dialogs.CreateTaskDialog(OfficialDocuments.Resources.ContinueCreationApprovalTaskQuestion,
                                              OfficialDocuments.Resources.DocumentHasApprovalTasks,
                                              MessageType.Question);
        var showButton = dialog.Buttons.AddCustom(OfficialDocuments.Resources.ShowButtonText);
        var continueButton = dialog.Buttons.AddCustom(OfficialDocuments.Resources.SendButtonText);
        dialog.Buttons.AddCancel();
        
        CommonLibrary.DialogButton dialogResult = showButton;
        
        while (dialogResult == showButton)
        {
          dialogResult = dialog.Show();
          
          if (dialogResult.Equals(showButton))
          {
            if (createdTasks.Count() == 1)
              createdTasks.Single().ShowModal();
            else
              createdTasks.ShowModal();
          }
          if (dialogResult.Equals(continueButton))
            result = true;
        }
      }
      
      return result;
    }
    
    /// <summary>
    /// Если по документу уже были запущены задачи на рассмотрение,
    /// то с помощью диалога определить, нужно ли создавать ещё одну.
    /// </summary>
    /// <returns>True, если нужно создать еще одну задачу на рассмотрение. Иначе false.</returns>
    [Public]
    public bool NeedCreateReviewTask()
    {
      var result = true;
      
      var createdTasks = Docflow.PublicFunctions.Module.Remote.GetReviewTasks(_obj);
      if (createdTasks.Any())
      {
        result = false;
        
        var dialog = Dialogs.CreateTaskDialog(OfficialDocuments.Resources.ContinueCreationReviewTaskQuestion,
                                              OfficialDocuments.Resources.DocumentHasReviewTasks,
                                              MessageType.Question);
        var showButton = dialog.Buttons.AddCustom(OfficialDocuments.Resources.ShowButtonText);
        var continueButton = dialog.Buttons.AddCustom(OfficialDocuments.Resources.SendButtonText);
        dialog.Buttons.AddCancel();
        
        CommonLibrary.DialogButton dialogResult = showButton;
        
        while (dialogResult == showButton)
        {
          dialogResult = dialog.Show();
          
          if (dialogResult.Equals(showButton))
          {
            if (createdTasks.Count() == 1)
              createdTasks.Single().ShowModal();
            else
              createdTasks.ShowModal();
          }
          if (dialogResult.Equals(continueButton))
            result = true;
        }
      }
      
      return result;
    }
    
    #endregion
    
    #region Смена типа
    
    /// <summary>
    /// Сменить тип документа.
    /// </summary>
    /// <param name="types">Типы документов, на которые можно сменить.</param>
    /// <returns>Сконвертированный документ.</returns>
    [Public]
    public virtual Sungero.Docflow.IOfficialDocument ChangeDocumentType(List<Domain.Shared.IEntityInfo> types)
    {
      Sungero.Docflow.IOfficialDocument convertedDoc = null;
      
      // Запретить смену типа, если документ или его тело заблокировано.
      var isCalledByDocument = CallContext.CalledDirectlyFrom(OfficialDocuments.Info);
      if (isCalledByDocument && Functions.Module.IsLockedByOther(_obj) ||
          !isCalledByDocument && Functions.Module.IsLocked(_obj) ||
          Functions.Module.VersionIsLocked(_obj.Versions.ToList()))
      {
        Dialogs.ShowMessage(Docflow.ExchangeDocuments.Resources.ChangeDocumentTypeLockError,
                            MessageType.Error);
        return convertedDoc;
      }
      
      // Открыть диалог по смене типа.
      var title = ExchangeDocuments.Resources.TypeChange;
      var dialog = Dialogs.CreateSelectTypeDialog(title, types.ToArray());
      if (dialog.Show() == DialogButtons.Ok)
        convertedDoc = OfficialDocuments.As(_obj.ConvertTo(dialog.SelectedType));
      
      return convertedDoc;
    }
    
    /// <summary>
    /// Получить список типов документов, доступных для смены типа.
    /// </summary>
    /// <returns>Список типов документов, доступных для смены типа.</returns>
    public virtual List<Domain.Shared.IEntityInfo> GetTypesAvailableForChange()
    {
      return new List<Domain.Shared.IEntityInfo>() { Docflow.SimpleDocuments.Info };
    }
    
    /// <summary>
    /// Дополнительное условие доступности действия "Сменить тип".
    /// </summary>
    /// <returns>True - если действие "Сменить тип" доступно, иначе - false.</returns>
    public virtual bool CanChangeDocumentType()
    {
      return _obj.ExchangeState != null;
    }
    
    #endregion
    
    /// <summary>
    /// Получить текст для отметки документа устаревшим.
    /// </summary>
    /// <returns>Текст для диалога прекращения согласования.</returns>
    [Public]
    public virtual string GetTextToMarkDocumentAsObsolete()
    {
      return OfficialDocuments.Resources.MarkDocumentAsObsolete;
    }
    
    /// <summary>
    /// Показывать сводку по документу в заданиях на согласование и подписание.
    /// </summary>
    /// <returns>True, если в заданиях нужно показывать сводку по документу.</returns>
    [Public]
    public virtual bool NeedViewDocumentSummary()
    {
      return false;
    }
    
    /// <summary>
    /// Пометить документ как устаревший.
    /// </summary>
    /// <returns>True, если документ надо пометить как устаревший, иначе False.</returns>
    /// <remarks>Используется для отметки документа устаревшим в диалоге запроса причины прекращения задачи согласования.</remarks>
    [Public]
    public virtual bool MarkDocumentAsObsolete()
    {
      return false;
    }
    
    #region Сравнение версий
    
    /// <summary>
    /// Показать диалог сравнения версий документа.
    /// </summary>
    public virtual void ShowCompareVersionsDialog()
    {
      // Предполагается, что пользователь инициирует сравнение из сравниваемого (измененного, доработанного) документа.
      // Поэтому в первом поле "Версия" отображается именно последняя версия сравниваемого документа.
      // Далее предлагается выбрать эталонную версию этого же документа.
      var dialog = Dialogs.CreateInputDialog(OfficialDocuments.Resources.CompareVersionsDialogTitle);
      dialog.HelpCode = Constants.OfficialDocument.HelpCode.DocumentComparison;
      
      // Сравниваемая версия (например, новая редакция).
      var versions = _obj.Versions;
      var versionNames = this.GetVersionNamesForCompare(_obj);
      var versionToCompareName = dialog.AddSelect(OfficialDocuments.Resources.Version, true).From(versionNames.ToArray());
      versionToCompareName.Value = versionNames.FirstOrDefault();
      
      // Эталон.
      var etalonVersionName = dialog.AddSelect(OfficialDocuments.Resources.CompareTo, true);
      etalonVersionName.From(versionNames.Where(x => x != versionToCompareName.Value).ToArray());
      etalonVersionName.Value = versionNames.Count() == 2 ? versionNames.Last() : string.Empty;

      // Исключить сравниваемую версию из доступных для выбора в качестве эталонной версии.
      versionToCompareName.SetOnValueChanged(
        (e) =>
        {
          if (e.NewValue != e.OldValue)
          {
            // Перевыбрать текущее значение поля "Сравнить с", т.к. при пересоздании списка оно очистится.
            etalonVersionName.From(versionNames.Where(x => x != e.NewValue).ToArray());
            etalonVersionName.Value = etalonVersionName.Value != e.NewValue ? etalonVersionName.Value : string.Empty;
          }
        });
      
      // Кнопки.
      var compareButton = dialog.Buttons.AddCustom(OfficialDocuments.Resources.Compare);
      dialog.Buttons.AddCancel();
      
      IElectronicDocumentVersions etalonVersion = null;
      IElectronicDocumentVersions versionToCompare = null;
      
      dialog.SetOnRefresh(
        (e) =>
        {
          versionToCompare = versions.Where(v => string.Equals(versionToCompareName.Value, string.Format("{0}. {1} ({2})", v.Number, v.Note, v.AssociatedApplication.Extension))).FirstOrDefault();
          etalonVersion = versions.Where(v => string.Equals(etalonVersionName.Value, string.Format("{0}. {1} ({2})", v.Number, v.Note, v.AssociatedApplication.Extension))).FirstOrDefault();
          
          // Нельзя сравнивать версии неподдерживаемых форматов.
          var versionsExtensionErrorMessage = this.CheckVersionsExtension(versionToCompare, etalonVersion);
          if (!string.IsNullOrEmpty(versionsExtensionErrorMessage))
            e.AddError(versionsExtensionErrorMessage);
          
          // Нельзя сравнивать версии нулевого размера.
          var versionsMinSizeErrorMessage = this.CheckVersionsMinSize(versionToCompare, etalonVersion);
          if (!string.IsNullOrEmpty(versionsMinSizeErrorMessage))
            e.AddError(versionsMinSizeErrorMessage);
        });
      
      dialog.SetOnButtonClick(
        (e) =>
        {
          if (!e.IsValid || !Equals(e.Button, compareButton))
            return;
          
          // Нельзя сравнивать пустые версии.
          var versionToCompareIsEmpty = Functions.Module.Remote.IsVersionBodyEmpty(_obj.Id, versionToCompare.Number.Value);
          var etalonVersionIsEmpty = Functions.Module.Remote.IsVersionBodyEmpty(_obj.Id, etalonVersion.Number.Value);
          if (versionToCompareIsEmpty)
            e.AddError(Resources.VersionNotAvailableFormat(versionToCompare.Number, _obj.Id));
          if (etalonVersionIsEmpty)
            e.AddError(Resources.VersionNotAvailableFormat(etalonVersion.Number, _obj.Id));
          
          // Нельзя сравнивать заблокированные версии.
          var versionsLockErrorMessage = this.CheckIfVersionsAreLocked(versionToCompare, etalonVersion);
          if (!string.IsNullOrEmpty(versionsLockErrorMessage))
            e.AddError(versionsLockErrorMessage);

          // Нельзя сравнивать зашифрованные документы.
          var isDocumentEncrypted = Functions.Module.IsDocumentEncrypted(_obj);
          if (isDocumentEncrypted)
            e.AddError(OfficialDocuments.Resources.CannotCompareEncryptedDocuments);
          
        });
      
      // Показ диалога.
      if (dialog.Show() == compareButton)
      {
        this.RunComparison(versionToCompare, etalonVersion);
      }
    }
    
    /// <summary>
    /// Показать диалог сравнения документов.
    /// </summary>
    public virtual void ShowCompareDocumentsDialog()
    {
      // Предполагается, что пользователь инициирует сравнение из сравниваемого (измененного, доработанного) документа.
      // Поэтому в первом поле "Версия" отображается именно последняя версия сравниваемого документа.
      // Далее предлагается выбрать эталонный документ и его версию.
      var dialog = Dialogs.CreateInputDialog(OfficialDocuments.Resources.CompareDocumentsDialogTitle);
      dialog.HelpCode = Constants.OfficialDocument.HelpCode.DocumentComparison;

      // Сравниваемый документ (например: документ от контрагента, новая редакция, документ, сравниваемый с шаблоном).
      var documentToCompare = _obj;
      var versionToCompareNames = this.GetVersionNamesForCompare(documentToCompare);
      var versionToCompareName = dialog.AddSelect(OfficialDocuments.Resources.Version, true, versionToCompareNames.FirstOrDefault())
        .From(versionToCompareNames.ToArray());

      // Эталон.
      var etalonDocument = dialog.AddSelect(OfficialDocuments.Resources.CompareTo, true, Content.ElectronicDocuments.Null)
        .Where(x => !Equals(x, _obj) && x.IsEncrypted != true);
      etalonDocument.WithPlaceholder(OfficialDocuments.Resources.ChooseAnotherDocument);
      var etalonVersionName = dialog.AddSelect(OfficialDocuments.Resources.Version, true, string.Empty);
      
      // Кнопки.
      var compareButton = dialog.Buttons.AddCustom(OfficialDocuments.Resources.Compare);
      dialog.Buttons.AddCancel();
      
      IElectronicDocumentVersions versionToCompare = null;
      IElectronicDocumentVersions etalonVersion = null;
      
      dialog.SetOnRefresh(
        (e) =>
        {
          versionToCompare = documentToCompare.Versions
            .FirstOrDefault(v => string.Equals(versionToCompareName.Value, string.Format("{0}. {1} ({2})", v.Number, v.Note, v.AssociatedApplication.Extension)));
          etalonVersion = etalonDocument.Value != null ?
            etalonDocument.Value.Versions.FirstOrDefault(v => string.Equals(etalonVersionName.Value, string.Format("{0}. {1} ({2})", v.Number, v.Note, v.AssociatedApplication.Extension))) :
            null;
          
          // Нельзя сравнивать версии неподдерживаемых форматов.
          var versionsExtensionErrorMessage = this.CheckVersionsExtension(versionToCompare, etalonVersion);
          if (!string.IsNullOrEmpty(versionsExtensionErrorMessage))
            e.AddError(versionsExtensionErrorMessage);
          
          // Нельзя сравнивать версии нулевого размера.
          var versionsMinSizeErrorMessage = this.CheckVersionsMinSize(versionToCompare, etalonVersion);
          if (!string.IsNullOrEmpty(versionsMinSizeErrorMessage))
            e.AddError(versionsMinSizeErrorMessage);
        });
      
      // Обновление списка версий при изменении эталона.
      etalonDocument.SetOnValueChanged(
        (e) =>
        {
          if (e.NewValue != null && e.OldValue != e.NewValue)
          {
            var etalonVersionNames = this.GetVersionNamesForCompare(e.NewValue);
            etalonVersionName.From(etalonVersionNames.ToArray());
            if (etalonVersionNames.ToList().Count == 1)
              etalonVersionName.Value = etalonVersionNames.FirstOrDefault();
          }
          else if (e.NewValue == null)
          {
            etalonVersionName.From(string.Empty);
          }
        });
      
      dialog.SetOnButtonClick(
        (e) =>
        {
          if (!e.IsValid || !Equals(e.Button, compareButton))
            return;
          
          // Нельзя сравнивать пустые версии.
          var versionToCompareIsEmpty = Functions.Module.Remote.IsVersionBodyEmpty(documentToCompare.Id, versionToCompare.Number.Value);
          var etalonVersionIsEmpty = Functions.Module.Remote.IsVersionBodyEmpty(etalonDocument.Value.Id, etalonVersion.Number.Value);
          if (versionToCompareIsEmpty)
            e.AddError(Resources.VersionNotAvailableFormat(versionToCompare.Number, documentToCompare.Id));
          if (etalonVersionIsEmpty)
            e.AddError(Resources.VersionNotAvailableFormat(etalonVersion.Number, etalonDocument.Value.Id));
          
          // Нельзя сравнивать заблокированные версии.
          var versionsLockErrorMessage = this.CheckIfVersionsAreLocked(versionToCompare, etalonVersion);
          if (!string.IsNullOrEmpty(versionsLockErrorMessage))
            e.AddError(versionsLockErrorMessage);

          // Нельзя сравнивать зашифрованные документы.
          var isDocumentToCompareEncrypted = Functions.Module.IsDocumentEncrypted(documentToCompare);
          var isEtalonDocumentEncrypted = Functions.Module.IsDocumentEncrypted(etalonDocument.Value);
          if (isDocumentToCompareEncrypted || isEtalonDocumentEncrypted)
            e.AddError(OfficialDocuments.Resources.CannotCompareEncryptedDocuments);
        });
      
      // Показ диалога.
      if (dialog.Show() == compareButton)
      {
        this.RunComparison(versionToCompare, etalonDocument.Value, etalonVersion);
      }
    }
    
    /// <summary>
    /// Получить наименования версий документа для сравнения.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Список наименований версий в формате {Номер версии}. {Примечание} ({Расширение}).</returns>
    [Public]
    public virtual List<string> GetVersionNamesForCompare(IElectronicDocument document)
    {
      return document.Versions
        .OrderByDescending(v => v.Number)
        .Select(v => string.Format("{0}. {1} ({2})", v.Number, v.Note, v.AssociatedApplication.Extension))
        .ToList();
    }
    
    /// <summary>
    /// Проверить, что версии имеют корректное расширение для сравнения.
    /// </summary>
    /// <param name="versionToCompare">Сравниваемая версия.</param>
    /// <param name="etalonVersion">Эталон.</param>
    /// <returns>Текст ошибки или null, если ошибок нет.</returns>
    [Public]
    public virtual string CheckVersionsExtension(IElectronicDocumentVersions versionToCompare, IElectronicDocumentVersions etalonVersion)
    {
      var unsupportedExtensionsInVersions = string.Empty;
      var supportedExtensions = this.GetSupportedExtensionsForDocumentComparison();
      if (versionToCompare != null && !supportedExtensions.Contains(versionToCompare.AssociatedApplication.Extension))
        unsupportedExtensionsInVersions = versionToCompare.AssociatedApplication.Extension.ToUpper();
      if (etalonVersion != null && !supportedExtensions.Contains(etalonVersion.AssociatedApplication.Extension))
      {
        if (string.IsNullOrEmpty(unsupportedExtensionsInVersions) || versionToCompare.AssociatedApplication.Extension == etalonVersion.AssociatedApplication.Extension)
          unsupportedExtensionsInVersions = etalonVersion.AssociatedApplication.Extension.ToUpper();
        else
          unsupportedExtensionsInVersions = string.Format("{0}, {1}", unsupportedExtensionsInVersions, etalonVersion.AssociatedApplication.Extension.ToUpper());
      }
      
      if (!string.IsNullOrEmpty(unsupportedExtensionsInVersions))
        return OfficialDocuments.Resources.ExtensionNotSupportedForComparisonFormat(unsupportedExtensionsInVersions);
      
      return null;
    }
    
    /// <summary>
    /// Получить список поддерживаемых расширений для сравнения документов.
    /// </summary>
    /// <returns>Список поддерживаемых расширений.</returns>
    /// <remarks>Включает все расширения, которые обрабатываются в Aspose и Ario, кроме таблиц.</remarks>
    [Public]
    public virtual List<string> GetSupportedExtensionsForDocumentComparison()
    {
      return new List<string>()
      {
        "jpg", "jpeg", "png", "bmp", "gif",
        "tif", "tiff", "pdf", "doc", "docx",
        "dot", "dotx", "rtf", "odt", "ott",
        "txt", "docm"
      };
    }
    
    /// <summary>
    /// Проверить, что версии не нулевого размера.
    /// </summary>
    /// <param name="versionToCompare">Сравниваемая версия.</param>
    /// <param name="etalonVersion">Эталон.</param>
    /// <returns>Текст ошибки или null, если ошибок нет.</returns>
    [Public]
    public virtual string CheckVersionsMinSize(IElectronicDocumentVersions versionToCompare, IElectronicDocumentVersions etalonVersion)
    {
      if (versionToCompare != null && versionToCompare.Body.Size <= Constants.DocumentComparisonInfo.MinimumVersionSize)
        return OfficialDocuments.Resources.VersionHasNoContent;
      if (etalonVersion != null && etalonVersion.Body.Size <= Constants.DocumentComparisonInfo.MinimumVersionSize)
        return OfficialDocuments.Resources.VersionHasNoContent;
      
      return null;
    }
    
    /// <summary>
    /// Проверить, что версии не заблокированы.
    /// </summary>
    /// <param name="versionToCompare">Сравниваемая версия.</param>
    /// <param name="etalonVersion">Эталон.</param>
    /// <returns>Текст ошибки или null, если ошибок нет.</returns>
    [Public]
    public virtual string CheckIfVersionsAreLocked(IElectronicDocumentVersions versionToCompare, IElectronicDocumentVersions etalonVersion)
    {
      if (versionToCompare != null)
      {
        var bodyLockInfo = Locks.GetLockInfo(versionToCompare.Body);
        if (bodyLockInfo.IsLocked)
          return OfficialDocuments.Resources.VersionIsLockedFormat(versionToCompare.ElectronicDocument.Name, versionToCompare.Number, bodyLockInfo.OwnerName);
      }
      
      if (etalonVersion != null)
      {
        var bodyLockInfo = Locks.GetLockInfo(etalonVersion.Body);
        if (bodyLockInfo.IsLocked)
          return OfficialDocuments.Resources.VersionIsLockedFormat(etalonVersion.ElectronicDocument.Name, etalonVersion.Number, bodyLockInfo.OwnerName);
      }
      
      return null;
    }
    
    /// <summary>
    /// Запустить сравнение версий.
    /// </summary>
    /// <param name="versionToCompare">Сравниваемая версия.</param>
    /// <param name="etalonVersion">Эталонная версия.</param>
    public virtual void RunComparison(IElectronicDocumentVersions versionToCompare, IElectronicDocumentVersions etalonVersion)
    {
      this.RunComparison(versionToCompare, _obj, etalonVersion);
    }

    /// <summary>
    /// Запустить сравнение документов.
    /// </summary>
    /// <param name="versionToCompare">Сравниваемая версия.</param>
    /// <param name="etalonDocument">Эталонный документ.</param>
    /// <param name="etalonVersion">Эталонная версия.</param>
    public virtual void RunComparison(IElectronicDocumentVersions versionToCompare,
                                      IElectronicDocument etalonDocument, IElectronicDocumentVersions etalonVersion)
    {
      // Записать в историю документа информацию о сравнении.
      var historyOperation = new Enumeration(Equals(_obj, etalonDocument) ?
                                             Constants.OfficialDocument.Operation.CompareVersions :
                                             Constants.OfficialDocument.Operation.CompareDocuments);
      var historyComment = Equals(_obj, etalonDocument) ?
        OfficialDocuments.Resources.CompareVersionsHistoryCommentFormat(etalonVersion.Number.Value) :
        OfficialDocuments.Resources.CompareDocumentsHistoryCommentFormat(etalonDocument.Id, etalonVersion.Number.Value);
      _obj.History.Write(historyOperation, null, historyComment, versionToCompare.Number);
      
      // Если хеш версий одинаковый, то вывести сообщение, что версии одинаковы.
      if (etalonVersion.Body.Hash == versionToCompare.Body.Hash)
      {
        Dialogs.NotifyMessage(Resources.NoDiffInDocuments);
        return;
      }
      
      // Найти последний результат сравнения по этой паре версий.
      var comparisonInfo = Functions.DocumentComparisonInfo.Remote
        .GetLastDocumentComparisonInfo(etalonVersion.Body.Hash, versionToCompare.Body.Hash, Users.Current);
      var status = comparisonInfo?.ProcessingStatus;
      if (comparisonInfo != null && status != DocumentComparisonInfo.ProcessingStatus.Error)
      {
        if (status == DocumentComparisonInfo.ProcessingStatus.Compared)
        {
          if (comparisonInfo.DifferencesCount == 0)
          {
            Dialogs.NotifyMessage(Resources.NoDiffInDocuments);
            return;
          }
          var resultPdfName = Docflow.PublicFunctions.Module.GetComparisonResultPdfName(_obj, versionToCompare.Number.Value, etalonDocument, etalonVersion.Number.Value);
          comparisonInfo.ResultPdf.Open(resultPdfName);
        }
        else
        {
          Dialogs.NotifyMessage(OfficialDocuments.Resources.DocumentComparisonAlreadyStartedFormat(comparisonInfo.Name));
        }
        return;
      }
      
      // Запустить новое сравнение (или перезапустить, если не было успешных сравнений по этой паре версий).
      comparisonInfo = Functions.Module.Remote.CreateDocumentComparisonInfo(etalonDocument, etalonVersion.Number.Value, _obj, versionToCompare.Number.Value);
      Functions.Module.Remote.CreateCompareDocumentsAsyncHandler(comparisonInfo);
    }
    
    #endregion
    
    /// <summary>
    /// Показать диалог создания соглашения об аннулировании с указанием причины и подписанта.
    /// </summary>
    public virtual void ShowCreateCancellationAgreementDialog()
    {
      var ourSignatoriesIds = Functions.OfficialDocument.Remote.GetSignatoriesIdsForCancellationAgreement(_obj);
      var ourSignatories = Company.PublicFunctions.Module.Remote.GetEmployeesByIds(ourSignatoriesIds);
      
      var dialog = Dialogs.CreateInputDialog(OfficialDocuments.Resources.CreateCancellationAgreementDialogTitle);
      var defaultOurSignatory = ourSignatories.Contains(_obj.OurSignatory) ? _obj.OurSignatory : null;
      var ourSignatory = dialog.AddSelect(OfficialDocuments.Resources.CreateCancellationAgreementDialogOurSignatoryField, true, defaultOurSignatory)
        .From(ourSignatories);
      var reason = dialog.AddMultilineString(OfficialDocuments.Resources.CreateCancellationAgreementDialogReasonField, true);
      var createButton = dialog.Buttons.AddCustom(OfficialDocuments.Resources.CreateCancellationAgreementDialogButtonName);
      dialog.Buttons.AddCancel();
      dialog.HelpCode = Constants.OfficialDocument.CreateCancellationAgreementHelpCode;
      
      dialog.SetOnButtonClick(
        b =>
        {
          Structures.OfficialDocument.ICancellationAgreementCreatingResult result = null;
          if (b.Button == createButton && b.IsValid)
          {
            result = Functions.OfficialDocument.Remote.CreateCancellationAgreement(_obj, ourSignatory.Value, reason.Value);
            if (!string.IsNullOrEmpty(result.Error))
              b.AddError(result.Error);
            else
              result.CancellationAgreement.Show();
          }
        });
      
      dialog.Show();
    }
    
    /// <summary>
    /// Получить сертификат из основания подписания, если он подходит для подписания.
    /// </summary>
    /// <param name="certificates">Список действуюших сертификатов сотрудника.</param>
    /// <returns>Сертификат из основания подписания, если он подходит, иначе - null.</returns>
    [Public]
    public ICertificate ValidateAndRetrieveCertificateFromSigningReason(List<ICertificate> certificates)
    {
      var ourSigningReason = _obj.OurSigningReason;
      
      // Взять сертификат из основания подписания, если он подходит по критериям.
      // При рассмотрении адресатом поле "Основание" вернет сертификат подписавшего, а не рассматривающего, поэтому для рассмотрения автовыбор сертификата из "Основания" не делать.
      if (!CallContext.CalledFrom(ApprovalReviewAssignments.Info) &&
          ourSigningReason?.Certificate != null &&
          certificates.Contains(ourSigningReason.Certificate) &&
          Equals(ourSigningReason.Certificate.Owner, Company.Employees.Current) &&
          !Docflow.PublicFunctions.SignatureSetting.Remote.FormalizedPowerOfAttorneyIsExpired(ourSigningReason))
      {
        return ourSigningReason.Certificate;
      }
      
      return null;
    }
    
  }
}