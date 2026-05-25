using ArenaDomain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ArenaApplication.Dtos.Payment
{
    public class UpdatePaymentStatusDto
    {
        [Required]
        public PaymentStatus Status { get; set; }

        public string? FailureReason { get; set; }
    }
}
