using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Zibzie.HealthCore.Api.Security;
using Zibzie.HealthCore.Application.CarePlans;
using Zibzie.HealthCore.Application.Documents;
using Zibzie.HealthCore.Application.Measurements;
using Zibzie.HealthCore.Application.Patients;
using Zibzie.HealthCore.Application.ParaclinicalResults;
using Zibzie.HealthCore.Application.Reminders;
using Zibzie.HealthCore.Application.Security;
using Zibzie.HealthCore.Domain.Entities;
using Zibzie.HealthCore.Infrastructure.CarePlans;
using Zibzie.HealthCore.Infrastructure.Documents;
using Zibzie.HealthCore.Infrastructure.Measurements;
using Zibzie.HealthCore.Infrastructure.Patients;
using Zibzie.HealthCore.Infrastructure.ParaclinicalResults;
using Zibzie.HealthCore.Infrastructure.Persistence;
using Zibzie.HealthCore.Infrastructure.Reminders;
using Zibzie.HealthCore.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

if (OperatingSystem.IsWindows())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<HealthCoreAuthOptions>(builder.Configuration.GetSection("HealthCoreAuth"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<AdminAuthOptions>(builder.Configuration.GetSection("AdminAuth"));

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var healthCoreAuthOptions = builder.Configuration.GetSection("HealthCoreAuth").Get<HealthCoreAuthOptions>() ?? new HealthCoreAuthOptions();
var adminAuthOptions = builder.Configuration.GetSection("AdminAuth").Get<AdminAuthOptions>() ?? new AdminAuthOptions();

HealthCoreSecurityStartupValidation.Validate(
    builder.Environment,
    healthCoreAuthOptions,
    jwtOptions,
    adminAuthOptions);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        if (!string.IsNullOrWhiteSpace(jwtOptions.Authority))
        {
            options.Authority = jwtOptions.Authority;
            options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
        }

        if (!string.IsNullOrWhiteSpace(jwtOptions.Audience))
        {
            options.Audience = jwtOptions.Audience;
        }

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = jwtOptions.ValidateIssuer,
            ValidateAudience = jwtOptions.ValidateAudience,
            ValidateLifetime = jwtOptions.ValidateLifetime,
            ValidateIssuerSigningKey = jwtOptions.ValidateIssuerSigningKey,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        if (!string.IsNullOrWhiteSpace(jwtOptions.Issuer))
        {
            tokenValidationParameters.ValidIssuer = jwtOptions.Issuer;
        }

        if (!string.IsNullOrWhiteSpace(jwtOptions.Audience))
        {
            tokenValidationParameters.ValidAudience = jwtOptions.Audience;
        }

        if (!string.IsNullOrWhiteSpace(jwtOptions.EffectiveSigningKey))
        {
            tokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.EffectiveSigningKey));
        }

        options.TokenValidationParameters = tokenValidationParameters;
    });
builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IPatientSummaryService, PatientSummaryService>();
builder.Services.AddScoped<IPatientDocumentService, PatientDocumentService>();
builder.Services.AddScoped<ICarePlanItemService, CarePlanItemService>();
builder.Services.AddScoped<ICarePlanDueReminderService, CarePlanDueReminderService>();
builder.Services.AddScoped<IPatientReminderService, PatientReminderService>();
builder.Services.AddScoped<IPatientMeasurementService, PatientMeasurementService>();
builder.Services.AddScoped<IParaclinicalResultService, ParaclinicalResultService>();
builder.Services.AddScoped<IHealthCoreAuthorizationService, HealthCoreAuthorizationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IHealthCoreRequestContextProvider, HttpHealthCoreRequestContextProvider>();
builder.Services.AddScoped<IPatientAccessGrantService, PatientAccessGrantService>();
builder.Services.AddScoped<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

await AdminUserBootstrapper.SeedAsync(app.Services, app.Environment);

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("DefaultCors");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "Zibzie Health Core API",
    timestamp = DateTime.UtcNow
}));

app.Run();
