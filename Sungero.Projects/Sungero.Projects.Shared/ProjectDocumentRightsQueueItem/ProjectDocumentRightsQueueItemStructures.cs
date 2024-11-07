using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Projects.Structures.ProjectDocumentRightsQueueItem
{
  partial class ProxyQueueItem
  {
    public long Id { get; set; }
    
    public Guid Discriminator { get; set; }
    
    public long ProjectId_Project_Sungero { get; set; }
    
    public long DocumentId_Project_Sungero { get; set; }
  }

}