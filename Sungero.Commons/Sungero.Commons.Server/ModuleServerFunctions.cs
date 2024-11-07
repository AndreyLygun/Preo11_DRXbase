using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Sungero.Commons.Structures.Module;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.ElasticsearchExtensions;

namespace Sungero.Commons.Server
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Получить ссылки внешней системы, соответствующие заданным коду внешней системы и ИД сущности внешней системы.
    /// </summary>
    /// <param name="uuid">ИД сущности во внешней системе.</param>
    /// <param name="sysid">Код внешней системы.</param>
    /// <returns>Ссылки внешней системы.</returns>
    [Public]
    public IQueryable<IExternalEntityLink> GetExternalEntityLinks(string uuid, string sysid)
    {
      var result = ExternalEntityLinks.GetAll()
        .Where(x => x.ExtSystemId == sysid)
        .Where(x => x.ExtEntityId == uuid);
      return result;
    }
    
    /// <summary>
    /// Создать новый населенный пункт.
    /// </summary>
    /// <returns>Новый населенный пункт.</returns>
    [Remote]
    public static ICity CreateNewCity()
    {
      return Cities.Create();
    }
    
    /// <summary>
    /// Проверить, что для сущности все ExternalEntityLinks помечены IsDeleted.
    /// </summary>
    /// <param name="entity"> Сущность.</param>
    /// <returns>True, если так, иначе False.</returns>
    [Public]
    public static bool IsAllExternalEntityLinksDeleted(Sungero.Domain.Shared.IEntity entity)
    {
      var typeGuid = entity.TypeDiscriminator.ToString().ToUpper();
      var entityExternalLinks = ExternalEntityLinks.GetAll().Where(x => x.EntityType.ToUpper() == typeGuid &&
                                                                   x.EntityId == entity.Id ||
                                                                   x.ExtEntityId == entity.Id.ToString() &&
                                                                   x.ExtEntityType.ToUpper() == typeGuid);
      if (entityExternalLinks.Any(x => x.IsDeleted != true))
        return false;
      else
      {
        foreach (var link in entityExternalLinks)
        {
          ExternalEntityLinks.Delete(link);
        }
        return true;
      }
    }
    
    /// <summary>
    /// Проверить, что культура СП русская.
    /// </summary>
    /// <returns>True, если культура СП русская, иначе False.</returns>
    [Public]
    public static bool IsServerCultureRussian()
    {
      return Sungero.Core.TenantInfo.Culture.TwoLetterISOLanguageName.ToLower() == "ru";
    }
    
    /// <summary>
    /// Получить имя конечного типа сущности.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <returns>Имя конечного типа сущности.</returns>
    [Public]
    public static string GetFinalTypeName(Sungero.Domain.Shared.IEntity entity)
    {
      var entityFinalType = entity.GetType().GetFinalType();
      var entityTypeMetadata = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(entityFinalType);
      return entityTypeMetadata.GetDisplayName();
    }
    
    #region Интеллектуальная обработка
    
    /// <summary>
    /// Получить результаты предшествующего распознавания свойства сущности по факту, пришедшему из Ario.
    /// </summary>
    /// <param name="fact">Факт.</param>
    /// <param name="propertyName">Имя свойства, связанного с фактом.</param>
    /// <returns>Результаты последнего распознавания свойства сущности по факту, идентичному переданному.</returns>
    /// <remarks>Метод возвращает информацию о верификации данных пользователем.
    /// Подтвержденное пользователем значение находится в поле VerifiedValue IEntityRecognitionInfoFact.</remarks>
    [Public]
    public virtual IEntityRecognitionInfoFacts GetPreviousPropertyRecognitionResults(IArioFact fact,
                                                                                     string propertyName)
    {
      var factLabel = GetFactLabel(fact, propertyName);
      var recognitionInfo = EntityRecognitionInfos.GetAll()
        .Where(d => d.Facts.Any(f => f.FactLabel == factLabel && f.VerifiedValue != null && f.VerifiedValue != string.Empty))
        .OrderByDescending(d => d.Id)
        .FirstOrDefault();
      
      if (recognitionInfo == null)
        return null;
      
      return recognitionInfo.Facts
        .Where(f => f.FactLabel == factLabel && !string.IsNullOrWhiteSpace(f.VerifiedValue)).First();
    }
    
    /// <summary>
    /// Получить результаты предшествующего распознавания свойства сущности по факту, пришедшему из Ario.
    /// </summary>
    /// <param name="fact">Факт.</param>
    /// <param name="propertyName">Имя свойства, связанного с фактом.</param>
    /// <param name="filterPropertyValue">Значение свойства для дополнительной фильтрации результатов распознавания сущности.</param>
    /// <param name="filterPropertyName">Имя свойства для дополнительной фильтрации результатов распознавания сущности.</param>
    /// <returns>Результаты последнего распознавания свойства сущности по факту, идентичному переданному.</returns>
    /// <remarks>Метод возвращает информацию о верификации данных пользователем.
    /// Подтвержденное пользователем значение находится в поле VerifiedValue IEntityRecognitionInfoFact.</remarks>
    [Public]
    public virtual IEntityRecognitionInfoFacts GetPreviousPropertyRecognitionResults(IArioFact fact,
                                                                                     string propertyName,
                                                                                     string filterPropertyValue,
                                                                                     string filterPropertyName)
    {
      var factLabel = GetFactLabel(fact, propertyName);
      var recognitionInfo = EntityRecognitionInfos.GetAll()
        .Where(d => d.Facts.Any(f => f.FactLabel == factLabel && f.VerifiedValue != null && f.VerifiedValue != string.Empty) &&
               d.Facts.Any(f => f.PropertyName == filterPropertyName && f.PropertyValue == filterPropertyValue))
        .OrderByDescending(d => d.Id)
        .FirstOrDefault();
      
      if (recognitionInfo == null)
        return null;
      
      return recognitionInfo.Facts
        .Where(f => f.FactLabel == factLabel && !string.IsNullOrWhiteSpace(f.VerifiedValue)).First();
    }
    
    /// <summary>
    /// Проверить, приобретена ли лицензия на модуль Интеллектуальный слой.
    /// </summary>
    /// <returns>True - если лицензия есть, иначе - false.</returns>
    [Public, Obsolete("Метод не используется с 17.04.2024 и версии 4.10. Используйте Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Sungero.Commons.PublicConstants.Module.IntelligenceGuid).")]
    public bool IsIntelligenceEnabled()
    {
      if (!Sungero.Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Constants.Module.IntelligenceGuid))
      {
        Logger.Debug("IsIntelligenceEnabled. Module license \"Intelligence\" not found.");
        return false;
      }
      return true;
    }
    
    #region Работа с фактами и полями

    /// <summary>
    /// Получить список фактов с переданным именем факта.
    /// </summary>
    /// <param name="facts">Факты.</param>
    /// <param name="factName">Имя факта.</param>
    /// <returns>Список фактов с искомым именем.</returns>
    [Public]
    public static List<IArioFact> GetFacts(List<IArioFact> facts, string factName)
    {
      return facts
        .Where(f => f.Name == factName)
        .ToList();
    }
    
    /// <summary>
    /// Получить список фактов с переданными именем факта и именем поля.
    /// </summary>
    /// <param name="facts">Факты.</param>
    /// <param name="factName">Имя факта.</param>
    /// <param name="fieldName">Имя поля.</param>
    /// <returns>Список фактов с искомыми именами факта и поля.</returns>
    [Public]
    public static List<IArioFact> GetFacts(List<IArioFact> facts, string factName, string fieldName)
    {
      return facts
        .Where(f => f.Name == factName)
        .Where(f => f.Fields.Any(fl => fl.Name == fieldName))
        .ToList();
    }
    
    /// <summary>
    /// Получить метку факта.
    /// </summary>
    /// <param name="fact">Факт из Арио.</param>
    /// <param name="propertyName">Имя связанного свойства.</param>
    /// <returns>Метка факта.</returns>
    /// <remarks>Используется для быстрого поиска факта в результатах извлечения фактов.</remarks>
    [Public]
    public static string GetFactLabel(IArioFact fact, string propertyName)
    {
      string factInfo = fact.Name + propertyName;
      foreach (var field in fact.Fields)
        factInfo += field.Name + field.Value;
      
      var factHash = string.Empty;
      using (MD5 md5Hash = MD5.Create())
      {
        byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(factInfo));
        for (int i = 0; i < data.Length; i++)
          factHash += data[i].ToString("x2");
      }
      return factHash;
    }
    
    /// <summary>
    /// Получить список фактов, отфильтрованный по имени факта и отсортированный по вероятности поля.
    /// </summary>
    /// <param name="facts">Список фактов.</param>
    /// <param name="factName">Имя факта.</param>
    /// <param name="orderFieldName">Имя поля, по вероятности которого будет произведена сортировка.</param>
    /// <returns>Отсортированный список фактов.</returns>
    /// <remarks>С учетом вероятности факта.</remarks>
    [Public]
    public static List<IArioFact> GetOrderedFacts(List<IArioFact> facts, string factName, string orderFieldName)
    {
      return facts
        .Where(f => f.Name == factName)
        .Where(f => f.Fields.Any(fl => fl.Name == orderFieldName))
        .OrderByDescending(f => f.Fields.First(fl => fl.Name == orderFieldName).Probability)
        .ToList();
    }

    /// <summary>
    /// Получить список фактов, отфильтрованный по имени и типу факта и отсортированный по вероятности поля.
    /// </summary>
    /// <param name="facts">Список фактов.</param>
    /// <param name="factName">Имя факта.</param>
    /// <param name="factType">Тип факта.</param>
    /// <param name="typeFieldName">Имя поля, содержащего тип факта.</param>
    /// <param name="orderFieldName">Имя поля, по вероятности которого будет произведена сортировка.</param>
    /// <returns>Отсортированный список фактов.</returns>
    [Public]
    public static List<IArioFact> GetOrderedFactsByType(List<IArioFact> facts, string factName, string factType,
                                                        string typeFieldName, string orderFieldName)
    {
      return facts
        .Where(f => f.Name == factName)
        .Where(f => f.Fields.Any(fl => fl.Name == typeFieldName && fl.Value == factType))
        .Where(f => f.Fields.Any(fl => fl.Name == orderFieldName && !string.IsNullOrEmpty(fl.Value)))
        .OrderByDescending(f => f.Fields.First(fl => fl.Name == orderFieldName).Probability)
        .ToList();
    }
    
    /// <summary>
    /// Получить список фактов, отфильтрованный по имени факта и отсортированный по количеству и приоритету полей.
    /// </summary>
    /// <param name="facts">Список фактов.</param>
    /// <param name="factName">Имя факта.</param>
    /// <param name="fieldWeights">Список наименований полей и их весов для расчета вероятности факта.</param>
    /// <returns>Отсортированный список фактов.</returns>
    /// <remarks>При равном приоритете полей учитывается их средневзвешенная вероятность.</remarks>
    [Public]
    public static List<IArioFact> GetOrderedFactsByFieldPriorities(List<IArioFact> facts, string factName,
                                                                   System.Collections.Generic.IDictionary<string, double> fieldWeights)
    {
      if (!facts.Any())
        return facts;
      
      var fieldNames = fieldWeights.Keys.ToList();
      var filteredFacts = facts.Where(f => f.Name == factName && f.Fields.Any(fl => fieldNames.Contains(fl.Name) &&
                                                                              !string.IsNullOrEmpty(fl.Value)));
      
      /* Рассчитать оценку непустых полей для каждого факта в зависимости от занимаемой позиции поля в переданном списке.
       * Последнему полю присвоить оценку 1, предпоследнему - 2, третьему снизу - 4 и т.д. по степеням двойки.
       * Отсортировать факты по убыванию суммарной оценки полей.
       * При равной оценке более приоритетным считать факт с большей средневзвешенной вероятностью полей.
       */
      var factScores = new Dictionary<IArioFact, double>();
      var totalWeights = Math.Round(fieldWeights.Sum(x => x.Value), 4);
      foreach (var fact in filteredFacts)
      {
        var fieldsWithValue = fact.Fields.Where(fl => fieldNames.Contains(fl.Name) && !string.IsNullOrEmpty(fl.Value));
        var score = fieldsWithValue.Sum(fl => Math.Pow(2, fieldNames.Count - fieldNames.IndexOf(fl.Name) - 1));
        var aggregateProbability = totalWeights > 0 ?
          fieldsWithValue.Sum(fl => fl.Probability * fieldWeights[fl.Name]) / totalWeights :
          fieldsWithValue.Sum(fl => fl.Probability) / fieldWeights.Count;
        score += Math.Round(aggregateProbability / 100, 4);
        factScores.Add(fact, score);
      }
      
      return factScores.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();
    }
    
    /// <summary>
    /// Получить поле из факта.
    /// </summary>
    /// <param name="fact">Факт.</param>
    /// <param name="fieldName">Имя поля.</param>
    /// <returns>Поле из факта.</returns>
    [Public]
    public static IArioFactField GetField(IArioFact fact, string fieldName)
    {
      return GetFields(fact, new List<string>() { fieldName })
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить список полей из факта.
    /// </summary>
    /// <param name="fact">Имя факта.</param>
    /// <param name="fieldNames">Список имен поля.</param>
    /// <returns>Список полей.</returns>
    [Public]
    public static IQueryable<IArioFactField> GetFields(IArioFact fact, List<string> fieldNames)
    {
      if (fact == null)
        return null;
      return fact.Fields.Where(f => fieldNames.Contains(f.Name)).AsQueryable();
    }
    
    /// <summary>
    /// Получить обобщенную вероятность по полям факта.
    /// </summary>
    /// <param name="fact">Факт.</param>
    /// <param name="weightedFields">Имена полей факта с весами.</param>
    /// <returns>Обобщенная вероятность.</returns>
    [Public]
    public virtual double GetAggregateFieldsProbability(IArioFact fact, System.Collections.Generic.IDictionary<string, double> weightedFields)
    {
      var recognizedFields = fact.Fields
        .Where(fl => weightedFields.ContainsKey(fl.Name) && !string.IsNullOrEmpty(fl.Value))
        .ToDictionary(fl => fl, fl => weightedFields[fl.Name]);
      return this.GetAggregateFieldsProbability(recognizedFields);
    }
    
    /// <summary>
    /// Получить обобщенную вероятность по полям.
    /// </summary>
    /// <param name="weightedFields">Поля факта с весами.</param>
    /// <returns>Обобщенная вероятность.</returns>
    /// <remarks>Основана на формуле полной вероятности: P(A) = P(B1) x P(A|B1) + ... + P(Bn) x P(A|Bn).
    /// <para>Здесь:</para>
    /// <para>- P(Bi) - вероятность некоторого события/фактора;</para>
    /// <para>- P(A|Bi) - вероятность наступления события A в результате Bi.</para>
    /// <para></para>
    /// <para>В нашем случае:</para>
    /// <para>- А - насколько точную совокупную информацию несет факт;</para>
    /// <para>- Bi - вероятность конкретного поля факта;</para>
    /// <para>- P(A|Bi) - насколько поле значимо среди остальных полей факта.</para>
    /// <para></para>
    /// <para>Поля с пустыми Value исключаются из расчета. Нормализация полной вероятности применяется для защиты от:</para>
    /// <para>- отсутствующих полей;</para>
    /// <para>- полей с пустым Value.</para>
    /// <para>P(B1) x P(A|B1) + ... + P(Bk) x P(A|Bk)</para>
    /// <para>---------------------------------------</para>
    /// <para>        P(A|B1) + ... + P(A|Bk)        </para>
    /// </remarks>
    [Public]
    public virtual double GetAggregateFieldsProbability(System.Collections.Generic.IDictionary<IArioFactField, double> weightedFields)
    {
      // Сумма весов полей.
      var weightSum = 0.0;
      // Сумма произведений вероятностей непустых полей и их весов.
      var probabilitySum = 0.0;
      
      foreach (var weightedField in weightedFields)
      {
        // Если поле не имеет значения, то переходим к следующему.
        if (string.IsNullOrEmpty(weightedField.Key.Value))
          continue;
        
        weightSum += weightedField.Value;
        probabilitySum += weightedField.Key.Probability * weightedField.Value;
      }
      
      if (weightSum == 0)
        return 0.0;
      
      // Сумма весов полей в общем случае не равна 1.
      return probabilitySum / weightSum;
    }
    
    /// <summary>
    /// Получить значение поля из факта.
    /// </summary>
    /// <param name="fact">Имя факта, поле которого будет извлечено.</param>
    /// <param name="fieldName">Имя поля, значение которого нужно извлечь.</param>
    /// <returns>Значение поля.</returns>
    [Public]
    public static string GetFieldValue(IArioFact fact, string fieldName)
    {
      if (fact == null)
        return string.Empty;
      
      var field = fact.Fields.FirstOrDefault(f => f.Name == fieldName);
      if (field != null)
        return field.Value;
      
      return string.Empty;
    }

    /// <summary>
    /// Получить значение поля из фактов.
    /// </summary>
    /// <param name="facts"> Список фактов.</param>
    /// <param name="factName"> Имя факта, поле которого будет извлечено.</param>
    /// <param name="fieldName">Имя поля, значение которого нужно извлечь.</param>
    /// <returns>Значение поля, полученное из Ario с наибольшей вероятностью.</returns>
    [Public]
    public static string GetFieldValue(List<IArioFact> facts, string factName, string fieldName)
    {
      IEnumerable<IArioFactField> fields = facts
        .Where(f => f.Name == factName)
        .Where(f => f.Fields.Any())
        .SelectMany(f => f.Fields);
      var field = fields
        .OrderByDescending(f => f.Probability)
        .FirstOrDefault(f => f.Name == fieldName);
      if (field != null)
        return field.Value;
      
      return string.Empty;
    }
    
    /// <summary>
    /// Получить значение поля типа DateTime из фактов.
    /// </summary>
    /// <param name="fact">Имя факта, поле которого будет извлечено.</param>
    /// <param name="fieldName">Имя поля, значение которого нужно извлечь.</param>
    /// <returns>Значение поля типа DateTime.</returns>
    [Public]
    public static DateTime? GetFieldDateTimeValue(IArioFact fact, string fieldName)
    {
      var recognizedDate = GetFieldValue(fact, fieldName);
      if (string.IsNullOrWhiteSpace(recognizedDate))
        return null;
      
      DateTime date;
      if (Calendar.TryParseDate(recognizedDate, out date))
        return date;
      else
        return null;
    }

    /// <summary>
    /// Получить числовое значение поля из фактов.
    /// </summary>
    /// <param name="fact">Имя факта, поле которого будет извлечено.</param>
    /// <param name="fieldName">Имя поля, значение которого нужно извлечь.</param>
    /// <returns>Числовое значение поля.</returns>
    [Public]
    public static double? GetFieldNumericalValue(IArioFact fact, string fieldName)
    {
      var field = GetFieldValue(fact, fieldName);
      if (string.IsNullOrWhiteSpace(field))
        return null;

      double result;
      double.TryParse(field, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out result);
      return result;
    }

    /// <summary>
    /// Получить вероятность.
    /// </summary>
    /// <param name="fact">Факт.</param>
    /// <param name="fieldName">Имя поля.</param>
    /// <returns>Вероятность.</returns>
    [Public]
    public static double? GetFieldProbability(IArioFact fact, string fieldName)
    {
      if (fact == null)
        return null;
      
      var field = fact.Fields.FirstOrDefault(f => f.Name == fieldName);
      if (field == null)
        return null;
      
      return field.Probability;
    }
    
    #endregion
    
    #region Работа с Elasticsearch
    
    #region Подключение к сервису

    /// <summary>
    /// Проверить наличие сконфигурированного url Elasticsearch и существования индексов.
    /// </summary>
    /// <returns>Результат проверки.</returns>
    [Public]
    public virtual bool IsElasticsearchConfigured()
    {
      return !string.IsNullOrEmpty(this.GetElasticsearchUrl()) &&
        Docflow.PublicFunctions.Module.DocflowParamsTableExist() &&
        Docflow.PublicFunctions.Module.GetDocflowParamsValue(Commons.PublicConstants.Module.AllIndicesExistParamName) != null;
    }

    /// <summary>
    /// Получить url Elasticsearch.
    /// </summary>
    /// <returns>Url Elasticsearch.</returns>
    [Public]
    public virtual string GetElasticsearchUrl()
    {
      return Domain.Server.AppSettings.Instance.ElasticSearchUrl;
    }

    /// <summary>
    /// Получить коннектор к сервису Elasticsearch.
    /// </summary>
    /// <returns>Коннектор.</returns>
    /// <remarks>Таймаут подключения 10 мин.</remarks>
    public virtual Sungero.ElasticsearchExtensions.ElasticsearchConnector GetElasticsearchConnector()
    {
      return ElasticsearchConnector.Get(this.GetElasticsearchUrl());
    }

    /// <summary>
    /// Получить коннектор к сервису Elasticsearch.
    /// </summary>
    /// <param name="elasticsearchUrl">Адрес сервиса.</param>
    /// <param name="timeout">Таймаут подключения, в секундах.</param>
    /// <returns>Коннектор.</returns>
    public virtual Sungero.ElasticsearchExtensions.ElasticsearchConnector GetElasticsearchConnector(string elasticsearchUrl, int timeout)
    {
      return ElasticsearchConnector.Get(elasticsearchUrl, timeout);
    }
    
    /// <summary>
    /// Получить имя индекса Elasticsearch.
    /// </summary>
    /// <param name="entityName">Имя сущности.</param>
    /// <returns>Имя индекса.</returns>
    [Public]
    public virtual string GetIndexName(string entityName)
    {
      return string.Format(Constants.Module.IndexNameTemplate,
                           entityName,
                           TenantInfo.TenantId,
                           Domain.Server.AppSettings.Instance.ElasticSearchIndexPostfix)
        .ToLower();
    }
    
    /// <summary>
    /// Проверить возможность подключения к сервису Elasticsearch.
    /// </summary>
    /// <returns>True - если сервис доступен, иначе - false.</returns>
    [Public]
    public virtual bool IsElasticsearchEnabled()
    {
      var url = this.GetElasticsearchUrl();
      if (string.IsNullOrWhiteSpace(url) || !System.Uri.IsWellFormedUriString(url, UriKind.Absolute))
      {
        Logger.Debug("IsElasticsearchEnabled. Elasticsearch URL not found.");
        return false;
      }

      try
      {
        var connector = this.GetElasticsearchConnector();
        connector.CheckConnection();
        return true;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("IsElasticsearchEnabled. Error connecting to Elasticsearch. {0}", ex, ex.Message);
        return false;
      }
    }
    
    #endregion
    
    #region Создание индексов. Первоначальная индексация сущностей

    /// <summary>
    /// Создать индекс.
    /// </summary>
    /// <param name="indexName">Имя индекса.</param>
    /// <param name="configJson">Настройки индекса в формате JSON.</param>
    /// <remarks>Если индекс был уже создан, он удаляется и создается заново.</remarks>
    [Public]
    public virtual void ElasticsearchCreateIndex(string indexName, string configJson)
    {
      var connector = this.GetElasticsearchConnector();
      connector.DeleteIndex(indexName);
      connector.CreateIndex(indexName, configJson);
    }

    /// <summary>
    /// Загрузить данные в индекс.
    /// </summary>
    /// <param name="indexName">Имя индекса.</param>
    /// <param name="contentJson">Json строка c объектами для индексации.</param>
    [Public]
    public virtual void ElasticsearchBulk(string indexName, string contentJson)
    {
      // Операцию массовой загрузки выполнять с увеличенным таймаутом подключения к сервису.
      var connector = this.GetElasticsearchConnector(this.GetElasticsearchUrl(), 3600);
      connector.Bulk(indexName, contentJson);
    }
    
    /// <summary>
    /// Сформировать строку с синонимами ОПФ.
    /// </summary>
    /// <returns>Cтрока с синонимами.</returns>
    [Public]
    public virtual string GetLegalFormSynonyms()
    {
      var synonyms = Constants.Module.LegalFormSynonyms.Replace(System.Environment.NewLine, string.Empty);
      return this.SynonymsParse(synonyms);
    }
    
    /// <summary>
    /// Отформатировать строку с синонимами.
    /// </summary>
    /// <param name="synonyms">Синонимы.</param>
    /// <returns>Отформатированная строка с синонимами.</returns>
    [Public]
    public virtual string SynonymsParse(string synonyms)
    {
      // Синонимы разделяются запятыми и оборачиваются двойными кавычками.
      if (!string.IsNullOrWhiteSpace(synonyms))
        return string.Join(",", synonyms.Split(';').Select(x => string.Format("\"{0}\"", x.Trim())));
      else
        return string.Empty;
    }
    
    /// <summary>
    /// Проверить существование индекса.
    /// </summary>
    /// <param name="indexName">Имя индекса.</param>
    /// <returns>True - индекс существует, иначе false.</returns>
    [Public]
    public virtual bool IsIndexExist(string indexName)
    {
      var isIndexExist = this.GetElasticsearchConnector().IsIndexExist(indexName);
      Logger.DebugFormat("IsIndexExist. IndexName: {0}, result: {1}.", indexName, isIndexExist.ToString());
      return isIndexExist;
    }
    
    #endregion
    
    #region Обновление настроек, закрытие и открытие индекса.
    
    /// <summary>
    /// Обновить настройки индекса.
    /// </summary>
    /// <param name="indexName">Имя индекса.</param>
    /// <param name="configJson">Настройки индекса в формате JSON.</param>
    [Public]
    public virtual void ElasticsearchUpdateIndexSettings(string indexName, string configJson)
    {
      var connector = this.GetElasticsearchConnector();
      connector.UpdateIndexSettings(indexName, configJson);
    }

    /// <summary>
    /// Открыть индекс.
    /// </summary>
    /// <param name="indexName">Имя индекса.</param>
    [Public]
    public virtual void ElasticsearchOpenIndex(string indexName)
    {
      var connector = this.GetElasticsearchConnector();
      connector.OpenIndex(indexName);
    }
    
    /// <summary>
    /// Закрыть индекс.
    /// </summary>
    /// <param name="indexName">Имя индекса.</param>
    [Public]
    public virtual void ElasticsearchCloseIndex(string indexName)
    {
      var connector = this.GetElasticsearchConnector();
      connector.CloseIndex(indexName);
    }
    
    /// <summary>
    /// Получить синонимы из индекса компаний.
    /// </summary>
    /// <param name="indexName">Имя индекса.</param>
    /// <returns>Список синонимов.</returns>
    [Public]
    public virtual List<string> ElasticsearchGetIndexSynonyms(string indexName)
    {
      var connector = this.GetElasticsearchConnector();
      return connector.GetIndexSynonyms(indexName);
    }
    #endregion
    
    #region Индексация и удаление сущностей

    /// <summary>
    /// Индексировать сущность.
    /// </summary>
    /// <param name="indexName">Имя индекса.</param>
    /// <param name="contentJson">Json строка с сущностью для индексации.</param>
    /// <param name="entityId">Ид сущности.</param>
    /// <param name="entityUpdated">Время обновления сущности.</param>
    /// <param name="allowCreateRecord">Разрешить добавление сущности в индекс.</param>
    [Public]
    public virtual void ElasticsearchIndexEntity(string indexName, string contentJson, long entityId, DateTime entityUpdated, bool allowCreateRecord)
    {
      this.GetElasticsearchConnector().IndexEntity(indexName, contentJson, entityId, entityUpdated, allowCreateRecord);
    }

    /// <summary>
    /// Удалить сущность из индекса.
    /// </summary>
    /// <param name="indexName">Имя индекса.</param>
    /// <param name="entityId">Ид сущности.</param>
    [Public]
    public virtual void ElasticsearchRemoveEntity(string indexName, long entityId)
    {
      this.GetElasticsearchConnector().RemoveEntity(indexName, entityId);
    }
    
    /// <summary>
    /// Создать АО для выполнения индексации.
    /// </summary>
    /// <param name="entityName">Имя сущности.</param>
    /// <param name="entityId">Ид сущности.</param>
    /// <param name="contentJson">Json строка c объектами для индексации.</param>
    /// <param name="allowCreateRecord">Разрешить добавление записи, если она не существует.</param>
    [Public]
    public virtual void CreateIndexEntityAsyncHandler(string entityName, long entityId, string contentJson, bool allowCreateRecord)
    {
      var asyncIndexingEntity = AsyncHandlers.IndexEntity.Create();
      asyncIndexingEntity.Json = contentJson;
      asyncIndexingEntity.IndexName = this.GetIndexName(entityName);
      asyncIndexingEntity.EntityId = entityId;
      asyncIndexingEntity.AllowCreateRecord = allowCreateRecord;
      asyncIndexingEntity.AsyncCreated = Calendar.Now;
      asyncIndexingEntity.ExecuteAsync();
    }
    
    /// <summary>
    /// Создать АО для удаления из индекса.
    /// </summary>
    /// <param name="entityName">Имя сущности.</param>
    /// <param name="entityId">Ид сущности.</param>
    [Public]
    public virtual void CreateRemoveEntityFromIndexAsyncHandler(string entityName, long entityId)
    {
      var asyncRemoveEntityFromIndex = AsyncHandlers.RemoveEntityFromIndex.Create();
      asyncRemoveEntityFromIndex.IndexName = this.GetIndexName(entityName);
      asyncRemoveEntityFromIndex.EntityId = entityId;
      asyncRemoveEntityFromIndex.ExecuteAsync();
    }

    /// <summary>
    /// Заменить спец. символы и кавычки в строке.
    /// </summary>
    /// <param name="stringToConvert">Строка для преобразования.</param>
    /// <returns>Преобразованная строка.</returns>
    [Public]
    public string TrimSpecialSymbols(string stringToConvert)
    {
      var convertedString = string.Empty;
      if (!string.IsNullOrEmpty(stringToConvert))
      {
        convertedString = Regex.Replace(stringToConvert, Constants.Module.SpecialSymbolsPattern, " ");
        convertedString = Regex.Replace(convertedString, Constants.Module.DoubleSpacePattern, " ");
        convertedString = convertedString.Trim();
      }
      
      return convertedString;
    }

    #endregion

    #region Формирование запросов поиска
    
    /// <summary>
    /// Получить строку поиска по ключевому слову.
    /// </summary>
    /// <param name="termName">Имя поля.</param>
    /// <param name="termValue">Значение поля.</param>
    /// <returns>Строка поиска.</returns>
    [Public]
    public virtual string GetTermQuery(string termName, string termValue)
    {
      return QueryBuilder.GetTermQuery(termName, termValue);
    }

    /// <summary>
    /// Получить строку поиска по ключевым словам.
    /// </summary>
    /// <param name="termName">Имя поля.</param>
    /// <param name="termValues">Значения полей.</param>
    /// <returns>Строка поиска.</returns>
    [Public]
    public virtual string GetTermsQuery(string termName, List<string> termValues)
    {
      return QueryBuilder.GetTermsQuery(termName, termValues);
    }

    /// <summary>
    /// Получить строку поиска по нечеткому вхождению строк.
    /// </summary>
    /// <param name="fieldName">Имя текстового поля.</param>
    /// <param name="searchValue">Искомая строка.</param>
    /// <param name="andOperator">Способ объединения результата поиска отдельных слов (true = И, false = ИЛИ).</param>
    /// <returns>Строка поиска.</returns>
    [Public]
    public virtual string GetMatchFuzzyQuery(string fieldName, string searchValue, bool andOperator)
    {
      return QueryBuilder.GetMatchFuzzyQuery(fieldName, searchValue, andOperator);
    }

    /// <summary>
    /// Получить строку поиска по четкому вхождению строк.
    /// </summary>
    /// <param name="fieldName">Имя текстового поля.</param>
    /// <param name="searchValue">Искомая строка.</param>
    /// <param name="andOperator">Способ объединения результата поиска отдельных слов (true = И, false = ИЛИ).</param>
    /// <returns>Строка поиска.</returns>
    [Public]
    public virtual string GetMatchQuery(string fieldName, string searchValue, bool andOperator)
    {
      return QueryBuilder.GetMatchQuery(fieldName, searchValue, andOperator);
    }

    /// <summary>
    /// Получить строку поиска по соответствию фразы шаблону.
    /// </summary>
    /// <param name="fieldName">Имя поля.</param>
    /// <param name="fieldValue">Значение поля.</param>
    /// <returns>Строка поиска.</returns>
    [Public]
    public virtual string GetWildcardQuery(string fieldName, string fieldValue)
    {
      return QueryBuilder.GetWildcardQuery(fieldName, fieldValue);
    }

    /// <summary>
    /// Получить строку поиска по логическому условию.
    /// </summary>
    /// <param name="must">Json-строка поиска must (условия соединяются через логическое И).</param>
    /// <param name="should">Json-строка поиска should (условия соединяются через логическое ИЛИ).</param>
    /// <param name="filter">Json-строка фильтрации (найденные значения исключаются при оценке поиска).</param>
    /// <returns>Строка поиска.</returns>
    [Public]
    public virtual string GetBoolQuery(string must, string should, string filter)
    {
      return QueryBuilder.GetBoolQuery(must, should, filter);
    }
    
    #endregion

    #region Поиск сущностей

    /// <summary>
    /// Выполнить поиск по списку полей факта.
    /// </summary>
    /// <param name="factQueryData">Данные для поиска.</param>
    /// <returns>Результаты поиска.</returns>
    /// <remarks>Алгоритм:
    /// Если в результате поиска по текущему полю не найдено ни одной сущности, перейти к следующему полю.
    /// Если найдена единственная сущность, завершить поиск, вернуть ИД сущности.
    /// Если найдено несколько сущностей, перейти к следующему полю, добавив в условие фильтрации выражение для выполненного поиска.
    /// </remarks>
    [Public]
    public virtual IArioFactElasticsearchData ExecuteElasticsearchQuery(IArioFactElasticsearchData factQueryData)
    {
      factQueryData.EntityIds = new List<long>();
      factQueryData.FoundedFields = new List<IArioFactField>();
      
      // Имя сущности обязательно.
      if (string.IsNullOrWhiteSpace(factQueryData.EntityName))
      {
        Logger.Error("ExecuteElasticsearchQuery. Entity name not found.");
        return factQueryData;
      }

      try
      {
        var filter = string.Empty;
        foreach (var query in factQueryData.Queries)
        {
          // По умолчанию запрос должен возвращать хотя бы одну запись.
          query.ScoredResultsLimit = factQueryData.ScoredResultsLimit != 0 ?
            factQueryData.ScoredResultsLimit :
            Constants.Module.ElasticsearchScore.DefaultResultsLimit;
          
          if (!string.IsNullOrEmpty(filter))
            query.RefineSearchFilter = filter;
          
          if (string.IsNullOrEmpty(query.EntityName))
            query.EntityName = factQueryData.EntityName;
          
          // Запросы, помеченные "только для уточнения", выполнять только если заполнен фильтр.
          if (query.IsRefineSearchOnly && string.IsNullOrEmpty(query.RefineSearchFilter))
            continue;
          
          // Выполнить поиск по полю.
          var fieldResult = this.ExecuteElasticsearchQuery(query);
          
          // Если сущности не найдены, перейти к следующему полю.
          if (fieldResult.EntityCount == 0)
            continue;
          
          // Если найдены сущности с оценкой выше лимита.
          if (fieldResult.EntityIds != null)
          {
            factQueryData.EntityIds = fieldResult.EntityIds;
            
            // Если найдена единственная сущность, завершить поиск.
            if (fieldResult.EntityIds.Count == 1)
            {
              if (query.ArioField != null)
                factQueryData.FoundedFields.Add(query.ArioField);
              break;
            }
          }
          
          // Если найдено несколько сущностей, задать новое условие фильтрации.
          filter = fieldResult.RefineSearchFilter;
        }
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("ExecuteElasticsearchQuery. Error while searching entity {0} by ario fact {1}",
                           ex, factQueryData.EntityName, factQueryData.Fact.Id);
      }
      
      return factQueryData;
    }
    
    /// <summary>
    /// Выполнить поиск по значению поля индекса.
    /// </summary>
    /// <param name="queryData">Данные для поиска.</param>
    /// <returns>Результаты поиска.</returns>
    /// <remarks>Метод осуществляет поиск сущности в индексе по значению указанного поля Ario.
    /// Исходные данные для поиска фильтруются по готовому условию elasticsearch, если оно указано.
    /// При использовании нечеткого поиска рассчитывается лимит оценки.
    /// Для этого найденная сущность повторно ищется нечетким поиском, но уже по исходному значению поля в индексе.
    /// Оценка такого поиска считается максимально возможной для заданных критериев поиска.
    /// Для расчета лимита эта оценка умножается на переданный в параметрах процент.
    /// Если первоначальная оценка поиска выше или равна лимиту, возвращается ИД найденной записи.
    /// </remarks>
    [Public]
    public virtual IArioFieldElasticsearchData ExecuteElasticsearchQuery(IArioFieldElasticsearchData queryData)
    {
      // Максимальное число записей, возвращаемое Elasticsearch.
      const int MaxResultSize = 10000;
      try
      {
        if (string.IsNullOrWhiteSpace(queryData.SearchValue))
        {
          if (queryData.ArioField != null && !string.IsNullOrEmpty(queryData.ArioField.Value))
            queryData.SearchValue = queryData.ArioField.Value;
          else
            throw AppliedCodeException.Create("ExecuteElasticsearchQuery. Search value is empty.");
        }
        
        // Сформировать условие фильтра с учетом статуса сущностей.
        var statusFilter = string.Empty;
        if (!queryData.IsClosedEntitySearch)
          statusFilter = QueryBuilder.GetTermQuery("Status", CoreEntities.DatabookEntry.Status.Active.Value);

        var filterQueryPart = queryData.RefineSearchFilter;
        if (!string.IsNullOrEmpty(statusFilter))
        {
          if (!string.IsNullOrEmpty(filterQueryPart))
            filterQueryPart = string.Format("{0},{1}", filterQueryPart, statusFilter);
          else
            filterQueryPart = statusFilter;
        }
        
        /* Функция возвращает либо список идентификаторов сущностей с оценкой выше лимита,
         * либо, если таких сущностей найдено более заданного количества, функция возвращает условие фильтрации
         * для последующего уточнения по другим полям. Таким образом, для анализа числа найденных
         * сущностей с оценкой выше лимита достаточно получить на одну сущность больше заданного количества,
         * независимо от их общего количества, удовлетворяющего условиям поиска.
         */
        var indexName = this.GetIndexName(queryData.EntityName);
        var searchType = queryData.SearchType;
        var resultSize = queryData.ScoredResultsLimit + 1;
        if (queryData.SearchType == Constants.Module.ElasticsearchType.MatchExact)
        {
          searchType = Constants.Module.ElasticsearchType.MatchPhrase;
          resultSize = MaxResultSize;
        }
        var mustQueryPart = QueryBuilder.GetQueryByType(searchType, queryData.ElasticFieldName, queryData.SearchValue);
        var shouldQueryPart = string.Empty;
        var query = QueryBuilder.GetBoolQuery(mustQueryPart, shouldQueryPart, filterQueryPart);
        
        var connector = this.GetElasticsearchConnector();
        var searchResult = connector.Search(indexName, query, resultSize);
        
        queryData.EntityCount = searchResult.Total;
        if (queryData.EntityCount == 0 || !searchResult.Hits.Any())
          return queryData;
        
        // Отсортировать результаты в порядке убывания оценки. Если найдено несколько записей с одинаковой оценкой,
        // приоритетнее значения, совпадающие или близкие по размеру к искомой строке. Например, если искали по вхождению
        // слово "барс". То результаты отсортируются в порядке "барс", "барсук", "барсетка".
        var firstHit = searchResult.Hits
          .OrderByDescending(x => x.Score)
          .ThenBy(x => Math.Abs(x.GetFieldValue(queryData.ElasticFieldName).Length - queryData.SearchValue.Length))
          .First();
        
        // Если сущность найдена нечетким поиском, рассчитать и применить лимит оценки.
        var scoreLimit = queryData.ScoreMinLimit;
        if (!queryData.IsRefineSearchOnly &&
            firstHit.Score >= scoreLimit && queryData.ScoreLimitPercent > 0 &&
            (searchType == Constants.Module.ElasticsearchType.FuzzyAnd ||
             searchType == Constants.Module.ElasticsearchType.FuzzyOr))
        {
          var sourceFieldValue = firstHit.GetFieldValue(queryData.ElasticFieldName);
          if (string.Equals(queryData.SearchValue, sourceFieldValue, StringComparison.CurrentCultureIgnoreCase))
            scoreLimit = firstHit.Score;
          else
          {
            // Выполнить повторный нечеткий поиск, но уже по значению найденного поля.
            var searchLimitMust = QueryBuilder.GetQueryByType(searchType, queryData.ElasticFieldName, sourceFieldValue);
            var searchLimitQuery = QueryBuilder.GetBoolQuery(searchLimitMust, shouldQueryPart, filterQueryPart);
            var searchLimitResult = connector.Search(indexName, searchLimitQuery, 1);
            scoreLimit = searchLimitResult.Hits.First().Score;
          }
          scoreLimit = Math.Round(scoreLimit * queryData.ScoreLimitPercent / 100, 4);
        }
        
        if (queryData.SearchType == Constants.Module.ElasticsearchType.MatchExact)
        {
          // В случае поиска по вхождению фразы, применить более жесткие условия - текст должен быть идентичный.
          var foundExactIds = searchResult.Hits
            .Where(x => string.Equals(x.GetFieldValue(queryData.ElasticFieldName),
                                      queryData.SearchValue,
                                      StringComparison.CurrentCultureIgnoreCase))
            .Select(x => x.GetEntityId())
            .ToList();
          queryData.EntityCount = foundExactIds.Count;
          if (queryData.EntityCount > 0)
            queryData.EntityIds = foundExactIds;
        }
        else
        {
          // Вернуть найденные сущности, если их число не превышает лимит.
          queryData.EntityCount = searchResult.Hits.Count(x => x.Score >= scoreLimit);
          if (queryData.EntityCount > 0 && queryData.EntityCount <= queryData.ScoredResultsLimit)
            queryData.EntityIds = searchResult.Hits
              .Where(x => x.Score >= scoreLimit)
              .OrderByDescending(x => x.Score)
              .ThenBy(x => Math.Abs(x.GetFieldValue(queryData.ElasticFieldName).Length - queryData.SearchValue.Length))
              .Select(x => x.GetEntityId())
              .ToList();
        }
        
        // Если записи с оценкой выше лимита не найдены, завершить поиск, не изменяя уточняющий фильтр.
        if (queryData.EntityCount == 0)
          return queryData;
        
        // Если найдена единственная запись выше лимита, вернуть ее без последующих уточнений.
        if (queryData.EntityIds != null && queryData.EntityIds.Count == 1)
          return queryData;

        // Если количество найденных записей более одной, добавить в уточняющий фильтр их ИД
        // (если число не привышает лимит), либо условие поиска по текущему полю.
        var refineSearchFilter = queryData.EntityIds != null ?
          this.GetTermsQuery("Id", queryData.EntityIds.Select(x => x.ToString()).ToList()) :
          mustQueryPart;

        if (!string.IsNullOrEmpty(queryData.RefineSearchFilter))
          queryData.RefineSearchFilter += "," + refineSearchFilter;
        else
          queryData.RefineSearchFilter = refineSearchFilter;
      }
      catch (Exception ex)
      {
        throw AppliedCodeException.Create("ExecuteElasticsearchQuery. An error occurred while executing Elasticsearch request.", ex);
      }
      
      return queryData;
    }
    
    /// <summary>
    /// Выполнить поиск.
    /// </summary>
    /// <param name="entityName">Имя сущности.</param>
    /// <param name="query">Данные для поиска, json-строка в формате Elasticsearch.</param>
    /// <returns>Список ИД найденных записей.</returns>
    /// <remarks>Метод для точного поиска и поиска по вхождению.</remarks>
    [Public]
    public virtual List<long> ExecuteElasticsearchQuery(string entityName, string query)
    {
      // Все результаты считать достоверными, лимит оценки поиска не использовать.
      var minScore = 0d;
      return this.ExecuteElasticsearchQuery(entityName, query, minScore);
    }

    /// <summary>
    /// Выполнить поиск.
    /// </summary>
    /// <param name="entityName">Имя сущности.</param>
    /// <param name="query">Данные для поиска, json-строка в формате Elasticsearch.</param>
    /// <param name="minScore">Оценка, ниже которой результаты поиска недостоверны.</param>
    /// <returns>Список ИД найденных записей.</returns>
    /// <remarks>Метод для нечеткого поиска.</remarks>
    [Public]
    public virtual List<long> ExecuteElasticsearchQuery(string entityName, string query, double minScore)
    {
      var entityIds = new List<long>();
      try
      {
        var connector = this.GetElasticsearchConnector();
        var searchResult = connector.Search(this.GetIndexName(entityName), query);
        
        // Оставить результаты, у которых оценка поиска выше указанной.
        if (searchResult.Total > 0)
        {
          var firstHit = searchResult.Hits.First();
          entityIds.AddRange(searchResult.Hits.Where(x => x.Score >= minScore).Select(x => x.GetEntityId()));
        }
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("ExecuteElasticsearchQuery. Error while searching entity {0} by query {1}.", ex, entityName, query);
      }
      return entityIds;
    }
    
    /// <summary>
    /// Создать запрос для поиска по полю.
    /// </summary>
    /// <param name="arioField">Поле Ario.</param>
    /// <param name="elasticFieldName">Имя поля в индексе.</param>
    /// <param name="searchType">Тип поиска.</param>
    /// <returns>Данные для поиска.</returns>
    [Public]
    public virtual IArioFieldElasticsearchData CreateSearchFieldQuery(IArioFactField arioField, string elasticFieldName, string searchType)
    {
      return this.CreateSearchFieldQuery(arioField, elasticFieldName, searchType, arioField?.Value);
    }

    /// <summary>
    /// Создать запрос для поиска по полю.
    /// </summary>
    /// <param name="arioField">Поле Ario.</param>
    /// <param name="elasticFieldName">Имя поля в индексе.</param>
    /// <param name="searchType">Тип поиска.</param>
    /// <param name="searchValue">Искомое значение.</param>
    /// <returns>Данные для поиска.</returns>
    [Public]
    public virtual IArioFieldElasticsearchData CreateSearchFieldQuery(IArioFactField arioField, string elasticFieldName, string searchType, string searchValue)
    {
      return this.CreateSearchFieldQuery(arioField, elasticFieldName, searchType, searchValue, false, false);
    }

    /// <summary>
    /// Создать запрос для поиска по полю.
    /// </summary>
    /// <param name="searchValue">Значение для поиска.</param>
    /// <param name="elasticFieldName">Имя поля в индексе.</param>
    /// <param name="searchType">Тип поиска.</param>
    /// <param name="isRefineSearchOnly">Выполнять запрос только для уточнения.</param>
    /// <returns>Данные для поиска.</returns>
    [Public]
    public virtual IArioFieldElasticsearchData CreateSearchFieldQuery(string searchValue, string elasticFieldName, string searchType, bool isRefineSearchOnly)
    {
      return this.CreateSearchFieldQuery(ArioFactField.Create(), elasticFieldName, searchType, searchValue, isRefineSearchOnly, false);
    }
    
    /// <summary>
    /// Создать запрос для поиска по полю.
    /// </summary>
    /// <param name="arioField">Поле Ario.</param>
    /// <param name="elasticFieldName">Имя поля в индексе.</param>
    /// <param name="searchType">Тип поиска.</param>
    /// <param name="isRefineSearchOnly">Выполнять запрос только для уточнения.</param>
    /// <returns>Данные для поиска.</returns>
    [Public]
    public virtual IArioFieldElasticsearchData CreateSearchFieldQuery(IArioFactField arioField, string elasticFieldName, string searchType, bool isRefineSearchOnly)
    {
      return this.CreateSearchFieldQuery(arioField, elasticFieldName, searchType, arioField?.Value, isRefineSearchOnly, false);
    }

    /// <summary>
    /// Создать запрос для поиска по полю.
    /// </summary>
    /// <param name="arioField">Поле Ario.</param>
    /// <param name="elasticFieldName">Имя поля в индексе.</param>
    /// <param name="searchType">Тип поиска.</param>
    /// <param name="searchValue">Искомое значение.</param>
    /// <param name="isRefineSearchOnly">Выполнять запрос только для уточнения.</param>
    /// <param name="isClosedEntitySearch">Выполнять поиск закрытых записей.</param>
    /// <returns>Данные для поиска.</returns>
    [Public]
    public virtual IArioFieldElasticsearchData CreateSearchFieldQuery(IArioFactField arioField, string elasticFieldName, string searchType,
                                                                      string searchValue, bool isRefineSearchOnly, bool isClosedEntitySearch)
    {
      var queryData = ArioFieldElasticsearchData.Create();
      queryData.ArioField = arioField;
      queryData.ElasticFieldName = elasticFieldName;
      queryData.SearchType = searchType;
      queryData.SearchValue = searchValue;
      queryData.IsRefineSearchOnly = isRefineSearchOnly;
      queryData.IsClosedEntitySearch = isClosedEntitySearch;
      queryData.ScoreMinLimit = Constants.Module.ElasticsearchScore.DefaultMinLimit;
      queryData.ScoreLimitPercent = 0;
      return queryData;
    }
    
    #endregion

    #endregion

    #endregion
  }
}