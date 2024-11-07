using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.ExchangeCore.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// Фоновый процесс синхронизации филиалов и подразделений контрагентов.
    /// </summary>
    public virtual void SyncCounterpartyBranches()
    {
      Exchange.PublicFunctions.Module.LogDebugFormat("Execute job SyncCounterpartyBranches.");
      
      var boxes = Functions.BusinessUnitBox.GetConnectedBoxes().ToList();
      foreach (var box in boxes)
      {
        Functions.BusinessUnitBox.SyncBoxCounterpartyBranches(box);
      }
      
      Exchange.PublicFunctions.Module.LogDebugFormat("Done job SyncCounterpartyBranches.");
    }
    
    /// <summary>
    /// Фоновый процесс синхронизации контрагентов.
    /// </summary>
    public virtual void SyncCounterparties()
    {
      Exchange.PublicFunctions.Module.LogDebugFormat("Execute job SyncCounterparties.");
      
      var boxes = Functions.BusinessUnitBox.GetConnectedBoxes().ToList();
      foreach (var box in boxes)
      {
        Functions.BusinessUnitBox.SyncBoxCounterparties(box);
      }
      
      Exchange.PublicFunctions.Module.LogDebugFormat("Done job SyncCounterparties.");
    }
    
    /// <summary>
    /// Фоновый процесс синхронизации ящиков.
    /// </summary>
    public virtual void SyncBoxes()
    {
      Exchange.PublicFunctions.Module.LogDebugFormat("Execute job SyncBoxes.");
      
      var allBoxes = BusinessUnitBoxes.GetAll().ToList();
      var boxes = allBoxes.Where(b => Equals(b.ConnectionStatus, ExchangeCore.BusinessUnitBox.ConnectionStatus.Connected) ||
                                 Equals(b.ConnectionStatus, ExchangeCore.BusinessUnitBox.ConnectionStatus.Error)).ToList();
      foreach (var box in boxes)
      {
        Transactions.Execute(() =>
                             {
                               Functions.BusinessUnitBox.SyncBoxStatus(box);
                               
                               if (box.ConnectionStatus == ExchangeCore.BusinessUnitBox.ConnectionStatus.Connected)
                               {
                                 Functions.BusinessUnitBox.UpdateExchangeServiceCertificates(box);
                                 Functions.DepartmentBox.CreateDepartmentBoxes(box);
                               }
                             });
      }
      
      // Проставить статус соединения в абонентских ящиках подразделений.
      foreach (var box in allBoxes)
        Functions.Module.SetDepartmentBoxConnectionStatus(box);
      
      var sbisBoxes = BusinessUnitBoxes.GetAll(b => b.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis &&
                                               Equals(b.ConnectionStatus, ExchangeCore.BusinessUnitBox.ConnectionStatus.Connected));
      
      foreach (var box in sbisBoxes)
      {
        Transactions.Execute(() =>
                             {
                               Functions.BusinessUnitBox.UpdateServiceFormalizedPoA(box);
                             });
      }
      
      var boxIds = boxes.Select(b => b.Id).ToList();
      boxes = BusinessUnitBoxes.GetAll().Where(b => boxIds.Contains(b.Id)).ToList();
      
      foreach (var box in boxes)
        Functions.BusinessUnitBox.ValidateCertificates(box);
      
      Exchange.PublicFunctions.Module.LogDebugFormat("Done job SyncBoxes.");
    }
  }
}