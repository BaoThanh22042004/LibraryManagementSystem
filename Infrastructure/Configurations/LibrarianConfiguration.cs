using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class LibrarianConfiguration : IEntityTypeConfiguration<Librarian>
{
	public void Configure(EntityTypeBuilder<Librarian> builder)
	{
		builder.ToTable("Librarians");

		builder.HasIndex(l => l.EmployeeId).IsUnique();
	}
}