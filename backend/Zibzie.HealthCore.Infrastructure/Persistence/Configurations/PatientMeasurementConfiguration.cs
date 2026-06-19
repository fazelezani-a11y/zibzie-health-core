using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Infrastructure.Persistence.Configurations;

public class PatientMeasurementConfiguration : IEntityTypeConfiguration<PatientMeasurement>
{
    public void Configure(EntityTypeBuilder<PatientMeasurement> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.PatientProfileId)
            .IsRequired();

        entity.Property(x => x.MeasurementType)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(x => x.Value)
            .IsRequired();

        entity.Property(x => x.Unit)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(x => x.MeasuredAt)
            .IsRequired();

        entity.Property(x => x.Method)
            .HasMaxLength(100);

        entity.Property(x => x.BodySite)
            .HasMaxLength(100);

        entity.Property(x => x.Context)
            .HasMaxLength(1000);

        entity.Property(x => x.ReferenceRange)
            .HasMaxLength(200);

        entity.Property(x => x.SourceType)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(x => x.RelatedRecordType)
            .HasMaxLength(100);

        entity.Property(x => x.VerificationStatus)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(x => x.SensitivityLevel)
            .IsRequired()
            .HasMaxLength(50);

        entity.HasOne(x => x.PatientProfile)
            .WithMany(x => x.Measurements)
            .HasForeignKey(x => x.PatientProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => x.PatientProfileId);

        entity.HasIndex(x => x.MeasurementType);

        entity.HasIndex(x => x.MeasuredAt);

        entity.HasIndex(x => x.IsDeleted);

        entity.HasIndex(x => x.SourceType);

        entity.HasIndex(x => x.VerificationStatus);

        entity.HasIndex(x => x.SensitivityLevel);

        entity.HasIndex(x => x.RelatedRecordType);

        entity.HasIndex(x => x.RelatedRecordId);
    }
}
