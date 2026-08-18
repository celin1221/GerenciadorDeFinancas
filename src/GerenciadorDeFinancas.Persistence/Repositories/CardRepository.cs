using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorDeFinancas.Persistence.Repositories;

public sealed class CardRepository : ICardRepository
{
    private readonly FinanceDbContext _context;

    public CardRepository(FinanceDbContext context)
    {
        _context = context;
    }

    public Task<Card?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Cards.FirstOrDefaultAsync(card => card.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Card>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        await _context.Cards
            .Where(card => card.IsActive)
            .OrderBy(card => card.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Card>> ListAsync(CancellationToken cancellationToken = default) =>
        await _context.Cards
            .Include(card => card.Owner)
            .OrderBy(card => card.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Card>> ListByBankAsync(string bankId, CancellationToken cancellationToken = default) =>
        await _context.Cards
            .Where(card => card.BankId == bankId && card.IsActive)
            .OrderBy(card => card.Name)
            .ToListAsync(cancellationToken);

    public Task<Card?> GetByBankAndLast4Async(string bankId, string last4, CancellationToken cancellationToken = default) =>
        _context.Cards.FirstOrDefaultAsync(
            card => card.BankId == bankId && card.Last4Digits == last4 && card.IsActive,
            cancellationToken);

    public void Add(Card card) => _context.Cards.Add(card);

    public void Update(Card card) => _context.Cards.Update(card);
}
