using System;
using Sungero.Core;

namespace Sungero.SmartProcessing.Constants
{
  public static class Module
  {
    // Наименования для тела письма с электронной почты.
    public static class DcsMailBodyName
    {
      [Sungero.Core.Public]
      public const string Html = "body.html";
      
      [Sungero.Core.Public]
      public const string Txt = "body.txt";
    }
    
    /// <summary>
    /// Наименования фактов и полей фактов в правилах извлечения фактов Ario.
    /// </summary>
    /// <remarks>Составлен для версии Ario 1.7.</remarks>
    public static class ArioGrammars
    {
      /// <summary>
      /// Факт "Письмо".
      /// </summary>
      public static class LetterFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "Letter";
        
        /// <summary>
        /// Адресат письма.
        /// </summary>
        /// <remarks>Содержит информацию в формате "Фамилия И.О." или "Фамилия Имя Отчество".</remarks>
        [Sungero.Core.Public]
        public const string AddresseeField = "Addressee";
        
        /// <summary>
        /// Гриф доступа.
        /// </summary>
        /// <remarks>Гриф "Конфиденциально", "Для служебного пользования", "Коммерческая тайна".</remarks>
        [Sungero.Core.Public]
        public const string ConfidentialField = "Confidential";
        
        /// <summary>
        /// Организационно-правовая форма корреспондента.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrespondentLegalFormField = "CorrespondentLegalForm";
        
        /// <summary>
        /// Наименование корреспондента.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrespondentNameField = "CorrespondentName";

        /// <summary>
        /// Дата документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string DateField = "Date";
        
        /// <summary>
        /// Номер документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string NumberField = "Number";
        
        /// <summary>
        /// В ответ на дату документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string ResponseToDateField = "ResponseToDate";
        
        /// <summary>
        /// В ответ на номер документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string ResponseToNumberField = "ResponseToNumber";
        
        /// <summary>
        /// Тема документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string SubjectField = "Subject";
        
        /// <summary>
        /// ИНН.
        /// </summary>
        [Sungero.Core.Public]
        public const string TinField = "TIN";
        
        /// <summary>
        /// Признак "ИНН корректен".
        /// </summary>
        [Sungero.Core.Public]
        public const string TinIsValidField = "TinIsValid";
        
        /// <summary>
        /// КПП.
        /// </summary>
        [Sungero.Core.Public]
        public const string TrrcField = "TRRC";

        /// <summary>
        /// ОГРН.
        /// </summary>
        [Sungero.Core.Public]
        public const string PsrnField = "PSRN";

        /// <summary>
        /// Головная организация.
        /// </summary>
        [Sungero.Core.Public]
        public const string HeadCompanyNameField = "CorrHeadCompanyName";
        
        /// <summary>
        /// Адрес эл. почты.
        /// </summary>
        [Sungero.Core.Public]
        public const string EmailField = "Email";

        /// <summary>
        /// Номер телефона.
        /// </summary>
        [Sungero.Core.Public]
        public const string PhoneField = "Phone";

        /// <summary>
        /// Веб-сайт.
        /// </summary>
        [Sungero.Core.Public]
        public const string WebsiteField = "Website";

        /// <summary>
        /// Тип корреспондента.
        /// </summary>
        [Sungero.Core.Public]
        public const string TypeField = "Type";

        /// <summary>
        /// Типы корреспондентов: "Корреспондент", "Адресат".
        /// </summary>
        public static class CorrespondentTypes
        {
          [Sungero.Core.Public]
          public const string Correspondent = "CORRESPONDENT";
          
          [Sungero.Core.Public]
          public const string Recipient = "RECIPIENT";
        }

      }
      
      /// <summary>
      /// Факт "Персона письма".
      /// </summary>
      public static class LetterPersonFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "LetterPerson";
        
        /// <summary>
        /// Имя.
        /// </summary>
        [Sungero.Core.Public]
        public const string NameField = "Name";
        
        /// <summary>
        /// Отчество.
        /// </summary>
        [Sungero.Core.Public]
        public const string PatrnField = "Patrn";
        
        /// <summary>
        /// Фамилия.
        /// </summary>
        [Sungero.Core.Public]
        public const string SurnameField = "Surname";
        
        /// <summary>
        /// Тип персоны.
        /// </summary>
        [Sungero.Core.Public]
        public const string TypeField = "Type";
        
