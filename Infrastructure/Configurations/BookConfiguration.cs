using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books", t =>
        {
            // Constraints
            t.HasCheckConstraint("CK_Book_Copies", "[AvailableCopies] >= 0 AND [AvailableCopies] <= [TotalCopies]");
            t.HasCheckConstraint("CK_Book_TotalCopies", "[TotalCopies] >= 0");
            t.HasCheckConstraint("CK_Book_ReservedCopies", 
                "[ReservedCopies] >= 0 AND [AvailableCopies] + [ReservedCopies] <= [TotalCopies]");
        });

        // Indexes
        builder.HasIndex(b => b.ISBN)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Query filter for soft delete
        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
