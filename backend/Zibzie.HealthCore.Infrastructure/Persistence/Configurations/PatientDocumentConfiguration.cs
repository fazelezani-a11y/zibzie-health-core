using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Infrastructure.Persistence.Configurations;

public class PatientDocumentConfiguration : IEntityTypeConfiguration<PatientDocument>
{
    public void Configure(EntityTypeBuilder<PatientDocument> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.DocumentType)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(x => x.Description)
            .HasMaxLength(2000);

        entity.Property(x => x.IssuerName)
            .HasMaxLength(200);

        entity.Property(x => x.FileName)
            .HasMaxLength(500);

        entity.Property(x => x.FileUrl)
            .HasMaxLength(1000);

        entity.Property(x => x.FileReference)
            .HasMaxLength(500);

        entity.Property(x => x.MimeType)
            .HasMaxLength(100);

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
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.PatientProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => x.PatientProfileId);

        entity.HasIndex(x => x.DocumentType);

        entity.HasIndex(x => x.DocumentDate);

        entity.HasIndex(x => x.IsDeleted);

        entity.HasIndex(x => x.VerificationStatus);

        entity.HasIndex(x => x.SensitivityLevel);
    }
}
