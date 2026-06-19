namespace Zibzie.HealthCore.Application.ParaclinicalResults;

public interface IParaclinicalResultService
{
    Task<CreateParaclinicalResultResult> CreateParaclinicalResultAsync(
        Guid patientId,
        CreateParaclinicalResultRequest request,
        CancellationToken cancellationToken = default);
}

public class CreateParaclinicalResultResult
{
    public CreateParaclinicalResultStatus Status { get; init; }

    public ParaclinicalResultDto? Result { get; init; }

    public static CreateParaclinicalResultResult Created(ParaclinicalResultDto result)
    {
        return new CreateParaclinicalResultResult
        {
            Status = CreateParaclinicalResultStatus.Created,
            Result = result
        };
    }

    public static CreateParaclinicalResultResult PatientNotFound()
    {
        return new CreateParaclinicalResultResult
        {
            Status = CreateParaclinicalResultStatus.PatientNotFound
        };
    }

    public static CreateParaclinicalResultResult LinkedDocumentNotFound()
    {
        return new CreateParaclinicalResultResult
        {
            Status = CreateParaclinicalResultStatus.LinkedDocumentNotFound
        };
    }

    public static CreateParaclinicalResultResult LabItemTestNameRequired()
    {
        return new CreateParaclinicalResultResult
        {
            Status = CreateParaclinicalResultStatus.LabItemTestNameRequired
        };
    }
}

public enum CreateParaclinicalResultStatus
{
    Created,
    PatientNotFound,
    LinkedDocumentNotFound,
    LabItemTestNameRequired
}
