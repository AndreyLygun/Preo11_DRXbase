using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalReviewAssignment;

namespace Sungero.Docflow.Server
{
  partial class ApprovalReviewAssignmentFunctions
  {
    #region Контроль состояния
    
    /// <summary>
    /// Построить регламент.
    /// </summary>
    /// <returns>Регламент.</returns>
    [Remote(IsPure = true)]
    [LocalizeFunction("GetStagesStateViewFunctionName", "GetStagesStateViewFunctionDescription")]
    public Sungero.Core.StateView GetStagesStateView()
    {
      return PublicFunctions.ApprovalRuleBase.GetStagesStateView(_obj);
    }
    
    #endregion

    #region Лист согласования

    /// <summary>
    /// Получить модель контрола состояния листа согласования.
    /// </summary>
    /// <returns>Модель контрола состояния листа согласования.</returns>
    [Remote]
    [LocalizeFunction("GetApprovalListStateFunctionName", "GetApprovalListStateFunctionDescription")]
    public StateView GetApprovalListState()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      return CreateApprovalListStateView(document);
    }
    
    /// <summary>
    /// Создать модель контрола состояния листа согласования.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Модель контрола состояния листа согласования.</returns>
    [Remote(IsPure = true)]
    public static StateView CreateApprovalListStateView(IOfficialDocument document)
    {
      return Functions.Module.CreateApprovalListStateView(document);
    }
    
    /// <summary>
    /// Получить ошибки валидации подписи.
    /// </summary>
    /// <param name="signature">Электронная подпись.</param>
    /// <returns>Ошибки валидации подписи.</returns>
    public static Structures.ApprovalTask.SignatureValidationErrors GetValidationInfo(Sungero.Domain.Shared.ISignature signature)
    {
      return Functions.Module.GetValidationInfo(signature);
    }
    
    #endregion
    
    /// <summary>
    /// Необходимо ли скрыть "Вынести резолюцию".
    /// </summary>
    /// <returns>True, если скрыть, иначе - false.</returns>
    [Remote(IsPure = true)]
    public bool NeedHideAddResolutionAction()
    {
      // Скрыть вынесение резолюции, если этапа создания поручений нет в правиле.
      var stages = Functions.ApprovalTask.GetStages(ApprovalTasks.As(_obj.Task)).Stages;
      var executionStage = stages.FirstOrDefault(s => s.StageType == Docflow.ApprovalStage.StageType.Execution);
      if (executionStage == null)
        return true;

      // Скрыть вынесение резолюции, если этап создания поручений схлопнут.
      var isExecutionStageCollapsed = _obj.CollapsedStagesTypesRe.Any(cst => cst.StageType == Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.Execution);
      if (isExecutionStageCollapsed)
        return true;
      
      var task = ApprovalTasks.As(_obj.Task);
      
      // Скрыть вынесение резолюции, если у этапа создания поручений нет исполнителя.
      if (Functions.ApprovalStage.GetStagePerformer(task, executionStage.Stage) == null)
        return true;
      
      // Скрыть вынесение резолюции, если это обработка резолюции.
      var reviewStage = stages.FirstOrDefault(s => s.StageType == Docflow.ApprovalStage.StageType.Review);
      if (reviewStage.Stage.IsResultSubmission == true && !Equals(task.Addressee, _obj.Performer))
        return true;
      
      return false;
    }
    
  }
}