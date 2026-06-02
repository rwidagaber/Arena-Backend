using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.Payment
{
    public class WebhookDto
    {
            public string TransactionId { get; set; }
            public string PaymentIntentId { get; set; } 
    }
}
