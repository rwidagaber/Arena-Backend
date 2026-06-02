using ArenaApplication.Dtos.SubscriptionPlanDtos;
using ArenaApplication.Services.SubscriptionPlan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArenaMVC.Controllers
{
    //[Authorize(Roles = "Admin")]
    public class SubscriptionPlansController : Controller
    {
        private readonly ISubscriptionPlanService _subscriptionPlanService;

        public SubscriptionPlansController(ISubscriptionPlanService subscriptionPlanService)
        {
            _subscriptionPlanService = subscriptionPlanService;
        }

        /// <summary>
        /// Get all subscription plans (Admin only)
        /// </summary>
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
                TempData["Error"] = "An error occurred while retrieving subscription plans.";
                return View(new List<SubscriptionPlanDto>());
            }
        }

        /// <summary>
        /// Display create form
        /// </summary>
        [HttpGet("create")]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Create a new subscription plan (Admin only)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(
            SubscriptionPlanDto createDto,
            CancellationToken cancellationToken
        )
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(createDto);
                }

                var createdPlan = await _subscriptionPlanService.CreateAsync(
                    createDto,
                    cancellationToken
                );
                TempData["Success"] = "Subscription plan created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(createDto);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while creating the subscription plan.";
                return View(createDto);
            }
        }

        /// <summary>
        /// Display edit form
        /// </summary>
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var plan = await _subscriptionPlanService.GetByIdAsync(id, cancellationToken);
                return View(plan);
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = "Subscription plan not found.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while retrieving the subscription plan.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Update a subscription plan (Admin only)
        /// </summary>
        [HttpPost("edit/{id}")]
        public async Task<IActionResult> Edit(
            Guid id,
            UpdateSubscriptionPlanDto updateDto,
            CancellationToken cancellationToken
        )
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(updateDto);
                }

                var updatedPlan = await _subscriptionPlanService.UpdateAsync(
                    id,
                    updateDto,
                    cancellationToken
                );
                TempData["Success"] = "Subscription plan updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = "Subscription plan not found.";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(updateDto);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating the subscription plan.";
                return View(updateDto);
            }
        }

        /// <summary>
        /// Delete a subscription plan (Admin only)
        /// </summary>
        [HttpPost("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                await _subscriptionPlanService.DeleteAsync(id, cancellationToken);
                TempData["Success"] = "Subscription plan deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException ex)
            {
                TempData["Error"] = "Subscription plan not found.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while deleting the subscription plan.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
