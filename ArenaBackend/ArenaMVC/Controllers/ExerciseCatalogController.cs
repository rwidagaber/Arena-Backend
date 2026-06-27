using ArenaApplication.Dtos.Gym;
using ArenaApplication.Dtos.Workout;
using ArenaApplication.IServices;
using ArenaDomain.Shared;
using ArenaMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ExerciseCatalogController : Controller
    {
        private readonly IExerciseCatalogService _exerciseCatalogService;
        private readonly IEquipmentService _equipmentService;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public ExerciseCatalogController(
            IExerciseCatalogService exerciseCatalogService,
            IEquipmentService equipmentService,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _exerciseCatalogService = exerciseCatalogService;
            _equipmentService = equipmentService;
            _localizer = localizer;
        }

        private const int DefaultPageSize = 10;

        [HttpGet]
        public async Task<IActionResult> SearchPartial(string? search, int page = 1, int pageSize = DefaultPageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = DefaultPageSize;

            try
            {
                var result = await _exerciseCatalogService.GetAllAsync(search, page, pageSize);
                if (!result.IsSuccess)
                    return PartialView("_ExerciseResults", new ExerciseCatalogListPagedViewModel { Page = page, PageSize = pageSize, Search = search });

                var pagedResult = result.Value;
                var viewModel = new ExerciseCatalogListPagedViewModel
                {
                    Items = pagedResult.Items.ToList(),
                    TotalCount = pagedResult.TotalCount,
                    Page = page,
                    PageSize = pageSize,
                    Search = search
                };

                return PartialView("_ExerciseResults", viewModel);
            }
            catch (Exception)
            {
                return PartialView("_ExerciseResults", new ExerciseCatalogListPagedViewModel { Page = page, PageSize = pageSize, Search = search });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = DefaultPageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = DefaultPageSize;

            try
            {
                var result = await _exerciseCatalogService.GetAllAsync(search, page, pageSize);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.Errors?.FirstOrDefault() ?? _localizer["AnErrorOccurredRetrievingExerciseCatalogItems"].Value;
                    return View(new ExerciseCatalogListPagedViewModel { Page = page, PageSize = pageSize, Search = search });
                }

                var pagedResult = result.Value;
                var viewModel = new ExerciseCatalogListPagedViewModel
                {
                    Items = pagedResult.Items.ToList(),
                    TotalCount = pagedResult.TotalCount,
                    Page = page,
                    PageSize = pageSize,
                    Search = search
                };

                ViewBag.SearchString = search;
                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredRetrievingExerciseCatalogItems"].Value;
                return View(new ExerciseCatalogListPagedViewModel { Page = page, PageSize = pageSize, Search = search });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateEquipmentsViewBag();
            return View(new ExerciseCatalogItemDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExerciseCatalogItemDto dto)
        {
            if (ModelState.IsValid)
            {
                var result = await _exerciseCatalogService.CreateAsync(dto);
                if (result.IsSuccess)
                {
                    TempData["Success"] = _localizer["ExerciseCatalogItemCreatedSuccessfully"]?.Value ?? "Exercise catalog item created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["Error"] = result.Errors?.FirstOrDefault() ?? "Failed to create exercise catalog item.";
            }

            await PopulateEquipmentsViewBag();
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            if (id == Guid.Empty) return NotFound();

            var result = await _exerciseCatalogService.GetByIdAsync(id);
            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Errors?.FirstOrDefault() ?? _localizer["ExerciseCatalogItemNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            await PopulateEquipmentsViewBag();
            return View(result.Value);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ExerciseCatalogItemDto dto)
        {
            if (id != dto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var result = await _exerciseCatalogService.UpdateAsync(dto);
                if (result.IsSuccess)
                {
                    TempData["Success"] = _localizer["ExerciseCatalogItemUpdatedSuccessfully"]?.Value ?? "Exercise catalog item updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["Error"] = result.Errors?.FirstOrDefault() ?? "Failed to update exercise catalog item.";
            }

            await PopulateEquipmentsViewBag();
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var result = await _exerciseCatalogService.DeleteAsync(id);
            if (result.IsSuccess)
            {
                TempData["Success"] = _localizer["ExerciseCatalogItemDeletedSuccessfully"]?.Value ?? "Exercise catalog item deleted successfully.";
            }
            else
            {
                TempData["Error"] = result.Errors?.FirstOrDefault() ?? "Failed to delete exercise catalog item.";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateEquipmentsViewBag()
        {
            var equipmentResult = await _equipmentService.GetAllEquipmentsAsync(null, 1, 1000);
            if (equipmentResult.IsSuccess)
            {
                ViewBag.Equipments = equipmentResult.Value.Items.ToList();
            }
            else
            {
                ViewBag.Equipments = new List<EquipmentDto>();
            }
        }
    }
}
