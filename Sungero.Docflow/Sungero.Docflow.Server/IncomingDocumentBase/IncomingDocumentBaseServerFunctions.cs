using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.IncomingDocumentBase;

namespace Sungero.Docflow.Server
{
  partial class IncomingDocumentBaseFunctions
  {
    
    #region Преобразование в PDF с отметками
    
    /// <summary>
    /// Создать экземпляр отметки о поступлении для простановки по координатам от правого нижнего угла.
    /// </summary>
    /// <param name="rightIndent">Отступ справа, см.</param>
    /// <param name="bottomIndent">Отступ слева, см.</param>
    /// <returns>Отметка о поступлении для простановки по координатам от правого нижнего угла.</returns>
    /// <remarks>Если отметки о поступлении ещё не существует, она будет создана.</remarks>
    [Remote]
    public virtual IMark GetOrCreateRightBottomCoordinatesBasedReceiptMark(double rightIndent, double bottomIndent)
    {
      var receiptMark = this.GetOrCreateReceiptMark();
      receiptMark.Page = Constants.IncomingDocumentBase.DefaultReceiptMarkPage;
      Functions.Mark.FillXIndentFromRight(receiptMark, rightIndent);
      Functions.Mark.FillYIndentFromBottom(receiptMark, bottomIndent);
      return receiptMark;
    }
    
    /// <summary>
    /// Создать экземпляр отметки о поступлении для документа.
    /// </summary>
    /// <returns>Созданная отметка.</returns>
    [Remote]
    public virtual IMark GetOrCreateReceiptMark()
    {
      var receiptMark = Functions.OfficialDocument.GetOrCreateMark(_obj, Constants.MarkKind.ReceiptMarkKindSid);
      return receiptMark;
    }
    
    public override Sungero.Docflow.Structures.OfficialDocument.IVersionBody GetBodyToConvertToPdfWithMarks(Sungero.Content.IElectronicDocumentVersions version)
    {
      var result = base.GetBodyToConvertToPdfWithMarks(version);
      
      var hasPublicBody = version.PublicBody != null && version.PublicBody.Size != 0;
      if (hasPublicBody)
      {
        result.Body = Functions.Module.GetBinaryData(version.PublicBody);
        result.Extension = version.AssociatedApplication.Extension;
      }
      
      if (result.Body == null || result.Body.Length == 0)
        throw AppliedCodeException.Create($"Document (ID={_obj.Id}). Version (ID={version.Id}) is null or body is empty");
      
      return result;
    }
    
    public override void WriteConvertedBodyToVersion(Sungero.Docflow.Structures.Module.IDocumentMarksDto documentWithMarks)
    {
      _obj.CreateVersionFrom(documentWithMarks.Body, Sungero.Docflow.Constants.OfficialDocument.PdfExtension);
      _obj.LastVersion.Note = Sungero.Docflow.OfficialDocuments.Resources.VersionWithRegistrationStamp;
    }
    
    public override string GetHistoryCommentParamName()
    {
      return PublicConstants.OfficialDocument.AddHistoryCommentAboutRegistrationStamp;
    }
    
    #endregion
    
    /// <summary>
    /// Заполнить текстовое отображение адресатов.
    /// </summary>
    public virtual void SetManyAddresseesLabel()
    {
      var addressees = _obj.Addressees
        .Where(x => x.Addressee != null)
        .Select(x => x.Addressee)
        .ToList();
      
      var maxLength = _obj.Info.Properties.ManyAddresseesLabel.Length;
      var label = Functions.Module.BuildManyAddresseesLabel(addressees, maxLength);
      if (_obj.ManyAddresseesLabel != label)
        _obj.ManyAddresseesLabel = label;
    }
    
