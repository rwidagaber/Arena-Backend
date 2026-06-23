using ArenaApplication.Dtos.Gym;
using ArenaApplication.Services.Gym;
using ArenaDomain.Shared;
using ArenaMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArenaMVC.Controllers
{
    public class WorkingHoursController : Controller
    {
        private readonly IWorkingHoursService _workingHoursService;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public WorkingHoursController(
            IWorkingHoursService workingHoursService,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _workingHoursService = workingHoursService;
            _localizer = localizer;
        }

        // GET: WorkingHours
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            try
            {
                var hours = await _workingHoursService.GetWorkingHoursAsync(cancellationToken);
                return View(hours);
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredRetrievingWorkingHours"].Value;
                return View(new List<WorkingHoursDto>());
            }
        }

        // POST: WorkingHours/BulkEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkEdit(List<int> ids, EditWorkingHoursViewModel model, CancellationToken cancellationToken)
        {
            if (ids == null || !ids.Any())
            {
                TempData["Error"] = _localizer["NoDaysSelected"].Value;
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = _localizer["PleaseFixErrors"].Value;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var updateDto = new UpdateWorkingHoursDto
                {
                    IsClosed = model.IsClosed,
                    OpenTime = model.IsClosed ? null : model.OpenTime,
                    CloseTime = model.IsClosed ? null : model.CloseTime
                };

                await _workingHoursService.BulkUpdateWorkingHoursAsync(ids, updateDto, cancellationToken);

                TempData["Success"] = _localizer["WorkingHoursUpdatedSuccessfully"].Value;
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredUpdatingWorkingHours"].Value;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: WorkingHours/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditWorkingHoursViewModel model, CancellationToken cancellationToken)
        {
            // model.Id is now int? — guard against a null or mismatched Id
            if (!model.Id.HasValue || id != model.Id.Value)
            {
                TempData["Error"] = _localizer["AnErrorOccurredUpdatingWorkingHours"].Value;
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var updateDto = new UpdateWorkingHoursDto
                {
                    IsClosed = model.IsClosed,
                    OpenTime = model.IsClosed ? null : model.OpenTime,
                    CloseTime = model.IsClosed ? null : model.CloseTime
                };

                await _workingHoursService.UpdateWorkingHoursAsync(id, updateDto, cancellationToken);
                TempData["Success"] = _localizer["WorkingHoursUpdatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredUpdatingWorkingHours"].Value;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
