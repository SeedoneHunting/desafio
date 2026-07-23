using Cashflow.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace Cashflow.Tests;

public class LancamentosApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public LancamentosApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:LancamentosDb", $"Data Source={Path.GetTempFileName()}");
            builder.UseSetting("Features:EnableBackgroundWorkers", "false");
        }).CreateClient();
    }

    [Fact]
    public async Task CreateEntry_ReturnsCreated_WithValidPayload()
    {
        var request = new CreateEntryRequest(
            EntryType.Credit,
            100m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Venda");

        var response = await _client.PostAsJsonAsync("/entries", request);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<EntryResponse>();
        Assert.NotNull(body);
        Assert.Equal(100m, body.Amount);
    }

    [Fact]
    public async Task CreateEntry_ReturnsBadRequest_WhenAmountIsZero()
    {
        var request = new CreateEntryRequest(
            EntryType.Debit,
            0m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Invalid");

        var response = await _client.PostAsJsonAsync("/entries", request);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Health_ReturnsPendingOutboxCount()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(health);
        Assert.Equal("Healthy", health.Status);
        Assert.True(health.PendingOutboxCount >= 0);
    }
}
