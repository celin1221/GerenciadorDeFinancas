using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Persistence.Repositories;

namespace GerenciadorDeFinancas.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly FinanceDbContext _context;
    private bool _disposed;

    public IPersonRepository Persons { get; }

    public ICardRepository Cards { get; }

    public IStatementRepository Statements { get; }

    public IPurchaseRepository Purchases { get; }

    public IMerchantRepository Merchants { get; }

    public ICategoryRepository Categories { get; }

    public INotificationButtonRepository NotificationButtons { get; }

    public UnitOfWork(FinanceDbContext context)
    {
        _context = context;
        Persons = new PersonRepository(context);
        Cards = new CardRepository(context);
        Statements = new StatementRepository(context);
        Purchases = new PurchaseRepository(context);
        Merchants = new MerchantRepository(context);
        Categories = new CategoryRepository(context);
        NotificationButtons = new NotificationButtonRepository(context);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _context.Dispose();
        _disposed = true;
    }
}
