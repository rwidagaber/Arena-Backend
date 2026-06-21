using ArenaApplication.AI;
using ArenaApplication.Dtos.Nutrition;
using ArenaApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArenaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NutritionPlansController : ControllerBase
    {
        private readonly INutritionPlanService _nutritionPlanService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IGeminiCompletionService _geminiCompletionService;
        private readonly IMealLogService _mealLogService;

        public NutritionPlansController(
            INutritionPlanService nutritionPlanService,
            ICurrentUserService currentUserService,
            IGeminiCompletionService geminiCompletionService,
            IMealLogService mealLogService)
        {
            _nutritionPlanService = nutritionPlanService;
            _currentUserService = currentUserService;
            _geminiCompletionService = geminiCompletionService;
            _mealLogService = mealLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNutritionPlans()
        {
            var memberProfileId = _currentUserService.MemberProfileId;
            var result = await _nutritionPlanService.GetNutritionPlansByMemberIdAsync(memberProfileId);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return Ok(result.Value);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetMyActiveNutritionPlan()
        {
            var memberProfileId = _currentUserService.MemberProfileId;
            var result = await _nutritionPlanService.GetActiveNutritionPlanByMemberIdAsync(memberProfileId);
            if (!result.IsSuccess)
                return NotFound(result.Errors);

            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNutritionPlanById(Guid id)
        {
            Guid? memberProfileId = null;
            if (!User.IsInRole("Admin") && !User.IsInRole("Trainer"))
            {
                memberProfileId = _currentUserService.MemberProfileId;
            }

            var result = await _nutritionPlanService.GetNutritionPlanByIdAsync(id, memberProfileId);
            if (!result.IsSuccess)
                return NotFound(result.Errors);

            return Ok(result.Value);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNutritionPlan(Guid id)
        {
            Guid? memberProfileId = null;
            if (!User.IsInRole("Admin") && !User.IsInRole("Trainer"))
            {
                memberProfileId = _currentUserService.MemberProfileId;
            }

            var result = await _nutritionPlanService.DeleteNutritionPlanAsync(id, memberProfileId);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return NoContent();
        }

        [HttpPost("analyze-meal-image")]
        [RequestSizeLimit(8 * 1024 * 1024)]
        public async Task<IActionResult> AnalyzeMealImage([FromForm] IFormFile image, [FromForm] bool logMeal = true)
        {
            if (image == null || image.Length == 0)
                return BadRequest("Please upload a meal image.");

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(image.ContentType, StringComparer.OrdinalIgnoreCase))
                return BadRequest("Only JPG, PNG, and WEBP images are supported.");

            if (image.Length > 8 * 1024 * 1024)
                return BadRequest("Image size must be 8 MB or less.");

            await using var stream = image.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            var systemPrompt = """
                You are Arena's meal image nutrition analyst.
                Estimate visible foods and return ONLY valid JSON.
                Values must be realistic estimates from the image, not medical advice.
                Macro percentages must add up to 100 and represent calorie share:
                protein calories = protein grams * 4, carbs calories = carbs grams * 4, fat calories = fat grams * 9.
                If the image is unclear, lower confidence and explain that in notes.
                Do not write markdown, explanations, or any text outside the JSON object.
                Every numeric field must be a JSON number, not a string.
                JSON shape:
                {
                  "mealName": "",
                  "summary": "",
                  "estimatedCalories": 0,
                  "protein": { "grams": 0, "percentage": 0 },
                  "carbs": { "grams": 0, "percentage": 0 },
                  "fat": { "grams": 0, "percentage": 0 },
                  "confidence": 0,
                  "detectedFoods": [""],
                  "notes": [""]
                }
                """;

            try
            {
                var raw = await _geminiCompletionService.GetVisionCompletionAsync(
                    systemPrompt,
                    "Analyze this meal photo. Return exactly one JSON object using the requested schema. Do not include markdown, comments, units, or text before/after the JSON.",
                    image.ContentType,
                    Convert.ToBase64String(memoryStream.ToArray()));

                var result = await ParseOrRepairMealImageAnalysisAsync(raw);

                if (result == null)
                    return BadRequest("Unable to analyze this image. Please try another clear meal photo.");

                // Link the analysis to the member's nutrition plan: log the meal and
                // report how many calories are left of today's target.
                if (logMeal)
                {
                    var memberProfileId = _currentUserService.MemberProfileId;
                    var logResult = await _mealLogService.LogAnalyzedMealAsync(memberProfileId, result);

                    if (logResult.IsSuccess)
                        return Ok(logResult.Value);
                }

                return Ok(result);
            }
            catch (InvalidOperationException ex) when (IsGeminiConfigurationError(ex.Message))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Meal analysis AI is not configured. Set a valid Gemini API key on the backend.");
            }
            catch (JsonException)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    "Meal analysis returned an unexpected format. Please try again, or use a clearer image with the meal centered.");
            }
            catch (Exception ex) when (IsGeminiConfigurationError(ex.Message))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Meal analysis AI is not configured. Set a valid Gemini API key on the backend.");
            }
            catch (Exception ex) when (IsGeminiProviderError(ex.Message))
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    $"Meal analysis AI error: {CreateSafeProviderErrorMessage(ex.Message)}");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred during meal analysis. Please try again.");
            }
        }

        [HttpGet("daily-summary")]
        public async Task<IActionResult> GetDailySummary([FromQuery] DateTime? date = null)
        {
            var memberProfileId = _currentUserService.MemberProfileId;
            var result = await _mealLogService.GetDailySummaryAsync(memberProfileId, date);

            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return Ok(result.Value);
        }

        private async Task<MealImageAnalysisDto?> ParseOrRepairMealImageAnalysisAsync(string raw)
        {
            if (TryParseMealImageAnalysis(raw, out var parsed))
                return parsed;

            var repairPrompt = """
                Convert the user's text into ONLY valid JSON for this exact schema:
                {
                  "mealName": "",
                  "summary": "",
                  "estimatedCalories": 0,
                  "protein": { "grams": 0, "percentage": 0 },
                  "carbs": { "grams": 0, "percentage": 0 },
                  "fat": { "grams": 0, "percentage": 0 },
                  "confidence": 0,
                  "detectedFoods": [""],
                  "notes": [""]
                }
                Rules:
                - Return one JSON object only.
                - Use JSON numbers, not strings, for all numeric fields.
                - If a value is unknown, estimate conservatively from the text or use 0.
                """;

            var repaired = await _geminiCompletionService.GetCompletionAsync(
                repairPrompt,
                [],
                raw);

            return TryParseMealImageAnalysis(repaired, out var repairedParsed)
                ? repairedParsed
                : null;
        }

        private static bool TryParseMealImageAnalysis(string raw, out MealImageAnalysisDto? analysis)
        {
            analysis = null;

            try
            {
                var cleanJson = AIHelper.CleanJson(raw);
                analysis = ParseMealImageAnalysis(cleanJson);
                return analysis != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool IsGeminiConfigurationError(string message)
        {
            return message.Contains("api key", StringComparison.OrdinalIgnoreCase) ||
                   message.Contains("API_KEY_INVALID", StringComparison.OrdinalIgnoreCase) ||
                   message.Contains("key not valid", StringComparison.OrdinalIgnoreCase) ||
                   message.Contains("permission", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsGeminiProviderError(string message)
        {
            return message.Contains("Gemini", StringComparison.OrdinalIgnoreCase) ||
                   message.Contains("model", StringComparison.OrdinalIgnoreCase) ||
                   message.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
                   message.Contains("rate", StringComparison.OrdinalIgnoreCase) ||
                   message.Contains("temporarily", StringComparison.OrdinalIgnoreCase);
        }

        private static string CreateSafeProviderErrorMessage(string message)
        {
            if (message.Contains("API key", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("API_KEY", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("key not valid", StringComparison.OrdinalIgnoreCase))
            {
                return "Gemini API key is invalid or missing.";
            }

            if (message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("not supported", StringComparison.OrdinalIgnoreCase))
            {
                return "The configured Gemini model does not support this image analysis request.";
            }

            if (message.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("rate", StringComparison.OrdinalIgnoreCase))
            {
                return "Gemini quota or rate limit was reached.";
            }

            if (message.Contains("image", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("inline", StringComparison.OrdinalIgnoreCase))
            {
                return "Gemini rejected the uploaded image. Try JPG or PNG under 8 MB.";
            }

            return "Gemini could not complete the meal analysis request.";
        }

        private static MealImageAnalysisDto? ParseMealImageAnalysis(string json)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return null;

            var analysis = new MealImageAnalysisDto
            {
                MealName = ReadString(root, "mealName", "meal", "name", "title"),
                Summary = ReadString(root, "summary", "description", "analysis"),
                EstimatedCalories = ReadDecimal(root, "estimatedCalories", "calories", "kcal", "totalCalories"),
                Confidence = ReadDecimal(root, "confidence", "confidenceScore"),
                Protein = ReadMacro(root, "protein", "proteinGrams", "proteinPercentage"),
                Carbs = ReadMacro(root, "carbs", "carbohydrates", "carb", "carbsGrams", "carbsPercentage"),
                Fat = ReadMacro(root, "fat", "fats", "lipids", "fatGrams", "fatPercentage"),
                DetectedFoods = ReadStringList(root, "detectedFoods", "foods", "ingredients", "foodItems"),
                Notes = ReadStringList(root, "notes", "warnings", "recommendations")
            };

            if (string.IsNullOrWhiteSpace(analysis.MealName))
                analysis.MealName = "Analyzed meal";

            if (string.IsNullOrWhiteSpace(analysis.Summary))
                analysis.Summary = "Estimated nutrition based on visible foods in the uploaded image.";

            NormalizeMacroPercentages(analysis);

            return analysis;
        }

        private static MacroPercentageDto ReadMacro(
            JsonElement root,
            string objectName,
            string gramsFallbackName,
            string percentageFallbackName)
        {
            return ReadMacro(root, objectName, objectName, objectName, gramsFallbackName, percentageFallbackName);
        }

        private static MacroPercentageDto ReadMacro(
            JsonElement root,
            string objectName,
            string alternateObjectName,
            string secondAlternateObjectName,
            string gramsFallbackName,
            string percentageFallbackName)
        {
            foreach (var name in new[] { objectName, alternateObjectName, secondAlternateObjectName }.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!TryGetProperty(root, name, out var macro))
                    continue;

                if (macro.ValueKind == JsonValueKind.Object)
                {
                    return new MacroPercentageDto
                    {
                        Grams = ReadDecimal(macro, "grams", "gram", "g", "amount", "value"),
                        Percentage = ReadDecimal(macro, "percentage", "percent", "pct", "caloriePercentage")
                    };
                }

                return new MacroPercentageDto
                {
                    Grams = ReadDecimalValue(macro)
                };
            }

            return new MacroPercentageDto
            {
                Grams = ReadDecimal(root, gramsFallbackName),
                Percentage = ReadDecimal(root, percentageFallbackName)
            };
        }

        private static string ReadString(JsonElement element, params string[] names)
        {
            foreach (var name in names)
            {
                if (TryGetProperty(element, name, out var value))
                    return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
            }

            return string.Empty;
        }

        private static decimal ReadDecimal(JsonElement element, params string[] names)
        {
            foreach (var name in names)
            {
                if (TryGetProperty(element, name, out var value))
                    return ReadDecimalValue(value);
            }

            return 0;
        }

        private static decimal ReadDecimalValue(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
                return Math.Round(number, 1);

            if (value.ValueKind != JsonValueKind.String)
                return 0;

            var text = value.GetString();
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            var numericText = new string(text
                .Where(ch => char.IsDigit(ch) || ch == '.' || ch == ',' || ch == '-')
                .ToArray())
                .Replace(",", ".");

            return decimal.TryParse(
                numericText,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsed)
                ? Math.Round(parsed, 1)
                : 0;
        }

        private static List<string> ReadStringList(JsonElement element, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetProperty(element, name, out var value))
                    continue;

                if (value.ValueKind == JsonValueKind.Array)
                {
                    return value.EnumerateArray()
                        .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString())
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Select(item => item!.Trim())
                        .ToList();
                }

                if (value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString()?
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToList() ?? [];
                }
            }

            return [];
        }

        private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static void NormalizeMacroPercentages(MealImageAnalysisDto analysis)
        {
            var proteinCalories = analysis.Protein.Grams * 4;
            var carbsCalories = analysis.Carbs.Grams * 4;
            var fatCalories = analysis.Fat.Grams * 9;
            var macroCalories = proteinCalories + carbsCalories + fatCalories;

            if (analysis.EstimatedCalories <= 0 && macroCalories > 0)
                analysis.EstimatedCalories = Math.Round(macroCalories, 0);

            if (analysis.Protein.Percentage <= 0 && analysis.Carbs.Percentage <= 0 && analysis.Fat.Percentage <= 0 && macroCalories > 0)
            {
                analysis.Protein.Percentage = Math.Round(proteinCalories / macroCalories * 100, 1);
                analysis.Carbs.Percentage = Math.Round(carbsCalories / macroCalories * 100, 1);
                analysis.Fat.Percentage = Math.Round(fatCalories / macroCalories * 100, 1);
            }
        }
    }
}
