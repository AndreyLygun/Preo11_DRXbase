using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ApprovalIncInvoicePaidStage;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts.Server
{
  partial class ApprovalIncInvoicePaidStageFunctions
  {
    /// <summary>
    /// Смена статуса входящего счета в процессе согласования по регламенту.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <returns>Результат выполнения кода.</returns>
    public override Docflow.Structures.ApprovalFunctionStageBase.ExecutionResult Execute(Sungero.Docflow.IApprovalTask approvalTask)
    {
      Logger.DebugFormat("ApprovalIncInvoicePaidStage. Start change incoming invoice state stage, approval task (ID={0}) (StartId={1}) (Iteration={2}) (StageNumber={3}).",
                         approvalTask.Id, approvalTask.StartId, approvalTask.Iteration, approvalTask.StageNumber);
      
      var mainDocument = approvalTask.DocumentGroup.OfficialDocuments.SingleOrDefault();
      if (mainDocument == null)
      {
        Logger.ErrorFormat("ApprovalIncInvoicePaidStage. Primary document not found. Approval task (ID={0}) (StartId={1}) (Iteration={2}) (StageNumber={3}).",
                           approvalTask.Id, approvalTask.StartId, approvalTask.Iteration, approvalTask.StageNumber);
        return this.GetErrorResult(Docflow.Resources.PrimaryDocumentNotFoundError);
      }
      
      var invoices = this.GetIncomingInvoicesToSetPaid(approvalTask);
      if (!invoices.Any())
      {
        Logger.DebugFormat("ApprovalIncInvoicePaidStage. Incoming invoices not found, no need to change state. Approval task (ID={0}).", approvalTask.Id);
        return this.GetSuccessResult();
      }
      
      var needRetry = false;
      foreach (var invoice in invoices)
      {
        var lockInfo = Locks.GetLockInfo(invoice);
        if (!lockInfo.IsLockedByOther)
        {
          try
          {
            Sungero.Contracts.Functions.IncomingInvoice.SetLifeCycleStateToPaid(invoice);
            invoice.Save();
            Logger.DebugFormat("ApprovalIncInvoicePaidStage. Set incoming invoice state to Paid. Approval task (ID={0}), Document (ID={1}), State = Paid.",
                               approvalTask.Id, invoice.Id);
          }
          catch (Exception ex)
          {
            needRetry = true;
            Logger.ErrorFormat("ApprovalIncInvoicePaidStage. Set incoming invoice state error. Approval task (ID={0}) (Iteration={1}) (StageNumber={2}) for document (ID={3})",
                               ex, approvalTask.StartId, approvalTask.Iteration, approvalTask.StageNumber, invoice.Id);
          }
        }
        else
        {
          needRetry = true;
          Logger.DebugFormat("ApprovalIncInvoicePaidStage. Document locked. Approval task (ID={0}), Document (ID={1}), Locked By (LoginId={2}).",
                             approvalTask.Id, invoice.Id, lockInfo.LoginId);
        }
      }
      
      if (needRetry)
        return this.GetRetryResult(string.Empty);
      
      return this.GetSuccessResult();
    }
    
    /// <summary>
    /// Получить входящие счета для установки статуса "Оплачено".
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <returns>Список входящих счетов.</returns>
    /// <remarks>Получает основной документ задачи на согласование, если это входящий счет.
    /// Получает все входящие счета из группы "Приложения", но только со статусом "Принят к оплате".
    /// Счета из группы "Дополнительно" игнорируются.</remarks>
    [Public]
    public virtual List<IIncomingInvoice> GetIncomingInvoicesToSetPaid(Sungero.Docflow.IApprovalTask approvalTask)
    {
      var result = new List<IIncomingInvoice>();
      
      var mainDocument = approvalTask.DocumentGroup.OfficialDocuments.SingleOrDefault();
      if (mainDocument != null && IncomingInvoices.Is(mainDocument))
        result.Add(IncomingInvoices.As(mainDocument));
      
      var addendaInvoices = approvalTask.AddendaGroup.OfficialDocuments
        .Where(x => IncomingInvoices.Is(x) && x.LifeCycleState == Sungero.Contracts.IncomingInvoice.LifeCycleState.Active)
        .Select(x => IncomingInvoices.As(x));
      result.AddRange(addendaInvoices);
      
      return result;
    }
    
  }
}