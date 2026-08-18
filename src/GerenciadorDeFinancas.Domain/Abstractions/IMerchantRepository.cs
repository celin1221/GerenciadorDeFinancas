using GerenciadorDeFinancas.Domain.Entities;

namespace GerenciadorDeFinancas.Domain.Abstractions;

public interface IMerchantRepository
{
    Task<Merchant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Merchant?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);

    void Add(Merchant merchant);

    void Update(Merchant merchant);
}