        /// <summary>
        /// Типы персоны: "Подписант", "Исполнитель".
        /// </summary>
        public static class PersonTypes
        {
          [Sungero.Core.Public]
          public const string Signatory = "SIGNATORY";
          
          [Sungero.Core.Public]
          public const string Responsible = "RESPONSIBLE";
        }
      }
      
      /// <summary>
      /// Факт "Контрагент".
      /// </summary>
      public static class CounterpartyFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "Counterparty";
        
        /// <summary>
        /// Расчетный счет.
        /// </summary>
        [Sungero.Core.Public]
        public const string BankAccountField = "BankAccount";
        
        /// <summary>
        /// БИК.
        /// </summary>
        [Sungero.Core.Public]
        public const string BinField = "BIN";
        
        /// <summary>
        /// Тип контрагента.
        /// </summary>
        [Sungero.Core.Public]
        public const string CounterpartyTypeField = "CounterpartyType";
        
        /// <summary>
        /// Типы контрагента.
        /// </summary>
        public static class CounterpartyTypes
        {
          [Sungero.Core.Public]
          public const string Consignee = "CONSIGNEE";
          
          [Sungero.Core.Public]
          public const string Payer = "PAYER";
          
          [Sungero.Core.Public]
          public const string Shipper = "SHIPPER";
          
          [Sungero.Core.Public]
          public const string Supplier = "SUPPLIER";
          
          [Sungero.Core.Public]
          public const string Buyer = "BUYER";
          
          [Sungero.Core.Public]
          public const string Seller = "SELLER";
        }
        
        /// <summary>
        /// Организационно-правовая форма.
        /// </summary>
        [Sungero.Core.Public]
        public const string LegalFormField = "LegalForm";
        
        /// <summary>
        /// Наименование.
        /// </summary>
        [Sungero.Core.Public]
        public const string NameField = "Name";
        
        /// <summary>
        /// Полное ФИО подписанта документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string SignatoryField = "Signatory";

        /// <summary>
        /// Имя подписанта документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string SignatoryNameField = "SignatoryName";
        
        /// <summary>
        /// Отчество подписанта документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string SignatoryPatrnField = "SignatoryPatrn";
        
        /// <summary>
        /// Фамилия подписанта документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string SignatorySurnameField = "SignatorySurname";
        
        /// <summary>
        /// ИНН.
        /// </summary>
        [Sungero.Core.Public]
        public const string TinField = "TIN";
        
        /// <summary>
        /// Признак "ИНН корректен".
        /// </summary>
        [Sungero.Core.Public]
        public const string TinIsValidField = "TinIsValid";
        
        /// <summary>
        /// КПП.
        /// </summary>
        [Sungero.Core.Public]
        public const string TrrcField = "TRRC";
        
