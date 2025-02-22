using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.ApprovalStage;
using Init = Sungero.FinancialArchive.Constants.Module.Initialize;

namespace Sungero.FinancialArchive.Server
{
  public class ModuleFunctions
  {
    #region МКДО
    
    /// <summary>
    /// Создать накладную.
    /// </summary>
    /// <param name="comment">Комментарий.</param>
    /// <param name="box">Ящик обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="info">Информация о документе.</param>
    /// <returns>Созданный документ.</returns>
    [Public]
    public static IWaybill CreateWaybillDocument(string comment, Sungero.ExchangeCore.IBoxBase box, Sungero.Parties.ICounterparty counterparty, Sungero.Exchange.IExchangeDocumentInfo info)
    {
      var waybill = CreateDocument<IWaybill>(comment, box, counterparty, info);
      waybill.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.Waybill;

      var kind = Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Init.WaybillDocumentKind);
      if (kind != null)
        waybill.DocumentKind = kind;

      return waybill;
    }
    
    /// <summary>
    /// Создать счёт-фактуру полученный.
    /// </summary>
    /// <param name="comment">Комментарий.</param>
    /// <param name="box">Ящик обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="isAdjustment">Корректировка.</param>
    /// <param name="corrected">Корректирует.</param>
    /// <param name="info">Информация о документе.</param>
    /// <returns>Созданный документ.</returns>
    [Public]
    public static FinancialArchive.IIncomingTaxInvoice CreateIncomingTaxInvoiceDocument(string comment, Sungero.ExchangeCore.IBoxBase box,
                                                                                        Sungero.Parties.ICounterparty counterparty, bool isAdjustment,
                                                                                        Sungero.Docflow.IAccountingDocumentBase corrected, Sungero.Exchange.IExchangeDocumentInfo info)
    {
      var incomingTaxInvoice = CreateDocument<IIncomingTaxInvoice>(comment, box, counterparty, info);
      incomingTaxInvoice.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.Invoice;
      incomingTaxInvoice.IsAdjustment = isAdjustment;

      var kind = Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Init.IncomingTaxInvoiceKind);
      if (kind != null)
        incomingTaxInvoice.DocumentKind = kind;

      if (corrected != null)
      {
        incomingTaxInvoice.Corrected = corrected;
        incomingTaxInvoice.LeadingDocument = corrected.LeadingDocument;
      }
      
