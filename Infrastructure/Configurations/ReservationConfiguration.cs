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

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status)
            .HasConversion<int>();

        // Indexes
        builder.HasIndex(r => new { r.BookId, r.QueuePosition });
        builder.HasIndex(r => new { r.MemberId, r.Status });
        builder.HasIndex(r => r.ReservationDate);

        // Relationships
        builder.HasOne(r => r.Member)
            .WithMany(m => m.Reservations)
            .HasForeignKey(r => r.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Book)
            .WithMany(b => b.Reservations)
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
