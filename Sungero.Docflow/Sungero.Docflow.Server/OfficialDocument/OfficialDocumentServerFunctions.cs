using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CommonLibrary;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.Server;
using Sungero.Docflow.ApprovalStage;
using Sungero.Docflow.OfficialDocument;
using Sungero.DocflowApproval;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using Sungero.Parties;
using Sungero.Workflow;
using DeclensionCase = Sungero.Core.DeclensionCase;
using HistoryOperation = Sungero.Docflow.Structures.OfficialDocument.HistoryOperation;
using ReturnResult = Sungero.Docflow.OfficialDocumentTracking.ReturnResult;
using Tags = Sungero.Docflow.Constants.OfficialDocument.MarkTags;

namespace Sungero.Docflow.Server
{
  partial class OfficialDocumentFunctions
  {
    
    #region Преобразование в PDF с отметками
    
    #region Методы алгоритма преобразования
    
    /// <summary>
    /// Преобразовать документ в PDF с простановкой отметок.
    /// </summary>
    /// <param name="versionId">ИД версии, на которую будут проставлены отметки.</param>
    /// <returns>Результат преобразования.</returns>
    [Remote]
    public virtual Structures.OfficialDocument.IConversionToPdfResult ConvertToPdfWithMarks(long versionId)
    {
      return Functions.Module.ConvertToPdfWithMarks(_obj, versionId);
    }
    
    /// <summary>
    /// Сохранить тело документа после преобразования в PDF с отметками.
    /// </summary>
    /// <param name="documentWithMarks">Структура с преобразованным телом документа.</param>
    public virtual void SaveConvertedBody(Structures.Module.IDocumentMarksDto documentWithMarks)
    {
      Functions.Module.SaveConvertedBody(_obj, documentWithMarks);
    }
    
    /// <summary>
    /// Записать результат преобразования документа.
    /// </summary>
    /// <param name="documentWithMarks">Структура с преобразованным телом документа.</param>
    public virtual void WriteConvertedBodyToVersion(Structures.Module.IDocumentMarksDto documentWithMarks)
    {
      var version = _obj.Versions.FirstOrDefault(x => x.Id == documentWithMarks.VersionId);
      if (version == null)
      {
        Functions.OfficialDocument.LogPdfConversion(_obj, documentWithMarks.VersionId, "Cannot save converted body, version not found");
        throw AppliedCodeException.Create("Cannot save converted body, version not found");
      }
      version.PublicBody.Write(documentWithMarks.Body);
      version.AssociatedApplication = Content.AssociatedApplications.GetByExtension(Sungero.Docflow.Constants.OfficialDocument.PdfExtension);
    }
    
    /// <summary>
    /// Получить имя параметра для записи в историю о преобразовании.
    /// </summary>
    /// <returns>Имя параметра.</returns>
    public virtual string GetHistoryCommentParamName()
    {
      return PublicConstants.OfficialDocument.AddHistoryCommentAboutPDFConvert;
    }
    
    #endregion
    
    #region RegistrationNumberMark
    
    /// <summary>
    /// Получить отметку о номере регистрации документа.
    /// </summary>
    /// <returns>Отметка о номере регистрации документа.</returns>
    /// <remarks>Если отметки о номере регистрации документа не существует, то будет создана отметка для простановки по тэгам.</remarks>
    [Remote, Public]
    public virtual IMark GetOrCreateRegistrationNumberMark()
    {
      var regNumberMark = this.GetVersionMarks(_obj.LastVersion.Id, Constants.MarkKind.RegistrationNumberMarkKindSid).FirstOrDefault();
      if (regNumberMark == null)
        regNumberMark = this.GetOrCreateTagBasedRegistrationNumberMark();
      return regNumberMark;
    }
    
    /// <summary>
    /// Получить отметку о номере регистрации документа для простановки по тэгам.
    /// </summary>
    /// <returns>Отметка о номере регистрации документа для простановки по тэгам.</returns>
    /// <remarks>Если отметки о номере регистрации не существует, то она будет создана.</remarks>
    [Public, Remote]
    public virtual IMark GetOrCreateTagBasedRegistrationNumberMark()
    {
      var regNumberMark = this.GetOrCreateTagBasedMark(
        Constants.MarkKind.RegistrationNumberMarkKindSid,
        new List<string>() { Tags.RegistrationNumberEn, Tags.RegistrationNumberRu });
      var markLogView = $"Mark(Id={regNumberMark.Id} Tags=[{string.Join(",", regNumberMark.Tags.Select(x => x.Tag))}])";
      Functions.OfficialDocument.LogPdfConversion(_obj, _obj.LastVersion.Id, $"Registration number mark created {markLogView}");
      return regNumberMark;
    }
    
    /// <summary>
    /// Получить отметку о номере регистрации документа для простановки по координатам от левого верхнего угла документа.
    /// </summary>
    /// <param name="page">Страница.</param>
    /// <param name="leftIndent">Отступ слева, см.</param>
    /// <param name="topIndent">Отступ сверху, см.</param>
    /// <returns>Отметка о номере регистрации документа для простановки по координатам от левого верхнего угла документа.</returns>
    /// <remarks>Если отметки о номере регистрации не существует, то она будет создана.</remarks>
    [Public, Remote]
    public virtual IMark GetOrCreateLeftTopCoordinateBasedRegistrationNumberMark(int page, double leftIndent, double topIndent)
    {
      var regNumberMark = this.GetOrCreateLeftTopCoordinateBasedMark(Constants.MarkKind.RegistrationNumberMarkKindSid, page, leftIndent, topIndent);
      var markLogView = $"Mark(Id={regNumberMark.Id} Page={regNumberMark.Page} XIndent={regNumberMark.XIndent} YIndent={regNumberMark.YIndent})";
      Functions.OfficialDocument.LogPdfConversion(_obj, _obj.LastVersion.Id, $"Registration number mark created {markLogView}");
      return regNumberMark;
    }
    
    /// <summary>
    /// Получить содержание отметки о регистрационном номере документа для проставления по тегам.
    /// </summary>
    /// <param name="versionId">ИД версии документа.</param>
    /// <returns>Содержание отметки о регистрационном номере документа.</returns>
    /// <remarks>Используется в методе GetContent вида отметки "Регистрационный номер". ИД версии - потенциальная точка расширения для перекрытий.</remarks>
    public virtual string GetRegistrationNumberMarkContent(long versionId)
    {
      return _obj.RegistrationNumber;
    }
    
    /// <summary>
    /// Удалить отметку о регистрационном номере документа.
    /// </summary>
    [Public, Remote]
    public virtual void DeleteRegistrationNumberMark()
    {
      this.DeleteVersionMark(_obj.LastVersion.Id, Constants.MarkKind.RegistrationNumberMarkKindSid);
    }
    
    #endregion
    
    #region RegistrationDateMark
    
    /// <summary>
    /// Получить экземпляр отметки о дате регистрации документа.
    /// </summary>
    /// <returns>Отметка о дате регистрации документа.</returns>
    /// <remarks>Если отметки о дате регистрации документа не существует, то будет создана отметка для простановки по тэгам.</remarks>
    [Remote, Public]
    public virtual IMark GetOrCreateRegistrationDateMark()
    {
      var regDateMark = this.GetVersionMarks(_obj.LastVersion.Id, Constants.MarkKind.RegistrationDateMarkKindSid).FirstOrDefault();
      if (regDateMark == null)
        regDateMark = this.GetOrCreateTagBasedRegistrationDateMark();
      return regDateMark;
    }
    
    /// <summary>
    /// Получить отметку о дате регистрации документа для простановки по тэгам.
    /// </summary>
    /// <returns>Отметка о дате регистрации документа для простановки по тэгам.</returns>
    /// <remarks>Если отметки о дате регистрации не существует, то она будет создана.</remarks>
    [Public, Remote]
    public virtual IMark GetOrCreateTagBasedRegistrationDateMark()
    {
      var regDateMark = this.GetOrCreateTagBasedMark(
        Constants.MarkKind.RegistrationDateMarkKindSid,
        new List<string>() { Tags.RegistrationDateEn, Tags.RegistrationDateRu });
      Functions.Mark.AddAdditionalParameter(regDateMark, Constants.OfficialDocument.DocxTagUpdaterDateTimeFormatParamName, Constants.Module.ShortDateFormat);
      var markLogView = $"Mark(Id={regDateMark.Id} Tags=[{string.Join(",", regDateMark.Tags.Select(x => x.Tag))}])";
      Functions.OfficialDocument.LogPdfConversion(_obj, _obj.LastVersion.Id, $"Registration date mark created {markLogView}");
      return regDateMark;
    }
    
    /// <summary>
    /// Получить отметку о дате регистрации документа для простановки по координатам от левого верхнего угла документа.
    /// </summary>
    /// <param name="page">Страница.</param>
    /// <param name="leftIndent">Отступ слева, см.</param>
    /// <param name="topIndent">Отступ сверху, см.</param>
    /// <returns>Отметка о дате регистрации документа для простановки по координатам от левого верхнего угла документа.</returns>
    /// <remarks>Если отметки о дате регистрации не существует, то она будет создана.</remarks>
    [Public, Remote]
    public virtual IMark GetOrCreateLeftTopCoordinateBasedRegistrationDateMark(int page, double leftIndent, double topIndent)
    {
      var regDateMark = this.GetOrCreateLeftTopCoordinateBasedMark(Constants.MarkKind.RegistrationDateMarkKindSid, page, leftIndent, topIndent);
      var markLogView = $"Mark(Id={regDateMark.Id} Page={regDateMark.Page} XIndent={regDateMark.XIndent} YIndent={regDateMark.YIndent})";
      Functions.OfficialDocument.LogPdfConversion(_obj, _obj.LastVersion.Id, $"Registration date mark created {markLogView}");
      return regDateMark;
    }
    
    /// <summary>
    /// Получить дату регистрации документа в виде строки.
    /// </summary>
    /// <param name="versionId">ИД версии документа.</param>
    /// <returns>Дата регистрации документа в виде строки.</returns>
    /// <remarks>Используется в методе GetContent вида отметки "Дата регистрации". ИД версии - потенциальная точка расширения для перекрытий.</remarks>
    public virtual string GetRegistrationDateMarkContent(long versionId)
    {
      return _obj.RegistrationDate.Value.ToString(Constants.Module.ShortDateFormat);
    }
    
    /// <summary>
    /// Удалить отметку о дате регистрации документа.
    /// </summary>
    [Public, Remote]
    public virtual void DeleteRegistrationDateMark()
    {
      this.DeleteVersionMark(_obj.LastVersion.Id, Constants.MarkKind.RegistrationDateMarkKindSid);
    }
    
    #endregion
    
    #region SignatureMark
    
    /// <summary>
    /// Получить экземпляр отметки об электронной подписи документа.
    /// </summary>
    /// <returns>Отметка об электронной подписи документа.</returns>
    /// <remarks>Если отметки об электронной подписи документа не существует, то будет создана отметка для простановки по якорю.</remarks>
    [Public, Remote]
    public virtual IMark GetOrCreateSignatureMark()
    {
      var signatureMark = this.GetVersionMarks(_obj.LastVersion.Id, Constants.MarkKind.ElectronicSignatureMarkKindSid).FirstOrDefault();
      if (signatureMark == null)
        signatureMark = this.GetOrCreateAnchorBasedSignatureMark();
      return signatureMark;
    }
    
    /// <summary>
    /// Получить отметку об электронной подписи документа для простановки по якорю.
    /// </summary>
    /// <returns>Отметка об электронной подписи документа для простановки по якорю.</returns>
    /// <remarks>Если отметки об электронной подписи не существует, то она будет создана.</remarks>
    [Public, Remote]
    public virtual IMark GetOrCreateAnchorBasedSignatureMark()
    {
      var signatureMark = this.GetOrCreateAnchorBasedMark(Constants.MarkKind.ElectronicSignatureMarkKindSid, Docflow.Resources.SignatureMarkAnchorSymbol);
      var markLogView = $"Mark(Id={signatureMark.Id} Anchor={signatureMark.Anchor})";
      Functions.OfficialDocument.LogPdfConversion(_obj, _obj.LastVersion.Id, $"Signature mark created {markLogView}");
      return signatureMark;
    }
    
    /// <summary>
    /// Удалить отметку об электронной подписи документа.
    /// </summary>
    [Public, Remote]
    public virtual void DeleteSignatureMark()
    {
      this.DeleteVersionMark(_obj.LastVersion.Id, Constants.MarkKind.ElectronicSignatureMarkKindSid);
    }
    
    #endregion
    
    /// <summary>
    /// Получить отметку документа для простановки по координатам от левого верхнего угла документа.
    /// </summary>
    /// <param name="markKindSid">Sid вида отметки.</param>
    /// <param name="page">Страница.</param>
    /// <param name="leftIndent">Отступ слева, см.</param>
    /// <param name="topIndent">Отступ сверху, см.</param>
    /// <returns>Отметка документа для простановки по координатам от левого верхнего угла документа.</returns>
    /// <remarks>Если отметки не существует, то она будет создана.</remarks>
    [Public, Remote]
    public virtual IMark GetOrCreateLeftTopCoordinateBasedMark(string markKindSid, int page, double leftIndent, double topIndent)
    {
      var mark = this.GetOrCreateMark(markKindSid);
      mark.Tags.Clear();
      mark.Anchor = null;
      mark.Page = page;
      Functions.Mark.FillXIndentFromLeft(mark, leftIndent);
      Functions.Mark.FillYIndentFromTop(mark, topIndent);
      return mark;
    }
    
    /// <summary>
    /// Получить отметку документа для простановки по якорю.
    /// </summary>
    /// <param name="markKindSid">Sid вида отметки.</param>
    /// <param name="anchor">Якорь.</param>
    /// <returns>Отметка документа для простановки по якорю.</returns>
    /// <remarks>Если отметки не существует, то она будет создана.</remarks>
    [Public, Remote]
    public virtual IMark GetOrCreateAnchorBasedMark(string markKindSid, string anchor)
    {
      var mark = this.GetOrCreateMark(markKindSid);
      mark.Tags.Clear();
      Functions.Mark.UpdateAnchor(mark, anchor);
      return mark;
    }
    
    /// <summary>
    /// Получить отметку документа для простановки по тэгам.
    /// </summary>
    /// <param name="markKindSid">Sid вида отметки.</param>
    /// <param name="tags">Тэги.</param>
    /// <returns>Отметка документа для простановки по тэгам.</returns>
    /// <remarks>Если отметки не существует, то она будет создана.</remarks>
    [Public, Remote]
    public virtual IMark GetOrCreateTagBasedMark(string markKindSid, List<string> tags)
    {
      var mark = this.GetOrCreateMark(markKindSid);
      Functions.Mark.UpdateTags(mark, tags);
      return mark;
    }
    
    /// <summary>
    /// Получить отметку документа определённого вида.
    /// </summary>
    /// <param name="markKindSid">Sid вида отметки.</param>
    /// <returns>Отметка указанного вида.</returns>
    /// <remarks>Если отметки указанного вида не существует, то она будет создана.</remarks>
    public virtual IMark GetOrCreateMark(string markKindSid)
    {
      var markKind = Functions.MarkKind.GetMarkKind(markKindSid);
      return this.GetOrCreateMark(markKind);
    }
    
    /// <summary>
    /// Получить отметку документа определённого вида.
    /// </summary>
    /// <param name="markKind">Вид отметки.</param>
    /// <returns>Отметка указанного вида.</returns>
    /// <remarks>Если отметки указанного вида не существует, то она будет создана.</remarks>
    public virtual IMark GetOrCreateMark(IMarkKind markKind)
    {
      var mark = this.GetVersionMarks(_obj.LastVersion.Id, markKind).FirstOrDefault();
      if (mark == null)
      {
        mark = Marks.Create();
        mark.DocumentId = _obj.Id;
        mark.VersionId = _obj.LastVersion.Id;
        mark.MarkKind = markKind;
      }
      return mark;
    }
    
    /// <summary>
    /// Удалить отметку документа.
    /// </summary>
    /// <param name="versionId">ИД версии.</param>
    /// <param name="markKindSid">Sid вида отметки.</param>
    [Public, Remote]
    public virtual void DeleteVersionMark(long versionId, string markKindSid)
    {
      var mark = this.GetVersionMarks(versionId, markKindSid).FirstOrDefault();
      this.DeleteVersionMark(mark);
    }
    
    /// <summary>
    /// Удалить отметку документа.
    /// </summary>
    /// <param name="mark">Отметка.</param>
    [Public, Remote]
    public virtual void DeleteVersionMark(IMark mark)
    {
      Docflow.PublicFunctions.Module.DeleteMark(_obj, mark);
    }
    
    /// <summary>
    /// Получить отметки для версии документа.
    /// </summary>
    /// <param name="versionId">ИД версии документа.</param>
    /// <param name="markKindSid">Sid вида отметки.</param>
    /// <returns>Отметки указанного вида для версии документа.</returns>
    [Public, Remote(IsPure = true)]
    public virtual List<IMark> GetVersionMarks(long versionId, string markKindSid)
    {
      var markKind = Functions.MarkKind.GetMarkKind(markKindSid);
      return this.GetVersionMarks(versionId, markKind);
    }
    
    /// <summary>
    /// Получить отметки для версии документа.
    /// </summary>
    /// <param name="versionId">ИД версии документа.</param>
    /// <param name="markKind">Вид отметки.</param>
    /// <returns>Отметки указанного вида для версии документа.</returns>
    [Public, Remote(IsPure = true)]
    public virtual List<IMark> GetVersionMarks(long versionId, IMarkKind markKind)
    {
      return this.GetVersionMarks(versionId).Where(x => Equals(x.MarkKind, markKind)).ToList();
    }
    
    /// <summary>
    /// Получить сохранённые отметки для версии.
    /// </summary>
    /// <param name="versionId">ИД версии документа.</param>
    /// <returns>Список отметок.</returns>
    [Public, Remote(IsPure = true)]
    public virtual List<IMark> GetVersionMarks(long versionId)
    {
      // Удаленные ранее в текущем сеансе работы отметки все равно сохраняются в кеше и возвращаются GetAll.
      // ИД удаленных отметок передается через Params и они исключаются из выборки.
      var deletedMarkIds = new List<long>();
      var paramName = Sungero.Docflow.Constants.OfficialDocument.DeletedMarkIdsParamName;
      var value = Commons.PublicFunctions.Module.GetEntityParamsValue(_obj, paramName);
      if (value != null)
        deletedMarkIds = (List<long>)Commons.PublicFunctions.Module.GetEntityParamsValue(_obj, paramName);

      return Marks.GetAll(m => m.DocumentId == _obj.Id && m.VersionId == versionId && !deletedMarkIds.Contains(m.Id)).ToList();
    }
    
    #endregion

    #region Контрол "Состояние"

    /// <summary>
    /// Построить модель состояния документа.
    /// </summary>
    /// <returns>Контрол состояния.</returns>
    [Remote(IsPure = true)]
    [LocalizeFunction("GetStateViewXmlFunctionName", "GetStateViewXmlFunctionDescription")]
    public Sungero.Core.StateView GetStateViewXml()
    {
      // Переполучить электронный документ для отображения ПО, когда документ еще не сохранен (после смены типа).
      var document = ElectronicDocuments.GetAll(a => a.Id == _obj.Id).FirstOrDefault();
      if (document != null)
        return GetStateView(document);
      
      return GetStateView(_obj);
    }
    
    /// <summary>
    /// Построить модель состояния документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Схема модели состояния.</returns>
    /// <remarks>По идее, одноименная функция ожидается у всех сущностей, которым нужно представление состояния.</remarks>
    [Public]
    public static Sungero.Core.StateView GetStateView(IElectronicDocument document)
    {
      var stateView = StateView.Create();
      stateView.AddDefaultLabel(OfficialDocuments.Resources.StateViewDefault);
      AddTasksViews(stateView, document);
      stateView.IsPrintable = true;
      return stateView;
    }
    
    /// <summary>
    /// Построить сводку по документу.
    /// </summary>
    /// <returns>Сводка по документу.</returns>
    [Remote(IsPure = true)]
    [LocalizeFunction("GetDocumentSummaryFunctionName", "GetDocumentSummaryFunctionDescription")]
    public virtual StateView GetDocumentSummary()
    {
      var documentSummary = StateView.Create();
      documentSummary.AddDefaultLabel(string.Empty);
      return documentSummary;
    }
    
    /// <summary>
    /// Получить отображение суммы документа.
    /// </summary>
    /// <param name="totalAmount">Значение суммы.</param>
    /// <returns>Отображение суммы документа.</returns>
    public virtual string GetTotalAmountDocumentSummary(double? totalAmount)
    {
      var canRead = _obj.AccessRights.CanRead();
      var amount = "-";
      
      if (canRead && totalAmount.HasValue)
        amount = totalAmount.Value.ToString("N2");
      
      return amount;
    }

    /// <summary>
    /// Добавить информацию о задачах, в которые вложен документ.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <param name="document">Документ.</param>
    private static void AddTasksViews(StateView stateView, IElectronicDocument document)
    {
      var tasks = Tasks.GetAll()
        .Where(task => task.AttachmentDetails
               .Any(a => a.AttachmentTypeGuid == document.GetEntityMetadata().GetOriginal().NameGuid &&
                    a.AttachmentId == document.Id))
        .OrderBy(task => task.Created)
        .ToList();
      
      var tasksCount = tasks.Count;
      if (Sungero.Docflow.PublicFunctions.Module.IsBlocksNumberOverLimit(tasksCount))
        throw AppliedCodeException.Create(Sungero.Docflow.Resources.ExceededNumberOfBlocks);
      
      foreach (var task in tasks)
      {
        if (stateView.Blocks.Any(b => b.HasEntity(task)))
          continue;
        
        AddTaskViewXml(stateView, task, document);
      }
    }
    
    /// <summary>
    /// Построение модели задачи, в которую вложен документ.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <param name="task">Задача.</param>
    /// <param name="document">Документ.</param>
    private static void AddTaskViewXml(StateView stateView, ITask task, IElectronicDocument document)
    {
      // Добавить предметное отображение для простых задач.
      if (SimpleTasks.Is(task))
      {
        AddSimpleTaskView(stateView, SimpleTasks.As(task));
        return;
      }
      
      // Добавить предметное отображение для прикладных задач.
      var taskStateView = Functions.Module.GetServerEntityFunctionResult(task, "GetStateView", new List<object>() { document });
      if (taskStateView != null)
      {
        // Избавиться от дублирующих блоков, если таковые были.
        List<StateBlock> blockWhiteList = new List<StateBlock>() { };
        
        foreach (var block in ((StateView)taskStateView).Blocks)
        {
          if (block.Entity == null || !stateView.Blocks.Any(b => b.HasEntity(block.Entity)))
            blockWhiteList.Add(block);
        }
        
        foreach (var block in blockWhiteList)
          stateView.AddBlock(block);
      }
    }
    
    /// <summary>
    /// Добавить блок информации о действии.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <param name="user">Пользователь, выполнивший действие.</param>
    /// <param name="text">Текст действия.</param>
    /// <param name="date">Дата действия.</param>
    /// <param name="entity">Сущность, над которой было совершено действие.</param>
    /// <param name="comment">Примечание к действию.</param>
    /// <param name="substituted">Замещающий.</param>
    [Public]
    public static void AddUserActionBlock(object stateView, IUser user, string text, DateTime date,
                                          IEntity entity, string comment, IUser substituted)
    {
      StateBlock block;
      if (stateView is StateBlock)
        block = (stateView as StateBlock).AddChildBlock();
      else
        block = (stateView as StateView).AddBlock();
      block.Entity = entity;
      block.DockType = DockType.Bottom;
      block.AssignIcon(StateBlockIconType.User, StateBlockIconSize.Small);
      block.ShowBorder = false;
      var userActionText = string.Format("{0}. ", GetUserActionText(user, text, substituted));
      block.AddLabel(userActionText);
      
      var style = Functions.Module.CreateStyle(false, true);
      block.AddLabel(string.Format("{0}: {1}", OfficialDocuments.Resources.StateViewDate.ToString(), Functions.Module.ToShortDateShortTime(date.ToUserTime())), style);
      
      comment = Functions.Module.GetFormatedUserText(comment);
      if (!string.IsNullOrWhiteSpace(comment))
      {
        block.AddLineBreak();
        block.AddEmptyLine(1);
        block.AddLabel(comment, style);
      }
    }
    
    /// <summary>
    /// Получить автора задачи (автор, либо кто за кого выполнил).
    /// </summary>
    /// <param name="author">Автор.</param>
    /// <param name="startedBy">Выполнивший.</param>
    /// <returns>Фамилия инициалы автора, либо фамилия инициалы с учетом замещения.</returns>
    [Public]
    public static string GetAuthor(IUser author, IUser startedBy)
    {
      // Костыль для системных пользователей.
      if (!Employees.Is(author))
        return author.IsSystem == true ? OfficialDocuments.Resources.StateViewSystem.ToString() : author.Name;
      
      if (Equals(author, startedBy) || startedBy == null)
        return Company.PublicFunctions.Employee.GetShortName(Employees.As(author), false);

      var started = OfficialDocuments.Resources.StateViewSystem.ToString();
      if (Employees.Is(startedBy))
      {
        started = Company.PublicFunctions.Employee.GetShortName(Employees.As(startedBy), false);
      }
      
      return started + OfficialDocuments.Resources.StateViewInstead.ToString() + Company.PublicFunctions.Employee.GetShortName(Employees.As(author), DeclensionCase.Accusative, false);
    }
    
    /// <summary>
    /// Построить текст действия от пользователя.
    /// </summary>
    /// <param name="user">Пользователь.</param>
    /// <param name="text">Текст.</param>
    /// <param name="substituted">Замещаемый.</param>
    /// <returns>Сформированная строка вида "Пользователь (за замещаемого). Текст действия.".</returns>
    [Public]
    public static string GetUserActionText(IUser user, string text, IUser substituted)
    {
      return string.Format("{0}. {1}", GetAuthor(user, substituted).TrimEnd('.'), text.TrimEnd('.'));
    }

    /// <summary>
    /// Подсчет рабочих дней в промежутке времени.
    /// </summary>
    /// <param name="startDate">Начало.</param>
    /// <param name="endDate">Окончание.</param>
    /// <param name="user">Пользователь.</param>
    /// <returns>Количество рабочих дней.</returns>
    [Public]
    public static int DurationInWorkdays(DateTime startDate, DateTime endDate, IUser user)
    {
      var start = Functions.Module.GetDateWithTime(startDate, user);
      var end = Functions.Module.GetDateWithTime(endDate, user);
      var days = (end - start).TotalDays;
      var calendarDays = (int)(endDate.Date - startDate.Date).TotalDays;
      var workdays = WorkingTime.GetDurationInWorkingDays(start, end);
      if (days < 0 || Equals(start, end))
        return -1;
      if (calendarDays == 0 && workdays == 1)
        return 0;
      return Math.Min(calendarDays, workdays);
    }

    /// <summary>
    /// Подсчет рабочих часов в промежутке времени.
    /// </summary>
    /// <param name="startDate">Начало.</param>
    /// <param name="endDate">Окончание.</param>
    /// <param name="user">Пользователь.</param>
    /// <returns>Количество рабочих часов.</returns>
    /// <remarks>Только в рамках одного дня.</remarks>
    [Public]
    private static int DurationInWorkhours(DateTime startDate, DateTime endDate, IUser user)
    {
      var workday = CoreEntities.WorkingTime.GetAllCachedByYear(startDate.Year).Where(c => startDate.Year == c.Year)
        .SelectMany(y => y.Day)
        .ToList()
        .SingleOrDefault(d => startDate.Date == d.Day.Date && d.Day.IsWorkingDay(user));
      
      if (workday == null)
        return 0;

      var start = Functions.Module.GetDateWithTime(startDate, user);
      var end = Functions.Module.GetDateWithTime(endDate, user);
      var duration = WorkingTime.GetDurationInWorkingHours(start, end, user);
      var result = Math.Round(duration, 0);
      return result < 1 ? 1 : (int)result;
    }

