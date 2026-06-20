using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Infrastructure.Persistence.Configurations;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> entity)
    {
        entity.ToTable("AuditLogEntries");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.ServiceAccountId)
            .HasMaxLength(200);

        entity.Property(x => x.ProductCode)
            .HasMaxLength(100);

        entity.Property(x => x.ProductRole)
            .HasMaxLength(150);

        entity.Property(x => x.ActionType)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.ResourceType)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.Permission)
            .HasMaxLength(150);

        entity.Property(x => x.AccessScope)
            .HasMaxLength(100);

        entity.Property(x => x.AuthorizationReason)
            .HasMaxLength(100);

        entity.Property(x => x.Succeeded)
            .IsRequired();

        entity.Property(x => x.FailureReason)
            .HasMaxLength(1000);

        entity.Property(x => x.IpAddress)
            .HasMaxLength(100);

        entity.Property(x => x.UserAgent)
            .HasMaxLength(500);

        entity.Property(x => x.CorrelationId)
            .HasMaxLength(100);

        entity.Property(x => x.RequestPath)
            .HasMaxLength(1000);

        entity.Property(x => x.HttpMethod)
            .HasMaxLength(20);

        entity.Property(x => x.MetadataJson)
            .HasColumnType("text");

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.HasIndex(x => x.CreatedAt);

        entity.HasIndex(x => x.UserId);

        entity.HasIndex(x => x.ServiceAccountId);

        entity.HasIndex(x => x.PatientId);

        entity.HasIndex(x => x.ProductCode);

        entity.HasIndex(x => x.ActionType);

        entity.HasIndex(x => x.ResourceType);

        entity.HasIndex(x => x.Succeeded);

        entity.HasIndex(x => x.CorrelationId);

        entity.HasIndex(x => new { x.PatientId, x.CreatedAt });

        entity.HasIndex(x => new { x.UserId, x.CreatedAt });

        entity.HasIndex(x => new { x.ProductCode, x.CreatedAt });
    }
}
