using Grand.Framework.Mvc.ModelBinding;
using Grand.Framework.Mvc.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Grand.Plugin.Payments.IDeal.Models
{
    public class ConfigurationModel : BaseGrandModel
    {
        [GrandResourceDisplayName("Plugins.Payment.IDeal.PublishableKey")]
        public string PublishableKey { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.IDeal.SecretKey")]
        public string SecretKey { get; set; }

    }
}
