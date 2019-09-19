using Grand.Core;
using Grand.Core.Domain.Orders;
using Grand.Core.Domain.Payments;
using Grand.Core.Plugins;
using Grand.Plugin.Payments.IDeal.Controllers;
using Grand.Plugin.Payments.IDeal.Models;
using Grand.Services.Configuration;
using Grand.Services.Localization;
using Grand.Services.Orders;
using Grand.Services.Payments;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Stripe;

namespace Grand.Plugin.Payments.IDeal
{
    /// <summary>
    /// PayInStore payment processor
    /// </summary>
    public class IDealPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISettingService _settingService;
        #endregion

        #region Ctor
        public IDealPaymentProcessor(ILocalizationService localizationService, IWebHelper webHelper, IServiceProvider serviceProvider, ISettingService settingService)
        {
            this._localizationService = localizationService;
            this._webHelper = webHelper;
            this._serviceProvider = serviceProvider;
            this._settingService = settingService;
        }

        #endregion

        #region Methods
        public RecurringPaymentType RecurringPaymentType {
            get { return RecurringPaymentType.NotSupported; }
        }

        public PaymentMethodType PaymentMethodType {
            get { return PaymentMethodType.Redirection; }
        }

        public Task<CancelRecurringPaymentResult> CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CanRePostProcessPayment(Core.Domain.Orders.Order order)
        {
            throw new NotImplementedException();
        }

        public Task<CapturePaymentResult> Capture(CapturePaymentRequest capturePaymentRequest)
        {
            throw new NotImplementedException();
        }

        public async Task<decimal> GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var result = await Task.FromResult<decimal>(2);
            return result;
        }

        public Type GetControllerType()
        {
            return typeof(PaymentIDealController);
        }

        public async Task<ProcessPaymentRequest> GetPaymentInfo(IFormCollection form)
        {
           
            return await Task.FromResult(new ProcessPaymentRequest());
        }

        public void GetPublicViewComponent(out string viewComponentName)
        {
            viewComponentName = "PaymentIDeal";
        }

        public async Task<bool> HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return await Task.FromResult(false);
        }

        public override async Task Install()
        {
            //locales
            await this.AddOrUpdatePluginLocaleResource(_serviceProvider, "Plugins.Payment.IDeal.DescriptionText", "Select your Bank");
            await this.AddOrUpdatePluginLocaleResource(_serviceProvider, "Plugins.Payment.IDeal.PublishableKey", "Publishable key");
            await this.AddOrUpdatePluginLocaleResource(_serviceProvider, "Plugins.Payment.IDeal.SecretKey", "Secret Key");

            await base.Install();
        }

        public async Task<string> PaymentMethodDescription()
        {
            //return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
            //for example, for a redirection payment method, description may be like this: "You will be redirected to PayPal site to complete the payment"
            return await Task.FromResult(_localizationService.GetResource("Plugins.Payments.IDeal.PaymentMethodDescription"));
        }

        public async Task PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var secretKey = _settingService.GetSettingByKey("Plugins.Payment.IDeal.SecretKey", "SecretKey");
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                StripeConfiguration.ApiKey = secretKey;

                var options = new ChargeCreateOptions() {
                    Amount = 2000,
                    Currency = "usd",
                    Source = "tok_visa",

                    Metadata = new Dictionary<String, String>()
                    {
                        { "OrderId", "6735"}
                    }           
                };

                var service = new ChargeService();
                Charge charge = await service.CreateAsync(options);
            }


        }


        public async Task<ProcessPaymentResult> ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;

            return await Task.FromResult(result);
        }

        public Task<ProcessPaymentResult> ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            throw new NotImplementedException();
        }

        public Task<RefundPaymentResult> Refund(RefundPaymentRequest refundPaymentRequest)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> SkipPaymentInfo()
        {
            return await Task.FromResult(false);
        }

        public async Task<bool> SupportCapture()
        {
            return await Task.FromResult(false);
        }

        public async Task<bool> SupportPartiallyRefund()
        {
            return await Task.FromResult(false);
        }

        public async Task<bool> SupportRefund()
        {
            return await Task.FromResult(false);
        }

        public async Task<bool> SupportVoid()
        {
            return await Task.FromResult(false);
        }

        public override async Task Uninstall()
        {
            //locales
            await this.DeletePluginLocaleResource(_serviceProvider, "Plugins.Payment.IDeal.DescriptionText");
            await this.DeletePluginLocaleResource(_serviceProvider, "Plugins.Payment.IDeal.PublishableKey");
            await this.DeletePluginLocaleResource(_serviceProvider, "Plugins.Payment.IDeal.SecreteKey");

            await base.Uninstall();
        }

        public async Task<IList<string>> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();
            var bank = form["Banks"];

            //validate

            return await Task.FromResult(warnings);
        }

        public Task<VoidPaymentResult> Void(VoidPaymentRequest voidPaymentRequest)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentIDeal/Configure";
        }

        #endregion
    }
}
