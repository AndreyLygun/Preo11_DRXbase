using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Projects.Structures.ProjectCore
{
  partial class ProjectMemberRights
  {
    public IRecipient Recipient { get; set; }
           
    public string ProjectRightsType { get; set; }
    
    public string FoldersRightsType { get; set; }
  }
}