    /// <summary>
    /// Преобразовать документ в PDF с наложением отметки о поступлении в новую версию.
    /// </summary>
    /// <param name="rightIndent">Значение отступа справа.</param>
    /// <param name="bottomIndent">Значение отступа снизу.</param>
    /// <returns>Результат преобразования.</returns>
    [Remote]
    public virtual Structures.OfficialDocument.IConversionToPdfResult AddRegistrationStamp(double rightIndent, double bottomIndent)
    {
      var versionId = _obj.LastVersion.Id;
      var result = Structures.OfficialDocument.ConversionToPdfResult.Create();
      result.HasErrors = true;
      
      // Проверки возможности преобразования и наложения отметки.
      var lastVersionExtension = _obj.LastVersion.AssociatedApplication.Extension.ToLower();
      if (!PublicFunctions.OfficialDocument.CheckPdfConvertibilityByExtension(_obj, lastVersionExtension))
        return Functions.OfficialDocument.GetExtensionValidationError(_obj, lastVersionExtension);
      
      // Выбор способа преобразования.
      var isInteractive = Functions.OfficialDocument.CanConvertToPdfInteractively(_obj);
      if (isInteractive)
      {
        // Способ преобразования: интерактивно.
        var registrationStamp = this.GetRegistrationStampAsHtml(versionId);
        result = this.ConvertToPdfAndAddRegistrationStamp(versionId, registrationStamp, rightIndent, bottomIndent);
        result.IsFastConvertion = true;
        result.ErrorTitle = Docflow.Resources.AddRegistrationStampErrorTitle;
      }
      else
      {
        Functions.IncomingDocumentBase.CreateConvertToPdfAndAddRegistrationMarkAsyncHandler(_obj, versionId, rightIndent, bottomIndent);
        result.IsOnConvertion = true;
        result.HasErrors = false;
      }
      
      Logger.DebugFormat("Registration stamp. Added {5}. Document id - {0}, kind - {6}, format - {1}, application - {2}, right indent - {3}, bottom indent - {4}.",
                         _obj.Id, _obj.AssociatedApplication.Extension, _obj.AssociatedApplication, rightIndent, bottomIndent,
                         isInteractive ? "interactively" : "async", _obj.DocumentKind.DisplayValue);
      
      return result;
    }
    
    /// <summary>
    /// Создать асинхронный обработчик для преобразования документа в PDF с отметкой о поступлении.
    /// </summary>
    /// <param name="versionId">ИД версии документа.</param>
    /// <param name="rightIndent">Отступ справа для отметки.</param>
    /// <param name="bottomIndent">Отступ снизу для отметки.</param>
    [Public, Remote]
    public virtual void CreateConvertToPdfAndAddRegistrationMarkAsyncHandler(long versionId, double rightIndent, double bottomIndent)
    {
      var asyncAddRegistrationStamp = Docflow.AsyncHandlers.AddRegistrationStamp.Create();
      asyncAddRegistrationStamp.DocumentId = _obj.Id;
      asyncAddRegistrationStamp.VersionId = versionId;
      asyncAddRegistrationStamp.RightIndent = rightIndent;
      asyncAddRegistrationStamp.BottomIndent = bottomIndent;
      
      var startedNotificationText = OfficialDocuments.Resources.ConvertionInProgress;
      var completedNotificationText = IncomingDocumentBases.Resources.AddRegistrationStampCompleteNotificationFormat(Hyperlinks.Get(_obj));
      var errorNotificationText = Sungero.Docflow.IncomingDocumentBases.Resources.AddRegistrationStampErrorNotificationFormat(Hyperlinks.Get(_obj), Environment.NewLine);
      
      asyncAddRegistrationStamp.ExecuteAsync(startedNotificationText, completedNotificationText, errorNotificationText, Users.Current);
    }
    
    /// <summary>
    /// Проверить, что можно сменить тип документа на простой.
    /// </summary>
    /// <returns>True - если можно сменить, иначе - false.</returns>
    [Remote(IsPure = true)]
    public override bool HasSpecifiedTypeRelations()
    {
      var hasSpecifiedTypeRelations = false;
      AccessRights.AllowRead(
        () =>
        {
          hasSpecifiedTypeRelations = OutgoingDocumentBases.GetAll().Any(x => x.InResponseToDocuments.Any(d => Equals(d.Document, _obj)));
        });
      return _obj.InResponseTo != null || hasSpecifiedTypeRelations || base.HasSpecifiedTypeRelations();
    }
  }
}