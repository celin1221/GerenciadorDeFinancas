using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Domain.Abstractions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class ListPendingPurchasesUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public ListPendingPurchasesUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<IReadOnlyList<PendingPurchaseItem>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var purchases = await unitOfWork.Purchases.ListPendingAsync(cancellationToken);

        return purchases
            .Select(purchase => new PendingPurchaseItem(
                purchase.Id,
                purchase.Description,
                purchase.Merchant?.DisplayName,
                purchase.Card?.Name ?? "Cartão",
                purchase.AmountCents,
                purchase.Date,
                purchase.Card!.OwnerPersonId))
            .ToList();
    }
}
