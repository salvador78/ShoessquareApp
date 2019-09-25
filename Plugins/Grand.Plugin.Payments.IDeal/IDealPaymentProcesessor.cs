using Grand.Core;
using Grand.Core.Domain.Orders;
using Grand.Core.Domain.Payments;
using Grand.Core.Plugins;
using Grand.Plugin.Payments.IDeal.Controllers;
using Grand.Plugin.Payments.IDeal.Models;
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
        private readonly IDealPaymentSettings _iDealPaymentSettings;
        private readonly ICustomerService _customerService;
        private readonly IPaymentService _paymentService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        #endregion

        #region Ctor
        public IDealPaymentProcessor(ILocalizationService localizationService, IWebHelper webHelper, IServiceProvider serviceProvider,
            ISettingService settingService, IDealPaymentSettings iDealPaymentSettings, ICustomerService customerService, IPaymentService paymentService,
            IHttpContextAccessor httpContextAccessor)
        {
            _localizationService = localizationService;
            _webHelper = webHelper;
            _serviceProvider = serviceProvider;
            _settingService = settingService;
            _iDealPaymentSettings = iDealPaymentSettings;
            _customerService = customerService;
            _paymentService = paymentService;
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
            return typeof(PaymentIDealController);
        }

        public async Task<ProcessPaymentRequest> GetPaymentInfo(IFormCollection form)
        {
            var result = new ProcessPaymentRequest();

            foreach (var key in form.Keys)
            {
                string value = form[key];
                if (key == "bank")
                {
                    result.CustomValues.Add("iDEAL", value);
                    break;
                }
            }

            return await Task.FromResult(result);
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
            var secretKey = _iDealPaymentSettings.SecretKey;
            var source = string.Empty;
            var clientSecret = _httpContextAccessor.HttpContext.Request.Query["client_secret"].ToString();

            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                if (!string.IsNullOrWhiteSpace(secretKey))
                {
                    //get store location
                    var storeLocation = _webHelper.GetStoreLocation();

                    StripeConfiguration.ApiKey = secretKey;
                    if (!string.IsNullOrWhiteSpace(postProcessPaymentRequest.Order.CustomValuesXml))
                    {
                        var bank = StringToXMLObject<DictionarySerializer>(postProcessPaymentRequest.Order.CustomValuesXml);


                        var options = new SourceCreateOptions() {
                            Amount = (long)postProcessPaymentRequest.Order.OrderTotal,
                            Currency = postProcessPaymentRequest.Order.CustomerCurrencyCode.ToLower(),
                            Type = SourceType.Ideal,
                            Redirect = new SourceRedirectOptions {
                                ReturnUrl = $"{storeLocation}checkout/OpcCompleteRedirectionPayment",
                            },
                            Ideal = new SourceIdealCreateOptions {
                                Bank = bank.Item.Value
                            }
                        };
                        var service = new SourceService();
                        Source charge = await service.CreateAsync(options);

                        if (charge.StripeResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            JObject jObject = JObject.Parse(charge.StripeResponse.Content);
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
                if(charge.StripeResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    postProcessPaymentRequest.Order.PaymentStatus = PaymentStatus.Paid;
                }
                _httpContextAccessor.HttpContext.Session.SetString("source", null);
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
            await this.DeletePluginLocaleResource(_serviceProvider, "Plugins.Payment.IDeal.DescriptionText");
            await this.DeletePluginLocaleResource(_serviceProvider, "Plugins.Payment.IDeal.PublishableKey");
            await this.DeletePluginLocaleResource(_serviceProvider, "Plugins.Payment.IDeal.SecreteKey");

            await base.Uninstall();
        }

        public async Task<IList<string>> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();
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

        #region Helpers
        public static T StringToXMLObject<T>(string str)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                T res = (T)serializer.Deserialize(new StringReader(str));
                return res;
            }
            catch(Exception ex)
            {
                throw ex;
            }

        }
        
        #endregion
    }
}
