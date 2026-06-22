namespace Zibzie.HealthCore.Application.Security;

public enum PatientAccessGrantServiceError
{
    None,
    Validation,
    Conflict,
    NotFound
}

public sealed record PatientAccessGrantServiceResult<T>(
    bool Succeeded,
    T? Value,
    PatientAccessGrantServiceError Error,
    string? ErrorMessage)
{
    public static PatientAccessGrantServiceResult<T> Success(T value)
    {
        return new PatientAccessGrantServiceResult<T>(
            true,
            value,
            PatientAccessGrantServiceError.None,
            null);
    }

    public static PatientAccessGrantServiceResult<T> Failure(
        PatientAccessGrantServiceError error,
        string errorMessage)
    {
        return new PatientAccessGrantServiceResult<T>(
            false,
            default,
            error,
            errorMessage);
    }
}
