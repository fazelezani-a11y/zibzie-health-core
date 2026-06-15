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

    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();

    public DbSet<PatientParaclinicalResult> PatientParaclinicalResults => Set<PatientParaclinicalResult>();

    public DbSet<PatientLabResultItem> PatientLabResultItems => Set<PatientLabResultItem>();

    public DbSet<CarePlanItem> CarePlanItems => Set<CarePlanItem>();

    public DbSet<PatientReminder> PatientReminders => Set<PatientReminder>();

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

        modelBuilder.Entity<PatientDocument>(entity =>
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
        });

        modelBuilder.Entity<PatientParaclinicalResult>(entity =>
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
        });

        modelBuilder.Entity<PatientLabResultItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TestName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Value)
                .HasMaxLength(200);

            entity.Property(x => x.Unit)
                .HasMaxLength(50);

            entity.Property(x => x.ReferenceRange)
                .HasMaxLength(200);

            entity.Property(x => x.Interpretation)
                .HasMaxLength(1000);

            entity.HasOne(x => x.PatientParaclinicalResult)
                .WithMany(x => x.LabItems)
                .HasForeignKey(x => x.PatientParaclinicalResultId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.PatientParaclinicalResultId);

            entity.HasIndex(x => x.DisplayOrder);
        });

        modelBuilder.Entity<CarePlanItem>(entity =>
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
        });

        modelBuilder.Entity<PatientReminder>(entity =>
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
        });
    }
}
