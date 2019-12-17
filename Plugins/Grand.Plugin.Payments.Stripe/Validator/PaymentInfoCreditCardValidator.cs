using FluentValidation;
using Grand.Plugin.Payments.Stripe.Models;
using Grand.Services.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Grand.Plugin.Payments.Stripe.Validator
{
    public class PaymentInfoCreditCardValidator : AbstractValidator<PaymentInfoModel>
    {
        public PaymentInfoCreditCardValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.CardholderName).NotEmpty().WithMessage(localizationService.GetResource("Payment.CardholderName.Required"));
            RuleFor(x => x.CardNumber).CreditCard().WithMessage(localizationService.GetResource("Payment.CardNumber.Wrong"));
            RuleFor(x => x.CardCode).Matches(@"^[0-9]{3,4}$").WithMessage(localizationService.GetResource("Payment.CardCode.Wrong"));
            RuleFor(x => x.ExpireDate).NotEmpty().WithMessage(localizationService.GetResource("Payment.ExpireDate.Required"));
        }
    }
}
