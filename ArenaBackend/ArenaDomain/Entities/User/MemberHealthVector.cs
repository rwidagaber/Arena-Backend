using ArenaDomain.Shared;

namespace ArenaDomain.Entities
{
    public class MemberHealthVector : BaseEntity<Guid>
    {
        public Guid MemberProfileId { get; set; }
        public virtual MemberProfile MemberProfile { get; set; } = null!;

        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Legacy SQL Server column — kept nullable for backward compatibility.
        /// The live vector data is stored in PostgreSQL (Neon) via NeonVectorStore.
        /// </summary>
        public string? EmbeddingJson { get; set; }

        public DateTime RecordedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
