using System;
using Sungero.Core;

namespace Sungero.ContractsUI.Constants
{
  public static class Module
  {
    /// <summary>
    /// Наименование отметки в истории о заключении договора.
    /// </summary>
    [Public]
    public const string SetToActiveOperationName = "SetToActive";
    
    /// <summary>
    /// Наименование отметки в истории о исполнении договора.
    /// </summary>
    [Public]
    public const string SetToClosedOperationName = "SetToClosed";
    
    /// <summary>
    /// Наименование отметки в истории о расторжении договора.
    /// </summary>
    [Public]
    public const string SetToTerminatedOperationName = "SetToTerminated";
    
    /// <summary>
    /// Наименование отметки в истории о аннулировании договора.
    /// </summary>
    [Public]
    public const string SetToObsoleteOperationName = "SetToObsolete";
  }
}