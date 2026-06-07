using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.Payment
{
    public class PaymentGatewayResponse
    {
        public string IframeUrl { get; set; } = default!;
        public string OrderId { get; set; } = default!;
    }
}
