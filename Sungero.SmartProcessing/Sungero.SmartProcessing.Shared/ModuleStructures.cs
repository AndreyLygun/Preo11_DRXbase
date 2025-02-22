using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.SmartProcessing.Structures.Module
{
  /// <summary>
  /// Данные для обучения классификатора.
  /// </summary>
  [Public]
  partial class ClassifierTrainingData
  {
    // Класс Ario.
    public string VerifiedClass { get; set; }
    
    // Текстовый слой.
    public string Text { get; set; }
    
    // Результат распознавания.
    public Commons.IEntityRecognitionInfo RecognitionInfo { get; set; }
    
    // Признак, что данные для обучения включены в сессию.
    public bool IncludedInSession { get; set; }
    
    // Порядковый номер.
    public int SerialNumber { get; set; }
  }
  
  /// <summary>
  /// Результаты классификации в Ario.
  /// </summary>
  [Public]
  partial class ArioClassificationResult
  {
    // ИД классификатора.
    public int ClassifierId { get; set; }
    
    // Предсказанный класс.
    public Sungero.SmartProcessing.Structures.Module.IArioClass PredictedClass { get; set; }
    
    // Список результатов по классам.
    public List<Sungero.SmartProcessing.Structures.Module.IArioClass> ClassResults { get; set; }
  }

  /// <summary>
  /// Информация о задаче Ario.
  /// </summary>
  [Public]
  partial class ArioTaskInfo
  {
    // ИД задачи.
    public int? Id { get; set; }

    // Статус задачи (0 - Новая, 1 - В работе, 2 - Завершена, 3 - Произошла ошибка, 4 - Обучение завершено, 5 - Прекращена.
    public int State { get; set; }

    // Результат выполнения задачи.
    public string ResultJson { get; set; }

    // Сообщение об ошибке.
    public string ErrorMessage { get; set; }
    
    // Информация о моделях после обучения классификатора.
    public Sungero.SmartProcessing.Structures.Module.IArioClassifierTrainingModel ClassifierTrainingModel { get; set; }
  }
  
  /// <summary>
  /// Информация о моделях после обучения классификатора.
  /// </summary>
  [Public]
  partial class ArioClassifierTrainingModel
  {
    // ИД классификатора.
    public int ClassifierId { get; set; }
    
    // Дата время публикации модели.
    public DateTime? Published { get; set; }
    
    // ИД опубликованной модели.
    public int? PublishedModelId { get; set; }
    
    // ИД обученной модели.
    public int TrainedModelId { get; set; }
    
    // Время создания модели.
    public DateTime TrainedModelCreated { get; set; }
    
    // Размер набора для обучения.
    public int TrainSetCount { get; set; }

    // F1-мера.
    public double F1Measure { get; set; }
  }

  /// <summary>
  /// Класс классификатора.
  /// </summary>
  [Public]
  partial class ArioClass
  {
    // Имя класса.
    public string Name { get; set; }
    
    // Вероятность класса.
    public double? Probability { get; set; }
  }

  /// <summary>
  /// Пакет результатов обработки документов в Ario.
  /// </summary>
  [Public]
  partial class ArioPackage
  {
    // Результаты обработки документов в Ario.
    public List<Sungero.SmartProcessing.Structures.Module.IArioDocument> Documents { get; set; }
  }
  
  /// <summary>
  /// Распознанный в Ario документ.
  /// </summary>
  [Public]
  partial class ArioDocument
  {
    // Guid pdf версии документа.
    public string BodyGuid { get; set; }
    
    // Тело pdf версии документа из Ario.
    public byte[] BodyFromArio { get; set; }
    
    // Извлеченные из документа факты.
    public List<Sungero.Commons.Structures.Module.IArioFact> Facts { get; set; }
    
    // Извлеченные из документа штампы.
    public List<Sungero.Commons.Structures.Module.IArioStamp> Stamps { get; set; }
    
    // Извлеченные из документа подписи.
    public List<Sungero.Commons.Structures.Module.IArioSignature> Signatures { get; set; }
    
    // Запись в справочнике для сохранения результатов распознавания документа.
    public Sungero.Commons.IEntityRecognitionInfo RecognitionInfo { get; set; }
    
    // Исходный файл.
    public IBlob OriginalBlob { get; set; }
    
    // Признак обработанности документа Арио.
    public bool IsProcessedByArio { get; set; }
    
    // Признак распознанности документа Арио.
    public bool IsRecognized { get; set; }
    
    // Не удалось обработать документ в Ario.
    public bool FailedArioProcessDocument { get; set; }
  }
  
  /// <summary>
  /// Пакет документов.
  /// </summary>
  [Public]
  partial class DocumentPackage
  {
    /// <summary>
    /// Документ.
    /// </summary>
    public List<Sungero.SmartProcessing.Structures.Module.IDocumentInfo> DocumentInfos { get; set; }
    
    /// <summary>
    /// Пакет блобов с информацией о поступивших файлах.
    /// </summary>
    public IBlobPackage BlobPackage { get; set; }
    
    /// <summary>
    /// Ответственный за верификацию пакета документов.
    /// </summary>
    public IEmployee Responsible { get; set; }
    
    // Не удалось упорядочить и связать документы в пакете.
    public bool FailedOrderAndLinkDocuments { get; set; }
    
    // Не удалось создать документ на основе тела письма.
    public bool FailedCreateDocumentFromEmailBody { get; set; }
  }
  
  /// <summary>
  /// Информация о документе.
  /// </summary>
  [Public]
  partial class DocumentInfo
  {
    // Документ.
    public Sungero.Docflow.IOfficialDocument Document { get; set; }
    
    // Распознанный в Ario документ.
    public Sungero.SmartProcessing.Structures.Module.IArioDocument ArioDocument { get; set; }
    
    // Является ли ведущим.
    public bool IsLeadingDocument { get; set; }
    
    // Не удалось зарегистрировать или пронумеровать.
    public bool RegistrationFailed { get; set; }
    
    // Признак распознанности документа Арио.
    public bool IsRecognized { get; set; }
    
    // Является телом письма.
    public bool IsEmailBody { get; set; }
    
    // Найдены по штрихкоду.
    public bool FoundByBarcode { get; set; }
    
    // Не удалось создать версию.
    public bool FailedCreateVersion { get; set; }
    
    // Не удалось создать документ.
    public bool FailedCreateDocument { get; set; }
    
    // Не удалось обработать документ в Ario.
    public bool FailedArioProcessDocument { get; set; }
    
    // Признак использования нечеткого поиска.
    public bool IsFuzzySearchEnabled { get; set; }
  }
  
  /// <summary>
  /// Полное и краткое ФИО персоны.
  /// </summary>
  [Public]
  partial class RecognizedPersonNaming
  {
    // Полное ФИО персоны.
    public string FullName { get; set; }
    
    // Фамилия И.О. персоны.
    public string ShortName { get; set; }
  }
  
  /// <summary>
  /// Результат распознавания валюты.
  /// </summary>
  [Public]
  partial class RecognizedCurrency
  {
    // Валюта.
    public Commons.ICurrency Currency { get; set; }
    
    // Признак - есть значение.
    public bool HasValue { get; set; }
    
    // Вероятность.
    public double? Probability { get; set; }
    
    // Факт
    public Sungero.Commons.Structures.Module.IArioFact Fact { get; set; }
  }

  /// <summary>
  /// Результат распознавания номера документа.
  /// </summary>
  [Public]
  partial class RecognizedDocumentNumber
  {
    public string Number { get; set; }
    
    public double? Probability { get; set; }
    
    public Sungero.Commons.Structures.Module.IArioFact Fact { get; set; }
  }
  
  /// <summary>
  /// Результат распознавания даты документа.
  /// </summary>
  [Public]
  partial class RecognizedDocumentDate
  {
    public DateTime? Date { get; set; }
    
    // Вероятность.
    public double? Probability { get; set; }
    
    public Sungero.Commons.Structures.Module.IArioFact Fact { get; set; }
  }
  
  /// <summary>
  /// Результат распознавания суммы.
  /// </summary>
  [Public]
  partial class RecognizedAmount
  {
    // Сумма.
    public double Amount { get; set; }
    
    // Признак - есть значение.
    public bool HasValue { get; set; }

    // Факт
    public Sungero.Commons.Structures.Module.IArioFact Fact { get; set; }
    
    // Вероятность.
    public double? Probability { get; set; }
  }
    
  /// <summary>
  /// Результат подбора сторон сделки для документа.
  /// </summary>
  [Public]
  partial class RecognizedDocumentParties
  {
    // НОР.
    public Sungero.SmartProcessing.Structures.Module.IRecognizedCounterparty BusinessUnit { get; set; }
    
    // Контрагент.
    public Sungero.SmartProcessing.Structures.Module.IRecognizedCounterparty Counterparty { get; set; }
    
    // НОР подобранная из ответственного сотрудника.
    public Sungero.Company.IBusinessUnit ResponsibleEmployeeBusinessUnit { get; set; }
    
    // Признак, что документ исходящий. Используется при создании счет-фактур.
    public bool? IsDocumentOutgoing { get; set; }
  }

  /// <summary>
  /// НОР и сопоставленный с ней список адресатов.
  /// </summary>
  [Public]
  partial class RecognizedLetterRecipient
  {
    // НОР.
    public Sungero.SmartProcessing.Structures.Module.IRecognizedCounterparty BusinessUnit { get; set; }

    // Адресаты.
    public List<Sungero.SmartProcessing.Structures.Module.IRecognizedOfficial> Addressees { get; set; }
  }

  /// <summary>
  /// Корреспондент и сопоставленные с ним контакты.
  /// </summary>
  [Public]
  partial class RecognizedLetterCorrespondent
  {
    // Корреспондент.
    public Sungero.SmartProcessing.Structures.Module.IRecognizedCounterparty Correspondent { get; set; }

    // Подписант.
    public Sungero.SmartProcessing.Structures.Module.IRecognizedOfficial Signatory { get; set; }
    
    // Исполнитель.
    public Sungero.SmartProcessing.Structures.Module.IRecognizedOfficial Responsible { get; set; }
  }
  
  /// <summary>
  /// Контрагент, НОР и сопоставленный с ними факт с типом "Контрагент".
  /// </summary>
  [Public]
  partial class RecognizedCounterparty
  {
    // НОР.
    public Sungero.Company.IBusinessUnit BusinessUnit { get; set; }
    
    // Контрагент.
    public Sungero.Parties.ICounterparty Counterparty { get; set; }
    
    // Факт с типом контрагент, по полям которого осуществлялся поиск.
    public Sungero.Commons.Structures.Module.IArioFact Fact { get; set; }
    
    // Тип найденного значения (Buyer, Seller и т.д.).
    public string Type { get; set; }
    
    // Вероятность определения НОР.
    public double? BusinessUnitProbability { get; set; }
    
    // Вероятность определения КА.
    public double? CounterpartyProbability { get; set; }
  }
  
  /// <summary>
  /// Подписант (контакт или сотрудник) и сопоставленный с ним факт.
  /// </summary>
  [Public]
  partial class RecognizedOfficial
  {
    // Сотрудник.
    public Sungero.Company.IEmployee Employee { get; set; }
    
    // Контактное лицо.
    public Sungero.Parties.IContact Contact { get; set; }
    
    // Факт, по полям которого было найдено контактное лицо.
    public Sungero.Commons.Structures.Module.IArioFact Fact { get; set; }
    
    // Вероятность.
    public double? Probability { get; set; }
  }
  
  /// <summary>
  /// Договорной документ и сопоставленный с ним факт.
  /// </summary>
  [Public]
  partial class RecognizedContract
  {
    // Договорной документ.
    public Sungero.Contracts.IContractualDocument Contract { get; set; }
    
    // Факт, по полям которого был найден договорной документ.
    public Sungero.Commons.Structures.Module.IArioFact Fact { get; set; }
    
    // Вероятность.
    public double? Probability { get; set; }
  }
  
  /// <summary>
  /// Документ и сопоставленный с ним факт.
  /// </summary>
  [Public]
  partial class RecognizedDocument
  {
    // Ведущий документ.
    public Sungero.Docflow.IOfficialDocument Document { get; set; }
    
    // Факт, по полям которого был найден ведущий документ.
    public Sungero.Commons.Structures.Module.IArioFact Fact { get; set; }
    
    // Вероятность.
    public double? Probability { get; set; }
  }
  
  /// <summary>
  /// Правила обработки.
  /// </summary>
  [Public]
  partial class ProcessingRules
  {
    // Имя класса.
    public string ClassName { get; set; }
    
    // Имя правила извлечения факта.
    public string GrammarName { get; set; }
  }
  
  /// <summary>
  /// Пакет бинарных образов документов DCS (используется для обработки на клиенте).
  /// </summary>
  [Public]
  partial class DcsPackage
  {
    // Имя линии.
    public string SenderLine { get; set; }

    // Тип источника.
    public string SourceType { get; set; }

    // Имя источника.
    // Для электронной почты: имя экземпляра ввода (из конфигурационного файла DCS).
    // Для папки: путь до папки, из которой DCS берет документы на обработку.
    public string SourceName { get; set; }

    // Путь к пакету.
    public string PackageFolderPath { get; set; }

    // ИД пакета.
    public string PackageId { get; set; }

    // Дата и время начала обработки пакета DCS.
    public DateTime? DcsProcessingBeginDate { get; set; }

    // Отправлен по почте.
    public bool SentByMail { get; set; }

    // Информация о письме.
    public Sungero.SmartProcessing.Structures.Module.IMailInfo MailInfo { get; set; }

    // Тело письма.
    public Sungero.SmartProcessing.Structures.Module.IDcsBlob MailBodyBlob { get; set; }

    // Бинарные образы документов DCS, которые пришли на вход DCS.
    public List<Sungero.SmartProcessing.Structures.Module.IDcsBlob> SourceBlobs { get; set; }

    // Бинарные образы документов DCS, которые вышли из DCS и пришли к нам.
    public List<Sungero.SmartProcessing.Structures.Module.IDcsBlob> Blobs { get; set; }
  }

  /// <summary>
  /// Информация о письме.
  /// </summary>
  [Public]
  partial class MailInfo
  {
    // Тема.
    public string Subject { get; set; }

    // Адрес отправителя.
    public string FromAddress { get; set; }

    // Имя отправителя.
    public string FromName { get; set; }

    // Получатели.
    public List<Sungero.SmartProcessing.Structures.Module.IMailRecipient> To { get; set; }

    // Копия.
    public List<Sungero.SmartProcessing.Structures.Module.IMailRecipient> CC { get; set; }

    // ИД почтового сообщения. Присваивается почтовым сервером.
    public string MessageId { get; set; }

    // Приоритет (важность) письма. Возможные значения: Low, Normal и High.
    public string Priority { get; set; }

    // Дата и время отправки письма.
    public DateTime? SendDate { get; set; }
  }

  /// <summary>
  /// Получатель письма.
  /// </summary>
  [Public]
  partial class MailRecipient
  {
    // Имя.
    public string Name { get; set; }

    // Адрес.
    public string Address { get; set; }
  }

  /// <summary>
  /// Бинарный образ документа DCS (используется для обработки на клиенте).
  /// </summary>
  [Public]
  partial class DcsBlob
  {
    // Оригинальное имя файла.
    public string OriginalFileName { get; set; }

    // Путь до файла.
    public string FilePath { get; set; }

    // Тело документа.
    public byte[] Body { get; set; }

    // Результат обработки Ario в json формате.
    public string ArioResultJson { get; set; }

    // Дата и время создания файла.
    public DateTime? Created { get; set; }

    // Дата и время последнего изменения файла.
    public DateTime? Modified { get; set; }

    // Размер файла в байтах.
    public long FileSize { get; set; }

    // Количество страниц в многостраничном файле.
    public int? PageCount { get; set; }

    // Номер первой страницы многостраничного файла, попавшей в пакет.
    public int? FirstPage { get; set; }

    // Номер последней страницы многостраничного файла, попавшей в пакет.
    public int? LastPage { get; set; }

    // Список штрихкодов. Заполняется, если в DCS настроено разделение по штрихкодам.
    public List<string> Barcodes { get; set; }
    
    // Признак того, что исходный файл является содержимым, пришедшим в теле письма.
    public bool IsInlineMailContent { get; set; }
    
    // Идентификатор содержимого внутри тела письма.
    public string MailContentId { get; set; }
  }

  /// <summary>
  /// Информация по страницам документов из перекомплектования.
  /// </summary>
  [Public(Isolated = true)]
  partial class RepackingPage
  {
    // ИД исходного документа.
    public long DocumentId { get; set; }
    
    // Поворот страницы.
    public int Rotation { get; set; }
    
    // Порядковый номер страницы.
    public int Number { get; set; }
  }

  /// <summary>
  /// Информация о новом документе, который был создан в рамках сессии перекомплектования.
  /// </summary>
  [Public(Isolated = true)]
  partial class NewDocument
  {
    // Имя документа.
    public string Name { get; set; }
    
    // Тип документа.
    public string TypeId { get; set; }
    
    // Информация о страницах.
    public List<Sungero.SmartProcessing.Structures.Module.IRepackingPage> Pages { get; set; }

    // Тип документа.
    public bool IsLeading { get; set; }
  }

  /// <summary>
  /// Информация по документу, у которого было изменено тело в рамках сессии перекомплектования.
  /// </summary>
  [Public]
  partial class ChangedDocument
  {
    // ИД документа.
    public long OriginalDocumentId { get; set; }
    
    // Информация о страницах.
    public List<Sungero.SmartProcessing.Structures.Module.IRepackingPage> Pages { get; set; }
  }

  /// <summary>
  /// Информация об исходном документе для перекомплектования.
  /// </summary>
  [Public(Isolated = true)]
  partial class RepackingDocumentDTO
  {
    // Имя документа.
    public string Name { get; set; }
    
    // Ид документа.
    public long DocumentId { get; set; }
    
    // Ид версии.
    public long VersionId { get; set; }
    
    // Тип документа.
    public string Type { get; set; }
  }

  /// <summary>
  /// Информация о типах документов.
  /// </summary>
  [Public(Isolated = true)]
  partial class RepackingDocumentType
  {
    // ИД типа.
    public string Id { get; set; }
    
    // Имя типа.
    public string Name { get; set; }
  }
  
  /// <summary>
  /// Извлеченный из документа текстовый слой.
  /// </summary>
  [Public(Isolated = true)]
  partial class ExtractedText
  {
    // Извлеченный текст документа.
    public string Text { get; set; }

    // Извлеченный текст всех страниц.
    public List<string> Pages { get; set; }

    // Сообщение об ошибке, если текст извлечь не удалось.
    public string ErrorMessage { get; set; }
  }

  /// <summary>
  /// Наименование и ОПФ организации, разделённые сервисом Ario.
  /// </summary>
  [Public]
  partial class CounterpartyNameAndLegalForm
  {
    // Название организации без ОПФ.
    public string Name { get; set; }

    // Организационно-правовая форма.
    public string LegalForm { get; set; }
  }
  
  /// <summary>
  /// Информация о документе для выгрузки.
  /// </summary>
  [Public]
  partial class DocumentForMetric
  {
    public string Type { get; set; }

    public long Id { get; set; }

    public string Extension { get; set; }

    public byte[] Body { get; set; }

    public string Error { get; set; }
  }
  
}