using Domain.Enums;

namespace Domain.Entities;

public class SystemConfiguration : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ConfigurationType Type { get; set; }
    public bool IsSystemConfig { get; set; }
}
