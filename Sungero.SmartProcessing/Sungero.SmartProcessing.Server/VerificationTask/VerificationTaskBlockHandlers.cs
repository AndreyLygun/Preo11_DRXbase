using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.VerificationTask;
using Sungero.Workflow;

namespace Sungero.SmartProcessing.Server.VerificationTaskBlocks
{
  partial class AnalyzeDocumentPackageSeparationBlockHandlers
  {

    public virtual void AnalyzeDocumentPackageSeparationBlockExecute()
    {
      Functions.VerificationTask.CreateNewDocumentRecognitionInfo(_obj);
    }
  }

  partial class VerificationBlockHandlers
  {
    public virtual void VerificationBlockStart()
    {
      // Отправить запрос на подготовку предпросмотра для документов.
      Docflow.PublicFunctions.Module.PrepareAllAttachmentsPreviews(_obj);
      Functions.VerificationTask.PrepareAllAttachmentsRepackingPreviews(_obj);
    }

    public virtual void VerificationBlockStartAssignment(Sungero.SmartProcessing.IVerificationAssignment assignment)
    {
      if (assignment.ForwardedFrom == null)
        _obj.Deadline = assignment.Deadline;
      
      if (_block.GrantRightsByDefault != true)
        return;
      
      foreach (var performer in _block.Performers.Where(p => Company.Employees.Is(p)))
        Functions.VerificationTask.GrantAccessRights(_obj, Company.Employees.As(performer));
    }
    
    public virtual void VerificationBlockCompleteAssignment(Sungero.SmartProcessing.IVerificationAssignment assignment)
    {
      PublicFunctions.VerificationAssignment.ForwardAssigment(assignment, _obj);
    }
    
    public virtual void VerificationBlockEnd(System.Collections.Generic.IEnumerable<Sungero.SmartProcessing.IVerificationAssignment> createdAssignments)
    {
      Functions.VerificationTask.SetCompleteVerificationState(_obj);
    }
    
  }
}