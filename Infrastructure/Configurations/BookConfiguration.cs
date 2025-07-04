using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

/// <summary>
/// Entity Framework configuration for the Book entity.
/// Supports UC010-UC014 by providing proper database schema and constraints.
/// </summary>
/// <remarks>
/// Key Configurations:
/// - Enforces ISBN uniqueness across the catalog (UC010.E1, UC013)
/// - Provides proper table naming and structure
/// - Supports efficient querying for search operations (UC013)
/// - Enables category browsing functionality (UC014)
/// 
/// Business Rules Enforced:
/// - BR-06: Book management rights through proper schema design
/// - BR-22: Audit logging support through BaseEntity configuration
/// </remarks>
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
	public void Configure(EntityTypeBuilder<Book> builder)
	{
		builder.ToTable("Books");

		// Enforce ISBN uniqueness across the catalog
		// This supports UC010.E1 (Duplicate ISBN Found) and UC013 (Search Books)
		builder.HasIndex(b => b.ISBN)
			.IsUnique();
	}
}
