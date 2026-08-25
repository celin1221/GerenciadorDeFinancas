using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class ReopenPurchaseUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public ReopenPurchaseUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task ExecuteAsync(Guid purchaseId, CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId, cancellationToken)
            ?? throw new DomainException("Compra não encontrada.");

        purchase.Reopen();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
