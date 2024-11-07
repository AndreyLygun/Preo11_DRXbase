using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.MediumType;

namespace Sungero.Docflow.Server
{
  partial class MediumTypeFunctions
  {
    /// <summary>
    /// Получить предустановленный электронный вид носителя документа.
    /// </summary>
    /// <returns>Предустановленный электронный вид носителя документа.</returns>
    [Remote, Public]
    public static IMediumType GetNativeElectronicMediumType()
    {
      return GetMediumType(Constants.MediumType.ElectronicMediumTypeSid);
    }
    
    /// <summary>
    /// Получить предустановленный бумажный вид носителя документа.
    /// </summary>
    /// <returns>Предустановленный бумажный вид носителя документа.</returns>
    [Remote, Public]
    public static IMediumType GetNativePaperMediumType()
    {
      return GetMediumType(Constants.MediumType.PaperMediumTypeSid);
    }

    /// <summary>
    /// Получить предустановленный гибридный вид носителя документа.
    /// </summary>
    /// <returns>Предустановленный гибридный вид носителя документа.</returns>
    [Remote, Public]
    public static IMediumType GetNativeMixedMediumType()
    {
      return GetMediumType(Constants.MediumType.MixedMediumTypeSid);
    }
    
    /// <summary>
    /// Получить вид носителя документа по sid.
    /// </summary>
    /// <param name="sid">Sid.</param>
    /// <returns>Вид носителя документа.</returns>
    public static IMediumType GetMediumType(string sid)
    {
      return MediumTypes.GetAll(t => t.Sid == sid).FirstOrDefault();
    }
  }
}