using System;

namespace Sungero.RecordManagement.Constants
{
  public static class DocumentReviewTask
  {
    /// <summary>
    /// ИД диалога подтверждения при старте задачи.
    /// </summary>
    public const string StartConfirmDialogID = "37e9e49b-6985-4a87-b9e8-25b27aaca93b";
    
    /// <summary>
    /// ИД диалога подтверждения при старте задачи с удалением проектов резолюции.
    /// </summary>
    public const string StartWithDropConfirmDialogID = "23ef03f5-fc00-4fbc-81cd-4ea260e2a7d5";
    
    /// <summary>
    /// ИД диалога подтверждения при старте задачи с удалением проектов резолюции неактуальному адресату.
    /// </summary>
    public const string StartWithDropWrongActionItemsConfirmDialogID = "E9E38607-A51B-4903-B67D-7642C941EB3E";
    
    /// <summary>
    /// ИД диалога подтверждения при прекращении задачи.
    /// </summary>
    public const string AbortConfirmDialogID = "ec8301d0-6a9e-4f7c-827b-326945cc3a22";
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на рассмотрение проекта резолюции.
    /// </summary>
    public static class ReviewDraftResolutionAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Утвердить проект резолюции".
      /// </summary>
      public const string ForExecution = "2c387e45-877c-4c8a-9ea6-b11c417cee1c";
      
      /// <summary>
      /// С результатом "Принято к сведению".
      /// </summary>
      public const string Informed = "82752618-87f6-4186-b631-649a84b61968";
      
      /// <summary>
      /// С результатом "Принято к сведению" и удалением проекта.
      /// </summary>
      public const string InformedWithDrop = "c50923e8-8e0c-444a-97a5-d77c1f14e691";
      
      /// <summary>
      /// С результатом "Вернуть помощнику".
      /// </summary>
      public const string AddResolution = "dc675254-31a7-4e65-9f8f-f8a029f3d95f";
    }
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на рассмотрение.
    /// </summary>
    public static class ReviewManagerAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Вынесена резолюция".
      /// </summary>
      public const string AddResolution = "05a15cec-09dc-4c98-8caa-8f7ca6f2c56b";
      
      /// <summary>
      /// С результатом "Принято к сведению".
      /// </summary>
      public const string Explored = "d5c6bb60-2df4-440d-99a4-a4c6d82ecd45";
      
      /// <summary>
      /// С результатом "Принято к сведению" с заполненной резолюцией.
      /// </summary>
      public const string ExploredWithResolution = "929c182c-0741-44ac-b044-61185914dbbc";
      
      /// <summary>
      /// С результатом "Отправлено на исполнение".
      /// </summary>
      public const string AddAssignment = "9c0ba458-bed6-4d1c-8344-2cfd3b801c28";
      
