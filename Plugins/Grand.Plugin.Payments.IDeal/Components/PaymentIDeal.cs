using Grand.Plugin.Payments.IDeal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace Grand.Plugin.Payments.IDeal.Components
{
    [ViewComponent(Name = "PaymentIDeal")]
    public class PaymentIDealViewComponent : ViewComponent
    {
        private readonly IDealPaymentSettings _iDealPaymentSettings;
        public PaymentIDealViewComponent(IDealPaymentSettings iDealPaymentSettings)
        {
            this._iDealPaymentSettings = iDealPaymentSettings;
        }

        public IViewComponentResult Invoke()
        {
            var model = new PaymentInfoModel();
            model.PublishableKey = _iDealPaymentSettings.PublishableKey;
            model.Banks = ListOfBanks();

            return View("~/Plugins/Payments.IDeal/Views/PaymentIDeal/PaymentInfo.cshtml", model);
        }
        private IList<Banks> ListOfBanks()
        {
            return new List<Banks>() {
                new Banks { Text = "ABN AMRO" , Value = "abn_amro" },
                new Banks { Text = "ASN Bank" , Value = "asn_bank" },
                new Banks { Text = "ING" , Value = "ing" },
                new Banks { Text = "Knab" , Value = "knab" },
                new Banks { Text = "Rabobank" , Value = "rabobank" },
                new Banks { Text = "RegioBank" , Value = "regiobank" },
                new Banks { Text = "SNS Bank (De Volksbank)" , Value = "sns_bank" },
                new Banks { Text = "Triodos Bank" , Value = "triodos_bank" },
                new Banks { Text = "Van Lanschot" , Value = "van_lanschot" }
            };
        }
    }
}
