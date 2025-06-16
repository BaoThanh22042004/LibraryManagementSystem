using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.ToTable("BookCopies");

        // Improved query filter to handle cases when Book isn't loaded and to support soft delete
        builder.HasQueryFilter(bc => !bc.IsDeleted);

        // Indexes
        builder.HasIndex(bc => bc.Barcode)
            .IsUnique()
            .HasFilter("[Barcode] IS NOT NULL AND [Barcode] != '' AND [IsDeleted] = 0");
    }
}