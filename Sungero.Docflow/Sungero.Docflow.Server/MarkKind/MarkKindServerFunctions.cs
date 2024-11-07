using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.MarkKind;

namespace Sungero.Docflow.Server
{
  partial class MarkKindFunctions
  {
    /// <summary>
    /// Получить вид отметки по SID.
    /// </summary>
    /// <param name="sid">Строковый идентификатор.</param>
    /// <returns>Вид отметки с указанным SID.</returns>
    [Public]
    public static IMarkKind GetMarkKind(string sid)
    {
      return MarkKinds.GetAll(mk => mk.Sid == sid).FirstOrDefault();
    }
    
    /// <summary>
    /// Получить содержимое отметки для простановки.
    /// </summary>
    /// <param name="document">Документ, на который будет проставлена отметка.</param>
    /// <param name="versionId">ИД версии документа.</param>
    /// <returns>Содержимое отметки в виде строки.</returns>
    public virtual string GetContent(IOfficialDocument document, long versionId)
    {
      var parameters = new object[] { document, versionId };
      return PublicFunctions.Module.ExecuteMarkFunction(_obj.MarkContentClassName, _obj.MarkContentFunctionName, parameters).ToString();
    }
  }
}