        /// <summary>
        /// ОГРН.
        /// </summary>
        [Sungero.Core.Public]
        public const string PsrnField = "PSRN";
      }
      
      /// <summary>
      /// Факт "Документ".
      /// </summary>
      /// <remarks>Используется в договорах, актах.</remarks>
      public static class DocumentFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "Document";
        
        /// <summary>
        /// Дата документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string DateField = "Date";
        
        /// <summary>
        /// Номер документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string NumberField = "Number";
      }
      
      /// <summary>
      /// Факт "Дополнительное соглашение".
      /// </summary>
      public static class SupAgreementFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "SupAgreement";
        
        /// <summary>
        /// Номер ведущего документа (договора).
        /// </summary>
        [Sungero.Core.Public]
        public const string DocumentBaseNumberField = "DocumentBaseNumber";
        
        /// <summary>
        /// Дата ведущего документа (договора).
        /// </summary>
        [Sungero.Core.Public]
        public const string DocumentBaseDateField = "DocumentBaseDate";
        
        /// <summary>
        /// Дата.
        /// </summary>
        [Sungero.Core.Public]
        public const string DateField = "Date";
        
        /// <summary>
        /// Номер.
        /// </summary>
        [Sungero.Core.Public]
        public const string NumberField = "Number";
      }
      
      /// <summary>
      /// Факт "Сумма документа".
      /// </summary>
      public static class DocumentAmountFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "DocumentAmount";
        
        /// <summary>
        /// Целая часть.
        /// </summary>
        [Sungero.Core.Public]
        public const string AmountField = "Amount";
        
        /// <summary>
        /// Дробная часть.
        /// </summary>
        [Sungero.Core.Public]
        public const string AmountCentsField = "AmountCents";
        
        /// <summary>
        /// Валюта.
        /// </summary>
        [Sungero.Core.Public]
        public const string CurrencyField = "Currency";
        
        /// <summary>
        /// Целая часть суммы НДС.
        /// </summary>
        [Sungero.Core.Public]
        public const string VatAmountField = "VatAmount";
        
        /// <summary>
        /// Дробная часть суммы НДС.
        /// </summary>
        [Sungero.Core.Public]
        public const string VatAmountCentsField = "VatAmountCents";
      }
      
      /// <summary>
      /// Факт "Финансовый документ".
      /// </summary>
      public static class FinancialDocumentFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "FinancialDocument";
        
        /// <summary>
        /// Наименование ведущего документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string DocumentBaseNameField = "DocumentBaseName";
        
        /// <summary>
        /// Номер ведущего документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string DocumentBaseNumberField = "DocumentBaseNumber";
        
        /// <summary>
        /// Дата ведущего документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string DocumentBaseDateField = "DocumentBaseDate";
        
        /// <summary>
        /// Дата.
        /// </summary>
        [Sungero.Core.Public]
        public const string DateField = "Date";
        
        /// <summary>
        /// Номер.
        /// </summary>
        [Sungero.Core.Public]
        public const string NumberField = "Number";
        
        /// <summary>
        /// Дата корректируемого документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrectionDateField = "CorrectionDate";
        
        /// <summary>
        /// Номер корректируемого документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrectionNumberField = "CorrectionNumber";
        
        /// <summary>
        /// Номер исправления.
        /// </summary>
        [Sungero.Core.Public]
        public const string RevisionNumberField = "RevisionNumber";
        
        /// <summary>
        /// Дата исправления.
        /// </summary>
        [Sungero.Core.Public]
        public const string RevisionDateField = "RevisionDate";
        
        /// <summary>
        /// Номер исправления корректировки.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrectionRevisionNumberField = "CorrectionRevisionNumber";
        
        /// <summary>
        /// Дата исправления корректировки.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrectionRevisionDateField = "CorrectionRevisionDate";
      }
      
      /// <summary>
      /// Факт "Номенклатура".
      /// </summary>
      public static class GoodsFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "Goods";
        
        /// <summary>
        /// Наименование товара.
        /// </summary>
        [Sungero.Core.Public]
        public const string NameField = "Name";
        
        /// <summary>
        /// Количество (объем) товара.
        /// </summary>
        [Sungero.Core.Public]
        public const string CountField = "Count";
        
        /// <summary>
        /// Наименование, условное обозначение единицы измерения.
        /// </summary>
        [Sungero.Core.Public]
        public const string UnitNameField = "UnitName";
        
        /// <summary>
        /// Цена за единицу измерения.
        /// </summary>
        [Sungero.Core.Public]
        public const string PriceField = "Price";
        
        /// <summary>
        /// Сумма НДС, по товару.
        /// </summary>
        [Sungero.Core.Public]
        public const string VatAmountField = "VatAmount";
        
        /// <summary>
        /// Ставка НДС, по товару.
        /// </summary>
        [Sungero.Core.Public]
        public const string VatRateField = "VatRate";
        
        /// <summary>
        /// Стоимость с НДС, товара.
        /// </summary>
        [Sungero.Core.Public]
        public const string AmountField = "Amount";
      }

      /// <summary>
      /// Факт "Наименование контрагента".
      /// </summary>
      public static class CounterpartyNameFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "CounterpartyName";

        /// <summary>
        /// Наименование контрагента без ОПФ.
        /// </summary>
        [Sungero.Core.Public]
        public const string FieldName = "CounterpartyName";
      }
      
      /// <summary>
      /// Факт "ОПФ контрагента".
      /// </summary>
      public static class CounterpartyLegalFormFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "CounterpartyLegalForm";

        /// <summary>
        /// Организационно-правовая форма.
        /// </summary>
        [Sungero.Core.Public]
        public const string FieldName = "CounterpartyLegalForm";
      }
      
      [Sungero.Core.Public]
      public const string LegalFormAndNameGrammar = "LegalFormAndName";
    }
    
    // Уровни вероятности.
    public static class PropertyProbabilityLevels
    {
      [Sungero.Core.Public]
      public const double Max = 90;

      [Sungero.Core.Public]
      public const double UpperMiddle = 75;
      
      [Sungero.Core.Public]
      public const double Middle = 50;
      
      [Sungero.Core.Public]
      public const double LowerMiddle = 25;
      
      [Sungero.Core.Public]
      public const double Min = 5;
    }
    
    // Html расширение.
    public static class HtmlExtension
    {
      [Sungero.Core.Public]
      public const string WithDot = ".html";
      
      [Sungero.Core.Public]
      public const string WithoutDot = "html";
    }
    
    // Html теги.
    public static class HtmlTags
    {
      [Sungero.Core.Public]
      public const string MaskForSearch = "<html";
      
      [Sungero.Core.Public]
      public const string StartTag = "<html>";
      
      [Sungero.Core.Public]
      public const string EndTag = "</html>";
    }
    
    /// <summary>
    /// Статусы задачи на обработку файла.
    /// </summary>
    /// <remarks>Значения статусов Task.State: 0 - Новая, 1 - В работе, 2 - Завершена,
    /// 3 - Произошла ошибка, 4 - Обучение завершено, 5 - Прекращена.</remarks>
    public static class ProcessingTaskStates
    {
      // Новая.
      [Sungero.Core.Public]
      public const int New = 0;

      // В работе.
      [Sungero.Core.Public]
      public const int InWork = 1;
      
      // Завершена.
      [Sungero.Core.Public]
      public const int Completed = 2;
      
      // Произошла ошибка.
      [Sungero.Core.Public]
      public const int ErrorOccurred = 3;
      
      // Обучение завершено.
      [Sungero.Core.Public]
      public const int TrainingCompleted = 4;
      
      // Прекращена.
      [Sungero.Core.Public]
      public const int Terminated = 5;
    }
    
    /// <summary>
    /// Минимальное время обработки одного документа в Ario.
    /// </summary>
    public const int ArioDocumentProcessingMinDuration = 20;
    
    /// <summary>
    /// Минимальный промежуток обращения к Ario.
    /// </summary>
    public const int ArioAccessMinPeriod = 5;
    
    /// <summary>
    /// Время ожидания запроса к сервису перекомплектования в секундах.
    /// </summary>
    public const int RepackingRequestTimeout = 600;
    
    /// <summary>
    /// Ответ сервиса интеграции при обращении к закрытой сессии.
    /// </summary>
    public const string RepackingClosedSessionResponse = "SessionClosed";
    
    /// <summary>
    /// Ответ сервиса интеграции при неудачной повторной блокировки документов.
    /// </summary>
    public const string RepackingFailRelockResponse = "FailRelock";
    
    /// <summary>
    /// Период доступности сессии перекомплектования после закрытия без сохранения в секундах.
    /// </summary>
    public const int RepackingTabCloseTimeout = 2;
    
    /// <summary>
    /// Имя параметра "Период доступности сессии перекомплектования после закрытия без сохранения".
    /// </summary>
    public const string RepackingTabCloseTimeoutNameParamName = "RepackingTabCloseTimeout";
    
    /// <summary>
    /// Имя плагина предпросмотра для перекомплектования.
    /// </summary>
    public const string RepackingPreviewPluginName = "Completing";
    
    /// <summary>
    /// Символ номера.
    /// </summary>
    public const char NumberSign = '№';

    /// <summary>
    /// Имя параметра: минимальная F1-мера для публикации модели.
    /// </summary>
    [Sungero.Core.PublicAttribute]
    public const string LowerFMeasureLimitParamName = "LowerFMeasureLimit";
    
    /// <summary>
    /// Минимальная F1-мера для публикации модели.
    /// </summary>
    [Sungero.Core.PublicAttribute]
    public const double DefaultLowerFMeasureLimit = 0.95;
    
    /// <summary>
    /// Имя параметра: минимальная F1-мера для публикации модели классификатора первых страниц.
    /// </summary>
    [Sungero.Core.PublicAttribute]
    public const string FirstPageClassifierLowerFMeasureLimitParamName = "FirstPageClassifierLowerFMeasureLimit";
    
    /// <summary>
    /// Минимальная F1-мера для публикации модели классификатора первых страниц.
    /// </summary>
    [Sungero.Core.PublicAttribute]
    public const double DefaultFirstPageClassifierLowerFMeasureLimit = 0.90;
    
    /// <summary>
    /// Минимальное количество документов для дообучения.
    /// </summary>
    public const int MinDocumentClassifierTrainingCount = 10;
    
    /// <summary>
    /// Максимальное количество итераций асинхронных обработчиков обучения классификаторов.
    /// </summary>
    public const int ClassifierTrainingRetryLimit = 400;
    
    /// <summary>
    /// Максимальный размер CSV-файла в МБ.
    /// </summary>
    [Sungero.Core.Public]
    public const int CsvTrainingDatasetLimit = 100;
    
    /// <summary>
    /// Ключ параметра для максимального размера CSV-файла в МБ.
    /// </summary>
    [Sungero.Core.Public]
    public const string CsvTrainingDatasetLimitKey = "CsvTrainingDatasetLimit";
    
    /// <summary>
    /// Максимальное количество токенов на страницу для дообучения.
    /// </summary>
    public const int DefaultCsvTrainingTokensPerPageLimit = 75;
    
    /// <summary>
    /// Имя параметра для максимального количества токенов на страницу для дообучения.
    /// </summary>
    public const string CsvTrainingTokensPerPageLimitParamName = "CsvTrainingTokensPerPageLimit";
    
    /// <summary>
    /// Имя параметра конфигурационного файла, для включения режима передачи дат в СИ без учета часового пояса сервера.
    /// </summary>
    public const string TenantOffsetInDateTimeParamName = "TENANT_OFFSET_IN_DATETIME";
    
    /// <summary>
    /// Имена классов у классификатора Ario.
    /// </summary>
    public static class ArioClassNames
    {
      /// <summary>
      /// Имя класса первых страниц.
      /// </summary>
      public const string FirstPage = "First";
      
      /// <summary>
      /// Имя класса непервых страниц.
      /// </summary>
      public const string NotFirstPage = "Not First";
    }
    
    /// <summary>
    /// Имена статусов обучения классификаторов.
    /// </summary>
    public static class ClassifierTrainingStatusNames
    {
      /// <summary>
      /// Имя статуса "Ожидание обучения".
      /// </summary>
      public const string Awaiting = "Awaiting";
      
      /// <summary>
      /// Имя статуса "Обучение в процессе".
      /// </summary>
      public const string InProcess = "InProcess";
      
      /// <summary>
      /// Имя статуса "Обучение завершено".
      /// </summary>
      public const string Completed = "Completed";
      
      /// <summary>
      /// Имя статуса "Возникла ошибка".
      /// </summary>
      public const string Error = "Error";
    }
    
    /// <summary>
    /// Поля наименования организации с организацонно-правовой формой.
    /// </summary>
    public static class LegalFormFields
    {
      /// <summary>
      /// Организацонно-правовая форма.
      /// </summary>
      public const string LegalFormField = "LegalForm";
      
      /// <summary>
      /// Наименование организации без организацонно-правовой формы.
      /// </summary>
      public const string NameWithoutLegalFormField = "NameWithoutLegalForm";
    }
    
    /// <summary>
    /// Значение по умолчанию для максимального количества асинхронных задач на извлечение текста в Ario.
    /// </summary>
    public const int DefaultTextExtractionTasksLimit = 20;
    
    /// <summary>
    /// Имя параметра для максимального количества асинхронных задач на извлечение текста в Ario.
    /// </summary>
    public const string TextExtractionTasksLimitParamName = "TextExtractionTasksMaxCount";
    
    /// <summary>
    /// Постфикс логгера интеллектуальной обработки.
    /// </summary>
    [Sungero.Core.Public]
    public const string SmartProcessingLoggerPostfix = "SmartProcessing";
    
    /// <summary>
    /// Константы инициализации модуля.
    /// </summary>
    public static class Init
    {
      public static class SmartProcessing
      {
        /// <summary>
        /// Название параметра для хранения проинициализированной версии кода модуля.
        /// </summary>
        public const string Name = "InitSmartProcessingUpdate";
        
        public const string FirstInitVersion = "4.8.0.0";
        public const string Version49 = "4.9.0.0";
      }
    }
  }
}