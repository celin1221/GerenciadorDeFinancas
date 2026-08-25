using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class DeletePurchaseUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public DeletePurchaseUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<int> ExecuteAsync(
        Guid purchaseId,
        bool includeInstallmentGroup = false,
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId, cancellationToken)
            ?? throw new DomainException("Compra não encontrada.");

        var removed = 1;
        unitOfWork.Purchases.Remove(purchase);

        if (includeInstallmentGroup && purchase.InstallmentGroupId is { } groupId)
        {
            var siblings = await unitOfWork.Purchases.ListByGroupAsync(groupId, cancellationToken);
            foreach (var sibling in siblings.Where(item => item.Id != purchase.Id))
            {
                unitOfWork.Purchases.Remove(sibling);
                removed++;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return removed;
    }
}
