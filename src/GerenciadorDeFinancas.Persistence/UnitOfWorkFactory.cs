using GerenciadorDeFinancas.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorDeFinancas.Persistence;

public sealed class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IDbContextFactory<FinanceDbContext> _factory;

    public UnitOfWorkFactory(IDbContextFactory<FinanceDbContext> factory)
    {
        _factory = factory;
    }

    public IUnitOfWork Create() => new UnitOfWork(_factory.CreateDbContext());
}
