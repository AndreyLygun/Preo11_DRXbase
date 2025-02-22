using System;

namespace Sungero.Commons.Constants
{
  public static class Module
  {
    /// <summary>
    /// Типы поиска Elasticsearch.
    /// </summary>
    public static class ElasticsearchType
    {
      [Sungero.Core.Public]
      public const string Wildcard = "Wildcard";
      [Sungero.Core.Public]
      public const string Term = "Term";
      [Sungero.Core.Public]
      public const string MatchAnd = "MatchAnd";
      [Sungero.Core.Public]
      public const string MatchOr = "MatchOr";
      [Sungero.Core.Public]
      public const string FuzzyAnd = "FuzzyAnd";
      [Sungero.Core.Public]
      public const string FuzzyOr = "FuzzyOr";
      [Sungero.Core.Public]
      public const string MatchPhrase = "MatchPhrase";
      [Sungero.Core.Public]
      public const string MatchExact = "MatchExact";
    }
    
    /// <summary>
    /// Значения оценки Elasticsearch, ниже которой результаты нечеткого поиска недостоверны.
    /// </summary>
    public static class ElasticsearchScore
    {
      // Процент от максимально возможной оценки для расчета лимита.
      [Sungero.Core.Public]
      public const double DefaultLimitPercent = 67;
      
      // Минимальная оценка по умолчанию.
      [Sungero.Core.Public]
      public const double DefaultMinLimit = 0.5;
      
      // Число записей с оценкой выше лимита, которое нужно вернуть.
      [Sungero.Core.Public]
      public const int DefaultResultsLimit = 1;
    }
    
    /// <summary>
    /// Шаблон bulk-запроса к целевому индексу Elasticsearch.
    /// </summary>
    [Sungero.Core.Public]
    public const string BulkOperationIndexToTarget = "{\"index\":{}}";
    
    /// <summary>
    /// Максимальное количество записей, получаемых за 1 запрос.
    /// </summary>
    [Sungero.Core.Public]
    public const int MaxQueryIds = 999;

    /// <summary>
    /// Максимальное количество итераций асинхронных обработчиков индексации и удаления из индекса.
    /// </summary>
    public const int IndexingAsyncsRetryCount = 6;
    
    /// <summary>
    /// Имя ключа в параметрах сущности - признак изменения индексируемых полей и того, что сущность создана и еще не сохранена в БД.
    /// </summary>
    [Sungero.Core.Public]
    public const string IsIndexedEntityInsertedParamKey = "IsIndexedEntityInserted";

    /// <summary>
    /// Шаблон имени индекса Elasticsearch.
    /// </summary>
    public const string IndexNameTemplate = "rxsearch_{0}_{1}_{2}";
    
    /// <summary>
    /// Guid модуля Intelligence.
    /// </summary>
    [Sungero.Core.Public]
    public static readonly Guid IntelligenceGuid = Guid.Parse("e08dc659-2828-4d50-b90d-7d06408ab7cb");

    /// <summary>
    /// Шаблон для поиска спец. символов.
    /// </summary>
    public const string SpecialSymbolsPattern = @"\n|\t|\\|\/|\:|\;|\*|\?|\<|\>|\||\'|\`|\«|\»|\“|\”|\(|\)|\[|\]|\{|\}|\^|\%|\!|_|-|""";
    
    /// <summary>
    /// Шаблон для поиска двойных пробелов.
    /// </summary>
    public const string DoubleSpacePattern = " {2,}";
    
    /// <summary>
    /// Имя параметра "Все индексы созданы".
    /// </summary>
    [Sungero.Core.Public]
    public const string AllIndicesExistParamName = "AllIndicesExist";
    
