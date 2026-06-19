using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Infrastructure.Persistence.Configurations;

public class CarePlanItemConfiguration : IEntityTypeConfiguration<CarePlanItem>
{
    public void Configure(EntityTypeBuilder<CarePlanItem> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Category)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.ItemType)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(x => x.Description)
            .HasMaxLength(2000);

        entity.Property(x => x.Reason)
            .HasMaxLength(1000);

        entity.Property(x => x.RequestedBy)
            .HasMaxLength(200);

        entity.Property(x => x.AssignedTo)
            .HasMaxLength(200);

        entity.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(x => x.Priority)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(x => x.ResultSummary)
            .HasMaxLength(2000);

        entity.Property(x => x.NextAction)
            .HasMaxLength(1000);

        entity.Property(x => x.RelatedRecordType)
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
            .WithMany(x => x.CarePlanItems)
            .HasForeignKey(x => x.PatientProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => x.PatientProfileId);

        entity.HasIndex(x => x.Category);

        entity.HasIndex(x => x.ItemType);

        entity.HasIndex(x => x.Status);

        entity.HasIndex(x => x.Priority);

        entity.HasIndex(x => x.DueAt);

        entity.HasIndex(x => x.PlannedAt);

        entity.HasIndex(x => x.IsDeleted);

        entity.HasIndex(x => x.RelatedRecordType);

        entity.HasIndex(x => x.RelatedRecordId);
    }
}
