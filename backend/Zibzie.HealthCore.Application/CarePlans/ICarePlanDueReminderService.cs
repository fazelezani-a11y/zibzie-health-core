using Zibzie.HealthCore.Domain.Entities;

namespace Zibzie.HealthCore.Application.CarePlans;

public interface ICarePlanDueReminderService
{
    Task TryAddDueReminderForCarePlanItemAsync(
        CarePlanItem item,
        CancellationToken cancellationToken = default);
}
