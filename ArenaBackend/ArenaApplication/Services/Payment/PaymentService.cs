using ArenaApplication.Dtos.Payment;
using ArenaApplication.IServices.Payment;
using ArenaDomain.Entities.Payments;
using ArenaDomain.Enums;
using ArenaDomain.Interfacees;
using ArenaDomain.Shared;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ArenaApplication.Services.Payment
{
    public class PaymentService : IPaymentService
    {
        private readonly IGenericRepository<ArenaDomain.Entities.Payments.Payment, Guid> _paymentRepo;
        private readonly IGenericRepository<ArenaDomain.Entities.Subscription.UserSubscription, Guid> _subscriptionRepo;
      
        
        public PaymentService(
            IGenericRepository<ArenaDomain.Entities.Payments.Payment, Guid> paymentRepo,
            IGenericRepository<ArenaDomain.Entities.Subscription.UserSubscription, Guid> subscriptionRepo
            )
        {
            _paymentRepo = paymentRepo;
            _subscriptionRepo = subscriptionRepo;
        } 

        public async Task<Result<PaymentDto>> CreateAsync(CreatePaymentDto dto, Guid userId)
        {
            var subscription = await _subscriptionRepo.GetAll()
            .Include(s=>s.Plan)
            .Include(s=>s.MemberProfile)
            .FirstOrDefaultAsync(s => s.Id == dto.UserSubscriptionId && s.MemberProfile.UserId == userId);
        if (subscription == null)
            {
                return Result<PaymentDto>.Failure("Subscription NotFound");
            }

            decimal secureAmount = subscription.Plan.Price;

            var payment = new ArenaDomain.Entities.Payments.Payment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserSubscriptionId = dto.UserSubscriptionId,
                Amount = secureAmount,
                Currency = dto.Currency,
                PaymentMethod = dto.PaymentMethod,
                Status = ArenaDomain.Enums.PaymentStatus.Pending,
                TransactionId = null,
                PaymentIntentId = null
            };
                await _paymentRepo.AddAsync(payment);

                //await _paymentRepo.saveChanges??


            var paymentDto = payment.Adapt<PaymentDto>();

            return Result<PaymentDto>.Success(paymentDto);



        }

        //(Admin)
        public async Task<Result<IEnumerable<PaymentDto>>> GetAllAsyc(PaymentFilterDto? filter = null)
        {
            var query = _paymentRepo.GetAll();

            if (filter is not null)
            {
                if (filter.Status.HasValue)
                {
                    query = query.Where(p => p.Status == filter.Status.Value);
                }

                if (filter.PaymentMethod.HasValue)
                {
                    query = query.Where(p => p.PaymentMethod == filter.PaymentMethod.Value);
                }

                if (filter.From.HasValue)
                {
                    query = query.Where(p => p.CreatedAt >= filter.From.Value);
                }

                if (filter.To.HasValue)
                {
                    query = query.Where(p => p.CreatedAt <= filter.To.Value);
                }
            }

            var filteredPayments = await query
                .OrderByDescending(p => p.CreatedAt) 
                .ToListAsync();

            var paymentDtos = filteredPayments.Adapt<IEnumerable<PaymentDto>>();

            return Result<IEnumerable<PaymentDto>>.Success(paymentDtos);
        }

        public async Task<Result<PaymentDto>> GetByIdAsync(Guid PaymentId)
        {
            var payment = await _paymentRepo.GetAll().FirstOrDefaultAsync(p => p.Id == PaymentId);
            if(payment is null)
            {
                return Result<PaymentDto>.Failure("Payment not found.");
            }
            var paymentDro = payment.Adapt<PaymentDto>();
            return Result<PaymentDto>.Success(paymentDro);
        }

        public async Task<Result<IEnumerable<PaymentDto>>> GetMyPaymentsAsync(Guid userId)
        {
            var payments = await _paymentRepo.GetAll()
                .Include(p=>p.User)
                .Include(p=>p.UserSubscription).ThenInclude(p=>p.Plan)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

            return Result<IEnumerable<PaymentDto>>.Success(
                payments.Adapt<IEnumerable<PaymentDto>>());




        }

        public async Task<Result> MarkAsCompletedAsync(string transactionId, string paymentIntentId)
        {
            var payment = await _paymentRepo.GetAll()
                .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId);

            if(payment is null)
            {
                return Result.Failure("Payment not found with the provided Intent ID.");
            }
            if(payment.Status==ArenaDomain.Enums.PaymentStatus.Paid)
            {
                return Result.Success();
            }

            payment.TransactionId = transactionId;
            payment.Status= ArenaDomain.Enums.PaymentStatus.Paid;
            payment.PaymentDate = DateTime.UtcNow;

            await _paymentRepo.UpdateAsync(payment);
            return Result.Success();

        }

        public async Task<Result> MarkAsFailedAsync(string paymentIntentId, string reason)
        {
            var payment = await _paymentRepo.GetAll()
                 .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId);

            if (payment is null)
            {
                return Result.Failure("Payment not found with the provided Intent ID.");
            }

            if (payment.Status == ArenaDomain.Enums.PaymentStatus.Failed)
            {
                return Result.Success();
            }
            payment.Status = ArenaDomain.Enums.PaymentStatus.Failed;
            payment.FailureReason = reason;

            await _paymentRepo.UpdateAsync(payment);

            return Result.Success();


        }

        public async Task<Result<PaymentDto>> UpdateStatusAsync(Guid paymentId, UpdatePaymentStatusDto dto)
        {
            var payment = await _paymentRepo.GetAll()
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment is null)
            {
                return Result<PaymentDto>.Failure("Payment not found.");
            }

            payment.Status = dto.Status;
            payment.FailureReason = dto.FailureReason;

            if (dto.Status == ArenaDomain.Enums.PaymentStatus.Paid)
            {
                payment.PaymentDate = DateTime.UtcNow;
            }

            await _paymentRepo.UpdateAsync(payment);

            // وزي ما اتفقنا، هتسأل الليدر لو الـ Repo بيعمل Save لوحده أو هيرجع الـ context بعدين.

            var paymentDto = payment.Adapt<PaymentDto>();

            return Result<PaymentDto>.Success(paymentDto);
        }
    } 
}