    /// <summary>
    /// Добавить в заголовок информацию о задержке выполнения.
    /// </summary>
    /// <param name="block">Блок схемы.</param>
    /// <param name="deadline">Планируемый срок выполнения.</param>
    /// <param name="user">Исполнитель.</param>
    [Public]
    public static void AddDeadlineHeaderToRight(Sungero.Core.StateBlock block, DateTime deadline, IUser user)
    {
      var now = Calendar.Now;
      var delayInDays = DurationInWorkdays(deadline, now, user);
      var delayInHours = 0;
      
      if (delayInDays < 0)
        return;
      
      if (delayInDays < 1)
      {
        delayInHours = DurationInWorkhours(deadline, now, user);
        if (delayInHours == 0)
          return;
      }
      
      var delay = delayInDays < 1 ?
        Functions.Module.GetNumberDeclination(delayInHours,
                                              Resources.StateViewHour,
                                              Resources.StateViewHourGenetive,
                                              Resources.StateViewHourPlural) :
        Functions.Module.GetNumberDeclination(delayInDays,
                                              Resources.StateViewDay,
                                              Resources.StateViewDayGenetive,
                                              Resources.StateViewDayPlural);
      
      var label = string.Format("{0} {1} {2}", OfficialDocuments.Resources.StateViewDelay, delayInDays < 1 ? delayInHours : delayInDays, delay);
      var style = Functions.Module.CreateStyle(Sungero.Core.Colors.Common.Red);
      
      // Добавить колонку справа, если всего одна колонка (main).
      var rightContent = block.Contents.LastOrDefault();
      if (block.Contents.Count() <= 1)
        rightContent = block.AddContent();
      else
        rightContent.AddLineBreak();
      
      rightContent.AddLabel(label, style);
    }
    
    /// <summary>
    /// Добавить предметное отображение простой задачи.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <param name="task">Задача.</param>
    private static void AddSimpleTaskView(StateView stateView, ISimpleTask task)
    {
      if (task == null)
        return;
      
      // Не добавлять блок, если нет заданий. Черновик - исключение.
      var assignments = new List<IAssignment>() { };
      assignments.AddRange(SimpleAssignments.GetAll().Where(a => Equals(a.Task, task)).ToList());
      assignments.AddRange(ReviewAssignments.GetAll().Where(a => Equals(a.Task, task) && a.Result == null).ToList());
      if (!assignments.Any() && task.Status != Workflow.Task.Status.Draft)
        return;
      
      // Добавить блок информации о действии.
      if (task.Started.HasValue)
        AddUserActionBlock(stateView, task.Author, OfficialDocuments.Resources.StateViewTaskSent, task.Started.Value, task, string.Empty, task.StartedBy);
      else
        AddUserActionBlock(stateView, task.Author, ApprovalTasks.Resources.StateViewTaskDrawCreated, task.Created.Value, task, string.Empty, task.Author);
      
      // Добавить блок информации по задаче.
      var mainBlock = GetSimpleTaskMainBlock(task);
      stateView.AddBlock(mainBlock);
      
      // Маршрут.
      var iterations = Functions.Module.GetIterationDates(task);
      foreach (var iteration in iterations)
      {
        var date = iteration.Date;
        var hasReworkBefore = iteration.IsRework;
        var hasRestartBefore = iteration.IsRestart;
        
        var nextIteration = iterations.Where(d => d.Date > date).FirstOrDefault();
        var nextDate = nextIteration != null ? nextIteration.Date : Calendar.Now;
        
        // Получить задания в рамках круга согласования.
        var iterationAssignments = assignments
          .Where(a => a.Created >= date && a.Created < nextDate)
          .OrderBy(a => a.Created)
          .ToList();
        
        if (!iterationAssignments.Any())
          continue;
        
        if (hasReworkBefore || hasRestartBefore)
        {
          var activeText = task.Texts
            .Where(t => t.Modified >= date)
            .OrderBy(t => t.Created)
            .FirstOrDefault();
          
          var comment = activeText != null ? activeText.Body : string.Empty;
          var started = activeText != null ? activeText.Modified : task.Started;
          
          var header = hasReworkBefore ? OfficialDocuments.Resources.StateViewTaskSentForRevision : OfficialDocuments.Resources.StateViewTaskSentAfterRestart;
          AddUserActionBlock(mainBlock, task.Author, header, started.Value, task, comment, task.StartedBy);
        }
        
        AddSimpleTaskIterationsBlocks(mainBlock, iterationAssignments);
      }
    }
    
    /// <summary>
    /// Получить предметное отображение простой задачи.
    /// </summary>
    /// <param name="task">Простая задача.</param>
    /// <returns>Предметное отображение простой задачи.</returns>
    private static Sungero.Core.StateBlock GetSimpleTaskMainBlock(ISimpleTask task)
    {
      var stateView = StateView.Create();
      var block = stateView.AddBlock();
      if (task == null)
        return block;
      
      block.Entity = task;
      var inWork = task.Status == Workflow.Task.Status.InProcess || task.Status == Workflow.Task.Status.UnderReview;
      block.IsExpanded = inWork;
      block.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Large);
      
      // Заголовок. Тема.
      block.AddLabel(string.Format("{0}. {1}", OfficialDocuments.Resources.StateViewTask, task.Subject), Functions.Module.CreateHeaderStyle());
      
      // Срок.
      var hasDeadline = task.MaxDeadline.HasValue;
      var deadline = hasDeadline ? Functions.Module.ToShortDateShortTime(task.MaxDeadline.Value.ToUserTime()) : OfficialDocuments.Resources.StateViewWithoutTerm;
      block.AddLineBreak();
      block.AddLabel(string.Format("{0}: {1}", OfficialDocuments.Resources.StateViewFinalDeadline, deadline), Functions.Module.CreatePerformerDeadlineStyle());
      
      // Текст задачи.
      var taskText = Functions.Module.GetTaskUserComment(task, string.Empty);
      if (!string.IsNullOrWhiteSpace(taskText))
      {
        block.AddLineBreak();
        block.AddLabel(Constants.Module.SeparatorText, Docflow.PublicFunctions.Module.CreateSeparatorStyle());
        block.AddLineBreak();
        block.AddEmptyLine(Constants.Module.EmptyLineMargin);
        
        // Форматирование текста задачи.
        block.AddLabel(taskText);
      }
      
      // Статус.
      var status = Workflow.SimpleTasks.Info.Properties.Status.GetLocalizedValue(task.Status);
      if (!string.IsNullOrEmpty(status))
        Functions.Module.AddInfoToRightContent(block, status);
      
      // Задержка.
      if (hasDeadline && inWork)
        AddDeadlineHeaderToRight(block, task.MaxDeadline.Value, Users.Current);
      
      return block;
    }
    
    /// <summary>
    /// Добавить маршрут в предметное отображение простой задачи.
    /// </summary>
    /// <param name="mainBlock">Блок задачи.</param>
    /// <param name="assignments">Задания по задаче.</param>
    private static void AddSimpleTaskIterationsBlocks(StateBlock mainBlock, List<IAssignment> assignments)
    {
      var statusGroups = assignments.OrderByDescending(a => a.Status == Workflow.AssignmentBase.Status.Completed).GroupBy(a => a.Status);
      foreach (var statusGroup in statusGroups)
      {
        var deadlineGroups = statusGroup.OrderBy(a => a.Deadline).GroupBy(a => a.Deadline);
        foreach (var deadlineGroup in deadlineGroups)
        {
          var textGroups = deadlineGroup.OrderBy(a => a.Modified).GroupBy(a => a.ActiveText);
          foreach (var textGroup in textGroups)
          {
            var assignmentBlocks = GetSimpleAssignmentsView(textGroup.ToList()).Blocks;
            if (assignmentBlocks.Any())
              foreach (var block in assignmentBlocks)
                mainBlock.AddChildBlock(block);
          }
        }
      }
    }
    
    /// <summary>
    /// Получить предметное отображение группы простых заданий.
    /// </summary>
    /// <param name="assignments">Простые задания.</param>
    /// <returns>Предметное отображение простого задания.</returns>
    private static Sungero.Core.StateView GetSimpleAssignmentsView(List<IAssignment> assignments)
    {
      var stateView = StateView.Create();
      if (!assignments.Any())
        return stateView;

      // Т.к. задания в пачке должны быть с одинаковым статусом, одинаковым дедлайном - вытаскиваем первый элемент для удобной работы.
      var assignment = assignments.First();
      
      var block = stateView.AddBlock();
      if (assignments.Count == 1)
        block.Entity = assignment;

      // Иконка.
      block.AssignIcon(ApprovalRuleBases.Resources.Assignment, StateBlockIconSize.Large);
      if (assignments.All(a => a.Status == Workflow.AssignmentBase.Status.Completed))
      {
        block.AssignIcon(ApprovalTasks.Resources.Completed, StateBlockIconSize.Large);
      }
      else if (assignments.All(a => a.Status == Workflow.AssignmentBase.Status.Aborted || a.Status == Workflow.AssignmentBase.Status.Suspended))
      {
        block.AssignIcon(StateBlockIconType.Abort, StateBlockIconSize.Large);
      }
      
      // Заголовок.
      var header = ReviewAssignments.Is(assignment) ? OfficialDocuments.Resources.StateViewAssignmentForReview : OfficialDocuments.Resources.StateViewAssignment;
      block.AddLabel(header, Functions.Module.CreateHeaderStyle());
      
      // Кому.
      block.AddLineBreak();
      var performers = assignments.Where(a => Employees.Is(a.Performer)).Select(a => Employees.As(a.Performer)).ToList();
      block.AddLabel(string.Format("{0}: {1} ", OfficialDocuments.Resources.StateViewTo, GetPerformersInText(performers)), Functions.Module.CreatePerformerDeadlineStyle());
      
      // Срок.
      var deadline = assignment.Deadline.HasValue ?
        Functions.Module.ToShortDateShortTime(assignment.Deadline.Value.ToUserTime()) :
        OfficialDocuments.Resources.StateViewWithoutTerm;
      block.AddLabel(string.Format("{0}: {1}", OfficialDocuments.Resources.StateViewDeadline, deadline), Functions.Module.CreatePerformerDeadlineStyle());
      
      // Результат выполнения.
      var activeText = Functions.Module.GetAssignmentUserComment(assignment);
      if (!string.IsNullOrWhiteSpace(activeText))
      {
        block.AddLineBreak();
        block.AddLabel(Constants.Module.SeparatorText, Docflow.PublicFunctions.Module.CreateSeparatorStyle());
        block.AddLineBreak();
        block.AddEmptyLine(Constants.Module.EmptyLineMargin);
        
        block.AddLabel(activeText);
      }
      
      // Статус.
      var assignmentStatus = Workflow.SimpleAssignments.Info.Properties.Status.GetLocalizedValue(assignment.Status);
      if (assignment.Status == Workflow.AssignmentBase.Status.InProcess && assignment.IsRead == false)
      {
        assignmentStatus = Docflow.ApprovalTasks.Resources.StateViewUnRead;
      }
      else if (assignment.Status == Workflow.AssignmentBase.Status.Aborted)
      {
        assignmentStatus = Docflow.ApprovalTasks.Resources.StateViewAborted;
      }
      
      if (!string.IsNullOrEmpty(assignmentStatus))
        Functions.Module.AddInfoToRightContent(block, assignmentStatus);
      
      // Задержка.
      if (assignment.Deadline.HasValue && assignment.Status == Workflow.AssignmentBase.Status.InProcess)
        AddDeadlineHeaderToRight(block, assignment.Deadline.Value, assignment.Performer);
      
      return stateView;
    }
    
    /// <summary>
    /// Сформировать текстовый список исполнителей заданий.
    /// </summary>
    /// <param name="employees">Сотрудники.</param>
    /// <returns>Строка в формате "Ардо Н.А., Соболева Н.Н. и еще 2 сотрудника.".</returns>
    [Public]
    public static string GetPerformersInText(List<IEmployee> employees)
    {
      var employeesCount = employees.Count();
      var maxDisplayedNumberCount = 5;
      var minHiddenNumberCount = 3;
      var displayedValuesCount = employeesCount;
      if (employeesCount >= (maxDisplayedNumberCount + minHiddenNumberCount))
      {
        displayedValuesCount = maxDisplayedNumberCount;
      }
      else if (employeesCount > maxDisplayedNumberCount)
      {
        displayedValuesCount = employeesCount - minHiddenNumberCount;
      }
      
      var employeesText = string.Join(", ", employees.Select(p => Company.PublicFunctions.Employee.GetShortName(p, false)).ToArray(), 0, displayedValuesCount);
      var hiddenSkippedNumberCount = employeesCount - displayedValuesCount;
      if (hiddenSkippedNumberCount > 0)
      {
        var numberLabel = Functions.Module.GetNumberDeclination(hiddenSkippedNumberCount,
                                                                Resources.StateViewEmployee,
                                                                Resources.StateViewEmployeeGenetive,
                                                                Resources.StateViewEmployeePlural);
        employeesText += OfficialDocuments.Resources.StateViewAndFormat(hiddenSkippedNumberCount, numberLabel);
      }
      
      return employeesText;
    }
    
    #endregion
    
    #region Конвертеры

    /// <summary>
    /// Получить ФИО сотрудника для шаблона документа.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>ФИО сотрудника.</returns>
    [Sungero.Core.Converter("FullName")]
    public static PersonFullName FullName(IEmployee employee)
    {
      return FullName(employee.Person);
    }
    
    /// <summary>
    /// Получить ФИО контакта для шаблона документа.
    /// </summary>
    /// <param name="contact">Контакт.</param>
    /// <returns>ФИО контакта.</returns>
    [Sungero.Core.Converter("FullName")]
    public static PersonFullName FullName(IContact contact)
    {
      if (contact.Person != null)
        return FullName(contact.Person);
      
      PersonFullName contactPersonalData;
      return PersonFullName.TryParse(contact.Name, out contactPersonalData) ?
        contactPersonalData :
        PersonFullName.CreateUndefined(contact.Name);
    }
    
    /// <summary>
    /// Получить ФИО персоны для шаблона документа.
    /// </summary>
    /// <param name="person">Персона.</param>
    /// <returns>ФИО персоны.</returns>
    [Sungero.Core.Converter("FullName")]
    public static PersonFullName FullName(IPerson person)
    {
      person = Sungero.Parties.People.As(person);
      if (person == null)
        return null;
      return PersonFullName.Create(person.LastName, person.FirstName, person.MiddleName);
    }
    
    /// <summary>
    /// Получить фамилию и инициалы сотрудника для шаблона документа.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Фамилия и инициалы сотрудника.</returns>
    [Sungero.Core.Converter("LastNameAndInitials")]
    public static PersonFullName LastNameAndInitials(IEmployee employee)
    {
      return LastNameAndInitials(employee.Person);
    }
    
    /// <summary>
    /// Получить фамилию и инициалы контакта для шаблона документа.
    /// </summary>
    /// <param name="contact">Контакт.</param>
    /// <returns>Фамилия и инициалы контакта.</returns>
    [Sungero.Core.Converter("LastNameAndInitials")]
    public static PersonFullName LastNameAndInitials(IContact contact)
    {
      if (contact.Person != null)
        return LastNameAndInitials(contact.Person);
      
      PersonFullName contactPersonalData;
      return PersonFullName.TryParse(contact.Name, out contactPersonalData) ?
        PersonFullName.Create(contactPersonalData.LastName, contactPersonalData.FirstName, contactPersonalData.MiddleName, PersonFullNameDisplayFormat.LastNameAndInitials) :
        PersonFullName.CreateUndefined(contact.Name);
    }
    
    /// <summary>
    /// Получить фамилию и инициалы персоны для шаблона документа.
    /// </summary>
    /// <param name="counterparty">Персона.</param>
    /// <returns>Фамилия и инициалы персоны.</returns>
    [Sungero.Core.Converter("LastNameAndInitials")]
    public static PersonFullName LastNameAndInitials(IPerson counterparty)
    {
      var person = Sungero.Parties.People.As(counterparty);
      if (person != null)
        return PersonFullName.Create(person.LastName, person.FirstName, person.MiddleName, PersonFullNameDisplayFormat.LastNameAndInitials);
      return null;
    }
    
    /// <summary>
    /// Получить инициалы и фамилию сотрудника для шаблона документа.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Инициалы и фамилия сотрудника.</returns>
    [Sungero.Core.Converter("InitialsAndLastName")]
    public static PersonFullName InitialsAndLastName(IEmployee employee)
    {
      return InitialsAndLastName(employee.Person);
    }
    
    /// <summary>
    /// Получить инициалы и фамилию контакта для шаблона документа.
    /// </summary>
    /// <param name="contact">Контакт.</param>
    /// <returns>Инициалы и фамилия контакта.</returns>
    [Sungero.Core.Converter("InitialsAndLastName")]
    public static PersonFullName InitialsAndLastName(IContact contact)
    {
      if (contact.Person != null)
        return InitialsAndLastName(contact.Person);
      
      PersonFullName contactPersonalData;
      return PersonFullName.TryParse(contact.Name, out contactPersonalData) ?
        PersonFullName.Create(contactPersonalData.LastName, contactPersonalData.FirstName, contactPersonalData.MiddleName, PersonFullNameDisplayFormat.InitialsAndLastName) :
        PersonFullName.CreateUndefined(contact.Name);
    }
    
    /// <summary>
    /// Получить инициалы и фамилию персоны для шаблона документа.
    /// </summary>
    /// <param name="person">Персона.</param>
    /// <returns>Инициалы и фамилия персоны.</returns>
    [Sungero.Core.Converter("InitialsAndLastName")]
    public static PersonFullName InitialsAndLastName(IPerson person)
    {
      person = Sungero.Parties.People.As(person);
      if (person == null)
        return null;
      return PersonFullName.Create(person.LastName, person.FirstName, person.MiddleName, PersonFullNameDisplayFormat.InitialsAndLastName);
    }
    
    /// <summary>
    /// Получить перечень приложений для шаблона документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Перечень приложений.</returns>
    [Sungero.Core.Converter("Addenda")]
    public static string Addenda(IOfficialDocument document)
    {
      var documentAddenda = document.Relations.GetRelatedDocuments(Constants.Module.AddendumRelationName).Select(doc => doc.DisplayValue).ToList();
      var result = string.Empty;
      for (var i = 1; i <= documentAddenda.Count; i++)
        result += string.Format("{0}. {1}{2}", i, documentAddenda[i - 1], Environment.NewLine);
      
      return result;
    }
    
    /// <summary>
    /// Получить отметку об исполнителе для шаблона документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Отметка об исполнителе.</returns>
    [Sungero.Core.Converter("PerformerNotes")]
    public static string PerformerNotes(IOfficialDocument document)
    {
      // Исключить дублирование исполнителя (подготовил) и подписывающего в шаблоне.
      if (!Equals(document.PreparedBy, document.OurSignatory))
      {
        if (document.PreparedBy.Phone == null)
          return FullName(document.PreparedBy.Person).ToString();
        else
          return string.Format("{0} {1} {2}",
                               FullName(document.PreparedBy.Person).ToString(),
                               Environment.NewLine, document.PreparedBy.Phone);
      }
      
      return string.Empty;
      
    }
    
    #endregion
    
    #region Работа со связями
    
    /// <summary>
    /// Получить тип связи по наименованию.
    /// </summary>
    /// <param name="relationName">Наименование типа связи.</param>
    /// <returns>Тип связи.</returns>
    [Remote]
    public static Sungero.CoreEntities.IRelationType GetRelationTypeByName(string relationName)
    {
      return Sungero.CoreEntities.RelationTypes.GetAll()
        .FirstOrDefault(x => x.Name == relationName);
    }
    
    /// <summary>
    /// Получить связанные документы по типу связи.
    /// </summary>
    /// <param name="document">Документ, для которого получаются связанные документы.</param>
    /// <param name="relationTypeName">Наименование типа связи.</param>
    /// <param name="withVersion">Учитывать только документы с версиями.</param>
    /// <returns>Связанные документы.</returns>
    [Remote]
    public static List<IOfficialDocument> GetRelatedDocumentsByRelationType(IOfficialDocument document, string relationTypeName, bool withVersion)
    {
      if (string.IsNullOrWhiteSpace(relationTypeName))
        return new List<IOfficialDocument>();
      
      var relationType = GetRelationTypeByName(relationTypeName);
      
      if (relationType == null)
        return new List<IOfficialDocument>();
      
      // Прямая связь. Текущий - Source, связанный - Target.
      var relations = Sungero.Content.DocumentRelations.GetAll()
        .Where(x => Equals(x.RelationType, relationType) &&
               Equals(x.Source, document));
      var documents = relations
        .Where(x => !withVersion || x.Target.HasVersions)
        .Where(x => OfficialDocuments.Is(x.Target))
        .Select(x => x.Target)
        .Cast<IOfficialDocument>().ToList();
      
      if (relationType.HasDirection == true)
        return documents;
      
      // Обратная связь. Текущий - Target, связанный - Source.
      relations = Sungero.Content.DocumentRelations.GetAll()
        .Where(x => Equals(x.RelationType, relationType) &&
               Equals(x.Target, document));
      documents.AddRange(relations
                         .Where(x => !withVersion || x.Source.HasVersions)
                         .Where(x => OfficialDocuments.Is(x.Source))
                         .Select(x => x.Source)
                         .Cast<IOfficialDocument>().ToList());
      
      return documents;
    }
    
    /// <summary>
    /// Связать с основным документом документы из списка, если они не были связаны ранее.
    /// </summary>
    /// <param name="documents">Список документов.</param>
    [Public, Obsolete("Метод не используется с 04.04.2024 и версии 4.10. Используйте метод RelateDocumentsToPrimaryDocumentAsAddenda модуля DocflowApproval.")]
    public virtual void RelateDocumentsToPrimaryDocumentAsAddenda(List<IOfficialDocument> documents)
    {
      Sungero.DocflowApproval.PublicFunctions.Module.RelateDocumentsToPrimaryDocumentAsAddenda(_obj, documents.Cast<IElectronicDocument>().ToList());
    }
    
    #endregion
    
    #region Запрос подготовки предпросмотра
    
    /// <summary>
    /// Отправить запрос на подготовку предпросмотра документа.
    /// </summary>
    [Public]
    public virtual void PreparePreview()
    {
      if (_obj.HasVersions)
        Sungero.Core.PreviewService.PreparePreview(_obj.LastVersion);
    }
    
    #endregion
    
    #region Выдача и контроль возврата
    
    /// <summary>
    /// Добавить или обновить информацию о согласовании с контрагентом в выдаче документа.
    /// </summary>
    /// <param name="responsibleForReturnId">ИД ответственного за возврат документа.</param>
    /// <param name="returnDeadline">Срок возврата.</param>
    /// <param name="returnTask">Задача возврата.</param>
    /// <remarks>Если в выдаче есть только строки с действием "Отправка контрагенту",
    /// изменяем последнюю по дате выдачи запись на "Согласование с контрагентом".</remarks>
    [Public]
    public virtual void AddOrUpdateEndorsementInfoInTracking(long responsibleForReturnId, DateTime returnDeadline, ITask returnTask)
    {
      var responsibleForReturn = Sungero.Company.Employees.Get(responsibleForReturnId);
      var endorsementTracking = this.GetUnreturnedFromCounterpartyTracking(returnTask);
      var sendingTracking = _obj.Tracking.Where(x => x.Action == Docflow.OfficialDocumentTracking.Action.Sending);
      
      if (endorsementTracking.Any())
      {
        foreach (var tracking in endorsementTracking)
        {
          tracking.DeliveredTo = responsibleForReturn;
          tracking.ReturnDeadline = returnDeadline;
          tracking.ReturnTask = returnTask;
          Docflow.PublicFunctions.OfficialDocument.WriteTrackingLog("CheckReturnFromCounterparty. Update endorsement info in tracking.", tracking);
        }
      }
      else if (sendingTracking.Any())
      {
        var tracking = sendingTracking.OrderByDescending(x => x.DeliveryDate).First();
        tracking.Action = Docflow.OfficialDocumentTracking.Action.Endorsement;
        tracking.DeliveredTo = responsibleForReturn;
        tracking.ReturnDeadline = returnDeadline;
        tracking.Note = OfficialDocuments.Resources.DocumentOnCounterpartyApproval;
        tracking.ReturnTask = returnTask;
        Docflow.PublicFunctions.OfficialDocument.WriteTrackingLog("CheckReturnFromCounterparty. Update sending to endorsement tracking.", tracking);
      }
      else
      {
        this.IssueDocumentToCounterparty(responsibleForReturnId, Docflow.OfficialDocumentTracking.Action.Endorsement, returnDeadline, returnTask);
      }
    }
    
    /// <summary>
    /// Обновить выдачу в документе после возврата контрагентом.
    /// </summary>
    /// <param name="responsibleForReturn">Ответственный за возврат документа.</param>
    /// <param name="returnTask">Задача возврата.</param>
    /// <param name="isSigned">Признак того, подписал ли контрагент документ.</param>
    [Public]
    public virtual void UpdateTrackingAfterReturnFromCounterparty(IRecipient responsibleForReturn, ITask returnTask, bool isSigned)
    {
      var tracking = this.GetUnreturnedFromCounterpartyTracking(returnTask);
      foreach (var row in tracking)
      {
        row.ReturnDate = Calendar.GetUserToday(responsibleForReturn);
        row.ReturnResult = isSigned
          ? Docflow.OfficialDocumentTracking.ReturnResult.Signed
          : Docflow.OfficialDocumentTracking.ReturnResult.NotSigned;
        if (row.Note == OfficialDocuments.Resources.DocumentOnCounterpartyApproval)
          row.Note = null;
        
        Functions.OfficialDocument.WriteTrackingLog("OfficialDocument. Update tracking after return from counterparty.", row);
      }
    }
    
    /// <summary>
    /// Получить строки выдачи документа с незаполненной датой возврата (документ еще на согласовании у контрагента).
    /// </summary>
    /// <param name="returnTask">Задача возврата.</param>
    /// <returns>Строки выдачи с незаполненной датой возврата.</returns>
    [Public]
    public virtual List<Sungero.Docflow.IOfficialDocumentTracking> GetUnreturnedFromCounterpartyTracking(ITask returnTask)
    {
      return _obj.Tracking
        .Where(x => x.Action == Docflow.OfficialDocumentTracking.Action.Endorsement &&
               (Equals(x.ReturnTask, returnTask) || x.ReturnTask == null) &&
               x.ReturnDeadline.HasValue && !x.ReturnDate.HasValue)
        .ToList();
    }
    
    /// <summary>
    /// Получить строку выдачи документа с незаполненным результатом возврата (документ еще на согласовании у контрагента в сервисе обмена).
    /// </summary>
    /// <param name="externalLinkId">ИД записи с информацией о документе в сервисе обмена.</param>
    /// <returns>Строка выдачи с незаполненным результатом возврата.</returns>
    [Public]
    public virtual Sungero.Docflow.IOfficialDocumentTracking GetUnreturnedFromCounterpartyTracking(long externalLinkId)
    {
      return _obj.Tracking.FirstOrDefault(x => x.Action == Docflow.OfficialDocumentTracking.Action.Endorsement &&
                                          !x.ReturnResult.HasValue && x.ExternalLinkId == externalLinkId);
    }
    
