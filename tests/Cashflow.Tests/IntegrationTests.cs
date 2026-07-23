extern alias ConsolidadoApi;
using System.Net.Http.Json;
using System.Text.Json;
using Cashflow.Contracts;
using ConsolidadoApi::Consolidado.Api.Data;
using ConsolidadoApi::Consolidado.Api.Services;
using Lancamentos.Api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task EntryOutbox_ProjectsToConsolidatedBalance()
    {
        var lancamentosDbPath = Path.GetTempFileName();
        var consolidadoDbPath = Path.GetTempFileName();
        var date = new DateOnly(2026, 1, 25);

        await using var lancamentosFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:LancamentosDb", $"Data Source={lancamentosDbPath}");
                builder.UseSetting("Features:EnableBackgroundWorkers", "false");
            });

        var lancamentosClient = lancamentosFactory.CreateClient();

        await lancamentosClient.PostAsJsonAsync("/entries", new CreateEntryRequest(
            EntryType.Credit, 100m, date, "Venda"));
        await lancamentosClient.PostAsJsonAsync("/entries", new CreateEntryRequest(
            EntryType.Debit, 25.50m, date, "Fornecedor"));

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
                new MemoryCache(new MemoryCacheOptions()));

            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

            foreach (var message in messages)
            {
                var domainEvent = JsonSerializer.Deserialize<EntryCreatedEvent>(message.Payload, jsonOptions);
                Assert.NotNull(domainEvent);
                await projection.ProcessEventAsync(domainEvent, CancellationToken.None);
            }

            var balance = await projection.GetByDateAsync(date, CancellationToken.None);
            Assert.NotNull(balance);
            Assert.Equal(74.50m, balance.Balance);
        }
    }
}
