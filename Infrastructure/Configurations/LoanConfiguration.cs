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
            t.HasCheckConstraint("CK_Loan_ReturnDate", "[ReturnDate] IS NULL OR [ReturnDate] >= [LoanDate]");
            t.HasCheckConstraint("CK_Loan_RenewalCount", "[RenewalCount] >= 0 AND [RenewalCount] <= 2");
            t.HasCheckConstraint("CK_Loan_OverdueFine", "[OverdueFine] IS NULL OR [OverdueFine] >= 0");
        });

        builder.HasQueryFilter(l => !l.BookCopy.IsDeleted);
    }
}
