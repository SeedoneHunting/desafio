using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Consolidado.Api.Data;

public sealed class ConsolidadoDbContextFactory : IDesignTimeDbContextFactory<ConsolidadoDbContext>
{
    public ConsolidadoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONSOLIDADO_DB")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__ConsolidadoDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set CONSOLIDADO_DB (or ConnectionStrings__ConsolidadoDb) for design-time migrations. Do not hardcode credentials.");
        }

        var options = new DbContextOptionsBuilder<ConsolidadoDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new ConsolidadoDbContext(options);
    }
}
