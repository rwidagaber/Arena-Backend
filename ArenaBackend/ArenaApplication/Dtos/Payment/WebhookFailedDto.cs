using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.Payment
{
    public class WebhookFailedDto
    {
        public string PaymentIntentId { get; set; }
        public string Reason { get; set; }
    }
}
