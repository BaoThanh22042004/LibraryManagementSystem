using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Reflection.Emit;

namespace Infrastructure.Configurations;

public class FineConfiguration : IEntityTypeConfiguration<Fine>
{
    public void Configure(EntityTypeBuilder<Fine> builder)
    {
        builder.ToTable("Fines", t =>
        {
            // Constraints
            t.HasCheckConstraint("CK_Fine_Amount", "[Amount] >= 0");
            t.HasCheckConstraint("CK_Fine_AmountPaid", "[AmountPaid] >= 0 AND [AmountPaid] <= [Amount]");
        });

		builder.HasQueryFilter(f => !f.Member.User.IsDeleted);
	}
}
