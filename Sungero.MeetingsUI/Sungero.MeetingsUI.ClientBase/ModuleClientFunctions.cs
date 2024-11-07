using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.MeetingsUI.Client
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Отобразить отчет по исполнению поручений по совещаниям.
    /// </summary>
    [LocalizeFunction("OpenActionItemExecutionReportFunctionName", "OpenActionItemExecutionReportFunctionDescription")]
    public virtual void OpenActionItemExecutionReport()
    {
      var actionItemExecutionReport = RecordManagement.Reports.GetActionItemsExecutionReport();
      actionItemExecutionReport.IsMeetingsCoverContext = true;
      actionItemExecutionReport.Open();
    }

    /// <summary>
    /// Создать протокол по совещанию.
    /// </summary>
    [LocalizeFunction("CreateMinutesFunctionName", "CreateMinutesFunctionDescription")]
    public virtual void CreateMinutes()
    {
      Meetings.PublicFunctions.Minutes.Remote.CreateMinutes().Show();
    }

    /// <summary>
    /// Создать повестку.
    /// </summary>
    [LocalizeFunction("CreateAgendaFunctionName", "CreateAgendaFunctionDescription")]
    public virtual void CreateAgenda()
    {
      if (!Docflow.PublicFunctions.Module.Remote.IsModuleAvailableForCurrentUserByLicense(Sungero.Meetings.PublicConstants.Module.MeetingsUIGuid))
      {
        Dialogs.NotifyMessage(Sungero.Meetings.Meetings.Resources.NoLicenceToCreateAgenda);
        return;
      }
      
      Meetings.PublicFunctions.Agenda.Remote.CreateAgenda().Show();
    }

    /// <summary>
    /// Создать совещание.
    /// </summary>
    [LocalizeFunction("CreateMeetingFunctionName", "")]
    public virtual void CreateMeeting()
    {
      Meetings.PublicFunctions.Meeting.Remote.CreateMeeting().Show();
    }
   
    /// <summary>
    /// Отобразить список поручений по совещаниям.
    /// </summary>
    /// <returns>Список поручений.</returns>
    [LocalizeFunction("ShowMeetingActionItemExecutionTasksFunctionName", "ShowMeetingActionItemExecutionTasksFunctionDescription")]
    public virtual IQueryable<RecordManagement.IActionItemExecutionTask> ShowMeetingActionItemExecutionTasks()
    {
      return Meetings.PublicFunctions.Module.Remote.GetMeetingActionItemExecutionTasks();
    }    

  }
}