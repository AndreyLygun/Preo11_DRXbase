using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.MeetingsUI.Server
{
  public class ModuleFunctions
  {
    #region Фильтрация протоколов совещаний
    
    /// <summary>
    /// Отфильтровать протоколы совещаний по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Протоколы совещаний для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные протоколы совещаний.</returns>
    /// <remarks>Условия, которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<Sungero.Meetings.IMinutes> MinutesApplyStrongFilter(IQueryable<Sungero.Meetings.IMinutes> query, Sungero.MeetingsUI.FolderFilterState.IMinutesFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр по участникам совещания.
      var allMyGroups = Recipients.AllRecipientIds;
      if (filter.President || filter.Member || filter.Secretary)
        query = query.Where(p => p.Meeting != null && ((Equals(p.Meeting.President, Users.Current) && filter.President) ||
                                                       (Equals(p.Meeting.Secretary, Users.Current) && filter.Secretary) ||
                                                       (filter.Member && p.Meeting.Members.Any(m => Equals(m.Member, Users.Current) || allMyGroups.Contains(m.Member.Id)))));
      
      // Фильтр "Дата документа".
      if (filter.LastWeek)
        query = this.MinutesApplyFilterByDate(query, filter);
      
      return query;
    }

    /// <summary>
    /// Отфильтровать протоколы совещаний по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query">Протоколы совещаний для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Протоколы совещаний.</returns>
    /// <remarks>Условия, которые используют индексы, но не максимально оптимально.</remarks>
    public virtual IQueryable<Sungero.Meetings.IMinutes> MinutesApplyOrdinaryFilter(IQueryable<Sungero.Meetings.IMinutes> query, Sungero.MeetingsUI.FolderFilterState.IMinutesFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "Дата документа".
      if (filter.LastMonth || filter.Last90Days || filter.ManualPeriod)
        query = this.MinutesApplyFilterByDate(query, filter);
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать протоколы совещаний по неоптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Протоколы совещаний для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные протоколы совещаний.</returns>
    /// <remarks>Условия, которые могут выполняться долго (например те, которые не могут использовать индексы).</remarks>
    public virtual IQueryable<Sungero.Meetings.IMinutes> MinutesApplyWeakFilter(IQueryable<Sungero.Meetings.IMinutes> query, Sungero.MeetingsUI.FolderFilterState.IMinutesFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр по исполнителю.
      var assignees = new List<Sungero.CoreEntities.IUser>();
      if (filter.ByMe)
        assignees.Add(Users.Current);
      if (filter.ByMySubordinate)
        assignees.AddRange(Company.Employees.GetAll(e => e.Department.Manager != null && Equals(e.Department.Manager, Users.Current) && !Equals(e, Users.Current)).ToList());
      if (filter.ByEmployee && filter.Employee != null)
        assignees.Add(filter.Employee);
      if (filter.ByMe || filter.ByMySubordinate || filter.ByEmployee)
        query = query.Where(x => Sungero.RecordManagement.ActionItemExecutionTasks.GetAll().Any(p => p.AttachmentDetails.Any(a => a.AttachmentId == x.Id)
                                                                                                && assignees.Contains(p.Assignee)));
      return query;
    }
    
    /// <summary>
    /// Отфильтровать протоколы совещаний по установленной дате.
    /// </summary>
    /// <param name="query">Протоколы для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные протоколы.</returns>
    public virtual IQueryable<Sungero.Meetings.IMinutes> MinutesApplyFilterByDate(IQueryable<Sungero.Meetings.IMinutes> query, Sungero.MeetingsUI.FolderFilterState.IMinutesFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      var beginDate = Calendar.UserToday.AddDays(-7);
      var endDate = Calendar.UserToday;
      
      if (filter.LastMonth)
        beginDate = Calendar.UserToday.AddDays(-30);
      if (filter.Last90Days)
        beginDate = Calendar.UserToday.AddDays(-90);
      
      if (filter.ManualPeriod)
      {
        beginDate = filter.DateRangeFrom ?? Calendar.SqlMinValue;
        endDate = filter.DateRangeTo ?? Calendar.SqlMaxValue;
      }
      
      query = Docflow.PublicFunctions.Module.OfficialDocumentsApplyFilterByDate(query, beginDate, endDate)
        .Cast<Meetings.IMinutes>();
      
      return query;
    }
    
    /// <summary>
    /// Определить, нужно ли использовать предварительную фильтрацию для протоколов совещаний.
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True, если нужно использовать предварительную фильтрацию.</returns>
    public virtual bool UsePrefilterMinutes(Sungero.MeetingsUI.FolderFilterState.IMinutesFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.President ||
         filter.Member ||
         filter.Secretary ||
         filter.LastWeek);
      return hasStrongFilter;
    }
    #endregion
  }
}
