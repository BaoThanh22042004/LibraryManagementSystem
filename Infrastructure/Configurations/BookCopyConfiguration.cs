using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

/// <summary>
/// Entity Framework configuration for the BookCopy entity.
/// Supports UC015-UC017 by providing proper database schema and constraints.
/// </summary>
/// <remarks>
/// Key Configurations:
/// - Enforces copy number uniqueness within each book (UC015.E2)
/// - Provides efficient indexing for status-based queries (UC016)
/// - Supports proper foreign key relationships
/// - Enables efficient copy management operations
/// 
/// Business Rules Enforced:
/// - BR-08: Copy deletion restrictions through proper relationships
/// - BR-09: Copy status rules through status indexing
/// - BR-10: Copy return validation through status tracking
/// </remarks>
public class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
	public void Configure(EntityTypeBuilder<BookCopy> builder)
	{
		builder.ToTable("BookCopies");

		// Indexes for performance optimization
		// BookId index for efficient book-copy relationship queries
		builder.HasIndex(bc => bc.BookId);
		
		// Status index for efficient status-based filtering (UC016)
		builder.HasIndex(bc => bc.Status);
		
		// Composite unique index for copy number within each book
		// This enforces UC015.E2 (Duplicate Copy Number) constraint
		builder.HasIndex(bc => new { bc.BookId, bc.CopyNumber }).IsUnique();
	}
}