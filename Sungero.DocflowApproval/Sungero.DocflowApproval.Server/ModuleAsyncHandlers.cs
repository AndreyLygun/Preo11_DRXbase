using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;

namespace Sungero.DocflowApproval.Server
{
  public class ModuleAsyncHandlers
  {
    /// <summary>
    /// Выполнить действия, необходимые при прекращении задачи на согласование по процессу.
    /// </summary>
    /// <param name="args">Параметры вызова асинхронного обработчика.</param>
    public virtual void ProcessDocumentFlowTaskAbort(Sungero.DocflowApproval.Server.AsyncHandlerInvokeArgs.ProcessDocumentFlowTaskAbortInvokeArgs args)
    {
      var task = DocumentFlowTasks.Get(args.TaskId);
      if (task == null)
      {
        Logger.DebugFormat("ProcessDocumentFlowTaskAbort. Task not found. TaskId: {0}", args.TaskId);
        return;
      }
      
      var document = OfficialDocuments.As(task.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (document == null)
      {
        Logger.DebugFormat("ProcessDocumentFlowTaskAbort. Task: {0}. Primary Document not found.", args.TaskId);
        return;
      }
      
      var hasNewApprovalTasks = Docflow.PublicFunctions.OfficialDocument.HasNewApprovalTasks(document, args.Aborted);
      if (hasNewApprovalTasks)
      {
        Logger.DebugFormat("ProcessDocumentFlowTaskAbort. Has new tasks. Document: {0}", document.Id);
        return;
      }
      
      Functions.DocumentFlowTask.ProcessTaskAbort(task, args.SetObsolete);
      task.Save();
    }

  }
}