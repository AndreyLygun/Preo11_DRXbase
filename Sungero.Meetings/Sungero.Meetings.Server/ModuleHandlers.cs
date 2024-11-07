using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Meetings.Server
{
  partial class MinutesFolderHandlers
  {

    public virtual IQueryable<Sungero.Meetings.IMinutes> MinutesDataQuery(IQueryable<Sungero.Meetings.IMinutes> query)
    {
      if (_filter == null)
        return query;
      
      if (!Functions.Module.UsePrefilterMinutes(_filter))
        query = Functions.Module.MinutesApplyOrdinaryFilter(query, _filter);
      
      query = Functions.Module.MinutesApplyWeakFilter(query, _filter);
      
      return query;
    }

    public virtual IQueryable<Sungero.Meetings.IMinutes> MinutesPreFiltering(IQueryable<Sungero.Meetings.IMinutes> query)
    {
      if (_filter == null)
        return query;
      
      if (Functions.Module.UsePrefilterMinutes(_filter))
      {
        query = Functions.Module.MinutesApplyStrongFilter(query, _filter);
        query = Functions.Module.MinutesApplyOrdinaryFilter(query, _filter);
      }
      
      return query;
    }
  }

  partial class MeetingsHandlers
  {
  }
}