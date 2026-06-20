namespace Zibzie.HealthCore.Domain.Security;

public static class AuditActionTypes
{
    public const string View = "View";
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string Export = "Export";
    public const string Share = "Share";
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string AccessDenied = "AccessDenied";
    public const string GrantAccess = "GrantAccess";
    public const string RevokeAccess = "RevokeAccess";
    public const string SystemAction = "SystemAction";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        View,
        Create,
        Update,
        Delete,
        Export,
        Share,
        Login,
        Logout,
        AccessDenied,
        GrantAccess,
        RevokeAccess,
        SystemAction,
    };
}
