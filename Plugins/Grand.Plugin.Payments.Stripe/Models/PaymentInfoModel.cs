using Grand.Framework.Mvc.ModelBinding;
using Grand.Framework.Mvc.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace Grand.Plugin.Payments.Stripe.Models
{
    public class PaymentInfoModel : BaseGrandModel
    {
        public PaymentInfoModel()
        {
            Banks = new List<Banks>();
        }
        public string PublishableKey { get; set; }

        [GrandResourceDisplayName("Payment.Stripe.DescriptionText")]
        public string DescriptionText { get; set; }
        public IList<Banks> Banks { get; set; }

        public string CardholderName { get; set; }

        public string CardNumber { get; set; }

        public string ExpireDate { get; set; }

        public string CardCode { get; set; }
    }
    public class Banks
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public bool Selected { get; set; }
    }
}
