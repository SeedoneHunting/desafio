using Lancamentos.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Cashflow.Tests;

public class DecimalPrecisionTests
{
    [Fact]
    public void EntryAmount_IsMappedAsNumeric18_2()
    {
        var options = new DbContextOptionsBuilder<LancamentosDbContext>()
            .UseSqlite($"Data Source={Path.GetTempFileName()}")
            .Options;

        using var db = new LancamentosDbContext(options);
        var amount = db.Model.FindEntityType(typeof(EntryEntity))!
            .FindProperty(nameof(EntryEntity.Amount))!;

        Assert.Equal(typeof(decimal), amount.ClrType);
        Assert.Equal(18, amount.GetPrecision());
        Assert.Equal(2, amount.GetScale());
    }
}
