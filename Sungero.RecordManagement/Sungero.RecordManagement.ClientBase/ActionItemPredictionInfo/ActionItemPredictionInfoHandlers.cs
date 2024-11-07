using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemPredictionInfo;

namespace Sungero.RecordManagement
{
  partial class ActionItemPredictionInfoClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      // Снятие блокировки для исключения ошибок изменения записи под системным пользователем, т.к. все поля карточки недоступны для редактирования.
      if (Locks.GetLockInfo(_obj).IsLockedByMe)
        Locks.Unlock(_obj);
    }

  }
}