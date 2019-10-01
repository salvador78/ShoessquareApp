using Grand.Core;
using Grand.Framework.Controllers;
using Grand.Framework.Mvc.Filters;
using Grand.Plugin.Payments.Stripe.Models;
using Grand.Services.Configuration;
using Grand.Services.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Stripe;

namespace Grand.Plugin.Payments.Stripe.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    public class PaymentStripeController : BasePaymentController
    {
        private readonly ISettingService _settingService;
        private readonly IWorkContext _workContext;
        private readonly StripePaymentSettings _stripePaymentSettings;
        private readonly ILocalizationService _localizationService;

        public PaymentStripeController(ISettingService settingService, IWorkContext workContext, ILocalizationService localizationService, StripePaymentSettings iDealPaymentSettings)
        {
            _workContext = workContext;
            _settingService = settingService;
            _localizationService = localizationService;
            _stripePaymentSettings = iDealPaymentSettings;
        }
        public IActionResult Configure()
        {
            var model = new ConfigurationModel();
            model.SecretKey = _stripePaymentSettings.SecretKey;
            model.PublishableKey = _stripePaymentSettings.PublishableKey;

            return View("~/Plugins/Payments.Stripe/Views/PaymentStripe/Configure.cshtml");
        }
        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //save settings
            _stripePaymentSettings.SecretKey = model.SecretKey;

            await _settingService.SaveSetting(_stripePaymentSettings);

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

    }
}
