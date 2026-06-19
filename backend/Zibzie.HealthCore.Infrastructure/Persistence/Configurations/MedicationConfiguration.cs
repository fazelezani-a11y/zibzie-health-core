using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Infrastructure.Persistence.Configurations;

public class MedicationConfiguration : IEntityTypeConfiguration<Medication>
{
    public void Configure(EntityTypeBuilder<Medication> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(x => x.Dose)
            .HasMaxLength(100);

        entity.Property(x => x.Frequency)
            .HasMaxLength(100);

        entity.Property(x => x.Route)
            .HasMaxLength(100);

        entity.Property(x => x.Reason)
            .HasMaxLength(500);

        entity.Property(x => x.ClinicianNote)
            .HasMaxLength(2000);

        entity.Property(x => x.SourceType)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(x => x.VerificationStatus)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(x => x.SensitivityLevel)
            .IsRequired()
            .HasMaxLength(50);

        entity.HasOne(x => x.PatientProfile)
            .WithMany(x => x.Medications)
            .HasForeignKey(x => x.PatientProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => x.PatientProfileId);

        entity.HasIndex(x => x.IsCurrent);

        entity.HasIndex(x => x.IsDeleted);
    }
}
