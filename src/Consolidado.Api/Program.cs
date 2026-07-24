using System.Threading.RateLimiting;
using Cashflow.Contracts;
using Consolidado.Api.Data;
using Consolidado.Api.Health;
using Consolidado.Api.Middleware;
using Consolidado.Api.Services;
using Consolidado.Api.Workers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

var connectionString = builder.Configuration.GetConnectionString("ConsolidadoDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Connection string 'ConsolidadoDb' is not configured. Set ConnectionStrings__ConsolidadoDb or use Docker Compose with a local .env file.");

builder.Services.AddDbContext<ConsolidadoDbContext>(options =>
{
    if (IsSqliteConnection(connectionString))
        options.UseSqlite(connectionString);
    else
        options.UseNpgsql(connectionString);
});

builder.Services.AddMemoryCache();
builder.Services.AddScoped<BalanceProjectionService>();
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));

var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var enableWorkers = builder.Configuration.GetValue("Features:EnableBackgroundWorkers", true);
if (enableWorkers)
{
    builder.Services.AddSingleton<KafkaConsumerWorker>();
    builder.Services.AddHostedService(provider => provider.GetRequiredService<KafkaConsumerWorker>());
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database")
        .AddCheck<KafkaConsumerHealthCheck>("kafka-consumer");
}
else
{
    // Tests disable the worker — register a stub so health DI still resolves if needed.
    builder.Services.AddSingleton<KafkaConsumerWorker>();
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database");
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("balances", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromSeconds(1);
        limiter.QueueLimit = 0;
    });
});

var app = builder.Build();

app.UseCors();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseRateLimiter();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
    if (IsSqliteConnection(connectionString))
    {
        Directory.CreateDirectory(Path.GetDirectoryName(connectionString.Replace("Data Source=", "")) ?? "./data");
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        await db.Database.MigrateAsync();
    }
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteConsolidadoHealthAsync
});

app.MapGet("/balances/{entryDate}", async (
    DateOnly entryDate,
    BalanceProjectionService projection,
    CancellationToken ct) =>
{
    var balance = await projection.GetByDateAsync(entryDate, ct);
    return balance is null ? Results.NotFound() : Results.Ok(balance);
}).RequireRateLimiting("balances");

app.MapGet("/balances", async (
    DateOnly? start_date,
    DateOnly? end_date,
    BalanceProjectionService projection,
    CancellationToken ct) =>
{
    var items = await projection.ListAsync(start_date, end_date, ct);
    return Results.Ok(items);
}).RequireRateLimiting("balances");

app.MapGet("/admin/processed-events", async (ConsolidadoDbContext db, CancellationToken ct) =>
{
    var items = await db.ProcessedEvents.AsNoTracking().Take(200).ToListAsync(ct);
    return Results.Ok(items.OrderByDescending(e => e.ProcessedAt).Take(100));
});

app.MapGet("/admin/snapshot", async (ConsolidadoDbContext db, CancellationToken ct) =>
{
    var balances = await db.DailyBalances.AsNoTracking().Take(200).ToListAsync(ct);
    var events = await db.ProcessedEvents.AsNoTracking().Take(200).ToListAsync(ct);

    return Results.Ok(new
    {
        database = "consolidado_db",
        dailyBalances = balances.OrderByDescending(b => b.Date).Take(100),
        processedEvents = events.OrderByDescending(e => e.ProcessedAt).Take(100)
    });
});

app.Run();

static async Task WriteConsolidadoHealthAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new HealthResponse(report.Status.ToString(), 0));
}

static bool IsSqliteConnection(string cs) =>
    cs.Contains("Data Source=", StringComparison.OrdinalIgnoreCase);

public partial class Program;
