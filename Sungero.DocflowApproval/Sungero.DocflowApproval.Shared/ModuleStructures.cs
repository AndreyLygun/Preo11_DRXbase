using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.DocflowApproval.Structures.Module
{
  partial class ExchangeServices
  {
    public List<ExchangeCore.IExchangeService> Services { get; set; }
    
    public ExchangeCore.IExchangeService DefaultService { get; set; }
       
  }

}