    /// <summary>
    /// Выдать документ сотруднику для отправки контрагенту.
    /// </summary>
    /// <param name="responsibleForReturnId">ИД ответственного за возврат документа.</param>
    /// <param name="action">Действие.</param>
    /// <param name="returnDeadline">Срок возврата.</param>
    /// <param name="returnTask">Задача возврата.</param>
    [Public]
    public virtual void IssueDocumentToCounterparty(long responsibleForReturnId, Enumeration action, DateTime? returnDeadline, ITask returnTask)
    {
      var tracking = _obj.Tracking.AddNew();
      var responsibleForReturn = Sungero.Company.Employees.Get(responsibleForReturnId);
      
      tracking.DeliveredTo = responsibleForReturn;
      tracking.Action = action;
      tracking.DeliveryDate = Calendar.GetUserToday(responsibleForReturn);
      tracking.IsOriginal = true;
      tracking.Note = returnDeadline.HasValue ? OfficialDocuments.Resources.DocumentOnCounterpartyApproval : OfficialDocuments.Resources.CounterpartyDocument;
      tracking.ReturnTask = returnTask;
      tracking.ReturnDeadline = returnDeadline;
      Docflow.PublicFunctions.OfficialDocument.WriteTrackingLog("CheckReturnFromCounterparty. Issue document to counterparty.", tracking);
    }
    
    /// <summary>
    /// Обновить статус согласования с контрагентом после возврата документа.
    /// </summary>
    public virtual void UpdateExternalApprovalStateAfterReturnFromCounterparty()
    {
      var unreturnedTracking = _obj.Tracking.Where(x => x.Action == Docflow.OfficialDocumentTracking.Action.Endorsement &&
                                                   x.ReturnDeadline != null && x.ReturnDate == null);
      
      if (!unreturnedTracking.Any() && (_obj.ExternalApprovalState == null || _obj.ExternalApprovalState == ExternalApprovalState.OnApproval))
      {
        var changedTracking = Functions.OfficialDocument.GetChangedTrackingRecordsWithTasksInProcess(_obj);
        
        if (changedTracking.Any() && changedTracking.All(x => x.ReturnResult == Docflow.OfficialDocumentTracking.ReturnResult.Signed))
          _obj.ExternalApprovalState = ExternalApprovalState.Signed;
        else if (changedTracking.Any() && changedTracking.All(x => x.ReturnResult == Docflow.OfficialDocumentTracking.ReturnResult.NotSigned))
          _obj.ExternalApprovalState = ExternalApprovalState.Unsigned;
      }
    }
    
    /// <summary>
    /// Получить самую позднюю строку выдачи с действием "Отправка контрагенту" или "Согласование с контрагентом" и незаполненной датой возврата.
    /// </summary>
    /// <returns>Строка выдачи с незаполненной датой возврата и наибольшей датой выдачи.</returns>
    /// <remarks>Если есть подходящая строка выдачи с действием "Согласование с контрагентом", то в результате вернется она.</remarks>
    [Public]
    public virtual IOfficialDocumentTracking GetLatestDocumentTracking()
    {
      return _obj.Tracking.Where(t => (t.Action == Docflow.OfficialDocumentTracking.Action.Sending ||
                                       t.Action == Docflow.OfficialDocumentTracking.Action.Endorsement) &&
                                 t.DeliveryDate.HasValue && !t.ReturnDate.HasValue)
        .OrderByDescending(t => t.Action == Docflow.OfficialDocumentTracking.Action.Endorsement)
        .OrderByDescending(t => t.DeliveryDate)
        .FirstOrDefault();
    }
    
    #endregion
    
    /// <summary>
    /// Получить все данные для отображения диалога регистрации.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="operation">Операция.</param>
    /// <returns>Параметры диалога.</returns>
    [Remote(IsPure = true)]
    public static Structures.OfficialDocument.IDialogParamsLite GetRegistrationDialogParams(IOfficialDocument document, Enumeration operation)
    {
      var leadDocumentId = Functions.OfficialDocument.GetLeadDocumentId(document);
      var leadDocumentNumber = Functions.OfficialDocument.GetLeadDocumentNumber(document);
      var numberValidationDisabled = Functions.OfficialDocument.IsNumberValidationDisabled(document);
      var departmentId = document.Department != null ? document.Department.Id : 0;
      var departmentCode = document.Department != null ? document.Department.Code : string.Empty;
      var businessUnitId = document.BusinessUnit != null ? document.BusinessUnit.Id : 0;
      var businessUnitCode = document.BusinessUnit != null ? document.BusinessUnit.Code : string.Empty;
      var docKindCode = document.DocumentKind != null ? document.DocumentKind.Code : string.Empty;
      var caseFileIndex = document.CaseFile != null ? document.CaseFile.Index : string.Empty;
      var isClerk = document.AccessRights.CanRegister();
      var counterpartyCode = Functions.OfficialDocument.GetCounterpartyCode(document);
      var currentRegistrationDate = document.RegistrationDate ?? Calendar.UserToday;
      
      var registersIds = Functions.OfficialDocument.GetDocumentRegistersIdsByDocument(document, operation);
      var defaultDocumentRegister = Functions.DocumentRegister.GetDefaultDocRegister(document, registersIds, operation);
      string nextNumber = string.Empty;
      if (defaultDocumentRegister != null)
      {
        var useObsoleteRegNumberGeneration = Functions.Module.GetDocflowParamsBoolValue(PublicConstants.Module.UseObsoleteRegNumberGenerationParamName);
        if (useObsoleteRegNumberGeneration)
        {
          var formatItems = Functions.DocumentRegister.GetNumberFormatItemsValues(document);
          nextNumber = Functions.DocumentRegister.GetNextNumber(defaultDocumentRegister, currentRegistrationDate, formatItems.LeadingDocumentId, document,
                                                                formatItems.LeadingDocumentNumber, formatItems.DepartmentId, formatItems.BusinessUnitId,
                                                                formatItems.CaseFileIndex, formatItems.DocumentKindCode, Constants.OfficialDocument.DefaultIndexLeadingSymbol);
        }
        else
        {
          nextNumber = Functions.DocumentRegister.GetNextNumber(defaultDocumentRegister, document, currentRegistrationDate);
        }
      }

      return Structures.OfficialDocument.DialogParamsLite.Create(registersIds, operation, defaultDocumentRegister,
                                                                 document.RegistrationNumber, currentRegistrationDate, nextNumber,
                                                                 leadDocumentId, leadDocumentNumber, numberValidationDisabled,
                                                                 departmentId, departmentCode, businessUnitCode, businessUnitId,
                                                                 caseFileIndex, docKindCode, counterpartyCode, isClerk);
    }
    
    /// <summary>
    /// Признак того, что формат номера не надо валидировать.
    /// </summary>
    /// <returns>True, если формат номера неважен.</returns>
    [Remote(IsPure = true)]
    public virtual bool IsNumberValidationDisabled()
    {
      // Для всех контрактных документов валидация отключена (в т.ч. - для автонумеруемых).
      if (_obj.DocumentKind != null && _obj.DocumentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts)
        return true;
      
      // Для автонумеруемых неконтрактных документов валидация включена.
      if (_obj.DocumentKind != null && _obj.DocumentKind.AutoNumbering.Value)
        return false;
      
      // Для неавтонумеруемых финансовых документов валидация отключена.
      return AccountingDocumentBases.Is(_obj);
    }
    
    /// <summary>
    /// Сформировать текстовку для местонахождения.
    /// </summary>
    /// <returns>Местонахождение.</returns>
    public virtual string GetLocationState()
    {
      var tracking = _obj.Tracking.Where(l => !l.ReturnDate.HasValue && l.DeliveredTo != null).OrderByDescending(l => l.DeliveryDate);
      var originalTracking = tracking.Where(l => (l.IsOriginal ?? false) && l.Action == Docflow.OfficialDocumentTracking.Action.Delivery);
      var copyTracking = tracking.Where(l => !(l.IsOriginal ?? false) && l.Action == Docflow.OfficialDocumentTracking.Action.Delivery && l.ReturnDeadline.HasValue);

      var originalTrackingAtContractor = tracking.Where(l => (l.IsOriginal ?? false) &&
                                                        (l.Action == Docflow.OfficialDocumentTracking.Action.Endorsement ||
                                                         l.Action == Docflow.OfficialDocumentTracking.Action.Sending) &&
                                                        l.ReturnDeadline.HasValue &&
                                                        l.ExternalLinkId == null);
      
      var copyTrackingAtContractor = tracking.Where(l => !(l.IsOriginal ?? false) &&
                                                    (l.Action == Docflow.OfficialDocumentTracking.Action.Endorsement ||
                                                     l.Action == Docflow.OfficialDocumentTracking.Action.Sending) &&
                                                    l.ReturnDeadline.HasValue &&
                                                    l.ExternalLinkId == null);
      
      var canShowExchange = _obj.Tracking.Any(t => t.ExternalLinkId != null && t.ReturnResult == null && t.ReturnDeadline != null) ||
        Sungero.Exchange.ExchangeDocumentInfos.GetAll(x => Equals(x.Document, _obj)).Any();
      
      // Сформировать в культуре тенанта.
      using (Core.CultureInfoExtensions.SwitchTo(TenantInfo.Culture))
      {
        var trackingState = string.Empty;
        if (originalTracking.Any() || copyTracking.Any() || originalTrackingAtContractor.Any() || copyTrackingAtContractor.Any()
            || _obj.State.Properties.ExchangeState.IsChanged || _obj.State.Properties.Tracking.IsChanged || canShowExchange)
        {
          var originals = string.Join("; \n", originalTracking
                                      .Select(l => Docflow.Resources.OriginalDocumentLocatedInFormat(Company.PublicFunctions.Employee.GetShortName(l.DeliveredTo, DeclensionCase.Genitive, false))));
          
          var copies = string.Join("; \n", copyTracking
                                   .Select(l => Docflow.Resources.CopyDocumentLocatedInFormat(Company.PublicFunctions.Employee.GetShortName(l.DeliveredTo, DeclensionCase.Genitive, false))));
          
          var originalsAtContractor = string.Join("; \n", originalTrackingAtContractor
                                                  .Select(l => Docflow.Resources.OriginalDocumentLocatedInFormat(Docflow.Resources.AtContractorTrackingShowing)));
          
          var exchangeLocation = Functions.OfficialDocument.GetExchangeLocation(_obj);
          
          var copiesAtContractor = string.Join("; \n", copyTrackingAtContractor
                                               .Select(l => Docflow.Resources.CopyDocumentLocatedInFormat(Docflow.Resources.AtContractorTrackingShowing)));
          
          trackingState = originals;
          
          if (!string.IsNullOrEmpty(originalsAtContractor))
            trackingState = string.Join(string.IsNullOrEmpty(trackingState) ? string.Empty : "; \n", trackingState, originalsAtContractor);
          
          if (!string.IsNullOrEmpty(exchangeLocation))
            trackingState = string.Join(string.IsNullOrEmpty(trackingState) ? string.Empty : "; \n", trackingState, exchangeLocation);
          
          if (!string.IsNullOrEmpty(copies))
            trackingState = string.Join(string.IsNullOrEmpty(trackingState) ? string.Empty : "; \n", trackingState, copies);
          
          if (!string.IsNullOrEmpty(copiesAtContractor))
            trackingState = string.Join(string.IsNullOrEmpty(trackingState) ? string.Empty : "; \n", trackingState, copiesAtContractor);
        }
        
        if (string.IsNullOrEmpty(trackingState) && _obj.ExchangeState == null)
        {
          if (_obj.CaseFile != null)
            trackingState = Sungero.Docflow.Resources.InFilelistFormat(_obj.CaseFile.Index, _obj.CaseFile.Title);
          else if (_obj.RegistrationState == Docflow.OfficialDocument.RegistrationState.Registered && _obj.DocumentRegister != null && _obj.DocumentRegister.RegistrationGroup != null)
            trackingState = Sungero.Docflow.Resources.InRegistrationGroupOfFormat(_obj.DocumentRegister.RegistrationGroup);
        }
        return trackingState;
      }
    }
    
    /// <summary>
    /// Проверить, изменялась ли только версия.
    /// </summary>
    /// <returns>Признак измененности.</returns>
    public bool IsOnlyVersionChanged()
    {
      // Свойства, которые меняются при изменении тела.
      var versionChangingProperties = new List<IPropertyStateBase>()
      {
        _obj.State.Properties.Modified,
        _obj.State.Properties.AssociatedApplication,
        _obj.State.Properties.Versions,
        _obj.State.Properties.LastVersionChanged
      };
      
      var onlyBodyPropertiesChanged = !_obj.State.Properties
        .Where(p => (p as IPropertyState).IsChanged && !versionChangingProperties.Contains(p)).Any();

      // У документов, полученных из сервиса обмена, могло измениться местонахождение.
      var locationChanged = _obj.LocationState != Functions.OfficialDocument.GetLocationState(_obj);
      
      return onlyBodyPropertiesChanged && !locationChanged;
    }
    
    /// <summary>
    /// Получить местонахождение предыдущих версий документа в сервисе обмена.
    /// </summary>
    /// <returns>Список местонахождений предыдущих версий.</returns>
    private List<string> GetOldVersionsExchangeLocation()
    {
      var result = new List<string>();
      var infos = Sungero.Exchange.ExchangeDocumentInfos.GetAll(x => Equals(x.Document, _obj)).ToList();
      foreach (var version in _obj.Versions.Where(v => !Equals(v, _obj.LastVersion)).OrderByDescending(v => v.Number))
      {
        var exchangeDocumentInfo = infos.FirstOrDefault(x => x.VersionId == version.Id);
        if (exchangeDocumentInfo == null)
          continue;
        
        var exchangeService = ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(exchangeDocumentInfo.Box).Name;
        var isIncoming = exchangeDocumentInfo.MessageType == Sungero.Exchange.ExchangeDocumentInfo.MessageType.Incoming;
        var prefix = isIncoming ? OfficialDocuments.Resources.DocumentIsReceivedFromFormat(exchangeService) : OfficialDocuments.Resources.DocumentIsSentToFormat(exchangeService);
        var detailed = this.GetExchangeState(exchangeDocumentInfo);
        
        if (!string.IsNullOrEmpty(detailed))
          result.Add(string.Format("{0}. {1}", prefix, OfficialDocuments.Resources.LocationVersionFormat(detailed, version.Number)));
        else
          result.Add(string.Format("{0}{1}", prefix, OfficialDocuments.Resources.LocationVersionFormat(string.Empty, version.Number)));
        
        if (exchangeDocumentInfo.ExchangeState == ExchangeState.Signed)
          break;
      }
      
      return result;
    }
    
    /// <summary>
    /// Получить статус документа в сервисе обмена.
    /// </summary>
    /// <param name="exchangeDocumentInfo">Информация о документе.</param>
    /// <returns>Статус в сервисе обмена.</returns>
    private string GetExchangeState(Exchange.IExchangeDocumentInfo exchangeDocumentInfo)
    {
      var result = string.Empty;
      if (exchangeDocumentInfo == null || exchangeDocumentInfo.ExchangeState == null)
        return result;
      if (exchangeDocumentInfo.ExchangeState == ExchangeState.Signed || exchangeDocumentInfo.ExchangeState == ExchangeState.Obsolete ||
          exchangeDocumentInfo.ExchangeState == ExchangeState.Rejected || exchangeDocumentInfo.ExchangeState == ExchangeState.Terminated)
      {
        // Подписан, или аннулирован, или отказано в подписании, или отозван.
        result = _obj.Info.Properties.ExchangeState.GetLocalizedValue(exchangeDocumentInfo.ExchangeState);
      }
      else if (exchangeDocumentInfo.ExchangeState == ExchangeState.SignAwaited)
      {
        // Ожидается подписание контрагентом.
        result = OfficialDocuments.Resources.ExchangeStateSignAwaited;
      }
      else if (exchangeDocumentInfo.ExchangeState == ExchangeState.SignRequired)
      {
        // Требуется подписание.
        result = OfficialDocuments.Resources.ExchangeStateSignRequired;
      }
      
      return result;
    }
    
    /// <summary>
    /// Получить местонахождение документа в сервисе обмена.
    /// </summary>
    /// <returns>Местонахождение документа в сервисе обмена. Пусто - если документ не ходил через сервис обмена.</returns>
    public string GetExchangeLocation()
    {
      if (_obj.LastVersion == null)
        return string.Empty;
      
      var result = string.Empty;
      var lastVersionId = _obj.LastVersion.Id;
      var accountDocument = AccountingDocumentBases.As(_obj);
      var isFormalized = accountDocument != null && accountDocument.IsFormalized == true;
      
      // У документов с титулом покупателя инфошка только на титул продавца.
      if (isFormalized && accountDocument.BuyerTitleId == lastVersionId)
        lastVersionId = accountDocument.SellerTitleId.Value;

      var exchangeDocumentInfo = Sungero.Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetExDocumentInfoFromVersion(_obj, lastVersionId);
      var lastVersionIsSigned = exchangeDocumentInfo != null && exchangeDocumentInfo.ExchangeState == ExchangeState.Signed;
      if (_obj.Versions.Count > 1 && !lastVersionIsSigned && !isFormalized)
      {
        var oldVersionLocations = this.GetOldVersionsExchangeLocation();
        if (oldVersionLocations.Any())
          result = string.Join("; \n", oldVersionLocations);
      }

      if (exchangeDocumentInfo == null)
        return result;
      
      var exchangeService = ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(exchangeDocumentInfo.Box).Name;
      var isIncoming = exchangeDocumentInfo.MessageType == Sungero.Exchange.ExchangeDocumentInfo.MessageType.Incoming;
      var prefix = isIncoming ? OfficialDocuments.Resources.DocumentIsReceivedFromFormat(exchangeService) : OfficialDocuments.Resources.DocumentIsSentToFormat(exchangeService);
      var detailed = this.GetExchangeState(exchangeDocumentInfo);
      
      var main = string.Format("{0}. {1}", prefix, detailed);
      if (isIncoming && exchangeDocumentInfo.InvoiceState == Exchange.ExchangeDocumentInfo.InvoiceState.Rejected)
        main = string.Format("{0}. {1}", main.TrimEnd(' ', '.'), OfficialDocuments.Resources.LocationIncomingInvoiceRejected);
      if (!isIncoming && exchangeDocumentInfo.InvoiceState == Exchange.ExchangeDocumentInfo.InvoiceState.Rejected)
        main = string.Format("{0}. {1}", main.TrimEnd(' ', '.'), OfficialDocuments.Resources.LocationOutgoingInvoiceRejected);
      if (!string.IsNullOrEmpty(result))
        main = OfficialDocuments.Resources.LocationVersionFormat(main, _obj.Versions.Where(v => v.Id == lastVersionId).Single().Number);
      
      return string.Join(string.IsNullOrEmpty(result) ? string.Empty : "; \n", main, result);
    }
    
    /// <summary>
    /// Получить сведения об организации, подписавшей документ, из сведений о документе обмена и подписи.
    /// </summary>
    /// <param name="signature">Подпись.</param>
    /// <returns>Наименование и ИНН организации.</returns>
    public virtual Exchange.Structures.Module.IOrganizationInfo GetSigningOrganizationFromExchangeInfo(Sungero.Domain.Shared.ISignature signature)
    {
      var info = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(_obj);
      if (info == null || signature == null)
        return Exchange.Structures.Module.OrganizationInfo.Create();
      else
        return Exchange.PublicFunctions.ExchangeDocumentInfo.GetSigningOrganizationInfo(info, signature);
    }
    
    /// <summary>
    /// Есть ли новые задачи в работе после прекращения задачи на согласование документа по регламенту/процессу.
    /// </summary>
    /// <param name="abortedDate">Дата и время прекращения задачи.</param>
    /// <returns>True - есть задачи в работе, False - нет.</returns>
    [Public]
    public virtual bool HasNewApprovalTasks(DateTime? abortedDate)
    {
      var documentTaskIds = this.GetTaskIdsWhereDocumentInRequredGroup();
      return ApprovalTasks.GetAll(t => documentTaskIds.Contains(t.Id) && t.Started > abortedDate).Any() ||
        Sungero.DocflowApproval.DocumentFlowTasks.GetAll(t => documentTaskIds.Contains(t.Id) && t.Started > abortedDate).Any();
    }
    
    /// <summary>
    /// Получить ИД задач, в которых документ вложен в обязательные группы.
    /// </summary>
    /// <returns>Список ИД задач.</returns>
    [Remote]
    public virtual List<long> GetTaskIdsWhereDocumentInRequredGroup()
    {
      var ids = new List<long>();
      using (var session = new Domain.Session())
      {
        var innerSession = (Sungero.Domain.ISession)session
          .GetType()
          .GetField("InnerSession", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          .GetValue(session);
        
        var docGuid = _obj.GetEntityMetadata().GetOriginal().NameGuid;
        var tasks = innerSession.GetAll<ITask>()
          .Where(t => t.Status != Workflow.Task.Status.Draft)
          .Where(t => t.AttachmentDetails
                 .Any(att => att.AttachmentId == _obj.Id && att.EntityTypeGuid == docGuid))
          .ToList();
        var attachmentDetails = tasks
          .SelectMany(t => t.AttachmentDetails)
          .Where(ad => ad.AttachmentId == _obj.Id && ad.EntityTypeGuid == docGuid)
          .ToList();
        
        foreach (var task in tasks)
        {
          var groups = attachmentDetails.Any(x => x.Group.IsRequired);
          
          // Другие задачи, где документы вложены в основную, но необязательную группу и их удаление приведет к нарушению процесса.
          var otherTasks = Functions.Module.GetServerEntityFunctionResult(task, "DocumentInRequredGroup", new List<object> { _obj });
          if (groups || (otherTasks != null && (bool)otherTasks))
            ids.Add(task.Id);
        }
      }
      return ids;
    }
    
    /// <summary>
    /// Проверить наличие согласующих или утверждающих подписей на документе.
    /// </summary>
    /// <returns>True, если есть хоть одна подпись для отображения в отчете.</returns>
    [Remote(IsPure = true)]
    public bool HasSignatureForApprovalSheetReport()
    {
      var setting = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(null);
      var showNotApproveSign = setting != null ? setting.ShowNotApproveSign == true : false;
      
      foreach (var version in _obj.Versions)
      {
        if (Signatures.Get(version, q => q.Where(s => (showNotApproveSign || s.SignatureType != SignatureType.NotEndorsing) && s.IsExternal != true).Take(1)).Any())
          return true;
      }
      
      return false;
    }
    
    /// <summary>
    /// Получить все задачи на ознакомление.
    /// </summary>
    /// <returns>Задачи на ознакомление с документом.</returns>
    [Public, Remote(IsPure = true)]
    public List<RecordManagement.IAcquaintanceTask> GetAcquaintanceTasks()
    {
      var tasks = RecordManagement.AcquaintanceTasks.GetAll()
        .Where(x => x.Status == Workflow.Task.Status.InProcess ||
               x.Status == Workflow.Task.Status.Suspended ||
               x.Status == Workflow.Task.Status.Completed ||
               x.Status == Workflow.Task.Status.Aborted)
        .Where(x => x.AttachmentDetails.Any(d => d.GroupId.ToString() == "19c1e8c9-e896-4d93-a1e8-4e22b932c1ce" &&
                                            d.AttachmentId == _obj.Id))
        .ToList();
      
      return tasks;
    }
    
    /// <summary>
    /// Определить, есть ли задачи на ознакомление документа.
    /// </summary>
    /// <param name="versionNumber">Номер версии.</param>
    /// <param name="includeCompleted">Учитывать выполненные задачи.</param>
    /// <param name="includeAborted">Учитывать прекращенные задачи.</param>
    /// <returns>True, если есть.</returns>
    [Public, Remote(IsPure = true)]
    public virtual bool HasAcquaintanceTasks(int? versionNumber, bool includeCompleted, bool includeAborted)
    {
      var anyTasks = false;
      Sungero.Core.AccessRights.AllowRead(
        () =>
        {
          // Искать не только задачи, где документ является основным (IsMainDocument == true),
          // т.к. Solo и Jazz проверяют ещё и приложения.
          anyTasks = RecordManagement.AcquaintanceTasks.GetAll()
            .Where(x => x.Status == Workflow.Task.Status.InProcess ||
                   x.Status == Workflow.Task.Status.Suspended ||
                   includeCompleted && x.Status == Workflow.Task.Status.Completed ||
                   includeAborted && x.Status == Workflow.Task.Status.Aborted)
            .Where(x => x.AcquaintanceVersions.Any(v => v.DocumentId == _obj.Id &&
                                                   v.Number == versionNumber))
            .Any();
        });
      
      return anyTasks;
    }
    
    /// <summary>
    /// Получить задачи на рассмотрение по текущему документу.
    /// </summary>
    /// <returns>Задачи на рассмотрение по текущему документу.</returns>
    [Public]
    public List<RecordManagement.IDocumentReviewTask> GetDocumentReviewTasks()
    {
      var docGuid = _obj.GetEntityMetadata().GetOriginal().NameGuid;
      var reviewTaskDocumentGroupGuid = Constants.Module.TaskMainGroup.DocumentReviewTask;
      var tasks = RecordManagement.DocumentReviewTasks.GetAll()
        .Where(x => x.Status == Workflow.Task.Status.InProcess ||
               x.Status == Workflow.Task.Status.Suspended)
        .Where(x => x.AttachmentDetails.Any(att => att.AttachmentId == _obj.Id &&
                                            att.EntityTypeGuid == docGuid &&
                                            att.GroupId == reviewTaskDocumentGroupGuid))
        .ToList();
      
      return tasks;
    }
    
    /// <summary>
    /// Получить руководителя НОР документа или сотрудника.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <param name="document">Документ. По нему определяется НОР. Если не указан, будет выбрана НОР сотрудника.</param>
    /// <returns>Подписывающий.</returns>
    public static IEmployee GetBusinessUnitCEO(IEmployee employee, IOfficialDocument document)
    {
      var businessUnit = document != null ? document.BusinessUnit : Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(employee);
      return businessUnit != null ? businessUnit.CEO : null;
    }
    
    /// <summary>
    /// Получить подписантов, которые могут подписывать соглашение об аннулировании.
    /// </summary>
    /// <returns>ИД подписантов.</returns>
    [Remote(IsPure = true)]
    public virtual List<long> GetSignatoriesIdsForCancellationAgreement()
    {
      if (_obj == null)
        return new List<long>();
      
      var settings = this.GetSignatureSettingsForCancellationAgreement();
      
      return this.ExpandSignatoriesBySignatureSettings(settings);
    }
    
    /// <summary>
    /// Получить права подписи для соглашения об аннулировании по основному документу.
    /// </summary>
    /// <returns>Права подписи на соглашение об аннулировании по основному документу.</returns>
    [Public]
    public virtual IQueryable<ISignatureSetting> GetSignatureSettingsForCancellationAgreement()
    {
      // Права подписи, подходящие для подписания основного документа.
      var settingIds = Functions.OfficialDocument.GetSignatureSettingsQuery(_obj).Select(s => s.Id).ToList();
      
      // Права подписи, подходящие для подписания соглашения об аннулировании.
      // При создании соглашения об аннулировании самого соглашения еще не существует, поэтому опираемся на вид документа, а не на сам документ.
      var cancellationAgreementKind = Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Exchange.PublicConstants.Module.Initialize.CancellationAgreementKind);
      var businessUnits = this.GetBusinessUnits();
      var kinds = new List<IDocumentKind> { cancellationAgreementKind };
      var cancellationAgreementSignatureSettingIds = GetSignatureSettingsQuery(businessUnits, kinds, _obj.Department, cancellationAgreementKind.DocumentFlow)
        .Select(s => s.Id)
        .ToList();
      
      settingIds.AddRange(cancellationAgreementSignatureSettingIds);
      return SignatureSettings.GetAll(s => settingIds.Contains(s.Id));
    }
    
