using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorDeFinancas.Persistence.Repositories;

public sealed class PurchaseRepository : IPurchaseRepository
{
    private readonly FinanceDbContext _context;

    public PurchaseRepository(FinanceDbContext context)
    {
        _context = context;
    }

    public Task<Purchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Purchases
            .Include(purchase => purchase.Card)
            .Include(purchase => purchase.Shares)
                .ThenInclude(share => share.Person)
            .Include(purchase => purchase.Merchant)
            .Include(purchase => purchase.Category)
            .FirstOrDefaultAsync(purchase => purchase.Id == id, cancellationToken);

    public Task<Purchase?> GetByDedupHashAsync(string dedupHash, CancellationToken cancellationToken = default) =>
        _context.Purchases.FirstOrDefaultAsync(purchase => purchase.DedupHash == dedupHash, cancellationToken);

    public async Task<IReadOnlyList<Purchase>> ListPendingAsync(CancellationToken cancellationToken = default) =>
        await _context.Purchases
            .Include(purchase => purchase.Card)
            .Include(purchase => purchase.Merchant)
            .Where(purchase => purchase.Status == Domain.Enums.PurchaseStatus.Pending)
            .OrderByDescending(purchase => purchase.Date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Purchase>> ListClassifiedAsync(CancellationToken cancellationToken = default) =>
        await _context.Purchases
            .Include(purchase => purchase.Card)
            .Include(purchase => purchase.Merchant)
            .Include(purchase => purchase.Category)
            .Include(purchase => purchase.Shares)
                .ThenInclude(share => share.Person)
            .Where(purchase => purchase.Status == Domain.Enums.PurchaseStatus.Classified)
            .OrderByDescending(purchase => purchase.Date)
            .ToListAsync(cancellationToken);

    public Task<int> CountIgnoredAsync(CancellationToken cancellationToken = default) =>
        _context.Purchases.CountAsync(purchase => purchase.Status == Domain.Enums.PurchaseStatus.Ignored, cancellationToken);

    public async Task<IReadOnlyList<Purchase>> ListByStatementAsync(Guid statementId, CancellationToken cancellationToken = default) =>
        await _context.Purchases
            .Include(purchase => purchase.Merchant)
            .Include(purchase => purchase.Category)
            .Include(purchase => purchase.Shares)
                .ThenInclude(share => share.Person)
            .Where(purchase => purchase.StatementId == statementId)
            .OrderByDescending(purchase => purchase.Date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Purchase>> ListByCardAsync(Guid cardId, CancellationToken cancellationToken = default) =>
        await _context.Purchases
            .Include(purchase => purchase.Merchant)
            .Where(purchase => purchase.CardId == cardId)
            .OrderByDescending(purchase => purchase.Date)
            .ToListAsync(cancellationToken);

    public void Add(Purchase purchase) => _context.Purchases.Add(purchase);

    public void AddShare(PurchaseShare share) => _context.PurchaseShares.Add(share);

    public void Update(Purchase purchase) => _context.Purchases.Update(purchase);

    public async Task<bool> HasUnpaidSharesForPersonAsync(Guid personId, CancellationToken cancellationToken = default) =>
        await _context.PurchaseShares
            .Include(share => share.Purchase)
                .ThenInclude(purchase => purchase!.Statement)
            .AnyAsync(share =>
                share.PersonId == personId &&
                share.Purchase != null &&
                (share.Purchase.Statement == null ||
                 share.Purchase.Statement.Status != Domain.Enums.StatementStatus.Paid),
            cancellationToken);
}
