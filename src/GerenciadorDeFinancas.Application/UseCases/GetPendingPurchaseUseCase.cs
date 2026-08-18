using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class GetPendingPurchaseUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public GetPendingPurchaseUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<PendingPurchaseItem> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var purchase = await unitOfWork.Purchases.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Compra não encontrada.");

        return new PendingPurchaseItem(
            purchase.Id,
            purchase.Description,
            purchase.Merchant?.DisplayName,
            purchase.Card?.Name ?? "Cartão",
            purchase.AmountCents,
            purchase.Date,
            purchase.Card!.OwnerPersonId);
    }
}
