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

        // Indexes
        builder.HasIndex(m => m.MembershipNumber)
            .IsUnique();
        
        // Add matching query filter to align with User's soft delete filter
        builder.HasQueryFilter(m => !m.User.IsDeleted);
    }
}
