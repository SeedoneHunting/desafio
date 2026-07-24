using Cashflow.Contracts;
using Lancamentos.Api.Data;
using Lancamentos.Api.Health;
using Lancamentos.Api.Messaging;
using Lancamentos.Api.Middleware;
using Lancamentos.Api.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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

var connectionString = builder.Configuration.GetConnectionString("LancamentosDb")
    ?? "Host=localhost;Port=5432;Database=lancamentos_db;Username=cashflow;Password=cashflow";

builder.Services.AddDbContext<LancamentosDbContext>(options =>
{
    if (IsSqliteConnection(connectionString))
        options.UseSqlite(connectionString);
    else
        options.UseNpgsql(connectionString);
});

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));
builder.Services.AddScoped<EntryService>();

var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<OutboxBacklogHealthCheck>("outbox");

var enableWorkers = builder.Configuration.GetValue("Features:EnableBackgroundWorkers", true);
if (enableWorkers)
{
    builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
    builder.Services.AddHostedService<OutboxRelayWorker>();
}

var app = builder.Build();

app.UseCors();
app.UseMiddleware<CorrelationIdMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
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
    ResponseWriter = WriteLancamentosHealthAsync
});

app.MapPost("/entries", async (CreateEntryRequest request, EntryService entries, CancellationToken ct) =>
{
    try
    {
        var (entry, created) = await entries.CreateAsync(request, ct);
        return created
            ? Results.Created($"/entries/{entry.Id}", entry)
            : Results.Ok(entry);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/entries", async (DateOnly? entry_date, EntryService entries, CancellationToken ct) =>
{
    var items = await entries.ListAsync(entry_date, ct);
    return Results.Ok(items);
});

app.MapGet("/admin/outbox", async (LancamentosDbContext db, CancellationToken ct) =>
{
    var items = await db.OutboxMessages
        .AsNoTracking()
        .Take(200)
        .ToListAsync(ct);

    return Results.Ok(items
        .OrderByDescending(m => m.CreatedAt)
        .Take(100)
        .Select(m => new
        {
            m.Id,
            m.Payload,
            m.CreatedAt,
            m.ProcessedAt,
            Status = m.ProcessedAt == null ? "Pending" : "Published"
        }));
});

app.MapGet("/admin/snapshot", async (LancamentosDbContext db, CancellationToken ct) =>
{
    var entries = await db.Entries.AsNoTracking().Take(200).ToListAsync(ct);
    var outbox = await db.OutboxMessages.AsNoTracking().Take(200).ToListAsync(ct);

    return Results.Ok(new
    {
        database = "lancamentos_db",
        entries = entries
            .OrderByDescending(e => e.CreatedAt)
            .Take(100)
            .Select(e => new
            {
                e.Id,
                e.ExternalId,
                Type = ((EntryType)e.Type).ToString(),
                e.Amount,
                e.Date,
                e.Description,
                e.CreatedAt
            }),
        outbox = outbox
            .OrderByDescending(m => m.CreatedAt)
            .Take(100)
            .Select(m => new
            {
                m.Id,
                m.Payload,
                m.CreatedAt,
                m.ProcessedAt,
                Status = m.ProcessedAt == null ? "Pending" : "Published"
            }),
        pendingOutbox = outbox.Count(m => m.ProcessedAt == null)
    });
});

app.Run();

static async Task WriteLancamentosHealthAsync(HttpContext context, HealthReport report)
{
    var pending = 0;
    if (report.Entries.TryGetValue("outbox", out var outbox)
        && outbox.Data.TryGetValue("pending", out var pendingValue)
        && pendingValue is int pendingInt)
    {
        pending = pendingInt;
    }

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new HealthResponse(report.Status.ToString(), pending));
}

static bool IsSqliteConnection(string cs) =>
    cs.Contains("Data Source=", StringComparison.OrdinalIgnoreCase);

public partial class Program;
