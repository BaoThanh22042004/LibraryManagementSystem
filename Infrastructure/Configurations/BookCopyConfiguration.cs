using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
	public void Configure(EntityTypeBuilder<BookCopy> builder)
	{
		builder.ToTable("BookCopies");
	}
}