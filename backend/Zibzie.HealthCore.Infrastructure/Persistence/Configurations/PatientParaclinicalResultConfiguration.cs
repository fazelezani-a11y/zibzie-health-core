using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Infrastructure.Persistence.Configurations;

public class PatientParaclinicalResultConfiguration : IEntityTypeConfiguration<PatientParaclinicalResult>
{
    public void Configure(EntityTypeBuilder<PatientParaclinicalResult> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.ResultType)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(x => x.Description)
            .HasMaxLength(2000);

        entity.Property(x => x.ProviderName)
            .HasMaxLength(200);

        entity.Property(x => x.Summary)
            .HasMaxLength(2000);

        entity.Property(x => x.Interpretation)
            .HasMaxLength(2000);

        entity.Property(x => x.FollowUpNote)
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
            .WithMany(x => x.ParaclinicalResults)
            .HasForeignKey(x => x.PatientProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.LinkedDocument)
            .WithMany()
            .HasForeignKey(x => x.LinkedDocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasIndex(x => x.PatientProfileId);

        entity.HasIndex(x => x.ResultType);

        entity.HasIndex(x => x.ResultDate);

        entity.HasIndex(x => x.PerformedAt);

        entity.HasIndex(x => x.IsDeleted);

        entity.HasIndex(x => x.VerificationStatus);

        entity.HasIndex(x => x.SensitivityLevel);

        entity.HasIndex(x => x.RequiresFollowUp);
    }
}
