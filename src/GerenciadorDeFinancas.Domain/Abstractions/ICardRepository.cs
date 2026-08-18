using GerenciadorDeFinancas.Domain.Entities;

namespace GerenciadorDeFinancas.Domain.Abstractions;

public interface ICardRepository
{
    Task<Card?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Card>> ListActiveAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Card>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Card>> ListByBankAsync(string bankId, CancellationToken cancellationToken = default);

    Task<Card?> GetByBankAndLast4Async(string bankId, string last4, CancellationToken cancellationToken = default);

    void Add(Card card);

    void Update(Card card);
}
