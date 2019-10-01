using Grand.Framework.Mvc.ModelBinding;
using Grand.Framework.Mvc.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Grand.Plugin.Payments.Stripe.Models
{
    public class ConfigurationModel : BaseGrandModel
    {
        [GrandResourceDisplayName("Plugins.Payment.Stripe.PublishableKey")]
        public string PublishableKey { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.Stripe.SecretKey")]
        public string SecretKey { get; set; }

    }
}
