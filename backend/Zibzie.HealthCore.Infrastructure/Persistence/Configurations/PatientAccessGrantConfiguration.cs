using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Infrastructure.Persistence.Configurations;

public class PatientAccessGrantConfiguration : IEntityTypeConfiguration<PatientAccessGrant>
{
    public void Configure(EntityTypeBuilder<PatientAccessGrant> entity)
    {
        entity.ToTable("PatientAccessGrants");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.PatientId)
            .IsRequired();

        entity.Property(x => x.ServiceAccountId)
            .HasMaxLength(200);

        entity.Property(x => x.ProductCode)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.ProductRole)
            .IsRequired()
            .HasMaxLength(150);

        entity.Property(x => x.AccessScope)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.AuthorizationReason)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.ValidFrom)
            .IsRequired();

        entity.Property(x => x.GrantedAt)
            .IsRequired();

        entity.Property(x => x.GrantNote)
            .HasMaxLength(1000);

        entity.Property(x => x.RevokeReason)
            .HasMaxLength(1000);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.HasOne(x => x.PatientProfile)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => x.PatientId);

        entity.HasIndex(x => x.UserId);

        entity.HasIndex(x => x.ServiceAccountId);

        entity.HasIndex(x => x.ProductCode);

        entity.HasIndex(x => new { x.PatientId, x.ProductCode });

        entity.HasIndex(x => new { x.UserId, x.PatientId, x.ProductCode });

        entity.HasIndex(x => new { x.ProductCode, x.ProductRole });

        entity.HasIndex(x => x.RevokedAt);

        entity.HasIndex(x => x.ValidUntil);
    }
}
