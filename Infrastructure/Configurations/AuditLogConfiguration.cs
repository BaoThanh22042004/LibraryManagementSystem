using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
	public void Configure(EntityTypeBuilder<AuditLog> builder)
	{
		builder.ToTable("AuditLogs");
		
		// Indexes for faster lookup
		builder.HasIndex(a => a.UserId);
		builder.HasIndex(a => a.ActionType);
		builder.HasIndex(a => a.EntityType);
		builder.HasIndex(a => a.CreatedAt);
		
		// Configure BeforeState and AfterState to allow large values
		builder.Property(a => a.BeforeState)
			.HasColumnType("nvarchar(max)");
			
		builder.Property(a => a.AfterState)
			.HasColumnType("nvarchar(max)");
	}
}