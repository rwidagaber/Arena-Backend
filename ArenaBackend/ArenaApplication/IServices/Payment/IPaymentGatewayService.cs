using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices.Payment
{
    public interface IPaymentGatewayService
    {
        Task<string> GetIframeUrlAsync(
            decimal amount,
            string userEmail,
            string userName);
    }
}
