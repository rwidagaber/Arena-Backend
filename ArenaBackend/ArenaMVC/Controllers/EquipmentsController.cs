using ArenaApplication.Dtos.Gym;
using ArenaApplication.IServices;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Threading.Tasks;

using System.Linq;
using ArenaMVC.Models;

namespace ArenaMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EquipmentsController : Controller
    {
        private readonly IEquipmentService _equipmentService;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public EquipmentsController(IEquipmentService equipmentService, IStringLocalizer<ArenaLocalization> localizer)
        {
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
                var result = await _equipmentService.GetAllEquipmentsAsync(search, page, pageSize);
                if (!result.IsSuccess)
                    return PartialView("_EquipmentResults", new EquipmentListPagedViewModel { Page = page, PageSize = pageSize, Search = search });

                var pagedResult = result.Value;
                var viewModel = new EquipmentListPagedViewModel
                {
                    Items = pagedResult.Items.ToList(),
                    TotalCount = pagedResult.TotalCount,
                    Page = page,
                    PageSize = pageSize,
                    Search = search
                };

                return PartialView("_EquipmentResults", viewModel);
            }
            catch (Exception)
            {
                return PartialView("_EquipmentResults", new EquipmentListPagedViewModel { Page = page, PageSize = pageSize, Search = search });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = DefaultPageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = DefaultPageSize;

            try
            {
                var result = await _equipmentService.GetAllEquipmentsAsync(search, page, pageSize);
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.Errors?.FirstOrDefault() ?? _localizer["AnErrorOccurredRetrievingEquipments"].Value;
                    return View(new EquipmentListPagedViewModel { Page = page, PageSize = pageSize, Search = search });
                }

                var pagedResult = result.Value;
                var viewModel = new EquipmentListPagedViewModel
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
                TempData["Error"] = _localizer["AnErrorOccurredRetrievingEquipments"].Value;
                return View(new EquipmentListPagedViewModel { Page = page, PageSize = pageSize, Search = search });
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EquipmentDto dto)
        {
            if (ModelState.IsValid)
            {
                var result = await _equipmentService.CreateEquipmentAsync(dto);
                if (result.IsSuccess)
                {
                    TempData["Success"] = _localizer["EquipmentCreatedSuccessfully"]?.Value ?? "Equipment created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, result.Errors?.FirstOrDefault() ?? "An error occurred");
            }
            return View(dto);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            if (id == Guid.Empty) return NotFound();

            var result = await _equipmentService.GetEquipmentByIdAsync(id);
            if (result.IsSuccess)
            {
                return View(result.Value);
            }
            TempData["Error"] = result.Errors?.FirstOrDefault() ?? "An error occurred";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EquipmentDto dto)
        {
            if (id != dto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var result = await _equipmentService.UpdateEquipmentAsync(dto);
                if (result.IsSuccess)
                {
                    TempData["Success"] = _localizer["EquipmentUpdatedSuccessfully"]?.Value ?? "Equipment updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, result.Errors?.FirstOrDefault() ?? "An error occurred");
            }
            return View(dto);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty) return NotFound();

            var result = await _equipmentService.GetEquipmentByIdAsync(id);
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
            var result = await _equipmentService.DeleteEquipmentAsync(id);
            if (result.IsSuccess)
            {
                TempData["Success"] = _localizer["EquipmentDeletedSuccessfully"]?.Value ?? "Equipment deleted successfully.";
            }
            else
            {
                TempData["Error"] = result.Errors?.FirstOrDefault() ?? "An error occurred";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
