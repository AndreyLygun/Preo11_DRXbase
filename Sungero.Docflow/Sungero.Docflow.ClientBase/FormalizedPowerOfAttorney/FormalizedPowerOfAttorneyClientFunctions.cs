using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FormalizedPowerOfAttorney;

namespace Sungero.Docflow.Client
{
  partial class FormalizedPowerOfAttorneyFunctions
  {
    /// <summary>
    /// Показать диалог создания заявления на отзыв эл. доверенности с указанием причины.
    /// </summary>
    [Public]
    public virtual void ShowCreateRevocationDialog()
    {
      var dialog = Dialogs.CreateInputDialog(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.RevocationDialogTitle);
      var reason = dialog.AddMultilineString(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.RevocationDialogReasonFieldName, true);
      reason.MaxLength(Constants.FormalizedPowerOfAttorney.FPoARevocationReasonMaxLength);
      var createButton = dialog.Buttons.AddCustom(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.CreateRevocationDialogButtonName);
      dialog.Buttons.AddCancel();
      dialog.HelpCode = Constants.FormalizedPowerOfAttorney.CreatePowerOfAttorneyRevocationHelpCode;
      
      dialog.SetOnButtonClick(
        b =>
        {
          if (b.Button == createButton && b.IsValid)
          {
            // Дополнительная проверка, так как обязательное поле допускает заполнение пробелами.
            if (string.IsNullOrWhiteSpace(reason.Value))
            {
              b.AddError(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.FillRevocationReason);
            }
            else
            {
              IPowerOfAttorneyRevocation revocation = null;
              try
              {
                revocation = Functions.FormalizedPowerOfAttorney.Remote.CreateRevocation(_obj, reason.Value.Trim());
              }
              catch (Exception ex)
              {
                Logger.ErrorFormat("ShowCreateRevocationDialog. Failed to create power of attorney revocation. Document Id {0}", ex, _obj.Id);
              }
              
              if (revocation == null)
                b.AddError(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.PowerOfAttorneyRevocationCreationFailed);
              else
                revocation.Show();
            }
          }
        });
      
      dialog.Show();
    }
    
    /// <summary>
    /// Показать диалог импорта эл. доверенности с подписью из файла.
    /// </summary>
    /// <returns>True, если импорт прошел успешно, иначе - false.</returns>
    [Public]
    public virtual bool ShowImportVersionWithSignatureDialog()
    {
      var dialog = Dialogs.CreateInputDialog(FormalizedPowerOfAttorneys.Resources.ImportPowerOfAttorneyFromXml);
      var fileSelector = dialog.AddFileSelect(FormalizedPowerOfAttorneys.Resources.File, true);
      fileSelector.WithFilter(string.Empty, Constants.FormalizedPowerOfAttorney.XmlExtension);
      var signatureSelector = dialog.AddFileSelect(FormalizedPowerOfAttorneys.Resources.SignatureFile, true);
      signatureSelector.WithFilter(string.Empty, "sgn", "sig");
      var importButton = dialog.Buttons.AddCustom(FormalizedPowerOfAttorneys.Resources.Import);
      dialog.HelpCode = Constants.FormalizedPowerOfAttorney.ImportFromXmlHelpCode;
      dialog.Buttons.AddCancel();
      
      dialog.SetOnButtonClick(
        b =>
        {
          if (b.Button == importButton && b.IsValid)
          {
            try
            {
              // Импорт тела, заполнение свойств и импорт подписи выполняются в одной Remote-функции,
              // чтобы при ошибке на любом из этапов откатывалось всё остальное.
              // Создание версии выполняется на клиенте для корректного обновления карточки после импорта.
              var xml = Docflow.Structures.Module.ByteArray.Create(fileSelector.Value.Content);
              var signature = Docflow.Structures.Module.ByteArray.Create(signatureSelector.Value.Content);
              
              // Перейти в невизуальный режим для возможности сохранения (сохранение необходимо для импорта подписи).
              // Визуальный режим и обязательность полей восстановятся после выполнения действия на рефреше.
              ((Domain.Shared.IExtendedEntity)_obj).Params.Remove(Sungero.Docflow.PublicConstants.OfficialDocument.IsVisualModeParamName);
              Functions.OfficialDocument.SetRequiredProperties(_obj);
              
              // Импорт тела и подписи.
              Functions.FormalizedPowerOfAttorney.Remote.ImportFormalizedPowerOfAttorneyFromXmlAndSign(_obj, xml, signature);
            }
            catch (AppliedCodeException aex)
            {
              var version = _obj.LastVersion;
              if (version != null)
                _obj.Versions.Remove(version);
              var error = FormalizedPowerOfAttorneys.Resources.FormalizedPowerOfAttorneyImportFailed;
              Logger.Error(error, aex);
              b.AddError(string.Format("{0} {1}", error, aex.Message), fileSelector);
            }
            catch (Exception ex)
            {
              var version = _obj.LastVersion;
              if (version != null)
                _obj.Versions.Remove(version);
              var error = FormalizedPowerOfAttorneys.Resources.FormalizedPowerOfAttorneyImportFailed;
              Logger.Error(error, ex);
              b.AddError(string.Format("{0} {1}", error, FormalizedPowerOfAttorneys.Resources.XmlLoadFailed), fileSelector);
            }
          }
        });
      
      if (dialog.Show() == importButton)
      {
        Dialogs.NotifyMessage(FormalizedPowerOfAttorneys.Resources.PowerOfAttorneySuccessfullyImported);
        return true;
      }
      
      return false;
    }
    
