using ArenaApplication.Dtos.UserSubscription;
using ArenaApplication.Dtos.UserSupscriptionDto;
using ArenaApplication.IServices;
using ArenaApplication.IServices.User;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using ArenaInfrastructure.Repositories;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace ArenaApplication.Services.UserSubscription
{
    public class UserSubscriptionService : IUserSubscriptionService
    {
        private readonly IGenericRepository<ArenaDomain.Entities.Subscription.UserSubscription, Guid> _repository;
        private readonly IGenericRepository<ArenaDomain.Entities.Subscription.SubscriptionPlan, Guid> _planRepository;
        private readonly IMemberProfileRepository _memberProfileRepository;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;
        private readonly IUserQueryService _userQueryService;
        private readonly IBackgroundJobService _backgroundJobService;

        public UserSubscriptionService(
            IGenericRepository<ArenaDomain.Entities.Subscription.UserSubscription, Guid> repository,
            IGenericRepository<ArenaDomain.Entities.Subscription.SubscriptionPlan, Guid> planRepository,
            IMemberProfileRepository memberProfileRepository,
            IStringLocalizer<ArenaLocalization> localizer,
            IUserQueryService userQueryService,
            IBackgroundJobService backgroundJobService)
        {
            _repository = repository;
            _planRepository = planRepository;
            _memberProfileRepository = memberProfileRepository;
            _localizer = localizer;
            _userQueryService = userQueryService;
            _backgroundJobService = backgroundJobService;
        }

        public async Task<IEnumerable<UserSubscriptionDto>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            var subscriptions = await _repository.GetAllAsync(cancellationToken);
            var activeSubscriptions = subscriptions.Where(s => !s.IsDeleted).ToList();

            var result = new List<UserSubscriptionDto>();
            foreach (var s in activeSubscriptions)
            {
                var plan = await _planRepository.GetByIdAsync(s.PlanId, cancellationToken);
                var member = await _memberProfileRepository.GetByIdAsync(s.MemberProfileId, cancellationToken);
                var user = member != null ? await _userQueryService.GetByIdAsync(member.UserId) : null;
                result.Add(MapToDto(s, plan, user));
            }

            return result;
        }

        public async Task<PagedResult<UserSubscriptionDto>> GetAllPagedAsync(
            int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var subscriptions = await _repository.GetAllAsync(cancellationToken);
            var activeSubscriptions = subscriptions.Where(s => !s.IsDeleted).ToList();

            int totalCount = activeSubscriptions.Count;
            var paged = activeSubscriptions.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var result = new List<UserSubscriptionDto>();
            foreach (var s in paged)
            {
                var plan = await _planRepository.GetByIdAsync(s.PlanId, cancellationToken);
                var member = await _memberProfileRepository.GetByIdAsync(s.MemberProfileId, cancellationToken);
                var user = member != null ? await _userQueryService.GetByIdAsync(member.UserId) : null;
                result.Add(MapToDto(s, plan, user));
            }

            return new PagedResult<UserSubscriptionDto>
            {
                Items = result,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<UserSubscriptionDto> GetByIdAsync(
            Guid id, CancellationToken cancellationToken = default)
        {
            var subscriptions = await _repository.GetAllAsync(cancellationToken);
            var subscription = subscriptions.FirstOrDefault(s => s.Id == id && !s.IsDeleted);

            if (subscription == null)
                throw new KeyNotFoundException(_localizer["UserSubscriptionNotFoundById"]);

            var plan = await _planRepository.GetByIdAsync(subscription.PlanId, cancellationToken);
            var member = await _memberProfileRepository.GetByIdAsync(subscription.MemberProfileId, cancellationToken);
            var user = member != null ? await _userQueryService.GetByIdAsync(member.UserId) : null;

            return MapToDto(subscription, plan, user);
        }

        public async Task<IEnumerable<UserSubscriptionDto>> GetByMemberIdAsync(
            Guid memberProfileId, CancellationToken cancellationToken = default)
        {
            var subscriptions = await _repository.FindAsync(
                s => s.MemberProfileId == memberProfileId && !s.IsDeleted, cancellationToken);

            var member = await _memberProfileRepository.GetByIdAsync(memberProfileId, cancellationToken);
            var user = member != null ? await _userQueryService.GetByIdAsync(member.UserId) : null;

            var result = new List<UserSubscriptionDto>();
            foreach (var s in subscriptions)
            {
                var plan = await _planRepository.GetByIdAsync(s.PlanId, cancellationToken);
                result.Add(MapToDto(s, plan, user));
            }

            return result;
        }

        private const int MaxActivePlans = 2;

        public async Task<UserSubscriptionDto> CreateAsync(
            CreateUserSubscriptionDto createDto, CancellationToken cancellationToken = default)
        {
            var plan = await _planRepository.GetByIdAsync(createDto.SubscriptionPlanId, cancellationToken);
            if (plan == null)
                throw new KeyNotFoundException(
                    string.Format(_localizer["EntityNotFoundById"], "Subscription plan", createDto.SubscriptionPlanId));

            var member = await _memberProfileRepository.GetByIdAsync(createDto.MemberProfileId, cancellationToken);
            if (member == null)
                throw new KeyNotFoundException(
                    string.Format(_localizer["EntityNotFoundById"], "Member profile", createDto.MemberProfileId));

            // Enforce maximum active plans limit
            var activePlans = await _repository.FindAsync(
                s => s.MemberProfileId == createDto.MemberProfileId
                     && !s.IsDeleted
                     && s.EndDate > DateTime.UtcNow,
                cancellationToken);

            if (activePlans.Count >= MaxActivePlans)
                throw new InvalidOperationException(_localizer["MaxActivePlansReached"]);

            var subscription = new ArenaDomain.Entities.Subscription.UserSubscription
            {
                Id = Guid.NewGuid(),
                MemberProfileId = createDto.MemberProfileId,
                PlanId = createDto.SubscriptionPlanId,
                StartDate = createDto.StartDate,
                EndDate = createDto.StartDate.AddMonths(plan.DurationMonths),
                Status = ArenaDomain.Enums.SubscriptionStatus.Active,
                RemainingSessions = plan.SessionLimit ?? 0,
                ReminderSent = false,
                CreatedAt = DateTime.UtcNow,
            };

            await _repository.AddAsync(subscription, cancellationToken);

            // ✅ Payment confirmation — SignalR + Email
            await _backgroundJobService.EnqueueSubscriptionPaymentJobAsync(
                createDto.MemberProfileId,
                plan.Price,
                plan.NameEn);

            // ✅ Expiry reminder — 3 days before end date
            await _backgroundJobService.ScheduleSubscriptionExpiryReminderAsync(
                createDto.MemberProfileId,
                subscription.EndDate);

            var user = await _userQueryService.GetByIdAsync(member.UserId);
            return MapToDto(subscription, plan, user);
        }

        public async Task<UserSubscriptionDto> UpdateStatusAsync(
            Guid id, UpdateUserSubscriptionStatusDto updateDto, CancellationToken cancellationToken = default)
        {
            var subscriptions = await _repository.GetAllAsync(cancellationToken);
            var subscription = subscriptions.FirstOrDefault(s => s.Id == id && !s.IsDeleted);

            if (subscription == null)
                throw new KeyNotFoundException(_localizer["UserSubscriptionNotFoundById"]);

            subscription.Status = updateDto.Status;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(subscription, cancellationToken);

            // ✅ لو الـ admin عمل expire يدوي
            if (updateDto.Status == ArenaDomain.Enums.SubscriptionStatus.Expired)
            {
                await _backgroundJobService.EnqueueSubscriptionExpiredAsync(
                    subscription.MemberProfileId);
            }

            var plan = await _planRepository.GetByIdAsync(subscription.PlanId, cancellationToken);
            var member = await _memberProfileRepository.GetByIdAsync(subscription.MemberProfileId, cancellationToken);
            var user = member != null ? await _userQueryService.GetByIdAsync(member.UserId) : null;

            return MapToDto(subscription, plan, user);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var subscriptions = await _repository.GetAllAsync(cancellationToken);
            var subscription = subscriptions.FirstOrDefault(s => s.Id == id && !s.IsDeleted);

            if (subscription == null)
                throw new KeyNotFoundException(_localizer["UserSubscriptionNotFoundById"]);

            await _repository.SoftDeleteAsync(subscription, cancellationToken);
        }

        private UserSubscriptionDto MapToDto(
            ArenaDomain.Entities.Subscription.UserSubscription subscription,
            ArenaDomain.Entities.Subscription.SubscriptionPlan? plan,
            ArenaDomain.Entities.User.ApplicationUser? user)
        {
            return new UserSubscriptionDto
            {
                Id = subscription.Id,
                PlanNameEn = plan?.NameEn ?? string.Empty,
                PlanNameAr = plan?.NameAr ?? string.Empty,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                Status = subscription.Status.ToString(),
                RemainingSessions = subscription.RemainingSessions,
                TotalSessions = plan?.SessionLimit ?? 0,
                PaymentAmount = plan?.Price ?? 0,
                ReminderSent = subscription.ReminderSent,
            };
        }
    }
}