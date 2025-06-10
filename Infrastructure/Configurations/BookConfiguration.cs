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
            t.HasCheckConstraint("CK_Book_Rating", "[Rating] >= 0 AND [Rating] <= 5");
            t.HasCheckConstraint("CK_Book_Copies", "[AvailableCopies] >= 0 AND [AvailableCopies] <= [TotalCopies]");
        });

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Author)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.ISBN)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.Publisher)
            .HasMaxLength(100);

        builder.Property(b => b.Description)
            .HasMaxLength(2000);

        builder.Property(b => b.Format)
            .HasConversion<int>();

        builder.Property(b => b.Status)
            .HasConversion<int>();

        builder.Property(b => b.Rating)
            .HasColumnType("decimal(3,2)");

        // Indexes
        builder.HasIndex(b => b.ISBN)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(b => b.Title);
        builder.HasIndex(b => b.Author);
        builder.HasIndex(b => b.Status);

        // Query filter for soft delete
        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
