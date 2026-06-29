using ArenaApplication.Dtos.Workout;
using ArenaApplication.IServices;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MuscleGroupsController : Controller
    {
        private readonly IMuscleGroupService _muscleGroupService;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public MuscleGroupsController(
            IMuscleGroupService muscleGroupService,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _muscleGroupService = muscleGroupService;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var result = await _muscleGroupService.GetAllMuscleGroupsAsync();
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.Errors?.FirstOrDefault() ?? _localizer["AnErrorOccurredRetrievingMuscleGroups"].Value;
                    return View(System.Array.Empty<MuscleGroupDto>());
                }

                return View(result.Value);
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredRetrievingMuscleGroups"].Value;
                return View(System.Array.Empty<MuscleGroupDto>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MuscleGroupDto dto)
        {
            if (ModelState.IsValid)
            {
                var result = await _muscleGroupService.CreateMuscleGroupAsync(dto);
                if (result.IsSuccess)
                {
                    TempData["Success"] = _localizer["MuscleGroupCreatedSuccessfully"]?.Value ?? "Muscle group created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, result.Errors?.FirstOrDefault() ?? "An error occurred");
            }
            return View(dto);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            if (id == Guid.Empty) return NotFound();

            var result = await _muscleGroupService.GetMuscleGroupByIdAsync(id);
            if (result.IsSuccess)
            {
                return View(result.Value);
            }
            TempData["Error"] = result.Errors?.FirstOrDefault() ?? "An error occurred";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, MuscleGroupDto dto)
        {
            if (id != dto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var result = await _muscleGroupService.UpdateMuscleGroupAsync(dto);
                if (result.IsSuccess)
                {
                    TempData["Success"] = _localizer["MuscleGroupUpdatedSuccessfully"]?.Value ?? "Muscle group updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, result.Errors?.FirstOrDefault() ?? "An error occurred");
            }
            return View(dto);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty) return NotFound();

            var result = await _muscleGroupService.GetMuscleGroupByIdAsync(id);
            if (result.IsSuccess)
            {
                return View(result.Value);
            }
            TempData["Error"] = result.Errors?.FirstOrDefault() ?? "An error occurred";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var result = await _muscleGroupService.DeleteMuscleGroupAsync(id);
            if (result.IsSuccess)
            {
                TempData["Success"] = _localizer["MuscleGroupDeletedSuccessfully"]?.Value ?? "Muscle group deleted successfully.";
            }
            else
            {
                TempData["Error"] = result.Errors?.FirstOrDefault() ?? "An error occurred";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
