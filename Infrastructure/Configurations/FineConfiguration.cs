using System;
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
            t.HasCheckConstraint("CK_Fine_AmountPaid", "[AmountPaid] >= 0 AND [AmountPaid] <= [Amount]");
        });

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Type)
            .HasConversion<int>();

        builder.Property(f => f.Status)
            .HasConversion<int>();

        builder.Property(f => f.Amount)
            .HasColumnType("decimal(10,2)");

        builder.Property(f => f.AmountPaid)
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0);

        builder.Property(f => f.Description)
            .IsRequired()
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(f => new { f.MemberId, f.Status });
        builder.HasIndex(f => f.FineDate);

        // Relationships
        builder.HasOne(f => f.Member)
            .WithMany(m => m.Fines)
            .HasForeignKey(f => f.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Loan)
            .WithMany()
            .HasForeignKey(f => f.LoanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
