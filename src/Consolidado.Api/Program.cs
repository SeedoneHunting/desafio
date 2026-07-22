using System.Threading.RateLimiting;
using Cashflow.Contracts;
using Consolidado.Api.Data;
using Consolidado.Api.Middleware;
using Consolidado.Api.Services;
using Consolidado.Api.Workers;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

var connectionString = builder.Configuration.GetConnectionString("ConsolidadoDb")
    ?? "Host=localhost;Port=5432;Database=consolidado_db;Username=cashflow;Password=cashflow";

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

var enableWorkers = builder.Configuration.GetValue("Features:EnableBackgroundWorkers", true);
if (enableWorkers)
    builder.Services.AddHostedService<KafkaConsumerWorker>();

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

app.MapGet("/health", () => Results.Ok(new HealthResponse("Healthy", 0)));

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

app.Run();

static bool IsSqliteConnection(string cs) =>
    cs.Contains("Data Source=", StringComparison.OrdinalIgnoreCase);

public partial class Program;