    /// <summary>
    /// Отфильтровать список подписантов, доступных для документа.
    /// </summary>
    /// <param name="query">Список подписантов.</param>
    /// <returns>Отфильтрованный список подписантов.</returns>
    [Public]
    public virtual IQueryable<IEmployee> FilterSignatories(IQueryable<IEmployee> query)
    {
      if (this.SignatorySettingWithAllUsersExist())
        return query;
      
      var signatories = this.GetSignatoriesIds();
      return query.Where(s => signatories.Contains(s.Id));
    }
    
    /// <summary>
    /// Возвращает список ИД подписывающих по критериям.
    /// </summary>
    /// <returns>Список тех, кто имеет право подписи.</returns>
    /// <remarks>Исключаются права подписи, выданные всем пользователям.</remarks>
    [Remote(IsPure = true)]
    public virtual List<long> GetSignatoriesIds()
    {
      if (_obj == null)
        return new List<long>();

      var settings = Functions.OfficialDocument.GetSignatureSettingsQuery(_obj);

      return this.ExpandSignatoriesBySignatureSettings(settings);
    }
    
    /// <summary>
    /// Получить развернутый список подписывающих по правам подписи.
    /// </summary>
    /// <param name="settings">Список прав подписи.</param>
    /// <returns>Список ИД сотрудников. </returns>
    /// <remarks>Исключаются права подписи, выданные всем пользователям.</remarks>
    public virtual List<long> ExpandSignatoriesBySignatureSettings(IQueryable<ISignatureSetting> settings)
    {
      var signatories = new List<long>();
      
      // Права подписи с сотрудником.
      signatories.AddRange(settings.Where(s => Employees.Is(s.Recipient)).Select(s => s.Recipient.Id));
      
      // Права подписи по группам.
      var groupIds = settings.Where(s => Groups.Is(s.Recipient) && s.Recipient.Sid != Constants.OfficialDocument.AllUsersSid)
        .Select(s => s.Recipient.Id)
        .ToList();
      
      foreach (var groupId in groupIds)
        signatories.AddRange(Functions.OfficialDocument.GetAllRecipientMembersIdsInGroup(groupId));

      return signatories.Distinct().ToList();
    }
    
    /// <summary>
    /// Проверить наличие права подписи со всеми сотрудниками.
    /// </summary>
    /// <returns>True - если есть право подписи со всеми сотрудниками.</returns>
    [Remote(IsPure = true)]
    public virtual bool SignatorySettingWithAllUsersExist()
    {
      var settings = Functions.OfficialDocument.GetSignatureSettingsQuery(_obj);
      
      return settings.Any(s => s.Recipient.Sid == Constants.OfficialDocument.AllUsersSid);
    }
    
    /// <summary>
    /// Получить подписывающего по умолчанию.
    /// </summary>
    /// <returns>Подписывающий по умолчанию.</returns>
    public virtual Sungero.Company.IEmployee GetDefaultSignatory()
    {
      var settingsQuery = Functions.OfficialDocument.GetSignatureSettingsQuery(_obj);
      var maxPriority = settingsQuery.Max(s => s.Priority);
      var signatoriesMaxPriority = settingsQuery.Where(s => s.Priority == maxPriority);
      
      if (signatoriesMaxPriority.Any(s => Groups.Is(s.Recipient) && s.Recipient.Sid == Constants.OfficialDocument.AllUsersSid))
        return null;
      
      var signatoriesIds = this.ExpandSignatoriesBySignatureSettings(signatoriesMaxPriority);
      
      if (signatoriesIds.Count() == 1)
        return Employees.Get(signatoriesIds.First());
      
      return null;
    }
    
