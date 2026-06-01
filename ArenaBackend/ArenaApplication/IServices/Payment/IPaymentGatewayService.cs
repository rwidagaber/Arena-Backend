using ArenaApplication.Dtos.Payment;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices.Payment
{
    public interface IPaymentGatewayService
    {
        Task<PaymentGatewayResponse> GetIframeUrlAsync(
            decimal amount,
            string userEmail,
            string userName);

        bool VerifyWebhookHmac(PaymobWebhookDto webhook, string receivedHmac);

    }
}
