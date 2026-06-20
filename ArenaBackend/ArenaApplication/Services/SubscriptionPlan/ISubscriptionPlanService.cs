using ArenaApplication.Dtos.SubscriptionPlanDtos;

namespace ArenaApplication.Services.SubscriptionPlan
{
    public interface ISubscriptionPlanService
    {
        Task<IEnumerable<SubscriptionPlanDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<SubscriptionPlanDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<SubscriptionPlanDto> CreateAsync(CreateSubscriptionPlanDto createDto, CancellationToken cancellationToken = default);
        Task<SubscriptionPlanDto> UpdateAsync(Guid id, UpdateSubscriptionPlanDto updateDto, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