    /// <summary>
    /// Стандартные синонимы ОПФ для индексов ElasticSearch.
    /// </summary>
    [Sungero.Core.Public]
    public const string LegalFormSynonyms = @"
      Автономная некоммерческая организация,АНО;
      Автономное образовательное учреждение,АОУ;
      Автономное учреждение,АУ;
      Автономное учреждение дополнительного образования,АУДО;
      Автономное учреждение дополнительного образования детей,АУДОД;
      Автономное учреждение дополнительного профессионального образования,АУДПО;
      Автономное учреждение культуры,АУК;
      Автономное учреждение социального обслуживания населения,АУ СОН;
      Агропромышленный холдинг,АПХ;
      Акционерное общество,АО;
      Акционерный коммерческий банк,АКБ;
      Бюджетное профессиональное образовательное учреждение,БПОУ;
      Бюджетное учреждение,БУ;
      Бюджетное учреждение культуры,БУК;
      Бюро технической инвентаризации,БТИ;
      Военный комиссариат,ВК;
      Всероссийский научно-исследовательский институт,ВНИИ;
      Всероссийское общественное движение,ВОД;
      Гаражно-строительный кооператив,ГСК;
      Главное управление внутренних дел,ГУВД;
      Государственная компания,ГК;
      Государственная телерадиокомпания,ГТРК;
      Государственное автономное образовательное учреждение высшего образования,ГАОУ ВО;
      Государственное автономное образовательное учреждение высшего профессионального образования,ГАОУ ВПО;
      Государственное автономное образовательное учреждение дополнительного профессионального образования,ГАОУ ДПО;
      Государственное автономное образовательное учреждение среднего профессионального образования,ГАОУ СПО;
      Государственное автономное профессиональное образовательное учреждение,ГАПОУ;
      Государственное автономное учреждение,ГАУ;
      Государственное автономное учреждение здравоохранения,ГАУЗ;
      Государственное автономное учреждение культуры,ГАУК;
      Государственное бюджетное образовательное учреждение,ГБОУ;
      Государственное бюджетное образовательное учреждение высшего профессионального образования,ГБОУ ВПО;
      Государственное бюджетное образовательное учреждение дополнительного профессионального образования,ГБОУ ДПО;
      Государственное бюджетное образовательное учреждение среднего профессионального образования,ГБОУ СПО;
      Государственное бюджетное профессиональное образовательное учреждение,ГБПОУ;
      Государственное бюджетное учреждение,ГБУ;
      Государственное бюджетное учреждение здравоохранения,ГБУЗ;
      Государственное бюджетное учреждение культуры,ГБУК;
      Государственное военное образовательное учреждение высшего профессионального образования,ГВОУ ВПО;
      Государственное казённое учреждение,ГКУ;
      Государственное казённое учреждение культуры,ГКУК;
      Государственное краевое бюджетное учреждение,ГКБУ;
      Государственное краевое бюджетное учреждение культуры,ГКБУК;
      Государственное лечебно профилактическое учреждение,ГЛПУ;
      государственное научное учреждение,ГНУ;
      государственное областное образовательное учреждение,ГООУ;
      Государственное образовательное бюджетное учреждение культуры,ГОБУК;
      Государственное образовательное учреждение высшего образования,ГОУ ВО;
      Государственное образовательное учреждение высшего профессионального образования,ГОУ ВПО;
      Государственное образовательное учреждение доплнительного профессионального образования,ГОУ ДПО;
      Государственное образовательное учреждение среднего профессионального образования,ГОУ СПО;
      Государственное предприятие,ГП;
      Государственное профессиональное образовательное учреждение,ГПОУ;
      Государственное спортивное учреждение,ГСУ;
      Государственное унитарное предприятие,ГУП;
      Государственное учреждение,ГУ;
      Государственное учреждение здравоохранения,ГУЗ;
      Государственное учреждение культуры,ГУК;
      Дачное некоммерческое товарищество,ДНТ;
      Детская юношеская спортивная школа,ДЮСШ;
      Добровольное общество содействии армии, авиации и флоту,ДОАСААФ;
      Дорожно-патрульная служба,ДПС;
      Дочернее открытое акционерное общество,ДОАО;
      Жилищностроительный кооператив,ЖСК;
      Закрытое акционерное общество,ЗАО;
      Инвестиционный коммерческий банк развития,ИКБР;
      Индивидуальный предприниматель,ИП;
      Инспекция Федеральной налоговой службы России,ИФНС;
      Информационный центр,ИЦ;
      Коммерческий банк,КБ;
      Коммерческий инвестиционный банк ,КИБ;
      Комплексный центр социального обслуживания населения,КЦСОН;
      Краевое государственное автономное профессиональное образовательное учреждение,КГА ПОУ;
      Краевое государственное автономное учреждение,КГАУ;
      Краевое государственное автономное учреждение культуры,КГАУК ;
      Краевое государственное бюджетное научное учреждение культуры,КГБНУК;
      Краевое государственное бюджетное учреждение,КГБУ;
      Краевое государственное казённое учреждение,КГКУ;
      Краевое государственное учреждение,КГУ;
      Крестьянское (фермерское) хозяйство,КФХ;
      Межмуниципальный отдел Министерства внутренних дел,МО МВД;
      Межотраслевой Коммерческий Банк,МБК;
      Межрегиональная общественная организация,МОО;
      Министерство внутренних дел,МВД;
      Муниципальное автономное дошкольное образовательное учреждение,МАДОУ;
      Муниципальное автономное образовательное учреждение дополнительного образования детей,МАОУДОД;
      Муниципальное автономное общеобразовательное учреждение,МАОУ;
      Муниципальное автономное учреждение,МАУ;
      Муниципальное автономное учреждение культуры,МАУК;
      Муниципальное бюджетное общеобразовательное учреждение,МБОУ;
      Муниципальное бюджетное учреждение,МБУ;
      Муниципальное бюджетное учреждение культуры,МБУК;
      Муниципальное дошкольное образование,МДОУ;
      Муниципальное казённое учреждение,МКУ;
      Муниципальное казённое учреждение культуры,МКУК ;
      Муниципальное медицинское лечебно-профилактические учреждение,ММЛПУ;
      Муниципальное образовательное учреждение,МОУ;
      Муниципальное унитарное предприятие,МУП;
      Муниципальное учреждение,МУ;
      Муниципальное учреждение здравоохранения,МУЗ;
      Муниципальное учреждение культуры,МУК ;
      Научно-исследовательский институт,НИИ;
      Научно-производственное объединение,НПО;
      Научно-производственное предприятие,НПП;
      Научно-технический центр,НТЦ;
      Негосударственное образовательное учреждение высшего профессионального образования,НОУ ВПО;
      Негосударственное образовательное учреждение дополнительного профессионального образования,НОУ ДПО;
      Негосударственное образовательное учреждение среднего профессионального образования,НОУ СПО;
      Негосударственное образовательное частное учреждение,НОЧУ;
      Негосударственное учреждение здравоохранения,НУЗ ;
      Некоммерческая организация,НО;
      Некоммерческое партнерство,НП;
      Неправительственная некоммерческая организация,ННО;
      Нефтегазодобывающее управление,НГДУ;
      Областное бюджетное учреждение культуры,ОБУК;
      Областное государственное автономное учреждение,ОГАУ;
      Областное государственное автономное учреждение культуры,ОГАУК;
      Областное государственное бюджетное учреждение,ОГБУ;
      Областное государственное бюджетное учреждение культуры,ОГБУК;
      Областное государственное казённое учреждение,ОГКУ;
      Областное государственное казённое учреждение культуры,ОГКУК ;
      Областное государственное унитарное предприятие,ОГУП;
      Областное государственное учреждение,ОГУ;
      Общероссийская физкультурно-спортивная общественная организация,ОФСОО;
      Общество с ограниченной ответственностью,ООО;
      Отдел внутренних дел,ОВД;
      Отдел социальной защиты населения,ОСЗН;
      Открытое акционерное общество,ОАО;
      Платежная небанковская кредитная организация,ПНКО;
      Полевое учреждение,ПУ;
      Производственный кооператив,ПК;
      Публичное акционерное общество,ПАО;
      Районный отдел судебных приставов,РОСП;
      Расчетно-кассовый центр,РКЦ;
      Региональная общественная организация,РОО;
      Рекламно-информационное агентство,РИА;
      Российский государственный университет,РГУ;
      Садовое некоммерческое товарищество,СНТ;
      Саморегулируемая организация,СРО;
      Сельскохозяйственный производственный кооператив,СХПК;
      Средняя общеобразовательная школа,СОШ;
      Территориальное общественное самоуправление,ТОС;
      Территориально-производственный комплекс,ТПК;
      Товарищество с ограниченной ответственностью,ТОО;
      Товарищество собственников жилья,ТСЖ;
      Товарищество собственников недвижимости,ТСН;
      Управление вневедомственной охраны,УВО;
      Управление внутренних дел,УВД;
      Управление министерства внутренних дел,УМВД;
      Управление социальной защиты населения,УСЗН;
      Управление Федеральной миграционной службы,УФМС;
      Управление Федеральной налоговой службы,УФНС;
      Управление Федеральной почтовой связи,УФПС;
      Управление Федеральной службы исполнения наказаний,УФСИН;
      Управление Федеральной службы судебных приставов,УФССП;
      Управляющая компания,УК;
      Федеральная налоговая служба,ФНС;
      Федеральное автономное учреждение,ФАУ;
      Федеральное бюджетное учреждение,ФБУ;
      Федеральное бюджетное учреждение здравоохранения,ФБУЗ;
      Федеральное бюджетное учреждение науки,ФБУН;
      Федеральное государственное автономное образовательное учреждение высшего образования,ФГАОУ ВО;
      Федеральное государственное автономное образовательное учреждение высшего профессионального образования,ФГАОУ ВПО;
      Федеральное государственное автономное образовательное учреждение дополнительного профессионального образования,ФГАОУ ДПО;
      Федеральное государственное автономное образовательное учреждение среднего профессионального образования,ФГАОУ СПО;
      Федеральное государственное бюджетное военное образовательное учреждение высшего образования,ФГБВОУ ВО;
      Федеральное государственное бюджетное научное учреждение,ФГБНУ;
      Федеральное государственное бюджетное научно-исследовательское учреждение,ФГБНИУ;
      Федеральное государственное бюджетное образовательное учреждение,ФГБОУ;
      Федеральное государственное бюджетное образовательное учреждение высшего профессионального образования,ФГБОУ ВПО;
      Федеральное государственное бюджетное образовательное учреждение дополнительного профессионального образования,ФГБОУ ДПО;
      Федеральное государственное бюджетное образовательное учреждение среднего профессионального образования,ФГБОУ СПО;
      Федеральное государственное бюджетное учреждение,ФГБУ;
      Федеральное государственное бюджетное учреждение культуры,ФГБУК;
      Федеральное государственное бюджетное учреждение науки,ФГБУН;
      Федеральное государственное военное образовательное учреждение высшего профессионального образования,ФГВОУ ВПО;
      Федеральное государственное казённое военное образовательное учреждение,ФГКВОУ;
      Федеральное государственное казённое образовательное учреждение,ФГКОУ;
      Федеральное государственное казённое учреждение,ФГКУ;
      Федеральное государственное образовательное учреждение,ФГОУ;
      Федеральное государственное образовательное учреждение высшего профессионального образования,ФГОУ ВПО;
      Федеральное государственное образовательное учреждение дополнительного профессионального образования,ФГОУ ДПО;
      Федеральное государственное образовательное учреждение среднего профессионального образования,ФГОУ СПО;
      Федеральное государственное унитарное предприятие,ФГУП;
      Федеральное государственное учреждение,ФГУ;
      Федеральное государственное учреждение здравоохранения,ФГУЗ;
      Федеральное государственное учреждение культуры,ФГУК;
      Федеральное казённое предприятие,ФКП;
      Федеральное казённое учреждение,ФКУ;
      Федеральное казённое учреждение культуры,ФКУК;
      Фонд социального страхования,ФСС;
      Центр занятости населения,ЦЗН;
      Центр социальной помощи населению,ЦСПН;
      Частная охранная организация,ЧОО;
      Частное образовательное учреждение дополнительного профессионального образования,ЧОУ ДПО;";
    
    /// <summary>
    /// Константы инициализации модуля.
    /// </summary>
    public static class Init
    {
      public static class Commons
      {
        /// <summary>
        /// Название параметра для хранения проинициализированной версии кода модуля.
        /// </summary>
        public const string Name = "InitCommonsUpdate";
        
        public const string FirstInitVersion = "4.8.0.0";
        public const string Version49 = "4.9.0.0";
      }
    }
  }
}
