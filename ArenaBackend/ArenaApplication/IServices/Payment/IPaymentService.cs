using ArenaApplication.Dtos.Payment;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices.Payment
{
    public interface IPaymentService
    {
        //Member View
        Task<Result<PaymentDto>> CreateAsync(CreatePaymentDto dto, Guid userId);
        Task<Result<IEnumerable<PaymentDto>>> GetMyPaymentsAsync(Guid userId);

        Task<Result<PaymentDto>> GetByIdAsync(Guid PaymentId);

        //Admin View
        Task<Result<IEnumerable<PaymentDto>>> GetAllAsync(PaymentFilterDto? filter = null);

        //inCash Status
        Task<Result<PaymentDto>> UpdateStatusAsync(Guid PaymentId, UpdatePaymentStatusDto dto);
        //webhook
        Task<Result> MarkAsCompletedAsync(string transactionId, string paymentIntentId);
        Task<Result> MarkAsFailedAsync(string paymentIntentId, string reason);
    }
}
