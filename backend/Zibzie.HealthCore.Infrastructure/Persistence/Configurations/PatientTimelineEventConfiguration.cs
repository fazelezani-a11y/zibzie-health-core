using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Infrastructure.Persistence.Configurations;

public class PatientTimelineEventConfiguration : IEntityTypeConfiguration<PatientTimelineEvent>
{
    public void Configure(EntityTypeBuilder<PatientTimelineEvent> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(x => x.Description)
            .HasMaxLength(2000);

        entity.Property(x => x.OccurredAt)
            .IsRequired();

        entity.Property(x => x.SourceType)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(x => x.RelatedRecordType)
            .HasMaxLength(100);

        entity.Property(x => x.Visibility)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(x => x.SensitivityLevel)
            .IsRequired()
            .HasMaxLength(50);

        entity.HasOne(x => x.PatientProfile)
            .WithMany(x => x.TimelineEvents)
            .HasForeignKey(x => x.PatientProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => x.PatientProfileId);

        entity.HasIndex(x => x.OccurredAt);

        entity.HasIndex(x => x.IsDeleted);

        entity.HasIndex(x => x.EventType);
    }
}
