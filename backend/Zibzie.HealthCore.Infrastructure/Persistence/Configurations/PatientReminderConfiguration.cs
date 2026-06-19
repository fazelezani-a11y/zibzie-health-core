using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Infrastructure.Persistence.Configurations;

public class PatientReminderConfiguration : IEntityTypeConfiguration<PatientReminder>
{
    public void Configure(EntityTypeBuilder<PatientReminder> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.ReminderType)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(x => x.Description)
            .HasMaxLength(2000);

        entity.Property(x => x.DueAt)
            .IsRequired();

        entity.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(x => x.Priority)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(x => x.Audience)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(x => x.Channel)
            .HasMaxLength(50);

        entity.Property(x => x.RelatedRecordType)
            .HasMaxLength(100);

        entity.Property(x => x.SourceType)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(x => x.SensitivityLevel)
            .IsRequired()
            .HasMaxLength(50);

        entity.HasOne(x => x.PatientProfile)
            .WithMany(x => x.Reminders)
            .HasForeignKey(x => x.PatientProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => x.PatientProfileId);

        entity.HasIndex(x => x.ReminderType);

        entity.HasIndex(x => x.DueAt);

        entity.HasIndex(x => x.Status);

        entity.HasIndex(x => x.Priority);

        entity.HasIndex(x => x.Audience);

        entity.HasIndex(x => x.IsDeleted);

        entity.HasIndex(x => x.RelatedRecordType);

        entity.HasIndex(x => x.RelatedRecordId);
    }
}
