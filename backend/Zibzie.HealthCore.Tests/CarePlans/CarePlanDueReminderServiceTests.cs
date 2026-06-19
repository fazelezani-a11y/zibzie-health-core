using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.CarePlans;
using Zibzie.HealthCore.Domain.Common;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.CarePlans;
using Zibzie.HealthCore.Infrastructure.Persistence;

namespace Zibzie.HealthCore.Tests.CarePlans;

public class CarePlanDueReminderServiceTests
{
    [Fact]
    public async Task CreateCarePlanItemAsync_WithDueAtAndActiveStatus_CreatesLinkedSystemReminder()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedActivePatientAsync(dbContext);
        var service = CreateCarePlanItemService(dbContext);
        var dueAt = DateTimeOffset.UtcNow.AddDays(3);

        var createdItem = await service.CreateCarePlanItemAsync(
            patient.Id,
            CreateRequest(dueAt: dueAt),
            CancellationToken.None);

        Assert.NotNull(createdItem);

        var reminder = await dbContext.PatientReminders.SingleAsync();
        Assert.Equal(patient.Id, reminder.PatientProfileId);
        Assert.Equal(ReminderTypes.CarePlan, reminder.ReminderType);
        Assert.Equal(SourceTypes.System, reminder.SourceType);
        Assert.Equal(RecordTypes.CarePlanItem, reminder.RelatedRecordType);
        Assert.Equal(createdItem.Id, reminder.RelatedRecordId);
        Assert.Equal(AudienceTypes.Internal, reminder.Audience);
        Assert.Equal(ReminderStatuses.Pending, reminder.Status);
        Assert.Equal(dueAt.ToUniversalTime(), reminder.DueAt);
        Assert.Equal(CommonPriorities.High, reminder.Priority);
        Assert.Equal(SensitivityLevels.Sensitive, reminder.SensitivityLevel);

        var reminderTimelineEvent = await dbContext.PatientTimelineEvents.SingleAsync(timelineEvent =>
            timelineEvent.RelatedRecordType == RecordTypes.PatientReminder &&
            timelineEvent.RelatedRecordId == reminder.Id);

        Assert.Equal(TimelineEventTypes.Reminder, reminderTimelineEvent.EventType);
        Assert.Equal(SourceTypes.System, reminderTimelineEvent.SourceType);
        Assert.Equal(VisibilityValues.Internal, reminderTimelineEvent.Visibility);
        Assert.Equal(reminder.SensitivityLevel, reminderTimelineEvent.SensitivityLevel);
    }

    [Fact]
    public async Task CreateCarePlanItemAsync_WithoutDueAt_DoesNotCreateReminder()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedActivePatientAsync(dbContext);
        var service = CreateCarePlanItemService(dbContext);

        await service.CreateCarePlanItemAsync(
            patient.Id,
            CreateRequest(dueAt: null),
            CancellationToken.None);

        Assert.Empty(await dbContext.PatientReminders.ToListAsync());
    }

    [Fact]
    public async Task CreateCarePlanItemAsync_WithCompletedStatus_DoesNotCreateReminder()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedActivePatientAsync(dbContext);
        var service = CreateCarePlanItemService(dbContext);

        await service.CreateCarePlanItemAsync(
            patient.Id,
            CreateRequest(dueAt: DateTimeOffset.UtcNow.AddDays(3), status: CarePlanStatuses.Completed),
            CancellationToken.None);

        Assert.Empty(await dbContext.PatientReminders.ToListAsync());
    }

    [Fact]
    public async Task CreateCarePlanItemAsync_WithCancelledStatus_DoesNotCreateReminder()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedActivePatientAsync(dbContext);
        var service = CreateCarePlanItemService(dbContext);

        await service.CreateCarePlanItemAsync(
            patient.Id,
            CreateRequest(dueAt: DateTimeOffset.UtcNow.AddDays(3), status: CarePlanStatuses.Cancelled),
            CancellationToken.None);

        Assert.Empty(await dbContext.PatientReminders.ToListAsync());
    }

    [Fact]
    public async Task TryAddDueReminderForCarePlanItemAsync_WhenCalledTwice_DoesNotCreateDuplicateReminder()
    {
        await using var dbContext = CreateDbContext();
        var patient = await SeedActivePatientAsync(dbContext);
        var service = new CarePlanDueReminderService(dbContext);
        var carePlanItem = CreateCarePlanItem(patient.Id, DateTimeOffset.UtcNow.AddDays(3));

        dbContext.CarePlanItems.Add(carePlanItem);

        await service.TryAddDueReminderForCarePlanItemAsync(carePlanItem, CancellationToken.None);
        await service.TryAddDueReminderForCarePlanItemAsync(carePlanItem, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        var reminders = await dbContext.PatientReminders
            .Where(reminder =>
                reminder.PatientProfileId == patient.Id &&
                reminder.RelatedRecordType == RecordTypes.CarePlanItem &&
                reminder.RelatedRecordId == carePlanItem.Id &&
                reminder.ReminderType == ReminderTypes.CarePlan &&
                !reminder.IsDeleted)
            .ToListAsync();

        Assert.Single(reminders);
    }

    private static CarePlanItemService CreateCarePlanItemService(AppDbContext dbContext)
    {
        return new CarePlanItemService(
            dbContext,
            new CarePlanDueReminderService(dbContext));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<PatientProfile> SeedActivePatientAsync(AppDbContext dbContext)
    {
        var patient = new PatientProfile
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Patient",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.PatientProfiles.Add(patient);
        await dbContext.SaveChangesAsync();

        return patient;
    }

    private static CreateCarePlanItemRequest CreateRequest(DateTimeOffset? dueAt, string status = CarePlanStatuses.Planned)
    {
        return new CreateCarePlanItemRequest
        {
            Category = CarePlanCategories.FollowUp,
            ItemType = CarePlanItemTypes.Visit,
            Title = "Follow-up visit",
            Reason = "Medication review",
            DueAt = dueAt,
            Status = status,
            Priority = CommonPriorities.High,
            SourceType = SourceTypes.Manual,
            VerificationStatus = VerificationStatuses.Unverified,
            SensitivityLevel = SensitivityLevels.Sensitive
        };
    }

    private static CarePlanItem CreateCarePlanItem(Guid patientId, DateTimeOffset dueAt)
    {
        return new CarePlanItem
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            Category = CarePlanCategories.FollowUp,
            ItemType = CarePlanItemTypes.Visit,
            Title = "Follow-up visit",
            Reason = "Medication review",
            DueAt = dueAt.ToUniversalTime(),
            Status = CarePlanStatuses.Planned,
            Priority = CommonPriorities.High,
            SourceType = SourceTypes.Manual,
            VerificationStatus = VerificationStatuses.Unverified,
            SensitivityLevel = SensitivityLevels.Sensitive,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
