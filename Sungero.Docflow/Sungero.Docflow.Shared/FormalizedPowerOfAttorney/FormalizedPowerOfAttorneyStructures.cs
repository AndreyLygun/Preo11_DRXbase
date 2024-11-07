using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.FormalizedPowerOfAttorney
{
  /// <summary>
  /// Информация о том, кому выдана доверенность.
  /// </summary>
  [Public]
  partial class IssuedToInfo
  {
    public string FullName { get; set; }
    
    public string TIN { get; set; }
    
    public string INILA { get; set; }
  }
  
  /// <summary>
  /// Результат синхронизации состояния эл. доверенности с данными сервиса эл. доверенностей.
  /// </summary>
  [Public]
  partial class FPoAStateSynchronizationResult
  {
    /// <summary>
    /// Итоговое сообщение, описывающее результат синхронизации.
    /// </summary>
    public string ResultMessage { get; set; }
    
    /// <summary>
    /// Признак успешности операции синхронизации.
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Признак того, что доверенность не найдена в сервисе.
    /// </summary>
    public bool PoaNotFound { get; set; }
  }
}