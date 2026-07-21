using Cashflow.Contracts;
using Lancamentos.Api.Data;
using Lancamentos.Api.Messaging;
using Lancamentos.Api.Middleware;
using Lancamentos.Api.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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

var enableWorkers = builder.Configuration.GetValue("Features:EnableBackgroundWorkers", true);
if (enableWorkers)
{
    builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
    builder.Services.AddHostedService<OutboxRelayWorker>();
}

var app = builder.Build();

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

app.Run();

static bool IsSqliteConnection(string cs) =>
    cs.Contains("Data Source=", StringComparison.OrdinalIgnoreCase);

public partial class Program;
