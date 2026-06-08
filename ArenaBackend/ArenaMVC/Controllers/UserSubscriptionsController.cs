//using ArenaApplication.Dtos.UserSubscription;
//using ArenaApplication.Services.UserSubscription;
//using ArenaDomain.Shared;
//using ArenaMVC.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Localization;

//namespace ArenaMVC.Controllers
//{
//    public class UserSubscriptionsController : Controller
//    {
//        private const int DefaultPageSize = 10;

//        private readonly IUserSubscriptionService _userSubscriptionService;
//        private readonly IStringLocalizer<ArenaLocalization> _localizer;

//        public UserSubscriptionsController(
//            IUserSubscriptionService userSubscriptionService,
//            IStringLocalizer<ArenaLocalization> localizer)
//        {
//            _userSubscriptionService = userSubscriptionService;
//            _localizer = localizer;
//        }

//        // GET: UserSubscriptions?page=1&pageSize=10&status=Active
//        [HttpGet]
//        public async Task<IActionResult> Index(
//            int page = 1,
//            int pageSize = DefaultPageSize,
//            string? status = null,
//            CancellationToken cancellationToken = default)
//        {
//            if (page < 1) page = 1;
//            if (pageSize < 1 || pageSize > 100) pageSize = DefaultPageSize;

//            try
//            {
//                var pagedResult = await _userSubscriptionService.GetAllPagedAsync(page, pageSize, cancellationToken);

//                // Apply optional status filter in-memory (client-side after fetch)
//                var items = pagedResult.Items;
//                int totalCount = pagedResult.TotalCount;

//                if (!string.IsNullOrWhiteSpace(status))
//                {
//                    // Re-fetch all to apply filter properly, then page manually
//                    var all = await _userSubscriptionService.GetAllAsync(cancellationToken);
//                    var filtered = all
//                        .Where(s => s.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
//                        .ToList();

//                    totalCount = filtered.Count;
//                    items = filtered
//                        .Skip((page - 1) * pageSize)
//                        .Take(pageSize);
//                }

//                var viewModel = new UserSubscriptionViewModel
//                {
//                    Items = items,
//                    TotalCount = totalCount,
//                    Page = page,
//                    PageSize = pageSize,
//                    StatusFilter = status
//                };

//                return View(viewModel);
//            }
//            catch (Exception)
//            {
//                TempData["Error"] = _localizer["AnErrorOccurredRetrievingUserSubscriptions"];
//                return View(new UserSubscriptionViewModel { Page = page, PageSize = pageSize });
//            }
//        }
//    }
//}
