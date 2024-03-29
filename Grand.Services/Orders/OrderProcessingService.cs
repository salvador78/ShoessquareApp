using Grand.Core;
using Grand.Core.Domain.Catalog;
using Grand.Core.Domain.Common;
using Grand.Core.Domain.Customers;
using Grand.Core.Domain.Discounts;
using Grand.Core.Domain.Localization;
using Grand.Core.Domain.Logging;
using Grand.Core.Domain.Orders;
using Grand.Core.Domain.Payments;
using Grand.Core.Domain.Shipping;
using Grand.Core.Domain.Tax;
using Grand.Core.Domain.Vendors;
using Grand.Services.Affiliates;
using Grand.Services.Catalog;
using Grand.Services.Common;
using Grand.Services.Customers;
using Grand.Services.Directory;
using Grand.Services.Discounts;
using Grand.Services.Events.Web;
using Grand.Services.Localization;
using Grand.Services.Logging;
using Grand.Services.Messages;
using Grand.Services.Payments;
using Grand.Services.Security;
using Grand.Services.Shipping;
using Grand.Services.Tax;
using Grand.Services.Vendors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Services.Orders
{
    /// <summary>
    /// Order processing service
    /// </summary>
    public partial class OrderProcessingService : IOrderProcessingService
    {
        #region Fields

        private readonly IOrderService _orderService;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;
        private readonly ILanguageService _languageService;
        private readonly IProductService _productService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger _logger;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IGiftCardService _giftCardService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
        private readonly IShippingService _shippingService;
        private readonly IShipmentService _shipmentService;
        private readonly ITaxService _taxService;
        private readonly ICustomerService _customerService;
        private readonly IDiscountService _discountService;
        private readonly IEncryptionService _encryptionService;
        private readonly IWorkContext _workContext;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly IVendorService _vendorService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ICustomerActionEventService _customerActionEventService;
        private readonly ICurrencyService _currencyService;
        private readonly IAffiliateService _affiliateService;
        private readonly IMediator _mediator;
        private readonly IPdfService _pdfService;
        private readonly IRewardPointsService _rewardPointsService;
        private readonly IReturnRequestService _returnRequestService;
        private readonly IStoreContext _storeContext;
        private readonly IProductReservationService _productReservationService;
        private readonly IAuctionService _auctionService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ShippingSettings _shippingSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly OrderSettings _orderSettings;
        private readonly TaxSettings _taxSettings;
        private readonly LocalizationSettings _localizationSettings;

        #endregion

        #region Ctor

        public OrderProcessingService(IOrderService orderService,
            IWebHelper webHelper,
            ILocalizationService localizationService,
            ILanguageService languageService,
            IProductService productService,
            IPaymentService paymentService,
            ILogger logger,
            IOrderTotalCalculationService orderTotalCalculationService,
            IPriceCalculationService priceCalculationService,
            IPriceFormatter priceFormatter,
            IProductAttributeParser productAttributeParser,
            IProductAttributeFormatter productAttributeFormatter,
            IGiftCardService giftCardService,
            IShoppingCartService shoppingCartService,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            IShippingService shippingService,
            IShipmentService shipmentService,
            ITaxService taxService,
            ICustomerService customerService,
            IDiscountService discountService,
            IEncryptionService encryptionService,
            IWorkContext workContext,
            IWorkflowMessageService workflowMessageService,
            IVendorService vendorService,
            ICustomerActivityService customerActivityService,
            ICustomerActionEventService customerActionEventService,
            ICurrencyService currencyService,
            IAffiliateService affiliateService,
            IMediator mediator,
            IPdfService pdfService,
            IRewardPointsService rewardPointsService,
            IReturnRequestService returnRequestService,
            IStoreContext storeContext,
            IProductReservationService productReservationService,
            IAuctionService auctionService,
            IGenericAttributeService genericAttributeService,
            IServiceProvider serviceProvider,
            ShippingSettings shippingSettings,
            PaymentSettings paymentSettings,
            RewardPointsSettings rewardPointsSettings,
            OrderSettings orderSettings,
            TaxSettings taxSettings,
            LocalizationSettings localizationSettings)
        {
            this._orderService = orderService;
            this._webHelper = webHelper;
            this._localizationService = localizationService;
            this._languageService = languageService;
            this._productService = productService;
            this._paymentService = paymentService;
            this._logger = logger;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._priceCalculationService = priceCalculationService;
            this._priceFormatter = priceFormatter;
            this._productAttributeParser = productAttributeParser;
            this._productAttributeFormatter = productAttributeFormatter;
            this._giftCardService = giftCardService;
            this._shoppingCartService = shoppingCartService;
            this._checkoutAttributeFormatter = checkoutAttributeFormatter;
            this._workContext = workContext;
            this._workflowMessageService = workflowMessageService;
            this._vendorService = vendorService;
            this._shippingService = shippingService;
            this._shipmentService = shipmentService;
            this._taxService = taxService;
            this._customerService = customerService;
            this._discountService = discountService;
            this._encryptionService = encryptionService;
            this._customerActivityService = customerActivityService;
            this._customerActionEventService = customerActionEventService;
            this._currencyService = currencyService;
            this._affiliateService = affiliateService;
            this._mediator = mediator;
            this._pdfService = pdfService;
            this._rewardPointsService = rewardPointsService;
            this._returnRequestService = returnRequestService;
            this._storeContext = storeContext;
            this._productReservationService = productReservationService;
            this._auctionService = auctionService;
            this._genericAttributeService = genericAttributeService;
            this._serviceProvider = serviceProvider;
            this._paymentSettings = paymentSettings;
            this._shippingSettings = shippingSettings;
            this._rewardPointsSettings = rewardPointsSettings;
            this._orderSettings = orderSettings;
            this._taxSettings = taxSettings;
            this._localizationSettings = localizationSettings;
        }

        #endregion

        #region Utilities

        protected virtual async Task<PlaceOrderContainter> PreparePlaceOrderDetailsForRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var details = new PlaceOrderContainter();
            //customer
            details.Customer = await _customerService.GetCustomerById(processPaymentRequest.CustomerId);
            if (details.Customer == null)
                throw new ArgumentException("Customer is not set");

            //affiliate
            var affiliate = await _affiliateService.GetAffiliateById(details.Customer.AffiliateId);
            if (affiliate != null && affiliate.Active && !affiliate.Deleted)
                details.AffiliateId = affiliate.Id;

            // Recurring orders.Load initial order
            details.InitialOrder = await _orderService.GetOrderById(processPaymentRequest.InitialOrderId);
            if (details.InitialOrder == null)
                throw new ArgumentException("Initial order is not set for recurring payment");

            processPaymentRequest.PaymentMethodSystemName = details.InitialOrder.PaymentMethodSystemName;

            details.CustomerCurrencyCode = details.InitialOrder.CustomerCurrencyCode;
            details.CustomerCurrencyRate = details.InitialOrder.CurrencyRate;
            details.CustomerLanguage = await _languageService.GetLanguageById(details.InitialOrder.CustomerLanguageId);
            
            if (details.InitialOrder.BillingAddress == null)
                throw new GrandException("Billing address is not available");

            //clone billing address
            details.BillingAddress = (Address)details.InitialOrder.BillingAddress.Clone();
            if (!String.IsNullOrEmpty(details.BillingAddress.CountryId))
            {
                var country = await _serviceProvider.GetRequiredService<ICountryService>().GetCountryById(details.BillingAddress.CountryId);
                if (country != null)
                    if (!country.AllowsBilling)
                        throw new GrandException(string.Format("Country '{0}' is not allowed for billing", country.Name));
            }

            details.CheckoutAttributesXml = details.InitialOrder.CheckoutAttributesXml;
            details.CheckoutAttributeDescription = details.InitialOrder.CheckoutAttributeDescription;

            details.CustomerTaxDisplayType = details.InitialOrder.CustomerTaxDisplayType;

            details.OrderSubTotalInclTax = details.InitialOrder.OrderSubtotalInclTax;
            details.OrderSubTotalExclTax = details.InitialOrder.OrderSubtotalExclTax;
            details.OrderDiscountAmount = details.InitialOrder.OrderDiscount;
            details.OrderSubTotalDiscountExclTax = details.InitialOrder.OrderSubTotalDiscountExclTax;
            details.OrderSubTotalDiscountInclTax = details.InitialOrder.OrderSubTotalDiscountInclTax;
            details.OrderTotal = details.InitialOrder.OrderTotal;

            var shoppingCartRequiresShipping = details.InitialOrder.ShippingStatus != ShippingStatus.ShippingNotRequired;
            if (shoppingCartRequiresShipping)
            {
                var pickupPoint = await details.Customer.GetAttribute<string>(_genericAttributeService, SystemCustomerAttributeNames.SelectedPickupPoint, processPaymentRequest.StoreId);
                if (_shippingSettings.AllowPickUpInStore && pickupPoint != null)
                {
                    details.PickUpInStore = true;
                    details.PickupPoint = await _shippingService.GetPickupPointById(pickupPoint);
                }
                else
                {
                    if (details.InitialOrder.ShippingAddress == null)
                        throw new GrandException("Shipping address is not available");

                    //clone shipping address
                    details.ShippingAddress = (Address)details.InitialOrder.ShippingAddress.Clone();
                    if (!String.IsNullOrEmpty(details.ShippingAddress.CountryId))
                    {
                        var country = await _serviceProvider.GetRequiredService<ICountryService>().GetCountryById(details.ShippingAddress.CountryId);
                        if (country != null)
                            if (!country.AllowsShipping)
                                throw new GrandException(string.Format("Country '{0}' is not allowed for shipping", country.Name));
                    }
                }

                details.ShippingMethodName = details.InitialOrder.ShippingMethod;
                details.ShippingRateComputationMethodSystemName = details.InitialOrder.ShippingRateComputationMethodSystemName;
            }
            details.ShippingStatus = shoppingCartRequiresShipping
                ? ShippingStatus.NotYetShipped
                : ShippingStatus.ShippingNotRequired;

            details.OrderShippingTotalInclTax = details.InitialOrder.OrderShippingInclTax;
            details.OrderShippingTotalExclTax = details.InitialOrder.OrderShippingExclTax;
            details.PaymentAdditionalFeeInclTax = details.InitialOrder.PaymentMethodAdditionalFeeInclTax;
            details.PaymentAdditionalFeeExclTax = details.InitialOrder.PaymentMethodAdditionalFeeExclTax;
            details.OrderTaxTotal = details.InitialOrder.OrderTax;
            details.TaxRates = details.InitialOrder.TaxRates;
            details.IsRecurringShoppingCart = true;
            processPaymentRequest.OrderTotal = details.OrderTotal;
            processPaymentRequest.CustomerCurrencyCode = details.CustomerCurrencyCode;

            return details;
        }

        protected virtual async Task<ProcessPaymentResult> PrepareProcessPaymentResult(ProcessPaymentRequest processPaymentRequest, PlaceOrderContainter details)
        {
            //process payment
            ProcessPaymentResult processPaymentResult = null;
            //skip payment workflow if order total equals zero
            var skipPaymentWorkflow = details.OrderTotal == decimal.Zero;
            if (!skipPaymentWorkflow)
            {
                var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(processPaymentRequest.PaymentMethodSystemName);
                if (paymentMethod == null)
                    throw new GrandException("Payment method couldn't be loaded");

                //ensure that payment method is active
                if (!paymentMethod.IsPaymentMethodActive(_paymentSettings))
                    throw new GrandException("Payment method is not active");

                if (!processPaymentRequest.IsRecurringPayment)
                {
                    if (details.IsRecurringShoppingCart)
                    {
                        //recurring cart
                        var recurringPaymentType = _paymentService.GetRecurringPaymentType(processPaymentRequest.PaymentMethodSystemName);
                        switch (recurringPaymentType)
                        {
                            case RecurringPaymentType.NotSupported:
                                throw new GrandException("Recurring payments are not supported by selected payment method");
                            case RecurringPaymentType.Manual:
                            case RecurringPaymentType.Automatic:
                                processPaymentResult = await _paymentService.ProcessRecurringPayment(processPaymentRequest);
                                break;
                            default:
                                throw new GrandException("Not supported recurring payment type");
                        }
                    }
                    else
                    {
                        //standard cart
                        processPaymentResult = await _paymentService.ProcessPayment(processPaymentRequest);
                    }
                }
                else
                {
                    if (details.IsRecurringShoppingCart)
                    {
                        //Old credit card info
                        processPaymentRequest.CreditCardType = details.InitialOrder.AllowStoringCreditCardNumber ? _encryptionService.DecryptText(details.InitialOrder.CardType) : "";
                        processPaymentRequest.CreditCardName = details.InitialOrder.AllowStoringCreditCardNumber ? _encryptionService.DecryptText(details.InitialOrder.CardName) : "";
                        processPaymentRequest.CreditCardNumber = details.InitialOrder.AllowStoringCreditCardNumber ? _encryptionService.DecryptText(details.InitialOrder.CardNumber) : "";
                        processPaymentRequest.CreditCardCvv2 = details.InitialOrder.AllowStoringCreditCardNumber ? _encryptionService.DecryptText(details.InitialOrder.CardCvv2) : "";
                        try
                        {
                            processPaymentRequest.CreditCardExpireMonth = details.InitialOrder.AllowStoringCreditCardNumber ? Convert.ToInt32(_encryptionService.DecryptText(details.InitialOrder.CardExpirationMonth)) : 0;
                            processPaymentRequest.CreditCardExpireYear = details.InitialOrder.AllowStoringCreditCardNumber ? Convert.ToInt32(_encryptionService.DecryptText(details.InitialOrder.CardExpirationYear)) : 0;
                        }
                        catch { }

                        var recurringPaymentType = _paymentService.GetRecurringPaymentType(processPaymentRequest.PaymentMethodSystemName);
                        switch (recurringPaymentType)
                        {
                            case RecurringPaymentType.NotSupported:
                                throw new GrandException("Recurring payments are not supported by selected payment method");
                            case RecurringPaymentType.Manual:
                                processPaymentResult = await _paymentService.ProcessRecurringPayment(processPaymentRequest);
                                break;
                            case RecurringPaymentType.Automatic:
                                //payment is processed on payment gateway site
                                processPaymentResult = new ProcessPaymentResult();
                                break;
                            default:
                                throw new GrandException("Not supported recurring payment type");
                        }
                    }
                    else
                    {
                        throw new GrandException("No recurring products");
                    }
                }
            }
            else
            {
                //payment is not required
                if (processPaymentResult == null)
                    processPaymentResult = new ProcessPaymentResult();
                processPaymentResult.NewPaymentStatus = PaymentStatus.Paid;
            }

            if (processPaymentResult == null)
                throw new GrandException("processPaymentResult is not available");

            return processPaymentResult;
        }

        protected virtual async Task<Order> PrepareOrder(ProcessPaymentRequest processPaymentRequest, ProcessPaymentResult processPaymentResult, PlaceOrderContainter details)
        {
            var order = new Order {
                StoreId = processPaymentRequest.StoreId,
                OrderGuid = processPaymentRequest.OrderGuid,
                CustomerId = details.Customer.Id,
                CustomerLanguageId = details.CustomerLanguage.Id,
                CustomerTaxDisplayType = details.CustomerTaxDisplayType,
                CustomerIp = _webHelper.GetCurrentIpAddress(),
                OrderSubtotalInclTax = Math.Round(details.OrderSubTotalInclTax, 6),
                OrderSubtotalExclTax = Math.Round(details.OrderSubTotalExclTax, 6),
                OrderSubTotalDiscountInclTax = Math.Round(details.OrderSubTotalDiscountInclTax, 6),
                OrderSubTotalDiscountExclTax = Math.Round(details.OrderSubTotalDiscountExclTax, 6),
                OrderShippingInclTax = Math.Round(details.OrderShippingTotalInclTax, 6),
                OrderShippingExclTax = Math.Round(details.OrderShippingTotalExclTax, 6),
                PaymentMethodAdditionalFeeInclTax = Math.Round(details.PaymentAdditionalFeeInclTax, 6),
                PaymentMethodAdditionalFeeExclTax = Math.Round(details.PaymentAdditionalFeeExclTax, 6),
                TaxRates = details.TaxRates,
                OrderTax = Math.Round(details.OrderTaxTotal, 6),
                OrderTotal = Math.Round(details.OrderTotal, 6),
                RefundedAmount = decimal.Zero,
                OrderDiscount = Math.Round(details.OrderDiscountAmount, 6),
                CheckoutAttributeDescription = details.CheckoutAttributeDescription,
                CheckoutAttributesXml = details.CheckoutAttributesXml,
                CustomerCurrencyCode = details.CustomerCurrencyCode,
                PrimaryCurrencyCode = details.PrimaryCurrencyCode,
                CurrencyRate = details.CustomerCurrencyRate,
                AffiliateId = details.AffiliateId,
                OrderStatus = OrderStatus.Pending,
                AllowStoringCreditCardNumber = processPaymentResult.AllowStoringCreditCardNumber,
                CardType = processPaymentResult.AllowStoringCreditCardNumber ? _encryptionService.EncryptText(processPaymentRequest.CreditCardType) : string.Empty,
                CardName = processPaymentResult.AllowStoringCreditCardNumber ? _encryptionService.EncryptText(processPaymentRequest.CreditCardName) : string.Empty,
                CardNumber = processPaymentResult.AllowStoringCreditCardNumber ? _encryptionService.EncryptText(processPaymentRequest.CreditCardNumber) : string.Empty,
                MaskedCreditCardNumber = _encryptionService.EncryptText(_paymentService.GetMaskedCreditCardNumber(processPaymentRequest.CreditCardNumber)),
                CardCvv2 = processPaymentResult.AllowStoringCreditCardNumber ? _encryptionService.EncryptText(processPaymentRequest.CreditCardCvv2) : string.Empty,
                CardExpirationMonth = processPaymentResult.AllowStoringCreditCardNumber ? _encryptionService.EncryptText(processPaymentRequest.CreditCardExpireMonth.ToString()) : string.Empty,
                CardExpirationYear = processPaymentResult.AllowStoringCreditCardNumber ? _encryptionService.EncryptText(processPaymentRequest.CreditCardExpireYear.ToString()) : string.Empty,
                PaymentMethodSystemName = processPaymentRequest.PaymentMethodSystemName,
                AuthorizationTransactionId = processPaymentResult.AuthorizationTransactionId,
                AuthorizationTransactionCode = processPaymentResult.AuthorizationTransactionCode,
                AuthorizationTransactionResult = processPaymentResult.AuthorizationTransactionResult,
                CaptureTransactionId = processPaymentResult.CaptureTransactionId,
                CaptureTransactionResult = processPaymentResult.CaptureTransactionResult,
                SubscriptionTransactionId = processPaymentResult.SubscriptionTransactionId,
                PaymentStatus = processPaymentResult.NewPaymentStatus,
                PaidDateUtc = null,
                BillingAddress = details.BillingAddress,
                ShippingAddress = details.ShippingAddress,
                ShippingStatus = details.ShippingStatus,
                ShippingMethod = details.ShippingMethodName,
                PickUpInStore = details.PickUpInStore,
                PickupPoint = details.PickupPoint,
                ShippingRateComputationMethodSystemName = details.ShippingRateComputationMethodSystemName,
                CustomValuesXml = processPaymentRequest.SerializeCustomValues(),
                VatNumber = await details.Customer.GetAttribute<string>(_genericAttributeService, SystemCustomerAttributeNames.VatNumber),
                VatNumberStatusId = await details.Customer.GetAttribute<int>(_genericAttributeService, SystemCustomerAttributeNames.VatNumberStatusId),
                CompanyName = await details.Customer.GetAttribute<string>(_genericAttributeService, SystemCustomerAttributeNames.Company),
                FirstName = await details.Customer.GetAttribute<string>(_genericAttributeService, SystemCustomerAttributeNames.FirstName),
                LastName = await details.Customer.GetAttribute<string>(_genericAttributeService, SystemCustomerAttributeNames.LastName),
                CustomerEmail = details.Customer.Email,
                UrlReferrer = await details.Customer.GetAttribute<string>(_genericAttributeService, SystemCustomerAttributeNames.LastUrlReferrer),
                ShippingOptionAttributeDescription = await details.Customer.GetAttribute<string>(_genericAttributeService, SystemCustomerAttributeNames.ShippingOptionAttributeDescription, processPaymentRequest.StoreId),
                ShippingOptionAttributeXml = await details.Customer.GetAttribute<string>(_genericAttributeService, SystemCustomerAttributeNames.ShippingOptionAttributeXml, processPaymentRequest.StoreId),
                CreatedOnUtc = DateTime.UtcNow
            };

            return order;
        }

        protected virtual async Task<PlaceOrderContainter> PreparePlaceOrderDetails(ProcessPaymentRequest processPaymentRequest)
        {
            var details = new PlaceOrderContainter();

            //customer
            details.Customer = await _customerService.GetCustomerById(processPaymentRequest.CustomerId);
            if (details.Customer == null)
                throw new ArgumentException("Customer is not set");

            //affiliate
            var affiliate = await _affiliateService.GetAffiliateById(details.Customer.AffiliateId);
            if (affiliate != null && affiliate.Active && !affiliate.Deleted)
                details.AffiliateId = affiliate.Id;

            //customer currency
            var currencyTmp = await _currencyService.GetCurrencyById(await details.Customer.GetAttribute<string>(_genericAttributeService, SystemCustomerAttributeNames.CurrencyId, processPaymentRequest.StoreId));
            var customerCurrency = (currencyTmp != null && currencyTmp.Published) ? currencyTmp : _workContext.WorkingCurrency;
            details.CustomerCurrencyCode = customerCurrency.CurrencyCode;
            var primaryStoreCurrency = await _currencyService.GetPrimaryStoreCurrency();
            details.CustomerCurrencyRate = customerCurrency.Rate / primaryStoreCurrency.Rate;
            details.PrimaryCurrencyCode = primaryStoreCurrency.CurrencyCode;

            //customer language
            details.CustomerLanguage = await _languageService.GetLanguageById(await details.Customer.GetAttribute<string>(
               _genericAttributeService, SystemCustomerAttributeNames.LanguageId, processPaymentRequest.StoreId));

            if (details.CustomerLanguage == null || !details.CustomerLanguage.Published)
                details.CustomerLanguage = _workContext.WorkingLanguage;

            //check whether customer is guest
            if (details.Customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
                throw new GrandException("Anonymous checkout is not allowed");

            //billing address
            if (details.Customer.BillingAddress == null)
                throw new GrandException("Billing address is not provided");

            if (!CommonHelper.IsValidEmail(details.Customer.BillingAddress.Email))
                throw new GrandException("Email is not valid");

            //clone billing address
            details.BillingAddress = (Address)details.Customer.BillingAddress.Clone();
            if (!String.IsNullOrEmpty(details.BillingAddress.CountryId))
            {
                var country = await _serviceProvider.GetRequiredService<ICountryService>().GetCountryById(details.BillingAddress.CountryId);
                if (country != null)
                    if (!country.AllowsBilling)
                        throw new GrandException(string.Format("Country '{0}' is not allowed for billing", country.Name));
            }

            //checkout attributes
            details.CheckoutAttributesXml = await details.Customer.GetAttribute<string>(_genericAttributeService, SystemCustomerAttributeNames.CheckoutAttributes, processPaymentRequest.StoreId);
            details.CheckoutAttributeDescription = await _checkoutAttributeFormatter.FormatAttributes(details.CheckoutAttributesXml, details.Customer);

            //load and validate customer shopping cart
            //load shopping cart
            details.Cart = details.Customer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart || sci.ShoppingCartType == ShoppingCartType.Auctions)
                .LimitPerStore(_serviceProvider.GetRequiredService<ShoppingCartSettings>().CartsSharedBetweenStores, processPaymentRequest.StoreId)
                .ToList();

            if (!details.Cart.Any())
                throw new GrandException("Cart is empty");

            //validate the entire shopping cart
            var warnings = await _shoppingCartService.GetShoppingCartWarnings(details.Cart,
                details.CheckoutAttributesXml,
                true);
            if (warnings.Any())
            {
                var warningsSb = new StringBuilder();
                foreach (string warning in warnings)
                {
                    warningsSb.Append(warning);
                    warningsSb.Append(";");
                }
                throw new GrandException(warningsSb.ToString());
            }

            //validate individual cart items
            foreach (var sci in details.Cart)
            {
                var product = await _productService.GetProductById(sci.ProductId);
                var sciWarnings = await _shoppingCartService.GetShoppingCartItemWarnings(details.Customer, sci, product, false);
                if (sciWarnings.Any())
                {
                    var warningsSb = new StringBuilder();
                    foreach (string warning in sciWarnings)
                    {
                        warningsSb.Append(warning);
                        warningsSb.Append(";");
                    }
                    throw new GrandException(warningsSb.ToString());
                }
            }

            //min totals validation
            bool minOrderSubtotalAmountOk = await ValidateMinOrderSubtotalAmount(details.Cart);
            if (!minOrderSubtotalAmountOk)
            {
                decimal minOrderSubtotalAmount = await _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderSubtotalAmount, _workContext.WorkingCurrency);
                throw new GrandException(string.Format(_localizationService.GetResource("Checkout.MinOrderSubtotalAmount"), _priceFormatter.FormatPrice(minOrderSubtotalAmount, true, false)));
            }
            bool minOrderTotalAmountOk = await ValidateMinOrderTotalAmount(details.Cart);
            if (!minOrderTotalAmountOk)
            {
                decimal minOrderTotalAmount = await _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderTotalAmount, _workContext.WorkingCurrency);
                throw new GrandException(string.Format(_localizationService.GetResource("Checkout.MinOrderTotalAmount"), _priceFormatter.FormatPrice(minOrderTotalAmount, true, false)));
            }

            //tax display type
            if (_taxSettings.AllowCustomersToSelectTaxDisplayType)
                details.CustomerTaxDisplayType = (TaxDisplayType)await details.Customer.GetAttribute<int>(_genericAttributeService, SystemCustomerAttributeNames.TaxDisplayTypeId, processPaymentRequest.StoreId);
            else
                details.CustomerTaxDisplayType = _taxSettings.TaxDisplayType;

            //sub total
            //sub total (incl tax)
            var shoppingCartSubTotal = await _orderTotalCalculationService.GetShoppingCartSubTotal(details.Cart, true);
            decimal orderSubTotalDiscountAmount = shoppingCartSubTotal.discountAmount;
            List<AppliedDiscount> orderSubTotalAppliedDiscounts = shoppingCartSubTotal.appliedDiscounts;
            decimal subTotalWithoutDiscountBase = shoppingCartSubTotal.subTotalWithoutDiscount;
            decimal subTotalWithDiscountBase = shoppingCartSubTotal.subTotalWithDiscount;

            details.OrderSubTotalInclTax = subTotalWithoutDiscountBase;
            details.OrderSubTotalDiscountInclTax = orderSubTotalDiscountAmount;

            foreach (var disc in orderSubTotalAppliedDiscounts)
                if (!details.AppliedDiscounts.Where(x => x.DiscountId == disc.DiscountId).Any())
                    details.AppliedDiscounts.Add(disc);

            //sub total (excl tax)
            shoppingCartSubTotal = await _orderTotalCalculationService.GetShoppingCartSubTotal(details.Cart, false);
            orderSubTotalDiscountAmount = shoppingCartSubTotal.discountAmount;
            orderSubTotalAppliedDiscounts = shoppingCartSubTotal.appliedDiscounts;
            subTotalWithoutDiscountBase = shoppingCartSubTotal.subTotalWithoutDiscount;
            subTotalWithDiscountBase = shoppingCartSubTotal.subTotalWithDiscount;

            details.OrderSubTotalExclTax = subTotalWithoutDiscountBase;
            details.OrderSubTotalDiscountExclTax = orderSubTotalDiscountAmount;

            //shipping info
            bool shoppingCartRequiresShipping = shoppingCartRequiresShipping = details.Cart.RequiresShipping();

            if (shoppingCartRequiresShipping)
            {
                var pickupPoint = await details.Customer.GetAttribute<string>(_genericAttributeService, SystemCustomerAttributeNames.SelectedPickupPoint, processPaymentRequest.StoreId);
                if (_shippingSettings.AllowPickUpInStore && pickupPoint != null)
                {
                    details.PickUpInStore = true;
                    details.PickupPoint = await _shippingService.GetPickupPointById(pickupPoint);
                }
                else
                {
                    if (details.Customer.ShippingAddress == null)
                        throw new GrandException("Shipping address is not provided");

                    if (!CommonHelper.IsValidEmail(details.Customer.ShippingAddress.Email))
                        throw new GrandException("Email is not valid");

                    //clone shipping address
                    details.ShippingAddress = (Address)details.Customer.ShippingAddress.Clone();
                    if (!String.IsNullOrEmpty(details.ShippingAddress.CountryId))
                    {
                        var country = await _serviceProvider.GetRequiredService<ICountryService>().GetCountryById(details.ShippingAddress.CountryId);
                        if (country != null)
                            if (!country.AllowsShipping)
                                throw new GrandException(string.Format("Country '{0}' is not allowed for shipping", country.Name));
                    }
                }
                var shippingOption = await details.Customer.GetAttribute<ShippingOption>(_genericAttributeService, SystemCustomerAttributeNames.SelectedShippingOption, processPaymentRequest.StoreId);
                if (shippingOption != null)
                {
                    details.ShippingMethodName = shippingOption.Name;
                    details.ShippingRateComputationMethodSystemName = shippingOption.ShippingRateComputationMethodSystemName;
                }
            }
            details.ShippingStatus = shoppingCartRequiresShipping
                ? ShippingStatus.NotYetShipped
                : ShippingStatus.ShippingNotRequired;

            //shipping total

            var shoppingCartShippingTotal = await _orderTotalCalculationService.GetShoppingCartShippingTotal(details.Cart, true);
            decimal tax = shoppingCartShippingTotal.taxRate;
            List<AppliedDiscount> shippingTotalDiscounts = shoppingCartShippingTotal.appliedDiscounts;
            var orderShippingTotalInclTax = shoppingCartShippingTotal.shoppingCartShippingTotal;
            var orderShippingTotalExclTax = (await _orderTotalCalculationService.GetShoppingCartShippingTotal(details.Cart, false)).shoppingCartShippingTotal;
            if (!orderShippingTotalInclTax.HasValue || !orderShippingTotalExclTax.HasValue)
                throw new GrandException("Shipping total couldn't be calculated");

            foreach (var disc in shippingTotalDiscounts)
            {
                if (!details.AppliedDiscounts.Where(x => x.DiscountId == disc.DiscountId).Any())
                    details.AppliedDiscounts.Add(disc);
            }


            details.OrderShippingTotalInclTax = orderShippingTotalInclTax.Value;
            details.OrderShippingTotalExclTax = orderShippingTotalExclTax.Value;

            //payment total
            decimal paymentAdditionalFee = await _paymentService.GetAdditionalHandlingFee(details.Cart, processPaymentRequest.PaymentMethodSystemName);
            details.PaymentAdditionalFeeInclTax = (await _taxService.GetPaymentMethodAdditionalFee(paymentAdditionalFee, true, details.Customer)).paymentPrice;
            details.PaymentAdditionalFeeExclTax = (await _taxService.GetPaymentMethodAdditionalFee(paymentAdditionalFee, false, details.Customer)).paymentPrice;

            //tax total
            //tax amount
            var (taxtotal, taxRates) = await _orderTotalCalculationService.GetTaxTotal(details.Cart);
            SortedDictionary<decimal, decimal> taxRatesDictionary = taxRates;
            details.OrderTaxTotal = taxtotal;

            //tax rates
            foreach (var kvp in taxRatesDictionary)
            {
                var taxRate = kvp.Key;
                var taxValue = kvp.Value;
                details.TaxRates += string.Format("{0}:{1};   ", taxRate.ToString(CultureInfo.InvariantCulture), taxValue.ToString(CultureInfo.InvariantCulture));
            }


            //order total (and applied discounts, gift cards, reward points)
            var shoppingCartTotal = await _orderTotalCalculationService.GetShoppingCartTotal(details.Cart);
            List<AppliedGiftCard> appliedGiftCards = shoppingCartTotal.appliedGiftCards;
            List<AppliedDiscount> orderAppliedDiscounts = shoppingCartTotal.appliedDiscounts;
            decimal orderDiscountAmount = shoppingCartTotal.discountAmount;
            int redeemedRewardPoints = shoppingCartTotal.redeemedRewardPoints;
            decimal redeemedRewardPointsAmount = shoppingCartTotal.redeemedRewardPointsAmount;
            var orderTotal = shoppingCartTotal.shoppingCartTotal;
            if (!orderTotal.HasValue)
                throw new GrandException("Order total couldn't be calculated");

            details.OrderDiscountAmount = orderDiscountAmount;
            details.RedeemedRewardPoints = redeemedRewardPoints;
            details.RedeemedRewardPointsAmount = redeemedRewardPointsAmount;
            details.AppliedGiftCards = appliedGiftCards;
            details.OrderTotal = orderTotal.Value;

            //discount history
            foreach (var disc in orderAppliedDiscounts)
            {
                if (!details.AppliedDiscounts.Where(x => x.DiscountId == disc.DiscountId).Any())
                    details.AppliedDiscounts.Add(disc);
            }

            details.IsRecurringShoppingCart = details.Cart.IsRecurring();
            if (details.IsRecurringShoppingCart)
            {
                var (info, cycleLength, cyclePeriod, totalCycles) = await details.Cart.GetRecurringCycleInfo(_localizationService, _productService);

                int recurringCycleLength = cycleLength;
                RecurringProductCyclePeriod recurringCyclePeriod = cyclePeriod;
                int recurringTotalCycles = totalCycles;
                string recurringCyclesError = info;
                if (!string.IsNullOrEmpty(recurringCyclesError))
                    throw new GrandException(recurringCyclesError);

                processPaymentRequest.RecurringCycleLength = recurringCycleLength;
                processPaymentRequest.RecurringCyclePeriod = recurringCyclePeriod;
                processPaymentRequest.RecurringTotalCycles = recurringTotalCycles;
            }

            processPaymentRequest.OrderTotal = details.OrderTotal;
            processPaymentRequest.CustomerCurrencyCode = details.CustomerCurrencyCode;

            return details;
        }

        protected virtual async Task<Order> SaveOrderDetails(PlaceOrderContainter details, Order order)
        {
            var reservationsToUpdate = new List<ProductReservation>();
            var bidsToUpdate = new List<Bid>();

            //move shopping cart items to order items
            foreach (var sc in details.Cart)
            {
                //prices
                decimal taxRate;
                List<AppliedDiscount> scDiscounts;
                decimal discountAmount;
                decimal scUnitPrice = (await _priceCalculationService.GetUnitPrice(sc)).unitprice;
                decimal scUnitPriceWithoutDisc = (await _priceCalculationService.GetUnitPrice(sc, false)).unitprice;

                var product = await _productService.GetProductById(sc.ProductId);
                var subtotal = await _priceCalculationService.GetSubTotal(sc, true);
                decimal scSubTotal = subtotal.subTotal;
                discountAmount = subtotal.discountAmount;
                scDiscounts = subtotal.appliedDiscounts;

                var prices = await _taxService.GetTaxProductPrice(product, details.Customer, scUnitPrice, scUnitPriceWithoutDisc, scSubTotal, discountAmount, _taxSettings.PricesIncludeTax);
                taxRate = prices.taxRate;
                decimal scUnitPriceWithoutDiscInclTax = prices.UnitPriceWihoutDiscInclTax;
                decimal scUnitPriceWithoutDiscExclTax = prices.UnitPriceWihoutDiscExclTax;
                decimal scUnitPriceInclTax = prices.UnitPriceInclTax;
                decimal scUnitPriceExclTax = prices.UnitPriceExclTax;
                decimal scSubTotalInclTax = prices.SubTotalInclTax;
                decimal scSubTotalExclTax = prices.SubTotalExclTax;
                decimal discountAmountInclTax = prices.discountAmountInclTax;
                decimal discountAmountExclTax = prices.discountAmountExclTax;

                foreach (var disc in scDiscounts)
                {
                    if (!details.AppliedDiscounts.Where(x => x.DiscountId == disc.DiscountId).Any())
                        details.AppliedDiscounts.Add(disc);
                }

                //attributes
                string attributeDescription = await _productAttributeFormatter.FormatAttributes(product, sc.AttributesXml, details.Customer);

                if (string.IsNullOrEmpty(attributeDescription) && sc.ShoppingCartType == ShoppingCartType.Auctions)
                    attributeDescription = _localizationService.GetResource("ShoppingCart.auctionwonon") + " " + product.AvailableEndDateTimeUtc;

                var itemWeight = await _shippingService.GetShoppingCartItemWeight(sc);

                var warehouseId = _storeContext.CurrentStore.DefaultWarehouseId;
                if (!product.UseMultipleWarehouses)
                {
                    if (!string.IsNullOrEmpty(product.WarehouseId))
                    {
                        warehouseId = product.WarehouseId;
                    }
                }

                //save order item
                var orderItem = new OrderItem {
                    OrderItemGuid = Guid.NewGuid(),
                    ProductId = sc.ProductId,
                    VendorId = product.VendorId,
                    WarehouseId = warehouseId,
                    UnitPriceWithoutDiscInclTax = Math.Round(scUnitPriceWithoutDiscInclTax, 6),
                    UnitPriceWithoutDiscExclTax = Math.Round(scUnitPriceWithoutDiscExclTax, 6),
                    UnitPriceInclTax = Math.Round(scUnitPriceInclTax, 6),
                    UnitPriceExclTax = Math.Round(scUnitPriceExclTax, 6),
                    PriceInclTax = Math.Round(scSubTotalInclTax, 6),
                    PriceExclTax = Math.Round(scSubTotalExclTax, 6),
                    OriginalProductCost = await _priceCalculationService.GetProductCost(product, sc.AttributesXml),
                    AttributeDescription = attributeDescription,
                    AttributesXml = sc.AttributesXml,
                    Quantity = sc.Quantity,
                    DiscountAmountInclTax = Math.Round(discountAmountInclTax, 6),
                    DiscountAmountExclTax = Math.Round(discountAmountExclTax, 6),
                    DownloadCount = 0,
                    IsDownloadActivated = false,
                    LicenseDownloadId = "",
                    ItemWeight = itemWeight,
                    RentalStartDateUtc = sc.RentalStartDateUtc,
                    RentalEndDateUtc = sc.RentalEndDateUtc,
                    CreatedOnUtc = DateTime.UtcNow,
                };

                string reservationInfo = "";
                if (product.ProductType == ProductType.Reservation)
                {
                    if (sc.RentalEndDateUtc == default(DateTime) || sc.RentalEndDateUtc == null)
                    {
                        reservationInfo = sc.RentalStartDateUtc.ToString();
                    }
                    else
                    {
                        reservationInfo = sc.RentalStartDateUtc + " - " + sc.RentalEndDateUtc;
                    }
                    if (!string.IsNullOrEmpty(sc.Parameter))
                    {
                        reservationInfo += "<br>" + string.Format(_localizationService.GetResource("ShoppingCart.Reservation.Option"), sc.Parameter);
                    }
                    if (!string.IsNullOrEmpty(sc.Duration))
                    {
                        reservationInfo += "<br>" + _localizationService.GetResource("Products.Duration") + ": " + sc.Duration;
                    }
                }
                if (!string.IsNullOrEmpty(reservationInfo))
                {
                    if (!string.IsNullOrEmpty(orderItem.AttributeDescription))
                    {
                        orderItem.AttributeDescription += "<br>" + reservationInfo;
                    }
                    else
                    {
                        orderItem.AttributeDescription = reservationInfo;
                    }
                }

                order.OrderItems.Add(orderItem);

                await _productService.UpdateSold(sc.ProductId, sc.Quantity);

                //gift cards
                if (product.IsGiftCard)
                {
                    _productAttributeParser.GetGiftCardAttribute(sc.AttributesXml,
                        out string giftCardRecipientName, out string giftCardRecipientEmail,
                        out string giftCardSenderName, out string giftCardSenderEmail, out string giftCardMessage);

                    for (int i = 0; i < sc.Quantity; i++)
                    {
                        var gc = new GiftCard {
                            GiftCardType = product.GiftCardType,
                            PurchasedWithOrderItem = orderItem,
                            Amount = product.OverriddenGiftCardAmount ?? scUnitPriceExclTax,
                            IsGiftCardActivated = false,
                            GiftCardCouponCode = _giftCardService.GenerateGiftCardCode(),
                            RecipientName = giftCardRecipientName,
                            RecipientEmail = giftCardRecipientEmail,
                            SenderName = giftCardSenderName,
                            SenderEmail = giftCardSenderEmail,
                            Message = giftCardMessage,
                            IsRecipientNotified = false,
                            CreatedOnUtc = DateTime.UtcNow
                        };
                        await _giftCardService.InsertGiftCard(gc);
                    }
                }

                //reservations
                if (product.ProductType == ProductType.Reservation)
                {
                    if (!string.IsNullOrEmpty(sc.ReservationId))
                    {
                        var reservation = await _productReservationService.GetProductReservation(sc.ReservationId);
                        reservationsToUpdate.Add(reservation);
                    }

                    if (sc.RentalStartDateUtc.HasValue && sc.RentalEndDateUtc.HasValue)
                    {
                        var reservations = await _productReservationService.GetProductReservationsByProductId(product.Id, true, null);
                        var grouped = reservations.GroupBy(x => x.Resource);

                        IGrouping<string, ProductReservation> groupToBook = null;
                        foreach (var group in grouped)
                        {
                            bool groupCanBeBooked = true;
                            if (product.IncBothDate && product.IntervalUnitType == IntervalUnit.Day)
                            {
                                for (DateTime iterator = sc.RentalStartDateUtc.Value; iterator <= sc.RentalEndDateUtc.Value; iterator += new TimeSpan(24, 0, 0))
                                {
                                    if (!group.Select(x => x.Date).Contains(iterator))
                                    {
                                        groupCanBeBooked = false;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                for (DateTime iterator = sc.RentalStartDateUtc.Value; iterator < sc.RentalEndDateUtc.Value; iterator += new TimeSpan(24, 0, 0))
                                {
                                    if (!group.Select(x => x.Date).Contains(iterator))
                                    {
                                        groupCanBeBooked = false;
                                        break;
                                    }
                                }
                            }
                            if (groupCanBeBooked)
                            {
                                groupToBook = group;
                                break;
                            }
                        }

                        if (groupToBook == null)
                        {
                            throw new Exception("ShoppingCart.Reservation.Nofreereservationsinthisperiod");
                        }
                        else
                        {
                            var temp = groupToBook.AsQueryable();
                            if (product.IncBothDate && product.IntervalUnitType == IntervalUnit.Day)
                            {
                                temp = temp.Where(x => x.Date >= sc.RentalStartDateUtc && x.Date <= sc.RentalEndDateUtc);
                            }
                            else
                            {
                                temp = temp.Where(x => x.Date >= sc.RentalStartDateUtc && x.Date < sc.RentalEndDateUtc);
                            }

                            foreach (var item in temp)
                            {
                                item.OrderId = order.OrderGuid.ToString();
                                await _productReservationService.UpdateProductReservation(item);
                            }

                            reservationsToUpdate.AddRange(temp);
                        }
                    }
                }

                //auctions
                if (sc.ShoppingCartType == ShoppingCartType.Auctions)
                {
                    var bid = (await _auctionService.GetBidsByProductId(product.Id)).Where(x => x.Amount == product.HighestBid).FirstOrDefault();
                    if (bid == null)
                        throw new ArgumentNullException("bid");

                    bidsToUpdate.Add(bid);
                }

                if (product.ProductType == ProductType.Auction && sc.ShoppingCartType == ShoppingCartType.ShoppingCart)
                {
                    await _auctionService.UpdateAuctionEnded(product, true, true);
                    await _auctionService.UpdateHighestBid(product, product.Price, order.CustomerId);
                    await _workflowMessageService.SendAuctionEndedCustomerNotificationBin(product, order.CustomerId, order.CustomerLanguageId, order.StoreId);
                    await _auctionService.InsertBid(new Bid() {
                        CustomerId = order.CustomerId,
                        OrderId = order.Id,
                        Amount = product.Price,
                        Date = DateTime.UtcNow,
                        ProductId = product.Id,
                        StoreId = order.StoreId,
                        Win = true,
                        Bin = true,
                    });
                }
                if (product.ProductType == ProductType.Auction && _orderSettings.UnpublishAuctionProduct)
                {
                    await _productService.UnpublishProduct(product.Id);
                }

                //inventory
                await _productService.AdjustInventory(product, -sc.Quantity, sc.AttributesXml, warehouseId);
            }

            //insert order
            await _orderService.InsertOrder(order);
            
            var reserved = await _productReservationService.GetCustomerReservationsHelpers();
            foreach (var res in reserved)
            {
                await _productReservationService.DeleteCustomerReservationsHelper(res);
            }

            foreach (var resToUpdate in reservationsToUpdate)
            {
                resToUpdate.OrderId = order.Id;
                await _productReservationService.UpdateProductReservation(resToUpdate);
            }

            foreach (var bid in bidsToUpdate)
            {
                bid.OrderId = order.Id;
                await _auctionService.UpdateBid(bid);
            }

            //clear shopping cart
            //await _customerService.ClearShoppingCartItem(order.CustomerId, details.Cart);

            //product also purchased
            await _orderService.InsertProductAlsoPurchased(order);

            if (!details.Customer.HasContributions)
            {
                await _customerService.UpdateContributions(details.Customer);
            }

            //discount usage history
            foreach (var discount in details.AppliedDiscounts)
            {
                var duh = new DiscountUsageHistory {
                    DiscountId = discount.DiscountId,
                    CouponCode = discount.CouponCode,
                    OrderId = order.Id,
                    CustomerId = order.CustomerId,
                    CreatedOnUtc = DateTime.UtcNow
                };
                await _discountService.InsertDiscountUsageHistory(duh);
            }

            //gift card usage history
            if (details.AppliedGiftCards != null)
                foreach (var agc in details.AppliedGiftCards)
                {
                    decimal amountUsed = agc.AmountCanBeUsed;
                    var gcuh = new GiftCardUsageHistory {
                        GiftCardId = agc.GiftCard.Id,
                        UsedWithOrderId = order.Id,
                        UsedValue = amountUsed,
                        CreatedOnUtc = DateTime.UtcNow
                    };
                    agc.GiftCard.GiftCardUsageHistory.Add(gcuh);
                    await _giftCardService.UpdateGiftCard(agc.GiftCard);
                }

            //reset checkout data
            await _customerService.ResetCheckoutData(details.Customer, order.StoreId, clearCouponCodes: true, clearCheckoutAttributes: true);
            await _customerActivityService.InsertActivity("PublicStore.PlaceOrder", "", _localizationService.GetResource("ActivityLog.PublicStore.PlaceOrder"), order.Id);

            return order;
        }

        protected virtual async Task<Order> SaveOrderDetailsForReccuringPayment(PlaceOrderContainter details, Order order)
        {
            #region recurring payment

            var initialOrderItems = details.InitialOrder.OrderItems;
            foreach (var orderItem in initialOrderItems)
            {

                //save item
                var newOrderItem = new OrderItem {

                    OrderItemGuid = Guid.NewGuid(),
                    ProductId = orderItem.ProductId,
                    VendorId = orderItem.VendorId,
                    WarehouseId = orderItem.WarehouseId,
                    UnitPriceWithoutDiscInclTax = orderItem.UnitPriceWithoutDiscInclTax,
                    UnitPriceWithoutDiscExclTax = orderItem.UnitPriceWithoutDiscExclTax,
                    UnitPriceInclTax = orderItem.UnitPriceInclTax,
                    UnitPriceExclTax = orderItem.UnitPriceExclTax,
                    PriceInclTax = orderItem.PriceInclTax,
                    PriceExclTax = orderItem.PriceExclTax,
                    OriginalProductCost = orderItem.OriginalProductCost,
                    AttributeDescription = orderItem.AttributeDescription,
                    AttributesXml = orderItem.AttributesXml,
                    Quantity = orderItem.Quantity,
                    DiscountAmountInclTax = orderItem.DiscountAmountInclTax,
                    DiscountAmountExclTax = orderItem.DiscountAmountExclTax,
                    DownloadCount = 0,
                    IsDownloadActivated = false,
                    LicenseDownloadId = "",
                    ItemWeight = orderItem.ItemWeight,
                    RentalStartDateUtc = orderItem.RentalStartDateUtc,
                    RentalEndDateUtc = orderItem.RentalEndDateUtc,
                    CreatedOnUtc = DateTime.UtcNow,
                };
                order.OrderItems.Add(newOrderItem);

                //gift cards
                var product = await _productService.GetProductById(orderItem.ProductId);
                if (product.IsGiftCard)
                {
                    _productAttributeParser.GetGiftCardAttribute(orderItem.AttributesXml,
                        out string giftCardRecipientName, out string giftCardRecipientEmail,
                        out string giftCardSenderName, out string giftCardSenderEmail, out string giftCardMessage);

                    for (int i = 0; i < orderItem.Quantity; i++)
                    {
                        var gc = new GiftCard {
                            GiftCardType = product.GiftCardType,
                            PurchasedWithOrderItem = newOrderItem,
                            Amount = orderItem.UnitPriceExclTax,
                            IsGiftCardActivated = false,
                            GiftCardCouponCode = _giftCardService.GenerateGiftCardCode(),
                            RecipientName = giftCardRecipientName,
                            RecipientEmail = giftCardRecipientEmail,
                            SenderName = giftCardSenderName,
                            SenderEmail = giftCardSenderEmail,
                            Message = giftCardMessage,
                            IsRecipientNotified = false,
                            CreatedOnUtc = DateTime.UtcNow
                        };
                        await _giftCardService.InsertGiftCard(gc);
                    }
                }

                //inventory
                await _productService.AdjustInventory(product, -orderItem.Quantity, orderItem.AttributesXml, orderItem.WarehouseId);
            }

            //insert order
            await _orderService.InsertOrder(order);

            return order;

            #endregion
        }

        protected virtual async Task UpdateCustomer(Order order)
        {
            //Updated field "free shipping" after added a new order
            await _customerService.UpdateFreeShipping(order.CustomerId, false);

            //Update customer reminder history
            await _customerService.UpdateCustomerReminderHistory(order.CustomerId, order.Id);

            //Update field Last purchase date after added a new order
            await _customerService.UpdateCustomerLastPurchaseDate(order.CustomerId, order.CreatedOnUtc);

            //Update field last update cart
            await _customerService.UpdateCustomerLastUpdateCartDate(order.CustomerId, null);
        }

        protected virtual async Task CreateRecurringPayment(ProcessPaymentRequest processPaymentRequest, Order order)
        {
            var rp = new RecurringPayment {
                CycleLength = processPaymentRequest.RecurringCycleLength,
                CyclePeriod = processPaymentRequest.RecurringCyclePeriod,
                TotalCycles = processPaymentRequest.RecurringTotalCycles,
                StartDateUtc = DateTime.UtcNow,
                IsActive = true,
                CreatedOnUtc = DateTime.UtcNow,
                InitialOrder = order,
            };
            await _orderService.InsertRecurringPayment(rp);


            var recurringPaymentType = _paymentService.GetRecurringPaymentType(processPaymentRequest.PaymentMethodSystemName);
            switch (recurringPaymentType)
            {
                case RecurringPaymentType.NotSupported:
                    {
                        //not supported
                    }
                    break;
                case RecurringPaymentType.Manual:
                    {
                        //first payment
                        var rph = new RecurringPaymentHistory {
                            CreatedOnUtc = DateTime.UtcNow,
                            OrderId = order.Id,
                            RecurringPaymentId = rp.Id
                        };
                        rp.RecurringPaymentHistory.Add(rph);
                        await _orderService.UpdateRecurringPayment(rp);
                    }
                    break;
                case RecurringPaymentType.Automatic:
                    {
                        //will be created later (process is automated)
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Award reward points
        /// </summary>
        /// <param name="order">Order</param>
        protected virtual async Task AwardRewardPoints(Order order)
        {
            var customer = await _customerService.GetCustomerById(order.CustomerId);

            int points = _orderTotalCalculationService.CalculateRewardPoints(customer, order.OrderTotal - order.OrderShippingInclTax);
            if (points <= 0)
                return;

            //Ensure that reward points were not added before. We should not add reward points if they were already earned for this order
            if (order.RewardPointsWereAdded)
                return;

            //add reward points
            await _rewardPointsService.AddRewardPointsHistory(customer.Id, points, order.StoreId, string.Format(_localizationService.GetResource("RewardPoints.Message.EarnedForOrder"), order.OrderNumber));

        }

        /// <summary>
        /// Award reward points
        /// </summary>
        /// <param name="order">Order</param>
        protected virtual async Task ReduceRewardPoints(Order order)
        {
            var customer = await _customerService.GetCustomerById(order.CustomerId);
            int points = _orderTotalCalculationService.CalculateRewardPoints(customer, order.OrderTotal - order.OrderShippingInclTax);
            if (points <= 0)
                return;

            //ensure that reward points were already earned for this order before
            if (!order.RewardPointsWereAdded)
                return;

            //reduce reward points
            await _rewardPointsService.AddRewardPointsHistory(customer.Id, -points, order.StoreId,
                string.Format(_localizationService.GetResource("RewardPoints.Message.ReducedForOrder"), order.OrderNumber));

            await _orderService.UpdateOrder(order);
        }

        /// <summary>
        /// Return back redeemded reward points to a customer (spent when placing an order)
        /// </summary>
        /// <param name="order">Order</param>
        protected virtual async Task ReturnBackRedeemedRewardPoints(Order order)
        {
            //were some points redeemed when placing an order?
            if (order.RedeemedRewardPointsEntry == null)
                return;

            //return back
            await _rewardPointsService.AddRewardPointsHistory(order.CustomerId, -order.RedeemedRewardPointsEntry.Points, order.StoreId,
                string.Format(_localizationService.GetResource("RewardPoints.Message.ReturnedForOrder"), order.OrderNumber));

            await _orderService.UpdateOrder(order);
        }

        /// <summary>
        /// Set IsActivated value for purchase gift cards for particular order
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="activate">A value indicating whether to activate gift cards; true - activate, false - deactivate</param>
        protected virtual async Task SetActivatedValueForPurchasedGiftCards(Order order, bool activate)
        {
            foreach (var orderItem in order.OrderItems)
            {
                var giftCards = await _giftCardService.GetAllGiftCards(purchasedWithOrderItemId: orderItem.Id,
                    isGiftCardActivated: !activate);
                foreach (var gc in giftCards)
                {
                    if (activate)
                    {
                        //activate
                        bool isRecipientNotified = gc.IsRecipientNotified;
                        if (gc.GiftCardType == GiftCardType.Virtual)
                        {
                            //send email for virtual gift card
                            if (!String.IsNullOrEmpty(gc.RecipientEmail) &&
                                !String.IsNullOrEmpty(gc.SenderEmail))
                            {
                                var customerLang = await _languageService.GetLanguageById(order.CustomerLanguageId);
                                if (customerLang == null)
                                    customerLang = (await _languageService.GetAllLanguages()).FirstOrDefault();
                                if (customerLang == null)
                                    throw new Exception("No languages could be loaded");
                                int queuedEmailId = await _workflowMessageService.SendGiftCardNotification(gc, customerLang.Id);
                                if (queuedEmailId > 0)
                                    isRecipientNotified = true;
                            }
                        }
                        gc.IsGiftCardActivated = true;
                        gc.IsRecipientNotified = isRecipientNotified;
                        await _giftCardService.UpdateGiftCard(gc);
                    }
                    else
                    {
                        //deactivate
                        gc.IsGiftCardActivated = false;
                        await _giftCardService.UpdateGiftCard(gc);
                    }
                }
            }

        }

        /// <summary>
        /// Sets an order status
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="os">New order status</param>
        /// <param name="notifyCustomer">True to notify customer</param>
        protected virtual async Task SetOrderStatus(Order order, OrderStatus os, bool notifyCustomer, bool notifyStoreOwner)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            OrderStatus prevOrderStatus = order.OrderStatus;
            if (prevOrderStatus == os)
                return;

            //set and save new order status
            order.OrderStatusId = (int)os;
            await _orderService.UpdateOrder(order);

            //order notes, notifications
            await _orderService.InsertOrderNote(new OrderNote {
                Note = string.Format("Order status has been changed to {0}", os.ToString()),
                DisplayToCustomer = false,
                OrderId = order.Id,
                CreatedOnUtc = DateTime.UtcNow
            });

            if (prevOrderStatus != OrderStatus.Complete &&
                os == OrderStatus.Complete
                && notifyCustomer)
            {
                //notification
                var orderCompletedAttachmentFilePath = _orderSettings.AttachPdfInvoiceToOrderCompletedEmail ?
                    await _pdfService.PrintOrderToPdf(order, "") : null;
                var orderCompletedAttachmentFileName = _orderSettings.AttachPdfInvoiceToOrderCompletedEmail ?
                    "order.pdf" : null;

                var orderCompletedAttachments = _orderSettings.AttachPdfInvoiceToOrderCompletedEmail && _orderSettings.AttachPdfInvoiceToBinary ?
                    new List<string> { await _pdfService.SaveOrderToBinary(order, "") } : new List<string>();

                int orderCompletedCustomerNotificationQueuedEmailId = await _workflowMessageService
                    .SendOrderCompletedCustomerNotification(order, order.CustomerLanguageId, orderCompletedAttachmentFilePath,
                    orderCompletedAttachmentFileName, orderCompletedAttachments);
                if (orderCompletedCustomerNotificationQueuedEmailId > 0)
                {
                    await _orderService.InsertOrderNote(new OrderNote {
                        Note = "\"Order completed\" email (to customer) has been queued.",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,
                    });
                }
            }

            if (prevOrderStatus != OrderStatus.Cancelled &&
                os == OrderStatus.Cancelled
                && notifyCustomer)
            {
                //notification customer
                int orderCancelledCustomerNotificationQueuedEmailId = await _workflowMessageService.SendOrderCancelledCustomerNotification(order, order.CustomerLanguageId);
                if (orderCancelledCustomerNotificationQueuedEmailId > 0)
                {
                    await _orderService.InsertOrderNote(new OrderNote {
                        Note = "\"Order cancelled\" email (to customer) has been queued.",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,
                    });
                }
            }

            if (prevOrderStatus != OrderStatus.Cancelled &&
                os == OrderStatus.Cancelled
                && notifyStoreOwner)
            {
                //notification store owner
                int orderCancelledStoreOwnerNotificationQueuedEmailId = await _workflowMessageService.SendOrderCancelledStoreOwnerNotification(order, order.CustomerLanguageId);
                if (orderCancelledStoreOwnerNotificationQueuedEmailId > 0)
                {
                    await _orderService.InsertOrderNote(new OrderNote {
                        Note = "\"Order cancelled\" by customer.",
                        DisplayToCustomer = true,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,
                    });
                }
            }

            //reward points
            if (_rewardPointsSettings.PointsForPurchases_Awarded == order.OrderStatus)
            {
                await AwardRewardPoints(order);
            }
            if (_rewardPointsSettings.PointsForPurchases_Canceled == order.OrderStatus)
            {
                await ReduceRewardPoints(order);
            }

            //gift cards activation
            if (_orderSettings.GiftCards_Activated_OrderStatusId > 0 &&
               _orderSettings.GiftCards_Activated_OrderStatusId == (int)order.OrderStatus)
            {
                await SetActivatedValueForPurchasedGiftCards(order, true);
            }

            //gift cards deactivation
            if (_orderSettings.GiftCards_Deactivated_OrderStatusId > 0 &&
               _orderSettings.GiftCards_Deactivated_OrderStatusId == (int)order.OrderStatus)
            {
                await SetActivatedValueForPurchasedGiftCards(order, false);
            }
        }

        /// <summary>
        /// Process order paid status
        /// </summary>
        /// <param name="order">Order</param>
        protected virtual async Task ProcessOrderPaid(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //raise event
            await _mediator.Publish(new OrderPaidEvent(order));

            //order paid email notification
            if (order.OrderTotal != decimal.Zero)
            {
                //we should not send it for free ($0 total) orders?
                //remove this "if" statement if you want to send it in this case

                var orderPaidAttachmentFilePath = _orderSettings.AttachPdfInvoiceToOrderPaidEmail && !_orderSettings.AttachPdfInvoiceToBinary ?
                    await _pdfService.PrintOrderToPdf(order, "")
                    : null;
                var orderPaidAttachmentFileName = _orderSettings.AttachPdfInvoiceToOrderPaidEmail && !_orderSettings.AttachPdfInvoiceToBinary ?
                    "order.pdf" : null;

                var orderPaidAttachments = _orderSettings.AttachPdfInvoiceToOrderPaidEmail && _orderSettings.AttachPdfInvoiceToBinary ?
                    new List<string> { await _pdfService.SaveOrderToBinary(order, "") } : new List<string>();

                await _workflowMessageService.SendOrderPaidCustomerNotification(order, order.CustomerLanguageId,
                    orderPaidAttachmentFilePath, orderPaidAttachmentFileName, orderPaidAttachments);

                await _workflowMessageService.SendOrderPaidStoreOwnerNotification(order, _localizationSettings.DefaultAdminLanguageId);
                var vendors = await GetVendorsInOrder(order);
                foreach (var vendor in vendors)
                {
                    await _workflowMessageService.SendOrderPaidVendorNotification(order, vendor, _localizationSettings.DefaultAdminLanguageId);
                }
                //TODO add "order paid email sent" order note
            }

            //customer roles with "purchased with product" specified
            await ProcessCustomerRolesWithPurchasedProductSpecified(order, true);
        }

        /// <summary>
        /// Process customer roles with "Purchased with Product" property configured
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="add">A value indicating whether to add configured customer role; true - add, false - remove</param>
        protected virtual async Task ProcessCustomerRolesWithPurchasedProductSpecified(Order order, bool add)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //purchased product identifiers
            var purchasedProductIds = new List<string>();
            foreach (var orderItem in order.OrderItems)
            {
                //standard items
                purchasedProductIds.Add(orderItem.ProductId);

                //bundled (associated) products
                var product = await _productService.GetProductById(orderItem.ProductId);
                var attributeValues = _productAttributeParser.ParseProductAttributeValues(product, orderItem.AttributesXml);
                foreach (var attributeValue in attributeValues)
                {
                    if (attributeValue.AttributeValueType == AttributeValueType.AssociatedToProduct)
                    {
                        purchasedProductIds.Add(attributeValue.AssociatedProductId);
                    }
                }
            }

            //list of customer roles
            var customerRoles = (await _customerService
                .GetAllCustomerRoles(true))
                .Where(cr => purchasedProductIds.Contains(cr.PurchasedWithProductId))
                .ToList();

            if (customerRoles.Any())
            {
                var customer = await _customerService.GetCustomerById(order.CustomerId);
                foreach (var customerRole in customerRoles)
                {
                    if (customer.CustomerRoles.Count(cr => cr.Id == customerRole.Id) == 0)
                    {
                        //not in the list yet
                        if (add)
                        {
                            //add
                            customerRole.CustomerId = customer.Id;
                            customer.CustomerRoles.Add(customerRole);
                            await _customerService.InsertCustomerRoleInCustomer(customerRole);
                        }
                    }
                    else
                    {
                        //already in the list
                        if (!add)
                        {
                            //remove
                            customer.CustomerRoles.Remove(customerRole);
                            customerRole.CustomerId = customer.Id;
                            await _customerService.InsertCustomerRoleInCustomer(customerRole);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get a list of vendors in order (order items)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Vendors</returns>
        protected virtual async Task<IList<Vendor>> GetVendorsInOrder(Order order)
        {
            var vendors = new List<Vendor>();
            foreach (var orderItem in order.OrderItems)
            {
                //find existing
                var vendor = vendors.FirstOrDefault(v => v.Id == orderItem.VendorId);
                if (vendor == null)
                {
                    //not found. load by Id
                    vendor = await _vendorService.GetVendorById(orderItem.VendorId);
                    if (vendor != null && !vendor.Deleted && vendor.Active)
                    {
                        vendors.Add(vendor);
                    }
                }
            }

            return vendors;
        }

        #endregion

        #region Methods
        /// <summary>
        /// Send notification order 
        /// </summary>
        /// <param name="order">Order</param>
        public virtual async Task SendNotification(Order order)
        {
            //notes, messages
            if (_workContext.OriginalCustomerIfImpersonated != null)
            {
                //this order is placed by a store administrator impersonating a customer
                await _orderService.InsertOrderNote(new OrderNote {
                    Note = string.Format("Order placed by a store owner ('{0}'. ID = {1}) impersonating the customer.",
                        _workContext.OriginalCustomerIfImpersonated.Email, _workContext.OriginalCustomerIfImpersonated.Id),
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,
                });
            }
            else
            {
                await _orderService.InsertOrderNote(new OrderNote {
                    Note = "Order placed",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,

                });
            }

            //send email notifications
            int orderPlacedStoreOwnerNotificationQueuedEmailId = await _workflowMessageService.SendOrderPlacedStoreOwnerNotification(order, _localizationSettings.DefaultAdminLanguageId);
            if (orderPlacedStoreOwnerNotificationQueuedEmailId > 0)
            {
                await _orderService.InsertOrderNote(new OrderNote {
                    Note = "\"Order placed\" email (to store owner) has been queued",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,

                });
            }

            var orderPlacedAttachmentFilePath = _orderSettings.AttachPdfInvoiceToOrderPlacedEmail && !_orderSettings.AttachPdfInvoiceToBinary ?
                await _pdfService.PrintOrderToPdf(order, order.CustomerLanguageId) : null;
            var orderPlacedAttachmentFileName = _orderSettings.AttachPdfInvoiceToOrderPlacedEmail && !_orderSettings.AttachPdfInvoiceToBinary ?
                "order.pdf" : null;
            var orderPlacedAttachments = _orderSettings.AttachPdfInvoiceToOrderPlacedEmail && _orderSettings.AttachPdfInvoiceToBinary ?
                new List<string> { await _pdfService.SaveOrderToBinary(order, order.CustomerLanguageId) } : new List<string>();

            int orderPlacedCustomerNotificationQueuedEmailId = await _workflowMessageService
                .SendOrderPlacedCustomerNotification(order, order.CustomerLanguageId, orderPlacedAttachmentFilePath, orderPlacedAttachmentFileName, orderPlacedAttachments);
            if (orderPlacedCustomerNotificationQueuedEmailId > 0)
            {
                await _orderService.InsertOrderNote(new OrderNote {
                    Note = "\"Order placed\" email (to customer) has been queued",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,

                });
            }

            var vendors = await GetVendorsInOrder(order);
            foreach (var vendor in vendors)
            {
                int orderPlacedVendorNotificationQueuedEmailId = await _workflowMessageService.SendOrderPlacedVendorNotification(order, vendor, _localizationSettings.DefaultAdminLanguageId);
                if (orderPlacedVendorNotificationQueuedEmailId > 0)
                {
                    await _orderService.InsertOrderNote(new OrderNote {
                        Note = "\"Order placed\" email (to vendor) has been queued",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,
                    });
                }
            }
        }
        /// <summary>
        /// Checks order status
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Validated order</returns>
        public virtual async Task CheckOrderStatus(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (order.PaymentStatus == PaymentStatus.Paid && !order.PaidDateUtc.HasValue)
            {
                //ensure that paid date is set
                order.PaidDateUtc = DateTime.UtcNow;
                await _orderService.UpdateOrder(order);
            }

            if (order.OrderStatus == OrderStatus.Pending)
            {
                if (order.PaymentStatus == PaymentStatus.Authorized ||
                    order.PaymentStatus == PaymentStatus.Paid)
                {
                    await SetOrderStatus(order, OrderStatus.Processing, false, false);
                }

                if (order.ShippingStatus == ShippingStatus.PartiallyShipped ||
                    order.ShippingStatus == ShippingStatus.Shipped ||
                    order.ShippingStatus == ShippingStatus.Delivered)
                {
                    await SetOrderStatus(order, OrderStatus.Processing, false, false);
                }
            }

            if (order.OrderStatus != OrderStatus.Cancelled &&
                order.OrderStatus != OrderStatus.Complete)
            {
                if (order.PaymentStatus == PaymentStatus.Paid)
                {
                    var completed = false;
                    if (order.ShippingStatus == ShippingStatus.ShippingNotRequired)
                    {
                        completed = true;
                    }
                    else
                    {
                        if (_orderSettings.CompleteOrderWhenDelivered)
                        {
                            completed = order.ShippingStatus == ShippingStatus.Delivered;
                        }
                        else
                        {
                            completed = order.ShippingStatus == ShippingStatus.Shipped ||
                                order.ShippingStatus == ShippingStatus.Delivered;
                        }
                    }
                    if (completed)
                    {
                        await SetOrderStatus(order, OrderStatus.Complete, true, false);
                    }
                }
            }
        }

        /// <summary>
        /// Places an order
        /// </summary>
        /// <param name="processPaymentRequest">Process payment request</param>
        /// <returns>Place order result</returns>
        public virtual async Task<PlaceOrderResult> PlaceOrder(ProcessPaymentRequest processPaymentRequest)
        {
            if (processPaymentRequest == null)
                throw new ArgumentNullException("processPaymentRequest");

            var result = new PlaceOrderResult();
            try
            {
                if (processPaymentRequest.OrderGuid == Guid.Empty)
                    processPaymentRequest.OrderGuid = Guid.NewGuid();

                //prepare order details
                var details = !processPaymentRequest.IsRecurringPayment ? await PreparePlaceOrderDetails(processPaymentRequest) : await PreparePlaceOrderDetailsForRecurringPayment(processPaymentRequest);

                //event notification
                await _mediator.PlaceOrderDetailsEvent(result, details);

                //return if exist errors
                if (result.Errors.Any())
                    return result;

                #region Payment workflow

                var processPaymentResult = await PrepareProcessPaymentResult(processPaymentRequest, details);

                #endregion

                if (processPaymentResult.Success)
                {
                    #region Save order details

                    var order = await PrepareOrder(processPaymentRequest, processPaymentResult, details);

                    if (!processPaymentRequest.IsRecurringPayment)
                    {
                        result.PlacedOrder = await SaveOrderDetails(details, order);
                    }
                    else
                    {
                        result.PlacedOrder = await SaveOrderDetailsForReccuringPayment(details, order);
                        
                    }
                    //recurring orders
                    if (details.IsRecurringShoppingCart && !processPaymentRequest.IsRecurringPayment)
                    {
                        //create recurring payment (the first payment)
                        await CreateRecurringPayment(processPaymentRequest, order);
                    }
                    //reward points history
                    if (details.RedeemedRewardPointsAmount > decimal.Zero)
                    {
                        var rph = await _rewardPointsService.AddRewardPointsHistory(details.Customer.Id,
                            -details.RedeemedRewardPoints, order.StoreId,
                            string.Format(_localizationService.GetResource("RewardPoints.Message.RedeemedForOrder", order.CustomerLanguageId), order.OrderNumber),
                            order.Id, details.RedeemedRewardPointsAmount);
                        order.RewardPointsWereAdded = true;
                        order.RedeemedRewardPointsEntry = rph;
                        await _orderService.UpdateOrder(order);
                    }

                    #endregion

                    #region Notifications & notes

                    await SendNotification(order);

                    //check order status
                    await CheckOrderStatus(order);

                    //update customer 
                    await UpdateCustomer(order);

                    //raise event       
                    await _mediator.Publish(new OrderPlacedEvent(order));

                    await _customerActionEventService.AddOrder(order, _workContext.CurrentCustomer);

                    if (order.PaymentStatus == PaymentStatus.Paid)
                    {
                        await ProcessOrderPaid(order);
                    }

                    #endregion
                }
                else
                {
                    foreach (var paymentError in processPaymentResult.Errors)
                        result.AddError(string.Format(_localizationService.GetResource("Checkout.PaymentError"), paymentError));
                }
            }
            catch (Exception exc)
            {
                _logger.Error(exc.Message, exc);
                result.AddError(exc.Message);
            }

            #region Process errors

            string error = "";
            for (int i = 0; i < result.Errors.Count; i++)
            {
                error += string.Format("Error {0}: {1}", i + 1, result.Errors[i]);
                if (i != result.Errors.Count - 1)
                    error += ". ";
            }
            if (!string.IsNullOrEmpty(error))
            {
                //log it
                string logError = string.Format("Error while placing order. {0}", error);
                var customer = await _customerService.GetCustomerById(processPaymentRequest.CustomerId);
                _logger.Error(logError, customer: customer);
            }

            #endregion

            return result;
        }

        /// <summary>
        /// Deletes an order
        /// </summary>
        /// <param name="order">The order</param>
        public virtual async Task DeleteOrder(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //check whether the order wasn't cancelled before
            //if it already was cancelled, then there's no need to make the following adjustments
            //(such as reward points, inventory, recurring payments)
            //they already was done when cancelling the order
            if (order.OrderStatus != OrderStatus.Cancelled)
            {
                //return (add) back redeemded reward points
                await ReturnBackRedeemedRewardPoints(order);
                //reduce (cancel) back reward points (previously awarded for this order)
                await ReduceRewardPoints(order);

                //cancel recurring payments
                var recurringPayments = await _orderService.SearchRecurringPayments(initialOrderId: order.Id);
                foreach (var rp in recurringPayments)
                {
                    var errors = CancelRecurringPayment(rp);
                    //use "errors" variable?
                }

                //Adjust inventory for already shipped shipments
                //only products with "use multiple warehouses"
                foreach (var shipment in await _shipmentService.GetShipmentsByOrder(order.Id))
                {
                    foreach (var shipmentItem in shipment.ShipmentItems)
                    {
                        var product = await _productService.GetProductById(shipmentItem.ProductId);
                        shipmentItem.ShipmentId = shipment.Id;
                        if (product != null)
                            await _productService.ReverseBookedInventory(product, shipmentItem);
                    }
                }
                //Adjust inventory
                foreach (var orderItem in order.OrderItems)
                {
                    var product = await _productService.GetProductById(orderItem.ProductId);
                    if (product != null)
                        await _productService.AdjustInventory(product, orderItem.Quantity, orderItem.AttributesXml, orderItem.WarehouseId);
                }

                //cancel reservations
                await _productReservationService.CancelReservationsByOrderId(order.Id);

                //cancel bid
                await _auctionService.CancelBidByOrder(order.Id);
            }
            //deactivate gift cards
            if (_orderSettings.DeactivateGiftCardsAfterDeletingOrder)
                await SetActivatedValueForPurchasedGiftCards(order, false);

            order.Deleted = true;
            //now delete an order
            await _orderService.UpdateOrder(order);

            //cancel discounts 
            await _discountService.CancelDiscount(order.Id);
        }

        /// <summary>
        /// Process next recurring psayment
        /// </summary>
        /// <param name="recurringPayment">Recurring payment</param>
        public virtual async Task ProcessNextRecurringPayment(RecurringPayment recurringPayment)
        {
            if (recurringPayment == null)
                throw new ArgumentNullException("recurringPayment");
            try
            {
                if (!recurringPayment.IsActive)
                    throw new GrandException("Recurring payment is not active");

                var initialOrder = recurringPayment.InitialOrder;
                if (initialOrder == null)
                    throw new GrandException("Initial order could not be loaded");

                var customer = await _customerService.GetCustomerById(initialOrder.CustomerId);
                if (customer == null)
                    throw new GrandException("Customer could not be loaded");

                var nextPaymentDate = recurringPayment.NextPaymentDate;
                if (!nextPaymentDate.HasValue)
                    throw new GrandException("Next payment date could not be calculated");

                //payment info
                var paymentInfo = new ProcessPaymentRequest {
                    StoreId = initialOrder.StoreId,
                    CustomerId = customer.Id,
                    OrderGuid = Guid.NewGuid(),
                    IsRecurringPayment = true,
                    InitialOrderId = initialOrder.Id,
                    RecurringCycleLength = recurringPayment.CycleLength,
                    RecurringCyclePeriod = recurringPayment.CyclePeriod,
                    RecurringTotalCycles = recurringPayment.TotalCycles,
                };

                //place a new order
                var result = await PlaceOrder(paymentInfo);
                if (result.Success)
                {
                    if (result.PlacedOrder == null)
                        throw new GrandException("Placed order could not be loaded");

                    var rph = new RecurringPaymentHistory {
                        RecurringPaymentId = recurringPayment.Id,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = result.PlacedOrder.Id,
                    };
                    recurringPayment.RecurringPaymentHistory.Add(rph);
                    await _orderService.UpdateRecurringPayment(recurringPayment);
                }
                else
                {
                    string error = "";
                    for (int i = 0; i < result.Errors.Count; i++)
                    {
                        error += string.Format("Error {0}: {1}", i, result.Errors[i]);
                        if (i != result.Errors.Count - 1)
                            error += ". ";
                    }
                    throw new GrandException(error);
                }
            }
            catch (Exception exc)
            {
                _logger.Error(string.Format("Error while processing recurring order. {0}", exc.Message), exc);
                throw;
            }
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="recurringPayment">Recurring payment</param>
        public virtual async Task<IList<string>> CancelRecurringPayment(RecurringPayment recurringPayment)
        {
            if (recurringPayment == null)
                throw new ArgumentNullException("recurringPayment");

            var initialOrder = recurringPayment.InitialOrder;
            if (initialOrder == null)
                return new List<string> { "Initial order could not be loaded" };


            var request = new CancelRecurringPaymentRequest();
            CancelRecurringPaymentResult result = null;
            try
            {
                request.Order = initialOrder;
                result = await _paymentService.CancelRecurringPayment(request);
                if (result.Success)
                {
                    //update recurring payment
                    recurringPayment.IsActive = false;
                    await _orderService.UpdateRecurringPayment(recurringPayment);

                    //add a note
                    await _orderService.InsertOrderNote(new OrderNote {
                        Note = "Recurring payment has been cancelled",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = initialOrder.Id,

                    });

                    //notify a store owner
                    await _workflowMessageService
                        .SendRecurringPaymentCancelledStoreOwnerNotification(recurringPayment,
                        _localizationSettings.DefaultAdminLanguageId);
                }
            }
            catch (Exception exc)
            {
                if (result == null)
                    result = new CancelRecurringPaymentResult();
                result.AddError(string.Format("Error: {0}. Full exception: {1}", exc.Message, exc));
            }


            //process errors
            string error = "";
            for (int i = 0; i < result.Errors.Count; i++)
            {
                error += string.Format("Error {0}: {1}", i, result.Errors[i]);
                if (i != result.Errors.Count - 1)
                    error += ". ";
            }
            if (!String.IsNullOrEmpty(error))
            {
                //add a note
                await _orderService.InsertOrderNote(new OrderNote {
                    Note = string.Format("Unable to cancel recurring payment. {0}", error),
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = initialOrder.Id,

                });

                //log it
                string logError = string.Format("Error cancelling recurring payment. Order #{0}. Error: {1}", initialOrder.Id, error);
                await _logger.InsertLog(LogLevel.Error, logError, logError);
            }
            return result.Errors;
        }

        /// <summary>
        /// Gets a value indicating whether a customer can cancel recurring payment
        /// </summary>
        /// <param name="customerToValidate">Customer</param>
        /// <param name="recurringPayment">Recurring Payment</param>
        /// <returns>value indicating whether a customer can cancel recurring payment</returns>
        public virtual async Task<bool> CanCancelRecurringPayment(Customer customerToValidate, RecurringPayment recurringPayment)
        {
            if (recurringPayment == null)
                return false;

            if (customerToValidate == null)
                return false;

            var initialOrder = recurringPayment.InitialOrder;
            if (initialOrder == null)
                return false;

            var customer = await _customerService.GetCustomerById(recurringPayment.InitialOrder.CustomerId);
            if (customer == null)
                return false;

            if (initialOrder.OrderStatus == OrderStatus.Cancelled)
                return false;

            if (!customerToValidate.IsAdmin())
            {
                if (customer.Id != customerToValidate.Id)
                    return false;
            }

            if (!recurringPayment.NextPaymentDate.HasValue)
                return false;

            return true;
        }



        /// <summary>
        /// Send a shipment
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <param name="notifyCustomer">True to notify customer</param>
        public virtual async Task Ship(Shipment shipment, bool notifyCustomer)
        {
            if (shipment == null)
                throw new ArgumentNullException("shipment");

            var order = await _orderService.GetOrderById(shipment.OrderId);
            if (order == null)
                throw new Exception("Order cannot be loaded");

            if (shipment.ShippedDateUtc.HasValue)
                throw new Exception("This shipment is already shipped");

            shipment.ShippedDateUtc = DateTime.UtcNow;
            await _shipmentService.UpdateShipment(shipment);

            //process products with "Multiple warehouse" support enabled
            foreach (var item in shipment.ShipmentItems)
            {
                var orderItem = order.OrderItems.Where(x => x.Id == item.OrderItemId).FirstOrDefault();
                var product = await _productService.GetProductByIdIncludeArch(orderItem.ProductId);
                await _productService.BookReservedInventory(product, item.AttributeXML, item.WarehouseId, -item.Quantity);
            }

            //check whether we have more items to ship
            if (await order.HasItemsToAddToShipment(_orderService, _shipmentService, _productService) || await order.HasItemsToShip(_orderService, _shipmentService, _productService))
                order.ShippingStatusId = (int)ShippingStatus.PartiallyShipped;
            else
                order.ShippingStatusId = (int)ShippingStatus.Shipped;

            await _orderService.UpdateOrder(order);

            //add a note
            await _orderService.InsertOrderNote(new OrderNote {
                Note = $"Shipment #{shipment.ShipmentNumber} has been sent",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
            });

            if (notifyCustomer)
            {
                //notify customer
                int queuedEmailId = await _workflowMessageService.SendShipmentSentCustomerNotification(shipment, order.CustomerLanguageId);
                if (queuedEmailId > 0)
                {
                    await _orderService.InsertOrderNote(new OrderNote {
                        Note = "\"Shipped\" email (to customer) has been queued.",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,
                    });
                }
            }

            //event
            await _mediator.PublishShipmentSent(shipment);

            //check order status
            await CheckOrderStatus(order);
        }

        /// <summary>
        /// Marks a shipment as delivered
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <param name="notifyCustomer">True to notify customer</param>
        public virtual async Task Deliver(Shipment shipment, bool notifyCustomer)
        {
            if (shipment == null)
                throw new ArgumentNullException("shipment");

            var order = await _serviceProvider.GetRequiredService<IOrderService>().GetOrderById(shipment.OrderId);
            if (order == null)
                throw new Exception("Order cannot be loaded");

            if (!shipment.ShippedDateUtc.HasValue)
                throw new Exception("This shipment is not shipped yet");

            if (shipment.DeliveryDateUtc.HasValue)
                throw new Exception("This shipment is already delivered");

            shipment.DeliveryDateUtc = DateTime.UtcNow;
            await _shipmentService.UpdateShipment(shipment);

            if (!await order.HasItemsToAddToShipment(_orderService, _shipmentService, _productService) && !await order.HasItemsToShip(_orderService, _shipmentService, _productService) && !await order.HasItemsToDeliver(_orderService, _shipmentService, _productService))
                order.ShippingStatusId = (int)ShippingStatus.Delivered;

            await _orderService.UpdateOrder(order);

            //add a note
            await _orderService.InsertOrderNote(new OrderNote {
                Note = $"Shipment #{shipment.ShipmentNumber} has been delivered",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
            });

            if (notifyCustomer)
            {
                //send email notification
                int queuedEmailId = await _workflowMessageService.SendShipmentDeliveredCustomerNotification(shipment, order.CustomerLanguageId);
                if (queuedEmailId > 0)
                {
                    await _orderService.InsertOrderNote(new OrderNote {
                        Note = "\"Delivered\" email (to customer) has been queued.",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,
                    });
                }
            }

            //event
            await _mediator.PublishShipmentDelivered(shipment);

            //check order status
            await CheckOrderStatus(order);
        }



        /// <summary>
        /// Gets a value indicating whether cancel is allowed
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>A value indicating whether cancel is allowed</returns>
        public virtual bool CanCancelOrder(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (order.OrderStatus == OrderStatus.Cancelled)
                return false;

            return true;
        }

        /// <summary>
        /// Cancels order
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="notifyCustomer">True to notify customer</param>
        public virtual async Task CancelOrder(Order order, bool notifyCustomer, bool notifyStoreOwner = false)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (!CanCancelOrder(order))
                throw new GrandException("Cannot do cancel for order.");

            //Cancel order
            await SetOrderStatus(order, OrderStatus.Cancelled, notifyCustomer, notifyStoreOwner);

            //add a note
            await _orderService.InsertOrderNote(new OrderNote {
                Note = "Order has been cancelled",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,

            });

            //return (add) back redeemded reward points
            await ReturnBackRedeemedRewardPoints(order);

            //cancel recurring payments
            var recurringPayments = await _orderService.SearchRecurringPayments(initialOrderId: order.Id);
            foreach (var rp in recurringPayments)
            {
                var errors = CancelRecurringPayment(rp);
                //use "errors" variable?
            }

            //Adjust inventory for already shipped shipments
            //only products with "use multiple warehouses"
            var shipments = await _serviceProvider.GetRequiredService<IShipmentService>().GetShipmentsByOrder(order.Id);
            foreach (var shipment in shipments)
            {
                foreach (var shipmentItem in shipment.ShipmentItems)
                {
                    var product = await _productService.GetProductById(shipmentItem.ProductId);
                    shipmentItem.ShipmentId = shipment.Id;
                    await _productService.ReverseBookedInventory(product, shipmentItem);
                }
            }
            //Adjust inventory
            foreach (var orderItem in order.OrderItems)
            {
                var product = await _productService.GetProductById(orderItem.ProductId);
                await _productService.AdjustInventory(product, orderItem.Quantity, orderItem.AttributesXml, orderItem.WarehouseId);
            }

            //cancel reservations
            await _productReservationService.CancelReservationsByOrderId(order.Id);

            //cancel bid
            await _auctionService.CancelBidByOrder(order.Id);

            //cancel discount
            await _discountService.CancelDiscount(order.Id);

            await _mediator.Publish(new OrderCancelledEvent(order));

        }

        /// <summary>
        /// Gets a value indicating whether order can be marked as authorized
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>A value indicating whether order can be marked as authorized</returns>
        public virtual bool CanMarkOrderAsAuthorized(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (order.OrderStatus == OrderStatus.Cancelled)
                return false;

            if (order.PaymentStatus == PaymentStatus.Pending)
                return true;

            return false;
        }

        /// <summary>
        /// Marks order as authorized
        /// </summary>
        /// <param name="order">Order</param>
        public virtual async Task MarkAsAuthorized(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            order.PaymentStatusId = (int)PaymentStatus.Authorized;
            await _orderService.UpdateOrder(order);

            //add a note
            await _orderService.InsertOrderNote(new OrderNote {
                Note = "Order has been marked as authorized",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
            });

            //check order status
            await CheckOrderStatus(order);
        }



        /// <summary>
        /// Gets a value indicating whether capture from admin panel is allowed
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>A value indicating whether capture from admin panel is allowed</returns>
        public virtual async Task<bool> CanCapture(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (order.OrderStatus == OrderStatus.Cancelled ||
                order.OrderStatus == OrderStatus.Pending)
                return false;

            if (order.PaymentStatus == PaymentStatus.Authorized &&
                await _paymentService.SupportCapture(order.PaymentMethodSystemName))
                return true;

            return false;
        }

        /// <summary>
        /// Capture an order (from admin panel)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>A list of errors; empty list if no errors</returns>
        public virtual async Task<IList<string>> Capture(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (!await CanCapture(order))
                throw new GrandException("Cannot do capture for order.");

            var request = new CapturePaymentRequest();
            CapturePaymentResult result = null;
            try
            {
                //old info from placing order
                request.Order = order;
                result = await _paymentService.Capture(request);

                if (result.Success)
                {
                    var paidDate = order.PaidDateUtc;
                    if (result.NewPaymentStatus == PaymentStatus.Paid)
                        paidDate = DateTime.UtcNow;

                    order.CaptureTransactionId = result.CaptureTransactionId;
                    order.CaptureTransactionResult = result.CaptureTransactionResult;
                    order.PaymentStatus = result.NewPaymentStatus;
                    order.PaidDateUtc = paidDate;
                    await _orderService.UpdateOrder(order);

                    //add a note
                    await _orderService.InsertOrderNote(new OrderNote {
                        Note = "Order has been captured",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,

                    });

                    await CheckOrderStatus(order);

                    if (order.PaymentStatus == PaymentStatus.Paid)
                    {
                        await ProcessOrderPaid(order);
                    }
                }
            }
            catch (Exception exc)
            {
                if (result == null)
                    result = new CapturePaymentResult();
                result.AddError(string.Format("Error: {0}. Full exception: {1}", exc.Message, exc));
            }


            //process errors
            string error = "";
            for (int i = 0; i < result.Errors.Count; i++)
            {
                error += string.Format("Error {0}: {1}", i, result.Errors[i]);
                if (i != result.Errors.Count - 1)
                    error += ". ";
            }
            if (!String.IsNullOrEmpty(error))
            {
                //add a note
                await _orderService.InsertOrderNote(new OrderNote {
                    Note = string.Format("Unable to capture order. {0}", error),
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,
                });

                //log it
                string logError = string.Format("Error capturing order #{0}. Error: {1}", order.Id, error);
                await _logger.InsertLog(LogLevel.Error, logError, logError);
            }
            return result.Errors;
        }

        /// <summary>
        /// Gets a value indicating whether order can be marked as paid
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>A value indicating whether order can be marked as paid</returns>
        public virtual async Task<bool> CanMarkOrderAsPaid(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (order.OrderStatus == OrderStatus.Cancelled)
                return false;

            if (order.PaymentStatus == PaymentStatus.Paid ||
                order.PaymentStatus == PaymentStatus.Refunded ||
                order.PaymentStatus == PaymentStatus.Voided)
                return false;

            return await Task.FromResult(true);
        }

        /// <summary>
        /// Marks order as paid
        /// </summary>
        /// <param name="order">Order</param>
        public virtual async Task MarkOrderAsPaid(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (!await CanMarkOrderAsPaid(order))
                throw new GrandException("You can't mark this order as paid");

            order.PaymentStatusId = (int)PaymentStatus.Paid;
            order.PaidDateUtc = DateTime.UtcNow;
            await _orderService.UpdateOrder(order);

            //add a note
            await _orderService.InsertOrderNote(new OrderNote {
                Note = "Order has been marked as paid",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,

            });

            await CheckOrderStatus(order);

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                await ProcessOrderPaid(order);
            }
        }



        /// <summary>
        /// Gets a value indicating whether refund from admin panel is allowed
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>A value indicating whether refund from admin panel is allowed</returns>
        public virtual async Task<bool> CanRefund(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (order.OrderTotal == decimal.Zero)
                return false;

            //refund cannot be made if previously a partial refund has been already done. only other partial refund can be made in this case
            if (order.RefundedAmount > decimal.Zero)
                return false;

            //uncomment the lines below in order to allow this operation for cancelled orders
            //if (order.OrderStatus == OrderStatus.Cancelled)
            //    return false;

            if (order.PaymentStatus == PaymentStatus.Paid &&
                await _paymentService.SupportRefund(order.PaymentMethodSystemName))
                return true;

            return false;
        }

        /// <summary>
        /// Refunds an order (from admin panel)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>A list of errors; empty list if no errors</returns>
        public virtual async Task<IList<string>> Refund(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (!await CanRefund(order))
                throw new GrandException("Cannot do refund for order.");

            var request = new RefundPaymentRequest();
            RefundPaymentResult result = null;
            try
            {
                request.Order = order;
                request.AmountToRefund = order.OrderTotal;
                request.IsPartialRefund = false;
                result = await _paymentService.Refund(request);
                if (result.Success)
                {
                    //total amount refunded
                    decimal totalAmountRefunded = order.RefundedAmount + request.AmountToRefund;

                    //update order info
                    order.RefundedAmount = totalAmountRefunded;
                    order.PaymentStatus = result.NewPaymentStatus;
                    await _orderService.UpdateOrder(order);

                    //add a note
                    await _orderService.InsertOrderNote(new OrderNote {
                        Note = string.Format("Order has been refunded. Amount = {0}", request.AmountToRefund),
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,
                    });

                    //check order status
                    await CheckOrderStatus(order);

                    //notifications
                    var orderRefundedStoreOwnerNotificationQueuedEmailId = await _workflowMessageService.SendOrderRefundedStoreOwnerNotification(order, request.AmountToRefund, _localizationSettings.DefaultAdminLanguageId);
                    if (orderRefundedStoreOwnerNotificationQueuedEmailId > 0)
                    {
                        await _orderService.InsertOrderNote(new OrderNote {
                            Note = "\"Order refunded\" email (to store owner) has been queued.",
                            DisplayToCustomer = false,
                            CreatedOnUtc = DateTime.UtcNow,
                            OrderId = order.Id,
                        });
                    }

                    //notifications
                    var orderRefundedCustomerNotificationQueuedEmailId = await _workflowMessageService.SendOrderRefundedCustomerNotification(order, request.AmountToRefund, order.CustomerLanguageId);
                    if (orderRefundedCustomerNotificationQueuedEmailId > 0)
                    {
                        await _orderService.InsertOrderNote(new OrderNote {
                            Note = "\"Order refunded\" email (to customer) has been queued.",
                            DisplayToCustomer = false,
                            CreatedOnUtc = DateTime.UtcNow,
                            OrderId = order.Id,
                        });
                    }

                    //raise event       
                    await _mediator.Publish(new OrderRefundedEvent(order, request.AmountToRefund));
                }

            }
            catch (Exception exc)
            {
                if (result == null)
                    result = new RefundPaymentResult();
                result.AddError(string.Format("Error: {0}. Full exception: {1}", exc.Message, exc.ToString()));
            }

            //process errors
            string error = "";
            for (int i = 0; i < result.Errors.Count; i++)
            {
                error += string.Format("Error {0}: {1}", i, result.Errors[i]);
                if (i != result.Errors.Count - 1)
                    error += ". ";
            }
            if (!String.IsNullOrEmpty(error))
            {
                //add a note
                await _orderService.InsertOrderNote(new OrderNote {
                    Note = string.Format("Unable to refund order. {0}", error),
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,
                });

                //log it
                string logError = string.Format("Error refunding order #{0}. Error: {1}", order.Id, error);
                await _logger.InsertLog(LogLevel.Error, logError, logError);
            }
            return result.Errors;
        }

        /// <summary>
        /// Gets a value indicating whether order can be marked as refunded
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>A value indicating whether order can be marked as refunded</returns>
        public virtual bool CanRefundOffline(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (order.OrderTotal == decimal.Zero)
                return false;

            //refund cannot be made if previously a partial refund has been already done. only other partial refund can be made in this case
            if (order.RefundedAmount > decimal.Zero)
                return false;


            //uncomment the lines below in order to allow this operation for cancelled orders
            //if (order.OrderStatus == OrderStatus.Cancelled)
            //     return false;

            if (order.PaymentStatus == PaymentStatus.Paid)
                return true;

            return false;
        }

        /// <summary>
        /// Refunds an order (offline)
        /// </summary>
        /// <param name="order">Order</param>
        public virtual async Task RefundOffline(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (!CanRefundOffline(order))
                throw new GrandException("You can't refund this order");

            //amout to refund
            decimal amountToRefund = order.OrderTotal;

            //total amount refunded
            decimal totalAmountRefunded = order.RefundedAmount + amountToRefund;

            //update order info
            order.RefundedAmount = totalAmountRefunded;
            order.PaymentStatus = PaymentStatus.Refunded;
            await _orderService.UpdateOrder(order);

            //add a note
            await _orderService.InsertOrderNote(new OrderNote {
                Note = string.Format("Order has been marked as refunded. Amount = {0}", amountToRefund),
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
            });

            //check order status
            await CheckOrderStatus(order);

            //notifications
            var orderRefundedStoreOwnerNotificationQueuedEmailId = await _workflowMessageService.SendOrderRefundedStoreOwnerNotification(order, amountToRefund, _localizationSettings.DefaultAdminLanguageId);
            if (orderRefundedStoreOwnerNotificationQueuedEmailId > 0)
            {
                await _orderService.InsertOrderNote(new OrderNote {
                    Note = "\"Order refunded\" email (to store owner) has been queued.",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,
                });

            }


            var orderRefundedCustomerNotificationQueuedEmailId = await _workflowMessageService.SendOrderRefundedCustomerNotification(order, amountToRefund, order.CustomerLanguageId);
            if (orderRefundedCustomerNotificationQueuedEmailId > 0)
            {
                await _orderService.InsertOrderNote(new OrderNote {
                    Note = "\"Order refunded\" email (to customer) has been queued.",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,
                });
            }

            //raise event       
            await _mediator.Publish(new OrderRefundedEvent(order, amountToRefund));
        }

        /// <summary>
        /// Gets a value indicating whether partial refund from admin panel is allowed
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="amountToRefund">Amount to refund</param>
        /// <returns>A value indicating whether refund from admin panel is allowed</returns>
        public virtual async Task<bool> CanPartiallyRefund(Order order, decimal amountToRefund)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (order.OrderTotal == decimal.Zero)
                return false;

            //uncomment the lines below in order to allow this operation for cancelled orders
            //if (order.OrderStatus == OrderStatus.Cancelled)
            //    return false;

            decimal canBeRefunded = order.OrderTotal - order.RefundedAmount;
            if (canBeRefunded <= decimal.Zero)
                return false;

            if (amountToRefund > canBeRefunded)
                return false;

            if ((order.PaymentStatus == PaymentStatus.Paid ||
                order.PaymentStatus == PaymentStatus.PartiallyRefunded) &&
                await _paymentService.SupportPartiallyRefund(order.PaymentMethodSystemName))
                return true;

            return false;
        }

        /// <summary>
        /// Partially refunds an order (from admin panel)
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="amountToRefund">Amount to refund</param>
        /// <returns>A list of errors; empty list if no errors</returns>
        public virtual async Task<IList<string>> PartiallyRefund(Order order, decimal amountToRefund)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (!await CanPartiallyRefund(order, amountToRefund))
                throw new GrandException("Cannot do partial refund for order.");

            var request = new RefundPaymentRequest();
            RefundPaymentResult result = null;
            try
            {
                request.Order = order;
                request.AmountToRefund = amountToRefund;
                request.IsPartialRefund = true;

                result = await _paymentService.Refund(request);

                if (result.Success)
                {
                    //total amount refunded
                    decimal totalAmountRefunded = order.RefundedAmount + amountToRefund;

                    //update order info
                    order.RefundedAmount = totalAmountRefunded;
                    order.PaymentStatus = result.NewPaymentStatus;
                    await _orderService.UpdateOrder(order);

                    //add a note
                    await _orderService.InsertOrderNote(new OrderNote {
                        Note = string.Format("Order has been partially refunded. Amount = {0}", amountToRefund),
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,
                    });

                    //check order status
                    await CheckOrderStatus(order);

                    //notifications
                    var orderRefundedStoreOwnerNotificationQueuedEmailId = await _workflowMessageService.SendOrderRefundedStoreOwnerNotification(order, amountToRefund, _localizationSettings.DefaultAdminLanguageId);
                    if (orderRefundedStoreOwnerNotificationQueuedEmailId > 0)
                    {
                        await _orderService.InsertOrderNote(new OrderNote {
                            Note = "\"Order refunded\" email (to store owner) has been queued.",
                            DisplayToCustomer = false,
                            CreatedOnUtc = DateTime.UtcNow,
                            OrderId = order.Id,
                        });
                    }


                    var orderRefundedCustomerNotificationQueuedEmailId = await _workflowMessageService.SendOrderRefundedCustomerNotification(order, amountToRefund, order.CustomerLanguageId);
                    if (orderRefundedCustomerNotificationQueuedEmailId > 0)
                    {
                        await _orderService.InsertOrderNote(new OrderNote {
                            Note = "\"Order refunded\" email (to customer) has been queued.",
                            DisplayToCustomer = false,
                            CreatedOnUtc = DateTime.UtcNow,
                            OrderId = order.Id,
                        });
                    }

                    //raise event       
                    await _mediator.Publish(new OrderRefundedEvent(order, amountToRefund));
                }
            }
            catch (Exception exc)
            {
                if (result == null)
                    result = new RefundPaymentResult();
                result.AddError(string.Format("Error: {0}. Full exception: {1}", exc.Message, exc.ToString()));
            }

            //process errors
            string error = "";
            for (int i = 0; i < result.Errors.Count; i++)
            {
                error += string.Format("Error {0}: {1}", i, result.Errors[i]);
                if (i != result.Errors.Count - 1)
                    error += ". ";
            }
            if (!String.IsNullOrEmpty(error))
            {
                //add a note
                await _orderService.InsertOrderNote(new OrderNote {
                    Note = string.Format("Unable to partially refund order. {0}", error),
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,
                });

                //log it
                string logError = string.Format("Error refunding order #{0}. Error: {1}", order.Id, error);
                await _logger.InsertLog(LogLevel.Error, logError, logError);
            }
            return result.Errors;
        }

        /// <summary>
        /// Gets a value indicating whether order can be marked as partially refunded
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="amountToRefund">Amount to refund</param>
        /// <returns>A value indicating whether order can be marked as partially refunded</returns>
        public virtual bool CanPartiallyRefundOffline(Order order, decimal amountToRefund)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (order.OrderTotal == decimal.Zero)
                return false;

            //uncomment the lines below in order to allow this operation for cancelled orders
            //if (order.OrderStatus == OrderStatus.Cancelled)
            //    return false;

            decimal canBeRefunded = order.OrderTotal - order.RefundedAmount;
            if (canBeRefunded <= decimal.Zero)
                return false;

            if (amountToRefund > canBeRefunded)
                return false;

            if (order.PaymentStatus == PaymentStatus.Paid ||
                order.PaymentStatus == PaymentStatus.PartiallyRefunded)
                return true;

            return false;
        }

        /// <summary>
        /// Partially refunds an order (offline)
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="amountToRefund">Amount to refund</param>
        public virtual async Task PartiallyRefundOffline(Order order, decimal amountToRefund)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (!CanPartiallyRefundOffline(order, amountToRefund))
                throw new GrandException("You can't partially refund (offline) this order");

            //total amount refunded
            decimal totalAmountRefunded = order.RefundedAmount + amountToRefund;

            //update order info
            order.RefundedAmount = totalAmountRefunded;
            order.PaymentStatus = PaymentStatus.PartiallyRefunded;
            await _orderService.UpdateOrder(order);

            //add a note
            await _orderService.InsertOrderNote(new OrderNote {
                Note = string.Format("Order has been marked as partially refunded. Amount = {0}", amountToRefund),
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
            });

            //check order status
            await CheckOrderStatus(order);

            //notifications
            var orderRefundedStoreOwnerNotificationQueuedEmailId = await _workflowMessageService.SendOrderRefundedStoreOwnerNotification(order, amountToRefund, _localizationSettings.DefaultAdminLanguageId);
            if (orderRefundedStoreOwnerNotificationQueuedEmailId > 0)
            {
                await _orderService.InsertOrderNote(new OrderNote {
                    Note = "\"Order refunded\" email (to store owner) has been queued.",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,
                });
            }

            var orderRefundedCustomerNotificationQueuedEmailId = await _workflowMessageService.SendOrderRefundedCustomerNotification(order, amountToRefund, order.CustomerLanguageId);
            if (orderRefundedCustomerNotificationQueuedEmailId > 0)
            {
                await _orderService.InsertOrderNote(new OrderNote {
                    Note = "\"Order refunded\" email (to customer) has been queued.",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,
                });
            }
            //raise event       
            await _mediator.Publish(new OrderRefundedEvent(order, amountToRefund));
        }



        /// <summary>
        /// Gets a value indicating whether void from admin panel is allowed
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>A value indicating whether void from admin panel is allowed</returns>
        public virtual async Task<bool> CanVoid(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (order.OrderTotal == decimal.Zero)
                return false;

            if (order.PaymentStatus == PaymentStatus.Authorized &&
                await _paymentService.SupportVoid(order.PaymentMethodSystemName))
                return true;

            return false;
        }

        /// <summary>
        /// Voids order (from admin panel)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Voided order</returns>
        public virtual async Task<IList<string>> Void(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (!await CanVoid(order))
                throw new GrandException("Cannot do void for order.");

            var request = new VoidPaymentRequest();
            VoidPaymentResult result = null;
            try
            {
                request.Order = order;
                result = await _paymentService.Void(request);

                if (result.Success)
                {
                    //update order info
                    order.PaymentStatus = result.NewPaymentStatus;
                    await _orderService.UpdateOrder(order);

                    //add a note
                    await _orderService.InsertOrderNote(new OrderNote {
                        Note = "Order has been voided",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,
                    });

                    //check order status
                    await CheckOrderStatus(order);
                }
            }
            catch (Exception exc)
            {
                if (result == null)
                    result = new VoidPaymentResult();
                result.AddError(string.Format("Error: {0}. Full exception: {1}", exc.Message, exc.ToString()));
            }

            //process errors
            string error = "";
            for (int i = 0; i < result.Errors.Count; i++)
            {
                error += string.Format("Error {0}: {1}", i, result.Errors[i]);
                if (i != result.Errors.Count - 1)
                    error += ". ";
            }
            if (!String.IsNullOrEmpty(error))
            {
                //add a note
                await _orderService.InsertOrderNote(new OrderNote {
                    Note = string.Format("Unable to voiding order. {0}", error),
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,
                });

                //log it
                string logError = string.Format("Error voiding order #{0}. Error: {1}", order.Id, error);
                await _logger.InsertLog(LogLevel.Error, logError, logError);
            }
            return result.Errors;
        }

        /// <summary>
        /// Gets a value indicating whether order can be marked as voided
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>A value indicating whether order can be marked as voided</returns>
        public virtual bool CanVoidOffline(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (order.OrderTotal == decimal.Zero)
                return false;

            if (order.PaymentStatus == PaymentStatus.Authorized)
                return true;

            return false;
        }

        /// <summary>
        /// Voids order (offline)
        /// </summary>
        /// <param name="order">Order</param>
        public virtual async Task VoidOffline(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (!CanVoidOffline(order))
                throw new GrandException("You can't void this order");

            order.PaymentStatusId = (int)PaymentStatus.Voided;
            await _orderService.UpdateOrder(order);

            //add a note
            await _orderService.InsertOrderNote(new OrderNote {
                Note = "Order has been marked as voided",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
            });
            //check orer status
            await CheckOrderStatus(order);
        }

        /// <summary>
        /// Place order items in current user shopping cart.
        /// </summary>
        /// <param name="order">The order</param>
        public virtual async Task ReOrder(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            var customer = await _customerService.GetCustomerById(order.CustomerId);

            foreach (var orderItem in order.OrderItems)
            {
                if (_productService.GetProductById(orderItem.ProductId) != null)
                {
                    var product = await _productService.GetProductById(orderItem.ProductId);
                    if (product != null && product.ProductType == ProductType.SimpleProduct)
                    {
                        await _shoppingCartService.AddToCart(customer, orderItem.ProductId,
                            ShoppingCartType.ShoppingCart, order.StoreId,
                            orderItem.AttributesXml, orderItem.UnitPriceExclTax,
                            orderItem.RentalStartDateUtc, orderItem.RentalEndDateUtc,
                            orderItem.Quantity, false);
                    }
                }
            }
        }

        /// <summary>
        /// Check whether return request is allowed
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public virtual async Task<bool> IsReturnRequestAllowed(Order order)
        {
            if (!_orderSettings.ReturnRequestsEnabled)
                return false;

            if (order == null || order.Deleted)
                return false;

            var shipments = await _shipmentService.GetShipmentsByOrder(order.Id);

            //validate allowed number of days
            if (_orderSettings.NumberOfDaysReturnRequestAvailable > 0)
            {
                var daysPassed = (DateTime.UtcNow - order.CreatedOnUtc).TotalDays;
                if (daysPassed >= _orderSettings.NumberOfDaysReturnRequestAvailable)
                    return false;
            }
            foreach (var item in order.OrderItems)
            {
                var product = await _productService.GetProductById(item.ProductId);
                if (product == null)
                    return false;

                var qtyDelivery = shipments.Where(x => x.DeliveryDateUtc.HasValue).SelectMany(x => x.ShipmentItems).Where(x => x.OrderItemId == item.Id).Sum(x => x.Quantity);
                var returnRequests = await _returnRequestService.SearchReturnRequests(customerId: order.CustomerId, orderItemId: item.Id);
                int qtyReturn = 0;

                foreach (var rr in returnRequests)
                {
                    foreach (var rrItem in rr.ReturnRequestItems)
                    {
                        qtyReturn += rrItem.Quantity;
                    }
                }

                if (!product.NotReturnable && qtyDelivery - qtyReturn > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Valdiate minimum order sub-total amount
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - OK; false - minimum order sub-total amount is not reached</returns>
        public virtual async Task<bool> ValidateMinOrderSubtotalAmount(IList<ShoppingCartItem> cart)
        {
            if (cart == null)
                throw new ArgumentNullException("cart");

            //min order amount sub-total validation
            if (cart.Any() && _orderSettings.MinOrderSubtotalAmount > decimal.Zero)
            {
                //subtotal
                var (discountAmount, appliedDiscounts, subTotalWithoutDiscount, subTotalWithDiscount, taxRates) = await _orderTotalCalculationService.GetShoppingCartSubTotal(cart, false);
                decimal orderSubTotalDiscountAmountBase = discountAmount;
                List<AppliedDiscount> orderSubTotalAppliedDiscounts = appliedDiscounts;
                decimal subTotalWithoutDiscountBase = subTotalWithoutDiscount;
                decimal subTotalWithDiscountBase = subTotalWithDiscount;

                if (subTotalWithoutDiscountBase < _orderSettings.MinOrderSubtotalAmount)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Valdiate minimum order total amount
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - OK; false - minimum order total amount is not reached</returns>
        public virtual async Task<bool> ValidateMinOrderTotalAmount(IList<ShoppingCartItem> cart)
        {
            if (cart == null)
                throw new ArgumentNullException("cart");

            if (cart.Any() && _orderSettings.MinOrderTotalAmount > decimal.Zero)
            {
                decimal? shoppingCartTotalBase = (await _orderTotalCalculationService.GetShoppingCartTotal(cart)).shoppingCartTotal;
                if (shoppingCartTotalBase.HasValue && shoppingCartTotalBase.Value < _orderSettings.MinOrderTotalAmount)
                    return false;
            }

            return true;
        }

        #endregion
    }
}