    /// <summary>
    /// Запустить преобразование в PDF.
    /// </summary>
    [Public]
    public virtual void GeneratePdfWithSignatureMark()
    {
      // Установить параметр, чтобы вызвать асинхронный обработчик по выдаче прав на документ.
      // Асинхронный обработчик запускается после выполнения всех операций по документу.
      var documentParams = ((Sungero.Domain.Shared.IExtendedEntity)_obj).Params;
      if (!documentParams.ContainsKey(Sungero.Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToDocumentAsync))
        documentParams.Add(Sungero.Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToDocumentAsync, true);
      
      var result = Structures.OfficialDocument.ConversionToPdfResult.Create();
      result.HasErrors = true;
      try
      {
        Functions.OfficialDocument.LogPdfConversion(_obj, _obj.LastVersion.Id, "GeneratePdfWithSignatureMark. Try convert to PDF interactively");
        result = Functions.FormalizedPowerOfAttorney.Remote.ConvertToPdfAndAddSignatureMark(_obj, _obj.LastVersion.Id);
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
      }
      
      if (result.HasErrors)
        Docflow.PublicFunctions.OfficialDocument.Remote.CreateConvertToPdfAndAddSignatureMarkAsyncHandler(_obj, _obj.LastVersion.Id);
    }
    
    /// <summary>
    /// Открыть ссылку на эл. доверенность на сайте ФНС.
    /// </summary>
    public virtual void OpenInFtsRegistry()
    {
      var issuerTin = _obj.BusinessUnit.TIN;
      var agentTin = this.GetAgentTin();
      var ftsRegistryHyperlink = this.GetFtsRegistryHyperlink(_obj.UnifiedRegistrationNumber, issuerTin, agentTin);
      Hyperlinks.Open(ftsRegistryHyperlink);
    }
    
    /// <summary>
    /// Получить ИНН представителя в зависимости от типа.
    /// </summary>
    /// <returns>ИНН представителя.</returns>
    public virtual string GetAgentTin()
    {
      var representative = _obj.Representatives.FirstOrDefault();
      return representative?.IssuedTo?.TIN;
    }
    
    /// <summary>
    /// Получить гиперссылку на эл. доверенность на сайте ФНС.
    /// </summary>
    /// <param name="unifiedRegistrationNumber">Единый рег. номер.</param>
    /// <param name="issuerTin">ИНН доверителя.</param>
    /// <param name="representativeTin">ИНН представителя.</param>
    /// <returns>Гиперссылка на эл. доверенность на сайте ФНС.</returns>
    public virtual string GetFtsRegistryHyperlink(string unifiedRegistrationNumber, string issuerTin, string representativeTin)
    {
      var urlTemplate = PublicFunctions.Module.Remote.GetDocflowParamsStringValue(Constants.Module.SearchFPoAInFtsRegistryTemplateParamName) ??
        Constants.Module.SearchFPoAInFtsRegistryTemplate;
      return string.Format(urlTemplate, unifiedRegistrationNumber, issuerTin, representativeTin);
    }
  }
}