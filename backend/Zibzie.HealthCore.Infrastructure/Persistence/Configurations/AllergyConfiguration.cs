using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Infrastructure.Persistence.Configurations;

public class AllergyConfiguration : IEntityTypeConfiguration<Allergy>
{
    public void Configure(EntityTypeBuilder<Allergy> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Allergen)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(x => x.AllergyType)
            .HasMaxLength(100);

        entity.Property(x => x.Severity)
            .HasMaxLength(100);

        entity.Property(x => x.ReactionDescription)
            .HasMaxLength(1000);

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
            .WithMany(x => x.Allergies)
            .HasForeignKey(x => x.PatientProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => x.PatientProfileId);

        entity.HasIndex(x => x.IsDeleted);
    }
}
