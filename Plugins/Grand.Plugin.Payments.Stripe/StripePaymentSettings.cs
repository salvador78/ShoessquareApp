using Grand.Core.Configuration;

namespace Grand.Plugin.Payments.Stripe
{
    public class StripePaymentSettings : ISettings
    {
        public string PublishableKey { get; set; }
        public string SecretKey { get; set; }

    }
}
                                                                                                                                                                                                                                                                        