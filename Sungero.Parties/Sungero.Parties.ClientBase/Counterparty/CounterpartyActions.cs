using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Counterparty;

namespace Sungero.Parties.Client
{
  partial class CounterpartyAnyChildEntityCollectionActions
  {
    public override void DeleteChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.DeleteChildEntity(e);
    }

    public override bool CanDeleteChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      // Дизейбл грида абонентских ящиков.
      var root = Counterparties.As(e.RootEntity);
      return (root != null && _all == root.ExchangeBoxes)
        ? false
        : base.CanDeleteChildEntity(e);
    }

  }

  partial class CounterpartyAnyChildEntityActions
  {
    public override void CopyChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.CopyChildEntity(e);
    }

    public override bool CanCopyChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var root = Counterparties.As(e.RootEntity);
      return (root != null && _all == root.ExchangeBoxes)
        ? false
        : base.CanCopyChildEntity(e);
    }

    public override void AddChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.AddChildEntity(e);
    }

    public override bool CanAddChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var root = Counterparties.As(e.RootEntity);
      return (root != null && _all == root.ExchangeBoxes)
        ? false
        : base.CanAddChildEntity(e);
    }

  }

  partial class CounterpartyExchangeBoxesActions
  {

    public virtual bool CanAcceptInvitation(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return (_obj.Status == CounterpartyExchangeBoxes.Status.ApprovingByUs || _obj.Status == CounterpartyExchangeBoxes.Status.Closed) &&
        _obj.Counterparty.AccessRights.CanUpdate() && _obj.Counterparty.AccessRights.CanSetExchange();
    }

    public virtual void AcceptInvitation(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      // Возобновление обмена возможно только с головной организацией.
      if (!string.IsNullOrEmpty(_obj.CounterpartyBranchId))
      {
        Dialogs.NotifyMessage(ExchangeCore.BusinessUnitBoxes.Resources.BranchOfficeCanNotAcceptInvitation);
        return;
      }
      
      Functions.Counterparty.ShowInvitationDialog(_obj, true);
    }

    public virtual bool CanRejectInvitation(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return _obj.Counterparty.AccessRights.CanUpdate() && _obj.Counterparty.AccessRights.CanSetExchange();
    }

    public virtual void RejectInvitation(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var dialog = Dialogs.CreateInputDialog(Sungero.Parties.Counterparties.Resources.ExchangeDialogCounterpartyRejectTitle);
      dialog.HelpCode = Constants.Counterparty.HelpCodes.RejectExchange;
      
      dialog.Text = Sungero.Parties.Counterparties.Resources.ExchangeDialogCounterpartyRejectInfo;
      dialog.AddSelect(_obj.Info.Properties.Box.LocalizedName, false, _obj.Box).IsEnabled = false;
      dialog.AddString(Sungero.Parties.Counterparties.Resources.ExchangeDialogOrganizationId, false, _obj.OrganizationId).IsEnabled = false;
      
      dialog.AddString(_obj.Info.Properties.CounterpartyBranchId.LocalizedName, false, _obj.CounterpartyBranchId).IsEnabled = false;
      dialog.AddString(_obj.Info.Properties.FtsId.LocalizedName, false, _obj.FtsId).IsEnabled = false;
      
      var closeExchangeButton = dialog.Buttons.AddCustom(Sungero.Parties.Counterparties.Resources.ExchangeDialogCounterpartyRejectStop);
      var deleteExchangeButton = dialog.Buttons.AddCustom(Sungero.Parties.Counterparties.Resources.ExchangeDialogCounterpartyRejectDelete);
      dialog.Buttons.AddCancel();
      dialog.Buttons.Default = DialogButtons.Cancel;
      
      closeExchangeButton.IsEnabled = _obj.Status != CounterpartyExchangeBoxes.Status.Closed;
      
      if (_obj.Box.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis || !string.IsNullOrEmpty(_obj.CounterpartyBranchId))
        closeExchangeButton.IsVisible = false;
      else
        dialog.Text = string.Join(Environment.NewLine, Sungero.Parties.Counterparties.Resources.ExchangeDialogCounterpartyRejectStopInfo, dialog.Text);
      
      dialog.SetOnRefresh(x =>
                          {
                            x.AddWarning(Sungero.Parties.Counterparties.Resources.ExchangeDialogWarning);
                          });
      
      var button = dialog.Show();
      
      if (button == deleteExchangeButton)
      {
        var confirmDiadlog = Dialogs.CreateConfirmDialog(Sungero.Parties.Counterparties.Resources.ExchangeDialogCounterpartyRejectConfirm);
        if (confirmDiadlog.Show())
        {
          var cp = Counterparties.Get(_obj.Counterparty.Id);
          cp.ExchangeBoxes.Remove(_obj);
          // Отключаем событие До сохранения, чтобы не сработала валидация дублей.
          using (EntityEvents.Disable(cp.Info.Events.BeforeSave))
            cp.Save();
          
          Dialogs.NotifyMessage(Sungero.Parties.Counterparties.Resources.ExchangeDialogCounterpartyRejectSuccessFormat(_obj.Box.Name));
        }
        return;
      }
      
      if (button == closeExchangeButton)
      {
        // Прекращение обмена возможно только с головной организацией.
        if (!string.IsNullOrEmpty(_obj.CounterpartyBranchId))
        {
          Dialogs.NotifyMessage(ExchangeCore.BusinessUnitBoxes.Resources.BranchOfficeCanNotRejectInvitation);
          return;
        }
        
        Functions.Counterparty.ShowInvitationDialog(_obj, false);
      }
    }

    public virtual bool CanShowDepartmentBoxes(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return true;
    }

    public virtual void ShowDepartmentBoxes(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      Sungero.ExchangeCore.PublicFunctions.Module.Remote.GetCounterpartyDepartmentBoxes(_obj.Counterparty, _obj.Box, _obj.CounterpartyBranchId).Show();
    }

    public virtual bool CanEditExchange(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return _obj.Counterparty.AccessRights.CanUpdate() && _obj.Counterparty.AccessRights.CanSetExchange();
    }

    public virtual void EditExchange(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var counterpartyOrganizationIdChanged = false;
      var ftsIdChanged = false;
      var counterpartyBranchIdChanged = false;
      
      var dialog = Dialogs.CreateInputDialog(Sungero.Parties.Counterparties.Resources.ExchangeDialogTitle);
      dialog.HelpCode = Constants.Counterparty.HelpCodes.EditExchange;
      
      var box = dialog.AddSelect(_obj.Info.Properties.Box.LocalizedName, true, _obj.Box);
      box.IsEnabled = false;
      
      var counterpartyOrganizationId = dialog.AddString(Sungero.Parties.Counterparties.Resources.ExchangeDialogOrganizationId, true, _obj.OrganizationId);
      
      var counterpartyBranchId = dialog.AddString(_obj.Info.Properties.CounterpartyBranchId.LocalizedName, false, _obj.CounterpartyBranchId);
      
      var ftsId = dialog.AddString(_obj.Info.Properties.FtsId.LocalizedName, true, _obj.FtsId);

      var saveButton = dialog.Buttons.AddCustom(Sungero.Parties.Counterparties.Resources.ExchangeDialogSaveButton);
      dialog.Buttons.AddCancel();
      dialog.Buttons.Default = saveButton;
      
      counterpartyOrganizationId.SetOnValueChanged(
        (args) => {
          counterpartyOrganizationIdChanged = args.NewValue != args.OldValue;
        });
      
      ftsId.SetOnValueChanged(
        (args) => {
          ftsIdChanged = args.NewValue != args.OldValue;
        });

      counterpartyBranchId.SetOnValueChanged(
        (args) => {
          counterpartyBranchIdChanged = args.NewValue != args.OldValue;
        });
      
      dialog.SetOnRefresh(
        (dialogArgs) => {
          saveButton.IsEnabled = counterpartyOrganizationIdChanged || ftsIdChanged || counterpartyBranchIdChanged;
          
          dialogArgs.AddWarning(Sungero.Parties.Counterparties.Resources.ExchangeDialogWarning);
        });

      dialog.SetOnButtonClick(x =>
                              {
                                if (x.Button == saveButton && x.IsValid)
                                {
                                  
                                  // Проверить ИНН/КПП для Сбис.
                                  var organizationIdCorrect = true;
                                  if (ExchangeCore.ExchangeService.ExchangeProvider.Sbis == box.Value?.ExchangeService.ExchangeProvider)
                                  {
                                    var company = Parties.Companies.As(_obj.Counterparty);
                                    var splittedOrgId = counterpartyOrganizationId.Value.Trim().Split('/');
                                    var tin = _obj.Counterparty.TIN;
                                    var trrc = company != null ? company.TRRC : string.Empty;
                                    var newTin = splittedOrgId[0];
                                    var newTrrc = splittedOrgId.Count() == 2 ? splittedOrgId[1] : string.Empty;
                                    if ((tin ?? string.Empty) != newTin || (trrc ?? string.Empty) != newTrrc)
                                      organizationIdCorrect = false;
                                  }

                                  if (!organizationIdCorrect)
                                    x.AddError(Sungero.Parties.Counterparties.Resources.ExchangeDialogCounterpartyChangeIncorrectOrganisationId);
                                  else
                                  {
                                    var result = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.UpdateExchange(box.Value, _obj.Counterparty,
                                                                                                                    counterpartyOrganizationId.Value,
                                                                                                                    ftsId.Value, counterpartyBranchId.Value, _obj.Id);
                                    if (string.IsNullOrEmpty(result))
                                      Dialogs.NotifyMessage(Sungero.Parties.Counterparties.Resources.ExchangeDialogCounterpartyUpdateSuccess);
                                    else
                                      x.AddError(result);
                                  }
                                }
                              });
      dialog.Show();
    }
  }

  partial class CounterpartyActions
  {

    public virtual void ForceDuplicateSave(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      e.Params.AddOrUpdate(Counterparties.Resources.ParameterIsForceDuplicateSaveFormat(_obj.Id), true);
      _obj.Save();
    }

    public virtual bool CanForceDuplicateSave(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ShowCounterpartyDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var documents = Docflow.PublicFunctions.Module.Remote.GetCounterpartyDocuments(_obj);
      documents.Show(_obj.Name);
    }

    public virtual bool CanShowCounterpartyDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicates = Functions.Counterparty.GetDuplicates(_obj, true);
      if (duplicates.Any())
        duplicates.Show();
      else
        Dialogs.NotifyMessage(Parties.Counterparties.Resources.DuplicateNotFound);
    }

    public virtual bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void CanExchange(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var info = Functions.Counterparty.ValidateExchangeAction(_obj, e);
      if (!info.CanDoAction)
        return;
      
      var names = string.Empty;
      if (info.Services.Count < 3)
        names = string.Join(Counterparties.Resources.ExchangeServicesSeparator, info.Services.Select(x => x.Info.Properties.ExchangeProvider.GetLocalizedValue(x.ExchangeProvider)));
      else
      {
        var exchangeServices = info.Services.GetRange(0, 2).Select(x => x.Info.Properties.ExchangeProvider.GetLocalizedValue(x.ExchangeProvider));
        var exchangeServicesNames = string.Join(Counterparties.Resources.ExchangeServicesSeparator, exchangeServices);
        names = Counterparties.Resources.ToManyExchangeServicesFormat(exchangeServices);
      }
      
      var dialog = Dialogs.CreateTaskDialog(Counterparties.Resources.FoundInExchangeServices,
                                            Counterparties.Resources.FoundInExchangeServicesDescriptionFormat(names),
                                            MessageType.Information, _obj.Info.Actions.CanExchange.LocalizedName);
      var send = dialog.Buttons.AddCustom(_obj.Info.Actions.SendInvitation.LocalizedName);
      dialog.Buttons.Default = send;
      dialog.Buttons.AddCancel();
      if (dialog.Show() == send)
      {
        this.SendInvitation(e);
      }
    }

    public virtual bool CanCanExchange(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return this.CanSendInvitation(e);
    }

    public virtual void SendInvitation(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var info = Functions.Counterparty.ValidateExchangeAction(_obj, e);
      if (!info.CanDoAction)
        return;

      var dialog = Dialogs.CreateInputDialog(Counterparties.Resources.InvitationTitle);
      
      var box = dialog.AddSelect(_obj.Info.Properties.ExchangeBoxes.Properties.Box.LocalizedName, true, info.DefaultBox).From(info.Boxes);
      
      var company = CompanyBases.As(_obj);
      var counterpartyInfo = dialog.AddString(Sungero.Parties.Counterparties.Resources.ExchangeDialogCounterpartyInfo, false);
      counterpartyInfo.IsEnabled = false;
      counterpartyInfo.Value = string.Format("{0}/{1}", _obj.TIN, company != null ? company.TRRC : string.Empty);
      
      var counterpartyBranchId = dialog.AddString(Sungero.Parties.Counterparties.Resources.ExchangeDialogCounterpartyBranchId, false);
      var isSbis = Exchange.PublicFunctions.Module.IsSbisExchangeService(box.Value);
      counterpartyBranchId.IsEnabled = isSbis;

      var operatorCode = dialog.AddString(Sungero.Parties.Counterparties.Resources.ExchangeDialogOperatorCode, false);
      operatorCode.IsEnabled = isSbis;
      
      var comment = dialog.AddMultilineString(Counterparties.Resources.InvitationMessageHeader, false,
                                              Counterparties.Resources.InvitationMessageDefault);

      box.SetOnValueChanged((args) => {
                              isSbis = Exchange.PublicFunctions.Module.IsSbisExchangeService(args.NewValue);
                              counterpartyBranchId.IsEnabled = isSbis;
                              operatorCode.IsEnabled = isSbis;
                              
                              counterpartyBranchId.Value = string.Empty;
                              operatorCode.Value = string.Empty;
                            });
      
      dialog.HelpCode = Constants.Counterparty.HelpCodes.SendInvitation;
      var sendButton = dialog.Buttons.AddCustom(Sungero.Parties.Counterparties.Resources.ExchangeDialogButtonSend);
      dialog.Buttons.AddCancel();
      dialog.Buttons.Default = sendButton;
      dialog.SetOnButtonClick(x =>
                              {
                                if (x.Button == sendButton && x.IsValid && e.Validate())
                                {
                                  var operatorCodeValue = !string.IsNullOrEmpty(operatorCode.Value) ? operatorCode.Value.Trim() : operatorCode.Value;
                                  
                                  if (!string.IsNullOrEmpty(operatorCodeValue) && operatorCodeValue.Length != 3)
                                    x.AddError(Sungero.Parties.Counterparties.Resources.ExchangeDialogOperatorCodeError);
                                  else
                                  {
                                    var result = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.SendInvitation(box.Value, _obj, operatorCodeValue, counterpartyBranchId.Value, comment.Value);
                                    if (!string.IsNullOrWhiteSpace(result))
                                      x.AddError(result);
                                    else
                                    {
                                      var counterpartyExchangeBox = _obj.ExchangeBoxes.FirstOrDefault(b => Equals(b.Box, box.Value));
                                      Dialogs.NotifyMessage(counterpartyExchangeBox.Info.Properties.Status.GetLocalizedValue(counterpartyExchangeBox.Status));
                                    }
                                  }
                                }
                              });
      dialog.Show();
    }

    public virtual bool CanSendInvitation(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() && _obj.AccessRights.CanSetExchange();
    }

    public virtual void SearchDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Sungero.Shell.PublicFunctions.Module.SearchDocumentsWithCounterparties(_obj);
    }

    public virtual bool CanSearchDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void WriteLetter(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.WriteLetter(_obj.Email);
    }

    public virtual bool CanWriteLetter(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual bool CanGoToWebsite(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Functions.Module.CanGoToWebsite(_obj.Homepage);
    }

    public virtual void GoToWebsite(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.GoToWebsite(_obj.Homepage, e);
    }

  }

}