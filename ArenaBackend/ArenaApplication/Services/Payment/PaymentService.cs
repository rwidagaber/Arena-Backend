using ArenaApplication.Dtos.Payment;
using ArenaApplication.IServices.Payment;
using ArenaApplication.IServices.User;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Entities;
using ArenaDomain.Entities.Payments;
using ArenaDomain.Entities.User;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using Mapster;
using Microsoft.AspNetCore.Identity;
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

        private readonly IGenericRepository<
            ArenaDomain.Entities.Subscription.SubscriptionPlan,
            Guid> _planRepo;
        private readonly IGenericRepository<MemberProfile, Guid> _memberProfileRepo;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserQueryService _userQuery;


        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentGatewayService _paymentGateway;


        public PaymentService(
            IGenericRepository<ArenaDomain.Entities.Payments.Payment, Guid> paymentRepo,
            IGenericRepository<ArenaDomain.Entities.Subscription.UserSubscription, Guid> subscriptionRepo,
            IUserQueryService userQuery,
            IUnitOfWork unitOfWork,
             IPaymentGatewayService paymentGateway,
             IGenericRepository<MemberProfile, Guid> memberProfileRepo,
            IGenericRepository<
            ArenaDomain.Entities.Subscription.SubscriptionPlan,
            Guid> planRepo
            )
        {
            _paymentRepo = paymentRepo;
            _subscriptionRepo = subscriptionRepo;
            _userQuery = userQuery;
            _unitOfWork = unitOfWork;
            _paymentGateway = paymentGateway;
            _memberProfileRepo = memberProfileRepo;
            _planRepo = planRepo;
        } 

        public async Task<Result<PaymentDto>> CreateAsync(CreatePaymentDto dto, Guid userId)
        {
            var plan = await _planRepo.GetAll().FirstOrDefaultAsync(p => p.Id == dto.PlanId);
            if (plan is null)
                return Result<PaymentDto>.Failure("Subscription Plan NotFound.");

            var memberProfile = await _memberProfileRepo.GetAll().FirstOrDefaultAsync(m => m.UserId == userId);
            if (memberProfile is null)
                return Result<PaymentDto>.Failure("Member Profile NotFound.");

            var activeSubscription = await _subscriptionRepo.GetAll()
                .FirstOrDefaultAsync(s => s.MemberProfileId == memberProfile.Id && s.PlanId == dto.PlanId && s.Status == SubscriptionStatus.Active);

            if (activeSubscription != null)
            {
                return Result<PaymentDto>.Failure("User already has an active subscription for this plan.");
            }

            var newSubscription = new ArenaDomain.Entities.Subscription.UserSubscription
            {
                Id = Guid.NewGuid(),
                MemberProfileId = memberProfile.Id,
                PlanId = dto.PlanId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow, 
                Status = SubscriptionStatus.Pending,
                RemainingSessions = plan.SessionLimit ?? 0,
                ReminderSent = false
            };

            await _subscriptionRepo.AddAsync(newSubscription);

            decimal secureAmount = plan.Price;

            var payment = new ArenaDomain.Entities.Payments.Payment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserSubscriptionId = newSubscription.Id,
                Amount = secureAmount,
                Currency = dto.Currency,
                PaymentMethod = dto.PaymentMethod,
                Status = ArenaDomain.Enums.PaymentStatus.Pending,
                TransactionId = null,
                PaymentIntentId = null
            };
            await _paymentRepo.AddAsync(payment);

            var user = await _userQuery.GetByIdAsync(userId);
            if (user is null)
                return Result<PaymentDto>.Failure("User not found.");

            var gatewayResponse = await _paymentGateway.GetIframeUrlAsync(
                payment.Amount,
                user.Email!,
                $"{user.FirstName} {user.LastName}");

            payment.PaymentIntentId = gatewayResponse.OrderId;

            await _unitOfWork.SaveChangesAsync();

            var paymentDto = payment.Adapt<PaymentDto>();
            paymentDto.IframeUrl = gatewayResponse.IframeUrl;

            return Result<PaymentDto>.Success(paymentDto);
        }

        //(Admin)
        public async Task<Result<IEnumerable<PaymentDto>>> GetAllAsync(PaymentFilterDto? filter = null)
        {
            var query = _paymentRepo.GetAll()
               .Include(p => p.User)
               .Include(p => p.UserSubscription).ThenInclude(s => s.Plan)
               .Cast<ArenaDomain.Entities.Payments.Payment>();


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
            var payment = await _paymentRepo.GetAll()
            .Include(p => p.User)
            .Include(p => p.UserSubscription).ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(p => p.Id == PaymentId);
            if (payment is null)
            {
                return Result<PaymentDto>.Failure("Payment not found.");
            }
            var paymentDto = payment.Adapt<PaymentDto>();
            return Result<PaymentDto>.Success(paymentDto);
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
            var payment = _paymentRepo.GetAll()
                .FirstOrDefault(p => p.PaymentIntentId == paymentIntentId);

            if (payment is null)
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

            var subscription = await _subscriptionRepo.GetAll()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == payment.UserSubscriptionId);

            if (subscription is not null)
            {
                subscription.Status = SubscriptionStatus.Active;

                subscription.StartDate = DateTime.UtcNow;

                subscription.EndDate = DateTime.UtcNow.AddMonths(
                    subscription.Plan.DurationMonths);

                await _subscriptionRepo.UpdateAsync(subscription);
            }

            await _paymentRepo.UpdateAsync(payment);
            await _unitOfWork.SaveChangesAsync();
            return Result.Success();

        }

        public async Task<Result> MarkAsFailedAsync(string paymentIntentId, string reason)
        {
            var payment = _paymentRepo.GetAll()
         .FirstOrDefault(p => p.PaymentIntentId == paymentIntentId);

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
            await _unitOfWork.SaveChangesAsync();

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

            if (payment.Status == PaymentStatus.Paid)
            {
                return Result<PaymentDto>
                    .Failure("Paid payment cannot be modified.");
            }
            payment.FailureReason = dto.FailureReason;

            payment.Status = dto.Status;

            if (dto.Status == ArenaDomain.Enums.PaymentStatus.Paid)
            {
                payment.PaymentDate = DateTime.UtcNow;

                // FIX: Also activate the user's subscription if Admin manually marks it as Paid
                var subscription = await _subscriptionRepo.GetAll()
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.Id == payment.UserSubscriptionId);

                if (subscription is not null)
                {
                    subscription.Status = SubscriptionStatus.Active;
                    subscription.StartDate = DateTime.UtcNow;
                    subscription.EndDate = DateTime.UtcNow.AddMonths(subscription.Plan.DurationMonths);
                    
                    await _subscriptionRepo.UpdateAsync(subscription);
                }
            }

            await _paymentRepo.UpdateAsync(payment);

            await _unitOfWork.SaveChangesAsync();

            var paymentDto = payment.Adapt<PaymentDto>();

            return Result<PaymentDto>.Success(paymentDto);
        }
    } 
}
