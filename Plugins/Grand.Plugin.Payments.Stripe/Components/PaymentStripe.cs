using Grand.Plugin.Payments.Stripe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace Grand.Plugin.Payments.Stripe.Components
{
    [ViewComponent(Name = "PaymentStripe")]
    public class PaymentStripeViewComponent : ViewComponent
    {
        private readonly StripePaymentSettings _stripePaymentSettings;
        public PaymentStripeViewComponent(StripePaymentSettings iDealPaymentSettings)
        {
            this._stripePaymentSettings = iDealPaymentSettings;
        }

        public IViewComponentResult Invoke()
        {
            var model = new PaymentInfoModel();
            model.Banks = ListOfBanks();

            var form = Request.Form;
            model.CardholderName = form["CardholderName"];
            model.CardNumber = form["CardNumber"];
            model.CardCode = form["CardCode"];

            return View("~/Plugins/Payments.Stripe/Views/PaymentStripe/PaymentInfo.cshtml", model);
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
                new Banks { Text = "Van Lanschot" , Value = "van_lanschot" },
                new Banks { Text = "Moneyou" , Value = "moneyou" }
            };
        }
    }
}
