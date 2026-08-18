using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorDeFinancas.Persistence.Repositories;

public sealed class MerchantRepository : IMerchantRepository
{
    private readonly FinanceDbContext _context;

    public MerchantRepository(FinanceDbContext context)
    {
        _context = context;
    }

    public Task<Merchant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Merchants.FirstOrDefaultAsync(merchant => merchant.Id == id, cancellationToken);

    public Task<Merchant?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default) =>
        _context.Merchants.FirstOrDefaultAsync(
            merchant => merchant.NormalizedName == normalizedName,
            cancellationToken);

    public void Add(Merchant merchant) => _context.Merchants.Add(merchant);

    public void Update(Merchant merchant) => _context.Merchants.Update(merchant);
}
