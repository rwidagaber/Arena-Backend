using ArenaDomain.Shared;

namespace ArenaDomain.Entities.Localization;

public class Translation : BaseEntity<Guid>
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}
