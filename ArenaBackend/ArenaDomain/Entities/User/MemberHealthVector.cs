using ArenaDomain.Shared;

namespace ArenaDomain.Entities
{
    public class MemberHealthVector : BaseEntity<Guid>
    {
        public Guid MemberProfileId { get; set; }
        public virtual MemberProfile MemberProfile { get; set; } = null!;

        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string EmbeddingJson { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
