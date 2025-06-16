using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
{
    public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
    {
        builder.ToTable("SystemConfigurations", t =>
        {
            t.HasCheckConstraint("CK_SystemConfig_KeyNotEmpty", "[Key] != ''");
            t.HasCheckConstraint("CK_SystemConfig_ValueNotEmpty", "[Value] != ''");
        });

        // Indexes
        builder.HasIndex(sc => sc.Key)
            .IsUnique();
    }
}
