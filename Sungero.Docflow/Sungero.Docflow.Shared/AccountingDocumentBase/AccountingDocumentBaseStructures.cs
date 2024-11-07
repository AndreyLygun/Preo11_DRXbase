using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.AccountingDocumentBase
{
  [PublicAttribute]
  partial class BuyerTitle
  {
    public DateTime? AcceptanceDate { get; set; }
    
    /// <summary>
    /// Результат приемки.
    /// </summary>
    public Sungero.Core.Enumeration BuyerAcceptanceStatus { get; set; }
    
    /// <summary>
    /// Акт разногласий.
    /// </summary>
    public string ActOfDisagreement { get; set; }

    /// <summary>
    /// Подписывающий.
    /// </summary>
    public Company.IEmployee Signatory { get; set; }

    /// <summary>
    /// Основание полномочий подписывающего.
    /// </summary>
    public string SignatoryPowersBase { get; set; }

    /// <summary>
    /// Сотрудник, принявший груз.
    /// </summary>
    public Company.IEmployee Consignee { get; set; }

    /// <summary>
    /// Основание полномочий принявшего груз.
    /// </summary>
    public string ConsigneePowersBase { get; set; }
    
    /// <summary>
    /// Область полномочий (оформление, подписание, оформление и подписание).
    /// </summary>
    public string SignatoryPowers { get; set; }
    
    /// <summary>
    /// Доверенность.
    /// </summary>
    public IPowerOfAttorneyBase ConsigneePowerOfAttorney { get; set; }
    
    /// <summary>
    /// Другой документ.
    /// </summary>
    public string ConsigneeOtherReason { get; set; }
    
    /// <summary>
    /// Право подписи.
    /// </summary>
    public ISignatureSetting SignatureSetting { get; set; }
    
    /// <summary>
    /// Доп. сведения о подписывающем.
    /// </summary>
    public string SignerAdditionalInfo { get; set; }
    
    /// <summary>
    /// Классификатор налогового документа.
    /// </summary>
    public string TaxDocumentClassifierCode { get; set; }
    
    /// <summary>
    /// Версия формата.
    /// </summary>
    public string TaxDocumentClassifierFormatVersion { get; set; }
  }
  
  [PublicAttribute]
  partial class SellerTitle
  {
    /// <summary>
    /// Подписывающий.
    /// </summary>
    public Company.IEmployee Signatory { get; set; }

    /// <summary>
    /// Основание полномочий подписывающего.
    /// </summary>
    public string SignatoryPowersBase { get; set; }
    
    /// <summary>
    /// Область полномочий (оформление, подписание, оформление и подписание).
    /// </summary>
    public string SignatoryPowers { get; set; }
    
    /// <summary>
    /// Право подписи.
    /// </summary>
    public ISignatureSetting SignatureSetting { get; set; }
    
    /// <summary>
    /// Доп. сведения о подписывающем.
    /// </summary>
    public string SignerAdditionalInfo { get; set; }
    
    /// <summary>
    /// Классификатор налогового документа.
    /// </summary>
    public string TaxDocumentClassifierCode { get; set; }
    
    /// <summary>
    /// Версия формата.
    /// </summary>
    public string TaxDocumentClassifierFormatVersion { get; set; }
  }
  
  partial class GenerateTitleError
  {
    public string Type { get; set; }
    
    public string Text { get; set; }
  }
  
  [PublicAttribute]
  partial class TitleGenerationDialogProperties
  {
    /// <summary>
    /// Право подписи подписавшего.
    /// </summary>
    public ISignatureSetting SignatorySetting { get; set; }
    
    /// <summary>
    /// Подписал.
    /// </summary>
    public Company.IEmployee Signatory { get; set; }
    
    /// <summary>
    /// Грузополучатель.
    /// </summary>
    public Company.IEmployee Consignee { get; set; }
    
    /// <summary>
    /// Доверенность грузополучателя.
    /// </summary>
    public IPowerOfAttorneyBase ConsigneePowerOfAttorney { get; set; }
    
    /// <summary>
    /// Документ грузополучателя.
    /// </summary>
    public string ConsigneeOtherReason { get; set; }
    
    /// <summary>
    /// Дополнительные сведения о подписавшем.
    /// </summary>
    public string SignerAdditionalInfo { get; set; }
    
    /// <summary>
    /// Необходимость валидации основания грузополучателя.
    /// </summary>
    public bool NeedValidatePowersBaseConsignee { get; set; }
  }
}