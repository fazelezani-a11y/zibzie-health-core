using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Infrastructure.Persistence.Configurations;

public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> entity)
    {
        entity.ToTable("AdminUsers");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Username)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.DisplayName)
            .HasMaxLength(200);

        entity.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(1000);

        entity.Property(x => x.ProductRole)
            .IsRequired()
            .HasMaxLength(150);

        entity.Property(x => x.IsActive)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.HasIndex(x => x.Username)
            .IsUnique();

        entity.HasIndex(x => x.ProductRole);

        entity.HasIndex(x => x.IsActive);
    }
}
