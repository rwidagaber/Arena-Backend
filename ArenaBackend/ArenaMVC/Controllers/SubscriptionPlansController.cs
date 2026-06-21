using ArenaApplication.Dtos.SubscriptionPlanDtos;
using ArenaApplication.Services.SubscriptionPlan;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ArenaMVC.Controllers
{
    public class SubscriptionPlansController : Controller
    {
        private readonly ISubscriptionPlanService _subscriptionPlanService;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public SubscriptionPlansController(
            ISubscriptionPlanService subscriptionPlanService,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _subscriptionPlanService = subscriptionPlanService;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            try
            {
                var plans = await _subscriptionPlanService.GetAllAsync(cancellationToken);
                return View(plans);
            }
            catch (Exception ex)
            {
                TempData["Error"] = _localizer["AnErrorOccurredRetrievingSubscriptionPlans"].Value;
                return View(new List<SubscriptionPlanDto>());
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            CreateSubscriptionPlanDto createDto,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(createDto);
                }

                var createdPlan = await _subscriptionPlanService.CreateAsync(createDto, cancellationToken);
                TempData["Success"] = _localizer["SubscriptionPlanCreatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(createDto);
            }
            catch (Exception ex)
            {
                TempData["Error"] = _localizer["AnErrorOccurredCreatingSubscriptionPlan"].Value;
                return View(createDto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var plan = await _subscriptionPlanService.GetByIdAsync(id, cancellationToken);
                return View(plan);
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = _localizer["SubscriptionPlanNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = _localizer["AnErrorOccurredRetrievingSubscriptionPlan"].Value;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(
            Guid id,
            UpdateSubscriptionPlanDto updateDto,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(updateDto);
                }

                var updatedPlan = await _subscriptionPlanService.UpdateAsync(id, updateDto, cancellationToken);
                TempData["Success"] = _localizer["SubscriptionPlanUpdatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = _localizer["SubscriptionPlanNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(updateDto);
            }
            catch (Exception ex)
            {
                TempData["Error"] = _localizer["AnErrorOccurredUpdatingSubscriptionPlan"].Value;
                return View(updateDto);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                await _subscriptionPlanService.DeleteAsync(id, cancellationToken);
                TempData["Success"] = _localizer["SubscriptionPlanDeletedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = _localizer["SubscriptionPlanNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = _localizer["AnErrorOccurredDeletingSubscriptionPlan"].Value;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
