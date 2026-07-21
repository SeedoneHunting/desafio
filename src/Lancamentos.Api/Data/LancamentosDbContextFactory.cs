using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lancamentos.Api.Data;

public sealed class LancamentosDbContextFactory : IDesignTimeDbContextFactory<LancamentosDbContext>
{
    public LancamentosDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("LANCAMENTOS_DB")
            ?? "Host=localhost;Port=5432;Database=lancamentos_db;Username=cashflow;Password=cashflow";
        var options = new DbContextOptionsBuilder<LancamentosDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new LancamentosDbContext(options);
    }
}
