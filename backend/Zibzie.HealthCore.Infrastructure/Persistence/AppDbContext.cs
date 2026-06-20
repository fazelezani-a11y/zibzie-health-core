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

    public DbSet<PatientMeasurement> PatientMeasurements => Set<PatientMeasurement>();

    public DbSet<PatientAccessGrant> PatientAccessGrants => Set<PatientAccessGrant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
