using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Mark;

namespace Sungero.Docflow.Shared
{
  partial class MarkFunctions
  {
    /// <summary>
    /// Заполнить имя отметки.
    /// </summary>
    public virtual void FillName()
    {
      _obj.Name = Marks.Resources.MarkNameTemplateFormat(_obj.MarkKind.Name, _obj.DocumentId, _obj.VersionId);
    }
    
    /// <summary>
    /// Заполнить отступ снизу.
    /// </summary>
    /// <param name="indent">Отступ снизу в см.</param>
    /// <remarks>В отметке координаты хранятся в системе координат с началом в левом верхнем углу.</remarks>
    public virtual void FillYIndentFromBottom(double? indent)
    {
      _obj.YIndent = -1 * indent;
    }
    
    /// <summary>
    /// Заполнить отступ сверху.
    /// </summary>
    /// <param name="indent">Отступ сверху в см.</param>
    /// <remarks>В отметке координаты хранятся в системе координат с началом в левом верхнем углу.</remarks>
    public virtual void FillYIndentFromTop(double? indent)
    {
      _obj.YIndent = indent;
    }
    
    /// <summary>
    /// Заполнить отступ слева.
    /// </summary>
    /// <param name="indent">Отступ слева в см.</param>
    /// <remarks>В отметке координаты хранятся в системе координат с началом в левом верхнем углу.</remarks>
    public virtual void FillXIndentFromLeft(double? indent)
    {
      _obj.XIndent = indent;
    }
    
    /// <summary>
    /// Заполнить отступ справа.
    /// </summary>
    /// <param name="indent">Отступ справа в см.</param>
    /// <remarks>В отметке координаты хранятся в системе координат с началом в левом верхнем углу.</remarks>
    public virtual void FillXIndentFromRight(double? indent)
    {
      _obj.XIndent = -1 * indent;
    }
    
    /// <summary>
    /// Обновить список тегов для отметки.
    /// </summary>
    /// <param name="tags">Список тегов.</param>
    /// <remarks>Не добавляет уже имеющиеся теги.</remarks>
    public virtual void UpdateTags(List<string> tags)
    {
      foreach (var tag in tags)
      {
        if (!_obj.Tags.Any(t => t.Tag == tag))
          _obj.Tags.AddNew().Tag = tag;
      }
    }
    
    /// <summary>
    /// Обновить символ якоря для отметки.
    /// </summary>
    /// <param name="anchor">Символ якоря.</param>
    public virtual void UpdateAnchor(string anchor)
    {
      _obj.Anchor = anchor;
    }
    
    /// <summary>
    /// Добавить дополнительный параметр.
    /// </summary>
    /// <param name="name">Имя параметра.</param>
    /// <param name="value">Значение параметра.</param>
    /// <remarks>Не добавляет уже имеющиеся параметры.</remarks>
    public virtual void AddAdditionalParameter(string name, string value)
    {
      if (!_obj.AdditionalParams.Any(x => x.Name == name))
      {
        var paramRow = _obj.AdditionalParams.AddNew();
        paramRow.Name = name;
        paramRow.Value = value;
      }
    }
  }
}