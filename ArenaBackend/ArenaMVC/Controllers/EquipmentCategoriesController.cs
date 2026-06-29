using ArenaApplication.Dtos.Gym;
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
    public class EquipmentCategoriesController : Controller
    {
        private readonly IEquipmentCategoryService _categoryService;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public EquipmentCategoriesController(
            IEquipmentCategoryService categoryService,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _categoryService = categoryService;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var result = await _categoryService.GetAllCategoriesAsync();
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.Errors?.FirstOrDefault() ?? _localizer["AnErrorOccurredRetrievingCategories"].Value;
                    return View(System.Array.Empty<EquipmentCategoryDto>());
                }

                return View(result.Value);
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredRetrievingCategories"].Value;
                return View(System.Array.Empty<EquipmentCategoryDto>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EquipmentCategoryDto dto)
        {
            if (ModelState.IsValid)
            {
                var result = await _categoryService.CreateCategoryAsync(dto);
                if (result.IsSuccess)
                {
                    TempData["Success"] = _localizer["CategoryCreatedSuccessfully"]?.Value ?? "Category created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, result.Errors?.FirstOrDefault() ?? "An error occurred");
            }
            return View(dto);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            if (id == Guid.Empty) return NotFound();

            var result = await _categoryService.GetCategoryByIdAsync(id);
            if (result.IsSuccess)
            {
                return View(result.Value);
            }
            TempData["Error"] = result.Errors?.FirstOrDefault() ?? "An error occurred";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EquipmentCategoryDto dto)
        {
            if (id != dto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var result = await _categoryService.UpdateCategoryAsync(dto);
                if (result.IsSuccess)
                {
                    TempData["Success"] = _localizer["CategoryUpdatedSuccessfully"]?.Value ?? "Category updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, result.Errors?.FirstOrDefault() ?? "An error occurred");
            }
            return View(dto);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty) return NotFound();

            var result = await _categoryService.GetCategoryByIdAsync(id);
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
            var result = await _categoryService.DeleteCategoryAsync(id);
            if (result.IsSuccess)
            {
                TempData["Success"] = _localizer["CategoryDeletedSuccessfully"]?.Value ?? "Category deleted successfully.";
            }
            else
            {
                TempData["Error"] = result.Errors?.FirstOrDefault() ?? "An error occurred";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
