using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
	public void Configure(EntityTypeBuilder<Notification> builder)
	{
		builder.ToTable("Notifications");

		// Configure relationships
		builder.HasOne(n => n.User)
			.WithMany(u => u.Notifications)
			.HasForeignKey(n => n.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		// Configure properties
		builder.Property(n => n.UserId)
			.IsRequired(false);

		builder.Property(n => n.Type)
			.HasConversion<int>()
			.IsRequired();

		builder.Property(n => n.Status)
			.HasConversion<int>()
			.IsRequired();

		builder.Property(n => n.Subject)
			.HasMaxLength(200)
			.IsRequired();

		builder.Property(n => n.Message)
			.HasMaxLength(500)
			.IsRequired();

		builder.Property(n => n.SentAt)
			.IsRequired(false);

		builder.Property(n => n.ReadAt)
			.IsRequired(false);
	}
}
