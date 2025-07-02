using Domain.Entities;
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

	public DbSet<Loan> Loans { get; set; }
	public DbSet<Reservation> Reservations { get; set; }
	public DbSet<Fine> Fines { get; set; }

	public DbSet<Notification> Notifications { get; set; }

	public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Apply all configurations
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);

		// Additional global configurations
		ConfigureGlobalFilters(modelBuilder);
	}

	private static void ConfigureGlobalFilters(ModelBuilder modelBuilder)
	{
		var entityTypes = modelBuilder.Model.GetEntityTypes();

		// Configure decimal precision globally
		foreach (var property in entityTypes
			.SelectMany(t => t.GetProperties())
			.Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
		{
			property.SetColumnType("decimal(18,2)");
		}

		// Configure string length limits globally
		entityTypes.SelectMany(t => t.GetProperties())
				   .Where(p => p.ClrType == typeof(string) && p.GetMaxLength() == null)
				   .ToList()
				   .ForEach(p => p.SetMaxLength(255));

		// Store enum values as strings for better readability in the database
		foreach (var entityType in entityTypes)
		{
			foreach (var property in entityType.GetProperties())
			{
				if (property.ClrType.IsEnum)
				{
					modelBuilder
						.Entity(entityType.ClrType)
						.Property(property.Name)
						.HasConversion<string>();
				}
			}
		}

		foreach (var entityType in entityTypes)
		{
			foreach (var property in entityType.GetProperties())
			{
				if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
				{
					property.SetColumnType("datetime2");
				}
			}
		}
	}
}
