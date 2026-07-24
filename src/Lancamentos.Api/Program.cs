using Cashflow.Contracts;
using Lancamentos.Api.Data;
using Lancamentos.Api.Messaging;
using Lancamentos.Api.Middleware;
using Lancamentos.Api.Services;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

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

app.MapGet("/health", async (EntryService entries, CancellationToken ct) =>
{
    var pending = await entries.CountPendingOutboxAsync(ct);
    return Results.Ok(new HealthResponse("Healthy", pending));
});

app.MapPost("/entries", async (CreateEntryRequest request, EntryService entries, CancellationToken ct) =>
{
    try
    {
        var created = await entries.CreateAsync(request, ct);
        return Results.Created($"/entries/{created.Id}", created);
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
        .OrderByDescending(m => m.CreatedAt)
        .Take(100)
        .Select(m => new
        {
            m.Id,
            m.Payload,
            m.CreatedAt,
            m.ProcessedAt,
            Status = m.ProcessedAt == null ? "Pending" : "Published"
        })
        .ToListAsync(ct);

    return Results.Ok(items);
});

app.MapGet("/admin/snapshot", async (LancamentosDbContext db, CancellationToken ct) =>
{
    var entries = await db.Entries.AsNoTracking().OrderByDescending(e => e.CreatedAt).Take(100).ToListAsync(ct);
    var outbox = await db.OutboxMessages.AsNoTracking().OrderByDescending(m => m.CreatedAt).Take(100).ToListAsync(ct);

    return Results.Ok(new
    {
        database = "lancamentos_db",
        entries = entries.Select(e => new
        {
            e.Id,
            Type = ((EntryType)e.Type).ToString(),
            e.Amount,
            e.Date,
            e.Description,
            e.CreatedAt
        }),
        outbox = outbox.Select(m => new
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

static bool IsSqliteConnection(string cs) =>
    cs.Contains("Data Source=", StringComparison.OrdinalIgnoreCase);

public partial class Program;
