namespace Zibzie.HealthCore.Application.CarePlans;

public interface ICarePlanItemService
{
    Task<CarePlanItemDto?> CreateCarePlanItemAsync(
        Guid patientId,
        CreateCarePlanItemRequest request,
        CancellationToken cancellationToken = default);
}
