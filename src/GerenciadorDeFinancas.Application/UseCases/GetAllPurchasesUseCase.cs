using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Domain.Abstractions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class GetAllPurchasesUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public GetAllPurchasesUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<IReadOnlyList<AllPurchaseItem>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var classified = await unitOfWork.Purchases.ListClassifiedAsync(cancellationToken);
        var cutoff = DateTime.UtcNow.AddMonths(-3);

        return classified
            .Where(p => p.Date >= cutoff)
            .Select(p => new AllPurchaseItem(
                p.Id,
                p.Description,
                p.Merchant?.DisplayName,
                p.Card?.Name ?? "Cartão",
                p.Date,
                p.AmountCents,
                p.Shares.Count,
                p.InstallmentNumber,
                p.TotalInstallments))
            .ToList();
    }
}
