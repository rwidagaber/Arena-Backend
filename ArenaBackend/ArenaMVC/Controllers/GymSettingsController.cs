using ArenaApplication.IServices;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArenaMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GymSettingsController : Controller
    {
        private readonly IGymSettingsService _settingsService;
        private readonly INoShowPenaltyService _penaltyService;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public GymSettingsController(
            IGymSettingsService settingsService,
            INoShowPenaltyService penaltyService,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _settingsService = settingsService;
            _penaltyService = penaltyService;
            _localizer = localizer;
        }

        // GET: GymSettings
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            try
            {
                var settings = await _settingsService.GetGymSettingsAsync(cancellationToken);
                ViewBag.Threshold = settings.NoShowThreshold;
                ViewBag.IsEnabled = settings.IsNoShowPenaltyEnabled;
                return View();
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredLoadingSettings"].Value;
                ViewBag.Threshold = 2;
                ViewBag.IsEnabled = true;
                return View();
            }
        }

        // POST: GymSettings/UpdateSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(int noShowThreshold, bool isNoShowPenaltyEnabled, CancellationToken cancellationToken)
        {
            if (noShowThreshold < 1)
            {
                TempData["Error"] = _localizer["ThresholdMustBeGreaterThanZero"].Value;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _settingsService.UpdateGymSettingsAsync(noShowThreshold, isNoShowPenaltyEnabled, cancellationToken);
                TempData["Success"] = _localizer["SettingsUpdatedSuccessfully"].Value;
            }
            catch (Exception)
            {
                TempData["Error"] = _localizer["AnErrorOccurredUpdatingSettings"].Value;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: GymSettings/RunPenaltyProcess
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunPenaltyProcess(CancellationToken cancellationToken)
        {
            try
            {
                await _penaltyService.ProcessNoShowPenaltiesAsync(cancellationToken);
                TempData["Success"] = _localizer["PenaltyProcessorCompletedSuccessfully"].Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RunPenaltyProcess] Exception occurred: {ex}");
                TempData["Error"] = $"{_localizer["AnErrorOccurredRunningPenaltyProcessor"].Value}: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
