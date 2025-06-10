using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans", t =>
        {
            // Constraints
            t.HasCheckConstraint("CK_Loan_Dates", "[DueDate] > [LoanDate]");
            t.HasCheckConstraint("CK_Loan_RenewalCount", "[RenewalCount] >= 0 AND [RenewalCount] <= 2");
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Status)
            .HasConversion<int>();

        builder.Property(l => l.OverdueFine)
            .HasColumnType("decimal(10,2)");

        // Indexes
        builder.HasIndex(l => new { l.MemberId, l.Status });
        builder.HasIndex(l => l.DueDate);
        builder.HasIndex(l => l.IsOverdue);

        // Relationships
        builder.HasOne(l => l.Member)
            .WithMany(m => m.Loans)
            .HasForeignKey(l => l.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Book)
            .WithMany(b => b.Loans)
            .HasForeignKey(l => l.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.BookCopy)
            .WithMany(bc => bc.Loans)
            .HasForeignKey(l => l.BookCopyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
