using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement.Structures.Module
{

  /// <summary>
  /// Облегченное задание. Минимально необходимое количество полей для расчета просрочки.
  /// </summary>
  partial class LightAssignment
  {
    public long Id { get; set; }
    
    /// <summary>
    /// Статус задания.
    /// </summary>
    public Sungero.Core.Enumeration? Status { get; set; }
    
    /// <summary>
    /// Срок задания.
    /// </summary>
    public DateTime? Deadline { get; set; }
    
    /// <summary>
    /// Дата изменения задания.
    /// </summary>
    public DateTime? Modified { get; set; }
    
    /// <summary>
    /// Дата создания задания.
    /// </summary>
    public DateTime? Created { get; set; }
  }
  
  /// <summary>
  /// Краткая информация по исполнению поручения.
  /// </summary>
  partial class LightActionItem
  {
    /// <summary>
    /// ID поручения.
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Статус поручения.
    /// </summary>
    public Sungero.Core.Enumeration? Status { get; set; }
    
    /// <summary>
    /// Дата завершения.
    /// </summary>
    public DateTime? ActualDate { get; set; }
    
    /// <summary>
    /// Срок.
    /// </summary>
    public DateTime? Deadline { get; set; }
    
    /// <summary>
    /// ID автора поручения.
    /// </summary>
    public long? AuthorId { get; set; }
    
    /// <summary>
    /// ID исполнителя.
    /// </summary>
    public long? AssigneeId { get; set; }
    
    /// <summary>
    /// Текст поручения.
    /// </summary>
    public string ActionItem { get; set; }
    
    /// <summary>
    /// Состояние.
    /// </summary>
    public Sungero.Core.Enumeration? ExecutionState { get; set; }
    
    /// <summary>
    /// Автор.
    /// </summary>
    public Sungero.CoreEntities.IUser Author { get; set; }
    
    /// <summary>
    /// Исполнитель.
    /// </summary>
    public Sungero.Company.IEmployee Assignee { get; set; }
    
    /// <summary>
    /// Имена соисполнителей в короткой форме.
    /// </summary>
    public string CoAssigneeShortNames { get; set; }
  }
  
  /// <summary>
  /// Краткая информация по исполнению поручения для виджета "Исполнение поручений в срок".
  /// </summary>
  partial class LightActionItemForChart
  {
    /// <summary>
    /// Идентификатор поручения.
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Статус поручения.
    /// </summary>
    public Sungero.Core.Enumeration? Status { get; set; }
    
    /// <summary>
    /// Дата завершения.
    /// </summary>
    public DateTime? ActualDate { get; set; }
    
    /// <summary>
    /// Срок.
    /// </summary>
    public DateTime? Deadline { get; set; }
    
    /// <summary>
    /// Автор поручения.
    /// </summary>
    public IUser Author { get; set; }
    
    /// <summary>
    /// Исполнитель.
    /// </summary>
    public Sungero.Company.IEmployee Assignee { get; set; }
  }
  
  /// <summary>
  /// Параметры для получения краткой информации по исполнению поручений в срок за период.
  /// </summary>
  partial class ActionItemCompletionDataParameters
  {
    /// <summary>
    /// Запускается от администратора или аудитора.
    /// </summary>
    public bool IsAdministratorOrAdvisor { get; set; }
    
    /// <summary>
    /// Начало периода.
    /// </summary>
    public DateTime? BeginDate { get; set; }
    
    /// <summary>
    /// Конец периода.
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Автор поручения.
    /// </summary>
    public Sungero.Company.IEmployee Author { get; set; }
    
    /// <summary>
    /// ID всех, кто может быть автором поручения.
    /// </summary>
    public List<long> AuthorIds { get; set; }
    
    /// <summary>
    /// ID всех, кто мог стартовать поручение.
    /// </summary>
    public List<long> StartedByIds { get; set; }
    
    /// <summary>
    /// Исполнитель поручения.
    /// </summary>
    public Sungero.CoreEntities.IUser Assignee { get; set; }
    
    /// <summary>
    /// Подразделение.
    /// </summary>
    public Sungero.Company.IDepartment Department { get; set; }
    
    /// <summary>
    /// НОР.
    /// </summary>
    public Sungero.Company.IBusinessUnit BusinessUnit { get; set; }
    
    /// <summary>
    /// Совещание.
    /// </summary>
    public Sungero.Meetings.IMeeting Meeting { get; set; }
    
    /// <summary>
    /// Признак того, что запрос информации вызван с обложки модуля "Совещания".
    /// </summary>
    public bool? IsMeetingsCoverContext { get; set; }
    
    /// <summary>
    /// Документ.
    /// </summary>
    public Sungero.Docflow.IOfficialDocument Document { get; set; }
    
    /// <summary>
    /// Тип документа.
    /// </summary>
    public Sungero.Docflow.IDocumentType DocumentType { get; set; }
  }
  
  /// <summary>
  /// Параметры отчета по журналу регистрации.
  /// </summary>
  partial class DocumentRegisterReportParametrs
  {
    public bool RunReport { get; set; }
    
    public DateTime? PeriodBegin { get; set; }
    
    public DateTime? PeriodEnd { get; set; }
    
    public Sungero.Docflow.IDocumentRegister DocumentRegister { get; set; }
  }

  /// <summary>
  /// Динамика исполнения поручений в срок.
  /// </summary>
  partial class ActionItemStatistic
  {
    public int? Statistic { get; set; }
    
    public DateTime Month { get; set; }
  }
    
  /// <summary>
  /// Данные для обучения классификатора виртуальных помощников.
  /// </summary>
  [Public]
  partial class AIAssistantTrainingData
  {
    // ИД исполнителя.
    public long AssigneeId { get; set; }
    
    // ИД документа.
    public int SerialNumber { get; set; }
    
    // Текстовый слой.
    public string Text { get; set; }
    
    // Элемент очереди обучения.
    public RecordManagement.IActionItemTrainQueueItem ActionItemTrainQueueItem { get; set; }
    
    // Признак, что данные будут отправлены на обучение.
    public bool SentForTraining { get; set; }
    
    // Признак, что при получении данных возникли ошибки.
    public bool HasError { get; set; }
  }
  
  /// <summary>
  /// Параметры прекращения подчиненных поручений.
  /// </summary>
  [Public]
  partial class AbortSubActionItemsParameters
  {
    public bool IsConfirmed { get; set; }
    
    public bool NeedAbortChildActionItems { get; set; }
  }
}