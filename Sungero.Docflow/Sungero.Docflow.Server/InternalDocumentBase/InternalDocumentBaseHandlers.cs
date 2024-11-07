using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.InternalDocumentBase;
using Sungero.Domain.Shared;

namespace Sungero.Docflow
{
  partial class InternalDocumentBaseConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      e.Without(Sungero.Docflow.Addendums.Info.Properties.LeadingDocument);
      
      // При смене типа с вх. документа эл. обмена, а также с финансовых и договорных документов
      // дополнить примечание информацией об основании подписания со стороны контрагента.
      var sourceOfficialDocument = OfficialDocuments.As(_source);
      if (sourceOfficialDocument != null)
      {
        var note = PublicFunctions.OfficialDocument.GetNoteWithCounterpartySigningReason(sourceOfficialDocument);
        
        e.Map(_info.Properties.Note, note);
      }
    }
  }

  partial class InternalDocumentBaseFilteringServerHandler<T>
  {
    /// <summary>
    /// Признак папки "Внутренние документы".
    /// </summary>
    /// <returns>True, для внутренних документов, False для наследников.</returns>
    private bool IsInternalDocumentBase()
    {
      // HACK Zamerov: 35180, чтобы в наследниках не фильтровало.
      return Equals(typeof(T), typeof(IInternalDocumentBase));
    }
    
    public virtual IQueryable<Sungero.Company.IDepartment> DepartmentFiltering(IQueryable<Sungero.Company.IDepartment> query, Sungero.Domain.FilteringEventArgs e)
    {
      return query;
    }

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> DocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query, Sungero.Domain.FilteringEventArgs e)
    {
      // Вызов из списка внутренних документов.
      if (this.IsInternalDocumentBase())
      {
        // Не выводить в списке:
        
        // - не нумеруемые документы,
        query = query.Where(d => d.NumberingType != DocumentKind.NumberingType.NotNumerable && d.DocumentType.IsRegistrationAllowed == true);
        
        // - приложения,
        var addendumTypeGuid = Server.Addendum.ClassTypeGuid.ToString();
        query = query.Where(d => d.DocumentType.DocumentTypeGuid != addendumTypeGuid);
        
        // - документы по контрагенту,
        var counterpartyDocumentTypeGuid = Server.CounterpartyDocument.ClassTypeGuid.ToString();
        query = query.Where(d => d.DocumentType.DocumentTypeGuid != counterpartyDocumentTypeGuid);
        
        // - приказы,
        var orderTypeGuid = typeof(RecordManagement.IOrder).GetEntityMetadata().NameGuid.ToString();
        query = query.Where(d => d.DocumentType.DocumentTypeGuid != orderTypeGuid);
        
        // - распоряжения,
        var companyDirectiveTypeGuid = typeof(RecordManagement.ICompanyDirective).GetEntityMetadata().NameGuid.ToString();
        query = query.Where(d => d.DocumentType.DocumentTypeGuid != companyDirectiveTypeGuid);
        
        // - служебные записки,
        var memoTypeGuid = Server.Memo.ClassTypeGuid.ToString();
        query = query.Where(d => d.DocumentType.DocumentTypeGuid != memoTypeGuid);
        
        // - доверенности,
        var powerOfAttorneyTypeGuid = Server.PowerOfAttorney.ClassTypeGuid.ToString();
        query = query.Where(d => d.DocumentType.DocumentTypeGuid != powerOfAttorneyTypeGuid);
        
        // - эл. доверенности,
        var formalizedPowerOfAttorneyTypeGuid = Server.FormalizedPowerOfAttorney.ClassTypeGuid.ToString();
        query = query.Where(d => d.DocumentType.DocumentTypeGuid != formalizedPowerOfAttorneyTypeGuid);
        
        // - заявления на отзыв доверенности.
        var powerOfAttorneyRevocationTypeGuid = Server.PowerOfAttorneyRevocation.ClassTypeGuid.ToString();
        query = query.Where(d => d.DocumentType.DocumentTypeGuid != powerOfAttorneyRevocationTypeGuid);
      }
      
      var kinds = Functions.DocumentKind.GetAvailableDocumentKinds(typeof(T));
      return query.Where(d => d.Status == CoreEntities.DatabookEntry.Status.Active &&
                         d.DocumentType.DocumentFlow == DocumentType.DocumentFlow.Inner && kinds.Contains(d));
    }

    public virtual IQueryable<Sungero.Docflow.IDocumentRegister> DocumentRegisterFiltering(IQueryable<Sungero.Docflow.IDocumentRegister> query, Sungero.Domain.FilteringEventArgs e)
    {
      return Functions.DocumentRegister.GetAvailableDocumentRegisters(DocumentRegister.DocumentFlow.Inner);
    }
    
    public override IQueryable<T> PreFiltering(IQueryable<T> query, Sungero.Domain.PreFilteringEventArgs e)
    {
      if (_filter == null)
        return base.PreFiltering(query, e);
      
      if (Functions.Module.UsePrefilterInternalDocuments(_filter))
      {
        query = Functions.Module.InternalDocumentsApplyStrongFilter(query, _filter).Cast<T>();
        query = Functions.Module.InternalDocumentsApplyOrdinaryFilter(query, _filter).Cast<T>();
      }
      return query;
    }
    
    /// <summary>
    /// Фильтрация списка внутренних документов.
    /// </summary>
    /// <param name="query">Фильтруемый список внутренних документов.</param>
    /// <param name="e">Аргументы события фильтрации.</param>
    /// <returns>Список внутренних документов с примененными фильтрами.</returns>
    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return base.Filtering(query, e);
      
      if (this.IsInternalDocumentBase())
      {
        // Не выводить в списке:
        // - не нумеруемые документы,
        // - приложения,
        // - документы по контрагенту,
        // - приказы/распоряжения,
        // - служебные записки,
        // - доверенности/эл. доверенности,
        // - заявления на отзыв доверенности.
        query = query.Where(d => d.DocumentKind.NumberingType != DocumentKind.NumberingType.NotNumerable &&
                            !Addendums.Is(d) &&
                            !CounterpartyDocuments.Is(d) &&
                            !RecordManagement.OrderBases.Is(d) &&
                            !Memos.Is(d) &&
                            !PowerOfAttorneyBases.Is(d) &&
                            !PowerOfAttorneyRevocations.Is(d));
      }

      if (!Functions.Module.UsePrefilterInternalDocuments(_filter))
        query = Functions.Module.InternalDocumentsApplyOrdinaryFilter(query, _filter).Cast<T>();
      
      query = Functions.Module.InternalDocumentsApplyWeakFilter(query, _filter).Cast<T>();
      
      return query;
    }
  }

  partial class InternalDocumentBaseDocumentKindPropertyFilteringServerHandler<T>
  {
    public override IQueryable<T> DocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.DocumentKindFiltering(query, e);
      
      // Отфильтровать внутренние виды документов.
      return query.Where(k => k.DocumentFlow.Value == Docflow.DocumentKind.DocumentFlow.Inner);
    } 
  }
}
