using ArenaDomain.Shared;

namespace ArenaDomain.Entities
{
    public class MemberHealthVector : BaseEntity<Guid>
    {
        public Guid MemberProfileId { get; set; }
        public virtual MemberProfile MemberProfile { get; set; } = null!;

        public string Content { get; set; } = string.Empty;
        // e.g. "Member has a left knee injury from a previous sports accident"

        public string Category { get; set; } = string.Empty;
        // "Injury" | "HealthCondition" | "DietaryRestriction" | "Goal" | "ChatMention"

        public string EmbeddingJson { get; set; } = string.Empty;
        // serialized float[] as JSON

        public DateTime RecordedAt { get; set; }
        public bool IsActive { get; set; } = true;
        // can deactivate if injury healed, etc.
    }
}