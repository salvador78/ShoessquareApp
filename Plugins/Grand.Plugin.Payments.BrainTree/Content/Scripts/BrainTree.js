function createLocalPaymentClickListener(type) {
    return function (event) {
        event.preventDefault();

        localPaymentInstance.startPayment({
            paymentType: type,
            amount: '10.67',
            fallback: { // see Fallback section for details on these params
                url: 'https://your-domain.com/page-to-complete-checkout',
                buttonText: 'Complete Payment'
            },
            currencyCode: 'EUR',
            address: {
                countryCode: 'NL'
            },
            onPaymentStart: function (data, start) {
                // NOTE: It is critical here to store data.paymentId on your server
                //       so it can be mapped to a webhook sent by Braintree once the
                //       buyer completes their payment. See Start the payment
                //       section for details.

                // Call start to initiate the popup
                start();
            }
        }, function (startPaymentError, payload) {
            if (startPaymentError) {
                if (startPaymentError.code === 'LOCAL_PAYMENT_POPUP_CLOSED') {
                    console.error('Customer closed Local Payment popup.');
                } else {
                    console.error('Error!', startPaymentError);
                }
            } else {
                // Send the nonce to your server to create a transaction
                console.log(payload.nonce);
            }
        });
    };
}
var idealButton = document.getElementById('ideal-button');
var sofortButton = document.getElementById('sofort-button');

idealButton.addEventListener('click', createLocalPaymentClickListener('ideal'));
sofortButton.addEventListener('click', createLocalPaymentClickListener('sofort'));