using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", t =>
        {
            // Constraints
            t.HasCheckConstraint("CK_Category_NoSelfReference", "[Id] != [ParentCategoryId]");
		});

        // Indexes
        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Query filter for soft delete
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
