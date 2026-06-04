using ArenaApplication.Dtos.UserSubscription;

namespace ArenaApplication.Services.UserSubscription
{
    public interface IUserSubscriptionService
    {
        Task<IEnumerable<UserSubscriptionDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<UserSubscriptionDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<UserSubscriptionDto>> GetByMemberIdAsync(Guid memberProfileId, CancellationToken cancellationToken = default);
        Task<UserSubscriptionDto> CreateAsync(CreateUserSubscriptionDto createDto, CancellationToken cancellationToken = default);
        Task<UserSubscriptionDto> UpdateStatusAsync(Guid id, UpdateUserSubscriptionStatusDto updateDto, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
