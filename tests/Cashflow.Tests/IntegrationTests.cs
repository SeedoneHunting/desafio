extern alias ConsolidadoApi;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cashflow.Contracts;
using ConsolidadoApi::Consolidado.Api.Data;
using ConsolidadoApi::Consolidado.Api.Services;
using Lancamentos.Api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cashflow.Tests;

public class IntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task EntryOutbox_ProjectsToConsolidatedBalance()
    {
        var lancamentosDbPath = Path.GetTempFileName();
        var consolidadoDbPath = Path.GetTempFileName();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        await using var lancamentosFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:LancamentosDb", $"Data Source={lancamentosDbPath}");
                builder.UseSetting("Features:EnableBackgroundWorkers", "false");
            });

        var lancamentosClient = lancamentosFactory.CreateClient();

        await lancamentosClient.PostAsJsonAsync("/entries", new CreateEntryRequest(
            Guid.NewGuid(), EntryType.Credit, 100m, date, "Venda"), JsonOptions);
        await lancamentosClient.PostAsJsonAsync("/entries", new CreateEntryRequest(
            Guid.NewGuid(), EntryType.Debit, 25.50m, date, "Fornecedor"), JsonOptions);

        await using (var scope = lancamentosFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
            var messages = await db.OutboxMessages.Where(m => m.ProcessedAt == null).ToListAsync();
            Assert.Equal(2, messages.Count);

            var consolidadoOptions = new DbContextOptionsBuilder<ConsolidadoDbContext>()
                .UseSqlite($"Data Source={consolidadoDbPath}")
                .Options;

            await using var consolidadoDb = new ConsolidadoDbContext(consolidadoOptions);
            await consolidadoDb.Database.EnsureCreatedAsync();

            var projection = new BalanceProjectionService(
                consolidadoDb,
                new MemoryCache(new MemoryCacheOptions()),
                NullLogger<BalanceProjectionService>.Instance);

            foreach (var message in messages)
            {
                var domainEvent = JsonSerializer.Deserialize<EntryCreatedEvent>(message.Payload, JsonOptions);
                Assert.NotNull(domainEvent);
                await projection.ProcessEventAsync(domainEvent, CancellationToken.None);
            }

            var balance = await projection.GetByDateAsync(date, CancellationToken.None);
            Assert.NotNull(balance);
            Assert.Equal(74.50m, balance.Balance);
        }
    }
}
