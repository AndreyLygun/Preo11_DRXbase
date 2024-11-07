using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.DocumentKind;

namespace Sungero.RecordManagement.Server
{
  partial class OrdersCompanyDirectivesFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IDocumentRegister> OrdersCompanyDirectivesDocumentRegisterFiltering(IQueryable<Sungero.Docflow.IDocumentRegister> query)
    {
      return Docflow.PublicFunctions.DocumentRegister.GetAvailableDocumentRegisters(Docflow.DocumentRegister.DocumentFlow.Inner);
    }

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> OrdersCompanyDirectivesDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      var kinds = Docflow.PublicFunctions.DocumentKind.GetAvailableDocumentKinds(typeof(Sungero.RecordManagement.IOrderBase));
      return query.Where(d => d.Status == CoreEntities.DatabookEntry.Status.Active &&
                         d.DocumentType.DocumentFlow == Docflow.DocumentType.DocumentFlow.Inner &&
                         kinds.Contains(d));
    }
    
    public virtual IQueryable<Sungero.RecordManagement.IOrderBase> OrdersCompanyDirectivesPreFiltering(IQueryable<Sungero.RecordManagement.IOrderBase> query)
    {
      if (_filter == null)
        return query;
      
      if (Functions.Module.UsePrefilterOrdersCompanyDirectives(_filter))
      {
        query = Functions.Module.OrdersCompanyDirectivesApplyStrongFilter(query, _filter);
        query = Functions.Module.OrdersCompanyDirectivesApplyOrdinaryFilter(query, _filter);
      }
      
      return query;
    }

    public virtual IQueryable<Sungero.RecordManagement.IOrderBase> OrdersCompanyDirectivesDataQuery(IQueryable<Sungero.RecordManagement.IOrderBase> query)
    {
      if (_filter == null)
        return query;
      
      if (!Functions.Module.UsePrefilterOrdersCompanyDirectives(_filter))
        query = Functions.Module.OrdersCompanyDirectivesApplyOrdinaryFilter(query, _filter);
      
      query = Functions.Module.OrdersCompanyDirectivesApplyWeakFilter(query, _filter);
      
      return query;
    }
  }

  partial class DocumentsToReturnFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> DocumentsToReturnDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      return query.Where(d => d.Status == CoreEntities.DatabookEntry.Status.Active &&
                         d.DocumentFlow != Docflow.DocumentKind.DocumentFlow.Contracts &&
                         d.DocumentType.DocumentTypeGuid != Docflow.PublicConstants.AccountingDocumentBase.IncomingInvoiceGuid &&
                         d.DocumentType.DocumentTypeGuid != Docflow.PublicConstants.AccountingDocumentBase.IncomingTaxInvoiceGuid &&
                         d.DocumentType.DocumentTypeGuid != Docflow.PublicConstants.AccountingDocumentBase.OutcomingTaxInvoiceGuid);
    }

    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> DocumentsToReturnDataQuery(IQueryable<Sungero.Docflow.IOfficialDocument> query)
    {
      var today = Calendar.UserToday;
      var documents = query.Where(l => l.IsReturnRequired == true || l.IsHeldByCounterParty == true);
      
      documents = documents.Where(d => !Docflow.ContractualDocumentBases.Is(d) && !Docflow.AccountingDocumentBases.Is(d));
      
      if (_filter == null)
        return documents;

      // Фильтр по статусу.
      if (_filter.Overdue)
      {
        documents = documents.Where(l => l.Tracking.Any(d => d.ReturnDate > d.ReturnDeadline ||
                                                        (!d.ReturnDate.HasValue && d.ReturnDeadline < today)));
      }
      
      // Фильтр по виду документа.
      if (_filter.DocumentKind != null)
      {
        documents = documents.Where(l => Equals(l.DocumentKind, _filter.DocumentKind));
      }
      
      // Фильтр по сотруднику.
      if (_filter.Employee != null)
      {
        documents = documents.Where(l => l.Tracking.Any(d => Equals(d.DeliveredTo, _filter.Employee)));
      }
      
      // Фильтр по подразделению.
      if (_filter.Department != null)
      {
        documents = documents.Where(l => l.Tracking.Any(d => Equals(d.DeliveredTo.Department, _filter.Department)));
      }

      // Фильтр по группе регистрации.
      if (_filter.RegistrationGroup != null)
      {
        documents = documents.Where(l => l.DocumentRegister != null &&
                                    Equals(l.DocumentRegister.RegistrationGroup, _filter.RegistrationGroup));
      }
      
      // Фильтр по делу.
      if (_filter.Filelist != null)
      {
        documents = documents.Where(l => Equals(l.CaseFile, _filter.Filelist));
      }
      
      // Исключить строки из таблиц Выдачи с Результатом возврата: "Возвращен".
      var returned = Docflow.OfficialDocumentTracking.ReturnResult.Returned;

      // Фильтр по сроку возврата: до конца дня.
      if (_filter.EndDay)
        documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) && p.ReturnDeadline < today.AddDays(1)));

      // Фильтр по сроку возврата: до конца недели.
      if (_filter.EndWeek)
        documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) && p.ReturnDeadline <= today.EndOfWeek()));

      // Фильтр по сроку возврата: до конца месяца.
      if (_filter.EndMonth)
        documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) && p.ReturnDeadline <= today.EndOfMonth()));

      // Фильтр по сроку возврата: в период с, по.
      if (_filter.Manual)
      {
        var dateFrom = _filter.ReturnPeriodDataRangeFrom;
        var dateTo = _filter.ReturnPeriodDataRangeTo;
        
        if (dateFrom.HasValue && !dateTo.HasValue)
          documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) && p.ReturnDeadline >= dateFrom.Value));
        
        if (dateTo.HasValue && !dateFrom.HasValue)
          documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) && p.ReturnDeadline <= dateTo.Value));
        
        if (dateFrom.HasValue && dateTo.HasValue)
          documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) &&
                                                          (p.ReturnDeadline >= dateFrom.Value &&
                                                           p.ReturnDeadline <= dateTo.Value)));
      }
      
      return documents;
    }
  }

  partial class ActionItemsFolderHandlers
  {

    public virtual IQueryable<Sungero.Workflow.ITask> ActionItemsDataQuery(IQueryable<Sungero.Workflow.ITask> query)
    {
      query = query.Where(t => ActionItemExecutionTasks.Is(t));
      if (_filter == null)
        return Functions.Module.ApplyCommonSubfolderFilters(query);
      
      // Фильтры по статусу и периоду.
      query = Functions.Module.ApplyCommonSubfolderFilters(query, _filter.InProcess,
                                                           _filter.Last30Days, _filter.Last90Days, _filter.Last180Days, false);
      return query;
    }

    public virtual bool IsActionItemsVisible()
    {
      return Docflow.PublicFunctions.Module.IncludedInBusinessUnitHeadsRole() ||
        Docflow.PublicFunctions.Module.IncludedInDepartmentManagersRole() ||
        Docflow.PublicFunctions.Module.Remote.IncludedInClerksRole();
    }
  }

  partial class ForExecutionFolderHandlers
  {

    public virtual IQueryable<Sungero.Workflow.IAssignmentBase> ForExecutionDataQuery(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      var result = query.Where(a => ActionItemExecutionAssignments.Is(a));
      
      // Запрос количества непрочитанных без фильтра.
      if (_filter == null)
        return Functions.Module.ApplyCommonSubfolderFilters(result);
      
      // Фильтры по статусу, замещению и периоду.
      result = Functions.Module.ApplyCommonSubfolderFilters(result, _filter.InProcess,
                                                            _filter.Last30Days, _filter.Last90Days, _filter.Last180Days, false);
      
      return result;
    }

    public virtual bool IsForExecutionVisible()
    {
      return !Docflow.PublicFunctions.Module.IncludedInBusinessUnitHeadsRole();
    }
  }
}