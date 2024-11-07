using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractualDocument;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{
  partial class ContractualDocumentMilestonesClientHandlers
  {

    public virtual void MilestonesDaysToFinishWorksValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (_obj.IsCompleted.Value)
        e.AddError(ContractualDocuments.Resources.CannotChangeCompleteMilestone);
      if (e.NewValue <= 0)
        e.AddError(ContractualDocuments.Resources.IncorrectFinishWorksDay);
    }

    public virtual void MilestonesNoteValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (_obj.IsCompleted.Value)
        e.AddError(ContractualDocuments.Resources.CannotChangeCompleteMilestone);
    }

    public virtual void MilestonesPerformerValueInput(Sungero.Contracts.Client.ContractualDocumentMilestonesPerformerValueInputEventArgs e)
    {
      if (_obj.IsCompleted.Value)
        e.AddError(ContractualDocuments.Resources.CannotChangeCompleteMilestone);
      if (_obj.Task != null)
        e.AddError(ContractualDocuments.Resources.CannotChangePerformer);
      
      // Проверить корректность срока.
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(e.NewValue ?? Users.Current, _obj.Deadline);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
    }

    public virtual void MilestonesDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (_obj.IsCompleted.Value)
        e.AddError(ContractualDocuments.Resources.CannotChangeCompleteMilestone);
      
      // Проверить корректность срока.
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(_obj.Performer ?? Users.Current, e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.Performer ?? Users.Current, e.NewValue, Calendar.Today))
        e.AddWarning(ContractualDocuments.Resources.DeadlineMilestoneLessThenToday);
    }

    public virtual void MilestonesNameValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (_obj.IsCompleted.Value)
        e.AddError(ContractualDocuments.Resources.CannotChangeCompleteMilestone);
    }
  }

  partial class ContractualDocumentClientHandlers
  {

    public override void VatRateValueInput(Sungero.Docflow.Client.ContractualDocumentBaseVatRateValueInputEventArgs e)
    {
      Docflow.PublicFunctions.ContractualDocumentBase.FillVatAmount(_obj, _obj.TotalAmount, e.NewValue);
      Docflow.PublicFunctions.ContractualDocumentBase.FillNetAmount(_obj, _obj.TotalAmount, _obj.VatAmount);
      
      base.VatRateValueInput(e);
    }

    public override void NetAmountValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue <= 0)
        e.AddError(Sungero.Docflow.Resources.NetAmountMustBePositive);
      
      base.NetAmountValueInput(e);
    }

    public override void VatAmountValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue < 0)
        e.AddError(Sungero.Docflow.Resources.VatAmountMustBePositive);
      
      if (e.NewValue > _obj.TotalAmount)
        e.AddError(_obj.Info.Properties.VatAmount, Sungero.Docflow.Resources.VatAmountMustBeMoreTotalAmount, _obj.Info.Properties.TotalAmount);
      
      Docflow.PublicFunctions.ContractualDocumentBase.FillNetAmount(_obj, _obj.TotalAmount, e.NewValue);

      base.VatAmountValueInput(e);
    }

    public virtual void CounterpartyRegistrationNumberValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (!string.IsNullOrEmpty(e.NewValue))
        e.NewValue = e.NewValue.Trim();
    }

    public override void TotalAmountValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue < 0)
        e.AddError(Docflow.Resources.TotalAmountMustBePositive);
      
      Docflow.PublicFunctions.ContractualDocumentBase.FillVatAmount(_obj, e.NewValue, _obj.VatRate);
      Docflow.PublicFunctions.ContractualDocumentBase.FillNetAmount(_obj, e.NewValue, _obj.VatAmount);
      
      base.TotalAmountValueInput(e);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      var isCompany = Sungero.Parties.CompanyBases.Is(_obj.Counterparty) || _obj.Counterparty == null;

      _obj.State.Properties.Contact.IsEnabled = isCompany;
      _obj.State.Properties.CounterpartySignatory.IsEnabled = isCompany;
      
      var canChangeVatAmount = !(_obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnApproval ||
                                 _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.PendingSign ||
                                 _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed);
      
      if (canChangeVatAmount && !Docflow.PublicFunctions.ContractualDocumentBase.CheckVatAmount(_obj, _obj.VatAmount))
      {
        e.AddWarning(Sungero.Docflow.Resources.VerifyVatAmount);
        _obj.State.Properties.VatAmount.HighlightColor = Colors.Common.LightYellow;
      }
    }
  }
}