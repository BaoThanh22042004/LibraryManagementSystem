using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations", t =>
        {
            // Constraints
            t.HasCheckConstraint("CK_Reservation_QueuePosition", "[QueuePosition] > 0");
        });

        // Add matching query filter to align with Book's soft delete filter
        builder.HasQueryFilter(r => !r.Book.IsDeleted && !r.Member.User.IsDeleted);
    }
}
