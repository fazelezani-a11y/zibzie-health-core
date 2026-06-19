using Microsoft.EntityFrameworkCore;
using Zibzie.HealthCore.Application.CarePlans;
using Zibzie.HealthCore.Application.Documents;
using Zibzie.HealthCore.Application.Patients;
using Zibzie.HealthCore.Infrastructure.CarePlans;
using Zibzie.HealthCore.Infrastructure.Documents;
using Zibzie.HealthCore.Infrastructure.Patients;
using Zibzie.HealthCore.Infrastructure.Persistence;

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

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IPatientSummaryService, PatientSummaryService>();
builder.Services.AddScoped<IPatientDocumentService, PatientDocumentService>();
builder.Services.AddScoped<ICarePlanItemService, CarePlanItemService>();

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

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("DefaultCors");

app.UseHttpsRedirection();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "Zibzie Health Core API",
    timestamp = DateTime.UtcNow
}));

app.Run();
