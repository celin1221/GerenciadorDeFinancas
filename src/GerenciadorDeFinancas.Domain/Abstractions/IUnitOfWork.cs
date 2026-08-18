namespace GerenciadorDeFinancas.Domain.Abstractions;

public interface IUnitOfWork : IDisposable
{
    IPersonRepository Persons { get; }

    ICardRepository Cards { get; }

    IStatementRepository Statements { get; }

    IPurchaseRepository Purchases { get; }

    IMerchantRepository Merchants { get; }

    ICategoryRepository Categories { get; }

    INotificationButtonRepository NotificationButtons { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
