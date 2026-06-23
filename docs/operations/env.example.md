# Health Core Environment Example

This file shows placeholder-only environment variables for staging/production planning.

Do not copy these values as real secrets. Do not commit actual `.env` files. The repository `.gitignore` ignores `.env` and `.env.*`.

## Backend API

```powershell
# Environment
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443
AllowedHosts=healthcore.example.com

# Database - secret; use deployment secret store
ConnectionStrings__DefaultConnection=Host=<db-host>;Port=5432;Database=<db-name>;Username=<db-user>;Password=<db-password>;SSL Mode=Require;Trust Server Certificate=false

# JWT validation - choose Authority/JWKS or local signing key model
Jwt__Issuer=<issuer>
Jwt__Audience=<audience>
Jwt__Authority=https://<identity-provider>/
Jwt__RequireHttpsMetadata=true
Jwt__ValidateIssuer=true
Jwt__ValidateAudience=true
Jwt__ValidateLifetime=true
Jwt__ValidateIssuerSigningKey=true
Jwt__AccessTokenMinutes=60

# If no Authority/JWKS is used, store the signing key as a secret.
# Do not put the real value in this file.
Jwt__Key=<32+ byte high entropy secret from secret store>

# Fallback must remain disabled outside Development.
HealthCoreAuth__AllowHeaderFallback=false
HealthCoreAuth__AllowDefaultDevFallback=false
HealthCoreAuth__DefaultDevProductCode=InternalAdmin
HealthCoreAuth__DefaultDevProductRole=HealthCoreAdmin
HealthCoreAuth__DefaultDevServiceAccountId=dev-admin

# Bootstrap must remain disabled in Production.
AdminAuth__BootstrapAdmin__Enabled=false
AdminAuth__BootstrapAdmin__Username=
AdminAuth__BootstrapAdmin__Password=
AdminAuth__BootstrapAdmin__DisplayName=
AdminAuth__BootstrapAdmin__ProductRole=HealthCoreAdmin

# Process-local throttle; production should add reverse-proxy/WAF/distributed controls.
AdminAuth__LoginThrottle__Enabled=true
AdminAuth__LoginThrottle__MaxFailedAttempts=5
AdminAuth__LoginThrottle__WindowMinutes=15
AdminAuth__LoginThrottle__LockoutMinutes=5
```

## Next Frontend / BFF

```powershell
NODE_ENV=production

# Server-side only preferred. This points Next route handlers at the backend API.
HEALTH_CORE_API_BASE_URL=https://healthcore-api.example.com

# Avoid NEXT_PUBLIC_API_BASE_URL in production unless a public browser-visible value is truly required.
# NEXT_PUBLIC_API_BASE_URL=https://healthcore-api.example.com
```

## Rotation Placeholders

Track these outside the repository:

- JWT key id or secret-store version
- rotation owner
- rotation date
- validation command/result
- rollback plan
- affected sessions/services

## Forbidden

Never place these in committed files:

- real database password
- real JWT signing key
- admin bootstrap password
- service-account secret
- backup encryption key
- Ministry/PGSB/SHAMS certificates or credentials
- production `.env`
