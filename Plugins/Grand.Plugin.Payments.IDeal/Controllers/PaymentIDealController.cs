using Grand.Core;
using Grand.Framework.Controllers;
using Grand.Framework.Mvc.Filters;
using Grand.Plugin.Payments.IDeal.Models;
using Grand.Services.Configuration;
using Grand.Services.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Stripe;

namespace Grand.Plugin.Payments.IDeal.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    public class PaymentIDealController : BasePaymentController
    {
        private readonly ISettingService _settingService;
        private readonly IWorkContext _workContext;
        private readonly IDealPaymentSettings _iDealPaymentSettings;
        private readonly ILocalizationService _localizationService;

        public PaymentIDealController(ISettingService settingService, IWorkContext workContext, ILocalizationService localizationService, IDealPaymentSettings iDealPaymentSettings)
        {
            this._workContext = workContext;
            this._settingService = settingService;
            this._localizationService = localizationService;
            this._iDealPaymentSettings = iDealPaymentSettings;
        }
        public IActionResult Configure()
        {
            var model = new ConfigurationModel();
            model.SecretKey = _iDealPaymentSettings.SecretKey;
            model.PublishableKey = _iDealPaymentSettings.PublishableKey;

            return View("~/Plugins/Payments.IDeal/Views/PaymentIDeal/Configure.cshtml");
        }
        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //save settings
            _iDealPaymentSettings.SecretKey = model.SecretKey;

            await _settingService.SaveSetting(_iDealPaymentSettings);

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

    }
}