      /// <summary>
      /// С результатом "Вернуть инициатору".
      /// </summary>
      public const string ForRework = "2a9900bb-0adB-4be4-8138-d79ada4b0103";
    }
    
    /// <summary>
    /// ИД диалога подтверждения при выполнении задания на обработку резолюции.
    /// </summary>
    public const string ReviewResolutionAssignmentConfirmDialogID = "6808a0c8-1aa9-4027-9c9e-c1537a453d2a";
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на подготовку проекта резолюции.
    /// </summary>
    public static class PreparingDraftResolutionAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Отправить на рассмотрение".
      /// </summary>
      public const string SendForReview = "f40e7720-9cd3-41db-b0c1-af2e97a7417c";
      
      /// <summary>
      /// С результатом "Отправить на рассмотрение" и удалением всех проектов резолюции.
      /// </summary>
      public const string SendForReviewWithDeletingDraftResolutions = "F20EF8C9-32CC-4AA4-83C1-96EE90503DC1";
      
      /// <summary>
      /// С результатом "Принято к сведению".
      /// </summary>
      public const string Explored = "d3872afd-91ff-4c10-9fd7-6b64c5488ad1";
      
      /// <summary>
      /// С результатом "Принято к сведению" и удалением всех проектов резолюции.
      /// </summary>
      public const string ExploredWithDeletingDraftResolutions = "310dec3c-22db-41a0-93e9-0e13ef346bcb";
      
      /// <summary>
      /// С результатом "Отправить на исполнение".
      /// </summary>
      public const string AddAssignment = "5318bbf1-371b-42ea-aca3-a8ed7e225134";
      
      /// <summary>
      /// С результатом "Вернуть инициатору".
      /// </summary>
      public const string ForRework = "ce4a26b5-60e9-ff5a-7f3a-36abe3798964";
      
      /// <summary>
      /// С результатом "Прекратить".
      /// </summary>
      public const string Abort = "a433031c-c7b8-4741-b492-1f79faf35ea9";
      
      /// <summary>
      /// Возврат конкурентного задания.
      /// </summary>
      public const string ReturnUncompleted = "e6d87eac-b4dc-4c09-870f-43482bf7f7aa";
      
      /// <summary>
      /// Возврат конкурентного задания и удаление всех проектов резолюции.
      /// </summary>
      public const string ReturnUncompletedWithDeletingDraftResolutions = "f72c4809-7ece-4d30-baac-e516e606a88a";
    }
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на рассмотрение.
    /// </summary>
    public static class DocumentReviewAssignmentConfirmDialogID
    {
      /// <summary>
      /// Возврат конкурентного задания.
      /// </summary>
      public const string ReturnUncompleted = "6307dca8-32e4-4069-9a0c-ab60cca67b54";
      
      /// <summary>
      /// Возврат конкурентного задания и удаление всех проектов резолюции.
      /// </summary>
      public const string ReturnUncompletedWithDeletingDraftResolutions = "f60b046f-c4f4-4101-a777-e6f1c4aa949d";
    }
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на доработку инициатором.
    /// </summary>
    public static class ReviewReworkAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Отправить на рассмотрение".
      /// </summary>
      public const string SendForReview = "3118f0c5-19d3-4f7a-9e19-b22ce6e4ef3a";
      
      /// <summary>
      /// С результатом "Отправить на рассмотрение" и удалением проекта.
      /// </summary>
      public const string SendForReviewWithDrop = "61748ebc-e48f-41cb-a231-dfdffa94bf0d";
      
      /// <summary>
      /// С результатом "Прекратить".
      /// </summary>
      public const string Abort = "c24465c0-2038-4557-b422-768846be1c5d";
      
      /// <summary>
      /// Возврат конкурентного задания.
      /// </summary>
      public const string ReturnUncompleted = "60816f2e-22fa-4df2-a71b-8fb7bff2f54d";
      
      /// <summary>
      /// Возврат конкурентного задания и удаление всех проектов резолюции.
      /// </summary>
      public const string ReturnUncompletedWithDeletingDraftResolutions = "b3771a2a-46f8-4198-8a51-7774fdb6a1ec";
    }
    
    /// <summary>
    /// Параметр "Возможность добавить поручение в "Проект резолюции"".
    /// </summary>
    [Sungero.Core.Public]
    public const string CanPrepareDraftResolutionParamName = "CanPrepareDraftResolution";
    
    /// <summary>
    /// Признак того, что работа с задачей идет в визуальном режиме.
    /// </summary>
    public const string WorkingWithGuiParamName = "WorkingWithGUI";
    
    /// <summary>
    /// Период проверки завершения рассмотрения в часах.
    /// </summary>
    public const double CheckCompletionMonitoringPeriodInHours = 8;
    
    /// <summary>
    /// ИД группы приложений.
    /// </summary>
    [Sungero.Core.Public]
    public static readonly Guid AddendaGroupGuid = Guid.Parse("5320f83f-1364-4035-a7ab-44e457b9b388");
    
    /// <summary>
    /// Параметры блока мониторинга результатов Ario по умолчанию.
    /// </summary>
    public static class WaitArioProcessingBlockDefaultParams
    {
      public const int PeriodInSeconds = 60;
      public const int RelativeDeadlineInMinutes = 60;
    }

  }
}