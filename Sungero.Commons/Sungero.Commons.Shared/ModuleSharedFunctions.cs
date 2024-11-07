using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace Sungero.Commons.Shared
{
  public class ModuleFunctions
  {
    #region Интеллектуальная обработка
    
    /// <summary>
    /// Получить значение объекта в виде строки.
    /// </summary>
    /// <param name="propertyValue">Объект.</param>
    /// <returns>Значение объекта в виде строки.</returns>
    /// <remarks>Для объектов типа Sungero.Domain.Shared.IEntity будет возвращена строка с ID сущности.</remarks>
    [Public]
    public static string GetValueAsString(object propertyValue)
    {
      if (propertyValue == null)
        return string.Empty;
      
      var propertyStringValue = propertyValue.ToString();
      if (propertyValue is Sungero.Domain.Shared.IEntity)
        propertyStringValue = ((Sungero.Domain.Shared.IEntity)propertyValue).Id.ToString();
      return propertyStringValue;
    }
    
    #endregion
    
    #region Работа с параметрами сущности
    
    /// <summary>
    /// Проверить наличие параметра сущности по наименованию.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <param name="paramName">Наименование параметра.</param>
    /// <returns>True - сущность содержит параметр, False - иначе.</returns>
    /// <exception cref="ArgumentNullException">Если entity == null.</exception>
    [Public]
    public static bool EntityParamsContainsKey(IEntity entity, string paramName)
    {
      if (entity == null)
        throw new ArgumentNullException(nameof(entity));
      
      return ((Domain.Shared.IExtendedEntity)entity).Params.ContainsKey(paramName);
    }
    
    /// <summary>
    /// Получить значение параметра сущности по наименованию.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <param name="paramName">Наименование параметра.</param>
    /// <returns>Значение параметра или null, если такого параметра не существует.</returns>
    /// <exception cref="ArgumentNullException">Если entity == null.</exception>
    [Public]
    public static object GetEntityParamsValue(IEntity entity, string paramName)
    {
      if (!EntityParamsContainsKey(entity, paramName))
        return null;
      
      object value;
      if (((Domain.Shared.IExtendedEntity)entity).Params.TryGetValue(paramName, out value))
        return value;
      
      return null;
    }
    
    /// <summary>
    /// Получить bool значение параметра сущности по наименованию.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <param name="paramName">Наименование параметра.</param>
    /// <returns>Значение параметра или False, если такого параметра не существует.</returns>
    /// <exception cref="ArgumentNullException">Если entity == null.</exception>
    [Public]
    public static bool GetBooleanEntityParamsValue(IEntity entity, string paramName)
    {
      var value = (bool?)GetEntityParamsValue(entity, paramName);
      return value.GetValueOrDefault();
    }
    
    /// <summary>
    /// Добавить или обновить значение параметра сущности по наименованию.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <param name="paramName">Наименование параметра.</param>
    /// <param name="paramValue">Значение параметра.</param>
    [Public]
    public static void AddOrUpdateEntityParams(IEntity entity, string paramName, object paramValue)
    {
      var extendedEntity = (Domain.Shared.IExtendedEntity)entity;
      if (EntityParamsContainsKey(entity, paramName))
        extendedEntity.Params[paramName] = paramValue;
      else
        extendedEntity.Params.Add(paramName, paramValue);
    }
    
    /// <summary>
    /// Удалить параметр сущности по наименованию.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <param name="paramName">Наименование параметра.</param>
    [Public]
    public static void DeleteEntityParams(IEntity entity, string paramName)
    {
      var extendedEntity = (Domain.Shared.IExtendedEntity)entity;
      if (EntityParamsContainsKey(entity, paramName))
        extendedEntity.Params.Remove(paramName);
    }
    
    #endregion
    
    /// <summary>
    /// Рассчитать сумму НДС.
    /// </summary>
    /// <param name="totalAmount">Сумма с НДС.</param>
    /// <param name="vatRate">Ставка НДС.</param>
    /// <returns>Сумма НДС.</returns>
    [Public]
    public virtual double GetVatAmountFromTotal(double totalAmount, Sungero.Commons.IVatRate vatRate)
    {
      if (vatRate == null)
        throw new ArgumentNullException(VatRates.Info.LocalizedName);
      
      if (!vatRate.Rate.HasValue)
        throw new ArgumentNullException(VatRates.Info.Properties.Rate.LocalizedName);
      
      var rateValue = Math.Round((double)vatRate.Rate.Value / 100, 2);
      return Math.Round(totalAmount * rateValue / (1 + rateValue), 2);
    }
    
    /// <summary>
    /// Получить отображаемое имя типа сущности.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <param name="declension">Падеж.</param>
    /// <returns>Отображаемое имя типа сущности.</returns>
    [Public]
    public virtual string GetTypeDisplayValue(Sungero.Domain.Shared.IEntity entity, CommonLibrary.DeclensionCase declension = CommonLibrary.DeclensionCase.Nominative)
    {
      if (entity == null)
        return string.Empty;
      
      var entityFinalType = entity.GetType().GetFinalType();
      var entityTypeMetadata = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(entityFinalType);
      var displayName = entityTypeMetadata.GetDisplayName();
      string result = displayName;
      try
      {
        result = CommonLibrary.Padeg.ConvertCurrencyNameToTargetDeclension(displayName, declension);
      }
      catch (Exception ex)
      {
        // Журналируем ошибку, связанную с падежом для выявления обстоятельств возникновения ошибки.
        Logger.Error("Exception in CommonLibrary.Padeg.ConvertCurrencyNameToTargetDeclension. entityFinalType: '{0}'; entity.Id: {1}; declension: {2}",
                     ex, entityFinalType, entity.Id, declension);
      }

      return result;
    }
  }
}