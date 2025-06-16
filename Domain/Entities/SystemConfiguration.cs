using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class SystemConfiguration : BaseEntity
{
    [MaxLength(100)]
	public string Key { get; set; } = string.Empty;
    [MaxLength(2000)]
	public string Value { get; set; } = string.Empty;
    [MaxLength(500)]
	public string? Description { get; set; }
    public ConfigurationType Type { get; set; }
    public bool IsSystemConfig { get; set; }
}
