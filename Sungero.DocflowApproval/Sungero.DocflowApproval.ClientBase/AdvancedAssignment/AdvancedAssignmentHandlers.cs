using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.DocflowApproval.AdvancedAssignment;

namespace Sungero.DocflowApproval
{
  partial class AdvancedAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!this.NeedRightsToOfficialDocumentParamExist())
        this.AddOrUpdateNeedRightsToOfficialDocumentParam();
      
      if (_obj.Status == Status.InProcess && this.NeedRightsToOfficialDocument())
      {
        _obj.State.Properties.ReworkPerformer.IsVisible = false;
        e.AddError(Docflow.Resources.NoRightsToDocument);
      }
      
      if (this.NeedFillMissingAssignmentBlockParams())
        Functions.AdvancedAssignment.Remote.FillAssignmentBlockParams(_obj);
      
      if (!Functions.AdvancedAssignment.CanSendForRework(_obj))
        _obj.State.Properties.ReworkPerformer.IsVisible = false;
      else
        _obj.State.Properties.ReworkPerformer.IsVisible = Functions.AdvancedAssignment.CanChangeReworkPerformer(_obj);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      // При первом обращении к вложениям они кэшируются с учетом прав на сущности,
      // последующие обращения, в том числе через AllowRead, работают с закешированными сущностями и правами.
      // Если первое обращение было через AllowRead, то последующий код будет работать так, будто есть права, и наоборот,
      // если кэширование было без прав на сущности, то в AllowRead вложений не получить.
      // Корректность доступных действий важнее функциональности ниже, поэтому обеспечиваем работу NeedRightsToOfficialDocument
      // с серверными вложениями, а не из кэша.
      // BUGS 319348, 320495.
      this.AddOrUpdateNeedRightsToOfficialDocumentParam();
      
      if (_obj.Status == Status.InProcess && this.NeedRightsToOfficialDocument())
      {
        e.HideAction(_obj.Info.Actions.Complete);
        e.HideAction(_obj.Info.Actions.Forward);
      }
      
      Functions.AdvancedAssignment.Remote.FillAssignmentBlockParams(_obj);
      
      if (!Functions.AdvancedAssignment.CanForward(_obj))
        e.HideAction(_obj.Info.Actions.Forward);
      
      if (!Functions.AdvancedAssignment.CanSendForRework(_obj))
        e.HideAction(_obj.Info.Actions.ForRework);
    }
    
    /// <summary>
    /// Добавить или обновить значение параметра, указывающего на то, что на основной документ не хватает прав..
    /// </summary>
    public virtual void AddOrUpdateNeedRightsToOfficialDocumentParam()
    {
      Sungero.Commons.PublicFunctions.Module.AddOrUpdateEntityParams(_obj,
                                                                     Constants.AdvancedAssignment.NeedRightsToOfficialDocumentParamName,
                                                                     Functions.AdvancedAssignment.Remote.NeedRightsToOfficialDocument(_obj));
    }
    
    /// <summary>
    /// Проверить основной документ на нехватку прав, на основании данных в param.
    /// </summary>
    /// <returns>True - на документ не хватает прав. False - права есть, или их выдавать не нужно.</returns>
    public virtual bool NeedRightsToOfficialDocument()
    {
      return Sungero.Commons.PublicFunctions.Module.GetBooleanEntityParamsValue(_obj, Constants.AdvancedAssignment.NeedRightsToOfficialDocumentParamName);
    }
    
    /// <summary>
    /// Проверить, нужно ли заполнить отсутствующие параметры блока в параметры задания.
    /// </summary>
    /// <returns>True - нужно заполнять.</returns>
    public virtual bool NeedFillMissingAssignmentBlockParams()
    {
      return !Sungero.Commons.PublicFunctions.Module.EntityParamsContainsKey(_obj, Constants.AdvancedAssignment.AllowForwardParamName) ||
        !Sungero.Commons.PublicFunctions.Module.EntityParamsContainsKey(_obj,  Constants.AdvancedAssignment.AllowSendForReworkParamName) ||
        !Sungero.Commons.PublicFunctions.Module.EntityParamsContainsKey(_obj, Constants.AdvancedAssignment.AllowChangeReworkPerformerParamName);
    }
    
    /// <summary>
    /// Проверить, что параметр NeedRightsToOfficialDocument существует.
    /// </summary>
    /// <returns>True - параметр существует.</returns>
    public virtual bool NeedRightsToOfficialDocumentParamExist()
    {
      return Sungero.Commons.PublicFunctions.Module.EntityParamsContainsKey(_obj, Constants.AdvancedAssignment.NeedRightsToOfficialDocumentParamName);
    }
  }
}