using System;

namespace Domain.Entities;

public abstract class SoftDeleteEntity : BaseEntity
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
