using ArenaApplication.Dtos.HealthIntelligence;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface IHealthIntelligenceService
    {
        Task<HealthProfileDto> ExtractHealthProfileAsync(string userMessage);
        Task<string> RetrieveMedicalGuidelinesAsync(HealthProfileDto profile);
        Task<ValidationResultDto> ValidatePlanAsync(HealthProfileDto profile, string planJson, string planType);
    }
}
