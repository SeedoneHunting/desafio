using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Consolidado.Api.Data;

public sealed class ConsolidadoDbContextFactory : IDesignTimeDbContextFactory<ConsolidadoDbContext>
{
    public ConsolidadoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONSOLIDADO_DB")
            ?? "Host=localhost;Port=5432;Database=consolidado_db;Username=cashflow;Password=cashflow";
        var options = new DbContextOptionsBuilder<ConsolidadoDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new ConsolidadoDbContext(options);
    }
}
