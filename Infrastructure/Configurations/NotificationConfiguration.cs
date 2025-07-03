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
			.WithMany()
			.HasForeignKey(n => n.UserId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
