using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lancamentos.Api.Data;

public sealed class LancamentosDbContextFactory : IDesignTimeDbContextFactory<LancamentosDbContext>
{
    public LancamentosDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("LANCAMENTOS_DB")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__LancamentosDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set LANCAMENTOS_DB (or ConnectionStrings__LancamentosDb) for design-time migrations. Do not hardcode credentials.");
        }

        var options = new DbContextOptionsBuilder<LancamentosDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new LancamentosDbContext(options);
    }
}
