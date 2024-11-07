using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server
{
  partial class StatusReportRequestTaskFunctions
  {
    /// <summary>
    /// Построить модель состояния.
    /// </summary>
    /// <returns>Модель состояния.</returns>
    [Remote(IsPure = true)]
    [LocalizeFunction("GetStateViewFunctionName", "GetStateViewFunctionDescription")]
    public Sungero.Core.StateView GetStateView()
    {
      var parent = _obj.ParentAssignment != null ? _obj.ParentAssignment.Task : _obj.ParentTask;
      var parentTask = ActionItemExecutionTasks.As(parent);
      return Functions.ActionItemExecutionTask.GetStateView(parentTask);
    }
    
    /// <summary>
    /// Создать Запрос отчета.
    /// </summary>
    /// <param name="task">Поручение, для которого нужен отчет.</param>
    /// <returns>Задача "Запрос отчета по поручению".</returns>
    [Remote(PackResultEntityEagerly = true)]
    public static IStatusReportRequestTask CreateStatusReportRequest(IActionItemExecutionTask task)
    {
      var performers = Functions.Module.GetUnfinishedActionItemsAssignees(task).Distinct().ToList();
      if (!performers.Any())
        return null;
      
      var statusReportRequest = StatusReportRequestTasks.CreateAsSubtask(task);
      
      var document = task.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      var addenda = task.AddendaGroup.OfficialDocuments;
      Functions.StatusReportRequestTask.AddDocumentsToStatusReport(statusReportRequest, document, addenda.ToList());
      
      Functions.StatusReportRequestTask.SetStatusReportDetails(statusReportRequest);
      
      if (task.IsCompoundActionItem ?? false)
        statusReportRequest.Assignee = Functions.StatusReportRequestTask.GetDefaultPerformer(performers);
      else
        statusReportRequest.Assignee = task.Assignee;
      
      return statusReportRequest;
    }
    
    /// <summary>
    /// Создать Запрос отчета.
    /// </summary>
    /// <param name="assignment">Задание по поручению, для которого нужен отчет.</param>
    /// <returns>Задача "Запрос отчета по поручению".</returns>
    [Remote(PackResultEntityEagerly = true)]
    public static IStatusReportRequestTask CreateStatusReportRequest(IActionItemExecutionAssignment assignment)
    {
      var performers = Functions.Module.GetUnfinishedSubActionItemsAssignees(assignment).Distinct().ToList();
      if (!performers.Any())
        return null;
      
      var statusReportRequest = StatusReportRequestTasks.CreateAsSubtask(assignment);
      
      var document = assignment.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      var addenda = assignment.AddendaGroup.OfficialDocuments;
      Functions.StatusReportRequestTask.AddDocumentsToStatusReport(statusReportRequest, document, addenda.ToList());
      
      Functions.StatusReportRequestTask.SetStatusReportDetails(statusReportRequest);
      statusReportRequest.Assignee = Functions.StatusReportRequestTask.GetDefaultPerformer(performers);
      
      statusReportRequest.Author = assignment.Performer;
      return statusReportRequest;
    }
    
    /// <summary>
    /// Получить исполнителя по умолчанию.
    /// </summary>
    /// <param name="performers">Возможные исполнители.</param>
    /// <returns>Исполнитель по умолчанию.</returns>
    public static Company.IEmployee GetDefaultPerformer(List<IUser> performers)
    {
      var activePerformers = performers.Where(p => p.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
      if (performers.Count != 1 && activePerformers.Count() != 1)
        return null;
      
      return activePerformers.Any() ? Company.Employees.As(activePerformers.First()) : Company.Employees.As(performers.First());
    }
    
    /// <summary>
    /// Добавить документы во вложения задачи.
    /// </summary>
    /// <param name="document">Основной документ.</param>
    /// <param name="addenda">Приложения.</param>
    public virtual void AddDocumentsToStatusReport(Docflow.IOfficialDocument document, List<Docflow.IOfficialDocument> addenda)
    {
      if (document != null)
        _obj.DocumentsGroup.OfficialDocuments.Add(document);
      
      foreach (var addendum in addenda)
        _obj.AddendaGroup.All.Add(addendum);
    }
    
    /// <summary>
    /// Заполнить детали в запросе отчета.
    /// </summary>
    public virtual void SetStatusReportDetails()
    {
      _obj.Subject = Functions.StatusReportRequestTask.GetStatusReportRequestSubject(_obj,
                                                                                     StatusReportRequestTasks.Resources.ReportRequestTaskSubject,
                                                                                     _obj.Assignee);
      _obj.ActiveText = StatusReportRequestTasks.Resources.ReportFromJob;
    }
    
    /// <summary>
    /// Выдать права на задание.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="assignment">Задание.</param>
    [Obsolete("Метод не используется с 02.04.2024 и версии 4.10, т.к. прекращение задачи происходит асинхронно от системы и выдавать права больше не нужно.")]
    public static void GrantRightToAssignment(ITask task, IAssignment assignment)
    {
      // Выдать права на задание контролеру, инициатору и группе регистрации инициатора ведущей задачи (включая ведущие ведущего).
      var leadPerformers = Functions.ActionItemExecutionTask.GetLeadActionItemExecutionPerformers(ActionItemExecutionTasks.As(task));
      foreach (var performer in leadPerformers)
        assignment.AccessRights.Grant(performer, DefaultAccessRightsTypes.Change);
    }
    
    /// <summary>
    /// Проверить документ на вхождение в обязательную группу вложений.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если документ обязателен.</returns>
    public virtual bool DocumentInRequredGroup(Docflow.IOfficialDocument document)
    {
      return _obj.DocumentsGroup.OfficialDocuments.Any(d => Equals(d, document));
    }
    
    /// <summary>
    /// Получить нестандартных исполнителей задачи.
    /// </summary>
    /// <returns>Исполнители.</returns>
    public virtual List<IRecipient> GetTaskAdditionalAssignees()
    {
      var assignees = new List<IRecipient>();

      var statusReport = StatusReportRequestTasks.As(_obj);
      if (statusReport == null)
        return assignees;
      
      if (statusReport.Assignee != null)
        assignees.Add(statusReport.Assignee);
      
      return assignees.Distinct().ToList();
    }
    
    /// <summary>
    /// Продлить срок задачи.
    /// </summary>
    /// <param name="assignment">Задание, из которого было вызвано продление.</param>
    /// <param name="newDeadline">Новый срок.</param>
    /// <returns>Срок задачи продлен - true, иначе - false.</returns>
    public virtual bool ExtendTaskDeadline(IAssignment assignment, DateTime newDeadline)
    {
      _obj.MaxDeadline = newDeadline;
      return true;
    }
    
    #region Expression-функции для No-Code
    
    /// <summary>
    /// Получить тему задания на запрос отчета по поручению.
    /// </summary>
    /// <param name="task">Задача на запрос отчета по поручению.</param>
    /// <returns>Тема задания на запрос отчета по поручению.</returns>
    [ExpressionElement("ReportRequestAssignmentSubject", "")]
    public static string GetReportRequestAssignmentSubject(IStatusReportRequestTask task)
    {
      var beginningSubject = string.IsNullOrEmpty(task.ReportNote) ? StatusReportRequestTasks.Resources.ProvideReportByJob : StatusReportRequestTasks.Resources.FinalizeReportByJob;
      return Functions.StatusReportRequestTask.GetStatusReportRequestSubject(task, beginningSubject, task.Assignee);
    }
    
    /// <summary>
    /// Получить тему задания на приемку отчета по поручению.
    /// </summary>
    /// <param name="task">Задача на запрос отчета по поручению.</param>
    /// <returns>Тема задания на приемку отчета по поручению.</returns>
    [ExpressionElement("ReportRequestCheckAssignmentSubject", "")]
    public static string GetReportRequestCheckAssignmentSubject(IStatusReportRequestTask task)
    {
      return Functions.StatusReportRequestTask.GetStatusReportRequestSubject(task, StatusReportRequestTasks.Resources.CheckReportJob, task.Assignee);
    }
    
    #endregion

  }
}