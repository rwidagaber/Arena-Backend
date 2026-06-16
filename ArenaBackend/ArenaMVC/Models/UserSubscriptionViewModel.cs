using ArenaApplication.Dtos.UserSubscription;
using ArenaApplication.Dtos.UserSupscriptionDto;

namespace ArenaMVC.Models
{
    public class UserSubscriptionViewModel
    {
        public IEnumerable<UserSubscriptionDto> Items { get; set; } =
            Enumerable.Empty<UserSubscriptionDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages =>
            PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
        public string? StatusFilter { get; set; }
    }
}
