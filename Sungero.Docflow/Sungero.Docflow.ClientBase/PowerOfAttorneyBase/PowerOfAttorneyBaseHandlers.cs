using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorneyBase;

namespace Sungero.Docflow
{
  partial class PowerOfAttorneyBaseClientHandlers
  {
    
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      Functions.PowerOfAttorneyBase.SetAgentFieldsVisibleAndRequiredFlags(_obj);
      this.UpdateManyRepresentativesPlaceholder();
      
      _obj.State.Pages.ManyRepresentatives.IsVisible = _obj.IsManyRepresentatives == true;
    }

    public override void NameValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      base.NameValueInput(e);
      
      // Убрать пробелы в имени, если оно вводится вручную.
      if (_obj.DocumentKind != null && _obj.DocumentKind.GenerateDocumentName == false && !string.IsNullOrEmpty(e.NewValue))
        e.NewValue = e.NewValue.Trim();
    }

    public virtual void DaysToFinishWorksValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue < 0)
        e.AddError(PowerOfAttorneyBases.Resources.IncorrectReminder);
      
      var errorText = Sungero.Docflow.Functions.PowerOfAttorneyBase.CheckCorrectnessDaysToFinishWorks(_obj.ValidTill, e.NewValue);
      if (!string.IsNullOrEmpty(errorText))
        e.AddError(errorText);
    }

    public virtual void ValidTillValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var errorText = Sungero.Docflow.Functions.PowerOfAttorneyBase.CheckCorrectnessDaysToFinishWorks(e.NewValue, _obj.DaysToFinishWorks);
      if (!string.IsNullOrEmpty(errorText))
        e.AddError(errorText);
    }
    
    /// <summary>
    /// Установить видимость и текст (если не установлен) свойству "Нескольким представителям".
    /// </summary>
    private void UpdateManyRepresentativesPlaceholder()
    {
      // Видимость
      _obj.State.Properties.ManyRepresentativesPlaceholder.IsVisible = _obj.IsManyRepresentatives == true;
      
      // Текст, если не установлен и контрол отображается
      if (_obj.State.Properties.ManyRepresentativesPlaceholder.IsVisible &&
          string.IsNullOrEmpty(_obj.ManyRepresentativesPlaceholder))
        _obj.ManyRepresentativesPlaceholder = Sungero.Docflow.PowerOfAttorneyBases.Resources.ManyRepresentatives;
    }
  }
}