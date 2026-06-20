using ArenaApplication.Dtos.SubscriptionPlanDtos;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace ArenaApplication.Services.SubscriptionPlan
{
    public class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly IGenericRepository<ArenaDomain.Entities.Subscription.SubscriptionPlan, Guid> _repository;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public SubscriptionPlanService(
            IGenericRepository<ArenaDomain.Entities.Subscription.SubscriptionPlan, Guid> repository,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _repository = repository;
            _localizer = localizer;
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
                throw new KeyNotFoundException(_localizer["SubscriptionPlanNotFoundById"]);

            return MapToDto(plan);
        }

        public async Task<SubscriptionPlanDto> CreateAsync(CreateSubscriptionPlanDto createDto, CancellationToken cancellationToken = default)
        {
            var plan = new ArenaDomain.Entities.Subscription.SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                NameEn = createDto.NameEn ?? string.Empty,
                NameAr = createDto.NameAr ?? string.Empty,
                DescriptionEn = createDto.DescriptionEn ?? string.Empty,
                DescriptionAr = createDto.DescriptionAr ?? string.Empty,
                DurationMonths = createDto.DurationMonths,
                Price = createDto.Price,
                SessionLimit = createDto.SessionLimit,
                IsActive = true,
                HasAI = createDto.HasAI,
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
                throw new KeyNotFoundException(_localizer["SubscriptionPlanNotFoundById"]);

            if (!string.IsNullOrEmpty(updateDto.NameEn))
                plan.NameEn = updateDto.NameEn;

            if (!string.IsNullOrEmpty(updateDto.NameAr))
                plan.NameAr = updateDto.NameAr;

            if (!string.IsNullOrEmpty(updateDto.DescriptionEn))
                plan.DescriptionEn = updateDto.DescriptionEn;

            if (!string.IsNullOrEmpty(updateDto.DescriptionAr))
                plan.DescriptionAr = updateDto.DescriptionAr;

            if (updateDto.DurationMonths.HasValue)
                plan.DurationMonths = updateDto.DurationMonths.Value;

            if (updateDto.Price.HasValue)
                plan.Price = updateDto.Price.Value;

            if (updateDto.SessionLimit.HasValue)
                plan.SessionLimit = updateDto.SessionLimit.Value;

            if (updateDto.IsActive.HasValue)
                plan.IsActive = updateDto.IsActive.Value;

            if (updateDto.HasAI.HasValue)
                plan.HasAI = updateDto.HasAI.Value;

            plan.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(plan, cancellationToken);

            return MapToDto(plan);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var plans = await _repository.GetAllAsync(cancellationToken);
            var plan = plans.FirstOrDefault(p => p.Id == id && !p.IsDeleted);

            if (plan == null)
                throw new KeyNotFoundException(_localizer["SubscriptionPlanNotFoundById"]);

            await _repository.SoftDeleteAsync(plan, cancellationToken);
        }

        private static SubscriptionPlanDto MapToDto(ArenaDomain.Entities.Subscription.SubscriptionPlan plan)
        {
            var isArabic = CultureInfo.CurrentUICulture.Name.StartsWith("ar");

            return new SubscriptionPlanDto
            {
                Id = plan.Id,
                Name = isArabic ? plan.NameAr : plan.NameEn,
                Description = isArabic ? plan.DescriptionAr : plan.DescriptionEn,
                NameEn = plan.NameEn,
                NameAr = plan.NameAr,
                DescriptionEn = plan.DescriptionEn,
                DescriptionAr = plan.DescriptionAr,
                DurationMonths = plan.DurationMonths,
                Price = plan.Price,
                SessionLimit = plan.SessionLimit ?? 0,
                IsActive = plan.IsActive,
                HasAI = plan.HasAI
            };
        }
    }
}
