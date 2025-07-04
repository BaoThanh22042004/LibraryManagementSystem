namespace Domain.Entities;

/// <summary>
/// Base class for all domain entities, providing a unique identifier and audit timestamps.
/// Used for entity identification and tracking creation/modification times (UC047, audit logging).
/// </summary>
public abstract class BaseEntity
{
	public int Id { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime? LastModifiedAt { get; set; }
}
