using System;
using Sungero.Core;

namespace Sungero.Intelligence.Constants
{
  public static class AIManagersAssistant
  {
    /// <summary>
    /// Значение минимального порога классификации по умолчанию.
    /// </summary>
    [Sungero.Core.Public]
    public const int LowerClassificationLimit = 40;
    
    /// <summary>
    /// Значение максимально возможного порога классификации.
    /// </summary>
    public const int MaxClassificationLimit = 100;
  }
}