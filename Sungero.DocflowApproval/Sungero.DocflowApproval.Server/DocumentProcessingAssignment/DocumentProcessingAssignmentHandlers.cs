using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.DocflowApproval.DocumentProcessingAssignment;

namespace Sungero.DocflowApproval
{
  partial class DocumentProcessingAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (Functions.DocumentProcessingAssignment.AreDocumentsLockedByMe(_obj))
      {
        e.AddError(Resources.SaveDocumentsBeforeComplete);
        return;
      }
      
      var resultText = Sungero.Commons.Resources.Empty;
      if (_obj.Result == Result.ForRework)
      {
        e.Result = DocflowApproval.Resources.ForRework;
        return;
      }
      
      if (_obj.PrintDocument == true)
        resultText.AppendLine(DocumentProcessingAssignments.Resources.DocumentPrinted);
      
      var officialDocument = OfficialDocuments.As(_obj.DocumentGroup.ElectronicDocuments.FirstOrDefault());
      if (officialDocument != null && _obj.RegisterDocument == true)
      {
        var registrationNumber = officialDocument.RegistrationNumber;
        var documentRegister = officialDocument.DocumentRegister;
        if (registrationNumber != null && documentRegister != null)
        {
          var registerName = documentRegister.DisplayName;
          if (registerName.Length > 50)
            registerName = DocumentProcessingAssignments.Resources.RegisterNameCutFormat(registerName.Substring(0, 50));
          resultText.AppendLine(DocumentProcessingAssignments.Resources.DocumentRegisteredFormat(registrationNumber, registerName));
        }
      }
      
      if (_obj.SendToCounterparty == true)
        resultText.AppendLine(DocumentProcessingAssignments.Resources.DocumentSentToCounterparty);
      
      if (_obj.CreateActionItems == true && _obj.PrintDocument != true && _obj.RegisterDocument != true && _obj.SendToCounterparty != true)
        resultText = DocumentProcessingAssignments.Resources.Done;
      
      e.Result = resultText;
    }
  }

}