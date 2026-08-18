using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class ClassifyPurchaseUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public ClassifyPurchaseUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task ExecuteAsync(Guid purchaseId, Guid personId, CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId, cancellationToken)
            ?? throw new DomainException("Compra não encontrada.");
        if (await unitOfWork.Persons.GetByIdAsync(personId, cancellationToken) is null)
        {
            throw new DomainException("Pessoa não encontrada.");
        }

        purchase.AssignToSingle(personId);
        foreach (var share in purchase.Shares)
        {
            unitOfWork.Purchases.AddShare(share);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
