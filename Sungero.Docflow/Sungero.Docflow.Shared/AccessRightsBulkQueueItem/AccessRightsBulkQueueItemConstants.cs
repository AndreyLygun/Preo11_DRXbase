using System;
using Sungero.Core;

namespace Sungero.Docflow.Constants
{
  public static class AccessRightsBulkQueueItem
  {
    /// <summary>
    /// Приоритеты элементов очереди.
    /// </summary>
    public static class Priorities
    {
      // Приоритет обработки элементов очереди по умолчанию.
      public const int Default = 1;
      
      // Низкий приоритет обработки элементов очереди.
      public const int Low = 0;
    }
  }
}