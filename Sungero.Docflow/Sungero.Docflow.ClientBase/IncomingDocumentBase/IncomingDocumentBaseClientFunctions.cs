using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.IncomingDocumentBase;
using Sungero.Docflow.Structures.IncomingDocumentBase;

namespace Sungero.Docflow.Client
{
  partial class IncomingDocumentBaseFunctions
  {
    
    #region Преобразование в PDF с отметками.
    
    public override bool CreateMarksFromDialog()
    {
      var receiptMarkPosition = this.ShowAddRegistrationStampDialog();
      if (receiptMarkPosition == null)
        return false;
      
      var receiptMark = Functions.IncomingDocumentBase.Remote.GetOrCreateRightBottomCoordinatesBasedReceiptMark(_obj,
                                                                                                                receiptMarkPosition.RightIndent,
                                                                                                                receiptMarkPosition.BottomIndent);
      receiptMark.Save();
      var markLogView = $"Mark(Id={receiptMark.Id} Page={receiptMark.Page} XIndent={receiptMark.XIndent} YIndent={receiptMark.YIndent})";
      Functions.OfficialDocument.LogPdfConversion(_obj, _obj.LastVersion.Id, $"Receipt mark created {markLogView}");
      return true;
    }
    
    #endregion
    
    /// <summary>
    /// Получить текст для отметки документа устаревшим.
    /// </summary>
    /// <returns>Текст для диалога прекращения согласования.</returns>
    public override string GetTextToMarkDocumentAsObsolete()
    {
      return IncomingDocumentBases.Resources.MarkDocumentAsObsolete;
    }
    
    /// <summary>
    /// Показать диалог для выбора расположения отметки о поступлении.
    /// </summary>
    /// <returns>Отступы для простановки отметки.</returns>
    public virtual RegistrationStampPosition ShowAddRegistrationStampDialog()
    {
      string positionValue = IncomingDocumentBases.Resources.AddRegistrationStampDialogBottomRightPosition;
      double rightIndentValue = PublicConstants.Module.RegistrationStampDefaultRightIndent;
      double bottomIndentValue = PublicConstants.Module.RegistrationStampDefaultBottomIndent;
      var personalSettings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(null);
      if (personalSettings != null)
      {
        positionValue = personalSettings.RegistrationStampPosition.HasValue ?
          PersonalSettings.Info.Properties.RegistrationStampPosition.GetLocalizedValue(personalSettings.RegistrationStampPosition.Value) :
          IncomingDocumentBases.Resources.AddRegistrationStampDialogBottomRightPosition;
        rightIndentValue = personalSettings.RightIndent ?? PublicConstants.Module.RegistrationStampDefaultRightIndent;
        bottomIndentValue = personalSettings.BottomIndent ?? PublicConstants.Module.RegistrationStampDefaultBottomIndent;
      }
      
      var dialog = Dialogs.CreateInputDialog(IncomingDocumentBases.Resources.AddRegistrationStampDialogTitle);
      dialog.HelpCode = Constants.IncomingDocumentBase.AddRegistrationStampHelpCode;
      var position = dialog.AddSelect(IncomingDocumentBases.Resources.AddRegistrationStampDialogPosition,
                                      true,
                                      positionValue)
        .From(IncomingDocumentBases.Resources.AddRegistrationStampDialogBottomRightPosition,
              IncomingDocumentBases.Resources.AddRegistrationStampDialogBottomCenterPosition,
              IncomingDocumentBases.Resources.AddRegistrationStampDialogCustomPosition);
      var rightIndent = dialog.AddDouble(IncomingDocumentBases.Resources.AddRegistrationStampDialogRightIndent, false, rightIndentValue);
      rightIndent.IsVisible = false;
      var bottomIndent = dialog.AddDouble(IncomingDocumentBases.Resources.AddRegistrationStampDialogBottomIndent, false, bottomIndentValue);
      bottomIndent.IsVisible = false;
      
      var addButton = dialog.Buttons.AddCustom(IncomingDocumentBases.Resources.AddRegistrationStampDialogCreateButton);
      dialog.Buttons.AddCancel();
      
      dialog.SetOnRefresh(
        args =>
        {
          if (position.Value == IncomingDocumentBases.Resources.AddRegistrationStampDialogCustomPosition)
          {
            rightIndent.IsVisible = true;
            bottomIndent.IsVisible = true;
            rightIndent.IsRequired = true;
            bottomIndent.IsRequired = true;
            if (rightIndent.Value.HasValue && rightIndent.Value <= 0 ||
                (bottomIndent.Value.HasValue && bottomIndent.Value <= 0))
            {
              args.AddError(Docflow.Resources.RegistrationStampCoordsGreaterThanZero);
            }
          }
          else
          {
            rightIndent.IsVisible = false;
            bottomIndent.IsVisible = false;
            rightIndent.IsRequired = false;
            bottomIndent.IsRequired = false;
          }
        });
      
      dialog.SetOnButtonClick(
        args =>
        {
          if (!Equals(args.Button, addButton))
            return;
          
          if (position.Value == IncomingDocumentBases.Resources.AddRegistrationStampDialogBottomRightPosition)
          {
            rightIndent.Value = PublicConstants.Module.RegistrationStampDefaultRightIndent;
            bottomIndent.Value = PublicConstants.Module.RegistrationStampDefaultBottomIndent;
          }
          
          if (position.Value == IncomingDocumentBases.Resources.AddRegistrationStampDialogBottomCenterPosition)
          {
            rightIndent.Value = PublicConstants.Module.RegistrationStampDefaultPageCenterIndent;
            bottomIndent.Value = PublicConstants.Module.RegistrationStampDefaultBottomIndent;
          }
          
          if (rightIndent.Value.HasValue && rightIndent.Value <= 0 ||
              (bottomIndent.Value.HasValue && bottomIndent.Value <= 0))
          {
            args.AddError(Docflow.Resources.RegistrationStampCoordsGreaterThanZero);
          }
        });
      
      if (dialog.Show() == addButton)
      {
        return RegistrationStampPosition.Create(rightIndent.Value.Value, bottomIndent.Value.Value);
      }
      
      return null;
    }
    
    /// <summary>
    /// Проверить возможность преобразования в PDF.
    /// </summary>
    /// <returns>Результат проверки.</returns>
    public virtual Structures.OfficialDocument.IConversionToPdfResult ValidatePdfConvertibilityByExtension()
    {
      var lastVersionExtension = _obj.LastVersion.AssociatedApplication.Extension.ToLower();
      if (!IsolatedFunctions.PdfConverter.CheckIfExtensionIsSupported(lastVersionExtension))
        return Functions.OfficialDocument.GetExtensionValidationError(_obj, lastVersionExtension);
      
      var result = Sungero.Docflow.Structures.OfficialDocument.ConversionToPdfResult.Create();
      result.HasErrors = false;
      return result;
    }
  }
}