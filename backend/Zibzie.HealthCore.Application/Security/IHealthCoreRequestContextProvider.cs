namespace Zibzie.HealthCore.Application.Security;

public interface IHealthCoreRequestContextProvider
{
    HealthCoreRequestContext GetCurrent();

    HealthCoreAuthorizationContext CreateAuthorizationContext(Guid patientId);
}
