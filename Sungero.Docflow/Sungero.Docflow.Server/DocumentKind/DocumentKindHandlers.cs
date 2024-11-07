using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentKind;

namespace Sungero.Docflow
{
  partial class DocumentKindCreatingFromServerHandler
  {
    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_source.Info.Properties.IsDefault);
    }
  }

  partial class DocumentKindDocumentTypePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentTypeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.NumberingType != NumberingType.NotNumerable)
        query = query.Where(d => d.IsRegistrationAllowed == true);
      
      return query.Where(d => Equals(d.DocumentFlow, _obj.DocumentFlow));
    }
  }

  partial class DocumentKindServerHandlers
  {

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      // Нельзя удалить вид документа МКДО, созданного инициализацией.
      if (Functions.DocumentKind.IsExchangeNativeDocumentKind(_obj))
        e.AddError(DocumentKinds.Resources.CantDelete);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (!_obj.State.IsCopied)
      {
        var contractContext = Domain.Shared.CallContextService.CurrentContext.Contains(frame => frame.FolderId != 0);
        _obj.DocumentFlow = contractContext ? DocumentFlow.Contracts : DocumentFlow.Inner;
        _obj.NumberingType = contractContext ? NumberingType.Registrable : NumberingType.NotNumerable;
        _obj.AutoNumbering = false;
        _obj.ProjectsAccounting = false;
        _obj.GrantRightsToProject = false;
      }
      
      _obj.IsDefault = false;

      // Для нумеруемых и регистрируемых видов по умолчанию ставим автоимя.
      _obj.GenerateDocumentName = _obj.GenerateDocumentName == true || _obj.NumberingType == NumberingType.Registrable || _obj.NumberingType == NumberingType.Numerable;
      
      Functions.DocumentKind.GrantDefaultAccessRightDocumentKind(_obj);
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Валидация срока рассмотрения.
      if (_obj.DeadlineInDays.HasValue && _obj.DeadlineInDays < 1)
        e.AddError(DocumentKinds.Resources.IncorrectDeadline);
      if (_obj.DeadlineInHours.HasValue && _obj.DeadlineInHours < 1)
        e.AddError(DocumentKinds.Resources.IncorrectDeadline);
      if (_obj.NumberingType != NumberingType.Numerable)
        _obj.AutoNumbering = false;
      
      // Нельзя изменить тип документа у видов документов МКДО, созданных инициализацией.
      if (!Equals(_obj.DocumentType, _obj.State.Properties.DocumentType.OriginalValue) && Functions.DocumentKind.IsExchangeNativeDocumentKind(_obj))
        e.AddError(DocumentKinds.Resources.CantChange);
      
      // Обрезать лишние пробелы.
      _obj.Name = _obj.Name.Trim();
      _obj.ShortName = _obj.ShortName.Trim();
      
      if (!string.IsNullOrWhiteSpace(_obj.Code))
      {
        _obj.Code = _obj.Code.Trim();
        if (Regex.IsMatch(_obj.Code, @"\s"))
          e.AddError(_obj.Info.Properties.Code, Docflow.Resources.NoSpacesInCode);
      }
      
      // Проверить код на пробелы, если свойство изменено.
      if (!string.IsNullOrEmpty(_obj.Code))
      {
        // При изменении кода e.AddError сбрасывается.
        var codeIsChanged = _obj.State.Properties.Code.IsChanged;
        _obj.Code = _obj.Code.Trim();
        
        if (codeIsChanged && Regex.IsMatch(_obj.Code, @"\s"))
          e.AddError(_obj.Info.Properties.Code, Company.Resources.NoSpacesInCode);
      }
      
      // Нельзя закрывать вид документа по умолчанию.
      // Проверка выполняется, только если были изменения, т.к. запись могли закрыть до добавления проверки в версии 2.8.
      if (_obj.IsDefault == true && _obj.Status == Status.Closed &&
          (_obj.State.Properties.IsDefault.IsChanged || _obj.State.Properties.Status.IsChanged))
        e.AddError(DocumentKinds.Resources.ClosedDocumentKindCannotBeDefault);
      
      // Нельзя закрывать вид документа, если по нему есть настройки или правила.
      // Проверка выполняется, только если были изменения, т.к. запись могли закрыть до добавления проверки в версии 3.0.
      var canClose = !Functions.Module.GetDocumentKindSettings(_obj).Any();
      if (_obj.Status == Status.Closed && !canClose && _obj.State.Properties.Status.IsChanged)
        e.AddError(DocumentKinds.Resources.CantClose, _obj.Info.Actions.ShowSettings);
      
      if (_obj.IsDefault == true)
      {
        var defaultKinds = DocumentKinds.GetAll(k => k.Status == Status.Active && !Equals(k, _obj) &&
                                                Equals(k.DocumentType, _obj.DocumentType) && k.IsDefault == true);
        foreach (var kind in defaultKinds)
        {
          var lockInfo = Locks.GetLockInfo(kind);
          if (lockInfo != null && lockInfo.IsLocked)
          {
            var error = Commons.Resources.LinkedEntityLockedFormat(
              kind.Name,
              kind.Id,
              lockInfo.OwnerName);
            e.AddError(error);
          }
          
          kind.IsDefault = false;
        }
      }
    }
  }
}