      return incomingTaxInvoice;
    }
    
    /// <summary>
    /// Создать счёт-фактуру выставленный.
    /// </summary>
    /// <param name="comment">Комментарий.</param>
    /// <param name="box">Ящик обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="isAdjustment">Корректировка.</param>
    /// <param name="corrected">Корректирует.</param>
    /// <param name="info">Информация о документе.</param>
    /// <returns>Созданный документ.</returns>
    [Public]
    public static FinancialArchive.IOutgoingTaxInvoice CreateOutgoingTaxInvoiceDocument(string comment, Sungero.ExchangeCore.IBoxBase box,
                                                                                        Sungero.Parties.ICounterparty counterparty, bool isAdjustment,
                                                                                        Sungero.Docflow.IAccountingDocumentBase corrected, Sungero.Exchange.IExchangeDocumentInfo info)
    {
      var outgoingTaxInvoice = CreateDocument<IOutgoingTaxInvoice>(comment, box, counterparty, info);
      outgoingTaxInvoice.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.Invoice;
      outgoingTaxInvoice.IsAdjustment = isAdjustment;

      var kind = Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Init.OutgoingTaxInvoiceKind);
      if (kind != null)
        outgoingTaxInvoice.DocumentKind = kind;

      if (corrected != null)
      {
        outgoingTaxInvoice.Corrected = corrected;
        outgoingTaxInvoice.LeadingDocument = corrected.LeadingDocument;
      }

      return outgoingTaxInvoice;
    }
    
    /// <summary>
    /// Создать акт.
    /// </summary>
    /// <param name="comment">Комментарий.</param>
    /// <param name="box">Ящик обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="info">Информация о документе.</param>
    /// <returns>Созданный документ.</returns>
    [Public]
    public static IContractStatement CreateContractStatementDocument(string comment, Sungero.ExchangeCore.IBoxBase box, Sungero.Parties.ICounterparty counterparty, Sungero.Exchange.IExchangeDocumentInfo info)
    {
      var contractStatement = CreateDocument<IContractStatement>(comment, box, counterparty, info);
      contractStatement.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.Act;
      
      var kind = Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Init.ContractStatementKind);
      if (kind != null)
        contractStatement.DocumentKind = kind;
      
      return contractStatement;
    }
    
    /// <summary>
    /// Создать универсальный передаточный документ СЧФДОП.
    /// </summary>
    /// <param name="comment">Комментарий.</param>
    /// <param name="box">Ящик обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="isAdjustment">Корректировка.</param>
    /// <param name="corrected">Корректирует.</param>
    /// <param name="info">Информация о документе.</param>
    /// <returns>Созданный документ.</returns>
    [Public]
    public static Docflow.IAccountingDocumentBase CreateUniversalTaxInvoiceAndBasic(string comment, Sungero.ExchangeCore.IBoxBase box,
                                                                                    Sungero.Parties.ICounterparty counterparty, bool isAdjustment,
                                                                                    Sungero.Docflow.IAccountingDocumentBase corrected, Sungero.Exchange.IExchangeDocumentInfo info)
    {
      var universalDocument = CreateDocument<IUniversalTransferDocument>(comment, box, counterparty, info);
      universalDocument.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer;
      universalDocument.IsAdjustment = isAdjustment;
      
      var kind = Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Init.UniversalTaxInvoiceAndBasicKind);
      if (kind != null)
        universalDocument.DocumentKind = kind;
      
      if (corrected != null)
      {
        universalDocument.Corrected = corrected;
        universalDocument.LeadingDocument = corrected.LeadingDocument;
      }

      return universalDocument;
    }
    
    /// <summary>
    /// Создать универсальный передаточный документ ДОП.
    /// </summary>
    /// <param name="comment">Комментарий.</param>
    /// <param name="box">Ящик обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="isAdjustment">Корректировка.</param>
    /// <param name="corrected">Корректирует.</param>
    /// <param name="info">Информация о документе.</param>
    /// <returns>Созданный документ.</returns>
    [Public]
    public static Docflow.IAccountingDocumentBase CreateUniversalBasic(string comment, Sungero.ExchangeCore.IBoxBase box,
                                                                       Sungero.Parties.ICounterparty counterparty, bool isAdjustment,
                                                                       Sungero.Docflow.IAccountingDocumentBase corrected, Sungero.Exchange.IExchangeDocumentInfo info)
    {
      var universalDocument = CreateDocument<IUniversalTransferDocument>(comment, box, counterparty, info);
      universalDocument.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer;
      universalDocument.IsAdjustment = isAdjustment;
      
      var kind = Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Init.UniversalBasicKind);
      if (kind != null)
        universalDocument.DocumentKind = kind;
      
      if (corrected != null)
      {
        universalDocument.Corrected = corrected;
        universalDocument.LeadingDocument = corrected.LeadingDocument;
      }
      
      return universalDocument;
    }

    /// <summary>
    /// Создать документ из МКДО.
    /// </summary>
    /// <param name="comment">Комментарий.</param>
    /// <param name="box">Ящик обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="info">Информация о документе.</param>
    /// <returns>Созданный документ.</returns>
    private static T CreateDocument<T>(string comment, Sungero.ExchangeCore.IBoxBase box,
                                       Sungero.Parties.ICounterparty counterparty, Sungero.Exchange.IExchangeDocumentInfo info) where T : Docflow.IAccountingDocumentBase
    {
      var exchangeDoc = Sungero.Docflow.Shared.OfficialDocumentRepository<T>.Create();
      
      if (!string.IsNullOrEmpty(comment) && comment.Length > exchangeDoc.Info.Properties.Note.Length)
        comment = comment.Substring(0, exchangeDoc.Info.Properties.Note.Length);
      
      exchangeDoc.IsFormalized = true;
      exchangeDoc.Note = comment;
      exchangeDoc.BusinessUnit = ExchangeCore.PublicFunctions.BoxBase.GetBusinessUnit(box);
      exchangeDoc.Counterparty = counterparty;

      return exchangeDoc;
    }
    
    #endregion
    
    #region Поиск по гиперссылкам
    
    /// <summary>
    /// Найти бухгалтерский документ.
    /// </summary>
    /// <param name="number">Номер.</param>
    /// <param name="date">Дата.</param>
    /// <param name="butin">ИНН НОР.</param>
    /// <param name="butrrc">КПП НОР.</param>
    /// <param name="cuuid">Uuid контрагента.</param>
    /// <param name="ctin">ИНН контрагента.</param>
    /// <param name="ctrrc">КПП контрагента.</param>
    /// <param name="corrective">Признак "Корректировочный".</param>
    /// <param name="incomingTaxInvoice">True, если искать среди счетов-фактур полученных.</param>
    /// <param name="outgoingTaxInvoice">True, если искать среди счетов-фактур выставленных.</param>
    /// <param name="contractStatement">True, если искать среди актов.</param>
    /// <param name="waybill">True, если искать среди накладных.</param>
    /// <param name="universalTransferDocument">True, если искать среди УПД.</param>
    /// <returns>Список бухгалтерских документов.</returns>
    [Remote(IsPure = true)]
    public List<IAccountingDocumentBase> FindAccountingDocuments(string number, string date,
                                                                 string butin, string butrrc,
                                                                 string cuuid, string ctin, string ctrrc,
                                                                 bool corrective,
                                                                 bool incomingTaxInvoice,
                                                                 bool outgoingTaxInvoice,
                                                                 bool contractStatement,
                                                                 bool waybill,
                                                                 bool universalTransferDocument)
    {
      var result = AccountingDocumentBases.GetAll()
        .Where(a => incomingTaxInvoice && IncomingTaxInvoices.Is(a) ||
               outgoingTaxInvoice && OutgoingTaxInvoices.Is(a) ||
               contractStatement && ContractStatements.Is(a) ||
               waybill && Waybills.Is(a) ||
               universalTransferDocument && UniversalTransferDocuments.Is(a));
      
      // Фильтр по НОР.
      if (string.IsNullOrWhiteSpace(butin) || string.IsNullOrWhiteSpace(butrrc))
        return new List<IAccountingDocumentBase>();
      
      var businessUnit = Sungero.Company.BusinessUnits.GetAll().FirstOrDefault(x => x.TIN == butin && x.TRRC == butrrc);
      if (businessUnit == null)
        return new List<IAccountingDocumentBase>();
      else
        result = result.Where(x => Equals(x.BusinessUnit, businessUnit));
      
      // Фильтр по номеру.
      var relevantNumbers = this.GetRelevantNumbers(number);
      result = result.Where(x => relevantNumbers.Contains(x.RegistrationNumber));
      
      // Фильтр по дате.
      DateTime parsedDate;
      if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParseExact(date,
                                                                     "dd'.'MM'.'yyyy",
                                                                     System.Globalization.CultureInfo.InvariantCulture,
                                                                     System.Globalization.DateTimeStyles.None,
                                                                     out parsedDate))
        result = result.Where(x => x.RegistrationDate == parsedDate);
      
      // Фильтр по контрагенту.
      var counterparties = Sungero.Parties.PublicFunctions.Module.Remote.FindCounterparty(cuuid, ctin, ctrrc, string.Empty);
      if (counterparties.Any())
        result = result.Where(x => counterparties.Contains(x.Counterparty));
      
      // Фильтр корректировочный или нет.
      result = result.Where(x => x.IsAdjustment == corrective);
      
      return result.ToList();
    }
    
    /// <summary>
    /// Получить список номеров, соответствующих заданному рег. номеру документа.
    /// </summary>
    /// <param name="number">Рег. номер.</param>
    /// <returns>Список номеров, соответствующих заданному.</returns>
    private List<string> GetRelevantNumbers(string number)
    {
      var relevantNumbers = new List<string>();
      relevantNumbers.Add(number);
      
      // Регулярное выражение соответствует первому вхождению нуля или группы нулей.
      var leadZerosRegex = new System.Text.RegularExpressions.Regex("0+");
      
      // Поиск в конце номера подстроки, состоящей только из цифр.
      var pattern = @"\d+$";
      System.Text.RegularExpressions.Match isMatch = System.Text.RegularExpressions.Regex.Match(number, pattern);
      if (isMatch.Success)
      {
        // Если подстрока найдена, то удаляем ведущие нули в ней и добавляем в список номеров.
        relevantNumbers.Add(leadZerosRegex.Replace(isMatch.Value, string.Empty, 1));
      }
      
      // Получаем номер с префиксом, но без ведущих нулей.
      pattern = @"^\D*0+\d+$";
      isMatch = System.Text.RegularExpressions.Regex.Match(number, pattern);
      if (isMatch.Success)
      {
        relevantNumbers.Add(leadZerosRegex.Replace(number, string.Empty, 1));
      }
      return relevantNumbers;
    }
    
    #endregion
    
    #region Импорт формализованных документов
    
    /// <summary>
    /// Загрузить формализованный документ из XML.
    /// </summary>
    /// <param name="file">XML.</param>
    /// <param name="requireFtsId">Соотносить НОР и Контрагента только по ФНС-ИД.</param>
    /// <returns>Структура с созданным документом и его телами.</returns>
    [Remote, Public]
    public virtual Structures.Module.IImportResult ImportFormalizedDocument(Docflow.Structures.Module.IByteArray file, bool requireFtsId)
    {
      var sellerTitle = FormalizeDocumentsParser.Extension.GetDocument<FormalizeDocumentsParser.ISellerTitle>(file.Bytes);
      
      var beforeCreateError = this.ValidateBeforeCreateDocument(sellerTitle);
      if (!string.IsNullOrEmpty(beforeCreateError))
        return CreateErrorImportResult(beforeCreateError);
      
      var document = this.CreateAccountingDocument(sellerTitle, requireFtsId);

      var afterCreateError = this.ValidateAfterCreateDocument(sellerTitle, document, requireFtsId);
      if (!string.IsNullOrEmpty(afterCreateError))
        return CreateErrorImportResult(afterCreateError);
      
      return this.CreateDocumentImportResult(sellerTitle, document);
    }
    
    /// <summary>
    /// Создать финансовый документ.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="requireFtsId">Соотносить НОР и Контрагента только по ФНС-ИД.</param>
    /// <returns>Документ.</returns>
    public virtual Sungero.Docflow.IAccountingDocumentBase CreateAccountingDocument(FormalizeDocumentsParser.ISellerTitle sellerTitle, bool requireFtsId)
    {
      Logger.DebugFormat("CreateAccountingDocument. Start.");
      var document = this.CreateEmptyAccountingDocument(sellerTitle);
      this.FillDocumentProperties(sellerTitle, document, requireFtsId);
      Logger.DebugFormat("CreateAccountingDocument. Done. Document id = '{0}'", document.Id);
      return document;
    }
    
    /// <summary>
    /// Заполнить свойства документа.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="document">Документ.</param>
    /// <param name="requireFtsId">Соотносить НОР и Контрагента только по ФНС ИД.</param>
    public virtual void FillDocumentProperties(FormalizeDocumentsParser.ISellerTitle sellerTitle, IAccountingDocumentBase document, bool requireFtsId)
    {
      Logger.DebugFormat("FillDocumentProperties. Start. Document id = '{0}'", document.Id);
      
      // НОР документа должна подбираться по параметрам, а не от сотрудника.
      document.BusinessUnit = null;
      
      document.IsAdjustment = sellerTitle.IsAdjustment;
      document.IsRevision = sellerTitle.IsRevision;
      document.TotalAmount = (double?)sellerTitle.TotalAmount;
      document.IsFormalized = true;
      document.IsFormalizedSignatoryEmpty = !FormalizeDocumentsParser.SellerSignatoryInfo.HasSellerSignatoryInfo(sellerTitle.Body);
      
      if (document.TotalAmount.HasValue)
        document.Currency = Commons.Currencies.GetAll().SingleOrDefault(c => c.NumericCode == sellerTitle.CurrencyCode);
      
      if (sellerTitle.Function.HasValue)
      {
        document.FormalizedFunction = this.GetUniversalDocumentFunction(sellerTitle);
        document.DocumentKind = this.GetDocumentKind(sellerTitle);
      }

      document.FormalizedServiceType = this.GetFormalizedServiceType(sellerTitle);
      document.BusinessUnitBox = GetBusinessUnitBoxForImportedDocument(sellerTitle, requireFtsId);
      document.BusinessUnit = GetBusinessUnitForImportedDocument(document, sellerTitle, requireFtsId);
      document.Counterparty = GetCounterpartyForImportedDocument(document, sellerTitle, requireFtsId);
      
      Docflow.PublicFunctions.OfficialDocument.SetElectronicMediumType(document);
      
      var version = this.CreateNewDocumentVersion(document);
      // Не переносить очистку содержания bug 348057.
      document.Subject = string.Empty;
      document.SellerTitleId = version.Id;
      this.TryRegisterDocument(sellerTitle, document);
      
      var isUniversalCorrection = sellerTitle.DocumentType == FormalizeDocumentsParser.DocumentType.UniversalCorrectionTransferDocument;
      if (isUniversalCorrection && !string.IsNullOrEmpty(sellerTitle.Number) && sellerTitle.Date != null)
        document.Corrected = this.GetCorrectedDocument(sellerTitle, document);
      
      Logger.DebugFormat("FillDocumentProperties. Done. Document id = '{0}'", document.Id);
    }
    
    /// <summary>
    /// Проверка документа после создания.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="document">Документ.</param>
    /// <param name="requireFtsId">Соотносить НОР и Контрагента только по ФНС-ИД.</param>
    /// <returns>Текст ошибки.</returns>
    public virtual string ValidateAfterCreateDocument(FormalizeDocumentsParser.ISellerTitle sellerTitle, IAccountingDocumentBase document, bool requireFtsId)
    {
      if (document.BusinessUnit == null)
      {
        return requireFtsId ?
          Resources.ImportDialog_NoBusinessUnitWithFTSIdFormat(sellerTitle.Seller.GetName(), sellerTitle.Seller.Tin, sellerTitle.Seller.Trrc, sellerTitle.SenderId) :
          Resources.ImportDialog_NoBusinessUnitWithTINFormat(sellerTitle.Seller.GetName(), sellerTitle.Seller.Tin, sellerTitle.Seller.Trrc);
      }
      
      if (document.Counterparty == null)
      {
        return requireFtsId ?
          Resources.ImportDialog_NoCounterpartyWithFTSIdFormat(sellerTitle.Buyer.GetName(), sellerTitle.Buyer.Tin, sellerTitle.Buyer.Trrc, sellerTitle.ReceiverId) :
          Resources.ImportDialog_NoCounterpartyWithTINFormat(sellerTitle.Buyer.GetName(), sellerTitle.Buyer.Tin, sellerTitle.Buyer.Trrc);
      }
      
      if (document.Counterparty.CanExchange != true)
        return Resources.ImportDialog_NoExchangeWithCounterpartyFormat(sellerTitle.Buyer.GetName(), sellerTitle.Buyer.Tin, sellerTitle.Buyer.Trrc);
      
      return string.Empty;
    }
    
    /// <summary>
    /// Получить корректируемый документ.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Корректируемый документ.</returns>
    public virtual IAccountingDocumentBase GetCorrectedDocument(FormalizeDocumentsParser.ISellerTitle sellerTitle, IAccountingDocumentBase document)
    {
      if (sellerTitle.Function == FormalizeDocumentsParser.UniversalDocumentFunction.Schf)
      {
        return Sungero.FinancialArchive.OutgoingTaxInvoices.GetAll()
          .Where(x => x.RegistrationNumber == sellerTitle.Number &&
                 x.RegistrationDate == sellerTitle.Date.Value && Equals(x.Counterparty, document.Counterparty))
          .FirstOrDefault();
      }
      
      return Sungero.FinancialArchive.UniversalTransferDocuments.GetAll()
        .Where(x => x.RegistrationNumber == sellerTitle.Number &&
               x.RegistrationDate == sellerTitle.Date.Value && Equals(x.Counterparty, document.Counterparty))
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Создать новую версию документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Новая версия документа.</returns>
    public virtual Sungero.Content.IElectronicDocumentVersions CreateNewDocumentVersion(IAccountingDocumentBase document)
    {
      var version = document.Versions.AddNew();
      version.AssociatedApplication = Content.AssociatedApplications.GetByExtension("pdf");
      version.BodyAssociatedApplication = Content.AssociatedApplications.GetByExtension("xml");
      
      if (Waybills.Is(document) || ContractStatements.Is(document) || UniversalTransferDocuments.Is(document))
        version.Note = FinancialArchive.Resources.SellerTitleVersionNote;
      
      return version;
    }
    
    /// <summary>
    /// Получить тип документа в сервисе.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <returns>Тип документа в сервисе.</returns>
    public virtual Enumeration GetFormalizedServiceType(FormalizeDocumentsParser.ISellerTitle sellerTitle)
    {
      if (IsGoodsTransferDocumentType(sellerTitle))
        return Docflow.AccountingDocumentBase.FormalizedServiceType.GoodsTransfer;
      
      if (IsWorksTransferDocumentType(sellerTitle))
        return Docflow.AccountingDocumentBase.FormalizedServiceType.WorksTransfer;
      
      return Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer;
    }
    
    /// <summary>
    /// Получить функцию финансового документа.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <returns>Функция документа.</returns>
    public virtual Enumeration GetUniversalDocumentFunction(FormalizeDocumentsParser.ISellerTitle sellerTitle)
    {
      switch (sellerTitle.Function.Value)
      {
        case FormalizeDocumentsParser.UniversalDocumentFunction.Schf:
          return Docflow.AccountingDocumentBase.FormalizedFunction.Schf;
        case FormalizeDocumentsParser.UniversalDocumentFunction.SchfDop:
          return Docflow.AccountingDocumentBase.FormalizedFunction.SchfDop;
        case FormalizeDocumentsParser.UniversalDocumentFunction.Dop:
          return Docflow.AccountingDocumentBase.FormalizedFunction.Dop;
        default:
          throw new Exception("Invalid value for UniversalDocumentFunction");
      }
    }
    
    /// <summary>
    /// Получить вид финансового документа.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <returns>Вид документа.</returns>
    public virtual IDocumentKind GetDocumentKind(FormalizeDocumentsParser.ISellerTitle sellerTitle)
    {
      switch (sellerTitle.Function.Value)
      {
        case FormalizeDocumentsParser.UniversalDocumentFunction.Schf:
          return Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Constants.Module.Initialize.OutgoingTaxInvoiceKind);
        case FormalizeDocumentsParser.UniversalDocumentFunction.SchfDop:
          return Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Constants.Module.Initialize.UniversalTaxInvoiceAndBasicKind);
        case FormalizeDocumentsParser.UniversalDocumentFunction.Dop:
          return Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(Constants.Module.Initialize.UniversalBasicKind);
        default:
          throw new Exception("Invalid value for UniversalDocumentFunction");
      }
    }
    
    /// <summary>
    /// Попытаться зарегистрировать документ с настройками по умолчанию.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="document">Документ.</param>
    public virtual void TryRegisterDocument(FormalizeDocumentsParser.ISellerTitle sellerTitle, IAccountingDocumentBase document)
    {
      if (document.IsRevision == true)
        document.Note = Exchange.Resources.TaxInvoiceRevisionFormat(sellerTitle.RevisionNumber, sellerTitle.RevisionDate.Value.Date.ToString("d"));
      else if (IsUniversalCorrectionDocumentType(sellerTitle))
        document.Note = Exchange.Resources.TaxInvoiceToFormat(sellerTitle.Number, sellerTitle.Date.Value.Date.ToString("d"));
      
      var number = sellerTitle.IsAdjustment ? sellerTitle.CorrectionNumber : sellerTitle.Number;
      var date = sellerTitle.IsAdjustment ? sellerTitle.CorrectionDate : sellerTitle.Date;
      var isRegistered = Docflow.PublicFunctions.OfficialDocument.TryExternalRegister(document, number, date);
      if (isRegistered && document.DocumentKind.NumberingType != Docflow.DocumentKind.NumberingType.NotNumerable)
        return;
      
      var note = string.Empty;
      if (IsUniversalTransferDocumentType(sellerTitle))
        note = Exchange.Resources.TaxInvoiceFormat(number, date.Value.Date.ToString("d"));
      else if (IsUniversalCorrectionDocumentType(sellerTitle))
        note = Exchange.Resources.TaxInvoiceCorrectionFormat(number, date.Value.Date.ToString("d"));
      else
        note = Exchange.Resources.IncomingNotNumeratedDocumentNoteFormat(date.Value.Date.ToString("d"), number);
      
      document.Note = note + Environment.NewLine + document.Note;
    }
    
    /// <summary>
    /// Заполнить свойства в структуре результата импорта XML.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Структура с результатом импорта XML.</returns>
    protected Structures.Module.IImportResult CreateDocumentImportResult(FormalizeDocumentsParser.ISellerTitle sellerTitle, IAccountingDocumentBase document)
    {
      Logger.DebugFormat("CreateDocumentImportResult. Start. Document id = '{0}'", document.Id);
      var result = Structures.Module.ImportResult.Create();
      this.FillResultGeneralInfo(sellerTitle, document, result);
      result.IsBusinessUnitFound = document.BusinessUnit != null;
      result.IsCounterpartyFound = document.Counterparty != null;
      result.IsCounterpartyCanExchange = document.Counterparty != null && document.Counterparty.CanExchange == true;
      result.Body = sellerTitle.Body;
      result.PublicBody = Docflow.PublicFunctions.Module.Remote.GeneratePublicBodyForFormalizedXml(Docflow.Structures.Module.ByteArray.Create(result.Body)).Bytes;
      result.IsSuccess = true;
      Logger.DebugFormat("CreateDocumentImportResult. Done. Document id = '{0}'", document.Id);
      return result;
    }
    
    /// <summary>
    /// Заполнить информацию в cтруктурe результата импорта XML.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="document">Документ.</param>
    /// <param name="result">Структура с результатом импорта XML.</param>
    protected void FillResultGeneralInfo(FormalizeDocumentsParser.ISellerTitle sellerTitle, IAccountingDocumentBase document,
                                         Structures.Module.IImportResult result)
    {
      result.Document = document;
      result.BusinessUnitName = sellerTitle.Seller.GetName();
      result.BusinessUnitTin = sellerTitle.Seller.Tin;
      result.BusinessUnitTrrc = sellerTitle.Seller.Trrc;
      result.BusinessUnitType = sellerTitle.Seller.Type.ToString();
      result.CounterpartyName = sellerTitle.Buyer.GetName();
      result.CounterpartyTin = sellerTitle.Buyer.Tin;
      result.CounterpartyTrrc = sellerTitle.Buyer.Trrc;
      result.CounterpartyType = sellerTitle.Buyer.Type.ToString();
      result.SenderFtsId = sellerTitle.SenderId;
      result.ReceiverFtsId = sellerTitle.ReceiverId;
    }
    
    /// <summary>
    /// Создать пустой (не заполненный) финансовый документ.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <returns>Документ.</returns>
    public virtual Sungero.Docflow.IAccountingDocumentBase CreateEmptyAccountingDocument(FormalizeDocumentsParser.ISellerTitle sellerTitle)
    {
      Logger.Debug("CreateEmptyAccountingDocument. Start.");
      
      if (IsGoodsTransferDocumentType(sellerTitle))
      {
        Logger.DebugFormat("CreateEmptyAccountingDocument. Done create waybill.");
        return FinancialArchive.Waybills.Create();
      }
      
      if (IsWorksTransferDocumentType(sellerTitle))
      {
        Logger.DebugFormat("CreateEmptyAccountingDocument. Done create contract statement.");
        return FinancialArchive.ContractStatements.Create();
      }
      
      if (IsSchfFunction(sellerTitle))
      {
        Logger.DebugFormat("CreateEmptyAccountingDocument. Done create outgoing tax invoice.");
        return FinancialArchive.OutgoingTaxInvoices.Create();
      }

      Logger.DebugFormat("CreateEmptyAccountingDocument. Done create universal transfer document.");
      return FinancialArchive.UniversalTransferDocuments.Create();
    }
    
    /// <summary>
    /// Валидация перед созданием документа.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <returns>Текст ошибки.</returns>
    public virtual string ValidateBeforeCreateDocument(FormalizeDocumentsParser.ISellerTitle sellerTitle)
    {
      if (sellerTitle == null)
        return Resources.ImportDialog_CannotRecognizeDocument;
      
      if (IsGoodsTransferDocumentType(sellerTitle) && !FinancialArchive.Waybills.AccessRights.CanCreate())
        return Resources.NoRightsToCreateDocumentFormat(FinancialArchive.Waybills.Info.LocalizedName);
      
      if (IsWorksTransferDocumentType(sellerTitle) && !FinancialArchive.ContractStatements.AccessRights.CanCreate())
        return Resources.NoRightsToCreateDocumentFormat(FinancialArchive.ContractStatements.Info.LocalizedName);
      
      if (IsSchfFunction(sellerTitle) && !FinancialArchive.OutgoingTaxInvoices.AccessRights.CanCreate())
        return Resources.NoRightsToCreateDocumentFormat(FinancialArchive.OutgoingTaxInvoices.Info.LocalizedName);
      
      if (!FinancialArchive.UniversalTransferDocuments.AccessRights.CanCreate())
        return Resources.NoRightsToCreateDocumentFormat(FinancialArchive.UniversalTransferDocuments.Info.LocalizedName);
      
      return string.Empty;
    }
    
    /// <summary>
    /// Проверить, является ли тип документа ДПТ.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <returns>True - если является ДПТ, иначе - false.</returns>
    private static bool IsGoodsTransferDocumentType(FormalizeDocumentsParser.ISellerTitle sellerTitle)
    {
      return sellerTitle.DocumentType == FormalizeDocumentsParser.DocumentType.GoodsTransferDocument;
    }
    
    /// <summary>
    /// Проверить, является ли тип документа ДПРР.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <returns>True - если является ДПРР, иначе - false.</returns>
    private static bool IsWorksTransferDocumentType(FormalizeDocumentsParser.ISellerTitle sellerTitle)
    {
      return sellerTitle.DocumentType == FormalizeDocumentsParser.DocumentType.WorksTransferDocument;
    }
    
    /// <summary>
    /// Проверить, является ли функция СЧФ.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <returns>True - если является СЧФ, иначе - false.</returns>
    private static bool IsSchfFunction(FormalizeDocumentsParser.ISellerTitle sellerTitle)
    {
      return sellerTitle.Function == FormalizeDocumentsParser.UniversalDocumentFunction.Schf;
    }
    
    /// <summary>
    /// Проверить, является ли тип документа УПД.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <returns>True - если является УПД, иначе - false.</returns>
    private static bool IsUniversalTransferDocumentType(FormalizeDocumentsParser.ISellerTitle sellerTitle)
    {
      return sellerTitle.DocumentType == FormalizeDocumentsParser.DocumentType.UniversalTransferDocument;
    }
    
    /// <summary>
    /// Проверить, является ли тип документа УКД.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <returns>True - если является УКД, иначе - false.</returns>
    private static bool IsUniversalCorrectionDocumentType(FormalizeDocumentsParser.ISellerTitle sellerTitle)
    {
      return sellerTitle.DocumentType == FormalizeDocumentsParser.DocumentType.UniversalCorrectionTransferDocument;
    }
    
    /// <summary>
    /// Получить результат импорта с ошибкой.
    /// </summary>
    /// <param name="errorMessage">Текст ошибки.</param>
    /// <returns>Результат импорта с ошибкой.</returns>
    private static Structures.Module.IImportResult CreateErrorImportResult(string errorMessage)
    {
      var result = Structures.Module.ImportResult.Create();
      result.Error = errorMessage;
      result.IsSuccess = false;
      return result;
    }
    
    /// <summary>
    /// Проверить, заполнена ли в титуле продавца информация о подписывающем.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если уже есть информация о подписывающем.</returns>
    [Remote, Public]
    public virtual bool HasSellerSignatoryInfo(Docflow.IAccountingDocumentBase document)
    {
      if (document.IsFormalized != true)
        return false;
      
      using (var body = document.Versions.Single(v => v.Id == document.SellerTitleId).Body.Read())
      {
        using (var memory = new System.IO.MemoryStream())
        {
          body.CopyTo(memory);
          memory.Position = 0;
          return FormalizeDocumentsParser.SellerSignatoryInfo.HasSellerSignatoryInfo(memory);
        }
      }
    }
    
    /// <summary>
    /// Проверить, заполнены ли в титуле продавца ФНС Ид отправителя и получателя.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если уже есть информация о ФНС.</returns>
    [Remote, Public]
    public virtual bool HasSellerTitleInfo(Docflow.IAccountingDocumentBase document)
    {
      if (document.IsFormalized != true)
        return false;
      
      using (var body = document.Versions.Single(v => v.Id == document.SellerTitleId).Body.Read())
      {
        using (var memory = new System.IO.MemoryStream())
        {
          body.CopyTo(memory);
          return FormalizeDocumentsParser.SellerTitleInfo.HasRequiredProperties(memory.ToArray());
        }
      }
    }
    
    /// <summary>
    /// Получить сервис обмена.
    /// </summary>
    /// <param name="title">Титул продавца.</param>
    /// <returns>Сервис обмена.</returns>
    private static Enumeration? GetExchangeService(FormalizeDocumentsParser.IFormalizedDocument title)
    {
      switch (title.FromService)
      {
        case FormalizeDocumentsParser.SupportedService.Diadoc:
          return ExchangeCore.ExchangeService.ExchangeProvider.Diadoc;
        case FormalizeDocumentsParser.SupportedService.Sbis:
          return ExchangeCore.ExchangeService.ExchangeProvider.Sbis;
        default:
          Logger.DebugFormat("GetExchangeService. Unsupportable exchange service {0}", title.FromService.ToString());
          return null;
      }
    }

    /// <summary>
    /// Получить абонентский ящик нашей организации для импортированного документа.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="requireFtsId">Соотносить НОР только по ФНС ИД.</param>
    /// <returns>Абонентский ящик нашей организации.</returns>
    private static ExchangeCore.IBusinessUnitBox GetBusinessUnitBoxForImportedDocument(FormalizeDocumentsParser.ISellerTitle sellerTitle, bool requireFtsId)
    {
      var box = ExchangeCore.BusinessUnitBoxes.Null;

      if (!string.IsNullOrWhiteSpace(sellerTitle.SenderId))
      {
        var exchangeProvider = GetExchangeService(sellerTitle);
        box = ExchangeCore.BusinessUnitBoxes.GetAll()
          .Where(b => b.ExchangeService.ExchangeProvider == exchangeProvider && b.FtsId == sellerTitle.SenderId)
          .SingleOrDefault();
      }

      // Поиск по ИНН \ КПП.
      if (!requireFtsId && box == null)
        return GetBox(sellerTitle.Seller);

      return box;
    }

    /// <summary>
    /// Получить нашу организацию для импортированного документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="requireFtsId">Соотносить НОР только по ФНС ИД.</param>
    /// <returns>Наша организация.</returns>
    private static Company.IBusinessUnit GetBusinessUnitForImportedDocument(Docflow.IAccountingDocumentBase document,
                                                                            FormalizeDocumentsParser.ISellerTitle sellerTitle,
                                                                            bool requireFtsId)
    {
      if (document.BusinessUnitBox != null)
        return document.BusinessUnitBox.BusinessUnit;
      
      // Ищем по ИНН \ КПП, не связываясь с МКДО.
      return !requireFtsId ? GetBusinessUnit(sellerTitle.Seller) : Company.BusinessUnits.Null;
    }

    /// <summary>
    /// Получить контрагента для импортированного документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="requireFtsId">Соотносить контрагента только по ФНС ИД.</param>
    /// <returns>Контрагент.</returns>
    private static Parties.ICounterparty GetCounterpartyForImportedDocument(Docflow.IAccountingDocumentBase document,
                                                                            FormalizeDocumentsParser.ISellerTitle sellerTitle,
                                                                            bool requireFtsId)
    {
      var counterparty = Parties.Counterparties.Null;
      var box = document.BusinessUnitBox;

      if (box != null && !string.IsNullOrWhiteSpace(sellerTitle.ReceiverId))
      {
        var counterparties = GetCounterparties(sellerTitle.ReceiverId, box);
        if (counterparties.Count > 1)
          counterparty = GetCounterparty(counterparties, sellerTitle.Buyer, box, true);
        else
        {
          counterparty = counterparties.FirstOrDefault();
          if (counterparty == null)
            counterparty = GetCounterparty(sellerTitle.Buyer, box, true);
        }
      }

      // Поиск по ИНН \ КПП.
      if (!requireFtsId && counterparty == null)
      {
        if (box != null)
          counterparty = GetCounterparty(sellerTitle.Buyer, box, true);

        if (counterparty == null)
          counterparty = GetCounterparty(sellerTitle.Buyer, null, false);
      }

      return counterparty;
    }

    /// <summary>
    /// Получить абонентский ящик участника ЭДО.
    /// </summary>
    /// <param name="participant">Участник ЭДО.</param>
    /// <returns>Абонентский ящик.</returns>
    private static ExchangeCore.IBusinessUnitBox GetBox(FormalizeDocumentsParser.IExchangeParticipant participant)
    {
      var boxes = ExchangeCore.BusinessUnitBoxes.GetAll()
        .Where(b => b.BusinessUnit.TIN == participant.Tin)
        .ToList();
      if (boxes.Count > 1)
        boxes = boxes.Where(b => b.BusinessUnit.TRRC == participant.Trrc).ToList();
      if (boxes.Count > 1)
        boxes = boxes.Where(b => b.Status == CoreEntities.DatabookEntry.Status.Active).ToList();
      if (boxes.Any())
        return boxes.Single();
      return null;
    }
    
    /// <summary>
    /// Определить НОР по информации об участнике ЭДО.
    /// </summary>
    /// <param name="participant">Участник ЭДО.</param>
    /// <returns>Наша организация.</returns>
    private static Company.IBusinessUnit GetBusinessUnit(FormalizeDocumentsParser.IExchangeParticipant participant)
    {
      var units = Company.BusinessUnits.GetAll()
        .Where(b => b.TIN == participant.Tin)
        .ToList();
      if (units.Count > 1)
        units = units.Where(b => b.TRRC == participant.Trrc).ToList();
      if (units.Count > 1)
        units = units.Where(b => b.Status == CoreEntities.DatabookEntry.Status.Active).ToList();
      if (units.Any())
        return units.Single();
      return null;
    }
    
    /// <summary>
    /// Определить контрагента по информации об участнике ЭДО.
    /// </summary>
    /// <param name="participant">Участник ЭДО.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="canExchange">Участвует ли в электронном обмене.</param>
    /// <returns>Контрагент.</returns>
    private static Parties.ICounterparty GetCounterparty(FormalizeDocumentsParser.IExchangeParticipant participant,
                                                         ExchangeCore.IBusinessUnitBox box, bool canExchange)
    {
      return GetCounterparty(null, participant, box, canExchange);
    }
    
    /// <summary>
    /// Определить контрагента по информации об участнике ЭДО.
    /// </summary>
    /// <param name="counterparties">Список контрагентов.</param>
    /// <param name="participant">Участник ЭДО.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="canExchange">Участвует ли в электронном обмене.</param>
    /// <returns>Контрагент.</returns>
    private static Parties.ICounterparty GetCounterparty(List<Parties.ICounterparty> counterparties,
                                                         FormalizeDocumentsParser.IExchangeParticipant participant,
                                                         ExchangeCore.IBusinessUnitBox box, bool canExchange)
    {
      if (counterparties == null)
        counterparties = Parties.Counterparties.GetAll().Where(b => b.TIN == participant.Tin).ToList();
      
      if (canExchange)
        counterparties = counterparties.Where(c => c.ExchangeBoxes.Any(b => Equals(b.Box, box))).ToList();

      if (counterparties.Count > 1)
        counterparties = counterparties.Where(b => b.Status == CoreEntities.DatabookEntry.Status.Active).ToList();
      
      if (counterparties.Count > 1)
      {
        if (!string.IsNullOrWhiteSpace(participant.Trrc))
          counterparties = counterparties.OfType<Parties.ICompanyBase>().Where(b => b.TRRC == participant.Trrc).ToList<Parties.ICounterparty>();
        else
          counterparties = counterparties.Where(b => !Parties.CompanyBases.Is(b)).ToList();
      }
      
      if (counterparties.Count > 1 && counterparties.Any(b => b.CanExchange == true))
        counterparties = counterparties.Where(b => b.CanExchange == true).ToList();
      
      return counterparties.Any() ? counterparties.Single() : null;
    }
    
    /// <summary>
    /// Определить контрагента по ФНС ИД и абонентскому ящику.
    /// </summary>
    /// <param name="ftsId">ФНС ИД.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <returns>Контрагент.</returns>
    private static Parties.ICounterparty GetCounterparty(string ftsId, ExchangeCore.IBusinessUnitBox box)
    {
      return Parties.Counterparties.GetAll()
        .Where(c => c.ExchangeBoxes.Any(b => b.FtsId == ftsId && b.Box == box))
        .SingleOrDefault();
    }
    
    /// <summary>
    /// Найти все организации и филиалы контрагентов по ФНС ИД и абонентскому ящику нашей организации.
    /// </summary>
    /// <param name="ftsId">ФНС ИД.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <returns>Список контрагентов.</returns>
    private static List<Parties.ICounterparty> GetCounterparties(string ftsId, ExchangeCore.IBusinessUnitBox box)
    {
      return Parties.Counterparties.GetAll()
        .Where(c => c.ExchangeBoxes.Any(b => b.FtsId == ftsId && b.Box == box))
        .ToList();
    }
    
    /// <summary>
    /// Сгенерировать ФНС-ид (и связанные свойства) для документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <remarks>Документу будет перезаписано тело версии.</remarks>
    [Public, Remote]
    public virtual void AddOrReplaceSellerTitleInfo(Docflow.IAccountingDocumentBase document)
    {
      if (document == null || !document.HasVersions)
        return;
      
      var version = document.Versions.SingleOrDefault(v => v.Id == document.SellerTitleId);
      if (version == null)
        return;
      
      using (var memory = new System.IO.MemoryStream())
      {
        version.Body.Read().CopyTo(memory);
        var newBody = AddOrReplaceSellerTitleInfo(memory, document.BusinessUnitBox, document.Counterparty,
                                                  document.FormalizedServiceType.Value, document.IsAdjustment == true);
        version.Body.Write(newBody);
        document.Save();
      }
    }
    
    /// <summary>
    /// Сгенерировать ФНС-ид (и связанные свойства) для документа.
    /// </summary>
    /// <param name="stream">Поток с XML в исходном виде.</param>
    /// <param name="rootBox">Ящик, через который будет отправлен документ.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="formalizedServiceType">Тип документа (УПД, ДПТ, ДПРР).</param>
    /// <param name="isAdjustment">Корректировочный (важно только для УКД).</param>
    /// <returns>Сгенерированный XML новым потоком.</returns>
    private static System.IO.MemoryStream AddOrReplaceSellerTitleInfo(System.IO.MemoryStream stream, ExchangeCore.IBusinessUnitBox rootBox,
                                                                      Parties.ICounterparty counterparty, Sungero.Core.Enumeration formalizedServiceType,
                                                                      bool isAdjustment)
    {
      var counterpartyExchange = counterparty.ExchangeBoxes.SingleOrDefault(c => Equals(c.Box, rootBox));
      if (counterpartyExchange == null)
        throw AppliedCodeException.Create(string.Format("Counterparty {0} must have exchange from box {1}", counterparty.Id, rootBox.Id));
      
      var sellerTitleInfo = new FormalizeDocumentsParser.SellerTitleInfo();
      if (formalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GoodsTransfer)
        sellerTitleInfo.DocumentType = FormalizeDocumentsParser.DocumentType.GoodsTransferDocument;
      else if (formalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.WorksTransfer)
        sellerTitleInfo.DocumentType = FormalizeDocumentsParser.DocumentType.WorksTransferDocument;
      else if (formalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer)
        sellerTitleInfo.DocumentType = isAdjustment ? FormalizeDocumentsParser.DocumentType.UniversalCorrectionTransferDocument : FormalizeDocumentsParser.DocumentType.UniversalTransferDocument;

      if (rootBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
        sellerTitleInfo.Operator = FormalizeDocumentsParser.SupportedEdoOperators.Diadoc;
      else if (rootBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis)
        sellerTitleInfo.Operator = FormalizeDocumentsParser.SupportedEdoOperators.Sbis;
      else
        Logger.DebugFormat("AddOrReplaceSellerTitleInfo. Unsupportable exchange service {0}", rootBox.ExchangeService.ExchangeProvider);
      
      sellerTitleInfo.Receiver = counterpartyExchange.FtsId;
      sellerTitleInfo.Sender = rootBox.FtsId;
      return sellerTitleInfo.AddOrReplaceToXml(stream);
    }
    
    /// <summary>
    /// Сгенерировать титул продавца.
    /// </summary>
    /// <param name="document">Документ, для которого генерируется титул.</param>
    /// <param name="sellerTitle">Информация о титуле продавца.</param>
    [Public, Remote]
    public static void GenerateSellerTitle(Docflow.IAccountingDocumentBase document, Docflow.Structures.AccountingDocumentBase.ISellerTitle sellerTitle)
    {
      if (document.IsFormalized != true)
      {
        Logger.DebugFormat("GenerateSellerTitle. Can't generate seller title for nonformalized document (Id = {0}).", document.Id);
        return;
      }
      
      var sellerSignatoryInfo = Functions.Module.CreateSellerSignatoryInfo(document, sellerTitle);
      Functions.Module.AddSellerTitleToDocumentVersion(document, sellerSignatoryInfo);
      
      document.SellerTitleId = document.LastVersion.Id;
      document.OurSignatory = sellerTitle.Signatory;
      document.OurSigningReason = sellerTitle.SignatureSetting;
      document.IsFormalizedSignatoryEmpty = false;
      document.Save();
    }
    
    /// <summary>
    /// Создать информацию о подписывающем.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <returns>Информация о подписывающем.</returns>
    public virtual Sungero.FormalizeDocumentsParser.SellerSignatoryInfo CreateSellerSignatoryInfo(IAccountingDocumentBase document,
                                                                                                  Docflow.Structures.AccountingDocumentBase.ISellerTitle sellerTitle)
    {
      var sellerSignatoryInfo = new FormalizeDocumentsParser.SellerSignatoryInfo();
      sellerSignatoryInfo.CompanyName = document.BusinessUnit.LegalName;
      sellerSignatoryInfo.FirstName = sellerTitle.Signatory.Person.FirstName;
      sellerSignatoryInfo.LastName = sellerTitle.Signatory.Person.LastName;
      sellerSignatoryInfo.MiddleName = sellerTitle.Signatory.Person.MiddleName;
      sellerSignatoryInfo.JobTitle = Functions.Module.GetSellerJobTitle(sellerTitle);
      
      sellerSignatoryInfo.Powers = this.GetSellerSignatoryPowers(sellerTitle);
      
      sellerSignatoryInfo.PowersBase = sellerTitle.SignatureSetting != null
        ? Docflow.PublicFunctions.Module.GetSigningReason(sellerTitle.SignatureSetting)
        : Docflow.SignatureSettings.Info.Properties.Reason.GetLocalizedValue(Docflow.SignatureSetting.Reason.Duties);

      sellerSignatoryInfo.TIN = document.BusinessUnit.TIN;

      var taxDocumentClassifier = Exchange.Structures.Module.TaxDocumentClassifier.Create();
      taxDocumentClassifier.TaxDocumentClassifierCode = sellerTitle.TaxDocumentClassifierCode;
      taxDocumentClassifier.TaxDocumentClassifierFormatVersion = sellerTitle.TaxDocumentClassifierFormatVersion;
      
      if (Sungero.Exchange.PublicFunctions.Module.IsUniversalTransferDocumentSeller970(taxDocumentClassifier))
      {
        sellerSignatoryInfo.SignerAdditionalInfo = sellerTitle.SignerAdditionalInfo;
        sellerSignatoryInfo.SignerPowersConfirmationMethod = this.GetSignerPowersConfirmationMethodForSellerTitle(sellerTitle.SignatureSetting);
        
        if (sellerSignatoryInfo.SignerPowersConfirmationMethod == FormalizeDocumentsParser.SignerPowersConfirmationMethod.FormalizedPoA)
          this.FillSellerTitleFormalizedPoAInfo(sellerTitle, sellerSignatoryInfo);

        if (sellerSignatoryInfo.SignerPowersConfirmationMethod == FormalizeDocumentsParser.SignerPowersConfirmationMethod.PaperPoA)
          this.FillSellerTitlePowerOfAttorneyInfo(sellerTitle, sellerSignatoryInfo);
      }
      
      return sellerSignatoryInfo;
    }
    
    /// <summary>
    /// Получить область полномочий подписанта.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <returns>Область полномочий подписанта.</returns>
    protected string GetSellerSignatoryPowers(Docflow.Structures.AccountingDocumentBase.ISellerTitle sellerTitle)
    {
      // Лицо, совершившее сделку и ответственное за ее оформление.
      if (sellerTitle.SignatoryPowers == Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_DealAndRegister)
        return Constants.Module.SellerTitlePowers.MadeAndSignOperation;
      // Лицо, ответственное за оформление свершившегося события.
      if (sellerTitle.SignatoryPowers == Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Register)
        return Constants.Module.SellerTitlePowers.PersonDocumentedOperation;
      // Лицо, совершившее сделку и операцию, ответственное за ее оформление и за подписание счетов-фактур.
      if (sellerTitle.SignatoryPowers == Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_DealAndRegisterAndInvoiceSignatory)
        return Constants.Module.SellerTitlePowers.MadeAndResponsibleForOperationAndSignedInvoice;
      // Лицо, ответственное за оформление свершившегося события и за подписание счетов-фактур.
      if (sellerTitle.SignatoryPowers == Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_RegisterAndInvoiceSignatory)
        return Constants.Module.SellerTitlePowers.ResponsibleForOperationAndSignatoryForInvoice;
      
      return null;
    }
    
    /// <summary>
    /// Получить способ подтверждения полномочий для титула продавца.
    /// </summary>
    /// <param name="signatureSetting">Право подписи.</param>
    /// <returns>Способ подтверждения полномочий.</returns>
    public virtual FormalizeDocumentsParser.SignerPowersConfirmationMethod GetSignerPowersConfirmationMethodForSellerTitle(ISignatureSetting signatureSetting)
    {
      var reason = signatureSetting?.Reason;
      
      if (reason == Docflow.SignatureSetting.Reason.Duties)
        return FormalizeDocumentsParser.SignerPowersConfirmationMethod.Duties;
      
      if (reason == Docflow.SignatureSetting.Reason.FormalizedPoA)
        return FormalizeDocumentsParser.SignerPowersConfirmationMethod.FormalizedPoA;

      if (reason == Docflow.SignatureSetting.Reason.PowerOfAttorney)
        return FormalizeDocumentsParser.SignerPowersConfirmationMethod.PaperPoA;
      
      if (reason == Docflow.SignatureSetting.Reason.Other)
        return FormalizeDocumentsParser.SignerPowersConfirmationMethod.Other;
      
      return FormalizeDocumentsParser.SignerPowersConfirmationMethod.Duties;
    }
    
    /// <summary>
    /// Заполнить сведения об МЧД в титул продавца.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="sellerSignatoryInfo">Структура с данными для генерации титула продавца.</param>
    public virtual void FillSellerTitleFormalizedPoAInfo(Docflow.Structures.AccountingDocumentBase.ISellerTitle sellerTitle,
                                                         FormalizeDocumentsParser.SellerSignatoryInfo sellerSignatoryInfo)
    {
      var formalizedPoA = FormalizedPowerOfAttorneys.As(sellerTitle.SignatureSetting.Document);
      sellerSignatoryInfo.PowerOfAttorney.UnifiedRegNumber = formalizedPoA.UnifiedRegistrationNumber;
      if (formalizedPoA.ValidFrom.HasValue)
        sellerSignatoryInfo.PowerOfAttorney.ValidFrom = formalizedPoA.ValidFrom.Value;
      
      sellerSignatoryInfo.PowerOfAttorney.SystemId = Docflow.PublicFunctions.FormalizedPowerOfAttorney.GetStorageSystemId(formalizedPoA);
      sellerSignatoryInfo.PowerOfAttorney.SystemUrl = Docflow.PublicFunctions.FormalizedPowerOfAttorney.GetSystemUrlFromBodyXml(formalizedPoA, formalizedPoA.LastVersion);
    }
    
    /// <summary>
    /// Заполнить сведения о доверенности в титул продавца.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="sellerSignatoryInfo">Структура с данными для генерации титула продавца.</param>
    public virtual void FillSellerTitlePowerOfAttorneyInfo(Docflow.Structures.AccountingDocumentBase.ISellerTitle sellerTitle, FormalizeDocumentsParser.SellerSignatoryInfo sellerSignatoryInfo)
    {
      var powerOfAttorney = PowerOfAttorneyBases.As(sellerTitle.SignatureSetting.Document);
      sellerSignatoryInfo.PowerOfAttorney.InternalNumber = powerOfAttorney.RegistrationNumber;
      
      var validFrom = powerOfAttorney.ValidFrom ?? powerOfAttorney.RegistrationDate;
      if (validFrom.HasValue)
        sellerSignatoryInfo.PowerOfAttorney.ValidFrom = validFrom.Value;
    }
    
    /// <summary>
    /// Добавить титул продавца в версию документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="sellerSignatoryInfo">Информация о подписывающем.</param>
    /// <remarks>Если документ имеет подписанный титул продавца, то новый титул запишется в новую версию.</remarks>
    public static void AddSellerTitleToDocumentVersion(IAccountingDocumentBase document, Sungero.FormalizeDocumentsParser.SellerSignatoryInfo sellerSignatoryInfo)
    {
      using (var body = document.Versions.Single(v => v.Id == document.SellerTitleId).Body.Read())
      {
        using (var memory = new System.IO.MemoryStream())
        {
          body.CopyTo(memory);
          memory.Position = 0;
          using (var patchedXml = sellerSignatoryInfo.AddOrReplaceToXml(memory))
          {
            if (!HasUnsignedSellerTitle(document))
              Docflow.PublicFunctions.OfficialDocument.CreateVersionWithRestoringExchangeState(document);
            
            var version = document.LastVersion;
            version.Body.Write(patchedXml);
          }
        }
      }
    }
    
    /// <summary>
    /// Получить наименование должности для титула продавца.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <returns>Наименование должности.</returns>
    public virtual string GetSellerJobTitle(Docflow.Structures.AccountingDocumentBase.ISellerTitle sellerTitle)
    {
      if (sellerTitle == null)
        return null;
      
      var settingJobTitle = sellerTitle.SignatureSetting != null && sellerTitle.SignatureSetting.JobTitle != null ? sellerTitle.SignatureSetting.JobTitle.Name : null;
      var signatoryJobTitle = sellerTitle.Signatory.JobTitle != null ? sellerTitle.Signatory.JobTitle.Name : null;
      return Docflow.PublicFunctions.Module.CutText(settingJobTitle != null ? settingJobTitle : signatoryJobTitle,
                                                    Docflow.PublicConstants.AccountingDocumentBase.JobTitleMaxLength);
    }
    
    /// <summary>
    /// Определить, есть ли у документа неподписанный титул продавца.
    /// </summary>
    /// <param name="statement">Документ.</param>
    /// <returns>True, если есть неподписанный титул продавца, иначе - false.</returns>
    [Public, Remote]
    public static bool HasUnsignedSellerTitle(Docflow.IAccountingDocumentBase statement)
    {
      if (statement.SellerTitleId != null)
      {
        var existingSellerTitle = statement.Versions.Where(x => x.Id == statement.SellerTitleId).FirstOrDefault();
        if (existingSellerTitle != null && !Signatures.Get(existingSellerTitle).Any())
          return true;
      }
      
      return false;
    }
    
    #endregion
    
    #region Выгрузка документа из финархива

    /// <summary>
    /// Получить ИД подписи отправителя.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="version">Версия документа.</param>
    /// <returns>ИД подписи отправителя.</returns>
    [Public]
    public virtual long? GetSenderSignatureId(IOfficialDocument document, Sungero.Content.IElectronicDocumentVersions version)
    {
      var info = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetExDocumentInfoFromVersion(document, version.Id);
      var senderSignature = Signatures.Get(version).Where(x => x.Id == info.SenderSignId).SingleOrDefault();
      if (senderSignature != null)
        return senderSignature.Id;
      else
        return null;
    }

    /// <summary>
    /// Получить ИД подписи получателя.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="version">Версия документа.</param>
    /// <returns>ИД подписи получателя.</returns>
    [Public]
    public virtual long? GetReceiverSignatureId(IOfficialDocument document, Sungero.Content.IElectronicDocumentVersions version)
    {
      var info = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetExDocumentInfoFromVersion(document, version.Id);
      var senderSignature = Signatures.Get(version).Where(x => x.Id == info.ReceiverSignId).SingleOrDefault();
      if (senderSignature != null)
        return senderSignature.Id;
      else
        return null;
    }
    
    #endregion
    
    #region Фильтрация
    
    #region Договоры и доп. соглашения
    
    /// <summary>
    /// Отфильтровать договоры и доп. соглашения по оптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Договоры и доп. соглашения для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договоры и доп. соглашения.</returns>
    /// <remarks>Условия, которые используют индексы и максимально (на порядки) сужают выборку.</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> FinContractListApplyStrongFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.FinancialArchive.FolderFilterState.IFinContractListFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "Категория".
      if (filter.Category != null)
        query = query.Where(c => Equals(c.DocumentGroup, filter.Category));
      
      // Фильтр "Контрагент".
      if (filter.Contractor != null)
        query = query.Where(c => Equals(c.Counterparty, filter.Contractor));
      
      // Фильтр "Подразделение".
      if (filter.Department != null)
        query = query.Where(c => Equals(c.Department, filter.Department));
      
      // Фильтр "Период".
      if (filter.CurrentMonth || filter.PreviousMonth)
        query = this.FinContractListApplyFilterByDate(query, filter);
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договоры и доп. соглашения по обычным условиям фильтрации.
    /// </summary>
    /// <param name="query"> Договоры и доп. соглашения для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Договоры и доп. соглашения.</returns>
    /// <remarks>Условия, которые используют индексы, но не максимально оптимально.</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> FinContractListApplyOrdinaryFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.FinancialArchive.FolderFilterState.IFinContractListFilterState filter)
    {
      if (filter == null || query == null)
        return query;
      
      // Фильтр "Статус" (безусловный фильтр).
      query = query.Where(x => x.LifeCycleState == Contracts.ContractBase.LifeCycleState.Terminated ||
                          x.LifeCycleState == Contracts.ContractBase.LifeCycleState.Active ||
                          x.LifeCycleState == Contracts.ContractBase.LifeCycleState.Closed);
      
      // Фильтр "Тип документа".
      if (filter.Contracts || filter.SupAgreements)
        query = query.Where(d => (Sungero.Contracts.ContractBases.Is(d) && filter.Contracts) ||
                            Sungero.Contracts.SupAgreements.Is(d) && filter.SupAgreements);
      
      // Фильтр "Вид документа".
      if (filter.DocumentKind != null)
        query = query.Where(c => Equals(c.DocumentKind, filter.DocumentKind));
      
      // Фильтр "Наша организация".
      if (filter.BusinessUnit != null)
        query = query.Where(c => Equals(c.BusinessUnit, filter.BusinessUnit));
      
      // Фильтр "Период".
      if (filter.CurrentQuarter || filter.PreviousQuarter || filter.ManualPeriod)
        query = this.FinContractListApplyFilterByDate(query, filter);
      
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договоры и доп. соглашения по неоптимальным условиям фильтрации.
    /// </summary>
    /// <param name="query">Договоры и доп. соглашения для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договоры и доп. соглашения.</returns>
    /// <remarks>Условия, которые могут выполняться долго (например те, которые не могут использовать индексы).</remarks>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> FinContractListApplyWeakFilter(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.FinancialArchive.FolderFilterState.IFinContractListFilterState filter)
    {
      return query;
    }
    
    /// <summary>
    /// Отфильтровать договоры и доп. соглашения по установленной дате.
    /// </summary>
    /// <param name="query">Договоры и доп. соглашения для фильтрации.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Отфильтрованные договоры и доп. соглашения.</returns>
    public virtual IQueryable<Sungero.Contracts.IContractualDocument> FinContractListApplyFilterByDate(IQueryable<Sungero.Contracts.IContractualDocument> query, Sungero.FinancialArchive.FolderFilterState.IFinContractListFilterState filter)
    {
      var today = Calendar.UserToday;
      var beginDate = today.BeginningOfMonth();
      var endDate = today.EndOfMonth();

      if (filter.PreviousMonth)
      {
        beginDate = today.AddMonths(-1).BeginningOfMonth();
        endDate = today.AddMonths(-1).EndOfMonth();
      }
      
      if (filter.CurrentQuarter)
      {
        beginDate = Docflow.PublicFunctions.AccountingDocumentBase.BeginningOfQuarter(today);
        endDate = Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(today);
      }
      
      if (filter.PreviousQuarter)
      {
        beginDate = Docflow.PublicFunctions.AccountingDocumentBase.BeginningOfQuarter(today.AddMonths(-3));
        endDate = Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(today.AddMonths(-3));
      }

      if (filter.ManualPeriod)
      {
        beginDate = filter.DateRangeFrom ?? Calendar.SqlMinValue;
        endDate = filter.DateRangeTo ?? Calendar.SqlMaxValue;
      }

      query = Sungero.Docflow.PublicFunctions.Module.OfficialDocumentsApplyFilterByDate(query, beginDate, endDate)
        .Cast<Sungero.Contracts.IContractualDocument>();
      
      return query;
    }
    
    /// <summary>
    /// Определить, нужно ли использовать предварительную фильтрацию для договоров и доп. соглашений.
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>True, если нужно использовать предварительную фильтрацию.</returns>
    public virtual bool UsePrefilterFinContractList(Sungero.FinancialArchive.FolderFilterState.IFinContractListFilterState filter)
    {
      var hasStrongFilter = filter != null &&
        (filter.Category != null ||
         filter.Contractor != null ||
         filter.Department != null ||
         filter.CurrentMonth ||
         filter.PreviousMonth);

      return hasStrongFilter;
    }
    
    #endregion
    
    #endregion
  }
}