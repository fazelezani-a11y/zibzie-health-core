using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();

    public DbSet<ContactInfo> ContactInfos => Set<ContactInfo>();

    public DbSet<Condition> Conditions => Set<Condition>();

    public DbSet<Allergy> Allergies => Set<Allergy>();

    public DbSet<Medication> Medications => Set<Medication>();

    public DbSet<PatientTimelineEvent> PatientTimelineEvents => Set<PatientTimelineEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PatientProfile>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.NationalCode)
                .HasMaxLength(20);

            entity.Property(x => x.Gender)
                .HasMaxLength(30);

            entity.Property(x => x.BloodType)
                .HasMaxLength(10);

            entity.Property(x => x.MaritalStatus)
                .HasMaxLength(50);

            entity.Property(x => x.EducationLevel)
                .HasMaxLength(100);

            entity.Property(x => x.Occupation)
                .HasMaxLength(100);

            entity.Property(x => x.ProfileImageUrl)
                .HasMaxLength(500);

            entity.HasOne(x => x.ContactInfo)
                .WithOne(x => x.PatientProfile)
                .HasForeignKey<ContactInfo>(x => x.PatientProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContactInfo>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.MobileNumber)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(x => x.Email)
                .HasMaxLength(200);

            entity.Property(x => x.EmergencyContactName)
                .HasMaxLength(150);

            entity.Property(x => x.EmergencyContactPhone)
                .HasMaxLength(30);

            entity.Property(x => x.HomeAddress)
                .HasMaxLength(1000);

            entity.Property(x => x.WorkAddress)
                .HasMaxLength(1000);
        });

        modelBuilder.Entity<Condition>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Status)
                .HasMaxLength(100);

            entity.Property(x => x.TreatmentSummary)
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
                .WithMany(x => x.Conditions)
                .HasForeignKey(x => x.PatientProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.PatientProfileId);

            entity.HasIndex(x => x.IsDeleted);
        });

        modelBuilder.Entity<Allergy>(entity =>
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
        });

        modelBuilder.Entity<Medication>(entity =>
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
        });

        modelBuilder.Entity<PatientTimelineEvent>(entity =>
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
        });
    }
}
