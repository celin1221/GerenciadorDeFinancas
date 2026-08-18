using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorDeFinancas.Persistence;

public sealed class DbInitializer : IDbInitializer
{
    private readonly IDbContextFactory<FinanceDbContext> _factory;

    public DbInitializer(IDbContextFactory<FinanceDbContext> factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        await SeedCategoriesAsync(context, cancellationToken);
    }

    private static async Task SeedCategoriesAsync(
        FinanceDbContext context,
        CancellationToken cancellationToken)
    {
        var existing = await context.Categories.ToListAsync(cancellationToken);
        foreach (var seed in DefaultSeed.Categories)
        {
            if (existing.Any(category => string.Equals(category.Name, seed.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            context.Categories.Add(new Category(seed.Name, seed.Icon, seed.Color, isSystem: true));
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
