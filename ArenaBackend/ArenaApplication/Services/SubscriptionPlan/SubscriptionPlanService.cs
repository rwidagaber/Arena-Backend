using ArenaApplication.Dtos.SubscriptionPlanDtos;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Interfacees;

namespace ArenaApplication.Services.SubscriptionPlan
{
    public class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly IGenericRepository<ArenaDomain.Entities.Subscription.SubscriptionPlan, Guid> _repository;

        public SubscriptionPlanService(IGenericRepository<ArenaDomain.Entities.Subscription.SubscriptionPlan, Guid> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<SubscriptionPlanDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var plans = await _repository.GetAllAsync(cancellationToken);
            return plans.Where(p => !p.IsDeleted).Select(MapToDto);
        }

        public async Task<SubscriptionPlanDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var plans = await _repository.GetAllAsync(cancellationToken);
            var plan = plans.FirstOrDefault(p => p.Id == id && !p.IsDeleted);

            if (plan == null)
                throw new KeyNotFoundException($"Subscription plan with ID {id} not found.");

            return MapToDto(plan);
        }

        public async Task<SubscriptionPlanDto> CreateAsync(SubscriptionPlanDto createDto, CancellationToken cancellationToken = default)
        {
            var plan = new ArenaDomain.Entities.Subscription.SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                NameEn = createDto.Name ?? string.Empty,
                NameAr = createDto.Name ?? string.Empty,
                DescriptionEn = createDto.Description ?? string.Empty,
                DescriptionAr = createDto.Description ?? string.Empty,
                DurationMonths = createDto.DurationMonths,
                Price = createDto.Price,
                SessionLimit = createDto.SessionLimit,
                IsActive = createDto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(plan, cancellationToken);
            return MapToDto(plan);
        }

        public async Task<SubscriptionPlanDto> UpdateAsync(Guid id, UpdateSubscriptionPlanDto updateDto, CancellationToken cancellationToken = default)
        {
            var plans = await _repository.GetAllAsync(cancellationToken);
            var plan = plans.FirstOrDefault(p => p.Id == id && !p.IsDeleted);

            if (plan == null)
                throw new KeyNotFoundException($"Subscription plan with ID {id} not found.");

            if (!string.IsNullOrEmpty(updateDto.Name))
            {
                plan.NameEn = updateDto.Name;
                plan.NameAr = updateDto.Name;
            }

            if (!string.IsNullOrEmpty(updateDto.Description))
            {
                plan.DescriptionEn = updateDto.Description;
                plan.DescriptionAr = updateDto.Description;
            }

            if (updateDto.DurationMonths.HasValue)
                plan.DurationMonths = updateDto.DurationMonths.Value;

            if (updateDto.Price.HasValue)
                plan.Price = updateDto.Price.Value;

            if (updateDto.SessionLimit.HasValue)
                plan.SessionLimit = updateDto.SessionLimit.Value;

            if (updateDto.IsActive.HasValue)
                plan.IsActive = updateDto.IsActive.Value;

            plan.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(plan, cancellationToken);

            return MapToDto(plan);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var plans = await _repository.GetAllAsync(cancellationToken);
            var plan = plans.FirstOrDefault(p => p.Id == id && !p.IsDeleted);

            if (plan == null)
                throw new KeyNotFoundException($"Subscription plan with ID {id} not found.");

            await _repository.SoftDeleteAsync(plan, cancellationToken);
        }

        private static SubscriptionPlanDto MapToDto(ArenaDomain.Entities.Subscription.SubscriptionPlan plan)
        {
            return new SubscriptionPlanDto
            {
                Id = plan.Id,
                Name = plan.NameEn,
                Description = plan.DescriptionEn,
                DurationMonths = plan.DurationMonths,
                Price = plan.Price,
                SessionLimit = plan.SessionLimit ?? 0,
                IsActive = plan.IsActive
            };
        }
    }
}
