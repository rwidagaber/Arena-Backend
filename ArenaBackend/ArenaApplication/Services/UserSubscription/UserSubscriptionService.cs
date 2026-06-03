using ArenaApplication.Dtos.UserSubscription;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Interfaces;
using ArenaInfrastructure.Repositories;

namespace ArenaApplication.Services.UserSubscription
{
    public class UserSubscriptionService : IUserSubscriptionService
    {
        private readonly IGenericRepository<ArenaDomain.Entities.Subscription.UserSubscription, Guid> _repository;
        private readonly IGenericRepository<ArenaDomain.Entities.Subscription.SubscriptionPlan, Guid> _planRepository;
        private readonly IMemberProfileRepository _memberProfileRepository;

        public UserSubscriptionService(
            IGenericRepository<ArenaDomain.Entities.Subscription.UserSubscription, Guid> repository,
            IGenericRepository<ArenaDomain.Entities.Subscription.SubscriptionPlan, Guid> planRepository,
            IMemberProfileRepository memberProfileRepository)
        {
            _repository = repository;
            _planRepository = planRepository;
            _memberProfileRepository = memberProfileRepository;
        }

        public async Task<IEnumerable<UserSubscriptionDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var subscriptions = await _repository.GetAllAsync(cancellationToken);
            var activeSubscriptions = subscriptions.Where(s => !s.IsDeleted).ToList();
            
            var result = new List<UserSubscriptionDto>();
            foreach(var s in activeSubscriptions)
            {
                var plan = await _planRepository.GetByIdAsync(s.PlanId, cancellationToken);
                var member = await _memberProfileRepository.GetByIdAsync(s.MemberProfileId, cancellationToken);
                result.Add(MapToDto(s, plan, member?.User));
            }

            return result;
        }

        public async Task<UserSubscriptionDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var subscriptions = await _repository.GetAllAsync(cancellationToken);
            var subscription = subscriptions.FirstOrDefault(s => s.Id == id && !s.IsDeleted);

            if (subscription == null)
                throw new KeyNotFoundException($"User subscription with ID {id} not found.");

            var plan = await _planRepository.GetByIdAsync(subscription.PlanId, cancellationToken);
            var member = await _memberProfileRepository.GetByIdAsync(subscription.MemberProfileId, cancellationToken);

            return MapToDto(subscription, plan, member?.User);
        }

        public async Task<IEnumerable<UserSubscriptionDto>> GetByMemberIdAsync(Guid memberProfileId, CancellationToken cancellationToken = default)
        {
            var subscriptions = await _repository.FindAsync(s => s.MemberProfileId == memberProfileId && !s.IsDeleted, cancellationToken);
            var member = await _memberProfileRepository.GetByIdAsync(memberProfileId, cancellationToken);
            
            var result = new List<UserSubscriptionDto>();
            foreach(var s in subscriptions)
            {
                var plan = await _planRepository.GetByIdAsync(s.PlanId, cancellationToken);
                result.Add(MapToDto(s, plan, member?.User));
            }

            return result;
        }

        public async Task<UserSubscriptionDto> CreateAsync(CreateUserSubscriptionDto createDto, CancellationToken cancellationToken = default)
        {
            var plan = await _planRepository.GetByIdAsync(createDto.SubscriptionPlanId, cancellationToken);
            if (plan == null)
                throw new KeyNotFoundException($"Subscription plan with ID {createDto.SubscriptionPlanId} not found.");

            var member = await _memberProfileRepository.GetByIdAsync(createDto.MemberProfileId, cancellationToken);
            if (member == null)
                throw new KeyNotFoundException($"Member profile with ID {createDto.MemberProfileId} not found.");

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
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(subscription, cancellationToken);
            return MapToDto(subscription, plan, member.User);
        }

        public async Task<UserSubscriptionDto> UpdateStatusAsync(Guid id, UpdateUserSubscriptionStatusDto updateDto, CancellationToken cancellationToken = default)
        {
            var subscriptions = await _repository.GetAllAsync(cancellationToken);
            var subscription = subscriptions.FirstOrDefault(s => s.Id == id && !s.IsDeleted);

            if (subscription == null)
                throw new KeyNotFoundException($"User subscription with ID {id} not found.");

            subscription.Status = updateDto.Status;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(subscription, cancellationToken);

            var plan = await _planRepository.GetByIdAsync(subscription.PlanId, cancellationToken);
            var member = await _memberProfileRepository.GetByIdAsync(subscription.MemberProfileId, cancellationToken);

            return MapToDto(subscription, plan, member?.User);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var subscriptions = await _repository.GetAllAsync(cancellationToken);
            var subscription = subscriptions.FirstOrDefault(s => s.Id == id && !s.IsDeleted);

            if (subscription == null)
                throw new KeyNotFoundException($"User subscription with ID {id} not found.");

            await _repository.SoftDeleteAsync(subscription, cancellationToken);
        }

        private static UserSubscriptionDto MapToDto(ArenaDomain.Entities.Subscription.UserSubscription subscription, ArenaDomain.Entities.Subscription.SubscriptionPlan? plan, ArenaDomain.Entities.User.ApplicationUser? user)
        {
            string memberName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Unknown Member";

            return new UserSubscriptionDto
            {
                Id = subscription.Id,
                MemberName = memberName,
                PlanName = plan?.NameEn ?? string.Empty,
                PlanPrice = plan?.Price ?? 0,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                Status = subscription.Status.ToString(),
                RemainingSessions = subscription.RemainingSessions,
                ReminderSent = subscription.ReminderSent
            };
        }
    }
}
