namespace ArenaApplication.Dtos.HealthIntelligence
{
    public class ValidationResultDto
    {
        public bool IsValid { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
    }
}
