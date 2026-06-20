using ArenaDomain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.Payment
{
    public class PaymentFilterDto
    {
        public PaymentStatus? Status { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
