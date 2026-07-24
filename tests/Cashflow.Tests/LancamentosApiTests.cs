using Cashflow.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cashflow.Tests;

public class LancamentosApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public LancamentosApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:LancamentosDb", $"Data Source={Path.GetTempFileName()}");
            builder.UseSetting("Features:EnableBackgroundWorkers", "false");
            builder.UseSetting("Cors:AllowedOrigin", "http://localhost:3000");
        }).CreateClient();
    }

    [Fact]
    public async Task CreateEntry_ReturnsCreated_WithValidPayload()
    {
        var request = new CreateEntryRequest(
            Guid.NewGuid(),
            EntryType.Credit,
            100m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Venda");

        var response = await _client.PostAsJsonAsync("/entries", request, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<EntryResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(100m, body.Amount);
        Assert.Equal(request.ExternalId, body.ExternalId);
    }

    [Fact]
    public async Task CreateEntry_IsIdempotent_ForSameExternalId()
    {
        var externalId = Guid.NewGuid();
        var request = new CreateEntryRequest(
            externalId,
            EntryType.Credit,
            100m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Venda");

        var first = await _client.PostAsJsonAsync("/entries", request, JsonOptions);
        var second = await _client.PostAsJsonAsync("/entries", request, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var firstBody = await first.Content.ReadFromJsonAsync<EntryResponse>(JsonOptions);
        var secondBody = await second.Content.ReadFromJsonAsync<EntryResponse>(JsonOptions);

        Assert.NotNull(firstBody);
        Assert.NotNull(secondBody);
        Assert.Equal(firstBody.Id, secondBody.Id);
        Assert.Equal(externalId, secondBody.ExternalId);

        var listResponse = await _client.GetAsync("/entries");
        var listBody = await listResponse.Content.ReadAsStringAsync();
        Assert.True(listResponse.IsSuccessStatusCode, listBody);
        var list = JsonSerializer.Deserialize<List<EntryResponse>>(listBody, JsonOptions);
        Assert.NotNull(list);
        Assert.Single(list, e => e.ExternalId == externalId);
    }

    [Fact]
    public async Task CreateEntry_ReturnsBadRequest_WhenAmountIsZero()
    {
        var request = new CreateEntryRequest(
            Guid.NewGuid(),
            EntryType.Debit,
            0m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Invalid");

        var response = await _client.PostAsJsonAsync("/entries", request, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
