using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Librarian> Librarians { get; set; }

    public DbSet<Book> Books { get; set; }
    public DbSet<BookCopy> BookCopies { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<BookCategory> BookCategories { get; set; }

    public DbSet<Loan> Loans { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Fine> Fines { get; set; }

    public DbSet<Notification> Notifications { get; set; }
    public DbSet<SystemConfiguration> SystemConfigurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);

        // Additional global configurations
        ConfigureGlobalFilters(modelBuilder);
        SeedData(modelBuilder);
    }

    private void ConfigureGlobalFilters(ModelBuilder modelBuilder)
    {
        // Configure decimal precision globally
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }

        // Configure string length limits globally
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(string)))
        {
            if (property.GetMaxLength() == null)
            {
                property.SetMaxLength(255);
            }
        }
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Fiction", Description = "Fiction books and novels", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Name = "Non-Fiction", Description = "Non-fiction books", CreatedAt = DateTime.UtcNow },
            new Category { Id = 3, Name = "Reference", Description = "Reference materials", CreatedAt = DateTime.UtcNow },
            new Category { Id = 4, Name = "Science Fiction", Description = "Science fiction novels", ParentCategoryId = 1, CreatedAt = DateTime.UtcNow }
        );

        // Seed system configurations
        modelBuilder.Entity<SystemConfiguration>().HasData(
            new SystemConfiguration
            {
                Id = 1,
                Key = "MaxLoansPerMember",
                Value = "5",
                Description = "Maximum number of books a member can borrow",
                Type = ConfigurationType.Integer,
                IsSystemConfig = true,
                CreatedAt = DateTime.UtcNow
            },
            new SystemConfiguration
            {
                Id = 2,
                Key = "LoanPeriodDays",
                Value = "14",
                Description = "Standard loan period in days",
                Type = ConfigurationType.Integer,
                IsSystemConfig = true,
                CreatedAt = DateTime.UtcNow
            },
            new SystemConfiguration
            {
                Id = 3,
                Key = "OverdueFinePerDay",
                Value = "0.50",
                Description = "Overdue fine amount per day",
                Type = ConfigurationType.Decimal,
                IsSystemConfig = true,
                CreatedAt = DateTime.UtcNow
            }
        );
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAt = DateTime.UtcNow;
            }
        }

        // Handle soft delete
        var softDeleteEntries = ChangeTracker.Entries<SoftDeleteEntity>()
            .Where(e => e.State == EntityState.Deleted);

        foreach (var entry in softDeleteEntries)
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = DateTime.UtcNow;
        }
    }
}
