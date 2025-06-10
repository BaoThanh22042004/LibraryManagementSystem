using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members", t =>
        {
            // Constraints
            t.HasCheckConstraint("CK_Member_LoanCount", "[CurrentLoanCount] >= 0 AND [CurrentLoanCount] <= 5");
            t.HasCheckConstraint("CK_Member_ReservationCount", "[CurrentReservationCount] >= 0 AND [CurrentReservationCount] <= 3");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MembershipNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(m => m.MembershipStatus)
            .HasConversion<int>();

        builder.Property(m => m.OutstandingFines)
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0);

        builder.Property(m => m.CurrentLoanCount)
            .HasDefaultValue(0);

        builder.Property(m => m.CurrentReservationCount)
            .HasDefaultValue(0);

        // Indexes
        builder.HasIndex(m => m.MembershipNumber)
            .IsUnique();

        builder.HasIndex(m => m.MembershipStatus);
    }
}
