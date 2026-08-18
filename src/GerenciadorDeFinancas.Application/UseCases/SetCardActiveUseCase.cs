using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Exceptions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class SetCardActiveUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public SetCardActiveUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task ExecuteAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var card = await unitOfWork.Cards.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Cartão não encontrado.");

        if (isActive)
        {
            card.Reactivate();
        }
        else
        {
            card.Deactivate();
        }

        unitOfWork.Cards.Update(card);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