    /// <summary>
    /// Получить список Ид участников группы.
    /// </summary>
    /// <param name="groupId">Ид группы.</param>
    /// <returns>Список Ид участников.</returns>
    [Public]
    public static List<long> GetAllRecipientMembersIdsInGroup(long groupId)
    {
      var employeesId = new List<long>();
      var commandText = string.Format(Queries.Module.GetAllRecipientMembers, groupId);
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = commandText;
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
            employeesId.Add(reader.GetInt64(0));
        }
      }
      return employeesId;
    }
    
    /// <summary>
    /// Проверить наличие права подписи у сотрудника.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>True, если сотрудник имеет право подписи, иначе - False.</returns>
    [Public, Remote(IsPure = true)]
    public virtual bool CanSignByEmployee(IEmployee employee)
    {
      if (_obj == null || employee == null)
        return false;
      
      return this.GetSignatureSettingsByEmployee(employee).Any();
    }
    
    /// <summary>
    /// Получить право подписи сотрудника по умолчанию.
    /// </summary>
    /// <param name="signatory">Сотрудник.</param>
    /// <returns>Право подписи сотрудника по умолчанию.</returns>
    [Public, Remote(IsPure = true)]
    public virtual ISignatureSetting GetDefaultSignatureSetting(IEmployee signatory)
    {
      var signatureSettingsByEmployee = this.GetSignatureSettingsByEmployee(signatory);
      return PublicFunctions.Module.GetOrderedSignatureSettings(signatureSettingsByEmployee).FirstOrDefault();
    }
    
    /// <summary>
    /// Получить права подписи у сотрудника c действующим сертификатом.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Список прав подписи.</returns>
    [Public, Remote(IsPure = true)]
    public virtual IQueryable<ISignatureSetting> GetSignatureSettingsWithCertificateByEmployee(IEmployee employee)
    {
      var now = Calendar.Now;
      return this.GetSignatureSettingsByEmployee(employee)
        .Where(s => (s.Certificate != null &&
                     ((!s.Certificate.NotAfter.HasValue || s.Certificate.NotAfter >= now) &&
                      (!s.Certificate.NotBefore.HasValue || s.Certificate.NotBefore <= now))) ||
               s.Certificate == null);
    }
    
    /// <summary>
    /// Получить права подписи у сотрудника.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Список прав подписи.</returns>
    [Public, Remote(IsPure = true)]
    public virtual IQueryable<ISignatureSetting> GetSignatureSettingsByEmployee(IEmployee employee)
    {
      if (_obj == null || employee == null)
        return Enumerable.Empty<ISignatureSetting>().AsQueryable();

      var suitableSignatureSettings = Functions.OfficialDocument.GetSignatureSettingsQuery(_obj);
      return Functions.SignatureSetting.FilterSignatureSettingsBySignatory(suitableSignatureSettings, employee);
    }
    
    /// <summary>
    /// Получить права подписания документов.
    /// </summary>
    /// <returns>Список подходящих правил.</returns>
    [Public, Remote(IsPure = true)]
    public virtual IQueryable<ISignatureSetting> GetSignatureSettingsQuery()
    {
      if (_obj.DocumentKind == null)
        return Enumerable.Empty<ISignatureSetting>().AsQueryable();
      
      var docflow = _obj.DocumentKind.DocumentFlow;
      
      var businessUnits = this.GetBusinessUnits();
      
      var kinds = this.GetDocumentKinds();
      
      var settings = GetSignatureSettingsQuery(businessUnits, kinds, _obj.Department, docflow);
      return settings;
    }
    
    /// <summary>
    /// Получить права подписания документов по заданным параметрам.
    /// </summary>
    /// <param name="businessUnits">Список наших организаций.</param>
    /// <param name="kinds">Список видов документов.</param>
    /// <param name="department">Подразделение.</param>
    /// <param name="docflow">Документопоток.</param>
    /// <returns>Список подходящих правил.</returns>
    public static IQueryable<ISignatureSetting> GetSignatureSettingsQuery(List<IBusinessUnit> businessUnits,
                                                                          List<IDocumentKind> kinds,
                                                                          IDepartment department,
                                                                          Enumeration? docflow)
    {
      return Functions.SignatureSetting.GetSignatureSettings(businessUnits, kinds)
        .Where(s => s.DocumentFlow == Docflow.SignatureSetting.DocumentFlow.All || s.DocumentFlow == docflow)
        .Where(s => !s.Departments.Any() || department == null || s.Departments.Any(d => Equals(d.Department, department)));
    }
    
    /// <summary>
    /// Получить наши организации для фильтрации подходящих прав подписи.
    /// </summary>
    /// <returns>Наши организации.</returns>
    public virtual List<IBusinessUnit> GetBusinessUnits()
    {
      var businessUnits = new List<IBusinessUnit>() { };
      
      if (_obj.BusinessUnit != null)
        businessUnits.Add(_obj.BusinessUnit);
      
      return businessUnits;
    }
    
    /// <summary>
    /// Получить виды документов для фильтрации подходящих прав подписи.
    /// </summary>
    /// <returns>Виды документов.</returns>
    public virtual List<IDocumentKind> GetDocumentKinds()
    {
      var kinds = new List<IDocumentKind>() { };
      
      if (_obj.DocumentKind != null)
        kinds.Add(_obj.DocumentKind);
      
      return kinds;
    }
    
    /// <summary>
    /// Получить права подписания документов.
    /// </summary>
    /// <param name="employee">Сотрудник, для которого запрашиваются права.</param>
    /// <returns>Список подходящих правил.</returns>
    [Public, Remote(IsPure = true)]
    public virtual List<ISignatureSetting> GetSignatureSettings(IEmployee employee)
    {
      return this.GetSignatureSettingsByEmployee(employee).ToList();
    }

    /// <summary>
    /// Заполнить статус согласования "Подписан".
    /// </summary>
    [Remote]
    public void SetInternalApprovalStateToSigned()
    {
      if (_obj.InternalApprovalState == InternalApprovalState.Aborted ||
          _obj.InternalApprovalState == InternalApprovalState.Signed)
        return;
      
      if (Equals(_obj.InternalApprovalState, InternalApprovalState.Signed))
        return;
      
      // HACK: если нет прав, то статус будет заполнен независимо от прав доступа.
      if (!_obj.AccessRights.CanUpdate())
      {
        using (var session = new Sungero.Domain.Session())
        {
          Functions.Module.AddFullAccessRightsInSession(session, _obj);
          _obj.InternalApprovalState = InternalApprovalState.Signed;
          _obj.Save();
        }
      }
      else
        _obj.InternalApprovalState = InternalApprovalState.Signed;
    }
    
    /// <summary>
    /// Заполнить подписывающего в карточке документа.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    [Public, Remote]
    public virtual void SetDocumentSignatory(IEmployee employee)
    {
      if (Equals(_obj.OurSignatory, employee))
        return;
      
      // HACK: если нет прав, то подписывающий будет заполнен независимо от прав доступа.
      if (!_obj.AccessRights.CanUpdate())
      {
        using (var session = new Sungero.Domain.Session())
        {
          Functions.Module.AddFullAccessRightsInSession(session, _obj);
          _obj.OurSignatory = employee;
          _obj.Save();
        }
      }
      else
      {
        _obj.OurSignatory = employee;
        _obj.Save();
      }
    }
    
    /// <summary>
    /// Заполнить основание в карточке документа.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <param name="e">Аргументы события подписания.</param>
    /// <param name="changedSignatory">Признак смены подписывающего.</param>
    public virtual void SetOurSigningReason(IEmployee employee, Sungero.Domain.BeforeSigningEventArgs e, bool changedSignatory)
    {
      var ourSigningReason = this.GetSuitableOurSigningReason(employee, e.Certificate, changedSignatory);
      
      if (ourSigningReason == null)
        e.AddError(Docflow.Resources.NoRightsToApproveDocument);
      
      if (Equals(_obj.OurSigningReason, ourSigningReason))
        return;
      
      // HACK: если нет прав, то основание будет заполнено независимо от прав доступа.
      if (!_obj.AccessRights.CanUpdate())
      {
        using (var session = new Sungero.Domain.Session())
        {
          Functions.Module.AddFullAccessRightsInSession(session, _obj);
          _obj.OurSigningReason = ourSigningReason;
          _obj.Save();
        }
      }
      else
      {
        _obj.OurSigningReason = ourSigningReason;
        _obj.Save();
      }
    }
    
    /// <summary>
    /// Получить подходящее право подписи.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <param name="changedSignatory">Признак смены подписывающего.</param>
    /// <returns>Право подписи.</returns>
    public virtual ISignatureSetting GetSuitableOurSigningReason(IEmployee employee, ICertificate certificate, bool changedSignatory = false)
    {
      var documentSignatureSettings = this.GetSignatureSettingsByEmployee(employee).ToList();
      
      if (!documentSignatureSettings.Any())
        return null;
      
      if (!changedSignatory && _obj.OurSigningReason != null && documentSignatureSettings.Contains(_obj.OurSigningReason) &&
          this.OurSigningReasonIsValid(_obj.OurSigningReason, certificate, documentSignatureSettings))
        return _obj.OurSigningReason;
      
      return PublicFunctions.Module.GetOurSigningReasonWithHighPriority(documentSignatureSettings, certificate);
    }
    
    /// <summary>
    /// Заполнить Единый рег. № из эл. доверенности в подпись.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <param name="signature">Подпись.</param>
    /// <param name="certificate">Сертификат для подписания.</param>
    public virtual void SetUnifiedRegistrationNumber(Company.IEmployee employee, Sungero.Domain.Shared.ISignature signature, ICertificate certificate)
    {
      if (signature.SignCertificate == null)
        return;
      
      this.SetUnifiedRegistrationNumber(_obj.OurSigningReason, signature, certificate);
    }
    
    /// <summary>
    /// Заполнить Единый рег. № из эл. доверенности в подпись.
    /// </summary>
    /// <param name="ourSigningReason">Основание.</param>
    /// <param name="signature">Подпись.</param>
    /// <param name="certificate">Сертификат для подписания.</param>
    public virtual void SetUnifiedRegistrationNumber(ISignatureSetting ourSigningReason, Sungero.Domain.Shared.ISignature signature, ICertificate certificate)
    {
      if (signature.SignCertificate != null && ourSigningReason != null && PublicFunctions.SignatureSetting.ReasonIsFormalizedPoA(ourSigningReason))
      {
        var formalizedPoA = Docflow.FormalizedPowerOfAttorneys.As(ourSigningReason.Document);
        
        PublicFunctions.Module.AddUnsignedAttribute(signature, Constants.Module.UnsignedAdditionalInfoKeyFPoA, formalizedPoA.UnifiedRegistrationNumber);
      }
    }
    
    /// <summary>
    /// Проверить возможность подписания с выбранным основанием.
    /// </summary>
    /// <param name="ourSigningReason">Основание документа.</param>
    /// <param name="certificate">Сертификат для подписания.</param>
    /// <param name="settings">Список прав подписи.</param>
    /// <returns>Признак того, подходит ли основания для подписания документа.</returns>
    [Public]
    public virtual bool OurSigningReasonIsValid(ISignatureSetting ourSigningReason, ICertificate certificate, List<ISignatureSetting> settings)
    {
      // Проверка, что сертификат в праве подписи совпадает с сертификатом, выбранным при подписании.
      if (certificate != null && ourSigningReason.Certificate != null && !certificate.Equals(ourSigningReason.Certificate))
        return false;
      
      // Проверка, что нет более подходящих по сертификату, указанному при подписании, прав подписи, если в Основании не указан сертификат.
      if (certificate != null && ourSigningReason.Certificate == null &&
          settings.Any(s => s.Certificate != null && certificate.Equals(s.Certificate)))
        return false;
      
      // Проверка, что нет более подходящих прав подписи при подписании простой подписью, если в Основании указан сертификат.
      if (certificate == null && ourSigningReason.Certificate != null && settings.Any(s => s.Certificate == null))
        return false;
      
      // Проверка, что срок действия электронной доверенности, указанной в праве подписи, не вышел.
      if (PublicFunctions.SignatureSetting.Remote.FormalizedPowerOfAttorneyIsExpired(ourSigningReason))
        return false;
      
      return true;
    }
    
    /// <summary>
    /// Получить электронную доверенность.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <returns>Электронная доверенность.</returns>
    [Public]
    public virtual IFormalizedPowerOfAttorney GetFormalizedPoA(IEmployee employee, ICertificate certificate)
    {
      var ourSigningReason = this.GetSuitableOurSigningReason(employee, certificate);
      
      if (ourSigningReason == null)
      {
        var businessUnit = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(_obj)?.RootBox.BusinessUnit ?? this.GetBusinessUnits().FirstOrDefault();
        
        if (businessUnit != null)
          return Docflow.PublicFunctions.Module.GetFormalizedPoAByEmployee(businessUnit, employee);
      }
      else if (Docflow.PublicFunctions.SignatureSetting.ReasonIsFormalizedPoA(ourSigningReason))
        return Docflow.FormalizedPowerOfAttorneys.As(ourSigningReason.Document);
      
      return null;
    }
    
    [Public, Remote]
    public virtual string GetFormalizedPoAUnifiedRegNo(IEmployee employee, ICertificate certificate)
    {
      return this.GetFormalizedPoA(employee, certificate)?.UnifiedRegistrationNumber;
    }
    
    /// <summary>
    /// Получить задания на возврат по документу.
    /// </summary>
    /// <param name="returnTask">Задача.</param>
    /// <returns>Задания на возврат.</returns>
    [Remote(IsPure = true)]
    public static List<Sungero.Workflow.IAssignment> GetReturnAssignments(Sungero.Workflow.ITask returnTask)
    {
      return GetReturnAssignments(new List<Sungero.Workflow.ITask>() { returnTask });
    }
    
    /// <summary>
    /// Получить задания на возврат по документу.
    /// </summary>
    /// <param name="returnTasks">Задачи.</param>
    /// <returns>Задания на возврат.</returns>
    [Remote(IsPure = true)]
    public static List<Sungero.Workflow.IAssignment> GetReturnAssignments(List<Sungero.Workflow.ITask> returnTasks)
    {
      var assignments = new List<Sungero.Workflow.IAssignment>();
      assignments.AddRange(CheckReturnCheckAssignments.GetAll(a => returnTasks.Contains(a.Task) && a.Status == Workflow.AssignmentBase.Status.InProcess).ToList());
      assignments.AddRange(CheckReturnAssignments.GetAll(a => returnTasks.Contains(a.Task) && a.Status == Workflow.AssignmentBase.Status.InProcess).ToList());
      assignments.AddRange(ApprovalCheckReturnAssignments.GetAll(a => returnTasks.Contains(a.Task) && a.Status == Workflow.AssignmentBase.Status.InProcess).ToList());
      assignments.AddRange(CheckReturnFromCounterpartyAssignments.GetAll(a => returnTasks.Contains(a.Task) && a.Status == Workflow.AssignmentBase.Status.InProcess).ToList());
      
      return assignments.ToList();
    }
    
    /// <summary>
    /// Возвращает ошибки валидации подписания документа.
    /// </summary>
    /// <param name="checkSignatureSettings">Проверять права подписи.</param>
    /// <returns>Ошибки валидации.</returns>
    [Remote(IsPure = true), Public]
    public virtual List<string> GetApprovalValidationErrors(bool checkSignatureSettings)
    {
      var errors = new List<string>();
      if (!_obj.AccessRights.CanApprove())
        errors.Add(Docflow.Resources.NoAccessRightsToApprove);
      
      if (checkSignatureSettings)
      {
        // Поиск прав подписи документа.
        var canSignByEmployee = Functions.OfficialDocument.CanSignByEmployee(_obj, Employees.Current);
        
        if (_obj.AccessRights.CanApprove() && !canSignByEmployee)
          errors.Add(Docflow.Resources.NoRightsToApproveDocument);
      }
      
      // Если документ заблокирован - утвердить его нельзя (т.к. мы должны заполнить два поля при утверждении).
      errors.AddRange(Functions.OfficialDocument.GetDocumentLockErrors(_obj));
      return errors;
    }

    /// <summary>
    /// Возвращает ошибки заблокированности документа.
    /// </summary>
    /// <returns>Ошибки заблокированности документа.</returns>
    [Public]
    public virtual List<string> GetDocumentLockErrors()
    {
      var errors = new List<string>();
      var lockInfo = Locks.GetLockInfo(_obj);
      var canSignLockedDocument = Functions.OfficialDocument.CanSignLockedDocument(_obj);
      
      if (_obj.AccessRights.CanApprove() && lockInfo != null && lockInfo.IsLockedByOther && !canSignLockedDocument)
        errors.Add(lockInfo.LockedMessage);
      
      if (_obj.LastVersion != null)
      {
        var lockInfoVersion = Locks.GetLockInfo(_obj.LastVersion.Body);
        if (_obj.AccessRights.CanApprove() && lockInfoVersion != null && lockInfoVersion.IsLockedByOther)
          errors.Add(lockInfoVersion.LockedMessage);
      }
      return errors;
    }
    
    #region История смены состояний
    
    public virtual System.Collections.Generic.IEnumerable<Sungero.Docflow.Structures.OfficialDocument.HistoryOperation> StatusChangeHistoryOperations(Sungero.Content.DocumentHistoryEventArgs e)
    {
      var isUpdateAction = e.Action == Sungero.CoreEntities.History.Action.Update;
      var isDeleteVersion = e.Operation == new Enumeration(Constants.OfficialDocument.Operation.DeleteVersion);
      var properties = _obj.State.Properties;
      
      // Статус "Согласование".
      // Возвращаем исходный комментарий из события для корректной записи в историю о преобразовании документа (306963).
      if (_obj.InternalApprovalState != properties.InternalApprovalState.OriginalValue)
      {
        yield return HistoryOperation.Create(
          Functions.OfficialDocument.GetHistoryOperationTextByLifeCycleState(_obj.InternalApprovalState, Constants.OfficialDocument.Operation.Prefix.InternalApproval, isUpdateAction),
          e.Comment);
      }
      
      // Статус "Согл. с контрагентом".
      if (_obj.ExternalApprovalState != properties.ExternalApprovalState.OriginalValue)
      {
        yield return HistoryOperation.Create(
          Functions.OfficialDocument.GetHistoryOperationTextByLifeCycleState(_obj.ExternalApprovalState, Constants.OfficialDocument.Operation.Prefix.ExternalApproval, isUpdateAction),
          null);
      }

      // Статус "Исполнение".
      if (_obj.ExecutionState != properties.ExecutionState.OriginalValue)
      {
        yield return HistoryOperation.Create(
          Functions.OfficialDocument.GetHistoryOperationTextByLifeCycleState(_obj.ExecutionState, Constants.OfficialDocument.Operation.Prefix.Execution, isUpdateAction),
          null);
      }

      // Статус "Контроль исполнения".
      if (_obj.ControlExecutionState != properties.ControlExecutionState.OriginalValue)
      {
        yield return HistoryOperation.Create(
          Functions.OfficialDocument.GetHistoryOperationTextByLifeCycleState(_obj.ControlExecutionState, Constants.OfficialDocument.Operation.Prefix.ControlExecution, isUpdateAction),
          null);
      }

      // Статус "Жизненный цикл".
      if (_obj.LifeCycleState != properties.LifeCycleState.OriginalValue && !isDeleteVersion)
      {
        yield return HistoryOperation.Create(
          Functions.OfficialDocument.GetHistoryOperationTextByLifeCycleState(_obj.LifeCycleState, Constants.OfficialDocument.Operation.Prefix.LifeCycle, isUpdateAction),
          null);
      }
    }
    
    /// <summary>
    /// Получить операцию по статусу.
    /// </summary>
    /// <param name="state">Статус.</param>
    /// <param name="statePrefix">Префикс.</param>
    /// <param name="isUpdateAction">Признак обновления.</param>
    /// <returns>Операция по статусу.</returns>
    [Obsolete("Метод не используется с 30.08.2023 и версии 4.8. Используйте метод GetHistoryOperationTextByLifeCycleState.")]
    public static Enumeration? GetHistoryOperationByLifeCycleState(Enumeration? state, string statePrefix, bool isUpdateAction)
    {
      var text = GetHistoryOperationTextByLifeCycleState(state, statePrefix, isUpdateAction);
      return text == null ? (Enumeration?)null : new Enumeration(text);
    }
    
    /// <summary>
    /// Получить операцию по статусу.
    /// </summary>
    /// <param name="state">Статус.</param>
    /// <param name="statePrefix">Префикс.</param>
    /// <param name="isUpdateAction">Признак обновления.</param>
    /// <returns>Операция по статусу.</returns>
    public static string GetHistoryOperationTextByLifeCycleState(Enumeration? state, string statePrefix, bool isUpdateAction)
    {
      if (state != null)
      {
        var stateName = statePrefix + state.ToString();
        if (stateName.Length > Constants.OfficialDocument.Operation.OperationPropertyLength)
          stateName = stateName.Substring(0, Sungero.Docflow.Constants.OfficialDocument.Operation.OperationPropertyLength);
        return stateName;
      }
      else
      {
        return statePrefix + Constants.OfficialDocument.ClearStateOperation;
      }
    }
    
    /// <summary>
    /// Записать историю смены состояний.
    /// </summary>
    /// <param name="e">Аргументы события "До сохранения истории".</param>
    /// <param name="operations">Изменения значений статусов.</param>
    /// <param name="historyRecordOverwritten">Признак, что операция истории уже была перезаписана.</param>
    public void WriteStatusChangeHistory(Sungero.Content.DocumentHistoryEventArgs e,
                                         System.Collections.Generic.IEnumerable<Sungero.Docflow.Structures.OfficialDocument.HistoryOperation> operations,
                                         bool historyRecordOverwritten)
    {
      var isCreateAction = e.Action == Sungero.CoreEntities.History.Action.Create;
      var isChangeTypeAction = e.Action == Sungero.CoreEntities.History.Action.ChangeType;
      var documentKindOriginalValue = _obj.State.Properties.DocumentKind.OriginalValue;
      var documentTypeChange = documentKindOriginalValue != null &&
        !documentKindOriginalValue.DocumentType.Equals(_obj.DocumentKind.DocumentType);
      
      if (isCreateAction || isChangeTypeAction)
      {
        this.WriteHistory(e, operations);
        return;
      }
      
      var list = operations.ToList();
      if (documentTypeChange || !list.Any())
        return;
      
      if (!historyRecordOverwritten)
      {
        e.Operation = new Enumeration(list[0].Operation);
        e.Comment = list[0].Comment;
        this.WriteHistory(e, list.Skip(1));
      }
      else
      {
        this.WriteHistory(e, list);
      }
    }
    
    private void WriteHistory(Sungero.Content.DocumentHistoryEventArgs e, IEnumerable<HistoryOperation> operations)
    {
      foreach (var operation in operations)
      {
        e.Write(new Enumeration(operation.Operation), null, operation.Comment ?? string.Empty);
      }
    }
    
    #endregion
    
    /// <summary>
    /// Получить правила согласования для документа.
    /// </summary>
    /// <returns>Правила согласования, доступные для документа в порядке убывания приоритета.</returns>
    [Remote, Public]
    public virtual List<IApprovalRuleBase> GetApprovalRules()
    {
      return Docflow.PublicFunctions.ApprovalRuleBase.Remote.GetAvailableRulesByDocument(_obj)
        .OrderByDescending(r => r.Priority)
        .ToList();
    }
    
    /// <summary>
    /// Получить правила согласования по умолчанию для документа.
    /// </summary>
    /// <returns>Правила согласования по умолчанию.</returns>
    /// <remarks>Если подходящих правил нет или их несколько, то вернется null.</remarks>
    [Remote, Public]
    public virtual IApprovalRuleBase GetDefaultApprovalRule()
    {
      var availableApprovalRules = Functions.OfficialDocument.GetApprovalRules(_obj);
      if (availableApprovalRules.Any())
      {
        var maxPriority = availableApprovalRules.Select(a => a.Priority).OrderByDescending(a => a).FirstOrDefault();
        var defaultApprovalRule = availableApprovalRules.Where(a => Equals(a.Priority, maxPriority));
        if (defaultApprovalRule.Count() == 1)
          return defaultApprovalRule.First();
      }
      return null;
    }
    
    /// <summary>
    /// Получить вид документа по умолчанию.
    /// </summary>
    /// <returns>Вид документа.</returns>
    [Public]
    public virtual IDocumentKind GetDefaultDocumentKind()
    {
      var availableDocumentKinds = Functions.DocumentKind.GetAvailableDocumentKinds(_obj);
      return availableDocumentKinds.Where(k => k.IsDefault == true).FirstOrDefault();
    }
    
    /// <summary>
    /// Проверка, может ли текущий сотрудник менять поле "Исполнитель".
    /// </summary>
    /// <returns>True, если может.</returns>
    [Remote(IsPure = true)]
    public virtual bool CanChangeAssignee()
    {
      var documentRegister = _obj.DocumentRegister;
      if (_obj.AccessRights.CanRegister() && _obj.DocumentKind != null &&
          _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable &&
          _obj.AccessRights.CanUpdate() && _obj.RegistrationState == RegistrationState.Registered &&
          documentRegister != null && documentRegister.RegistrationGroup != null)
      {
        var employee = Employees.Current;
        return employee != null && (employee.IncludedIn(documentRegister.RegistrationGroup) ||
                                    Equals(employee, documentRegister.RegistrationGroup.ResponsibleEmployee));
      }
      return false;
    }
    
    /// <summary>
    /// Получить параметры для кеширования.
    /// </summary>
    /// <returns>Структура с параметрами документа.</returns>
    [Remote(IsPure = true)]
    public virtual Structures.OfficialDocument.IOfficialDocumentParams GetOfficialDocumentParams()
    {
      var parameters = Structures.OfficialDocument.OfficialDocumentParams.Create();
      
      var lockInfo = Locks.GetLockInfo(_obj);
      if (_obj.AccessRights.CanUpdate() && !(lockInfo != null && lockInfo.IsLockedByOther))
      {
        parameters.HasReservationSetting = PublicFunctions.Module.Remote.GetRegistrationSettings(Docflow.RegistrationSetting.SettingType.Reservation, _obj.BusinessUnit, _obj.DocumentKind, _obj.Department).Any();
      }
      
      if (_obj.DocumentKind != null &&
          _obj.DocumentKind.AutoNumbering == true &&
          _obj.RegistrationState == RegistrationState.NotRegistered &&
          !Functions.OfficialDocument.IsObsolete(_obj, _obj.LifeCycleState))
      {
        parameters.HasNumerationSetting = Functions.OfficialDocument.HasDocumentRegistersByDocument(_obj, Docflow.RegistrationSetting.SettingType.Numeration);
      }
      
      parameters.NeedShowRegistrationPane = PublicFunctions.PersonalSetting.Remote.GetShowRegistrationPaneParam(null);
      
      if (_obj.State.Properties.Assignee.IsVisible)
      {
        // Для зарегистрированных документов "Исполнитель" должно быть доступно только группе регистрации.
        if (_obj.AccessRights.CanRegister() && _obj.DocumentKind != null && _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable
            && _obj.AccessRights.CanUpdate() && _obj.RegistrationState == RegistrationState.Registered &&
            _obj.DocumentRegister != null && _obj.DocumentRegister.RegistrationGroup != null)
        {
          parameters.CanChangeAssignee = this.CanChangeAssignee();
        }
      }
      
      return parameters;
    }
    
    /// <summary>
    /// Признак того, что необходимо проверять наличие прав подписи на документ у сотрудника, указанного в качестве подписанта с нашей стороны.
    /// </summary>
    /// <returns>True - необходимо проверять, False - иначе.</returns>
    /// <remarks>Поведение по умолчанию - проверять.
    /// Может быть переопределена в наследниках.</remarks>
    public virtual bool NeedValidateOurSignatorySignatureSetting()
    {
      return true;
    }
    
    #region МКДО
    
    /// <summary>
    /// Признак, является ли документ МКДО.
    /// </summary>
    /// <param name="versionId">ИД версии.</param>
    /// <returns>True - если документ участвовал в сервисе обмена, либо формализованный, либо является соглашением об аннулировании.</returns>
    [Public, Remote(IsPure = true)]
    public virtual bool IsExchangeDocument(long versionId)
    {
      return Exchange.ExchangeDocumentInfos.GetAll().Any(x => Equals(x.Document, _obj) && x.VersionId == versionId) ||
        AccountingDocumentBases.Is(_obj) && AccountingDocumentBases.As(_obj).IsFormalized == true ||
        Exchange.CancellationAgreements.Is(_obj);
    }
    
    /// <summary>
    /// Получить сервисы обмена.
    /// </summary>
    /// <returns>Сервисы обмена.</returns>
    /// <remarks>Переопределено в OutgoingDocumentBase.</remarks>
    [Public]
    public virtual List<ExchangeCore.IExchangeService> GetExchangeServices()
    {
      var services = this.GetExchangeServicesByExchangeDocumentInfo();
      
      if (services.Any())
        return services;
      
      return this.DocumentHasCounterparties()
        ? this.GetExchangeServicesByCounterparties()
        : this.GetExchangeServicesByBusinessUnit();
    }
    
    private bool DocumentHasCounterparties()
    {
      var parties = Exchange.PublicFunctions.ExchangeDocumentInfo.GetDocumentCounterparties(_obj);
      return parties != null && parties.Any(p => p.Status == CoreEntities.DatabookEntry.Status.Active);
    }
    
    /// <summary>
    /// Получить сервисы обмена по сведениям о документе обмена.
    /// </summary>
    /// <returns>Сервисы обмена.</returns>
    public virtual List<ExchangeCore.IExchangeService> GetExchangeServicesByExchangeDocumentInfo()
    {
      var services = new List<ExchangeCore.IExchangeService>();
      
      if (this.CanSendAnswer())
      {
        var info = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetIncomingExDocumentInfo(_obj);
        if (info != null && info.Box.Status == CoreEntities.DatabookEntry.Status.Active)
        {
          var service = ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(info.Box);
          services.Add(service);
        }
      }
      
      return services;
    }
    
    /// <summary>
    /// Получить сервисы обмена по контрагентам.
    /// </summary>
    /// <returns>Сервисы обмена.</returns>
    public virtual List<ExchangeCore.IExchangeService> GetExchangeServicesByCounterparties()
    {
      var services = new List<ExchangeCore.IExchangeService>();
      
      // Если есть хоть один контрагент с МКДО, но нет контрагента без МКДО.
      if (Docflow.AccountingDocumentBases.Is(_obj) && Docflow.AccountingDocumentBases.As(_obj).IsFormalized == true)
      {
        var documentBox = Docflow.AccountingDocumentBases.As(_obj).BusinessUnitBox;
        if (documentBox.Status == CoreEntities.DatabookEntry.Status.Active)
        {
          var defaultService = Docflow.AccountingDocumentBases.As(_obj).BusinessUnitBox.ExchangeService;
          services.Add(defaultService);
        }
      }
      else
      {
        var parties = Exchange.PublicFunctions.ExchangeDocumentInfo.GetDocumentCounterparties(_obj);
        var lines = parties.SelectMany(p => p.ExchangeBoxes.Where(b => b.Status == Parties.CounterpartyExchangeBoxes.Status.Active &&
                                                                  Equals(b.Box.BusinessUnit, _obj.BusinessUnit) &&
                                                                  b.Box.Status == CoreEntities.DatabookEntry.Status.Active)).ToList();
        var hasPartyWithoutActiveExchange = parties.Any(p => p.ExchangeBoxes
                                                        .Where(b => Equals(b.Box.BusinessUnit, _obj.BusinessUnit))
                                                        .All(b => b.Status != Parties.CounterpartyExchangeBoxes.Status.Active));
        if (lines.Any() && !hasPartyWithoutActiveExchange)
        {
          services.AddRange(lines
                            .Select(line => line.Box.ExchangeService)
                            .Distinct()
                            .ToList());
        }
      }
      
      return services;
    }
    
    /// <summary>
    /// Получить сервисы обмена по нашей организации.
    /// </summary>
    /// <returns>Сервисы обмена.</returns>
    /// <remarks>Если в документе не указана НОР, то используется НОР от текущего сотрудника.</remarks>
    public virtual List<ExchangeCore.IExchangeService> GetExchangeServicesByBusinessUnit()
    {
      var boxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes();
      var businessUnit = _obj.BusinessUnit ?? Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(Company.Employees.Current);
      
      return boxes.Where(b => Equals(b.BusinessUnit, businessUnit))
        .Select(x => x.ExchangeService).ToList()
        .Distinct()
        .ToList();
    }
    
    /// <summary>
    /// Проверка возможности отправки ответа контрагенту через сервис обмена.
    /// </summary>
    /// <returns>True, если отправка ответа возможна, иначе - false.</returns>
    [Public, Remote]
    public virtual bool CanSendAnswer()
    {
      var exchangeDocumentInfo = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetIncomingExDocumentInfo(_obj);
      return _obj.Versions.Count == 1 && exchangeDocumentInfo != null;
    }
    
    /// <summary>
    /// Отправить ответ на неформализованный документ.
    /// </summary>
    /// <param name="box">Абонентский ящик обмена.</param>
    /// <param name="party">Контрагент.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <param name="isAgent">Признак вызова из фонового процесса. Иначе - пользователем в RX.</param>
    [Public]
    public virtual void SendAnswer(Sungero.ExchangeCore.IBusinessUnitBox box, Parties.ICounterparty party, ICertificate certificate, bool isAgent)
    {
      Exchange.PublicFunctions.Module.SendAnswerToNonformalizedDocument(_obj, party, box, certificate, isAgent);
    }
    
    /// <summary>
    /// Попытаться зарегистрировать документ с настройками по умолчанию.
    /// </summary>
    /// <param name="number">Номер.</param>
    /// <param name="date">Дата.</param>
    /// <returns>True, если регистрация была выполнена.</returns>
    [Public]
    public virtual bool TryExternalRegister(string number, DateTime? date)
    {
      if (string.IsNullOrWhiteSpace(number) || !date.HasValue || _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable)
        return false;
      
      var settingType = _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable ?
        Docflow.RegistrationSetting.SettingType.Registration :
        Docflow.RegistrationSetting.SettingType.Numeration;
      var registersIds = Functions.OfficialDocument.GetDocumentRegistersIdsByDocument(_obj, settingType);
      var defaultDocumentRegister = Functions.DocumentRegister.GetDefaultDocRegister(_obj, registersIds, settingType);
      
      if (defaultDocumentRegister != null)
      {
        _obj.RegistrationDate = date;
        var maxNumberLength = _obj.Info.Properties.RegistrationNumber.Length;
        _obj.RegistrationNumber = number.Length > maxNumberLength ? number.Substring(0, maxNumberLength) : number;
        _obj.RegistrationState = Docflow.OfficialDocument.RegistrationState.Registered;
        _obj.DocumentRegister = defaultDocumentRegister;
        return true;
      }
      
      return false;
    }
    
    #endregion
    
    /// <summary>
    /// Получить документ по ИД.
    /// </summary>
    /// <param name="id">ИД документа.</param>
    /// <returns>Документ.</returns>
    [Remote(IsPure = true), Public]
    public static Docflow.IOfficialDocument GetOfficialDocument(long id)
    {
      return Docflow.OfficialDocuments.Get(id);
    }
    
    /// <summary>
    /// Создать ответный документ.
    /// </summary>
    /// <returns>Ответный документ.</returns>
    [Remote, Public]
    public virtual Docflow.IOfficialDocument CreateReplyDocument()
    {
      return null;
    }
    
    /// <summary>
    /// Выдать сотруднику права на документ.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    [Public]
    public virtual void GrantAccessRightsToActionItemAttachment(IEmployee employee)
    {
      if (_obj != null)
        Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(_obj, employee, DefaultAccessRightsTypes.Read);
    }
    
    /// <summary>
    /// Создать PublicBody документа из html в формате pdf.
    /// </summary>
    /// <param name="sourceHtml">Исходный html.</param>
    [Public]
    public virtual void CreatePdfPublicBodyFromHtml(string sourceHtml)
    {
      if (sourceHtml.Contains("<script"))
      {
        var errorMessage = Sungero.Docflow.OfficialDocuments.Resources.CanNotUseScriptsInHtmlFormat(Sungero.Docflow.Resources.PdfConvertErrorFormat(_obj.Id));
        Logger.Error(errorMessage);
      }

      var sourceBytes = System.Text.Encoding.UTF8.GetBytes(sourceHtml);
      try
      {
        using (var inputStream = new System.IO.MemoryStream(sourceBytes))
          using (var pdfStream = IsolatedFunctions.PdfConverter.GeneratePdf(inputStream, "html"))
        {
          _obj.LastVersion.PublicBody.Write(pdfStream);
        }

        // Заполнение расширения обязательно. Делать это нужно после создания PublicBody, иначе затрется оригинальное расширение.
        _obj.LastVersion.AssociatedApplication = AssociatedApplications.GetByExtension("pdf");
      }
      catch (AppliedCodeException aex)
      {
        Logger.Error(Sungero.Docflow.Resources.PdfConvertErrorFormat(_obj.Id), aex.InnerException);
      }
      catch (Exception ex)
      {
        Logger.Error(Sungero.Docflow.Resources.PdfConvertErrorFormat(_obj.Id), ex);
      }
    }
    
    /// <summary>
    /// Удалить документ.
    /// </summary>
    /// <param name="documentId">ID документа.</param>
    [Public, Remote]
    public static void DeleteDocument(long documentId)
    {
      var doc = OfficialDocuments.GetAll(x => x.Id == documentId).FirstOrDefault();
      
      if (doc != null)
        OfficialDocuments.Delete(doc);
    }
    
    #region Диалог создания поручений по телу документа
    
    /// <summary>
    /// Старт задач на исполнение поручений по протоколу совещаний.
    /// </summary>
    /// <param name="actionItems">Список задач для старта.</param>
    [Public]
    public virtual void StartActionItemTasksFromDialog(List<RecordManagement.IActionItemExecutionTask> actionItems)
    {
      var taskIds = actionItems.Select(t => t.Id).ToList();
      var packedIds = PublicFunctions.Module.PackIds(taskIds);
      var resultHyperlink = Hyperlinks.Functions.ShowStartedActionItems(packedIds);
      var completedNotification = Sungero.Docflow.OfficialDocuments.Resources.OpenActionItemsStartResultFormat(resultHyperlink, Environment.NewLine);
      var startedNotification = Sungero.RecordManagement.Resources.ActionItemCreateFromDialogNotification;
      var errorNotification = Sungero.Docflow.OfficialDocuments.Resources.StartActionItemExecutionTasksErrorSolutionFormat(resultHyperlink, Environment.NewLine);

      var startActionItemsAsyncHandler = RecordManagement.AsyncHandlers.StartActionItemExecutionTasks.Create();
      startActionItemsAsyncHandler.TaskIds = string.Join(",", taskIds);
      if (Users.Current != null)
        startActionItemsAsyncHandler.StartedByUserId = Users.Current.Id;
      startActionItemsAsyncHandler.ExecuteAsync(startedNotification, completedNotification, errorNotification, Users.Current);
    }
    
    /// <summary>
    /// Удаление поручения, созданного по документу.
    /// </summary>
    /// <param name="actionItemId">ИД задачи, которую необходимо удалить.</param>
    /// <returns>True, если удаление прошло успешно.</returns>
    [Remote]
    public static bool TryDeleteActionItemTask(long actionItemId)
    {
      try
      {
        var task = RecordManagement.ActionItemExecutionTasks.Get(actionItemId);
        if (task.AccessRights.CanDelete())
          RecordManagement.ActionItemExecutionTasks.Delete(task);
        else
          return false;
      }
      catch
      {
        return false;
      }

      return true;
    }

    /// <summary>
    /// Получение созданных поручений по документу.
    /// </summary>
    /// <returns>Созданные поручения по документу.</returns>
    [Public, Remote]
    public virtual IQueryable<RecordManagement.IActionItemExecutionTask> GetCreatedActionItems()
    {
      var typeGuid = _obj.GetEntityMetadata().GetOriginal().NameGuid;
      var groupId = Docflow.PublicConstants.Module.TaskMainGroup.ActionItemExecutionTask;
      return RecordManagement.ActionItemExecutionTasks.GetAll(a => a.AttachmentDetails
                                                              .Any(d => d.EntityTypeGuid == typeGuid &&
                                                                   d.GroupId == groupId &&
                                                                   d.AttachmentId == _obj.Id));
    }
    
    /// <summary>
    /// Получить поручения первого уровня по документу.
    /// </summary>
    /// <returns>Поручения первого уровня по документу.</returns>
    /// <remarks>Самостоятельные поручения по документу и поручения,
    /// которые созданы от заданий и задач других типов (согласование или рассмотрение).</remarks>
    [Public]
    public virtual List<RecordManagement.IActionItemExecutionTask> GetFirstLevelActionItems()
    {
      var result = new List<RecordManagement.IActionItemExecutionTask>();
      
      // Самостоятельные поручения по документу.
      var mainActionItems = this.GetCreatedActionItems()
        .Where(t => t.ParentAssignment == null && t.ParentTask == null)
        .ToList();
      result.AddRange(mainActionItems);
      
      // Поручения, которые созданы от задач других типов (согласование или рассмотрение).
      var subActionItemsNotInActionItemTasks = this.GetCreatedActionItems()
        .Where(t => t.ParentTask != null &&
               !RecordManagement.ActionItemExecutionTasks.Is(t.ParentTask))
        .ToList();
      result.AddRange(subActionItemsNotInActionItemTasks);
      
      // Поручения, которые созданы от заданий других типов (при согласовании или рассмотрении).
      var subActionItemsNotInActionItemAssignments = this.GetCreatedActionItems()
        .Where(t => t.ParentTask == null &&
               !RecordManagement.ActionItemExecutionAssignments.Is(t.ParentAssignment))
        .ToList();
      result.AddRange(subActionItemsNotInActionItemAssignments);
      
      return result;
    }
    
    /// <summary>
    /// Создать поручения по документу.
    /// </summary>
    /// <returns>Список созданных поручений.</returns>
    [Remote, Public]
    public virtual List<RecordManagement.IActionItemExecutionTask> CreateActionItemsFromDocument()
    {
      Logger.DebugFormat("CreateActionItemsFromDocument. Start create actionItems from document with Id {0}.", _obj.Id);
      var resultList = new List<RecordManagement.IActionItemExecutionTask>();
      
      if (!_obj.HasVersions)
      {
        Logger.DebugFormat("CreateActionItemsFromDocument. Document with Id {0} has no version.", _obj.Id);
        return resultList;
      }
      
      var lastVersion = _obj.LastVersion;
      
      var supportedExtensions = Functions.Module.GetSupportedExtensionsForActionItems();
      var extension = lastVersion.BodyAssociatedApplication.Extension;
      if (!supportedExtensions.Contains(extension.ToLower()))
      {
        Logger.DebugFormat("CreateActionItemsFromDocument. Extension {0} is not supported, document with Id {1}.", extension, _obj.Id);
        throw new AppliedCodeException(OfficialDocuments.Resources.ActionItemCreationDialogOnlyForWordDocumentFormat(string.Join(", ", supportedExtensions).ToUpper()));
      }
      
      using (var stream = new System.IO.MemoryStream())
      {
        lastVersion.Body.Read().CopyTo(stream);
        var actionItemsListProperties = this.GetActionItemsProperties(stream);

        if (actionItemsListProperties == null || actionItemsListProperties.Count == 0)
        {
          Logger.DebugFormat("CreateActionItemsFromDocument. ActionItemProperties not found. Document(Id={0}).", _obj.Id);
          return resultList;
        }

        foreach (var actionItemProperties in actionItemsListProperties)
        {
          if (!this.CheckAllPropertiesFilled(actionItemProperties))
            continue;
          
          var actionItem = this.CreateActionItemFromProperties(actionItemProperties);
          this.FillActionItemExecutionTaskCommonProperties(actionItem);
          this.FillActionItemExecutionTaskOnCreatedFromDocument(actionItem);
          actionItem.Save();
          resultList.Add(actionItem);
        }
      }
      
      Logger.DebugFormat("CreateActionItemsFromDocument. Done create actionItems from document with Id {0}.", _obj.Id);
      return resultList;
    }

    /// <summary>
    /// Получить свойства поручений из тела документа.
    /// </summary>
    /// <param name="stream">Тело документа.</param>
    /// <returns>Список структур содержащих свойства поручений.</returns>
    public virtual List<Sungero.Docflow.Structures.Module.IMinutesActionItem> GetActionItemsProperties(System.IO.Stream stream)
    {
      if (stream == null)
        return null;
      try
      {
        return Sungero.Docflow.IsolatedFunctions.DocumentTableParser.GetActionItemsProperties(stream, this.GetDocumentActionItemTableTags());
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("GetActionItemsProperties. Error while reading tags from the document with Id {0}.", ex, _obj.Id);
        throw new AppliedCodeException(OfficialDocuments.Resources.ActionItemCreationDialogException);
      }
    }
    
    /// <summary>
    /// Получить список названий столбцов таблицы.
    /// </summary>
    /// <returns>Список названий столбцов таблицы.</returns>
    public virtual List<string> GetDocumentActionItemTableTags()
    {
      return new List<string> {
        OfficialDocuments.Resources.ActionItemCreationDialogActionItemTag,
        OfficialDocuments.Resources.ActionItemCreationDialogResponsibleTag,
        OfficialDocuments.Resources.ActionItemCreationDialogDeadlineTag
      };
    }
    
    /// <summary>
    /// Проверить что все свойства поручения заполнены.
    /// </summary>
    /// <param name="actionItemProperties">Список свойств поручения.</param>
    /// <returns>True если все свойства заполнены, иначе False.</returns>
    public virtual bool CheckAllPropertiesFilled(Structures.Module.IMinutesActionItem actionItemProperties)
    {
      var skipNames = new List<string>();
      if (actionItemProperties == null)
        return false;
      if (RecordManagement.PublicFunctions.Module.AllowActionItemsWithIndefiniteDeadline())
        skipNames.Add(OfficialDocuments.Resources.ActionItemCreationDialogDeadlineTag.ToString());
      
      return actionItemProperties.Properties.Where(x => !skipNames.Contains(x.Key)).All(x => !string.IsNullOrWhiteSpace(x.Value));
    }
    
    /// <summary>
    /// Заполнение общих свойств поручения.
    /// </summary>
    /// <param name="actionItem">Поручение.</param>
    public virtual void FillActionItemExecutionTaskCommonProperties(RecordManagement.IActionItemExecutionTask actionItem)
    {
      if (actionItem == null)
        return;
      var currentEmployee = Company.Employees.Current;
      if (!Equals(currentEmployee, actionItem.Assignee) && !actionItem.CoAssignees.Any(a => Equals(currentEmployee, a.Assignee)))
      {
        actionItem.IsUnderControl = true;
        actionItem.Supervisor = currentEmployee;
      }
      foreach (var property in actionItem.State.Properties)
        property.IsRequired = false;
      
      foreach (var actionItemPart in actionItem.ActionItemParts)
        foreach (var property in actionItemPart.State.Properties)
          property.IsRequired = false;
    }
    
    /// <summary>
    /// Создать поручение согласно списку свойств из протокола.
    /// </summary>
    /// <param name="actionItemProperties">Список свойств поручения.</param>
    /// <returns>Поручение.</returns>
    public virtual RecordManagement.IActionItemExecutionTask CreateActionItemFromProperties(Structures.Module.IMinutesActionItem actionItemProperties)
    {
      if (actionItemProperties == null)
        return null;
      var actionItem = RecordManagement.PublicFunctions.Module.Remote.CreateActionItemExecution(_obj);
      foreach (var property in actionItemProperties.Properties)
        this.FillActionItemProperty(actionItem, property.Key, property.Value);
      return actionItem;
    }
    
    /// <summary>
    /// Заполнить свойство поручения согласно значению свойства из протокола.
    /// </summary>
    /// <param name="actionItem">Поручение.</param>
    /// <param name="propertyName">Имя свойства поручения из протокола.</param>
    /// <param name="propertyValue">Значение свойства поручения из протокола.</param>
    public virtual void FillActionItemProperty(RecordManagement.IActionItemExecutionTask actionItem, string propertyName, string propertyValue)
    {
      if (actionItem == null || string.IsNullOrWhiteSpace(propertyName))
        return;
      if (propertyName == OfficialDocuments.Resources.ActionItemCreationDialogActionItemTag.ToString())
      {
        if (!string.IsNullOrWhiteSpace(propertyValue))
          actionItem.ActiveText = propertyValue;
      }
      else if (propertyName == OfficialDocuments.Resources.ActionItemCreationDialogResponsibleTag.ToString())
      {
        var employees = this.GetEmployeesFromText(propertyValue);
        if (employees.Count > 0)
        {
          actionItem.Assignee = employees.FirstOrDefault();
          foreach (var employee in employees.Skip(1))
          {
            if (employee != null && !Equals(actionItem.Assignee, employee) &&
                !actionItem.CoAssignees.Any(x => Equals(x.Assignee, employee)))
            {
              var newAssignee = actionItem.CoAssignees.AddNew();
              newAssignee.Assignee = employee;
            }
          }
        }
        else
          actionItem.Assignee = null;
      }
      else if (propertyName == OfficialDocuments.Resources.ActionItemCreationDialogDeadlineTag.ToString())
      {
        if (!string.IsNullOrWhiteSpace(propertyValue))
          actionItem.Deadline = this.GetDateFromText(propertyValue);
        else if (RecordManagement.PublicFunctions.Module.AllowActionItemsWithIndefiniteDeadline())
          actionItem.HasIndefiniteDeadline = true;
      }
    }
    
    /// <summary>
    /// Получить дату из текста.
    /// </summary>
    /// <param name="dateTimeText">Текст.</param>
    /// <returns>Дата.</returns>
    public virtual DateTime? GetDateFromText(string dateTimeText)
    {
      if (string.IsNullOrWhiteSpace(dateTimeText))
        return null;

      DateTime result;
      if (Calendar.TryParseDateTime(dateTimeText, out result) &&
          (result.HasTime() && Calendar.UserNow <= result || !result.HasTime() && Calendar.UserToday <= result))
        return result.FromUserTime();
      else
        return null;
    }
    
    /// <summary>
    /// Получить список сотрудников из текста.
    /// </summary>
    /// <param name="employeesText">Текст.</param>
    /// <returns>Список сотрудников.</returns>
    public virtual List<IEmployee> GetEmployeesFromText(string employeesText)
    {
      var result = new List<IEmployee>();
      if (string.IsNullOrWhiteSpace(employeesText))
        return result;
      
      var employeesNames = employeesText
        .Trim().Split(',', ';', '\r', '\n')
        .Where(n => !string.IsNullOrEmpty(n))
        .Select(n => n.Trim())
        .ToList();
      
      foreach (var employeeName in employeesNames)
        result.Add(Company.PublicFunctions.Employee.Remote.GetEmployeeByName(employeeName));
      
      return result;
    }
    
    /// <summary>
    /// Заполнение свойств поручения, созданного по документу.
    /// </summary>
    /// <param name="actionItem">Поручение, созданное по документу.</param>
    public virtual void FillActionItemExecutionTaskOnCreatedFromDocument(RecordManagement.IActionItemExecutionTask actionItem)
    {
      // Виртуальная функция. Переопределено в потомках.
    }
    
    /// <summary>
    /// Получить обновленный список поручений.
    /// </summary>
    /// <param name="ids">Список Id поручений.</param>
    /// <returns>Обновленный список поручений.</returns>
    [Remote]
    public static List<RecordManagement.IActionItemExecutionTask> GetActionItemsExecutionTasks(List<long> ids)
    {
      return RecordManagement.ActionItemExecutionTasks.GetAll(t => ids.Contains(t.Id)).ToList();
    }
    
    #endregion
    
    #region Генерация PDF с отметкой об ЭП
    
    /// <summary>
    /// Преобразовать документ в PDF с наложением отметки об ЭП.
    /// </summary>
    /// <returns>Результат преобразования.</returns>
    [Remote]
    public virtual Structures.OfficialDocument.IConversionToPdfResult ConvertToPdfWithSignatureMark()
    {
      var versionId = _obj.LastVersion.Id;
      var info = this.ValidateDocumentBeforeConversion(versionId);
      if (info.HasErrors)
        return info;
      
      // Документ МКДО.
      if (info.IsExchangeDocument)
      {
        Exchange.PublicFunctions.Module.Remote.GeneratePublicBody(_obj.Id);
        info.IsOnConvertion = true;
        info.HasErrors = false;
        
        Functions.OfficialDocument.LogPdfConversion(_obj, _obj.LastVersion.Id, "Signature mark. Exchange document. Added async");
      }
      else if (this.CanConvertToPdfInteractively())
      {
        // Способ преобразования: интерактивно.
        info = this.ConvertToPdfAndAddSignatureMark(versionId);
        info.IsFastConvertion = true;
        
        Functions.OfficialDocument.LogPdfConversion(_obj, _obj.LastVersion.Id, "Signature mark. Added interactively");
      }
      else
      {
        // Способ преобразования: асинхронно.
        this.CreateConvertToPdfAndAddSignatureMarkAsyncHandler(versionId);
        info.IsOnConvertion = true;
        info.HasErrors = false;
        
        Functions.OfficialDocument.LogPdfConversion(_obj, _obj.LastVersion.Id, "Signature mark. Added async");
      }
      
      return info;
    }
    
    /// <summary>
    /// Создать асинхронный обработчик для преобразования документа в PDF с отметкой об ЭП.
    /// </summary>
    /// <param name="versionId">ИД версии документа.</param>
    [Public, Remote]
    public virtual void CreateConvertToPdfAndAddSignatureMarkAsyncHandler(long versionId)
    {
      var asyncConvertToPdf = Docflow.AsyncHandlers.ConvertDocumentToPdf.Create();
      asyncConvertToPdf.DocumentId = _obj.Id;
      asyncConvertToPdf.VersionId = versionId;
      asyncConvertToPdf.UserId = Users.Current.Id;
      
      var startedNotificationText = OfficialDocuments.Resources.ConvertionInProgress;
      var completedNotificationText = OfficialDocuments.Resources.ConvertToPdfCompleteNotificationFormat(Hyperlinks.Get(_obj));
      var errorNotificationText = Sungero.Docflow.OfficialDocuments.Resources.ConvertionErrorNotificationFormat(Hyperlinks.Get(_obj), Environment.NewLine);

      asyncConvertToPdf.ExecuteAsync(startedNotificationText, completedNotificationText, errorNotificationText, Users.Current);
    }
    
    /// <summary>
    /// Преобразовать документ в PDF и поставить отметку об ЭП.
    /// </summary>
    /// <param name="versionId">Id версии документа.</param>
    /// <returns>Результат преобразования в PDF.</returns>
    [Remote]
    public virtual Structures.OfficialDocument.IConversionToPdfResult ConvertToPdfAndAddSignatureMark(long versionId)
    {
      var signatureMark = this.GetSignatureMarkAsHtml(versionId);
      return this.GeneratePublicBodyWithSignatureMark(versionId, signatureMark);
    }
    
    /// <summary>
    /// Проверить документ до преобразования в PDF.
    /// </summary>
    /// <param name="versionId">Id версии документа.</param>
    /// <returns>Результат проверки перед преобразованием документа.</returns>
    [Public, Remote, Obsolete("Метод не используется с 28.08.2024 и версии 4.11. Используйте метод ValidateDocumentBeforeConversion.")]
    public virtual Structures.OfficialDocument.IConversionToPdfResult ValidateDocumentBeforeConvertion(long versionId)
    {
      return this.ValidateDocumentBeforeConversion(versionId);
    }
    
    /// <summary>
    /// Проверить документ до преобразования в PDF.
    /// </summary>
    /// <param name="versionId">Id версии документа.</param>
    /// <returns>Результат проверки перед преобразованием документа.</returns>
    [Public, Remote]
    public virtual Structures.OfficialDocument.IConversionToPdfResult ValidateDocumentBeforeConversion(long versionId)
    {
      var info = this.ValidateVersionBeforeConversion(versionId);
      if (info.HasErrors)
        return info;
      
      if (this.IsExchangeDocument(versionId))
        info.IsExchangeDocument = true;
      
      return info;
    }
    
    /// <summary>
    /// Проверить версию документа перед преобразованием в PDF.
    /// </summary>
    /// <param name="versionId">ИД версии документа.</param>
    /// <returns>Результат проверки версии.</returns>
    public virtual Structures.OfficialDocument.IConversionToPdfResult ValidateVersionBeforeConversion(long versionId)
    {
      var info = Structures.OfficialDocument.ConversionToPdfResult.Create();
      
      // Проверить наличие версии.
      var version = _obj.Versions.FirstOrDefault(x => x.Id == versionId);
      if (version == null)
      {
        info.HasErrors = true;
        info.ErrorTitle = OfficialDocuments.Resources.ConvertionErrorTitleBase;
        info.ErrorMessage = OfficialDocuments.Resources.NoVersionError;
        return info;
      }
      
      // Проверить тело версии на пустоту.
      var versionBody = Functions.OfficialDocument.GetBodyToConvertToPdfWithMarks(_obj, version);
      if (versionBody.Body == null || versionBody.Body.Length == 0)
      {
        Functions.OfficialDocument.LogPdfConversion(_obj, versionId, "Document version is null or body is empty");
        info.HasErrors = true;
        info.ErrorMessage = OfficialDocuments.Resources.ConvertionErrorTitleBase;
        return info;
      }
      
      // Проверить заблокированность тела версии.
      var lockInfo = Locks.GetLockInfo(version.Body);
      if (lockInfo.IsLocked)
      {
        info.HasErrors = true;
        info.HasLockError = true;
        info.ErrorTitle = OfficialDocuments.Resources.ConvertionErrorTitleBase;
        info.ErrorMessage = lockInfo.LockedMessage;
        return info;
      }
      
      // Проверить формат.
      var versionExtension = version.BodyAssociatedApplication.Extension.ToLower();
      if (!this.CheckPdfConvertibilityByExtension(versionExtension))
        return Functions.OfficialDocument.GetExtensionValidationError(_obj, versionExtension);
      
      return info;
    }
    
    /// <summary>
    /// Проверить свойства документа, необходимые для простановки отметок.
    /// </summary>
    /// <param name="versionId">ИД версии документа.</param>
    /// <returns>Результат проверки свойств документа.</returns>
    [Remote]
    public virtual Structures.OfficialDocument.IConversionToPdfResult ValidateMarksDataBeforeConversion(long versionId)
    {
      var info = Structures.OfficialDocument.ConversionToPdfResult.Create();
      var useObsoletePdfConversion = PublicFunctions.Module.Remote.UseObsoletePdfConversion();
      var signature = Functions.OfficialDocument.GetSignatureForMark(_obj, versionId);
      var documentNotRegistered = _obj.RegistrationState == RegistrationState.NotRegistered;
      
      // Требуемая версия утверждена или зарегистрирована (для режима совместимости - утверждена).
      if (signature == null)
      {
        if (useObsoletePdfConversion)
        {
          info.HasErrors = true;
          info.ErrorTitle = OfficialDocuments.Resources.LastVersionNotApprovedTitle;
          info.ErrorMessage = OfficialDocuments.Resources.LastVersionNotApproved;
          return info;
        }
        else if (documentNotRegistered)
        {
          info.HasErrors = true;
          info.ErrorTitle = OfficialDocuments.Resources.DocumentNotApprovedAndNotRegisteredTitle;
          info.ErrorMessage = OfficialDocuments.Resources.DocumentNotApprovedAndNotRegistered;
          return info;
        }
      }
      
      // Валидация подписи.
      if (signature != null)
      {
        var separator = ". ";
        var validationError = Docflow.Functions.Module.GetSignatureValidationErrorsAsString(signature, separator);
        if (!string.IsNullOrEmpty(validationError))
        {
          info.HasErrors = true;
          info.ErrorTitle = OfficialDocuments.Resources.SignatureNotValidErrorTitle;
          info.ErrorMessage = string.Format(OfficialDocuments.Resources.SignatureNotValidError, validationError);
          return info;
        }
      }
      
      return info;
    }
    
    /// <summary>
    /// Сгенерировать PublicBody документа с отметкой об ЭП.
    /// </summary>
    /// <param name="versionId">ИД версии для генерации.</param>
    /// <param name="signatureMark">Отметка об ЭП (html).</param>
    /// <returns>Информация о результате генерации PublicBody для версии документа.</returns>
    public virtual Structures.OfficialDocument.IConversionToPdfResult GeneratePublicBodyWithSignatureMark(long versionId, string signatureMark)
    {
      return Functions.Module.GeneratePublicBodyWithSignatureMark(_obj, versionId, signatureMark);
    }
    
    /// <summary>
    /// Получить отметку об ЭП.
    /// </summary>
    /// <param name="versionId">ИД версии для генерации.</param>
    /// <returns>Изображение отметки об ЭП в виде html.</returns>
    [Public]
    public virtual string GetSignatureMarkAsHtml(long versionId)
    {
      return Functions.Module.GetSignatureMarkAsHtml(_obj, versionId);
    }
    
    /// <summary>
    /// Получить тело и расширение версии для преобразования в PDF с отметкой об ЭП.
    /// </summary>
    /// <param name="version">Версия для генерации.</param>
    /// <param name="isSignatureMark">Признак отметки об ЭП. True - отметка об ЭП, False - отметка о поступлении.</param>
    /// <returns>Тело версии документа и расширение.</returns>
    [Public]
    public virtual Structures.OfficialDocument.IVersionBody GetBodyToConvertToPdf(Sungero.Content.IElectronicDocumentVersions version, bool isSignatureMark)
    {
      var result = Structures.OfficialDocument.VersionBody.Create();
      if (version == null)
        return result;
      
      // Чтобы не потерять текстовый слой в pdf документе, который может находиться в публичном теле после интеллектуальной обработки.
      // Отметку о поступлении проставлять на публичное тело последней версии документа, если оно есть.
      if (isSignatureMark || version.PublicBody == null || version.PublicBody.Size == 0)
      {
        result.Body = Functions.Module.GetBinaryData(version.Body);
        result.Extension = version.BodyAssociatedApplication.Extension;
      }
      else
      {
        result.Body = Functions.Module.GetBinaryData(version.PublicBody);
        result.Extension = version.AssociatedApplication.Extension;
      }
      
      return result;
    }
    
    /// <summary>
    /// Получить тело и расширение версии для преобразования в PDF с отметками.
    /// </summary>
    /// <param name="version">Версия для преобразования и простановки отметок.</param>
    /// <returns>Тело и расширение версии документа.</returns>
    public virtual Structures.OfficialDocument.IVersionBody GetBodyToConvertToPdfWithMarks(Sungero.Content.IElectronicDocumentVersions version)
    {
      var result = Structures.OfficialDocument.VersionBody.Create();
      if (version == null)
        return result;
      
      result.Body = Functions.Module.GetBinaryData(version.Body);
      result.Extension = version.BodyAssociatedApplication.Extension;
      
      return result;
    }
    
    /// <summary>
    /// Получить электронную подпись для простановки отметки.
    /// </summary>
    /// <param name="versionId">Номер версии.</param>
    /// <returns>Электронная подпись.</returns>
    [Public]
    public virtual Sungero.Domain.Shared.ISignature GetSignatureForMark(long versionId)
    {
      // Только утверждающие подписи, без учета внешних.
      return this.GetSignatureForMark(versionId, false);
    }
    
    /// <summary>
    /// Получить электронную подпись для простановки отметки.
    /// </summary>
    /// <param name="versionId">Номер версии.</param>
    /// <param name="includeExternalSignature">Признак того, что в выборку включены внешние подписи.</param>
    /// <returns>Электронная подпись.</returns>
    [Public]
    public virtual Sungero.Domain.Shared.ISignature GetSignatureForMark(long versionId, bool includeExternalSignature)
    {
      var version = _obj.Versions.FirstOrDefault(x => x.Id == versionId);
      if (version == null)
        return null;
      
      // Только утверждающие подписи.
      var versionSignatures = Signatures.Get(version, q => q.Where(s => (includeExternalSignature || s.IsExternal != true) &&
                                                                   s.SignatureType == SignatureType.Approval))
        .ToList();
      if (!versionSignatures.Any())
        return null;
      
      // В приоритете подпись сотрудника из поля "Подписал". Квалифицированная ЭП приоритетнее простой.
      return versionSignatures
        .OrderByDescending(s => Equals(s.Signatory, _obj.OurSignatory))
        .ThenBy(s => s.SignCertificate == null)
        .ThenByDescending(s => s.SigningDate)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить электронную подпись для регистрации в ФНС.
    /// </summary>
    /// <param name="versionId">Номер версии.</param>
    /// <returns>Электронная подпись.</returns>
    [Public]
    public virtual Sungero.Domain.Shared.ISignature GetSignatureFromOurSignatory(long versionId)
    {
      var version = _obj.Versions.FirstOrDefault(x => x.Id == versionId);
      if (version == null)
        return null;
      
      var versionSignatures = Signatures.Get(version)
        .Where(s => s.SignatureType == SignatureType.Approval && s.IsValid);
      
      if (versionSignatures == null)
        return null;
      
      var externalSignatory = versionSignatures.Where(s => s.IsExternal == true)
        .OrderByDescending(s => s.SigningDate)
        .FirstOrDefault();
      
      if (externalSignatory != null)
        return externalSignatory;
      
      if (_obj.OurSignatory == null)
        return versionSignatures.Where(s => s.SignCertificate != null)
          .OrderByDescending(s => s.SigningDate)
          .FirstOrDefault();
      
      var signingReasonThumbprint = _obj.OurSigningReason?.Certificate?.Thumbprint;
      
      if (signingReasonThumbprint != null)
        return versionSignatures
          .Where(s => s.SignCertificate != null && signingReasonThumbprint == s.SignCertificate?.Thumbprint)
          .OrderByDescending(s => s.SigningDate)
          .FirstOrDefault();
      
      return versionSignatures
        .Where(s => s.SignCertificate != null && Equals(s.Signatory, _obj.OurSignatory))
        .OrderByDescending(s => s.SigningDate)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить подходящие настройки отметки об ЭП для документа.
    /// </summary>
    /// <returns>Список подходящих настроек.</returns>
    [Public]
    public virtual List<IStampSetting> GetStampSettings()
    {
      return PublicFunctions.StampSetting.GetStampSettings(_obj);
    }
    
    /// <summary>
    /// Определить возможность интерактивной конвертации документа.
    /// </summary>
    /// <returns>True - возможно, False - иначе.</returns>
    [Remote]
    public virtual bool CanConvertToPdfInteractively()
    {
      return Functions.Module.CanConvertToPdfInteractively(_obj);
    }
    
    /// <summary>
    /// Определить, поддерживается ли преобразование в PDF для переданного расширения.
    /// </summary>
    /// <param name="extension">Расширение.</param>
    /// <returns>True, если поддерживается, иначе False.</returns>
    [Public]
    public virtual bool CheckPdfConvertibilityByExtension(string extension)
    {
      return IsolatedFunctions.PdfConverter.CheckIfExtensionIsSupported(extension);
    }
    
    #endregion
    
    #region Генерация PDF с отметкой о регистрации
    
    /// <summary>
    /// Преобразовать в PDF с отметкой о регистрации в новую версию документа.
    /// </summary>
    /// <param name="versionId">ИД преобразуемой версии.</param>
    /// <param name="registrationStamp">Отметка о регистрации (html).</param>
    /// <param name="rightIndent">Значение отступа справа.</param>
    /// <param name="bottomIndent">Значение отступа снизу.</param>
    /// <returns>Информация о результате создания новой версии документа в PDF.</returns>
    public virtual Structures.OfficialDocument.IConversionToPdfResult ConvertToPdfAndAddRegistrationStamp(long versionId, string registrationStamp, double rightIndent, double bottomIndent)
    {
      return Docflow.Functions.Module.ConvertToPdfWithStamp(_obj, versionId, registrationStamp, false, rightIndent, bottomIndent);
    }
    
    /// <summary>
    /// Получить отметку о регистрации.
    /// </summary>
    /// <returns>Изображение отметки о регистрации в виде html.</returns>
    [Public, Obsolete("Метод не используется с 15.07.2024 и версии 4.11. Используйте метод GetRegistrationStampAsHtml(long versionId).")]
    public virtual string GetRegistrationStampAsHtml()
    {
      return this.GetRegistrationStampAsHtml(_obj.LastVersion.Id);
    }
    
    /// <summary>
    /// Получить отметку о регистрации.
    /// </summary>
    /// <param name="versionId">ИД версии документа.</param>
    /// <returns>Изображение отметки о регистрации в виде html.</returns>
    /// <remarks>Параметр versionId добавлен для совместимости с другими методами получения отметок и является потенциальной точкой расширения.</remarks>
    [Public]
    public virtual string GetRegistrationStampAsHtml(long versionId)
    {
      return Functions.Module.GetRegistrationStampAsHtml(_obj);
    }
    
    /// <summary>
    /// Получить сообщение об ошибке для неподдерживаемых форматов.
    /// </summary>
    /// <param name="extension">Расширение.</param>
    /// <returns>Результат преобразования.</returns>
    [Remote]
    public Structures.OfficialDocument.IConversionToPdfResult GetExtensionValidationErrorRemote(string extension)
    {
      // TODO Dmitriev: Вернуть код в разделяемый метод после исправления 327321
      var result = Structures.OfficialDocument.ConversionToPdfResult.Create();
      result.HasErrors = true;
      result.ErrorTitle = OfficialDocuments.Resources.ConvertionErrorTitleBase;
      result.ErrorMessage = OfficialDocuments.Resources.ExtensionNotSupportedFormat(extension.ToUpper());
      return result;
    }
    
    #endregion
    
    #region Интеллектуальная обработка
    
    /// <summary>
    /// Сохранить результат верификации заполнения свойств.
    /// </summary>
    [Public]
    public virtual void StoreVerifiedPropertiesValues()
    {
      var recognitionInfo = Commons.PublicFunctions.EntityRecognitionInfo.Remote.GetEntityRecognitionInfo(_obj);
      if (recognitionInfo == null)
        return;
      
      var properties = this.GetSmartProcessingSupportedProperties();
      if (!properties.Any())
        return;

      // Заполнить дату верификации.
      recognitionInfo.Verified = Calendar.Now;
      
      var propertyNames = properties.Select(x => x.Name).ToList();
      foreach (var propertyName in propertyNames)
        this.FillPropertyStatus(recognitionInfo, propertyName);
      
      recognitionInfo.Save();
      
      Logger.WithLogger(Constants.OfficialDocument.SmartProcessingDocflowLoggerPostfix)
        .Debug(string.Format("Saved verified properties. RecognitionInfo:{0}.", recognitionInfo.Id));
      
    }
    
    /// <summary>
    /// Заполнить статус корректности распознавания для свойства.
    /// </summary>
    /// <param name="recognitionInfo">Результат распознавания сущности.</param>
    /// <param name="propertyName">Имя свойства.</param>
    public virtual void FillPropertyStatus(Commons.IEntityRecognitionInfo recognitionInfo, string propertyName)
    {
      var linkedFacts = recognitionInfo.Facts.Where(x => x.PropertyName == propertyName).ToList();
      if (!linkedFacts.Any())
      {
        var newFact = recognitionInfo.Facts.AddNew();
        newFact.PropertyName = propertyName;
        linkedFacts.Add(newFact);
      }
      
      // Если у документа сменили тип, установить статус заполнения всех свойств в "x" независимо от их значения.
      var verifiedClass = SmartProcessing.PublicFunctions.Module.GetArioClassByEntityType(_obj);
      var isDocumentTypeChanged = !string.IsNullOrEmpty(verifiedClass) && recognitionInfo.RecognizedClass != verifiedClass;
      
      var verifiedValue = this.GetPropertyValue(propertyName);
      foreach (var linkedFact in linkedFacts)
      {
        linkedFact.VerifiedValue = verifiedValue;
        
        linkedFact.Filled = isDocumentTypeChanged ?
          Commons.EntityRecognitionInfoFacts.Filled.Empty :
          SmartProcessing.PublicFunctions.Module.GetPropertyFilledStatus(linkedFact.PropertyValue, linkedFact.VerifiedValue);
      }
    }
    
    /// <summary>
    /// Получить значение свойства.
    /// </summary>
    /// <param name="propertyName">Имя свойства.</param>
    /// <returns>Значение свойства в виде строки.</returns>
    public virtual string GetPropertyValue(string propertyName)
    {
      var propertyValue = _obj.GetPropertyValue(propertyName);
      return Commons.PublicFunctions.Module.GetValueAsString(propertyValue);
    }
    
    /// <summary>
    /// Получить список свойств для сбора статистики распознавания.
    /// </summary>
    /// <returns>Список свойств.</returns>
    [Public]
    public virtual List<Sungero.Domain.Shared.IPropertyInfo> GetSmartProcessingSupportedProperties()
    {
      var documentType = _obj.GetEntityMetadata().GetOriginal().NameGuid.ToString();
      var mapping = SmartProcessing.PublicFunctions.Module.GetEntityTypeAndPropertiesListMapping();
      if (!mapping.ContainsKey(documentType))
        return new List<IPropertyInfo>();
      return mapping[documentType];
    }

    #endregion

    /// <summary>
    /// Определить, есть ли активные задачи согласования по регламенту документа.
    /// </summary>
    /// <returns>True, если есть.</returns>
    [Public, Remote]
    public bool HasApprovalTasksWithCurrentDocument()
    {
      var anyTasks = false;

      Sungero.Core.AccessRights.AllowRead(
        () =>
        {
          var docGuid = _obj.GetEntityMetadata().GetOriginal().NameGuid;
          var approvalTaskDocumentGroupGuid = Constants.Module.TaskMainGroup.ApprovalTask;
          anyTasks = ApprovalTasks.GetAll()
            .Where(t => t.Status == Workflow.Task.Status.InProcess ||
                   t.Status == Workflow.Task.Status.Suspended)
            .Where(t => t.AttachmentDetails
                   .Any(att => att.AttachmentId == _obj.Id && att.EntityTypeGuid == docGuid &&
                        att.GroupId == approvalTaskDocumentGroupGuid))
            .Any();
          
        });
      
      return anyTasks;
    }
    
    /// <summary>
    /// Проверить, есть ли по документу поручения в работе.
    /// </summary>
    /// <returns>True, если по документу есть поручения в работе.</returns>
    [Public, Remote(IsPure = true)]
    public virtual bool HasActiveActionItemExecutionTasks()
    {
      var hasActionItemExecutionTasks = false;
      
      Sungero.Core.AccessRights.AllowRead(
        () =>
        {
          hasActionItemExecutionTasks = this.GetCreatedActionItems()
            .Where(a => a.Status == RecordManagement.ActionItemExecutionTask.Status.InProcess)
            .Any();
        });
      
      return hasActionItemExecutionTasks;
    }
    
    /// <summary>
    /// Проверить, есть ли по документу активные или исполненные поручения.
    /// </summary>
    /// <returns>True, если по документу есть активные или исполненные поручения.</returns>
    [Public, Remote(IsPure = true)]
    public virtual bool HasActiveOrCompletedActionItems()
    {
      var hasActionItemExecutionTasks = false;
      
      Sungero.Core.AccessRights.AllowRead(
        () =>
        {
          hasActionItemExecutionTasks = this.GetCreatedActionItems()
            .Where(a => a.Status == RecordManagement.ActionItemExecutionTask.Status.InProcess ||
                   a.Status == RecordManagement.ActionItemExecutionTask.Status.Completed)
            .Any();
        });
      
      return hasActionItemExecutionTasks;
    }
    
    /// <summary>
    /// Проверить, созданы ли по документу поручения.
    /// </summary>
    /// <returns>True, если по документу уже созданы поручения.</returns>
    [Public, Remote(IsPure = true)]
    public virtual bool HasActionItemExecutionTasks()
    {
      var hasActionItemExecutionTasks = false;
      
      Sungero.Core.AccessRights.AllowRead(
        () =>
        {
          hasActionItemExecutionTasks = this.GetCreatedActionItems().Any();
        });
      
      return hasActionItemExecutionTasks;
    }
    
    /// <summary>
    /// Фильтрация дел для документа.
    /// </summary>
    /// <param name="query">Исходные дела для документа.</param>
    /// <returns>Отфильтрованные дела для документа.</returns>
    [Public]
    public virtual IQueryable<ICaseFile> CaseFileFiltering(IQueryable<ICaseFile> query)
    {
      if (_obj.BusinessUnit != null)
        query = query.Where(x => Equals(x.BusinessUnit, _obj.BusinessUnit) || x.BusinessUnit == null);
      
      return query;
    }
    
    /// <summary>
    /// Установить способ доставки.
    /// </summary>
    /// <param name="deliveryMethod">Способ доставки.</param>
    [Public]
    public virtual void SetDeliveryMethod(IMailDeliveryMethod deliveryMethod)
    {
      // Виртуальная функция. Переопределено в потомках.
    }
    
    /// <summary>
    /// Изменить статус документа на "В разработке".
    /// </summary>
    [Public]
    public virtual void SetLifeCycleStateDraft()
    {
      if (_obj.LifeCycleState == null || _obj.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Obsolete)
      {
        Logger.DebugFormat("UpdateLifeCycleState: Document {0} changed LifeCycleState to 'Draft'.", _obj.Id);
        _obj.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Draft;
      }
    }
    
    /// <summary>
    /// Проверить, что состояние документа соответствует одному из доступных состояний официального документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если состояние совпадает с любым доступным состоянием официального документа.<br/>
    /// False, если документ не является официальным или его состояние отличается от доступных для официального документа.</returns>
    [Public]
    public static bool IsSupportedLifeCycleState(IElectronicDocument document)
    {
      var officialDocument = OfficialDocuments.As(document);
      if (officialDocument == null)
        return false;
      
      return officialDocument.LifeCycleState == null ||
        officialDocument.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Draft ||
        officialDocument.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Active ||
        officialDocument.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Obsolete;
    }
    
    /// <summary>
    /// Получить статус контроля исполнения документа.
    /// </summary>
    /// <returns>Статус контроля исполнения документа.</returns>
    [Public]
    public virtual Enumeration? GetControlExecutionState()
    {
      if (_obj.ExecutionState != ExecutionState.OnExecution &&
          _obj.ExecutionState != ExecutionState.Executed)
        return null;
      
      // Нужны поручения, которые созданы от заданий и задач других типов (согласование или рассмотрение).
      var firstLevelTasks = this.GetFirstLevelActionItems();
      var inProcess = firstLevelTasks.Where(t => t.ExecutionState == ExecutionState.OnExecution ||
                                            t.ExecutionState == RecordManagement.ActionItemExecutionTask.ExecutionState.OnRework ||
                                            t.ExecutionState == RecordManagement.ActionItemExecutionTask.ExecutionState.OnControl)
        .ToList();
      // Добавить составные поручения, если хотя бы один пункт поручения в процессе исполнения.
      var compoundTasks = firstLevelTasks.Where(i => i.IsCompoundActionItem.Value == true);
      inProcess.AddRange(compoundTasks.Where(t => t.ActionItemParts.Any(i => i.ActionItemPartExecutionTask == null ||
                                                                        i.ActionItemPartExecutionTask.ExecutionState == RecordManagement.ActionItemExecutionTask.ExecutionState.OnExecution ||
                                                                        i.ActionItemPartExecutionTask.ExecutionState == RecordManagement.ActionItemExecutionTask.ExecutionState.OnRework ||
                                                                        i.ActionItemPartExecutionTask.ExecutionState == RecordManagement.ActionItemExecutionTask.ExecutionState.OnControl)));
      
      var hasInProcessOnSpecialControl = inProcess.Any(t => t.IsUnderControl == true &&
                                                       t.Importance == Sungero.RecordManagement.ActionItemExecutionTask.Importance.High);
      if (hasInProcessOnSpecialControl)
        return Docflow.OfficialDocument.ControlExecutionState.SpecialControl;
      var hasInProcessOnControl = inProcess.Any(t => t.IsUnderControl == true);
      if (hasInProcessOnControl)
        return Docflow.OfficialDocument.ControlExecutionState.OnControl;
      if (inProcess.Any())
        return Docflow.OfficialDocument.ControlExecutionState.WithoutControl;
      
      var executedTasks = firstLevelTasks.Where(t => t.ExecutionState == ExecutionState.Executed).ToList();
      /* compoundTasks.Cast<ITask>() нужен для того, чтобы не возникало ошибок в приведении типов
       * между t.ParentTask (ITask) и compoundTasks (IActionItemExecutionTask).
       */
      var executedChildActionItemsOfCompoundActionItems = this.GetCreatedActionItems()
        .Where(t => compoundTasks.Cast<ITask>().Contains(t.ParentTask) && t.ExecutionState == ExecutionState.Executed)
        .ToList();
      executedTasks.AddRange(executedChildActionItemsOfCompoundActionItems);
      if (executedTasks.Any(t => t.IsUnderControl == true))
        return Docflow.OfficialDocument.ControlExecutionState.ControlRemoved;
      
      if (firstLevelTasks.Any() && firstLevelTasks.All(t => t.ExecutionState == ExecutionState.Aborted))
        return null;
      
      return Docflow.OfficialDocument.ControlExecutionState.WithoutControl;
    }
    
    /// <summary>
    /// Установить статус исполнения документа.
    /// </summary>
    /// <param name="state">Статус.</param>
    [Public]
    public virtual void SetExecutionState(Enumeration? state)
    {
      if (_obj.ExecutionState == state)
        return;
      
      Logger.DebugFormat("ExecutionState for document {0}. Current state: {1}, new state: {2}.", _obj.Id, _obj.ExecutionState, state);
      _obj.ExecutionState = state;
    }
    
    /// <summary>
    /// Установить статус контроля исполнения документа.
    /// </summary>
    /// <param name="state">Статус.</param>
    [Public]
    public virtual void SetControlExecutionState(Enumeration? state)
    {
      if (_obj.ControlExecutionState == state)
        return;
      
      Logger.DebugFormat("ControlExecutionState for document {0}. Current state: {1} new state: {2}.", _obj.Id, _obj.ControlExecutionState, state);
      _obj.ControlExecutionState = state;
    }
    
    /// <summary>
    /// Получить максимальный тип прав на документ, которые текущий пользователь может выдать.
    /// </summary>
    /// <returns>Guid типа прав. Guid.Empty, если текущий пользователь не может выдавать права на документ.</returns>
    [Public]
    public virtual Guid GetAvailableAccessRights()
    {
      // Dmitriev_IA: Проверять на администратора раньше, чем на запрещающие права. Bug 159165.
      if (Users.Current.IsSystem == true || Functions.Module.IsAdministrator())
        return DefaultAccessRightsTypes.FullAccess;
      
      var currentUser = Users.Current;
      
      if (_obj.AccessRights.IsGranted(DefaultAccessRightsTypes.Forbidden, currentUser))
        return Guid.Empty;
      
      if (_obj.AccessRights.IsGranted(DefaultAccessRightsTypes.FullAccess, currentUser))
        return DefaultAccessRightsTypes.FullAccess;
      
      if (_obj.AccessRights.IsGranted(DefaultAccessRightsTypes.Change, currentUser))
        return DefaultAccessRightsTypes.Change;
      
      if (_obj.AccessRights.IsGranted(DefaultAccessRightsTypes.Read, currentUser))
        return Guid.Empty;
      
      return Guid.Empty;
    }
    
    /// <summary>
    /// Скопировать права из текущего документа в указанный.
    /// </summary>
    /// <param name="document">Документ, в который копируются права.</param>
    /// <param name="accessRightsLimit">Максимальный тип прав, который может быть выдан. Guid.Empty, если устанавливать максимальный уровень прав не требуется.</param>
    [Public]
    public virtual void CopyAccessRightsToDocument(IOfficialDocument document, Guid accessRightsLimit)
    {
      if (document == null)
        return;
      
      foreach (var accessRight in _obj.AccessRights.Current)
      {
        var rightsTypeToGrant = accessRight.AccessRightsType;
        /* Тип прав Forbidden является самым высоким типом прав.
         * С типом прав Change нельзя выдать Forbidden и FullAccess.
         * С типом прав FullAccess можно выдать Forbidden.
         */
        if (accessRightsLimit == DefaultAccessRightsTypes.Change &&
            rightsTypeToGrant == DefaultAccessRightsTypes.Forbidden)
          continue;
        
        if (accessRightsLimit != Guid.Empty &&
            rightsTypeToGrant != DefaultAccessRightsTypes.Forbidden &&
            Functions.Module.CompareInstanceAccessRightsTypes(rightsTypeToGrant, accessRightsLimit) > 0)
          rightsTypeToGrant = accessRightsLimit;
        
        Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(document, accessRight.Recipient, rightsTypeToGrant);
        Logger.DebugFormat("Grant Access Rights ({0}) For document ({1}), employee: ({2})", rightsTypeToGrant, document.Id, accessRight.Recipient.Id);
      }
    }
    
    /// <summary>
    /// Проверить, связан ли документ специализированной связью.
    /// </summary>
    /// <returns>True - если связан, иначе - false.</returns>
    [Public, Remote(IsPure = true)]
    public virtual bool HasSpecifiedTypeRelations()
    {
      return false;
    }

    /// <summary>
    /// Получить результаты сравнения документов.
    /// </summary>
    /// <returns>Список результатов сравнений, инициированных текущим пользователем по текущему документу.</returns>
    [Remote(IsPure = true)]
    public virtual IQueryable<IDocumentComparisonInfo> GetComparisonResults()
    {
      return DocumentComparisonInfos
        .GetAll(x => Equals(x.Author, Users.Current) &&
                (x.SecondDocumentId == _obj.Id))
        .OrderByDescending(x => x.Id);
    }
    
    /// <summary>
    /// Получить текст примечания с основанием подписания контрагента.
    /// </summary>
    /// <returns>Текст примечания.</returns>
    [Public]
    public virtual string GetNoteWithCounterpartySigningReason()
    {
      var note = _obj.Note;
      
      // Получить основание подписания со стороны контрагента.
      var counterpartySigningReason = Docflow.PublicFunctions.OfficialDocument.GetCounterpartySigningReason(_obj);
      if (!string.IsNullOrWhiteSpace(counterpartySigningReason))
      {
        if (!string.IsNullOrWhiteSpace(note))
          note += Environment.NewLine;
        
        note += SimpleDocuments.Resources.CounterpartySigningReasonTitleFormat(counterpartySigningReason);
      }
      
      return note;
    }
    
    #region Работа с версией документа и подписью
    
    /// <summary>
    /// Получить хеш тела версии документа.
    /// </summary>
    /// <param name="version">Версия документа.</param>
    /// <returns>Хеш тела версии документа.</returns>
    /// <remarks>Если документ зашифрован то берем хеш расшифрованного тела версии документа.</remarks>
    [Public]
    public virtual string GetVersionBodyHash(IElectronicDocumentVersions version)
    {
      if (_obj.IsEncrypted)
      {
        using (var inputStream = new System.IO.MemoryStream())
        {
          // Выключить error-логирование при доступе к зашифрованной версии.
          Sungero.Core.AccessRights.SuppressSecurityEvents(() => version.Body.Read().CopyTo(inputStream));
          return inputStream.GetMD5Hash();
        }
      }
      
      return version.Body.Hash;
    }
    
    /// <summary>
    /// Проверить тело документа и наличие утверждающей подписи.
    /// </summary>
    /// <returns>Сообщение об ошибке или пустая строка, если ошибок нет.</returns>
    [Public]
    public virtual string ValidateBodyAndSignature()
    {
      // Проверить наличие версии.
      if (!_obj.HasVersions)
        return OfficialDocuments.Resources.NoVersionError;
      
      // Проверить наличие утверждающей подписи.
      var versionId = _obj.LastVersion.Id;
      var signature = Functions.OfficialDocument.GetSignatureFromOurSignatory(_obj, versionId);
      if (signature == null)
        return OfficialDocuments.Resources.LastVersionNotApprovedTitle;
      
      // Валидация подписи.
      var separator = ". ";
      var validationError = Docflow.Functions.Module.GetSignatureValidationErrorsAsString(signature, separator);
      if (!string.IsNullOrEmpty(validationError))
        return OfficialDocuments.Resources.SignatureNotValidErrorTitle;
      
      return string.Empty;
    }
    
    /// <summary>
    /// Записать тело документа из массива байт в версию.
    /// </summary>
    /// <param name="bytes">Структура с телом документа в виде массива байт.</param>
    /// <param name="extension">Расширение приложения-обработчика.</param>
    [Public]
    public virtual void WriteBytesToDocumentLastVersionBody(Docflow.Structures.Module.IByteArray bytes, string extension)
    {
      if (bytes == null)
        return;
      
      if (!_obj.HasVersions)
        _obj.Versions.AddNew();
      
      using (var body = new MemoryStream(bytes.Bytes))
      {
        _obj.LastVersion.Body.Write(body);
      }
      
      if (!string.IsNullOrWhiteSpace(extension))
        _obj.LastVersion.AssociatedApplication = Content.AssociatedApplications.GetByExtension(extension);
    }
    
    /// <summary>
    /// Заполнить дату отправки.
    /// </summary>
    /// <param name="correspondent">Корреспондент.</param>
    [Public]
    public virtual void FillSentDate(ICounterparty correspondent)
    {
      // Виртуальная функция. Переопределено в потомках.
    }
    
    /// <summary>
    /// Заполнить исполнителя по документу.
    /// </summary>
    /// <param name="assignee">Исполнитель.</param>
    [Public]
    public virtual void SetDocumentAssignee(IEmployee assignee)
    {
      if (_obj.Assignee == null && _obj.State.Properties.Assignee.IsVisible)
        _obj.Assignee = assignee;
    }

    /// <summary>
    /// Создать версию документа и восстановить прежний статус электронного обмена.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <remarks>При создании версии чистится статус эл. обмена, восстанавливаем его.</remarks>
    [Public]
    public static void CreateVersionWithRestoringExchangeState(IOfficialDocument document)
    {
      var oldExchangeState = document.ExchangeState;
      document.CreateVersion();
      document.ExchangeState = oldExchangeState;
    }
    
    #endregion
    
    /// <summary>
    /// Создать соглашение об аннулировании.
    /// </summary>
    /// <param name="ourSignatory">Подписант НОР.</param>
    /// <param name="reason">Причина аннулирования.</param>
    /// <returns>Соглашение об аннулировании.</returns>
    [Public, Remote]
    public virtual Sungero.Docflow.Structures.OfficialDocument.ICancellationAgreementCreatingResult CreateCancellationAgreement(IEmployee ourSignatory, string reason)
    {
      var result = Structures.OfficialDocument.CancellationAgreementCreatingResult.Create();
      var сancellationAgreement = Exchange.PublicFunctions.Module.Remote.CreateCancellationAgreement(_obj, reason, ourSignatory);
      var error = Exchange.PublicFunctions.CancellationAgreement.GenerateCancellationAgreementBody(сancellationAgreement, reason);
      if (!string.IsNullOrEmpty(error))
      {
        result.Error = error;
        return result;
      }

      Docflow.PublicFunctions.Module.GeneratePublicBodyForExchangeDocument(сancellationAgreement,
                                                                           сancellationAgreement.LastVersion.Id,
                                                                           сancellationAgreement.ExchangeState);
      
      result.CancellationAgreement = сancellationAgreement;
      return result;
    }

    /// <summary>
    /// Отправить документ в сервис обмена.
    /// </summary>
    /// <param name="addenda">Приложения.</param>
    /// <param name="receiver">Получатель (головная организация или филиал контрагента).</param>
    /// <param name="receiverServiceDepartmentId">Внешний ИД подразделения контрагента.</param>
    /// <param name="senderBox">Абонентский ящик отправителя.</param>
    /// <param name="senderServiceDepartmentId">Внешний ИД подразделения абонентского ящика отправителя.</param>
    /// <param name="certificate">Сертификат, которым подписаны документы.</param>
    /// <param name="needSign">Требовать подписание от контрагента.</param>
    /// <param name="comment">Комментарий к сообщению в сервисе.</param>
    [Remote, Public]
    public virtual void SendDocuments(List<Sungero.Docflow.IOfficialDocument> addenda,
                                      Parties.ICounterparty receiver, string receiverServiceDepartmentId,
                                      ExchangeCore.IBusinessUnitBox senderBox, string senderServiceDepartmentId,
                                      ICertificate certificate, bool needSign, string comment)
    {
      Exchange.PublicFunctions.Module.Remote.SendDocuments(_obj, addenda, receiver, receiverServiceDepartmentId, senderBox, senderServiceDepartmentId, certificate, needSign, comment);
    }
    
    #region Функции вычисляемых выражений
    
    /// <summary>
    /// Получить регистратора документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Регистратор.</returns>
    [ExpressionElement("DocumentRegistrar", "DocumentRegistrarDescription")]
    public static IEmployee GetDocumentRegistrar(IOfficialDocument document)
    {
      return Functions.Module.GetRegistrar(document);
    }
    
    /// <summary>
    /// Получить регистратора исходящей корреспонденции.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Регистратор.</returns>
    [ExpressionElement("OutgoingDocumentsRegistrar", "OutgoingDocumentsRegistrarDescription")]
    public static IEmployee GetOutgoingDocumentsRegistrar(IOfficialDocument document)
    {
      return Functions.Module.GetOutgoingDocumentsRegistrar(document);
    }
    
    /// <summary>
    /// Получить ответственного за контрагента.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Ответственный.</returns>
    [ExpressionElement("CounterpartyResponsible", "CounterpartyResponsibleDescription")]
    public static IEmployee GetCounterpartyResponsible(IOfficialDocument document)
    {
      var responsible = Employees.Null;
      if (document != null)
      {
        var counterparties = Functions.OfficialDocument.GetCounterparties(document);
        if (counterparties != null && counterparties.Count == 1)
        {
          var company = Parties.Companies.As(counterparties.FirstOrDefault());
          if (company != null && company.Responsible != null)
            responsible = company.Responsible;
        }
      }
      
      return responsible;
    }
    
    /// <summary>
    /// Получить ответственного за документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Ответственный.</returns>
    [ExpressionElement("DocumentResponsible", "DocumentResponsibleDescription")]
    public static IEmployee GetDocumentResponsible(IOfficialDocument document)
    {
      return Functions.OfficialDocument.GetDocumentResponsibleEmployee(document);
    }
    
    #endregion Функции вычисляемых выражений
    
    /// <summary>
    /// Обновить срок на вкладке "Выдача" документа.
    /// </summary>
    /// <param name="newDeadline">Новый срок.</param>
    /// <param name="checkReturnTask">Задача на контроль возврата.</param>
    [Public]
    public virtual void ExtendTrackingDeadline(DateTime newDeadline, ITask checkReturnTask)
    {
      // В документе не вызывать логику синхронизации сроков на закладки "Выдача" с задачами на контроль возврата.
      ((Domain.Shared.IExtendedEntity)_obj).Params[Docflow.Constants.Module.DeadlineExtentsionTaskCallContext] = true;
      
      var tracks = _obj.Tracking.Where(t => Equals(t.ReturnTask, checkReturnTask) &&
                                       t.ReturnResult == null && t.ReturnDeadline != null);
      foreach (var track in tracks)
      {
        if (track.DeliveryDate != null && newDeadline < track.DeliveryDate)
          newDeadline = track.DeliveryDate.Value;
        track.ReturnDeadline = newDeadline;
      }
    }
    
    #region Событие "До  сохранения"
    
    /// <summary>
    /// Проверить, что подразделение было изменено.
    /// </summary>
    /// <returns>True, если подразделение было изменено.</returns>
    public virtual bool DepartmentChanged()
    {
      return _obj.Department != _obj.State.Properties.Department.OriginalValue;
    }
    
    /// <summary>
    /// Проверить, что наша организация была изменена.
    /// </summary>
    /// <returns>True, если наша организация была изменена.</returns>
    public virtual bool BusinessUnitChanged()
    {
      return _obj.BusinessUnit != _obj.State.Properties.BusinessUnit.OriginalValue;
    }
    
    /// <summary>
    /// Проверить, что дата изменения содержимого документа изменилась.
    /// </summary>
    /// <returns>True, если дата изменения содержимого документа изменилась.</returns>
    public virtual bool ModifiedChanged()
    {
      return _obj.Modified != _obj.State.Properties.Modified.OriginalValue;
    }
    
    /// <summary>
    /// Проверить, что тип документа изменился.
    /// </summary>
    /// <returns>True, если тип документа изменился.</returns>
    public virtual bool DocumentTypeChanged()
    {
      return _obj.State.Properties.DocumentKind.OriginalValue?.DocumentType?.Equals(_obj.DocumentKind.DocumentType) == false;
    }
    
    /// <summary>
    /// Проверить, что у документа статус верификации "В процессе" или значение статуса изменено.
    /// </summary>
    /// <returns>True, если у документа статус верификации "В процессе" или значение статуса изменено.</returns>
    public virtual bool IsOnVerification()
    {
      return _obj.VerificationState == VerificationState.InProcess ||
        _obj.State.Properties.VerificationState.IsChanged && _obj.VerificationState == VerificationState.Completed;
    }
    
    /// <summary>
    /// Проверить, что документ можно зарегистрировать.
    /// </summary>
    /// <returns>True, если документ можно зарегистрировать.</returns>
    public virtual bool CanRegister()
    {
      return !Functions.OfficialDocument.IsRegistrable(_obj) || _obj.AccessRights.CanRegister();
    }
    
    /// <summary>
    /// Выдать права на изменение документа группе регистрации.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void GrantRegistrationGroupRights(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var registrationGroup = _obj.DocumentRegister?.RegistrationGroup;
      if (registrationGroup == null)
        return;
      // Выдать права группе регистрации. Заодно обновить кешированную проверку на доступность поля "Исполнитель".
      if (_obj.AccessRights.StrictMode != AccessRightsStrictMode.Enhanced)
        _obj.AccessRights.Grant(registrationGroup, DefaultAccessRightsTypes.Change);
      e.Params.AddOrUpdate(Constants.OfficialDocument.CanChangeAssignee, Functions.OfficialDocument.CanChangeAssignee(_obj));
    }

    /// <summary>
    /// Установить для документа хранилище из последней версии.
    /// </summary>
    public virtual void SyncStorage()
    {
      if (_obj.Storage == null && _obj.LastVersion != null)
        _obj.Storage = _obj.LastVersion.Body.Storage;
      if (_obj.Storage != null && !_obj.HasVersions)
        _obj.Storage = null;
    }
    
    /// <summary>
    /// Исправить дату изменения содержимого документа.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void FixModifiedField(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // TODO: удалить код после исправления бага платформы 46179.
      var isTransferring = _obj.State.IsBinaryDataTransferring;
      var dontUpdateModified = e.Params.Contains(Constants.OfficialDocument.DontUpdateModified);
      if (!dontUpdateModified && !isTransferring)
        _obj.Modified = Calendar.Now;
      else if (this.ModifiedChanged() && (dontUpdateModified || isTransferring))
        _obj.Modified = _obj.State.Properties.Modified.OriginalValue;
    }
    
    #region Регистрация/нумерация
    
    /// <summary>
    /// Выполнить действия по регистрации и нумерации.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void RegisterAndNumerate(Sungero.Domain.BeforeSaveEventArgs e)
    {
      this.AutoNumbering(e);
      
      // Убрать начальные и конечные пробелы в рег. номере.
      if (_obj.RegistrationNumber != null && Regex.IsMatch(_obj.RegistrationNumber, @"\s"))
        _obj.RegistrationNumber = _obj.RegistrationNumber.Trim();
      this.ValidateBeforeRegistration(e);
      
      // Очистить индекс, если рег.номер пуст.
      if (string.IsNullOrEmpty(_obj.RegistrationNumber))
        _obj.Index = 0;
      this.GenerateRegistrationPrefixAndPostfix(e);
      this.LockRegistrationFields();
      this.GrantRegistrationGroupRights();
      
      // Выдать права на документ при изменении проекта.
      if (_obj.Project != _obj.State.Properties.Project.OriginalValue && _obj.Project != null)
        e.Params.AddOrUpdate(Constants.OfficialDocument.GrantAccessRightsToProjectDocument, true);
      
      this.ValidateRegistrationDataIsUnchanged(e);
    }
    
    #region Данные для валидации рег. номера.
    
    /// <summary>
    /// Получить код подразделения.
    /// </summary>
    /// <returns>Код подразделения, либо пустая строка.</returns>
    public virtual string DepartmentCode()
    {
      return _obj.Department?.Code ?? string.Empty;
    }
    
    /// <summary>
    /// Получить код нашей организации.
    /// </summary>
    /// <returns>Код нашей организации, либо пустая строка.</returns>
    public virtual string BUnitCode()
    {
      return _obj.BusinessUnit?.Code ?? string.Empty;
    }
    
    /// <summary>
    /// Получить индекс дела.
    /// </summary>
    /// <returns>Индекс дела, либо пустая строка.</returns>
    public virtual string CaseIndex()
    {
      return _obj.CaseFile?.Index ?? string.Empty;
    }
    
    /// <summary>
    /// Получить код вида документа.
    /// </summary>
    /// <returns>Код вида документа, либо пустая строка.</returns>
    public virtual string KindCode()
    {
      return _obj.DocumentKind?.Code ?? string.Empty;
    }
    
    /// <summary>
    /// Получить регистрационный номер ведущего документа.
    /// </summary>
    /// <returns>Регистрационный номер ведущего документа, либо пустая строка.</returns>
    public virtual string LeadDocNumber()
    {
      return _obj.LeadingDocument?.RegistrationNumber ?? string.Empty;
    }
    
    /// <summary>
    /// Проверить, что документ с автоматической нумерацией.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    /// <returns>True, если документ с автоматической нумерацией.</returns>
    public virtual bool IsAutoNumbering(Sungero.Domain.BeforeSaveEventArgs e)
    {
      return _obj.DocumentKind?.AutoNumbering == true;
    }
    
    #endregion
    
    /// <summary>
    /// Проверить изменение вида документа.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ValidateChangingDocumentKind(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (!Functions.OfficialDocument.IsRegistered(_obj) || !Functions.OfficialDocument.NumberingTypeChanged(_obj))
        return;
      
      // Если изменен вид в зарегистрированном/пронумерованном документе и в виде и журнале отличается признак Регистрируемый/Нумеруемый, то выводим хинт.
      var originalNumberingType = _obj.State.Properties.DocumentKind.OriginalValue?.NumberingType;
      if (originalNumberingType == Docflow.DocumentKind.NumberingType.Registrable)
        e.AddError(OfficialDocuments.Resources.CannotChangeRegisteredDocumentToNumerable);
      if (originalNumberingType == Docflow.DocumentKind.NumberingType.Numerable)
        e.AddError(OfficialDocuments.Resources.CannotChangeNumeratedDocumentToRegistrable);
    }
    
    /// <summary>
    /// Выполнить действия по автоматической нумерации документа.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void AutoNumbering(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Автонумерация документа.
      if (!this.IsAutoNumbering(e))
        return;
      if (_obj.VerificationState == VerificationState.InProcess)
        return;
      if (!e.IsValid)
        return;
      if (!Functions.OfficialDocument.IsNotRegistered(_obj))
        return;
      if (Functions.OfficialDocument.IsObsolete(_obj, _obj.LifeCycleState))
        return;
      
      var documentRegistersIds = Functions.OfficialDocument.GetDocumentRegistersIdsByDocument(_obj, Docflow.RegistrationSetting.SettingType.Numeration);
      if (!documentRegistersIds.Any())
      {
        e.AddError(Docflow.Resources.NumberingSettingsRequiredForSave);
        return;
      }
      
      var register = DocumentRegisters.Get(documentRegistersIds.First());
      
      // Заполнить дело, если оно будет заполнено после регистрации.
      if (_obj.CaseFile == null)
        Functions.OfficialDocument.FillCaseFileAndDeliveryMethod(_obj, register);
      Functions.OfficialDocument.RegisterDocument(_obj, register, Calendar.UserToday, null, false, false);
      
      // Добавить параметр о необходимости валидации.
      e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.NeedValidateRegisterFormat, true);
    }
    
    /// <summary>
    /// Проверить регистрационный номер на валидность.
    /// </summary>
    /// <returns>Сообщение об ошибке. Пустая строка, если номер соответствует журналу.</returns>
    public virtual string CheckRegistrationNumberFormat()
    {
      return Functions.DocumentRegister.CheckRegistrationNumberFormat(_obj.DocumentRegister, _obj);
    }
    
    /// <summary>
    /// Проверить наличие прав на журнал регистрации при регистрации системным пользователем.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ValidateDocumentRegisterForSystemUser(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.DocumentRegister == null)
        return;
      if (_obj.State.Properties.DocumentRegister.OriginalValue == _obj.DocumentRegister)
        return;
      if (_obj.DocumentKind == null)
        return;
      if (Users.Current.IsSystem == true)
        return;
      
      var documentRegisters = Functions.OfficialDocument.GetDocumentRegistersByDocument(_obj);
      if (!documentRegisters.Contains(_obj.DocumentRegister.Id))
        e.AddError(Docflow.Resources.NoRightToRegistrationInDocumentRegister);
    }
    
    /// <summary>
    /// Проверить регистрационный номер на валидность.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    /// <returns>Результаты проверки регистрационного номера.</returns>
    public virtual Structures.OfficialDocument.RegNumberValidationResult GetRegNumberValidationResult(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Для некоторых документов отключена проверка номера.
      // Если рег. номера нет, то нет смысла выключать проверку номера, т.к. он будет сгенерен автоматически.
      var numberValidationDisabled = !string.IsNullOrWhiteSpace(_obj.RegistrationNumber) && Functions.OfficialDocument.IsNumberValidationDisabled(_obj);
      if (numberValidationDisabled)
        return Structures.OfficialDocument.RegNumberValidationResult.Create(true, string.Empty, false, string.Empty, false);
      
      var numberSectionsError = Functions.DocumentRegister.CheckDocumentRegisterSections(_obj.DocumentRegister, _obj);
      var hasSectionError = !string.IsNullOrWhiteSpace(numberSectionsError);
      var numberFormatError = hasSectionError
        ? null
        : this.CheckRegistrationNumberFormat();
      var hasNumberFormatError = !string.IsNullOrWhiteSpace(numberFormatError) &&
        !(this.IsAutoNumbering(e) && string.IsNullOrWhiteSpace(_obj.RegistrationNumber));
      var valid = !hasSectionError && !hasNumberFormatError;
      return Structures.OfficialDocument.RegNumberValidationResult.Create(valid, numberSectionsError, hasSectionError, numberFormatError, hasNumberFormatError);
    }
    
    /// <summary>
    /// Получить индекс рег. номера, и записать его в порядковый номер документа.
    /// </summary>
    public virtual void GetIndexFromRegistrationNumber()
    {
      if (string.IsNullOrEmpty(_obj.RegistrationNumber) ||
          !_obj.State.Properties.RegistrationNumber.IsChanged && _obj.Index != 0)
        return;
      
      var useObsoleteRegNumberGeneration = Functions.Module.GetDocflowParamsBoolValue(PublicConstants.Module.UseObsoleteRegNumberGenerationParamName);
      var formatItems = Functions.DocumentRegister.GetNumberFormatItemsValues(_obj);
      _obj.Index = useObsoleteRegNumberGeneration
        ? Functions.DocumentRegister.GetIndexFromRegistrationNumber(_obj.DocumentRegister, _obj.RegistrationDate.Value, _obj.RegistrationNumber,
                                                                    formatItems.DepartmentCode, formatItems.BusinessUnitCode, formatItems.CaseFileIndex,
                                                                    formatItems.DocumentKindCode, formatItems.CounterpartyCode, formatItems.LeadingDocumentNumber,
                                                                    Functions.OfficialDocument.CheckRegistrationNumberUnique(_obj))
        : Functions.DocumentRegister.GetIndexFromRegistrationNumber(_obj.DocumentRegister, _obj, _obj.RegistrationDate.Value, _obj.RegistrationNumber,
                                                                    Functions.OfficialDocument.CheckRegistrationNumberUnique(_obj));
    }
    
    /// <summary>
    /// Добавить ошибки проверки регистрационного номера.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    /// <param name="validation">Результаты проверки регистрационного номера.</param>
    public virtual void AddRegNumberValidationError(Sungero.Domain.BeforeSaveEventArgs e, Structures.OfficialDocument.RegNumberValidationResult validation)
    {
      bool needValidate = false;
      if (_obj.VerificationState != VerificationState.InProcess || _obj.DocumentKind?.AutoNumbering != true)
        e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.NeedValidateRegisterFormat, out needValidate);
      
      if (validation.HasSectionError && needValidate)
      {
        if (validation.NumberSectionError == string.Format(Docflow.Resources.FillCaseFile, Docflow.Resources.numberWord))
          e.AddError(_obj.Info.Properties.CaseFile, validation.NumberSectionError);
        else
          e.AddError(validation.NumberSectionError);
      }
      
      if (validation.HasNumberFormatError && needValidate)
        e.AddError(validation.NumberFormatError);
    }
    
    /// <summary>
    /// Проверить рег.номер на соответствие формату.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ValidateRegNumberFormat(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.DocumentRegister == null || !_obj.RegistrationDate.HasValue)
        return;
      
      var validation = this.GetRegNumberValidationResult(e);
      if (validation.Valid)
        this.GetIndexFromRegistrationNumber();
      else
        this.AddRegNumberValidationError(e, validation);
    }
    
    /// <summary>
    /// Проверить рег. номер на уникальность.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ValidateRegNumberUniqueness(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (string.IsNullOrEmpty(_obj.RegistrationNumber))
        return;
      if (_obj.RegistrationDate == null)
        return;
      if (_obj.DocumentRegister == null)
        return;
      if (_obj.RegistrationNumber == _obj.State.Properties.RegistrationNumber.OriginalValue &&
          _obj.RegistrationDate == _obj.State.Properties.RegistrationDate.OriginalValue &&
          Equals(_obj.DocumentRegister, _obj.State.Properties.DocumentRegister.OriginalValue))
        return;
      
      var useObsoleteRegNumberGeneration = Functions.Module.GetDocflowParamsBoolValue(PublicConstants.Module.UseObsoleteRegNumberGenerationParamName);
      var formatItems = Functions.DocumentRegister.GetNumberFormatItemsValues(_obj);
      var isRegistrationNumberUnique = useObsoleteRegNumberGeneration
        ? Functions.DocumentRegister.IsRegistrationNumberUnique(_obj.DocumentRegister, _obj, _obj.RegistrationNumber, _obj.Index ?? 0, _obj.RegistrationDate.Value,
                                                                formatItems.DepartmentCode, formatItems.BusinessUnitCode, formatItems.CaseFileIndex,
                                                                formatItems.DocumentKindCode, formatItems.CounterpartyCode, formatItems.LeadingDocumentId)
        : Functions.DocumentRegister.IsRegistrationNumberUnique(_obj.DocumentRegister, _obj, _obj.RegistrationNumber, _obj.Index ?? 0, _obj.RegistrationDate.Value);
      if (!isRegistrationNumberUnique)
        e.AddError(_obj.Info.Properties.RegistrationNumber,
                   Functions.OfficialDocument.IsRegistrable(_obj) ? Sungero.Docflow.Resources.RegistrationNumberIsNotUniqueFormat(_obj.RegistrationNumber) : Sungero.Docflow.Resources.RegistrationNumberIsNotUniqueForNumerable);
    }
    
    /// <summary>
    /// Проверить документ перед регистрацией.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ValidateBeforeRegistration(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Игнорировать проверку прав на регистрацию и валидацию рег. номера при смене типа документа.
      if (this.DocumentTypeChanged()) 
        return;
      this.ValidateDocumentRegisterForSystemUser(e);
      this.ValidateRegNumberFormat(e);
      this.ValidateRegNumberUniqueness(e);
    }
    
    /// <summary>
    /// Получить и запомнить в params префикс и постфикс регистрационного номера.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void GenerateRegistrationPrefixAndPostfix(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (!string.IsNullOrEmpty(_obj.RegistrationNumber) || _obj.RegistrationDate == null || _obj.DocumentRegister == null)
        return;
      
      var useObsoleteRegNumberGeneration = Functions.Module.GetDocflowParamsBoolValue(PublicConstants.Module.UseObsoleteRegNumberGenerationParamName);
      var formatItems = Functions.DocumentRegister.GetNumberFormatItemsValues(_obj);
      var prefixAndPostfix = useObsoleteRegNumberGeneration
        ? Functions.DocumentRegister.GenerateRegistrationNumberPrefixAndPostfix(_obj.DocumentRegister, _obj.RegistrationDate.Value,
                                                                                formatItems.LeadingDocumentNumber, formatItems.DepartmentCode, formatItems.BusinessUnitCode,
                                                                                formatItems.CaseFileIndex, formatItems.DocumentKindCode, formatItems.CounterpartyCode,
                                                                                false)
        : Functions.DocumentRegister.GenerateRegistrationNumberPrefixAndPostfix(_obj.DocumentRegister, _obj, _obj.RegistrationDate.Value);
      
      // Проверить длину автогенерируемого номера.
      if (_obj.DocumentRegister.NumberOfDigitsInNumber + prefixAndPostfix.Prefix.Length + prefixAndPostfix.Postfix.Length > _obj.Info.Properties.RegistrationNumber.Length)
      {
        var errorMessage = string.Format(Docflow.Resources.PropertyLengthError, _obj.Info.Properties.RegistrationNumber.LocalizedName, _obj.Info.Properties.RegistrationNumber.Length);
        e.AddError($"{errorMessage} {Parties.Resources.ContactAdministrator}");
        return;
      }
      
      e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.RegistrationNumberPrefix, prefixAndPostfix.Prefix);
      e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.RegistrationNumberPostfix, prefixAndPostfix.Postfix);
    }
    
    /// <summary>
    /// Закрыть регистрационные данные от изменения.
    /// </summary>
    public virtual void LockRegistrationFields()
    {
      _obj.State.Properties.RegistrationNumber.IsEnabled = false;
      _obj.State.Properties.RegistrationDate.IsEnabled = false;
      _obj.State.Properties.DocumentRegister.IsEnabled = false;
      _obj.State.Properties.DocumentKind.IsEnabled = false;
    }
    
    /// <summary>
    /// Выдать права на изменение документа группе регистрации.
    /// </summary>
    public virtual void GrantRegistrationGroupRights()
    {
      if (_obj.DocumentRegister == null || _obj.AccessRights.StrictMode == AccessRightsStrictMode.Enhanced)
        return;
      var registrationGroup = _obj.DocumentRegister.RegistrationGroup;
      if (registrationGroup != null)
        _obj.AccessRights.Grant(registrationGroup, DefaultAccessRightsTypes.Change);
    }
    
    /// <summary>
    /// Проверить неизменность даты регистрации.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ValidateRegistrationDateUnchanged(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.RegistrationDate != _obj.State.Properties.RegistrationDate.OriginalValue)
        e.AddError(Sungero.Docflow.Resources.NoRightsToModifyRegistrationDate);
    }
        
    /// <summary>
    /// Проверить неизменность регистрационного номера.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ValidateRegistrationNumberUnchanged(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.RegistrationNumber != _obj.State.Properties.RegistrationNumber.OriginalValue &&
          (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber) || !string.IsNullOrWhiteSpace(_obj.State.Properties.RegistrationNumber.OriginalValue)))
        e.AddError(Sungero.Docflow.Resources.NoRightsToModifyRegistrationNumber);
    }
    
    /// <summary>
    /// Проверить неизменность состояния регистрации.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ValidateRegistrationStateUnchanged(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var registrationStateOriginalValue = _obj.State.Properties.RegistrationState.OriginalValue;
      if (_obj.RegistrationState != _obj.State.Properties.RegistrationState.OriginalValue &&
          (_obj.State.Properties.RegistrationState.OriginalValue.HasValue || !Functions.OfficialDocument.IsNotRegistered(_obj)))
        e.AddError(Sungero.Docflow.Resources.NoRightsToModifyRegistrationInformation);
    }
    
    /// <summary>
    /// Проверить неизменность журнала регистрации.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ValidateDocumentRegisterIsUnchanged(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (!Equals(_obj.DocumentRegister, _obj.State.Properties.DocumentRegister.OriginalValue))
        e.AddError(Sungero.Docflow.Resources.NoRightsToModifyDocumentRegister);
    }
    
    /// <summary>
    /// Проверить неизменность данных: "Дело" и "Направлено в дело" - на сервере при отсутствии прав на регистрацию.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ValidateCaseFileDataIsUnchanged(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.PlacedToCaseFileDate.HasValue &&
          _obj.PlacedToCaseFileDate.Value != _obj.State.Properties.PlacedToCaseFileDate.OriginalValue &&
          Functions.OfficialDocument.IsRegistrable(_obj))
        e.AddError(Sungero.Docflow.Resources.NoRightsToModifyFAddedToFileListDate);

      // Проверить неизменность дела.
      if (!Equals(_obj.CaseFile, _obj.State.Properties.CaseFile.OriginalValue) &&
          Functions.OfficialDocument.IsRegistrable(_obj))
        e.AddError(Sungero.Docflow.Resources.NoRightsToModifyFileList);
    }
    
    /// <summary>
    /// Проверить неизменность данных по местонахождению документа.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ValidateDocumentTrackingIsUnchanged(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.State.Properties.Tracking.IsChanged && (!this.CanRegister() && Functions.OfficialDocument.IsRegistrable(_obj) && !_obj.AccessRights.CanSendByExchange()))
        e.AddError(Sungero.Docflow.Resources.NoRightsToModifyTracking);
    }
    
    /// <summary>
    /// Проверить неизменность регистрационных данных на сервере при отсутствии прав на изменение этих свойств.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ValidateRegistrationDataIsUnchanged(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Проверка неизменности данных.
      var canReserve = Functions.OfficialDocument.IsRegistrable(_obj) &&
        (_obj.AccessRights.CanRegister() || Docflow.PublicFunctions.RegistrationSetting.GetSettingByDocument(_obj, Docflow.RegistrationSetting.SettingType.Reservation) != null);
      // Игнорировать проверку изменения рег. данных при смене типа документа.
      if (this.CanRegister() || canReserve || this.DocumentTypeChanged() || !this.IsOnVerification())
        return;

      this.ValidateRegistrationDateUnchanged(e);
      this.ValidateRegistrationStateUnchanged(e);
      this.ValidateDocumentRegisterIsUnchanged(e);
      this.ValidateCaseFileDataIsUnchanged(e);
      this.ValidateDocumentTrackingIsUnchanged(e);
    }
    
    #endregion
    
    /// <summary>
    /// Проверить, что для возвращенных документов указан результат возврата.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ValidateReturnedDocumentResults(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var completeTracking = _obj.Tracking
        .Where(l => l.ReturnDate.HasValue && l.DeliveredTo != null);
      if (completeTracking.Any(l => l.ReturnResult == null))
        e.AddError(Docflow.Resources.ToReturnDocumentYouMustSpecifiedReturnResult);
    }
    
    /// <summary>
    /// Заполнить свойство "Местонахождение" в карточке документа.
    /// </summary>
    public virtual void FillLocationState()
    {
      var trackingState = Functions.OfficialDocument.GetLocationState(_obj);
      if (!string.IsNullOrEmpty(trackingState) || _obj.ExchangeState == null)
        _obj.LocationState = trackingState;
    }
    
    /// <summary>
    /// Заполнить свойства выдачи, относящиеся к контрагенту.
    /// </summary>
    /// <param name="tracking">Строка выдачи.</param>
    public virtual void FillIssueToContractor(System.Collections.Generic.IEnumerable<IOfficialDocumentTracking> tracking)
    {
      // Для отправки контрагенту.
      var issueToContractor = tracking
        .Where(l => l.ReturnDeadline.HasValue && l.Action == Sungero.Docflow.OfficialDocumentTracking.Action.Endorsement)
        .OrderBy(l => l.ReturnDeadline).ThenBy(l => !(l.IsOriginal ?? false)).FirstOrDefault();

      _obj.ResponsibleForReturnEmployee = issueToContractor?.DeliveredTo;
      _obj.IsHeldByCounterParty = issueToContractor != null;
      _obj.ScheduledReturnDateFromCounterparty = issueToContractor?.ReturnDeadline;
    }
    
    /// <summary>
    /// Заполнить свойства выдачи, относящиеся к сотруднику.
    /// </summary>
    /// <param name="tracking">Строка выдачи.</param>
    public virtual void FillIssueToEmployee(System.Collections.Generic.IEnumerable<IOfficialDocumentTracking> tracking)
    {
      // Для выдачи сотруднику.
      var issueToEmployee = tracking
        .Where(l => l.Action != Sungero.Docflow.OfficialDocumentTracking.Action.Endorsement && !l.ReturnDate.HasValue)
        .OrderBy(l => !l.ReturnDeadline.HasValue)
        .ThenByDescending(l => l.DeliveryDate)
        .ThenBy(l => !(l.IsOriginal ?? false))
        .FirstOrDefault();
      
      _obj.IsReturnRequired = issueToEmployee?.ReturnDeadline != null;
      _obj.ReturnDeadline = issueToEmployee?.ReturnDeadline;
      _obj.DeliveredTo = issueToEmployee?.DeliveredTo;
      _obj.ReturnDate = issueToEmployee != null
        ? null
        : _obj.Tracking.Where(l => l.ReturnDate.HasValue)
        .OrderByDescending(l => l.ReturnDate)
        .Select(l => l.ReturnDate)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Заполнение полей для вычислимых списков выданных и отправленных контрагенту.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void FillDocumentReturn(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var tracking = _obj.Tracking
        .Where(l => l.DeliveredTo != null)
        .Where(l => !l.ReturnDate.HasValue || Equals(l.ReturnResult, ReturnResult.AtControl))
        .OrderByDescending(l => l.DeliveryDate);
      this.FillIssueToContractor(tracking);
      this.FillIssueToEmployee(tracking);

      // Проверка срока возврата.
      if (_obj.Tracking.Any(l => l.DeliveryDate > l.ReturnDeadline))
        e.AddError(Docflow.Resources.ReturnDocumentDeliveryAndScheduledDate);
    }
    
    /// <summary>
    /// Валидация возврата от контрагента.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ValidateReturnFromCounterparty(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var activeCounterpartyTask = _obj.State.Properties.Tracking.Changed
        .Where(l => l.ReturnResult != null && l.Action != Docflow.OfficialDocumentTracking.Action.Delivery && l.ReturnTask != null)
        .Select(l => l.ReturnTask.Id).Distinct();
      foreach (var task in activeCounterpartyTask)
      {
        var counterpartyTrackings = _obj.State.Properties.Tracking.Changed
          .Where(l => l.ReturnTask != null && l.ReturnTask.Id == task && l.ReturnResult != null);
        var defaultResult = counterpartyTrackings.FirstOrDefault().ReturnResult;
        foreach (var counterpartyTracking in counterpartyTrackings)
          if (counterpartyTracking.ReturnResult != defaultResult)
            e.AddError(Docflow.Resources.DifferentReturnResultForTask);
      }
    }
    
    /// <summary>
    /// Прекратить задачи по удаленным из "Выдачи" строкам.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void StopTasksByDeletedTracking(Sungero.Domain.BeforeSaveEventArgs e)
    {
      foreach (var documentTracking in Functions.OfficialDocument.GetDeletedTrackingRecords(_obj))
      {
        var result = string.Empty;
        if (Docflow.Functions.Module.IsTaskUsingOldScheme(documentTracking.ReturnTask))
          result = Functions.Module.CompleteCheckReturnTask(documentTracking.ReturnTask.Id, Constants.Module.ReturnControl.AbortTask, CheckReturnTasks.Resources.TrackingHasBeenDeleted);
        else
          e.Params.AddOrUpdate(Constants.OfficialDocument.ExecuteAllMonitoringBlocksForReturnTasks, true);
        
        if (string.IsNullOrWhiteSpace(result))
        {
          if (!_obj.State.Properties.Tracking.Deleted.Contains(documentTracking))
            documentTracking.ReturnTask = null;
        }
        else
        {
          e.AddError(result);
          return;
        }
      }
    }
    
    /// <summary>
    /// Выполнить задание на контроль возврата.
    /// </summary>
    /// <param name="tracking">Строка выдачи.</param>
    /// <returns>Текст ошибки, пустую строку или null, если ошибок нет.</returns>
    public virtual string CompleteReturnTaskWithoutReturnControl(IOfficialDocumentTracking tracking)
    {
      var operation = tracking.ReturnResult == Docflow.OfficialDocumentTracking.ReturnResult.Signed
        ? Constants.Module.ReturnControl.SignAssignment
        : Constants.Module.ReturnControl.NotSignAssignment;
      var hasNotReturnedDocument = _obj.Tracking
        .Where(l => l.ReturnTask != null && l.ReturnTask.Id == tracking.ReturnTask.Id)
        .Any(l => l.ReturnResult == null);
      
      // Записать факт изменения Выдачи не системным пользователем через грид.
      if (Users.Current?.IsSystem != true && !hasNotReturnedDocument)
        Docflow.PublicFunctions.OfficialDocument.WriteTrackingLog("CompleteReturnTaskWithoutReturnControl. Update tracking manually.", tracking);
      
      return hasNotReturnedDocument ? null : Functions.Module.CompleteCheckReturnTask(tracking.ReturnTask.Id, operation);
    }
    
    /// <summary>
    /// Выполнить задания по тем измененным строкам, у которых указана дата возврата.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void CompleteTasksByChangedTrackingRecord(Sungero.Domain.BeforeSaveEventArgs e)
    {
      bool isCheckReturnTaskCallContext = false;
      e.Params.TryGetValue(Docflow.Constants.Module.CheckReturnTaskCallContext, out isCheckReturnTaskCallContext);
      if (isCheckReturnTaskCallContext)
        return;
      
      var groupedByReturnTask = Functions.OfficialDocument.GetChangedTrackingRecordsWithTasksInProcess(_obj)
        .GroupBy(t => t.ReturnTask.Id);
      foreach (var group in groupedByReturnTask)
      {
        var tracking = group.First();
        var isReturnControlTask = tracking.ReturnTask.Info.Name == CheckReturnTasks.Info.Name;
        var result = string.Empty;
        
        if (isReturnControlTask && !Docflow.Functions.Module.IsTaskUsingOldScheme(tracking.ReturnTask))
          e.Params.AddOrUpdate(Constants.OfficialDocument.ExecuteAllMonitoringBlocksForReturnTasks, true);
        else
          result = isReturnControlTask
            ? Functions.Module.CompleteCheckReturnTask(tracking.ReturnTask.Id, Constants.Module.ReturnControl.CompleteAssignment)
            : this.CompleteReturnTaskWithoutReturnControl(tracking);
        
        if (!string.IsNullOrWhiteSpace(result))
        {
          e.AddError(result);
          return;
        }
        foreach (var endorsedTracking in group.Where(t => t.Note == ApprovalTasks.Resources.CommentOnEndorsement))
          endorsedTracking.Note = null;
      }
    }
    
    /// <summary>
    /// Синхронизировать сроки заданий между "Выдачей" и задачами на контроль возврата.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void SyncReturnTaskDeadlines(Sungero.Domain.BeforeSaveEventArgs e)
    {
      bool isDeadlineExtensionTaskCallContext = false;
      e.Params.TryGetValue(Docflow.Constants.Module.DeadlineExtentsionTaskCallContext, out isDeadlineExtensionTaskCallContext);
      if (isDeadlineExtensionTaskCallContext) 
        return;
      foreach (var documentTracking in Functions.OfficialDocument.GetTrackingRecordsWithDeadlineChanged(_obj))
      {
        var result = string.Empty;
        if (Docflow.Functions.Module.IsTaskUsingOldScheme(documentTracking.ReturnTask))
          result = Functions.Module.CompleteCheckReturnTask(documentTracking.ReturnTask.Id, Constants.Module.ReturnControl.DeadlineChange, null, documentTracking.ReturnDeadline);
        else
          e.Params.AddOrUpdate(Constants.OfficialDocument.ExecuteAllMonitoringBlocksForReturnTasks, true);
        if (!string.IsNullOrWhiteSpace(result))
        {
          e.AddError(result);
          return;
        }
      }
    }
    
    /// <summary>
    /// Для всех измененных исполнителей прекращать старую и стартовать новую задачу.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ForChangedAssigneeCreateNewTask(Sungero.Domain.BeforeSaveEventArgs e)
    {
      foreach (var tracking in Functions.OfficialDocument.GetTrackingRecordsWithEmployeeChanged(_obj))
      {
        var result = string.Empty;
        if (Docflow.Functions.Module.IsTaskUsingOldScheme(tracking.ReturnTask))
          result = Functions.Module.CompleteCheckReturnTask(tracking.ReturnTask.Id, Constants.Module.ReturnControl.AbortTask, CheckReturnTasks.Resources.TrackingHasBeenDeleted);
        else
          e.Params.AddOrUpdate(Constants.OfficialDocument.ExecuteAllMonitoringBlocksForReturnTasks, true);
        if (!string.IsNullOrWhiteSpace(result))
        {
          e.AddError(result);
          return;
        }
        this.CreateAndStartCheckReturnTaskForDocument(tracking);
      }
    }
    
    /// <summary>
    /// Создать и стартовать задачу на контроль возврата.
    /// </summary>
    /// <param name="tracking">Строка выдачи.</param>
    public virtual void CreateAndStartCheckReturnTaskForDocument(IOfficialDocumentTracking tracking)
    {
      var task = CheckReturnTasks.Create();
      task.Assignee = tracking.DeliveredTo;
      task.MaxDeadline = tracking.ReturnDeadline;
      task.DocumentGroup.OfficialDocuments.Add(_obj);
      task.Start();
      tracking.ReturnTask = task;
    }
    
    /// <summary>
    /// Создать и стартовать новые задачи на контроль возврата.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void CreateNewTasks(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Отправить новые задачи.
      var trackingsToCreateTasks = _obj.Tracking
        .Where(l => l.ReturnDeadline.HasValue &&
               l.Action == Docflow.OfficialDocumentTracking.Action.Delivery &&
               !l.ReturnDate.HasValue &&
               l.ReturnTask == null &&
               l.ExternalLinkId == null);
      foreach (var tracking in trackingsToCreateTasks)
        this.CreateAndStartCheckReturnTaskForDocument(tracking);
    }
    
    /// <summary>
    /// Обработка задач на контроль возврата.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ReturningTasksControl(Sungero.Domain.BeforeSaveEventArgs e)
    {
      this.StopTasksByDeletedTracking(e);
      this.CompleteTasksByChangedTrackingRecord(e);
      this.SyncReturnTaskDeadlines(e);
      this.ForChangedAssigneeCreateNewTask(e);
      this.CreateNewTasks(e);
      
      // Обработка ручного возврата от контрагента.
      Functions.OfficialDocument.UpdateExternalApprovalStateAfterReturnFromCounterparty(_obj);
      if (_obj.State.Properties.ExternalApprovalState.OriginalValue != _obj.ExternalApprovalState)
        e.Params.AddOrUpdate(Constants.OfficialDocument.ExecuteAllMonitoringBlocksForCounterpartyReturnTasks, true);
    }
    
    /// <summary>
    /// Проверить право подписи у подписанта с нашей стороны при верификации.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void CheckSignatureRights(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (!this.IsOnVerification()) 
        return;
      
      // Проводить проверку только в UI, если это необходимо для документа и статус верификации "В процессе" или его значение изменено.
      var needValidateOurSignatory = Functions.OfficialDocument.NeedValidateOurSignatorySignatureSetting(_obj);
      var isVisualMode = ((Domain.Shared.IExtendedEntity)_obj).Params.ContainsKey(Constants.OfficialDocument.IsVisualModeParamName);
      
      if (needValidateOurSignatory && isVisualMode && _obj.OurSignatory != null
          && !Functions.OfficialDocument.CanSignByEmployee(_obj, _obj.OurSignatory))
      {
        var message = Sungero.Docflow.OfficialDocuments.Resources.IncorrectOurSignatoryFormat(_obj.OurSignatory.Name);
        e.AddError(_obj.Info.Properties.OurSignatory, message);
      }
    }
    
    /// <summary>
    /// Проверить неизменность подразделения, если нет прав на его изменение.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void ValidateOffDocBeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (this.DepartmentChanged() && Functions.OfficialDocument.NeedDisableDepartment(_obj))
        e.AddError(Sungero.Docflow.OfficialDocuments.Resources.DepartmentPropertyDisabled);
      
      // Проверить неизменность НОР, если нет прав на ее изменение.
      if (this.BusinessUnitChanged() && Functions.OfficialDocument.NeedDisableBusinessUnit(_obj))
        e.AddError(Sungero.Docflow.OfficialDocuments.Resources.BusinessUnitPropertyDisabled);
      this.CheckSignatureRights(e);
      this.ValidateReturnFromCounterparty(e);
      this.ValidateReturnedDocumentResults(e);
    }
    
    /// <summary>
    /// Обновить поля документа до сохранения.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void UpdateOffDocFieldsBeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // TODO: удалить код после исправления бага платформы 46179.
      this.FixModifiedField(e);
      this.FillLocationState();
      this.FillDocumentReturn(e);
      // Заполнить имя.
      Functions.OfficialDocument.FillName(_obj);
      // Очистка НОР для ненумеруемых документов.
      Functions.OfficialDocument.ClearBusinessUnit(_obj, _obj.DocumentKind);
      _obj.DocumentDate = _obj.RegistrationDate ?? _obj.Created;
    }
    
    #endregion
    
    /// <summary>
    /// Указать, что вид носителя документа - электронный.
    /// </summary>
    [Public]
    public virtual void SetElectronicMediumType()
    {
      _obj.Medium = Functions.MediumType.GetNativeElectronicMediumType();
    }
    
  }
}