using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Mark;

namespace Sungero.Docflow.Server
{
  partial class MarkFunctions
  {
    /// <summary>
    /// Создать модель отметки для простановки.
    /// </summary>
    /// <returns>Модель отметки.</returns>
    public virtual Structures.Module.IMarkDto CreateMarkDto()
    {
      var dto = Structures.Module.MarkDto.Create();
      dto.Id = _obj.Id;
      dto.Anchor = _obj.Anchor;
      dto.X = _obj.XIndent.GetValueOrDefault();
      dto.Y = _obj.YIndent.GetValueOrDefault();
      dto.RotateAngle = _obj.RotateAngle;
      dto.Page = _obj.Page.GetValueOrDefault();
      dto.OnBlankPage = _obj.MarkKind.OnBlankPage.GetValueOrDefault();
      dto.IsSuccessful = false;
      
      dto.Tags = new List<string>();
      foreach (var tagRow in _obj.Tags)
        dto.Tags.Add(tagRow.Tag);
      
      dto.AdditionalParams = new Dictionary<string, string>();
      foreach (var param in _obj.AdditionalParams)
        dto.AdditionalParams.Add(param.Name, param.Value);
      
      this.TryFillDtoContent(dto);
      
      return dto;
    }
    
    /// <summary>
    /// Попытаться заполнить контент для модели отметки.
    /// </summary>
    /// <param name="dto">Модель отметки.</param>
    public virtual void TryFillDtoContent(Structures.Module.IMarkDto dto)
    {
      try
      {
        dto.Content = this.GetContent();
      }
      catch (Exception ex)
      {
        dto.IsSuccessful = false;
        Logger.DebugFormat("ConvertToPDF. TryFillDtoContent. The content is not filled due to an error: {0}", ex.Message);
      }
    }
    
    /// <summary>
    /// Получить содержимое отметки для простановки.
    /// </summary>
    /// <returns>Содержимое отметки в виде строки.</returns>
    public virtual string GetContent()
    {
      string content = string.Empty;
      var document = OfficialDocuments.Get(_obj.DocumentId.Value);
      using (TenantInfo.Culture.SwitchTo())
        content = Functions.MarkKind.GetContent(_obj.MarkKind, document, _obj.VersionId.Value);
      
      /* Приоритет простановки штампов: тэги, координаты.
       * Для простановки по координатам нужно возвращать html-контент.
       */
      if (!_obj.Tags.Any() && !this.StringLikeHtmlContent(content))
        content = Docflow.Resources.StringToHtmlDocumentWrapperFormat(content);
      
      return content;
    }
    
    /// <summary>
    /// Строка похожа на Html.
    /// </summary>
    /// <param name="input">Строка.</param>
    /// <returns>True - строка похожа на Html. False - иначе.</returns>
    public virtual bool StringLikeHtmlContent(string input)
    {
      return System.Text.RegularExpressions.Regex.IsMatch(input, Constants.Module.HtmlOpeningTagPattern) &&
        System.Text.RegularExpressions.Regex.IsMatch(input, Constants.Module.HtmlClosingTagPattern);
    }
  }
}