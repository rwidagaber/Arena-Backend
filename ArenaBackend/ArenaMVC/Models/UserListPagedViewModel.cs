using System.Collections.Generic;

namespace ArenaMVC.Models
{
    public class UserListPagedViewModel
    {
        public IEnumerable<UserListViewModel> Items { get; set; } = Enumerable.Empty<UserListViewModel>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)System.Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public ArenaDomain.Enums.MembershipStatus? MembershipStatusFilter { get; set; }
        public string? SubscriptionStatusFilter { get; set; }
        public bool IsAscending { get; set; }
    }
}
