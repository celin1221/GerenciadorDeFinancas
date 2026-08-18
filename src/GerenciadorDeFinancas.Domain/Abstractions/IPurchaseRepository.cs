using GerenciadorDeFinancas.Domain.Entities;

namespace GerenciadorDeFinancas.Domain.Abstractions;

public interface IPurchaseRepository
{
    Task<Purchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Purchase?> GetByDedupHashAsync(string dedupHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Purchase>> ListPendingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Purchase>> ListClassifiedAsync(CancellationToken cancellationToken = default);

    Task<int> CountIgnoredAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Purchase>> ListByStatementAsync(Guid statementId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Purchase>> ListByCardAsync(Guid cardId, CancellationToken cancellationToken = default);

    void Add(Purchase purchase);

    void AddShare(PurchaseShare share);

    void Update(Purchase purchase);

    Task<bool> HasUnpaidSharesForPersonAsync(Guid personId, CancellationToken cancellationToken = default);
}
