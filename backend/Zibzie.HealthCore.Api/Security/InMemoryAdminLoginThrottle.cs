using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Zibzie.HealthCore.Application.Security;

namespace Zibzie.HealthCore.Api.Security;

public sealed class InMemoryAdminLoginThrottle : IAdminLoginThrottle
{
    private const string UnknownIpAddress = "unknown-ip";
    private const string UnknownUsername = "unknown-user";

    private readonly ConcurrentDictionary<string, LoginAttemptState> _attempts = new();
    private readonly AdminLoginThrottleOptions _options;

    public InMemoryAdminLoginThrottle(IOptions<AdminAuthOptions> options)
    {
        _options = options.Value.LoginThrottle;
    }

    public bool IsBlocked(
        string? normalizedUsername,
        HealthCoreRequestContext requestContext,
        DateTimeOffset now)
    {
        if (!IsEnabled())
        {
            return false;
        }

        var key = BuildKey(normalizedUsername, requestContext);

        if (!_attempts.TryGetValue(key, out var state))
        {
            return false;
        }

        lock (state)
        {
            if (state.BlockedUntil is { } blockedUntil && blockedUntil > now)
            {
                return true;
            }

            if (IsWindowExpired(state, now))
            {
                _attempts.TryRemove(key, out _);
            }

            return false;
        }
    }

    public void RecordFailure(
        string? normalizedUsername,
        HealthCoreRequestContext requestContext,
        DateTimeOffset now)
    {
        if (!IsEnabled())
        {
            return;
        }

        var key = BuildKey(normalizedUsername, requestContext);
        var state = _attempts.GetOrAdd(key, _ => new LoginAttemptState(now));

        lock (state)
        {
            if (IsWindowExpired(state, now))
            {
                state.WindowStartedAt = now;
                state.FailedAttempts = 0;
                state.BlockedUntil = null;
            }

            state.FailedAttempts++;

            if (state.FailedAttempts >= _options.MaxFailedAttempts)
            {
                state.BlockedUntil = now.AddMinutes(_options.LockoutMinutes);
            }
        }
    }

    public void RecordSuccess(
        string? normalizedUsername,
        HealthCoreRequestContext requestContext)
    {
        if (!IsEnabled())
        {
            return;
        }

        _attempts.TryRemove(BuildKey(normalizedUsername, requestContext), out _);
    }

    private bool IsEnabled()
    {
        return _options.Enabled
            && _options.MaxFailedAttempts > 0
            && _options.WindowMinutes > 0
            && _options.LockoutMinutes > 0;
    }

    private bool IsWindowExpired(LoginAttemptState state, DateTimeOffset now)
    {
        return state.WindowStartedAt.AddMinutes(_options.WindowMinutes) <= now;
    }

    private static string BuildKey(
        string? normalizedUsername,
        HealthCoreRequestContext requestContext)
    {
        var ipAddress = string.IsNullOrWhiteSpace(requestContext.IpAddress)
            ? UnknownIpAddress
            : requestContext.IpAddress.Trim();

        var username = string.IsNullOrWhiteSpace(normalizedUsername)
            ? UnknownUsername
            : normalizedUsername.Trim();

        return $"{ipAddress}|{username}";
    }

    private sealed class LoginAttemptState
    {
        public LoginAttemptState(DateTimeOffset windowStartedAt)
        {
            WindowStartedAt = windowStartedAt;
        }

        public DateTimeOffset WindowStartedAt { get; set; }

        public int FailedAttempts { get; set; }

        public DateTimeOffset? BlockedUntil { get; set; }
    }
}
