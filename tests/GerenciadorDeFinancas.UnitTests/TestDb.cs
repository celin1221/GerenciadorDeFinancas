using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorDeFinancas.UnitTests;

public static class TestDb
{
    public static IUnitOfWorkFactory CreateUnitOfWorkFactory()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var context = new FinanceDbContext(options))
        {
            context.Database.EnsureCreated();
        }

        return new UnitOfWorkFactory(new TestDbContextFactory(options));
    }

    private sealed class TestDbContextFactory : IDbContextFactory<FinanceDbContext>
    {
        private readonly DbContextOptions<FinanceDbContext> _options;

        public TestDbContextFactory(DbContextOptions<FinanceDbContext> options)
        {
            _options = options;
        }

        public FinanceDbContext CreateDbContext() => new(_options);
    }
}
