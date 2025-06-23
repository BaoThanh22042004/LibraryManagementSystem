using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class FineConfiguration : IEntityTypeConfiguration<Fine>
{
	public void Configure(EntityTypeBuilder<Fine> builder)
	{
		builder.ToTable("Fines", t =>
		{
			// Constraints
			t.HasCheckConstraint("CK_Fine_Amount", "[Amount] >= 0");
		});
	}
}
