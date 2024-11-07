using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.CancellationAgreement;

namespace Sungero.Exchange.Client
{
  partial class CancellationAgreementCollectionActions
  {
    public override void OpenDocumentEdit(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.OpenDocumentEdit(e);
    }

    public override bool CanOpenDocumentEdit(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }
  }

  partial class CancellationAgreementVersionsActions
  {
    public override bool CanDeleteVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override void DeleteVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.DeleteVersion(e);
    }

    public override void EditVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.EditVersion(e);
    }

    public override bool CanEditVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

  }

  partial class CancellationAgreementActions
  {
    public override void OpenInExchangeService(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.OpenInExchangeService(e);
    }

    public override bool CanOpenInExchangeService(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      // Открыть СоА в сервисах обмена нельзя, вся информация об аннулировании есть в основном документе.
      return false;
    }

    public override void ExportDocument(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ExportDocument(e);
    }

    public override bool CanExportDocument(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CreateCancellationAgreement(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateCancellationAgreement(e);
    }

    public override bool CanCreateCancellationAgreement(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override bool CanShowComparisonResult(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void ShowComparisonResult(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ShowComparisonResult(e);
    }

    public override bool CanCompareDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CompareDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CompareDocuments(e);
    }

    public override void CompareVersions(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CompareVersions(e);
    }

    public override bool CanCompareVersions(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void OpenExchangeOrderReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.OpenExchangeOrderReport(e);
    }

    public override bool CanOpenExchangeOrderReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void ChangeDocumentType(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ChangeDocumentType(e);
    }

    public override bool CanChangeDocumentType(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

  }

}