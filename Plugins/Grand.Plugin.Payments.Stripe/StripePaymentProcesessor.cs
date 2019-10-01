using Grand.Core;
using Grand.Core.Domain.Orders;
using Grand.Core.Domain.Payments;
using Grand.Core.Plugins;
using Grand.Plugin.Payments.Stripe.Controllers;
using Grand.Plugin.Payments.Stripe.Models;
using Grand.Plugin.Payments.Stripe.Validator;
using Grand.Services.Configuration;
using Grand.Services.Customers;
using Grand.Services.Localization;
using Grand.Services.Payments;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Stripe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Grand.Plugin.Payments.Stripe
{
    /// <summary>
    /// PayInStore payment processor
    /// </summary>
    public class StripePaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISettingService _settingService;
        private readonly StripePaymentSettings _stripePaymentSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        #endregion

        #region Ctor
        public StripePaymentProcessor(ILocalizationService localizationService, IWebHelper webHelper, IServiceProvider serviceProvider,
            ISettingService settingService, StripePaymentSettings stripePaymentSettings, IHttpContextAccessor httpContextAccessor)
        {
            _localizationService = localizationService;
            _webHelper = webHelper;
            _serviceProvider = serviceProvider;
            _settingService = settingService;
            _stripePaymentSettings = stripePaymentSettings;
            _httpContextAccessor = httpContextAccessor;

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
            return typeof(PaymentStripeController);
        }

        public async Task<ProcessPaymentRequest> GetPaymentInfo(IFormCollection form)
        {
            var result = new ProcessPaymentRequest();
            var selectedPaymentMethod = form["paymentRadio"];

            if (!string.IsNullOrWhiteSpace(selectedPaymentMethod))
            {
                switch (selectedPaymentMethod)
                {
                    case "1":
                        result.CustomValues.Add(Enum.GetName(typeof(PaymentMethod), PaymentMethod.iDeal), form["bank"].ToString());
                        break;
                    case "2":
                        result.CustomValues.Add(Enum.GetName(typeof(PaymentMethod), PaymentMethod.creditcard), "2");
                        result.CreditCardName = form["CardholderName"].ToString();
                        result.CreditCardNumber = form["CardNumber"].ToString();
                        result.CreditCardCvv2 = form["CardCode"].ToString();
                        var date = form["CardCode"].ToString();
                        if (!string.IsNullOrWhiteSpace(date))
                        {
                            var dateParts = date.Split('/');
                            result.CreditCardExpireMonth = int.Parse(dateParts[0]);
                            result.CreditCardExpireYear = int.Parse(dateParts[1]);
                        }
                        break;
                }
            }
            return await Task.FromResult(result);
        }

        public void GetPublicViewComponent(out string viewComponentName)
        {
            viewComponentName = "PaymentStripe";
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
            await this.AddOrUpdatePluginLocaleResource(_serviceProvider, "Plugins.Payment.Stripe.DescriptionText", "Select your Bank", "nl-NL");
            await this.AddOrUpdatePluginLocaleResource(_serviceProvider, "Plugins.Payment.Stripe.PublishableKey", "Publishable key", "nl-NL");
            await this.AddOrUpdatePluginLocaleResource(_serviceProvider, "Plugins.Payment.Stripe.SecretKey", "Secret Key", "nl-NL");

            await base.Install();
        }

        public async Task<string> PaymentMethodDescription()
        {
            //return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
            //for example, for a redirection payment method, description may be like this: "You will be redirected to PayPal site to complete the payment"
            return await Task.FromResult(_localizationService.GetResource("Plugins.Payments.Stripe.PaymentMethodDescription"));
        }

        public async Task PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var bank = StringToXMLObject<DictionarySerializer>(postProcessPaymentRequest.Order.CustomValuesXml);
            if (bank.Item.Key == Enum.GetName(typeof(PaymentMethod), PaymentMethod.iDeal))
            {
                await PostIDealProcessPayment(postProcessPaymentRequest, bank.Item.Value);
            }
            else
            {
                await PostCreditCardProcessPayment(postProcessPaymentRequest);
            }
        }


        public async Task<ProcessPaymentResult> ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
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
            await this.DeletePluginLocaleResource(_serviceProvider, "Plugins.Payment.Stripe.DescriptionText");
            await this.DeletePluginLocaleResource(_serviceProvider, "Plugins.Payment.Stripe.PublishableKey");
            await this.DeletePluginLocaleResource(_serviceProvider, "Plugins.Payment.Stripe.SecreteKey");

            await base.Uninstall();
        }

        public async Task<IList<string>> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();

            var creditCard = form["paymentRadio"];
            if (creditCard == "2")
            {
                //validate
                var validator = new PaymentInfoCreditCardValidator(_localizationService);
                var model = new PaymentInfoModel {
                    CardholderName = form["CardholderName"],
                    CardNumber = form["CardNumber"],
                    CardCode = form["CardCode"],
                    ExpireDate = form["ExpireDate"],
                };
                var validationResult = validator.Validate(model);
                if (!validationResult.IsValid)
                {
                    foreach (var error in validationResult.Errors)
                    {
                        warnings.Add(error.ErrorMessage);
                    }
                }

            }
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
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentStripe/Configure";
        }

        #endregion

        #region Helpers
        public static T StringToXMLObject<T>(string str)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                T res = (T)serializer.Deserialize(new StringReader(str));
                return res;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        enum PaymentMethod
        {
            iDeal = 1,
            creditcard
        }
        #endregion

        #region Private methods
        private async Task PostIDealProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest, string bank)
        {
            var secretKey = _stripePaymentSettings.SecretKey;
            var json = new StreamReader(_httpContextAccessor.HttpContext.Request.Body).ReadToEnd();

            var clientSecret = _httpContextAccessor.HttpContext.Request.Query["client_secret"].ToString();
            string source;

            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                if (!string.IsNullOrWhiteSpace(secretKey))
                {
                    //get store location
                    var storeLocation = _webHelper.GetStoreLocation();

                    StripeConfiguration.ApiKey = secretKey;
                    if (!string.IsNullOrWhiteSpace(postProcessPaymentRequest.Order.CustomValuesXml))
                    {
                        var options = new SourceCreateOptions() {
                            Amount = (long)postProcessPaymentRequest.Order.OrderTotal,
                            Currency = postProcessPaymentRequest.Order.CustomerCurrencyCode.ToLower(),
                            Type = SourceType.Ideal,
                            Redirect = new SourceRedirectOptions {
                                ReturnUrl = $"{storeLocation}checkout/OpcCompleteRedirectionPayment",
                            },
                            Ideal = new SourceIdealCreateOptions {
                                Bank = bank
                            }
                        };
                        var service = new SourceService();
                        Source charge = await service.CreateAsync(options);

                        if (charge.StripeResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var jObject = JObject.Parse(charge.StripeResponse.Content);
                            string value = (string)jObject.SelectToken("redirect.url");
                            source = (string)jObject.SelectToken("id");
                            _httpContextAccessor.HttpContext.Session.SetString("source", source);
                            _httpContextAccessor.HttpContext.Response.Redirect(value);
                            return;
                        }
                    }
                }
                else
                {
                    await Task.FromException(new Exception("secret key canot be null or empty"));
                }
            }
            else
            {
                source = _httpContextAccessor.HttpContext.Session.GetString("source");
                var options = new ChargeCreateOptions {
                    Amount = (long)postProcessPaymentRequest.Order.OrderTotal,
                    Currency = postProcessPaymentRequest.Order.CustomerCurrencyCode.ToLower(),
                    Source = source,
                };

                var service = new ChargeService();
                Charge charge = await service.CreateAsync(options);
                if (charge.StripeResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    postProcessPaymentRequest.Order.PaymentStatus = PaymentStatus.Paid;
                }
                _httpContextAccessor.HttpContext.Session.SetString("source", null);
            }
        }
        private async Task PostCreditCardProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {

        }
        #endregion
    }
}
